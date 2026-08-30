# The Vision

**The standing document: what the game is and what it is not.** Where anything else in this repository
disagrees with it, this is current.

It holds only what is true now. Every reversal is in [the decision log](decision-log.md); the five deep dives
it replaced are in [`archive/`](archive/README.md). The sequence it gets built in is
[the build order](build-order.md), and what is in scope but undecided is [open questions](open-questions.md).

---

## Bottom line

**A technically excellent tower defense, built for the pleasure of building it, whose multiplayer is real — and
every mode of it is the same machine at a different latency.**

Five claims carry the document.

1. **It is not a commercial product.** No store page, pricing, monetisation or marketing. The reward is the
   build. This deletes more scope than any other decision here and is what makes the rest affordable.
2. **The planning phase is the game, and the simulation is a design material.** Nothing happens during a wave,
   by construction, so the whole exercise of skill is build-and-compose — which must therefore be as dense as
   two phases are in any comparison game. Affordable because the simulation resolves a match in **2.75 ms**:
   anything worth knowing before committing is computable, and so is anything worth telling the player
   afterwards. [§9](#9-the-planning-phase-is-the-game).
3. **Shallow to look at, extreme to play.** Readable by a stranger inside a minute; Element TD's
   element-combination depth underneath, with the attacking half as deep as the defending one. §3 is the depth
   claim and §6 the presentation claim, and neither is traded for the other.
4. **The multiplayer is not a garnish.** Async round-robin against a shared pool of every player, a live lobby
   of friends, co-operative play, and a social layer of replays and named rivalries. All four, none deferred.
5. **They are one machine.** Every mode is *submit → wait → resolve → watch*. The round-robin is that loop at a
   latency of days with strangers in the seats; the lobby is the same loop at a latency of minutes with
   friends. **No lockstep, no rollback, no tick synchronisation anywhere — only a submission barrier.** That is
   the largest cost saving in the document. §2.

---

## 1. The destination

**A deep personal build with genuinely rich multiplayer for the developer and his friends.** Public release is
optional and never load-bearing.

"Personal" governs the *reason*, not the *scope*: the multiplayer has to be real, the pool is open to all
players, and the standard is a game people would want to play rather than a technical exercise with a
multiplayer sticker on it. Three consequences:

- **Commercial pressure is gone.** No monetisation design, store presence, launch window, demo cadence or
  support obligation.
- **The engineering bar goes *up*, not down.** Nothing here is justified by shipping speed. Architecture,
  determinism, tooling and verification are the deliverable, which is why the walking skeleton has seventy
  tests and a twelve-row sit-down.
- **Async exists because of schedule mismatch, not population.** True on day one, with three players, forever.

---

## 2. The loop — one machine at three latencies

Every mode is one pipeline — **submit → wait → resolve → watch** — and then it repeats.

| Mode | Who fills the other seats | Barrier clears when | Latency |
|---|---|---|---|
| **Round-robin** | The shared pool — K stored defenses every wave, drawn at your stage | Immediately; the pool is already there | Days, or none |
| **Lobby** | The friends present | Everyone present has submitted | Minutes |
| **Co-op** | Unspecified — [open questions](open-questions.md) | — | — |

**The lobby is simultaneous-turn, not real-time.** Everyone builds, everyone submits, the game waits on the
last submission, then it all resolves and everyone watches. Nobody acts during a wave (§3), so there is nothing
to synchronise while one runs: the network's whole job is to collect N submissions and broadcast the result.
Live head-to-head therefore costs a barrier and a lobby, not a netcode layer.

**K = 10, fixed, in every mode.** Each round draws a fresh field of ten stored defenses and ten waves at the
same stage, so round four meets ten people's round-four defenses. Stage-matching dissolves the hardest problem
in the async model: a stored defense never has to evolve mid-match, because you only ever meet it at one moment
of its life. A lobby smaller than ten is topped up from the pool at that `(map, stage)`.

**The field is the variance control.** A round's result is the average across ten, so a single bad matchup is a
rounding error rather than a dent. The cost is that a round tends toward the field mean, which risks rounds
feeling alike; the antidote on file — deliberately undecided — is a **gamble**: opt out of the average for a
single opponent drawn from the distribution, with best-of-ten as its natural payoff. Not tunable before a real
field exists.

Compute is not a constraint: at 2.75 ms a match, ten rounds against ten opponents on two sides is about half a
second for an entire run.

**The pool needs depth at every stage, not just at the end.** Hand-authored defenses per stage answer it and
cost nothing architecturally — a hand-built defense and a stored one are the same object.
[Map rotation](#the-map-rotates-and-it-is-generated) multiplies that cost by the rotation rate, since a pool
indexed on `(map, stage)` is emptied by every turnover. That is the strongest argument in the document for slow
rotation, and why the cadence is an open question rather than a preference.

---

## 3. What a match is

### Both boards, at once

**Your composed wave runs at an opponent's defense while their wave runs at yours.** Two resolutions per round,
and you watch both. Legion TD 2's structure.

Both halves are live every round with nothing cross-fed to make them matter, which is the complete answer to
the *defense feels meaningless* hazard. **The standard fix — two currencies, each earned by doing the other
thing — is therefore not adopted.**

The cost lands on the interface, and it is not two boards but twenty, since a round resolves against a field of
ten. What the player actually watches is unanswered here: [seam 7](build-order.md#7--the-interface).

### Build phases between waves

A wave resolves with no input; then a build phase opens; then the next wave. This is what makes it a *game*
rather than an auto-battler, and it is what makes a stored ghost legal. A pooled defense is a snapshot of one
stage: it never plays itself, never replays a build order chosen against a different wave, and never follows a
policy you authored. **It is finished, at that stage, forever.**

### A run is ten waves, and health is money

**Ten waves. A round is one build phase plus the wave that follows it.** Fixed and public. Short over Element
TD's fifty is mercy rather than pacing — a player off the curve gets out and into the next run. Ten also keeps
10% interest compounding to about 2.6× across a run, which is why the purse needs no interest cap. **N is a
lifecycle parameter, not a constant, and lifting the run cap forces an interest cap.**

**Health is a pool denominated in gold; a leaked creep costs health equal to its cost, one for one.** A 10-cost
creep does 10 damage. Health and money are therefore the same unit:

- **Conceding a wave has a literal exchange rate.** Underbuilding your defense to fund your offense already
  *is* spending health, so there is no sell-health button.
- **Damage taken in a round is the field average, not the sum.** Summing ten opponents' leaks would kill
  everybody immediately.
- **The pool is about three waves' worth of average creep value** — one deliberate concession affordable, a
  second a real decision, a losing player out around wave six or seven. A sweep target.
- **Gold cannot repair health.** Permanent depletion makes the pool a clock, and the clock ejects a struggling
  player.

**Zero health ends the run.** For the harness, death is a **flag** rather than a rule, so a sweep can run in
no-death mode and always yield ten rounds of data.

**Runs rank by waves survived first, then health remaining**, so the graded pool is both the in-run resource
and the final placing and no third number is invented. **The offense never enters the placing; it pays in gold
and nothing else** — in tension with [the attacking half being as deep as the defending
one](#depth-is-the-point), which [seam 1](build-order.md#1--the-match-format) reconciles.

**A run's outcome is a vector, not a score:** per round, `(leak cost dealt, leak cost taken)`, plus how the run
terminated. Health, waves survived, any score and what every wave was paid are folds over it; a scalar would
force re-simulation to recover them. A round's **offense** score is the average of the ten rather than the
best, symmetric with the damage rule.

### One purse

**One currency, called gold. Every coin of income lands as gold, and gold buys the defense and the offense
alike.** The one thing it does not buy is a capstone, which is
[granted at a gate](#a-capstone-is-granted-never-earned) rather than earned.

The objection a single wallet must answer: a coin spent attacking is simply gone, making attacking a pure tempo
loss and, at equilibrium, **dominated**. The answer is a payback, and a second currency is not the cheapest way
to supply one.

- **Attacking pays back through damage, not a second wallet.** Income is a flat base per wave **plus a bonus
  on top, at 25% of the leak cost your wave dealt**. Every point of health damage pays gold at that rate,
  uncapped: a wave that deals eighteen times as much is paid eighteen times as much. **Holding your defense
  well pays nothing extra** — a defense already pays by not costing you health, and health is the run's clock.
- **A creep is bought once and attacks every round after.** Buying is permanent: what you paid for in round
  three is still walking in round ten, and the round is charged only for what it *adds* to the wave. So an
  early purchase compounds — it is a stream of leak cost for the rest of the run rather than one round's
  worth — and a wave can only ever grow. There is no selling a creep back and no leaving one at home, which
  makes a bad early buy a lasting mistake and is the point.
- **The whole wave is rearranged every round.** A slot's position is its release order, and the order is a
  decision the round makes over everything it fields, not only over what it just bought. A creep fills at most
  one slot, so buying more of something you already send raises that box rather than opening a second one —
  until that box is at its count cap, and then the only way up is a box you have not opened yet.
- **Nothing is unlocked, and what is rationed is capacity.** Every creep in the roster is sendable from wave
  one, priced and nothing else. What [the gate rounds](#three-gates-at-waves-3-6-and-9) ration is **how many
  kinds a wave carries and how many of each**, never which kinds — so the scarcity on the sending side is the
  purse *and* a public schedule, and the schedule is what gives a round's spending a shape.
- **Timing comes from interest.** Unspent gold banks at **10% a wave, rounded up**, uncapped for now. Every
  purchase is measured against compounding, and adding nothing to the wave is investment rather than waste.

Each build phase therefore stays one decision over one wallet, and the three gate rounds are the deliberate
exception.

**No money moves between players.** Denial — Legion TD 2's rule that leaking pays the attacker — is rejected.
You are paid for what your wave dealt, never against a named opponent, so the payment reads the same whether
the field is one lobby or a global population and beating somebody takes nothing off them. The rate is never
negative: a wave is never charged for attacking.

**The bonus is bounded by the wave and by nothing else.** Leak cost sums price times leaked over a wave's own
orders, so a round deals at most the full price of the wave it sent — which is computable from a stored
decision without playing anything, and is what lets a stored stream still be refused at load for a decision no
run could have afforded.

Base, rate and creep costs are sweep targets, and a permanent purchase makes the last of those the sharpest of
them: a creep's price is paid once against every remaining round of leak cost it deals, so a small retune of
the cost column moves a whole run.

### Depth is the point

**A target rather than a design: the build space should be combinatorial, not a menu.** The reference is
**Element TD**, where picking elements unlocked dual-element combinations and the play lived in the synergies
rather than in any single tower — a space still being discovered after fifty runs, out of a roster small enough
for one person to build and a harness to sweep.

Three commitments, all direction rather than mechanism:

- **The attacking half is as deep as the defending half.** The corollary of both boards being live.
- **Your defense decides your offense** — a tower of a given type unlocks a skill tree for the creeps you can
  buy, so the pool you send from is a consequence of what you built.

  ⚠️ **The one piece of direction the research pushed back on.** Exactly one shipped precedent — a Bloons TD
  Battles 1 upgrade that let you send a tier *earlier*, gating the schedule rather than the pool, shipped with a
  carve-out and removed in the sequel. Every other game surveyed gates the opposite way: constrain the defense,
  keep the attacking vocabulary universal, because *a send is only a read if both players already know the whole
  menu*. Of the four failure modes it names, two bite hardest here — **double-dominance under one purse**, and
  **counter-picking collapsing to a lookup**, since a stored ghost shows you the defense before you compose.
  [Seam 1](build-order.md#1--the-match-format) owns the call.

- **You choose the order they come out in.** A wave is a sequence, not a bag —
  [`content/wave.txt`](../content/wave.txt) is already an ordered list of `(tick, type, count)`. Order is a real
  lever only where the path is single file, so [the maze](#the-board-is-a-maze) owes it one entrance,
  convergent rather than divergent branching, or guaranteed single-file sections. Two further preconditions the
  skeleton found the hard way: ordering is unobservable when units share a speed, and unobservable again when a
  count spawns as one pile — *a count is a column, not a pile.*

Creeps get **a roster with classes and roles** — tanks, damage, support, swarm, specialists — rather than a
stat ladder. Whether **one unit schema, two roles** survives that is
[seam 3](build-order.md#3--the-roster)'s to inherit.

**None of this is a mechanism yet, and it is not to be built from this section.**
[Seam 1](build-order.md#1--the-match-format) chooses from what
[the depth research](research/build-depth-in-tower-defense.md) found, including two findings that change what
*combinatorial* can mean: there are two structurally different ways to manufacture it and **only the generative
one** — a small vocabulary minting a large roster, as six element names predict fifty-six towers — is
simultaneously a depth mechanism, an accessibility mechanism, and enumerable by the harness rather than
sampled; and **Element TD's depth is the metered picks, not the combination table**, with every pick after the
first paid for inside the simulation rather than chosen from a menu.

### The board is a maze

**The geometry is a maze, and the goal is one that is far less solvable. Placements sit at three elevation
levels, and elevation changes range by the difference in height** — not by the height itself — which makes
**where** and **what** one decision instead of two.

**A map folds; it never branches.** There is always exactly one path from entrance to exit, and the player
never alters the route by building. That is a standing rule rather than a deferral, and it is what keeps a
search out of the simulation — see [the decision log](decision-log.md#16-august-2026-later--one-format-version-and-the-map-it-is-for).

Five consequences, none optional:

- **It is the genre's strongest skill axis.** A creep crossing a board in a straight line spends about two
  seconds under fire; the same board folded into a switchback, twelve — a sixfold damage multiplier bought with
  no gold at all.
- **No pathfinder, and no line of sight.** `HexMap` already traces the single corridor at load and asserts it
  never branches; folding it changes none of that. Both were priced as determinism obligations in the hottest
  loop, and both are out permanently.
- **Range is a signed difference.** `baseRange + (towerLevel − targetLevel) × 250`, in milli-hex, where a level
  is half a block: shooting down a whole block buys half a hex, shooting up one costs half a hex. Anything with
  a radius instead reads as a sphere — `hexDistance × 1000 + |levelDifference| × 250 ≤ radius` — where height
  only ever costs, so a tower on a cliff cannot blanket the board. A floor guarantees any tower reaches the
  hexes touching it.
- **Elevation is a third coordinate, and the map carries it.** The hex map gains a level layer: a format
  version and a hash-layout bump. `TowerLayout` does *not* — a tower stands on a hex and the hex knows its
  level — so the ghost record's format survives untouched. Cheap now, expensive later.
- **"Far less solvable" is a measurable target**, not a feeling — [§9](#9-the-planning-phase-is-the-game).

⚠️ **The bill it presents is ordering.** The single path is what preserves the send column; a map is designed
to keep that order rather than merely to be interesting.

⚠️ **Until the maze lands, the board is a hand-drawn 51-hex corridor one cell wide that folds and climbs two
whole blocks, and every number priced against it is provisional by construction.** The corridor is still
one hex wide, so placements along a single leg remain close to equivalent; what the climb adds is that a
block is worth half a hex of reach, which makes the same cell a different placement depending on what it stands
on. **The grid under it is now half a block per level and nine levels deep, and the committed board spends five
of those nine** — every change of height on it is a single level, which is half a block, and nothing on it
steps a whole one. How much of the thinness that actually removes is not yet measured — a fact about *this*
board, not about the mechanism.

### The map rotates, and it is generated

**The round-robin runs on one map at a time and that map turns over on a schedule. Maps are generated rather
than authored, and a map is identified by a seed rather than stored as a file.**

⚠️ **The first map is hand-authored, and generation and rotation are deferred behind it.** Selection pressure
needs a fitness function, and a fitness function needs one map that is demonstrably good to calibrate against.
The sweep already takes its map as a parameter, so scoring a candidate costs a flag — which is the half of
generated rotation that is paid for in advance, and which is worth nothing without a known-good reference.

A hard map buys time against solving; a map nobody has seen buys it permanently. The maze makes each map deep,
rotation makes the depth renewable — which matters more here than elsewhere because [§4](#4-what-persists) has
no unlock ramp to spread learning across weeks. Three load-bearing properties:

- **Everyone in a cycle plays the same map.** What makes results comparable at all — Slay the Spire's shared
  daily seed — and required by [both boards at once](#both-boards-at-once), since two resolutions on two
  different maps are not a match.
- **A map is a seed, not an asset.** `HexMap.FromCells` builds a grid without a filesystem, `Match` takes a
  `ulong seed`, and `HexMap.MapHash` hashes the parsed grid, so a generated map is a handful of bytes in a
  record and a hash the server can check. **Rotation costs the ghost format nothing and anti-cheat still falls
  out for free.**
- **Generation is filtered by simulation, not by taste.** Search-based procedural generation scores candidates
  with a fitness function and lets the score steer the next generation; the literature's standing complaint is
  that a simulation-based fitness function is too slow at scale — **the one problem this project does not
  have.** At 2.75 ms a match, a candidate map is swept by the same harness that prices units and scored on how
  widely outcomes spread across good plans. A map where every competent plan scores the same is solved.
  **Maps are selected against a measurement of their own solvability.**

⚠️ **Rotation partitions the ghost pool.** `GhostRecord` already carries a `MapHash` and a `MapHandle`, so a
`(map, stage)` index needs no format change. What it needs is **population**, and a daily map means a cold pool
at every stage every day. Three ways out, none chosen: rotate slowly enough that the pool fills, generate the
hand-authored floor for each map with the map, or carry the pool across maps and accept that a stored defense
meets waves on geometry it was not built for. [Open question](open-questions.md).

⚠️ **Rotation must be generated, never curated.** A map-of-the-week that a person authors is live-service
cadence, which [§4](#4-what-persists) rules out and [§7](#7-what-runs-it) has no room for. A scheduler drawing
from a pre-generated archive is not: the archive is built once, offline, by the harness, and the schedule is
arithmetic on a date.

### Three gates, at waves 3, 6 and 9

**Three fixed, known rounds where the run opens up. The schedule is public; what each player does with it is
not.**

With the player composing the whole wave there is otherwise no public constant to prepare against, so
*preparation* has nothing to be a skill about. A gate supplies the constant without supplying the content:
**everyone knows the wave gets wider at wave 6, and nobody knows what they widened it with.** It is also
progressive disclosure that resets every run, the only shape [§4](#4-what-persists) permits.

Wave 1 is the starting state and a gate at wave 10 would have nothing after it, so **wave 10 is the payoff
round**. A gate does three things at once — two on the attacking side, one on the defending side.

| At every gate | What it does |
|---|---|
| **The wave gains two slots** | How many kinds of creep it may field at once: 2 to begin with, then 4, 6 and 8 |
| **Every slot's count cap rises by ten** | How many of one kind a slot may hold: 10 to begin with, then 20, 30 and 40 |
| **One capstone is granted** | A single defense-only token, spendable on capstoning one tower — [below](#a-capstone-is-granted-never-earned) |

So a ten-wave run's capacity is fixed, public and known before it starts:

| Waves | Slots | Count cap per slot | Capstones held |
|---|---|---|---|
| 1–2 | 2 | 10 | 0 |
| 3–5 | 4 | 20 | 1 |
| 6–8 | 6 | 30 | 2 |
| 9–10 | 8 | 40 | 3 |

**A gate is the round the offense is worth investing in, and putting that on a clock is the point.** A
purchase is permanent and a wave only ever grows, so what an attacking purchase really spends is capacity:
gold buys a creep, and only a gate makes room for it. Two consequences the design is after:

- **The cap forces breadth.** Without one, compounding gold ends every run as a single enormous box of
  whichever creep is most cost-efficient — and send order stops being a decision when there is only one thing
  to order. A slot at its cap cannot absorb another coin, so the next one goes into a *different* creep, into
  the defense, or into the bank at 10%.
- **A gate is an economy beat.** Two rounds of saving against a known round where the wave can finally take
  the money is a timing decision, and timing is what the purse exists to make players practise. Arriving at a
  gate with nothing banked is the mistake it is there to punish.

**What a gate does not do is decide what is on the menu.** Every creep is purchasable from wave one, so a gate
opens *room* rather than options, and nothing about the capacity schedule is drawn.

Every integer here is a ruleset row and a sweep target: the two starting values, the two steps, the three
rounds, and how many tokens a gate hands over.

⚠️ **The variance half of a gate is designed and deliberately not built.** At each gate three **game changer**
creeps were to join that round's [public offering](#the-offering-is-public), with the player taking one thing
from the combined list — nine distinct creeps across the three gates, escalating, wave 9's opening a genuine
counter as a steep gradient on `bonusVsTag` rather than a binary immunity. It waits on a roster deep enough
for a menu to be a choice, and its one hard constraint on [seam 3](build-order.md#3--the-roster) is unchanged:
**for every gate, its counter must already be purchasable strictly before it.** Gates open offense, never
defense — preparation happens on the other side of the board.

That half is the one with two layers, turning over at different rates:

| | What it is | How long it holds |
|---|---|---|
| **Shape** | Which gate carries which tier, and which one opens the counter | **Per [rotation](#the-map-rotates-and-it-is-generated)** — public, stable, learnable |
| **Filling** | *Which* three creeps sit on each gate's menu | **Per run** — drawn from that gate's tier pool, revealed at run start |

Preparation is a skill about the **shape**; replay value comes from the **filling**. A single-layer schedule
cannot have both — fixed everywhere is solved by Tuesday, drawn everywhere has nothing to prepare against.
**The gates are the constant; the ordinary offering is the churn**, drawn per round and identical for
everyone, which is where most of a week's variety comes from.

**The ghost pool does not shard.** Ghosts draw on `(map, stage)` alone, and a ghost from this rotation was
played under the same shape and the same capacity schedule, so anything it fields is from the same tier on the
same axis — unfamiliar, never unanticipatable. That is what makes a per-run filling safe without paying for
variance with a thinner pool.

### A capstone is granted, never earned

**At each gate the player is handed one unit of a second currency, and the only thing it buys is a capstone —
the top tier of a tower line, spent on a tower already standing.** Three gates, three capstones a run.

This does not reopen the two-purse question [one purse](#one-purse) settled. A second *wallet* was declined
because income split across two pools makes every purchase a question about which pool to feed, and because a
currency earned by attacking is how other games pay attacking back. This is neither: **it has no income, no
exchange rate and exactly one sink.** Nothing earns it, nothing else spends it, and it converts to nothing.

Three things it buys the design:

- **The defending side gets a fixed reward on the clock the attacking side is already on.** A gate that only
  widened the wave would make gate rounds pure offense. A token makes the same round a defensive decision too:
  *which* tower is worth the top of its line, when you only ever get three.
- **The best of your defense is rationed by something that is not money.** Gold decides how much defense you
  have; the token decides how good the best of it is, and no amount of banking substitutes for it.
- **It gives the roster somewhere to build toward.** A tower line that terminates in a capstone has a top, and
  three tokens a run makes choosing *between* lines the interesting part rather than climbing one.

⚠️ **The currency has no name yet, and naming it is not an implementation detail** — see
[open questions](open-questions.md). Neither is the question of whether a token banks or must be spent on the
round it arrives.

### How a shot resolves

**Three attack types, three armour types, and one line of arithmetic.**

| Attack ↓ / Armour → | Swift | Armoured | Arcane |
|---|---|---|---|
| **Pierce** | 140% | 70% | 100% |
| **Impact** | 70% | 100% | 140% |
| **Magic** | 100% | 140% | 70% |

```
dealt = (base + bonus) * cell / (100 + armour)      // one multiply, one divide
if (dealt < 1) dealt = 1;                            // the floor
```

Every **row** and every **column** is a permutation of (70, 100, 140), which makes it a Latin square rather
than merely a table: **no attack type is globally better and no armour type globally tougher**, every cell is
reachable, and the whole thing fits in a player's head. The 2:1 spread moves shots-to-kill by exactly double —
a real read, and never an unwinnable draw.

**The armour coefficient is folded to 1.** The reduction shape is the one Warcraft 3 and League of Legends
independently arrived at, whose property is that *effective health is linear in armour*, so armour stacks
without a cap. A coefficient of 1 makes the authored number read as its own meaning: **one point of armour is
one percent of base effective health.** Every bit of strength a larger coefficient would buy is bought instead
by authoring a larger armour number, and the ruleset loses a constant nobody can check by eye.

**Hard counters do not come from this table.** They come from `bonusVsTag`, a per-gate integer added to the
base before typing and mitigation. That separation is why the matrix could stay narrow: *lean narrow* and *must
express a counter* were two constraints on two different layers. Because the bonus joins the hit rather than
bypassing it, **a high-armour game changer blunts its own counter.**

**Every damage and health number in the game carries a ×10 scale.** At the scale the skeleton shipped, a
9-damage bolt dealt **8 damage for eleven consecutive points of armour** — armour a player cannot feel and a
sweep cannot tune. The ×10 restores resolution without touching the arithmetic.

Three candidates the arithmetic eliminated, recorded because each reads as reasonable on paper:

- **Warcraft 3's 40:1 spread is impossible here, not merely violent.** A 15-damage hit through a 5% cell is
  `15 × 5 / 100` = **zero**: the type chart deletes the hit before armour is consulted, and a damage floor
  "rescues" that only by making every cell beneath it identical.
- **Flat subtraction is out with the number attached.** At armour 5 a five-archer volley of 5 damage each
  delivers **5** of its nominal 25; a single 25-damage cannon delivers **20**. Under the decided formula the
  volley and the cannon fall off *together* across the whole armour range.
- **The two-step and fused forms of the same algebra are different functions.** `d × cell / 100` then
  `× 100 / (100 + armour)` composes algebraically to the fused form, but across 411,600 swept triples they
  disagree on **42.7%**, and the fused form is never lower because it truncates once instead of twice —
  [ADR-0001](adr/0001-fixed-point-arithmetic.md) arriving in practice, and why the expression is written down
  here rather than left to whoever implements it.

**The shape survives the maze; the constants do not.** The Latin square, the fused expression, the ×10 scale
and the floor are independent of geometry. The cell values and armour numbers are priced against the corridor
[the board](#the-board-is-a-maze) is removing, and are sweep targets. Live numbers:
[`content/ruleset.txt`](../content/ruleset.txt). Derivation:
[`prototypes/damage-matrix-arithmetic.py`](prototypes/damage-matrix-arithmetic.py).

### The offering is public

**Each build phase offers the same small set of choices to every player in the match.** Not a private random
draw; one public offering everybody sees, **drawn fresh each round** — identical across the match, different
between runs — and on a gate round the three game changers are drawn into it. This is Mechabellum's
reinforcement system, whose players describe the consequence exactly right: the opponent sees the same choices,
*so it becomes a mind game.*

It buys three things at once, which is why it is here rather than in a seam:

- **A shared vocabulary.** A send is only a *read* if both players already know the menu, and a public offering
  makes the menu public by construction.
- **Scarcity without a private lottery.** The metered-offering depth mechanism, minus the variance that made
  the research rank it third.
- **A second-order decision.** Taking the thing you need is one decision; taking the thing you need *because
  your opponent also needs it* is a better one.

### Scouting depends on the mode

The two multiplayer modes differ on information, and the difference is the point rather than an accident of
implementation.

| | **Round-robin (async)** | **Lobby (live)** |
|---|---|---|
| Opponent's defense | Not shown as a board to compose against | **Shown as of the end of the previous round** — never live |
| What you optimise for | Performance across *many* defenses | Performance against *one known* defense |
| The skill | Robustness, and reading the shared offering | Hedging and counter-picking, TFT-style |
| Feedback | Mean and spread over the field | The board in front of you |

**In the lobby you can scout, and only backwards.** The **stale** variant is the one taken, because it prices
*change*: what they had is evidence, not truth. **What they are building right now is never shown, in any
mode.** This is Teamfight Tactics' loop, where between-round scouting decides itemisation, positioning and
whether to commit to a composition at all.

**In the round-robin you cannot scout a defense**, and that fixes a real defect rather than accepting a
limitation. A frozen defense is fully inspectable and cannot react or lie, so perfect information about it
produces optimisation rather than inference and degrades to a table. Withholding the board and paying the
player in **statistics over the field** instead is the fix, and it makes the async mode a genuinely different
game from the lobby rather than a slower copy of it.

**What you *can* see in the round-robin is the incoming waves**, which is a different object: a snapshot shows
a wave being sent at your stage, never a defense, and a wave is a composition you build against rather than a
board you solve. **Ten snapshots are free per run and further ones are bought with gold.** Scouting is
therefore one mechanic across all three latencies — *pay to reduce the blur on what you are facing* — and only
the source of the blur differs by mode.

**You do not know exactly what your wave will do**, and the reason matters: not because the simulation is
hidden or random, but because your wave is measured against *many* defenses, so the honest answer to "what does
this do?" is a distribution rather than a number. That is a real uncertainty a player can reason about, and it
is the one thing standing between a deterministic game and a solved one.

---

## 4. What persists

**Nothing but your rating.** Every run starts from the same position with the same options. No unlocks, no
roster to develop, no seasons, no account levels, no collection. This is Slay the Spire's daily and Backpack
Battles' model, and the single most scope-protective decision here after *not commercial*.

| What this buys | What it costs |
|---|---|
| A friend who joins in month six plays the same game you do | No dopamine drip — the play has to be good enough on its own |
| Nobody can out-grind anybody; the ladder measures skill and nothing else | No gentle first hour bought by a drip-fed option space |
| Matchmaking is a skill problem, never a power-level problem | Every unit must be interesting from the first run, since none are held back |
| The smallest content surface of any option — nothing exists to be unlocked | |
| **No live-service cadence, ever** — the one obligation a personal build must never take on | |

---

## 5. How it is balanced

**Computed. The simulation tells you.** The deterministic integer sim and headless CLI exist (`sim/`, `simcli/`,
`tools/run-headless-match.ps1`), and the balance harness sits on top: sweep every unit against every defense
across thousands of matches, produce win-rate and cost-efficiency matrices, and let a red cell name what is
mispriced before a human notices.

**A sweep is a minute of compute, not a night's.** `BudgetTests` times the committed match at **2.75 ms** on
the development laptop — roughly **360 matches per second on one core** — so a ten-thousand-matchup sweep is
under a minute. A harness that cheap is a `simcli` mode and a CSV, not a project, and it is worth building
*before* the roster is large: a red cell naming a mispriced unit while there are only eight units makes every
subsequent unit cheaper to author.

**The harness has a second job.** Pointed at units it prices them; pointed at **maps** it scores them, which is
what makes [generated rotation](#the-map-rotates-and-it-is-generated) possible. Same sweep, same CSV, different
axis, and one design consequence: **the sweep takes its map as a parameter, not as a fixed input.**

**It is not a luxury at this scale — it is the only option that works.** Telemetry balancing needs player volume
a personal build will never have. Hand balancing finds only the loudest problems and reliably confuses *feels
strong* with *is strong*. A harness is the only method whose accuracy does not depend on an audience.

Two limits, both stated plainly:

- **A harness measures what you tell it to measure.** It will find a mispriced tower and it will never tell you
  a unit is boring. Play remains the oracle for whether something is *fun* to lose to.
- **Computed balance is a budget, not a licence.** Every mechanism that manufactures depth does it by making
  one unit's value depend on the other units you chose, and that dependence is precisely what stops a harness
  pricing a unit in isolation. **Depth and computed balance are the same axis pointing opposite ways.** What
  matters is not *whether* a mechanism creates dependence but what the dependence is **indexed by**, because
  that index has a cardinality you can write down in advance.
  [Seam 1](build-order.md#1--the-match-format) spends this budget whether or not it knows it.

---

## 6. What it looks like

**Stylized low-poly 3D, a free perspective orbit that goes in close enough to read one model, and no
billboards, no flat cards, no painted-on shadows.**

### Legible to a stranger

**Juicy and accessible.** Every hit lands with weight, and a person who has never seen it can tell what is
happening within a minute. This sits on top of §3's depth, and the pair is not a contradiction — Bloons TD 6 is
the standing proof — but it is a tension to manage actively, and one settled decision makes it harder here than
elsewhere.

**The usual accessibility ramp is unavailable — but it has moved rather than gone.** Almost every deep game
onboards by *withholding*, and [§4](#4-what-persists) rules that out, so the full space is present on run one.
The replacement, already shipped in three games, is to put the disclosure ramp **inside the run**: Element TD 2
meters eleven element picks across fifty waves, Super Auto Pets unlocks shop tier *X* on turn 2*X*−1, BTD6's
hero starts at level 1 every game. Each resets every run, which is the shape §4 permits and strictly better
here, since it never advantages the player who has been at it longest. Of eight surveyed accessibility
mechanisms four need persistence and are dead here; the strongest survivor is **a generative, compressible
roster**, the only one that reduces what a player must *remember* rather than what they must *see*
([the note](research/build-depth-in-tower-defense.md)).

Two consequences:

- **Juice is a feature with a budget, not a polish pass.** Hit reactions, death weight, muzzle flashes, impact
  effects, number popups, screen shake, easing on every UI transition. None of it requires an artist.
- **Legibility is a design constraint on the depth, not just on the art.** A mechanism that cannot be read off
  the screen fails the accessibility pillar however deep it is. A real veto, and it should be used as one.

### The art pipeline

**Buy [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — $150 — and supplement only with free
CC0. KayKit's Skeletons are the creeps and the Adventurers are the towers**; each skeleton was built as a
specific adventurer's deliberate twin, so the two sides of the board are the two halves of one pack. Two facts
make this a *final-deliverable* pipeline rather than a placeholder one:

- **Recolouring is editing one small PNG.** KayKit models are UV-mapped onto flat palette textures, not painted
  detail maps, and the build already imports exactly that — `client/Assets/Art/Characters/ranger_texture.png`,
  `skeleton_texture.png`, `Buildings/hexagons_medieval.png`. A faction variant costs one texture and one
  material, and zero geometry.
- **The $150 buys `.blend` sources, not permission.** KayKit's free tiers carry the same CC0 as the paid ones,
  and CC0 already grants modification, commercially, with no attribution and no royalty. The money buys
  bundling, the original geometry and rig, and every future pack.

⚠️ **Confirm before paying.** Every licence and price claim here was read as extracted text rather than in a
browser. [#56](https://github.com/ssalter21/tower-defense-game/issues/56) is open for exactly this check.

### Where the effort goes

Not into custom character geometry. Stock models, and everything else into presentation:

1. **Lighting, VFX and camera.** URP lighting, colour grading, hit and death effects, projectile trails, camera
   framing. This is what separates a build that looks amateur from one that looks composed, and none of it
   needs an artist. Kenney's Particle Pack is the identified free source.
2. **Recolouring into factions.** Your units one palette, your opponent's another, tower tiers reading by
   colour. Cheap, and it doubles as the readability fix for watching two boards at once.
3. **UI and information design.** The screen must show two simultaneous battles, an economy, a build menu, and
   what your opponent just did. The hardest visual problem in the design, and it is UI work rather than art
   work.

**Art is not a risk item.** It is a workstream with a known pipeline, a known licence and a known cost.

---

## 7. What runs it

**A real server, self-run.** It holds accounts, the shared ghost pool, submissions, standings and replays.

Because the simulation is deterministic, the server can re-run any claimed result and compare — **anti-cheat
falls out for free**, and a client's reported outcome is a claim rather than a fact. Storage stays trivial: a
ghost record is hundreds of bytes, so a hundred thousand stored defenses is under a hundred megabytes.

**This is the only permanent obligation in the entire plan.** Everything else can be put down and picked up. A
service must stay up, be backed up and be secured for as long as the pool is meant to mean anything. It is
taken on deliberately, because *the pool is open to all players* is not achievable any other way.

---

## 8. Out of scope

Ruled beyond the destination. These do not graduate; they return only if the destination is redrawn.

- **Monetisation, pricing, store presence, wishlists, marketing, launch windows, demo cadence.** Consequences
  of §1.
- **Progression systems *between* runs.** Unlocks, collections, account levels, roster development, seasons,
  battle passes. Consequences of §4. **In-run progression is not ruled out and never was** — the skill tree a
  tower opens onto your creep pool lives and dies inside one run, which is what makes it legal.
- **Realtime netcode.** Lockstep, rollback, tick synchronisation, prediction. Consequence of §2 — no mode needs
  it.
- **Discovery, curation and browsing surfaces.** Consequence of the per-round draw.
- **Custom character geometry as the default.** Stock models are the pipeline; `.blend` editing is a tool kept
  for specific need, not a programme of work.
- **Moderation and community management at scale.** The pool is open, but a personal build does not take on a
  trust-and-safety function.

---

## 9. The planning phase is the game

Direction, in the same sense §3 is; the mechanisms belong to the seams. Companion note:
[Making the plan the game](research/planning-phase-and-simulated-stats.html).

**Nothing happens during a wave.** That is what makes a stored ghost a legal opponent and a submission barrier
a substitute for netcode, and the consequence is that **every axis of skill this design has is collected in one
phase**, where every comparison game in the genre spreads it across two. A build phase that offers three clicks
and a confirm button is not a smaller version of Legion TD 2's build phase. It is the entire game.

So the build-and-compose phase is treated as the main event and budgeted like one, from two assets this project
already owns: a **deterministic integer simulation that resolves a match in 2.75 ms** — ~360 matches a second
on one core, so anything a player might want to know is a computation rather than a guess — and a **record
format that stores inputs rather than outputs**, so any position can be re-run, re-run with one thing changed,
or re-run ten thousand times against a field.

**The models are Football Manager and the Zachtronics histogram**: the match is watched and the analysis lands
*after* it. Into the Breach supplies the calibration — perfect information about mechanism, never about
outcome. **Path of Building is the model this section declines**: the planning phase is not a calculator and it
forecasts nothing. The budget is spent on making the **rules** legible and the **retrospective** rich, not on
predicting the result.

### What the player may compute before committing

**Nothing is forecast.** The planning surface exposes the rules completely and predicts nothing, in any mode,
free or paid.

- **Mechanism is free, total and always on.** Range overlays, costs, interest, the offering, and the
  [fused expression](#how-a-shot-resolves) evaluated live for any attacker against any target. This is Into the
  Breach's calibration, and hiding any of it would only tax the players who do not keep a spreadsheet.
  **"Damage preview" means one shot, not one round** — showing what your *wave* does to a *field* is a
  forecast, and there is no such thing in this game.
- **Outcome is not computed at all.** No preview, no dummy defense, no distribution, no rate, no number that
  predicts a result. **The offense in particular gets nothing**: a wave is composed from the rules and from
  memory, which gives the two halves genuinely different textures — the defense is engineering, the offense is
  judgement. The one pre-commit channel is [scouting incoming waves](#scouting-depends-on-the-mode).
- **The simulator's home is the retrospective.** After a round, analysis is free, unlimited and exact against
  the real field.

### What the retrospective is for

A run against a hundred stored defenses is under a second of compute. That is a mechanic, a reward structure
and a presentation layer no tower defense has, because none could afford it.

- **A distribution instead of a result.** Reward **both the best you achieved and the average**: peak play and
  robust play are different skills, and a player optimising one will do poorly at the other. SpaceChem's three
  competing metrics exist for this reason.
- **Placement against the aggregate, not a leaderboard.** A leaderboard's only message to most players is
  *that* they are bad and not *by how much*, and a name at the top is an incentive to cheat. A server that
  re-simulates every claim already has the anti-cheat half; the histogram is the presentation half.
- **Retrospective analysis with real teeth.** Re-running a finished match with one purchase changed is one more
  match — 2.75 ms — so a run is reviewable the way a chess game is: *wave 7, the sniper instead of the tank was
  worth this much.* The genre has no equivalent.
- **A computed highlight reel.** The whole match resolves before anything is drawn, so the moments worth
  showing are *chosen* rather than recorded — the closest call, the first leak, the shot that decided it.
  Nobody watches a hundred matches; they watch the three that came down to one unit. **The direction is a field
  of 100+**, which raises `K` — a parameter rather than a constant.
- **"Far less solvable" becomes a filter.** Pointed at maps and strategies, the harness reports how wide the
  outcome spread is across good plans, which is what *solvable* means written down. Wired back into a generator
  it becomes **selection pressure** — the search-based loop
  [§3](#the-map-rotates-and-it-is-generated) rests on.
- **Seeding has to be as cheap as running.** `HexMap.FromCells` builds a grid from bytes, `Match` threads a
  `ulong seed`, and `MapHash` hashes the parsed grid so a server can verify which map a client claims to have
  played — the filesystem-free discipline
  [ADR-0018](adr/0018-the-simulation-never-touches-the-filesystem.md) already requires. **What is missing is a
  `simcli` mode that turns a seed into a map and a match.**

### Two constraints on anything built here

- **The price can only be charged on data, never on compute.** The simulation is deterministic, records store
  inputs, and a match is 2.75 ms — so anything the client holds the *data* for, a third-party calculator
  computes for free regardless of what the game charges. Query caps and per-simulation fees are unenforceable
  by construction. What the server can withhold is **the pool**.
- **Roughness has to come from unknown inputs, never from fuzzed arithmetic.** Your own towers are client-side,
  so an external tool resolves them exactly against any *specified* wave. A prediction is only honestly rough
  when the player does not know which waves are coming — the same structural trick as drawing the field after
  the commit, and the reason a paid predictor must sell **information** rather than precision.

⚠️ **A simulation that answers everything deletes the game.** If the player can compute their exact result
before committing there is no decision left, only data entry. The antidote is structural:
[the round-robin measures your wave against many defenses](#scouting-depends-on-the-mode), so the honest answer
is a distribution rather than a number. **Uncertainty comes from the breadth of the field, not from hidden
state or dice** — the only form of it compatible with a deterministic, re-simulable, cheat-proof game.

---

## Sources

Everything factual here is verifiable in this repository, established in the five archived deep dives, or
listed below.

1. **This repository** — `sim/`, `simcli/`, `content/`, `client/Assets/`, `tools/`,
   [`docs/sit-down.md`](sit-down.md).
2. **The five deep dives** — [`docs/archive/`](archive/README.md). Their claims are inherited except where
   [the archive index](archive/README.md) replaces them.
3. **KayKit** — [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete), CC0, $150, `.blend`
   sources. Licence and price **pending browser confirmation** under
   [#56](https://github.com/ssalter21/tower-defense-game/issues/56).
4. **CC0 1.0** — [Creative Commons deed](https://creativecommons.org/publicdomain/zero/1.0/): copy, modify,
   distribute and perform, including commercially, without permission.
5. **Legion TD 2** — the both-boards-at-once structure, and the two-currency loop whose *problem statement*
   [§3](#one-purse) adopts while declining the solution.
6. **Element TD** (Warcraft 3 mod) — the reference for combinatorial build depth. A target class, not a
   specification.
7. **Bloons TD 6** — the standing proof that legible-to-a-child and competitively deep are compatible.
8. **Super Auto Pets / Backpack Battles** — the per-round draw against a snapshot at the same stage, and the
   AI-fill answer to an empty pool.
9. **Supercell** — "Builder Base 2: Balancing Attacking, Defending and Builders", the source of the
   defense-feels-meaningless finding.
10. **Slay the Spire daily** — the nothing-persists-but-rating model, and the shared seed.
11. **Mechabellum** — the public shared offering, identical on both sides, which makes the shop a mind game.
12. **Teamfight Tactics** — between-round scouting as the loop the live lobby is modelled on.
13. **Zachtronics** (SpaceChem, Opus Magnum) — histograms instead of leaderboards, and competing optimisation
    metrics as a deliberate tension.
14. **Into the Breach** — perfect information about mechanism, never about outcome. The safety rail on §9.
15. **Path of Building** — the standing evidence that a planning tool can be the part of a game people love
    most, and the model §9 declines.
16. **Football Manager** — a match you watch rather than play, and the highlights-and-dashboard apparatus.
