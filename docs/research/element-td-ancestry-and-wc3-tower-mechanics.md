# Element TD's ancestry, and the tower mechanics of the Warcraft 3 original

**Research note** · 3 August 2026 · commissioned directly by the developer

**Question:** a full report on the tower mechanics of *the Warcraft 3 mod TD that inspired Element TD*, plus a
comparison to Element TD and to Legion TD.

**Inputs / prior art in this repo:**
[Build depth in tower defense](build-depth-in-tower-defense.md) §2 already measures Element TD 2 and the archived
WC3 original in detail; [The attacking half](attack-composition-and-sending.md) §1 already measures Legion TD 2's
send economy. **This note does not repeat either.** What is new here is (a) the ancestry question, (b) the WC3
original's four mechanics that did *not* survive into Element TD 2, and (c) the three-way structural comparison.

> ⚠️ **Read the first finding before the rest.** The premise of the question — that a distinct earlier Warcraft 3
> map inspired Element TD — **could not be confirmed against any source**. §1 lays out what the record actually
> says and lists the candidates that get conflated with it. If you had a specific map in mind, name it and this
> note can be re-pointed in an hour; everything in §2–§4 stands either way.

---

## Bottom line

### No earlier Warcraft 3 map is on record as Element TD's inspiration. Every candidate the community names either *post-dates* it, or is a *clone* of it, or belongs to a different subgenre entirely. On the present evidence Element TD's combination system is original to Element TD.

### Element TD and Legion TD are opposite answers to one question — *where does the decision live?* Element TD puts it in a metered tech tree against a fixed, public wave. Legion TD puts it in an economy against a live opponent. Neither is a "better TD"; they are different genres wearing the same costume.

Four claims carry the note.

**One. The ancestry is a dead end, and the dead end is itself informative.** Three sources were searched to
exhaustion — the designer's own archived site, the ModDB/Hive/EpicWar map records, and the secondary press. None
names a predecessor. The two maps most often confused for one are **Gem TD** (18 February 2007) [[6]](#s6) and
**Flash Element TD** (January 2007) [[3]](#s3), and *both post-date Element TD's first release* (23 October 2006)
[[1]](#s1). Flash Element TD is not an ancestor at all — Wikipedia records it as explicitly derived: *"the map and
name is based on the 'Element TD' map created for Warcraft III"* [[3]](#s3). See [§1](#1-the-ancestry-question).

**Two. The WC3 original ran four mechanics that Element TD 2 does not, and one of them is a genuinely different
idea about randomness.** In the WC3 map, **Interest was itself a pick** competing with elements in the same random
offering — capped at two per game, at 1/7 odds per offer [[4]](#s4). That is an economy upgrade priced against a
tech upgrade *on one dial*, which is structurally the "one purse" problem this project has already chosen to have
([The Vision §3](../vision.md#one-purse--restored-6-august-2026)), solved by a game that shipped it. The other three: **support towers**
(Well → attack speed, Blacksmith → damage per shot) [[5]](#s5), a **creep special-ability layer** independent of
armor (Fast / Healing / Mechanical / Undead) [[5]](#s5), and the fact that the whole element ring was **built out
of Warcraft 3's hardcoded armor and attack types**, remapped, with only 200% / 100% / 50% available as multipliers
[[4]](#s4). See [§2](#2-tower-mechanics-of-the-warcraft-3-original).

**Three. The element ring is not a matrix — it is a cycle, and that is why six elements are affordable.** Each
element deals 200% to exactly one element and 50% to exactly one other — *"light — 200% to darkness, 50% to earth"*
[[5]](#s5) — so the whole counter system is a **ring of six**, not a 6×6 table of 36 tuned numbers. Element TD 2
states the same rule from the creep's side: each armor *"takes additional damage from the element preceding it and
retains near-invulnerability from the element following it"* [[2]](#s2). Six elements cost six numbers. This is the
single cheapest thing in the design and it is the reason a two-person team could ship 41 towers in 2006.

**Four. The authorship record is contradictory, and the contradiction is probably cosmetic.** Map sources credit
**Karawasa and MrChak** [[1]](#s1)[[2]](#s2); Wikipedia credits **Brian Powers and Evan Hatampour** [[3]](#s3);
the current Element TD 2 press page lists **Evan Hatampour** as founder of Element Studios [[7]](#s7). The obvious
reading is that these are the same two people under handles and legal names — ⚠️ **inference, not sourced.** Do not
cite the pairing without checking it.

---

## 1. The ancestry question

### 1.1 What was searched, and what was found

| Source class | What it says about a predecessor |
|---|---|
| Designer's own site (`eletd.com`, live and archived) | Nothing. The press page's history section says only that Element TD 2 is *"the culmination of 15 years of multiplayer tower defense development"* across WC3 → SC2 → Dota 2 [[7]](#s7) |
| The official forums' own mechanics threads | Nothing. *"How Things Work"* and *"Basics of Element TD"* describe the systems and never cite an influence [[4]](#s4)[[5]](#s5) |
| Map databases (ModDB, EpicWar, Hive, wc3maps) | Nothing. Descriptions are feature lists |
| Secondary press / encyclopaedias | Only the *downstream* direction: Flash Element TD is based on Element TD [[3]](#s3) |

⚠️ **Two sources could not be read**: `hiveworkshop.com` and `moddb.com` return 403 to automated fetching, and
`web.archive.org` is blocked from this environment. A human with a browser should check the Hive thread
*"Warcraft 3 Tower Defense roots, which mod was first?"* [[8]](#s8) before treating §1 as final. That thread is the
single most likely place for a first-hand answer to exist.

### 1.2 The candidates, with dates — which is what rules most of them out

| Map | Date | Could it have inspired Element TD? | What it actually is |
|---|---|---|---|
| **Element TD** (Karawasa, MrChak) | first release **23 Oct 2006** [[1]](#s1) | — | The map in question |
| **Flash Element TD** (David Scott) | **Jan 2007** [[3]](#s3) | **No — it is derived from Element TD**, by its own account | A browser simplification |
| **Gem TD** (Bryan K.) | **18 Feb 2007** [[6]](#s6) | **No — post-dates it** | Random-roll gems + mazing; the ancestor of *GemCraft*, not of Element TD |
| **Wintermaul / Wintermaul Wars** | early 2000s [[9]](#s9) | Possible as *genre* background only | Free-placement **mazing** + PvP creep sending. Shares no mechanic with Element TD |
| **Legion TD** (Lisk) | **late 2000s** [[10]](#s10) | **No — post-dates it** | Units, not towers; see §4 |

The chronology is the finding. Element TD arrives **before** the two maps most often named alongside it, and the
one that clearly influenced *it* — the general WC3 mazing-TD scene — shares none of its mechanics. **The
combination system does not appear to be inherited from anywhere.**

### 1.3 If you meant something else

Three readings of the original request, and what each would need:

1. **"The WC3 map, i.e. the mod that the Element TD games came from"** — that is Element TD itself, and §2 is the
   report you wanted. *This is the reading this note assumes.*
2. **"A specific earlier map I can half-remember"** — name it and this gets re-pointed. The likeliest candidates
   given how people describe it are Gem TD and Wintermaul Wars, and §1.2 explains why neither fits.
3. **"Whatever the first WC3 TD was"** — genuinely unsettled in the sources available here, and the Hive thread
   [[8]](#s8) is where to settle it.

---

## 2. Tower mechanics of the Warcraft 3 original

Everything in this section is the **Warcraft 3 map**, not Element TD 2. Where the two differ, §3 says so.

### 2.1 The shape of a match

An **eight-player** custom game [[1]](#s1)[[2]](#s2). Each player defends their own shrine; the map is a survivor
TD, not a maze TD — you are not lengthening a path, you are choosing what to build. The archived official
navigation advertises **60 creep waves** and the tower manifest below [[11]](#s11).

### 2.2 The pick system — the actual game

This is the mechanism [Build depth](build-depth-in-tower-defense.md) §2 identifies as the transferable one, and
the WC3 version is where it starts:

- **From wave 5, and every 5 waves after**, an **Elemental Guardian** spawns. Killing it grants you that element
  [[12]](#s12). The tech choice is *paid for inside the simulation*, not chosen from a menu.
- Picks were denominated in **lumber**: one every five waves, **11 in total** over the run [[11]](#s11).
- Picking the same element again **levels it** — to 2, then 3 — and levels must be taken **in order**:
  *"to summon the higher levels, you need to have flattened the lower ones"* [[11]](#s11).
- **Element level gates tower level**: *"You need level three in an element to build the pure tower of that kind"*
  [[11]](#s11).
- **Interest competed in the same offering** — capped at **two per game**, at **1/7** probability per offer, and
  observably clustering mid-game [[4]](#s4).
- **Duplicate high-level picks convert to Essence** rather than being wasted: when the game would hand you a
  tier-3 element you already have, it grants an essence instead, which unlocks further pure towers [[4]](#s4).

Eleven picks against six elements × three levels is **structurally insufficient by design** — six elements at
level 3 would cost eighteen. You cannot go wide *and* deep, and that is the whole strategic tension.

### 2.3 The tower manifest

| Tier | Count | Why that number | Levels |
|---|---|---|---|
| Pure / elemental | 6 | one per element | up to 3, gated on element level [[11]](#s11) |
| **Dual** | **15** | C(6,2) | five-stage upgrade ladders [[11]](#s11) |
| **Triple** | **20** | C(6,3) | as above |
| Support | — | Well (attack speed), Blacksmith (damage per shot) [[5]](#s5) | — |

**41 combination towers from a six-word vocabulary, with no quads.** The subset lattice *is* the content manifest —
and the archived site published it as one [[11]](#s11). Building order was **single → dual → triple**: you built a
pure tower and upgraded it into a combination, rather than placing a triple directly [[2]](#s2).

### 2.4 The counter system

A **ring of six**: Light → Darkness → Water → Fire → Nature → Earth → Light. Each element deals **200%** to the
element it precedes and takes **50%** from the one it follows; everything else is **100%** [[5]](#s5)[[2]](#s2).
Implemented by **remapping Warcraft 3's hardcoded armor and attack types** onto the six elements, with custom
icons imported to display the relationships [[4]](#s4) — which is also *why* the multipliers are exactly
200/100/50: those were the numbers the engine would give them.

⚠️ Note for [the open damage-matrix question](../vision.md#the-open-questions): this is a **4:1 spread** (200% vs 50%), the
same figure [Part V §4.1](../archive/variance-levers-and-unit-schema.md#41-the-scalar-layer--three-shapes-pick-exactly-one)
records for Element TD 2 — so the spread has survived unchanged from 2006 to today across four engines. That is a
much stronger datum for "4:1 works" than a single game's current patch.

### 2.5 The creep layer — a second axis, independent of armor

Creeps carried special abilities that are *orthogonal* to their elemental armor [[5]](#s5):

| Ability | Effect |
|---|---|
| **Fast** | +25% movement speed |
| **Healing** | on death, restores 20% max HP to nearby units |
| **Mechanical** | periodic invulnerability |
| **Undead** | revives once at 33% HP |

This matters more than it looks. The armor ring asks *"which element?"*; the ability layer asks *"does your build
have burst, or sustain, or overkill tolerance?"* — a question the ring cannot ask. Two axes, six words each way.
See the sibling note on wave variety for what other games do with this layer.

### 2.6 Late-life mechanics (4.3b, Jan 2011)

The last WC3-line release shows the map still being tuned as a competitive object: tier-3 elemental boss HP halved
to 37,500 (Karawasa's Essence to 75,000), several towers reworked to distinct attack *shapes* rather than stat
tweaks — boomerang, chain-reaction AoE, ammo-reload, growing-AoE DoT — and "builder weapons" made optional behind a
**Super Weapons** mode [[13]](#s13). ⚠️ Note that *Element TD Survivor* (versions up to 9.x on the map databases)
is a **separate community line**, not Karawasa's; do not read its version numbers as continuous with 4.3b.

---

## 3. What changed on the way to Element TD 2

Only the deltas; [Build depth](build-depth-in-tower-defense.md) §2 has ETD2 measured in full.

| | WC3 Element TD (2006–2011) | Element TD 2 (2020–) |
|---|---|---|
| Elements | 6 | 6 + **Composite** (non-elemental, 90%) [[2]](#s2) |
| Top tier | Triple (20) | **Quad** (15, added 12 Dec 2021) — 59 towers total |
| Combinatorial completeness | Complete for k ≤ 3 | **Not** complete — no five-element tower |
| Tier levels | pure ≤ 3, ladders per tower [[11]](#s11) | 4/3/2/1 by tier — duals to level 3, triples one upgrade [[2]](#s2) |
| Picks | 11, lumber, every 5 waves [[11]](#s11) | 11, one at start + every 5 to wave 50 — **identical budget** |
| Interest | **A pick**, capped 2, 1/7 odds [[4]](#s4) | Not a pick |
| Essence | Consolation for duplicate tier-3 [[4]](#s4) | A resource: 2 from the essence pick + boss, free at rounds 50 and 56 [[2]](#s2) |
| Support towers | Well, Blacksmith [[5]](#s5) | Folded into the tower roster |
| Boss armor | — | Takes **10% from any source**; used for post-wave-55 creeps [[2]](#s2) |

**The system that survived fifteen years and four engines is the pick meter and the ring.** The parts that got
replaced are the parts that were *Warcraft 3 implementation detail* — support towers papering over WC3's stat
model, Interest sharing the offering because there was no second UI for it.

---

## 4. Element TD vs Legion TD — one question, opposite answers

**Legion TD**, by **Lisk**, Warcraft 3: The Frozen Throne, late 2000s; later open-sourced [[10]](#s10). 2–8
players. You buy **fighters** — units, not buildings — with gold; **lumber** buys units you send at opponents; and
the two currencies cross-feed, wisps bought with gold raising lumber income and lumber spending raising gold
[[10]](#s10). **Legion TD 2** (AutoAttack Games, 20 Nov 2017) [[10]](#s10) keeps the shape: 113 fighters as of
8.03, workers at 50 gold generating 1 mythium per 10s, and **25 mercenaries from 20 (Snail) to 400 (Kraken)
mythium**, where spending mythium *permanently and immediately* raises your gold income [[14]](#s14)[[15]](#s15).

| Axis | Element TD (WC3) | Element TD 2 | Legion TD / Legion TD 2 |
|---|---|---|---|
| What you place | Towers (buildings) | Towers | **Fighters** (units that die and revive each round) |
| Where the depth is | **The tech tree** — 11 metered picks | Same | **The economy** — income vs. defense vs. sending |
| What you fight | A **fixed, public** creep script | Same | A wave *plus* whatever your opponent bought |
| Counter system | Ring of 6, **4:1** spread | Ring of 6 + Composite, 4:1 | Damage/armor matrix, **1.67:1** — much gentler |
| Randomness | Which elements you're *offered* | Pick / AllPick / AllRandom / SameRandom | Which fighters you're *offered* (a roll of ~6–10) |
| Opponent interaction | **None** — parallel solitaire, scored by survival | Same, plus co-op/versus modes | **The entire game** |
| Economy | One purse + Interest | One purse | **Two purses that cross-feed** |
| Failure state | Lives → shrine falls | Same | Leaks damage **the king**; you lose when it dies |

**The one line worth keeping.** Element TD's tension is *internal* — eleven picks, eighteen picks' worth of
ambition. Legion TD's tension is *external* — every coin is a bet about a person. This project's vision
([both boards, one purse](../vision.md#one-purse--restored-6-august-2026)) is asking for **both at once**, which is why the
[attacking-half note](attack-composition-and-sending.md) §1.1 found the income loop missing: Legion TD's tension is
purchased with a second currency, and Element TD never needed one because it never had an opponent.

---

## 5. What this note actually changes for the project

Three things, all small, none of them a decision:

1. **The 4:1 damage spread has a fifteen-year track record**, not a one-patch one (§2.4). That strengthens one side
   of [the open damage-matrix question](../vision.md#the-open-questions).
2. **Interest-as-a-pick is a shipped answer to the one-purse problem** (§2.2) — an economy upgrade and a tech
   upgrade competing for the same metered offering, with a hard cap so it cannot run away. It is worth
   [the attacking half](attack-composition-and-sending.md) §5 knowing that this existed and was capped at two.
3. **The creep ability layer is a second orthogonal axis on the cheap** (§2.5) — four abilities that ask a question
   the armor ring cannot. That thread continues in the sibling note.

---

## Sources

<a id="s1"></a>1. **Element TD map records** — [wc3maps.com](https://wc3maps.com/map/195674),
[EpicWar](https://www.epicwar.com/maps/196175/). Eight-player WC3 custom game by Karawasa & mrchak; first version
23 October 2006. *Map-database record.*

<a id="s2"></a>2. **Element TD 2 official site and wiki material**, [eletd.com](https://www.eletd.com/) and the
Element TD 2 wiki. Six elements + Composite; armor *"takes additional damage from the element preceding it and
retains near-invulnerability from the element following it"*; single → dual → triple → quad; duals to level 3,
triples one upgrade; essence at rounds 50/56; boss armor 10%. ⚠️ The Fandom wiki returns **HTTP 402** to automated
fetching — these figures came via search-result extracts, not a direct read. *Mixed first-party / community.*

<a id="s3"></a>3. **Flash Element TD**, [Wikipedia](https://en.wikipedia.org/wiki/Flash_Element_TD). David Scott,
January 2007; *"the map and name is based on the 'Element TD' map created for Warcraft III by Brian Powers and Evan
Hatampour."* **Establishes the derivation direction.**

<a id="s4"></a>4. **"Element TD — How Things Work"**,
[forums.eletd.com](https://forums.eletd.com/topic/271-element-td-how-things-work/). Interest capped at two, 1/7
odds; duplicate tier-3 → essence; WC3 armor/attack types remapped to six elements with 200/100/50 multipliers.
*Official forum, developer-adjacent — semi-official.*

<a id="s5"></a>5. **"Basics of Element TD"**,
[forums.eletd.com](https://forums.eletd.com/topic/945-basics-of-element-td/). The ring (*"light — 200% to darkness,
50% to earth"*); Well and Blacksmith; Fast / Healing / Mechanical / Undead. *Player-written, published on the
official site — semi-official.*

<a id="s6"></a>6. **Gem Tower Defense** — [Alchetron](https://alchetron.com/Gem-Tower-Defense),
[GitHub: nvs/gem](https://github.com/nvs/gem). Created by Bryan K.; released **18 February 2007**. *Community
encyclopaedia — medium confidence on the exact date, high on the year.*

<a id="s7"></a>7. **Element TD 2 press page**, [eletd.com/press](https://www.eletd.com/press). *"The culmination of
15 years of multiplayer tower defense development"*; 5M+ downloads across WC3, SC2 and Dota 2; Evan Hatampour,
founder of Element Studios. **First-party.** Contains **no** statement of what inspired the original.

<a id="s8"></a>8. **"Warcraft 3 Tower Defense roots, which mod was first?"**,
[Hive Workshop](https://www.hiveworkshop.com/threads/warcraft-3-tower-defense-roots-which-mod-was-first.291585/).
⚠️ **Not read** — Hive returns 403 to automated fetching. Listed because it is the most likely place a first-hand
answer to §1 exists.

<a id="s9"></a>9. **Wintermaul Wars history**,
[maultactics.gg](https://maultactics.gg/articles/wintermaul-wars-history). ⚠️ **Low trust** — a fan/SEO site
(published by "GRANT_WORKS", authored "Beagboi Games", dated July 2026) with no sourcing of its own. Used only for
the uncontroversial claim that Wintermaul Wars is early-2000s mazing-plus-sending. Do not cite for anything else.

<a id="s10"></a>10. **Legion TD**, [Wikipedia](https://en.wikipedia.org/wiki/Legion_TD). Lisk, WC3:TFT, late
2000s; gold buys defenders, lumber buys attackers, wisps cross-feed; 2–8 players; Legion TD 2 by AutoAttack Games,
20 November 2017.

<a id="s11"></a>11. **Karawasa's archived `eletd.com`** — the 4.0-beta navigation (*"6 Elemental Towers / 15 Dual
Towers / 20 Triple Towers / 22 Build Lists / 60 Creep Waves"*), the Help→Element Table page, the 11-lumber pick
budget, and *"You need level three in an element to build the pure tower of that kind."* **First-party, archived.**
⚠️ Quoted here **via [Build depth in tower defense](build-depth-in-tower-defense.md) [[19]](build-depth-in-tower-defense.md#s19)[[20]](build-depth-in-tower-defense.md#s20)**,
which read it directly; `web.archive.org` is blocked from this environment and could not be re-verified.

<a id="s12"></a>12. **Elemental Guardian mechanic** — map descriptions on
[gaming-tools.com](https://gaming-tools.com/warcraft-3/element-td/) and the EpicWar/wc3maps records: *"beginning at
level 5 and for every 5 levels after that, the player chooses an Elemental Guardian to summon"*, killed to grant
the element. *Community.*

<a id="s13"></a>13. **Element TD 4.3b changelog**,
[forums.eletd.com](https://forums.eletd.com/topic/2258-wc3-element-td-43b/), 2 January 2011. Boss HP nerfs, tower
reworks, Super Weapons mode. **First-party.**

<a id="s14"></a>14. **Legion TD 2 — Mercenary**, [legiontd2.wiki.gg](https://legiontd2.wiki.gg/wiki/Mercenary).
20–400 mythium across 25 types; power mercs give reduced income; spawn position by type and cost. **Contains no
mercenary upgrade or levelling system** — relevant to the sibling note. *Community wiki.*

<a id="s15"></a>15. **Legion TD 2 mechanics** — [legiontd2.wiki.gg](https://legiontd2.wiki.gg/) and community
guides. 113 fighters at 8.03; workers 50 gold → 1 mythium/10s; income rises permanently when mythium is spent.
*Community.*
