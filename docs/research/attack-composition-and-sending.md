# The Attacking Half: How Sending Is Made Deep, and Whether a Defense May Gate It

**Research note** · 3 August 2026

> ⚠️ **Two premises moved on 6 August 2026. The survey stands; two of its verdicts are now weaker.**
>
> - **The corridor is gone.** This note's strongest positive finding — that send *ordering* is rescued by a
>   one-hex corridor, because a corridor that never branches already *is* a single-file column — lost its
>   support when [the board became a maze again](../vision.md#the-board-is-a-maze).
>   Ordering is not repealed, but it is now something the map must be *designed* to preserve rather than a free
>   consequence of the geometry. [The Vision §3](../vision.md#depth-is-the-point) records the downgrade.
> - **The one-purse problem has been answered.** The note's central warning — that under a single purse a coin
>   spent attacking is simply gone, so attacking is dominated — was taken seriously and resolved by
>   [#72](https://github.com/ssalter21/tower-defense-game/issues/72): the purse is still one, and the payback
>   comes from percentile performance bands, an unlock gate and scarce wave slots rather than a second wallet.
>   See [The Vision §3](../vision.md#one-purse).
>
> Everything else — the seven mechanisms, the five survivors, the thin precedent for defense-gated sending — is
> unaffected.

**Question:** how do tower defense games make *composing and sending a wave at another player* genuinely deep — and
has anyone shipped a game where the player's **defensive build determines their attacking options**?
**Inputs:** [The Vision](../vision.md) §3 (both boards, one purse), §4 (nothing persists), §8 seams 1 and 3;
[Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half) (the composition half of
the lever catalogue) and §2 (one unit, two roles); [Part II §3](../archive/async-ghost-round-robin.md#3-failure-modes--the-six-ways-this-goes-wrong).

---

## Bottom line

### Sending is deep in seven structurally distinct ways. Five survive this project's settled decisions, one is *strengthened* by the hex corridor, and one — the income loop every deep send system in the genre is built on — is the one the single purse takes away.

### The gating idea has exactly one shipped precedent, it is a single tower upgrade in Bloons TD Battles 1, and the sequel removed it. Every other game in the survey gates the opposite way round: it constrains the **defense** and leaves the **attack** vocabulary universal — deliberately, because a send is only a *read* if both players already know the whole menu.

Four claims carry this note.

**One. The north star's central tension is not available to copy.** Legion TD 2's send system is a two-currency
loop: gold buys workers, workers make mythium, mythium buys mercenaries, and *spending mythium permanently raises
your gold income* [[1]](#s1). Every send is an investment that pays you back. Under
[one purse](../vision.md#one-purse) a coin spent on an attacker is simply gone, which makes attacking a pure tempo
loss and — at equilibrium — dominated. **The one-purse precedent is Bloons TD Battles 2, not Legion TD 2**, and its
answer is that the send and the income are *the same purchase on one continuous dial* [[10]](#s10)[[24]](#s24).
§1.1 and §5 are about which of the three available answers this project takes; there is no fourth.

**Two. Ordering is the one mechanism the hex corridor makes stronger rather than dead.** Six of Part V's levers died
to the one-hex corridor; ordering is the compensation. A corridor exactly one hex wide that never branches *is* a
single-file column — Anomaly's convoy, with no route decision left to dilute it. And the walking skeleton has
already built the mechanism and already found its two preconditions:
[`content/wave.txt`](../../content/wave.txt) is an ordered list of `(tick, type, count)`, and its own comments record
that ordering is unobservable when units share a speed and unobservable again when a count spawns as one pile
[[30]](#s30). ⚠ But the skeptical case against ordering is real and specific, and it is in §2.

**Three. There is a risk in this design that no surveyed game carries, and it is not the gating question.** In
Legion TD 2 the wave is a public constant — *"The enemies have the same stats and come in the same order every game,
so planning ahead is advantageous"* [[1]](#s1) — so a mercenary is legible precisely because it is *the part that is
not the wave*. This project has the player compose the **whole** wave, against a stored defense they cannot
interrogate, resolving simultaneously with no input. That is strictly more authorship and strictly less legibility,
and the cheapest insurance is a shared baseline wave per stage with the player's composition layered on top. See
[§6](#6-recommendation).

**Four. "Towers gate creeps" is unprecedented in competitive TD, and the reason is structural rather than
historical.** It inverts the direction of gating that every shipped game chose. Legion TD 2 gates your *fighters* to
a semi-random roll and leaves all 26 mercenaries open to everyone, always [[1]](#s1)[[2]](#s2)[[16]](#s16). Clash of
Clans gates your army on Barracks and Army Camps — buildings that are *explicitly excluded from defending*
[[9]](#s9)[[28]](#s28). Direct Strike and Nexus Wars gate units on tech tiers and production buildings, with
cannons, bunkers and turrets sitting in a separate category that gates nothing [[22]](#s22)[[23]](#s23). §3 names
the four failure modes the inversion invites, names the one real upside, and gives three weaker dials that keep the
upside.

---

## 1. Seven mechanisms that make sending deep

Each row is a structurally distinct source of decision, not a flavour of one. The last column is the verdict from
[§5](#5-the-constraint-filter); read it there before acting on it.

| # | Mechanism | What the decision actually is | Shipped in | Cost to build here | Survives? |
|---|---|---|---|---|---|
| 1 | **Income vs. send** | Spend now on an attack that pays back later, or on defense that pays back now | Legion TD (lumber/wisps) [[15]](#s15)[[26]](#s26); Legion TD 2 (mythium/workers) [[1]](#s1); BTD Battles 1 & 2 (eco) [[10]](#s10)[[24]](#s24); Wintermaul Wars ⚠ [[27]](#s27) | An income ledger and a payout rule | **Only in a modified form** — §1.1 |
| 2 | **Timing** | *When* the send lands, within a round and across the run | LTD2 build-phase vs battle-phase arrival [[16]](#s16); BTDB2 six-slot FIFO queue plus per-send cooldowns [[25]](#s25); Clash Royale elixir and cycle [[29]](#s29) | Already built — orders carry a tick | **Yes, but only the build-phase half** |
| 3 | **Composition counters** | Pick attack types against what their defense is made of | LTD2's 75–125% matrix [[1]](#s1); Element TD 2's 4:1 cycle; Warcraft 3's 40:1 [[Part V §4.1]](../archive/variance-levers-and-unit-schema.md#41-the-scalar-layer--three-shapes-pick-exactly-one) | Part V §4 already specifies it | **Yes** |
| 4 | **Ordering** | Who walks in front of whom | Anomaly convoy [[17]](#s17)[[18]](#s18); Super Auto Pets board [[19]](#s19); BTDB2 queue order [[25]](#s25) | Already built [[30]](#s30) | **Yes — and strengthened** |
| 5 | **Stacking / overwhelm** | Save several rounds of purse for one break | LTD2 power mercs — *"only send power mercs if you think you can break your opponents on that wave"* [[16]](#s16); BTDB "layering" [[25]](#s25) | A carry-over rule on the purse | **Yes** |
| 6 | **Denial** | Make the outcome move money between the two players | LTD2: leaking pays *the attacker* [[1]](#s1), and one Legion Spell pays the *leaker* instead — *"Every time you leak, gain 60 Mythium"* [[3]](#s3); BTDB: the defender earns **nothing** for popping what you sent [[24]](#s24) | A payout rule — not a currency | **Yes, and it is the one-purse lever** |
| 7 | **The read** | Anticipate what they will send and what they built | LTD2 manual: *"Try to anticipate your opponent's sends as part of your strategy"* [[1]](#s1); Clash Royale Mirror Mode [[8]](#s8) | Free — it is a consequence of the format | **Yes, and it is what §3 is about** |

### 1.1 The one-purse problem, stated precisely

This is the most consequential finding in the note and it is not about gating.

Every send system in the genre that is regarded as deep pays the attacker back for attacking, and there are exactly
three shipped shapes for that payback:

| Shape | Who | Mechanism | Compatible with one purse? |
|---|---|---|---|
| **Second currency that cross-feeds** | Legion TD (gold ⇄ lumber via wisps) [[15]](#s15); Legion TD 2 (gold → workers → mythium → income → gold) [[1]](#s1)[[4]](#s4) | Sending is an investment in a *different* pool | **No.** This is precisely what [§3 One purse](../vision.md#one-purse) rules out, and what Part II's cross-feed proposal was declined for |
| **One currency, income as a continuous dial** | Bloons TD Battles 2 — *"Money … is used to buy towers, upgrades, and Bloon sends"*, and each send carries an eco change from +$1.00 (Grouped Reds, $20) to −$400 (Tight ZOMGs, $12,000) [[10]](#s10)[[24]](#s24) | The rush and the eco send are the same button with a different coefficient | **Yes.** This is the only shipped one-purse answer |
| **Outcome-driven transfer** | Legion TD 2 — when the defender leaks, *"You miss out on gold"* and *"Your opponents earn extra gold"* [[1]](#s1) | Attacking pays only if it *works* | **Yes.** It is a payout rule, not a currency |

Three primary numbers worth having on hand. A Legion TD 2 worker costs 50 gold and generates 1 mythium per 10
seconds [[4]](#s4)[[1]](#s1); the cheapest mercenary, Snail, costs 20 mythium and returns 6 income [[16]](#s16); the
Warcraft 3 original's community documentation puts the ratio at *"Income in a 20/1 relation"* [[26]](#s26). In BTDB2
the same tension is expressed as a break-even time per send — 120 s for Grouped Reds, 250 s for Grouped Rainbows,
1029 s for Tight Leads, never for blimps [[24]](#s24). **Both games make the attack purchase legible by attaching a
second number to it.** A one-purse design with no such number leaves the player comparing a tower to an attacker on
nothing but gut.

⚠ **The failure this predicts.** If attacking returns nothing at all, then in a game where both boards resolve
simultaneously the dominant line is to buy only defense and let the opponent bankrupt themselves attacking. That is
not a balance problem a harness can tune away with numbers; it is a structural one. Row 6 — denial — is the cheapest
fix that does not reintroduce a currency, and it is the one the north star already ships.

### 1.2 What "a send reads as skilful" is actually made of

Four things, all sourced, none of which is "picking the strongest unit":

- **It is a counter to something specific.** The LTD2 wiki's rule is to send mercenaries whose attack and defense
  types differ from the wave's, because the opponent has built fighters tuned against the wave [[16]](#s16). The
  send is skilful because it exploits a commitment the opponent already made.
- **It is a bet with a stated threshold.** *"Your team should only send power mercs if you think you can break your
  opponents on that wave… If you do not believe you can break your opponent on this wave, but will have trouble
  holding their send, send income"* [[16]](#s16). The decision has a named branch, which is what makes it teachable.
- **It is simultaneous.** Both players commit blind. Supercell's framing of Clash Royale's Mirror Mode is the
  cleanest developer statement of why this is where skill lives: *"same deck, same starting hand, what makes the
  differences are your decisions"* [[8]](#s8).
- **It compounds.** BTDB2's eco dial and LTD2's permanent income mean an early send is worth more than a late one,
  so the skill is partly in *when you started*, not only in what you picked.

---

## 2. Ordering and timing as a skill — and the case against

The developer wants explicit control over the order creeps emerge. The honest answer has three parts: it is real,
it is a known trap, and the specific reasons it is a trap mostly do not apply here.

### 2.1 Who actually ships player-authored ordering

| Game | Authored how | Re-orderable? | Verdict |
|---|---|---|---|
| **Anomaly: Warzone Earth** | The convoy is a single-file column and the player sets the order | Yes, mid-mission, **and the game pauses while you do it** [[18]](#s18) | The closest analogue to this project by far |
| **Super Auto Pets** | Five slots; *"the player's rightmost pet attacking the opponent's leftmost pet first"* [[19]](#s19) | Yes, in the shop phase | Real depth, and it ships a cautionary bug — §2.3 |
| **BTD Battles 1 & 2** | *"Bloon sends are deployed one at a time in order of earliness"*, with a 6-slot queue [[25]](#s25) | **No** — order is a consequence of when you clicked | Ordering is emergent from real-time timing, not authored |
| **Clash Royale** | Deployment order in real time; Supercell's own instruction is *"support him by dropping Ice Golem or Ice Spirit in front (before deploying Hog Rider)"* [[7]](#s7) | n/a | Ordering is a live-execution skill, not a build-phase one |
| **Legion TD 2** | **None.** *"After sending a mercenary, it will wait until the next enemy wave comes. Then, it will attack together with the enemy wave."* [[1]](#s1) | — | The north star does not do this at all |
| **This repository** | `content/wave.txt`: an ordered list of `(tick, type, count, corridor)` orders [[30]](#s30) | — | Already built, already asserted-not-sorted |

### 2.2 What makes ordering matter mechanically

Ordering is inert unless the simulation contains at least one of these. Each is a lever Part V already catalogues,
so the cost is picking them rather than inventing them.

| Mechanism | What it does | Where it lives |
|---|---|---|
| **Tanking** | The front unit absorbs fire the ones behind would have taken | Targeting priority. The skeleton already resolves ties by *"the creep furthest along, and the lower id if two are equal"* [[30]](#s30) — that is "first" targeting, and it is what makes front-ness mean anything |
| **Overkill baiting** | A cheap unit in front eats a slow heavy shot that would have one-shot the expensive one behind | Part V §3.2, *overkill policy* — carries or wasted |
| **Spin-up and reload** | A tower that ramps rewards a long column; one that reloads rewards a gap | Part V §3.10 |
| **Positional relations** | *"Friend ahead attacks: Gain +1 attack and +1 health"* (SAP, Kangaroo); *"Give the nearest friend behind +1 attack and +2 health"* (Camel); *"Faint: Give the two nearest friends behind…"* (Flamingo) [[19]](#s19) | A positional predicate — new vocabulary, and the expensive option |
| **Interleaving by speed** | A fast group launched later overtakes a slow one launched earlier, so the arrival order is not the launch order | Movement speed. Already exercised: the skeleton's runner is exactly twice the grunt's speed *specifically* so this happens, and the first overtake is a committed landmark at tick 366 [[30]](#s30)[[32]](#s32) |

**Two preconditions, both already discovered in this repository, both easy to violate.** `content/wave.txt` states
them plainly: units of one type share a speed and a route, so *"ten of them released together are one stack forever,
and a stack is the single arrangement in which unit ordering cannot be observed at all"*; and
`content/units.txt` records that the two creep types *"differ in SPEED AND MAXIMUM HP ONLY, so that a later fast
group catches an earlier slow group and unit ordering stops being theoretical"* [[30]](#s30). **A roster whose units
all move at one speed makes ordering decorative no matter how good the UI is.**

### 2.3 The skeptical case — three named traps

**Trap 1: ordering is usually solved once, then repeated.** This is the strongest negative evidence available, and
it comes from the game that shipped the mechanic best. Anomaly's own Steam reviews: *"once you get familiar with
game mechanics, you can finish this game with almost same unit formation every time on any difficulty"*
[[18]](#s18). And 11 bit studios never marketed the mechanic — the store page and the studio page both name *route*
and *squad composition*, and Paweł Miechowski's Gamasutra postmortem does not discuss convoy ordering at all
[[17]](#s17). **If ordering were the load-bearing decision, the studio would have said so once.**

**Trap 2: ordering scales badly against a second simultaneous per-unit decision.** Anomaly 2 added unit morphing on
top of the same convoy, and its Steam reception fell from Very Positive (84% of 1,008) to Mostly Positive (78% of
439). Representative community complaints: *"The game forces you to micromanage convoy order and unit morph form but
fails to make it easy to do so"* and *"I kept to the same 2 convoy configurations throughout the entire game"*
[[18]](#s18). ⚠ These are extracted from a review listing rather than attributed to named reviewers; treat as
directional sentiment, not as a citation.

**Trap 3 — the one that would actually hurt: a second, invisible ordering system.** In Super Auto Pets, board order
governs *spatial relations* but **not** the order in which abilities resolve within a phase. That is governed by
attack stat. The developer conceded it on the record, in a thread titled "Game needs a defined order of triggers in
the same phase": *"the ordering depends on attack power, but it sounds like it isn't clear how it works, so thank
you for the feedback!"* [[20]](#s20). A player in the same thread: *"everything in the chain would have survived
easily if the buffs had been applied first."* Community strategy pages now instruct players to build around the
hidden rule — *"abilities trigger based on order of attack, so you need to make the Skunk have higher attack than
the Dolphin"* [[19]](#s19).

**The cross-cut across every auto-battler surveyed is the design rule to take from this section:**

> **Position defines the wiring, not the schedule.** Not one of Super Auto Pets, TFT, Backpack Battles or The Bazaar
> uses board order as resolution order. When those designers wanted sequencing they reached for a stat (SAP: attack)
> or a timer (Backpack Battles and The Bazaar: per-item cooldowns) and let position define the *graph of
> relationships* instead [[19]](#s19)[[21]](#s21).

For this project that maps directly onto rules Part V already mandates: spawn order determines *who is in front of
whom*, and the order of simultaneous events is *"broken by a stable integer key present in the data… never by object
identity"* [[Part V §5.4]](../archive/variance-levers-and-unit-schema.md). Those must stay two different things. A wave
whose spawn index quietly leaks into modifier application order is SAP's bug with this project's name on it.

### 2.4 Verdict on ordering

**Not a trap here, for reasons specific to this design — and each reason is a constraint doing work.**

- Trap 1 is a *fixed-content* failure. Anomaly's convoy was solved once because the towers it walked past never
  changed. Here the thing being ordered against is [a different player's defense every wave](../vision.md#2-the-loop--one-machine-at-three-latencies).
- Trap 2's mitigation — Anomaly's pause-to-reorder, the one thing press explicitly credited [[18]](#s18) — is
  already the format. Everything is authored in a build phase and *"a wave resolves with no input"*
  [[Vision §3]](../vision.md#build-phases-between-waves-and-nothing-during-one). There is no time pressure to
  mitigate.
- Trap 3 is answerable by the arithmetic contract Part V §5.1 already requires, and by nothing else.
- And the corridor makes the column the *only* spatial decision the attacker has. In Anomaly ordering competed with
  route authoring for the player's attention; here route authoring does not exist. **This is the one place where
  "no mazing, ever" gives something back.**

The residual cost is honest and it is UI, not depth: an ordered list is a fourth thing on a screen that already has
to hold two live battles, an economy and a build menu [[Vision §8 seam 7]](../vision.md#7--the-interface). Bound it
by bounding the wave — BTDB2 caps the player at ten send options and six queue slots [[25]](#s25); Legion TD 2 shows
four numbers per unit [[Part V §11]](../archive/variance-levers-and-unit-schema.md#11-what-id-build-first).

---

## 3. The gating idea, tested against precedent

> *"Each tower of a certain type can unlock a skill tree for the creeps you can buy. The creeps you can buy are
> options given from a pool decided by your skills."*

### 3.1 What was searched

Tower defense with sending (Legion TD, Legion TD Mega, Legion TD 2, Wintermaul Wars, Gem TD, Element TD 1 and 2,
Green TD, Enfo's, Sunken Defence, CreepTD, Tower Wars, Tower Storm, Bloons TD Battles 1 and 2); SC2 tug-of-war
(Direct Strike, Desert Strike, Nexus Wars); Castle Fight; Clash of Clans and Clash Royale; Orcs Must Die! Unchained
Siege; Dungeon Keeper and successors; Prismata; Mechabellum; assorted mobile PvP TD. Coverage gaps are listed in
[§7](#7-what-could-not-be-verified).

### 3.2 The one precedent, and it is a real one

**Bloons TD Battles 1 — the COBRA tower, path 1, tier 4, "Offensive Push," $1,750: *"Next tier of Bloons becomes
available to send 1 round earlier than normal."*** [[13]](#s13)

That is, literally, a tower you place on your own track whose purchase changes which attackers you may buy. Four
details make it more useful than a curiosity:

- **It is a tower slot, not a menu.** COBRA *"use[s] pistols to pop Bloons and espionage tactics on either Bloons or
  enemy players"*; its *"attack power, while never missing a shot, is extremely poor"* and its value is support
  utility [[14]](#s14). So it occupies the defensive budget while paying out on the offensive side — the exact
  coupling the developer is proposing, at the smallest possible scale.
- **It gates the *schedule*, not the *pool*.** One round earlier. Nothing becomes available that would not have
  become available.
- **It ships with a carve-out.** Bloon modifiers (Regrow, Camo, Fortified) *"cannot be unlocked on an earlier round
  even with COBRA's Offensive Push"* [[25]](#s25) — the designers explicitly fenced off part of the send space from
  the coupling. That carve-out is independent evidence the upgrade does what the description says.
- ⚠ **Battles 2 does not have it.** COBRA was reworked into the hero Agent Jericho, whose send interaction reduces
  the *cost* of bloon modifiers rather than their availability [[24]](#s24)[[14]](#s14). **A shipped competitive TD
  built this exact coupling and the sequel replaced it with a discount.** Ninja Kiwi published no reasoning that
  could be retrieved, and Battles 2 rebuilt the entire tower roster, so this is suggestive rather than a verdict —
  but it is the single most decision-relevant fact in this note.

### 3.3 Everything else — how the near-misses actually gate

| Game | The gate | Is the gating thing defensive? | Verdict |
|---|---|---|---|
| **Legion TD 2** | None on mercenaries. All 26 are one unified list, available on affordability alone [[2]](#s2)[[16]](#s16). Your *fighters* are the gated half — a roll *"guaranteed to include a range of fighters from different price points, and also cover all of the attack and defence types"* [[16]](#s16) | — | **Gates the defense, universalises the offense.** The exact inverse of the proposal |
| **Legion TD (WC3, ENT)** | *"not all of them are available from the start"*; barracks 2 opens *"as soon as the Level 10 timer hits 0"*, barracks 3 at Level 15 [[26]](#s26) | No — a **wave-timer** gate | Time gate |
| **Clash of Clans** | *"Root Rider is a new Elixir troop available at Town Hall 15 when you upgrade your Barracks to Level 17"* [[9]](#s9) | **No, emphatically.** Army Camps *"do not defend your village during an attack… Its destruction will not affect your army in any way"* [[28]](#s28) | Army buildings only |
| **Direct Strike / Desert Strike (SC2)** | Paid **tech tier** upgrades unlock units | No — bunkers, cannons and turrets are a separate defensive category that unlocks nothing [[22]](#s22)[[23]](#s23) | Tech gate |
| **Nexus Wars (SC2)** | The production building **is** the send; *"Pylons give more income than buildings, but do not produce any units"*; *"Building cannons can be useful to hold off pushes"* [[23]](#s23) | No — three separate categories | No gate to speak of |
| **Wintermaul Wars** | A "Shrine" tier gates stronger sends [[27]](#s27) | ⚠ **Could not verify** whether the Shrine is part of your maze or a dedicated send building. Do not rely on this row | Unresolved |
| **Tower Wars (2012)** | *"Unlock and upgrade technologies to bolster the stats and functions of your units and towers!"* [[11]](#s11) | A **shared tech tree buffing both**, not a gate | Shared tree, no gate |
| **Orcs Must Die! Unchained**, Siege | Traps and minion cards compete for the **same deck slots** [[31]](#s31) | Shared *budget*, not a shared unlock graph | The genre's actual answer |
| **Prismata** | One shared tech tree; the same purchases attack and block [[31]](#s31) | One tree, both roles, no gate | Structural analogue |
| **Dungeon Keeper** | *"Which creatures enter the dungeon depends on which rooms the player has and how large they are; most creatures have prerequisites for entering service"* [[31]](#s31) | Rooms are infrastructure; defense proper is traps, doors and fortified walls | **The closest full-strength analogue found anywhere — and it is a different genre, largely PvE** |
| **Bloons TD Battles 2** | Rolling round window: every send has a *First Round Available* and a *Last Round Available*, capped at ten options at a time [[24]](#s24)[[25]](#s25) | No — a round gate | The most interesting *non*-build gate in the survey |

### 3.4 Verdict

**No shipped competitive tower defense gates the attacker pool on the defensive build.** One tower upgrade in one
game gated the attacker *schedule*, and its sequel dropped it. The nearest full-strength precedent is a 1997
single-player strategy game.

**This is not an unexplored idea; it is an idea every one of these games declined in the same direction.** The
mechanism is unmistakably available — five of the games above already gate offense on *something you build* — and
none of them chose a defensive structure as the gate. That is a pattern, not a coincidence, and the reason is
legible:

> **A send is a read, and a read requires common knowledge of the menu.** If both players know the whole attacker
> pool, then choosing one attacker is a statement about *the opponent*. If your pool is a function of your own
> build, choosing one is partly a statement about *yourself* — and in a format where the opponent's defense is
> already visible to you as a stored ghost, that statement was already made.

### 3.5 The four failure modes, named

**1. Double-dominance collapse — the serious one.** Under [one purse](../vision.md#one-purse), a build that is best
on defense would also be the gate to the best offense. Any mispricing compounds instead of trading off: in a
two-purse game an overtuned tower costs you mythium efficiency; here it wins both halves at once. This lands
squarely on [seam 4](../vision.md#4--the-balance-harness), because the harness's method is to sweep every unit
against every defense and let a red cell name what is mispriced. **A gated pool makes a tower's contribution
non-separable from the pool it unlocks**, so a red cell stops naming one thing and starts naming a bundle. That is a
direct tax on the one balancing method [Vision §5](../vision.md#5-how-it-is-balanced) says is the only one that
works at this scale.

**2. Counter-picking degrades to a lookup, then to nothing.** You attack a stored ghost whose defense you can see.
Under gating, seeing their defense tells you their send. Simultaneously, your defense tells them yours. That is not
a read; it is a table lookup — and because both boards resolve at once with no input, neither player can adapt
within the round. The game degenerates toward *who has the better single build* rather than *who read whom*, which
is the half of the loop Part II identified as the strongest idea in the design.

**3. It punishes experimentation twice, in a game that already forbids the usual cushion.** Nothing persists but
rating, so *"every unit must be interesting from the first run"* [[Vision §4]](../vision.md#4-what-persists). Under
gating, a defensive experiment costs you your offensive options as well — the price of trying something unfamiliar
is doubled at exactly the moment the design has removed the drip-fed option space that normally softens a first
hour. Part V §4.5 already flags that a stored-ghost pool degrades when one layout dominates; gating accelerates
that, because the dominant defense would be copied for two reasons rather than one.

**4. It is a fourth information layer on the hardest unsolved problem in the design.** [Seam
7](../vision.md#7--the-interface) already has to hold two live battles, an economy, a build menu and a readable
account of what the opponent just did. A per-tower skill tree governing a second menu is not a small addition to
that screen.

### 3.6 The upside, stated as strongly as it deserves

**One coherent identity per run.** With one purse and two independent menus, a build phase is arguably two small
decisions sharing a wallet — the exact failure [Vision §3](../vision.md#one-purse) chose one purse to avoid, arriving
by a different door. Gating makes it one decision with two consequences, and it gives a run a *name*: "I went frost."
That matters more here than it would elsewhere, because [seam 6](../vision.md#6--the-social-layer) needs an absent
opponent to feel like a person, and *"presence is made of specifics"*. A defense with a legible identity is a
specific; a defense that is a list of towers is not.

It also compresses the decision space at any one moment, which is worth something in a game that has deliberately
given up the usual on-ramp. Lars Doucet's stated reason for cutting mazing from Defender's Quest is the same
argument: *"the biggest was to limit the number of choices the player had to make at any given time"* [[12]](#s12).

### 3.7 Three weaker dials that keep the upside

Ranked by how much shipped evidence stands behind them.

| Dial | What it does | Precedent | Cost | Keeps the read? |
|---|---|---|---|---|
| **Gate the schedule** | Towers change *when* an attacker becomes available, on a rolling window | **COBRA's Offensive Push** [[13]](#s13), plus BTDB2's first/last-round window [[24]](#s24) | One integer per tower type; the window already needs to exist if the roster grows across a run | Yes — the menu is still common knowledge, only the clock moves |
| **Gate the price** | Every attacker available to everyone; towers make some cheaper | BTDB2's Agent Jericho reduces modifier *cost*, not availability [[24]](#s24); Part V §3.11's stacking discount caps | A cost expression, which Part V §9 already requires to be data | Yes, fully |
| **Gate a rider, not a unit** | Towers unlock *modifiers* on a universal roster — a tag, a status, an on-death | Bloon modifiers are separately gated from bloon tiers [[24]](#s24); Part V §3.8's status envelope holds this shape already | Highest — a second vocabulary layer, and the most exposed to failure mode 1 | Partly — the units are common knowledge, the riders are not |

All three preserve the property that makes a send a read: **the vocabulary stays universal, and what your build
changes is a coefficient on it.**

---

## 4. Roster design with classes and roles

### 4.1 What shipped role systems actually are

The developer wants "real depth, like building a roster with different classes and roles." Four surveyed systems,
and the useful finding is that the good ones define a role as a **job with a mechanical consequence**, not as a stat
band.

| Game | The taxonomy | What makes it more than labelling |
|---|---|---|
| **Legion TD 2** | Seven role icons: *"Tank: high health, but low attack power"* · *"DPS: high attack power, but low health"* · *"Versatile: good all-around stats"* · *"Aura: supports nearby allies"* · *"Carry: weak on its own, but powerful if supported"* · *"AoE: deals area-of-effect damage"* · *"Mana User: uses mana for special abilities"* [[1]](#s1) | Three of the seven are *relational* — Aura, Carry and Mana User only mean something with respect to the rest of the board. A role that describes one unit alone is a stat band; a role that describes a dependency is a build |
| **Teamfight Tactics** | "Roles Revamped" (Set 15): Tank, Fighter, Carry, Caster, Assassin. Riot's lead designer: *"A simple rule that Tanks win 50/50s on choosing your next target allows players to place melee champions in the front row instead of the second"* [[5]](#s5) | **The role is implemented as a targeting rule.** This is the mechanism ordering needs — it is what makes "front" a real position rather than a drawing |
| **League of Legends** | Six functional classes with subclasses, plus **Specialist** — the bucket for champions that do not fit *"the 'neat little boxes' of the other classes"* [[33]](#s33). Statikk's stated goals included *"Create a shared vocabulary!"* and the note that the structure is *"more of a set of guidelines than rigid rules"* | A named residue bucket. Riot did not force every unit into the grid; they gave the leftovers a name — which is precisely what Part V §10.8 argues for when it rejects class inheritance |
| **Overwatch** | Three verb-led role blurbs: Tank heroes *"soak up damage and shatter fortified positions"*; Damage heroes *"seek out, engage, and obliterate"*; Support heroes *"empower their allies by healing, shielding, boosting damage, and disabling foes"* [[6]](#s6) | Every definition is a verb plus a target. None is a number |

Riot also published the effect of naming roles, with playtest data behind it: among lower-ranked players, the change
improved understanding of *"what units do based on their roles and what items may be good on them"* [[5]](#s5).
**Naming the role made the correct use of the unit legible to weak players** — which is the exact problem
[Vision §4](../vision.md#4-what-persists) creates by refusing to drip-feed the option space.

### 4.2 What makes a role legible rather than a spreadsheet

Four mechanisms, in descending order of evidential strength.

1. **The silhouette carries the role.** Valve on Team Fortress 2: *"the silhouettes of the nine classes were
   carefully designed to be very distinct from one another"* and *"Even when viewed only in silhouette with no
   internal shading at all, the characters are readily identifiable to players"* [[34]](#s34). Riot: *"Silhouettes
   are the single most important thing for champion recognition in League"* [[35]](#s35). This is a
   [seam 8](../vision.md#8--the-presentation) obligation, and it is a constraint on the KayKit purchase: buying
   stock models means the roster's silhouette variety is a *shopping* decision made once, not a modelling decision
   made later.
2. **Threat class reads before identity.** Blizzard's "Play by Sound" hierarchy for Overwatch is *what your threat
   is → where it is coming from → can you tell who it is*, and its counterintuitive rule is to **reduce** variation
   to increase recognisability [[36]](#s36). Directly portable: make the *role* distinguishable before you make
   individual units distinguishable — which for two boards on one screen is not optional.
3. **Roles must be relational or they are stat bands.** Three of Legion TD 2's seven, and TFT's tank targeting rule.
4. **Keep a named escape hatch.** Riot's Specialist.

### 4.3 Does Part V's "one unit schema, two roles" survive a role-based roster?

**Yes, and it is strengthened rather than strained.** Three checks:

- **The role taxonomy is orthogonal to the placed/moving split.** Legion TD 2's seven icons are documented on
  fighters, and every one of them describes a mercenary equally well — a Tank mercenary, an Aura mercenary, a Carry
  that needs the rest of the send to survive. The role is a *tag over the shared schema*, exactly as Part V §3.1
  specifies (`Role: placed | moving` is "a filter, not a type"), and a second tag alongside it costs nothing.
- **TFT's evidence pushes the same way.** Riot implemented "Tank" as a targeting rule — that is, as a modification
  to a system both sides already share. In Part V's terms it is a component on the `Targeting` group, not a class.
- **⚠ One caution.** A role must not become a class. Part V §10.8 already forbids `MageTower : ProjectileTower :
  Tower`, and the pressure to write `class Tank` arrives the moment roles get mechanical consequences. The
  discipline is the one Riot itself stated: the taxonomy is *"more of a set of guidelines than rigid rules"*
  [[33]](#s33), with a Specialist bucket for the residue. In schema terms: **roles are a queryable tag set with a
  documented meaning, never an inheritance edge.**

One addition Part V does not have, and this note earns: **a role should be defined by the component that makes it
true.** "Tank" is not `maxHp > n`; it is *the unit other units' targeting prefers*. That definition is checkable by
the build helper, survives a rebalance, and is the difference between a role and a description.

---

## 5. The constraint filter

Read against [The Vision](../vision.md) §3, §4 and §11. **Dead means dead** — these are settled decisions, not
preferences.

| Mechanism | One purse | One-hex corridor, never branches | Nothing persists | No input during a wave | Both boards at once | Deterministic integer sim | Verdict |
|---|---|---|---|---|---|---|---|
| Two-currency income loop (LTD2, Legion TD) | **Kills it** | — | — | — | — | — | **DEAD** |
| Income-as-a-dial on one purse (BTDB2 eco) | Survives — one purchase, two numbers | — | — | — | — | Integer per-round accrual, fine | **Alive** |
| Outcome transfer (leak pays the attacker) | Survives — a payout rule, not a currency | — | — | — | Symmetric by construction | — | **Alive, and preferred** |
| Composition counters (type matrix / gates) | — | — | — | — | — | Part V §4 already integer | **Alive** |
| **Ordering the column** | — | **Strengthened** — one hex wide *is* single-file | — | Authored in the build phase | — | An ordered list, already built | **Alive** |
| Spawn *timing* within a wave | — | — | — | Authored, not clicked | — | Integer ticks, already built | **Alive** |
| Real-time send queue / "layering" (BTDB2) | — | — | — | **Kills it** — layering is clicking during a live round | — | — | **DEAD** |
| Reactive sending (answer their send) | — | — | — | — | **Kills it** — simultaneous commitment | — | **DEAD** — the game is simultaneous-move |
| Stacking / saving for a break | Survives; it *is* the purse decision | — | — | — | — | — | **Alive** |
| Route / lane choice, split-lane sends | — | **Kills it** | — | — | — | — | **DEAD** |
| Map-placed attacker abilities (Anomaly decoys) | Costs purse | Survives — off-corridor cells exist | — | — | — | — | Alive but unexamined |
| Persistent send unlocks, loadouts, hangar tech (Mechabellum) | — | — | **Kills it** | — | — | — | **DEAD** |
| Per-instance attacker levelling | — | — | Kills it (and Part V §10.5) | — | — | Replay hazard | **DEAD** |
| Roles as tags (LTD2, TFT) | — | — | — | — | — | Queryable, hashable | **Alive** |
| Roles as targeting rules (TFT tanks) | — | Ordering makes it meaningful | — | — | — | Needs a deterministic tiebreak — already has one | **Alive** |
| Towers gate the attacker **pool** | ⚠ Compounds under one purse — §3.5 | — | — | — | ⚠ Read collapses — §3.5 | — | **Possible, high risk** |
| Towers gate the attacker **schedule or price** | Survives | — | — | — | Read survives | One integer | **Alive, precedented** |

**Two consequences worth pulling out of the table.**

- **This is a simultaneous-move game, and that is a bigger constraint on sending than the corridor is.** Every deep
  send system surveyed lets a player react — LTD2 lets you send *during* the battle phase to land next wave
  [[16]](#s16), BTDB2 lets you rush the instant you see their defense thin. Here, both boards resolve at once and
  nobody acts during a wave. So the entire skill of sending must live in *prediction*, which is exactly the frame
  Supercell puts on Clash Royale's Mirror Mode [[8]](#s8) and exactly what Legion TD 2's manual asks for
  (*"Try to anticipate your opponent's sends"* [[1]](#s1)) — but here it is the only frame available, not one of two.
- **The round-robin format weakens the read further, and nothing in the survey addresses it.** [Vision
  §2](../vision.md#2-the-loop--one-machine-at-three-latencies) draws a *fresh opponent every wave*. You cannot build
  a model of one player across a match, because there is no one player. What you can read is the *stored defense*,
  which is visible — so the design's realistic read is "counter this layout," not "counter this person." That is
  closer to a puzzle than to a duel, and it is the thing Part II §6 step 3 already named as the real gate on the
  whole design.

---

## 6. Recommendation

Three directions. Each is a coherent whole, not a menu of features; the trade-off is named; ranked, with reasons.
**None of them is decided here.**

### First — Universal roster, and the wave *is* the order and the clock

Ship a small universal roster (LTD2's austerity is the target: *four numbers per unit* [[Part V
§11]](../archive/variance-levers-and-unit-schema.md#11-what-id-build-first)), role-tagged, with a type matrix, and put the
entire attacking decision into **which units, how many, in what order, at what ticks** — the exact shape
[`content/wave.txt`](../../content/wave.txt) already has. Depth comes from mechanisms 2, 3, 4, 5 and 6 in §1;
mechanism 1 arrives as an outcome transfer (breaking a defense pays you; leaking pays them), which keeps the purse
single. Attackers must vary in **speed** or ordering is decorative [[30]](#s30), and units must vary in **role**
relationally or the roster is a stat table.
**Trade-off:** it delivers no run identity. A run is a good list, not a named thing, and [seam
6](../vision.md#6--the-social-layer) has less to work with. **Why first:** it is the only direction whose cost is
approximately zero — the record format, the ordered wave, the tie-break rule and the overtake landmark all exist and
are tested. It is also the direction that answers Part II's open gate (*is composing a wave against a fixed,
non-reacting defense fun?*) with the fewest confounds, which is the question that decides whether any of this is
worth building.

### Second — Universal roster, and towers move the clock

Everything above, plus the developer's idea in the one form that has shipped: a tower type advances *when* certain
attackers become purchasable, on a rolling window with a first and last round, as in Bloons TD Battles
[[13]](#s13)[[24]](#s24). The pool stays common knowledge, so a send is still a read; what your build buys is tempo
on the attack side, which is a second reason to commit to a tower and a legible one.
**Trade-off:** it puts a second system in front of a player before wave three, and it partially couples the balance
harness's per-unit sweep — a tower's win-rate contribution now has an attack-side term. That is a real cost against
[Vision §5](../vision.md#5-how-it-is-balanced) and it should be priced before it is built, not after.
**Why second:** it delivers a meaningful slice of the "coherent identity" upside for one integer per tower type, and
it is the only version of the gating idea with a shipped precedent. Note honestly that the precedent's sequel
dropped it.

### Third — Towers unlock riders on a universal roster

Towers do not unlock *units*; they unlock **modifiers** applied to units everyone can buy — a tag, a status, an
on-death effect. Part V §3.8's status envelope and §7's effect tree already hold the shape, and Bloons' separation
of bloon *tiers* from bloon *modifiers* is the precedent for gating the two independently [[24]](#s24). This is the
fullest delivery of the "skill tree for the creeps you can buy" fantasy that does not collapse the read, because the
roster stays universal and only the coefficients move.
**Trade-off:** the highest schema cost in the note, the most exposure to §3.5's double-dominance collapse, and the
heaviest load on [seam 7](../vision.md#7--the-interface). It also multiplies the balance harness's sweep space by
the number of riders.
**Why third:** it is the most interesting and the least evidenced. It should not be built before the first direction
has been played.

### One thing to decide regardless of which is chosen

**Whether the wave has a shared, public baseline.** Legion TD 2's mercenary is legible because the wave is a
constant every player has memorised [[1]](#s1); this design has no constant at all. A hand-authored baseline wave
per stage — which [Vision §2](../vision.md#2-the-loop--one-machine-at-three-latencies) already commits to building
for the cold-start floor, in a different form — would give the player's composition something to be *a delta from*,
and would give the opponent something to plan against. It costs content that is already scheduled. It is also the
single cheapest insurance against the risk in claim three of the bottom line, and it is orthogonal to all three
directions above.

---

## 7. What could not be verified

Stated plainly, because several of the negative findings in §3 rest on absence of evidence rather than on evidence
of absence.

1. **Whether Wintermaul Wars' "Shrine" is a defensive structure.** If it is, §3.4's "no precedent" verdict weakens.
   The one guide found treats it as an offense/economy building but never says so [[27]](#s27). Hive Workshop and
   EpicWar return 403 to automated fetching; a human with a browser could settle this in ten minutes and it is the
   highest-value open item in the note.
2. **Why Bloons TD Battles 2 dropped Offensive Push.** Ninja Kiwi's de facto patch-note channel for Battles 2 is
   Reddit, which was unreachable. The COBRA→Jericho rework coincided with a full roster rebuild, so intent cannot be
   inferred from the change alone.
3. **The exact in-game text of Offensive Push.** The effect is corroborated independently by the carve-out on the
   Bloon send page — *"cannot be unlocked on an earlier round even with COBRA's Offensive Push"* [[25]](#s25) — so
   the mechanic is not in doubt; the wording is community-transcribed [[13]](#s13).
4. **Legion TD 2's mercenary pool being ungated** is an argument from silence across the developer manual, the
   developer's unit guide and the community wiki, none of which states any unlock condition [[1]](#s1)[[2]](#s2)[[16]](#s16).
   Strong, but not a positive statement.
5. **Clash Royale's 8-card deck, 4-card cycle and elixir rates are community-documented only.** Supercell publishes
   no mechanics page for them [[29]](#s29). The role vocabulary quotes in §1.2 and §2.1 *are* Supercell's own
   [[7]](#s7)[[8]](#s8).
6. **Element TD 1's send mechanics, Enfo's PvP disruption, Sunken Defence, CreepTD, Nexus Wars' defensive
   structures, Creeper World, Iron Marines and Sanctum** were not resolved. Fandom, Reddit, Hive Workshop, EpicWar,
   Liquipedia and the Wayback Machine all refused automated fetching during this session, and the session's search
   budget was exhausted.
7. **The inverse case — offense gating defense — got no coverage at all.**
8. **No designer writing was found that names "your build determines both halves" as a failure mode.** §3.5's four
   failure modes are derived from this project's own constraints, not quoted from anyone. Adjacent primary sources
   exist (Harvey Smith's *Orthogonal Unit Design*, GDC 2003; Keith Burgun on counter systems; David Sirlin on
   solvability) but none of them is about this coupling, and none is cited as if it were.

---

## Sources

Every entry names its own source type. **Primary** means developer-published; where a claim rests on a community
wiki, a forum or a press review, the entry says so — this genre is badly under-documented first-party, and the
Warcraft 3 lineage in particular has almost no surviving primary material. Entries 1–12 are the ones this note
leans on hardest.

<a id="s1"></a>1. **Legion TD 2 — official game manual**, AutoAttack Games, [beta.legiontd2.com/manual/](https://beta.legiontd2.com/manual/). Source of: *"Mythium is your secondary resource and is used to send mercenaries and upgrade the king"*; *"Income is a number that increases permanently whenever you spend mythium"*; *"At the end of each battle phase, you gain gold equal to your income"*; *"Mercenaries are units you send to attack your opponent. After sending a mercenary, it will wait until the next enemy wave comes. Then, it will attack together with the enemy wave"*; *"Try to anticipate your opponent's sends as part of your strategy"*; *"Each Worker generates 1 mythium per 10 seconds"*; leaking (*"You miss out on gold"*, *"Your opponents earn extra gold"*); *"The enemies have the same stats and come in the same order every game, so planning ahead is advantageous"*; the seven role icons; and the 75%–125% attack/defense multiplier rule.
<a id="s2"></a>2. **Legion TD 2 — official unit guide**, [beta.legiontd2.com/guide/units/](https://beta.legiontd2.com/guide/units/). Fighters organised by legion (Atlantean, Divine, Element, Forsaken, Grove, Mastermind, Mech, Nomad, Shrine); mercenaries presented as **one unified list with no faction division**; *"Hire mercenaries to attack the enemy king."*
<a id="s3"></a>3. **Legion TD 2 — Legion Spells**, [beta.legiontd2.com/legionspells/](https://beta.legiontd2.com/legionspells/). *"At the start of the game, all players are given the same three randomized Legion Spells."* Note two spells that couple the two halves explicitly — *All Out Assault* (+100 mythium, −12 income) and *Counterattack* (*"Every time you leak, gain 60 Mythium"*).
<a id="s4"></a>4. **Legion TD 2 Official Gameplay Guide**, AutoAttackGames, Steam, [id=1793195628](https://steamcommunity.com/sharedfiles/filedetails/?id=1793195628). *"Workers cost 50 gold and generate 1 mythium per 10 seconds."* Developer-authored but hosted on Steam.
<a id="s5"></a>5. **Riot Games — "TFT: K.O. Coliseum Learnings"**, Michael "TheDjinn" Sloan and Giovanni Scarpati (Lead Designer, Roles Revamped), [teamfighttactics.leagueoflegends.com](https://teamfighttactics.leagueoflegends.com/en-us/news/dev/dev-tft-ko-coliseum-learnings). *"A simple rule that Tanks win 50/50s on choosing your next target allows players to place melee champions in the front row instead of the second"*; roles named as Tank/Fighter/Carry/Caster/Assassin; the playtest finding on lower-ranked players understanding *"what units do based on their roles"*. Also Riot's breakpoint philosophy at [Enchanted Wilds overview](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/enchanted-wilds-overview) — trait ladders published as their identity (2|4, 3|5|7, 3|5|7|9|11), and [patch 17.7](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/teamfight-tactics-patch-17-7) moving a breakpoint as a balance lever (*"Anima Squad Trait Breakpoint: 3/6 ⇒ 3/5"*). Positioning named as a genre pillar at [What is Teamfight Tactics](https://teamfighttactics.leagueoflegends.com/en-us/news/game-updates/what-is-teamfight-tactics/).
<a id="s6"></a>6. **Blizzard — Overwatch hero roles**, [overwatch.blizzard.com/en-us/heroes/](https://overwatch.blizzard.com/en-us/heroes/), and [Introducing Role Queue](https://news.blizzard.com/en-us/overwatch/23060961/introducing-role-queue). The three verb-led role definitions.
<a id="s7"></a>7. **Supercell — "Archetype Deck Challenge"** (Clash Royale team, 8 Jun 2018), [supercell.com](https://supercell.com/en/games/clashroyale/blog/fun/archetype-deck-challenge/). *"support him by dropping Ice Golem or Ice Spirit in front (before deploying Hog Rider)"*; *"'tanking' with Lava Hound, using Balloon as your win condition, and supporting the attack with Minions"*; *"add supporting troops behind him"*. The developer's own use of tank / support / win condition / swarm.
<a id="s8"></a>8. **Supercell — "3 Tips for the Mirror Challenge"** (15 Jul 2021), [supercell.com](https://supercell.com/en/games/clashroyale/blog/fun/3-tips-for-the-mirror-challenge/). *"same deck, same starting hand, what makes the differences are your decisions"*; *"A win condition is a card designed to inflict severe damage to the towers."*
<a id="s9"></a>9. **Supercell — Clash of Clans**: [Builder Base 2: Balancing Attacking, Defending and Builders](https://supercell.com/en/games/clashofclans/blog/news/builder-base-2-balancing-attacking-defending-and-builders-2/) (Stuart, Game Lead, 10 Mar 2023) — *"attacking well gives you Builder Gold to build a stronger defense and defending well gives you Builder Elixir to build a stronger offense"*, and the admission *"instead of being annoyed to take a defense like in the Home Village"*. Also [TH16 patch notes](https://supercell.com/en/games/clashofclans/blog/release-notes/full-patch-notes-th16) — *"Root Rider is a new Elixir troop available at Town Hall 15 when you upgrade your Barracks to Level 17"* — and [Builder Base 2: More Heroic Troops](https://supercell.com/en/games/clashofclans/blog/news/builder-base-2-more-heroic-troops-2/) on *"fewer but stronger troops… less spammy and encourage more thoughtful troop placement."*
<a id="s10"></a>10. **Ninja Kiwi — Bloons TD Battles 2 product page**, [ninjakiwi.com](https://ninjakiwi.com/Games/Mobile/Bloons-TD-Battles-2.html). *"Brand new Bloon send system optimizes economy building and attacks"*; *"Balance the strength of your defenses while preparing a Bloon offensive that blitzes your opponent."* ⚠ This is the **only** substantive first-party description of the send system that could be retrieved; Ninja Kiwi's de facto patch-note channel for Battles 2 is Reddit, which was unreachable.
<a id="s11"></a>11. **Tower Wars** (SuperVillain Studios, 2012), [Steam store page](https://store.steampowered.com/app/214360/Tower_Wars/). *"Unlock and upgrade technologies to bolster the stats and functions of your units and towers!"* — a shared tech tree, not a gate. The direct commercial precedent for this pitch, and Part I's cautionary example.
<a id="s12"></a>12. **Lars Doucet — "Optimizing Tower Defense for Focus and Thinking"**, [fortressofdoors.com](https://www.fortressofdoors.com/optimizing-tower-defense-for-focus-and-thinking-defenders-quest/). *"There's a lot of reasons we opted for non-mazing, but the biggest was to limit the number of choices the player had to make at any given time."*
<a id="s13"></a>13. **"Offensive Push", verbatim text.** *"Next tier of Bloons becomes available to send 1 round earlier than normal."* — COBRA path 1 tier 4, $1,750, Bloons TD Battles (multiplatform). ⚠ **Community-transcribed**; the wiki page itself is flagged as needing expansion. The mechanic is corroborated independently by [[25]](#s25)'s carve-out. This is §3.2's precedent and it should be confirmed in-game or against a patch note before it is built on.
<a id="s14"></a>14. **COBRA (Blooncyclopedia)**, [bloonswiki.com/COBRA](https://www.bloonswiki.com/COBRA) — *"use[s] pistols to pop Bloons and espionage tactics on either Bloons or enemy players"*; *"attack power, while never missing a shot, is extremely poor"*; reworked into the hero Agent Jericho in Battles 2. **Community.**
<a id="s15"></a>15. **Wikipedia — Legion TD**, [en.wikipedia.org/wiki/Legion_TD](https://en.wikipedia.org/wiki/Legion_TD). **Tertiary.** *"spend gold to deploy soldiers to defend an immobile king; and to spend lumber to deploy soldiers to attack the enemy king"*; *"Hiring wisps with gold increases the rate of lumber acquisition, while spending lumber also increases the rate of gold acquisition."* Note that "mythium" is the *Legion TD 2* rename of lumber and should not be retro-applied to the Warcraft 3 map.
<a id="s16"></a>16. **Legion TD 2 Wiki (wiki.gg)** — [Mercenary](https://legiontd2.wiki.gg/wiki/Mercenary), [Income](https://legiontd2.wiki.gg/wiki/Income), [Mythium](https://legiontd2.wiki.gg/wiki/Mythium), [Snail](https://legiontd2.wiki.gg/wiki/Snail), [Lock-In](https://legiontd2.wiki.gg/wiki/Lock-In), [Chaos](https://legiontd2.wiki.gg/wiki/Chaos). **Community-maintained** — its own footer states pages predating October 2022 are adapted from the Fandom wiki. Source of: *"Many mercenaries are referred to as power mercs, meaning they are stronger in some way, but give reduced income"*; *"Your team should only send power mercs if you think you can break your opponents on that wave"*; *"Mercenaries hired during the build phase will attack your opponent as soon as the battle phase begins"*; Snail at 20 mythium / 6 income; and the roll — *"The remainder of your roll is selected semi-randomly, though will be guaranteed to include a range of fighters from different price points, and also cover all of the attack and defence types."* **No unlock condition is documented for any mercenary** — see §7 item 4.
<a id="s17"></a>17. **11 bit studios — Anomaly: Warzone Earth**: [Steam store page](https://store.steampowered.com/app/91200/Anomaly_Warzone_Earth/), the [studio page](https://11bitstudios.com/games/anomaly-warzone-earth/), and Paweł Miechowski's [Gamasutra postmortem](https://www.gamedeveloper.com/business/postmortem-11-bit-studios-i-anomaly-warzone-earth-i-). All three name *route* and *squad composition*; **none mentions convoy ordering**. Used in §2.3 as negative evidence.
<a id="s18"></a>18. **Anomaly press and community.** Destructoid (Maurice Tan, 9.0), [destructoid.com](https://www.destructoid.com/reviews/review-anomaly-warzone-earth/): *"Rocket units have a long range but are fragile, so you don't want them at the front unless you're continuously going to micromanage the unit order in your column (which you can do instantly by pausing the game)."* bit-tech (David Hing, 80): *"your decisions over unit selection and order really matter."* Steam review listings for [Anomaly](https://steamcommunity.com/app/91200/reviews/?browsefilter=toprated) and [Anomaly 2](https://steamcommunity.com/app/236730/reviews/?browsefilter=toprated) for the "solved once" and "micromanage" complaints. ⚠ **Press and community sentiment**, and the Anomaly 2 quotes were extracted from a listing page rather than attributed to named reviewers.
<a id="s19"></a>19. **Super Auto Pets Wiki (wiki.gg)** — [The Basics](https://superautopets.wiki.gg/wiki/The_Basics), [Kangaroo](https://superautopets.wiki.gg/wiki/Kangaroo), [Camel](https://superautopets.wiki.gg/wiki/Camel), [Flamingo](https://superautopets.wiki.gg/wiki/Flamingo), [Whale](https://superautopets.wiki.gg/wiki/Whale), [Emu](https://superautopets.wiki.gg/wiki/Emu), [List of Strategies](https://superautopets.wiki.gg/wiki/List_of_Strategies). **Community** (its footer likewise notes adaptation from Fandom). Source of *"Pets attack in a right-to-left order, with the player's rightmost pet attacking the opponent's leftmost pet first"* and the four position-dependent ability shapes: *friend ahead attacks*, *nearest friend behind*, *faint → two nearest friends behind*, and push/pull effects that rewrite the opponent's order mid-combat. ⚠ Note the orientation: the "front" pet is the *rightmost* on screen.
<a id="s20"></a>20. **Super Auto Pets — developer reply on trigger order**, Steam Community, [thread 3086646248547568129](https://steamcommunity.com/app/1714040/discussions/0/3086646248547568129). Team Wood's Lau: *"the ordering depends on attack power, but it sounds like it isn't clear how it works, so thank you for the feedback!"* The load-bearing citation for §2.3's Trap 3.
<a id="s21"></a>21. **Backpack Battles** — [presskit](https://playwithfurcifer.github.io/backpack-battles-presskit/): *"It matters what you buy, and especially how you place it!"* Developer inspiration quotes (*"Our biggest inspiration from the start was Super Auto Pets"*) via [GameDiscoverCo](https://newsletter.gamediscover.co/p/how-backpack-battles-sold-650k-copies). **The Bazaar** and Backpack Battles mechanics from their wiki.gg wikis (community).
<a id="s22"></a>22. **StarCraft II tug-of-war maps.** Direct Strike unit unlocks by paid tech tier, and bunkers/cannons/turrets as a separate defensive category: community guide at [log.havrlant.cz](https://log.havrlant.cz/starcraft-direct-strike/) (**personal blog**), corroborated by a community guide on Blizzard's official forum host, [us.forums.blizzard.com/en/sc2/t/direct-strike-standard-guide/1233](https://us.forums.blizzard.com/en/sc2/t/direct-strike-standard-guide/1233). Desert Strike tier gating: [eu.forums.blizzard.com/en/sc2/t/desert-strike-1338-guide/1814](https://eu.forums.blizzard.com/en/sc2/t/desert-strike-1338-guide/1814) (**forum, community-authored, official host**).
<a id="s23"></a>23. **Nexus Wars** — the map author's own in-listing instructions on the SC2 Arcade database, [sc2arcade.com/map/2/212445](https://sc2arcade.com/map/2/212445). *"Each player builds structures that automatically spawn attacking units"*; *"Pylons give more income than buildings, but do not produce any units"*; *"Building cannons can be useful to hold off pushes."* **Primary-ish** — author-written, third-party host.
<a id="s24"></a>24. **Bloons Wiki (Fandom)** — [Bloons TD Battles 2](https://bloons.fandom.com/wiki/Bloons_TD_Battles_2), [Eco (BTDB2)](https://bloons.fandom.com/wiki/Eco_(BTDB2)), [Bloon Sends (BTDB2)](https://bloons.fandom.com/wiki/Bloon_Sends_(BTDB2)), [Bloon Sends (BTDB1)](https://bloons.fandom.com/wiki/Bloon_Sends_(BTDB1)). **Community.** Source of: *"Money is the primary currency… It is used to buy towers, upgrades, and Bloon sends"*; *"Both players start with $650 and $250 Eco"*; *"Eco is the rate of income gained every 6 seconds"*; the per-send cost / eco-change / break-even / first-and-last-round-available table; the Regrow ×1.6 (R8), Camo ×2 (R12), Fortified ×2 (R18) modifier gates; and Agent Jericho reducing modifier **cost**. Fandom blocks automated fetching (HTTP 402); these pages were read through the `r.jina.ai` text proxy at the same URLs.
<a id="s25"></a>25. **Blooncyclopedia (bloonswiki.com)** — [Bloon send](https://www.bloonswiki.com/Bloon_send), [Cash](https://www.bloonswiki.com/Cash), [Offensive Push](https://www.bloonswiki.com/Offensive_Push), [COBRA](https://www.bloonswiki.com/COBRA). **A second, independent community wiki.** Source of: *"Once sent, Bloon sends are deployed one at a time in order of earliness"*; the 6/5/6 queue-slot counts; *"layering"*; *"Once unlocked, a type of Bloon send unlocks permanently"* (Battles 1) versus the rolling ≤10-send menu (Battles 2); the carve-out that modifiers *"cannot be unlocked on an earlier round even with COBRA's Offensive Push"*; and — decisively for §1.1 — *"In Bloons TD Battles and Bloons TD Battles 2, cash is not normally gained by popping Bloons or at the end of each round."*
<a id="s26"></a>26. **ENT Gaming wiki — Legion TD guides**, [wiki.entgaming.net](http://wiki.entgaming.net/index.php?title=EntGaming%3ALTDGuides). **Community wiki of a Warcraft 3 hosting-bot community**; the most detailed surviving documentation of the original map's send system. *"Mercenaries require Lumber, between 20 and 1500 and not all of them are available from the start"*; *"The following units get unlocked after Level 10. They are available as soon as the Level 10 timer hits 0"*; a third barracks at Level 15; *"Most of them award Income in a 20/1 relation."* ⚠ The page does not pin which map version it documents; ENT hosted several Legion TD Mega variants. Direct fetching is blocked; read via the `r.jina.ai` proxy.
<a id="s27"></a>27. **Wintermaul Wars** — the Wintermaul Wars League guide at [wmwl.weebly.com/wmw-guide.html](http://wmwl.weebly.com/wmw-guide.html) (**community guide site**) for the Shrine tiers gating stronger sends, and [forum.wc3edit.net thread 3338](https://forum.wc3edit.net/viewtopic.php?t=3338) (**forum**) for the basic format. ⚠ Neither states whether the Shrine is defensive. A modern blog asserting that Wintermaul Wars sends grant income was found, judged low-trust, and is **not** relied on here.
<a id="s28"></a>28. **Clash of Clans Wiki (Fandom)** — [Army Camp](https://clashofclans.fandom.com/wiki/Army_Camp/Home_Village), [Barracks](https://clashofclans.fandom.com/wiki/Barracks). **Community.** Quotes the in-game text: *"Upgrade the Barracks to unlock advanced units"*, and — the decisive line for §3.3 — *"Unlike Clan Castle troops, troops stationed in the Army Camp do not defend your village during an attack… Its destruction will not affect your army in any way."*
<a id="s29"></a>29. **Clash Royale Wiki (Fandom)** — [Basics of Battle](https://clashroyale.fandom.com/wiki/Basics_of_Battle), [Elixir](https://clashroyale.fandom.com/wiki/Elixir). **Community.** The 8-card deck, the 4-card cycle, 1 elixir per 2.8 s, and the defender's structural elixir advantage. Supercell publishes no equivalent page.
<a id="s30"></a>30. **`content/wave.txt`, `content/units.txt`, `content/defense.txt`, `content/map.txt`, `content/landmarks.txt`.** The wave is already an ordered list of `(tick, type, count, corridor)` orders, asserted in that order rather than sorted. Load-bearing comments quoted in §2: *"A COUNT IS A COLUMN, NOT A PILE… units of one type share a speed and a route, so ten of them released together are one stack forever, and a stack is the single arrangement in which unit ordering cannot be observed at all"*; *"The two creep types differ in SPEED AND MAXIMUM HP ONLY, so that a later fast group catches an earlier slow group and unit ordering stops being theoretical"*; and the targeting tie-break, *"pick the creep furthest along, and the lower id if two are equal."* The map is 47 corridor cells, one hex wide, never branching. `landmarks.txt` records the first overtake at tick 366 of a 1,852-tick match at 30 ticks a second.
<a id="s31"></a>31. **Wikipedia** — [Dungeon Keeper](https://en.wikipedia.org/wiki/Dungeon_Keeper) (*"Which creatures enter the dungeon depends on which rooms the player has and how large they are; most creatures have prerequisites for entering service"*), [Orcs Must Die! Unchained](https://en.wikipedia.org/wiki/Orcs_Must_Die!_Unchained) (traps and minion cards sharing deck slots), [Prismata](https://en.wikipedia.org/wiki/Prismata) (one shared tech tree; units that *"attack, block, produce gold or other resources"*). **Tertiary.** Lunarch Studios' design blog, the obvious primary source on Prismata, is offline with an expired certificate.
<a id="s32"></a>32. **Element TD 2** — the [Steam store page](https://store.steampowered.com/app/1018830/Element_TD_2__Tower_Defense/) (developer copy, describing competitive play purely as outlasting, never as sending) and [eletd2.fandom.com game modes](https://eletd2.fandom.com/wiki/Game_Modes) (**community**) for War / Team War being a shared-lane race in which creeps auto-spawn. **Element TD 2 has no send system**, which is why it appears in this note only as a counter-example.
<a id="s33"></a>33. **League of Legends champion classes** — the [official wiki taxonomy](https://wiki.leagueoflegends.com/en-us/Champion_classes) (Controller, Fighter, Mage, Marksman, Slayer, Tank, **Specialist**). ⚠ Statikk's original *Dev Blog: Classes & Subclasses* (April 2016) is **offline** — `boards.na.leagueoflegends.com` no longer resolves and the current URL 404s. The quotes *"Create a shared vocabulary!"* and *"more of a set of guidelines than rigid rules"* are from a [community mirror](https://www.surrenderat20.net/2016/04/red-post-collection-dev-blog-on-classes.html). Treat as reconstructed, not primary.
<a id="s34"></a>34. **Valve — "Illustrative Rendering in Team Fortress 2"** (Mitchell, Francke, Eng; NPAR 2007). *"the silhouettes of the nine classes were carefully designed to be very distinct from one another"*; *"Even when viewed only in silhouette with no internal shading at all, the characters are readily identifiable to players."* ⚠ The canonical Valve PDF returned as unparsed binary; the text was read from a re-host. Cite the paper; verify the wording against the PDF before quoting it in a shipped document.
<a id="s35"></a>35. **Riot Games — "Clarity in League"**, [leagueoflegends.com](https://www.leagueoflegends.com/en-us/news/dev/clarity-in-league/). *"Silhouettes are the single most important thing for champion recognition in League."*
<a id="s36"></a>36. **Blizzard, GDC 2016 — "Overwatch: The Elusive Goal: Play by Sound"** (Scott Lawlor, Tomas Neumann), [GDC Vault](https://gdcvault.com/play/1023317/Overwatch-The-Elusive-Goal-Play). The recognition hierarchy and the *"Minimal variation in sound and VO"* rule are from an archived slide transcript, not from the talk itself — **secondary host of primary slides**.
