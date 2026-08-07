# Creep wave variety, and whether anyone lets you upgrade creeps the way you upgrade towers

**Research note** · 3 August 2026 · commissioned directly by the developer

**Question:** which tower defense games have gone deep on **creep/wave variety** — and does any game let you
**upgrade the creeps** the way you upgrade towers?

**Inputs / prior art in this repo:** [The attacking half](attack-composition-and-sending.md) owns *sending as a
competitive mechanism* (income loops, timing, ordering, denial) and is not repeated here.
[Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half) owns the lever
catalogue. **What is new here is the creep side as a *progression system*** — the enemy as something that gets
upgraded, by someone, on purpose.

---

## Bottom line

### Wave variety is manufactured four structurally different ways, and only one of them scales: **orthogonal properties that stack onto existing types**. Bloons TD 6 is the depth benchmark not because it has 22 bloon types but because *camo × regrow × fortified* multiply against a fixed type list instead of adding to it.

### Creep upgrade systems exist and there are three distinct families of them — but **the literal thing you asked about, a persistent upgrade tree you spend on to make your creeps stronger, has essentially one clean shipped example: _Tower Wars_ (2012).** Everything else either upgrades creeps *indirectly* (buy better ones from a tech ladder) or upgrades them *for the defender's benefit* (difficulty-for-reward toggles).

Four claims carry the note.

**One. There are two independent axes on the creep side, and most games only build one.** An **identity** axis
(what kind of thing is it — armor class, element, family) and a **property** axis (what modifiers are stapled on —
camo, fortified, regrow, shielded, fast). Element TD's WC3 original had both and they were cleanly separated: a
six-element armor ring *plus* four abilities that are indifferent to element — Fast, Healing, Mechanical, Undead
[[1]](#s1). The armor ring asks *"which damage type?"*; the ability layer asks *"does your build have burst, or
sustain, or overkill tolerance?"* — a question a damage matrix structurally cannot ask.

**Two. BTD6 is the deep end, and the reason is multiplicative, not additive.** 22 bloon types [[2]](#s2), but the
depth is that **Fortified doubles HP (quadruples on Lead), and Camo, Regrow and Fortified compose** — *Camo Regrow
Fortified* is a real, common enemy [[2]](#s2). The MOAB class then uses composition as *authored bosses*: DDT
combines Camo + Lead + Black at extreme speed; BAD is immune to all slows, knockbacks and instakills [[2]](#s2).
Three orthogonal booleans over a type list is a bigger design space than three times as many types, and it is
cheaper to author and cheaper to explain.

**Three. Every "upgrade your creeps" system in the genre is really one of three things**, and they are not
interchangeable — §3 separates them:
(a) **send economies**, where creeps are your offense and the "upgrade" is a tech ladder and an income loop
(Wintermaul Wars, Line Tower Wars, Legion TD/2, Bloons TD Battles, Direct Strike / Nexus Wars);
(b) **difficulty-for-reward buffs**, where you upgrade the creeps attacking *you* in exchange for a payout multiplier
(GemCraft Battle Traits, Sanctum 2 Feats of Strength);
(c) **reverse TD**, where the creeps are simply your army (Anomaly).
Only (a) and (c) put the upgrade on the unit; (b) puts it on the *battle*.

**Four. The quadrant that is nearly empty is _PvE with a creep-side upgrade tree_ — and the reason it is empty is
a real design problem, not an oversight.** In PvP, buffing your creeps is self-evidently worth gold because it
hurts a person. In PvE, spending your own resources to make your enemies stronger is only a decision if it *pays* —
so the two games that do it both had to bolt on an explicit reward multiplier (GemCraft: XP per trait level;
Sanctum 2: +20% XP per feat, to +100%) [[3]](#s3)[[4]](#s4). **If you want a creep upgrade tree in a PvE loop, the
payout curve is the design, not the tree.**

---

## 1. Four ways wave variety gets manufactured

| # | Mechanism | The decision it creates for the player | Shipped in | Authoring cost | Scales? |
|---|---|---|---|---|---|
| 1 | **Identity / armor class** — enemies belong to one of *n* kinds, countered by damage type | "Which damage type do I own?" | Element TD's ring of 6 [[1]](#s1); CreepWars TD's light/medium/heavy [[5]](#s5); Legion TD 2's matrix | One number per pair — a **ring** costs *n*, a **matrix** costs *n²* | Yes, if it's a ring |
| 2 | **Orthogonal properties** — booleans stapled onto any type | "Does my build cover *every* property, or just most?" | **BTD6: camo / regrow / fortified, composable** [[2]](#s2) | One rule per property, reused across all types | **Best in class** |
| 3 | **Abilities that change how towers behave** — not stats, behaviour | "Is my damage the *right shape*?" | Element TD: Fast / Healing / Mechanical / Undead [[1]](#s1); **Infinitode 2** [[6]](#s6) | One implementation each; interacts with everything | Medium — this is where balance bugs live |
| 4 | **Authored pacing and bosses** — specific waves as set pieces | "Am I ready for round 40?" | BTD6's MOAB ladder at rounds 40/60/80/100 [[2]](#s2); Element TD 2's boss armor (10% from any source) after wave 55 | Pure content | No — but it's what people remember |

**Infinitode 2 is the sharpest example of row 3** and is under-cited in TD design writing. With only **11 enemy
types** [[6]](#s6) it gets variety from abilities that attack the *tower model* rather than the damage numbers:

- **Light** — after taking damage from a tower it becomes ~80% resistant *to that specific tower* for 6 seconds,
  then the ability restores after 4 [[6]](#s6). This punishes single-source damage and rewards tower *diversity* —
  a thing no armor matrix can express.
- **Armored** — blocks 50% of damage taken by *other nearby enemies* [[6]](#s6). Positional, so it makes wave
  *ordering* matter to the defender.
- **Healer** — completely immune to one specific tower [[6]](#s6).
- **Bonus / "star" enemies** — an optional challenge the player accepts or denies [[6]](#s6). This is
  difficulty-for-reward (§3b) at single-enemy granularity.

**CreepWars TD** (2020) is the brute-force end of row 1: **50+ creeps with individual abilities** across three armor
classes, against **three damage types × three delivery methods** (pierce/blunt/magic × direct/splash/DoT)
[[5]](#s5). Worth knowing as an existence proof that a 3×3 damage vocabulary can carry fifty enemies — you do not
need a large *tower* vocabulary to justify a large creep roster.

---

## 2. The identity/property split, stated as a rule

The transferable rule from §1 is small enough to write in one line:

> **Types should be few and properties should be many, because properties compose and types do not.**

Six types with three composable booleans is 48 distinct enemies from nine authored things. Forty-eight types is 48
authored things and a manual. BTD6 sits at one end (few types, composable properties); CreepWars TD sits at the
other (many types, no composition) — and CreepWars TD is not the one people cite for wave design.

⚠️ For [Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half): this argues
the creep schema wants a **flags/modifier list** alongside its type, not just a type. That is a schema shape
decision and it is cheap now and expensive later.

---

## 3. Creep upgrade systems — three families

### 3a. Send economies — creeps as your offense, upgraded via a tech ladder

This family is [the attacking half](attack-composition-and-sending.md)'s subject and is summarised here only for
completeness of the survey.

| Game | How "upgrading your creeps" actually works |
|---|---|
| **Wintermaul Wars** (WC3) | Sending *is* the income mechanism — the units you pay to unleash raise your income for the rest of the match ⚠️ [[7]](#s7) |
| **Line Tower Wars** (WC3) | Buy creeps to raise income per round; **cheap creeps give more income, expensive creeps steal more lives** — the upgrade decision is an explicit economy/pressure trade [[8]](#s8) |
| **Legion TD 2** | 25 mercenaries, 20–400 mythium; spending mythium **permanently raises gold income**; "power mercs" are stronger but pay less income [[9]](#s9). ⚠️ **There is no mercenary upgrade or levelling system** — you buy a *different, better* merc, you never improve one |
| **Bloons TD Battles / 2** | Eco: income every 6s, raised by sending bloons. Low-tier sends raise eco, high-tier sends carry an eco penalty; spaced sends are the most cost-efficient, grouped raise income fastest [[10]](#s10) |
| **Direct Strike / Nexus Wars** (SC2) | You build **spawners**, and higher-tech structures produce a bigger army — the closest thing in this family to a literal creep tech tree [[11]](#s11) |

**The pattern across all five: you upgrade the *purchase*, not the *unit*.** Progression is "unlock and afford a
better creep", never "make this creep stronger". That is the gap Tower Wars fills.

### 3b. Difficulty-for-reward — you upgrade the creeps attacking *you*

This is the family that most resembles a tower upgrade tree pointed backwards, and it is the answer for a
single-player loop.

**GemCraft — Battle Traits** [[3]](#s3). Before a level you toggle traits, each with levels, and **each trait level
multiplies the XP payout**. The traits are genuine creep upgrades:

| Trait | What it upgrades on the monsters |
|---|---|
| **Hatred** | Large flat HP boost |
| **Awakening** | HP boost that **compounds every wave** |
| **Adaptive Carapace** | Damage taken *reduces after each hit* — an anti-single-target rule |
| **Insulation** | Monsters arrive with shield layers that must be stripped before HP is touched |
| **Overcrowd** | More monsters — and it is *the only trait with no penalty*, because more monsters means more mana and more kills |

That last row is the most interesting single fact in this note: **one of the "make it harder" options is a
straight-up player benefit**, and the design ships it anyway. A creep-upgrade menu does not have to be monotonically
worse for the player to be a good menu.

**Sanctum 2 — Feats of Strength** [[4]](#s4). Sanctum 2 has **no difficulty levels at all**; instead five optional
feats, each **+20% XP, up to +100%** with all five. Verified effects include enemies gaining **+50% HP**, attacking
**~40% faster**, regenerating **150 HP/s**, and moving faster; a "Hardcore" feat changes *your* rules instead
(respawn next build phase, 50% refund on recycling). In survival/sandbox a **random feat auto-activates every four
waves** (5, 9, 13, 17, 21) — i.e. the game upgrades the creeps *for* you, on a schedule, as the difficulty curve.
⚠️ The mapping of feat *names* to effects in [[4]](#s4) looks garbled (one feat's name does not match its stated
effect); treat the effect list as sound and the naming as unverified.

**Why this family is the interesting one for a PvE-shaped game:** the "tree" is a set of dials with a payout
multiplier attached, so it needs no second currency, no opponent, and no new UI surface beyond a pre-battle menu.
Sanctum 2 additionally proves it can *replace* a difficulty selector entirely.

### 3c. Reverse TD — the creeps are your army, and you upgrade them mid-battle

**Anomaly: Warzone Earth** (11 bit studios, 2011) [[12]](#s12). You control a convoy of up to six vehicles walking
a route past enemy towers; destroying towers earns money spent on **new units or upgrades to existing ones, during
the battle**. This is the only surveyed game where the creep side has the full tower-defense progression shape —
buy, place (in the convoy order), upgrade, mid-run — because it simply *swapped which side the player is on*.

Note that [the attacking half](attack-composition-and-sending.md) §1 already cites Anomaly for **ordering**; the
upgrade half is the part relevant here.

---

## 4. The clean answer: Tower Wars (2012)

**This is the game that matches the question as asked.** Tower Wars (SuperVillain Studios, 14 August 2012) is a
competitive TD where you build towers *and* buy an army to send, and the army has **its own upgrade tree that buffs
the units themselves** [[13]](#s13):

- **A unit roster with roles**, unlocked and purchased with gold and Battle Points — a basic grunt (Mr. Moopsy), a
  **healer** (Baron von Pepto), a **shield-booster** (Madam Sudsie Lennor), a **tank** (Stanley Clunkerbottom),
  among others [[13]](#s13). Support creeps whose job is to keep other creeps alive is itself notable — it is the
  tower-support pattern (Element TD's Well and Blacksmith) applied to the attacking side.
- **Upgrades bought for the units, not for the purchase**: health, armor, shields, speed, and **Battle Point accrual
  rate** [[13]](#s13). That last one is an *economy upgrade on the creep line* — the creep equivalent of a farm.
- **The castle upgrades too** (armor, gunners), so both halves of the loop have a spend target [[13]](#s13).
- Eight tower types on the defensive half [[13]](#s13).

So the honest answer to "is there a game where creeps upgrade like towers?" is: **yes — Tower Wars, and it is the
only one in this survey where the upgrade lands on the creep's statline rather than on which creep you can afford.**

⚠️ **Sourcing caveat.** The Tower Wars detail here rests on a 2012 Destructoid review and a Steam community guide
[[13]](#s13), not on a wiki or first-party documentation; the game is long unsupported. Treat the *shape* as solid
and any specific number as unverified.

---

## 5. The map of the space, and the hole in it

| | **Creeps are yours (you send them)** | **Creeps are the enemy's (they attack you)** |
|---|---|---|
| **Upgrade = buy a better one** | Legion TD 2, Line TW, BTD Battles, Direct Strike [[8]](#s8)[[9]](#s9)[[10]](#s10)[[11]](#s11) | Every ordinary PvE TD — the wave script escalates on rails |
| **Upgrade = improve the unit's stats** | **Tower Wars** [[13]](#s13); Anomaly (mid-battle) [[12]](#s12) | **GemCraft Battle Traits; Sanctum 2 Feats** — but priced as difficulty, with an XP multiplier [[3]](#s3)[[4]](#s4) |

**The hole:** a game where a PvE player spends a *run resource* on a creep upgrade tree that meaningfully changes
*how the waves play*, rather than just scaling their HP for XP. GemCraft's Adaptive Carapace and Insulation are the
only surveyed traits that change wave *behaviour* rather than wave *magnitude* — and they are two rows in a menu
otherwise made of multipliers.

That hole is real, and §0's fourth claim says why it stays open: the payout curve is harder than the tree.

---

## 6. What transfers to this project

Stated as observations, not decisions — the match-format and roster seams own the calls.

1. **Give creeps a property list, not just a type** (§2). BTD6's whole advantage is that three booleans multiply
   against the type list. This is a [Part V §3.6](../archive/variance-levers-and-unit-schema.md#36-wave-and-spawn--the-composition-half)
   schema shape and it is cheap to add now.
2. **The ability layer is the cheapest depth on the creep side, and it is orthogonal to the damage ring** (§1 row
   3). Element TD shipped four abilities in 2006 [[1]](#s1); Infinitode 2 gets an entire game out of eleven
   enemies because its abilities attack the *tower model* [[6]](#s6). This is the lever that survives the one-hex
   corridor untouched.
3. **If a creep upgrade tree is wanted, Tower Wars is the reference implementation and "Battle Point accrual" is
   the row to steal** (§4) — an income upgrade sitting on the attacking line, which is exactly the shape
   [the attacking half](attack-composition-and-sending.md) §1.1 found missing under
   [one purse](../vision.md#one-purse--restored-6-august-2026).
4. **Sanctum 2's Feats are a shipped replacement for a difficulty selector** (§3b) — five toggles, +20% reward each,
   with the game auto-enabling one every four waves in endless. If this project ever wants an endless mode, that is
   the pattern, and it costs one multiplier.

---

## Not investigated

Named for completeness so their absence is not read as a finding. Each is plausibly relevant and none was checked:
**Plants vs. Zombies** (widely regarded as the deepest *authored* enemy taxonomy), **Kingdom Rush**, **Defense
Grid**, **Rogue Tower**, **Orcs Must Die! Unchained** (siege mode is reportedly a minion-deck send system — the
search for it failed), **Creeper World**, **Mindustry**, **Mazebert TD**. PvZ and Orcs Must Die! Unchained are the
two most likely to change §5's table.

---

## Sources

<a id="s1"></a>1. **"Basics of Element TD"**,
[forums.eletd.com](https://forums.eletd.com/topic/945-basics-of-element-td/). The six-element ring; creep abilities
Fast (+25% speed), Healing (20% max HP to nearby on death), Mechanical (periodic invulnerability), Undead (revives
once at 33% HP). *Player-written, published on the official site — semi-official.* See also the sibling note,
[Element TD's ancestry](element-td-ancestry-and-wc3-tower-mechanics.md).

<a id="s2"></a>2. **Bloons TD 6 — Bloon Properties / Bloon Types**,
[Bloons Wiki](https://bloons.fandom.com/wiki/Bloon_Properties),
[List of BTD Bloons](https://bloons.fandom.com/wiki/List_of_BTD_Bloons). 22 bloon types; Fortified doubles HP
(quadruples Lead); Camo/Regrow/Fortified compose; Purple immune to fire, plasma and energy; MOAB (R40) → BFB (R60,
contains 4 MOABs) → ZOMG (R80) → BAD (R100, immune to slows, knockbacks, instakills); DDT combines Camo + Lead +
Black at extreme speed. *Community wiki, well maintained.* ⚠️ Read via search extracts, not a direct page fetch.

<a id="s3"></a>3. **GemCraft — Battle Traits**,
[Gemcraft Wiki](https://gemcraft.fandom.com/wiki/Battle_Traits_(GCFW)) and
[(GCL)](https://gemcraft.fandom.com/wiki/Battle_Traits_(GCL)). Risk/reward: each trait level applies an XP
multiplier. Hatred (monster HP), Awakening (HP per wave), Adaptive Carapace (damage taken falls after each hit),
Insulation (shield layers), Overcrowd (more monsters, no penalty). *Community wiki.*

<a id="s4"></a>4. **Sanctum 2 — Feats of Strength**,
[Sanctum Game Wiki](https://sanctumgame.miraheze.org/wiki/Sanctum_2:Feats_of_Strength). No fixed difficulty levels;
five feats; **+20% XP each, +100% with all five**; effects include +50% enemy HP, ~40% faster enemy attacks, 150
HP/s regeneration, faster movement; Hardcore alters player rules. In sandbox/survival a random feat activates on
waves 5, 9, 13, 17, 21. ⚠️ **Feat-name-to-effect mapping looks garbled in the source** — effects trusted, names not.

<a id="s5"></a>5. **CreepWars TD**, [Steam store page](https://store.steampowered.com/app/1345120/CreepWars_TD/).
50+ creeps with individual abilities; three armor classes (light/medium/heavy); three damage types (pierce, blunt,
magic) × three delivery methods (direct, splash, DoT); 25+ defense units with multiple upgrade paths. **PvE only —
despite the name there is no creep sending.** **First-party.**

<a id="s6"></a>6. **Infinitode 2 — Enemies**, [Infinitode 2 Wiki](https://infinitode-2.fandom.com/wiki/Enemies).
11 enemy types; Light (becomes ~80% resistant to the tower that hit it for 6s, restoring after 4s); Armored (blocks
50% of damage to nearby enemies); Healer (immune to Flamethrower); bonus/"star" enemies the player may accept or
deny. *Community wiki.* ⚠️ Read via search extracts.

<a id="s7"></a>7. **Wintermaul Wars history**,
[maultactics.gg](https://maultactics.gg/articles/wintermaul-wars-history). ⚠️ **Low trust** — unsourced fan/SEO
site. Used only for the uncontroversial claim that sending raises the sender's income.

<a id="s8"></a>8. **Line Tower Wars walkthrough**,
[ayumilove.wordpress.com](https://ayumilove.wordpress.com/2009/04/16/line-tower-wars-walkthrough-tower-defense/).
Creeps bought for income; cheaper creeps yield more income, expensive ones steal more lives; send to the player on
your right. *Community guide.*

<a id="s9"></a>9. **Legion TD 2 — Mercenary**, [legiontd2.wiki.gg](https://legiontd2.wiki.gg/wiki/Mercenary).
25 mercenaries, 20 (Snail) to 400 (Kraken) mythium; power mercs trade income for strength; spawn position by type
and cost. **No upgrade or levelling system for mercenaries.** *Community wiki.*

<a id="s10"></a>10. **Bloons TD Battles eco** — [Bloons Wiki: Eco (BTDB2)](https://bloons.fandom.com/wiki/Eco_(BTDB2))
and Steam community guides. Income every 6 seconds, raised by sending; low-tier sends raise eco, higher tiers carry
penalties; spaced sends most cost-efficient, grouped raise income fastest. *Community.*

<a id="s11"></a>11. **Direct Strike / Nexus Wars** —
[Blizzard news](https://news.blizzard.com/en-us/article/21700687/new-in-the-arcade-direct-strike-and-ark-star),
[StarCraft Wiki](https://starcraft.fandom.com/wiki/Direct_Strike). Players build structures that spawn units in
waves; more and higher-tech structures produce a bigger army; pylons give income but produce nothing.
*First-party + community.*

<a id="s12"></a>12. **Anomaly: Warzone Earth**,
[Steam](https://store.steampowered.com/app/91200/Anomaly_Warzone_Earth/) and
[Anomaly Wiki](https://anomalygame.fandom.com/wiki/Anomaly:_Warzone_Earth). 11 bit studios, 8 April 2011. Reverse
TD; convoy of up to six units; *"gather resources to buy new units and upgrade your squad during a battle."*
**First-party store copy + community.**

<a id="s13"></a>13. **Tower Wars** (SuperVillain Studios, 14 August 2012) —
[Destructoid review](https://www.destructoid.com/reviews/review-tower-wars/) and
[Steam guide: Units and Upgrades](https://steamcommunity.com/app/214360/discussions/1/864945865119020302).
Unit roster with support roles; upgrades buff unit **health, armor, shields, speed and Battle Point accrual**;
castle upgrades (armor, gunners); eight tower types. ⚠️ **Press + community only**, no first-party documentation
still online.
