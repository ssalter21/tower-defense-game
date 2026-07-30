# Design documents

Deep dives, in the order they were written. Each one answers a question raised by the one before it, so
they read best in sequence.

| # | Document | Question it answers | Verdict |
|---|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? | Viable, but **not multiplayer-first**. Synchronous PvP TD tops out around 830 concurrent players. |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix that, and what does it cost? | The model is proven and fits TD unusually well. **Determinism is the whole build risk.** |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? | Unity 6 client, plain C# integer sim library, no realtime networking. |
| V | [Tower & Creep Variance Levers](variance-levers-and-unit-schema.md) | What can a tower or a creep actually differ by, and what data structure holds all of it? | **One unit schema, two roles**, levers as components. Version the numbers separately from the vocabulary — and never silently skip an unknown lever. |

> Part IV (art direction and asset strategy) is being written on a separate branch and is not merged yet, hence
> the gap. Part V does not depend on it.

## The thread running through all of them

Part I found that every competitive tower defense is population-gated, and that a tower layout is the most
snapshot-friendly artefact in strategy gaming — so the competition can be made asynchronous and the ceiling
disappears.

Part II examined that claim against shipped games, found it sound, and identified the one thing that cannot be
retrofitted: **the simulation must be deterministic**, in fixed-point integer math, isolated from rendering,
from the first commit.

Part III takes that as the binding constraint and works out the stack from it. The conclusion is that the
simulation must be a separately compiled library with no engine reference — consumed unchanged by the client,
the server that re-validates results, and a headless harness — which makes the engine choice reversible and
turns balance into a computation.

Part V takes *that* claim seriously. Balance is only a computation if the things being balanced are described
rather than coded, so it catalogues every axis a tower or a creep can vary along — surveyed against what a
dozen shipped games actually do — and derives the schema that has to hold them. Its two structural findings:
the tower/creep split is an artefact of single-player games and should not survive into a format where players
author both halves, and the vocabulary of levers must be versioned separately from the numbers, because a
stored ghost has to mean the same thing in two years.

## Status

No code yet. The build order is in [Part II, section 6](async-ghost-round-robin.md#6-build-order--how-to-de-risk-this-in-order);
step 1 is a determinism harness, and nothing has been started.

The open question flagged in Part III is whether a shareable browser replay viewer matters enough to move the
simulation to Rust. Current assumption: no — C# throughout.
