import unittest

from ohho import capabilities as caps


class TestCapabilities(unittest.TestCase):
    def test_membership(self):
        self.assertIn(caps.MANIPULATION, caps.ALL)
        self.assertIn(caps.BASE_HOLONOMIC, caps.ALL)

    def test_is_known(self):
        self.assertTrue(caps.is_known(caps.BASE_DRIVE))
        self.assertFalse(caps.is_known("not.a.capability"))


if __name__ == "__main__":
    unittest.main()
