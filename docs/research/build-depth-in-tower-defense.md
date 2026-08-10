# Where combinatorial build depth comes from, and which mechanisms survive a one-hex corridor

**Research note** · 3 August 2026 · commissioned by
[Open questions](../open-questions.md); input to
[seam 1 — the match format](../build-order.md#1--the-match-format),
[seam 3 — the roster](../build-order.md#3--the-roster) and [seam 7 — the interface](../build-order.md#7--the-interface)

> ⚠️ **The premise moved on 6 August 2026. The findings stand; the geometry does not.**
> This note was written against a corridor exactly one hex wide that never branched, and it asks which
> mechanisms survive it. That corridor was withdrawn —
> [the board is a maze again](../vision.md#the-board-is-a-maze), at several
> elevation levels. Read the survival verdicts as a *floor* rather than a filter: the one mechanism of eleven
> the corridor killed is alive again, and nothing the note ruled *in* is affected. Its headline finding — that
> only the generative route is simultaneously a depth mechanism, an accessibility mechanism and enumerable by
> the harness — never depended on the geometry at all.

**Question:** how do tower defense games manufacture extreme, combinatorial *build* depth; which of those
mechanisms survive this project's settled decisions; and how do the games that are both deep *and* accessible
pull it off without an unlock ramp?
**Inputs:** [The Vision](../vision.md) §3 ([Depth is the point](../vision.md#depth-is-the-point)),
§4, §5, §6 ([Juicy, and readable by a stranger](../vision.md#legible-to-a-stranger)), §11;
[Part V — Tower & Creep Variance Levers](../archive/variance-levers-and-unit-schema.md) §3.11, §4.1, §10.
**The developer's stated anchor:** Element TD (Karawasa, Warcraft 3, 2006) and its successor Element TD 2.

> **This note does not choose.** [§7](#7-three-directions-ranked) offers three directions, ranked, each with its
> trade-off named. The pick is the developer's, and it belongs to seam 1.

> **Where it stops.** Two of §3's three depth commitments belong to the sibling note
> `docs/research/attack-composition-and-sending.md` and are deliberately not answered here: *your defense decides
> your offense* (gating the creep pool on tower choices) and *you choose the order they come out in* (a wave as a
> sequence). This note touches the first only where a mechanism happens to deliver it for free — see
> [§7, Direction A](#direction-a--the-generative-roster--recommended-first) — and hands it over there.
> `docs/research/towers-versus-placed-squads.md` owns the silhouette question; nothing here depends on its answer,
> because every mechanism below is indifferent to whether a placement is drawn as a tower or a squad.

---

## Bottom line

### Depth is manufactured two structurally different ways — by a *generative rule* that mints a large roster from a small vocabulary, or by a *large authored pool* metered out by a random offering. Only the first is also an accessibility mechanism, and only the first leaves a balance surface a sweep harness can enumerate.

Four claims carry the note.

**One. Element TD's depth is not the combinations. It is the picks.** The combination table is what makes 56
towers *memorable*; the eleven metered picks are what make a run a decision. Element TD 2 gives one pick at the
start and one every five waves to wave 50 — and every pick after the first **summons an elemental boss you must
kill before the element unlocks** [[9]](#s9)[[10]](#s10). The tech choice is paid for inside the simulation, not
in a menu. That single mechanism is the most transferable thing in this whole survey and it is almost never the
thing people cite about the game.

**Two. Depth and computed balance are the same axis pointing opposite ways.** Every mechanism below manufactures
depth by making one unit's value depend on the other units you chose. That dependence is precisely what stops
[the harness](../vision.md#5-how-it-is-balanced) pricing a unit in isolation. "Balance is computed" is therefore
not a licence for unlimited interaction — it is a **budget**, and the taxonomy is a menu of ways to spend it.
The mechanisms differ not in *whether* they create dependence but in **what the dependence is indexed by**, and
that index has a cardinality you can write down. [§6](#6-how-big-the-balance-surface-actually-gets) writes them
down.

**Three. The hex corridor kills far less than it looks like it kills, and "nothing persists" kills more.** Exactly
**one** row of the eleven-row taxonomy dies on the corridor — geometry — and it takes six named levers down with it
([§5.1](#51-what-the-corridor-actually-kills-stated-once)). Everything else is indifferent to the playfield, and
one mechanism is made *cheaper* by it. Two shipped games settle the question rather than arguing it:
**Mazebert TD has a fixed path** and derives all of its depth from card draws, item stacking and synergy
[[25]](#s25); **Legion TD 2 has one lane per player and, by its own documentation, "generally no walls that block
a unit from where it wants to go"** [[26]](#s26). The vision's structural north star is already almost a corridor
game. See [§5.2](#52-the-good-news-and-it-corrects-a-claim-i-made-earlier-in-this-note) — it corrects a claim made
earlier in this note's own drafting. What
[§4 — what persists](../vision.md#4-what-persists) removes is not depth at all; it is the *onboarding ramp*. The
replacement is shipped and well-tested: **move the disclosure ramp inside the run.** Element TD 2 meters eleven
picks across fifty waves; Super Auto Pets unlocks shop tier *X* on turn 2*X*−1 [[14]](#s14); Legion TD 2 shows
you ten of its forty-eight draftable fighters [[5]](#s5)[[27]](#s27); YouTD makes you *buy* the width of the
distribution your offers are drawn from [[7]](#s7). Each is progressive disclosure that resets at the start of
every run — which is exactly the shape §4 permits.

**Four. Bloons TD 6 is the existence proof that meta-progression is not what carries depth.** Its hardest mode,
CHIMPS, disables Monkey Knowledge by rule — the acronym expands to include it — and Ninja Kiwi's *own* Open Data
API exposes a per-event **`disableMK`** switch that boss events set to `true` [[15]](#s15). BTD6's answer to
"meta-progression contaminates skill expression" is a **switchboard**, not a global choice: it keeps the drip for
newcomers and ships first-class modes that turn it off. The vision has flipped the same switch permanently. That
is a narrower decision than BTD6's, not a different one — and the mass-market accessible-and-deep tower defense
already runs, at its top end, in exactly the configuration this project has chosen for everything.

---

## 1. Wide is not deep

Three different quantities get called "depth" and conflating them is how a roster ends up big and boring.

| Quantity | What it measures | Who maxes it |
|---|---|---|
| **Width** | How many distinct things exist | YouTD 2 — **235 tower families over 690 tower/tier rows**, 315 items, 21 builders, counted from the shipped CSVs [[7]](#s7) |
| **Reachable configurations** | How many distinct legal builds the rules admit | Bloons TD 6 — 25 towers × 64 legal upgrade states = **1,600 tower configurations** from 375 authored upgrades [[6]](#s6). Legion TD 2 markets its own version of this number: *"12 million possible combinations"*, which is C(48,6) [[27]](#s27) |
| **Interaction degree** | How much one piece's value depends on the other pieces you chose | Mazebert TD, Legion TD 2 |

Width alone is a catalogue. **Depth is width × interaction × opportunity cost**, and the third term is the one
most designs forget: a roster where you can eventually have everything has no build in it, only an order.
Element TD 2's eleven picks against six elements, Bloons' crosspath cap, and Legion TD 2's roll of six are all
the same move — *deliberately withholding most of the roster from any one run*.

So every mechanism below is graded on four questions:

1. **Leverage** — reachable configurations per authored thing.
2. **Interaction degree** — what does a piece's value depend on?
3. **Learnability** — how much must a player memorise to predict what exists?
4. **Harness cost** — the cardinality of the thing a unit's value is indexed by ([§6](#6-how-big-the-balance-surface-actually-gets)).

---

## 2. The anchor, measured: what Element TD actually does

Worth doing precisely, because the developer named it and because **Part V's account of it is slightly wrong**.

### 2.1 The roster is combinatorially complete — with one hole

Element TD 2 has six elements — Light, Darkness, Water, Fire, Nature, Earth — plus a non-elemental
**Composite** type [[2]](#s2)[[11]](#s11). Towers exist for element subsets of size 1, 2, 3 and 4:

| Subset size | Count | C(6,k) | Present? |
|---|---|---|---|
| 1 (single) | 6 | 6 | yes |
| 2 (dual) | 15 | 15 | yes |
| 3 (triple) | 20 | 20 | yes |
| 4 (quad) | 15 | 15 | yes — added in **v1.4, 12 Dec 2021** [[3]](#s3) |
| 5 | — | 6 | **absent** |
| 6 | 1 | 1 | yes — the **Periodic Tower**, requires all six elements *and* an Essence [[11]](#s11) |

6 + 15 + 20 + 15 + 1 = **57 combination towers**, plus two always-available basics — **Arrow** and **Cannon**,
both dealing Composite damage [[11]](#s11) — for **59**, which is exactly the number on the first-party store page
and the official site: *"6 elements that combine to create 59 unique towers"* [[1]](#s1)[[2]](#s2).

> **Correction to [Part V §3.11](../archive/variance-levers-and-unit-schema.md#311-economy-and-upgrade-topology).** Part V
> says *"six singles, fifteen duals, twenty triples, fifteen quads — plus one for the full set. Fifty-seven
> towers, zero of them arbitrary."* The arithmetic is right and the framing is off by two: the shipped roster is
> **59**, because Arrow and Cannon sit outside the combination scheme entirely. And it is **not** combinatorially
> complete — there is no five-element tower, so six of the sixty-four subsets have no unit. Part V's later
> figure of "59 towers" in §6 is the correct one. I could find no first-party statement explaining the missing
> quintuple tier. ⚠️ Unverified speculation, flagged as such: with only eleven picks and quads costing four
> elements, a quint tier would be reachable but would sit one step from the Periodic and probably had nowhere
> distinct to stand.

### 2.2 The pick schedule is the actual mechanism

This is the part worth stealing and it is not the combination table.

| Fact | Detail | Confidence |
|---|---|---|
| Number of picks | **11** — one at game start, then one every 5 waves through wave 50 | **High.** Two independent guides [[9]](#s9)[[10]](#s10), *and* the developer's own roadmap post says *"until all 11 Picks are selected"* [[18]](#s18). Unchanged since the Warcraft 3 original, which granted 11 lumber on the same schedule [[20]](#s20) |
| What a pick offers | An **element**, an **Interest** upgrade (+0.6% per 15 s on a 2% base), or **Essence** | High [[9]](#s9)[[11]](#s11) |
| Gating | Every element pick after the first **spawns a boss of that element; the element does not unlock until you kill it.** Picking Essence summons a Composite-armor boss and pays 2 Essence for killing it | High — the boss gate is community-stated [[9]](#s9); the Essence boss is **first-party** [[18]](#s18) |
| **Elements have levels 1–3** | A pick can *deepen* an element instead of adding a new one, and element level gates the tower level you may build. War Mode's rule, first-party: *"you can't get **Level 2 Elements** until the 4th Pick, **Level 3 Elements** until the 8th Pick, and **Essence Upgrade** until the 10th Pick"* | **High that levels exist** — first-party patch notes reference them repeatedly [[18]](#s18), and the WC3 original states the rule outright (*"Elements can get as strong as level 3 … to summon the higher levels, you need to have flattened the lower ones"*) [[20]](#s20). ⚠️ **Medium on the exact level→tier-level mapping** — no first-party sentence states it |
| Upgrade graph | single → dual → triple → quad, and **tier-up is only available from level 1** | High [[3]](#s3)[[11]](#s11)[[13]](#s13) |
| Level caps by tier | single **4**, dual **3**, triple **2**, quad **1** (no upgrades) | Medium-high — community wiki [[11]](#s11) |
| Cost ladder | Quads **4,000 gold** from scratch, **2,500** upgrading from a triple [[3]](#s3). Arrow 75; single 175/675/2750/15000; dual 500/1300/3300; triple 1500/5000; Periodic 15000 + 1 Essence | Medium — the quad numbers are first-party; the rest is community wiki [[11]](#s11) |
| Damage cycle | Six-element ring — Light → Darkness → Water → Fire → Nature → Earth → Light — each dealing **200% to the next, 50% to the previous**; Composite deals 100% to everything and **takes 90%**; post-wave-55 boss armor **takes 10% from anything** | **High.** Community wiki for ETD2 [[11]](#s11), and the *official* archived help page for the Warcraft 3 original publishes the identical matrix [[19]](#s19) — the ring has been stable for eighteen years |

**The pick budget is a grid, not a set — and that is the mechanism.** This is the finding that most changes how Element
TD should be read. Eleven picks are not spent on "which of six elements do I own"; they are spent across a
**6 × 3 breadth-by-depth grid**, plus Interest, plus Essence. Six elements at level 3 would cost eighteen picks
against a budget of eleven, so the budget is *structurally* insufficient — you cannot go wide and deep, and the
whole game is where you sit on that trade. A flat "pick 6 of N" scheme has one axis; this has two, out of the same
eleven decisions and with no extra content.

**The tier/level trade is the second half of it.** Because tier-up is only available from a level-1 tower, gold
spent climbing levels is gold that cannot be converted into a higher tier, and the level cap *falls* as the tier
*rises* (4 → 3 → 2 → 1). A quad is the widest and shallowest thing you can own. Two orthogonal scarcities — picks
and gold — each with a breadth-versus-depth trade inside it, and they interact. That is a great deal of decision
surface for one recipe table and one predicate.

Three consequences land directly on this project.

**The pick menu couples the tech axis to the purse.** Putting *Interest* on the same menu as *Fire* means the
tech choice and the money choice are the same choice. That matters enormously here, because
[§3 — one purse](../vision.md#one-purse) chose a single currency specifically so each build phase is *one sharp
decision*. A pick schedule is not a second currency — nothing is bought with it, and it cannot be saved into or
out of gold — but it *is* a second decision axis, and it would be a violation of §3's spirit if the two axes
never met. Element TD 2's answer is to make them meet on the menu. Take the mechanism and the fix together or
neither.

**The gate is a boss, not a paywall.** "You picked Fire, now beat the Fire boss" converts an unlock into a
*wave*, which means it costs gold, costs board space, and can be failed. That is an unlock that survives §4
intact, because nothing persists past the run — and it is a far better answer than a timer.

**The pick is public.** In a two-boards-at-once format, the elements an opponent has taken are readable off
their defense, and eleven picks over fifty waves is eleven readable commitments. That is free feed for
[seam 6's social layer](../build-order.md#6--the-social-layer) and free feed for the counter-reading half of the loop.

### 2.3 The Warcraft 3 original — better sourced than expected, via the archive

The developer's stated anchor is the 2006 Warcraft 3 mod, and it turns out to be **primary-sourceable after all**:
Karawasa's own site `eletd.com` is in the Wayback Machine with its content pages intact. (`elementtd.com` was
domain-squatted and its archive is empty; `eletd.com` is the one that matters.)

The site's own navigation, on every page circa the 4.0 public beta, states the content surface outright
[[19]](#s19):

> *"View All **6 Elemental Towers** / View All **15 Dual Towers** / View All **20 Triple Towers** / View All
> **22 Build Lists** / View All **60 Creep Waves**"*

So the Warcraft 3 original had **singles, duals and triples only — no quads and no four-element towers at all**.
Quads are a 2021 addition, and the developer says so in the v1.4 announcement: *"It's been 16 years since Element
TD was originally released back in WarCraft III. … In all that time, some towers were changed out, but **we never
truly added new ones.** Until now."* [[18]](#s18)

**The single best primary artefact in this entire survey is that "22 Build Lists" page** [[20]](#s20). It
enumerates every element combination a player can end a game with and, for each, exactly which towers it unlocks:

| Build size | Count | C(6,k) | Each unlocks |
|---|---|---|---|
| 4 elements | 15 | C(6,4) = 15 | 6 duals + 4 triples = C(4,2) + C(4,3) |
| 5 elements | 6 | C(6,5) = 6 | 10 duals + 10 triples = C(5,2) + C(5,3) |
| 6 elements | 1 | C(6,6) = 1 | 15 duals + 20 triples |
| **Total** | **22** | | |

That is the subset lattice published as a content manifest, in 2009, by the designer. It is the clearest possible
statement of what "combinatorial roster" means as a shipped design decision, and it is worth reading before
committing to Direction A.

Two more facts from the same archive, both about the pick system and both confirming that today's Element TD 2 is
the same machine [[20]](#s20):

- Picks were **lumber**, granted one every five waves, *"That's 11 lumber in total"* — the identical budget.
- Elements levelled to **3**, strictly in order: *"to summon the higher levels, you need to have flattened the
  lower ones."* And *"You need level three in an element to build the pure tower of that kind"* — the explicit
  element-level → tower-level gate that Element TD 2 never states in so many words.

⚠️ **Sourcing grade.** The navigation, the element table and the tower ladders are the **official site's own
pages**. The FAQ and the build lists were written by players (`Cisz`, `holepercent`) and **published on the
official site** — that is stronger than a forum post and weaker than a designer's statement; treat it as
semi-official. The intermediate Dota 2 port is stronger still: its full source is public [[21]](#s21) and the
directory listing confirms 6 singles × 4 levels, exactly 15 duals × 3, exactly 20 triples × 3, **no quads
directory**, and the same `AllPick` / `AllRandom` / `SameRandom` mode taxonomy Element TD 2 inherited.

### 2.4 The failure mode, documented first-party across six years of patches

**This is the most decision-relevant thing in the note and it is not in any design write-up of the game.** Element
TD 2's balance history is a running, public fight over *how many elements a build should have*, and the
combination scheme keeps producing a **U-shaped meta** — the extremes dominate and the middle needs propping up.
All first-party [[18]](#s18):

| Patch | What the developer said |
|---|---|
| **0.51** | *"Bug Fixes and **Less 6-Element Randoms**"* — the random-pick modes were producing *"6-Element every single game"* |
| **0.61** | *"expect the silly **2 Element & 6 Element** builds to no longer automatically dominate, and more love for **4 & 5 Element** builds"* |
| **0.63** | *"certain builds have been absolutely dominating … In particular, **2 Element and 6 Element Builds**. We don't mind them being viable options, but they're **strictly better than everything else** right now"* |
| **1.4** | Quads added, explicitly to *"Fill out gaps in their associated 4 Element Builds"* and *"Expand build orders"* — and pointedly **not** for triples: *"Except 3 Element Builds. They've got enough love already."* |
| **1.9.4** | *"**6 Element has remained a dominant build** due to the potency of stacking all their support towers (Triples & Quads), as well as Periodic being exceedingly powerful … **This contributed heavily to 6 Element dominance.** As a result, we've severely reduced their damage"* |

**The mechanism of the failure is structural, not a mispriced number.** A combinatorial roster pays breadth a
*superlinear* return: the sixth element does not add one tower, it adds five duals, ten triples and ten quads —
C(6,k) − C(5,k) summed over k. Meanwhile depth pays at best linearly. So the widest build is the natural power
peak unless something actively taxes it, and the middle of the distribution is where nobody wants to be.

**And it is not one game's problem.** GemCraft reached the same place independently and answered it by
**removing content**: *GemCraft — Chasing Shadows* (2015) advertises *"Craft and combine **nine gem types**"*;
*GemCraft — Frostborn Wrath* (2020) advertises *"Craft and combine **six types of gems**"* [[22]](#s22). Its
skill system had the same superlinear leak — a single skill that *"provides a small bonus to pure gem damage
every level, and a large bonus to dual, triple and even quad gems every few levels, **with the latter bonus
eventually outstripping the former**"* — and the community's own read of the sequel is that pure gems are finally
competitive again [[23]](#s23). **Two independent studios, the same failure, one nerfing breadth for six years and
one cutting its palette by a third.**

> **What this means for [§5 — balance is computed](../vision.md#5-how-it-is-balanced).** This failure is *exactly*
> the kind a sweep harness catches, because it is a systematic win-rate gradient along one axis (build breadth)
> rather than a single red cell. A harness that reports win rate binned by *number of ingredients taken* would
> have found it in one overnight run. **If Direction A is chosen, that bin is the harness's first required
> report** — not a nice-to-have, because the two shipped games that took this route both spent years discovering
> it by hand.

---

## 3. The taxonomy — eleven mechanisms

Each row is a structurally distinct way to manufacture combinatorial depth. "Dependence index" is what a unit's
value is a function of — the thing [§6](#6-how-big-the-balance-surface-actually-gets) sizes.

| # | Mechanism | Shipped in | What it multiplies | Dependence index | Content cost | Learnability |
|---|---|---|---|---|---|---|
| **M1** | **Ingredient combination** — one unit per subset of a small vocabulary | Element TD / ETD2, Gem TD, GemCraft | Roster size, as C(n,k) | The unit's own recipe — **fixed at authoring** | One authored unit per subset. **No leverage.** | **Best in class** — 6 words predict 56 towers |
| **M2** | **Constrained upgrade topology** — N paths × M tiers plus a legality predicate | Bloons TD 6 | Configurations per tower | The tower's own config — **closed** | 375 authored upgrades → 1,600 configs. **4.3×** | Good — the menu shows all three paths at once |
| **M3** | **Randomised offering** — a small random hand drawn from a large pool | Legion TD 2, YouTD, Mazebert, Super Auto Pets | Runs-that-feel-different | **The hand** — C(pool, k) | Linear in pool, and the pool must be huge to matter | Poor — the pool is unlearnable by construction; the *hand* is learnable |
| **M4** | **Item / modifier layering** — equippable modifiers on built units | Mazebert TD, YouTD, GemCraft traits | Units × item subsets | **The item multiset** on that unit | Linear to author, **multiplicative to balance** | Poor |
| **M5** | **Adjacency & aura synergy** — a unit's output depends on neighbours | Mazebert, BTD6 Village, Rogue Tower, Sanctum | Layouts, not units | **The neighbourhood** — bounded by board topology | Cheap to author | Medium — needs a visual tell |
| **M6** | **Sacrifice / consumption** — built things are spent to make other things | BTD6 Paragons, GemCraft supergemming | End-state power ceilings | The consumed set | Cheap | Poor — famously opaque |
| **M7** | **Tiered exclusivity** — a metered, non-refundable scarcity that is not money | ETD2's 11 picks, BTD6's crosspath cap, LTD2's roll of 6 | **Opportunity cost**, which is the depth term everyone forgets | The pick history — a small ordered set | **Free. It is a predicate.** | Best in class |
| **M8** | **Geometry** — maze length, blocking, route choice, placement-vs-path | Gem TD, Legion TD 2, classic WC3 TDs | Layouts | The whole board's shape | Cheap to author, hard to balance | Medium |
| **M9** | **Economy shape** — interest, greed, income-vs-defense | ETD2's Interest pick, LTD2 workers/mythium, BTD6 farms | Decisions per build phase | Global board state over *time* | Free | Good, and it teaches itself |
| **M10** | **Counter-reading** — the type matrix and capability gates, against a *known* opponent | Legion TD 2, ETD2, BTD6's pop-gates | Decisions per build phase | The opponent's composition | Quadratic in type count | Good |
| **M11** | **In-run levelling** — one unit that starts at level 1 every run and grows during it | BTD6 Heroes (18 of them, level 1→20 *within a game*), Mazebert tower XP, Defender's Quest | Mid-run decisions, and a difficulty curve | **The run's own history** — a trajectory, not a set | Cheap: one unit, twenty rows | Good — a visible level number |

Plus one that must be named to be dismissed: **M12, meta-progression** — Monkey Knowledge's 100+ meta-upgrades
[[6]](#s6), Mazebert's card collection, YouTD's builders. It is the standard answer to both depth and
onboarding and it is ruled out by [§4](../vision.md#4-what-persists) and
[§11](../vision.md#8-out-of-scope). It gets no further consideration — except as evidence, because Ninja Kiwi's
own v25.0 notes say out loud that Monkey Knowledge is a **balance liability tied to account progression**:
Veteran Levels exist so that dedicated players *"keep earning XP … but **without unbalancing the Monkey Knowledge
system**"* [[17]](#s17). The company that ships the genre's most successful meta-progression system publicly
treats it as something to be contained. That is the strongest available endorsement of
[§4](../vision.md#4-what-persists) and it comes from the opposition.

### 3.1 M1 — ingredient combination buys learnability, not content

The most common misreading of Element TD is that the combination rule *generates* content. It does not. Fifteen
dual towers are fifteen hand-authored towers with fifteen sets of abilities; the recipe table saves the designer
nothing. What it saves is the **player's memory**, and that is a bigger prize than it sounds:

- The roster is **compressible**. Six words predict fifty-six towers. A player who has never seen the Nova tower
  can still say it is Light + Nature + Fire before reading a word of its tooltip.
- The roster is **complete**, so the absence of a tower is information rather than an oversight — which is why
  the missing quintuple tier (§2.1) is a genuine wart.
- The upgrade graph falls out for free: *a level-1 single upgrades into any dual containing it; a level-1 dual
  into any triple containing both.* That is one rule, not fifty-six edges — precisely
  [Part V §3.11's](../archive/variance-levers-and-unit-schema.md#311-economy-and-upgrade-topology) "an upgrade is an edge
  in a directed graph, guarded by a predicate."

The cost is an **authoring obligation**. Once Fire+Water exists, Fire+Earth must too, and the fifteenth dual gets
authored whether or not anyone had an idea for it — the rule decides how many units exist, not taste. Element TD 2
launched standalone in **February 2020** [[2]](#s2) and did not ship its fifteen quads until **December 2021**
[[3]](#s3): nearly two years, on a series that had been iterating on the same combination scheme since 2006. That
is what one tier of a completeness obligation costs a funded team.

**Gem TD and GemCraft are the same mechanism in two other shapes, and both are instructive.**

*Gem TD* (the Warcraft 3 map, and the Drodo Studio remake for Dota 2) runs **8 colours × 6 qualities = 48 base
gems**, on top of which sits a **three-input crafting DAG** of roughly 44 combined towers — `Silver = B1+Y1+D1`,
then `Silver Knight = Silver + Q2 + R3`, and so on up to apexes that need three sub-recipes each [[24]](#s24).
The design detail worth stealing is that **every apex has two routes**: the ingredient-graph route, and a
"straight flush" — one gem of each quality in a single colour (`P1+P2+P3+P4+P5` for Koh-i-noor Diamond,
`R1..R5` for The Crown Prince). One target, two completely different paths to it, without authoring a second
target. ⚠️ Gem TD is the weakest-sourced game here: its random-roll odds, its reroll rule and its keep-one-sell-the-rest
mazing loop **could not be verified at all**, and no product called "Gem TD+" could be found on Steam — treat that
name as unconfirmed. Since the mazing loop is dead here anyway (M8), the gap costs this note little.

*GemCraft* is the counter-example: a combination scheme with **no cap on how many components a gem carries**
(up to "prismatic", all of them), where mixing is paid for in **dilution** — *"a pure gem has a bonus to its
special, while a dual gem has a bonus to its damage and slightly reduced specials … gems with 4 or more colour
components never have any inherent bonuses"* [[23]](#s23). That is a *continuous* version of the breadth/depth
trade Element TD makes discrete, and §2.4 records that it leaked the same way and got the same treatment. Note
also the direction of travel on randomness: the first two GemCraft games let you *"only choose the grade of the
gem, the colour is random"*; from *Labyrinth* onward you choose both [[23]](#s23). Element TD 2 defaults to free
choice too. **Two combination games independently moved from a random offering toward free choice** — which is
one of the few pieces of evidence in this survey that bears directly on Direction C.

The **reroll/refeed** loop both games add on top is a different mechanism (M6) and it is the one that broke.
GemCraft's combine formula branches on grade gap: at a gap of two or more *"the higher of the two stats is left
as it is and a fraction of the smaller stat is added to it, which means you can keep increasing the stats of a
gem infinitely"* [[23]](#s23) — "supergemming", an emergent exploit rather than a feature, killed by changing
that branch in *Labyrinth*. Part V §3.11 already records this and the generalisation still holds: **if a merge's
output can equal or exceed its better input, the mechanic is an unbounded loop, and that is visible in the
coefficients before anything ships.**

### 3.2 M2 — cross-pathing is the highest leverage in the genre, and the leverage is in the *predicate*

Bloons TD 6 ships **25 Monkey Towers with 3 upgrade paths each** [[6]](#s6), five tiers per path. The legality
rule — community-documented, never published by Ninja Kiwi as a rules statement — is that **at most two paths may
be upgraded at all, and the secondary path is capped at tier 2** [[12]](#s12).

Counted out (my arithmetic, and it reproduces the community's independently-derived 64 for the Wizard Monkey
[[12]](#s12)):

| Shape | Count |
|---|---|
| `0-0-0` | 1 |
| Exactly one path used (3 paths × 5 tiers) | 15 |
| Exactly two paths used — for each of 3 path-pairs, the 25 combinations of tiers 1–5 minus the 9 where *both* exceed tier 2 | 3 × 16 = 48 |
| **Total legal configurations per tower** | **64** |
| Unconstrained (each path 0–5) | 216 |

**The predicate discards 70% of the space, and the 70% is the design.** Only 7 of a tower's 15 upgrades are ever
reachable on one instance (5 primary + 2 secondary), so the same tower placed twice with different paths is
genuinely two towers. Across the roster: 25 × 64 = **1,600 tower configurations from 375 authored upgrades**.

Two wrinkles a naive "upgrades are a tree" model cannot express, both already noted in Part V and both worth
re-flagging because they are what makes the system feel hand-made rather than generated: the secondary path's
bonus is **not uniform** across tiers, and **Paragons consume other towers as a cost**, breaking the topology
outright.

This is the cheapest depth in the survey per unit of authored content. It is also the depth that lives *inside a
tower* rather than *between towers* — which matters below.

### 3.3 M3 — randomised offering buys scarcity, and pays for it in variance

Legion TD 2 is the best-documented version, because it ships **ten different shapes of the same idea in one game**
and lets the player pick between them — see the playstyle table below, which is the most interesting thing in this
section. Ranked play is Mastermind-only, and Mastermind is not a legion at all: *"Mastermind is a special legion,
which has no fighters of its own. Instead, you draft a set of fighters (called a 'roll') from all legions"*
[[4]](#s4). The baseline is **10 offered, 6 kept**, with one reroll that *"swap[s] out up to 4 fighters"*
[[4]](#s4)[[26]](#s26).

Against **8 legions of 6 base fighters each — 48 draftable bases, 145 entries once upgrade forms are counted**
[[5]](#s5) — a Mastermind player ever sees ten. Precise corroboration comes from the store page's own
marketing arithmetic: *"Select fighters from each legion for **12 million possible combinations**"* [[27]](#s27),
and C(48,6) = **12,271,512** exactly. That is the entire trick, and the publisher counts it the same way:
**an offering converts a pool nobody could learn into a hand anybody can read, and the reachable-configuration
count is the pool choose the hand.**

YouTD is the extreme, and its modern remake is fully open source and CSV-driven, so the numbers are exact rather
than marketed: **235 distinct tower families across 690 tower/tier rows, 7 elements × 4 rarities, 315 items, 21
builders** — and, tellingly, **214 bespoke tower-behaviour scripts and 141 item-behaviour scripts**, so roughly
60% of the content is genuine code rather than stat reskins [[7]](#s7). That is the honest price of width.

Mazebert TD is the same shape with cards — **210 tower, item, potion and hero cards** (61/98/33/18, a count that
reconciles exactly between the official site's card pages and the open-sourced content arrays) [[8]](#s8)[[25]](#s25).
There is **no pre-run deck**: you choose a hero and **up to two elements**, which narrows the drop pool, then are
dealt 4 starting towers and **exactly one new tower card per round survived** [[25]](#s25).

The costs are three and they are all sharp:

1. **Variance.** [Part II §3](../archive/async-ghost-round-robin.md) built round-robin explicitly to control variance
   across ten opponents. A bad roll is variance *inside* a run, where there is nothing left to average.
   [Part V §10.2](../archive/variance-levers-and-unit-schema.md#10-levers-not-to-build) argues the same case against
   crit and evasion. A random offering is a much larger dose of the same medicine.
2. **The harness can only sample.** See [§6](#6-how-big-the-balance-surface-actually-gets).
3. **It fights "the reward is the build."** [The Vision §1](../vision.md#1-the-destination) says the point is the
   building. A hand you were dealt is a puzzle you solved, which is a different and lesser pleasure than a
   defense you designed. This is a taste judgement, stated as one.

**Determinism is *not* one of the costs.** A build-time offering is an *input*, fixed once submitted, so it lands
in the ghost record alongside everything else and replays exactly — provided it comes from a named RNG stream per
[Part V §5.4](../archive/variance-levers-and-unit-schema.md#54-determinism-constraints-the-schema-itself-must-carry). The
objection to M3 is competitive and aesthetic, never technical.

**Two pieces of evidence that cut against M3.**

*Randomness gets a mistake-refund attached, in two unrelated games.* Element TD 2's four pick modes are **Pick**
(free choice, the default), **Same Pick**, **All Random** and **Same Random** — and the two random modes **raise
the tower sell refund from 80% to 100%** [[11]](#s11). YouTD 2 does the mirror image: its non-random **Build**
mode refunds **0.5**, and both random modes refund **0.75** [[7]](#s7). Two independent designs concluded that
forced randomness must be paid for in the economy or it is punitive. If Direction C is taken, budget for the
equivalent.

*Both combination games moved away from random offerings over time* (§3.1). Two data points, not a law, and
neither is a competitive PvP game — but the arrow points one way.

**And three that cut for it, all found late and all changing the picture.**

*Legion TD 2 prices variance itself, and sells it as a choice.* Ranked play is Mastermind-only, and before the
roll you choose a **playstyle** that is nothing but a knob on how random your draft is — priced in income
[[28]](#s28):

| Playstyle | Effect, verbatim |
|---|---|
| **Lock-In** | *"+4 Income / Lock a fighter / Auto-drafted roll"* |
| **Greed** | *"+5 Income"* |
| **Redraw** | *"+4 Income / Random roll / Infinite random rerolls"* |
| **Yolo** | *"+7 Income / +4 Gold / Fully random roll / No Rerolls"* |
| **Chaos** | *"+3 Income / +4 Gold / New roll every wave / No Rerolls"* |
| **Hybrid** | *"+5 Income / Every fighter is random / No Rerolls"* |

**This dissolves the strongest objection to M3.** The complaint in cost 1 above is that a random offering imposes
variance on a player who did not choose it. Legion TD 2's answer is to make the *amount of variance* a player
decision, and to pay for it in the economy — more randomness, more income. In a one-purse game that is an
unusually clean fit, because income *is* the purse. Whether it survives contact with a rating ladder is a
different question, and it is one seam 1 has to answer rather than this note.

*YouTD spends a currency to widen the distribution it draws from.* The remake's roll algorithm is readable line by
line [[7]](#s7). Towers are not offered per wave from a flat pool; the chance of getting a tower of a given
element is `0.075 × that element's research level`, research is bought with a separate resource
(`cost = 20 + current level`, capped at level 15), and **rarity odds shift with research** — common decays at
−0.018 per level while unique rises from *zero* at +0.004. The player is not rolling for towers; the player is
**buying the shape of the distribution**. That is M3, M7 and M9 fused into one mechanic, and it is an in-run
disclosure ramp (A5) into the bargain.

*And its reroll trades quantity for quality, not gold.* The starting roll is 6; each reroll clears the stash and
deals **one fewer** [[7]](#s7). A scarcity that is not money, again — exactly M7's shape.

### 3.4 M4 — item layering is the most depth per idea and the worst balance surface in the survey

Mazebert TD's whole design: enemies drop loot, you equip it to towers, towers gain experience and level, and the
interactions between item, tower and hero are the game. The shipped numbers, from the open-sourced simulation
core [[25]](#s25): **4 to 6 item slots per tower**, items freely swappable, **base drop chance 3%**, potions
unlimited and permanent. YouTD 2 runs **315 items over 1–6 slots**, with slot count derived from tower cost and
clamped by rarity, plus **24 "oils"** that are permanent enchantments and **8 recipes** that transmute items
upward [[7]](#s7). It is enormously deep and it is the single hardest thing here to reconcile with this project,
for two independent reasons that stack:

- **Multiplicative surface.** A unit's value is indexed by *the multiset of items on it*. Thirty towers and fifty
  items with three slots is 30 × C(50,3) = **588,000** distinct equipped towers before anything is placed.
- **Per-instance state.** Items dropped mid-run and towers that level are exactly
  [Part V §10.5's](../archive/variance-levers-and-unit-schema.md#10-levers-not-to-build) replay hazard: *"a ghost is no
  longer a layout — it is a layout plus a biography."*

A **loadout** version — items chosen at build time from a fixed catalogue and paid for out of the purse — dodges
the second problem entirely and keeps the first. That is the only form of M4 worth considering here.

One shipped lever in this family is worth recording even though the family is discouraged, because it is the
cleanest solution to a slot cap anywhere in the genre. Mazebert's **Mr. Iron** *"leaves his armor for X seconds
to upgrade it, **permanently integrating all equipped items**"* — with legendaries, uniques and set items
excluded [[25]](#s25). One card converts a hard 4–6 slot cap into unbounded stacking of commons, and prices it in
downtime rather than in gold. If a loadout ever needs an escape hatch, that is its shape.

### 3.5 M5 — adjacency survives the corridor, and the corridor makes it *cheaper*

The obvious reading is that a one-hex corridor kills spatial synergy. It does not. It kills *maze geometry*
(M8), which is a different thing. Adjacency is a bounded-degree relation, and the corridor bounds the degree
hard: along a non-branching one-wide corridor, a placement has at most two along-axis neighbours, so an
adjacency lever is a function of at most a handful of neighbours rather than of the whole board. That is a
**cheaper** dependence index than the same lever on an open grid.

Which makes M5 unusually attractive here: it is the one mechanism in the taxonomy that the settled playfield
*improves*.

> ⚠️ **This rests on an assumption the vision does not state.** [§6](../vision.md#6-what-it-looks-like) fixes the
> corridor as one hex wide and non-branching, but nowhere says where towers sit relative to it — beside it, on
> a separate build ring, or in an unbounded field with the corridor merely defining the path. The degree bound
> above is only as good as that answer, and it belongs to seam 1. If towers are placed on an open field with a
> corridor drawn through it, M5's dependence index is board-global again and this paragraph is wrong.

[Part V §3.9](../archive/variance-levers-and-unit-schema.md#39-placement-and-space) also records the cheapest way to make
a short corridor hold more towers without lengthening it — **surface classes** (Orcs Must Die!'s floor / wall /
ceiling), three independent placement spaces occupying one corridor. That composes with adjacency and needs no
geometry.

### 3.6 M7 — tiered exclusivity is free content, and it is the mechanism this project is shaped for

A legality predicate costs nothing to author, cannot be un-balanced by a wrong number, and produces the term
everyone forgets in §1: **opportunity cost**. Three shipped versions:

| Game | The scarcity | Effect |
|---|---|---|
| Element TD 2 | 11 picks over a 6 × 3 element grid, plus Interest and Essence | The budget is *structurally* insufficient — six elements at level 3 costs eighteen picks. You cannot go wide and deep |
| Bloons TD 6 | at most two paths, secondary ≤ tier 2 | Every tower is a commitment; 8 of 15 upgrades stay unreachable |
| Legion TD 2 (Mastermind) | 10 offered, 6 kept, one reroll | Your roster is 12% of the 48 draftable bases |
| YouTD | rerolls deal **one fewer tower each time** | The scarcity is not gold; it is how many chances you have left |
| Mazebert TD | **up to 2 elements**, chosen pre-run, which narrows what can drop | The commitment is made before you know anything, and it shapes every draw after |
| Sanctum 2 | a pre-mission **loadout** of towers, weapons and perks | Sanctum 1 sold mazing; Sanctum 2 replaced open access with forced commitment — *"choose wisely because you are humanity's last defense"* [[29]](#s29) |

It is also, uniquely, **free to the harness**: a predicate does not need balancing, it needs *checking*, and the
sweep already enumerates legal configurations because that is what "legal" means.
### 3.7 M11 — in-run levelling, and why Bloons' heroes are the mechanism to copy

Called out separately because it is the one mechanism that is *simultaneously* a depth mechanism and an
accessibility mechanism and **does not need persistence** — and because it is easy to confuse with the thing
[Part V §10.5](../archive/variance-levers-and-unit-schema.md#10-levers-not-to-build) rules out.

A BTD6 hero is chosen before the run, starts at **level 1 every game**, and climbs to **20 during it**. The
proof is in the shipped achievement list rather than in marketing: *"Epic Hero — Level any Hero to level 20"* and,
decisively, *"Kali Maaaaaaaa — Gain 10 levels for Adora **in one round**"* [[18]](#s18) — a target that is only
coherent if heroes reset each game. There are **18 heroes** in the official API's hero list [[15]](#s15); the
store page's *"17 diverse Heroes"* [[6]](#s6) is one update stale.

Two properties make it fit this project unusually well:

- **It is a one-purse decision.** Heroes gain levels passively at end of round *and* can be levelled immediately
  by spending run cash — a patch-note line confirms the mechanic exists by fixing it (*"In Deflation, Hero 'cost
  to level up' should again update at the end of each round"*) [[16]](#s16). Money-for-tempo out of the same
  purse is exactly the shape [§3](../vision.md#one-purse) chose.
- **It is not Part V §10.5's hazard.** §10.5 rules out *per-instance experience on every tower*, because it makes
  a ghost "a layout plus a biography" and makes the build helper's "what does this tower do" unanswerable. **One**
  levelling unit whose trajectory is fully determined by the run's recorded inputs is a different object: it
  replays exactly, and the helper can answer "what does it do" with "it depends on the round, here is the table".
  The hazard scales with *how many* things carry history, and one is cheap.

The cost is that it is a **trajectory, not a set** — you cannot price a hero by looking at it, only by simulating
a run. That is the same cost M9 (economy) already carries, and the harness already runs whole matches, so it is
paid once for both.


---

## 4. The accessibility question

[§6 of the vision](../vision.md#legible-to-a-stranger) states the problem and the constraint exactly:
the game must be *"juicy and accessible — anyone could pick it up"*, Bloons TD 6 is *"the standing proof that a
game can be legible to a child and still have a competitive meta"*, and — the hard part —

> *"Almost every deep game onboards by withholding … §4 rules that out … Accessibility therefore has to be bought
> entirely with **legibility** … and never with progression."*

This section is the answer to the sentence the vision leaves open: *"what it means in practice is a live question
for the research."* The games that manage both do it with eight identifiable mechanisms. Four need persistence.
Four do not — and **the four that survive are not all legibility**, which is the one place this note pushes back
on §6's framing. A5 and A6 are *pacing*, not legibility: they control how much arrives at once, inside a run. The
vision is right that progression is unavailable and right that legibility must carry most of the load, but the
withholding ramp is not gone — **it has moved inside the run**, and that is a strictly better place for it here
because it resets.

| # | Mechanism | How it actually works | Needs between-run persistence? | Verdict here |
|---|---|---|---|---|
| A1 | **Progressive disclosure by account unlock** | BTD6 gates towers behind account XP levels; Mazebert gates cards behind a collection | **Yes** | ☠️ **Dead** — [§4](../vision.md#4-what-persists), [§11](../vision.md#8-out-of-scope) |
| A2 | **Meta-progression as a difficulty crutch** | Monkey Knowledge — *"Over 100 meta-upgrades"*, and the shipped achievement *"Dr. Monkey — Spend 106 Monkey Knowledge points"* [[6]](#s6)[[17]](#s17) — makes an early loss recoverable | **Yes** | ☠️ **Dead** — and BTD6 itself ships a per-event `disableMK` switch [[15]](#s15) |
| A3 | **A campaign / tutorial ladder** | Element TD 2 ships a 28-mission campaign [[1]](#s1); BTD6 delivers onboarding *as a Quest* rather than a bolt-on tutorial (*"First Steps — Complete the First Time Tutorial Quest"*) [[17]](#s17) | No, but it is authored content | ⚠️ Available, unbudgeted; the vision has no campaign and co-op is [not yet specified](../open-questions.md) |
| A4 | **Difficulty modes and map tiers** | BTD6 has three difficulties — **Easy / Medium / Hard** — with Impoppable, Half Cash and CHIMPS as *modes under Hard*, plus Beginner→Advanced map tiers, and CHIMPS is itself gated per map behind beating Impoppable [[17]](#s17) | No | ⚠️ Awkward — one ladder, one rating, and a stage-matched pool means difficulty is the *opponent*, not a setting |
| **A5** | **Progressive disclosure *inside the run*** | ETD2: 11 picks over 50 waves, each gated behind an elemental boss. Super Auto Pets: shop tier *X* unlocks on turn 2*X*−1 [[14]](#s14). BTD6: a hero that starts at level 1 **every game** and climbs to 20 during it [[15]](#s15)[[17]](#s17); money gates upgrades | **No — resets every run** | ✅ **The replacement for A1, and it is strictly better here** |
| **A6** | **A small offering out of a large pool** | LTD2 shows 10 of ~116 [[4]](#s4); Mazebert draws 1 card of 210 per round [[8]](#s8) | **No** | ✅ Works, at the variance cost in §3.3 |
| **A7** | **A generative, compressible roster** | Six element names predict fifty-six towers | **No** | ✅ **The strongest mechanism available, and the only one that reduces what must be *learned* rather than what must be *seen*** |
| **A8** | **Legibility furniture** — all upgrade options with prices and descriptions in one panel; safe defaults (BTD6's targeting priority ships set to *First*); a per-tower **performance readout** (BTD6 v54.0 added *"an extra button listing a performance summary with pops, damage, cash earned, value and lives earned"* [[16]](#s16)); counter-hints authored as data; colour-coded tells | BTD6's upgrade panel and info panel; ETD2's per-creep "weak to single-target / AoE / long range" annotations (recorded in [Part V §9](../archive/variance-levers-and-unit-schema.md#9-what-the-build-helper-actually-needs-from-this)) | **No** | ✅ Pure UI and data. Free, and it lands on [seam 7](../build-order.md#7--the-interface). The performance readout is the cheapest of the lot and the one most likely to be skipped |

**The sharp claim.** A1 through A4 are all mechanisms for controlling *how much a player sees at once*. A7 is the
only one that controls *how much a player has to remember*, and it is the only one that scales the roster and the
tutorial with the same lever. BTD6 spends its budget the other way — twenty-five hand-authored towers learned one
at a time, legibility bought with UI (A8) and unlock pacing (A1). **Strip A1 out, as §4 requires, and the
generative route is not merely available, it is dominant.**

Two secondary findings worth having on record:

**The bosses are the tutorial.** Element TD 2's element-boss gate (§2.2) does the job A3 usually does — it
introduces exactly one new thing at a time and makes you beat it — without a campaign, without persistence, and
inside the competitive mode. This is the most under-appreciated mechanism in the anchor game.

**Depth that lives inside one tower is more accessible than depth that lives between towers.** BTD6's cross-path
menu shows a player their entire decision at the moment they need it. A synergy between three towers cannot be
shown in a panel; it has to be discovered or read on a wiki. That is a real argument for M2 over M4/M5 on
accessibility grounds — and a real argument *against* M2 for a game whose signature is reading an opponent's
whole composition.

### 4.1 Running the taxonomy past §6's legibility veto

[§6](../vision.md#legible-to-a-stranger) arms a veto and says to use it as one: *"If a mechanism
cannot be read off the screen, it fails the accessibility pillar however deep it is."* Applied to §3’s eleven
mechanisms, at a fixed isometric camera, while watching two boards, by somebody who has never played it:

| Mechanism | Can it be read off the screen? | Cost of making it readable |
|---|---|---|
| **M1** Combination | ✅ **Best in class.** Element = colour, combination count = silhouette complexity. It is the one depth mechanism whose entire state is a *material swap*, which is the pipeline [§6](../vision.md#the-art-pipeline) already bought — one small PNG per palette | Near zero. It is the same recolour job as the faction colours |
| **M7** Tiered exclusivity | ✅ A pick is a banner and a persistent header showing what you took | Near zero — UI |
| **M9** Economy | ✅ It is a number that goes up | Zero |
| **M3** Offering | ✅ It is a menu; the whole point is that it is small | Zero |
| **M10** Counter-reading | ✅ if the matrix is colour-coded and the tell is on the creep | Low — [Part V §9's](../archive/variance-levers-and-unit-schema.md#9-what-the-build-helper-actually-needs-from-this) authored counter-hints |
| **M11** In-run levelling | ✅ It is a number over one unit's head | Zero |
| **M2** Cross-pathing | ⚠️ **Only if each configuration looks different.** BTD6 gives crosspaths distinct models — the community counts **52 unique models** for the Wizard Monkey alone [[12]](#s12) | **High, and it lands on the art pipeline.** 64 configurations × 12 towers is not a recolour job, and [§6](../vision.md#where-the-effort-goes) explicitly rules out custom character geometry as the default |
| **M5** Adjacency | ⚠️ Needs an explicit drawn link between the units | Medium — a link VFX, plus it must survive being read at a glance on *two* boards |
| **M6** Sacrifice | ☠️ **Fails.** A tower that is gone cannot be read; the resulting tower's power has no visible cause | Would need a permanent "made from" affordance |
| **M4** Item layering | ☠️ **Fails hardest.** An item equipped on a unit is invisible at this camera, and three items on thirty units is thirty times invisible | Would need per-item silhouette work, which is the ruled-out pipeline |

**This is the sharpest independent check in the note, because it was not chosen to agree with anything else.**
It ranks M1 first and M4/M6 last — the same order as the balance-surface table in
[§6](#6-how-big-the-balance-surface-actually-gets), arrived at from art and information design rather than from
combinatorics. When two unrelated filters agree, the ordering is probably real.

It also lands one blow the balance analysis does not: **M2's art cost.** Cross-pathing is the cheapest depth per
authored *rule* and one of the most expensive per authored *asset*, and this project's art plan is stock models
plus palette swaps. Direction B in [§7](#7-three-directions-ranked) is priced accordingly.

---

## 5. The constraint filter

Graded against [the vision's](../vision.md) settled decisions. "Corridor" means the one-hex non-branching
playfield with no mazing and no pathfinding, permanently
([§11](../vision.md#8-out-of-scope), and closed in [The Vision §11](../vision.md#8-out-of-scope)).

| Mechanism | Corridor | One purse | Nothing persists | Integer determinism | Verdict |
|---|---|---|---|---|---|
| **M1** Ingredient combination | ✅ Indifferent — recipes have no geometry | ✅ Prices in gold; the tech gate can be a boss wave, not a currency | ✅ The whole roster exists from run one, which §4 [demands](../vision.md#4-what-persists) | ✅ A recipe table is data | **Survives whole** |
| **M2** Constrained upgrade topology | ✅ Indifferent | ✅ Tier costs are gold | ✅ | ✅ A legality predicate is data | **Survives whole** |
| **M3** Randomised offering | ✅ Indifferent | ✅ | ✅ Rerolls per run, nothing carried | ✅ Seeded stream, recorded as ghost input | **Survives technically; fights [Part II §3's](../archive/async-ghost-round-robin.md) variance control** |
| **M4** Item layering | ✅ Indifferent | ✅ if items are bought, ☠️ if dropped | ⚠️ Drops are fine per-run; **tower XP is not** ([Part V §10.5](../archive/variance-levers-and-unit-schema.md#10-levers-not-to-build)) | ✅ | **Survives only as a build-time loadout** |
| **M5** Adjacency / aura | ✅ **Improved** — degree bounded by the corridor (⚠️ see §3.5) | ✅ | ✅ | ✅ Needs a declared stacking rule ([Part V §5.2](../archive/variance-levers-and-unit-schema.md#52-stacking-rules)) | **Survives, cheaper than elsewhere** |
| **M6** Sacrifice / consumption | ✅ Indifferent | ⚠️ Consuming towers is a gold sink with a refund question | ✅ | ⚠️ Check the fixed point ([Part V §3.11](../archive/variance-levers-and-unit-schema.md#311-economy-and-upgrade-topology)) | **Survives; carries GemCraft's known failure** |
| **M7** Tiered exclusivity | ✅ Indifferent | ⚠️ A second decision axis; ETD2's fix is to put income on the pick menu (§2.2) | ✅ Resets every run — this is the point | ✅ Free | **Survives whole. Best fit in the taxonomy** |
| **M8** Geometry — mazing, path length, blocking, route choice, placement-vs-path | ☠️ **DEAD** | — | — | — | **Dead, permanently** |
| **M9** Economy shape | ✅ Indifferent | ✅ **This is what one purse is *for*** | ✅ | ⚠️ Interest is integer division; specify rounding once ([Part V §4.2](../archive/variance-levers-and-unit-schema.md#42-the-reduction-formula-and-the-integer-contract)) | **Survives whole** |
| **M10** Counter-reading | ✅ Indifferent | ✅ | ✅ | ✅ | **Survives whole — and both boards at once makes it stronger here than in any surveyed game** |
| **M11** In-run levelling | ✅ Indifferent | ✅ **Better than indifferent** — BTD6 lets run cash buy a hero level, which is a money-for-tempo decision out of the one purse | ✅ Resets to level 1 every run, by construction | ⚠️ Fine for **one** unit whose trajectory is determined by recorded inputs; **not** for every tower ([Part V §10.5](../archive/variance-levers-and-unit-schema.md#10-levers-not-to-build)) | **Survives, bounded to one or two units** |
| **M12** Meta-progression | — | — | ☠️ **DEAD** | — | **Dead** |

### 5.1 What the corridor actually kills, stated once

**M8 in full, and nothing else.** Specifically dead, with no partial version:

- Maze construction and path lengthening (Gem TD's entire second half; the classic WC3 TD skill).
- `blocksPath`, and every design decision downstream of "the player has fully blocked the path"
  ([Part V §2](../archive/variance-levers-and-unit-schema.md#2-the-decisive-move-one-unit-two-roles)).
- Route choice as the attacker's mirror of mazing ([Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half)).
- Geometry-driven stats — a tower whose fire rate scales with how much path it overlooks
  ([Part V §3.9](../archive/variance-levers-and-unit-schema.md#39-placement-and-space)).
- The maze/gun resource split (Sanctum's base-versus-tower economy).
- Path policy and repath triggers ([Part V §3.5](../archive/variance-levers-and-unit-schema.md#35-movement)).

Those are the six levers [The Vision §11](../vision.md#8-out-of-scope) already lists as dead, plus
the activity they existed to support. **This note adds no new ones and removes none** — which is the useful
finding, because it means the corridor's cost was correctly priced when it was settled and nothing in this survey
raises it.

### 5.2 The good news, and it corrects a claim I made earlier in this note

An earlier draft of this section said that a meaningful fraction of Legion TD 2's depth is positional and
therefore does not transfer, and called that the most important sentence in the note for seam 1. **That is
wrong, and the correction is the best news in the survey.**

**Legion TD 2 has essentially no blocking geometry.** Its own pathing documentation says so:

> *"Units attempt to take the shortest route to their destination. In Legion TD 2, **the path is usually fairly
> straightforward since there are generally no walls that block a unit from where it wants to go.** Units steer
> around obstacles (i.e. other units) by using a flocking algorithm called Boids … Legion TD 2 is configured to
> prioritize smoothness, even if it means having some units walk through each other sometimes, because this leads
> to more reliable outcomes in battle."* [[26]](#s26)

And the map is **one lane per player** — *"Each player has their own segment, called a lane, that they defend by
building fighters"* [[26]](#s26). So the vision's structural north star is already, geometrically, very close to a
one-lane corridor. Its positioning depth is **aggro ordering**, not path length: the targeting tiebreak runs
missing-health → closer-position → *"Further Forward Tower Position"*, which produces the advice *"position your
DPS/carry unit on the side of your lane, while your tank is at least one column towards the centre"* [[26]](#s26).
That is front-line/back-line ordering — which a corridor reproduces directly, and which
[Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half) already has as
Anomaly's convoy ordering pointed the other way.

The game's own difficulty heuristic ranks its axes for you. **Recommended Value** tells a player how much gold to
have on the board and explicitly *"does not consider if your units are strong or weak on any specific wave, or if
they are positioned in any particular formation"* [[26]](#s26). In Legion TD 2's own terms: **gold-value pacing is
the first-order axis; type matchup and positioning are the two correction terms.** Both correction terms survive
the corridor intact.

**Mazebert TD is the strongest precedent of all, and it is the one nobody cites.** Mazebert has a **fixed path** —
its open-sourced simulation core builds a static polyline of waypoints once and never recomputes it [[25]](#s25) —
and it nonetheless generates all of its considerable depth from per-round random tower draws, a four-to-six-slot
item economy, and cross-tower synergy. Its developer built it explicitly as a distillation of YouTD: *"one of the
tower defenses that greatly inspired me … It's by far the best balanced tower defense I ever played"* [[25]](#s25).
**A no-mazing tower defense with extreme build depth is not a hypothesis. It shipped, twice.**

⚠️ The fixed-path claim is inferred from the source's structure rather than from a sentence the developer wrote;
it is high confidence but not quoted.

**And the compensation, restated.** [Part V §3.9's](../archive/variance-levers-and-unit-schema.md#39-placement-and-space)
surface classes (floor / wall / ceiling), **ordering along the corridor** — now known to be Legion TD 2's actual
positional mechanic rather than a consolation prize — and bounded adjacency all give placement something to mean
without geometry. A corridor is not a queue unless you make it one.

---

## 6. How big the balance surface actually gets

[§5 of the vision](../vision.md#5-how-it-is-balanced) says balance is computed, which is why a mechanism whose
balance surface explodes is not automatically disqualified. This section says how big each explosion is.

**First, the honest framing.** No harness enumerates *compositions* in any of these designs. A defense is a
multiset of units placed along a corridor and a wave is a multiset of units; that space is |alphabet|^slots under
every mechanism in the taxonomy, and it is astronomical in all of them. So the harness's real job is **unit-level
pricing** — is this thing worth its gold — and the question that decides whether it can do that job is:

> **Can a unit's contribution be priced without knowing the rest of the build?**

That is the dependence index from §3, and here is its cardinality.

| Mechanism | A unit's value is indexed by | Cardinality, at plausible content sizes | Enumerable overnight? |
|---|---|---|---|
| **M1** Combination | Nothing — the recipe is baked into the unit | **56** units for 6 ingredients at sizes 1–4; 41 at sizes 1–3; 25 for 5 ingredients at sizes 1–3 | ✅ Trivially |
| **M2** Cross-pathing | The tower's own config | **64 per tower**; 12 towers → 768; 25 towers → 1,600 | ✅ Trivially |
| **M7** Tiered exclusivity | The pick history | ≤ **2⁶ = 64** element sets (fewer once picks are metered) | ✅ Trivially |
| **M5** Adjacency, corridor-bounded | The ≤2 along-axis neighbours | roster² per pair ≈ **3,100** for a 56-unit roster | ✅ Trivially |
| **M9** Economy | Board state **over time** | Not a set — a trajectory. Priced by simulating a run, not by a table | ⚠️ Needs whole-run sweeps; that is what the harness already does |
| **M11** In-run levelling | The run's own history | A trajectory, like M9 — but **20 rows of table** describe the whole unit | ⚠️ Same cost as M9, paid once for both |
| **M10** Counter-reading | The opponent's composition | The matrix is types², **16–25 cells**; the composition is not enumerable | ⚠️ The matrix is; the composition is sampled |
| **M4** Item layering | The item multiset on that unit | 30 towers × C(50,3) = **588,000** equipped towers; against defenses, ~10¹¹ | ☠️ Sampling only |
| **M3** Randomised offering | The hand | Steeply pool-dependent: **C(48,6) = 12,271,512** (Legion TD 2's actual scale [[27]](#s27)); C(100,6) ≈ 1.2 × 10⁹; C(200,6) ≈ 8.2 × 10¹⁰ | ⚠️ **At a 48-unit pool, borderline** — 1.2 × 10⁷ hands is ~34 hours at 10 ms, so hands alone are enumerable and hands-against-defenses are not. ☠️ Beyond ~60 units, sampling only |
| **M6** Sacrifice | The consumed set | Depends; unbounded if the recipe is open | ⚠️ Check the fixed point analytically instead — it is cheaper and stronger |

**Read the table as a ranking, because it is one.** M1, M2, M7 and corridor-bounded M5 all leave a surface in the
low thousands — a rounding error against an overnight sweep. M4, and M3 above a pool of roughly sixty, push past
10⁸ and convert the harness's promise from *"a red cell names what is mispriced"* to *"a sampled estimate suggests
something might be"*. That is not fatal — sampling is a legitimate method — but it is a **downgrade of the one
balance method [§5](../vision.md#5-how-it-is-balanced) says is the only one that works at this scale**, and it
should be paid for knowingly.

Two numbers for scale: at 10 ms per headless match, **10⁶ matches is under three hours, 10⁷ is a day and a bit,
10⁹ is four months.** That day-and-a-bit is the practical ceiling for an overnight-plus-weekend sweep, and it is
the number that decides where M3's pool size can sit.

---

## 7. Three directions, ranked

Ranked, with the trade-off named. **A and B compose** — they are answers to different questions (what the roster
*is* versus what an upgrade *is*), and Element TD 2 in fact runs both, since its towers have four levels *and* a
combination roster. **C is the genuine alternative**, because a random offering over a generated roster confuses
the one thing the generated roster was for.

### Direction A — the generative roster · **recommended first**

Pick *n* ingredients — elements, damage families, whatever the fiction wants — and author exactly one unit per
subset up to size *k*. Meter the ingredients across the run: one pick at the start, one every few waves, with the
pick menu also offering income so that the tech axis and the purse meet on the same button (§2.2). Gate each pick
behind a wave you have to survive, not a timer. At *n*=5, *k*=3 that is 25 units; at *n*=6, *k*=3 it is 41; at
*n*=6, *k*=4 it is 56 and you have signed up for Element TD 2's authoring obligation.

**Take the whole mechanism, not just the recipe table.** Element TD's picks buy a position on an
*n* × *levels* grid, not a set of flags (§2.2), and the budget is deliberately too small to fill it — the
breadth-versus-depth trade *is* the build. Layer the gold economy's own version on top: tier-up available only
from a level-1 unit, with the level cap falling as the tier rises. Two scarcities, each with the same trade
inside it, out of one recipe table and one predicate.

**The trade-off: you buy learnability and an enumerable balance surface with a fixed, non-negotiable content
bill.** The rule decides how many units exist, not taste — the fifteenth dual gets authored whether or not anyone
had an idea for it, and Element TD 2 needed twenty-two months post-launch to deliver its fifteen quads
[[2]](#s2)[[3]](#s3). Against that: it is the only
mechanism in the survey that is simultaneously a depth mechanism, an accessibility mechanism (A7) and a harness
mechanism; the pick schedule is the in-run disclosure ramp (A5) that replaces the ramp §4 deleted; the picks are
readable off an opponent's board, which feeds both the counter-reading half of the loop and
[seam 6](../build-order.md#6--the-social-layer); and it commits you to a decision the vision already flags as open —
[how wide the damage-type matrix should be](../open-questions.md) — because a six-element cycle at
Element TD 2's 4:1 spread is a *much* more decisive matrix than Legion TD 2's 1.67:1.

**The one thing it delivers for free, flagged and handed on.**
[§3's](../vision.md#depth-is-the-point) second commitment — *"a tower of a given type unlocks a skill tree for
the creeps you can buy, so the pool you send from is a consequence of what you built"* — falls out of an
ingredient scheme without being designed for. If the vocabulary is *n* ingredients and both roles are drawn from
it, then picking Fire gates Fire towers **and** Fire creeps off the same pick, and "one coherent identity per
run" is the default rather than an extra rule. That is also precisely
[Part V §2's](../archive/variance-levers-and-unit-schema.md#2-the-decisive-move-one-unit-two-roles) *one unit, two roles*
paying off in content rather than only in schema. ⚠️ **I found no shipped game that does this** — Element TD 2's
waves are authored, not player-composed, and Legion TD 2's mercenaries are a separate list from its fighters —
so it is *unexplored*, not *known-bad*, and settling which of those it is belongs to
`docs/research/attack-composition-and-sending.md`, not here.

**The known failure, and it is known because two studios hit it.** A combinatorial roster pays breadth a
superlinear return and therefore produces a **U-shaped meta**: the widest build and the narrowest build dominate,
and the middle needs propping up patch after patch (§2.4). Element TD 2 has been nerfing six-element builds since
version 0.51; GemCraft cut its palette from nine gem types to six. **Do not adopt Direction A without adopting
its counter-measure**, and the counter-measure is cheap: a harness report of win rate **binned by number of
ingredients taken**, run on the first sweep and every sweep after. Two shipped games found this by hand over
years; this project can find it overnight. That is the concrete form
[§5's](../vision.md#5-how-it-is-balanced) promise takes here. Element TD 2's own second answer is worth having in
the back pocket too — **make the pick budget structurally insufficient** (eleven picks against a 6 × 3 grid, so
going wide costs going deep), which taxes breadth without nerfing anything.

**Ranked first** because it is the only direction where the accessibility answer and the depth answer are the
same mechanism, which is the exact thing the developer asked for — and because its one documented failure mode is
the one this project's balance method is best equipped to catch.

### Direction B — the predicate roster · **recommended second**

Keep the roster small — ten to fifteen units — and put the depth inside each one: three upgrade paths of four or
five tiers, plus a legality predicate that forbids most of the grid. Bloons' rule yields 64 configurations per
tower from 15 authored upgrades; twelve towers is 768 configurations from 180 upgrades, which is the best
leverage in the survey.

**The trade-off: the highest depth-per-authored-thing available, but the depth lives *inside* a tower rather than
*between* towers.** That is an accessibility win — a player's whole decision fits in one panel — and a structural
mismatch with a match format whose signature is reading an opponent's *composition*
([§3](../vision.md#3-what-a-match-is)). A cross-path is a private optimisation; an element pick is a public
commitment. Also: 180 authored upgrades with meaningful non-uniform crosspath bonuses is a great deal of writing
for a personal build, and BTD6's five-tier ladders need a long run to pay off, which collides with
[what a run is](../open-questions.md) still being open.

**Ranked second**, and worth noting that it is the *cheapest* thing on this list to add to Direction A later —
a legality predicate is free content and it does not touch the roster.

### Direction C — the metered offering · **recommended third**

Author a pool of units with no generative rule, and hand each player a small random hand each run — ten offered,
six kept, one reroll, in Legion TD 2's Mastermind shape. Depth comes from making a coherent defense out of what
you were dealt, plus what you can read off the opponent. **Size the pool at roughly fifty**, not a hundred:
Legion TD 2 drafts from 48 bases [[27]](#s27), and 48 is also, by luck, right at the point where the harness can
still enumerate every hand ([§6](#6-how-big-the-balance-surface-actually-gets)).

**The trade-off: the cheapest accessibility win on offer, bought at the cost of the harness's best trick.**
Nobody ever faces more than ten choices, the pool can grow forever without making the game harder to start, and
the "what did I get?" moment is good television for a lobby. It also has the best structural precedents here —
Mazebert TD runs this design on a **fixed path** [[25]](#s25), which is the closest existing thing to a corridor
game.

The variance objection turned out to be **softer than it first looked, and there are two shipped answers to it**:
price the randomness in the economy (Legion TD 2's playstyles — *"Yolo: +7 Income / Fully random roll / No
Rerolls"* against *"Lock-In: +4 Income / Lock a fighter"*) [[28]](#s28), or let the player buy the shape of the
distribution (YouTD's element research) [[7]](#s7). Either turns "the game rolled badly for me" into "I chose the
risk", which is a different complaint entirely and one a one-purse economy is well shaped to hold.

What does **not** soften is the harness, and it is the reason for the fifty-unit ceiling above. At C(48,6) the
hand space is 1.2 × 10⁷ and enumerable in about a day; at a hundred units it is 1.2 × 10⁹ and
[§5's](../vision.md#5-how-it-is-balanced) promise degrades from *"a red cell names what is mispriced"* to *"a
sampled estimate suggests something might be"*. **Direction C is the only one of the three whose content budget
is capped by the balance method rather than by authoring effort** — grow the pool and you lose the harness. Second,
more quietly: it sits awkwardly with
[§1's](../vision.md#1-the-destination) "the reward is the build," because a hand you were dealt is a puzzle you
solved rather than a defense you designed. That is a taste judgement, stated as one.

**Ranked third**, and it moved up during the writing of this note rather than down. It is not a weak design; it
is the only one of the three whose cost lands on the balance method rather than on content.

---

## 8. What could not be verified

⚠️ **Standing caveat, and it is a large one.** `fandom.com` returned **HTTP 402 Payment Required**,
`ninjakiwi.com` **403**, `support.ninjakiwi.com` **403**, `bloonswiki.com` **403** and `forums.ninjakiwi.com`
**522** to automated fetching, and the session's 200-call web-search budget was exhausted. Three workarounds
rescued most of it and are worth recording for the next note: **MediaWiki's `?action=raw`** reaches Fandom content
that `WebFetch` cannot; **Steam's `ISteamNews` API** returns full announcement bodies that the JS-rendered news
pages do not; and **the Wayback Machine** reaches the Warcraft 3 era. Where a claim still rests on a
search-result summary rather than a fetched page, it is marked ⚠️ below.

| Claim | Status |
|---|---|
| Element TD 2 — 59 towers, 6 elements, 28-mission campaign | **First-party** store page and official site [[1]](#s1)[[2]](#s2) |
| Element TD 2 — 15 quads at 4,000 / 2,500, added 12 Dec 2021, and the stated design reasons | **First-party** — the v1.4 announcement, retrieved in full via Steam's news API [[18]](#s18) |
| Element TD 2 — 11 picks, start + every 5 waves to wave 50 | **High.** Two community guides [[9]](#s9)[[10]](#s10) **and** the developer's own roadmap post [[18]](#s18) **and** the WC3 original's identical 11-lumber budget [[20]](#s20) |
| Element TD 2 — the elemental-boss gate on each pick | Community guide [[9]](#s9); the analogous Essence-boss gate is first-party [[18]](#s18) |
| **Element levels 1–3 exist and gate tower level** | **Existence: first-party**, referenced repeatedly in patch notes [[18]](#s18) and stated outright for the WC3 original [[20]](#s20). ⚠️ **The exact level → tier-level mapping is not stated anywhere first-party** and is the largest single gap in §2 |
| Element TD 2 — level caps 4/3/2/1, tier-up only from level 1, cost ladder | Community wiki [[11]](#s11), ⚠️ read via `?action=raw` rather than a browser |
| Element TD 2 — damage ring, Composite 90%, boss armor 10% | Community wiki [[11]](#s11), **corroborated by the official archived element table** of the WC3 original [[19]](#s19) |
| The U-shaped meta and six years of breadth nerfs | **First-party**, extensively — five separate patch announcements [[18]](#s18). The structural explanation (superlinear C(n,k) return) is mine |
| Why there is no five-element tower | ⚠️ **Unexplained.** No source found either way |
| Warcraft 3 Element TD — 6/15/20 towers, 22 build lists, 60 waves, the element table, tower ladders | **First-party, archived** — Karawasa's own site via the Wayback Machine [[19]](#s19). A large upgrade on what this note originally claimed |
| Warcraft 3 Element TD — 11 lumber, element levels ≤3, pure-essence rules | **Semi-official** — player-written guides *published on the official site* [[20]](#s20) |
| Dota 2 Element TD — 6×4 singles, 15 duals, 20 triples, no quads | **Shipped game data** — the port's public source tree [[21]](#s21) |
| BTD6 — 25 towers by name, 18 heroes by name, `disableMK` / `maxParagons` / `path*NumBlockedTiers` | **First-party machine-readable** — Ninja Kiwi's Open Data API [[15]](#s15). Note the store page's *"17 diverse Heroes"* [[6]](#s6) is **one update stale** |
| BTD6 — heroes level 1→20 within a single game | **First-party shipped data** — the achievements *"Epic Hero"* and, decisively, *"Kali Maaaaaaaa — Gain 10 levels for Adora **in one round**"* [[17]](#s17) |
| **The BTD6 crosspath legality rule itself** | ⚠️ **Community-documented only, and this is a real negative result.** Two independent passes found no Ninja Kiwi statement of it in store copy, patch notes or any reachable support page — though `support.ninjakiwi.com` was never readable, so this is *found no evidence*, not *proved absent*. The 64-configuration arithmetic is mine and agrees with the community's own count [[12]](#s12). NK's own notation is `025`, not `0-2-5` [[16]](#s16) |
| BTD6 Paragon degree range 1–100 and the tier-5 prerequisite | **First-party** — the v27.0 announcement [[16]](#s16) |
| BTD6 Paragon **degree formula** | ⚠️ **Reverse-engineered by the community, with conflicting versions.** Not relied on here |
| BTD6 account-level tower-unlock table | ⚠️ **Not established.** Two passes failed. Immaterial to this note's argument, since account unlocks are dead by §4 |
| Ninja Kiwi on onboarding / new-player design | ⚠️ **Almost nothing public.** No NK statement found. The unexploited lead is NK's *"Insider Session #3"* with a BTD6 design co-lead, which is video-only |
| BTD6 reach | ⚠️ **No official figure exists** — NK says only *"a favorite game for millions of players"*. Third-party: 228,647 Steam reviews at 97% positive, 345K App Store ratings at 4.9, all-time Steam peak 53,891 concurrent. The review volume at a paid price point is the defensible framing; concurrents are not, since BTD6 is mobile-first |
| Legion TD 2 — 8 legions, 12–15 fighters each, reroll, 75–125% spread | **First-party** manual [[4]](#s4). The ~116 total is my count off the official unit guide [[5]](#s5) and does not match Part V's 159, which counts mercenaries and wave creatures too |
| **Legion TD 2's "10 offered, select 6"** | ⚠️ **Community wiki only** [[26]](#s26) — no first-party page states the roll size, though the manual describes the draft [[4]](#s4) and the store page's *"12 million possible combinations"* = C(48,6) corroborates the pool half [[27]](#s27). Flagged because it is load-bearing for Direction C |
| Legion TD 2's ten playstyles and their income prices | **First-party** — the official Mastermind page [[28]](#s28) |
| **Legion TD 2 has essentially no blocking geometry** | ⚠️ **Community wiki, quoting what reads as developer knowledge** [[26]](#s26). This corrects an earlier claim in this note's own drafting (§5.2) and it is important enough that **a human should confirm it in-game before seam 1 leans on it** |
| YouTD 2 — 235 tower families / 690 rows, 315 items, 21 builders, the roll algorithm, research-widens-the-distribution, reroll-deals-fewer | **Shipped source, exact** — the MIT repo's CSVs and `tower_distribution.gd` [[7]](#s7), whose own comment says it *"attempts to accurately reproduce the algorithm from the original game"*. The **WC3 original's tower count remains unestablished** (a community database implies ~676 tower tiers; nobody publishes a figure) |
| Mazebert TD — 210 cards as 61/98/33/18, 4–6 slots, 3% drop, 2 elements, 1 card per round, **fixed path** | **Shipped source** — the developer open-sourced the simulation core [[25]](#s25), and the counts reconcile against the official site independently. ⚠️ The fixed-path claim is inferred from source structure, not quoted |
| **Mazebert: difficulty → better loot** | ☠️ **Verified false.** Difficulty changes creep HP and wizard XP only; a full search of the shipped loot system finds no difficulty term [[25]](#s25). A widely repeated claim that this note does not make |
| Gem TD — 8 colours × 6 qualities, the combine DAG, the dual "straight flush" routes | ⚠️ **Medium.** Stats and names come from a community site that generated them by extracting the shipped `vpk`; recipes from a community wiki [[24]](#s24) |
| Gem TD — random-roll odds, reroll rule, the keep-one-maze-the-rest loop | ⚠️ **NOT VERIFIED AT ALL.** No citable source found. Mitigated only by the fact that the mazing half is dead here anyway (M8) |
| **"Gem TD+" as a product** | ⚠️ **Could not be found on Steam.** The verifiable modern remake is a Dota 2 custom game by Drodo Studio [[24]](#s24). Treat the name as unconfirmed |
| GemCraft — nine gem types (2015) cut to six (2020) | **First-party** — both Steam store pages [[22]](#s22) |
| GemCraft — pure/dual/prismatic dilution, random→chosen colour, supergemming and its removal | ⚠️ Community wiki [[23]](#s23). **No developer commentary on combining design intent was found**; `gameinabottle.com/blog/` is the remaining lead |
| Super Auto Pets tier-unlock schedule (tier *X* on turn 2*X*−1) | ⚠️ Community wiki [[14]](#s14). Used only as one worked example of A5 |
| That an adjacency lever is bounded-degree on this playfield | ⚠️ **Depends on an unstated design fact** — where towers sit relative to the corridor. §3.5 |

**Considered, and what each contributes.** Six more games were surveyed. Only one added a taxonomy row (Sanctum 2,
into M7); the rest are recorded here so nobody has to survey them again.

| Game | Its one structural contribution | Verdict |
|---|---|---|
| **Sanctum 1 → 2** | The **loadout constraint.** Sanctum 1 sold mazing (*"Don't just build towers. Build mazes!"*); Sanctum 2 replaced open access with a pre-mission loadout of towers, weapons and perks [[29]](#s29) — deliberately shrinking the available set to force commitment. The exact inverse of YouTD's approach, and the only franchise here that made the move *away* from geometry on purpose | **Folded into M7.** The most directly relevant precedent for a game that has given up mazing |
| **Rogue Tower** | Player-steered **procedural path growth** plus in-run upgrade card draws, over *"400 unique cards and upgrades"*, with elevation making some sites premium | ☠️ The path-growth half is M8 and dead. The card-draw half is M3. Its economy levers (count-scaled pricing, diversity bounty) are already in [Part V §3.11](../archive/variance-levers-and-unit-schema.md#311-economy-and-upgrade-topology) and **deserve a second look under any of the three directions** — a stored-ghost pool that everyone copies is exactly the problem they solve |
| **Infinitode 2** | Only *"15+ different types of towers"* but *"300+ unique upgrades, almost every one of which can be improved to infinity"*, plus per-tower RPG levelling | ☠️ **Depth by vertical meta-progression.** M12, dead by §4. Useful as the clearest example of the road not taken |
| **Kingdom Rush** | The **fixed branching specialisation tree** — the same tree every run, variance only from map and enemy composition | The control case. This is the baseline every combinatorial game in §3 is departing *from* |
| **Defender's Quest** | Towers as persistent RPG characters carried **across battles**, levelled and equipped individually | ☠️ Campaign-scoped persistence — dead by §4. Its damage-*flavour* tag system is already in [Part V §4.1](../archive/variance-levers-and-unit-schema.md#41-the-scalar-layer--three-shapes-pick-exactly-one) |
| **Legion TD Reborn** (Dota 2) | Symmetric PvP send-economy with a **duel every five waves** — the opponent chooses your difficulty curve rather than your own tower pool | Structurally close to this project's match format, but its depth is economic rather than combinatorial. Nothing new for the taxonomy |

---

## Sources

Ordered by how much weight this note puts on them. First-party status is stated for every one, because this genre
is badly under-documented first-party and several of its most-cited numbers are folklore.

**First-party**

<a id="s1"></a>1. **Element TD 2 — Steam store page**, [store.steampowered.com/app/1018830](https://store.steampowered.com/app/1018830/Element_TD_2/). *"With 6 elements that combine to create 59 unique towers, you will need to anticipate incoming waves…"*; 59 towers, 28-mission campaign, 55 waves, 26 maps, 10 modifiers, 8-player co-op. **First-party (developer-authored store copy).**
<a id="s2"></a>2. **Element TD 2 — official site**, [eletd.com](https://www.eletd.com/). *"Element TD 2 sees you combine powerful elements to create unique towers"*; 59 towers, 6 elements; the WC3 (2006) → SC2 → Dota 2 → mobile → standalone (Feb 2020) lineage. **First-party.**
<a id="s3"></a>3. **Element TD 2 — Version 1.4, Quad Element Towers**, Steam news, 12 Dec 2021, [store.steampowered.com/news/app/1018830/view/3101288742628824105](https://store.steampowered.com/news/app/1018830/view/3101288742628824105). 15 quad towers added; 4,000 gold from scratch, 2,500 upgrading from a triple. **First-party announcement** (read via secondary summaries — the Steam news body did not render to automated fetch; the numbers are consistent across three reports). ⚠️
<a id="s4"></a>4. **Legion TD 2 — official game manual**, [beta.legiontd2.com/manual](https://beta.legiontd2.com/manual/). *"Legion TD 2 has 8 factions, called 'legions' … Each legion contains 12-15 unique fighters"*; *"Instead, you draft a set of fighters (called a 'roll') from all legions"*; *"Once per game, you can use a Reroll to swap out up to 4 fighters"*; *"multiplied by a factor ranging from 75% to 125%"*; mythium, workers, king upgrades. **First-party.**
<a id="s5"></a>5. **Legion TD 2 — official unit guide**, [beta.legiontd2.com/guide/units](https://beta.legiontd2.com/guide/units/). Per-legion fighter listings (~116 fighters across 8 legions plus Mastermind), 27 mercenaries, 21 wave creature types. Counts are mine, off the official tables. **First-party data, my arithmetic.**
<a id="s6"></a>6. **Bloons TD 6 — Steam store page**, [store.steampowered.com/app/960090](https://store.steampowered.com/app/960090/Bloons_TD_6/). *"25 powerful Monkey Towers"*, *"3 upgrade paths"*, *"17 diverse Heroes"* with *"20 signature upgrades and 2 special abilities"*, Monkey Knowledge as *"Over 100 meta-upgrades"*, 70+ maps, Paragons. **First-party store copy.** CHIMPS's expansion — no **C**ontinues, **H**earts lost, **I**ncome, **M**onkey knowledge, **P**owers or **S**elling — is community-documented but is an official mode-rule statement in-game. ⚠️
<a id="s7"></a>7. **YouTD 2 — developer's itch.io page**, [praytic.itch.io/youtd2](https://praytic.itch.io/youtd2), and [github.com/Praytic/youtd2](https://github.com/Praytic/youtd2). *"200+ towers"*, *"300+ items"*, *"21 Builders"*, *"40+ enemy abilities"*; a remake of the WC3 mod by geX and the YouTD community; MIT-licensed Godot source. **First-party for the remake; nothing here is a source for the WC3 original.**
<a id="s8"></a>8. **Mazebert TD — official site**, [mazebert.com](https://mazebert.com/). *"210 tower, item, potion and hero cards"* drawn at random each round; enemy loot drops equipped to towers; towers gain experience and level; never-ending bonus round; free-to-play, not pay-to-win; now retired with offline builds available. **First-party.**

**Community and forum sources — used with care, marked at every use**

<a id="s9"></a>9. **"A Beginner's Guide to Element TD 2"**, Steam Community guide, [steamcommunity.com/sharedfiles/filedetails/?id=2360427994](https://steamcommunity.com/sharedfiles/filedetails/?id=2360427994). *"You get to make a new pick every 5 waves up to wave 50. In total, you get 11 picks"*; the start-of-game choice between Interest (+0.6% per 15 s) and one of six elements; *"every elemental pick after the first one will summon a boss that corresponds to the element. You won't unlock access to the new element until you've killed the boss"*; Essence buys level-4 singles or Periodic towers. **Community, player-authored.**
<a id="s10"></a>10. **"Beginner Strategy Guide"**, forums.eletd.com, [forums.eletd.com/topic/95770](https://forums.eletd.com/topic/95770-beginner-strategy-guide/). Corroborates eleven picks *("Every 5 waves (up thru wave 50 so ELEVEN total points granted)")*; 2% interest per 15 s of game time; tower cost ladders (single 175/750/3000/9000, dual 600/1700/4700, triple 1500/5000); the three support duals (Blacksmith, Well, Trickery). **Community-authored, hosted on the developer's own domain** — that is a hosting fact, not an endorsement.
<a id="s11"></a>11. **Element TD 2 Wiki (Fandom)** — Elements, Towers, Periodic Tower pages. The six-element cycle (200% to the next, 50% to the previous), Composite dealing 100% and taking 90%, Arrow and Cannon as always-unlocked Composite basics, the Periodic Tower as a six-element Essence tower. **Community wiki, ⚠️ read via search-result summaries only — fandom.com returned HTTP 402 to direct fetch.**
<a id="s12"></a>12. **Bloons Wiki (Fandom)** — Crosspathing, Upgrade Path, Crosspath Chart pages. The legality rule (secondary path capped at tier 2, third path untouched) and the count of 64 combinations for the Wizard Monkey. **Community wiki, ⚠️ read via search-result summaries only.** The 64-configuration arithmetic in §3.2 is derived independently in this note and happens to agree.
<a id="s13"></a>13. **Element TD 2 Steam discussions**, Karawasa's posts on multi-element towers, [steamcommunity.com/app/1018830/discussions](https://steamcommunity.com/app/1018830/discussions/0/3881598799638342855/). The single → dual → triple → quad upgrade progression and the "corresponding dual" relation between a triple and the three duals inside it. **Developer posting in a community forum** — first-party voice, secondary venue.
<a id="s14"></a>14. **Super Auto Pets Wiki** — Tier pages. Shop tier *X* becomes purchasable on turn 2*X*−1 (tier 1 turn 1 through tier 6 turn 11); levelling a pet offers early access to two next-tier pets. **Community wiki, ⚠️ via search-result summaries.** Used only as one worked example of in-run progressive disclosure.

**First-party, added on a second pass — the strongest sources in the note**

<a id="s15"></a>15. **Ninja Kiwi Open Data API**, [data.ninjakiwi.com](https://data.ninjakiwi.com/) — e.g. `/btd6/challenges/challenge/ZFVFOXP`, `/btd6/races/Tough_Candy_ms4ugwkz/metadata`, `/btd6/bosses/Vortex46_ms8tf3cb/metadata/standard`. **Machine-readable, NK-authored.** The 25-tower `_towers` roster by internal name, the 18-hero roster (`Quincy … DanDMonke`), and the mode switchboard: `path1/2/3NumBlockedTiers`, `disableMK` ("true when Monkey Knowledge is disabled"), `disablePowers`, `disableInstas`, `disableSelling`, `noContinues`, `maxParagons`. Boss events set `disableMK: true`; odysseys `false`; races vary per event.
<a id="s16"></a>16. **Bloons TD 6 official Steam announcements**, via the `steam_community_announcements` feed for app 960090. **v27.0** (25 Jul 2021) for Paragons — *"Merge together all three Tier 5 upgrades … Paragon Degrees start at 1 and reach a maximum Degree of 100"*, *"cannot be buffed by other towers, nor can their transformation price be reduced"*. **v25.0** (15 Apr 2021) for Veteran Levels and Monkey Knowledge. **v54.0** for the tower performance-summary panel. Balance-note notation (`4xx`, `x3x`, `032`) throughout.
<a id="s17"></a>17. **Bloons TD 6 Steam achievement strings**, [steamcommunity.com/stats/960090/achievements](https://steamcommunity.com/stats/960090/achievements). **Shipped game data.** *"Epic Hero — Level any Hero to level 20"*; *"Kali Maaaaaaaa — Gain 10 levels for Adora in one round"*; *"Dr. Monkey — Spend 106 Monkey Knowledge points"*; *"Knowledgeable Primate — Unlock all Monkey Knowledge in one branch"*; *"First Steps — Complete the First Time Tutorial Quest"*; the four class-only win achievements; the Beginner/Intermediate/Advanced map-tier achievements.
<a id="s18"></a>18. **Element TD 2 official Steam announcements**, retrieved in full via `ISteamNews/GetNewsForApp` for app 1018830 (the rendered news pages are JS-only). **v1.4** — quads, costs, and the stated design goals. **v1.6** — War Mode's *"you can't get Level 2 Elements until the 4th Pick, Level 3 Elements until the 8th Pick, and Essence Upgrade until the 10th Pick"*. **v0.51 / v0.61 / v0.63 / v1.9 / v1.9.4 / "Core Balance Changes"** — the six-year breadth-versus-depth balance fight quoted in §2.4, the Essence boss, and the roadmap post's *"until all 11 Picks are selected"*.
<a id="s19"></a>19. **eletd.com — Karawasa's own site for the Warcraft 3 original, via the Wayback Machine.** The 4.0-beta navigation (*"6 Elemental Towers / 15 Dual Towers / 20 Triple Towers / 22 Build Lists / 60 Creep Waves"*), the official **Help — Element Table** page (the full 200% / 50% / 100% ring), and the elemental-tower pages with their five-stage ladders and costs. **First-party, archived.** Note `elementtd.com` was squatted and its archive is empty.
<a id="s21"></a>21. **Element TD, Dota 2 port** — [github.com/MNoya/Element-TD](https://github.com/MNoya/Element-TD). **Shipped game data.** `scripts/npc/units/towers/` confirms 6 singles × 4 levels, exactly 15 duals × 3, exactly 20 triples × 3, **no quads**; `gamesettings.kv` carries the `AllPick` / `AllRandom` / `SameRandom` mode taxonomy Element TD 2 inherited.
<a id="s22"></a>22. **GemCraft — Steam store pages**, Game in a Bottle. [Chasing Shadows](https://store.steampowered.com/app/296490/) (2015): *"Craft and combine **nine gem types** with different effects"*. [Frostborn Wrath](https://store.steampowered.com/app/1106530/) (2020): *"Craft and combine **six types of gems**"*. **First-party.** The palette shrank by a third between the two.
<a id="s25"></a>25. **Mazebert TD — the open-sourced simulation core**, [github.com/casid/mazebert-simulation](https://github.com/casid/mazebert-simulation) (MIT, by the developer), plus [mazebert.com](https://mazebert.com/) and its `/forum/news/` posts. **First-party.** 61 towers / 98 items / 33 potions / 18 heroes = the advertised 210, reconciling against the site's own card pages; `MIN_INVENTORY_SIZE = 4`, `MAX_INVENTORY_SIZE = 6`; `DEFAULT_DROP_CHANCE = 0.03`; `MAX_ELEMENTS = 2`; four starting towers then one card per round; a `Path` built once from a fixed waypoint list. Developer quotes on the YouTD lineage from `/forum/news/tribute-to-youtd--id799/`. **Verified negative:** difficulty affects creep HP and wizard XP only — it does **not** affect loot chance, contrary to a widely repeated claim.
<a id="s27"></a>27. **Legion TD 2 — Steam store page**, [store.steampowered.com/app/469600](https://store.steampowered.com/app/469600/). *"Choose from 8 unique legions and over 100 fighters. Select fighters from each legion for **12 million possible combinations**."* C(48,6) = 12,271,512, which decodes the claim exactly. **First-party.**
<a id="s28"></a>28. **Legion TD 2 — official Mastermind page**, [beta.legiontd2.com/mastermind](https://beta.legiontd2.com/mastermind/). The ten playstyles and their income prices, quoted in §3.3. **First-party.**
<a id="s29"></a>29. **Sanctum / Sanctum 2 — Steam store pages**, [91600](https://store.steampowered.com/app/91600/) and [210770](https://store.steampowered.com/app/210770/). Sanctum 1: *"Don't just build towers. Build mazes!"* Sanctum 2: *"Choose your own loadout of towers, weapons and perks, but choose wisely because you are humanity's last defense."* **First-party.** ⚠️ Exact loadout slot counts not established.

**Community, added on a second pass**

<a id="s20"></a>20. **Guides published on eletd.com, via the Wayback Machine** — *"Element Tower Defense FAQ by Cisz"* (11 lumber; the elemental summoning centre; *"Elements can get as strong as level 3 … to summon the higher levels, you need to have flattened the lower ones"*; *"You need level three in an element to build the pure tower of that kind"*; interest +0.75% per lumber on a 2% base) and *"Element Tower Defense Build List by holepercent"* (the 22 build lists — 15 four-element, 6 five-element, 1 six-element, each listing exactly the C(k,2) duals and C(k,3) triples it unlocks). **Player-authored, published on the developer's own site — semi-official.**
<a id="s23"></a>23. **GemCraft Wiki** — Gems, Gem Optimization, Fusion. Pure/dual/prismatic dilution rules, the per-game colour→special tables, random-colour → chosen-colour history, the grade-gap branch behind supergemming and its removal in *Labyrinth*, and the "True Colors" skill that eventually makes breadth outstrip purity. **Community wiki.** No developer commentary on combining design intent was found.
<a id="s24"></a>24. **Gem TD** — the Dota 2 remake's [Steam Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=474619917) (Drodo Studio, first-party to the remake); a community site that generated the tower tables by extracting the shipped `vpk`; and the Gem TD community wiki for the recipe list. **Mixed, mostly community.** ⚠️ Roll odds, reroll rules and the mazing loop are **unverified**; no "Gem TD+" product could be found.
<a id="s26"></a>26. **Legion TD 2 Wiki**, [legiontd2.wiki.gg](https://legiontd2.wiki.gg/) — linked from the official site's Game Guide menu but **community-run**. Pathing & Targeting (*"the path is usually fairly straightforward since there are generally no walls that block a unit from where it wants to go"*; the Boids flocking note; the missing-health → closer-position → further-forward tiebreak; the front-line/back-line advice), Maps (one lane per player), Mastermind (*"you start the game with 10 options and select 6 fighters"* — **not stated first-party anywhere**), Recommended Value, and the exact 4 × 5 type table.

**Inherited**

30. **[Part V — Tower & Creep Variance Levers](../archive/variance-levers-and-unit-schema.md)**, especially §3.9 (placement and surface classes), §3.11 (upgrade topology, GemCraft's supergemming, Rogue Tower's diversity levers), §4.1 (the matrix-width question and the 1.67 : 1 / 4 : 1 / 40 : 1 spreads), §4.3 (capability gates), §4.5 (adaptive counters), §5.4 (named RNG streams), §10.2 and §10.5 (variance and per-instance experience). This note is written to sit **above** Part V: Part V catalogues *levers*, this catalogues *mechanisms that combine levers into builds*. Where they overlap, Part V is the more detailed and is not repeated.
31. **[The Vision](../vision.md)** §1, §3, §4, §5, §11 and [The Vision §11](../vision.md#8-out-of-scope) — the settled decisions the filter in §5 grades against.
