# Async Ghost Round-Robin: A Design & Feasibility Deep Dive

**Part II of III** · Design & feasibility · 30 July 2026

> ### 📦 Archived — superseded, kept as the reading
>
> **The conclusion stands and the reason does not.** Async survives because players are never free at the same
> time, not because of a population ceiling — which is a narrower justification and a stronger one, since it is
> true at three players. Its cross-fed-currency fix, its matching axis and its build order are all replaced.
> [The Vision §9](../vision.md#9-what-this-overturns) records each. The determinism argument in §4 is the part
> that became load-bearing, and it is now built.

> **Real opponents, no queue.**
>
> Part I concluded that synchronous PvP tower defense has a hard population ceiling, and that an asynchronous
> round-robin against stored player defenses removes it. This is the examination of that claim: who has proven
> it, what it costs to build, and the six specific ways it goes wrong.

---

## Bottom line

### The model is proven, the fit is unusually good, and the hard part is determinism — not design.

Asynchronous ghost PvP is not speculative. **Backpack Battles** — two people, explicitly inspired by Super Auto
Pets — sold **640,000 copies in its first month** and peaked at **36,348 concurrent players**, roughly nineteen
times the all-time peak of the best synchronous tower defense ever made. Two years on it still holds around a
thousand concurrents, and unlike a synchronous game, that number doesn't threaten it. **Clash of Clans** has run
attack-versus-player-built-defense at planetary scale for over a decade.

Tower defense fits the model better than the auto-battlers that popularised it, for one structural reason: **a
tower layout is already a static, serialisable artefact.** An auto-battler ghost is a team snapshot that only
makes sense at one point in a run. A defense is a map you can store in a few hundred bytes and replay forever.
The genre has been sitting on the ideal ghost format the whole time.

The real risk is not "will players accept it" — Phantom Abyss and Backpack Battles settled that. It's
**simulation determinism**, and it is a day-one architectural commitment you cannot retrofit. Get that right
and everything else here is tractable design work.

---

## 1. The mechanism — what the loop actually is

Stated precisely, so the rest of this document has something to refer to. Each player submits a **defense** — a
tower layout — which enters a pool. Each player also composes an **attacking wave**. Their wave is then run
against several other players' submitted defenses, one after another. Nobody is online at the same time; nobody
waits.

| Phase | What happens |
|---|---|
| **Build** | **You lay out a defense.** On submission it is serialised — tower types, positions, upgrade states — and written to the ghost pool with your name and rating attached. It is now a piece of content other players will fight, indefinitely, whether or not you ever log in again. |
| **Compose** | **You build an attacking wave.** Units, counts, order, timing, whatever the economy allows. This is the mechanic Part I identified as your strongest idea, and it is the half of the loop that rewards reading an opponent. |
| **Resolve** | **Your wave runs against N stored defenses** — five, say — drawn from players near your rating and at the same stage. Each resolution is a deterministic simulation, so it produces an identical result on your machine and on the server. |
| **Score** | **You score on the aggregate** — how far your wave got through each, how much you broke. Meanwhile your submitted defense is being attacked by other people's waves, and holding it earns you separately. |
| **Report** | **You get replays.** Named opponents, watchable runs, "your defense held against 4 of 7 attackers this week." This is the layer that converts a solo experience into a social one — and it is not optional, as the Phantom Abyss evidence below makes clear. |

> **Why round-robin specifically, rather than 1v1 async**
>
> Facing *one* stored defense is a coin-flip against whatever the pool handed you — the exact complaint levelled
> at The Bazaar, where players build a strong board and meet a ghost that happens to hard-counter it. Facing
> **five at once and scoring on the aggregate** converts a single unlucky matchup from a loss into a dropped
> fifth of a round. Round-robin is not a flavour choice here; it is the variance control that makes async ghost
> matching fair enough to rank people on.

---

## 2. Precedent — who has already proven this works

| Figure | What it is | Source |
|---|---|---|
| **640k** | Copies Backpack Battles sold in its first month — async PvP, two-person team | Game World Observer |
| **36,348** | Its peak concurrent players. Legion TD 2's all-time peak is 1,950 | Steam trackers |
| **~$9.5M** | Backpack Battles estimated gross *(est)* — ~$2.8M net to the developers | Steam Revenue Calculator |
| **38** | Weekly demo patches shipped before launch, from June 2023 — how they built the audience | GameDiscoverCo |

### Peak concurrent players: asynchronous vs. synchronous PvP

All-time Steam peaks. The comparison is between competitive multiplayer games of similar team size and budget.

| Title | Model | Peak concurrents |
|---|---|---:|
| **Backpack Battles** — 2 devs, EA Mar 2024 | Asynchronous ghost PvP | 36,348 |
| **Legion TD 2** — 4 devs, EA 2017 | Synchronous PvP tower defense | 1,950 |
| **Bloons TD Battles 2** — Ninja Kiwi, 2021 | Synchronous PvP tower defense | 1,937 |
| **Element TD 2** — 2021 | Synchronous PvP tower defense | 783 |

*Sources: Steam trackers (Raijin, Steambase, SteamCharts), July 2026. This is not a like-for-like genre
comparison — Backpack Battles is an inventory auto-battler, not a tower defense. The point being made is
narrower: removing the queue removes the ceiling on how many people can be playing your competitive game at
once, because none of them need to find each other.*

### The precedents in detail

| Precedent | What it proves | Scale reached | The lesson to steal |
|---|---|---|---|
| **Backpack Battles** (PlayWithFurcifer, 2024) | Async ghost PvP sells, at indie scale, from a two-person team | 640k units, month one | Opponents are real players' builds, matched by "similar ranking, same point in the game" — the dev's own description. Not AI. |
| **Super Auto Pets** (Team Wood Games) | The template. Timer-free Arena mode against ghost teams | 1M+ on Google Play alone | AI-generated teams fill in when no real player sits at your turn. That's the cold-start answer, shipped. |
| **Clash of Clans** (Supercell) | Attacking player-built defenses works at planetary scale, for a decade | Billions lifetime revenue | Defense replays are the retention hook: you watch how you were beaten and re-lay your base. |
| **The Bazaar** (Tempo, 2024–25) | The design cuts dead time — and shows exactly how it frustrates | — | Cautionary. Ghost hard-counters feel unjust; the F2P monetisation soured an otherwise clever system. |
| **Phantom Abyss** (Team WIBY / Devolver) | Ghosts of absent players create genuine felt presence | — | "You can't interact with the ghosts, but they make you feel like you're playing against other players." |
| **Slay the Spire daily** (Mega Crit) | A shared seed turns solo play into ranked competition | — | Same seed worldwide, 24-hour window, one life, score leaderboard. Zero networking required. |

### The number that matters most

Backpack Battles today sits at roughly **1,000–2,000 concurrent players** — very close to Legion TD 2's 830.
The difference is what that number *means*. At 1,000 concurrents Backpack Battles works exactly as designed,
because it never needed two people online simultaneously. At 29 concurrents, Element TD 2's multiplayer is
unusable. **Async doesn't raise your player count; it severs your player count from whether the game
functions.** That is the entire argument, and it is worth more than any launch spike.

---

## 3. Failure modes — the six ways this goes wrong

Each of these has been observed in a shipped game. Each has a known fix. Design them in from the start — five
of the six are cheap at the beginning and expensive later.

### ⚠ Defense feels meaningless
*Observed in Clash of Clans*

The structural hazard of every attack-versus-stored-defense game: attacking is active and skilful, defending is
something that happens to you while you're asleep. Supercell hit this hard enough in Clash of Clans' Builder
Base that they rebuilt the mode around it, stating plainly that the old system "made defending feel unrewarding
compared to attacking."

> **The fix Supercell shipped** — Cross-feed the rewards. Attacking well earns the currency that upgrades your
> *defense*; defending well earns the currency that upgrades your *offense*. You are paid for stars your
> opponent *fails* to take. Both halves become load-bearing, and neither can be ignored. Copy this directly —
> it maps onto your two-resource TD economy almost without translation.

### ⚠ Ghost hard-counters feel unjust
*Observed in The Bazaar*

Players build the strongest thing they can, get matched into a stored board that happens to invalidate it, and
lose to a person who isn't there and never made a decision about them. Synchronous PvP has the same counters
but they feel earned, because a human chose them. Async counters feel like weather.

> **The fix** — Round-robin against N opponents, scored in aggregate — one bad draw is a dent, not a defeat.
> Then cap the swing: score partial progress through a defense rather than binary win/loss, so a countered wave
> still registers what it did achieve.

### ⚠ Matching on the wrong axis
*Observed in Super Auto Pets*

Super Auto Pets' most persistent complaint is being matched by *win count* rather than by *round* — so a player
on turn three with starting resources meets a snapshot from someone who has been accumulating gold for six
turns. The mismatch isn't in skill, it's in economy, and it reads as the game cheating.

> **The fix** — Match on progression state first, rating second. Backpack Battles' dev states the rule as
> "similar ranking *and* at the same point in the game" — both conditions. For a TD, "same point" means same
> wave number and comparable accumulated economy. Store the economy state alongside the layout so you can
> filter on it.

### ◐ An empty ghost pool on day one
*Cold start*

On launch morning there are no stored defenses, and at 04:00 in a small player base there may be none at your
bracket. A pool-driven game that can't fill the pool is just a broken menu.

> **The fix** — Super Auto Pets' shipped answer: AI-generated teams fill in when no real player occupies your
> turn. Better for you — author a set of hand-designed defenses as the launch pool and as the permanent floor.
> These double as your single-player content, which you need anyway. A hand-built defense is indistinguishable
> from a stored one at the format level, so this costs you nothing architecturally.

### ◐ The pool goes stale and the meta lags
*Slow decay*

Ghosts are frozen at submission. After a balance patch, a pool full of pre-patch defenses is a museum of a game
that no longer exists — and rating people against it is meaningless. Elo has a related weakness: inactive
players keep inflated ratings they are no longer defending.

> **The fix** — Expire ghosts on a rolling window (submissions from the last N days), re-validate the pool on
> every balance patch, and use Glicko-style rating with a deviation term that widens with inactivity rather
> than plain Elo. Weekly seeded gauntlets, on the Slay the Spire daily model, give you a clean fixed-pool
> leaderboard immune to this entirely.

### ⚠ "That's not really multiplayer"
*Perception*

The commercial risk, not the design one. If it reads as single-player with a leaderboard, you lose the social
pull that justified building any of it. Phantom Abyss is the counter-evidence: players describe its ghosts as
making them "feel like you're actively playing against other players," and its designer framed the appeal
precisely — "it's really cool to have real people around doing stuff, but none of them are judging."

> **The fix** — Presence is made of specifics. Name the opponent. Show their defense as *theirs*, with whatever
> they named it. Send the notification that their wave broke your line. Ship replays you can watch and share.
> The mechanism is asynchronous; the *presentation* must be relentlessly personal.

---

## 4. Engineering — determinism is the whole build risk

Everything above assumes that a stored defense plus a stored wave produces the same outcome every time, on
every machine. If it doesn't, your replays desync, your leaderboard is unverifiable, and your anti-cheat has
nothing to check against. This is the one part of the plan that is genuinely hard, and it constrains your
engine from the first commit.

The classic reference on this — Glenn Fiedler's *Floating Point Determinism* — is a catalogue of how badly
naive floating point behaves across machines:

- **Same compiler, same architecture is the baseline requirement**, not a nice-to-have. Cross-vendor float
  results diverge.
- **SSE/SSE2 are "too under-specified to be deterministic"** for this purpose; transcendental functions (`sin`,
  `cos`, `tan`) differ between AMD and Intel silicon.
- **Debug and release builds diverge.** The MotoGP developers found replays recorded in debug wouldn't play
  back in release.
- **Patching breaks stored replays.** A newer compiler on identical source can produce a binary that no longer
  reproduces old recordings — fatal for a persistent ghost pool.
- Gas Powered Games' shipped mitigation on Supreme Commander was explicit FPU control at startup —
  `_controlfp(_PC_24, _MCW_PC)` and `_controlfp(_RC_NEAR, _MCW_RC)` — plus asserting the mode hadn't been
  silently changed by a Windows API.

That is a workable path for a single-platform, single-binary game. It is a bad path for a game whose entire
premise is **replaying strangers' recordings across platforms and across patches, for years.**

> **The recommendation**
>
> **Run the gameplay simulation in fixed-point integer math, isolated from rendering, from day one.** Floats
> stay in the renderer where nobody verifies them; the simulation touches only integers. This is the standard
> answer for lockstep RTS engines, and it is dramatically cheaper to adopt at the start than to retrofit —
> retrofitting means rewriting every gameplay system you have.
>
> Version the simulation. Every stored ghost records the sim version that produced it; ghosts from an
> incompatible version are retired rather than silently replayed wrong. You will patch balance, and you need
> that to be a pool migration rather than a correctness bug.

### What a ghost record actually contains

The storage story is trivially cheap, which is the other reason tower defense suits this better than most
genres. You are not storing video or per-frame state — you store the inputs and re-derive everything:

```
GhostRecord
  sim_version     u32        // retire on mismatch
  seed            u64        // any RNG the sim uses
  author          PlayerId + display name
  rating          i32        // for bracket matching
  stage           u16        // wave number / economy tier — match on this FIRST
  submitted_at    timestamp  // for rolling expiry
  layout          []Tower    // ~16 bytes each: kind, cell, upgrades
  economy         EconState  // gold/income at submission

AttackRecord
  wave            []UnitOrder // kind, count, lane, timing
  → resolved against a GhostRecord, deterministically, anywhere
```

A defense of fifty towers is well under a kilobyte. A hundred thousand stored defenses is under a hundred
megabytes — a rounding error on any host. Replays are the same two records plus a version tag, so "watch how
they beat you" is a link, not a video file.

### Anti-cheat falls out of determinism for free

Because results are reproducible, the client's reported outcome is a *claim*, not a fact. The server re-runs
the same two records and compares. This is exactly the property the anti-cheat literature recommends chasing —
cheaters must ultimately manifest their advantage in server-observable actions, and a deterministic sim makes
every claimed result checkable at negligible cost. Re-simulate everything that touches the ladder; sample the
rest.

---

## 5. Second-order — your creep waves are user-generated content

Worth naming explicitly, because it changes what the game is. Once players compose attacking waves and those
waves are stored and replayable, you have a UGC game — and UGC games live or die on discovery, not on creation.

Super Mario Maker 2 sold over eight million copies and hosts millions of levels, and its most consistent
criticism is that in-game discovery and curation are poor enough that players rely on outside communities to
find the good ones. That is the failure mode to design against: a system where the best player-made content is
invisible without a Discord.

- **Curation is a feature, not a backlog item.** Surface waves and defenses by what happened to them — "held
  against 12 attackers", "broken by 3 of 40" — rather than by upload date.
- **Make the leaderboard the discovery surface.** Beating a named defense is more motivating than browsing a
  list, and it needs no ratings UI.
- **Weekly seeded gauntlets solve curation by fiat.** A fixed set of opponents each week, identical for
  everyone, is both a fair leaderboard and an editorial choice. Slay the Spire's daily proves the format
  retains people for years.

---

## 6. Build order — how to de-risk this, in order

Sequenced so that the thing most likely to kill the project is tested first, and so that you have a sellable
game before you have a server.

1. **Determinism harness before gameplay.** A fixed-point simulation core and a test that runs the same layout
   plus wave ten thousand times, across debug and release builds and across platforms, asserting bit-identical
   results. If this is painful, you want to know in week two, not year two. Everything else depends on it.
2. **The ghost record format, immediately after.** Serialise a defense and a wave to bytes, replay from bytes.
   Once this works, a hand-authored defense and a stranger's defense are the same object, and every later
   feature is a query over a pool.
3. **The solo game, against authored ghosts.** Play the whole loop against defenses you designed yourself. This
   is the real test of the design question — *is composing a wave against a fixed layout fun?* — and it needs
   no networking, no accounts and no players. If it isn't fun here, no amount of multiplayer will save it, and
   you've spent nothing.
4. **Ship that as the game.** A complete roguelite TD with authored gauntlets is a product. Backpack Battles
   ran a public demo from June 2023 with 38 weekly content patches before Early Access — that cadence, on a
   solo-complete build, is how you arrive at launch with an audience rather than hoping for one.
5. **Turn on submission.** Real defenses enter the pool alongside the authored ones. Nothing about the client
   changes; the pool just gets bigger and more varied. Cross-feed the attack and defense rewards from the start
   so both halves matter, per the Supercell lesson.
6. **Weekly seeded gauntlet, then rating.** The gauntlet is cheap, fair, and immune to pool staleness — ship it
   first. Add Glicko-style persistent rating only once the pool is dense enough that bracket matching returns
   sensible opponents.
7. **Private friend lobbies, live.** The one synchronous mode worth building, because friends bring their own
   opponents and it can never queue-die. Co-op and head-to-head both belong here, and both can reuse the
   deterministic sim.

> **The one thing that would invalidate this**
>
> Step 3 is the real gate. If composing a wave against a *fixed, non-reacting* defense turns out to be flat — if
> the fun was always in watching a human adapt — then async round-robin is the wrong frame and you'd be back to
> synchronous PvP and its 830-player ceiling. That question is answerable with a prototype and no server, which
> is exactly why it should be answered first.

---

## Sources

1. Game World Observer / WN Hub — Backpack Battles sales milestones (100k in two days, 500k in two weeks, 640k
   in month one; China 48%, Japan 11%, US 10%); Game Developer on the two-day figure.
2. GameDiscoverCo — "How Backpack Battles sold 650k copies": demo from June 2023 with 38 weekly patches, bundle
   cross-promotion (~35% of sales), localisation strategy, and the developers' stated inspiration ("the world
   needs more asynchronous auto battlers").
3. Backpack Battles Steam discussions — developer description of matchmaking: opponents are snapshots picked by
   "similar ranking… at the same point in the game". Wikipedia for EA and 1.0 dates (8 Mar 2024; 13 Jun 2025).
4. Steam trackers (Raijin, Steambase, SteamCharts) — peak and current concurrents for Backpack Battles, Legion
   TD 2, Bloons TD Battles 2, Element TD 2, July 2026. Steam Revenue Calculator for the gross/net estimate.
5. Super Auto Pets — Steam/Google Play listings and Grokipedia on Arena mode's timer-free asynchronous battles
   and AI-team fallback; Steam discussion thread "Matchmaking is this game's biggest problem" for the
   win-count-versus-round complaint.
6. Supercell — "Builder Base 2: Balancing Attacking, Defending and Builders", on defense feeling unrewarding
   and the cross-fed reward structure (Builder Gold from attacking, Builder Elixir from defending).
7. The Bazaar — Steam reviews and coverage on ghost-board hard counters, UI friction, and the open-beta
   monetisation backlash.
8. Phantom Abyss — Checkpoint Gaming, GamesHub and Unreal Engine developer interviews with Team WIBY; designer
   Ben Marrinan on ghost presence.
9. Slay the Spire daily run documentation and Slay the Spire 2 daily climb guides — shared worldwide seed,
   24-hour window, permadeath, score leaderboard.
10. Glenn Fiedler (Gaffer On Games) — "Floating Point Determinism" and "Deterministic Lockstep":
    compiler/architecture requirements, SSE and transcendental hazards, debug-vs-release divergence, and Gas
    Powered Games' `_controlfp` mitigation on Supreme Commander.
11. Anti-cheat literature (arXiv surveys on server-side detection) — server-authoritative validation and the
    observation that cheats must manifest in server-observable actions.
12. Elo/Glicko rating literature — Elo's inactivity weakness and Glicko's rating-deviation correction.
13. Critpoints, Inverse and ACM's UGC survey — Super Mario Maker discovery and curation shortcomings; 8M+
    copies of Super Mario Maker 2.
