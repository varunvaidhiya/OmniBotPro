import unittest

from ohho.adapters import AdapterUnavailable, available_adapters, resolve_transport
from ohho.registry import get_spec


class TestAdapters(unittest.TestCase):
    def test_sim_scheme(self):
        tp = resolve_transport("sim://", get_spec("sim"))
        self.assertEqual(tp.protocol, "simulated")

    def test_explicit_unknown_raises(self):
        with self.assertRaises(AdapterUnavailable):
            resolve_transport("dds://192.168.1.10", get_spec("unitree-go2"))

    def test_auto_falls_back_to_sim(self):
        # unitree-go2's adapter ("unitree-dds") isn't bundled -> simulation
        tp = resolve_transport(None, get_spec("unitree-go2"))
        self.assertEqual(tp.protocol, "simulated")

    def test_available(self):
        self.assertIn("sim", available_adapters())


if __name__ == "__main__":
    unittest.main()
