import math
import unittest

from ohho.adapters.sim import SimTransport
from ohho.nav import (
    FREE,
    OCCUPIED,
    UNKNOWN,
    Mapper,
    Navigator,
    OccupancyGrid,
    astar,
    find_frontiers,
)
from ohho.registry import RobotSpec, get_spec
from ohho.robot import Robot
from ohho.runtime import NativeRuntime
from ohho.schema import Odometry, Scan


def _bot(robot_id="sim", objects=None):
    spec = get_spec(robot_id)
    tp = SimTransport(spec, objects=objects)
    tp.connect()
    return Robot(spec, tp, NativeRuntime()), tp


def _grid(size=8.0, res=0.08):
    return OccupancyGrid(size_m=size, resolution=res)


class TestOccupancyGrid(unittest.TestCase):
    def test_world_grid_round_trip(self):
        g = _grid()
        cell = g.world_to_grid(1.0, -0.5)
        self.assertIsNotNone(cell)
        wx, wy = g.grid_to_world(*cell)
        self.assertLess(abs(wx - 1.0), g.resolution)
        self.assertLess(abs(wy + 0.5), g.resolution)

    def test_out_of_bounds_is_none(self):
        g = _grid(size=4.0)
        self.assertIsNone(g.world_to_grid(10.0, 0.0))

    def test_trace_marks_free_and_hit(self):
        g = _grid()
        g.trace(0.0, 0.0, 1.0, 0.0, hit=True)
        end = g.world_to_grid(1.0, 0.0)
        mid = g.world_to_grid(0.5, 0.0)
        self.assertEqual(g.get(*end), OCCUPIED)
        self.assertEqual(g.get(*mid), FREE)

    def test_inflate_blocks_neighbors(self):
        g = _grid()
        cell = g.world_to_grid(0.0, 0.0)
        g.set(*cell, OCCUPIED)
        fat = g.inflate(0.2)
        near = fat.world_to_grid(0.16, 0.0)
        self.assertEqual(fat.get(*near), OCCUPIED)
        far = fat.world_to_grid(1.0, 0.0)
        self.assertNotEqual(fat.get(*far), OCCUPIED)


class TestAstar(unittest.TestCase):
    def test_straight_path_on_open_grid(self):
        g = _grid()
        start = g.world_to_grid(0.0, 0.0)
        goal = g.world_to_grid(1.5, 0.0)
        path = astar(g, start, goal)
        self.assertIsNotNone(path)
        self.assertEqual(path[0], start)
        self.assertEqual(path[-1], goal)

    def test_detours_around_wall(self):
        g = _grid()
        # vertical wall at x=0.8 with no gap in the corridor band
        for wy in range(g.height):
            wall = g.world_to_grid(0.8, 0.0)[0]
            g.set(wall, wy, OCCUPIED)
        gap_y = g.world_to_grid(0.8, 2.5)[1]
        g.set(g.world_to_grid(0.8, 2.5)[0], gap_y, FREE)  # one-cell gap
        path = astar(g, g.world_to_grid(0.0, 0.0), g.world_to_grid(1.6, 0.0))
        self.assertIsNotNone(path)
        # the path must pass through the gap's row neighborhood
        ys = {cell[1] for cell in path}
        self.assertIn(gap_y, ys)

    def test_no_path_when_sealed(self):
        g = _grid(size=3.0)
        for wy in range(g.height):
            g.set(g.width // 2, wy, OCCUPIED)
        path = astar(g, g.world_to_grid(-1.0, 0.0), g.world_to_grid(1.0, 0.0))
        self.assertIsNone(path)


class TestMapperAndFrontiers(unittest.TestCase):
    def test_mapper_builds_map_from_scan(self):
        g = _grid()
        scan = Scan(
            angle_min=-math.pi,
            angle_increment=2 * math.pi / 8,
            ranges=[2.0] * 8,
            range_max=4.0,
        )
        Mapper(g).update(Odometry(), scan)
        counts = g.counts()
        self.assertGreater(counts["free"], 0)
        self.assertGreater(counts["occupied"], 0)  # rays hit at 2.0 < 4.0

    def test_frontiers_exist_on_partial_map(self):
        g = _grid()
        scan = Scan(
            angle_min=-math.pi,
            angle_increment=2 * math.pi / 16,
            ranges=[3.9] * 16,
            range_max=4.0,
        )
        Mapper(g).update(Odometry(), scan)
        clusters = find_frontiers(g)
        self.assertGreater(len(clusters), 0)

    def test_no_frontiers_on_fully_known_grid(self):
        g = _grid(size=2.0)
        g.cells = [FREE] * len(g.cells)
        self.assertEqual(find_frontiers(g), [])
        self.assertNotIn(UNKNOWN, g.cells)


class TestNavigator(unittest.TestCase):
    def test_navigate_open_space_holonomic(self):
        bot, tp = _bot("sim")
        nav = Navigator(bot, _grid())
        result = nav.navigate_to(1.0, 0.5, tol=0.2, timeout=20.0, step_fn=tp.step)
        self.assertTrue(result.reached, msg=str(result))
        t = tp.read()
        self.assertLess(math.hypot(t.odom.x - 1.0, t.odom.y - 0.5), 0.25)

    def test_navigate_around_obstacle(self):
        from ohho.adapters.sim import SimObject

        bot, tp = _bot("sim", objects=[SimObject(1.0, 0.0, 0.35, "box")])
        nav = Navigator(bot, _grid())
        result = nav.navigate_to(2.0, 0.0, tol=0.25, timeout=30.0, step_fn=tp.step)
        self.assertTrue(result.reached, msg=str(result))

    def test_navigate_non_holonomic(self):
        spec = RobotSpec(
            id="diff-nav",
            name="Diff",
            category="wheeled",
            capabilities=("base.drive", "perception.scan"),
            max_lin=0.5,
            max_ang=1.5,
        )
        tp = SimTransport(spec)
        tp.connect()
        bot = Robot(spec, tp, NativeRuntime())
        nav = Navigator(bot, _grid())
        result = nav.navigate_to(0.8, 0.6, tol=0.2, timeout=30.0, step_fn=tp.step)
        self.assertTrue(result.reached, msg=str(result))

    def test_goal_inside_obstacle_diverts_nearby(self):
        from ohho.adapters.sim import SimObject

        # Goal is the table's center — unreachable; the navigator should stop
        # at the nearest reachable point beside it instead of failing.
        table = SimObject(2.0, 1.0, 0.45, "table")
        bot, tp = _bot("sim", objects=[table])
        nav = Navigator(bot, _grid())
        result = nav.navigate_to(2.0, 1.0, tol=0.25, timeout=30.0, step_fn=tp.step)
        self.assertTrue(result.reached, msg=str(result))
        t = tp.read()
        dist_center = math.hypot(t.odom.x - 2.0, t.odom.y - 1.0)
        # beside the table (collision physics keeps it at/outside the surface)
        self.assertGreaterEqual(dist_center, table.radius - 1e-6)
        self.assertLess(dist_center, 1.5)

    def test_navigate_timeout(self):
        bot, tp = _bot("sim")
        nav = Navigator(bot, _grid(), speed=0.01)  # too slow to arrive
        result = nav.navigate_to(3.0, 0.0, tol=0.1, timeout=1.0, step_fn=tp.step)
        self.assertFalse(result.reached)
        self.assertEqual(result.reason, "timeout")

    def test_explore_builds_map(self):
        from ohho.adapters.sim import demo_world

        bot, tp = _bot("sim", objects=demo_world())
        nav = Navigator(bot, _grid())
        log = nav.explore(max_frontiers=1, timeout_per=8.0, step_fn=tp.step)
        self.assertTrue(any("explore:" in line for line in log))
        self.assertGreater(nav.grid.counts()["free"], 0)


if __name__ == "__main__":
    unittest.main()
