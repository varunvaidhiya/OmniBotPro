"""Tests for the competitive advantage advisor (no API calls)."""

import unittest

from competitive_advantage.advisor import _parse_advantage, _parse_positioning


SAMPLE_ADVANTAGE = """\
Your Core Advantage: Deep robotics domain knowledge combined with hands-on hardware integration that only comes from building real systems.
Why Competitors Can't Match It:
1. Years of embedded real-time debugging experience cannot be replicated by a software-only team in 6 months.
2. Existing relationships with niche hardware vendors (Yahboom, Feetech) give exclusive early access to unreleased components.
Where You're Unbeatable: Early-stage robotics startups that need a trusted technical co-founder proxy for their first hardware sprint.
The Gap You're Exploiting: Consultants pitch slides; you ship prototypes — that gap is invisible to clients until they've burned cash on the alternative.
"""

SAMPLE_POSITIONING = """\
Current Battlefield: Competing as a general robotics consultant against large consultancies on RFPs requiring teams, certifications, and account management overhead.
Winning Battlefield: Direct engagement with seed-stage founders who need a single expert to own the hardware stack end-to-end and move fast.
The Shift Required: Stop responding to enterprise RFPs and start publishing build logs and teardowns that attract founders who already trust your technical judgment.
Terrain You're Missing: The founder-to-first-prototype niche — no large consultancy can move fast enough or cheap enough to compete there.
"""


class TestParseAdvantage(unittest.TestCase):
    def test_fields_extracted(self):
        result = _parse_advantage(SAMPLE_ADVANTAGE)
        self.assertIn("robotics", result.core_advantage.lower())
        self.assertGreaterEqual(len(result.why_competitors_cant_match), 1)
        self.assertIn("startup", result.where_unbeatable.lower())
        self.assertIn("gap", result.gap_exploiting.lower())

    def test_raw_preserved(self):
        result = _parse_advantage(SAMPLE_ADVANTAGE)
        self.assertEqual(result.raw_response, SAMPLE_ADVANTAGE)


class TestParsePositioning(unittest.TestCase):
    def test_fields_extracted(self):
        result = _parse_positioning(SAMPLE_POSITIONING)
        self.assertIn("consultant", result.current_battlefield.lower())
        self.assertIn("founder", result.winning_battlefield.lower())
        self.assertIn("RFP", result.shift_required)
        self.assertIn("prototype", result.terrain_missing.lower())

    def test_raw_preserved(self):
        result = _parse_positioning(SAMPLE_POSITIONING)
        self.assertEqual(result.raw_response, SAMPLE_POSITIONING)


if __name__ == "__main__":
    unittest.main()
