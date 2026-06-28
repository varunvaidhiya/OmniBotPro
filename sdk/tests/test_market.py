import unittest

from ohho.adapters.sim import SimTransport
from ohho.market import (
    Skill,
    SkillRequirementsNotMet,
    get_skill,
    list_skills,
    register_skill,
    run_skill,
    skill,
)
from ohho.registry import get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime


def _bot(robot_id="omnibot"):
    spec = get_spec(robot_id)
    tp = SimTransport(spec)
    tp.connect()
    return Robot(spec, tp, NativeRuntime())


class TestSkillRegistry(unittest.TestCase):
    def test_builtin_skills_exist(self):
        names = [s.name for s in list_skills()]
        self.assertIn("patrol", names)
        self.assertIn("wave", names)
        self.assertIn("stop", names)
        self.assertIn("status", names)

    def test_get_skill(self):
        s = get_skill("patrol")
        self.assertEqual(s.name, "patrol")
        self.assertIn("base.drive", s.requires)

    def test_get_unknown_skill_raises(self):
        with self.assertRaises(KeyError):
            get_skill("nonexistent")

    def test_register_custom_skill(self):
        @skill("custom-test", "A test skill", requires=["base.drive"])
        def _custom(robot, x=1):
            return f"custom {x}"

        s = get_skill("custom-test")
        self.assertEqual(s.description, "A test skill")
        result = run_skill("custom-test", _bot("sim"), x=42)
        self.assertEqual(result, "custom 42")


class TestSkillExecution(unittest.TestCase):
    def test_patrol_runs_on_sim(self):
        bot = _bot("sim")
        result = run_skill("patrol", bot, side=0.1, speed=0.05)
        self.assertEqual(result, "patrol complete")

    def test_wave_runs_on_omnibot(self):
        bot = _bot("omnibot")
        result = run_skill("wave", bot, reps=1)
        self.assertEqual(result, "waved 1 times")

    def test_wave_fails_on_go2(self):
        bot = _bot("unitree-go2")
        with self.assertRaises(SkillRequirementsNotMet):
            run_skill("wave", bot)

    def test_stop_runs_on_any_robot(self):
        bot = _bot("sim")
        result = run_skill("stop", bot)
        self.assertEqual(result, "emergency stop engaged")

    def test_status_runs_on_any_robot(self):
        bot = _bot("sim")
        result = run_skill("status", bot)
        self.assertIn("status=", result)
        self.assertIn("protocol=", result)

    def test_can_run_checks_capabilities(self):
        s = get_skill("wave")
        self.assertTrue(s.can_run(_bot("omnibot")))
        self.assertFalse(s.can_run(_bot("unitree-go2")))


class TestSkillDataclass(unittest.TestCase):
    def test_skill_fields(self):
        s = Skill(
            name="test",
            description="test skill",
            handler=lambda r: "ok",
            requires=["base.drive"],
            author="tester",
            version="1.2.3",
            tags=["a", "b"],
        )
        self.assertEqual(s.name, "test")
        self.assertEqual(s.version, "1.2.3")
        self.assertEqual(s.author, "tester")
        self.assertEqual(s.tags, ["a", "b"])

    def test_register_and_list(self):
        register_skill(Skill(name="zzz-test", description="z", handler=lambda r: "z"))
        skills = list_skills()
        names = [s.name for s in skills]
        self.assertIn("zzz-test", names)


if __name__ == "__main__":
    unittest.main()
