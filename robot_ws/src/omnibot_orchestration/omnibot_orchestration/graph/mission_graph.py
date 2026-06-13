"""
LangGraph StateGraph for OmniBot mission execution.

Graph topology:
  parse_intent → [route] → navigate → [success/fail] → execute_vla / recover / finalize
                         → execute_vla → [success/fail] → finalize / recover / navigate(return)
                         → observe → finalize
  recover → [retry/escalate] → navigate|execute_vla | human_checkpoint
  human_checkpoint → [INTERRUPT — graph suspends, resumes on next /ai/command]
  finalize → END

The ROS node reference is passed via LangGraph config.configurable so nodes
can publish to ROS topics without holding a global reference.
"""

from typing import Any, Literal

try:
    from langgraph.checkpoint.memory import MemorySaver
    from langgraph.graph import END, StateGraph
except ImportError as _e:
    raise ImportError(
        "langgraph is required for the mission graph. "
        "Install with: pip install langgraph"
    ) from _e

from .nodes import (
    execute_vla_node,
    finalize_node,
    human_checkpoint_node,
    navigate_node,
    observe_node,
    parse_intent_node,
    recover_node,
)
from .state import MissionState


# ---------------------------------------------------------------------------
# Routing functions (determine next node from state)
# ---------------------------------------------------------------------------


def route_after_parse(
    state: MissionState,
) -> Literal["navigate", "execute_vla", "observe", "finalize"]:
    if state.get("vla_task") == "__observe__":
        return "observe"
    if state.get("nav_location") and state.get("vla_task"):
        return "navigate"
    if state.get("nav_location"):
        return "navigate"
    if state.get("vla_task"):
        return "execute_vla"
    return "finalize"


def route_after_navigate(
    state: MissionState,
) -> Literal["execute_vla", "finalize", "recover"]:
    if state.get("last_error") or state.get("phase") == "failed":
        return "recover"
    if state.get("vla_task"):
        return "execute_vla"
    return "finalize"


def route_after_vla(
    state: MissionState,
) -> Literal["navigate", "finalize", "recover"]:
    if state.get("last_error") or state.get("phase") == "failed":
        return "recover"
    if state.get("return_home") and state.get("phase") != "returning":
        return "navigate"
    return "finalize"


def route_after_recover(
    state: MissionState,
) -> Literal["navigate", "execute_vla", "human_checkpoint"]:
    if state.get("requires_human"):
        return "human_checkpoint"
    # Retry the failed phase
    ph = state.get("phase", "")
    if state.get("nav_location") and ph in ("navigating", "recovering", "failed"):
        return "navigate"
    if state.get("vla_task"):
        return "execute_vla"
    return "human_checkpoint"


# ---------------------------------------------------------------------------
# Graph assembly
# ---------------------------------------------------------------------------


def build_mission_graph(ros_node: Any):
    """
    Build and compile the mission StateGraph.

    Args:
        ros_node: LangchainAgentNode instance. Passed via config.configurable
                  at invoke time so nodes can publish to ROS topics.
                  Pass None for testing (nodes will skip ROS calls).

    Returns:
        Compiled LangGraph graph with MemorySaver checkpointer.
    """
    graph = StateGraph(MissionState)

    # Register nodes
    graph.add_node("parse_intent", parse_intent_node)
    graph.add_node("navigate", navigate_node)
    graph.add_node("execute_vla", execute_vla_node)
    graph.add_node("observe", observe_node)
    graph.add_node("recover", recover_node)
    graph.add_node("human_checkpoint", human_checkpoint_node)
    graph.add_node("finalize", finalize_node)

    # Entry point
    graph.set_entry_point("parse_intent")

    # Conditional routing after parse
    graph.add_conditional_edges(
        "parse_intent",
        route_after_parse,
        {
            "navigate": "navigate",
            "execute_vla": "execute_vla",
            "observe": "observe",
            "finalize": "finalize",
        },
    )

    # After navigation: go to VLA, finish, or recover
    graph.add_conditional_edges(
        "navigate",
        route_after_navigate,
        {
            "execute_vla": "execute_vla",
            "finalize": "finalize",
            "recover": "recover",
        },
    )

    # After VLA: return home, finish, or recover
    graph.add_conditional_edges(
        "execute_vla",
        route_after_vla,
        {
            "navigate": "navigate",
            "finalize": "finalize",
            "recover": "recover",
        },
    )

    # After observation: always finalize
    graph.add_edge("observe", "finalize")

    # Recovery routing: retry or escalate
    graph.add_conditional_edges(
        "recover",
        route_after_recover,
        {
            "navigate": "navigate",
            "execute_vla": "execute_vla",
            "human_checkpoint": "human_checkpoint",
        },
    )

    # Human checkpoint → END (graph suspends; resumes when re-invoked)
    graph.add_edge("human_checkpoint", END)

    # Finalize → END
    graph.add_edge("finalize", END)

    # Compile with in-memory checkpointer for state persistence
    checkpointer = MemorySaver()
    return graph.compile(
        checkpointer=checkpointer,
        interrupt_before=["human_checkpoint"],  # pause before asking the human
    )
