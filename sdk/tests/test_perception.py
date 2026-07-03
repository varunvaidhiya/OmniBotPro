import math
import unittest

from ohho.adapters.sim import SimObject, SimTransport
from ohho.memory import SpatialMemory
from ohho.perception import (
    Detection,
    SimPerceptor,
    VlmPerceptor,
    parse_vlm_detections,
    remember_detections,
)
from ohho.registry import get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime


def _bot(objects):
    spec = get_spec("sim")
    tp = SimTransport(spec, objects=objects)
    tp.connect()
    return Robot(spec, tp, NativeRuntime())


class TestSimPerceptor(unittest.TestCase):
    def test_detects_object_in_front(self):
        bot = _bot([SimObject(2.0, 0.0, 0.3, "chair")])
        det = SimPerceptor(bot).look()
        self.assertEqual(len(det), 1)
        d = det[0]
        self.assertEqual(d.label, "chair")
        self.assertAlmostEqual(d.bearing, 0.0, places=3)
        self.assertAlmostEqual(d.range, 2.0, places=3)
        self.assertAlmostEqual(d.x, 2.0)

    def test_ignores_behind_and_far(self):
        bot = _bot(
            [
                SimObject(-2.0, 0.0, 0.3, "behind"),  # outside 120° FOV
                SimObject(5.0, 0.0, 0.3, "far"),  # beyond max_range
                SimObject(1.0, 0.2, 0.2, "near"),
            ]
        )
        labels = [d.label for d in SimPerceptor(bot).look()]
        self.assertEqual(labels, ["near"])

    def test_sorted_by_range(self):
        bot = _bot([SimObject(3.0, 0.0, 0.3, "far"), SimObject(1.0, 0.0, 0.3, "close")])
        labels = [d.label for d in SimPerceptor(bot).look()]
        self.assertEqual(labels, ["close", "far"])

    def test_wide_fov_sees_behind(self):
        bot = _bot([SimObject(-1.5, 0.0, 0.3, "behind")])
        det = SimPerceptor(bot, fov=2 * math.pi).look()
        self.assertEqual([d.label for d in det], ["behind"])


class TestRememberDetections(unittest.TestCase):
    def test_detections_land_in_memory(self):
        memory = SpatialMemory()
        n = remember_detections(
            memory,
            [
                Detection("cup", 0.9, x=1.0, y=2.0),
                Detection("noise", 0.5),  # no position → skipped
            ],
        )
        self.assertEqual(n, 1)
        self.assertIsNotNone(memory.where_is("cup"))
        self.assertIsNone(memory.where_is("noise"))

    def test_perceive_remember_recall_loop(self):
        bot = _bot([SimObject(1.5, 0.5, 0.3, "plant")])
        memory = SpatialMemory()
        remember_detections(memory, SimPerceptor(bot).look())
        e = memory.where_is("plant")
        self.assertIsNotNone(e)
        self.assertAlmostEqual(e.x, 1.5, places=3)


class TestVlmParsing(unittest.TestCase):
    def test_parses_clean_json(self):
        out = parse_vlm_detections('[{"label": "cup", "confidence": 0.9}]')
        self.assertEqual(len(out), 1)
        self.assertEqual(out[0].label, "cup")
        self.assertAlmostEqual(out[0].confidence, 0.9)

    def test_parses_json_wrapped_in_prose(self):
        text = 'Here you go:\n[{"label": "chair", "confidence": 0.7}]\nDone.'
        out = parse_vlm_detections(text)
        self.assertEqual([d.label for d in out], ["chair"])

    def test_garbage_returns_empty(self):
        self.assertEqual(parse_vlm_detections("no json here"), [])
        self.assertEqual(parse_vlm_detections("[not json]"), [])

    def test_confidence_clamped_and_defaulted(self):
        out = parse_vlm_detections(
            '[{"label": "a", "confidence": 7}, {"label": "b"}, {"nope": 1}]'
        )
        self.assertEqual(len(out), 2)
        self.assertEqual(out[0].confidence, 1.0)
        self.assertEqual(out[1].confidence, 0.5)


class _FakeBlock:
    def __init__(self, text):
        self.text = text


class _FakeMessage:
    def __init__(self, text):
        self.content = [_FakeBlock(text)]


class _FakeMessages:
    def __init__(self, text):
        self._text = text
        self.last_kwargs = None

    def create(self, **kwargs):
        self.last_kwargs = kwargs
        return _FakeMessage(self._text)


class _FakeClient:
    def __init__(self, text):
        self.messages = _FakeMessages(text)


class TestVlmPerceptor(unittest.TestCase):
    def test_look_with_injected_client(self):
        client = _FakeClient('[{"label": "person", "confidence": 0.8}]')
        vlm = VlmPerceptor(client=client)
        det = vlm.look(b"\xff\xd8fakejpeg")
        self.assertEqual([d.label for d in det], ["person"])
        sent = client.messages.last_kwargs
        self.assertEqual(sent["model"], vlm.model)
        self.assertEqual(sent["messages"][0]["content"][0]["type"], "image")


if __name__ == "__main__":
    unittest.main()
