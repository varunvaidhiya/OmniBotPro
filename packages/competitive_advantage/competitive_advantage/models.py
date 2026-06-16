from pydantic import BaseModel, Field
from typing import Optional


class AdvantageInput(BaseModel):
    business_description: str = Field(
        description="What your business is and what you sell"
    )
    core_strengths: list[str] = Field(
        description="Your three biggest strengths (list of 3)",
        min_length=1,
        max_length=5,
    )
    main_competitors: list[str] = Field(
        description="Who your main competitors are",
        min_length=1,
    )
    competitor_advantages: str = Field(
        description="What competitors do better than you"
    )
    best_customer_segment: str = Field(
        description="Who you actually serve best — the segment where you win"
    )


class AdvantageResult(BaseModel):
    core_advantage: str = Field(description="Your unfair advantage in one sentence")
    why_competitors_cant_match: list[str] = Field(
        description="Two reasons specific to you why competitors can't replicate this"
    )
    where_unbeatable: str = Field(
        description="The exact customer segment or market position where you win"
    )
    gap_exploiting: str = Field(description="What competitors miss that you own")
    raw_response: Optional[str] = None


class PositioningInput(BaseModel):
    competitive_advantage: str = Field(
        description="Your identified competitive advantage"
    )
    current_marketing: str = Field(description="How you currently market yourself")
    feels_generic: str = Field(description="Where you feel like just another option")
    feels_unique: str = Field(description="Where you feel unique and winning")


class PositioningResult(BaseModel):
    current_battlefield: str = Field(description="Where you're positioned now")
    winning_battlefield: str = Field(description="Where you'd be unbeatable")
    shift_required: str = Field(
        description="What changes from current positioning to winning"
    )
    terrain_missing: str = Field(
        description="The specific customer or market position you're not owning"
    )
    raw_response: Optional[str] = None
