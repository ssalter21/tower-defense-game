# The archive

**The five deep dives the vision was built on.** They are an input to it, not the current state of anything.
Read them for their evidence; read [The Vision](../vision.md) for the plan.

They read best in sequence — each answers a question raised by the one before it.

| # | Document | Question it answered |
|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix the population ceiling, and what does it cost? |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? |
| IV | [Art Direction & Asset Pack Strategy](art-direction-and-assets.md) | What does it look like, and what do we buy to get there fast? |
| V | [Tower & Creep Variance Levers](variance-levers-and-unit-schema.md) | What can a tower or a creep differ by, and what data structure holds all of it? |

## What the vision overturns

Row by row, so nothing below is left standing where it has been replaced. **This is the only place that
account exists.**

| Where | What it said | What is true now |
|---|---|---|
| **I** — whole document | Commercial viability is the question | **Superseded.** Not a commercial product. The market analysis is background, not a constraint. |
| **II** — the async argument | Async is justified by the 830-player synchronous ceiling | **Reason replaced, conclusion kept.** Async is justified by schedule mismatch. True at three players. |
| **II §3** — defense feels meaningless | Fix it with cross-fed currencies, per Supercell | **Not adopted.** Both boards are live every round, so nothing needs cross-feeding. One purse. |
| **II §3** — matching axis | Match on progression state first, rating second | **Sharpened.** The draw is per *round*, at the matching stage — the only matching axis that exists — and it draws a **field of ten** rather than one. |
| **II §5** — UGC discovery | Curation is a feature, not a backlog item | **Does not apply.** Opponents are drawn, never browsed. No discovery surface exists to get wrong. |
| **II §6** — build order | Private friend lobbies are step 7, the one synchronous mode | **Promoted and reclassified.** The lobby is not synchronous and is not last; it is the same loop at low latency. |
| **III** — networking | No realtime networking | **Stands, for a new reason.** Live PvP is in scope and still needs none — a build phase with a barrier is a turn. |
| **III** — balance as computation | A deterministic sim turns balance into a computation | **Adopted as the method.** Seam 4 is where the claim gets spent. |
| **IV** — KayKit Complete, $150 | The day-one purchase | **Reactivated.** It was paused for the free-tier walking skeleton, never overturned. |
| **IV** — can the dev rig and animate? | The KayKit-versus-Synty recommendation turns on this | **Closed by irrelevance.** KayKit ships animations; the question only mattered for Synty. |
| **V** — the unit schema | One unit, two roles; levers as components; versioned vocabulary | **Stands, and is now load-bearing.** Seam 3 fills it in. |

One further reversal touches Parts IV and V but is the vision's own, not theirs: **mazing and pathfinding are
in scope.** They were ruled out on the grounds that the corridor is one hex wide and never branches, so no
unit ever chooses a path. [The board is a maze](../vision.md#the-board-is-a-maze) instead. Several passages in
Parts IV and V still reason from the corridor.

## The thread running through the five

Part I found that every competitive tower defense is population-gated, and that a tower layout is the most
snapshot-friendly artefact in strategy gaming — so the competition can be made asynchronous and the ceiling
disappears.

Part II examined that claim against shipped games, found it sound, and identified the one thing that cannot be
retrofitted: **the simulation must be deterministic**, in fixed-point integer math, isolated from rendering,
from the first commit.

Part III takes that as the binding constraint and works out the stack from it: the simulation must be a
separately compiled library with no engine reference — consumed unchanged by the client, the server that
re-validates results, and a headless harness — which makes the engine choice reversible and turns balance into
a computation.

Part IV tests the one assumption Part III made without examining it — that the art is stylized 3D — and closes
it. The art direction holds, but the reason changes: 2D loses on **unit-count × facings arithmetic**, not on
taste. Part IV also extends Part III's rendering rule into art: **the sim owns attack timing in integer ticks
and the view scales animation playback to fit** — which is what makes swapping asset packs, or the whole art
direction, a reversible decision rather than a rewrite.

Part V takes Part III's closing claim — that balance becomes a computation — and points out it is only true if
the things being balanced are *described* rather than *coded*. So it catalogues every axis a tower or a creep
can vary along and derives the schema that has to hold them. Two findings change the shape of the sim: the
tower-versus-creep split is an artefact of single-player games and should not survive into a format where
players author both halves, so there is **one unit with two roles**; and the vocabulary of levers must be
versioned separately from the numbers, because a stored ghost has to mean the same thing in two years. Part V
also sharpens Part III's no-floats rule with a reason Part III did not have: ECMA-334 §8.3.7 permits any C#
implementation to compute floating point at higher precision than the declared type, so a float sim is not
replay-stable even on one machine with one binary.
