import os
import tempfile
import unittest

from ohho.memory import SpatialMemory, default_memory_path


class TestSpatialMemory(unittest.TestCase):
    def test_observe_creates_entity(self):
        m = SpatialMemory()
        e = m.observe("cup", 1.0, 2.0, t=100.0)
        self.assertEqual(e.label, "cup")
        self.assertEqual(e.count, 1)
        self.assertEqual(len(m.entities()), 1)

    def test_object_permanence_associates_nearby(self):
        m = SpatialMemory(associate_radius=0.75)
        e1 = m.observe("cup", 1.0, 2.0, t=100.0)
        e2 = m.observe("cup", 1.2, 2.1, t=105.0)
        self.assertEqual(e1.id, e2.id)
        self.assertEqual(e2.count, 2)
        # EMA pulls the position toward the new sighting
        self.assertGreater(e2.x, 1.0)
        self.assertLess(e2.x, 1.2)

    def test_far_sighting_becomes_new_entity(self):
        m = SpatialMemory(associate_radius=0.75)
        m.observe("cup", 0.0, 0.0, t=100.0)
        m.observe("cup", 3.0, 3.0, t=101.0)
        self.assertEqual(len(m.entities("cup")), 2)

    def test_different_labels_never_associate(self):
        m = SpatialMemory()
        m.observe("cup", 0.0, 0.0)
        m.observe("plate", 0.05, 0.0)
        self.assertEqual(len(m.entities()), 2)

    def test_where_is_returns_most_recent(self):
        m = SpatialMemory()
        m.observe("cup", 0.0, 0.0, t=100.0)
        m.observe("cup", 5.0, 5.0, t=200.0)  # far → separate entity, newer
        e = m.where_is("cup")
        self.assertAlmostEqual(e.x, 5.0)
        self.assertIsNone(m.where_is("unicorn"))

    def test_near_query_sorted_by_distance(self):
        m = SpatialMemory()
        m.observe("chair", 1.0, 0.0)
        m.observe("table", 0.2, 0.0)
        found = m.near(0.0, 0.0, radius=1.5)
        self.assertEqual([e.label for e in found], ["table", "chair"])
        self.assertEqual(m.near(10.0, 10.0, radius=0.5), [])

    def test_timeline_filters(self):
        m = SpatialMemory()
        m.observe("cup", 0.0, 0.0, t=100.0)
        m.note("picked up the cup", t=150.0)
        m.observe("chair", 1.0, 1.0, t=200.0)
        self.assertEqual(len(m.timeline()), 3)
        self.assertEqual(len(m.timeline(label="cup")), 1)
        self.assertEqual(len(m.timeline(since=140.0)), 2)

    def test_describe_mentions_entities(self):
        m = SpatialMemory()
        self.assertIn("empty", m.describe())
        m.observe("cup", 1.0, 2.0)
        self.assertIn("cup", m.describe())

    def test_save_load_round_trip(self):
        with tempfile.TemporaryDirectory() as tmp:
            path = os.path.join(tmp, "mem.json")
            m = SpatialMemory(path=path)
            m.observe("cup", 1.0, 2.0, t=100.0)
            m.note("hello")
            m.save()
            m2 = SpatialMemory(path=path)  # loads on construction
            e = m2.where_is("cup")
            self.assertIsNotNone(e)
            self.assertAlmostEqual(e.x, 1.0)
            self.assertEqual(len(m2.timeline()), 2)

    def test_clear(self):
        m = SpatialMemory()
        m.observe("cup", 0.0, 0.0)
        m.clear()
        self.assertEqual(m.entities(), [])

    def test_event_log_bounded(self):
        m = SpatialMemory(max_events=10)
        for i in range(30):
            m.note(f"event {i}")
        self.assertEqual(len(m.timeline()), 10)

    def test_default_memory_path_per_robot(self):
        a = default_memory_path("omnibot")
        b = default_memory_path("unitree-go2")
        self.assertNotEqual(a, b)
        self.assertTrue(a.endswith("omnibot.json"))


if __name__ == "__main__":
    unittest.main()
