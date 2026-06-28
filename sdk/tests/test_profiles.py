import os
import unittest

from ohho.profiles import (
    NodeSpec,
    detect_profile,
    describe,
    get_profile,
    list_profiles,
)


class TestProfiles(unittest.TestCase):
    def test_builtin_profiles_exist(self):
        names = list_profiles()
        self.assertIn("pi_workstation", names)
        self.assertIn("workstation_single", names)
        self.assertIn("jetson_single", names)
        self.assertIn("mac_dev", names)
        self.assertIn("edge_cpu", names)

    def test_get_profile(self):
        p = get_profile("workstation_single")
        self.assertEqual(p.name, "workstation_single")
        self.assertGreater(len(p.nodes), 0)

    def test_get_unknown_raises(self):
        with self.assertRaises(KeyError):
            get_profile("nonexistent")

    def test_node_for_role(self):
        p = get_profile("pi_workstation")
        node = p.node_for("control")
        self.assertIsNotNone(node)
        self.assertEqual(node.name, "pi5")
        self.assertIn("control", node.roles)

    def test_node_for_missing_role(self):
        p = get_profile("mac_dev")
        node = p.node_for("control")
        self.assertIsNone(node)

    def test_device_for_role(self):
        p = get_profile("workstation_single")
        dev = p.device_for("inference")
        # On this machine (no torch) it resolves to cpu
        self.assertIn(dev, ("cpu", "cuda", "mps"))

    def test_detect_profile_with_env_override(self):
        old = os.environ.get("OHHO_HW_PROFILE")
        try:
            os.environ["OHHO_HW_PROFILE"] = "jetson_single"
            p = detect_profile()
            self.assertEqual(p.name, "jetson_single")
        finally:
            if old is None:
                os.environ.pop("OHHO_HW_PROFILE", None)
            else:
                os.environ["OHHO_HW_PROFILE"] = old

    def test_detect_profile_falls_back(self):
        old = os.environ.pop("OHHO_HW_PROFILE", None)
        old2 = os.environ.pop("OMNIBOT_HW_PROFILE", None)
        try:
            p = detect_profile()
            self.assertIn(p.name, list_profiles())
        finally:
            if old is not None:
                os.environ["OHHO_HW_PROFILE"] = old
            if old2 is not None:
                os.environ["OMNIBOT_HW_PROFILE"] = old2

    def test_describe_returns_dict(self):
        d = describe()
        self.assertIn("platform", d)
        self.assertIn("profile", d)
        self.assertIn("device", d)
        self.assertIn("nodes", d)

    def test_node_spec_defaults(self):
        n = NodeSpec("test", ["control"])
        self.assertEqual(n.device, "auto")
        self.assertEqual(n.accelerator, "")
        self.assertEqual(n.notes, "")


if __name__ == "__main__":
    unittest.main()
