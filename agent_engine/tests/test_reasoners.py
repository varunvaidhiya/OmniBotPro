"""Tests for the Claude tool-calling reasoner (backend-agnostic via router)."""

from __future__ import annotations

import json
import unittest

from agent_engine.core.harness import AgentHarness
from agent_engine.core.blackboard import WorldState
from agent_engine.core.types import Goal, GoalStatus, ToolResult
from agent_engine.reasoners.claude_tool_caller import ClaudeToolCallingReasoner
from agent_engine.reasoning.factory import build_reasoning_router
from agent_engine.reasoning.router import EchoBackend, ReasoningRouter

from .helpers import FakePerceptor, make_registry, make_world


def reasoner_with(responder):
    router = ReasoningRouter([EchoBackend(responder)])
    return ClaudeToolCallingReasoner(router, make_registry([]))


class TestClaudeToolCallingReasoner(unittest.TestCase):
    def test_parses_tool_call(self):
        reply = json.dumps(
            {
                "rationale": "head to the kitchen",
                "tool_calls": [
                    {"tool": "navigate_to", "args": {"location": "kitchen"}}
                ],
                "goal_complete": False,
            }
        )
        plan = reasoner_with(lambda p, s: reply).plan(Goal("go"), WorldState(), "", ())
        self.assertEqual(len(plan.calls), 1)
        self.assertEqual(plan.calls[0].tool, "navigate_to")
        self.assertEqual(plan.calls[0].args["location"], "kitchen")
        self.assertFalse(plan.goal_complete)

    def test_parses_goal_complete(self):
        plan = reasoner_with(lambda p, s: '{"goal_complete": true}').plan(
            Goal("g"), WorldState(), "", ()
        )
        self.assertTrue(plan.goal_complete)
        self.assertEqual(plan.calls, [])

    def test_parses_ask_human(self):
        plan = reasoner_with(lambda p, s: '{"ask_human": "which cup?"}').plan(
            Goal("g"), WorldState(), "", ()
        )
        self.assertEqual(plan.ask_human, "which cup?")

    def test_unparseable_reply_gives_up(self):
        plan = reasoner_with(lambda p, s: "the model rambled with no json").plan(
            Goal("g"), WorldState(), "", ()
        )
        self.assertTrue(plan.give_up)

    def test_prompt_includes_world_memory_and_tools(self):
        captured = {}

        def responder(prompt, system):
            captured["prompt"] = prompt
            captured["system"] = system
            return '{"goal_complete": true}'

        reasoner_with(responder).plan(
            Goal("fetch mug"),
            make_world(mission_phase="navigating"),
            "Known objects: mug at office",
            (ToolResult(False, "nav timeout"),),
        )
        self.assertIn("fetch mug", captured["prompt"])
        self.assertIn("navigating", captured["prompt"])
        self.assertIn("mug at office", captured["prompt"])
        self.assertIn("nav timeout", captured["prompt"])
        self.assertIn("navigate_to", captured["system"])  # tool schema injected

    def test_drives_harness_to_success(self):
        # Stateful echo: navigate first, then report complete.
        calls = {"n": 0}

        def responder(prompt, system):
            calls["n"] += 1
            if calls["n"] == 1:
                return json.dumps(
                    {
                        "tool_calls": [
                            {"tool": "navigate_to", "args": {"location": "lab"}}
                        ]
                    }
                )
            return '{"goal_complete": true}'

        router = ReasoningRouter([EchoBackend(responder)])
        log = []
        harness = AgentHarness(
            FakePerceptor(make_world()),
            ClaudeToolCallingReasoner(router, make_registry(log)),
            make_registry(log),
        )
        goal = harness.submit_goal("go to the lab")
        harness.run_until_idle()
        self.assertIn("navigate_to:lab", log)
        self.assertEqual(goal.status, GoalStatus.SUCCEEDED)


class TestRouterFactory(unittest.TestCase):
    def test_builds_with_echo_fallback_always_available(self):
        router = build_reasoning_router(api_key="")
        status = router.status()
        self.assertIn("echo", status)
        self.assertTrue(status["echo"])
        # Cloud unavailable without a key/anthropic -> echo still serves.
        self.assertEqual(router.select().name, "echo")

    def test_local_runner_becomes_available(self):
        router = build_reasoning_router(local_runner=lambda p, s: "local reply")
        self.assertTrue(router.status()["local_llm"])
        self.assertEqual(router.complete("x", kind="reactive"), "local reply")


if __name__ == "__main__":
    unittest.main()
