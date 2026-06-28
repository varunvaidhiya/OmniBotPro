"""Hardware-in-the-loop tests — skipped unless ``OHHO_HIL=1``.

These tests connect to **real robots** and verify the full adapter chain:
connect → drive → read telemetry → disconnect. They are skipped in CI and only
run on the bench when the operator sets ``OHHO_HIL=1``.

Usage::

    OHHO_HIL=1 OHHO_OMNIBOT_PORT=/dev/ttyUSB0 OHHO_OMNIBOT_ARM=/dev/ttyACM0 \\
        python -m unittest discover -s sdk/tests/hil -v
    OHHO_HIL=1 OHHO_GO2_IFACE=eth0 \\
        python -m unittest discover -s sdk/tests/hil -v
"""

from __future__ import annotations

import os
import time
import unittest

HIL = os.environ.get("OHHO_HIL", "") == "1"


@unittest.skipUnless(HIL, "set OHHO_HIL=1 to run hardware tests")
class TestHilOmniBotBase(unittest.TestCase):
    """Drive the real OmniBot base over Yahboom serial."""

    def test_drive_and_read(self):
        from ohho.adapters.yahboom import YahboomTransport
        from ohho.registry import get_spec
        from ohho.schema import Velocity

        port = os.environ.get("OHHO_OMNIBOT_PORT", "/dev/ttyUSB0")
        spec = get_spec("omnibot")
        tp = YahboomTransport(spec, port)
        tp.connect()
        try:
            tp.send_velocity(Velocity(0.05, 0.0, 0.0))
            time.sleep(1.0)
            tp.send_velocity(Velocity())
            time.sleep(0.2)
            t = tp.read()
            self.assertIsNotNone(t.odom)
        finally:
            tp.disconnect()


@unittest.skipUnless(HIL, "set OHHO_HIL=1 to run hardware tests")
class TestHilOmniBotArm(unittest.TestCase):
    """Move the real SO-101 arm over Feetech serial."""

    def test_move_joints_and_read(self):
        from ohho.adapters.feetech import FeetechTransport
        from ohho.registry import get_spec

        port = os.environ.get("OHHO_OMNIBOT_ARM", "/dev/ttyACM0")
        spec = get_spec("omnibot")
        tp = FeetechTransport(spec, port)
        tp.connect()
        try:
            tp.send_joint_command("arm_gripper", 0.2)
            time.sleep(1.0)
            tp.send_joint_command("arm_gripper", 0.0)
            time.sleep(1.0)
            t = tp.read()
            self.assertEqual(len(t.joints), 6)
        finally:
            tp.disconnect()


@unittest.skipUnless(HIL, "set OHHO_HIL=1 to run hardware tests")
class TestHilOmniBotComposite(unittest.TestCase):
    """Drive the full OmniBot (base + arm) through the composite transport."""

    def test_composite_drive_and_arm(self):
        from ohho.adapters.composite import CompositeTransport
        from ohho.adapters.feetech import FeetechTransport
        from ohho.adapters.yahboom import YahboomTransport
        from ohho.registry import get_spec
        from ohho.schema import Velocity

        base_port = os.environ.get("OHHO_OMNIBOT_PORT", "/dev/ttyUSB0")
        arm_port = os.environ.get("OHHO_OMNIBOT_ARM", "/dev/ttyACM0")
        spec = get_spec("omnibot")
        base = YahboomTransport(spec, base_port)
        arm = FeetechTransport(spec, arm_port)
        ct = CompositeTransport(base, arm)
        ct.connect()
        try:
            ct.send_velocity(Velocity(0.05, 0.0, 0.0))
            ct.send_joint_command("arm_gripper", 0.2)
            time.sleep(1.0)
            ct.send_velocity(Velocity())
            ct.send_joint_command("arm_gripper", 0.0)
            time.sleep(0.5)
            t = ct.read()
            self.assertIsNotNone(t.odom)
            self.assertEqual(len(t.joints), 6)
        finally:
            ct.disconnect()


@unittest.skipUnless(HIL, "set OHHO_HIL=1 to run hardware tests")
class TestHilUnitreeGo2(unittest.TestCase):
    """Drive the real Unitree Go2 over DDS."""

    def test_drive_and_read(self):
        from ohho.adapters.unitree import UnitreeDdsTransport
        from ohho.registry import get_spec
        from ohho.schema import Velocity

        iface = os.environ.get("OHHO_GO2_IFACE", "eth0")
        spec = get_spec("unitree-go2")
        tp = UnitreeDdsTransport(spec, iface)
        tp.connect()
        try:
            time.sleep(1.0)
            t = tp.read()
            self.assertIsNotNone(t.odom)
            tp.send_velocity(Velocity(0.0, 0.0, 0.0))
        finally:
            tp.disconnect()


if __name__ == "__main__":
    unittest.main()
