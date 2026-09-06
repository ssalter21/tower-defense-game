# The Vision

**What this game is, and what it is not.** Where anything else in this repository disagrees, this is current.

It states decisions and not the arguments for them. The reasoning and every reversal are in
[the decision log](decision-log.md); the sequence is [the build order](build-order.md); what is in scope and
unsettled is [open questions](open-questions.md); every number is in
[`content/ruleset.txt`](../content/ruleset.txt) and [`content/units.txt`](../content/units.txt).

**Decided is not built.** *(designed, not built)* marks settled direction that no code implements.

---

## Bottom line

**A technically excellent tower defense, built for the pleasure of building it, whose multiplayer is real —
and every mode of it is the same machine at a different latency.**

1. **Not a commercial product.** No store page, pricing, monetisation or marketing.
2. **The planning phase is the game.** Nothing happens during a wave, so all the skill is build-and-compose.
3. **Shallow to look at, extreme to play.** Legible to a stranger in a minute; Element TD's combinatorial
   depth underneath; the attacking half as deep as the defending one.
4. **The multiplayer is real, and all of it is deferred.** Round-robin, lobby, co-op and a social layer are in
   the destination. None are built; the loop is found at zero latency first.
5. **They are one machine.** Every mode is *submit → wait → resolve → watch*. **No lockstep, no rollback, no
   tick synchronisation — only a submission barrier.**

---

## 1. The destination

**A deep personal build with genuinely rich multiplayer, for the developer and his friends.** Public release
is optional and never load-bearing.

Personal governs the reason, not the scope. The pool is open to all players, and the engineering bar goes
**up**: architecture, determinism, tooling and verification are the deliverable. Nothing here is justified by
shipping speed.

---

## 2. The loop — one machine at three latencies

Every mode is **submit → wait → resolve → watch**, repeated.

| Mode | Who fills the other seats | Barrier clears when | Latency |
|---|---|---|---|
| **Round-robin** | The shared pool — K stored defenses at your stage | Immediately | Days, or none |
| **Lobby** | The friends present | Everyone present has submitted | Minutes |
| **Co-op** | Undecided — [open questions](open-questions.md) | — | — |

**The lobby is simultaneous-turn, not real-time.** The network collects N submissions and broadcasts the
result; nothing is synchronised while a wave runs.

**K = 10 in every mode.** Each round draws a fresh field of ten stored defenses and ten waves at the same
stage, and a round's result is the average across the ten. A lobby smaller than ten is topped up from the pool
at that `(map, stage)`. **N and K are lifecycle parameters, not constants.**

**The pool needs depth at every stage, not just at the end.**

---

## 3. What a match is

**Your composed wave runs at an opponent's defense while their wave runs at yours.** Two resolutions a round,
both watched. No second currency cross-feeds them.

**A wave resolves with no input; then a build phase opens; then the next wave.** A stored ghost is legal
because it is finished, at that stage, forever.

**A run is ten waves.** A round is one build phase plus the wave after it.

**Health is a pool denominated in gold, and a leaked creep costs health equal to its cost, one for one.** Gold
cannot repair it. Damage taken in a round is the field average, not the sum. Zero health ends the run — a flag
for the harness, not a rule, so a sweep can run in no-death mode. **Runs rank by waves survived, then health
remaining; the offense never enters the placing.** A run's outcome is a **vector** — per round,
`(leak cost dealt, leak cost taken)`, plus how it terminated — never a scalar.

### One purse

**One currency, called gold. It buys the defense and the offense alike.**

- **Attacking pays back through damage, not a second wallet.** A flat base per wave plus a bonus on the leak
  cost the wave dealt, uncapped. Holding your defense well pays nothing extra.
- **A creep is bought once and attacks every round after.** The round is charged only what the purchase adds.
  Nothing is sold back and nothing is left at home.
- **The whole wave is rearranged every round.** A slot's position is its release order; a creep fills at most
  one slot.
- **Nothing is unlocked.** Every creep is sendable from wave one.
- **Timing comes from interest**, so adding nothing to the wave is investment rather than waste.
- **No money moves between players.** You are paid for what your wave dealt, never against a named opponent.

### The gates

*(designed, not built)* — **waves 3, 6 and 9 open the run up. The schedule is public; what a player does with
it is not.**

At each gate the wave gains **two slots**, every slot's **count cap rises by ten**, and the player is handed
**one capstone token** — spendable only on capstoning a tower already standing.

| Waves | Slots | Count cap | Capstones held |
|---|---|---|---|
| 1–2 | 2 | 10 | 0 |
| 3–5 | 4 | 20 | 1 |
| 6–8 | 6 | 30 | 2 |
| 9–10 | 8 | 40 | 3 |

**A gate rations capacity, never which kinds.** The token has no income, no exchange rate and one sink, so it
does not reopen the one-purse question. One hard constraint on the roster: **for every gate, its counter is
purchasable strictly before it.**

**The schedule has two layers turning over at different rates.** Its **shape** — which gate carries which
tier, and which one opens the counter — holds for a whole rotation and is what preparation is a skill about.
Its **filling** — which creeps sit on each gate's menu — is drawn per run and is where replay value comes
from. **The ghost pool does not shard for it**: ghosts draw on `(map, stage)` alone, and a ghost from this
rotation played under the same shape.

The opening pair, one token per gate, whether a token banks, and the currency's name are
[open questions](open-questions.md). **The schedule is fitted after the roster has depth worth rationing.**

### Depth is the point

**A target, not a mechanism: the build space should be combinatorial rather than a menu.** Three commitments:

- **The attacking half is as deep as the defending half.**
- **Your defense decides your offense** — a tower unlocks a skill tree for the creeps you can buy.
  [The research pushed back on this one](open-questions.md#what-the-design-research-found);
  [seam 1](build-order.md#1--the-match-format) owns the call.
- **You choose the order they come out in.** A wave is a sequence, not a bag. Order is a lever only on a
  single-file path, only where speeds differ, and only where *a count is a column, not a pile*.

Creeps get **classes and roles** — tanks, damage, support, swarm, specialists — rather than a stat ladder.

**Nothing is to be built from this section.** [Seam 1](build-order.md#1--the-match-format) chooses, from
[what the depth research found](open-questions.md#what-the-design-research-found).

### The board is a maze

**A maze that climbs, and a goal that is far less solvable.** The grid is half a block per level and nine
levels deep.

- **A map folds; it never branches.** Exactly one path in to out, and building never alters the route.
- **No pathfinder and no line of sight, permanently.**
- **Range is a signed difference**, `± 250` milli-hex per level: shooting down a whole block buys half a hex,
  shooting up one costs half a hex. Anything with a radius reads as a sphere, where height only ever costs.
- **Elevation is a coordinate the map carries, not the tower.**
- **Maps are generated and rotate** *(designed, not built)*. A map is a **seed, not an asset**; everyone in a
  cycle plays the same one; candidates are **filtered by sweeping them rather than by taste**. The first map
  is hand-authored. Cadence and how the `(map, stage)` pool survives a turnover are
  [open questions](open-questions.md).

**Until the maze lands the board is a hand-drawn 51-hex corridor one cell wide that folds and climbs two whole
blocks**, and every number priced against it is provisional by construction. It spends five of the nine levels
available to it, and every change of height on it is a single level -- half a block -- so nothing on it steps a
whole one.

### How a shot resolves

**Three attack types, three armour types, one line of arithmetic.**

```
dealt = (base + bonusVsTag) * cell / (100 + armour)   // one multiply, one divide
if (dealt < 1) dealt = 1;                             // the floor
```

The matrix is a **Latin square** — every row and column a permutation of (70, 100, 140) — so no attack type is
globally better and no armour type globally tougher. **The armour coefficient is folded to 1**: one point of
armour is one percent of base effective health. **Hard counters come from `bonusVsTag`, not the table.**
**Every damage and health number carries a ×10 scale.**

**The expression is fused, not two-step** — [ADR-0001](adr/0001-fixed-point-arithmetic.md).

**The shape survives the maze; the constants do not.**

### What a player sees before committing

**One public offering per build phase**, drawn fresh each round, identical for everyone in the match.

**Scouting differs by mode.** In the **lobby**, the opponent's defense as of the end of the previous round —
stale, never live. In the **round-robin**, no defense at all; what you buy instead is **snapshots of incoming
waves**, some free and the rest priced. One mechanic at every latency: *pay to reduce the blur on what you are
facing.*

---

## 4. What persists

**Nothing but your rating.** No unlocks, no roster development, no seasons, no account levels, no collection,
and **no live-service cadence, ever.**

The cost is paid deliberately: no dopamine drip, no gentle first hour, and every unit must be interesting from
the first run. **The onboarding ramp moves inside the run rather than disappearing.**

---

## 5. How it is balanced

**Computed. The simulation tells you.** A committed match is **2.75 ms**, so a ten-thousand-matchup sweep is
under a minute — a `simcli` mode and a CSV, worth having before the roster is large. Telemetry balancing needs
volume this will never have; hand balancing confuses *feels strong* with *is strong*.

**The harness also scores maps.** Same sweep, different axis — so **the sweep takes its map as a parameter.**

Two limits:

- **A harness measures what you tell it to.** It will never tell you a unit is boring. Play is the oracle for
  fun.
- **Depth and computed balance are the same axis pointing opposite ways.** Depth makes one unit's value depend
  on the others, which is what stops a harness pricing a unit alone. What matters is what that dependence is
  **indexed by**, because the index has a cardinality you can write down in advance.

---

## 6. What it looks like

**Stylized low-poly 3D, a free perspective orbit close enough to read one model, and no billboards, flat cards
or painted-on shadows.**

**Juicy and accessible** — every hit lands with weight, and a stranger can read the screen in a minute.
**Legibility is a veto on the depth, not only on the art.**

**The art pipeline is final-deliverable, not placeholder.** The Complete KayKit is bought, CC0, and committed
at `client/Assets/Art/Kaykit/` ([inventory](research/kaykit-collection-inventory.md)). **Skeletons are the
creeps and Adventurers are the towers.** **Recolouring is one small PNG** — the models are UV-mapped onto flat
palette textures, so a faction variant costs one texture and one material and zero geometry.

**The effort goes into lighting, VFX and camera; faction recolours; and UI and information design** — not
custom character geometry. Showing two battles, an economy and a build menu at once is the hardest visual
problem here, and it is UI work. **Art is not a risk item.**

---

## 7. What runs it

*(not built)* — **a real server, self-run**, holding accounts, the shared ghost pool, submissions, standings and replays.
Determinism means the server re-runs any claimed result, so **anti-cheat falls out for free**. A ghost record
is hundreds of bytes.

**This is the only permanent obligation in the plan.** Everything else can be put down and picked up.

---

## 8. Out of scope

They return only if the destination is redrawn.

- **Monetisation, pricing, store presence, wishlists, marketing, launch windows, demo cadence.**
- **Progression *between* runs** — unlocks, collections, account levels, seasons, battle passes. **In-run
  progression is not ruled out and never was.**
- **Realtime netcode** — lockstep, rollback, tick synchronisation, prediction.
- **Discovery, curation and browsing surfaces.**
- **Custom character geometry as the default.**
- **Moderation and community management at scale.**

---

## 9. The planning phase is the game

**Nothing happens during a wave**, so **every axis of skill is collected in one phase** where the comparison
games spread it across two. The build phase is the entire game and is budgeted like one.

**Nothing is forecast.**

- **Mechanism is free, total and always on**: range overlays, costs, interest, the offering, and the
  expression evaluated live for any attacker against any target. **"Damage preview" means one shot, not one
  round.**
- **Outcome is not computed at all.** No preview, no dummy defense, no distribution, no rate. **The offense
  gets nothing** — the defense is engineering, the offense is judgement.
- **The retrospective is where the simulator lives**: free, unlimited and exact against the real field.
  Re-running a finished match with one purchase changed is 2.75 ms. Reward **both the best achieved and the
  average**; place players against a **histogram, not a leaderboard**; choose the highlights rather than
  recording them.

Two constraints:

- **The price is charged on data, never on compute.** Anything the client holds the data for, a third-party
  calculator computes for free. What the server withholds is **the pool**.
- **Roughness comes from unknown inputs, never fuzzed arithmetic.** A paid predictor sells **information**,
  never precision.

**A simulation that answers everything deletes the game.** The antidote is structural: your wave is measured
against many defenses, so the honest answer is a distribution rather than a number. **Uncertainty comes from
the breadth of the field, not from hidden state or dice.**
