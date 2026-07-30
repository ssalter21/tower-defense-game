# Design documents

Three deep dives, in the order they were written. Each one answers a question raised by the one before it, so
they read best in sequence.

| # | Document | Question it answers | Verdict |
|---|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? | Viable, but **not multiplayer-first**. Synchronous PvP TD tops out around 830 concurrent players. |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix that, and what does it cost? | The model is proven and fits TD unusually well. **Determinism is the whole build risk.** |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? | Unity 6 client, plain C# integer sim library, no realtime networking. |

## The thread running through all three

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

## Status

No code yet. The build order is in [Part II, section 6](async-ghost-round-robin.md#6-build-order--how-to-de-risk-this-in-order);
step 1 is a determinism harness, and nothing has been started.

The open question flagged in Part III is whether a shareable browser replay viewer matters enough to move the
simulation to Rust. Current assumption: no — C# throughout.
