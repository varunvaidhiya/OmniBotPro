"""
Unit tests for MissionPlanner._parse_command — pure logic, no ROS required.

The parser is the most testable piece of mission_planner.py and carries
real correctness risk (wrong parse → wrong nav target or silent VLA skip).
We extract the method via a lightweight monkey-patch so we can test it
without spinning a full ROS node or loading Nav2 action clients.
"""

import pytest

# Import just the parsing logic without triggering ROS initialization.
# We instantiate a minimal stub with only the method we need.
from omnibot_hybrid.mission_planner import MissionPlanner


class _ParserStub:
    """Minimal stub that exposes only _parse_command."""

    _parse_command = MissionPlanner._parse_command  # unbound method


_p = _ParserStub()


def parse(raw: str):
    return _p._parse_command(raw)


# ── Happy paths ──────────────────────────────────────────────────────────────


class TestFullHybridCommand:
    def test_basic_hybrid(self):
        result = parse("navigate:kitchen,vla:find the red cup")
        assert result == {"navigate": "kitchen", "vla": "find the red cup"}

    def test_nav_before_vla(self):
        result = parse("navigate:bedroom,vla:wave")
        assert result["navigate"] == "bedroom"
        assert result["vla"] == "wave"

    def test_spaces_around_comma(self):
        result = parse("navigate: hallway , vla: pick up bottle")
        assert result["navigate"] == "hallway"
        assert result["vla"] == "pick up bottle"


class TestNavigateOnlyCommand:
    def test_navigate_only(self):
        result = parse("navigate:kitchen")
        assert result == {"navigate": "kitchen"}

    def test_nav_alias(self):
        result = parse("nav:office")
        assert result == {"navigate": "office"}

    def test_nav2_alias(self):
        result = parse("nav2:lab")
        assert result == {"navigate": "lab"}

    def test_go_alias(self):
        result = parse("go:hallway")
        assert result == {"navigate": "hallway"}


class TestVlaOnlyCommand:
    def test_vla_only(self):
        result = parse("vla:follow the person")
        assert result == {"vla": "follow the person"}

    def test_vla_with_spaces_in_task(self):
        result = parse("vla:pick up the blue cube and place it on the shelf")
        assert result["vla"] == "pick up the blue cube and place it on the shelf"


# ── Edge cases ───────────────────────────────────────────────────────────────


class TestParseEdgeCases:
    def test_empty_string_returns_none(self):
        assert parse("") is None

    def test_no_colon_returns_none(self):
        assert parse("just some text") is None

    def test_unknown_key_ignored(self):
        # Only known keys are extracted; unknown keys are silently dropped
        result = parse("vla:do something,foo:bar")
        assert result == {"vla": "do something"}
        assert "foo" not in (result or {})

    def test_nav_and_extra_unknown_key(self):
        result = parse("navigate:kitchen,unknown:blah,vla:task")
        assert result == {"navigate": "kitchen", "vla": "task"}

    def test_duplicate_navigate_last_wins(self):
        result = parse("navigate:room1,navigate:room2")
        assert result["navigate"] == "room2"

    def test_leading_trailing_whitespace(self):
        result = parse("  navigate:lounge  ")
        assert result == {"navigate": "lounge"}

    def test_multiword_location(self):
        result = parse("navigate:living room")
        assert result == {"navigate": "living room"}


# ── Location resolver ─────────────────────────────────────────────────────────


class TestResolveLocation:
    """Test _resolve_location with a mocked locations dict."""

    def _make_node(self, locations: dict):
        class Stub:
            _locations = locations

            def get_clock(self):
                import rclpy.clock

                return rclpy.clock.Clock()

            _resolve_location = MissionPlanner._resolve_location

        return Stub()

    def test_returns_none_for_unknown_location(self):
        stub = self._make_node({"kitchen": {"x": 1.0, "y": 2.0, "yaw": 0.0}})
        result = stub._resolve_location("living_room")
        assert result is None

    def test_returns_pose_for_known_location(self):
        stub = self._make_node({"kitchen": {"x": 3.5, "y": -1.2, "yaw": 1.57}})
        pose = stub._resolve_location("kitchen")
        assert pose is not None
        assert pose.pose.position.x == pytest.approx(3.5)
        assert pose.pose.position.y == pytest.approx(-1.2)
        assert pose.header.frame_id == "map"
