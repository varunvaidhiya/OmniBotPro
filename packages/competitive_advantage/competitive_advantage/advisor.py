import re
from typing import Optional

from .models import (
    AdvantageInput,
    AdvantageResult,
    PositioningInput,
    PositioningResult,
)
from .prompts import (
    ADVANTAGE_SYSTEM,
    ADVANTAGE_USER_TEMPLATE,
    POSITIONING_SYSTEM,
    POSITIONING_USER_TEMPLATE,
)

_MODEL = "claude-sonnet-4-6"


class AdvantageAdvisor:
    def __init__(self, api_key: Optional[str] = None, model: str = _MODEL):
        import anthropic  # lazy: not needed for parser-only usage or tests

        self._client = anthropic.Anthropic(api_key=api_key)
        self._model = model

    def identify_advantage(self, data: AdvantageInput) -> AdvantageResult:
        strengths_text = "\n".join(f"- {s}" for s in data.core_strengths)
        competitors_text = "\n".join(f"- {c}" for c in data.main_competitors)

        user_msg = ADVANTAGE_USER_TEMPLATE.format(
            business_description=data.business_description,
            core_strengths=strengths_text,
            main_competitors=competitors_text,
            competitor_advantages=data.competitor_advantages,
            best_customer_segment=data.best_customer_segment,
        )

        response = self._client.messages.create(
            model=self._model,
            max_tokens=1024,
            system=ADVANTAGE_SYSTEM,
            messages=[{"role": "user", "content": user_msg}],
        )
        raw = response.content[0].text
        return _parse_advantage(raw)

    def audit_positioning(self, data: PositioningInput) -> PositioningResult:
        user_msg = POSITIONING_USER_TEMPLATE.format(
            competitive_advantage=data.competitive_advantage,
            current_marketing=data.current_marketing,
            feels_generic=data.feels_generic,
            feels_unique=data.feels_unique,
        )

        response = self._client.messages.create(
            model=self._model,
            max_tokens=1024,
            system=POSITIONING_SYSTEM,
            messages=[{"role": "user", "content": user_msg}],
        )
        raw = response.content[0].text
        return _parse_positioning(raw)


def _extract(text: str, label: str) -> str:
    pattern = rf"{re.escape(label)}:\s*(.+?)(?=\n[A-Z][^\n]+:|$)"
    match = re.search(pattern, text, re.DOTALL | re.IGNORECASE)
    return match.group(1).strip() if match else ""


def _parse_advantage(raw: str) -> AdvantageResult:
    core = _extract(raw, "Your Core Advantage")
    cant_match_block = _extract(raw, "Why Competitors Can't Match It")
    reasons = [
        line.lstrip("0123456789.-) ").strip()
        for line in cant_match_block.splitlines()
        if line.strip()
    ]
    unbeatable = _extract(raw, "Where You're Unbeatable")
    gap = _extract(raw, "The Gap You're Exploiting")

    return AdvantageResult(
        core_advantage=core,
        why_competitors_cant_match=reasons or [cant_match_block],
        where_unbeatable=unbeatable,
        gap_exploiting=gap,
        raw_response=raw,
    )


def _parse_positioning(raw: str) -> PositioningResult:
    return PositioningResult(
        current_battlefield=_extract(raw, "Current Battlefield"),
        winning_battlefield=_extract(raw, "Winning Battlefield"),
        shift_required=_extract(raw, "The Shift Required"),
        terrain_missing=_extract(raw, "Terrain You're Missing"),
        raw_response=raw,
    )
