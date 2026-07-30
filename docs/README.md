# Design documents

Five deep dives, in the order they were written. Each one answers a question raised by the one before it, so
they read best in sequence.

| # | Document | Question it answers | Verdict |
|---|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? | Viable, but **not multiplayer-first**. Synchronous PvP TD tops out around 830 concurrent players. |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix that, and what does it cost? | The model is proven and fits TD unusually well. **Determinism is the whole build risk.** |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? | Unity 6 client, plain C# integer sim library, no realtime networking. |
| IV | [Art Direction & Asset Pack Strategy](art-direction-and-assets.md) | What does it look like, and what do we buy to get there fast? | Stylized 3D, not pixel art. **KayKit Complete, $150.** Synty ships no animations — that inverts the obvious pick. |
| V | [Tower & Creep Variance Levers](variance-levers-and-unit-schema.md) | What can a tower or a creep actually differ by, and what data structure holds all of it? | **One unit schema, two roles**, levers as components. Version the numbers apart from the vocabulary — and never silently skip an unknown lever. |

## The thread running through all five

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

Part IV tests the one assumption Part III made without examining it — that the art is stylized 3D — and closes
it. The art direction holds, but the reason changes: 2D loses on **unit-count × facings arithmetic**, not on
taste. Part III's stack verdict survives, though its Unity justification does not: asset packs are portable
between engines now, so what still holds Unity in place is Mecanim humanoid retargeting and UI, not lock-in.
Part IV also extends Part III's rendering rule into art: **the sim owns attack timing in integer ticks and the
view scales animation playback to fit** — which is what makes swapping asset packs, or the whole art direction,
a reversible decision rather than a rewrite.

Part V takes Part III's closing claim — that balance becomes a computation — and points out it is only true if
the things being balanced are *described* rather than *coded*. So it catalogues every axis a tower or a creep
can vary along, checked against what a dozen shipped games actually do, and derives the schema that has to hold
them. Two findings change the shape of the sim: the tower-versus-creep split is an artefact of single-player
games and should not survive into a format where players author both halves, so there is **one unit with two
roles**; and the vocabulary of levers must be versioned separately from the numbers, because a stored ghost has
to mean the same thing in two years. Part V also sharpens Part III's no-floats rule with a reason Part III did
not have: ECMA-334 §8.3.7 permits any C# implementation to compute floating point at higher precision than the
declared type, so a float sim is not replay-stable even on one machine with one binary.

## Status

No code yet. The build order is in [Part II, section 6](async-ghost-round-robin.md#6-build-order--how-to-de-risk-this-in-order);
step 1 is a determinism harness, and nothing has been started. Nothing in Part IV repeals Part III's rule that
art comes *after* the step-3 gate — steps 1 to 3 are answerable with capsules.

Open questions, in order of consequence:

- **Mazing or preset path?** Does the player author the route by placing towers on open ground (Wintermaul,
  Green Circle TD), or does the map ship with walls and a fixed route the player fills with towers (the Gem TD
  shape)? **Current lean: preset path** — fixed walls, designated tower slots. Six levers in Part V assume the
  mazing answer and become dead weight under the other: `Path policy` and `Repath trigger` (§3.5), `blocksPath`
  and Sanctum's maze/gun resource split and geometry-driven stats (§3.9), and route choice (§3.6). It also
  raises the stakes on the damage-type matrix spread (§4.1) — with no mazing to differentiate builds, tower
  selection and wave composition carry the entire strategic surface. Free to decide on paper; decide it before
  the first ruleset.
- **Top-down grid or side-on lane?** Flagged in [Part IV, §11](art-direction-and-assets.md#11-the-one-input-i-do-not-have).
  It decides whether 2D is viable at all, and it is a free decision made on paper. Answer it alongside the
  question above — the two constrain each other, and neither costs anything to settle now.
- **Can the developer rig and animate in Blender?** The Part IV recommendation flips from KayKit to Synty on
  this single fact.
- **How wide should the damage-type matrix be?** Flagged in [Part V, §4.1](variance-levers-and-unit-schema.md#41-the-scalar-layer--three-shapes-pick-exactly-one).
  Legion TD 2 runs a 1.67:1 spread, Element TD 2 runs 4:1, Warcraft 3's shipped constants run 40:1. It sets how
  much a matchup is decided before the wave starts, it is another free decision made on paper, and it is
  cheaper to set now than to retune later.
- **Does a shareable browser replay viewer matter enough to move the simulation to Rust?** Flagged in Part III.
  Current assumption: no — C# throughout.
