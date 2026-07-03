import unittest

from ohho.adapters.sim import SimObject, SimTransport, demo_world
from ohho.registry import get_spec


class TestSimWorld(unittest.TestCase):
    def _tp(self, objects=None):
        tp = SimTransport(get_spec("sim"), objects=objects)
        tp.connect()
        return tp

    def test_no_objects_means_no_scan(self):
        tp = self._tp()
        self.assertIsNone(tp.read().scan)

    def test_scan_present_with_objects(self):
        tp = self._tp([SimObject(2.0, 0.0, 0.5, "wall")])
        scan = tp.read().scan
        self.assertIsNotNone(scan)
        self.assertEqual(len(scan.ranges), 72)

    def test_ray_hits_obstacle_at_expected_range(self):
        tp = self._tp([SimObject(2.0, 0.0, 0.5, "wall")])
        scan = tp.read().scan
        # ray 36 points straight ahead (angle_min=-pi, inc=2pi/72 → rel angle 0)
        self.assertAlmostEqual(scan.ranges[36], 1.5, places=3)
        # ray 0 points straight back — nothing there → range_max
        self.assertAlmostEqual(scan.ranges[0], scan.range_max, places=3)

    def test_add_object_and_world_objects(self):
        tp = self._tp()
        tp.add_object(1.0, 1.0, 0.2, "cup")
        objs = tp.world_objects()
        self.assertEqual(len(objs), 1)
        self.assertEqual(objs[0].label, "cup")
        self.assertIsNotNone(tp.read().scan)

    def test_demo_world_is_labeled(self):
        labels = {ob.label for ob in demo_world()}
        self.assertIn("chair", labels)
        self.assertIn("cup", labels)


if __name__ == "__main__":
    unittest.main()
