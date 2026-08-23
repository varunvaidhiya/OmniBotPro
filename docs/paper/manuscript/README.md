# OmniBot Manuscript Workspace

This directory contains a **pre-results** LaTeX manuscript for the first planned OmniBot paper:

> *OmniBot: An Affordable, Distributed Embodied-AI Platform for Mobile Manipulation with Vision-Language-Action Models*

## Files

| File | Purpose |
|---|---|
| `main.tex` | Complete IEEE-conference-style manuscript draft with no fabricated measurements |
| `references.bib` | Curated seed bibliography used by the manuscript |
| `../LITERATURE_REVIEW.md` | Polished evidence-based related-work synthesis |
| `../RESEARCH_DESIGN.md` | Research questions, claim boundaries and experimental protocol |
| `../LITERATURE_REVIEW_NOTES.md` | Evidence log and source-level caution notes |
| `results/` | Place immutable paper-freeze copies of raw benchmark and trial results here |
| `figures/` | Place source-controlled figures here; retain scripts/source data alongside them |
| `tables/` | Place generated table inputs/scripts here |

## Before inserting a result

A measurement may enter `main.tex` only after the following are all stored in the paper-freeze release:

1. The exact git commit/tag and the benchmark or trial command are recorded.
2. The machine, GPU/driver, operating-system, ROS 2, model/checkpoint, quantization, network and clock-synchronization metadata are present.
3. The raw JSON/CSV output and the analysis script that generated the reported figure/table are available.
4. The local-edge and distributed conditions are matched or explicitly marked as non-comparable.
5. Physical trial success is reported with numerator/denominator, a confidence interval and a failure taxonomy.
6. Costs are assigned to robot-only, on-robot operational and full experimental-stack boundaries.

Do **not** replace a red `[TBD: ...]` placeholder with an expected, simulated or manually transcribed result.

## Recommended compilation

Use the target venue’s current IEEE RA-L or conference template as the final source of truth. This draft currently uses the standard `ieeeconf` class:

```text
pdflatex main
bibtex main
pdflatex main
pdflatex main
```

A LaTeX compiler was not installed on the attached Windows workstation during this draft cycle, so compilation must be run in Overleaf or an environment that provides `ieeeconf.cls`, `IEEEtran.bst`, and the packages listed in `main.tex`.

## Pre-submission gate

The paper is not submission-ready until all of the following are complete: a frozen BOM; Pi and GPU benchmark JSON; a matched local-versus-distributed experiment; synchronized timing metadata; a documented seeded-regression test; physical task-trial logs; final figures from raw data; verified bibliography metadata; author/affiliation details; licence/attribution review; and a line-by-line claim audit against released artefacts.
