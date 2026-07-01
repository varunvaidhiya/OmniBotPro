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
    import importlib.util

    from .brains import harness_available

    def have(mod: str) -> bool:
        return importlib.util.find_spec(mod) is not None

    runtimes = available_runtimes()
    print(f"OhhO OS {__version__}")
    print(f"  python      : {sys.version.split()[0]}")
    print(f"  runtimes    : {', '.join(runtimes)}")
    print(f"  adapters    : {', '.join(available_adapters())}  (others via extras)")
    print(f"  device      : {resolve_device('auto')}")
    print(f"  robots      : {', '.join(s.id for s in list_specs())}")
    brain = (
        "harness — full perceive→reason→act→reflect"
        if harness_available()
        else "scripted fallback  (install 'ohho-os[agent]' + repo agent_engine)"
    )
    print(f"  agent brain : {brain}")
    print(
        f"  serve       : {'ready (fastapi)' if have('fastapi') else 'needs [serve] extra'}"
    )
    print(
        f"  data writer : {'parquet (pyarrow)' if have('pyarrow') else 'json fallback  (add [data] for parquet)'}"
    )
    print(
        f"  train       : {'torch available' if have('torch') else 'mock only  (add [train] for real training)'}"
    )
    print(f"  recommended : runtime={'ros2' if 'ros2' in runtimes else 'native'}")
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


def _cmd_serve(args) -> int:
    from .serve import serve, ServeUnavailable

    try:
        serve(
            checkpoint=args.checkpoint,
            port=args.port,
            host=args.host,
            device=args.device,
            mock=args.mock,
        )
    except ServeUnavailable as e:
        print(f"error: {e}", file=sys.stderr)
        return 2
    return 0


def _cmd_market(args) -> int:
    from .market import list_skills, run_skill, SkillRequirementsNotMet

    if args.action == "list":
        skills = list_skills()
        if not skills:
            print("no skills registered")
            return 0
        for s in skills:
            reqs = ", ".join(s.requires) or "—"
            tags = ", ".join(s.tags) or "—"
            print(
                f"{s.name:<16} v{s.version:<6} [{reqs}]  "
                f"tags: {tags}\n  {s.description}"
            )
        return 0
    if args.action == "run":
        try:
            bot = Robot.connect(
                args.robot, transport=args.transport, runtime=args.runtime
            )
        except Exception as e:
            print(f"error: {e}", file=sys.stderr)
            return 2
        try:
            result = run_skill(args.skill_name, bot)
            print(f"skill '{args.skill_name}': {result}")
        except SkillRequirementsNotMet as e:
            print(f"error: {e}", file=sys.stderr)
            return 3
        except KeyError as e:
            print(f"error: {e}", file=sys.stderr)
            return 2
        finally:
            bot.disconnect()
        return 0
    print("usage: ohho market [list|run]")
    return 1


def _cmd_profile(args) -> int:
    from . import profiles

    if args.action == "list":
        for name in profiles.list_profiles():
            p = profiles.get_profile(name)
            print(f"{name:<22} {p.description}")
        return 0
    if args.action == "show":
        p = profiles.get_profile(args.profile_name)
        print(f"profile: {p.name}")
        print(f"  {p.description}")
        for node in p.nodes:
            print(f"  node: {node.name}")
            print(f"    roles: {', '.join(node.roles)}")
            print(f"    device: {node.device}  accelerator: {node.accelerator or '—'}")
            if node.notes:
                print(f"    notes: {node.notes}")
        return 0
    if args.action == "detect":
        p = profiles.detect_profile()
        desc = profiles.describe()
        print(f"detected: {p.name}")
        print(f"  {p.description}")
        print(f"  platform: {desc['platform']}")
        print(f"  device: {desc['device']}")
        return 0
    print("usage: ohho profile [list|show|detect]")
    return 1


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

    sv = sub.add_parser("serve", help="serve a trained policy over REST")
    sv.add_argument("--checkpoint", default="", help="path to trained checkpoint")
    sv.add_argument("--port", type=int, default=8000)
    sv.add_argument("--host", default="0.0.0.0")
    sv.add_argument("--device", default="auto")
    sv.add_argument(
        "--mock", action="store_true", help="use a no-op model (sim loop, no GPU)"
    )
    sv.set_defaults(func=_cmd_serve)

    mk = sub.add_parser("market", help="list and run robot skills")
    mk_sub = mk.add_subparsers(dest="action")
    mk_sub.add_parser("list", help="list registered skills").set_defaults(action="list")
    mk_run = mk_sub.add_parser("run", help="run a skill on a robot")
    _add_robot_args(mk_run)
    mk_run.add_argument("skill_name", help="skill name (see 'ohho market list')")
    mk_run.set_defaults(action="run")
    mk.set_defaults(func=_cmd_market)

    pf = sub.add_parser("profile", help="hardware deployment profiles")
    pf_sub = pf.add_subparsers(dest="action")
    pf_sub.add_parser("list", help="list built-in profiles").set_defaults(action="list")
    pf_show = pf_sub.add_parser("show", help="show a specific profile")
    pf_show.add_argument("profile_name", help="profile name")
    pf_show.set_defaults(action="show")
    pf_sub.add_parser(
        "detect", help="auto-detect the current machine's profile"
    ).set_defaults(action="detect")
    pf.set_defaults(func=_cmd_profile)

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
