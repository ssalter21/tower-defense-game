# Tower Defense: Market Report & Viability Read

**Part I of III** · Market research · 30 July 2026
Viability read for a multiplayer TD

> ### 📦 Archived — superseded, kept as the reading
>
> **This is the most fully overturned of the five deep dives: the game is not a commercial product, so the
> question this document exists to answer is no longer being asked.**
> [The Vision §1](../vision.md#1-the-destination) settled that, and
> [the archive index](README.md#what-the-vision-overturns) records what survived — the 830-player synchronous ceiling is still
> the number behind the async model, though [§1](../vision.md#1-the-destination) now justifies async by
> schedule mismatch instead. Read for the market evidence, not for the plan.

> **The genre is thriving. Multiplayer TD isn't.**
>
> Tower defense is one of the healthiest indie categories on Steam right now — and the head-to-head,
> send-your-own-creeps corner of it has a hard population ceiling that eight years of excellent execution has
> not broken. This report separates the two, and says what to build because of it.

---

## Bottom line

### Viable — but not as a multiplayer-first game.

The demand is real and the genre is growing, so a tower defense game is a sound bet in 2026. The specific plan
is not, in the shape described. Every PvP tower defense ever shipped, including the genuinely excellent one,
tops out in the hundreds of concurrent players. **Legion TD 2** — which already implements the exact "build
your own creep wave and send it at another player's defense" loop, extremely well, after nine years of
development — averages roughly **830 concurrent players** and 10,000 unique dailies. That is the ceiling for a
*successful* PvP TD, not the floor.

Meanwhile the solo and hybrid TD games are selling in the hundreds of thousands of copies with a fraction of
the engineering: **Thronefall** (two people) passed 1M copies, **The King is Watching** cleared 500k. The
winning move is a game that is complete and excellent for one player, with the round-robin PvP layered on
*asynchronously* — so it never needs a live opponent to be fun — and private friend lobbies for the social
hook. That version keeps everything interesting about your idea and removes the thing that kills games like it.

---

## 1. Market — where the money actually sits

Tower defense money is wildly unevenly distributed across platforms, and the distribution matters more than the
totals. Roughly: mobile is where the revenue is, Roblox is where the players are, and Steam is where a small
team can actually make a living without a live-ops department.

| Figure | What it is | Source |
|---|---|---|
| **$230M** | Rush Royale lifetime revenue in ~3 years — the top-grossing TD game, and a PvP one | Sensor Tower via GameDevReports |
| **$141.8M** | MTG's upfront purchase of Ninja Kiwi (Bloons), 2021 — NZD 203M all-in | Game Developer · MTG |
| **340k** | Peak concurrent players, Anime Vanguards — a Roblox TD, now its top-earning game | Beebom · Roblox |
| **3,399** | Tower-defense-tagged games on Steam. 187 of them are meaningful earners | Steambase · Steam Marketing Tool |

### The three markets, honestly labelled

**Mobile** is the biggest pot and the worst fit for a small premium team. Market-research houses put the mobile
TD segment around **$4.75B in 2025** growing ~9% CAGR — treat that number as directional only; syndicated
market reports of this kind are notoriously loose. The trustworthy datapoint is Rush Royale: **$230M lifetime,
63M downloads, 6M MAU, 1.14M DAU**. It is free-to-play, gacha-flavoured, and needs constant live ops. That is a
publisher's game, not a two-person game.

**Roblox** is where tower defense is culturally dominant right now. Anime Vanguards launched September 2024 and
passed **1.9 billion visits** with a 340k concurrent peak, overtaking Blox Fruits as the platform's top earner.
Tower Defense Simulator, All Star Tower Defense and Toilet Tower Defense all run enormous audiences. The lesson
is not "ship on Roblox" — it is that *co-op TD with collectible units is the single most engaging configuration
of this genre for a mass audience*, and it is currently being served almost entirely by Roblox.

**Steam** is the addressable market for what you're describing. Bloons TD 6 is the anchor tenant with an
estimated **$73.7M gross** and 391,651 reviews. Below it sits a healthy mid-tier where a good indie TD earns
$1–10M. Steam ran a dedicated **Tower Defense Fest** in March 2026 (9–16 March, 1,500+ titles discounted) —
Valve only builds festivals around categories with proven conversion.

> **On the numbers in this report**
>
> Review counts, player counts and store metadata are observed values from Steam and its trackers. Revenue and
> unit figures marked *(est)* are third-party estimates (Raijin, Gamalytic, VG Insights, SteamSpy) that
> routinely disagree by 2–3× on the same title — Legion TD 2's gross is variously estimated at $4.6M, $9.4M and
> $10.9M. Use them for order of magnitude, never for a business plan.

---

## 2. Comparables — the Steam scoreboard

Review count is the most honest cross-title comparison available — one source, one definition, no estimator
disagreement. As a rule of thumb, Steam owners run roughly 30–50× reviews. The pattern below is the whole
report in one picture: **the solo games out-sell the multiplayer games, and the co-op games sit between them.**

### Steam review counts, selected tower defense titles

Bloons TD 6 sits far off this scale at 391,651 reviews.

| Title | Shape | Reviews | Rating |
|---|---|---:|---|
| **Bloons TD Battles 2** — Ninja Kiwi, 2021 | Head-to-head PvP | 33,995 | 84% |
| **Thronefall** — Grizzly Games, 2023 | Solo / hybrid | ~19,000 | Overwhelmingly Positive |
| **Legion TD 2** — AutoAttack Games, 2021 | Head-to-head PvP | 12,535 | 87% |
| **Mechabellum** — Game River, 2024 | Head-to-head PvP | 9,697 | 84% |
| **Orcs Must Die! Deathtrap** — Robot Ent., 2025 | Co-op | 3,951 | 70% |
| **Element TD 2** — Karawasa, 2021 | Co-op | 3,166 | Very Positive |

*Source: Steam store pages and Steam review trackers, July 2026. Percentages are lifetime positive-review share
where Steam publishes one; otherwise Steam's own banding. Thronefall's count is approximate ("nearly 19,000"
per Grizzly Games' reporting). Legion TD 2 uses its store page's all-language total; third-party trackers
report up to 15,180 for the same title, which is the kind of disagreement to expect.*

### Full comparables table

| Title | Shape | Reviews | Units / revenue | Note |
|---|---|---:|---:|---|
| **Bloons TD 6** ($13.99, 2018) | Solo + 2–4p co-op | 391,651 (97%) | ~$73.7M *(est)*; 2–5M owners | Genre anchor. Franchise sold for NZD 203M. |
| **Thronefall** ($12.99, EA Aug 2023) | Solo, minimalist | ~19,000 (Overwhelm. Pos.) | 1M+ copies; $1.5M in 2 months | Two-person team. The efficiency benchmark. |
| **The King is Watching** ($14.99, Jul 2025) | Solo roguelite hybrid | — | 500k+ copies; $1.7M in week 1 | 139k units in first week. DICE finalist. |
| **Legion TD 2** ($24.99, 1.0 Oct 2021) | 2v2 / 4v4 PvP | 12,535 (87%; trackers say 15.2k) | $4.6–10.9M *(est)* | 4-person studio, 9 yrs. Cosmetic-only MTX. |
| **Mechabellum** ($19.99, 1.0 2024) | 1v1 / 2v2 / FFA auto-battler | 9,697 (84%) | 435k–553k *(est)*; $3.9–6M | Paradox + Dreamhaven published. |
| **Bloons TD Battles 2** (F2P, 2021) | 1v1 PvP | 33,995 (84%) | — | Best-resourced PvP TD. Still declining. |
| **Orcs Must Die! Deathtrap** ($29.99, Jan 2025) | 1–4p co-op action TD | 3,951 (70%) | 200–500k owners *(est)* | Established IP, weakest reception here. |
| **Element TD 2** ($14.99, 2021) | Solo + 8p co-op + PvP | 3,166 (Very Positive) | — | From a mod with 5M+ downloads. Loved, empty. |

---

## 3. The core risk — the liquidity problem, in numbers

A multiplayer game needs a live population to function. Below a threshold, queue times grow, players leave
because they can't find matches, and the population shrinks further — **queue death**. It is the single most
reliable killer of small multiplayer games, and tower defense is unusually exposed because its matches are long
and its audience is small.

Here is what the multiplayer TD population actually looks like. Note that these are the *survivors* — the games
that shipped, reviewed well, and are still running.

### All-time peak vs. current concurrent players

Steam concurrents, July 2026. Bloons TD 6 is excluded from the scale — it peaked at 53,818 and still averages
~9,027.

| Title | All-time peak | Current / recent average | Change |
|---|---:|---:|---:|
| **Orcs Must Die! Deathtrap** — co-op, Jan 2025 | 4,915 (1 Feb 2025) | ~326 | −93% |
| **Legion TD 2** — PvP, 2017–now | 1,950 | ~830 avg · 10k unique daily | — |
| **Bloons TD Battles 2** — PvP, F2P | 1,937 (21 Oct 2023) | ~310 | −84% |
| **Element TD 2** — co-op + PvP | 783 (3 May 2026) | 29 | −96% |

*Sources: Steambase, Raijin, SteamCharts, and Legion TD 2's own published community figures (830 average
concurrents, 10,000 unique daily players, 50,000 unique monthly). Bloons TD Battles 2's current figure is
derived from its published −84%-from-peak position.*

### Findings

**⚠ Caution — The best PvP TD ever made supports ~830 players at a time.**
Legion TD 2 is not a failure — it is the success case. Nine years of development, a 4-person studio, 87%
positive across 12,535 reviews, an active ranked ladder with eleven tiers, community tournaments with real
prize pools. And its steady-state is 830 concurrent, 10k daily, 50k monthly. If you build multiplayer-first,
that is your realistic ceiling, and you reach it only by being *as good as Legion TD 2*.

**⚠ Caution — Multiplayer-only TD has a graveyard.**
Tower Wars (2012) is the direct precedent for your pitch: competitive TD where you build towers and send units
at the other player. It launched to reasonable reviews, then died of thin content — three maps, effectively no
single-player, and difficulty finding opponents. Steam threads from 2025 are people complaining the multiplayer
no longer functions while the game is still on sale. The concept was right in 2012; the delivery vehicle was
fatal.

**◐ Mixed — Even great co-op TD decays fast.**
Orcs Must Die! Deathtrap had an established IP, a publisher, and four-player co-op — and fell 93% from peak
within eighteen months, on 70% reviews. Co-op is more resilient than PvP because two friends are their own
lobby, but it does not exempt you from retention economics.

**✓ Strong signal — Free giveaways are the standard liquidity patch, and they're a tax.**
Legion TD 2 gave the full game away on the Epic Games Store for a week in July 2025, and runs recurring Steam
free weekends. That is what sustaining a PvP population looks like in practice: periodically donating your
product to refill the queue. Budget for it as an ongoing cost, not a launch tactic.

---

## 4. Your concept — feature-by-feature against what already exists

You described four things: friend lobbies, co-op, player-built creep waves, and round-robin of your army
against other players' tower setups. Three of the four are shipped and mature. The fourth is the interesting
one.

| Your feature | Closest existing implementation | Status | What it means for you |
|---|---|---|---|
| **You build the creep wave sent at an opponent** | Legion TD 2 — spend "mythium" on mercenaries that spawn in an opponent's lane next wave; leaking gives your opponents bonus gold | **Solved** | Not a novel hook. It is a well-tuned, deeply understood system with a 15k-review community and published counter-charts. Do not pitch this as the differentiator. |
| **Lobbies for friends** | Legion TD 2 parties of 1–8; Bloons TD 6 private co-op matches; Element TD 2 8-player co-op | **Solved** | Table stakes, not a feature. **But**: private lobbies are the one multiplayer mode immune to queue death, because the players bring their own opponents. Lean on this. |
| **Co-op** | Bloons TD 6 (2–4p, added 2019, shared economy + own heroes); the entire Roblox TD category | **Crowded** | Proven to be the mass-market configuration — but the biggest audience for it currently plays it free on Roblox. Premium co-op TD on Steam works — Element TD 2 is Very Positive — but at modest scale. |
| **Round-robin: your army vs. several players' defenses** | Mechabellum and Super Auto Pets use round-robin auto-battler structure; no tower defense does it | **Genuine gap** | This is your idea. It is also the piece that solves your liquidity problem. |

> **The insight worth building on**
>
> Round-robin is not just a format — it's an **asynchronicity licence**. Super Auto Pets built a hit by matching
> players against *snapshots* of other players' teams: "ghosts" harvested from real players, with AI teams as
> fallback when nobody's at your turn. Nobody waits, nobody rage-quits mid-match, and the game is equally good
> with 50 players online or 50,000.
>
> A tower defense is a **perfect** fit for this, better than an auto-battler: a tower layout is a static, cheap,
> deterministic artefact. You can store thousands of them. So "round-robin your wave against other players'
> setups" can be entirely asynchronous — you are attacking real players' real defenses, submitted earlier,
> replayed deterministically. It *feels* like multiplayer, it produces a real ladder, and it has **zero
> matchmaking liquidity requirement.** That single design decision converts your biggest risk into a non-issue.

---

## 5. Reception — what reviews reward and punish

### What earns the high scores

- **Legible depth.** The 90%+ titles (Bloons TD 6 at 97%, Thronefall at 96%) are ones where a new player
  understands the system in five minutes and is still finding new lines at fifty hours.
- **Content volume as a moat.** Bloons TD 6's score is inseparable from eight years of free major updates.
  Reviewers explicitly cite the update cadence.
- **Hybridisation.** The 2025–26 breakouts are all TD crossed with something — roguelite runs (Emberward),
  kingdom-builder (The King is Watching, Thronefall), factory (Mindustry), base-survival (Age of Darkness, The
  Riftbreaker at 500k+ first-year copies). Pure lane defense is a solved, saturated space.
- **Respectful monetisation.** Legion TD 2 explicitly scrapped its microtransaction plan — "no pay to win,
  in-game purchases are 100% cosmetic" — and says it did so to spend its time on balance and content instead of
  deciding which skin to sell. Its reviews reflect that.

### What drags scores down

- **Repetition.** The genre's structural weakness. TD has minimal narrative and a fixed loop; without real
  variety in creeps, towers and objectives it reads as shallow fast.
- **Passivity.** A recurring criticism is that the player watches rather than plays. The successful modern
  answers give you something to do during the wave — a hero to steer, a path to re-route, a wave to compose.
- **Meta collapse.** Players turn on games where late-game converges on one dominant tower, and where the
  required units sit behind grind. This is *much* worse in PvP, where a stale meta is not a preference but a
  wall.
- **Late-game performance.** Bloons TD Battles 2's most-cited complaint is 10 FPS and input delay past round
  40. Thousands of entities on screen is a real engineering constraint in this genre, and PvP makes it a
  fairness issue too.
- **Thin content at launch.** Tower Wars died on three maps. Orcs Must Die! Deathtrap's 70% is largely a
  content-and-value complaint at $29.99.

---

## 6. Opportunity — real gaps in the 2026 landscape

**✓ Gap — Asynchronous competitive tower defense does not exist.**
Every competitive TD on the market is synchronous and therefore population-gated. Nobody has applied the Super
Auto Pets ghost model to tower defense, despite tower layouts being the most snapshot-friendly artefact in all
of strategy gaming. This is the clearest open lane in the genre.

**✓ Gap — Premium co-op TD with collectible depth, off Roblox.**
The Roblox anime-TD category proves enormous demand for co-op TD with unit collection and progression —
hundreds of thousands of concurrent players. On Steam that audience is served by Bloons TD 6 and almost nothing
else. A premium, non-predatory, well-crafted version of that loop for players who have aged out of Roblox is an
underserved position.

**◐ Gap, with a catch — The Warcraft 3 mod diaspora is still homeless.**
Legion TD, Wintermaul Wars and Element TD collectively had millions of players as mods. Element TD 2 alone
descends from a mod with 5M+ downloads. That audience exists and is nostalgic — but Legion TD 2 already owns
it, and its size is precisely the 830-concurrent number above. Treat it as a launch audience, not a market.

**◐ Gap — Deterministic, spectatable, short-session competitive TD.**
TD matches are long, which suppresses ladder play and streaming. Legion TD 2 runs real tournaments on a
$300–1,000 prize scale — the ceiling of a genre nobody can watch casually. A competitive TD built around
5-minute rounds and replay sharing has no incumbent.

---

## 7. Risks — pitfalls specific to this plan

- **Queue death is the default outcome, not the failure case.** Plan the game so that zero players online is
  still a complete experience. If any core mode requires a live stranger, that mode will be dead within a year
  of launch.
- **PvP balance is a permanent salaried job.** A solo dev can ship a balanced single-player TD once. A PvP TD
  needs re-balancing forever, informed by data you have to build tooling to collect. Legion TD 2 has four
  people doing this after nine years, and still patches constantly.
- **Two games' worth of content, one game's price.** A game with co-op, PvP and friend lobbies needs a solo
  campaign too (or it's Tower Wars). That is a lot of scope for a small team, and Deathtrap shows what the
  reviews do when the content-to-price ratio slips.
- **Netcode and simulation determinism.** Round-robin against stored setups only works if the simulation is
  deterministic — same layout, same wave, same result, on every machine. That constrains your engine choices
  from day one. It is much cheaper to design for than to retrofit.
- **Late-game entity counts.** The genre's most common technical complaint. Plan your entity budget and
  rendering before your first content pass.
- **Free-to-play is a trap at your scale.** Rush Royale's $230M comes with a live-ops org. Legion TD 2's
  premium-plus-cosmetics model is the one that works for four people. Price it as a premium game.
- **Wishlist conversion is unforgiving.** Median first-month wishlist-to-sale conversion is around 0.15. A
  20,000-wishlist launch is roughly 3,000 first-month units. Budget marketing accordingly, and note that TD's
  Steam festival gives you a reliable annual visibility beat in March.

---

## 8. Verdict and the shape to build

| Question | Answer | Reasoning |
|---|---|---|
| Is tower defense a good genre to enter in 2026? | **Yes** | Growing, festival-supported, and repeatedly producing 200k–1M-copy indie hits from tiny teams. Hybrids specifically. |
| Is "you build the creep wave" a novel hook? | **No** | Legion TD 2 has shipped it, refined it for nine years, and owns the audience that wants it. |
| Is round-robin vs. other players' defenses novel? | **Yes** | No tower defense does it. Mechabellum and Super Auto Pets prove the format sells; nobody has crossed it with TD. |
| Can a small team sustain synchronous PvP TD? | **Very hard** | The best-case outcome is ~830 concurrent players, reached after nine years, sustained by giving the game away periodically. |
| Does the plan work if PvP is asynchronous? | **Yes** | Ghost-replay round-robin keeps the competition and deletes the liquidity requirement. This is the version to build. |

### The shape I'd recommend

1. **A complete solo tower defense first.** Roguelite run structure, 2–5 hours to a satisfying arc, fun with
   the servers off forever. This is the product you actually sell, and the one reviewers score.
2. **Wave-building as the solo mechanic too.** Composing the attacking wave is your best idea — don't restrict
   it to PvP. Let solo players design waves against AI or authored gauntlets, so the system carries the whole
   game rather than one mode.
3. **Asynchronous round-robin as the ladder.** Submit your defense; your wave runs against five other players'
   submitted defenses; results deterministic and replayable. Weekly seeded gauntlets. No queue, no wait, works
   at any population.
4. **Private friend lobbies, synchronous.** The one live mode that never suffers queue death, because friends
   bring their own opponents. Co-op and head-to-head both belong here.
5. **Public matchmaking last, if the numbers justify it.** Ship it only once you can see from your own
   telemetry that concurrent population supports it. It is the mode most likely to be a maintenance liability.
6. **Premium price, cosmetic-only extras.** $15–25. Follow Legion TD 2's stated reasoning: it buys back the
   time you'd otherwise spend on monetisation design.

> **What would change this answer**
>
> If you can demonstrate a synchronous match that is genuinely great with a stranger *and* the game holds 500+
> concurrents in a closed beta, the multiplayer-first version becomes defensible. Short of that evidence, treat
> live PvP as the thing you earn the right to build, not the thing you build first.

---

## Sources

1. MTG — press releases on the Ninja Kiwi acquisition (SEK 1,216M / NZD 203M upfront); Game Developer, "MTG
   acquires *Bloons* dev Ninja Kiwi for $141.8 million".
2. GameDevReports / Sensor Tower — "Rush Royale reached $230M in revenue" (63M downloads, 6M MAU, 1.14M DAU,
   76k peak CCU).
3. Steam store pages and community stats for Bloons TD 6, Legion TD 2, Bloons TD Battles 2, Element TD 2,
   Mechabellum, Orcs Must Die! Deathtrap, Thronefall, The King is Watching.
4. Raijin, Steambase, SteamCharts, SteamSpy, Gamalytic, Steam Marketing Tool — player counts and revenue
   estimates (July 2026).
5. Legion TD 2 official site and wiki — matchmaking, mercenary/mythium systems, ranked tiers, and published
   community figures (830 avg concurrents, 10k daily, 50k monthly); Esports portal for tournament prize pools.
6. GameDiscoverCo — wishlist-to-sales conversion benchmarks (median ~0.15 first month, Sept 2024–Sept 2025);
   "How *The King Is Watching* sold >200k in a coupla weeks"; Riftbreaker wishlist case study.
7. Grizzly Games / Berlin.de / WN Hub — Thronefall 1M copies, $1.5M in first two months.
8. SNS Insider and comparable syndicated reports — mobile tower defense market sizing ($4.75B in 2025, ~9.4%
   CAGR). Directional only.
9. Beebom, Dexerto, Roblox — Anime Vanguards visits, concurrents, and top-earning status.
10. Steam Tower Defense Fest 2026 (9–16 March) coverage — GameGrin, Rogueliker, DLCompare.
11. Metacritic and Steam community threads for Tower Wars (2012) — reception and current multiplayer state.
12. Steam / Wikipedia / Grokipedia — Super Auto Pets asynchronous "ghost team" matchmaking design.
13. Stardock dev journal, "What Makes A Good Tower Defense Game?" — genre design critique on repetition and
    passivity.
