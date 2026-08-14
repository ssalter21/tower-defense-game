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
terminated. Health, waves survived and any score are folds over it; a scalar would force re-simulation to
recover the two distributions the percentile bands read. A round's **offense** score is the average of the ten
rather than the best, symmetric with the damage rule.

### One purse

**One currency, called gold. Every coin of income lands as gold, and gold buys the defense and the offense
alike.**

The objection a single wallet must answer: a coin spent attacking is simply gone, making attacking a pure tempo
loss and, at equilibrium, **dominated**. The answer is a payback, and a second currency is not the cheapest way
to supply one.

- **Attacking pays back through performance, not a second wallet.** Income is a flat base per wave **plus a
  bonus on top**, paid in non-linear percentile bands over two distributions: how your wave performed, and how
  your defense performed, each against the field.
- **A creep is bought once and attacks every round after.** Buying is permanent: what you paid for in round
  three is still walking in round ten, and the round is charged only for what it *adds* to the wave. So an
  early purchase compounds — it is a stream of leak cost for the rest of the run rather than one round's
  worth — and a wave can only ever grow. There is no selling a creep back and no leaving one at home, which
  makes a bad early buy a lasting mistake and is the point.
- **The whole wave is rearranged every round.** A slot's position is its release order, and the order is a
  decision the round makes over everything it fields, not only over what it just bought. A creep fills at most
  one slot, so buying more of something you already send raises that box rather than opening a second one.
- **Nothing is gated and nothing is unlocked.** Every creep in the roster is sendable from wave one, priced
  and nothing else. The purse is the only scarcity on the sending side.
- **Timing comes from interest.** Unspent gold banks at **10% a wave, rounded up**, uncapped for now. Every
  purchase is measured against compounding, and adding nothing to the wave is investment rather than waste.

Each build phase therefore stays one decision over one wallet.

**No money moves between players.** Denial — Legion TD 2's rule that leaking pays the attacker — is rejected.
The coupling is **statistical**: you are paid against the field's distribution, never a named opponent, which
reads the same whether the field is one lobby or a global population. Bands are progressive and never negative.

Base, thresholds and creep costs are sweep targets, and a permanent purchase makes the last of those the
sharpest of them: a creep's price is paid once against every remaining round of leak cost it deals, so a small
retune of the cost column moves a whole run. One consequence: **the bonus needs
a distribution to measure against**, and until real ghosts are stored the harness's canned field supplies one —
which cannot tell the middle bands apart, so four authored bands behave as two until a real pool exists
([measured](research/a-canned-field-of-one-collapses-the-bands.md)).

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

**The geometry is a maze, and the goal is one that is far less solvable. Placements sit at several elevation
levels, and elevation grants range** — the common shipped form is *+1 range per level*, which makes **where**
and **what** one decision instead of two.

Four consequences, none optional:

- **It is the genre's strongest skill axis.** A creep crossing a board in a straight line spends about two
  seconds under fire; the same board folded into a switchback, twelve — a sixfold damage multiplier bought with
  no gold at all.
- **Pathfinding enters the simulation, and that is a determinism obligation.** An integer pathfinder with a
  *fixed, asserted* tie-break, held to `sim/`'s existing standards, in the hottest loop.
- **Elevation is a third coordinate, and coordinates are in the record.** `TowerLayout` and the hex map gain a
  level: a format version, a hash-layout bump and a retired ghost pool. Cheap now, expensive later.
- **"Far less solvable" is a measurable target**, not a feeling — [§9](#9-the-planning-phase-is-the-game).

⚠️ **The bill it presents is ordering.** A branching map dilutes send order, so the map must be designed to
keep the column rather than merely to be interesting.

⚠️ **Until the maze lands, the board is a 47-hex corridor one cell wide, and every number priced against it is
provisional by construction.** On one-wide geometry many placements are equivalent, so the build phase will
feel thin — a fact about *this* corridor, not about the mechanism.

### The map rotates, and it is generated

**The round-robin runs on one map at a time and that map turns over on a schedule. Maps are generated rather
than authored, and a map is identified by a seed rather than stored as a file.**

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

### Three anchors, a shape and a filling

**At fixed, known waves the run injects a major variance event. The schedule is public; what each player does
with it is not.**

With the player composing the whole wave there is otherwise no public constant to prepare against, so
*preparation* has nothing to be a skill about. An anchor schedule supplies the constant without supplying the
content: **everyone knows the flying units unlock at wave 9, and nobody knows who took them.** It is also
progressive disclosure that resets every run, the only shape [§4](#4-what-persists) permits.

**Anchors sit at waves 3, 6 and 9. At each one, three *game changer* creeps join that round's public offering,
and the player takes one thing from the combined list.** Wave 1 is the starting state and an anchor at wave 10
would have nothing after it, so **wave 10 is the payoff round**.

- **Anchors open offense, never defense.** Preparation happens on the other side of the board: you know what
  lands at wave 9, so you buy the answer by wave 8. An anchor that handed you a better *tower* would be a gift
  rather than a preparation problem. One hard constraint on [seam 3](build-order.md#3--the-roster): **for every
  anchor, its counter must already be purchasable strictly before it.**
- **The menu is merged, not additional.** A game changer competes head-to-head with an ordinary unlock. A free
  extra pick would end every run with every player holding all three, leaving only *when they field it*
  unknown; merged, **who has what is unknown too.** The ratio is a sweep parameter.
- **Anchors do not repeat, and they escalate.** A game changer appears on exactly one anchor's menu — nine
  distinct creeps per shape — and wave 9's three are stronger than wave 3's, matching the wave widening and the
  gold curve. A flat pool could hand someone a wave-9-grade unit at wave 3, where nothing yet answers it.
- **Exactly one anchor per shape opens a counter, and it is wave 9.** The other two are extreme points on
  existing axes — a very fast unit, a very tough one — answered by generally competent defense. Three counters
  would make a run turn on a single missed buy. **The counter is a steep gradient, not a wall**: it lives on
  `bonusVsTag` rather than a binary gate, so on the decided ruleset a prepared tower kills the wave-9 anchor in
  nine shots and an unprepared one in thirty-six — **4.00×**. Mis-preparing is punished, not eliminated.

**The schedule has a shape and a filling, and they turn over at different rates.**

| | What it is | How long it holds |
|---|---|---|
| **Shape** | Anchors at 3, 6, 9; which tier each is; which one is the counter anchor | **Per [rotation](#the-map-rotates-and-it-is-generated)** — public, stable, learnable |
| **Filling** | *Which* three creeps sit on each anchor's menu | **Per run** — drawn from that anchor's tier pool, revealed at run start |

Preparation is a skill about the **shape**; replay value comes from the **filling**. A single-layer schedule
cannot have both — fixed everywhere is solved by Tuesday, drawn everywhere has nothing to prepare against.
**The anchors are the constant; the ordinary offering is the churn**, drawn per round and identical for
everyone, which is where most of a week's variety comes from.

**Nothing bounds how many slots a wave carries.** Scarcity on the sending side is the purse and the
permanence of a purchase, not a slot count — see [One purse](#one-purse). A creep fills at most one slot, so
what a wave can hold is what the roster has.

**The ghost pool does not shard.** Ghosts draw on `(map, stage)` alone, and a ghost from this rotation was
played under the same shape, so anything it fields is from the same tier on the same axis — unfamiliar, never
unanticipatable. That is what makes a per-run filling safe without paying for variance with a thinner pool.

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

**Hard counters do not come from this table.** They come from `bonusVsTag`, a per-anchor integer added to the
base before typing and mitigation. That separation is why the matrix could stay narrow: *lean narrow* and *must
express a counter* were two constraints on two different layers. Because the bonus joins the hit rather than
bypassing it, **a high-armour anchor blunts its own counter.**

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
between runs — and on an anchor round the three game changers are drawn into it. This is Mechabellum's
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
- **Outcome is not computed at all.** No preview, no dummy defense, no distribution, no band, no number that
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
