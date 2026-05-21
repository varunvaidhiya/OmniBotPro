import operator
from typing import Annotated, List, Optional

try:
    from langchain_core.messages import BaseMessage
except ImportError as _e:
    raise ImportError(
        "langchain_core is required for MissionState. "
        "Install with: pip install langchain-core"
    ) from _e
from typing_extensions import TypedDict


class MissionState(TypedDict):
    """
    Persistent state threaded through every node in the LangGraph mission graph.

    LangGraph merges node return dicts into this state. Fields not returned by
    a node are unchanged. The `messages` field uses operator.add so each node
    appends rather than overwrites.
    """

    # ── Input ─────────────────────────────────────────────────────────────
    user_input: str

    # ── Conversation history (append-only via reducer) ────────────────────
    messages: Annotated[List[BaseMessage], operator.add]

    # ── Parsed intent ─────────────────────────────────────────────────────
    nav_location: Optional[str]  # location name to navigate to
    vla_task: Optional[str]  # VLA task description
    return_home: bool  # navigate back to "home" after VLA task

    # ── Execution tracking ────────────────────────────────────────────────
    phase: str  # idle | navigating | vla | observing | recovering | done
    retry_count: int
    last_error: Optional[str]

    # ── Human interaction ─────────────────────────────────────────────────
    requires_human: bool
    human_question: str

    # ── Output ────────────────────────────────────────────────────────────
    response: str


def initial_state(user_input: str) -> MissionState:
    return MissionState(
        user_input=user_input,
        messages=[],
        nav_location=None,
        vla_task=None,
        return_home=False,
        phase="idle",
        retry_count=0,
        last_error=None,
        requires_human=False,
        human_question="",
        response="",
    )
