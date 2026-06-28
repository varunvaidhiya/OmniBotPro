import unittest

from ohho.adapters import AdapterUnavailable, available_adapters, resolve_transport
from ohho.registry import get_spec


class TestAdapters(unittest.TestCase):
    def test_sim_scheme(self):
        tp = resolve_transport("sim://", get_spec("sim"))
        self.assertEqual(tp.protocol, "simulated")

    def test_serial_scheme_resolves_to_yahboom(self):
        tp = resolve_transport("serial:///dev/ttyUSB0", get_spec("omnibot"))
        self.assertEqual(tp.protocol, "serial")
        self.assertEqual(tp.port, "/dev/ttyUSB0")

    def test_dds_scheme_resolves_to_unitree(self):
        tp = resolve_transport("dds://eth0", get_spec("unitree-go2"))
        self.assertEqual(tp.protocol, "dds")
        self.assertEqual(tp.address, "eth0")

    def test_explicit_unknown_raises(self):
        with self.assertRaises(AdapterUnavailable):
            resolve_transport("wat://nope", get_spec("sim"))

    def test_auto_runs_in_simulation(self):
        # No explicit transport -> simulation, even for a hardware robot.
        tp = resolve_transport(None, get_spec("unitree-go2"))
        self.assertEqual(tp.protocol, "simulated")

    def test_available(self):
        self.assertIn("sim", available_adapters())


if __name__ == "__main__":
    unittest.main()
