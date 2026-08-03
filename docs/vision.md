# The Vision

**The standing document** · 3 August 2026

> **What this game is, what it is not, and the eight seams that build it.**
>
> Parts I to V were written to answer whether this game was worth making and what to make it with. They were
> right about the machinery and wrong about the audience. This document fixes the destination, and where it
> disagrees with any of the five, this document is current.
>
> It is deliberately larger than one effort. Each seam in [§8](#8-the-seams) is the subject of its own
> wayfinder map.

---

## Bottom line

### A technically excellent tower defense, built for the pleasure of building it, whose multiplayer is real — and every mode of it is the same machine at a different latency.

Four claims carry the whole document.

**It is not a commercial product.** No store page, no pricing, no wishlists, no monetisation, no marketing.
The reward is the build. That deletes more scope than any other decision here, and it is what makes the rest
affordable.

**Shallow to look at, extreme to play.** Anyone should be able to pick it up and read what is happening on
screen inside a minute. Underneath that, the build depth is meant to be enormous — Element TD's
element-combination lineage rather than a list of towers — and the attacking half is meant to be as deep as the
defending one, with the creeps you can send determined by the towers you chose. Juice and legibility on the
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
| **Round-robin** | The shared pool — a different stored defense every wave, drawn at your stage | Immediately; the pool is already there | Days, or none |
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

**Round-robin draws a fresh opponent every wave, matched at the same stage.** Wave four meets somebody's
wave-four defense. This dissolves the hardest problem in the async model: a stored defense never has to evolve
mid-match, because you only ever meet it at one moment of its life. It is the Super Auto Pets structure, and it
is also the variance control Part II §3 argued round-robin exists for — ten opponents per run means one bad
draw is a dent, not a defeat.

The cost is that **the pool needs depth at every stage, not just at the end**, which sharpens Part II's
cold-start problem rather than removing it. Hand-authored defenses at each stage are the answer, and they cost
nothing architecturally: a hand-built defense and a stored one are the same object.

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

The cost is real and lands on [the interface seam](#8-the-seams): the player is reading two boards at once, and
that is the hardest unsolved presentation problem in this design.

### Build phases between waves, and nothing during one

The classic tower defense rhythm. A wave resolves with no input; then a build phase opens; then the next wave.

This is what makes the whole thing a *game* rather than an auto-battler, and it is compatible with stored
ghosts only because of the per-wave draw above. A defense in the pool is a snapshot of one stage. It never has
to play itself, never has to replay a build order chosen against a different wave, and never has to follow a
policy you authored. **It is finished, at that stage, forever.**

### One purse

A single currency. Every coin spent on a tower is a coin not spent on attackers.

The alternative — separate cross-fed currencies — would have turned each build phase into two small independent
decisions. One purse makes it one sharp decision, and it makes *what an opponent spent on* the thing you read
off them. Greeding into offense while your line thins is a real and readable gamble.

It is also the hardest of the four options to balance, which is why [§5](#5-how-it-is-balanced) is not
optional.

> ⚠️ **One purse takes away the tension every deep send system in the genre is built on, and this needs an
> answer.** [The sending research](research/attack-composition-and-sending.md) found that Legion TD 2's send
> loop is *two-currency*: gold buys workers, workers make mythium, mythium buys mercenaries, and spending
> mythium **permanently raises your income**. Every send is an investment that pays you back.
>
> Under one purse a coin spent on an attacker is simply gone — so attacking is a pure tempo loss and, at
> equilibrium, **dominated**. The one-purse precedent is not Legion TD 2 at all; it is Bloons TD Battles 2,
> where the send and the income are *the same purchase on one continuous dial*.
>
> One purse is not being reopened here — it is the sharp decision this game is built around. But it obliges
> seam 1 to make the attack purchase pay back some other way, and the note says there are three available
> answers and no fourth.

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

  > ✅ **The research came back positive on this one, and it is already half-built.** Ordering is usually a trap
  > that adds interface burden without depth — but the one-hex corridor is what rescues it: a corridor exactly
  > one hex wide that never branches **is** a single-file column, with no route decision left to dilute it. Six
  > of Part V's levers died to that corridor; ordering is the compensation.
  >
  > [`content/wave.txt`](../content/wave.txt) is already an ordered list of `(tick, type, count)`, and the
  > skeleton already found both preconditions the hard way and wrote them down: ordering is unobservable when
  > units share a speed, and unobservable again when a count spawns as one pile — *"a count is a column, not a
  > pile."*

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
defense across thousands of matches overnight, produce win-rate and cost-efficiency matrices, and let a red
cell name what is mispriced before a human notices.

This is the payoff Part III promised when it claimed a deterministic sim turns balance into a computation. It
was a claim on credit until now; this document is where it gets spent.

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

Part IV's art direction stands unchanged — stylized low-poly 3D, hex corridor one cell wide, fixed isometric
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

## 8. The seams

Eight seams. **Each is the subject of its own wayfinder map** — its own destination, its own decision tickets,
its own sessions. They are not a build order for one effort; they are the efforts.

| # | Seam | The destination it finds its way to | Depends on |
|---|---|---|---|
| 1 | **The match format** | A decided-in-full ruleset for a single match, including the shape of its depth | The three research notes ([§10](#10-not-yet-specified)) |
| 2 | **The submission barrier** | One mode architecture proven to serve all three latencies | 1 |
| 3 | **The roster** | What towers and attacking units exist, and what they vary by | 1 |
| 4 | **The balance harness** | A tool that names what is mispriced, and the definition of mispriced | 1, 3 |
| 5 | **The service** | Accounts, pool, submission, standings, replays, re-simulation | 1, 2 |
| 6 | **The social layer** | What makes an absent opponent feel like a person | 5 |
| 7 | **The interface** | Reading two boards, an economy and a build menu at once | 1 |
| 8 | **The presentation** | The art pipeline, and what makes it look composed | — |

### 1 · The match format — *next, and now unblocked*

What one wave actually is. Two boards resolving at once, one purse, the build-phase rhythm, what a build phase
offers, how a wave is composed, what a wave is worth and what winning one means.

It also owns the **shape of the depth** from [§3](#3-what-a-match-is), and that is the larger half of it: what
the combination system actually is, whether the creep pool is gated on your towers and how, whether send order
is a real decision, and — from [§10](#10-not-yet-specified) — whether the defending side is towers at all.

**Everything is downstream of this.** The roster cannot be designed, the harness cannot be pointed at anything,
the record format cannot be fixed and the interface cannot be laid out until these rules exist. It is also the
cheapest seam to be wrong about now and the most expensive later — and it needs no server, no art and no
friends to answer.

**All three research notes have landed**, so it is ready to chart. Each note ends with two or three ranked
candidate directions and none of them decides anything — that is deliberate, and it is seam 1's to do. It
inherits three obligations they surfaced: make the attack purchase pay back under one purse, spend the
computed-balance budget knowingly, and decide whether there is a baseline wave at all.

### 2 · The submission barrier

Design the one loop and prove the unification in §2 is real: that async round-robin and the live lobby are the
same machine at different latencies, and that the record format transmits a turn as cleanly as it stores a
ghost. Includes stage matching, pool draw, and the hand-authored floor that keeps every stage populated.

If the unification is wrong, this project is building two games — and that is worth finding out before the
rules are written into a service.

### 3 · The roster

What towers and attacking units exist, how many, and what they vary by. Part V already built the structure this
fills: **one unit schema, two roles**, levers as components, the vocabulary versioned separately from the
numbers. Six of Part V's levers are already dead — the ones that depended on mazing, which the hex corridor
settled permanently.

Now also owns the **creep roster's classes and roles** from [§3](#3-what-a-match-is) — tanks, damage, support,
swarm, specialists — and the open question that comes with them: whether Part V's *one unit schema, two roles*
still holds once the attacking side has genuine internal structure, or whether roles are a third thing the
schema has to carry.

Constrained hard by [§4](#4-what-persists): nothing is unlocked, so **every unit must be interesting from the
first run** — and by [§6](#6-what-it-looks-like), because a unit whose role cannot be read off its silhouette
fails the accessibility pillar however well it plays.

### 4 · The balance harness

The tool, and the definitions underneath it. What a sweep is, what it measures, what a red cell means, what
"cost-efficient" is in a one-purse economy, and how the harness's verdict gets back into `content/` without
invalidating a pool of stored ghosts.

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
harder still. Two live battles, one economy, a build menu, and a readable account of what the opponent just did
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
| **II §3** — matching axis | Match on progression state first, rating second | **Sharpened.** The draw is per *wave*, at the matching stage — so it is the only matching axis that exists. |
| **II §5** — UGC discovery | Curation is a feature, not a backlog item | **Does not apply.** Opponents are drawn, never browsed. No discovery surface exists to get wrong. |
| **II §6** — build order | Private friend lobbies are step 7, the one synchronous mode | **Promoted and reclassified.** The lobby is not synchronous and is not last; it is the same loop at low latency. |
| **III** — networking | No realtime networking | **Stands, for a new reason.** Live PvP is in scope and still needs none — a build phase with a barrier is a turn. |
| **III** — balance as computation | A deterministic sim turns balance into a computation | **Adopted as the method.** Seam 4 is where the claim gets spent. |
| **IV** — KayKit Complete, $150 | The day-one purchase | **Reactivated.** It was paused for the free-tier walking skeleton, never overturned. |
| **IV** — can the dev rig and animate? | The KayKit-versus-Synty recommendation turns on this | **Closed by irrelevance.** KayKit ships animations; the question only mattered for Synty. |
| **V** — the unit schema | One unit, two roles; levers as components; versioned vocabulary | **Stands, and is now load-bearing.** Seam 3 fills it in. |

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
| ✅ **[Towers, or placed squads?](research/towers-versus-placed-squads.md)** — *landed* | The open question below. Verdict: the aesthetic half is free and mostly already decided by Part IV §5; the mechanical half is one number, projectile volume, and it lands on `FlyProjectiles` rather than on target acquisition |

### The open questions

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
- **How wide the damage-type matrix should be, and what the armour formula is.** Carried forward from Part V
  §4.1, still open, still cheap to set on paper. Legion TD 2 runs 1.67:1, Element TD 2 runs 4:1, Warcraft 3
  runs 40:1. Belongs to seam 3 or 4 when one of them reaches it — but the squad research has already
  constrained it from an unexpected direction: **many-small-hits is punished quadratically by flat-subtraction
  armour**, so that formula should be ruled out if squads exist at all, and the same finding argues for the
  narrow end of the matrix. A presentation choice reaching back into the damage model is exactly the kind of
  coupling seam 1 exists to catch early.
- **What a run is.** How many waves, how long a session lasts, and whether a run ends in a loss condition or
  simply ends. Touched by seam 1 but may outgrow it.

- **Whether there is a baseline wave at all — a risk no surveyed game carries.** In Legion TD 2 the wave is a
  memorised public constant, which is exactly what makes a sent mercenary legible: it is *the part that is not
  the wave*. Here the player composes the **whole** wave, so there is no constant to read it against. The
  sending research suggests a shared baseline wave per stage as cheap insurance, and flags it as needing a
  decision regardless of which direction seam 1 takes. It also bears directly on
  [§6's](#6-what-it-looks-like) accessibility pillar: a newcomer with no baseline has nothing to compare
  against.
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
- **Mazing and pathfinding.** Settled before this document: the corridor is one hex wide and never branches, so
  no unit ever chooses a path.
- **Discovery, curation and browsing surfaces.** Consequence of the per-wave draw.
- **Custom character geometry as the default.** Stock models are the pipeline; `.blend` editing is a tool kept
  for specific need, not a programme of work.
- **Moderation and community management at scale.** The pool is open, but a personal build does not take on a
  trust-and-safety function.

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
5. **Legion TD 2** — the both-boards-at-once match structure and the one-purse tension it is built on.
6. **Element TD** (Warcraft 3 mod) — the named reference for §3's combinatorial build depth. Its element
   combination system is the target class of depth, not a specification. Under research.
7. **Bloons TD 6** — the standing proof that legible-to-a-child and competitively deep are compatible, which
   §6's accessibility pillar depends on being true. Under research.
6. **Super Auto Pets / Backpack Battles** — the per-round draw against a snapshot at the same stage, and the
   AI-fill answer to an empty pool.
7. **Supercell** — "Builder Base 2: Balancing Attacking, Defending and Builders", the source of the
   defense-feels-meaningless finding this document answers differently.
8. **Slay the Spire daily** — the nothing-persists-but-rating model.
