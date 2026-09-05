# Tower & Creep Variance Levers, and the Schema That Holds Them

**Part V** · Design & data architecture · 30 July 2026

**Subject:** Async ghost round-robin tower defense
**Input:** [Technology Stack Assessment](tech-stack-assessment.md) (Part III) — deterministic integer sim, no engine types
**Output it feeds:** the unit schema, and the build helper that queries it

> ### 📦 Archived — superseded as a plan, and the most load-bearing of the five
>
> **Its schema verdict stands and is now being built against**: one unit with two roles, levers as components,
> the vocabulary versioned apart from the numbers.
> [Seam 3](../build-order.md#3--the-roster) fills it in. One thing has changed under it since it was written:
>
> - **The six mazing-dependent levers are alive again** — `Path policy`, `Repath trigger`, `blocksPath`, the
>   maze/gun resource split, geometry-driven stats and route choice. They were written off when the corridor
>   was one hex wide and never branched; that was withdrawn 6 August 2026. See
>   [the maze reversal](../vision.md#the-board-is-a-maze). Where the text below
>   calls them dead weight, they are not.
>
> §3.11's Element TD 2 figures were corrected in place by
> [#60](https://github.com/ssalter21/tower-defense-game/issues/60) — 59 towers, not combinatorially complete —
> so no correction is outstanding.

> **What makes one tower different from another tower?**
>
> Part III concluded that balance becomes a computation once the simulation is a separate integer library. That
> is only true if the things being balanced are *described* rather than *coded*. This is the catalogue of every
> axis a tower or a creep can vary along, and the data structure that has to survive all of them.

---

## Bottom line

### Stop modelling towers and creeps as two things. Model one unit, with two roles, described by components rather than fields — then version the numbers separately from the vocabulary.

Three claims, in the order they matter.

**One.** In this game the player authors both halves. A defense is a set of placed units; a wave is a set of
moving units; they fight each other. Almost every lever that makes a tower interesting has a mirror that makes
a creep interesting — armor type, damage type, auras, statuses, on-death effects, targeting. The genre's habit
of writing `Tower` and `Enemy` as separate types is inherited from single-player games where only one side was
authored. It does not fit an attack-and-defend loop, and it doubles every balance change forever.

**Two.** A unit is a **bag of components**, not a struct with sixty mostly-null fields. Every lever in section 3
is a component that is either present or absent. This is not an aesthetic preference — it is what makes the
build helper possible, because "which units have a `SplashDamage` component" is a query, and "which units have
a non-zero `splashRadius` field" is a full table scan over meaningless zeroes.

**Three, and this is the future-proofing answer:** the schema has **two layers that change at different rates**.
The *vocabulary* — the set of lever kinds — must be append-only and effectively permanent. The *ruleset* — the
concrete units and numbers — changes every patch and is content-hashed. A stored ghost pins a ruleset hash. The
build helper reads the vocabulary. Conflating the two is why balance patches break replays.

> **The one thing that inverts the obvious advice**
>
> Every schema-evolution guide tells you to **tolerate unknown fields** — Protocol Buffers is built around it,
> and it is correct advice for messages. It is exactly wrong here. Silently ignoring a lever you don't
> understand does not degrade a deterministic replay gracefully; it produces a *confidently wrong* result that
> still validates. A ghost referencing an unknown lever must **refuse to replay**, loudly. Schema tolerance is
> a property you want in your network layer and must not have in your simulation.

---

## 1. What the design has already decided

| From Parts I–III | Constraint on the schema |
|---|---|
| Deterministic integer sim | No floats anywhere in a unit definition. No percentages as `0.15`. No `float` in an interchange format that a designer can type into. |
| Server re-runs the record | The schema must be readable by the sim library alone — no engine types, no `Vector3`, no `AnimationCurve`, no `ScriptableObject` in the sim's view of a unit. |
| Ghosts replay for years across patches | Unit definitions must be versioned as a set, not individually, and old sets kept forever. |
| Players author waves as well as defenses | Creeps are content the player composes, so creep levers need the same expressive range as tower levers — and the same cost model. |
| Round-robin exists to control variance | Levers that re-inject variance (crit chance, evasion) fight the thing Part II built the format around. See section 10. |
| Balance is a computation | Levers must be enumerable and queryable offline, which pushes hard toward declarative effects and away from per-unit code. |

---

## 2. The decisive move: one unit, two roles

The north star already does this, and its public API proves it rather than merely suggesting it. Every unit in
Legion TD 2 — fighter, wave creep, and mercenary alike — is returned by the same endpoint with the same field
set, and the thing that distinguishes them is a single `legion` enum whose values include `Mech`, `Forsaken`,
`Grove`, `Element`, `Divine` **and also `Creature` and `Mercenary`**. The creeps you fight and the mercenaries
you send are not a different type of object with a parallel schema; they are two more factions in the same list.
There is no `Tower` class in a Legion-shaped game. There are units that stand still and units that walk.

That generalises cleanly:

| | Placed role ("tower") | Moving role ("creep") |
|---|---|---|
| Position | Fixed at build time | Derived from pathing each tick |
| Usually has | `Attack`, `Placement`, `BuildCost` | `Movement`, `Pathing`, `Bounty` |
| Usually lacks | `Movement` | `Placement` |
| Shares | health, armor, damage types, targeting, auras, statuses, on-death effects, tags, scaling, immunities | — |

Every entry in the "shares" row is a component neither role owns. And the exceptions are the interesting part
of the design space, not edge cases to be excluded:

- A **moving tower** is a repositionable defense, or a patrolling blocker.
- A **placed creep** is a summoner's totem the attacker drops on the map, or a siege engine that stops to fire.
- A **creep that attacks towers** is one of the four shipped answers to "the player has fully blocked the path",
  alongside refusing the placement, auto-selling the offending tower, and zeroing the creep's collision so it
  walks through. It is the *minority* choice — Warcraft 3 does not do this by default at the engine level, it is
  a mapmaker's trigger — and it only arises at all if the player authors the geometry. **In a preset-path
  design it has no justification whatsoever.** See the mazing-versus-preset-path question in the
  [the maze reversal](../vision.md#the-board-is-a-maze); this lever, `blocksPath` (3.9) and `Path policy` (3.5) all stand or fall with it.
- A **tower with bounty** is a defense that pays its attacker for killing it.

A schema that treats these as special cases will need surgery for each one. A schema where they are the natural
result of attaching a component you already had needs none.

> **The naming consequence**
>
> If both roles are `Unit`, the domain language has to change with it. "Tower" and "creep" survive as *roles* in
> conversation and UI, not as types in the sim. The sim has `UnitDef`, `UnitInstance`, and a `role` tag that is
> a filter, not a class.

---

## 3. The lever catalogue

Thirteen groups. The claim is not that every one of these ships — most should not, per section 10 — but that
the *vocabulary* has a place to put each of them, so that adding one later is a new component rather than a
migration.

### 3.1 Identity and classification

| Lever | Shape | Notes |
|---|---|---|
| Stable ID | string | Never an array index. Never reused. See section 8. |
| Tags / keywords | set of string IDs | **The universal extension point.** `flying`, `armored`, `boss`, `mechanical`, `summoned`. Every counter-relationship that isn't a full matrix should be expressed here. |
| Role | `placed` \| `moving` | A filter, not a type. |
| Damage type | tag from a closed set | Interacts with armor type; see section 4. |
| Armor type | tag from a closed set | " |
| Collision size | integer units | Affects splash overlap, blocking, and formation density. |
| Faction / tier / family | tag | For upgrade legality and draft rules. |

### 3.2 Offense

| Lever | Shape | Why it varies play |
|---|---|---|
| Base damage | int | |
| Damage roll | min/max, or base + N dice of M sides | Warcraft 3's native form. A determinism hazard unless the RNG stream is seeded per attack — see 10.2. |
| Attack cooldown | integer ticks | |
| Windup / attack point | integer ticks before damage lands | Separates burst from sustained. Also the difference between a unit that gets a shot off before dying and one that doesn't. |
| Backswing / recovery | integer ticks | |
| Range, minimum range | integer units | Minimum range is an underused lever — it makes a tower bad at leaks and good at chokes. |
| Firing arc + turret turn rate | degrees, degrees/tick | Slow traverse is what makes a heavy tower miss fast creeps without any explicit accuracy stat. |
| Delivery | melee / projectile / hitscan / beam / continuous / persistent field | |
| Projectile speed, arc, homing | int, enum | Non-homing projectiles create whiff behaviour that is *deterministic but reads as skill*. |
| Pierce / pass-through count | int, plus falloff per unit hit | |
| Chain / bounce | count, range, damage falloff per bounce | |
| Shots per attack, spread, salvo timing | int, degrees, ticks | Sequential vs simultaneous matters for overkill. |
| Area shape | point / circle / annulus / cone / line / arc-band | Warcraft 3's three-ring splash (full, medium, small radius) is the most copied form; a single radius is the most common simplification. |
| Splash falloff | per-ring percentage, integer | |
| Friendly-fire flag | bool | |
| Overkill policy | carries / wasted | Decides whether high-damage-slow beats low-damage-fast against small creeps. |
| On-hit riders | list of effect refs | Apply status, lifesteal, armor shred, execute-below-threshold, bonus-vs-tag, cleave. |
| Conditional damage | predicate + modifier | Bonus vs tag, ramp on repeat target, first-hit bonus, falloff by distance. |
| Damage cap as a fraction of target max health | integer percentage | Mindustry's `maxDamageFraction`. The clean anti-one-shot lever: it lets a weapon be enormous against small things and merely good against a boss, without a separate boss-damage stat. |
| Sub-munitions | child projectile def + count + spread + trigger | Fragments on hit, on despawn, on absorption, or **on an interval while still flying** — three different triggers that produce three different weapons from one parameter set. |

> **A projectile is a unit too**
>
> Mindustry's larger missiles are not projectiles with extra fields; they are **units** — with health, turn
> rate, acceleration, their own weapon, and their own death effect — which means point-defense turrets can shoot
> them down and a shield can absorb them. It got there by the same route this document argues for: once
> "projectile" and "unit" both need health and targeting and status effects, keeping them separate costs more
> than merging them.
>
> This is the strongest available corroboration of section 2. The unification is not two categories (tower and
> creep) but three, and the third one arrives on its own if you let it.

### 3.3 Targeting

Targeting deserves its own group because it is the lever players *feel* most and schemas model worst.

| Lever | Shape |
|---|---|
| Acquisition priority | scoring rule — see below |
| Targets-allowed filter | tag predicate — ground, air, invisible, structures, summoned, self, allies |
| Simultaneous target count | int |
| Retarget policy | sticky until death / re-evaluate every shot / re-evaluate every N ticks |
| Retarget hysteresis | a separate, longer interval for switching *off* a live target than for acquiring one |
| Lock-on / spin-up | ticks before first shot at a new target |
| Lead prediction | none / linear / exact |
| Per-emitter priority | independent priority per barrel/arm, where a unit has several |
| External designation | this unit prefers targets *marked by another unit* |
| Player-overridable | bool — whether the priority is exposed in the build UI |

**The choice worth thinking hardest about is whether a priority is an enum or a scoring function**, because the
two produce visibly different games and the schema shape follows from the answer.

- **Enum.** Bloons' "Strong" is a fixed *rank order over bloon types* — Boss above BAD above ZOMG above Ceramic
  — and within a rank, current health is irrelevant; the tie goes to whoever entered first. It is completely
  predictable and completely insensitive to how damaged something is.
- **Scoring function.** Mindustry's is a scalar cost, and the composition is the point: `strongest` evaluates
  `-maxHealth + distance² / 6400`. The divisor is `80²`, ten tiles — so health dominates the choice nearby and
  distance takes over further out. One expression yields "prefers big things, but not stupidly far away," which
  an enum cannot express without a second enum.

A scoring function is the more future-proof shape and it costs one thing: the terms must be integers combined in
a stated order, or the sort is not deterministic. Mindustry also ships a *conditional* priority — one that adds
a bonus for targets currently standing in water — which an enum cannot represent at all.

Two further points from shipped games worth stealing outright:

- **Stack several criteria.** Rogue Tower lets each tower carry **up to three ordered priority criteria** drawn
  from progress, near-death, most/least health, most/least armor, most/least shield, slowest, fastest, and
  *marked*. Lexicographic ordering over a small set beats a large enum of pre-combined modes.
- **Let one unit designate for another.** Rogue Tower's Lookout exists to mark targets that other towers then
  prioritise. That is a support role built entirely out of the targeting system, with no damage involved, and
  it costs the schema exactly one tag.

"First" and "last" are only meaningful if the sim has a canonical notion of path progress, which is itself a
schema decision: **path progress must be an integer distance-along-path, not a position**, or two creeps at the
same coordinate on different lanes cannot be ordered deterministically.

### 3.4 Survivability

| Lever | Shape | Notes |
|---|---|---|
| Max health | int | |
| Health regeneration | int per tick, plus out-of-combat delay | |
| Armor value | int | The *formula* is a ruleset-level decision, not a per-unit one. See 4.2. |
| Armor type | tag | |
| Per-damage-type resistance | integer percentage per type | The OpenRA/Wesnoth form: a unit carries its own resistance vector rather than deriving it from a class. Strictly more expressive than an armor-type matrix, and strictly harder to balance. |
| Shield pool | separate hit points, recharge rate, recharge delay, optional per-type applicability | Shields are the richest sub-lever here. Mindustry ships a regenerating bubble, a **directional arc shield with a facing, a deflection chance and outright bullet reflection**, and a shield that a unit grants to its *neighbours*. Infinitode 2's shield does not absorb damage at all — it **blocks every movement-slow debuff** and reduces damage from specific delivery types. "Shield" is a family, not a stat. |
| Flat damage reduction | int subtracted after multipliers | Creates hard counters to many-small-hits. |
| Damage cap per hit | int | Creates hard counters to few-big-hits. |
| Minimum damage floor | int | Prevents unkillable-by-arithmetic. Non-optional in an integer sim. |
| Immunities | tag set — to statuses, to damage types, to targeting | |
| Invulnerability window | ticks, trigger | |
| Evasion / miss chance | percentage | Listed for completeness; argued against in 10.2. |
| Damage-taken modifier | percentage, conditional | Vulnerability marks, wet/oiled states. |

### 3.5 Movement

| Lever | Shape | Notes |
|---|---|---|
| Base speed | integer units per tick | |
| Acceleration, turn rate | int | |
| Speed clamps | min/max after modifiers | Essential: a slow stacking to zero is a stun with no counterplay. |
| Movement type | ground / flying / hovering / burrowing / phasing / teleporting | Determines which levers of section 3.9 apply at all. |
| Collision behaviour | blocks others / passes through / pushes | |
| Path policy | shortest-path-recomputed / fixed-lane / attacks-blockers / flees-when-damaged / seeks-highest-value-target | **The maze-legality lever.** |
| Repath trigger | on build / on block / never | |
| Movement modifiers | dash on cooldown, burst on damage taken, enrage below health threshold, slow-down near the exit | |

### 3.6 Wave and spawn — the composition half

These are creep-only, and in this game they are *the player's build*, so they need the same care as tower levers.

| Lever | Shape | Notes |
|---|---|---|
| Composition | list of (unit def, count) | |
| Spawn interval and jitter | integer ticks | Jitter must be seeded, not sampled. |
| Spawn order | explicit sequence | Ordering *is* strategy: tanks-first vs swarm-first changes which towers overkill. |
| Lane / entrance | id | |
| Group / squad | ties units to a shared leader or aura | |
| Cost | integer budget spend | Legion TD 2's mythium is the reference model: sending costs a currency that competes with your own economy. |
| Income granted to sender | int, and on what condition | |
| Bounty granted to defender | int per kill, int per wave cleared | |
| Leak consequence | damage to defender, refund to attacker | |
| Wave-scoped modifiers | an aura attached to the wave rather than a unit | "This wave has +20% health" is a lever that must not require editing unit defs. |
| Route choice | ordered list of path segments, where the map offers more than one | The attacker's mirror of mazing. |
| Map-placed attacker abilities | position + effect ref + trigger | See below. |
| Composition constraints | predicate over the composition itself | Infinitode 2 attaches these to the *creep*: some units only ever spawn in clusters, some never do, some may not appear alongside a healer. A legality rule that lives on the unit rather than the wave. |

The one genre that has thought hardest about the attacking side is the inverted tower defense, and **Anomaly:
Warzone Earth** is worth reading directly because its whole design is the half of our loop that has no
precedent in ordinary TD. Three of its levers transfer cleanly:

- **Convoy ordering is a first-class decision.** Its squad moves as a single-file column and the player reorders
  it; whoever is at the front absorbs fire first. That is the attacker's exact analogue of tower placement, and
  it costs the schema nothing but an ordered list — which section 3.6 already has as "spawn order". Worth
  recognising that the ordering *is* the build, not a detail of it.
- **Route authoring.** The player draws the path through the defended grid, choosing which towers to walk past
  and how often.
- **Abilities placed on the map rather than on units.** Repair zones that heal anything passing through, smoke
  that debuffs tower accuracy in an area, and a **Decoy that draws fire from every tower in range** — a taunt
  placed on the ground rather than carried by a unit. Anomaly's sequels then give the *defending* towers the
  same ability grammar (Regen, Taunt, Berserk, Kamikaze), which is the strongest available evidence that one
  vocabulary really does serve both roles.

A map-placed ability is, in schema terms, a unit with `Placement`, no `Attack`, an `Aura`, and a lifetime — which
is to say the "one unit, two roles" model already covers it and no new concept is needed.

### 3.7 Support, auras, and adjacency

| Lever | Shape |
|---|---|
| Aura radius | int, or global |
| Affected filter | tag predicate + friendly/hostile |
| Granted modifiers | list of stat modifiers or status refs |
| Stacking rule | none / refresh / independent / highest-only / intensity-capped |
| Conditionality | active only while charged / while target present / while below health |
| Adjacency bonus | predicate over neighbouring placements + modifier |
| Link / channel | one unit feeding another (power, ammo, buff) |
| Sacrifice / consume | destroys a unit to empower another |
| Global effect | applies to the whole board; the lever most likely to break the build helper's incremental evaluation |

**An aura can grant a capability rather than a stat, and that is the more interesting half.** Bloons' support
tower has one branch that hands out numbers and another that hands out *abilities*: one upgrade grants camo
detection to everything in radius, another suppresses enemy regeneration, and a third grants the "Normal" damage
type — which, given that damage types there are just bitmasks of what you cannot hurt (4.3), means it clears
every immunity gate at once. A capability aura converts a hard counter into a soft one for as long as it stands,
which is a far sharper strategic object than +10% damage.

Two other refinements from the same tower worth having in the vocabulary: buffs can be **class-gated** (affecting
only units carrying a given tag), and support effects can be **economic** rather than combat — a discount on
nearby construction, with an explicit cap on how many such auras stack.

Adjacency deserves a flag: it turns a build from a set into a graph, which means the build helper can no longer
evaluate a tower's value independently. That is a *feature* — it makes layout matter — but it is the single
largest complexity multiplier in the helper, and it should be a deliberate choice rather than a drifted-into one.

### 3.8 Status effects

Statuses are their own vocabulary, referenced by both attack riders and auras. Every status carries the same
envelope; only the payload differs.

| Envelope field | Shape |
|---|---|
| Magnitude | int (or per-mille) |
| Duration | integer ticks |
| Tick rate | integer ticks, for periodic payloads |
| Stack rule | none / refresh-duration / independent-instances / intensity-max / additive-capped |
| Max stacks | int |
| Diminishing returns | rule ref — see 5.3 |
| Source attribution | unit instance id, for stacking and for "who gets the kill" |
| Dispellable | tag |

| Payload family | Examples |
|---|---|
| Movement | slow, root, stun, freeze, knockback, pull, teleport |
| Offense | attack-speed slow, silence, disarm, damage amp/reduction |
| Damage over time | burn, poison, bleed — differing in whether they stack independently |
| Defense shred | armor reduction, resistance reduction, shield break |
| Marks | vulnerability, priority-target flag, tracking |
| Transform | polymorph, phase change, split trigger |
| Reward | bonus gold or experience from the affected unit — a debuff that changes payout, not combat |

**A status is a vector of multipliers, not a category.** Mindustry's `StatusEffect` is the cleanest shipped
proof: one class carries `damageMultiplier`, `healthMultiplier`, `speedMultiplier`, `reloadMultiplier`,
`dragMultiplier`, a `disarm` flag, *and* two independent damage-over-time channels (per-tick and per-interval).
Its `melting` is simultaneously a slow, a fragility multiplier and a DoT; its `electrified` is a slow plus an
enemy fire-rate reduction. Nothing in the schema decides that "slow" and "burn" are different kinds of thing —
they are different vectors. Resist the urge to make `SlowEffect` and `DotEffect` separate types.

#### Status–status interaction, as data

Freeze-then-shatter, wet-then-conduct, oiled-then-burn. These are not `if` chains; they are three separate
relations, and Mindustry ships all three explicitly:

| Relation | Meaning | Example |
|---|---|---|
| **Opposite** | Applying A cancels B outright | wet cancels burning; slow and fast cancel each other |
| **Affinity / transition** | A meeting B fires a handler that deals burst damage *and may write a third effect* | wet + shocked → 14 burst damage; burning + tarred → burning's duration is extended, capped |
| **Reactive-only** | An effect that exists solely as a reagent and can never be applied on its own | `shocked`, `blasted` |

The third one is the non-obvious one. A reagent-only status lets a lightning tower say "I apply `shocked`" and
have that mean *nothing at all* except in combination — which is a clean way to build combo mechanics without
every tower needing to know about every other tower.

#### Duration versus magnitude — two envelopes, not one

The envelope above assumes a status has a *duration*. There is a second shape worth having in the vocabulary,
because it solves the slow-stacking problem for free.

Rogue Tower's Slow, Haste and Fortification are **magnitudes that decay**, not timed effects: each accumulates
toward a cap of 60, decays at 6 per second, and its strength is proportional to the current amount. Slow is
gained in proportion to damage taken, so slowing is a *byproduct* of shooting rather than a separate apply. That
model is naturally self-limiting, needs no stack counting, and makes "raise the slow cap" and "lower the enemy's
haste cap" available as upgrades — both of which Rogue Tower ships.

Timed and decaying statuses are different enough that they should be different components rather than one with
a mode flag.

Two more stacking shapes seen in the wild, both worth having (5.2, 5.3):

- **Per-source caps** — Infinitode 2's poison stacks freely across towers but only once per tower; Bloons'
  Acidic Mixture Dip stacks to an explicit numeric cap that has been renumbered twice in patches, which is an
  argument for the cap being a ruleset number rather than a code constant.
- **Lifetime budgets** — Infinitode 2 allows a given enemy to be snowballed only **six times, ever**. A budget
  that persists across applications is a different thing from a cooldown, and it is the strongest available
  answer to permanent chain-crowd-control.

### 3.9 Placement and space

| Lever | Shape | Notes |
|---|---|---|
| Footprint | integer cells, shape mask | |
| Rotation | allowed orientations | |
| Terrain restriction | allowed tile tag set | |
| **Surface class** | floor / wall / ceiling / lane | Orcs Must Die! types every trap by mounting surface, so three independent placement spaces occupy the same corridor and a good killbox needs all three. It is the cheapest way to make a corridor hold more towers without making it longer. |
| Blocks pathing | bool | The mazing lever. |
| Elevation | int | Rogue Tower pays elevation in damage and range — **and only lets adjacency bonuses connect tiles at the same level**, so the two spatial bonuses are in direct tension. A placement lever that trades against another placement lever is worth more than either alone. |
| Line of sight | bool — whether LoS gates targeting | |
| Repositionable | bool, cost, cooldown | |
| Uniqueness / count limits | per-board maximum | |
| Attachment | can be built on/into another unit | |
| Geometry-driven stats | stat scales with covered path tiles / drawn length | Rogue Tower scales two towers' fire rate by how much path they overlook; Dungeon Defenders charges its drawable beams a budget cost proportional to their length. Placement stops being a yes/no and becomes a continuous input. |

Two structural decisions in this group are worth taking deliberately rather than inheriting:

**Separate the maze from the guns.** Sanctum spends one resource on tower *bases* — pure geometry, no combat —
and another on the towers built atop them, and both stay mutable between waves. Splitting path-lengthening from
damage means the two can be balanced independently, and it removes the usual failure where the cheapest tower
becomes the best wall.

**Blocking bodies are a different lever from blocking buildings.** Kingdom Rush's barracks spawn soldiers who
physically stop enemies, and the interesting part is that blocking is **opt-in and repositionable**: the player
moves a rally point to choose *which* enemies get intercepted, and can deliberately let a dangerous one past.
The cost is a respawn timer rather than gold, so the resource being spent is tempo. In our terms this is a
placed unit whose `OnDeath` is a timed respawn and whose position is player-steerable each round — again, no new
concept, just components the schema already has.

### 3.10 Uptime, ammo, and charge

The group most often missing from first-draft schemas, and the one that produces the most distinct *feel* per
tower.

| Lever | Shape |
|---|---|
| Ammo capacity, reload time | int, ticks |
| Ammo type | which item/resource it consumes, with per-type damage overrides |
| Power / energy draw | int per tick, with brownout behaviour |
| Heat and overheat lockout | int, threshold, cooldown |
| Spin-up | fire-rate ramp curve as an integer table |
| Charge accumulation | charges gained per tick, spent per shot |
| Active ability | cooldown, cast time, auto-cast vs manual |
| One-shot consumable | destroyed on use |
| Duty cycle | fire N ticks, idle M ticks |

Two refinements from Mindustry, which has the most developed version of this group in any shipped tower game:

- **Ammo type should be able to change the weapon, not just feed it.** Its item turrets map *item → projectile
  definition*, and the projectile carries its own `reloadMultiplier`, `rangeChange` and `ammoMultiplier`. The
  starter turret firing copper is a fast weak gun; the same turret firing graphite is slower, stronger and
  longer-ranged; firing silicon it is faster, weaker and homing. One tower, three genuinely different weapons,
  chosen by logistics rather than by an upgrade. Ammo is the only lever in this catalogue that lets a player
  re-role an already-built tower.
- **A consumable input can be a rate multiplier rather than ammunition.** Its coolant liquids do not become
  projectiles; they multiply reload speed. This is a distinct and underused lever: an input that improves a
  tower without being consumed *per shot*, so it scales with sustained fire rather than with burst.

Note that an ammo or power lever makes a tower's DPS **dependent on the rest of the board**, which is the same
graph problem adjacency creates — and unlike adjacency it is dependent on board *state over time*, not board
layout, so the build helper cannot answer it with a static query at all.

Finally, uptime applies to buffs as much as to weapons, and the best-tuned example in the genre is worth
recording because it uses four levers at once. Bloons' Berserker Brew potion **expires after five seconds or
twenty-five attacks, whichever comes first**, is thrown at *the nearest tower not currently buffed and not on
cooldown*, and cannot be re-applied to the same tower for five seconds after application. Dual expiry means a
fast tower extracts more total value but burns the shot budget sooner; the selection rule means the buff
spreads rather than concentrating; the lockout means it cannot be chain-stacked. Any one of those levers alone
would produce a degenerate result.

### 3.11 Economy and upgrade topology

| Topology | Shipped example | Schema shape |
|---|---|---|
| Linear tier chain | most classic TD | ordered list of defs |
| Branching paths with a crosspath cap | Bloons TD 6 | N paths × M tiers, plus a legality predicate |
| Combination / recipe | Element TD | `(inputs) → output` recipe table |
| Fusion with continuous grade | Gemcraft | grade int + trait vector |
| Merge-N | match-merge TDs | recipe table, degenerate case |
| Tech tree unlocks | Mindustry, Dungeon Defenders | prerequisite graph over defs |
| Per-instance experience | Infinitode, Defender's Quest | **replay hazard** — see 10.5 |

Plus the flat levers: build cost, sell refund percentage, upgrade cost curve, build time, supply budget cost,
and income generation.

Upgrade topology is where "future proof" is won or lost. The generalisation that covers all of the above is
**an upgrade is an edge in a directed graph over unit definitions, guarded by a predicate, priced by a cost
expression.** Every topology in the table is a shape of that graph. Model the graph, not the shape.

Bloons is the proof that the predicate has to be real rather than decorative. Its rule is that a tower has three
paths of five tiers, but **at most two paths may be touched at all, and at most one may exceed tier two** — so
`5-2-0` is legal and `3-0-3` is not, and only seven of the fifteen upgrades are ever reachable on one tower. The
community's own analysis is that this constraint, not the upgrades themselves, is what makes the game's build
variety. Two further wrinkles that a naive "upgrades are a tree" model cannot express: the bonus a tower gets
from its secondary path is *not uniform* — the same crossbow gains sixteen extra pierce from one secondary tier
and twenty-three from the next — and several endgame towers break the topology entirely by consuming other
towers as a cost.

The combination topologies deserve two specific warnings, because both have a shipped failure and a shipped
success.

**Combination can be made *nearly* combinatorially complete, and that is a real design.** Element TD 2 has six
elements and ships **exactly one tower for every subset of size one to four** — six singles, fifteen duals,
twenty triples, fifteen quads (the quad tier arrived in v1.4, December 2021) — plus the **Periodic Tower** for
the full set of six. That is fifty-seven combination towers, zero of them arbitrary, and a player who knows the
elements can predict what exists. The upgrade graph then trades tier against level: a level-1 single upgrades
into any dual containing it, a level-1 dual into any triple containing both. It is the cleanest answer in the
genre to "how do I generate a large roster without inventing a hundred units", and it falls straight out of
modelling upgrades as a guarded graph.

**But the lattice has a hole, and it is the interesting part: there is no five-element tower.** Six of the
sixty-four subsets have no unit. The shipped roster is **59**, not 57, because Arrow and Cannon are always
available, deal Composite damage, and sit outside the combination scheme entirely. So even the genre's cleanest
generative roster declined to fill its own lattice — which is worth knowing before committing to one, since
"the rule generates everything" is the whole appeal and this is the shipped counter-example.

> **★ Corrected by the build-depth research, [summarised in open questions](https://github.com/ssalter21/tower-defense-game/blob/main/docs/open-questions.md#what-the-design-research-found).**
> The prediction property is what makes a generative roster teach itself, and a hole costs exactly one exception
> to memorise — cheap. What it does mean is that **"generated by a rule" and "complete under the rule" are
> separate decisions**, and the shipped example takes the first without the second. No first-party statement
> explains the missing quintuple tier.

**Merge topologies need their combination arithmetic checked for a fixed point.** Gemcraft combines two gems
into one with published per-stat coefficients, and the original game's rule for a large grade gap was
`result = A + 0.25·B` — the larger stat is *preserved*, not averaged. That single choice created "supergemming":
feeding cheap gems into one good gem raised its damage without bound, which became the dominant strategy and
had to be killed in the sequel by making the same case diminishing. The lesson generalises to any merge or
sacrifice mechanic: **if the output can equal or exceed the better input, the mechanic is an unbounded loop**,
and that property is visible in the coefficients before anything ships. It is exactly the kind of thing a build
helper should be able to check automatically.

Three economy levers exist specifically to shape *build variety* rather than power, which makes them unusually
relevant to a format where the best defense gets copied by everyone:

| Lever | Shipped as | Effect |
|---|---|---|
| Count-scaled pricing | Rogue Tower — a tower's price rises with how many of that type are already placed, and falls again on demolish | Taxes monoculture without nerfing anything |
| Diversity bounty | Rogue Tower — a monster pays **+1 gold for every distinct tower type that damaged it** | Pays for variety directly |
| Stacking discount caps | Bloons — a village discounts nearby towers, and stacks from only three villages before capping | Bounds a compounding economy lever with a number rather than a rule |

And two budget shapes that are not gold: a **global placement budget** independent of currency (Dungeon
Defenders spends both mana *and* a per-map Defense Unit cap, so cost is two-dimensional), and a **recharge
timer per unit type** independent of price (Plants vs. Zombies, where a free unit with a long cooldown and an
expensive unit with a short one are different tools). Either makes "what can I afford" a richer question than a
single subtraction.

### 3.12 Scaling

| Lever | Shape | Notes |
|---|---|---|
| Per-level stat growth | **integer table, one row per level** | Not a formula. Tables are exactly tunable, trivially deterministic, and diffable in review. A formula saves bytes you do not need to save. |
| Wave-number scaling | table indexed by wave | |
| Scaling from an external stat | reference + integer coefficient | Dungeon Defenders' hero-stat model. Powerful, and it couples the ghost to the owner's account state — a replay hazard. |
| Soft caps and diminishing curves | table + rule ref | |

### 3.13 Stateful and conditional behaviour

| Lever | Shape |
|---|---|
| Phase transition | health threshold or timer → swap to another def, with health-carry policy |
| On-death effect | spawn children, explode, heal allies, refund, curse the killer |
| Triggered reactions | on-kill, on-damaged, on-ally-death, on-wave-start, on-leak, on-built |
| Memory | per-instance counters the sim owns (shots fired, same-target streak) |
| Environmental gates | day/night, weather, board state |

On-death child spawning is worth calling out as a first-class lever rather than a special case, because in
Bloons it is not *a* lever — it is the entire enemy model. Every bloon is a node in a linked list: one hit
point of its own plus a child type and a child count, so a Ceramic is ten hit points wrapping two Rainbows
wrapping four Zebras, and the whole tree collapses to a single number the community calls RBE — 104 for a
Ceramic, 616 for a MOAB, 55,760 for the largest. Three consequences worth copying:

- **The wave's difficulty is a computed property**, not an authored one, which is exactly what a build helper
  wants: total effective health falls out of the composition instead of being a designer's guess.
- **Overkill cascades.** Excess damage carries down the chain in the same hit rather than being wasted, which is
  the single decision that determines whether high-damage-slow or low-damage-fast wins against layered enemies.
  Have an explicit answer for it; the "overkill policy" row in 3.2 is that answer.
- **Modifiers on a layered enemy need an inheritance rule.** Bloons' Fortified doubles the outer layer's health
  and then does *not* pass to children — except on the largest class, where it does pass down until a stated
  layer. That rule is arbitrary, it is load-bearing, and it is the kind of thing that must be data
  (`inheritToChildren: bool`, `inheritDepth: int`) or it will be four `if` statements in three places.

The mirror-image lever on the support side is a creep that produces creeps: a continuous spawner, or a unit
whose death spawns a fixed count with a spread. And creeps take the full support vocabulary too — Mindustry
gives units a damage-reduction field, a healing field, a field that grants an *attack-speed buff* to nearby
allies, and one that suppresses the enemy's healing. Every one of those is a tower ability pointed the other
way, which is the argument of section 2 restated in content.

---

## 4. The interaction layer — four mechanics, not one

The most common schema mistake in this genre is assuming the tower-versus-creep interaction is *one* system. It
is four. They compose, they fail differently, and shipped games pick different subsets — which is the clearest
evidence available that a schema needs room for more than the one you start with.

### 4.1 The scalar layer — three shapes, pick exactly one

How much damage, as a multiplier. Three structurally different answers ship today.

| Shape | Who | How it works | Costs |
|---|---|---|---|
| **Multiplier matrix** | Warcraft 3 and its whole TD lineage; Legion TD 2; Infinitode 2 | `damageType × armorType → integer percentage` | Quadratic to balance. Infinitode 2's is a full 16-tower × 12-enemy grid, and it stays legible only because every cell is drawn from a fixed set of seven values — 0, 10, 25, 50, 75, 100, 150. |
| **Resistance vector** | OpenRA, Battle for Wesnoth, Kingdom Rush | Each unit carries its own per-type percentages rather than deriving them from a class | Strictly more expressive, strictly harder to balance, and it cannot be displayed as one table. |
| **Sequential pools** | Rogue Tower | A unit has **Shield, then Armor, then Health** — depleted strictly in order — and every tower carries a separate damage multiplier for each pool | The interaction changes *during* a single kill. |

The third is the least known and the most interesting. Because the pools deplete in order, a tower that is bad
against shields and excellent against health is not "average" — it is a finisher, and the player can feel the
handoff. It is a **type chart on the time axis**, and it is the cheapest way to make two towers with identical
DPS play differently. It also composes naturally with damage-over-time: Rogue Tower's Bleed, Burn and Poison
each halt regeneration on exactly one pool, so each DoT is *the* answer to one regenerating layer.

Pick one shape. Shipping two means every unit has two independent knobs doing the same job (see 10.3).

#### How wide should the matrix be? Three shipped answers, and they differ by a factor of five

The north star's own table, published first-party, is small and gentle:

| Attack ↓ / Defense → | Swift | Natural | Fortified | Arcane | Immaterial |
|---|---|---|---|---|---|
| **Pierce** | 120% | 85% | 80% | 115% | 100% |
| **Impact** | 80% | 90% | 115% | 115% | 100% |
| **Magic** | 100% | 125% | 105% | 75% | 100% |
| **Pure** | 100% | 100% | 100% | 100% | 100% |

Four attack types, five defense types, **every value between 75% and 125%** — the manual states that range as a
design rule rather than an emergent fact. Two of the twenty cells are structural rather than tuned: `Pure` is
an attack type with no advantage anywhere, `Immaterial` a defense type with no weakness anywhere, and both Kings
are `Pure`/`Immaterial` — the objective is deliberately placed *outside* the type game.

Warcraft 3's shipped constants are far more violent. From the game's own data, the multipliers run from **0.05×
to 2.00×** — Piercing does 200% to Light and 35% to Fortified, Magic does 200% to Heavy and 35% to Fortified,
and everything except Chaos does 5% to Divine. Element TD 2 is different again: a pure six-element cycle where
each element deals **200% to the next and 50% to the previous**, with a boss armor class that takes 10% from
everything.

| Design | Spread | Effect |
|---|---|---|
| Legion TD 2 | 1.67 : 1 | Types *tilt* a matchup. Composition matters, but nothing is unusable. |
| Element TD 2 | 4 : 1 | Types *decide* a matchup. Play is about assembling coverage. |
| Warcraft 3 | 40 : 1 (5% to 200%) | Types are near-binary at the extremes — which is why so many WC3 TD maps quietly rewrote the table. |

The spread is a more consequential decision than the cell values, and it is the one that should be set
deliberately and early. Given that Part II's whole argument is that a wave is composed to *read* an opponent,
something wider than the north star's 1.67:1 is probably right — but the closer it gets to Warcraft 3's, the
more a mismatched draw becomes an unwinnable one, which is the variance round-robin exists to suppress.

#### The fourth pole: no matrix at all

Defender's Quest is worth knowing about because it rejects the premise. It has no damage-type × armor-type
grid; it has an open set of about thirty **flavors** attached to damage, and `physical`, `magic`, `melee` and
`ranged` are merely four tags among them, alongside `burn`, `armor_pierce`, `knock_back` and `devour`.
Resistances are per-enemy rules keyed on tags. The developer's published mod docs even show composite tags
invented for convenience — a `physical-ranged` flavor that exists so one dodge rule can mean "arrows".

The trade is stark and worth stating plainly: a matrix is **closed, displayable, and quadratic**; a tag set is
**open, extensible, and impossible to show a player as one picture**. For a game whose build helper must
answer "what beats what" at a glance, the matrix wins — but the tag set should exist alongside it (4.4) for
everything the matrix should not be widened to hold.

### 4.2 The reduction formula and the integer contract

Separate from and applied after the scalar layer: how an integer `armor` value converts to damage taken. Three
shapes in the wild — a diminishing-returns coefficient formula (Warcraft 3's), flat subtraction (Mindustry's
`UnitType.armor` is literally "incoming damage is reduced by this amount"), and a banded percentage lookup
(Kingdom Rush's None / Low / Medium / High / Great / Max). **This is a ruleset-wide constant, not a per-unit
lever**, and it belongs in the ruleset header so a ghost records which formula it was balanced under.

In an integer sim the formula must specify **rounding direction and the exact order** of multiply-then-divide.
`damage * 100 / (100 + k*armor)` and `damage * (100 / (100 + k*armor))` are the same in algebra and different in
integers. The schema stores the numbers; the sim version owns the arithmetic; both are pinned by the replay.

**The shape to pick, and the reason.** Warcraft 3's constant is `0.06`, and its formula is
`reduction = 0.06·armor / (1 + 0.06·armor)`. That looks like an arbitrary hyperbola until you notice what it
buys: each point of armor increases the unit's *effective* health by exactly 6% of its base health. Reduction
diminishes; **effective health is linear in armor**. That is why armor can stack without caps and without ever
becoming absurd, and why League of Legends independently arrived at the same formulation with a constant of
one — `damage × 100 / (100 + armor)`, which in integers is one multiply and one divide, exact, with no table.

It is worth contrasting with flat subtraction, which is the intuitive choice and has a known failure mode.
Gemcraft subtracts armor from damage, and its own community documents the endgame consequence: at high levels
"the monster's armor exceeds the player's ability to meaningfully decrease it", so damage collapses to nothing
and armor-shredding stops being an option and becomes a requirement. Flat subtraction also inverts the value
of attack speed — it punishes many-small-hits quadratically — which is a large balance consequence for a
one-line formula. Factorio's answer, if a flat term is wanted anyway, is to carry *both*: a `decrease` applied
first and a `percent` after, with explicit branches ensuring the result approaches zero without reaching it.

Two escape hatches worth copying, because a pure formula leaves designers with nothing to reach for:

- **True damage** — a damage type that bypasses the whole layer. Kingdom Rush's is the reference.
- **Immune as distinct from 100%.** Kingdom Rush separates `Max` (100% reduction, but shreddable) from
  `Immune` (100% reduction that *cannot be reduced to a weaker band*). One is a number, the other is a promise,
  and armor-shred effects need something they are guaranteed not to beat.

### 4.3 The capability gate

Bloons' contribution, and the mechanic most worth stealing: some interactions are **binary, not scalar**. A
projectile either can or cannot damage a lead bloon. This is not a 0% cell in a matrix — it reads completely
differently to a player, it produces hard counters rather than soft ones, and it makes a tower feel like it has
a *role* rather than a *number*.

Bloons implements it as an integer bitfield on the attack, `immuneBloonProperties`, with one bit per gated
creep property — **1 = Lead, 2 = Black, 4 = White, 8 = Purple, 16 = Frozen**. A "damage type" is then just a
named bitmask: Sharp is `17` (Lead + Frozen), Explosion is `2` (Black), Energy is `9` (Lead + Purple), Normal is
`0`. That is worth pausing on, because it means BTD6's celebrated damage-type system **is not an enum at all** —
it is a derived label over a set of gates, which is why an upgrade can grant lead-popping to one tower without
reclassifying it, and why the Monkey Intelligence Bureau can hand out "Normal damage" to everything in radius by
simply zeroing the mask.

**The gate is two gates, and they must stay separate.** Bloons carries a `FilterFrozenBloonsModel` flag distinct
from the immunity bits — targeting exclusion is not the same as damage immunity. Mindustry makes the same split
three ways on the creep side, with `hittable`, `killable` and `targetable` as *independent* booleans, so
"phasing" decomposes rather than being a special case. Model it as:

- `canDamage: predicate over target tags` — the pop-rules gate.
- `canTarget: predicate over target tags` — the detection/acquisition gate.

A tower that can *see* camo but not *damage* lead is a real and useful design point; one combined flag cannot
express it.

### 4.4 Tag counters — the cheap middle

Between the matrix and the gate: `bonus damage vs tag`. No new axes, no new table, and it scales linearly with
content instead of quadratically. Most counter-relationships that feel like they need a new damage type actually
need a tag.

Bloons is again the evidence: it tracks **bonus damage as a separate scaling track from base damage**, so a
tower can grow its anti-MOAB number without touching its general number. Two integers, no new matrix.

> **Why more than one mechanic**
>
> A scalar layer alone produces a game of soft percentages where every tower is a bit good against everything —
> exactly the "no build feels distinct" failure mode. Gates produce hard counters, which produce *reads*, and
> reading the opponent is the half of the loop Part II identified as the strongest idea in the design. The gate
> layer is what turns composing a wave into a decision rather than a spreadsheet.
>
> The converse also holds: gates alone produce a checklist. You need the scalar layer so that a build can be
> *better* rather than merely *legal*.

### 4.5 Adaptive counters — the anti-monoculture lever

One creep lever deserves separate billing because it exists to solve a problem this game will have.

Infinitode 2's **Light** enemy gains resistance to the last *type* of projectile that hit it, for six seconds,
at most once every ten. It is a creep that punishes mono-tower defenses by construction.

In an async ghost format, a defense that is optimal is *stored*, copied, and faced by everyone — so a single
dominant layout degrades the whole pool's variety in a way a single-player game never suffers. An adaptive
creep is a mechanical answer to that, and it belongs in the vocabulary even if it is not in the first ruleset.
The related economic levers are in 3.11: Rogue Tower charges **more for each additional tower of the same type
already on the field**, and pays the attacker **+1 gold for every distinct tower type that damaged a monster**.
Both make diversity pay without a balance patch.

---

## 5. Stacking, order, and the arithmetic contract

Everything above is inert until the sim decides in what order it applies. This section is the part of the
schema that is *hardest to change later*, because changing it silently rebalances every unit at once.

### 5.1 The stat pipeline is a named, ordered list of stages — write it down as data

The instinct is to define one formula: base, plus flats, times percentages, clamp. That is what I would have
written before reading what shipped systems actually need, and it is not enough.

Dota 2's movement-speed resolution — reconstructed in full by a third-party reimplementation that has to
reproduce the engine's numbers exactly — has **three separate additive stages** (constants before the
multiplier, constants *after* it, and constants that raise the cap) and **four separate clamping stages**
(cap, floor, absolute override, and a final limit). Every one of those exists because a designer once needed a
buff that the previous stage list could not express, and each was bolted on afterwards.

So: define the pipeline as an explicit ordered stage list in the ruleset, give each stage a name, and let each
stat declare which stages it has. A superset that covers every system surveyed:

```
value = base                                   (per-level table lookup)
      + Σ flatAdd                              (stacking: sum)
      + max(flatAddUnique…)                    (stacking: highest-only)
value = value * (10000 + Σ additivePct) / 10000
value = value * Π (10000 + multPct_i) / 10000  (sorted; optional DR table — 5.3)
value = value + Σ postMultiplierFlatAdd
value = clamp(value, floor, cap)
value = override  if any override applies
```

Rules that make it survive:

1. **Additive percentages sum first, then apply once.** Two +50% additive modifiers give ×2.00, not ×2.25.
   Path of Exile's design is worth copying wholesale here: it puts the bucket *in the words* — "increased" is
   additive, "more" is multiplicative — so players reason about stacking without a wiki. If the schema has two
   buckets, the UI vocabulary should have two words.
2. **Multiplicative modifiers apply in a deterministic sorted order**, because integer division is not
   associative. Sort by `(stage, priority, sourceDefId, sourceInstanceId, appliedTick)` — a total order with no
   ties.
3. **One rounding point per derived quantity, at the very end.** Collect numerators and denominators, divide
   once. OpenRA does exactly this — it accumulates in `decimal` and truncates a single time — and the reason
   matters more than the technique: rounding between stages makes the result depend on how many stages happened
   to be non-empty, which means adding an unrelated buff changes an unrelated number.
4. **Percentages are integers.** Basis points (1/10000) or 1/1024 if you want shifts instead of divides. Never
   a decimal in a data file, because a designer typing `0.15` is a float entering the sim through the front
   door. Note that both OpenRA and Battle for Wesnoth independently converged on **integer percent with 100 as
   identity** for their damage-versus-armor tables; that convergence is worth respecting for the authoring
   format even if the resolved form is finer-grained.
5. **Rounding direction is specified once, in the ruleset header, and never varies by call site.** C#'s `(int)`
   cast truncates toward zero while `Math.Round` defaults to banker's rounding, and they disagree on exactly
   the values that will show up in a balance complaint.
6. **Clamp last.** A speed slow that stacks past zero, an armor debuff past the floor, a cooldown past one tick
   — all are the same bug, and all are prevented by a mandatory clamp rather than by care.

> **The counter-example worth knowing**
>
> OpenRA has *no additive bucket at all.* Every modifier in the entire game — damage, range, reload, speed,
> cost, build time — is an integer percentage multiplied into a running accumulator by one six-line function.
> The entire stat pipeline fits on a screen, it is order-independent up to the final truncation, and there has
> never been an argument about whether two +50%s make 200% or 225%.
>
> The cost is real: a designer cannot express "+5 flat damage", so a +20% aura is worth wildly different
> absolute amounts on different units, and every buff is proportional to what it buffs. For a tower defense
> with additive upgrade paths that is probably too austere. But it is the strongest available argument for
> keeping the stage list *short*, and for treating every new stage as a permanent tax rather than a feature.

### 5.2 Stacking rules

Each modifier source declares one, and the rule is part of the *vocabulary*, not per-unit config:

| Rule | Behaviour |
|---|---|
| `none` | Second application is ignored while the first is active. |
| `refresh` | Second application resets duration, magnitude unchanged. |
| `independent` | Each application is its own instance with its own timer. |
| `highestOnly` | Only the strongest instance applies; others sit dormant. |
| `additiveCapped` | Magnitudes sum up to a stated cap. |
| `diminishing` | See below. |

`highestOnly` is the rule that makes auras from the same tower type not stack, and it is the one most often
implemented as a special case in code rather than declared in data. Declare it.

Two refinements from shipped systems, both of which cost nothing now and are expensive to retrofit:

**Put the aggregation rule on the stat, as an enum.** Dota 2 encodes it in the *name* of each modifier property
— a `_CONSTANT` suffix means sum, `_UNIQUE` means highest-only, `_STACKING` means multiplicative, `_OVERRIDE`
means replace. It works, and it is the reason an external tool that wants to reproduce Dota's numbers has to
hand-classify several hundred property names by eye: the aggregation semantics live in engine C++, not in the
data. The same expressiveness is available for free by making it a declared field —
`aggregation: Sum | AdditivePct | MultiplicativePct | Max | Min | Override` — which is machine-checkable and
which the build helper can render without being taught.

**Let the dedupe key be authored data, not a code enum.** Battle for Wesnoth's rule is that two abilities
sharing an `id` do not stack unless one opts in with `cumulative=yes`. Non-stacking is the *default*, and the
grouping key is a string the designer chooses — so creating a new mutually-exclusive family of effects needs no
code change at all. Given that "why do these two auras stack when those two don't" is the single most common
balance complaint in this genre, having the answer be a visible string in the data is worth a great deal.

### 5.3 Diminishing returns

Needed for any lever a player can stack from many sources — slows above all. Two shapes worth having in the
vocabulary:

- **Multiplicative composition.** Each source multiplies the remainder rather than subtracting from the total.
  Two 50% slows give 75%, not 100%. This is the cheap default and it is usually enough.
- **Ranked penalty.** Sort the effects by magnitude and apply a decreasing weight to each successive one, so
  the fifth aura of a kind contributes almost nothing. EVE Online's is the canonical published version — the
  *n*-th strongest modifier is scaled by `e^(−((n−1)/2.67)²)`:

  | Rank | 1 | 2 | 3 | 4 | 5 | 6 | 7 |
  |---|---|---|---|---|---|---|---|
  | Weight | 100% | 86.9% | 57.1% | 28.3% | 10.6% | 3.0% | 0.6% |

  Two rules matter more than the curve. **Apply in descending order of strength** — that sort is what makes the
  result independent of the order effects were applied in, and without it the scheme is not deterministic under
  reordering. And **only percentage effects are penalised; flat effects never are.**

  For an integer sim, do not evaluate that exponential. `n` is a small integer, so **store the weights as an
  integer per-mille table indexed by rank** — `1000, 869, 571, 283, 106, 30, 6, 1, 0` — and look them up. It is
  exact by construction, it is hashed along with the rest of the ruleset, it cannot drift when someone
  "optimises" a `Math.Pow` call, and the build helper can show it to a player as a table. This generalises: any
  non-linear curve in the ruleset should be a table, not a formula.

A third shape, cheaper than either and easy to overlook: **decay per application**. Infinitode 2 shortens each
consecutive stun on the same enemy by ten percent of its maximum, which needs one counter per unit and no curve
at all. Where the concern is chain-crowd-control specifically rather than stat inflation generally, this is the
proportionate answer.

Whichever is chosen, it belongs in the ruleset header alongside the armor formula — a game-wide arithmetic
decision, recorded in the replay.

### 5.4 Determinism constraints the schema itself must carry

| Constraint | Consequence for the data |
|---|---|
| No floats | All numbers are `int`/`long`. Fractions are per-mille, basis points, or Q-format fixed point declared once. |
| Every quantity carries its unit in its *type* | Not a bare `int`, but `Ticks`, `Millipercent`, `WorldDist`. OpenRA's model is worth copying exactly: distance is a one-field struct at 1024 units per cell, angle is a one-field struct at 1024 units per turn, and neither converts implicitly from `int`. You never have to ask whether a number is already scaled, because the type answers. |
| Deterministic iteration | Any collection the sim iterates must have a defined order in the data — arrays, not maps; sorted by declared key, never by hash order. |
| Seeded randomness | Every random lever names its **RNG stream** (`stream: "attack-roll"`), so adding a new random lever elsewhere cannot shift an existing stream's sequence. This is the single cheapest thing you can do now to stop future features from breaking old replays. |
| Tick-quantised time | Every duration, cooldown, and interval is in integer ticks. No seconds in the data. |
| Order of simultaneous events | Ties broken by a stable integer key present in the data (spawn index, placement index) — never by object identity. |
| Expression order is part of the spec | `(a/b)*c` and `(a*c)/b` differ in integers. The order of operations for each derived quantity is a *documented schema decision*, not an implementation detail — because a refactor that "simplifies" the expression is a silent balance change and a silent replay break. |

> **Why the no-floats rule is stronger in C# than the usual argument admits**
>
> The familiar case against floats is the cross-platform one: x87 versus SSE, transcendentals disagreeing
> between vendors, debug diverging from release. All true, and Part III already recorded it.
>
> C# adds a worse problem, and it is in the language standard rather than the hardware. ECMA-334 §8.3.7 states
> that floating-point operations **may be performed with higher precision than the result type of the
> operation**, and explains that this is deliberate — the alternative would "require an implementation to
> forfeit both performance and precision." That is a standing licence for any future JIT to change your
> arithmetic results on the same machine with the same binary. A float simulation in C# is not replay-stable
> by construction, independently of what hardware it runs on.
>
> (The same standard specifies `decimal` completely, with no such latitude — so `decimal` *is* a deterministic
> C# type. It is also boxed at 16 bytes, software-implemented, and throws on overflow rather than saturating.
> Useful as an intermediate accumulator, as OpenRA uses it. Not a substitute for integer state.)

**Enforce it with the type system, not with review.** OpenRA's sync-hashing code carries an explicit allowlist
of hashable types — its integer world types, plus `int` — and any other type reaches a `throw` at load. A float
marked as sync-relevant is not a code-review finding; it is a startup failure. The equivalent here is a schema
loader that rejects any sim-affecting field whose declared type is not on the list. That is one afternoon of
work now and it is the only thing that will still be enforcing the rule in three years.

**One ECS-specific hazard, because Part III points at Unity.** If a modifier's application order is derived
from the order components come back from a query, the simulation's arithmetic depends on archetype and chunk
layout — which is a *performance* concern the framework is entitled to change between versions. Ordering must
come from data: a stage index, a sort key, an explicit priority. This is the sharpest reason the stage list in
5.1 has to be an authored artefact rather than the order the code happens to run in.

Related, and easy to get backwards: **components are for shape, flags are for state.** Adding and removing
components at runtime is a structural change and expensive enough that Unity ships enableable components
specifically to avoid it, and OpenRA ships conditional traits that stay attached and return identity when
disabled. So the "present or absent" model in section 6 describes the *authored* definition. At runtime, a
disabled lever stays attached and contributes nothing.
| Nothing faster than a tick | Attack speed cannot exceed one attack per tick. The schema needs a stated answer for what happens above that. |

> **The tick ceiling, and the best answer to it I have seen**
>
> Every attack-speed buff in the catalogue eventually pushes a cooldown below one tick, and a tick-quantised sim
> cannot represent it. The usual fixes are to clamp — which silently voids the player's investment — or to
> accumulate fractional cooldowns, which is a float creeping in through the side door.
>
> Infinitode 2 does neither: attack speed is **hard-capped at the tick rate, and every point of excess attack
> speed is converted into damage instead**. Nothing is wasted, nothing is fractional, and the arithmetic stays
> in integers. It is the rare case where the determinism constraint produces a *better* design rather than a
> compromise, and it should be the ruleset's stated policy from the first commit rather than a patch response
> to the first player who stacks four haste auras.

---

## 6. The schema in three layers

### Layer 1 — Vocabulary (stable, append-only)

The set of component kinds, status kinds, tag IDs, damage types, armor types, targeting modes, stacking rules,
and effect kinds. This is the part the build helper is written against, and the part that must not churn.

Rules: string IDs; append only; never redefine a meaning; deprecate by marking, never by deleting.

### Layer 2 — Ruleset (versioned as a unit, content-hashed)

Every unit definition, every number, the matrix, the armor formula, the DR curve, the scaling tables. One
ruleset is one immutable artefact with a hash. Patches produce new rulesets. **Old rulesets are kept forever** —
they are kilobytes, and they are the only thing that makes a two-year-old ghost meaningful.

A unit definition is then, structurally:

```jsonc
{
  "id": "unit.frost_pylon",
  "schema": 1,
  "tags": ["placed", "mechanical", "elemental"],
  "components": [
    { "kind": "Health",     "max": 1200, "regenPerTick": 0 },
    { "kind": "Armor",      "value": 4, "type": "armor.fortified" },
    { "kind": "Attack",
      "damage": 55, "damageType": "damage.frost",
      "cooldownTicks": 40, "windupTicks": 6, "rangeUnits": 900,
      "delivery": "projectile", "projectileSpeed": 1200,
      "canTarget":  { "allOf": ["ground"], "noneOf": ["burrowed"] },
      "canDamage":  { "noneOf": ["immune.frost"] },
      "onHit": [ { "effect": "effect.chill" } ] },
    { "kind": "Targeting",  "priority": "first", "retarget": "everyShot" },
    { "kind": "Placement",  "footprint": [[0,0],[1,0],[0,1],[1,1]], "blocksPath": true },
    { "kind": "BuildCost",  "gold": 180, "refundBps": 7500 },
    { "kind": "Upgradable", "edges": ["upgrade.frost_pylon_to_glacier"] }
  ]
}
```

Everything absent is genuinely absent. There is no `splashRadius: 0`, no `auraRange: null`. A query for "every
unit that splashes" is a component filter.

**Authoring inheritance is fine; runtime inheritance is not.** Templates and `inherits:` make a hundred units
maintainable, and every system surveyed has some form of it — OpenRA splices YAML trees, Factorio deep-copies
prototypes and mutates them, Flecs makes inheritance a first-class relationship with a per-component opt-in.
The decision that matters is *when* it resolves. Resolving at query time means a unit's effective value depends
on the parent chain as it exists at that moment. Resolving once, at load, produces a **flat, fully-resolved,
hashable artefact** — which is exactly the thing a ghost needs to pin and the build helper needs to cache
against.

So: resolve inheritance eagerly, and make the resolve step a first-class, invokable operation that emits text.
OpenRA ships `--resolved-rules` and `--map-hash` as ordinary command-line utilities for precisely this, and the
payoff is that no external tool ever has to reimplement the merge — it asks. That command should exist in this
project before the second unit definition does.

### Layer 3 — Instance (runtime only, never authored)

The placed or spawned unit: current health, active statuses, modifier stack, per-instance counters, RNG
substream state. Never serialised into a ghost — a ghost stores *inputs*, and the instance state is *output*.
This distinction is what keeps ghost files small and is why a replay must be re-simulated rather than
play-backed.

### Cutting across all three — quantise the lever space

The most surprising thing in this whole survey is how few distinct values the best-balanced games actually use.

Legion TD 2 has 159 fighters and quantises range into **four named buckets** — Melee, Short, Medium, Long —
with 91 of the 159 sitting at exactly melee range. Element TD 2 has 59 towers and uses **four range values,
four attack-speed values, and six area-of-effect values** across all of them; its cost ladder is fixed per
tier. Infinitode 2's entire effectiveness matrix draws from **seven percentages** — 0, 10, 25, 50, 75, 100,
150 — and nothing else.

None of these games is short of expressive range. They are enforcing a discipline: **differentiation comes from
which combination of levers a unit has, not from a unique number in every field.** The consequences are worth
listing because they all land on this project:

- A player can learn the vocabulary. "Long range" is a fact about a tower; `range: 847` is trivia.
- The build helper can group, sort and compare without inventing buckets of its own.
- Balance sweeps are tractable, because the space is combinatorial over small sets rather than continuous.
- Diffs are meaningful. A range change from Medium to Long is a design decision; 700 → 720 is noise.

So the vocabulary should define, for each quantised lever, the **enumerated set of legal values**, and the
loader should reject anything outside it. This is the cheapest constraint in the document and the one most
likely to be skipped, because every individual violation looks harmless.

---

## 7. Effects as data, and the escape hatch for behaviour

Levers like "on hit, apply chill" and "on death, spawn two children" are not stats; they are behaviour. The
choice of how to represent behaviour is the schema's second-hardest decision after the arithmetic contract.

**Represent behaviour as a tree of typed effect nodes.** A small closed set — `Damage`, `ApplyStatus`,
`SpawnUnit`, `ModifyStat`, `Search` (find targets in a shape), `Sequence`, `Conditional`, `Persistent` (repeat
every N ticks), `Destroy` — composes into most of what section 3 describes, and each node is inspectable.

StarCraft II's data editor is the industrial-scale version, and its split is worth taking whole rather than in
part. It separates **four** concerns into four catalogs:

| Catalog | Owns |
|---|---|
| **Ability** | What can be invoked: costs, targeting, range, cooldown |
| **Effect** | A composable side-effecting tree node — damage, search an area, apply a behavior, fan out to N children, create a persistent repeater |
| **Behavior** | Persistent state attached to an entity, carrying stat modifiers — every buff in the game is one effect type applying one behavior |
| **Actor** | Presentation. Subscribes to simulation events; never appears in an effect tree |

The Actor split is the one people skip and regret, and for this project it is not cosmetic: **it is the
boundary that keeps presentation out of the replay hash.** If art, sound and animation are separate catalogs
that only listen, then re-skinning a tower cannot invalidate a stored ghost — and if they are fields on the
unit, it can.

Two other borrowings from the same system. Its **validators** are a separate catalog of reusable predicates
that other data types reference, rather than conditions written inline at each use — and behaviors get *two*
validator slots, one that disables the effect while keeping it attached and one that removes it outright.
Those are genuinely different, and having only one is a recurring source of "why did my buff disappear" bugs.

Its cost should be stated fairly too: a five-node effect tree to express "shoot a missile that does 20 damage"
is a great deal of ceremony next to OpenRA's `Damage: 20`. The tree earns its keep only once effects genuinely
recombine. Start closer to the flat end and grow toward the tree.

### The escape hatch, in three tiers rather than two

Some levers genuinely need code. The temptation is a single boolean escape hatch; the better structure is a
declared tier, because it lets the build helper *tell the truth about what it does not know*.

| Tier | Shape | Build helper can |
|---|---|---|
| **1 — pure data** | `{stat, stage, aggregation, value}` and effect trees over the closed node set | Simulate, sweep, and explain it symbolically. Should cover the large majority of units. |
| **2 — parameterised code** | A named, versioned, pure function ID plus integer parameters, with a declared signature `(inputs) → int` | Evaluate it if it ships the same function table; degrade to "unknown effect" — never to a wrong number — if it does not |
| **3 — arbitrary code** | A function with access to world state | Nothing. The unit is flagged `notStaticallyAnalyzable`, and the helper says "this tower's output cannot be computed offline" rather than lying |

**Data names code; data never contains code.** A component may reference `behaviour.chain_seek_v2`; it may not
contain a Lua string or an expression language, because that turns the ruleset into a program and the ghost
hash into a promise about an interpreter version.

Tiers 1 and 2 are pinned by the ruleset hash. **Tier 3 can only be pinned by `simVersion`** — which is the
concrete reason to keep tier 3 rare, and a better argument than tidiness. Every tier-3 unit is one more reason
an old ghost needs an old binary.

> **The general principle, from the systems that got this right**
>
> Allow code, but require it to implement a *narrow declarative interface whose return value is a schema
> value*. OpenRA's stat modifiers are C# — but each one is a class whose entire job is to return an integer
> percentage, so an external tool only needs to know "there is a modifier here worth 120 under condition X",
> not how it decided. The moment a lever's interface is `void DoSomething(World world)`, external analysis is
> over and the build helper is guessing.

---

## 8. Versioning for ghosts that must outlive patches

| Rule | Reason |
|---|---|
| Ghosts store `(rulesetHash, simVersion, inputs)` | The result is recomputed, never trusted from the file. |
| Rulesets are immutable and archived forever | A ghost from 2027 replays under 2027's numbers or not at all. |
| IDs are strings, never indices, never reused | Reordering a content array must not silently reassign a unit. |
| Meanings never change; new meanings get new IDs | A `slow` that starts meaning attack-speed instead of movement invalidates history invisibly. |
| Unknown lever → hard failure, never skip | Section "bottom line". This is the inversion. |
| The arithmetic contract is part of `simVersion` | Changing rounding is a sim change even if no number moved. |
| Migration is *re-simulation under a stated ruleset*, not field-rewriting | An old ghost re-run under new numbers is a legitimate and separate product feature ("how would this defense hold today?"), but it must be labelled as such and never mixed with historical results. |

A ghost that cannot replay is not a crisis: it is a leaderboard entry that gets flagged as historical. A ghost
that replays *differently and silently* is a corrupted competitive record, which is the thing Part II said the
whole format rests on.

### 8.1 Two traps from the serialization formats

Both of these are things the formats do *on purpose*, correctly, for messaging — and both are wrong here.

**Never let a sim-affecting value be implied by a default.** FlatBuffers omits fields that equal their default
from the serialized bytes. That is a fine size optimisation and a disaster for a ruleset: change a default in
a later version and every previously-written record that relied on it silently means something new. Either
write every sim-affecting field explicitly, or — better — do not serialize resolved numbers into the ghost at
all, and pin the ruleset by hash instead.

**Never reuse an identifier, and prefer a stable numeric ID to a name.** Protocol Buffers' rule is that field
numbers may be added and retired but never recycled, because a recycled number makes old bytes decode into the
wrong field. Dota 2 applies the same discipline at the row level, and the comment sitting in its shipped
ability data says it better than I can: *"unique ID number for this ability. Do not change this once
established or it will invalidate collected stats."* The string key is a label; the number is the identity.

### 8.2 What counts as "a different ruleset"

Factorio has the most complete shipped answer, because it has to decide this every time a save is loaded with
a different mod set. Its configuration is considered changed when the game version changes, when any mod
version changes, when a mod is added or removed, when a startup setting changes, when any prototype is added
or removed, or when a migration was applied. That list is very close to the specification you want for "this
ghost belongs to a different ruleset and must be re-pinned rather than replayed."

Its recovery UI is the other half and is worth copying in spirit: the client compares checksums, refuses on
mismatch, and offers to *sync to the exact versions the save was made with*.

### 8.3 Migrations should be allowed to admit defeat

OpenRA maintains named update paths between release tags, each a list of rules that rewrite the data files.
The detail worth stealing is the return type: each rule returns **a list of human-readable strings describing
what a person still has to finish by hand**. A migration is allowed to say "I converted what I could, here is
what I could not." That is far more honest than the usual pretence that every migration is mechanical, and it
is the difference between a migration system people trust and one they route around.

### 8.4 Separate identity fields from tuning fields

There is a distinction hiding in how the north star actually patches, and it is worth making explicit in the
schema. Reading a real Legion TD 2 balance patch, what changes is health, damage, attack speed, ability
percentages and durations. What is *never* touched is a unit's attack type, its defense type, its range, or
its cost.

That is not an accident of one patch — it is the difference between **identity** and **tuning**. Identity
fields are what a player has learned about a unit and what a build is planned around; tuning fields are the
numbers that make it fair. Marking each field as one or the other in the vocabulary buys two concrete things:
a lint that makes changing an identity field a deliberate, reviewed act rather than a stray diff, and a
principled answer to the question every live game eventually faces — *when is this a rebalanced unit, and when
is it a new unit that should have a new ID?* When identity changes, mint a new ID and let the old one keep its
ghosts.

---

## 9. What the build helper actually needs from this

The helper is not a separate program with its own copy of the rules. **It is the headless sim plus an index over
the ruleset** — the same binary Part III already requires for balance sweeps, with a query layer on top.

Four things it needs, all of which are schema properties rather than helper features:

1. **Component-level queryability.** "Show me every unit with `Aura` affecting `armored`" is a filter over
   present components. This is the entire argument for composition over a wide struct.
2. **Declarative interactions.** The matrix, the gates, and tag counters are all lookups, so "what counters
   this wave" is answerable *without simulation* — instantly, while the player is still typing. Simulation is
   the fallback for the hard cases, not the default path.
3. **Cost expressions that are data.** Comparing builds means summing costs and comparing to a budget. If cost
   is a function in code, the helper re-implements it and drifts.
4. **A stated evaluation boundary.** Adjacency, auras, ammo, and power make a unit's value depend on the rest of
   the board. The helper must know *which* components are board-dependent so it can tell the player "this
   number assumes the rest of your layout" instead of quietly lying. Mark them in the vocabulary with a
   `boardDependent` flag.

The helper's three questions, in the order players will ask them: *what does this tower actually do*, *what
beats what*, and *is this wave affordable and does it get through*. The first two are index lookups. Only the
third needs the sim.

**This is a solved problem when the schema cooperates, and an unsolvable one when it doesn't.** Path of
Building computes full damage-per-second for Path of Exile builds — auras, curses, charges, resistances —
entirely outside the game, because that game's modifiers are `(stat, bucket, value)` rows and its calculation
is "sum the additive bucket, multiply the multiplicative one." The comparison case is instructive: the
equivalent project for Dota 2 had to hand-classify several hundred modifier properties into aggregation
families by eye, because the aggregation semantics are in the engine's C++ rather than in the data. That
labour is the exact tax that 5.2's `aggregation` field avoids paying.

Two smaller things worth designing in now:

**Publish the normalising ratios the balance team actually uses.** The north star's own unit pages carry
hidden tooltips reading "6 HP per gold" and "0.35 DPS per gold" — the two ratios its designers normalise
against. Those are precisely the numbers a build helper should surface, because they convert "is this unit
good" into "is this unit priced correctly", which is a question a player can act on. If the ruleset states its
own intended ratios, the helper can flag outliers without anyone maintaining a separate spreadsheet.

**Let content carry its own counter-hints.** Element TD 2 annotates each creep ability with the archetype that
beats it — "weak to single-target", "weak to AoE", "weak to long range" — as authored text sitting next to the
mechanic. That is a designer's intent captured as data rather than lost to a wiki, and it gives the helper
something useful to say about a matchup that the matrix alone cannot express. One optional string field per
ability.

---

## 10. Levers not to build

Each of these is expressible in the schema above. None should ship in the first ruleset.

**10.1 Floating-point anything.** Restated because it is the one that cannot be fixed later.

**10.2 Random crit and random evasion.** Part II built round-robin specifically to control variance across five
opponents. Per-attack RNG re-injects it *inside* each match, where there is no averaging left to do. If the
feel of a crit is wanted, make it deterministic — every Nth attack, or on a condition. Same fantasy, no
variance, and the build helper can compute exact DPS instead of an expected value.

If a rolled crit is nonetheless wanted, Rogue Tower's shape is the one to copy: crit chance above 100% does not
overflow into nothing, it **promotes the multiplier** — 0–50% is the chance of a double, 51–100% of a triple,
101–150% of a quadruple, rolled highest-first — so the stat never becomes dead weight and never becomes
unbounded. It is still variance, and it still costs the helper an exact answer.

**10.3 Per-damage-type resistance vectors on every unit** *and* **a damage-type × armor-type matrix.** Both are
good; having both means every unit has two independent knobs doing the same job, and nobody will be able to say
why a matchup is what it is. Pick the matrix — it displays, it teaches, and it is what the north star uses.

**10.4 Unbounded stacking auras.** Every aura needs a stacking rule and every stat needs a clamp from the first
commit, because the first time a build stacks twelve of something, the fix is a rebalance rather than a bug fix.

**10.5 Per-instance experience and levelling.** It makes a unit's power depend on its entire history, so a ghost
is no longer a layout — it is a layout plus a biography. It also makes the helper's "what does this tower do"
unanswerable without a timeline. The games that do it well do it *thoroughly*: Rogue Tower awards experience by
time spent aiming rather than damage dealt, at a rate of `0.5 + 1/(2 × range)` per second so short-ranged towers
level faster, and types the experience by which health pool the target was on at the time. That is a good
mechanic and a terrible thing to store in a ghost. If progression is wanted, put it on the *player*, between
rounds.

**10.6 Owner-account scaling.** A defense whose strength derives from the owner's meta-progression cannot be
fairly attacked by anyone, and pins the ghost to mutable account state. Dungeon Defenders is the extreme case
and clarifies why: its towers have **no stats of their own at all** — damage, health, area and rate all come
from the hero who placed them and are baked in at placement time. That is a coherent design for a co-op game
and an impossible one for a competitive ghost pool, where the same submitted layout would mean a different
thing depending on whose account it came from.

**10.7 Inline scripting in the ruleset.** Section 7. The moment a data file contains an expression to be
evaluated, the ruleset hash stops describing the behaviour.

**10.8 Class inheritance for unit types.** `MageTower : ProjectileTower : Tower` will look correct for eleven
towers and then the twelfth will need two parents. Composition has no such cliff, and the cliff always arrives.

**10.9 Aggregation semantics that live only in code.** If "these two auras don't stack" is an `if` in the sim
rather than a field in the data, then the build helper cannot know it, the balance sweep cannot vary it, and
the answer to a player's question lives in a file they cannot read. This is not a hypothetical cost: it is the
single largest difference between the games whose builds can be planned offline and the ones whose cannot.

**10.10 A typed value whose type depends on how it was written.** Battle for Wesnoth lets the same key take
either a flat number or a percentage, disambiguated by a trailing `%`. It is compact and it is charming, and
it means a value's meaning is a lexical property. Use two field names — `damageAdd` and `damageAddBps` — and
let the schema be boring.

---

## 11. What I'd build first

The vocabulary is large; the first ruleset should be small. How small is worth calibrating against the north
star, because it is more austere than it looks: **a Legion TD 2 unit page shows four numbers.** Health, DPS,
range, cost. There is no numeric armor stat anywhere in the game — armor is a type, not a value — and there is
no per-unit damage or attack-speed figure in the public data either. A hundred and fifty-nine distinct
fighters are differentiated by four numbers, two type tags, a movement class, and one ability each.

That is the target. A defensible minimum that still exercises every structural decision above:

1. **Components:** `Health`, `Armor`, `Attack`, `Targeting`, `Movement`, `Placement`, `BuildCost`, `Bounty`,
   `Aura`, `OnDeath`, `Upgradable`, `Tags`.
2. **Interaction:** four damage types, four armor types, a 16-cell integer matrix, one armor formula, and
   **two capability gates** (`flying` and one detection-style tag) — enough to prove that gates feel different
   from percentages before committing to more. Set the matrix spread deliberately (4.1); somewhere between the
   north star's 1.67:1 and Element TD's 4:1 is the interesting range.
3. **Statuses:** slow, damage-over-time, armor-shred. Three payloads, one envelope, all three stacking rules
   exercised.
4. **The arithmetic contract, complete and frozen:** the named stage list, basis points, sorted multiplicative
   order, one rounding point, named RNG streams, mandatory clamps, and the tick-ceiling policy. This is the
   part that is expensive later and nearly free now.
5. **Quantised value sets** for range, attack interval and area, enforced by the loader.
6. **Ruleset hashing, a `--resolved-rules` dump, and hard-fail-on-unknown — before the first ghost is stored.**

Two of these are worth doing even before there is a game to balance, because they pay for themselves
immediately rather than eventually. The **resolved-rules dump** is what lets the build helper exist at all
without reimplementing anything. And the **sim-affecting type allowlist** in the loader is what will still be
refusing floats in three years, when nobody remembers this document.

Everything else in section 3 is a component you add to a vocabulary that already has room for it. That is the
whole point of the exercise: the goal was never to know which levers the game ships with — it was to make sure
that finding out later costs a new component and not a migration.

---

## Sources

Ordered roughly by how much weight this document puts on them. Where a claim rests on a community wiki rather
than a developer or a shipped data file, that is stated — this genre is badly under-documented first-party, and
several of the most-cited numbers in it are folklore.

**First-party and shipped data (highest confidence)**

1. **Legion TD 2** — official type table (`beta.legiontd2.com/typetable/`), the per-unit stat pages and their
   embedded multiplier tooltips (all 159 fighters scraped), the official manual's 75%–125% design rule and role
   taxonomy, the King upgrade page, the Legion Spells page, and the v6.01 balance patch notes. The public API's
   unit schema — including the `legion` enum containing `Creature` and `Mercenary` — is from the documented
   v2 API via a community SDK's typed field list.
2. **Warcraft 3** — the shipped gameplay constants from `MiscData.txt` / `MiscGame.txt`: the `DamageBonus*`
   rows for both Reign of Chaos and The Frozen Throne, `DefenseArmor=0.06`, the aura- and illusion-stacking
   boolean switches, and the speed bounds. Blizzard's own armor-and-weapon-types page at
   `classic.battle.net/war3/` for the prose model. Note that Liquipedia and the Warcraft Wiki disagree with the
   shipped file on two cells (Piercing vs Heavy, Spells vs Hero); this document does not rely on either.
3. **Mindustry** — read directly from source (`github.com/Anuken/Mindustry`): the turret class hierarchy under
   `world/blocks/defense/turrets/`, `entities/bullet/BulletType.java`, `entities/UnitSorts.java`,
   `type/UnitType.java`, `type/StatusEffect.java`, `content/StatusEffects.java`, and the ability classes under
   `entities/abilities/`. Field names and numeric constants quoted in this document are from that source.
4. **OpenRA** — read from source on the `bleed` branch: `Warheads/DamageWarhead.cs` (the `Versus` dictionary
   and `DamageVersus`), `Util.ApplyPercentageModifiers`, `Traits/Armor.cs`, the `Traits/Multipliers/`
   directory, `WDist.cs` / `WAngle.cs` (integer world units and the cosine table), `Sync.cs` (the hashable-type
   allowlist), `GameInformation.cs` (replay metadata), and `UpdateRules/UpdatePath.cs`. Plus the official
   MiniYAML documentation and the generated trait/weapon docs at `docs.openra.net`.
5. **Dota 2** — the shipped `npc_abilities.txt` and per-patch hero KeyValues dumps, including the
   `AbilityValues` / `LinkedSpecialBonus` structure and the "do not change this once established" comment on
   `ID`. The aggregation-family semantics and the movement-speed pipeline are from a third-party
   reimplementation that exists to reproduce the engine's numbers — high confidence, not first-party. Valve's
   own developer wiki was unreachable.
6. **Factorio** — the official Lua API prototype documentation: the data lifecycle and its
   configuration-changed trigger list, `UnitPrototype`, and the two-stage `Resistance` type with the damage
   formula from the official wiki.
7. **Battle for Wesnoth** — the official WML reference: `UnitTypeWML`, `UnitsWML` (the "100 = neutral"
   resistance convention), `EffectWML` (the closed `apply_to` verb set), `AbilitiesWML` (id-keyed
   non-cumulative stacking).
8. **Defender's Quest** — the developer's own published mod documentation
   (`github.com/larsiusprime/tdrpg-mod-docs`): the damage-flavor system with its `(flavor, amount, time, rate)`
   envelope, the class and attack definitions, and the two passive types.
9. **Specifications** — ECMA-334 §8.3.7 (floating-point operations may use higher precision than the result
   type) and §8.3.8 (`decimal` semantics); the Protocol Buffers proto3 schema-evolution rules; the FlatBuffers
   schema documentation on field appending, deprecation and default elision.
10. **Photon Quantum** — the fixed-point documentation: `Q48.16`, the `±32768` safe-multiply bound, LUT-based
    trigonometry, and the simulation-purity rules in its FAQ.
11. **Unity Entities 1.3**, **Bevy** and **Flecs** official documentation — component taxonomies, structural
    change costs, enableable components, and Flecs' per-component `(OnInstantiate, Inherit)` policy.
12. **StarCraft II** — the Blizzard-sponsored community editor guide at `s2editor-guides.readthedocs.io` for
    the catalog model, the effect tree, and the two validator slots on behaviors; Blizzard's own editor
    reference is no longer served. `github.com/Blizzard/s2client-proto` for the headless/data-introspection API.

**Community wikis and reverse-engineered data (used with care)**

13. **Bloons TD 6** — the community wiki, which in several places quotes internal field names verbatim
    (`immuneBloonProperties` and its bit values, `FilterFrozenBloonsModel`, `isStunned`). Ninja Kiwi publishes
    no data dump. The wiki's own page carries a misinformation banner on three rows of the damage-type table
    post-v25.0; those rows are not used here. The layer/RBE model, Fortified multipliers and inheritance rules,
    targeting priorities, the crosspath legality rule, and the Berserker Brew uptime numbers are consistent
    across sources.
14. **Infinitode 2** — community wiki: the 16 × 12 effectiveness matrix and its seven legal values, the enemy
    ability list, the debuff catalogue with per-source and lifetime stacking rules, the two-axis progression
    model, and the attack-speed-capped-at-tick-rate rule.
15. **Rogue Tower** — community wiki: the three sequential hit-point pools with per-tower multipliers, the
    three regeneration-gating damage-over-time effects, the magnitude-with-decay status model, multi-criterion
    targeting, banded crit, and the economy levers. The elevation and adjacency numbers were flagged by the
    research pass as needing re-verification before being relied on.
16. **Element TD 2** — community wiki: the six-element cycle with its 2×/0.5× multipliers, the tower roster's
    structure (⚠ the completeness claim taken from it was wrong — see §3.11), the quantised stat ladders, the
    creep ability list with its counter annotations, and the `k^(n−1)` debuff diminishing-returns formula
    (k = 0.88 as of patch 1.7). Plus
    [Element TD 2 on Steam](https://store.steampowered.com/app/1018830/Element_TD_2/) and
    [eletd.com](https://www.eletd.com/) — **first-party**, both stating 59 towers from 6 elements.
17. **Gemcraft** — community wiki, entirely reverse-engineered: the colour-to-special mapping per game, the
    combining coefficient tables, and the flat-subtraction armor model with its documented endgame failure.
18. **Kingdom Rush**, **Dungeon Defenders**, **Orcs Must Die!**, **Plants vs. Zombies**, **Sanctum**,
    **Anomaly** — community wikis and store pages, used for structural levers (blocking bodies and rally
    points; hero-owned defense stats and the DU budget; floor/wall/ceiling surface classes; lane systems and
    dual placement costs; the base-versus-tower split; convoy ordering, route authoring and map-placed
    abilities) rather than for numbers.
19. **Path of Exile** — the community wiki's "increased" versus "more" definitions; Path of Building and RePoE
    as the existence proof that a declarative stat system can be recomputed outside the engine.
20. **EVE Online** — EVE University's stacking-penalty page for the `e^(−(u/2.67)²)` curve and its multiplier
    table. Note that the constant `2.22292081`, widely quoted elsewhere, does not reproduce the published
    table. **League of Legends** — the official wiki's armor formula and its multiplicative penetration rule.
21. **Bob Nystrom, *Game Programming Patterns*** — the Component and Type Object chapters, including the
    copy-down versus dynamic delegation distinction. **Adam Martin**, *Entity Systems are the future of MMOG
    development, Part 2*, for the entity/component/system definitions and the multiple-inheritance critique.
22. **Glenn Fiedler**, *Floating Point Determinism* — carried forward from Part III. Worth re-reading for what
    it actually argues: strict IEEE modes and identical toolchains, not fixed point. The reason this document
    goes further is the C# standard, not the hardware.

**Known gaps**

23. The two Overwatch GDC 2017 talks (Ford on ECS and determinism; Reed on networked scripted abilities) are
    the obvious primary sources for ECS-plus-determinism at scale and could not be retrieved — the vault is
    video-only and the slide PDF is not served. Nothing in this document rests on them.
24. Warcraft 3's *non-aura* buff and debuff stacking rules could not be sourced primarily. The aura and
    illusion cases are covered by the shipped boolean switches; the rest is not, and is not cited here.
25. Legion TD 2's per-creep health values, its mythium generation rate after wave 10, and the units of its
    API's `attackSpeed` field are not published. The API presumably carries the first; it requires a key.

