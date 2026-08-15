# Towers, or placed squads?

**Research note** · 3 August 2026

**Question:** does the defending side have to be *towers*? Could some placements be walls flanking the path with an
archer squad standing on them — all of them shooting, all of them upgradable — for a more RTS-ish look?
**Input:** [The Vision](../vision.md) §3, §6, §10, §11; [Part IV](../archive/art-direction-and-assets.md) §5, §7, §8;
[Part V](../archive/variance-levers-and-unit-schema.md) §3.2, §3.4, §3.9, §4, §5.4; the repository.

> ⚠️ **The premise moved on 6 August 2026, and it reopens one closed branch.**
> This note reasons about ramparts running *alongside a one-hex corridor*, and records "pathfinding stays out
> permanently" as settled. The corridor was withdrawn —
> [the board is a maze again](../vision.md#the-board-is-a-maze), at several
> elevation levels, with an integer pathfinder now owed to `sim/`. The note's central finding is untouched,
> because it is about arithmetic rather than geometry: projectile volume lands on `FlyProjectiles` at
> **O(projectiles × creeps)**, and N identical shooters are behaviourally identical to one shooter firing N
> arrows unless the bodies can die independently. Where the text says pathfinding is permanently out, it is not.

> ⚠️ **The camera moved on 13 August 2026.**
> §4 argues from "a fixed isometric orthographic camera with 60° yaw snapping and six snaps", and the rampart's
> value there is partly that "it survives every yaw snap identically". There are no snaps: the rig is a free
> perspective orbit with unclamped pitch and a dolly ([#195](https://github.com/ssalter21/tower-defense-game/issues/195),
> and the 13 August entry in [the decision log](../decision-log.md)). The finding that a continuous rail separates
> corridor from not-corridor better than a change of floor tile does not depend on the projection and stands; the
> claim that it reads identically from every angle no longer means anything, because there is no longer a fixed
> set of angles.

> **This note decides nothing.** Two branches were closed by the developer while it was being written — walls are
> a placement surface, not a blocker, and squads are static — and both are recorded below rather than argued,
> so nobody re-raises them. What is left is one live question with real cost in it: **projectile volume**.

---

## Bottom line

### The aesthetic half of this question is free and largely already decided. The mechanical half reduces to one number — how many projectiles a squad puts in the air — and that number lands on the simulation's hottest loop, which is `FlyProjectiles` and not, as you would guess, target acquisition.

Four claims.

**One. The look is already the plan.** Part IV §5 decided that towers are *"animated character models that play an
attack animation in place, not inert turret meshes"* — a creature standing on its plot, swinging or casting each
time it fires [[12]](#s12). A flanking rampart with archers on it is that decision, extended. Nothing in it needs
the simulation's permission.

**Two. A wall made of cells is free; a wall made of edges is a record-format change.** Placements today are hexes:
`(column, row)` converted to axial and stored as `i16 q, i16 r` [[3]](#s3)[[7]](#s7), and `TowerCoverage` measures
range in *"whole-hex integer arithmetic — so no division and no rounding rule is involved"* [[2]](#s2). Ground cells
adjacent to the corridor are already addressable and already legal. An edge between two hexes is not: it needs a
sixth-of-a-turn third coordinate, a wider tower record, and a half-integer hex distance the loader is built to
avoid. §3 prices both.

**Three. Static squads are free; the interesting finding is that modelling each archer as its own shooter buys
nothing.** N identical archers on one cell share one `TowerCoverage` interval, so `Acquire` hands them the same
target; they start in the same state and their cooldowns never drift apart. They fire in lockstep forever. **A
squad of N independent shooters is behaviourally identical to one shooter firing N arrows** — unless the bodies can
die independently or are deliberately desynchronised. That is an argument from the committed code, and it decides
§5.

**Four. Projectile volume is the real cost, and it is bigger than it looks because `FlyProjectiles` is
O(projectiles × creeps).** Every projectile resolves its target by a *linear scan of the creep array*, every tick it
is in flight — deliberately, because *"the alternative is a dictionary, whose enumeration order is an implementation
detail"* [[4]](#s4). At a plausible board that term already dominates target acquisition by about three to one at
N = 1, and a squad multiplies it directly. **It costs the ghost record nothing at all**, because projectiles are
simulation output and a record stores inputs [[7]](#s7).

---

## 1. The axis, separated

"Tower versus squad" bundles six questions that can each be answered on their own. Separating them is still the most
useful thing here, even though four are now settled.

| # | Axis | The question | Status |
|---|---|---|---|
| **A** | **Mobility** | Does it hold position or move? | **Closed — static.** See §2.2 |
| **B** | **Cardinality** | One body or several? | **Open in one respect only: what the extra bodies *shoot*.** §5 |
| **C** | **Occupancy** | Does it block the corridor or only shoot into it? | **Closed — only shoots.** Walls flank the path. See §2.1 |
| **D** | **Attrition** | Permanent, or destructible? | **Open, and it is the only reason to model bodies separately.** §5.2 |
| **E** | **Silhouette** | Turret, building, character, or rampart? | Decided in favour of characters (Part IV §5); the rampart question is §3–§4 |
| **F** | **Command** | Who chooses where it stands? | Moot once A is static |

The separation earns its place because the two axes that *sound* expensive — C and A — are now free by decision,
and the one that sounded like an art detail — B — is the one with a measurable simulation cost attached.

---

## 2. Two branches, priced and closed

Recorded so they are not re-opened, and deliberately short.

### 2.1 Walls do not block, so the mazing collision does not arise

The developer's clarification: *"I meant that the walls were on the sides of the path as a placement surface, more
of an aesthetic choice."*

That settles it. The four things "wall" could have meant, and which one is in play:

| Reading | Does the route change? | Does anything choose a path? | Status |
|---|---|---|---|
| **Flanking rampart as a placement surface** | No | No | **This is the one.** §3 and §4 |
| Corridor plug that stalls the wave in place | No — a stop is not a choice | No | Not meant. Would have been cheap; see the note below |
| Wall that lengthens or diverts the route | Yes | Yes, or a re-trace | Not meant. Would reopen a closed decision — and every shipped game that allows it also ships an anti-block rule, all of them different [[46]](#s46) |
| Free mazing on open ground | Yes | Yes, per unit, per tick | Not meant. Ruled out in Vision §11 |

Nothing about the corridor decision is threatened. `HexMap.TraceCorridor`'s assertions stand untouched — every
corridor cell has one or two corridor neighbours, exactly two have one, and the walk from entrance to exit visits
every cell, *"which is what keeps a pathfinder out of the simulation by accident"* [[1]](#s1). Part V's six
mazing-dependent levers stay dead [[14]](#s14). `TowerCoverage.RefuseInsideCorridor` — the throw that says *"A tower
standing in the corridor would be a wall, and walls are how mazing gets in"* [[2]](#s2) — keeps doing exactly the
job it was written for, and a flanking rampart never trips it, because a rampart is on ground cells.

> **One thing worth keeping on file, since it was priced anyway.** *Blocking is not the same as mazing.* Path choice
> requires two or more candidate routes; a corridor one hex wide has one. A defender standing *in* the corridor
> could therefore only cause a **stop**, never a **choice** — a clamp on `DistanceAlongPath`, which is the
> one-dimensional coordinate the simulation already moves creeps along [[5]](#s5). It would cost towers with health
> (`PlacedTower` is invulnerable by design [[3]](#s3)), a `Fold` field, and a `HashLabel` bump retiring the golden
> traces — but **zero record bytes**, and no pathfinder. If a "hold the line" mechanic is ever wanted, that is the
> shape, and this paragraph is the whole of the analysis.

### 2.2 Squads are static, so movement never enters the simulation

The developer's decision: *"i was thinking static, but it does change the volume of projectiles."*

| Variant | Verdict | Reasoning, in short |
|---|---|---|
| Squad holds position for the whole match | **free** | A tower with a different silhouette. No sim change, no record change, no determinism surface |
| Repositionable **between** waves only | **cheap, and it is a seam-1 question** | Coverage recomputed at the build barrier — the same load-time intersection run again. +4 bytes per tower if the position is player-authored |
| Moves **during** a wave, along the route line | cheap in itself, but it makes the defender a corridor occupant | One-dimensional, no search — and it inherits every consequence of the corridor-plug row above |
| Moves **during** a wave, on the ground plane | **reopens a closed decision** | Kills `TowerCoverage`'s load-time collapse of two dimensions into one [[2]](#s2); needs fixed-point steering, and `T:System.Math` is a verified build error [[16]](#s16); every steering tie-break is a place a desync hides |
| Player-steered rally point **during** a wave | **the hardest no** | Vision §2 and §3: a wave *"resolves with no input"*, and the submission-barrier unification rests on nobody acting during one [[11]](#s11) |

The precedent is worth one line, because it is the thing that would otherwise be cited *for* moving squads. Legion
TD 2's fighters do move, and the implementation is **Boids** — *"steering, alignment, cohesion, along with goal
seeking & obstacle avoidance"* — in an arena where *"there are generally no walls that block a unit from where it
wants to go"*, tuned to *"prioritize smoothness, even if it means having some units walk through each other
sometimes"* [[18]](#s18). It can accept a wrong answer for smoothness because it is not replaying anything: its own
developer post describes clients that *"constantly sync [themselves] with the server, rather than syncing with all
the other players"* [[19]](#s19). **Copy the design; never read that implementation as a costing.** Static closes
the question and the note moves on.

---

## 3. A flanking wall: is it a new spatial primitive, or a re-drawn cell?

Concretely, against the code. Three readings, three very different prices.

### 3.1 The three readings

| Reading | What it is | Sim change | Record change | Verdict |
|---|---|---|---|---|
| **R1 — Scenery** | A rampart drawn along the corridor; defenders stand on ordinary ground cells that happen to be *drawn* as being on top of it | **None** | None | **free** |
| **R2 — Cell-based rail** | The wall occupies the ground cells adjacent to the corridor, and those cells are the legal placement surface | **None to the spatial model.** A placement-legality predicate, which is content | None | **free — a constraint, not a mechanism** |
| **R3 — Edge-based rail** | A defender is placed on the *edge* between a ground hex and a corridor hex | Yes, and it is the awkward kind | Yes | **cheap but sticky — and it buys little** |

### 3.2 Why R1 and R2 are free

A placement today is an authored `(column, row)`, converted through `Hex.FromOddRowOffset` at load, stored in the
record as `u16 type_id + i16 q + i16 r` — six bytes — and used for exactly one thing: `TowerCoverage.Intersect`
walks the whole route and asks `tower.Hex.DistanceTo(map.Route[step]) * 1000 <= range`, in whole-hex integer
arithmetic, once, at load [[2]](#s2)[[3]](#s3)[[7]](#s7).

The ground cells adjacent to the corridor are already ordinary cells of that grid. They are already legal
placements (`RefuseInsideCorridor` permits any `MapCell.Ground`), already addressable, already in range, and the
neighbour walk that would identify them is the one `HexMap` already performs on every cell of every map that loads
[[1]](#s1). **A rampart on cells is not a new kind of place to stand. It is a picture drawn over places that
already exist**, plus — if desired — a rule about which of them a player may use.

**Elevation comes along free too.** `TowerCoverage` measures distance in the ground plane and the simulation has no
z-axis anywhere. A defender standing visibly higher is a view offset. If elevation is ever wanted as a *lever* —
Part V §3.9 records Rogue Tower paying elevation in damage and range [[13]](#s13) — it is a per-type integer, not a
new geometry.

### 3.3 What R3 would actually cost, and why it is not worth it

An edge is not a hex, and four separate things notice:

| What breaks | Detail |
|---|---|
| **Addressing** | An edge needs `(q, r, direction)` with direction in 0–5. `Hex.DirectionCount` exists, so the vocabulary is there — but the *coordinate* is not |
| **The record** | `RecordFormat.TowerBytes = 6` becomes 7 or 8, and `GhostVersion` bumps [[7]](#s7) |
| **Canonical order** | `TowerLayout` asserts ascending `(row, column)` and that *"One cell holds one tower: two towers sharing a cell would be two towers with one set of coordinates, and a record could not tell them apart"* [[3]](#s3). Six edge slots per cell means a third sort key and a rewritten uniqueness rule |
| **The distance arithmetic** | This is the real one. `Intersect` computes `Hex.DistanceTo` between two hex centres. An edge has no centre. You either round it to one of the two adjacent cells — in which case it *is* a cell and R3 was pointless — or you introduce half-integer hex distance, which contradicts the property the loader is built around: *"whole-hex integer arithmetic — so no division and no rounding rule is involved, and the answer cannot depend on how anything is drawn"* [[2]](#s2) |

And the payoff is small: an edge placement sits half a hex closer to the corridor than a cell placement, which
changes coverage intervals by well under one route step. **R3 is a format change and a new arithmetic in exchange
for a rounding difference.** Do not.

### 3.4 The one real consequence of R2, and it is a design cost rather than an engineering one

If the rail becomes the *only* legal placement surface, the number and variety of placement slots drops sharply —
and so does a lever the code already supports.

`TowerCoverage` gives each tower a **list** of intervals rather than one, and the docstring explains why: *"the
corridor doubles back on itself: a tower in the middle of the map can easily be within range of two separate
stretches of route with an out-of-range stretch between them"* [[2]](#s2). On the committed map the corridor bends
five times, so an interior placement genuinely can watch two stretches at once. **Every slot on a rail sits one hex
from the corridor**, which makes coverage intervals near-uniform and mostly collapses the two-stretch case.

That is a real trade and it should be made deliberately:

| | Open field of ground cells (today) | Rail flanking the corridor |
|---|---|---|
| Placement variety | High — distance from the corridor is a continuous input | Low — every slot is equivalent up to position along the line |
| Multi-interval coverage at bends | Real, and already implemented | Mostly gone |
| Legibility of "what does this cover" | Poor — the player must reason about a circle against a bending line | **Good — coverage is an arc of the rail either side of the defender** |
| Depth from geometry | Higher | Lower |

**A rail is more legible and less deep than an open field.** For a game whose accessibility pillar is real and whose
hardest unsolved problem is reading two boards at once [[11]](#s11), that is arguably the right trade — but it is a
trade, and it is not what "aesthetic choice" sounds like.

---

## 4. Does a flanking rampart read *better*?

Position, since one was asked for: **draw the rampart, and do not let it carry the defenders' identity.**

**It helps the playfield.** The single best thing a continuous rail does is separate "corridor" from "not corridor"
with an unbroken edge. On a hex grid seen through a fixed isometric orthographic camera with 60° yaw snapping and
six snaps [[14]](#s14)[[15]](#s15), a continuous line is the strongest available cue for *where the lane is* — much
stronger than a change of floor tile, and it survives every yaw snap identically. And it doubles as a **frame**: two
boards on one screen, each bounded by its own rail, are materially easier to parse than two unframed fields of
hexes. That lands directly on seam 7.

**It hurts the defenders.** The thing the player must read is *discrete*: which defender is where, and what it
covers. A continuous rampart removes the visual gap between placements. Two archers three hexes apart on open
ground read as two things; the same two on an unbroken wall read as "some archers on the wall". This compounds a
problem the pipeline already has: KayKit's animation library works because every character sits on the same rig with
near-identical proportions — the pack's own wording is that the clips *"might not look good on other
characters"* [[12]](#s12) — so if every defender is a humanoid on that rig, per-type identity is already leaning
hard on weapon silhouette and palette. **And palette is spoken for**: Vision §6 makes faction recolour *"the
readability fix for watching two boards at once — a lobby should feel like your colour against theirs"*
[[11]](#s11). Spend silhouette on a continuous wall and colour is asked to carry both *which side* and *which
tower*.

**So the rampart should be scenery, not architecture.** Low, uniform, unsaturated, and visually recessive — a rail
rather than a castle — with the defenders standing on it as clearly separated figures at their own cells. The
precedent points the same way:

| Precedent | What it does | What transfers |
|---|---|---|
| **Kingdom Rush** | Each melee placement is a **barracks building** with its own footprint and silhouette; the soldiers are the effect [[23]](#s23) | The placement carries identity, the bodies carry mass and juice. Directly applicable, and it is the arrangement that shipped |
| **Bad North** | Squads on a bare island; Stålberg: *"The shape of the terrain really matters. It creates different choke points and different ways you need to position your units"* [[39]](#s39) | It reads because the terrain is **empty**. The squads are the only figures on it. An ornate rampart is the opposite of this |
| **They Are Billions** | Continuous walls plus placed units plus towers, RTS aesthetic, up to 20,000 units [[29]](#s29) | **The warning.** Its own community cannot agree whether hordes route around walls or toward gaps [[31]](#s31), and its legibility is carried by a high-zoom isometric camera on **one** board. This project has two |
| **Age of Empires II garrisoning** | Units go *inside* a building and add arrows to its volley [[44]](#s44) | A third answer: bodies as an invisible upgrade. Maximum legibility, zero juice. ⚠ Secondary, read only as search snippets |
| **Orcs Must Die!** | Traps typed by mounting surface — floor, wall, ceiling — as distinct placement classes [[34]](#s34) | A rampart is a *surface class*, which is Part V §3.9's cheapest way to fit more defenders alongside a corridor without lengthening it [[13]](#s13) |

**Accessibility.** A rail helps a newcomer more than it helps an expert, which is the right way round for this
project. "Your guys go on the wall, the enemies walk down the middle" is a sentence that needs no tutorial, and
Vision §4's constraint — nothing is unlocked, *"every unit must be interesting from the first run"* [[11]](#s11) —
means the teaching budget is a build phase and a tooltip. A rail spends that budget well. What it must not do is
make two adjacent placements indistinguishable, because *that* is the thing a newcomer cannot recover from.

---

## 5. ⚠ Projectile volume — the live question

### 5.1 What the code does today, exactly

Five facts, all verified in the repository, because every number below rests on them.

1. **`Fire` creates one projectile per shot, and rolls the dice once.** *"The one and only draw. Once per shot, on
   the one stream, whether or not the shot is going to land on anything."* `Launch` stores an id, a `TargetRef`, a
   flight countdown and a damage value — **and no position, now or ever** [[4]](#s4)[[6]](#s6).
2. **`FlyProjectiles` is O(projectiles × creeps).** Each projectile calls `FindWalkingCreep`, which is *"a linear
   scan on purpose: the alternative is a dictionary, whose enumeration order is an implementation detail and which
   the scan over the compiled assembly refuses outright"* [[4]](#s4). That scan runs for every projectile on every
   tick it is in flight.
3. **`Acquire` is O(creeps), but it only runs on the tick a tower is ready to shoot** — `RunTowers` reaches it only
   from `Idle` with `Cooldown == 0` [[4]](#s4). Over a shot cycle of *C* ticks, a tower pays it once.
4. **`PullSnapshot` allocates three fresh arrays every tick it is called** — including `new
   ProjectileSnapshot[_projectileCount]` and `new TowerSnapshot[_towers.Length]`, the latter always at full length
   regardless of activity [[4]](#s4). It is **pulled, not pushed**: *"a run that never asks for one never builds
   one"* [[5]](#s5), so a headless sweep pays none of it.
5. **A hitscan shot produces no snapshot entity at all.** The asymmetry is deliberate: *"a hitscan tower's shot
   produces no entity in this snapshot at all — it exists only as an event and whatever tracer the view draws and
   forgets — while a projectile tower's shot produces a real `ProjectileSnapshot` that can be scrubbed backwards
   through. Same seam, opposite treatments, on purpose."* [[5]](#s5)

**The committed baseline is almost nothing, and that matters.** The defense has six towers: four `bolt` (hitscan,
windup 3 + backswing 2 + cooldown 6 = 11 ticks per shot, **zero projectiles**) and two `mortar` (windup 7 +
backswing 5 + cooldown 18 = 30 ticks per shot, flight 11 ticks) [[8]](#s8)[[10]](#s10). Because 11 < 30, a mortar
can never have two shells in the air. **The build has never had more than two projectiles airborne at once.** Every
projectile-scaling claim below is therefore arithmetic over the loop structure, not an extrapolation from
measurement — and it should be read that way.

### 5.2 The three models, and a fourth

> **The finding that decides this section.** N identical archers standing on one cell have **one** coverage interval
> between them, because `TowerCoverage` computes intervals per placement from `(hex, range)` [[2]](#s2). `Acquire`
> is deterministic and returns *"whichever creep it can reach is furthest along the corridor, and the lowest id of
> those if two are level"* [[4]](#s4) — so all N pick the same creep. They are constructed in the same state
> (`Idle`, cooldown zero) and nothing in `RunTowers` can separate them: `Fire` *"happens whatever became of the
> target"*, so even a target dying mid-windup does not shift anyone's timing. **They fire in lockstep, forever.**
>
> Therefore **option 1 and option 2 are behaviourally identical** — the same N arrows leave on the same tick with
> the same N damage rolls off the same stream. Option 1 differs only in carrying N tower states in the snapshot and
> in the rolling hash. It earns that cost in exactly one case: **when the bodies can diverge**, which in practice
> means when they can die.

| | **1. Sim-true volume** | **2. One shooter, N real projectiles** | **3. One shooter, one damage event, N arrows drawn** |
|---|---|---|---|
| Targeting decisions per volley | N (all identical) | 1 | 1 |
| Damage rolls per volley | N | N (or 1 — a choice) | 1 |
| Projectile entities per volley | N | N | **0** |
| Per-arrow orphaning at the sit-down's tick-224 landmark | Yes | Yes | **No — one arrow only** |
| Per-arrow scrub fidelity | Yes | Yes | **No** |
| Bodies can die independently | **Yes** | No | No |
| Code change from today | New placement→shooter expansion at load; `_towers` grows N× | ~3 lines: loop `Launch` N times inside `Fire` | View-only, plus a new event payload |
| What it is for | **Attrition.** Nothing else | Volume with per-arrow truth | Volume as decoration |

**Option 4 — one shooter, N real projectiles, launched over k consecutive ticks (a staggered volley).**
Recommended below, and it is barely more expensive than option 2: one counter on `Match.Tower` for "arrows of this
volley still to launch", one branch in `RunTowers`, one extra `.Add` in `Fold`. Part V §3.2 already has the
vocabulary — *"Shots per attack, spread, salvo timing… Sequential vs simultaneous matters for overkill"*
[[13]](#s13). It buys three things option 2 does not: it flattens the peak-projectile spike (§5.3), it makes overkill
*visible* rather than merely real (arrows three through five landing on a corpse), and it turns "volley shape" into
a per-type lever a balance harness can sweep.

### 5.3 The numbers

**Assumptions, stated so they can be disagreed with.** ⚠ None of these is a decision. A defense of **15
placements per board**, of which **8 deliver by projectile**; **60 live creeps** per board (Part III's stated target
is 40–60 simultaneous units [[12]](#s12)); an archer cycle of **13 ticks** (windup 3 + backswing 2 + cooldown 8) with
**6 ticks** of flight, so a duty cycle of 6/13 ≈ **0.45**. One board is one `Match`; two boards resolve per round, so
render figures double while per-match figures do not.

**Projectiles in the air.** Average = `projectile placements × N × duty`. Peak = `projectile placements × N`, which
is reached whenever the squads are in phase — and under options 1, 2 and 4 with identical bodies, **they always
are** unless the volley is staggered.

| N | avg in air (one board) | peak (one board) | peak, both boards |
|---:|---:|---:|---:|
| 1 | 3.6 | 8 | 16 |
| 3 | 10.8 | 24 | 48 |
| 5 | 18 | 40 | 80 |
| 8 | 28.8 | 64 | 128 |

*(Committed match today, for scale: **≤ 2**.)*

**Per-tick simulation cost.** The dominant term is creep-array scans, and there are two sources: `FlyProjectiles`
(`P × C`, every tick) and `Acquire` (`C` per tower per shot cycle, i.e. `T × C / cycle`). At N = 1 that is
`3.6 × 60 = 216` against `(15/13) × 60 ≈ 69` — **the projectile loop already dominates acquisition by about three to
one, before any squad exists.** That is the non-obvious result and it drives everything else.

| Model, N = 5 | Towers `T` | Acquire scans/tick | Avg `P` | Fly scans/tick | Total | vs baseline |
|---|---:|---:|---:|---:|---:|---:|
| Baseline (N = 1) | 15 | 69 | 3.6 | 216 | ~285 | 1.0× |
| **1. Sim-true** | 75 | 346 | 18 | 1,080 | ~1,426 | **~5.0×** |
| **2 / 4. One shooter, N arrows** | 15 | 69 | 18 | 1,080 | ~1,149 | **~4.0×** |
| **3. Arrows are decoration** | 15 | 69 | 3.6 | 216 | ~285 | **1.0×** |

At N = 8: option 1 ≈ 8.0×, option 2 ≈ 6.3×, option 3 ≈ 1.0×.

> **Read the gap between option 1 and option 2 carefully.** It is only about 25% at N = 5, not 5×, because both pay
> the same projectile bill and the targeting loop is small. **Performance is therefore *not* the main reason to
> prefer option 2 over option 1.** The reason is the lockstep argument in §5.2: option 1 costs N tower states in the
> snapshot and the hash and returns nothing for them.

**Snapshot growth** (view only — a headless sweep pulls no snapshot [[5]](#s5)). `ProjectileSnapshot` is
Id + TypeId + `TargetRef` + TicksInFlight + FlightDuration = **24 bytes**; `TowerSnapshot` is **16 bytes**. Both
arrays are reallocated every tick a snapshot is pulled, at 30 ticks per second, across two boards.

| N = 5 | projectile bytes/tick (peak) | tower bytes/tick | transient allocation, 2 boards @ 30 Hz |
|---|---:|---:|---:|
| Baseline (N = 1) | 192 | 240 | ~26 KB/s |
| **1. Sim-true** | 960 | 1,200 | **~130 KB/s** |
| **2 / 4.** | 960 | 240 | ~72 KB/s |
| **3.** | 192 | 240 | ~26 KB/s |

Modest in absolute terms, but it is per-frame garbage in the client, and option 1 is the only one that inflates the
*tower* array — which is allocated at full length every tick whether or not anything is happening.

**Ghost and replay record growth: zero, under every option.** This is the cleanest answer in the note. A defense
record is a map hash plus `u16 type_id + i16 q + i16 r` per tower; a replay bundle is a seed, a map, a defense and a
wave [[7]](#s7). **Projectiles are never in a record**, because a record stores *inputs* and a projectile is output
— Part V's layer-3 rule, that instance state is *"never serialised into a ghost"* [[13]](#s13). Squad size is a
property of the unit type, and the type table is content, hashed once, not carried per record. The **one** thing
that would change this is making individual bodies separately placeable, which would also collide with
`TowerLayout`'s *"One cell holds one tower"* assertion [[3]](#s3). Do not do that.

**Balance-harness sweep multiplier.** The sweep runs headless — no snapshot, no events (`ReportPasses` returns on
its first line when `events is null` [[4]](#s4)) — so it pays the `Step` cost and nothing else. The multipliers are
the per-tick column above: **≈4× at N = 5 and ≈6× at N = 8 for options 2 and 4, ≈5× and ≈8× for option 1, 1× for
option 3.** ⚠ These are arithmetic over the loop structure at assumed board sizes, not measurements. The real number
wants `tools/run-headless-match.ps1` timed against a defense built for it, and that is a half-hour experiment that
would replace this entire table with facts.

**Render and animation instances.** Bodies are N× under *all* options — that is the point of a squad, and it is the
cost §5.5 of the previous framing has to be honest about. At N = 5 with 15 placements across two boards: **150
defender rigs plus 80–120 creep rigs**. Each rig today is its own `PlayableGraph` in `DirectorUpdateMode.Manual`,
and `Pose(slot, phase)` writes time and weight to **every** slot before calling `_graph.Evaluate(0f)` [[9]](#s9), so
per-frame cost is O(rigs × slots) with one graph evaluation per rig. `EntityViewPool` keeps *instantiation* to once
per concurrently-live entity [[10]](#s10) but does nothing about per-frame evaluation.

⚠ **I could not verify Unity's guidance on this** — the session's web-search budget was exhausted during the
precedent research. The honest statement is structural: one graph per rig is the un-batched path, 150 of them is a
different regime from 15, **and the whole risk sits in one file that the architecture already isolates.** `Pose` is
a pure function of `(slot, phase)`, so consolidating N graphs into one graph with N outputs, or moving to Animation
C# Jobs, is a view-layer refactor that cannot touch the simulation, cannot touch a stored record, and cannot change
a replay. It is squarely in Part IV §8's **cheap** column [[12]](#s12).

### 5.4 Option 3 against this project's own standards

Option 3 keeps simulation cost flat and the juice intact. Three specific things it gives up, and the third is the
one that matters.

**It loses the tick-224 landmark for N−1 arrows.** `content/landmarks.txt` carries `projectile-orphaned` at tick
224 — *"shell 23 loses the creep it was aimed at, mid-flight"* — and sit-down row 6 is a person dragging to tick 240
and back to tick 210 to check that the shell does not linger [[15]](#s15). That property is committed, verified, and
falls straight out of the architecture: *"the target lookup that would have found somebody to damage does not find
them, so the projectile stops existing and therefore stops appearing in the snapshot. There is no path by which it
can linger, because there is no state it could linger in"* [[4]](#s4). Decorative arrows have no snapshot to be
absent from.

**It reintroduces exactly the failure `EntityViewPool` was built to prevent.** The pool's docstring is unusually
blunt: *"There is exactly one way an object goes back in the pool: its id stopped appearing… A second bookkeeping
path — an event that says 'this one is gone', a flag on the view, a timer — is a second opinion about what exists,
and the two disagree exactly when something interesting happened: **a projectile whose target died mid-flight**, a
creep removed on the tick a scrub jumped over"* [[10]](#s10). Decorative arrows *are* that second bookkeeping path,
by construction, and the example the docstring reaches for is this exact case.

**And what the player sees stops being what the simulation did.** One damage roll drawn as five arrows means five
arrows land and one number is applied. This project is unusually strict here — the sit-down deleted a scrub test
*"for being a tautology"*, `docs/frames/README.md` records deleting a screenshot check because it could not fail,
and `SimDrivenAnimator` exists so the view holds no playback head at all [[9]](#s9)[[15]](#s15).

> **The resolution, and it is clean.** Part IV §8 already licenced this — for *particles*: *"the sim emits what
> happened… and the view maps events to effects. Damage is a sim event on a tick; the explosion is a consequence you
> play, never a cause"*, together with the seek rule *"You cannot rewind a particle system. Clear all active VFX on
> any seek"* [[12]](#s12). **Option 3 is therefore legitimate if and only if an arrow is reclassified as VFX** — and
> then the existing rule covers it, including the scrub behaviour, at no new architectural cost.
>
> The test for whether that reclassification is honest is one question: **does any single arrow ever need to be
> orphaned, scrubbed to, or blamed for a kill?** If yes, it is an entity and option 3 is wrong. If the arrows are
> genuinely interchangeable spray around one real shot, it is VFX and option 3 is right.

### 5.5 The escape hatch nobody has noticed

**If projectile volume turns out to be the problem, making squad weapons hitscan is a one-field content change.**
`Delivery` is a column in `content/units.txt` [[8]](#s8), and a hitscan shot *"produces no entity in this snapshot at
all"* [[5]](#s5). A squad of five archers whose arrows are hitscan with a view-drawn tracer costs the simulation
**zero projectiles**, keeps per-shot damage truth (five real rolls, five real damage events), keeps every shot in
the state hash — and gives up only the arc, the travel time, and the orphaning drama.

That is not a compromise bolted on; it is the asymmetry the walking skeleton was deliberately built to demonstrate
*"same seam, opposite treatments, on purpose"* [[5]](#s5). It means the projectile-volume question is **reversible
per unit type, at any time, from a data file**, which is a much better position than the table in §5.3 implies. It
should probably be the default for high-rate squad weapons and the arc reserved for the slow, heavy, few-shots
weapons where the travel time is the mechanic.

---

## 6. Does a volume of small hits play differently from a few large hits?

Yes, and in four places the project has already thought about. This is where a squad earns or loses its keep as a
*mechanic* rather than a look.

**The armour formula is no longer a free choice.** Part V §4.2 records that flat subtraction *"inverts the value of
attack speed — it punishes many-small-hits quadratically"*, and documents Gemcraft's endgame failure under it
[[13]](#s13). Under flat subtraction a five-archer squad is not merely weaker against armour, it is *catastrophically*
weaker in a way no amount of tuning fixes. **If squads ship, flat subtraction should be ruled out**, and the
`damage × 100 / (100 + k·armor)` form — whose property is that effective health is linear in armour — becomes close
to mandatory. That formula is a ruleset-wide constant recorded in the header [[13]](#s13), so this is a decision to
take before the first ruleset, not after.

**The integer rounding hazard is specific to volume, and it is the thing to check first.** ⚠ In an integer sim,
`smallDamage × 100 / (100 + k·armor)` rounds toward zero. Five arrows of 5 damage against high armour can each round
to the damage floor, at which point armour stops discriminating at all above a threshold and five small hits and
five tiny hits are the same thing. Part V calls a minimum damage floor *"non-optional in an integer sim"*
[[13]](#s13); volume is what makes the floor load-bearing rather than defensive. **This is checkable today with
arithmetic and no code**, and it should be, before any number is chosen.

**Overkill stops being an occasional event and becomes the common case.** `Fire`'s docstring is explicit that a
committed shot lands whatever became of its target — *"two towers covering one stretch of corridor can both commit
to the same creep, and the second one's damage lands on something already dying and is discarded. Re-checking the
target here would quietly make that impossible, and with it the whole reason the ranges were made to overlap"*
[[4]](#s4). With N synchronised arrows aimed at one creep, a squad routinely commits five arrows to a target that
two would kill. Part V lists *"Overkill policy: carries / wasted"* as an explicit lever and warns it *"Decides
whether high-damage-slow beats low-damage-fast against small creeps"* [[13]](#s13). **Squads convert that from an
optional lever into a decision that must be made**, and a staggered volley (option 4) is the cheapest way to make
the answer *visible* to the player rather than merely true.

**It argues for the narrow end of an open question.** The damage-type matrix width is still open — Vision §10 carries
it forward, with Legion TD 2 at 1.67:1, Element TD 2 at 4:1 and Warcraft 3 at 40:1 [[11]](#s11)[[13]](#s13). Legion
TD 2's own published table runs every cell between 75% and 125% [[17]](#s17). **A wide matrix composes badly with
volume**, for the same rounding reason: five arrows each multiplied by 0.05 round to the floor five times, and the
type chart stops being a tilt and becomes an on/off switch. A squad roster is therefore a real argument for the
narrow end, and that is a genuine input to a decision that was going to be made on other grounds.

**And there is one architectural argument *for* squads that nobody has made.** Part V §5.4 records the tick ceiling:
*"Attack speed cannot exceed one attack per tick"*, and notes that the usual fixes are to clamp (voiding the
player's investment) or to accumulate fractional cooldowns (a float through the side door) [[13]](#s13). **N shooters
is the legal way to exceed one attack per tick per placement while staying entirely in integers.** A squad is a rate
multiplier that the arithmetic contract has no objection to. That is a better justification for squads-as-a-mechanic
than anything in the aesthetics.

Part V also already supplies both counters, which means the schema does not need widening: *"Flat damage
reduction… Creates hard counters to many-small-hits"* and *"Damage cap per hit… Creates hard counters to
few-big-hits"* [[13]](#s13). A roster containing both squads and single heavy hitters makes both of those levers
sharp instead of theoretical. **That is build depth genuinely gained**, and it is the strongest mechanical case for
the idea.

---

## 7. Volume, juice, and the two-board budget — a position

**Volume is juice up to the point where individual events stop being countable, and past that it is noise. The
two-board constraint moves that point much lower than a single-board game's, and it is the binding constraint here.**

A player watching one board can track a dozen arrows. A player watching two boards, an economy, a build menu and a
readable account of what the opponent just did — Vision's seam 7, *"the hardest unsolved problem in the design"*
[[11]](#s11) — cannot track eighty. What survives at that attention budget is not individual arrows but **rhythm and
density**: a volley that arrives as a recognisable pulse reads; a continuous drizzle of the same total DPS does not.
Three consequences, and they all point the same way:

1. **Stagger volleys, and put a clear gap between them** (option 4). A pulse of five arrows every thirteen ticks is
   legible; five arrows arriving continuously is a texture.
2. **Prefer fewer, larger squads to many small ones.** Five archers on one placement is one readable event; one
   archer on each of five placements is five events competing.
3. **Prefer hitscan tracers to slow arcs for the high-rate weapons** (§5.5). A tracer is gone by the next frame and
   accumulates nothing; an arc persists on screen and, at N × 8 placements × 2 boards, the screen fills with
   in-flight geometry that carries no information the player can act on.

And the conclusion that matters most: **squads should be an archetype, not the model for the whole defense.** That is
the same conclusion §4 reaches from silhouette legibility, by an entirely independent route — and two independent
lines converging is the strongest thing in this note. A roster where some placements are single heavy characters or
buildings and a few are squads keeps per-type silhouette, keeps the projectile budget bounded, keeps overkill an
interesting decision rather than a constant tax, and makes both of Part V's counter-levers meaningful. A roster
where every placement is five humanoids on a rampart spends silhouette, colour, projectile budget and attention all
at once.

---

## 8. Recommendation

Three, ranked. Each is a paragraph, and the price is named.

### Option A — Scenery rampart, view-only squads, hitscan for the fast weapons. **Recommended.**

Draw the flanking rampart as **R1/R2 scenery** on ground cells that are already legal placements — no new spatial
primitive, no record change, no coverage change (§3). Let *some* placements draw as N bodies in a fixed formation,
as one simulation entity with one cooldown and one target (§5, option 2), and make the fast squad weapons **hitscan**
so they put nothing in the air (§5.5). Keep buildings or single heavy characters for a meaningful share of the roster
so per-type silhouette survives (§4, §7). **Architectural price: nothing.** No sim change beyond a `Fire` that can
roll and apply damage N times, no `TowerSnapshot` change, no `TowerBytes` change, no `HashLabel` bump, no golden
traces retired, no measurable harness cost. **What it does not buy:** a mechanic. A squad plays as a tower with a
higher rate of fire — which, per §6, is genuinely worth having, because it is the only way to exceed one attack per
tick without leaving the integers.

### Option B — Option A, plus real staggered volleys on the slow, heavy squads.

For the weapons where travel time *is* the mechanic, keep `Delivery.Projectile` and emit N real arrows spread over
*k* consecutive ticks (§5.2, option 4). Cost: one counter on `Match.Tower`, one branch in `RunTowers`, one `.Add` in
`Fold` — and a `HashLabel` bump, which retires the golden traces and is therefore **much cheaper now than after a
ghost pool exists**. It buys per-arrow orphaning and scrub fidelity intact, a flattened projectile peak, visible
overkill, and "volley shape" as a per-type lever the balance harness can sweep. Budget for roughly **4× the
per-tick creep-scan cost at N = 5** on the squads that take it (§5.3) — ⚠ estimated from loop structure, not
measured — and keep the count of such placements small for exactly that reason.

### Option C — Sim-true bodies, and only if squads can lose members.

Model each archer as its own placed shooter *only* if the design wants a squad to be **attritable** — five archers
that become three after a bad wave. That is the one thing options A and B cannot express, and it is the only thing
that justifies N tower states in the snapshot and the hash (§5.2). It requires towers to have health, which
`PlacedTower` explicitly does not — *"There is no hp, no cooldown and no state here"* [[3]](#s3) — and that is a
`Fold` field plus a `HashLabel` bump. ⚠ **And it carries one hazard the other two do not:** if squad losses persist
*between* waves, a stored defense stops being a layout and becomes a layout plus a biography, which is Part V §10.5's
replay hazard [[13]](#s13). The north star refuses this explicitly — Legion TD 2's manual: *"After each enemy wave,
your fighters are fully healed and restored to their original positions"* [[17]](#s17). Kingdom Rush refuses it too,
by respawning soldiers on a timer so the resource spent is tempo rather than permanent strength; respawn is a tuned
per-tower stat there, e.g. Cannoneer Squad `10 → 8 seconds` in an Ironhide balance patch [[25]](#s25). **If attrition
is wanted, reset it at the wave boundary.**

---

## 9. What would change the analysis

1. **A timed headless sweep.** §5.3's whole cost table is arithmetic over loop structure at assumed board sizes.
   Build a defense with 8 projectile placements against a 60-creep wave, run `tools/run-headless-match.ps1` at
   N = 1 and at N = 5 arrows per shot, and time both. That is half an hour and it replaces the table with facts. If
   the answer is "4× of nothing is still nothing", option B stops needing to be rationed.
2. **The integer-rounding check in §6.** Pick a candidate armour constant, a candidate matrix width and a candidate
   squad arrow damage, and compute what a five-arrow volley does against the highest-armour creep. If arrows round
   to the floor, the squad decision has just constrained two open ruleset decisions and should say so loudly.
3. **A screenshot test for §4.** Put five KayKit rangers on a flanking rail beside the corridor and one Medieval
   Hexagon archery tower on the next cell, render at the actual camera distance across all six yaw snaps, and look.
   It settles the silhouette argument better than any reasoning here, and it is an afternoon.
4. **A measurement of per-rig Playables cost at 150 rigs.** §5.3's render row is the least-supported claim in the
   note. If one graph per rig is fine at 150, the body-count concern evaporates; if it is not, the fix is a
   view-layer refactor that touches nothing else.

---

## 10. What I could not verify

- **Any per-rig Playables cost figure, and Unity's guidance on it.** The session's web-search budget (200 calls) was
  exhausted during the precedent research, so §5.3's animation row is structural reasoning over the committed code,
  not a benchmark and not a citation.
- **Whether Unity's SRP Batcher covers `SkinnedMeshRenderer`.** The Unity 6 page states what it does and which
  pipelines support it, and says nothing about renderer types [[45]](#s45).
- **Kingdom Rush's numbers.** `kingdomrushtd.fandom.com` returns HTTP 402 and `kingdomrush.wiki.gg` returns 401 to
  automated fetching, so soldier counts per tier and rally-point radius are from **search-engine snippets of a wiki
  I could not read** [[28]](#s28), or from Wikipedia's one-line summary [[23]](#s23). Two facts are first-party: a
  Kingdom Rush *Battles* barracks *"spawns two soldiers that block enemies and attack melee units"* [[24]](#s24), and
  respawn time is a tuned per-tower stat [[25]](#s25). Nothing else about Kingdom Rush here should be relied on for
  a number.
- **Whether Legion TD 2's fighters can be repositioned and at what cost.** A v6.00 patch note mentions worker-queue
  handling *"when repositioning a tower on wave 1"*, so the concept exists; no official statement of the rule was
  found. Not load-bearing here now that squads are static.
- **They Are Billions' pathfinding.** No first-party description of the algorithm exists that I could find, and the
  community contradicts itself [[31]](#s31). Cited in §4 *as* a disagreement, which is the only thing it supports.
- **The AoE2 garrison arrow rule.** Read only as search snippets; one claim in them contradicts common play
  experience [[44]](#s44). Treat as unverified.

---

## Sources

**This repository — primary, and the authority for every architectural and numeric claim above.**

<a id="s1"></a>1. [`sim/HexMap.cs`](../../sim/HexMap.cs) — `TraceCorridor`, the corridor assertion in full: every corridor cell has one or two corridor neighbours, exactly two have one, those two are the entrance and the exit, and the walk visits every cell. "Together they are what keeps a pathfinder out of the simulation by accident: route derivation is this trace, done once, and there is nothing left for a search to do." The neighbour walk that would identify cells adjacent to the corridor is the one already run on every cell of every map that loads.
<a id="s2"></a>2. [`sim/TowerCoverage.cs`](../../sim/TowerCoverage.cs) — "This is where the two dimensions stop"; `Intersect` walks the route once at load using `Hex.DistanceTo` in "whole-hex integer arithmetic — so no division and no rounding rule is involved, and the answer cannot depend on how anything is drawn"; the per-tower **list** of intervals and why ("the corridor doubles back on itself"); `Covers` as "the simulation's second-hottest inner loop"; `RefuseInsideCorridor` — "A tower standing in the corridor would be a wall, and walls are how mazing gets in."
<a id="s3"></a>3. [`sim/TowerLayout.cs`](../../sim/TowerLayout.cs) — `PlacedTower`: authored `(column, row)` converted to axial at load; "There is no hp, no cooldown and no state here. A tower is invulnerable and static for the whole match." The canonical `(row, column)` order assertion, and "One cell holds one tower: two towers sharing a cell would be two towers with one set of coordinates, and a record could not tell them apart."
<a id="s4"></a>4. [`sim/Match.cs`](../../sim/Match.cs) — `Step`'s phase order; `RunTowers` (Acquire reached only from `Idle` with `Cooldown == 0`); `Acquire` ("whichever creep it can reach is furthest along the corridor, and the lowest id of those if two are level"; "The rule is a total order, which is the only kind of rule that can be replayed"); `Fire` ("The one and only draw. Once per shot"; "The shot happens whatever became of the target"); `Launch`; **`FlyProjectiles`** and its orphaning behaviour ("there is no state it could linger in"); **`FindWalkingCreep`** — "A linear scan on purpose: the alternative is a dictionary, whose enumeration order is an implementation detail"; `ReportPasses` returning immediately when `events is null`; `PullSnapshot`'s three per-tick allocations, with the tower array always at full length; `Fold`'s three hash rounds per projectile and `HashLabel = "match-state/1"`; `TickCeiling`; tower construction in `Idle` with cooldown zero.
<a id="s5"></a>5. [`sim/Snapshot.cs`](../../sim/Snapshot.cs) — the snapshot is "pulled, not pushed": "a run that never asks for one never builds one, which is the whole of what instant-resolve is." `CreepSnapshot` is `DistanceAlongPath` + `LateralOffset`, "never a point in a plane." **The hitscan/projectile asymmetry**: "a hitscan tower's shot produces no entity in this snapshot at all — it exists only as an event and whatever tracer the view draws and forgets — while a projectile tower's shot produces a real `ProjectileSnapshot` that can be scrubbed backwards through. Same seam, opposite treatments, on purpose." Field lists giving the 24-byte `ProjectileSnapshot` and 16-byte `TowerSnapshot`.
<a id="s6"></a>6. [`sim/TargetRef.cs`](../../sim/TargetRef.cs) — a projectile carries "a kind and an id, and no position of any sort"; "It also keeps free 2D out permanently. There is no field here that could hold a point." `TargetKind.Tower` exists and is unused — "an arm nothing takes is cheaper than a migration."
<a id="s7"></a>7. [`sim/RecordFormat.cs`](../../sim/RecordFormat.cs), [`sim/GhostRecord.cs`](../../sim/GhostRecord.cs) — `TowerBytes = 6`: `u16 type_id + i16 q + i16 r`; `OrderBytes = 9`; `GhostVersion`. "A tower entry names its type and never its stats." A defense is a map hash plus tower rows; nothing in any record is a projectile.
<a id="s8"></a>8. [`sim/UnitType.cs`](../../sim/UnitType.cs), [`content/units.txt`](../../content/units.txt) — one global id space; `UnitRole` as "a filter, not a class"; `Delivery` as a per-type column (`none` / `hitscan` / `projectile`); the committed `bolt` (windup 3, backswing 2, cooldown 6, hitscan) and `mortar` (windup 7, backswing 5, cooldown 18, flight 11).
<a id="s9"></a>9. [`client/Assets/View/SimDrivenAnimator.cs`](../../client/Assets/View/SimDrivenAnimator.cs) — one `PlayableGraph` per rig, `DirectorUpdateMode.Manual`, clip speed zeroed; `Pose(slot, phase)` writes time and weight to every slot then calls `_graph.Evaluate(0f)`; no `RuntimeAnimatorController` anywhere, banned outright rather than configured carefully.
<a id="s10"></a>10. [`client/Assets/View/EntityViewPool.cs`](../../client/Assets/View/EntityViewPool.cs) — "There is exactly one way an object goes back in the pool: its id stopped appearing… A second bookkeeping path — an event that says 'this one is gone', a flag on the view, a timer — is a second opinion about what exists, and the two disagree exactly when something interesting happened: a projectile whose target died mid-flight, a creep removed on the tick a scrub jumped over." Also `CreepView`, `RoutePath` ("the whole of the view's position arithmetic"), `ViewMaterials`; and [`content/defense.txt`](../../content/defense.txt), the committed six-tower defense (four `bolt`, two `mortar`).
<a id="s11"></a>11. [`docs/vision.md`](../vision.md) — §2 the submission barrier and "nobody is acting during a wave"; §3 "a wave resolves with no input", "never has to follow a policy you authored"; §4 "every unit must be interesting from the first run"; §6 faction recolour as the two-board readability fix; §7 seam 7, "the hardest unsolved problem in the design"; §10 the open damage-type matrix width; §11 mazing and pathfinding out of scope.
<a id="s12"></a>12. [`docs/art-direction-and-assets.md`](../archive/art-direction-and-assets.md) — §5 "Rooted animated characters — fixed placement, living towers"; the rooted-units-need-no-locomotion saving; the KayKit one-atlas/one-material property and the retargeting proportion caveat; §7.2 "Part III's target is 40–60 simultaneous units"; §8 the cheap/sticky split, the sim-owns-fire-cadence rule, and the particle rules — "Damage is a sim event on a tick; the explosion is a consequence you play, never a cause" and "You cannot rewind a particle system. Clear all active VFX on any seek."
<a id="s13"></a>13. [`docs/variance-levers-and-unit-schema.md`](../archive/variance-levers-and-unit-schema.md) — §3.2 "Shots per attack, spread, salvo timing… Sequential vs simultaneous matters for overkill" and "Overkill policy: carries / wasted"; §3.4 flat damage reduction as a counter to many-small-hits, damage cap per hit as a counter to few-big-hits, and the minimum damage floor as "non-optional in an integer sim"; §3.9 surface class and elevation; §4.1 the matrix-width spread (1.67:1 / 4:1 / 40:1); §4.2 the armour formula, flat subtraction "punishes many-small-hits quadratically", and the effective-health-linear-in-armour form; §5.4 "Attack speed cannot exceed one attack per tick"; §6 layer 3, instance state "never serialised into a ghost"; §10.5 per-instance history as a replay hazard.
<a id="s14"></a>14. [`docs/README.md`](../README.md) — "What has been settled since": no mazing ever, the six mazing-dependent levers killed by it, and the fixed isometric orthographic camera with 60° yaw snapping.
<a id="s15"></a>15. [`docs/sit-down.md`](../sit-down.md) — the landmark table, including `projectile-orphaned` at tick 224 ("shell 23 loses the creep it was aimed at, mid-flight"); row 6, scrubbing back across the orphaned shell; row 11, the six yaw snaps and the no-billboards check; the scrub test deleted "for being a tautology", and `docs/frames/README.md` on deleting a screenshot check that could not fail.
<a id="s16"></a>16. [`docs/research/unity-sim-library-integration.md`](unity-sim-library-integration.md) — §6a, the measured `BannedApiAnalyzers` matrix confirming `T:System.Math` is enforced as a build error.

**Primary — official manuals, developer posts, patch notes.**

<a id="s17"></a>17. Legion TD 2 — [official manual](https://beta.legiontd2.com/manual/). "Build fighters to defend your lane. When enemies come, your fighters automatically attack and cast spells." "**After each enemy wave, your fighters are fully healed and restored to their original positions.**" "When your fighters lose the battle, this is called a leak." "Damage dealt to a unit will be multiplied by a factor ranging from 75% to 125%." *(Read directly.)* The full four-by-five type table, every cell between 75% and 125%, is published at [beta.legiontd2.com/typetable](https://beta.legiontd2.com/typetable/).
<a id="s18"></a>18. Legion TD 2 — [Pathing & Targeting](https://legiontd2.wiki.gg/wiki/Pathing_%26_Targeting), wiki.gg, the wiki linked from the official site; **semi-primary**. "Boids, which consists of steering, alignment, cohesion, along with goal seeking & obstacle avoidance." "There are generally no walls that block a unit from where it wants to go." "Configured to prioritize smoothness, even if it means having some units walk through each other sometimes." Acquisition range versus attack range; the three-step target logic; tie-breaks on missing health, then proximity (ranged only), then forward placement. *(Read directly.)*
<a id="s19"></a>19. Legion TD 2 — [Multiplayer connections](https://beta.legiontd2.com/updates/multiplayer-connections/), developer post. "Each player's game will constantly sync itself with the server, rather than syncing with all the other players." Lagging clients fast-forward. **Server-authoritative, not lockstep, and not replay-driven.**
<a id="s24"></a>24. Ironhide — [Paladin Barracks skills breakdown, Kingdom Rush Battles support article](https://support.ironhidegames.com/support/solutions/articles/4000223657-paladin-barracks-skills-breakdown-defend-the-lines-in-kingdom-rush-battles). "Spawns two soldiers that block enemies and attack melee units." *(A different title from KR1; do not read the count across.)*
<a id="s25"></a>25. Ironhide — [Kingdom Rush 5: Alliance balance patch notes](https://www.ironhidegames.com/News/Details/405). Cannoneer Squad respawn "10 → 8 seconds" — respawn is a real, tuned, per-tower stat.
<a id="s29"></a>29. They Are Billions — [Steam store page](https://store.steampowered.com/app/644930/They_Are_Billions/). "Up to 20,000 units in real time… Every one of them has their own AI." "Build walls, gates, towers, and structures." Corroborated by [v1.0.8 patch notes](https://steamdb.info/patchnotes/3949342/), whose "Pathfinding for Swarms: Improved Performance" confirms a real pathfinder and says nothing about what it does with walls.
<a id="s39"></a>39. Bad North — [GamesBeat interview with Richard Meredith and Oskar Stålberg](https://gamesbeat.com/bad-north-shows-that-even-bloody-viking-battles-can-be-artsy-and-cute/). Stålberg: "The shape of the terrain really matters. It creates different choke points and different ways you need to position your units." Meredith: "Each unit is simulated. They're part of a squad, but they're making choices as individuals, not as a squad." Corroborated by the [Nintendo UK developer interview](https://www.nintendo.com/en-gb/News/2018/April/Interview-Taking-on-hordes-of-invading-Vikings-in-Bad-North-1368315.html): "players will be simply positioning their squads on a grid and then each of the units in that squad decide how/when to attack from there."
<a id="s45"></a>45. Unity 6 — [SRP Batcher](https://docs.unity3d.com/6000.0/Documentation/Manual/SRPBatcher.html). "A draw call optimization that significantly improves performance for applications that use an SRP"; "reduces the CPU time Unity requires to prepare and dispatch draw calls for materials that use the same shader variant"; supported on URP, HDRP and custom SRPs, not Built-In. **Says nothing about `SkinnedMeshRenderer`.** *(Read directly.)*

**Secondary — community wikis, forums and encyclopaedias. Used for structure, never for numbers.**

<a id="s23"></a>23. [Kingdom Rush](https://en.wikipedia.org/wiki/Kingdom_Rush), Wikipedia. "The player can choose between four types of towers to place: archers, mages, artillery, and barracks"; "barracks training soldiers that attack in melee and slow down enemies."
<a id="s28"></a>28. Kingdom Rush — [Melee Towers](https://kingdomrushtd.fandom.com/wiki/Melee_Towers), [Upgrades](https://kingdomrushtd.fandom.com/wiki/Upgrades). **⚠ HTTP 402 to automated fetch; read only as search-engine snippets.** Three soldiers per barracks across tiers; the *Improved Deployment* meta-upgrade improving rally-point range by 20%. **Low-to-medium confidence; do not quote a number from this.**
<a id="s31"></a>31. They Are Billions — Steam Community threads on [horde pathfinding](https://steamcommunity.com/app/644930/discussions/0/2549465882918844784/) and [the purpose of walls](https://steamcommunity.com/app/644930/discussions/0/4839771533593418120/). **Directly contradictory**: one asserts "The game does not attempt to path zombies around walls, nor does it check for holes in your walls"; another insists "Zombies 100% walk towards gaps in the walls." Cited *as* a disagreement, not as a fact.
<a id="s34"></a>34. Orcs Must Die! Deathtrap — [Traps](https://orcsmustdie.wiki.gg/wiki/Traps), wiki.gg. Floor, wall and ceiling as distinct placement classes, and a killbox mixing all three.
<a id="s44"></a>44. Age of Empires II — [Garrison](https://ageofempires.fandom.com/wiki/Garrison), community wiki. Town Centre, Watch/Guard Tower, Keep, Bombard Tower and Castle fire extra arrows per garrisoned unit, scaling with that unit's attack and rate of fire. **⚠ HTTP 402; read only as search-engine snippets, and one claim in them contradicts common play experience. Treat as unverified.**
<a id="s46"></a>46. Sanctum 2 — [Postmortem](https://www.gamedeveloper.com/business/postmortem-sanctum-2), Johannes Aspeby, Coffee Stain Studios (**primary**, retained from the superseded blocking analysis and no longer load-bearing). Sanctum 1 refused a build that sealed the maze; the free-form wall-drawing system for Sanctum 2 was "scrapped… half a year into development time." Noted here only because it is the best available evidence that ambitious wall systems are expensive, should the question ever return.
</content>
