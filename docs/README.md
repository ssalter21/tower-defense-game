# Design documents

## Start here

**[The Vision](vision.md)** is the standing document — what this game is, what it is not, and the eight seams
that build it. It fixes the destination the five deep dives below were written to find, and **where it
disagrees with any of them, it is current**; its [§9](vision.md#9-what-this-overturns) lists every place it
replaces one.

In one line: *a technically excellent tower defense, built for the pleasure of building it, whose multiplayer
is real — and every mode of it is the same machine at a different latency.*

Each of its eight seams is the subject of its own wayfinder map, planned and worked separately.

## The five deep dives

Written before the vision, in the order they were written. Each one answers a question raised by the one before
it, so they read best in sequence.

> **These five documents are an input to the vision, not the current state of anything.** Several of their
> claims have been overturned — by [The Vision](vision.md), and before it by the walking-skeleton map,
> [Walking skeleton: scope a small vertical that teaches Unity and proves the architecture](https://github.com/ssalter21/tower-defense-game/issues/2)
> (see [What has been settled since](#what-has-been-settled-since)). Where any of them disagrees with the
> vision, the vision is current.

| # | Document | Question it answers | Verdict |
|---|---|---|---|
| I | [Market Report & Viability Read](market-report.md) | Is a multiplayer tower defense worth building in 2026? | Viable, but **not multiplayer-first**. Synchronous PvP TD tops out around 830 concurrent players. |
| II | [Async Ghost Round-Robin](async-ghost-round-robin.md) | Does asynchronous ghost PvP fix that, and what does it cost? | The model is proven and fits TD unusually well. **Determinism is the whole build risk.** |
| III | [Technology Stack Assessment](tech-stack-assessment.md) | What do we build it with? | Unity 6 client, plain C# integer sim library, no realtime networking. |
| IV | [Art Direction & Asset Pack Strategy](art-direction-and-assets.md) | What does it look like, and what do we buy to get there fast? | Stylized 3D, not pixel art. Synty ships no animations — that inverts the obvious pick. Its **"KayKit Complete, $150" verdict is reactivated** by [The Vision §6](vision.md#6-what-it-looks-like); it had only been *paused* while the walking-skeleton effort ran free-tier only at $0 under [#17](https://github.com/ssalter21/tower-defense-game/issues/17), never overturned. The hex-kit mispricing was in **§5**, not §13, and is **now corrected in the document** by [#27](https://github.com/ssalter21/tower-defense-game/issues/27), along with §5's recommendation of a static-mesh pack for towers it requires to be animated. |
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

## Research notes

Narrower investigations in [`research/`](research/). Each resolves one ticket on the map and cites primary sources;
they are evidence, not design documents, and where the map has since moved past one the note says so in place.

| Note | Question it answers | Finding |
|---|---|---|
| [Claude Code inside a Unity 6 project](research/unity-agent-workflow.md) | What can a terminal-only agent do inside Unity, and what must be done by hand? | Unity is agent-hostile but **bounded and front-loaded** — roughly two hours of unavoidable mouse time, almost all of it installation. The working model is *agent writes editor C#, then triggers it*, never *agent edits scene files*. The first-party MCP bridge is **paywalled at $10/mo**; the free paths are `-batchmode -executeMethod` and the MIT community bridge. Resolves [#4](https://github.com/ssalter21/tower-defense-game/issues/4). |
| [Unity 6 project-creation settings](research/unity-project-settings.md) | Which settings are expensive to change later? | **Unity 6.3 LTS · Universal 3D (URP) · Linear · Input System · .NET Standard/Mono.** Only two dialog fields are expensive to get wrong — Editor version and template. URP stopped being a judgement call: Built-In is deprecated from 6.5 and HDRP is in maintenance. Resolves [#6](https://github.com/ssalter21/tower-defense-game/issues/6). |
| [How the Unity project consumes the sim library](research/unity-sim-library-integration.md) | Precompiled DLL or sources inside Unity? | **Build outside Unity with `dotnet build`; consume the compiled `netstandard2.1` DLL as a managed plug-in.** Decisive reason: Unity compiles with its own bundled Roslyn, so any source-in-Unity layout ships a *different IL image* than the determinism run hashed. Also found **two bugs in Part III's banned-API enforcement**. ⚠️ Carries [amendments](research/unity-sim-library-integration.md#amendments) — four supporting arguments were superseded by [#15](https://github.com/ssalter21/tower-defense-game/issues/15); the headline recommendation stands. Resolves [#5](https://github.com/ssalter21/tower-defense-game/issues/5). |

**One caveat all three carry.** `unity.com` returns 403 to automated fetching, so every licence and pricing claim in
them was read via a browser user-agent as extracted text. A human should confirm those in a real browser before
relying on them commercially.

## Status

**The walking skeleton is built and landed on `main`.** A deterministic integer simulation (`sim/`), the ghost
record format, a headless CLI (`simcli/`), and a Unity 6 URP view that scrubs a recorded match from snapshots
— with a passed twelve-row [sit-down](sit-down.md). Its map,
[#2](https://github.com/ssalter21/tower-defense-game/issues/2), is closed at 19 of 19.

Part II §6's build order is no longer the plan. It sequenced a *commercial* game, and steps 4 through 7 —
ship it, turn on submission, add rating, then friend lobbies last — do not survive
[the destination the vision fixes](vision.md#1-the-destination). What replaces it is
[the eight seams](vision.md#8-the-seams), each planned as its own wayfinder map. **The match format is next**,
because everything else is downstream of it.

## What has been settled since

Questions these documents raise that the map has since closed. Listed so nothing here is read as still open; the
detail lives in the linked ticket, not here.

- **Mazing or preset path? — settled: no mazing, ever.** The playfield is a **hex grid** with a corridor exactly one
  hex wide that never branches, so route derivation is a trace rather than a search and *no unit ever chooses its
  path* — which keeps pathfinding out of the sim library permanently. Part V's six mazing-dependent levers
  (`Path policy`, `Repath trigger`, `blocksPath`, the maze/gun resource split, geometry-driven stats, route choice)
  are dead weight under this answer. Closed by
  [Top-down grid or side-on lane?](https://github.com/ssalter21/tower-defense-game/issues/3).
- **Top-down grid or side-on lane? — settled: neither as posed.** The question welded a sticky decision (playfield
  shape) to a cheap one (camera), and they were answered separately. Playfield is the hex corridor above; camera is a
  **fixed isometric orthographic orbit with 60° yaw snapping**, view-only and never sim input. This **overturns Part
  III's "fixed camera, no free rotation"** and makes *no billboards, no flat cards, no painted-on shadows* a mandatory
  art rule. Track A (stylized low-poly 3D) stands. Closed by
  [the same ticket](https://github.com/ssalter21/tower-defense-game/issues/3).

And two more the vision has since closed:

- **Can the developer rig and animate in Blender? — closed by irrelevance.** Part IV's recommendation flipped
  from KayKit to Synty on this single fact, and the vision buys KayKit, which ships its own animations. The
  question only ever mattered for Synty. [The Vision §6](vision.md#6-what-it-looks-like).
- **Is this a commercial product? — settled: no.** Part I's viability analysis becomes background reading
  rather than a constraint, and Part II §6's build order goes with it.
  [The Vision §1](vision.md#1-the-destination).

Still open, and unchanged:

- **How wide should the damage-type matrix be?** Flagged in [Part V, §4.1](variance-levers-and-unit-schema.md#41-the-scalar-layer--three-shapes-pick-exactly-one).
  Legion TD 2 runs a 1.67:1 spread, Element TD 2 runs 4:1, Warcraft 3's shipped constants run 40:1. It sets how
  much a matchup is decided before the wave starts, it is another free decision made on paper, and it is
  cheaper to set now than to retune later. Carried into
  [the vision's fog](vision.md#10-not-yet-specified) — it belongs to the roster or balance seam.
- **Does a shareable browser replay viewer matter enough to move the simulation to Rust?** Flagged in Part III.
  Current assumption: no — C# throughout.
