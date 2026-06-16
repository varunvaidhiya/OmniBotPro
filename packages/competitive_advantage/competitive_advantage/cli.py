"""CLI entry point: competitive-advantage <command> [options]

Commands:
  advantage   Run The Advantage Identifier
  positioning Run The Positioning Audit
"""

import argparse
import json
import os
import sys
import textwrap

from .advisor import AdvantageAdvisor
from .models import AdvantageInput, PositioningInput


def _prompt(label: str, multiline: bool = False) -> str:
    if multiline:
        print(f"{label} (end with a blank line):")
        lines = []
        while True:
            line = input()
            if line == "":
                break
            lines.append(line)
        return "\n".join(lines)
    return input(f"{label}: ").strip()


def _prompt_list(label: str) -> list[str]:
    print(f"{label} (one per line, blank line to finish):")
    items = []
    while True:
        line = input("  - ").strip()
        if not line:
            break
        items.append(line)
    return items


def _divider() -> None:
    print("\n" + "─" * 60 + "\n")


def _print_advantage(result) -> None:
    _divider()
    print("YOUR CORE ADVANTAGE")
    print(textwrap.fill(result.core_advantage, width=72))

    print("\nWHY COMPETITORS CAN'T MATCH IT")
    for i, reason in enumerate(result.why_competitors_cant_match, 1):
        print(textwrap.fill(f"{i}. {reason}", width=72, subsequent_indent="   "))

    print("\nWHERE YOU'RE UNBEATABLE")
    print(textwrap.fill(result.where_unbeatable, width=72))

    print("\nTHE GAP YOU'RE EXPLOITING")
    print(textwrap.fill(result.gap_exploiting, width=72))
    _divider()


def _print_positioning(result) -> None:
    _divider()
    print("CURRENT BATTLEFIELD")
    print(textwrap.fill(result.current_battlefield, width=72))

    print("\nWINNING BATTLEFIELD")
    print(textwrap.fill(result.winning_battlefield, width=72))

    print("\nTHE SHIFT REQUIRED")
    print(textwrap.fill(result.shift_required, width=72))

    print("\nTERRAIN YOU'RE MISSING")
    print(textwrap.fill(result.terrain_missing, width=72))
    _divider()


def cmd_advantage(args, advisor: AdvantageAdvisor) -> None:
    if args.file:
        with open(args.file) as f:
            data = AdvantageInput(**json.load(f))
    else:
        print("\n=== THE ADVANTAGE IDENTIFIER ===\n")
        business = _prompt("Describe your business and what you sell", multiline=True)
        strengths = _prompt_list("Your three biggest strengths")
        competitors = _prompt_list("Your main competitors")
        comp_adv = _prompt("What competitors do better than you", multiline=True)
        segment = _prompt(
            "Who you serve best (the segment where you win)", multiline=True
        )
        data = AdvantageInput(
            business_description=business,
            core_strengths=strengths,
            main_competitors=competitors,
            competitor_advantages=comp_adv,
            best_customer_segment=segment,
        )

    print("\nAnalyzing your competitive advantage...")
    result = advisor.identify_advantage(data)

    if args.json:
        print(result.model_dump_json(indent=2, exclude={"raw_response"}))
    else:
        _print_advantage(result)


def cmd_positioning(args, advisor: AdvantageAdvisor) -> None:
    if args.file:
        with open(args.file) as f:
            data = PositioningInput(**json.load(f))
    else:
        print("\n=== THE POSITIONING AUDIT ===\n")
        advantage = _prompt("Your competitive advantage", multiline=True)
        marketing = _prompt("How you currently market yourself", multiline=True)
        generic = _prompt("Where you feel like just another option", multiline=True)
        unique = _prompt("Where you feel unique and winning", multiline=True)
        data = PositioningInput(
            competitive_advantage=advantage,
            current_marketing=marketing,
            feels_generic=generic,
            feels_unique=unique,
        )

    print("\nAuditing your market position...")
    result = advisor.audit_positioning(data)

    if args.json:
        print(result.model_dump_json(indent=2, exclude={"raw_response"}))
    else:
        _print_positioning(result)


def main() -> None:
    parser = argparse.ArgumentParser(
        prog="competitive-advantage",
        description="AI-powered competitive advantage identifier and positioning audit",
    )
    parser.add_argument(
        "--api-key",
        default=os.environ.get("ANTHROPIC_API_KEY"),
        help="Anthropic API key (defaults to $ANTHROPIC_API_KEY)",
    )
    parser.add_argument(
        "--model",
        default="claude-sonnet-4-6",
        help="Claude model to use",
    )

    sub = parser.add_subparsers(dest="command", required=True)

    adv = sub.add_parser("advantage", help="Run The Advantage Identifier")
    adv.add_argument("--file", "-f", help="JSON file with AdvantageInput fields")
    adv.add_argument("--json", action="store_true", help="Output as JSON")

    pos = sub.add_parser("positioning", help="Run The Positioning Audit")
    pos.add_argument("--file", "-f", help="JSON file with PositioningInput fields")
    pos.add_argument("--json", action="store_true", help="Output as JSON")

    args = parser.parse_args()

    if not args.api_key:
        print("Error: set ANTHROPIC_API_KEY or pass --api-key", file=sys.stderr)
        sys.exit(1)

    advisor = AdvantageAdvisor(api_key=args.api_key, model=args.model)

    if args.command == "advantage":
        cmd_advantage(args, advisor)
    elif args.command == "positioning":
        cmd_positioning(args, advisor)


if __name__ == "__main__":
    main()
