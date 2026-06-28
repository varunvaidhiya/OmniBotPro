"""The ``ohho`` command-line interface."""

from __future__ import annotations

import argparse
import sys
import time
from typing import Optional

from . import __version__
from .adapters import available_adapters
from .agent import Agent
from .hardware import resolve_device
from .registry import UnknownRobot, list_specs
from .robot import Robot
from .runtime import available_runtimes


def _cmd_version(args) -> int:
    print(f"OhhO OS {__version__}")
    return 0


def _cmd_doctor(args) -> int:
    print(f"OhhO OS {__version__}")
    print(f"  python      : {sys.version.split()[0]}")
    print(f"  runtimes    : {', '.join(available_runtimes())}")
    print(f"  adapters    : {', '.join(available_adapters())}  (others via extras)")
    print(f"  device      : {resolve_device('auto')}")
    print(f"  robots      : {', '.join(s.id for s in list_specs())}")
    print(
        "  recommended : runtime=native  (ROS 2 backend arrives in a later milestone)"
    )
    return 0


def _cmd_list(args) -> int:
    for s in list_specs():
        caps = ", ".join(s.capabilities) or "—"
        print(f"{s.id:<14} {s.name:<18} {s.category:<20} [{caps}]")
    return 0


def _connect(args) -> Robot:
    return Robot.connect(args.robot, transport=args.transport, runtime=args.runtime)


def _cmd_connect(args) -> int:
    try:
        bot = _connect(args)
    except UnknownRobot as e:
        print(f"error: {e}", file=sys.stderr)
        return 2
    try:
        print(f"connected: {bot!r}")
        s = bot.status()
        print(f"  status : {s.state.value} — {s.label}")
        time.sleep(0.3)
        t = bot.telemetry()
        if t and t.odom:
            print(
                f"  odom   : x={t.odom.x:.3f} y={t.odom.y:.3f} theta={t.odom.theta:.3f}"
            )
        if t and t.battery is not None:
            print(f"  battery: {t.battery * 100:.0f}%")
    finally:
        bot.disconnect()
    return 0


def _cmd_sim(args) -> int:
    try:
        bot = Robot.connect(args.robot, transport="sim://", runtime="native")
    except UnknownRobot as e:
        print(f"error: {e}", file=sys.stderr)
        return 2
    print(
        f"simulating {bot.spec.name} — driving a pattern for {args.seconds:.0f}s (Ctrl-C to stop)"
    )
    try:
        i = 0
        end = time.monotonic() + args.seconds
        while time.monotonic() < end:
            phase = i % 4
            if phase == 0:
                bot.drive(vx=0.15)
            elif phase == 1:
                bot.drive(w=0.6)
            elif phase == 2:
                bot.drive(vx=0.15, vy=0.1)
            else:
                bot.drive(w=-0.6)
            time.sleep(0.5)
            t = bot.telemetry()
            if t and t.odom:
                batt = (t.battery or 0.0) * 100
                print(
                    f"  t+{i * 0.5:>4.1f}s  x={t.odom.x:6.3f} y={t.odom.y:6.3f} "
                    f"th={t.odom.theta:6.3f}  batt={batt:3.0f}%"
                )
            i += 1
    except KeyboardInterrupt:
        print("\nstopped.")
    finally:
        bot.stop()
        bot.disconnect()
    return 0


def _cmd_drive(args) -> int:
    try:
        bot = _connect(args)
    except UnknownRobot as e:
        print(f"error: {e}", file=sys.stderr)
        return 2
    try:
        print(
            f"driving {bot.spec.id}: vx={args.vx} vy={args.vy} w={args.w} for {args.seconds:.0f}s"
        )
        end = time.monotonic() + args.seconds
        while time.monotonic() < end:
            bot.drive(vx=args.vx, vy=args.vy, w=args.w)
            time.sleep(0.1)
        bot.stop()
        time.sleep(0.1)
        t = bot.telemetry()
        if t and t.odom:
            print(
                f"final odom: x={t.odom.x:.3f} y={t.odom.y:.3f} theta={t.odom.theta:.3f}"
            )
    finally:
        bot.disconnect()
    return 0


def _cmd_agent(args) -> int:
    try:
        bot = _connect(args)
    except UnknownRobot as e:
        print(f"error: {e}", file=sys.stderr)
        return 2
    try:
        for line in Agent(bot).run(args.goal):
            print(f"  {line}")
    finally:
        bot.disconnect()
    return 0


def _add_robot_args(
    sp: argparse.ArgumentParser, transport_default: Optional[str] = None
) -> None:
    sp.add_argument("robot", help="robot id (see `ohho list`)")
    sp.add_argument(
        "--transport", default=transport_default, help="transport URI, e.g. sim://"
    )
    sp.add_argument("--runtime", default="auto", choices=["auto", "native", "ros2"])


def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="ohho", description="OhhO OS — the open robot engine."
    )
    p.add_argument(
        "-V", "--version", action="store_true", help="print version and exit"
    )
    sub = p.add_subparsers(dest="command")

    sub.add_parser(
        "doctor", help="show environment, runtimes, adapters and robots"
    ).set_defaults(func=_cmd_doctor)
    sub.add_parser("list", help="list built-in robots").set_defaults(func=_cmd_list)
    sub.add_parser("version", help="print version").set_defaults(func=_cmd_version)

    c = sub.add_parser("connect", help="connect and print status + telemetry")
    _add_robot_args(c)
    c.set_defaults(func=_cmd_connect)

    s = sub.add_parser("sim", help="run a robot in simulation")
    s.add_argument("--robot", default="omnibot")
    s.add_argument("--seconds", type=float, default=8.0)
    s.set_defaults(func=_cmd_sim)

    d = sub.add_parser("drive", help="drive a robot for a duration")
    _add_robot_args(d)
    d.add_argument("--vx", type=float, default=0.1)
    d.add_argument("--vy", type=float, default=0.0)
    d.add_argument("--w", type=float, default=0.0)
    d.add_argument("--seconds", type=float, default=3.0)
    d.set_defaults(func=_cmd_drive)

    a = sub.add_parser("agent", help="hand a natural-language goal to the agent")
    _add_robot_args(a)
    a.add_argument("goal", help='the goal, e.g. "explore the room"')
    a.set_defaults(func=_cmd_agent)

    return p


def main(argv: Optional[list[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    if getattr(args, "version", False):
        return _cmd_version(args)
    if not getattr(args, "command", None):
        parser.print_help()
        return 0
    return args.func(args)


if __name__ == "__main__":
    raise SystemExit(main())
