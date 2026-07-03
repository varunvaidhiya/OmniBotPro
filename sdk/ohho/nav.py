"""Native navigation — mapping, costmap, A* planning, frontier exploration.

The no-ROS counterpart to Nav2's core loop, sized for the SDK: an occupancy
grid built from the robot's planar scan, obstacle inflation for a costmap, an
8-connected A* planner, frontier-based exploration, and a `Navigator` that
closes the loop through ``Robot.drive()``. Pure standard library; works on any
robot with ``base.drive`` (mapping needs ``perception.scan``, otherwise the
planner is optimistic over unknown space).

Determinism note: every control loop accepts ``step_fn(dt)`` — tests pass the
sim's ``step`` to advance physics synchronously; live callers omit it and the
loop paces itself with ``time.sleep``.
"""

from __future__ import annotations

import heapq
import math
import time
from dataclasses import dataclass
from typing import Callable, List, Optional, Tuple

from .schema import Odometry, Scan, clamp

UNKNOWN = -1
FREE = 0
OCCUPIED = 100

GridIndex = Tuple[int, int]


class OccupancyGrid:
    """A square occupancy grid centered on the world origin."""

    def __init__(self, size_m: float = 12.0, resolution: float = 0.06) -> None:
        self.resolution = resolution
        self.width = max(8, int(round(size_m / resolution)))
        self.height = self.width
        self._origin = -self.width * resolution / 2.0  # world coord of cell (0,0)
        self.cells: list[int] = [UNKNOWN] * (self.width * self.height)

    # ── transforms ────────────────────────────────────────────────────────────
    def world_to_grid(self, x: float, y: float) -> Optional[GridIndex]:
        gx = int((x - self._origin) / self.resolution)
        gy = int((y - self._origin) / self.resolution)
        if 0 <= gx < self.width and 0 <= gy < self.height:
            return gx, gy
        return None

    def grid_to_world(self, gx: int, gy: int) -> Tuple[float, float]:
        return (
            self._origin + (gx + 0.5) * self.resolution,
            self._origin + (gy + 0.5) * self.resolution,
        )

    # ── cell access ───────────────────────────────────────────────────────────
    def get(self, gx: int, gy: int) -> int:
        return self.cells[gy * self.width + gx]

    def set(self, gx: int, gy: int, value: int) -> None:
        self.cells[gy * self.width + gx] = value

    def counts(self) -> dict:
        free = sum(1 for c in self.cells if c == FREE)
        occ = sum(1 for c in self.cells if c == OCCUPIED)
        return {"free": free, "occupied": occ, "unknown": len(self.cells) - free - occ}

    # ── ray updates (Bresenham) ───────────────────────────────────────────────
    def trace(self, x0: float, y0: float, x1: float, y1: float, hit: bool) -> None:
        """Mark the line from (x0,y0) toward (x1,y1) FREE; the endpoint OCCUPIED
        when ``hit`` (the ray stopped on an obstacle)."""
        a = self.world_to_grid(x0, y0)
        b = self.world_to_grid(x1, y1)
        if a is None or b is None:
            return
        (gx0, gy0), (gx1, gy1) = a, b
        dx, dy = abs(gx1 - gx0), abs(gy1 - gy0)
        sx = 1 if gx0 < gx1 else -1
        sy = 1 if gy0 < gy1 else -1
        err = dx - dy
        x, y = gx0, gy0
        while True:
            if (x, y) == (gx1, gy1):
                break
            if self.get(x, y) != OCCUPIED:
                self.set(x, y, FREE)
            e2 = 2 * err
            if e2 > -dy:
                err -= dy
                x += sx
            if e2 < dx:
                err += dx
                y += sy
        if hit:
            self.set(gx1, gy1, OCCUPIED)
        elif self.get(gx1, gy1) != OCCUPIED:
            self.set(gx1, gy1, FREE)

    # ── costmap ───────────────────────────────────────────────────────────────
    def inflate(self, radius_m: float) -> "OccupancyGrid":
        """Return a copy with every occupied cell dilated by ``radius_m``."""
        out = OccupancyGrid.__new__(OccupancyGrid)
        out.resolution = self.resolution
        out.width, out.height = self.width, self.height
        out._origin = self._origin
        out.cells = list(self.cells)
        r = max(1, int(math.ceil(radius_m / self.resolution)))
        stamps = [
            (dx, dy)
            for dx in range(-r, r + 1)
            for dy in range(-r, r + 1)
            if dx * dx + dy * dy <= r * r
        ]
        for gy in range(self.height):
            row = gy * self.width
            for gx in range(self.width):
                if self.cells[row + gx] == OCCUPIED:
                    for dx, dy in stamps:
                        nx, ny = gx + dx, gy + dy
                        if 0 <= nx < self.width and 0 <= ny < self.height:
                            out.cells[ny * self.width + nx] = OCCUPIED
        return out

    # ── display ───────────────────────────────────────────────────────────────
    def ascii(
        self, robot_xy: Optional[Tuple[float, float]] = None, cols: int = 60
    ) -> str:
        """A downsampled text rendering (# occupied · free, space unknown, R robot)."""
        step = max(1, self.width // cols)
        robot_cell = None
        if robot_xy is not None:
            robot_cell = self.world_to_grid(*robot_xy)
        lines: list[str] = []
        for gy in range(self.height - 1, -1, -2 * step):
            row_chars: list[str] = []
            for gx in range(0, self.width, step):
                if (
                    robot_cell
                    and abs(gx - robot_cell[0]) < step
                    and abs(gy - robot_cell[1]) < 2 * step
                ):
                    row_chars.append("R")
                    continue
                block = [
                    self.get(min(gx + i, self.width - 1), max(gy - j, 0))
                    for i in range(step)
                    for j in range(2 * step)
                ]
                if OCCUPIED in block:
                    row_chars.append("#")
                elif FREE in block:
                    row_chars.append("·")
                else:
                    row_chars.append(" ")
            lines.append("".join(row_chars))
        return "\n".join(lines)


class Mapper:
    """Integrates planar scans into an :class:`OccupancyGrid`."""

    def __init__(self, grid: OccupancyGrid) -> None:
        self.grid = grid

    def update(self, odom: Odometry, scan: Scan) -> None:
        px, py, th = odom.x, odom.y, odom.theta
        eps = 1e-3
        for k, rng in enumerate(scan.ranges):
            a = th + scan.angle_min + k * scan.angle_increment
            hit = rng < (scan.range_max - eps)
            r = min(rng, scan.range_max)
            self.grid.trace(px, py, px + r * math.cos(a), py + r * math.sin(a), hit)


# ── A* ─────────────────────────────────────────────────────────────────────────


def astar(
    grid: OccupancyGrid, start: GridIndex, goal: GridIndex
) -> Optional[List[GridIndex]]:
    """8-connected A* over the grid. Unknown space is traversable (optimistic —
    that's what makes frontier exploration converge); occupied is blocked."""
    if grid.get(*goal) == OCCUPIED or grid.get(*start) == OCCUPIED:
        return None
    w, h = grid.width, grid.height
    sqrt2 = math.sqrt(2.0)

    def hcost(n: GridIndex) -> float:
        dx, dy = abs(n[0] - goal[0]), abs(n[1] - goal[1])
        return (dx + dy) + (sqrt2 - 2.0) * min(dx, dy)

    open_q: list[tuple[float, GridIndex]] = [(hcost(start), start)]
    came: dict[GridIndex, GridIndex] = {}
    g: dict[GridIndex, float] = {start: 0.0}
    closed: set[GridIndex] = set()
    while open_q:
        _, cur = heapq.heappop(open_q)
        if cur in closed:
            continue
        if cur == goal:
            path = [cur]
            while cur in came:
                cur = came[cur]
                path.append(cur)
            path.reverse()
            return path
        closed.add(cur)
        cx, cy = cur
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                if dx == 0 and dy == 0:
                    continue
                nx, ny = cx + dx, cy + dy
                if not (0 <= nx < w and 0 <= ny < h):
                    continue
                if grid.get(nx, ny) == OCCUPIED:
                    continue
                step = sqrt2 if dx and dy else 1.0
                ng = g[cur] + step
                n = (nx, ny)
                if ng < g.get(n, math.inf):
                    g[n] = ng
                    came[n] = cur
                    heapq.heappush(open_q, (ng + hcost(n), n))
    return None


# ── frontiers ──────────────────────────────────────────────────────────────────


def find_frontiers(grid: OccupancyGrid, min_cluster: int = 3) -> List[List[GridIndex]]:
    """Clusters of FREE cells that border UNKNOWN — the exploration targets."""
    w, h = grid.width, grid.height

    def is_frontier(gx: int, gy: int) -> bool:
        if grid.get(gx, gy) != FREE:
            return False
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                nx, ny = gx + dx, gy + dy
                if 0 <= nx < w and 0 <= ny < h and grid.get(nx, ny) == UNKNOWN:
                    return True
        return False

    cells = {(x, y) for y in range(h) for x in range(w) if is_frontier(x, y)}
    clusters: list[list[GridIndex]] = []
    while cells:
        seed = cells.pop()
        cluster = [seed]
        stack = [seed]
        while stack:
            cx, cy = stack.pop()
            for dx in (-1, 0, 1):
                for dy in (-1, 0, 1):
                    n = (cx + dx, cy + dy)
                    if n in cells:
                        cells.remove(n)
                        cluster.append(n)
                        stack.append(n)
        if len(cluster) >= min_cluster:
            clusters.append(cluster)
    clusters.sort(key=len, reverse=True)
    return clusters


def _centroid(cluster: List[GridIndex]) -> GridIndex:
    return (
        int(round(sum(c[0] for c in cluster) / len(cluster))),
        int(round(sum(c[1] for c in cluster) / len(cluster))),
    )


# ── navigator ──────────────────────────────────────────────────────────────────

StepFn = Callable[[float], None]


@dataclass
class NavResult:
    reached: bool
    reason: str
    x: float
    y: float
    distance_remaining: float

    def __str__(self) -> str:
        state = "reached" if self.reached else f"stopped ({self.reason})"
        return f"{state} at ({self.x:.2f}, {self.y:.2f}), {self.distance_remaining:.2f} m from goal"


class Navigator:
    """Closed-loop point-to-point navigation + frontier exploration.

    Maps with the robot's scan when it has one; plans with A* over the inflated
    costmap; drives via the capability-gated ``Robot.drive`` (holonomic bases
    strafe, differential bases rotate-then-drive).
    """

    def __init__(
        self,
        robot,
        grid: Optional[OccupancyGrid] = None,
        *,
        robot_radius: float = 0.18,
        lookahead: float = 0.35,
        speed: Optional[float] = None,
        replan_every: int = 8,
    ) -> None:
        self.robot = robot
        self.grid = grid or OccupancyGrid()
        self.mapper = Mapper(self.grid)
        self.robot_radius = robot_radius
        self.lookahead = lookahead
        self.speed = speed if speed is not None else max(0.05, 0.8 * robot.spec.max_lin)
        self.replan_every = replan_every
        self._holonomic = robot.has("base.holonomic_drive")

    # ── sensing ───────────────────────────────────────────────────────────────
    def update_map(self) -> None:
        t = self.robot.telemetry()
        if t and t.odom and t.scan:
            self.mapper.update(t.odom, t.scan)

    def map_ascii(self) -> str:
        t = self.robot.telemetry()
        xy = (t.odom.x, t.odom.y) if (t and t.odom) else None
        return self.grid.ascii(robot_xy=xy)

    # ── go to a point ─────────────────────────────────────────────────────────
    def navigate_to(
        self,
        x: float,
        y: float,
        *,
        tol: float = 0.15,
        timeout: float = 30.0,
        hz: float = 10.0,
        step_fn: Optional[StepFn] = None,
    ) -> NavResult:
        dt = 1.0 / hz
        deadline = time.monotonic() + timeout
        ticks = 0
        path: Optional[List[GridIndex]] = None
        cx = cy = 0.0
        prev: Optional[Tuple[float, float]] = None
        stall = 0
        # "close enough when physically blocked" — arriving beside a solid
        # object whose interior was the requested goal still counts as arrival.
        arrive_slack = max(2.0 * tol, 4.0 * self.robot_radius)
        while True:
            if step_fn is not None:
                step_fn(dt)
            else:
                time.sleep(dt)
                if time.monotonic() > deadline:
                    self.robot.stop()
                    return NavResult(
                        False, "timeout", cx, cy, math.hypot(x - cx, y - cy)
                    )
            if step_fn is not None and ticks > timeout * hz:
                self.robot.stop()
                return NavResult(False, "timeout", cx, cy, math.hypot(x - cx, y - cy))

            t = self.robot.telemetry()
            if not (t and t.odom):
                ticks += 1
                continue
            odom = t.odom
            cx, cy, th = odom.x, odom.y, odom.theta
            if t.scan:
                self.mapper.update(odom, t.scan)

            if path is None or ticks % self.replan_every == 0:
                path, (tx, ty) = self._plan(cx, cy, x, y)
                if path is None:
                    self.robot.stop()
                    return NavResult(
                        False, "no path", cx, cy, math.hypot(x - cx, y - cy)
                    )

            dist = math.hypot(tx - cx, ty - cy)
            if dist <= tol:
                self.robot.stop()
                adjusted = math.hypot(tx - x, ty - y) > tol
                return NavResult(
                    True, "goal (adjusted)" if adjusted else "goal", cx, cy, dist
                )

            # stuck detection: driving but not moving means we're pushing a
            # solid obstacle the map doesn't (fully) know about.
            if (
                prev is not None
                and math.hypot(cx - prev[0], cy - prev[1]) < 0.25 * self.speed * dt
            ):
                stall += 1
            else:
                stall = 0
            prev = (cx, cy)
            if stall >= 12:  # ~1.2 s pinned at hz=10
                if dist <= arrive_slack:
                    self.robot.stop()
                    return NavResult(True, "goal (adjusted)", cx, cy, dist)
                # bump-map the cell just ahead and force a replan around it
                bump = self.grid.world_to_grid(
                    cx + math.cos(th) * 2.0 * self.grid.resolution,
                    cy + math.sin(th) * 2.0 * self.grid.resolution,
                )
                if bump is not None:
                    self.grid.set(*bump, OCCUPIED)
                path = None
                stall = 0

            wx, wy = self._waypoint(path, cx, cy, tx, ty)
            self._drive_toward(wx, wy, cx, cy, th)
            ticks += 1

    def _plan(
        self, cx: float, cy: float, x: float, y: float
    ) -> Tuple[Optional[List[GridIndex]], Tuple[float, float]]:
        """Plan to (x, y), diverting to the nearest reachable cell when the goal
        itself is inside an (inflated) obstacle — "go to the table" should stop
        beside the table, not fail. Returns ``(path, actual_goal_world)``."""
        costmap = self.grid.inflate(self.robot_radius)
        start = costmap.world_to_grid(cx, cy)
        goal = costmap.world_to_grid(x, y)
        if start is None or goal is None:
            return None, (x, y)
        if costmap.get(*goal) == OCCUPIED:
            goal = self._nearest_cell(costmap, goal)
            if goal is None:
                return None, (x, y)
        # Never wedge inside our own inflation: the robot physically occupies a
        # robot_radius disk around its pose, so that disk is free by definition.
        r = max(1, int(math.ceil(self.robot_radius / costmap.resolution)))
        sx, sy = start
        for dx in range(-r, r + 1):
            for dy in range(-r, r + 1):
                if dx * dx + dy * dy > r * r:
                    continue
                nx, ny = sx + dx, sy + dy
                if 0 <= nx < costmap.width and 0 <= ny < costmap.height:
                    if costmap.get(nx, ny) == OCCUPIED:
                        costmap.set(nx, ny, FREE)
        path = astar(costmap, start, goal)
        if path is None:
            # Goal may be enclosed (e.g. the unknown interior of a scanned
            # object). Divert to the nearest known-FREE cell and retry once —
            # "go to the table" stops beside the table instead of failing.
            alt = self._nearest_cell(costmap, goal, free_only=True, max_radius_m=2.5)
            if alt is not None and alt != goal:
                goal = alt
                path = astar(costmap, start, goal)
        return path, self.grid.grid_to_world(*goal)

    @staticmethod
    def _nearest_cell(
        costmap: OccupancyGrid,
        goal: GridIndex,
        *,
        free_only: bool = False,
        max_radius_m: float = 1.5,
    ) -> Optional[GridIndex]:
        """Ring-search outward from ``goal`` for the closest usable cell.

        Prefers known-FREE cells; unless ``free_only``, falls back to the
        nearest UNKNOWN cell when no FREE cell exists in a ring.
        """
        max_r = max(1, int(math.ceil(max_radius_m / costmap.resolution)))
        gx, gy = goal
        fallback: Optional[GridIndex] = None
        for r in range(1, max_r + 1):
            best_free: Optional[GridIndex] = None
            best_free_d = math.inf
            for dx in range(-r, r + 1):
                for dy in (-r, r):
                    for cand in ((gx + dx, gy + dy), (gx + dy, gy + dx)):
                        nx, ny = cand
                        if not (0 <= nx < costmap.width and 0 <= ny < costmap.height):
                            continue
                        value = costmap.get(nx, ny)
                        if value == OCCUPIED:
                            continue
                        d = dx * dx + dy * dy
                        if value == FREE and d < best_free_d:
                            best_free, best_free_d = cand, d
                        elif value == UNKNOWN and fallback is None:
                            fallback = cand
            if best_free is not None:
                return best_free
        if free_only:
            return None
        return fallback

    def _waypoint(
        self, path: List[GridIndex], cx: float, cy: float, gx: float, gy: float
    ) -> Tuple[float, float]:
        for cell in path:
            wx, wy = self.grid.grid_to_world(*cell)
            if math.hypot(wx - cx, wy - cy) >= self.lookahead:
                return wx, wy
        return gx, gy

    def _drive_toward(
        self, wx: float, wy: float, cx: float, cy: float, th: float
    ) -> None:
        dx, dy = wx - cx, wy - cy
        bx = math.cos(th) * dx + math.sin(th) * dy  # body frame
        by = -math.sin(th) * dx + math.cos(th) * dy
        if self._holonomic:
            norm = math.hypot(bx, by) or 1.0
            self.robot.drive(
                vx=self.speed * bx / norm, vy=self.speed * by / norm, w=0.0
            )
        else:
            ang = math.atan2(by, bx)
            max_w = self.robot.spec.max_ang
            if abs(ang) > 0.5:
                self.robot.drive(vx=0.0, w=clamp(2.0 * ang, -max_w, max_w))
            else:
                self.robot.drive(
                    vx=self.speed * math.cos(ang),
                    w=clamp(1.5 * ang, -max_w, max_w),
                )

    # ── frontier exploration ──────────────────────────────────────────────────
    def spin_scan(self, *, hz: float = 10.0, step_fn: Optional[StepFn] = None) -> None:
        """Rotate one full turn while integrating scans — the mapping bootstrap."""
        t = self.robot.telemetry()
        if not (t and t.odom):
            return
        w = max(0.3, 0.5 * self.robot.spec.max_ang)
        ticks = int((2.0 * math.pi / w) * hz)
        dt = 1.0 / hz
        for _ in range(ticks):
            self.robot.drive(w=w)
            if step_fn is not None:
                step_fn(dt)
            else:
                time.sleep(dt)
            self.update_map()
        self.robot.stop()

    def explore(
        self,
        *,
        max_frontiers: int = 4,
        timeout_per: float = 20.0,
        step_fn: Optional[StepFn] = None,
    ) -> List[str]:
        """Frontier exploration: spin-scan, then repeatedly drive to the nearest
        frontier cluster until none remain or the budget runs out."""
        log: list[str] = []
        self.spin_scan(step_fn=step_fn)
        for i in range(max_frontiers):
            self.update_map()
            clusters = find_frontiers(self.grid)
            if not clusters:
                log.append(f"explore: no frontiers left after {i} target(s) — done")
                break
            t = self.robot.telemetry()
            cx, cy = (t.odom.x, t.odom.y) if (t and t.odom) else (0.0, 0.0)

            def dist_to(cl: List[GridIndex]) -> float:
                wx, wy = self.grid.grid_to_world(*_centroid(cl))
                return math.hypot(wx - cx, wy - cy)

            target = min(clusters, key=dist_to)
            wx, wy = self.grid.grid_to_world(*_centroid(target))
            result = self.navigate_to(
                wx, wy, tol=0.3, timeout=timeout_per, step_fn=step_fn
            )
            log.append(f"explore: frontier {i + 1} at ({wx:.2f}, {wy:.2f}) → {result}")
        counts = self.grid.counts()
        log.append(
            f"explore: map now {counts['free']} free / {counts['occupied']} occupied cells"
        )
        return log


__all__ = [
    "UNKNOWN",
    "FREE",
    "OCCUPIED",
    "OccupancyGrid",
    "Mapper",
    "astar",
    "find_frontiers",
    "NavResult",
    "Navigator",
]
