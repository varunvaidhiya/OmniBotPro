import json
import os
import tempfile
import unittest

from ohho.registry import (
    UnknownRobot,
    get_spec,
    list_specs,
    load_manifest,
    spec_from_dict,
)


class TestRegistry(unittest.TestCase):
    def test_builtins_present(self):
        ids = {s.id for s in list_specs()}
        self.assertTrue({"omnibot", "unitree-go2", "sim"}.issubset(ids))

    def test_get_spec(self):
        self.assertEqual(get_spec("omnibot").dof_arm, 6)
        self.assertEqual(len(get_spec("omnibot").joint_names), 6)

    def test_unknown_raises(self):
        with self.assertRaises(UnknownRobot):
            get_spec("does-not-exist")

    def test_spec_from_dict_nested(self):
        spec = spec_from_dict(
            {
                "id": "x",
                "name": "X",
                "category": "wheeled",
                "capabilities": ["base.drive"],
                "dof": {"base": 3, "arm": 2},
                "limits": {"max_lin": 0.7, "max_ang": 1.3},
            }
        )
        self.assertEqual(spec.dof_arm, 2)
        self.assertAlmostEqual(spec.max_lin, 0.7)
        self.assertAlmostEqual(spec.max_ang, 1.3)

    def test_load_manifest_registers(self):
        data = {
            "id": "tmp-bot",
            "name": "Tmp Bot",
            "category": "wheeled",
            "capabilities": ["base.drive"],
            "adapter": "sim",
            "dof": {"base": 3, "arm": 0},
            "limits": {"max_lin": 0.5, "max_ang": 1.0},
        }
        fd, path = tempfile.mkstemp(suffix=".json")
        try:
            with os.fdopen(fd, "w") as f:
                json.dump(data, f)
            spec = load_manifest(path)
            self.assertEqual(spec.id, "tmp-bot")
            self.assertEqual(get_spec("tmp-bot").id, "tmp-bot")
        finally:
            os.unlink(path)


if __name__ == "__main__":
    unittest.main()
