# The build order

**The sequence, and the nine seams.** Ordered by what is cheapest to *learn*, not by what depends on what: a
dependency order tells you what must exist before what, not what to find out first. The one thing this project
has never tested is whether any of it is fun, and that question can invalidate everything downstream of it.

The design this builds is [the vision](vision.md). Reversals are in [the decision log](decision-log.md).

## The sequence

Steps 1–4 need no engine, no licence and no editor; they run from a shell. **Steps 1–4 are built and step 5
is next.** [The first played run](decision-log.md#13-august-2026--the-first-run-played-by-a-person) found the
roster too shallow for the build phase to be a decision worth making, and for a day that put step 3 back in
front of step 5; [the same evening reversed it](decision-log.md#13-august-2026-later--the-gates-come-out-and-the-client-comes-before-the-roster).
The finding stands and the sequence does not follow from it — see
[step 3 is not finished](#step-3-is-not-finished-and-a-played-run-is-how-that-was-found) below.

| # | Step | What it delivers | Size |
|---|---|---|---|
| 1 | **Cost column, one purse, wave slots, income between waves, and the damage model** | Every integer in `content/units.txt` becomes a design lever. The ×10 scale and the attack/armour columns land here too — cheap now, a content migration and a retired ghost pool after step 6. [§3](vision.md#how-a-shot-resolves) | Small |
| 2 | **A run is N waves with a build phase between, recorded as a command stream** | `Match` gains a lifecycle; the record gains `(wave index, decision)` pairs, which is what a build phase *is* to a record; `simcli` gains a mode that plays a command file | Medium — the real structural work |
| 3 | **Roster to about ten units, using only the levers `UnitType` already has** | Enough vocabulary for a decision to be interesting | Small — it is text rows |
| 4 | **The sweep harness: every unit against every defense, win rate and cost-efficiency to a CSV** | Balance becomes a computation while the roster is still small enough to enumerate rather than sample | Small |
| 5 | **Build-phase interaction in the client: click a hex, place, compose the next wave, commit** | The first thing that is *playable* rather than readable | Medium |
| 6 | **Opponent defenses read from a folder of ghost records** | The whole loop at zero latency, with no service in it | Small — `GhostRecord` already round-trips |
| 7 | **Then** the generative depth, the two-board interface, and the service | | |

**Step 5 is fifth deliberately.** Is the economy tense, is composing a wave interesting, does send order matter,
is the roster varied — these are the questions worth asking before an engine effort, at a fraction of the
wall-clock cost of asking through one. The engine answers whether it *reads* and whether it *feels*, which are
worth nothing if the cheap answers were no.

**Two of the four turned out not to be answerable from a shell, and that is a reversal.** The sentence here
used to claim all four were. A run played at a prompt on 13 August 2026 answered *is the economy tense* and
*is the roster varied*, and could not answer *is composing a wave interesting* or *does send order matter* —
because those are judged off range indicators and replays, which a terminal will never carry and which are not
worth building there. **The test that survives is: is it a picture, or a number?** Numbers the simulation
already computes are answerable at a prompt; pictures wait for the client. The full reasoning is in
[the decision log](decision-log.md#13-august-2026--the-first-run-played-by-a-person).

### Step 3 is not finished, and a played run is how that was found

Step 3 delivered *enough vocabulary for a decision to be interesting*, and playing one showed it is not there
yet: **composing a wave is not interesting enough, and the build phase has not got the depth to make it so.**
What it wants is plenty of money to spend and options worth spending it on, and neither is judgeable against
six walkers and four towers — three of which are equivalent on a one-hex corridor.

**It does not follow that seam 3 is built next, and for one day it was recorded as though it did.** Step 5
carries it instead: a client that can be clicked and played is what the look and feel is judged from, and
that judgement is wanted before the roster is deepened rather than after. Seam 3 is parked with its shape
changed — depth comes from **upgrading the creeps that exist**, in stats and speed, rather than from
authoring new unit types.

What was going to help does not, and is **deleted rather than deferred**: the forced pick, the round menu,
the special rounds and the per-wave type limit all come out of the played game, and go back when the roster
is deep enough for a gate to be gating something worth having. **The type limit is the first of the four to
be redesigned rather than merely parked** — [the gate rounds](vision.md#three-gates-at-waves-3-6-and-9) make
it a public capacity schedule with a second dimension in it, and the order is unchanged: the roster gets its
depth first and the schedule is fitted to it afterwards. Gating a shallow roster holds back early
testing and buys nothing, and a mechanic carried switched-off through a client build is a tax on every step
of it. What stays is the **upgrade prerequisite**: a unit that is some edge's target cannot be placed
directly, so an Archer must stand before a Ranger can.

## Three obligations the sequence carries

- **Step 1 builds the whole economy, not the easy half.** One purse, a flat base plus a bonus proportional to
  the leak cost a wave dealt, 10% interest on the bank, and a purchase that is permanent. The trap is shipping
  the cost column and the base income and calling it done: the base pays a wave for happening, and the bonus —
  which is what stops attacking being dominated — is what gets dropped. [§3](vision.md#one-purse)
- **Step 2 opens the input seam, and is the one place worth being slow.** ADRs and tests assert that no input
  reaches the simulation, and that discipline is why determinism holds. The shape that preserves it: **the view
  emits a command, the command goes into the record, the record is what the match consumes.** Done that way
  every playtest is also a determinism test and [seam 2](#2--the-submission-barrier) comes nearly free, because
  a submitted turn *is* a command batch.

  **Run length N and field size K are parameters of the lifecycle, not constants.** Ten and ten are the
  answers, and both are expected to move.
- **Step 3 exhausts rows before it touches the schema.** `UnitType` already carries eleven integer levers — HP,
  speed, range, cooldown, windup, backswing, damage min, damage max, delivery, projectile flight, dying ticks.
  A slow sieger is a cooldown and a damage range; a swarm is HP and speed; a sniper is range and windup. Adding
  a **row** costs a line. Adding a **field** costs a format version, a hash-layout bump and a retired ghost
  pool.

## What the sequence deliberately does not do

- **It does not decide the match format in full before building anything.** Steps 1–3 are the smallest form of
  seam 1's ruleset that can be played, chosen so that being wrong costs a text file rather than an effort.
- **It does not commit to the generative roster.**
  [The depth research](research/build-depth-in-tower-defense.md) ranks it first and the reasoning holds, but it
  signs a content bill of **25 to 56 authored units** decided by rule rather than taste. The note says the
  authored-pool direction composes onto the generative one later at no cost, so nothing is lost by starting
  flat.
- **It does not build the multiplayer.** Async round-robin is the same loop at a different latency
  ([§2](vision.md#2-the-loop--one-machine-at-three-latencies)), so the loop can be found and tuned at zero
  latency against defenses read from a folder. This is the largest scope deletion available: it removes
  accounts, matchmaking, rating, anti-cheat re-simulation and the two-board interface from the critical path to
  *is this fun*. **It defers them; it does not repeal them.**

## The nine seams

Each is the subject of its own wayfinder map — its own destination, decision tickets and sessions.

| # | Seam | The destination | Where it lands |
|---|---|---|---|
| 1 | **The match format** | A decided-in-full ruleset for a single match, including the shape of its depth | Steps 1–3 are its first half, taken as experiments |
| 2 | **The submission barrier** | One mode architecture proven to serve all three latencies | After step 6, half-paid by step 2 |
| 3 | **The roster** | What towers and attacking units exist, and what they vary by | Step 3 flat; deepened after step 5, revisited at step 7 |
| 4 | **The balance harness** | A tool that names what is mispriced, and the definition of mispriced | Step 4 |
| 5 | **The service** | Accounts, pool, submission, standings, replays, re-simulation | After step 6 |
| 6 | **The social layer** | What makes an absent opponent feel like a person | After seam 5 |
| 7 | **The interface** | Reading twenty boards, an economy and a build menu at once | Step 5 is its single-board half |
| 8 | **The presentation** | The art pipeline, and what makes it look composed | Independent, whenever there is appetite |
| 9 | **The board** | The maze and elevation. No pathfinder, ever; generation and rotation deferred behind the first authored map | Nothing before step 5 needs it; everything after is shaped by it |

### 1 · The match format

What one wave actually is: two boards resolving at once, the build-phase rhythm, what a build phase offers, how
a wave is composed, what a wave is worth and what winning one means. It also owns the **shape of the depth**
from [§3](vision.md#depth-is-the-point) — what the combination system is, whether the creep pool is gated on
your towers, whether send order is a real decision, and whether the defending side is towers at all.

Everything is downstream of this, but not of a *finished* version of it. The cheapest seam to be wrong about
now and the most expensive later, and it needs no server, art or friends to answer. Its unspent inheritance is
the **computed-balance budget** from [§5](vision.md#5-how-it-is-balanced).

**One reconciliation belongs to nobody else.** [*Your defense decides your offense*](vision.md#depth-is-the-point)
wants a private, tower-gated creep pool; [*the offering is public*](vision.md#the-offering-is-public) wants a
shared one. A pool that is both gated and public is a contradiction unless the gate applies to something other
than the offering. Seam 1 chooses which bends. It also owns the tension between the offense
[not entering the placing](vision.md#a-run-is-ten-waves-and-health-is-money) and the attacking half being as
deep as the defending one.

**It owns the capacity schedule's numbers.** [The gate rounds](vision.md#three-gates-at-waves-3-6-and-9)
fix the shape — two more slots and ten more count at each of waves 3, 6 and 9 — and every integer in it is a
ruleset row and a sweep target: the starting width, the two steps, the three rounds, and how many capstone
tokens a gate hands over. The question the sweep is owed is the one a capacity bound has and a purse does not:
**whether a capped slot ever leaves gold with nowhere to go**, and whether the answer is interest or a wasted
round.

**The cheapest coherent starting point is already identified**, and it is what steps 1–3 build:
[the sending research](research/attack-composition-and-sending.md) ranks *universal roster — the wave **is** the
order and the clock* first, because its cost is approximately zero. The ordered wave, the tie-break rule and
the overtake landmark all exist and are tested.

### 2 · The submission barrier

Prove the unification in [§2](vision.md#2-the-loop--one-machine-at-three-latencies) is real: that async
round-robin and the live lobby are the same machine at different latencies, and that the record format
transmits a turn as cleanly as it stores a ghost. Includes stage matching, pool draw, and the hand-authored
floor that keeps every stage populated. If the unification is wrong, this project is building two games.

**The modes differ on *information*, and this seam must establish that the difference lives entirely above the
barrier.** If it reaches into the record or the sim, the unification claim is weaker than §2 says. It also
inherits the pool's index: a draw is `(map, stage)` rather than stage alone — see [seam 9](#9--the-board).

### 3 · The roster

What towers and attacking units exist, how many, and what they vary by, filling the **one unit schema, two
roles** structure with levers as components and the vocabulary versioned separately from the numbers. The units
live in [the roster](roster.md).

It owns the creep roster's **classes and roles** — tanks, damage, support, swarm, specialists — and the
question they raise: whether *one unit schema, two roles* survives an attacking side with genuine internal
structure, or whether roles are a third thing the schema must carry.

Constrained by [§4](vision.md#4-what-persists) — nothing is unlocked, so every unit must be interesting from
the first run — and by [§6](vision.md#6-what-it-looks-like), since a unit whose role cannot be read off its
silhouette fails the accessibility pillar however well it plays.

Three inherited constraints. From [the gate rounds](vision.md#three-gates-at-waves-3-6-and-9): a
counter must be purchasable strictly before the gate that needs it, and the menu half of the schedule signs a
bill of **nine game changer creeps per shape**, tiered across three gates, one opening a genuine counter. From
[the damage model](vision.md#how-a-shot-resolves): every unit carries an attack or armour type from the fixed
three-way cycle, a counter is a `bonusVsTag` integer rather than an immunity, and every damage and health
number is authored at the ×10 scale.

**The menu half of that is deferred, and the bill is not owed yet.** The
[13 August played run](decision-log.md#13-august-2026--the-first-run-played-by-a-person) took the take gate
and the anchor schedule out of the played game until this seam has produced a roster deep enough for a gate to
be gating something worth having, and
[#179](https://github.com/ssalter21/tower-defense-game/issues/179) then deleted them outright — so
`content/schedule.txt` and its twelve placeholder names are gone rather than standing in for content nobody
should be designing yet. Re-authoring them is this seam's work if the depth ever calls for it.
**The depth comes first, and the gate is fitted to it afterwards.** The damage-model constraints are
untouched by that.

**The capacity half came back on 14 August, and it needs no depth to be true** — a schedule of slots and count
caps rations room rather than options, so none of it waits on the roster. What it does need is a roster worth
rationing: two slots against four creep types is the shallow-roster complaint one round further on, which is
why the schedule is design and not a ticket. See
[the entry](decision-log.md#14-august-2026-later-still--the-gates-come-back-with-a-different-job-and-a-capstone-is-paid-for-out-of-a-grant).

**A third bill arrives with it: a capstone per tower line.** A gate hands over a token and the token buys the
top of a line, so three tokens a run wants meaningfully more than three capstones to choose between — and each
of them signed against a pricing rule that
[no longer reaches them](roster.md#what-things-cost).

What is left in their place is the upgrade ladder, which is now the one prerequisite the game has: a unit some
edge of `content/upgrades.txt` points at is refused to `place` and reached by upgrading into. See
[the 13 August entry](decision-log.md#13-august-2026-later-still--the-gates-are-actually-out-and-the-ladder-becomes-the-rule-it-was-an-annotation-to).

### 4 · The balance harness

The tool and the definitions underneath it: what a sweep is, what it measures, what a red cell means, what
*cost-efficient* is in a one-purse economy where a unit competes with a tower and 10% interest at once — and
is paid for once, against every round of the run that is left — and how a verdict gets back into `content/`
without invalidating a pool of stored ghosts.

**It owes step 1 less than it used to.** The bonus was measured against a field until #209 made it a share of
what a wave dealt; a round is still resolved against K opponents drawn from the sweep's canned set, so the
harness is still what a run's damage is dealt *to* — but nothing about the payment waits on step 6 any more.

**It waits for no other seam.** At 2.75 ms a match it is a `simcli` mode and a CSV. If the generative direction
is ever adopted, its documented failure mode — a U-shaped meta where the widest and narrowest builds dominate —
is caught by win rate **binned by number of ingredients taken**, a grouping rather than a tool.

**Pointed at maps it scores them**, which is what makes
[generated rotation](vision.md#the-map-rotates-and-it-is-generated) a filter rather than a hope. **The sweep
must take its map as a parameter, not as a fixed input.** One further column is owed: the both-columns check.
Outcome spread and the ingredient bin are no longer columns — `--per-run` writes a row per run, so both are a
query over a file the sweep already produced rather than an edit to the harness. See
[the 16 August entry](decision-log.md#16-august-2026--the-sweeps-owed-columns-become-queries).

### 5 · The service

The permanent obligation from [§7](vision.md#7-what-runs-it). Accounts and identity, the pool and its stage
index, submission and the barrier, standings and rating, replay storage, and server-side re-simulation as
anti-cheat. Still open: ghost expiry windows, pool re-validation on a content change, and rating under
inactivity.

**Map rotation lands here and makes it harder.** The pool index becomes `(map, stage)`, so every rotation
empties it at every stage. The service also gains the rotation schedule, the generated map archive, and the
obligation to verify a claimed `MapHash`. None of it is new machinery; all of it is new state, and the
[cadence question](open-questions.md) is a question about this seam in a design costume.

### 6 · The social layer

What converts an absent opponent into a person. Named defenses, named opponents, replays you can watch and
send, *your defense held against three of five*, the challenge aimed at one specific friend. Presence is made
of specifics. Not here: browsing, curation and discovery surfaces — opponents are drawn, not shopped for.

**It inherits most of [§9](vision.md#9-the-planning-phase-is-the-game)'s output surface.** Placement against the
aggregate, the two competing rewards, the computed highlight reel and event-derived commentary are presence
mechanisms as much as information ones, and the sim produces them for free. A shared map cycle adds one more:
everyone played the same board this week, which makes a comparison feel like a conversation.

### 7 · The interface

The hardest unsolved problem in the design, and [§6](vision.md#6-what-it-looks-like)'s accessibility pillar
makes it harder. A round resolves against a field of ten, so **what the player watches at all is the largest
open question here** and the vision does not answer it: twenty resolutions cannot each be watched, and a round
that is only ever summarised is a round nobody sees. Two live battles, one economy, a build menu and a readable
account of what the opponent just did — on one screen, legible at a glance, to somebody who has never played
it, with no unlock ramp available to stagger the options. Includes the faction-colour scheme and the
presentation of whatever combination system seam 1 chooses.

**It should probably be split.** [§9](vision.md#9-the-planning-phase-is-the-game) puts the entire planning
surface here — range and elevation overlays, damage previews, the purchase shown against what it replaces, the
lobby scouting view, the post-run dashboard. That is information design for a decision; reading two boards at
once is legibility under simultaneity. **They share a screen and nothing else.** The natural cut is *the
planning surface* and *the watching surface*, worth taking before either is charted.

**It holds the whole post-round surface.** With no forecast anywhere in the planning phase, the retrospective
is where the simulation's value reaches a player: the computed highlight reel that makes a field of 100+
watchable, the stats and histogram that turn a result into a comparison, and the deferred kill heatmap.

**The build phase's arrangement is decided, and it was decided off pictures.** Six layouts were rendered
against the real board with the real prices on them and chosen from by looking — the tool is
[`tools/capture-ui-previews.ps1`](../tools/capture-ui-previews.ps1) and the reasoning for choosing that way is
in [the chrome sheets](chrome/README.md). What was chosen, on 17 August 2026, is
[`docs/chrome/chosen-build-phase.png`](chrome/chosen-build-phase.png):

- **The chrome is as little as it can be** — one line of run state, one commit button, and nothing else that is
  not about somewhere in particular.
- **Towers are chosen at the hex.** Hovering lights a cell and the options open beside it, carrying a portrait,
  a name and a price each. The menu never covers the cell, because the cell is what the choice is being made
  against.
- **The side rail is the wave**, in portraits, in send order. It is the one half of the decision with nowhere
  else to live, and putting it there is what stops anything appearing twice.

**Two consequences, and neither is free.** The rail's portraits make
[the thumbnail seam](#3--the-roster) load-bearing rather than optional — `RosterThumbnails` returns null today
and the mockups borrowed the armed roster's framing, which is a stand-in and not an answer. And the hex menu
comfortably holds three towers against a roster that is meant to get deeper, so **the menu is the surface that
has to survive seam 3**, and it wants a sheet of its own against a padded-out roster before any of this is
built.

### 8 · The presentation

The KayKit purchase and licence confirmation
([#56](https://github.com/ssalter21/tower-defense-game/issues/56)), the atlas-recolour workflow, the `.blend`
editing path, and the lighting, VFX and camera work that makes stock models look composed. Independent of the
others.

**Two things stop it being purely an art seam.** The camera is a *directed* camera: the whole match resolves
before anything is drawn, so the moments worth showing can be chosen by computed salience rather than captured.
And a map with elevation is a lighting and readability problem, since height is load-bearing information — **a
player who cannot tell which tier a placement is on cannot read the range that comes with it**, which makes
elevation legibility a veto rather than a nicety.

### 9 · The board

**The maze and elevation.** None of the other eight is the right home: seam 1 owns the rules of a match, seam
3 the units, seam 4 the measuring tool, seam 5 the storage. Its destination is a board that is verifiable,
deterministic and demonstrably worth playing. **Pathfinding is no longer in it and never will be, and
generation and rotation are deferred behind the first hand-authored map** — [#213](https://github.com/ssalter21/tower-defense-game/issues/213), recorded in
[the decision log](decision-log.md#16-august-2026-later--one-format-version-and-the-map-it-is-for).

- **The maze and elevation** — **nine levels of half a block each**, a level worth **250 milli-hex**, applied
  as the signed difference `baseRange + (towerLevel − targetLevel) × 250` rather than as a bonus for standing
  high. A whole block is two levels and still buys half a hex, which is what it was worth when a level *was* a
  block; the half step in between is new, and it is what lets a slope be graded instead of stepped. Radii read
  as spheres, `hexDistance × 1000 + |levelDifference| × 250 ≤ radius`, so height only ever costs them, and
  a floor guarantees any tower reaches the hexes touching it. **The ceiling moved with the count:** four blocks
  of relief is a two-hex refund where three tiers capped it at one, which is asserted in `ReachTests` rather
  than left to be discovered. How the map preserves the send column
  [ordering](vision.md#depth-is-the-point) needs is still a design question, with a legibility veto on it.
- **The single path** — a map folds but never branches, and the player never alters the route by building, so
  `HexMap`'s existing load-time trace is the whole of it: still one hex wide, still asserted, still no search
  in the hot loop. The rule that a corridor cell may not have three corridor neighbours is **kept**, so two
  legs of a fold are never adjacent and a longer path comes from a bigger board rather than a tighter one.
  **No pathfinder and no line of sight**, both permanently.
- **The record** — elevation is a third coordinate and the **map** carries it; `TowerLayout` does not, because
  a tower stands on a hex and the hex knows its level. A format version and a hash-layout bump for the map,
  while `RecordFormat.TowerBytes` and `GhostRecord` are untouched. Cheap now, expensive later, and the reason
  this seam wants charting before step 5.
- **The first map is hand-authored**, and **generation and rotation wait behind it** — seed-to-map as a pure
  function, a `simcli` mode to invoke it, the sweep-scored archive, and the schedule that draws from it, all
  downstream of one map that is demonstrably good to score candidates against. Surveyed in
  [Generated maps, and how often they turn over](research/generated-maps-and-rotation.html).

**Nothing before step 5 needs it, and everything after step 5 is shaped by it.** Steps 1–4 run against the
corridor that exists, so **the numbers they produce are provisional by construction** — stated where they are
set rather than discovered when they move. It shares one dependency with seam 4: **the sweep must take its map
as a parameter.**
