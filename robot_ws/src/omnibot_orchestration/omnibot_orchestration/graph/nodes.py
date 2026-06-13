"""
LangGraph node functions for the OmniBot mission state machine.

Each node receives the current MissionState and a RunnableConfig that carries
the ROS node reference in config['configurable']['ros_node']. Nodes return a
partial dict of state fields to update.

LLM is used in only two nodes:
  - parse_intent: understands the user request
  - recover: decides retry vs escalate after failure
All other nodes are pure ROS publishers or local logic — no LLM calls.
"""

import json
import re
import threading
import time
from typing import Any, Dict

from langchain_anthropic import ChatAnthropic
from langchain_core.messages import AIMessage, HumanMessage, SystemMessage
from langchain_core.runnables import RunnableConfig

from .state import MissionState

# ---------------------------------------------------------------------------
# Shared LLM (lazy-initialised per process)
# ---------------------------------------------------------------------------

_llm: Any = None


def _get_llm(api_key: str = "") -> ChatAnthropic:
    global _llm
    if _llm is None:
        _llm = ChatAnthropic(
            model="claude-sonnet-4-6",
            anthropic_api_key=api_key or None,
            max_tokens=1024,
            temperature=0.1,
        )
    return _llm


def _ros_node(config: RunnableConfig):
    return config["configurable"]["ros_node"]


# ---------------------------------------------------------------------------
# Node 1: parse_intent
# ---------------------------------------------------------------------------

_PARSE_SYSTEM = """You are an intent parser for OmniBot, a mobile manipulation robot.
Extract the user's intent as JSON with these fields:
  nav_location  : string or null   — named location to navigate to (null if not needed)
  vla_task      : string or null   — manipulation task description (null if not needed)
  return_home   : boolean          — true if user wants robot to return to "home" after task
  observe_only  : boolean          — true if user just wants a scene description
  response      : string           — brief acknowledgment of what you understood

KNOWN LOCATIONS: {locations}

Rules:
- If the user says "fetch X from Y", set nav_location=Y, vla_task="pick up X", return_home=true
- If the user says "go to X", set nav_location=X only
- If the user says "pick up / grab / manipulate X" with no location, set vla_task only
- If the user says "what do you see" or "describe", set observe_only=true
- If the user says "plan: ...", treat it as a complex mission — extract best nav+vla intent
- Return ONLY valid JSON, no markdown fences."""


def parse_intent_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    locations = sorted(node._locations.keys()) if node else []
    api_key = node._api_key if node else ""

    llm = _get_llm(api_key)
    system = _PARSE_SYSTEM.format(locations=", ".join(locations) or "none")

    response = llm.invoke(
        [
            SystemMessage(content=system),
            HumanMessage(content=state["user_input"]),
        ]
    )

    try:
        raw = response.content
        # Strip markdown fences if model wraps in them
        raw = re.sub(r"```(?:json)?", "", raw).strip().strip("`")
        parsed = json.loads(raw)
    except (json.JSONDecodeError, AttributeError):
        parsed = {
            "nav_location": None,
            "vla_task": state["user_input"],
            "return_home": False,
            "observe_only": False,
            "response": f"Executing: {state['user_input']}",
        }

    updates: Dict = {
        "nav_location": parsed.get("nav_location"),
        "vla_task": parsed.get("vla_task"),
        "return_home": bool(parsed.get("return_home", False)),
        "response": parsed.get("response", ""),
        "messages": [
            HumanMessage(content=state["user_input"]),
            AIMessage(content=parsed.get("response", "")),
        ],
    }

    # observe_only: set a sentinel so router sends to observe node
    if parsed.get("observe_only"):
        updates["nav_location"] = None
        updates["vla_task"] = "__observe__"

    return updates


# ---------------------------------------------------------------------------
# Node 2: navigate
# ---------------------------------------------------------------------------


def _wait_for_acceptance(node: Any, phase: str, timeout: float = 5.0) -> bool:
    """Poll mission status briefly to check if the command was accepted."""
    deadline = time.time() + timeout
    while time.time() < deadline:
        status = node._latest_mission_status if node else ""
        if f"phase={phase}" in status:
            return True
        if "idle" in status and phase not in status:
            # Planner reverted to idle without entering this phase — likely failure
            return False
        threading.Event().wait(0.2)
    return False  # timeout


def navigate_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    location = state["nav_location"]

    if node:
        node._publish_mission(f"navigate:{location}")
        node.get_logger().info(f"[Graph] navigate → '{location}'")
        if not _wait_for_acceptance(node, "navigating"):
            node.get_logger().error(
                f"[Graph] Navigate mission was not accepted for '{location}'."
            )
            return {"phase": "failed", "last_error": f"Navigation to {location} failed"}

    return {"phase": "navigating"}


# ---------------------------------------------------------------------------
# Node 3: execute_vla
# ---------------------------------------------------------------------------


def execute_vla_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    task = state["vla_task"]

    if node:
        node._publish_mission(f"vla:{task}")
        node.get_logger().info(f"[Graph] execute_vla → '{task}'")
        if not _wait_for_acceptance(node, "vla"):
            node.get_logger().error(
                f"[Graph] VLA mission was not accepted for '{task}'."
            )
            return {"phase": "failed", "last_error": f"VLA task '{task}' failed"}

    return {"phase": "vla"}


# ---------------------------------------------------------------------------
# Node 4: observe
# ---------------------------------------------------------------------------


def observe_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    description = "No camera available."

    if node and node._vision:
        description = node._vision.describe_scene(camera="front")

    # Augment the visual description with metric object perception
    # (distance / bearing / 3-D position from object_perception_node),
    # so the agent can ground language in real measurements.
    if node is not None:
        try:
            perception = node.get_fresh_perception()
        except AttributeError:
            perception = None
        if perception and perception != "[]":
            description += (
                "\n\nMetric object detections (base_link frame, from the "
                f"depth camera): {perception}"
            )

    return {
        "phase": "observing",
        "response": description,
        "messages": [AIMessage(content=description)],
    }


# ---------------------------------------------------------------------------
# Node 5: recover
# ---------------------------------------------------------------------------

_RECOVER_SYSTEM = """You are the recovery planner for OmniBot.
A mission step failed. Decide what to do:
  - If retry_count < 2: suggest a recovery action (retry with clearer task description)
  - If retry_count >= 2: recommend escalating to a human

Respond as JSON:
  {"action": "retry" | "escalate",
   "refined_task": "...",      (only if action=retry; improved task description)
   "question": "...",          (only if action=escalate; question for the operator)}"""


def recover_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    api_key = node._api_key if node else ""

    # Cancel any running mission
    if node:
        node._publish_cancel("recover")

    retry_count = state["retry_count"] + 1
    error = state.get("last_error", "unknown error")

    llm = _get_llm(api_key)
    context = (
        f"Failed phase: {state['phase']}\n"
        f"Error: {error}\n"
        f"retry_count (after increment): {retry_count}\n"
        f"Original task: {state.get('vla_task', '')}\n"
        f"Navigation target: {state.get('nav_location', '')}"
    )

    response = llm.invoke(
        [SystemMessage(content=_RECOVER_SYSTEM), HumanMessage(content=context)]
    )

    try:
        raw = re.sub(r"```(?:json)?", "", response.content).strip().strip("`")
        plan = json.loads(raw)
    except (json.JSONDecodeError, AttributeError):
        plan = {"action": "escalate", "question": f"Mission failed: {error}"}

    updates: Dict = {"retry_count": retry_count, "phase": "recovering"}

    if plan.get("action") == "retry" and retry_count < 3:
        if plan.get("refined_task"):
            updates["vla_task"] = plan["refined_task"]
    else:
        question = plan.get(
            "question",
            f"Mission failed after {retry_count} attempts. How should I proceed?",
        )
        updates["requires_human"] = True
        updates["human_question"] = question
        # Publish clarification BEFORE the graph halts at human_checkpoint intercept.
        # The human_checkpoint_node itself is guarded by interrupt_before and never
        # executes — this publish here ensures the user actually sees the question.
        if node:
            from std_msgs.msg import String

            resp_msg = String()
            resp_msg.data = question
            node._resp_pub.publish(resp_msg)
            node.get_logger().info(f"[recover] Awaiting human response: {question}")

    return updates


# ---------------------------------------------------------------------------
# Node 6: human_checkpoint
# ---------------------------------------------------------------------------


def human_checkpoint_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)
    question = state.get("human_question", "Clarification needed.")

    if node:
        from std_msgs.msg import String

        msg = String()
        msg.data = question
        node._response_needed_pub.publish(msg)
        node.get_logger().info(f"[Graph] Awaiting human input: {question}")

    return {
        "requires_human": True,
        "phase": "waiting_human",
        "response": f"Waiting for operator: {question}",
    }


# ---------------------------------------------------------------------------
# Node 7: finalize
# ---------------------------------------------------------------------------


def finalize_node(state: MissionState, config: RunnableConfig) -> Dict:
    node = _ros_node(config)

    response = state.get("response") or "Mission complete."
    if not response or response == "":
        phase = state.get("phase", "done")
        nav = state.get("nav_location")
        task = state.get("vla_task")
        parts = []
        if nav:
            parts.append(f"navigated to {nav}")
        if task and task != "__observe__":
            parts.append(f"executed '{task}'")
        response = "Done: " + ", ".join(parts) if parts else f"Completed ({phase})."

    if node:
        node._publish_ai_status(f"DONE: {response}")
        node.get_logger().info(f"[Graph] Mission finalized: {response}")

    return {"phase": "done", "response": response}
