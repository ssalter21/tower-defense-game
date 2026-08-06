# State of the project

**A point-in-time report** · 6 August 2026 · written against `main` at `d31ed66`

> Not a standing document and not indexed in [the docs README](README.md). It reads the repository as it is
> today, sets it against [The Vision](vision.md), and names the levers that decide how fast this gets to a
> thing you can play and tune for fun.

---

## Bottom line

**You have built an exceptionally good spine and no game.** The simulation, the record format, the
determinism guarantees, the headless CLI and the Unity view are all done to a standard most shipped games
never reach. What none of it does is take an input from a player. Today's build is a *replay viewer* — it
plays one recorded match of one fixed defense against one fixed wave, and `PlaybackControls` says so in its
own class comment: *"Nothing here can reach the simulation."*

**The gap between what exists and something playable is one structural change, not many.** Today a match is a
pure function of `(map, defense, wave, seed)`. To be a game it has to be a pure function of
`(map, seed, a recorded sequence of player decisions)`. That is the same shape — inputs in, deterministic
result out — and the record format already stores inputs rather than outputs, so nothing about the
architecture resists it. Everything else on the way to fun is content and numbers, and the numbers live in
integer text files that need no engine to change.

**The main risk to "playable quickly" is not technical. It is this repository's own standard.** Poison
suites, a six-row determinism matrix, an ADR per decision, comment-convention rewrites — that rigour is
exactly why the spine is trustworthy, and it is also the thing that will make the first ten gameplay
experiments cost ten times what they should. Fun is found by throwing things away. This repo is not currently
set up to throw anything away.

---

## 1. What exists today, verified

Run on this machine, 6 Aug 2026, from a clean worktree of `main`:

```
./tools/run-headless-match.ps1

simulation Committed, SHA-256 8D2541E0E79B6C48
replay bundle format 0, simulation version 1, content 39B848CEFDDCC9CF
seed       20260801
result     12 of 40 leaked, tick 1852 (61 seconds at 30 ticks a second), state CA3F66473C4B975D
landmark  first-overtake            366     25     19
landmark  projectile-orphaned       224     23      0
landmark  first-leak                551     29      0
landmark  last-creep-dies          1840    107      0
```

### The simulation — `sim/`, 30 source files, ~7,100 lines

| Piece | What it is |
|---|---|
| `Fix64`, `Pcg32`, `Hash64` | Fixed-point arithmetic, one seeded RNG stream, rolling state hash computed every tick |
| `Hex`, `HexMap` | Odd-r offset map parsed from text, corridor well-formedness asserted on load, route traced not searched |
| `Match` (1,081 lines) | The tick loop: release, move, acquire, fire, fly, land, die. One authored defense, one authored wave, one seed |
| `TowerCoverage` | Precomputed per-tower coverage intervals over the route |
| `Snapshot` | Everything that moves, pulled on demand — the view's only input |
| `MatchEvents` | Decorative event stream; landmarks are derived from it, never inferred from outside |
| `GhostRecord`, `WaveRecord`, `ReplayBundle` | Hand-rolled binary records, content-addressed ids, per-kind format versions, all-or-nothing reads |

### The guarantees — `sim.tests/`, `sim.poison/`, `.github/workflows/build-gate.yml`

- **~270 xUnit tests** covering arithmetic, dice, hex geometry, content parsing, record round-trips, a 21-case
  negative suite, hostile-locale parsing and the golden trace.
- **An IL scan** over the *committed* assembly and a *fresh build* of the same sources, banning floats,
  ambient nondeterminism, hashed collections, unstable sorts, threading, I/O and conditional diagnostics.
- **A poison suite** — eight deliberate violations, each of which must make its own ban row fire.
- **A six-row determinism matrix** in CI: three operating systems, two processor architectures, two
  differently compiled images, every one required to reproduce the committed trace byte for byte.
- **A calibrated re-simulation budget** — the match is timed as a *ratio* against a reference workload on the
  same machine, so a slow runner no longer flakes it.

### The view — `client/`, Unity 6 URP, 25 view files, ~100 tests

Match scene built from code rather than authored YAML. Hex floor mesh, isometric orthographic camera with six
yaw snaps, object pool keyed by entity id, Playables-driven animation **sampled from the snapshot's distance
rather than a clock**, weapon sockets, projectile views, and a playback bar (pause / speed 1-8x / to the end /
scrub). `LocomotionTests` asserts the animation bet directly: scrub backwards and the legs go backwards.

### The written record

- **Five deep dives** (market, async model, stack, art, variance levers), **the vision**, **32 ADRs**,
  **8 research notes**, **the sit-down checklist**.
- **15 PowerShell entry points** in `tools/`, every one runnable from a cold shell with no editor bridge.

### What the content actually contains

This is the part worth sitting with:

| | Today |
|---|---|
| Unit types | **4** — grunt, runner (differ in *speed and HP only*), bolt (hitscan), mortar (projectile) |
| Maps | 1, a 47-hex corridor one cell wide |
| Waves | 1, forty creeps in three orders |
| Defenses | 1, six towers, fixed in a file |
| Economy | **none** — no cost column, no gold, no income |
| Build phase | **none** |
| Waves per run | **1** |
| Win / loss | none; the match reports a leak count |
| Player input reaching the simulation | **none, by design and by assertion** |

---

## 2. Where this sits against the vision

[The Vision](vision.md) names eight seams. **None of them has been started.** The walking skeleton was the
work *before* seam 1, and it is finished and merged. The only open issue in the repository is
[#56](https://github.com/ssalter21/tower-defense-game/issues/56), a licence confirmation. There is no active
map. You are at a clean stopping point.

| Seam | Status | Distance from today |
|---|---|---|
| 1 · Match format | **Next, unblocked** — all three research notes landed, nothing charted | This is the whole blocker |
| 2 · Submission barrier | Not started | Not needed for fun |
| 3 · Roster | Not started, structure exists (Part V schema) | Needed for fun; cheap, it is text rows |
| 4 · Balance harness | Not started | **Cheaper than the vision assumes — see lever 6** |
| 5 · Service | Not started | Not needed for fun |
| 6 · Social layer | Not started | Not needed for fun |
| 7 · Interface | Not started | Partly needed; the two-board version is not |
| 8 · Presentation | Free-tier placeholder art in place | Not needed for fun; needs your decisions, not mine |

### What the vision asks for that the sim cannot express yet

| Vision claim | Sim today |
|---|---|
| Build phases between waves, nothing during one | No build phase; a match is one wave |
| One purse — every coin on a tower is a coin not on an attacker | No currency at all |
| Both boards live every round | One board, and it is not interactive |
| You choose the order creeps come out in | ✅ `wave.txt` is already an ordered `(tick, type, count)` list, and the spawn interval, speed spread and tiebreak that make ordering *observable* are all built and tested |
| Combinatorial build space | 2 tower types |
| Creep roster with classes and roles | 2 creep types differing in two numbers |
| Nothing persists between runs | ✅ trivially true, nothing persists at all |
| Balance is computed | ✅ the machine to compute it exists; the harness on top does not |

**The one row with a tick in the "player decision" column is the ordered wave** — and that is not an accident.
The skeleton deliberately tuned the wave so a fast group overtakes a slow one, and wrote down the two
preconditions it found the hard way: *ordering is unobservable when units share a speed, and unobservable
again when a count spawns as a pile.* That is the attacking half's core mechanic already de-risked.

---

## 3. The levers, ranked by how much they move "time to fun"

### Lever 1 — Give yourself permission to build gameplay at a lower standard than the spine

**The highest-leverage change here is a process one.** The spine deserves the poison suite and the six-row
matrix; a first attempt at an economy does not. Every gameplay experiment that has to arrive with ADRs, a
regenerated golden trace and a green determinism matrix is an experiment you will run once instead of ten
times — and finding fun is a numbers game.

Concretely: a scratch branch (or a `sim.sandbox/` project) where content, costs and rules can be hacked with
no gate, no ADR and no committed trace, and where the only rule is *nothing merges to `main` until it has
earned the spine's standard.* The determinism guarantee is not at risk from this — it is enforced by the IL
scan and the matrix at the merge boundary, which is exactly where it belongs.

This is the lever that multiplies every other one.

### Lever 2 — The economy is the smallest change that turns this into a game

A purse, a `cost` column in `units.txt`, and income per wave. That is it. Today there is no decision anywhere
in the match: the defense is a file and the wave is a file. Add one currency and *every* number in
`units.txt` becomes a design lever, because cost-per-effect is what makes a unit good or bad.

It is small in the simulation — an integer that goes down when you buy and up between waves — and it is the
precondition for literally every gameplay question you want to ask.

**Watch the one flagged trap:** the vision itself carries a ⚠️ on one purse — under a single currency a coin
spent attacking is *gone*, so attacking is a pure tempo loss and at equilibrium dominated. The sending
research says there are three available answers and no fourth. The cheapest is an **outcome transfer**:
breaking a defense pays you, leaking pays them. Decide this the day you add the purse, not later.

### Lever 3 — A run is N waves with build phases between, and that changes the record format

`Match` today ends when the wave is done. A run is ~10–20 of those with a build phase between each. This is
the real structural work, and it lands in three places:

1. `Match` gains a lifecycle — wave, resolve, open build phase, accept commands, next wave.
2. The record gains a **command stream** — `(wave index, decision)` pairs, which is what a build phase *is*
   from the record's point of view. `GhostRecord` stores a finished tower layout; it now needs to store the
   sequence that produced it.
3. `simcli` gains a mode that plays a command file rather than a bundle.

**Do this in the CLI before Unity.** A hand-written command file — *place bolt at 4,2; send 6 grunts then 4
runners* — is both the fastest possible playtest harness and the exact record format extension you need
anyway. You get to feel the loop's shape before spending a single hour on mouse input.

### Lever 4 — Open the input seam deliberately, once, through commands

This is the one place worth being slow. The project has ADRs and *tests* asserting that no input reaches the
simulation, and that discipline is why determinism holds. Opening it means adding a **command channel that is
recorded**, not letting the view mutate match state.

The shape that preserves everything: the view emits a command, the command goes into the record, the record
is what the match consumes. A replay then re-consumes the same commands and produces the same result — which
means **every playtest you run is also a determinism test**, exactly as scrubbing is today. Get this right
once and the async multiplayer in the vision is nearly free later, because a submitted turn *is* a command
batch. Get it wrong and you contaminate the one guarantee the whole design rests on.

### Lever 5 — Play solo first; the multiplayer is delivery, not gameplay

The vision is emphatic that multiplayer is not a garnish, and it does not have to be to be *deferred*. Async
round-robin is the same loop at a different latency — the vision says so itself — so the loop can be found,
tuned and enjoyed at zero latency, against defenses read from a folder of `.replay` files that you authored
or that a previous run produced.

**This is the largest scope deletion available to you.** No accounts, no server, no matchmaking, no rating,
no anti-cheat re-simulation, and no two-board interface — which the vision calls the hardest unsolved problem
in the design. None of it is needed to answer *is this fun?*

### Lever 6 — Build the balance harness early; it is far cheaper than the vision assumes

The vision describes overnight sweeps. **It is not an overnight job.** The 62-second match re-simulates in
**~2.75 ms** on your laptop — that is roughly 360 matches per second per core, so a sweep of ten thousand
matchups is under a minute on one thread.

That changes the harness from a "seam 4, after 1 and 3" project into a `simcli` mode plus a CSV, and it
changes when it is worth building: **before** the roster is big, not after. A red cell that names a mispriced
unit while there are only eight units is a tool that makes every subsequent unit cheaper to add. Pull it
forward.

### Lever 7 — Iterate numbers entirely on the command line

`units.txt`, `wave.txt` and `defense.txt` are integer text with comments, hashed by parsed value rather than
by bytes. Adding a tower is one line. Rebalancing is editing integers and re-running a script that needs no
engine, no licence and no editor.

This is a genuine advantage most projects do not have, and it means **the fun-finding loop for numbers never
touches Unity**. Reserve Unity time for the things that only Unity answers: does it read, does it feel, does
it look like anything.

### Lever 8 — Expand the roster along the axes that already exist before inventing new ones

`UnitType` already carries eleven integer levers: HP, speed, range, cooldown, windup, backswing, damage min,
damage max, delivery, projectile flight, dying ticks. Four unit types use maybe half of that space. **You can
build eight to twelve genuinely different units without adding a single field** — a slow high-damage sieger
is a cooldown and a damage range; a swarm is HP and speed; a sniper is range and windup.

That matters because adding a *field* costs a format version, a hash-layout bump and a retired ghost pool.
Adding a *row* costs a line. Exhaust the rows before you touch the schema.

---

## 4. What the research already decided for you, so you do not re-open it

Three notes landed and each ends with ranked directions. **The cheapest coherent starting point is already
identified in them, and it costs almost nothing:**

- **The attacking half** →  *"Universal roster, and the wave **is** the order and the clock"* — ranked first
  explicitly because *"its cost is approximately zero — the record format, the ordered wave, the tie-break
  rule and the overtake landmark all exist and are tested"*, and because it answers the open gate — **is
  composing a wave against a fixed, non-reacting defense fun?** — with the fewest confounds. That is your
  question, and that is the direction that asks it cleanly.
- **Build depth** → Direction A, the generative roster (*n* ingredients, one unit per subset). Powerful, and
  it commits you to a **content bill of 25–56 authored units** that the rule decides rather than your taste.
  ⚠️ **Do not sign that bill before the core loop is proven fun.** Its own note says A and B compose and B is
  the cheapest thing to add to A later — so nothing is lost by starting flat and generative-ising afterwards.
- **Towers or squads** → squads are static, the record cost is zero, and delivery is *a column in
  `units.txt`* — so this stays reversible per unit type from a data file. Nothing to decide now.

One decision the sending note says must be made **regardless of direction**: *is there a shared public
baseline wave?* Without one, the player composes the whole wave and there is no constant for an opponent — or
a newcomer — to read a send against. It is cheap insurance and it is orthogonal to everything else.

---

## 5. The fastest credible path to something you can tune for fun

Sequenced, with the cost honestly named. Steps 1–4 need **no Unity at all**.

| # | Step | What it delivers | Rough size |
|---|---|---|---|
| 1 | **Cost column + one purse + income between waves** | Every existing number becomes a design lever | Small |
| 2 | **Run structure: N waves, build phase between, command stream in the record** | The loop exists; a hand-written command file is a playtest | Medium — the real structural work |
| 3 | **Roster to ~10 units using existing `UnitType` fields only** | Enough vocabulary for a decision to be interesting | Small, and it is text |
| 4 | **Sweep harness in `simcli` — every unit against every defense, win rate and cost-efficiency to CSV** | Balance becomes a computation, while the roster is still small enough to enumerate | Small, given 2.75 ms a match |
| 5 | **Unity: build-phase interaction — click a hex, place a tower, compose the next wave, hit go** | The first thing that is actually *playable* rather than readable | Medium; the grid, camera and pooling already exist |
| 6 | **Opponent defenses read from a folder of ghost files** | The full loop at zero latency, with no service | Small — `GhostRecord` already round-trips |
| 7 | *Then* revisit generative depth, the two-board interface, and the service | | |

**The single most important thing about this sequence is that step 5 is fifth.** You can answer most of the
fun question — is the economy tense, is composing a wave interesting, does ordering matter, is the roster
varied — from a command line and a CSV, at a fraction of the wall-clock cost of doing it through an engine.

---

## 6. What to defer, explicitly

- **The two-board interface.** The vision's own words: *the hardest unsolved presentation problem in this
  design.* It is a presentation of a loop you have not yet proven is fun.
- **The service, accounts, rating, pool, anti-cheat re-simulation.** Zero contribution to fun-finding.
- **Generative combinatorial depth (Direction A).** Adopt after the flat roster is proven, not before. Its
  own note says B composes onto A later at no cost.
- **Art beyond the free-tier placeholders.** Nothing about balance or fun needs it, and the choices there are
  yours to make, not something to resolve while heads-down on mechanics.
- **Co-operative play.** The vision already parks it.

## 7. Risks worth naming

- **The rigour tax is real and it compounds.** Every content change today regenerates a golden trace and a
  landmark table; every rule change bumps a simulation version. That is correct for a shipped spine and
  actively hostile to rapid iteration. Lever 1 is the mitigation and it is worth doing first.
- **The input seam is the one irreversible-feeling decision on this path.** Command-recorded, not
  view-mutated. Everything downstream — replays, async turns, server re-simulation — depends on it.
- **`Match` is 1,081 lines and about to grow a lifecycle.** Build phases, an economy and a command stream all
  land in the same class. Worth a seam before it becomes the thing nobody wants to touch.
- **One purse's dominated-attack problem is a design bug waiting in the vision, not in the code.** Decide the
  payback mechanism when you add the purse.
- **The vision is much larger than the fun question.** Eight seams, a service, a social layer. Nothing in
  this report argues against any of it — only that the order in the vision is a *dependency* order, not a
  *learning* order, and the learning order puts the cheap fun-finding first.

## 8. Decisions that have to be made before step 1

Small, on paper, and blocking:

1. **How does an attack purchase pay back under one purse?** (Outcome transfer is the cheapest of the three.)
2. **Is there a shared public baseline wave?**
3. **What is a run?** How many waves, does it end in a loss condition or simply end.
4. **How wide is the damage-type matrix, and what is the armour formula?** Carried since Part V. The squad
   research already constrains it: **flat-subtraction armour punishes many-small-hits quadratically**, so rule
   it out, and lean narrow.

None of the four needs an engine, a server or an asset. All four are an afternoon with a notebook, and all
four get more expensive the more content exists when you answer them.
