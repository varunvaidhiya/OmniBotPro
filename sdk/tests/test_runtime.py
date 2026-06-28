import time
import unittest

from ohho.runtime import NativeRuntime, available_runtimes, get_runtime
from ohho.runtime.base import RuntimeUnavailable


class TestNativeRuntime(unittest.TestCase):
    def test_timer_fires(self):
        rt = NativeRuntime(tick_s=0.002)
        hits = []
        rt.create_timer(0.02, lambda: hits.append(1))
        rt.start()
        try:
            time.sleep(0.15)
        finally:
            rt.stop()
        self.assertGreaterEqual(len(hits), 3)

    def test_timer_cancel(self):
        rt = NativeRuntime(tick_s=0.002)
        hits = []
        h = rt.create_timer(0.01, lambda: hits.append(1))
        h.cancel()
        rt.start()
        try:
            time.sleep(0.05)
        finally:
            rt.stop()
        self.assertEqual(len(hits), 0)

    def test_pubsub(self):
        rt = NativeRuntime()
        got = []
        off = rt.subscribe("topic", got.append)
        rt.publish("topic", 5)
        self.assertEqual(got, [5])
        off()
        rt.publish("topic", 6)
        self.assertEqual(got, [5])

    def test_params(self):
        rt = NativeRuntime()
        rt.set_param("k", 42)
        self.assertEqual(rt.get_param("k"), 42)
        self.assertIsNone(rt.get_param("missing"))


class TestRuntimeSelection(unittest.TestCase):
    def test_get_native(self):
        self.assertIsInstance(get_runtime("native"), NativeRuntime)
        self.assertIsInstance(get_runtime("auto"), NativeRuntime)

    def test_available(self):
        self.assertIn("native", available_runtimes())

    def test_ros2_unavailable_in_m0(self):
        with self.assertRaises(RuntimeUnavailable):
            get_runtime("ros2")

    def test_bad_name(self):
        with self.assertRaises(ValueError):
            get_runtime("nope")


if __name__ == "__main__":
    unittest.main()
