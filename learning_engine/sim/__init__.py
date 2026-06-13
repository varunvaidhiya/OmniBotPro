from .curriculum import CurriculumScheduler, CurriculumStage, DomainRandomizer
from . import adapters  # noqa: F401 — populates the ENVS registry

__all__ = ["CurriculumScheduler", "CurriculumStage", "DomainRandomizer"]
