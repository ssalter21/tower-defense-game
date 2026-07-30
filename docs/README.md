# Design documents

Four deep dives, in the order they were written. Each one answers a question raised by the one before it, so
they read best in sequence.

| # | Document | Question it answers | Verdict |
|---|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? | Viable, but **not multiplayer-first**. Synchronous PvP TD tops out around 830 concurrent players. |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix that, and what does it cost? | The model is proven and fits TD unusually well. **Determinism is the whole build risk.** |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? | Unity 6 client, plain C# integer sim library, no realtime networking. |
| IV | [Art Direction & Asset Pack Strategy](art-direction-and-assets.md) | What does it look like, and what do we buy to get there fast? | Stylized 3D, not pixel art. **KayKit Complete, $150.** Synty ships no animations — that inverts the obvious pick. |

## The thread running through all four

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
- **Does a shareable browser replay viewer matter enough to move the simulation to Rust?** Flagged in Part III.
  Current assumption: no — C# throughout.
