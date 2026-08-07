# The Vision

**The standing document** · 3 August 2026

> **What this game is, what it is not, and the order it gets built in.**
>
> Parts I to V were written to answer whether this game was worth making and what to make it with. They were
> right about the machinery and wrong about the audience. This document fixes the destination, and where it
> disagrees with any of the five, this document is current.
>
> It is deliberately larger than one effort. [§8](#8-the-build-order) sequences it, and each seam named there
> is the subject of its own wayfinder map.

---

## Bottom line

### A technically excellent tower defense, built for the pleasure of building it, whose multiplayer is real — and every mode of it is the same machine at a different latency.

Five claims carry the whole document.

**It is not a commercial product.** No store page, no pricing, no wishlists, no monetisation, no marketing.
The reward is the build. That deletes more scope than any other decision here, and it is what makes the rest
affordable.

**The planning phase is the game, and the simulation is a design material.** This is the identity, added
6 August 2026, and it re-reads everything below it. The player's whole exercise of skill is the build and
compose phase — nothing happens during a wave, by construction — so that phase must be as dense as two phases
are in any comparison game. What makes that affordable is the thing this project already built: a
deterministic integer simulation that resolves a match in **2.75 ms**. Anything the player would want to know
before committing can be *computed*, and anything worth telling them afterwards can be computed too. §12 is
the section this claim owns.

**Shallow to look at, extreme to play.** Anyone should be able to pick it up and read what is happening on
screen inside a minute. Underneath that, the build depth is meant to be enormous — Element TD's
element-combination lineage rather than a list of towers — and the attacking half is meant to be as deep as the
defending one. Juice and legibility on the
surface; combinatorics underneath. §3 is the design claim, §6 is the presentation claim, and neither is
allowed to be traded for the other.

**The multiplayer is not a garnish.** Async round-robin against a shared pool of every player, a live lobby of
friends, co-operative play, and a social layer of replays and named rivalries. All four, and none of them
deferred to a "step 7".

**They are one machine.** Every mode is *submit → wait → resolve → watch*. The round-robin is that loop with a
latency of days and strangers in the seats; the lobby is the same loop with a latency of minutes and your
friends in them. There is no lockstep, no rollback, no tick synchronisation anywhere in this plan — only a
submission barrier. That single realisation is the largest cost saving in the document, and §2 is about why it
holds.

---

## 1. The destination

**A deep personal build with genuinely rich multiplayer for the developer and his friends.**

The point is the building — a deterministic, verifiable, well-architected game that keeps getting deeper. Public
release is optional and never load-bearing. But "personal" governs the *reason*, not the *scope*: the
multiplayer has to be real, the pool is open to all players, and the standard is a game people would actually
want to play, not a technical exercise with a multiplayer sticker on it.

Two things follow immediately, and they pull in opposite directions:

- **Commercial pressure is gone.** No revenue means no monetisation design, no store presence, no launch
  window, no demo cadence, no support obligation. Part I's entire viability argument becomes background
  reading rather than a constraint.
- **The engineering bar goes *up*, not down.** Nothing here is justified by shipping speed. Architecture,
  determinism, tooling and verification are the deliverable, which is why the walking skeleton has seventy
  tests and a twelve-row sit-down and why the balance seam is a harness rather than a spreadsheet.

### Why async survives, for a different reason

Part II justified asynchronous ghosts by the **population ceiling** — synchronous PvP tower defense tops out
around 830 concurrent players, so the queue kills the game. At friend scale that argument evaporates. You can
always get four people into a lobby.

**Async survives because the problem was never finding people. It is that you are never free at the same
time.** Schedule mismatch, not population. That is a narrower justification than the one on file and a stronger
one, because it does not depend on the game ever being popular. It is also true on day one, with three
players, forever.

---

## 2. The loop — one machine at three latencies

The unifying claim, stated precisely so the rest of the document can lean on it.

```
        ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────┐
        │  SUBMIT  │ ──► │   WAIT   │ ──► │ RESOLVE  │ ──► │  WATCH   │ ──┐
        │ build +  │     │ for the  │     │ determin-│     │ both     │   │
        │ compose  │     │ barrier  │     │ istic sim│     │ boards   │   │
        └──────────┘     └──────────┘     └──────────┘     └──────────┘   │
             ▲                                                            │
             └────────────────────────────────────────────────────────────┘
```

| Mode | Who fills the other seats | Barrier clears when | Latency |
|---|---|---|---|
| **Round-robin** | The shared pool — a **field of ten** stored defenses every wave, drawn at your stage | Immediately; the pool is already there | Days, or none |
| **Lobby** | The friends in the lobby | Everyone present has submitted | Minutes |
| **Co-op** | *Not yet specified* — see [§10](#10-not-yet-specified) | — | — |

**The lobby is simultaneous-turn, not real-time.** Everyone builds, everyone submits, the game waits on the
last submission, then all of it resolves and everyone watches. Nobody is acting during a wave — that was
settled in §3 — so there is nothing to synchronise while a wave runs. The network's whole job is to collect N
submissions and broadcast the result.

> **This is what preserves Part III's "no realtime networking".**
>
> Part III ruled out realtime networking on the assumption that live PvP was out of scope. It is not out of
> scope — but it does not need realtime networking either, because a build phase with a submission barrier is a
> *turn*. The deterministic sim that already exists resolves it, and the record format that already exists
> transmits it. Live head-to-head costs a barrier and a lobby, not a netcode layer.

**Every round draws a fresh field of ten, matched at the same stage** — *changed 7 August 2026, see
[§9](#and-what-was-reversed-on-7-august-2026).* Round four meets ten people's round-four defenses, and ten
people's round-four waves. This dissolves the hardest problem in the async model: a stored defense never has to
evolve mid-match, because you only ever meet it at one moment of its life. It is the Super Auto Pets structure.

**The field is also the variance control** Part II §3 argued round-robin exists for, and it does that job far
harder than the earlier one-opponent-per-wave draw did: a round's result is the *average* across ten, so a
single bad matchup is a rounding error rather than a dent. The number is **K = 10, fixed, in every mode** — a
lobby smaller than ten is topped up from the pool at that `(map, stage)`. Decided in
[#74](https://github.com/ssalter21/tower-defense-game/issues/74).

The cost of averaging is that a round tends toward the field mean, which risks rounds feeling alike. The
antidote on file — deliberately undecided — is a **gamble**: opt out of the average and face a single opponent
drawn from the distribution, with best-of-ten as its natural payoff. It cannot be tuned before a real field
exists at step 6.

Compute is not a constraint here. At the 2.75 ms a match `BudgetTests` measures, ten rounds against ten
opponents on two sides is about half a second for an entire run.

The cost is that **the pool needs depth at every stage, not just at the end**, which sharpens Part II's
cold-start problem rather than removing it. Hand-authored defenses at each stage are the answer, and they cost
nothing architecturally: a hand-built defense and a stored one are the same object.

> ⚠️ **And [map rotation](#the-map-rotates-and-it-is-generated) multiplies that cost by the rotation rate.**
> A pool is now indexed by `(map, stage)` rather than by stage alone, so every turnover empties it. The
> cold-start problem stops being something the project has *once* and becomes something it has *every cycle*.
> This is the strongest argument in the document for a slow rotation, and it is why the cadence is an open
> question rather than a preference.

---

## 3. What a match is

### Both boards, at once

Every wave, **your composed wave runs at an opponent's defense while their wave runs at yours.** Two
resolutions per round, and you watch both. This is Legion TD 2's structure.

It is chosen for one reason above the others: it is the complete answer to Part II §3's *"defense feels
meaningless"* hazard — the failure Supercell hit hard enough in Clash of Clans that they rebuilt a whole mode
around it. Both halves are live every round, in front of you, with nothing needing to be cross-fed to make them
matter. **Part II's recommended fix — two currencies, each earned by doing the other thing — is therefore not
needed, and is explicitly not adopted.**

The cost is real and lands on [the interface seam](#8-the-build-order): the player is reading two boards at once, and
that is the hardest unsolved presentation problem in this design.

> ⚠️ **And it got harder on 7 August 2026.** A round now resolves against
> [a field of ten](#a-run-is-ten-waves-and-health-is-money--decided-7-august-2026), so the problem is no longer
> reading two boards at once — it is reading *twenty*. **What the player actually watches is now the biggest
> open thing in [seam 7](#7--the-interface)**, and nothing in this document answers it.

### Build phases between waves, and nothing during one

The classic tower defense rhythm. A wave resolves with no input; then a build phase opens; then the next wave.

This is what makes the whole thing a *game* rather than an auto-battler, and it is compatible with stored
ghosts only because of the per-wave draw above. A defense in the pool is a snapshot of one stage. It never has
to play itself, never has to replay a build order chosen against a different wave, and never has to follow a
policy you authored. **It is finished, at that stage, forever.**

### A run is ten waves, and health is money — *decided 7 August 2026*

**Ten waves. A round is one build phase plus the wave that follows it, so ten waves is ten rounds.** The
length is fixed and public, and it is the number this document was already leaning on without deciding it.

The reason for short over Element TD's fifty is not pacing but mercy: **a player who has fallen behind the
curve should not be stuck in a long game.** Get them out and into the next run, where the thing they just
learned is worth something. Ten also keeps 10% interest compounding to about 2.6x across a run, which is why
the purse below needs no interest cap.

> ⚠️ **The cap may be lifted later, so run length is a parameter and not a constant** — and **removing it
> forces an interest cap**, because uncapped 10% a wave is bounded today only by the run being ten waves long.

**Health is a pool denominated in sauce. A leaked creep costs health equal to its cost, one for one.** A
10-cost creep does 10 damage.

That single sentence is doing most of the work in this section, because it makes health and money the same
unit:

- **It gives conceding a wave a literal exchange rate.** Under one purse, underbuilding your defense to fund
  your offense already *is* spending health. There is no sell-health button and none is needed — the shared
  wallet is the dial, which is the same argument [#72](https://github.com/ssalter21/tower-defense-game/issues/72)
  landed on for timing.
- **Damage taken in a round is the field average, not the sum.** Summing ten opponents' leaks would kill
  everybody immediately; averaging is the entire point of a field of ten.
- **The pool is about three waves' worth of average creep value** — deep enough that one deliberate concession
  is affordable and a second is a real decision, shallow enough that a losing player is out around wave six or
  seven. A single number, and a step 4 sweep target rather than an argument.
- **Sauce cannot repair health.** Permanent depletion is what makes the pool a clock, and the clock is the
  thing that ejects a struggling player. A repair rate would sell them a way to stay in a run they are losing.

**Death is in. Zero health ends the run there.** The wall sits at the bottom of a graded pool rather than in
place of one — Legion TD 2's *"King HP is a resource, to a certain extent."* For the step 4 harness, death is a
**flag** rather than a rule, so a sweep can run in no-death mode and always yield ten rounds of data.

**Runs are ranked by waves survived first, then health remaining.** The graded pool therefore does double duty
— the resource during the run and the placing at the end of it — so no third number has to be invented.

**The offense never enters the placing. It pays in sauce and nothing else.**

> ⚠️ **This is in tension with [*"the attacking half is as deep as the defending half"*](#depth-is-the-point)
> below.** Secondary in *scoring* is not the same as shallow in *decision* — the wave slots, the unlock gate
> and the percentile bands all still make composing a wave a real choice — but an attacking half that can never
> win you a placing is a weaker claim than that section makes. **Seam 1 owns the reconciliation.**

**A run's outcome is a vector, not a score:** per round, the pair `(leak cost dealt, leak cost taken)`, plus how
the run terminated. Health remaining, waves survived and any score are folds over it. Recording a scalar
instead would mean re-simulating to recover the two distributions the percentile bands are computed from. A
round's **offense** score is the average of the ten rather than the best of them — the same rule as the damage,
taken symmetrically.

The simulation already produces the raw material. `sim/MatchResult.cs` returns `Leaked / Total / FinalTick` and
says so in its own summary: *"Carries no win or loss verdict."* Health is a layer over a number that exists.

Decided in [#74](https://github.com/ssalter21/tower-defense-game/issues/74), which holds the detail.

### One purse — *restored 6 August 2026*

**One currency, called sauce. Every coin of income lands as sauce, and sauce buys the defense and the offense
alike.**

This reverses the two purses adopted earlier the same day, and restores the single purse — but **not the
argument the single purse used to rest on.** It stands now for a reason it did not have that morning, and the
distinction is the whole of this section.

**Why two purses were adopted.** [The sending research](research/attack-composition-and-sending.md) found the
genre's deep send systems are two-currency, and that under one purse a coin spent attacking is simply gone —
which makes attacking a pure tempo loss and, at equilibrium, **dominated**.
[The skill note](research/fun-and-skill-expression.html) restated it as something worse than a balance problem:
**a purchase which only ever subtracts has no timing question attached to it, and timing is what players
practise.**

**Why one purse survives anyway.** Read closely, neither objection is to the single wallet. Both are to a
*missing payback*. A second currency was one way to supply it. It is not the only way, and it is not the
cheapest:

- **Attacking pays back through performance, not through a second wallet.** Income is a flat base per wave,
  **plus a bonus on top of that base** paid in non-linear percentile bands, measured over two distributions:
  how your creep wave performed, and how your defense performed, each against the field. A wave that does well
  pays you for having sent it. The payback the research demanded is present; the currency it was going to
  arrive in is not.
- **The offense is separated by a gate, not by a budget.** Each round offers a selection of choices, and taking
  one **unlocks a creep permanently for the run** — free to unlock, paid to buy. What you may field is bounded
  by what you have unlocked, not by which wallet you remembered to save into.
- **Scarcity comes from wave slots.** A wave has a limited number of slots, and that number grows each round. A
  slot is one creep type plus a count, and slots may be left empty. **A slot spent on a cheap column is a slot
  not spent on a heavy unit** — which is precisely the opportunity cost the second wallet was there to
  manufacture.
- **Timing comes from interest.** Unspent sauce banks and earns **10% a wave, rounded up**, uncapped for now.
  Every purchase is therefore measured against compounding, and leaving a slot empty is not waste but
  investment. That is the dial the skill note said one purse had removed, and it is now attached to every
  purchase in the game rather than to one of two wallets.

**And it buys back what two purses cost.** Each build phase is one decision over one wallet again, rather than
two small independent ones — the loss named on the morning the two purses were adopted.

**No money moves between players.** Denial — Legion TD 2's rule that leaking pays the attacker — is
deliberately rejected. The coupling between players is **statistical**: you are paid against the field's
distribution, never against a named opponent, and that reads the same whether the field is one lobby or a
global population. The bands are **progressive and never negative**: performing below average earns a smaller
bonus, never a penalty.

Decided in [#72](https://github.com/ssalter21/tower-defense-game/issues/72), which holds the detail. The
numbers — base, band thresholds, slot count and growth, creep costs — are step 4 sweeps rather than arguments.
One consequence worth carrying: **the bonus is not computable until step 4**, because a distribution needs a
field, so steps 1 to 3 pay the base alone and should not read a zero bonus as a fault.

### Depth is the point

The ambition, stated as a target rather than a design: **the build space should be combinatorial, not a menu.**

The named reference is **Element TD**, the Warcraft 3 mod, where picking elements unlocked dual-element tower
combinations and the interesting play lived in the synergies between them rather than in any single tower. That
is the class of depth wanted here — a space you can still be discovering after fifty runs, out of a roster
small enough for one person to build and for a harness to sweep.

Three commitments follow, all of them direction rather than mechanism:

- **The attacking half is as deep as the defending half.** This is the corollary of both boards being live
  every round. A game where you build a rich defense and then pick creeps off a flat list is only half
  designed.
- **Your defense decides your offense.** The stated idea: a tower of a given type unlocks a skill tree for the
  creeps you can buy, so the pool you send from is a consequence of what you built. One coherent identity per
  run rather than two unrelated shopping trips.

  > ⚠️ **This is the one piece of direction the research pushed back on.** It found **exactly one shipped
  > precedent** — a single tower upgrade in Bloons TD Battles 1 that let you send a tier *earlier*, gating the
  > schedule rather than the pool; it shipped with a carve-out, and the sequel removed it. **Every other game
  > surveyed gates the opposite way round**: constrain the defense, keep the attacking vocabulary universal —
  > and deliberately, because *a send is only a read if both players already know the whole menu.*
  >
  > That is not a verdict of "known bad" — it is closer to unexplored, and the note offers three graded
  > versions that keep the upside. But it names four specific failure modes, of which two bite hardest here:
  > **double-dominance under one purse** (one build wins both halves), and **counter-picking collapsing to a
  > lookup**, because the stored ghost already shows you the defense before you compose. Seam 1 owns the call.

- **You choose the order they come out in.** A wave is a sequence, not a bag.

  > ✅ **The research came back positive on this one, and it is already half-built** — but its justification
  > has just been weakened and this needs to be said plainly. Ordering was rescued by the one-hex corridor: a
  > corridor exactly one hex wide that never branches **is** a single-file column, with no route decision left
  > to dilute it. [The maze reversal below](#the-board-is-a-maze-again--reversed-6-august-2026) takes that
  > corridor away. **Wherever the path branches, the order you sent in is no longer the order that arrives**,
  > and ordering degrades toward the trap the research warned it usually is.
  >
  > This does not kill it. It converts ordering from a free consequence of the geometry into a thing the
  > geometry has to be *designed to preserve* — one entrance, convergent rather than divergent branching, or
  > sections of guaranteed single file. Seam 1 and the map design now share this constraint.
  >
  > [`content/wave.txt`](../content/wave.txt) is already an ordered list of `(tick, type, count)`, and the
  > skeleton already found both preconditions the hard way and wrote them down: ordering is unobservable when
  > units share a speed, and unobservable again when a count spawns as one pile — *"a count is a column, not a
  > pile."* A branching map is now a **third** precondition on the same list.

And the creeps themselves get **a roster with classes and roles** — tanks, damage, support, swarm, specialists
— rather than a stat ladder. Part V's *one unit schema, two roles* is the structure this fills, and whether it
survives contact with a role-based roster is a question the roster seam inherits.

> **None of this is a mechanism yet, and it is not to be built from this section.** It is a direction, and
> [the three research notes](#10-not-yet-specified) commissioned against it have now landed. Seam 1 chooses
> from what they found; this section only says what the game is reaching for.

Two findings from [the depth research](research/build-depth-in-tower-defense.md) belong here, because they
change what "combinatorial" can mean rather than merely how to build it:

- **There are two structurally different ways to manufacture it, and only one is also an accessibility
  mechanism.** Either a **generative rule** mints a large roster from a small vocabulary — Element TD's six
  element names predicting fifty-six towers — or a **large authored pool** is metered out by a random offering,
  as in YouTD, Mazebert and Legion TD 2. Only the generative route reduces what has to be *learned*, and only
  it leaves a balance surface the harness can enumerate rather than sample.
- **Element TD's depth is not the combinations. It is the picks.** The combination table is what makes the
  towers memorable; the eleven metered picks across fifty waves are what make a run a decision — and every pick
  after the first **summons a boss you must kill before the element unlocks**. The tech choice is paid for
  inside the simulation rather than chosen in a menu. The note calls this the most transferable mechanism in
  the whole survey, and the thing nobody cites about the game.

### The board is a maze again — *reversed 6 August 2026*

**The geometry changes dramatically, and the goal is a maze that is far less solvable.** The one-hex corridor
that never branches is withdrawn. Mazing and pathfinding leave [§11](#11-out-of-scope) and come back into the
design.

**Placements sit at several elevation levels, and elevation grants range.** Height is the reason a range
upgrade pays off in a later round instead of being a flat stat: a tower that is merely accurate on the floor
becomes a board-controller on the top tier. This is a shipped pattern rather than an invention — the common
form is *+1 range per elevation level* — and it does something the flat map could not, which is to make
**where** and **what** into one decision instead of two.

This is the single largest reversal in the document, and four things follow from it that are not optional:

- **It restores the genre's strongest skill axis.** [The skill note](research/fun-and-skill-expression.html)
  found geometry to be the axis tower defense was popularised on, and the deletion of it to be the largest in
  the design. The measured prize is not subtle: a creep crossing a board in a straight line spends about two
  seconds under fire, and the same board folded into a switchback puts it under fire for twelve — a sixfold
  damage multiplier bought with no gold at all.
- **Pathfinding enters the simulation, and that is a determinism obligation.** "No unit ever chooses a path"
  is currently load-bearing. A deterministic maze needs an integer pathfinder with a *fixed, asserted*
  tie-break, and it needs to be as hard-nosed as everything else in `sim/` — same one-RNG-stream rule, same
  canonical-order assertions, same IL scan. It also lands on the hottest loop.
- **Elevation is a third coordinate, and coordinates are in the record.** `TowerLayout` and the hex map gain a
  level. That is a format version, a hash-layout bump and a retired ghost pool — which is exactly why it is
  cheap now and expensive later.
- **"Far less solvable" is a measurable target, not a feeling, and this project can measure it.** See
  [§12](#12-the-planning-phase-is-the-game).

> ⚠️ **The bill this reversal presents is ordering.** The corridor was what made send order a real lever; a
> branching map dilutes it. See [the ordering note above](#depth-is-the-point). Six of Part V's variance
> levers died to the corridor and now return, which is a gain — but the map has to be designed to keep the
> column, not merely to be interesting.

### The map rotates, and it is generated

**The round-robin runs on one map at a time, and that map turns over on a schedule — daily or weekly.** Maps
are generated rather than authored, and a map is identified by a seed rather than stored as a file.

This is the second half of the answer to solvability, and it is the half that keeps working. A hard map buys
time; **a map nobody has seen before buys it permanently.** The [maze reversal](#the-board-is-a-maze-again--reversed-6-august-2026)
makes each map deep; rotation makes the depth renewable, which matters more here than in most games because
[§4](#4-what-persists) has no unlock ramp to spread learning across weeks.

Three properties come with it and each is load-bearing:

- **Everyone in a cycle plays the same map.** This is what makes results comparable at all — the shared-seed
  logic Slay the Spire's daily runs on, where an identical seed for every player worldwide is precisely what
  makes the scoreboard mean something. It is also required by
  [§3's both-boards structure](#both-boards-at-once): two resolutions on two different maps are not a match.
- **A map is a seed, not an asset.** `HexMap.FromCells` already builds a grid without a filesystem, `Match`
  already takes a `ulong seed`, and `HexMap.MapHash` already hashes the parsed grid. So a generated map is a
  handful of bytes in a record and a hash the server can check — which means **rotation costs the ghost format
  nothing and anti-cheat still falls out for free.**
- **Generation is filtered by simulation, not by taste.** This is where the identity in
  [§12](#12-the-planning-phase-is-the-game) pays for itself, and it gets its own paragraph below.

> **The generator is the harness pointed backwards.** The standard method for this is *search-based procedural
> content generation*: generate a large volume of candidates, score each with a fitness function, and let the
> score steer the next generation. The literature's standing complaint about it is that a simulation-based
> fitness function is too slow to run at scale — **which is the one problem this project does not have.** At
> 2.75 ms a match, a candidate map can be swept by the same harness that prices units, and scored on the thing
> that actually matters: how widely outcomes spread across good plans. A map where every competent plan scores
> the same is solved; a map where they diverge has decisions in it. **Maps are therefore selected against a
> measurement of their own solvability**, which no tower defense has been able to afford.

⚠️ **Rotation partitions the ghost pool, and this is a real cost that needs an answer.**
[§2](#2-the-loop--one-machine-at-three-latencies) already says the pool needs depth *at every stage*.
`GhostRecord` already carries both a `MapHash` and a `MapHandle` — the format anticipated this — so a pool
index keyed on `(map, stage)` needs no format change. What it needs is **population**, and a daily map means a
cold pool at every stage every single day. Three ways out, none chosen here: rotate slowly enough that the pool
fills, generate the hand-authored floor for each map with the map, or let the pool carry across maps and accept
that a stored defense meets waves on geometry it was not built for. This is now an open question in
[§10](#the-open-questions).

⚠️ **Rotation must be generated, never curated.** [§7](#7-what-runs-it) permits exactly one permanent
obligation and [§4](#4-what-persists) rules out live-service cadence in the strongest terms in the document. A
map-of-the-week that a person authors is that cadence wearing a different hat. A scheduler drawing from a
pre-generated archive is not: the archive is built once, offline, by the harness, and the schedule is
arithmetic on a date. **The distinction is not pedantic — it is the difference between a rotation this project
can keep and one it cannot.**

### Wave variance is anchored, not emergent

**At fixed, known waves, the run injects a major variance event** — a choice, an upgrade, or a class of unit
that was not previously available to anyone. The schedule is public. What each player does with it is not.

This is the answer to the hole [the skill note](research/fun-and-skill-expression.html) found: with the player
composing the whole wave there was no public constant to prepare against, so *preparation* — the axis Bloons
TD 6's hardest mode is almost entirely made of — had nothing to be a skill about. An anchor schedule supplies
the constant without supplying the content. **Everyone knows the flying units unlock at wave 9. Nobody knows
who took them.**

It also does the pacing job [§6](#6-what-it-looks-like) needs. Element TD meters eleven element picks across
fifty waves and gates each behind a boss; Super Auto Pets unlocks shop tier *X* on turn 2*X*−1. Both are
progressive disclosure that resets every run, which is the only shape [§4](#4-what-persists) permits.

### The options are the same for everyone — *the Mechabellum move*

**Each build phase offers the same small set of choices to every player in the match.** Not a private random
draw; one public offering that everybody sees.

This is Mechabellum's reinforcement system, and its own players describe the consequence exactly right: the
opponent sees the same choices, *so it becomes a mind game*. It buys three things at once, which is why it
earns a place in the vision rather than in a seam:

- **A shared vocabulary.** A send is only a *read* if both players already know the menu — the finding the
  sending research kept arriving at. A public offering makes the menu public by construction.
- **Scarcity without a private lottery.** The metered-offering depth mechanism, minus the variance that made
  the research rank it third.
- **A second-order decision.** Taking the thing you need is one decision. Taking the thing you need *because
  your opponent also needs it* is a better one.

### What you see of your opponent depends on the mode, and that is deliberate

The two multiplayer modes now differ on information, and the difference is the point rather than an accident
of implementation.

| | **Round-robin (async)** | **Lobby (live)** |
|---|---|---|
| Opponent's defense | Not shown as a board to compose against | **Shown** — live, or as of the previous round |
| What you are optimising for | Performance across *many* defenses | Performance against *one known* defense |
| The skill | Robustness, and reading the shared offering | Hedging and counter-picking, TFT-style |
| Feedback | Mean and spread over the field (see [§12](#12-the-planning-phase-is-the-game)) | The board in front of you |

**In the lobby you can scout.** Seeing the opponent's towers — live or one round stale — makes composing a
wave a counter-pick, and the stale variant is the more interesting one because it prices *change*: what they
had is evidence, not truth. This is Teamfight Tactics' loop, where scouting between rounds decides
itemisation, positioning and whether to commit to a composition at all.

**In the round-robin you cannot**, and this fixes a real defect rather than merely accepting a limitation.
[The skill note](research/fun-and-skill-expression.html) found that a stored ghost inverts *reading the
opponent* into a lookup: a frozen defense is fully inspectable and cannot react or lie, so perfect information
about it produces optimisation, not inference, and degrades to a table. Withholding the board and paying the
player in **statistics over the field instead** is the fix, and it also turns the async mode into a genuinely
different game from the lobby rather than a slower copy of it.

> **You do not know exactly what your wave will do — and the reason matters.** Not because the simulation is
> hidden or random; it is neither. Because in the round-robin your wave is measured against *many* defenses,
> so the honest answer to "what does this do?" is a distribution rather than a number. That is a real
> uncertainty a player can reason about, and it is the one thing standing between a deterministic game and a
> solved one. [§12](#12-the-planning-phase-is-the-game) is where it is spent.

---

## 4. What persists

**Nothing but your rating.**

Every run starts from the same position with the same options. No unlocks, no roster to develop, no seasons, no
account levels, no collection.

| What this buys | What it costs |
|---|---|
| A friend who joins in month six plays the same game you do | No dopamine drip — the play has to be good enough on its own |
| Nobody can out-grind anybody; the ladder measures skill and nothing else | No gentle first hour bought by a drip-fed option space |
| Matchmaking is a skill problem, never a power-level problem | Every unit must be interesting from the first run, since none are held back |
| The smallest content surface of any option — nothing exists to be unlocked | |
| No live-service cadence, ever — the one obligation a personal build must never take on | |

This is Slay the Spire's daily and Backpack Battles' model, and it is the single most scope-protective decision
in this document after "not commercial".

---

## 5. How it is balanced

**Computed. The simulation tells you.**

The deterministic integer sim and headless CLI already exist (`sim/`, `simcli/`,
`tools/run-headless-match.ps1`). The balance harness is built on top of them: sweep every unit against every
defense across thousands of matches, produce win-rate and cost-efficiency matrices, and let a red cell name
what is mispriced before a human notices.

This is the payoff Part III promised when it claimed a deterministic sim turns balance into a computation. It
was a claim on credit until now; this document is where it gets spent.

> **It is not an overnight job, and that changes when it gets built.** This section used to say "across
> thousands of matches overnight". The skeleton has since measured the thing that sentence was guessing at:
> `BudgetTests` times the committed match at **2.75 ms** on the development laptop, which is roughly **360
> matches per second on one core**, so a ten-thousand-matchup sweep is under a minute rather than a night.
>
> A harness that costs a minute is a **`simcli` mode and a CSV**, not a project — and a tool that cheap is
> worth building *before* the roster is large, not after. A red cell naming a mispriced unit while there are
> only eight units makes every subsequent unit cheaper to author, which is why the build order in
> [§8](#8-the-build-order) puts the harness fourth instead of leaving it downstream of a finished roster.

> **The harness has a second job now, and it was not foreseen when this section was written.** Pointed at
> units it prices them; pointed at **maps** it scores them, which is what makes
> [generated rotation](#the-map-rotates-and-it-is-generated) possible. Same sweep, same CSV, different axis —
> and it is the component that turns "a maze that is far less solvable" from a wish into a filter. It should
> be built with that second use in mind at step 4 rather than retrofitted, because the difference is whether
> the sweep takes its map as a fixed input or as a parameter.

**It is not a luxury at this scale — it is the only option that works.** Telemetry balancing needs player
volume that a personal build will never have. Hand balancing finds only the loudest problems and reliably
confuses "feels strong" with "is strong". A harness is the only method whose accuracy does not depend on an
audience.

The known limit, stated plainly: **a harness measures what you tell it to measure.** It will find a mispriced
tower and it will never tell you a unit is boring. Play remains the oracle for whether something is *fun* to
lose to.

> ⚠️ **And a second limit the research found, which is sharper: computed balance is a budget, not a licence.**
> Every mechanism that manufactures depth does it by making one unit's value depend on the other units you
> chose — and that dependence is precisely what stops a harness pricing a unit in isolation. So "balance is
> computed" does not buy unlimited interaction. **Depth and computed balance are the same axis pointing
> opposite ways**, and what matters is not *whether* a mechanism creates dependence but what the dependence is
> **indexed by**, because that index has a cardinality you can write down in advance. Seam 1 is spending a
> budget here whether or not it knows it.

---

## 6. What it looks like

Part IV's art direction stands **except for the board itself** — stylized low-poly 3D, fixed isometric
orthographic orbit with 60° yaw snapping, and **no billboards, no flat cards, no painted-on shadows**.

### Juicy, and readable by a stranger

The stated goal is that it should be **juicy and accessible — anyone could pick it up.** Every hit lands with
weight, every purchase feels good, and a person who has never seen it can tell what is happening within a
minute of looking at it.

This sits directly on top of [§3's](#3-what-a-match-is) extreme depth, and the pair is not a contradiction —
Bloons TD 6 is the standing proof that a game can be legible to a child and still have a competitive meta. But
it is a **tension that has to be actively managed**, and one settled decision makes it harder than it is for
anybody else:

> **The usual accessibility ramp is unavailable — but the ramp is not gone, it has moved.** Almost every deep
> game onboards by *withholding*: you start with four options and the space widens over weeks.
> [§4](#4-what-persists) rules that out, so the full space is present on run one.
>
> ✅ **[The research](research/build-depth-in-tower-defense.md) found the replacement already shipped in three
> games: put the disclosure ramp *inside the run*.** Element TD 2 meters eleven element picks across fifty
> waves — each one gated behind an elemental boss you must kill first. Super Auto Pets unlocks shop tier *X* on
> turn 2*X*−1. Bloons TD 6's hero starts at level 1 **every game** and climbs to 20 during it. Each is
> progressive disclosure that **resets at the start of every run**, which is exactly the shape §4 permits — and
> a strictly better place for it here, because it never advantages the player who has been at it longest.
>
> *This corrects an earlier claim in this section that accessibility had to be bought "entirely with legibility
> and never with progression". Legibility still carries most of the load, but in-run pacing is not progression
> in §4's sense and was never ruled out.*
>
> The note's sharper finding: of eight accessibility mechanisms in shipped games, four need persistence and are
> dead here — but the strongest survivor is **a generative, compressible roster**, because it is the only one
> that reduces what a player must *remember* rather than what they must *see*. Six element names predicting
> fifty-six towers is a tutorial and a content strategy in the same lever.

Two consequences for what gets built:

- **Juice is a feature with a budget, not a polish pass.** Hit reactions, death weight, muzzle flashes, impact
  effects, number popups, screen shake, easing on every UI transition. It is the majority of what "feels good"
  is made of, and none of it requires an artist.
- **Legibility is a design constraint on the depth, not just on the art.** If a mechanism cannot be read off
  the screen, it fails the accessibility pillar however deep it is. That is a real veto and it should be used
  as one.

### The pipeline

**Buy [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — $150 — and supplement only with free
CC0.** This reactivates Part IV's original day-one recommendation, which was never overturned; it was paused
because the walking-skeleton effort was scoped free-tier-only at $0.

Two facts make this a *final-deliverable* pipeline rather than a placeholder one:

- **Recolouring is editing one small PNG.** KayKit models are UV-mapped onto flat palette textures, not painted
  detail maps. The build already imports exactly that — `client/Assets/Art/Characters/ranger_texture.png`,
  `skeleton_texture.png`, `Buildings/hexagons_medieval.png`. A faction variant costs one texture and one
  material, and zero geometry.
- **The $150 buys `.blend` sources, not permission.** KayKit's free tiers carry the same CC0 as the paid ones,
  and CC0 already grants modification, commercially, with no attribution and no royalty. What the money buys is
  bundling, the original geometry and rig, and every future pack.

> ⚠️ **Confirm before paying.** Every licence and price claim in Part IV was read as extracted text rather than
> in a browser. [#56](https://github.com/ssalter21/tower-defense-game/issues/56) is open for exactly this
> check. It is five minutes, and it is the last thing between this plan and money moving.

### Where the effort goes

Not into custom character geometry. Stock models, and everything else into presentation:

1. **Lighting, VFX and camera.** URP lighting, colour grading, hit and death effects, projectile trails, camera
   framing. This is what separates a build that looks amateur from one that looks composed, and none of it
   needs an artist. Kenney's Particle Pack is the free source already identified.
2. **Recolouring into factions.** Your units one palette, your opponent's another, tower tiers reading by
   colour. Cheap, and it does double duty as the readability fix for watching two boards at once — a lobby
   should feel like *your* colour against *theirs*.
3. **UI and information design.** The screen must show two simultaneous battles, an economy, a build menu, and
   what your opponent just did. This is the hardest visual problem in the design and it is UI work rather than
   art work. Getting it wrong makes a well-lit game unplayable.

**Art is no longer a risk item.** It is a workstream with a known pipeline, a known licence and a known cost.

---

## 7. What runs it

**A real server, self-run.** It holds accounts, the shared ghost pool, submissions, standings and replays.

Because the simulation is deterministic, the server can re-run any claimed result and compare — **anti-cheat
falls out for free**, exactly as Part II §4 predicted. A client's reported outcome is a claim, not a fact.

The storage story stays trivial: a ghost record is hundreds of bytes, so a hundred thousand stored defenses is
under a hundred megabytes.

> **This is the only permanent obligation in the entire plan.** Everything else here can be put down and picked
> up. A service must stay up, be backed up, and be secured, for as long as the pool is meant to mean anything.
> It is taken on deliberately, because "the pool is open to all players" is not achievable any other way.

---

## 8. The build order

**This section used to be a list of eight seams ordered by what depends on what. It is now a sequence, and the
sequence is ordered by what is cheapest to *learn*.** The eight destinations below are unchanged and none of
them has been dropped — what changed is which one is approached first, and why.

The reason for the change is a reading of the repository as it actually stands, and it is short enough to
state here. The walking skeleton is finished and its machinery is excellent: a deterministic integer
simulation, hand-rolled records, an IL scan over both the committed and a freshly built assembly, an
eight-case poison suite, and a six-row determinism matrix across three operating systems and two processor
architectures. **What none of it does is take an input from a player.** Today's build is a replay viewer of
one fixed defense against one fixed wave, over a roster of four unit types, with no currency, no build phase
and one wave per run.

A dependency order tells you what must exist before what. It does not tell you what to find out first — and
the one thing this project has never tested is **whether any of it is fun**. That question is answerable far
earlier and far more cheaply than the dependency order implies, and it is the question with the power to
invalidate everything downstream of it. So the order below asks it first.

### The sequence

Steps 1 to 4 need no engine, no licence and no editor. They run from a shell.

| # | Step | What it delivers | Size |
|---|---|---|---|
| 1 | **Cost column, one purse, wave slots, and income between waves** | Every integer already in `content/units.txt` becomes a design lever, because cost-per-effect is what makes a unit good or bad. Today there is no decision anywhere in a match: the defense is a file and the wave is a file | Small |
| 2 | **A run is N waves, with a build phase between, recorded as a command stream** | `Match` gains a lifecycle; the record gains `(wave index, decision)` pairs, which is what a build phase *is* from the record's point of view; `simcli` gains a mode that plays a command file | Medium — the real structural work |
| 3 | **Roster to about ten units, using only the levers `UnitType` already has** | Enough vocabulary for a decision to be interesting | Small — it is text rows |
| 4 | **The sweep harness: every unit against every defense, win rate and cost-efficiency to a CSV** | Balance becomes a computation while the roster is still small enough to enumerate rather than sample | Small — see [§5](#5-how-it-is-balanced) |
| 5 | **Build-phase interaction in the client: click a hex, place, compose the next wave, commit** | The first thing that is *playable* rather than readable | Medium |
| 6 | **Opponent defenses read from a folder of ghost records** | The whole loop at zero latency, with no service in it | Small — `GhostRecord` already round-trips |
| 7 | **Then** the generative depth, the two-board interface, and the service | | |

**Step 5 is fifth deliberately, and that is the load-bearing claim of this section.** Is the economy tense? Is
composing a wave interesting? Does send order matter? Is the roster varied? Every one of those is answerable
from a command line and a spreadsheet, at a fraction of the wall-clock cost of asking it through an engine.
The engine is where you find out whether it *reads* and whether it *feels* — which are real questions, and
which are worth nothing if the answer to the cheap ones was no.

### Three obligations the sequence carries

- **Step 1 inherits a decided economy, and must build the whole of it rather than the easy half.**
  *(Discharged 6 August 2026 by [#72](https://github.com/ssalter21/tower-defense-game/issues/72). This
  obligation has now read three ways — "how an attack purchase pays back", then "how the two purses feed each
  other", now this — and the churn is the point of settling it on paper before content exists.)* The economy is
  one purse, a flat base plus percentile-band bonuses on top of it, 10% interest on the bank, an unlock gate
  and scarce wave slots. **The trap is shipping the cost column and the base income and calling step 1 done**,
  because the base is the only part computable without a field — and the parts that are not computable yet are
  the parts that stop attacking being dominated. See [§3](#one-purse--restored-6-august-2026).
- **Step 2 opens the input seam, and it is the one place in this sequence worth being slow.** There are ADRs
  *and tests* asserting that no input reaches the simulation, and that discipline is why determinism holds.
  The shape that preserves it: **the view emits a command, the command goes into the record, the record is
  what the match consumes.** Done that way every playtest is also a determinism test — exactly as scrubbing is
  today — and [seam 2](#2--the-submission-barrier) comes nearly free later, because a submitted turn *is* a
  command batch. Done the other way it contaminates the one guarantee everything here rests on.

  *Added 7 August 2026 by [#74](https://github.com/ssalter21/tower-defense-game/issues/74):* **run length N and
  field size K are parameters of the lifecycle, not constants.** Ten and ten are the answers, and both are
  expected to move — the round cap may be lifted entirely. Two signatures, cheap now and tedious across every
  call site later, which is the same argument [seam 4](#4--the-balance-harness--pulled-forward-to-step-4) makes
  for the sweep taking its map as a parameter.
- **Step 3 exhausts rows before it touches the schema.** `UnitType` already carries eleven integer levers —
  HP, speed, range, cooldown, windup, backswing, damage min, damage max, delivery, projectile flight, dying
  ticks — and the four committed unit types use about half of that space. A slow sieger is a cooldown and a
  damage range; a swarm is HP and speed; a sniper is range and windup. Adding a **row** costs a line. Adding a
  **field** costs a format version, a hash-layout bump and a retired ghost pool.

### What this sequence deliberately does not do

- **It does not decide the match format in full before building anything.** Seam 1's ruleset is still owed, and
  §3's depth direction is still where the game is going. What the sequence rejects is the idea that it must be
  *finished* first. Steps 1 to 3 are the smallest form of that ruleset that can be played, chosen so that being
  wrong about them costs a text file rather than an effort.
- **It does not commit to the generative roster yet.**
  [The depth research](research/build-depth-in-tower-defense.md) ranks Direction A first and the reasoning
  holds — but it signs a content bill of **25 to 56 authored units** that the rule decides rather than taste.
  That bill should be signed after the loop is proven, not before, and the note itself says Direction B
  composes onto A later at no cost, so nothing is lost by starting flat.
- **It does not build the multiplayer.** Async round-robin is the same loop at a different latency — which is
  [§2's](#2-the-loop--one-machine-at-three-latencies) own claim — so the loop can be found and tuned at zero
  latency against defenses read from a folder. This is the largest scope deletion available and it removes
  accounts, matchmaking, rating, anti-cheat re-simulation and the two-board interface from the critical path
  to *is this fun*. **It defers them; it does not repeal them.**

### The five decisions that block step 1

Small, on paper, blocking, and all of them get more expensive the more content exists when they are answered.
None needs an engine, a server or an asset. This is a **checklist**; the full description of each lives where
it already did, and the right-hand column links to it rather than restating it. They are charted as
[map #70](https://github.com/ssalter21/tower-defense-game/issues/70), one ticket each.

| # | Question | What is already known |
|---|---|---|
| 1 | ~~How does an attack purchase pay back under one purse?~~ ~~How do the two purses feed each other?~~ **✅ Decided** | **Resolved 6 August 2026 by [#72](https://github.com/ssalter21/tower-defense-game/issues/72).** One purse, called sauce. The payback is a flat base plus percentile-band bonuses on top; separation comes from an unlock gate and scarce wave slots; timing comes from 10% interest. No money moves between players. Detail in [§3](#one-purse--restored-6-august-2026) |
| 2 | ~~Is there a shared, public baseline wave?~~ **What is on the variance anchor schedule?** | **Answered in principle 6 August 2026.** There is a public constant, and it is an anchor schedule rather than a baseline wave — fixed waves at which a major variance event unlocks for everyone. What remains is which waves and which events. Detail in [§3](#wave-variance-is-anchored-not-emergent) |
| 3 | ~~What is a run?~~ **✅ Decided** | **Resolved 7 August 2026 by [#74](https://github.com/ssalter21/tower-defense-game/issues/74).** Ten waves, each resolved against a field of ten, ending at zero health or the tenth wave. Health is denominated in sauce and cannot be repaired; damage is the field average; ranking is waves survived then health remaining; the outcome is a vector of per-round `(dealt, taken)` pairs. Detail in [§3](#a-run-is-ten-waves-and-health-is-money--decided-7-august-2026) |
| 4 | **How wide is the damage-type matrix, and what is the armour formula?** | Flat-subtraction armour punishes many-small-hits quadratically, so rule it out, and lean narrow. Detail in [§10](#the-open-questions) |
| 5 | **What does the player get to compute before they commit, and what does it cost them?** | **New, 6 August 2026, and it blocks step 1 for the same reason the others do.** A deterministic sim can answer any question the player asks, and a game that answers all of them has been solved rather than played. Detail in [§12](#12-the-planning-phase-is-the-game) |

### The eight seams, and where they land

The destinations are unchanged. This is where each one meets the sequence above. **Each is still the subject
of its own wayfinder map** — its own destination, its own decision tickets, its own sessions.

| # | Seam | The destination it finds its way to | Where it lands |
|---|---|---|---|
| 1 | **The match format** | A decided-in-full ruleset for a single match, including the shape of its depth | Steps 1–3 are its first half, taken as experiments rather than as a finished ruleset |
| 2 | **The submission barrier** | One mode architecture proven to serve all three latencies | After step 6, and half-paid by step 2's command stream |
| 3 | **The roster** | What towers and attacking units exist, and what they vary by | Step 3 flat, then revisited at step 7 |
| 4 | **The balance harness** | A tool that names what is mispriced, and the definition of mispriced | **Pulled forward to step 4** — it is a minute's compute, not a night's |
| 5 | **The service** | Accounts, pool, submission, standings, replays, re-simulation | After step 6. Nothing before it needs a server |
| 6 | **The social layer** | What makes an absent opponent feel like a person | After seam 5, unchanged |
| 7 | **The interface** | Reading two boards, an economy and a build menu at once | Step 5 is its single-board half; the two-board problem is step 7 |
| 8 | **The presentation** | The art pipeline, and what makes it look composed | Independent, whenever there is appetite |

### 1 · The match format — *its first half is steps 1 to 3*

What one wave actually is. Two boards resolving at once, the build-phase rhythm, what a build phase
offers, how a wave is composed, what a wave is worth and what winning one means. *(The economy is no longer
part of this list — [§3](#one-purse--restored-6-august-2026) settled it.)*

It also owns the **shape of the depth** from [§3](#3-what-a-match-is), and that is the larger half of it: what
the combination system actually is, whether the creep pool is gated on your towers and how, whether send order
is a real decision, and — from [§10](#10-not-yet-specified) — whether the defending side is towers at all.

**Everything is downstream of this** — but not of a *finished* version of it, and that distinction is the whole
change in this section. The roster cannot be designed, the harness cannot be pointed at anything, the record
format cannot be fixed and the interface cannot be laid out until *some* rules exist. It does not follow that
all of them must be decided before any of them are played, and the sequence above takes the other reading:
build the smallest ruleset that can be played, at a standard where being wrong costs a text file.

It remains the cheapest seam to be wrong about now and the most expensive later, and it needs no server, no art
and no friends to answer.

**All three research notes have landed**, so it is ready to chart. Each note ends with two or three ranked
candidate directions and none of them decides anything — that is deliberate, and it is seam 1's to do. It
inherits three obligations they surfaced: make attacking pay for itself, spend the
computed-balance budget knowingly, and decide whether there is a baseline wave at all. The first and third are
now [step 1's blocking decisions](#the-five-decisions-that-block-step-1) — **and the first is discharged**, by
[§3](#one-purse--restored-6-august-2026).

**The cheapest coherent starting point is already identified in the research**, and it is what steps 1 to 3
build. [The sending research](research/attack-composition-and-sending.md) ranks *universal roster — the wave
**is** the order and the clock* first, explicitly because its cost is approximately zero: the ordered wave, the
tie-break rule and the overtake landmark all exist and are tested. It is also the direction that asks *is
composing a wave against a fixed, non-reacting defense fun?* with the fewest confounds — which is the question
the build order above exists to reach.

### 2 · The submission barrier

Design the one loop and prove the unification in §2 is real: that async round-robin and the live lobby are the
same machine at different latencies, and that the record format transmits a turn as cleanly as it stores a
ghost. Includes stage matching, pool draw, and the hand-authored floor that keeps every stage populated.

If the unification is wrong, this project is building two games — and that is worth finding out before the
rules are written into a service.

### 3 · The roster

What towers and attacking units exist, how many, and what they vary by. Part V already built the structure this
fills: **one unit schema, two roles**, levers as components, the vocabulary versioned separately from the
numbers. **Six of Part V's levers were killed by the corridor and are now alive again** — they were the ones
that depended on mazing, and the [maze reversal](#the-board-is-a-maze-again--reversed-6-august-2026) restores
every one of them. This seam got materially larger on 6 August 2026.

Now also owns the **creep roster's classes and roles** from [§3](#3-what-a-match-is) — tanks, damage, support,
swarm, specialists — and the open question that comes with them: whether Part V's *one unit schema, two roles*
still holds once the attacking side has genuine internal structure, or whether roles are a third thing the
schema has to carry.

Constrained hard by [§4](#4-what-persists): nothing is unlocked, so **every unit must be interesting from the
first run** — and by [§6](#6-what-it-looks-like), because a unit whose role cannot be read off its silhouette
fails the accessibility pillar however well it plays.

### 4 · The balance harness — *pulled forward to step 4*

The tool, and the definitions underneath it. What a sweep is, what it measures, what a red cell means, what
"cost-efficient" is in a one-purse economy where a unit competes with a tower, a wave slot and 10% interest all
at once, and how the harness's verdict gets back into `content/` without invalidating a pool of stored ghosts.

**It also owes step 1 a debt it does not obviously owe.** The percentile bands in
[§3](#one-purse--restored-6-august-2026) are measured against a field, and there is no field until step 6 — so
**the sweep's canned set is what the economy measures against in the meantime.** That makes the harness part of
the economy rather than a tool pointed at it, and it is why the bonus is uncomputable before step 4.

**It used to depend on seams 1 and 3 and now it does not wait for either.** The measurement in
[§5](#5-how-it-is-balanced) is why: at 2.75 ms a match, a sweep is a minute of compute, so the harness is a
`simcli` mode and a CSV rather than a project. Building it against a ten-unit roster is what makes the eleventh
unit cheap to author — and if Direction A is ever adopted, its one documented failure mode (a U-shaped meta
where the widest and narrowest builds dominate) is caught by a report of win rate **binned by number of
ingredients taken**, which is a column in a sweep that already exists rather than a tool built in response to a
problem two studios took years to notice.

### 5 · The service

The permanent obligation from [§7](#7-what-runs-it). Accounts and identity, the pool and its stage index,
submission and the barrier, standings and rating, replay storage and retrieval, and server-side re-simulation
as anti-cheat. Also the questions Part II raised and this document has not closed: ghost expiry windows, pool
re-validation on a content change, and rating under inactivity.

### 6 · The social layer

What converts an absent opponent into a person. Named defenses, named opponents, replays you can watch and
send, "your defense held against three of five", the challenge you aim at one specific friend. Part II §3 is
blunt that this is not optional — presence is made of specifics, and a mechanism that is asynchronous must be
presented in a way that is relentlessly personal.

Note what is *not* here: browsing, curation and discovery surfaces. Opponents are drawn, not shopped for, so
Part II §5's UGC-discovery failure mode does not apply.

### 7 · The interface

The hardest unsolved problem in the design, and [§6's](#6-what-it-looks-like) accessibility pillar makes it
harder still. **Harder again since 7 August 2026**, because a round now resolves against
[a field of ten](#a-run-is-ten-waves-and-health-is-money--decided-7-august-2026) rather than one opponent —
so *what the player watches at all* is the seam's largest open question, and this document does not answer it.
Twenty resolutions cannot each be watched, and a round that is only ever summarised is a round nobody sees.
Two live battles, one economy, a build menu, and a readable account of what the opponent just did
— on one screen, legible at a glance, to somebody who has never played it, with **no unlock ramp available to
stagger the options**. Includes the faction-colour scheme, which is as much an information-design decision as
an art one, and the presentation of whatever combination system seam 1 chooses — a combinatorial build space
that cannot be read is a menu with extra steps.

### 8 · The presentation

The KayKit purchase and the licence confirmation ([#56](https://github.com/ssalter21/tower-defense-game/issues/56)),
the atlas-recolour workflow, the `.blend` editing path, and the lighting, VFX and camera work that makes stock
models look composed. Independent of the others — it can run whenever there is appetite for it.

---

## 9. What this overturns

Read against Parts I to V, so nothing below is left standing where it has been replaced.

| Where | What it said | What is true now |
|---|---|---|
| **I** — whole document | Commercial viability is the question | **Superseded.** Not a commercial product. The market analysis is background, not a constraint. |
| **II** — the async argument | Async is justified by the 830-player synchronous ceiling | **Reason replaced, conclusion kept.** Async is justified by schedule mismatch. True at three players. |
| **II §3** — defense feels meaningless | Fix it with cross-fed currencies, per Supercell | **Not adopted.** Both boards are live every round, so nothing needs cross-feeding. One purse. |
| **II §3** — matching axis | Match on progression state first, rating second | **Sharpened twice.** The draw is per *round*, at the matching stage — so it is the only matching axis that exists — and since 7 Aug 2026 it draws a **field of ten** rather than one. |
| **II §5** — UGC discovery | Curation is a feature, not a backlog item | **Does not apply.** Opponents are drawn, never browsed. No discovery surface exists to get wrong. |
| **II §6** — build order | Private friend lobbies are step 7, the one synchronous mode | **Promoted and reclassified.** The lobby is not synchronous and is not last; it is the same loop at low latency. |
| **III** — networking | No realtime networking | **Stands, for a new reason.** Live PvP is in scope and still needs none — a build phase with a barrier is a turn. |
| **III** — balance as computation | A deterministic sim turns balance into a computation | **Adopted as the method.** Seam 4 is where the claim gets spent. |
| **IV** — KayKit Complete, $150 | The day-one purchase | **Reactivated.** It was paused for the free-tier walking skeleton, never overturned. |
| **IV** — can the dev rig and animate? | The KayKit-versus-Synty recommendation turns on this | **Closed by irrelevance.** KayKit ships animations; the question only mattered for Synty. |
| **V** — the unit schema | One unit, two roles; levers as components; versioned vocabulary | **Stands, and is now load-bearing.** Seam 3 fills it in. |

### And what this document overturns in itself

Three claims made above were written before the walking skeleton existed, and reading the finished skeleton
changed them. Recorded here rather than quietly edited, because a standing document that revises itself
silently is one nobody can trust the age of.

| Where | What it said | What is true now |
|---|---|---|
| **§5** — the harness | Sweep thousands of matches **overnight** | **Off by orders of magnitude.** `BudgetTests` measures the committed match at 2.75 ms — ~360 matches a second on one core. A sweep is a minute, so the harness is a `simcli` mode and a CSV, and it moves *before* the roster instead of after it. |
| **§8** — the seams | Eight seams ordered by what depends on what; the match format decided in full first | **Reordered, not repealed.** A dependency order does not say what is cheapest to *learn*, and the one untested claim in the whole design is that this is fun. The seams stand as destinations; the sequence in §8 is how they are approached. |
| **§8 seam 4** — the harness again | Depends on seams 1 and 3 | **Independent of both.** It needs a purse and a roster of any size, not a finished ruleset. |

### And what was reversed on 6 August 2026

Six changes, made after [the skill note](research/fun-and-skill-expression.html) audited which of the genre's
skill axes this design could still charge the player for. Four of the six exist to buy back an axis the design
had deleted or inverted.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§3** — one purse | A single currency; the sharp decision the game is built around | **Reversed, then reversed back the same day — see the note below.** Two purses stood for a matter of hours; the settled answer is **one purse**, with the payback supplied by performance rather than by a second currency | A purchase that only subtracts has no timing question attached, and timing is what players practise |
| **§3 / §11** — the corridor | One hex wide, never branches; mazing and pathfinding permanently out of scope | **Reversed. A maze, deliberately hard to solve, at several elevation levels**, with elevation granting range | Geometry is the axis the genre was popularised on, and it was the largest deletion in the design |
| **§3** — send ordering | Rescued by the corridor, which *is* a single-file column | **Weakened, not repealed.** A branching map dilutes order; the map must now be designed to preserve it | Consequence of the reversal above, recorded rather than discovered later |
| **§3** — wave composition | The player composes the whole wave; a baseline wave was an open question | **Anchored.** A public schedule injects major variance at fixed, known waves | Without a public constant, preparation had nothing to be a skill about |
| **§3** — the offering | Not specified; the depth research ranked a private random offering third | **Public. Everyone sees the same options**, Mechabellum-style | A send is only a read if both players know the menu — and it makes the shop a second-order decision |
| **§2 / §3** — the async ghost | Opponents are drawn and their stored defense is what you compose against | **The round-robin no longer shows you a board.** It pays you in statistics over the field instead | A frozen defense cannot react or lie, so inspecting it produces a lookup rather than a read |
| **§2 / §3** — the map | One authored corridor, implicitly permanent | **Generated, and rotating daily or weekly**, selected by sweeping candidates for outcome spread | A hard map buys time; an unseen one buys it permanently — and the harness can already measure which is which |
| **Bottom line** | "the creeps you can send determined by the towers you chose" | **Dropped from the bottom line, still live in §3 as a direction.** A public shared offering is in tension with a private tower-gated pool, and seam 1 now owns the reconciliation | Recorded rather than silently cut |

#### And the purse went back, later the same day

The first row above is a round trip, and it is left visible on purpose. One purse was reversed to two in the
morning and back to one by evening, decided in
[#72](https://github.com/ssalter21/tower-defense-game/issues/72).

**Nothing was wrong with the reasoning that produced two purses; it was answering a question that turned out to
have a cheaper answer.** The objection on file was never "one wallet is bad" — it was "attacking must pay you
back". A second currency pays that back through the economy's *structure*. Percentile bands pay it back through
the wave's *result*, and they cost one income rule instead of a whole parallel wallet with its own generator,
its own prices and its own balance surface.

Worth keeping for its own sake: **the two-purse decision survived less than a day of being written down, and
cost nothing but a section.** That is the sequence in [§8](#8-the-build-order) working exactly as designed — a
decision made on paper before any content exists is a decision that can be unmade for the price of editing a
paragraph. Had it been reversed after step 3, it would have cost a roster, a cost column and a record format.

### And what was reversed on 7 August 2026

One change, and it is a structural one. It came out of
[#74](https://github.com/ssalter21/tower-defense-game/issues/74) — a ticket asking only *what is a run* — which
is worth noting, because the reversal was not the question being asked.

| Where | What it said | What is true now | Why |
|---|---|---|---|
| **§2** — the round-robin draw | A different stored defense **every wave**; ten opponents per run | **A field of ten every round.** Ten rounds against ten opponents each, with the round's result taken as the **average** across the field | The one-draw-per-wave rule was carrying the whole variance-control argument on ten samples across an entire run. Averaging ten *per round* does that job far harder — a bad matchup becomes a rounding error rather than a dent |

**Two things this nearly broke, and how each was closed:**

- **Mode symmetry.** In a lobby your wave is one of the ten in everybody else's field, so it takes real health
  off real players; in round-robin the ten are stored ghosts, and a ghost has no run to lose. Same machine,
  different consequence — precisely what [§2's](#2-the-loop--one-machine-at-three-latencies) *one machine at
  three latencies* exists to preclude. **Closed by storing your wave and letting it enter other players' fields
  later**, identically in all three modes: in a lobby that lands within the round, in round-robin whenever you
  are drawn. Symmetry is restored across *time* instead of within a round, and it costs nothing, because a
  stored wave and a stored defense are the same kind of object the pool already holds.
- **The interface.** [Seam 7](#7--the-interface) was already the hardest unsolved problem in the design at two
  boards. It is now twenty. **Not closed** — recorded, and handed to the seam.

**And one thing that got quietly better.** [§12's](#12-the-planning-phase-is-the-game) argument that the honest
answer to *"will this wave work"* is a **distribution rather than a result** stops being an aspiration and
becomes the literal shape of the data: the field of ten *is* the distribution, and it exists every round rather
than being assembled out of a pool.

---

## 10. Not yet specified

In scope, headed toward the destination, not yet sharp enough to seam.

### Research in flight

### Research landed

Three notes were commissioned against [§3's](#3-what-a-match-is) depth direction and the open question below.
**All three are in.** They are decision inputs for seams 1, 3 and 7, and the match-format session is now
unblocked.

| Note | The question it answers |
|---|---|
| ✅ **[Build depth](research/build-depth-in-tower-defense.md)** — *landed* | How TD games produce combinatorial depth. Verdict: two structurally different routes, and **only the generative one is simultaneously a depth mechanism, an accessibility mechanism, and enumerable by the harness**. The corridor kills **one of eleven** mechanisms, far less than feared; what "nothing persists" removes is the onboarding ramp, and the fix is to move it *inside the run* |
| ✅ **[The attacking half](research/attack-composition-and-sending.md)** — *landed* | How sending is made deep. Verdict: seven mechanisms, five survive, **ordering is *strengthened* by the hex corridor**, and the income loop the genre is built on is the one the single purse takes away. The gating idea has **one thin precedent, since removed** |
| ✅ **[Why tower defense is fun, and where the skill is](research/fun-and-skill-expression.html)** — *landed 6 Aug 2026* | Why the genre is fun, and where its skill expression lives. Verdict: six fun mechanisms, each of which **inverts into a known failure mode**; skill comes from **eight axes**, of which this design was deleting two, inverting one and leaving a fourth unanchored. **Four of the six reversals recorded in [§9](#and-what-was-reversed-on-6-august-2026) are answers to this note** |
| ✅ **[Making the plan the game](research/planning-phase-and-simulated-stats.html)** — *landed 6 Aug 2026* | How to elevate the build phase, and what a fast deterministic sim can be spent on as design material rather than as tooling. The direction it feeds is [§12](#12-the-planning-phase-is-the-game) |
| ✅ **[Towers, or placed squads?](research/towers-versus-placed-squads.md)** — *landed* | The open question below. Verdict: the aesthetic half is free and mostly already decided by Part IV §5; the mechanical half is one number, projectile volume, and it lands on `FlyProjectiles` rather than on target acquisition |

### The open questions

> **Three of these are no longer unscheduled — they block step 1.** *How wide the damage-type matrix is*,
> *what a run is*, and *whether there is a baseline wave* are marked **→ blocks step 1** below, and appear as a
> checklist alongside the settled economy question from [§3](#one-purse--restored-6-august-2026) in
> [§8's five decisions](#the-five-decisions-that-block-step-1). **The detail stays here** and the checklist
> there points back to it, so there is one description of each question and not two that can drift apart.

- **Does the defending side have to be towers?** The alternative floated: **walls flanking the path as a
  placement surface** — archers on a rampart running alongside the corridor — with squads that shoot, upgrade
  and get augmented. An RTS-ish read rather than a tower-defense one.

  > **The walls do not block, and that is the developer's own clarification.** They are a surface you place
  > defenders *onto*, beside the corridor, chosen for how it looks. They do not sit in the path, do not alter
  > the route, and **do not threaten "no mazing, ever"** — recorded here because "walls" reads as "blocking" to
  > anyone arriving cold, and this question should not have to be re-litigated every time somebody does.

  > **Squads are static — settled.** A stationary squad is a tower with a different silhouette. No movement
  > decisions enter the simulation and pathfinding stays out permanently. The moving-squad branch — chasing,
  > retreating, re-blocking — was priced and closed: it would have reopened a settled decision, and it is not
  > being taken.

  > ⚠️ **What survives is projectile volume**, and
  > [the research note](research/towers-versus-placed-squads.md) has now priced it. Three findings from it
  > matter at vision level:
  >
  > - **The ghost record costs nothing.** A record stores *inputs*; projectiles are output. `GhostRecord`,
  >   `ReplayBundle`, `TowerLayout` and `RecordFormat` do not mention projectiles at all, so squad size is free
  >   in stored bytes. *(This corrects an earlier claim here that a squad cost N× the replay. It does not.)*
  > - **The cost lands on the simulation's hottest loop, and it is not the one you would guess.** Not target
  >   acquisition — `FlyProjectiles`. Every projectile resolves its target by a deliberate linear scan of the
  >   creep array, every tick it is in flight, so the term is **O(projectiles × creeps)** and a squad
  >   multiplies it directly. It is then multiplied again by every match the balance harness sweeps.
  > - **Modelling each archer as its own shooter buys nothing.** N identical archers on one cell share one
  >   coverage interval, are handed the same target, start in the same state and never drift apart. A squad of
  >   N independent shooters is *behaviourally identical* to one shooter firing N arrows — **unless the bodies
  >   can die independently.** Attrition is therefore the only thing that justifies the expensive model, which
  >   turns a performance question into a design one.

  The note's recommendation is a scenery rampart with squads as one simulation entity drawn as N bodies, and
  hitscan for the fast squad weapons — with the escape hatch that delivery is a *column in `content/units.txt`*,
  so projectile volume stays reversible per unit type from a data file rather than a rewrite. **Two independent
  lines — silhouette legibility, and the attention budget of watching two boards — converge on squads being an
  archetype rather than the model for the whole defense.** None of that is decided here; it is seam 1's to take
  or leave.

- **Co-operative play.** Wanted, and deliberately unstructured. Every other mode fits the submit-wait-resolve
  loop; co-op may or may not, and it needs authored escalating content rather than player-composed waves, which
  is a different content problem from anything else here. Revisit once seams 1 and 2 have resolved.
- **How wide the damage-type matrix should be, and what the armour formula is.** **→ blocks step 1.** Carried forward from Part V
  §4.1, still open, still cheap to set on paper. Legion TD 2 runs 1.67:1, Element TD 2 runs 4:1, Warcraft 3
  runs 40:1. Belongs to seam 3 or 4 when one of them reaches it — but the squad research has already
  constrained it from an unexpected direction: **many-small-hits is punished quadratically by flat-subtraction
  armour**, so that formula should be ruled out if squads exist at all, and the same finding argues for the
  narrow end of the matrix. A presentation choice reaching back into the damage model is exactly the kind of
  coupling seam 1 exists to catch early.
- ~~**What a run is.**~~ **Closed 7 August 2026 by
  [#74](https://github.com/ssalter21/tower-defense-game/issues/74) — ten waves, a field of ten each round, a
  sauce-denominated health pool, and death.** See
  [§3](#a-run-is-ten-waves-and-health-is-money--decided-7-august-2026). What it left open, and deliberately:
  **the gamble** — opting out of the field average to face a single opponent drawn from the distribution,
  possibly choosing where in the distribution to draw from. It is the antidote to averaging making every round
  tend toward the mean, and best-of-ten is its natural payoff. Not decidable before a real field exists at
  step 6.

- ~~**Whether there is a baseline wave at all — a risk no surveyed game carries.**~~ **Closed 6 August 2026 —
  there is a public constant, and it is [the variance anchor schedule](#wave-variance-is-anchored-not-emergent)
  rather than a baseline wave.** An anchor supplies what a baseline was wanted for — something for a send, and
  a newcomer, to be read against — without also supplying the content of the wave, which is the part the
  player is meant to author. What remains open is *which* waves and *which* events, which is now question 2 on
  [the step 1 checklist](#the-five-decisions-that-block-step-1). The original statement of the problem is kept
  below because the reasoning is what justifies the anchor.

  In Legion TD 2 the wave is a
  memorised public constant, which is exactly what makes a sent mercenary legible: it is *the part that is not
  the wave*. Here the player composes the **whole** wave, so there is no constant to read it against. The
  sending research suggests a shared baseline wave per stage as cheap insurance, and flags it as needing a
  decision regardless of which direction seam 1 takes. It also bears directly on
  [§6's](#6-what-it-looks-like) accessibility pillar: a newcomer with no baseline has nothing to compare
  against.
- **What the rotation cadence is, and how the pool survives it.** **Added 6 August 2026.** Daily and weekly
  pull in opposite directions and the pool is the thing being pulled. Faster rotation buys freshness against
  solving and gives the whole player base one shared map to be compared on — but it empties the
  `(map, stage)` ghost pool every cycle, and the pool is what the async mode *is*. Slower rotation lets the
  pool fill and lets a map be learned, which is most of where mastery would come from, at the cost of the map
  being solved before it turns over. **The three candidate answers are in
  [§3](#the-map-rotates-and-it-is-generated)**; the note that surveys them is
  [Generated maps, and how often they turn over](research/generated-maps-and-rotation.html). Not blocking
  until step 6, since nothing before that reads a pool at all.
- **How big the map archive has to be, and whether a map may ever repeat.** A generator plus a sweep produces
  an archive; a scheduler draws from it. Whether the archive is large enough that no player sees a map twice,
  or small enough that maps become known quantities with a metagame, is a design choice and not a capacity
  one — and it is the same lever as the cadence, viewed from the other end.
- **Rating at two scales at once.** The pool is all players and the rivalry is a friend group. Whether those
  are one ladder or two is unresolved.

## 11. Out of scope

Ruled beyond the destination. These do not graduate; they return only if the destination is redrawn.

- **Monetisation, pricing, store presence, wishlists, marketing, launch windows, demo cadence.** Consequences
  of §1, all of them.
- **Progression systems *between* runs.** Unlocks, collections, account levels, roster development, seasons,
  battle passes. Consequences of §4. **In-run progression is not ruled out and never was** — the skill tree a
  tower opens onto your creep pool ([§3](#3-what-a-match-is)) lives and dies inside one run, which is exactly
  what makes it legal.
- **Realtime netcode.** Lockstep, rollback, tick synchronisation, prediction. Consequence of §2 — no mode needs
  it.
- ~~**Mazing and pathfinding.**~~ **Withdrawn 6 August 2026 — this is now in scope.** It was settled before
  this document on the grounds that the corridor is one hex wide and never branches, so no unit ever chooses a
  path. The geometry is being changed dramatically instead; see
  [§3](#the-board-is-a-maze-again--reversed-6-august-2026) for what that costs and what it buys. Left in place
  struck through rather than deleted, because several documents downstream still reason from the corridor.
- **Discovery, curation and browsing surfaces.** Consequence of the per-wave draw.
- **Custom character geometry as the default.** Stock models are the pipeline; `.blend` editing is a tool kept
  for specific need, not a programme of work.
- **Moderation and community management at scale.** The pool is open, but a personal build does not take on a
  trust-and-safety function.

---

## 12. The planning phase is the game

**Added 6 August 2026.** This is the section the identity claim in the bottom line owns, and it is a
*direction* in the same sense [§3](#3-what-a-match-is) is — the mechanisms belong to the seams. Its companion
research note is [Making the plan the game](research/planning-phase-and-simulated-stats.html), which surveys
the shipped precedents and ranks the candidates.

### The claim

Nothing happens during a wave. That is settled, and it is what makes a stored ghost a legal opponent and a
submission barrier a substitute for netcode. The consequence is that **every axis of skill this design has is
collected in one phase** — where every comparison game in the genre spreads it across two. A build phase that
offers three clicks and a confirm button is not a smaller version of Legion TD 2's build phase. It is the
entire game.

So the build-and-compose phase gets treated as the main event and budgeted like one, and the two things it
gets budgeted from are unusual assets that this project already owns:

- **A deterministic integer simulation that resolves a match in 2.75 ms** — roughly 360 matches a second on
  one core. Anything a player might want to know before committing is a computation, not a guess.
- **A record format that stores inputs rather than outputs**, so any position can be re-run, re-run with one
  thing changed, or re-run ten thousand times against a field.

The nearest neighbours are therefore not tower defense games at all. They are Path of Building, whose
community plans in an offline calculator because it is better than the game's own tools; Zachtronics'
histograms, which turn a finished solution into a comparison; Football Manager, whose match is watched rather
than played; and Into the Breach, which gives away the enemy's next move on purpose.

### Two things this buys, stated as direction

**The plan phase should sing.** Perfect information about *mechanism* is a feature, not a leak — Into the
Breach telegraphs every enemy attack precisely so that failure belongs to the player and not to a black box.
Range overlays, damage previews, the arithmetic done for you, the thing you are about to buy shown against the
thing it replaces: none of that reduces the decision, and all of it moves the difficulty from *arithmetic* to
*judgement*, which is where it should be.

**The stats the sim can compute are game design material, not a debug menu.** A run against a hundred stored
defenses is under a second of compute. That is a mechanic, a reward structure and a presentation layer that no
tower defense has because no tower defense could afford it:

- **A distribution instead of a result.** Your wave scored a mean and a spread against the field. Reward
  **both the best you achieved and the average**, because they are different skills — peak play and robust
  play — and a player optimising one will do poorly at the other. That tension is deliberate and it is
  SpaceChem's, whose three competing metrics exist for exactly this reason.
- **Placement against the aggregate, not a leaderboard.** SpaceChem replaced global leaderboards with
  histograms because a leaderboard's only message to most players is *that* they are bad and not *by how
  much*, and because a name at the top is an incentive to cheat. A server that re-simulates every claim
  already has the anti-cheat half; the histogram is the presentation half.
- **Retrospective analysis with real teeth.** Re-running a finished match with one purchase changed is one
  more match — 2.75 ms. A run can therefore be reviewed the way a chess game is: *wave 7, the sniper instead
  of the tank was worth this much.* This is the most distinctive thing on the list and the genre has no
  equivalent.
- **A computed highlight reel.** Because the whole match is resolved before anything is drawn, the moments
  worth showing can be *chosen* rather than recorded — the closest call, the first leak, the shot that
  decided it. You should not watch a hundred matches. You should watch the three that came down to one unit.
- **"Far less solvable" becomes measurable, and then becomes a filter.** The harness already exists to price
  units; pointed at maps and strategies instead it reports how wide the outcome spread is across good plans,
  which is what "solvable" means when written down. Wire that score back into a generator and it stops being
  a report and becomes **selection pressure** — the search-based generation loop
  [§3](#the-map-rotates-and-it-is-generated) rests on. The standard objection to that method is that
  simulating every candidate is too slow; at 2.75 ms it is not.
- **Seeding has to be as cheap as running.** A generated, rotating, verifiable map is only affordable if
  producing one is a pure function of a seed with no filesystem in it — which is what
  [ADR 0018](adr/0018-the-simulation-never-touches-the-filesystem.md) already requires of everything else in
  `sim/`. The pieces are in place: `HexMap.FromCells` builds a grid from bytes, `Match` already threads a
  `ulong seed`, and `MapHash` hashes the parsed grid so a server can verify which map a client claims to have
  played. **What is missing is a `simcli` mode that turns a seed into a map and a match, and it should be
  built when the harness is** — a generator you cannot invoke from a shell is a generator no sweep can use.

### The constraint that makes all of it safe

> ⚠️ **A simulation that answers everything deletes the game, and this is the one real hazard in the section.**
> Into the Breach gives perfect information about *the next turn*, never about the outcome. If the player can
> compute their exact result before committing, there is no decision left — only data entry.
>
> The design already contains its own antidote and it should be recognised as one:
> [the round-robin measures your wave against many defenses](#what-you-see-of-your-opponent-depends-on-the-mode-and-that-is-deliberate),
> so the honest answer is a distribution rather than a number. **Uncertainty here comes from the breadth of
> the field, not from hidden state or dice** — which is the only form of it compatible with a deterministic,
> re-simulable, cheat-proof game.
>
> Whatever else seam 1 grants the player, the forecast must stay *partial, sampled, or paid for*. That is
> [question 5 on the step 1 checklist](#the-five-decisions-that-block-step-1).

---

## Sources

Everything factual here is either established in Parts I to V, verifiable in this repository, or listed below.

1. **Parts I–V** — [`docs/`](README.md). Their claims are inherited except where [§9](#9-what-this-overturns)
   replaces them.
2. **This repository** — `sim/` (deterministic Fix64 simulation, hex map, ghost record), `simcli/`,
   `client/Assets/` (Unity 6 URP view, KayKit imports), `tools/` (headless entry points),
   [`docs/sit-down.md`](sit-down.md). The walking skeleton is landed on `main`.
3. **KayKit** — [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete), CC0, $150, `.blend`
   sources. Licence and price **pending browser confirmation** under
   [#56](https://github.com/ssalter21/tower-defense-game/issues/56).
4. **CC0 1.0** — [Creative Commons deed](https://creativecommons.org/publicdomain/zero/1.0/): copy, modify,
   distribute and perform, including commercially, without permission.
5. **Legion TD 2** — the both-boards-at-once match structure, and the two-currency loop whose *problem
   statement* [§3](#one-purse--restored-6-august-2026) adopts even though it declines the solution: attacking
   must pay you back.
6. **Element TD** (Warcraft 3 mod) — the named reference for §3's combinatorial build depth. Its element
   combination system is the target class of depth, not a specification. Under research.
7. **Bloons TD 6** — the standing proof that legible-to-a-child and competitively deep are compatible, which
   §6's accessibility pillar depends on being true. Under research.
6. **Super Auto Pets / Backpack Battles** — the per-round draw against a snapshot at the same stage, and the
   AI-fill answer to an empty pool.
7. **Supercell** — "Builder Base 2: Balancing Attacking, Defending and Builders", the source of the
   defense-feels-meaningless finding this document answers differently.
8. **Slay the Spire daily** — the nothing-persists-but-rating model.

Added 6 August 2026, for the reversals in [§3](#3-what-a-match-is) and the direction in
[§12](#12-the-planning-phase-is-the-game). Each is surveyed properly in
[Making the plan the game](research/planning-phase-and-simulated-stats.html) and
[Why tower defense is fun, and where the skill is](research/fun-and-skill-expression.html).

9. **Mechabellum** — the public shared offering. Its reinforcements are the same on both sides, which is what
   makes the shop a mind game rather than a private draw.
10. **Teamfight Tactics** — between-round scouting as the loop the live lobby is modelled on.
11. **Zachtronics** (SpaceChem, Opus Magnum) — histograms instead of leaderboards, and competing optimisation
    metrics as a deliberate tension. The source of the best-and-average reward shape.
12. **Into the Breach** — perfect information about mechanism, never about outcome. The safety rail on §12.
13. **Path of Building** — the community's offline planner for Path of Exile, and the standing evidence that a
    planning tool can be the part of a game people love most.
14. **Football Manager** — a match you watch rather than play, and the highlights-and-dashboard apparatus that
    makes that work.
