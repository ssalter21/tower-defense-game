# Documents

An index. **Every verdict lives in the document that holds it** — this file says only what each one is for, so
there is nowhere for a claim to sit and go quietly out of date.

## Start here

**[The Vision](vision.md)** is the standing document — what this game is, what it is not, and the order it gets
built in. Where anything else in this directory disagrees with it, it is current.

In one line: *a technically excellent tower defense, built for the pleasure of building it, whose multiplayer
is real — and every mode of it is the same machine at a different latency.*

| | |
|---|---|
| [The Vision](vision.md) | The destination, the pillars, the open questions, and [the build order](vision.md#8-the-build-order) |
| [The decision log](decision-log.md) | Every time the vision changed its own mind, and why |
| [The sit-down](sit-down.md) | Twelve things to look at in the build, once, each naming the exact tick |
| [`adr/`](adr/) | Why the code is shaped the way it is — 33 records. Source comments say *what*; these say *why* |
| [`research/`](research/) | Evidence notes. Each answers one question and cites primary sources |
| [`archive/`](archive/) | The five deep dives the vision was built on. Superseded; kept as the reading |
| [`frames/`](frames/) | Rendered match frames — documentation, not an oracle |

## Research notes

In [`research/`](research/). They are **evidence, not design documents**: each resolves one question, cites
primary sources, and decides nothing. Where the vision has since moved past one, the note says so in a banner
at its top rather than being rewritten.

**Design research**, commissioned against the vision's depth direction and open questions:

| Note | The question it answers |
|---|---|
| [Build depth in tower defense](research/build-depth-in-tower-defense.md) | How do TD games produce extreme, combinatorial build depth, and which mechanisms survive this project's constraints? |
| [The attacking half](research/attack-composition-and-sending.md) | How is sending made deep — and has anyone gated the attacking options on the player's defensive build? |
| [Creep wave variety and creep upgrade systems](research/creep-wave-variety-and-creep-upgrade-systems.md) | Which games went deep on creep variety, and does anything let you upgrade creeps the way you upgrade towers? |
| [Element TD's ancestry](research/element-td-ancestry-and-wc3-tower-mechanics.md) | Which WC3 map inspired Element TD, and what were the original's tower mechanics? |
| [Towers, or placed squads?](research/towers-versus-placed-squads.md) | Does the defending side have to be towers, or could placements be flanking walls with archer squads? |
| [Why tower defense is fun, and where the skill is](research/fun-and-skill-expression.html) *(HTML)* | Why is the genre fun, and where does its skill expression actually live? |
| [Making the plan the game](research/planning-phase-and-simulated-stats.html) *(HTML)* | How do you make a build phase carry a whole game, and what can a 2.75 ms sim be spent on as design material? |
| [Generated maps, and how often they turn over](research/generated-maps-and-rotation.html) *(HTML)* | How do you generate maps worth playing, seed them cheaply, and pick a rotation cadence? |

**Simulation research**, measured in this repository rather than commissioned:

| Note | The question it answers |
|---|---|
| [Why the golden trace moved when the balance did not](research/the-tenfold-rescale-and-the-dice.md) | Multiplying every damage and health number by ten moved every generated artefact. Is that the rescale working, or a desync? |

**Build research**, on the tools rather than the game:

| Note | The question it answers |
|---|---|
| [Claude Code inside a Unity 6 project](research/unity-agent-workflow.md) | What can a terminal-only agent do inside Unity, and what must be done by hand? |
| [Unity 6 project-creation settings](research/unity-project-settings.md) | Which settings are expensive to change later? |
| [How the Unity project consumes the sim library](research/unity-sim-library-integration.md) | Precompiled DLL, or sources inside Unity? Carries [amendments](research/unity-sim-library-integration.md#amendments) |
| [How long Unity takes to notice a rebuilt plug-in](research/unity-hot-reload-timing.md) | Does an agent working while nobody is at the keyboard get stuck waiting for a reimport? |

> **One caveat the three Unity notes carry.** `unity.com` returns 403 to automated fetching, so every licence
> and pricing claim in them was read via a browser user-agent as extracted text. A human should confirm those
> in a real browser before relying on them commercially.

## The archive

The five deep dives that were written before the vision, in [`archive/`](archive/). They read best in sequence —
each answers a question raised by the one before it — and each carries a banner saying what survived it.

| # | Document | Question it answered |
|---|---|---|
| I | [Market Report & Viability Read](archive/market-report.md) | Is a multiplayer tower defense worth building in 2026? |
| II | [Async Ghost Round-Robin](archive/async-ghost-round-robin.md) | Does asynchronous ghost PvP fix the population ceiling, and what does it cost? |
| III | [Technology Stack Assessment](archive/tech-stack-assessment.md) | What do we build it with? |
| IV | [Art Direction & Asset Pack Strategy](archive/art-direction-and-assets.md) | What does it look like, and what do we buy to get there fast? |
| V | [Tower & Creep Variance Levers](archive/variance-levers-and-unit-schema.md) | What can a tower or a creep differ by, and what data structure holds all of it? |

**They are an input to the vision, not the current state of anything.** Part I's question is no longer being
asked; Part II's conclusion survived with a different reason; Parts III, IV and V are still being built
against. [The Vision §9](vision.md#9-what-this-overturns) is the row-by-row account, and it is the only place
that account exists.

## The thread running through the five

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
