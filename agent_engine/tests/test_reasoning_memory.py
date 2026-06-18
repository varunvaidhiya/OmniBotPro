"""Tests for the reasoning router and working memory."""

from __future__ import annotations

import unittest

from agent_engine.core.blackboard import WorldState
from agent_engine.core.types import Goal, Reflection
from agent_engine.memory.working_memory import WorkingMemory
from agent_engine.reasoning.cloud_claude import CloudClaudeBackend
from agent_engine.reasoning.local_llm import LocalLLMBackend
from agent_engine.reasoning.router import EchoBackend, ReasoningRouter

from .helpers import FakeEntityStore


class _Unavailable:
    name = "down"

    def available(self):
        return False

    def complete(self, prompt, system="", images=None):  # pragma: no cover
        raise AssertionError("should not be called")


class TestReasoningRouter(unittest.TestCase):
    def test_prefers_first_available(self):
        echo = EchoBackend(lambda p, s: "echo!")
        router = ReasoningRouter([_Unavailable(), echo])
        self.assertIs(router.select(), echo)
        self.assertEqual(router.complete("hi"), "echo!")

    def test_falls_back_when_preferred_unavailable(self):
        echo = EchoBackend(lambda p, s: "fallback")
        router = ReasoningRouter(
            [echo, _Unavailable()], deliberate_order=["down", "echo"]
        )
        # 'down' preferred but unavailable -> echo used.
        self.assertEqual(router.complete("x", kind="deliberate"), "fallback")

    def test_raises_when_none_available(self):
        router = ReasoningRouter([_Unavailable()])
        with self.assertRaises(RuntimeError):
            router.complete("x")

    def test_status(self):
        router = ReasoningRouter([EchoBackend(), _Unavailable()])
        self.assertEqual(router.status(), {"echo": True, "down": False})

    def test_cloud_backend_unavailable_without_key(self):
        backend = CloudClaudeBackend(api_key="")
        self.assertFalse(backend.available())

    def test_local_backend_available_only_with_runner(self):
        self.assertFalse(LocalLLMBackend().available())
        b = LocalLLMBackend(runner=lambda p, s: "local")
        self.assertTrue(b.available())
        self.assertEqual(b.complete("hi"), "local")


class TestWorkingMemory(unittest.TestCase):
    def test_context_includes_entity_summary(self):
        store = FakeEntityStore()
        store.remember_object("red_cup", "kitchen")
        mem = WorkingMemory(entity_store=store)
        ctx = mem.context_for(Goal("find the cup"), WorldState())
        self.assertIn("red_cup", ctx)
        self.assertIn("kitchen", ctx)

    def test_context_uses_world_summary_without_store(self):
        mem = WorkingMemory(entity_store=None)
        ws = WorldState(memory_summary="- mug: last seen at office")
        self.assertIn("mug", mem.context_for(Goal("g"), ws))

    def test_record_outcome_writes_back_and_appears_in_context(self):
        store = FakeEntityStore()
        mem = WorkingMemory(entity_store=store)
        refl = Reflection(
            success=True, summary="found it", learned_objects={"mug": "office"}
        )
        mem.record_outcome(Goal("fetch mug"), refl)
        self.assertEqual(store.recall_object("mug")["last_seen_location"], "office")
        ctx = mem.context_for(Goal("again"), WorldState())
        self.assertIn("Recent goal outcomes", ctx)
        self.assertIn("fetch mug", ctx)

    def test_recent_outcomes_are_bounded(self):
        mem = WorkingMemory(max_recent=2)
        for i in range(3):
            mem.record_outcome(Goal(f"g{i}"), Reflection(success=True, summary="ok"))
        ctx = mem.context_for(Goal("x"), WorldState())
        self.assertNotIn("g0", ctx)  # evicted
        self.assertIn("g2", ctx)


if __name__ == "__main__":
    unittest.main()
