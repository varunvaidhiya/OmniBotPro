"""DeepX NPU support in the hardware abstraction (device + profiles)."""

from __future__ import annotations

import unittest

from learning_engine.hardware import (
    BUILTIN_PROFILES,
    AcceleratorType,
    get_profile,
    onnx_providers,
)
from learning_engine.hardware.device import _ONNX_PROVIDER_PREFERENCE


class TestDeepX(unittest.TestCase):
    def test_enum_value(self):
        self.assertEqual(AcceleratorType.DEEPX.value, "deepx")

    def test_ep_preference_cpu_fallback_last(self):
        pref = _ONNX_PROVIDER_PREFERENCE[AcceleratorType.DEEPX]
        self.assertEqual(pref[-1], "CPUExecutionProvider")
        self.assertEqual(pref[0], "DeepXExecutionProvider")

    def test_providers_filter_to_installed_with_cpu(self):
        # The DeepX EP is not in any stock ORT build, so onnx_providers must
        # still yield a valid (CPU-inclusive) list of real providers.
        prov = onnx_providers(AcceleratorType.DEEPX)
        self.assertIn("CPUExecutionProvider", prov)
        self.assertTrue(all(p.endswith("ExecutionProvider") for p in prov))

    def test_profile_registered_with_useful_roles(self):
        self.assertIn("pi_deepx_workstation", BUILTIN_PROFILES)
        profile = get_profile("pi_deepx_workstation")
        self.assertEqual(profile.node_for("inference").name, "pi5")
        self.assertEqual(profile.node_for("control").name, "pi5")
        self.assertEqual(profile.node_for("training").name, "workstation")


if __name__ == "__main__":
    unittest.main()
