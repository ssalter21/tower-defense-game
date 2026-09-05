# The roster

**The design side of [`content/units.txt`](../content/units.txt).** That file holds the numbers the simulation
reads; this one holds what each unit is *for*, what it looks like, and what about it is still unsigned. Where a
number appears here it is a **proposal** until it appears there.

This is a working document. It is meant to be opened, argued with and edited every time new gameplay is
specified, so it is written line-by-line rather than as a wide table — a wide table is unreadable in a diff and
miserable to edit by hand.

## How to edit this

**One block per unit, the same five lines every time.** Leave a line blank when it is undecided. **A blank is
not an omission, it is the ask** — the blanks are the agenda for the next conversation.

| Line | What goes on it |
|---|---|
| `Does` | The mechanic, in the terms the simulation would have to implement |
| `Looks` | The art direction — model, silhouette, what reads at a glance |
| `Numbers` | Only what has actually been decided. `_` for what has not |
| `Needs` | What the schema or the engine would have to gain. `nothing` means it is authorable today |
| `Open` | The question that has to be settled before it can be signed |

**Status is one of four words.** `proposed` — written here and nowhere else. `signed` — the numbers are agreed.
`live` — there is a row in `content/units.txt`. `retired` — there was one, and there is not now.

**Ids come from `units.txt`'s one global space and ascend forever.** Never reused, never an index, never
reserved in advance — the next unit built takes id 50, whatever it is. **A tier is its own id and its own row**;
the Sergeant is not the Soldier with a flag set.

## What things cost

**Neither side of the purse is authored. Both are arithmetic**, and they are priced in the same quantity so
that one wallet can buy both — which is what [§3's one purse](vision.md#one-purse) actually requires.

| | The rule | Which means |
|---|---|---|
| **A creep** | effective health ÷ 160 | You pay for the health a defense must spend to stop it. Effective health is the pool times the armour multiplier |
| **A tower** | one gold per **5 damage a second**, times the bodies a shot hits | You pay for the health it removes |

Signing a creep therefore means signing **health, speed and armour**; the price follows. Signing a tower means
signing **damage, cooldown and how many bodies it hits**.

> ⚠️ **Two honest gaps in the tower rule.**
>
> **It does not price range.** A one-hex tower and an eight-hex tower with the same damage cost the same, so
> the Soldier being cheap is an accident of the formula agreeing with the design rather than the formula
> knowing that reach is worth paying for.
>
> **The constant is tied to the tick rate.** "Five damage a second" is a number about seconds. If
> [the clock](#the-clock) moves, re-derive the constant or every tower silently stops being based.

**The rule does not reach a capstone, because gold does not buy one.** A run is granted **one capstone token
at rounds 3, 6 and 9** — three a run against nine capstones — and the token is the whole price. The cost
column prices what gold buys, and the top of a line is not in it. That retires the shallow-U exemption this
section used to reserve: an exemption to a gold rule, for a thing gold does not buy, is a clause about
nothing.

> **The token is a currency, not a gate.** It goes up on those three rounds and it is spent on a capstone
> edge; that is the whole mechanic. The gates were deleted on
> [13 August 2026](decision-log.md#13-august-2026-later--the-gates-come-out-and-the-client-comes-before-the-roster)
> and they are not coming back with it — no capacity schedule, no per-wave type limit, no offering. The
> [14 August proposal](decision-log.md) carried both halves; **only the token half is taken.**

**Scarcity is the grant schedule, not the price, and that is deliberate.** Five of the nine capstones change
neither the damage roll nor the bodies a shot hits — Shield Wall, Blessing, Consecration, Overgrowth and
Unravel are auras or debuffs — so the damage rule prices each of them *identically to the rung below it*.
That is not an oversight to be corrected with an authored premium. What makes spending a token a decision is
that there are three of them and nine places to put one. **Do not author a capstone premium**; report what the
sweep says about a capstone and leave the number alone.

> **Seven price flat, not five, and the extra two are Slam and Mortar.** Both spread one roll over a bubble,
> and the rule's bodies term is the `targets` column — which a bubble row must leave at 1, because a damage
> bubble is *one* shot drawing *one* roll. So the rule counts one body for a swing that hits everything
> touching the Barbarian, and the row prices flat against the Berserker. **This is the Mage's gap again**, one
> rung higher: a bubble's worth is a radius, and radius is what the rule does not price. Measured when the
> rows were authored on 5 September 2026, and left standing.

**The two that do move the rule's inputs still price under it** — Fan of Knives sets `targets` to 3, and
Overwatch changes the damage roll and the cooldown — but nothing is *charged* that price, because a capstone
is bought with a token. The rule's output on those rows is a reading, not a bill.

## The clock

**Thirty ticks a second.** Every duration in `units.txt` — cooldown, windup, backswing, flight, dying — is in
ticks, and speed is thousandths of a hex per tick. The board is a **51-hex corridor that folds and climbs
through three tiers**.

| | |
|---|---|
| Minion walking speed | 0.84 hexes/sec |
| Time to cross the board | 61 sec |
| One wave | ~3 min |
| Archer rate of fire | 1.67/sec |
| Mage rate of fire | 0.56/sec |

**Two speed relationships are load-bearing.** The Scout walks at exactly **twice** the Minion — 56 against 28 —
so two bodies are level for exactly one tick as one passes the other, which is the case the target-selection
tiebreak exists for. The Skeleton Mage at 33 and the Warrior at 18 are deliberately *not* whole multiples, so a
pass that lands between ticks exists as well. A merely-different speed silently deletes one of those two cases.

**The twelve rows added on 5 September 2026 populate both cases and neither is an accident.** The Shade's 84 is
exactly three Minions and the Bone Golem's 14 exactly half of one, so both are passed on whole ticks; 12, 16,
22, 44 and 50 are none of them multiples. The roster now spans **a factor of seven in speed**, from the
Abomination at 12 to the Shade at 84.

## The index

| id | unit | role | tier | status | label in `units.txt` |
|---|---|---|---|---|---|
| 1 | Minion | creep | — | live | `minion` |
| 2 | Skeleton Scout | creep | — | live | `skeleton-scout` |
| 3 | Archer | tower | 1 | live | `archer` |
| 4 | Mage | tower | 1 | live | `mage` |
| 7 | **Skeleton Mage** | creep | — | live | `skeleton-mage` |
| 11 | Soldier | tower | 1 | live | `soldier` |
| 12 | Skeleton | creep | — | live | `skeleton` |
| 13 | Skeleton Warrior | creep | — | live | `skeleton-warrior` |
| 14 | Ranger | tower | 2 | live | `ranger` |
| 15 | Sergeant | tower | 2 | live | `sergeant` |
| 16 | Shield Wall | tower | 3 | live | `shield-wall` |
| 17 | Barbarian | tower | 1 | live | `barbarian` |
| 18 | Berserker | tower | 2 | live | `berserker` |
| 19 | Slam | tower | 3 | live | `slam` |
| 20 | Paladin | tower | 1 | live | `paladin` |
| 21 | Templar | tower | 2 | live | `templar` |
| 22 | Blessing | tower | 3 | live | `blessing` |
| 23 | Cleric | tower | 1 | live | `cleric` |
| 24 | Bishop | tower | 2 | live | `bishop` |
| 25 | Consecration | tower | 3 | live | `consecration` |
| 26 | Sorcerer | tower | 2 | live | `sorcerer` |
| 27 | Unravel | tower | 3 | live | `unravel` |
| 28 | Druid | tower | 1 | live | `druid` |
| 29 | Elder | tower | 2 | live | `elder` |
| 30 | Overgrowth | tower | 3 | live | `overgrowth` |
| 31 | Overwatch | tower | 3 | live | `overwatch` |
| 32 | Rogue | tower | 1 | live | `rogue` |
| 33 | Cutthroat | tower | 2 | live | `cutthroat` |
| 34 | Fan of Knives | tower | 3 | live | `fan-of-knives` |
| 35 | Engineer | tower | 1 | live | `engineer` |
| 36 | Artificer | tower | 2 | live | `artificer` |
| 37 | Mortar | tower | 3 | live | `mortar` |
| 38 | Necromancer | creep | — | live | `necromancer` |
| 39 | Bone Golem | creep | — | live | `bone-golem` |
| 40 | Black Knight | creep | — | live | `black-knight` |
| 41 | Frost Wight | creep | — | live | `frost-wight` |
| 42 | Abomination | creep | — | live | `abomination` |
| 43 | Vampire | creep | — | live | `vampire` |
| 44 | Witch | creep | — | live | `witch` |
| 45 | Fiend | creep | — | live | `fiend` |
| 46 | Shade | creep | — | live | `shade` |
| 47 | Cursed Villager | creep | — | live | `cursed-villager` |
| 48 | Werewolf | creep | — | live | `werewolf` |
| 49 | Grave Robber | creep | — | live | `grave-robber` |
| 5, 6, 8, 9, 10 | *retired* | — | — | — | see [below](#what-is-retired-and-why) |

> **id 7's label moved as well as its name, and both landed with the Necromancer row.** It was `necromancer`
> and it is `skeleton-mage`, because the new Necromancer row wanted that label and two rows cannot share one —
> which is why the rename waited for the row. Renaming a label moves no hash; nothing in the simulation
> branches on it. **What moved the hash is the aura signed with the rename**, and the twelve rows beside it.

> **Tiers 1 and 2 are named for a body; a capstone is named for the upgrade.** Soldier → Sergeant → Shield
> Wall changes what kind of noun the row is at the top rung, and that is deliberate rather than a slip: a
> capstone is the one rung bought with a different currency, and it is not a new body but a thing the tower
> learns to do. Read the capstone rows as the name of what the token buys.

**The file interleaves towers and creeps, and it has gaps.** Ids ascend strictly down the file and ascend past
the roles, so two creeps sit below a tower and 5, 6, 8, 9 and 10 are permanently absent. Both are deliberate:
the order records what was decided when, and the gaps make the retirements visible instead of papering over
them. Grouping is what this document is for.

**Labels in `units.txt` are lowercase single tokens, so the two-word names are hyphenated there.** The parser
allows letters, digits, `-` and `_` and nothing else — a space would be two fields. The label is for people
reading the file and for error messages; nothing in the simulation branches on it, and renaming one moves no
hash.

**The ladder that joins them is [`content/upgrades.txt`](../content/upgrades.txt)**, one `upgrade <from> <to>`
row per edge, printed by `./tools/show-ladder.ps1`. It holds **eighteen edges**: two per tower line, so nine
roots may be placed and every other rung is reached by standing the one below it and upgrading.

---

# Towers

> **The widening is signed.** [The expansion proposal](roster-expansion-proposal.md) was taken on
> **5 September 2026**: nine lines, three stages each. The Captain, the Hero and the elemental branch are
> retired with it, and their mechanics moved to capstones on models that ship the prop for them.

**Nine lines, three tiers each, and one attack type per line.** It is what makes the three-way cycle readable
off the board: you know what a tower does to a body by knowing which line it came from, and it costs nothing,
because attack type is a column that already exists.

Impact ×3 — Knight, Barbarian, Engineer. Pierce ×2 — Archer, Rogue. Magic ×4 — Mage, Druid, Cleric, Paladin.
**Magic is over-represented on purpose**: the creep side is undead and mostly armoured, and magic is what
beats armoured. The creep table balances it back with swift and arcane bodies.

**The second stage is one stat. The third stage is a capstone that changes how the tower works**, and each
capstone is drawn from what its model is holding or wearing.

### The tier signal is never size

**Size is retired as a tier signal**, reversed on 5 September 2026. A rung is told apart by **what the body
wears, holds or stands beside** — never by how big it is. Three materials, in the order they are reached for:

| Material | What it is | Where it applies |
|---|---|---|
| **Colour** | The pack's alternate texture for that character, applied per row | Every line. 8 of the 9 base characters ship one; only the Lorekeeper does not, and it is a model swap anyway |
| **A prop** | A different or additional thing in a hand, or standing on the tile beside the tower | Every line |
| **A second model** | A different character, the same person promoted | Knight, Cleric and Engineer have none anywhere in the collection; the other six do |

**Tier 2 is colour plus a prop. Tier 3 is the second model where one exists, and colour plus a signature prop
where one does not.** Knight, Cleric, Engineer and Druid take the second road.

> **A glow is not a tier signal, and that is a reservation rather than an omission.** A persistent glow is
> reserved for reading *"this tower is projecting an aura"* — Shield Wall, Blessing, Consecration and
> Overgrowth. If it also meant "tier 3" the two readings would collide on exactly the rows that need the
> first one. Note that it is not free either way: the client has no `ParticleSystem` and two play-mode tests
> forbid one, so a glow has to be real mesh geometry or an emissive material.

**A line that shoots names where its shot leaves from, and that is part of choosing the prop.** `UnitArt`
carries an effect anchor per row — a bone, or a transform inside the held prop, optionally its far end — and
every flash and tracer is drawn from it. A row without one falls back to a fixed height above its own root,
which is the thing anchors replaced, so a tier that changes what a tower is holding changes where its shot
leaves too: a crossbow, a tome and a turret barrel are three different points on three different rigs.
Anchoring is a view fact and not a signed number, but it is set in the art ticket that chooses the prop,
because that ticket is the only one that knows what the prop is called.

**One consequence that is work rather than prose.** A per-row texture is built: `UnitArt` carries the atlas a
row wears, the two views put it on the body before anything goes in a hand, and the alternate atlases the rows
above name are imported beside their own packs. **A third socket is built too, and it is not a hand bone**:
`UnitArt` carries a beside prop — a model and a size — and `TowerView` stands it one tile from the tower root,
where it stays while the tower turns to aim. `turret_base`, `paladin_statue`, `Cleric_Font` and the Druid's
weirwood each have somewhere to stand. **The size is per prop and it is a view fact**, never a column in
`content/units.txt`: the three props authored in their characters' own packs come in at the right size, and a
Forest Nature tree does not. The quiver the Ranger carries is still in its fist, because that is a spine socket
and not this one.

**One tower has one beside slot, and one rung wants two.** The Artificer's look puts an `ammo_crate` beside
the turret. That is the one place on this page the socket as built does not reach, and it is written on that
rung's `Needs` line rather than settled here.

**Every tier on this page is a row in `content/units.txt` as of 5 September 2026**, at ids 15 to 37 in the
order the lines are written above. Layout 3 authors every shape and #217 plays them; see [the column
list](#what-this-roster-needs-that-the-schema-does-not-have). What each one still needs is its art.

> **Windup and backswing are the one pair of numbers this page has not signed.** The Knight and Barbarian
> lines carry both, and so do the Mage and the Archer — which the Sorcerer, Unravel and the Ranger inherit.
> **Sixteen rows carry zero**: the Paladin, Cleric, Druid, Rogue and Engineer lines, which say nothing about
> either, and Overwatch, whose two rungs below carry 9 and 6 and whose own tuning is a different shape. Zero is
> a tower that fires the tick it acquires and goes straight back on cooldown — the absence and not a choice.
> How long a tower winds up is how it feels, so the `_` is on each of those six blocks below, and the art
> ticket that picks a line's clips is where a real number is signed.

## The Knight line — impact, melee

### 11 · Soldier · tier 1 · status live

- **Does** — one hex of range, striking every creep touching him, fast. The adjacency floor means height never
  takes his neighbours away from him.
- **Looks** — knight, full helm down, short sword.
- **Numbers** — range 1000, cooldown 15 (two swings a second), damage 60–90, windup 7, backswing 5, hitscan,
  impact, **cost 30**.
- **Needs** — nothing. `bubbleRadius` and `bubbleOrigin` landed with layout 3, so the sweep is a bubble on
  himself with no period: radius 1000, origin `self`, affects `enemy`, payload `damage`. **It is not authored
  in `content/units.txt`** — the row there still fires one shot at one creep, because giving the Soldier his
  sweep is a design decision and a balance change rather than a schema one.
- **Answered** — he was the unit seam 9 was expected to retire, and he is kept instead. A tower that strikes
  everything touching it is the one tower whose whole value is positional, which is exactly what a fold is
  for.

> **Why it is the cheapest thing on the board.** 150 damage a second against the Archer's 200, at 30 gold
> against 40 — identical value per gold, with a third of the reach. You buy Soldiers because they are cheap,
> not because they are good, and the melee line earns its identity at tiers 2 and 3 rather than here.

### 15 · Sergeant · tier 2 · status live

- **Does** — swings faster. One stat.
- **Looks** — `Knight`, `knight_texture_alt_A`, and a `shield_square` in the off hand.
- **Numbers** — cooldown 15 → **11**, every other number the Soldier's. Cost ~41.
- **Needs** — nothing. A cooldown is a column.
- **Open** — none.

### 16 · Shield Wall · tier 3 · status live

- **Does** — every creep touching him walks at half speed while it is touching him, and he keeps swinging.
- **Looks** — `Knight`, `knight_texture_alt_B`, shield raised (`Melee_Blocking`), visor closed. A persistent
  glow reads the aura once one exists.
- **Numbers** — aura: origin `self`, radius 1000, affects `enemy`, payload `speed`, magnitude −50, period 15,
  duration 20. The Sergeant's damage and cooldown carry.
- **Needs** — nothing. Layout 3 authors it and #217 plays it.
- **Open** — none.

> **It is the one slow in the game that costs no range.** Purely positional, and it bunches bodies under
> whatever stands beside him. On a fold, that is the Barbarian.

## The Barbarian line — impact, melee, slow and heavy

### 17 · Barbarian · tier 1 · status live

- **Does** — one hex, slow, heavy, one target.
- **Looks** — `Barbarian`, `axe_2handed`, `Melee_2H_Attack_Chop`.
- **Numbers** — range 1000, cooldown 45, damage 200–300, windup 20, backswing 12, hitscan, impact, cost ~33.
- **Needs** — nothing.
- **Open** — none.

### 18 · Berserker · tier 2 · status live

- **Does** — hits harder. One stat.
- **Looks** — `Barbarian`, `barbarian_texture_alt_A`, `axe_2handed_Large`.
- **Numbers** — damage 200–300 → **300–450**. Cost ~50.
- **Needs** — nothing.
- **Open** — none.

### 19 · Slam · tier 3 · status live

- **Does** — every swing hits everything touching him. The same roll, every body.
- **Looks** — the **`Barbarian_Large`** model, `Melee_2H_Slam`. This is the line's second model, and it is on
  the **Large rig**, so it needs that rig's clip bank.
- **Numbers** — bubble: origin `self`, radius 1000, payload `damage`. The Berserker's roll.
- **Needs** — nothing.
- **Open** — none.

> **This is where the Hero's 360° sweep went.** It was retired from the Soldier line on 5 September 2026 and
> landed here, on a model that ships a two-handed slam clip for it.

## The Paladin line — magic, melee

### 20 · Paladin · tier 1 · status live

- **Does** — one hex, holy damage, one target.
- **Looks** — `Paladin`, bare head, `paladin_hammer`.
- **Numbers** — range 1000, cooldown 24, damage 120–180, windup `_`, backswing `_`, hitscan, magic, cost ~37.
- **Needs** — nothing.
- **Open** — none.

### 21 · Templar · tier 2 · status live

- **Does** — hits harder. One stat.
- **Looks** — the **`Paladin_with_Helmet`** model, `paladin_hammer` and `paladin_shield`.
- **Numbers** — damage 120–180 → **180–270**. Cost ~56.
- **Needs** — nothing.
- **Open** — the second model lands at tier 2 here rather than tier 3, because the helmet is the smaller of
  the two changes this line has available and the statue is the larger.

### 22 · Blessing · tier 3 · status live

- **Does** — every tower within two hexes fires a quarter faster, always.
- **Looks** — `Paladin_with_Helmet`, `paladin_texture_B`, `paladin_book` open, and the gold `paladin_statue`
  standing on the tile beside him — **drawn at 1**, the size it imports at, which is 2.55 m tall and 1.60 across
  and stands level with the Paladin himself.
- **Numbers** — aura: origin `self`, radius 2000, affects `friend`, payload `cooldown`, magnitude −25,
  period 30, duration 30.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

> **Two Blessings over one tower do not stack; the timer refreshes.** That is the rule the effect model
> already has, and it is what stops a ring of Paladins running away. This is where the Captain's attack-speed
> aura went when it was retired.

## The Cleric line — magic, ranged

### 23 · Cleric · tier 1 · status live

- **Does** — three hexes, holy bolt, one target.
- **Looks** — `Cleric`, `Cleric_Tome`, `Ranged_Magic_Shoot`.
- **Numbers** — range 3200, cooldown 30, damage 130–190, windup `_`, backswing `_`, hitscan, magic, cost ~32.
- **Needs** — nothing.
- **Open** — none.

### 24 · Bishop · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Cleric`, `cleric_texture_B`, `Cleric_Mace`.
- **Numbers** — range 3200 → **4200**. Cost 32 — **range is unpriced**, so the rung costs what the Cleric
  costs. Same shape as `archer → ranger`, and the ladder prints a flat-price note against it for the same
  reason.
- **Needs** — nothing.
- **Open** — none.

### 25 · Consecration · tier 3 · status live

- **Does** — every undead within three hexes loses a third of its armour while it is there.
- **Looks** — `Cleric`, `cleric_texture_B`, `Cleric_Mace`, and the `Cleric_Font` on the tile beside him, light
  on the ground — **drawn at 1**, which is 0.81 m tall and 1.44 across, a basin at knee height. The Cleric has
  **no second model anywhere in the collection**, so this line is colour and props at every rung.
- **Numbers** — aura: origin `self`, radius 3000, affects `enemy`, payload `armour`, magnitude −30, period 30,
  duration 30.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

> **Zeal is the successor, not the alternative.** *Every tower within two hexes deals more damage* is the
> better holy aura and it is written down here so it is not re-invented: it needs the sixth `bubblePayload`
> value — a damage **modifier**, as distinct from the roll a damage bubble spreads — which does not exist.
> **The keyword's name is deliberately not chosen**, because naming a payload word nobody is implementing
> this effort would be signing a word blind. Name it when it is built. Consecration is what the Cleric has
> until then, and it is not a placeholder — it ships.

## The Archer line — pierce

### 3 · Archer · tier 1 · status live

- **Does** — three hexes of range, modest damage, fast.
- **Looks** — the ranger model.
- **Numbers** — range 3200, cooldown 18, damage 90–150, windup 9, backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing.
- **Open** — none.

**The line's identity is fast-and-modest**, and the home for slow-and-heavy is Overwatch at tier 3. Four of
the six committed defense slots are Archers, so retuning this row moves most of what the golden trace measures.

### 14 · Ranger · tier 2 · status live

- **Does** — +1 hex of range.
- **Looks** — `Ranger`, `ranger_texture_alt_A`, and a `quiver`. **The 1.5 scale is reverted** — size is no
  longer a tier signal anywhere on this page, and the colour and the quiver are what separate the rungs on
  sight. The revert and the replacement land in the same commit, so no build ever ships two identical rungs.
- **Numbers** — range 4200, and every other number the Archer's: cooldown 18, damage 90–150, windup 9,
  backswing 6, hitscan, pierce, **cost 40**.
- **Needs** — nothing. It is the only tier on this page that is purely a number.
- **Open** — none. A tier that is one stat is the middle rung, and a second clause can be added to it later
  without moving its id.

> **It costs the same as the Archer, and that is the rule rather than a mistake.** A tower is priced at one gold
> per five damage a second times the bodies a shot hits, and **the rule does not price range** — so a tower that
> differs from the Archer in range alone prices identically to it. `./tools/show-ladder.ps1` prints a *flat or
> falling price* note against the `archer → ranger` edge for exactly this reason. It is a note and not a fault,
> nothing goes red, and there is nothing here for anybody to go and fix.
>
> The gap is [already written down beside the rule](#what-things-cost). Two futures were named and neither
> gates this row: range may become an input to the cost algorithm, or the algorithm may be replaced by
> something derived from **many simulations rather than from a row's stats**.

### 31 · Overwatch · tier 3 · status live

- **Does** — sees the whole leg. Slow, enormous single shots from wherever he is stood. **This is where the
  line's slow-and-heavy tuning lives.**
- **Looks** — the **`Marksman`** model, prone-ish `Ranged_2H_Aiming`, holding **`crossbow_2handed`** from the
  Adventurers pack.
- **Numbers** — range **8000**, cooldown 60, damage 500–700, windup `_`, backswing `_`, hitscan, pierce.
  Cost ~60.
- **Needs** — nothing.
- **Open** — none.

> **The rifle is rejected and the crossbow is signed.** `Marksman_Rifle` is the only firearm in the collection
> the roster would touch, and it puts the top of the Archer line in a different century from every other body
> on the board. The crossbow is a different pack's art style, which is the smaller break of the two.

> **Multishot moved to the Rogue.** It was this row's mechanic until 5 September 2026; it belongs to the model
> that throws knives, and the Marksman got the long single shot instead.

## The Mage line — magic

### 4 · Mage · tier 1 · status live

- **Does** — magic damage with splash of one additional hex.
- **Looks** — the mage, book in hand.
- **Numbers** — range 4600, cooldown 54, damage 210–340, windup 21, backswing 15, projectile, flight 33,
  splash radius 1000, magic, **cost 92**.
- **Needs** — nothing. The splash is on the row as of 5 September 2026: origin `target`, radius 1000,
  payload `damage`.
- **Answered on 5 September 2026 — author the splash, defer the price.** The bubble is authored: origin
  `target`, radius 1000, payload `damage`. **The cost stays 92 and is not re-derived.** The rule's bodies term
  reads `targets`, which is 1, so the rule says 30 and the row says 92; that gap is now a *known* gap held
  open on purpose rather than an unanswered question. Repricing a row whose value is a splash radius is
  exactly what the cost rule is worst at, and **the price waits for the automated balance sweeps to be
  trustworthy enough to derive it.** Until then 92 stands and the sweep reports what it is worth.
- **Open** — the price, and only the price.

### 26 · Sorcerer · tier 2 · status live

- **Does** — casts faster. One stat.
- **Looks** — `Mage`, `mage_texture_alt_A`, holding `staff` rather than the open book.
- **Numbers** — cooldown 54 → **40**. Cost **124**, and the splash carries.
- **Needs** — nothing.
- **Open** — the price, as the Mage's is. 124 is the Mage's 92 scaled by the cooldown it changed, so this rung
  inherits the deferral rather than making a second decision. The damage rule reads 41 against it.

### 27 · Unravel · tier 3 · status live

- **Does** — his bolt strips most of the armour off what it hits, for five seconds.
- **Looks** — the **`Lorekeeper`** model, `Lorekeeper_Tome` open. The Lorekeeper is the one character in the
  roster with **no alternate texture**, which costs nothing: this rung is a model swap.
- **Numbers** — bubble on target: radius 1000, payload **armour**, magnitude −60, duration 150. Cost **124**,
  the Sorcerer's, since it changes neither the roll nor the bodies.
- **Needs** — nothing.
- **Open** — the price, inherited from the Mage with the rest of the line.

> **The capstone trades the splash for the strip.** One row carries one bubble, so Unravel's bubble replaces
> the tier-1 splash: the roll lands on one body and the armour strip lands on the hex around it. Keeping both
> would need a second bubble column, which is a format version. **That trade is the choice the token buys**,
> and it is the reason this capstone is not a strict upgrade on the rung below it.

> **This is the only armour strip on the tower side**, against a creep table that is seven-armoured out of
> seventeen. Consecration is the other one, by a different geometry — an aura around the Cleric rather than a
> bolt on a body — and having both is a pairing rather than a duplicate.

## The Druid line — magic, ranged

### 28 · Druid · tier 1 · status live

- **Does** — three and a half hexes, nature bolt, one target.
- **Looks** — `Druid`, `druid_staff`, `Ranged_Magic_Shoot`.
- **Numbers** — range 3600, cooldown 36, damage 150–210, windup `_`, backswing `_`, hitscan, magic, cost ~30.
- **Needs** — nothing.
- **Open** — none.

### 29 · Elder · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Druid`, `druid_texture_alt_A`.
- **Numbers** — range 3600 → **4600**. Cost 30 — range is unpriced, same shape as `archer → ranger`.
- **Needs** — nothing.
- **Open** — none.

### 30 · Overgrowth · tier 3 · status live

- **Does** — the whole board slows a fifth while he stands. Every board.
- **Looks** — `Druid`, `druid_texture_alt_B`, and a **bare weirwood standing on the tile beside him** —
  **`Tree_Bare_1_C_Color8`** from the Forest Nature pack, signed on 5 September 2026 from a rendered sheet of
  all six `Color8` bare trees turned through the game's own six camera angles. It is the largest of the six at
  936 triangles and the only silhouette that reads as an ancient tree rather than a dead stick from every
  angle. **Drawn at 0.55**: at its own size it spreads 3.74 m, which is nearly two tiles and reaches back
  through the Druid, and 0.55 brings that to the 2.06 m of the tile it stands on and leaves it 2.89 m tall,
  half again the Druid's own height. Roots on every hex once they are drawn.
- **Numbers** — aura: origin `self`, radius 60000, affects `enemy`, payload `speed`, magnitude −20, period 30,
  duration 30.
- **Needs** — nothing. The beside slot is built and the tree is picked.
- **Open** — none.

> **The Druid keeps his own body, and the PlantWarrior is set aside.** It was proposed as this line's second
> model and it is rejected: of the six second models it was the only one that read as a *different creature*
> rather than the same person promoted. So this line has no model swap, and it is colour and a prop at every
> rung, like the Knight, the Cleric and the Engineer.

> **A whole-board pulse is one row.** The roster has said so since layout 3 and nobody had built one. This is
> where the retired elemental branch's area slow went. **A creep never drops below a tenth of its authored
> speed** — a floor binding every effect at once — so stacking Overgrowth with Shield Wall has a bounded
> bottom rather than an open one.

## The Rogue line — pierce, short range, very fast

### 32 · Rogue · tier 1 · status live

- **Does** — two hexes, three throws a second, light.
- **Looks** — `Rogue`, `dagger`, the `Throw` clip.
- **Numbers** — range 2200, cooldown 9, damage 40–60, windup `_`, backswing `_`, hitscan, pierce, cost ~33.
- **Needs** — nothing.
- **Open** — none.

### 33 · Cutthroat · tier 2 · status live

- **Does** — throws faster. One stat.
- **Looks** — the **`Rogue_Hooded`** model, `dagger`.
- **Numbers** — cooldown 9 → **7**. Cost ~43.
- **Needs** — nothing.
- **Open** — the second model lands at tier 2 here, because the hood is this line's smaller change and the
  capstone is carried by a clip and a `targets` column rather than by a body.

### 34 · Fan of Knives · tier 3 · status live

- **Does** — three knives a throw, at the three bodies nearest the exit.
- **Looks** — `Rogue_Hooded`, `rogue_texture_alt_A`, dual `dagger`, `Melee_Dualwield_Attack_Slice` as the
  throw.
- **Numbers** — `targets` 3. Cost ~129, **since bodies are priced** — this is one of the four capstones the
  damage rule's inputs actually move under.
- **Needs** — nothing. `targets` landed with layout 3, and target selection answers an ordered *n* under the
  same total order it always answered one under.
- **Open** — none.

> **Three shots at one roll each, not one roll split three ways.** `targets` of *n* fires *n* shots at *n*
> creeps and draws *n* damage rolls; one shot split *n* ways is the other shape, and it is a bubble. This is
> where the Marksman's multishot went.

## The Engineer line — impact, projectile, long range

### 35 · Engineer · tier 1 · status live

- **Does** — four hexes, slow lobbed shot, one target.
- **Looks** — `Engineer`, `engineer_Wrench` in hand, a `turret_base` on the tile beside him doing the firing —
  **drawn at 1**, which is 1.13 m tall and 1.00 across, and the shell leaves the top of it at 0.77 m rather
  than leaving the man.
- **Numbers** — range 4000, cooldown 60, damage 250–350, windup `_`, backswing `_`, projectile, flight 45,
  impact, cost ~30.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

### 36 · Artificer · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Engineer`, `engineer_texture_alt_A`, an `ammo_crate` beside the turret — the crate is 0.46 m
  tall at 1.
- **Numbers** — range 4000 → **5000**. Cost 30 — range is unpriced.
- **Needs** — **a second beside slot.** A tower has one, and this rung names two things standing on the
  ground; until there are two, this rung draws the turret or the crate and not both.
- **Open** — none.

### 37 · Mortar · tier 3 · status live

- **Does** — the shell bursts across a hex and a half.
- **Looks** — `Engineer`, `engineer_texture_alt_B`, a heavier `turret_base` beside him and the lobbing arc
  drawn. The Engineer has **no second model anywhere in the collection**, so this line is colour and props at
  every rung.
- **Numbers** — bubble on target: radius 1500, payload `damage`.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

> **Two blasts on the board, and they are not the same tool.** The Mage's is magic at radius 1000 and lands at
> tier 1; this one is impact at radius 1500 and costs a token. The impact one is the answer to arcane bodies
> the Mage cannot chew.

---

# Creeps

**Creeps never attack.** `dmgMin`, `dmgMax` and `attack` are zero and `none` on every walking row, and no aura
below is an exception to that — they buff, shield, hasten and hobble, and none of them deals damage.

> **Seventeen creeps are live as of 5 September 2026** — the five older rows and twelve new ones, at ids 38 to
> 49. Armour is spread deliberately: **seven armoured, five swift, five arcane**, which balances back a tower
> side that is four-ninths magic, and which is what closed the warning this section used to carry about a
> matrix with a single occupant in two of its three columns.

**Every creep with a health pool carries one armour type from the fixed three-way cycle**, so "no armour" is
not available: `armourValue 0` means the type still applies, at zero points.

**All seventeen rows, in full:**

| id | name | maxHp | speed | armour | armourValue | shield | dying | effective hp | cost |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Minion | 1550 | 28 | armoured | 0 | — | 36 | 1550 | **10** |
| 2 | Skeleton Scout | 1500 | 56 | swift | 0 | — | 36 | 1500 | **9** |
| 7 | Skeleton Mage | 2400 | 33 | arcane | 25 | — | 36 | 3000 | **19** |
| 12 | Skeleton | 2200 | 28 | armoured | 20 | — | 36 | 2640 | **17** |
| 13 | Skeleton Warrior | 3400 | 18 | armoured | 45 | — | 48 | 4930 | **31** |
| 38 | Necromancer | 2600 | 28 | arcane | 30 | — | 0 | 3380 | **21** |
| 39 | Bone Golem | 9000 | 14 | armoured | 60 | — | 0 | 14400 | **90** |
| 40 | Black Knight | 5000 | 22 | armoured | 80 | — | 0 | 9000 | **56** |
| 41 | Frost Wight | 6000 | 16 | arcane | 40 | — | 0 | 8400 | **53** |
| 42 | Abomination | 12000 | 12 | armoured | 0 | — | 0 | 12000 | **75** |
| 43 | Vampire | 2800 | 44 | swift | 20 | 1400 | 0 | 3360 | **21** |
| 44 | Witch | 2000 | 33 | arcane | 20 | — | 0 | 2400 | **15** |
| 45 | Fiend | 3200 | 33 | arcane | 45 | — | 0 | 4640 | **29** |
| 46 | Shade | 1200 | 84 | swift | 0 | — | 0 | 1200 | **8** |
| 47 | Cursed Villager | 1800 | 28 | swift | 0 | — | 0 | 1800 | **11** |
| 48 | Werewolf | 2600 | 50 | swift | 10 | — | 0 | 2860 | **18** |
| 49 | Grave Robber | 3000 | 22 | armoured | 30 | 2000 | 0 | 3900 | **24** |

> **The effective-health column is what the price is derived from, and it does not include the shield.** The
> Vampire stands on 3360 plus 1400 raw and the Grave Robber on 3900 plus 2000, and neither is charged for the
> pool. See [the tuning target](#the-tuning-target).

> **Three rows land on a dead tie, and every one of them is resolved upward.** The Skeleton's 2640 ÷ 160 is
> exactly **16.5** and goes to 17; the Shade's 1200 ÷ 160 is **7.5** and goes to 8; the Frost Wight's 8400 ÷ 160
> is **52.5** and goes to 53. Every other row lands clearly on one side. Recorded because a tie is the kind of
> thing someone recomputes later, reads as an error, and silently "corrects" in the other direction — and one
> rule for all three is what stops the correction being made row by row.

> ⚠️ **`dying` is UNSIGNED on the twelve new rows, and the zero in the table is the blank showing through
> rather than an answer.** Four of the five older rows carry 36 and the Warrior 48. This page signs no dying
> number for the twelve, and the column in `content/units.txt` has to hold something, so it holds zero — the
> absence, in the same sense the tower rows hold zero windup and backswing for the lines this page does not
> sign them for. **How long a body takes to die is how a death reads on screen, and it is Sam's to sign**,
> with the clips. Until then a corpse is gone the tick after it falls.

### 1 · Minion · status live

- **Does** — health and nothing else. The baseline body.
- **Looks** — the minion skin, no tools.
- **Numbers** — 1550 hp, speed 28, armoured, armourValue 0, dying 36, cost 10.
- **Needs** — nothing.
- **Open** — none.

**This is the row every other row is read against**, which is why nothing about it moves. Re-baselining it
would re-baseline every measurement in the sweep.

### 12 · Skeleton · status live

- **Does** — the Minion with a little armour. The low rung of the armoured ladder.
- **Looks** — the minion skin with shield and sword.
- **Numbers** — 2200 hp, speed 28, armoured, armourValue 20, dying 36, cost 17.
- **Needs** — nothing.
- **Open** — none, but see below.

> **This is knowingly the dullest row on the page** — the Minion at the same speed with a bigger pool and some
> armour. Speed is the only lever that would fix it, and moving it off 28 breaks the whole-multiple
> relationship with the Scout that makes the target-selection tiebreak get consulted at all. A boring middle
> rung was judged cheaper than deleting a test.

### 13 · Skeleton Warrior · status live

- **Does** — slow and genuinely armoured. The heavy.
- **Looks** — the warrior skeleton, full kit.
- **Numbers** — 3400 hp, speed 18, armoured, armourValue 45, dying 48, cost 31.
- **Needs** — nothing.
- **Open** — none.

**What separates it from the Skeleton is two axes, not one** — armour 45 against 20, and speed 18 against 28.

### 2 · Skeleton Scout · status live

- **Does** — fast, no armour value.
- **Looks** — the rogue skeleton.
- **Numbers** — 1500 hp, speed 56, swift, armourValue 0, dying 36, cost 9.
- **Needs** — nothing.
- **Open** — none.

**Exactly twice the Minion's speed, and that is load-bearing rather than tidy.** See [the clock](#the-clock).

### 7 · Skeleton Mage · status live

**Relabelled from `necromancer` on 5 September 2026, and its aura signed with the rename.**

- **Does** — **Haste**: every creep within two hexes walks a fifth faster.
- **Looks** — `Skeleton_Mage`, `Skeleton_Staff`, casting continuously.
- **Numbers** — 2400 hp, speed 33, arcane, armourValue 25, dying 36, cost 19. Aura: origin `self`, affects
  `friend`, payload `speed`, magnitude **+20**, radius 2000, period 30, duration 30.
- **Needs** — nothing. Layout 3 authors it and #217 plays it.
- **Open** — none. **The aura is signed**; it had stood unsigned since the row went live.

> **Why the rename is the cheap half of this.** The id does not move, so no hash moves and no stored record is
> touched; only the new aura does that. The pack's own `Necromancer` model had been sitting unused while the
> name pointed at a `Skeleton_Mage` body — see [the new Necromancer](#38--necromancer--status-live), which takes
> both the model and the shield aura originally designed for this row.

> **Three rules #217 had to pick to build the shield aura at all still stand**, and they now apply to the
> Necromancer rather than here: the granted pool **persists until spent or until its duration ends**,
> whichever comes first, with a duration of zero meaning until spent; it does **not** move with its source, so
> killing the source stops the pulses and what is already granted is spent or times out rather than vanishing;
> and the magnitude is **a share of the health it stands in front of**, because a pool has no rate of its own
> for a percentage to be a percentage of. All three were the implementer's reading rather than a decision, and
> any of them can be moved without another format version. See
> [ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md).

### 38 · Necromancer · status live

- **Does** — **Ward**: grants creeps within two hexes a shield worth a quarter of their health, every three
  seconds. **Raise**: spawns a Minion beside itself every **150 ticks**, for as long as it lives.
- **Looks** — the pack's own `Necromancer` model, `Skeleton_Scythe`.
- **Numbers** — 2600 hp, speed 28, arcane, armourValue 30, cost 21. Ward: origin `self`, affects `friend`,
  payload `shield`, magnitude 25, radius 2000, period 90, duration 0. Raise: one Minion per 150 ticks.
- **Needs** — **engine, for Raise only.** Ward is on the row and pulsing; Raise is not — a creep spawning
  creeps is a new mechanic, and [#268](https://github.com/ssalter21/tower-defense-game/issues/268) is where it
  lands.
- **Open** — none.

> **There is no cap on how many it raises, and that is the decision rather than an omission.** It raises for
> as long as it is alive and walking, so the board is what bounds it: at speed 28 across the 51-hex corridor
> it lives about 1,545 ticks and raises **roughly ten**. A hundred gold of bodies from a twenty-one gold row
> is a sweep finding, and it is left standing.
>
> **A slowed Necromancer raises more, and that is a live trade-off against stacking slows.** The speed floor
> is a tenth, so a fully-slowed one crosses in about 17,000 ticks and raises **on the order of a hundred**.
> Shield Wall and Overgrowth are slows, so the two capstones built to handle a push are the two that make this
> body worst. Spamming or stacking slows is supposed to cost something; here is where it costs.
>
> **The arithmetic that guarantees a match ends does not cover this**, and extending it is the spawn ticket's
> job. `Match.RequireItArrives` proves at construction that every *authored* order reaches the exit inside the
> tick ceiling, at the floor speed; a creep spawned at runtime is in no order. Nothing runs away — a Minion
> does not itself raise — but the proof has to be re-derived over the spawner's floored crossing time rather
> than assumed. See [ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md) and the spawn
> ticket's own ADR.

### 39 · Bone Golem · status live

- **Does** — nothing but mass. Half the Minion's speed.
- **Looks** — `Skeleton_Golem`, `Skeleton_Golem_Axe_Large`. On the Large rig, which walks and dies.
- **Numbers** — 9000 hp, speed 14, armoured, armourValue 60, cost 90.
- **Needs** — nothing.
- **Open** — none. **14 is exactly half the Minion's 28**, so it is passed on whole ticks.

### 40 · Black Knight · status live

- **Does** — the Knight's twin. Nothing but armour.
- **Looks** — `BlackKnight`, `BlackKnight_Sword_Large`, `BlackKnight_Shield_Large`. Large rig.
- **Numbers** — 5000 hp, speed 22, armoured, armourValue 80, cost 56.
- **Needs** — nothing.
- **Open** — none.

### 41 · Frost Wight · status live

- **Does** — **Frostbite**: towers within two hexes fire a third slower while it passes. The only creep aura
  that reaches the tower side.
- **Looks** — `FrostGolem`, `FrostGolem_Axe`. Large rig.
- **Numbers** — 6000 hp, speed 16, arcane, armourValue 40, cost 53. Aura: origin `self`, affects **`enemy`**,
  payload `cooldown`, magnitude +30, radius 2000, period 30, duration 30.
- **Needs** — nothing.
- **Open** — none.

### 42 · Abomination · status live

- **Does** — the biggest body on the board. No armour: flesh, not bone.
- **Looks** — `Monstrosity`, `Monstrosity_BarndoorShield_Large`. Large rig.
- **Numbers** — 12000 hp, speed 12, armoured, armourValue 0, cost 75.
- **Needs** — nothing.
- **Open** — none.

### 43 · Vampire · status live

- **Does** — **Blood**: a raw pool armour does not apply to, spent before health.
- **Looks** — `Vampire`, `Vampire_Sword`.
- **Numbers** — 2800 hp, speed 44, swift, armourValue 20, shield 1400, cost 21.
- **Needs** — nothing.
- **Open** — **the shield is unpriced, and the sweep has now measured what that is worth.** The cost rule has
  no term for a pool, so this row is cheaper than it should be: it returns 94 percent of a column's gold
  against the committed defense, the highest reading inside the band. Known gap, same family as radius and
  range; a sweep target, not something to hand-correct.

### 44 · Witch · status live

- **Does** — **Hex Ward**: creeps within two hexes gain 30 armour.
- **Looks** — `Witch`, `Broom`.
- **Numbers** — 2000 hp, speed 33, arcane, armourValue 20, cost 15. Aura: origin `self`, affects `friend`,
  payload `armour`, magnitude +30, radius 2000, period 30, duration 30.
- **Needs** — nothing.
- **Open** — none.

### 45 · Fiend · status live

- **Does** — an arcane heavy; the Warrior's counterpart on another armour type.
- **Looks** — `Tiefling`, `Tiefling_SwordsBackpack`. A horned demon rather than an undead body — the theme is
  *undead, and the dark or hooded*, and this is the dark half, the same licence the Witch and the Vampire use.
- **Numbers** — 3200 hp, speed 33, arcane, armourValue 45, cost 29.
- **Needs** — nothing.
- **Open** — none.

### 46 · Shade · status live

- **Does** — three times the Minion's speed. The fine end of the granularity axis.
- **Looks** — `Ninja`, `Ninja_Katana`, **in the darkest of the pack's four atlases**. Read as a silhouette at
  gameplay distance it stops being a ninja; that is the whole reason the model is admissible, and the atlas
  pick is not optional decoration.
- **Numbers** — 1200 hp, speed 84, swift, armourValue 0, cost 8.
- **Needs** — nothing. The atlas pick rides on the same per-row texture work the tier signal needs.
- **Open** — none. **84 is exactly three Minions**, so it passes on whole ticks like the Scout does.

### 47 · Cursed Villager · status live

- **Does** — a cheap body that is the Werewolf's first form. **On the first damage it takes, it becomes the
  Werewolf.**
- **Looks** — `Werewolf_Man`, `axe`.
- **Numbers** — 1800 hp, speed 28, swift, armourValue 0, cost 11.
- **Needs** — **engine.** A creep becoming another row mid-lane is a new mechanic, and
  [#267](https://github.com/ssalter21/tower-defense-game/issues/267) is where it lands. The row itself is live
  and walks as an ordinary cheap body until then.
- **Open** — none.

### 48 · Werewolf · status live

- **Does** — fast and durable at once. What the Cursed Villager becomes.
- **Looks** — `Werewolf_Wolf`.
- **Numbers** — 2600 hp, speed 50, swift, armourValue 10, cost 18.
- **Needs** — engine, with the Villager. Both rows are live and both walk on their own until
  [#267](https://github.com/ssalter21/tower-defense-game/issues/267).
- **Open** — none.

> **A lethal first hit does not kill the Villager; it produces a Werewolf at full health.** The trigger is the
> first damage taken, and the change resolves ahead of the death — so the Werewolf enters on its own full
> 2600 rather than on whatever the Villager had left, and **no Cursed Villager can ever be one-shot**. The
> pair is therefore worth 1800 + 2600 = 4400 effective health *always*, not as a worst case, and it must be
> priced and swept as a pair. What else carries over at the change — position, route progress — is the
> transform ticket's ADR.

> **This is the pairing `lancer` occupied with no design behind it.** Now the design is the transformation.

### 49 · Grave Robber · status live

- **Does** — the pack soaks hits: a raw pool in front of ordinary health. **Pays 12 gold to the defender that
  kills it**, mid-match, into the one purse.
- **Looks** — `Hoarder`, wearing `Hoarder_Backpack`. **The backpack, not the sword** — the pack is what the
  mechanic is about, and a sword on a creep that never attacks reads as a lie.
- **Numbers** — 3000 hp, speed 22, armoured, armourValue 30, shield 2000, cost 24. Pays **12** on a kill.
- **Needs** — **engine.** Gold paid on a kill is the first income during a wave, and
  [#269](https://github.com/ssalter21/tower-defense-game/issues/269) is where it lands. The row is live and the
  pool is on it; the payment is not.
- **Open** — the shield is unpriced, as the Vampire's is.

> **Twelve is half its own price, and the half is the point.** Paying its full 24 back would make it free to
> send. Half means killing it refunds half of what the attacker laid out, so it is a body that rewards being
> killed without being one you are glad to see. **A leaked Grave Robber pays nothing.** This is the first
> income inside a wave and it moves the leak exchange rate, which is the gold ticket's ADR.

---

## What is retired, and why

**Ids are never reused, so 5, 6, 8, 9 and 10 stay empty forever.**

| id | row | why |
|---|---|---|
| 5 | `wisp` | The swarm. 57 bodies for 400 gold — one end of the granularity axis. Out of scope with the roster at five creeps |
| 6 | `bulwark` | The wall. 8 bodies for the same 400 — the other end. Same reason |
| 8 | `lancer` | A swift heavy with no designed counterpart |
| 9 | `sniper` | **Magic, in a line that is now pierce.** One attack type per line retires it as written; the long single shot returned as Overwatch, on pierce |
| 10 | `sieger` | An impact projectile whose line's tier 3 was the Hero — a 360° melee sweep, which a slow siege shell is not. The shape returned as the Engineer's Mortar, on its own line |

### Proposals retired on 5 September 2026

**These never reached `units.txt`, so no id is burned and nothing is pinned to them.** They are recorded
because each was written down here for weeks and would otherwise be re-proposed.

| row | was | where it went |
|---|---|---|
| Captain | tower, tier 2, attack-speed aura | The aura is the **Paladin's Blessing**, on a model that ships a book and a statue for it. A tier 2 is one stat now |
| Hero | tower, tier 3, 360° sweep | The sweep is the **Barbarian's Slam**, on a model that ships a two-handed slam clip |
| Pyromancer | tower, tier 2a, fire branch | Retired with the branch |
| Cryomancer | tower, tier 2b, frost branch | Retired with the branch. The area slow is the **Druid's Overgrowth** |
| Frostfire Archmage | tower, tier 3, both branches | Retired with the branch |

> **The branch is what was actually retired.** Three stages, no branch — which closes the open question that
> said the tier-2 element pick was temporary, and leaves *one line, three stages* an invariant with no
> exception. It also stops two roads ending at one tower, which made the pick a tempo decision rather than a
> build decision.

**Nothing structural breaks.** Stored bundles carry their own copy of the unit table —
`content/golden/defense-0.units` is still in the fifteen-column layout 1 and still replays — so retiring a row
invalidates no record; it leaves those bundles pinned to an older roster, which is exactly what they are for.

## What is deliberately absent

> **All three are filled as of 5 September 2026.** They were blocked on models, and the models arrived with
> [the expansion](roster-expansion-proposal.md). The table below is kept as the record of what was absent and
> what closed it.

**Recorded so it is not silently re-proposed.** These were never design rejections — they were shapes that
were wanted and blocked on art rather than on argument.

| shape | what it was for | what filled it |
|---|---|---|
| **Fast and cheap, in numbers** | The fine end of the granularity axis — many light bodies, so a purse is a decision about *shape* rather than a lookup | The **Shade**, at speed 84 and 8 gold |
| **Slow, dear and very tough** | The coarse end — a few heavy bodies, priced the same | The **Bone Golem** at 9000 and the **Abomination** at 12000 |
| **Fast and durable at once** | The pairing `lancer` occupied without a design behind it | The **Werewolf**, and the design behind it is the transformation |

> **These are named by their levers on purpose.** *Swarm* and *wall* were the words until 13 August 2026 and
> they are rejected: speed, health and armour are the levers, and the two ends of the granularity axis are
> just the ends of it. A category name invites a category the schema does not have. Same reasoning as
> [§12's *ordinary* and *game changer*](vision.md).

**Both consequences this section used to carry are gone.** The thin-draw one went with the offering, deleted
on 13 August 2026; the other was that the Hero's 360° sweep answered a swarm that did not exist. It does now —
the Shade is the fine end of the granularity axis — and the sweep is the Barbarian's Slam.

## The tuning target

**The band is a quarter to a half of the wave, and the committed match is under it.** A defense that holds
tells you nothing when it changes, and one that collapses tells you nothing either; a partial break makes the
leak count a number a person can watch. Ten to twenty of forty is the target.

> ⚠️ **Three of forty leak, as of 5 September 2026, and it is the Mage's splash.** The row has been priced for
> three bodies since the roster was signed and hit one until the bubble was authored; the committed defense is
> four archers and two mages, so authoring it roughly tripled what the two of them remove. Nothing was retuned
> to answer it — a retune means moving creep numbers this page signs, or the committed defense, and both are
> decisions rather than consequences of authoring a signed row.
>
> **Seven creep rows are outside their own band with it.** Four hundred gold of one creep against the
> committed defense returns 60 to 95 percent of its gold for ten of the seventeen rows; six are under and one
> is over. The full table, measured on 5 September 2026 with the twelve new rows in:
>
> | row | returns | | row | returns |
> |---|---|---|---|---|
> | Minion | **25** | | Vampire | 94 |
> | Skeleton Scout | 77 | | Witch | 84 |
> | Skeleton Mage | 90 | | Fiend | 84 |
> | Skeleton | 69 | | Shade | **42** |
> | Skeleton Warrior | **41** | | Cursed Villager | **36** |
> | Necromancer | **100** | | Werewolf | 86 |
> | Bone Golem | **25** | | Grave Robber | 81 |
> | Black Knight | 71 | | | |
> | Frost Wight | 71 | | | |
> | Abomination | **20** | | | |
>
> **Under the band, for two opposite reasons.** A splash is worth most against a dense column, and a column of
> one cheap row is the densest thing that can be sent — so what the Mage's splash costs most is the fine end
> of the granularity axis: the Minion at forty bodies, the Shade at fifty, the Cursed Villager at thirty-six.
> The Bone Golem, the Abomination and the Warrior are under it from the coarse end instead: the slowest bodies
> on the board stand in front of the wall longest and are shot at for longer.
>
> **Over the band, one row, and it is the Necromancer — which is the shield and the radius going unpriced at
> once.** Nineteen of them walk together and each pulses a pool worth a quarter of a body's health over the
> two hexes around it, so the column is handed raw shield faster than four archers and two mages take it off:
> every one of the nineteen leaks. The cost rule reads health and armour and can see neither the pool nor the
> reach that spreads it. **The Vampire at 94 is the same gap without the aura** — a raw 1400 in front of 3360
> the price was derived from — and the Grave Robber's 2000 sits behind an armoured body slow enough to be shot
> for it.
>
> **Nothing here is retuned.** Every reading is asserted as *missed* in `sim.tests/MatchTests.cs` — both ends
> of the band, as two exact lists — rather than widened away, so the day somebody retunes, the tests go red
> and say which band to put back.
>
> **And no row deals zero.** The floor of the table is the Abomination at 20, so there is no dead row on the
> menu; the two rows that never win a round of the sweep are the Minion at 21 dealt per hundred gold and the
> Cursed Villager at 17, which are the same two ends of the same axis. Against the smaller four-wave field the
> test fixture plays, the Abomination is the one row that deals nothing at all.

**Measure before you retune.** Two changes have moved this number without any creep row moving — an attack type
changing line, and the clock dilating while `wave.txt`'s order ticks did not. Both were found by running the
match rather than by reading the spreadsheet, and one of them — the release cadence inside a column, a
simulation constant rather than a content number — could not be fixed from content at all.

## Which pack is which side

**KayKit's Skeletons are the creeps and the Adventurers are the towers.** Each skeleton was built as a specific
adventurer's deliberate twin, so **the two sides of the board are the two halves of one pack**, and a body reads
against the tower it is the shadow of. Quaternius's Ultimate Monsters are rejected.

**The pack holds six models and four were assigned first**: the Minion and the Skeleton share the minion skin,
the Warrior takes the warrior, the Scout the rogue and the Skeleton Mage the mage. The Minion and the Skeleton
sharing is a **kit variation and not a shortage** — the Skeleton is that model with shield and sword, and the
pack ships the weapons for it.

The two not named above are a dedicated **Necromancer** and a **Skeleton Golem**, the second of which the
publisher sells as a boss; the [collection inventory](research/kaykit-collection-inventory.md) counts all six.
**Both are assigned as of 5 September 2026** — the Necromancer model to the new Necromancer row, the Golem to
the Bone Golem — and id 7 was relabelled **Skeleton Mage** to free the name for the body that should have
carried it.

**The two-halves-of-one-pack rule is extended rather than replaced.** The expansion pulls sixteen further
characters from the Mystery Monthly series, and the line it draws is the same one: **the ones that read as
heroes join the tower side; the ones that read as undead, dark or hooded join the creeps.** That admits the
Vampire, the Witch, the Tiefling and the Werewolf, which are not undead but are unmistakably the dark half.
Quaternius's Ultimate Monsters are still rejected.

### The assignments are signed

The complete collection — 22 packs, CC0, 61 rigged characters, 159 clips — is on disk and catalogued from the
archive itself in [the collection inventory](research/kaykit-collection-inventory.md). The assignments above
are **adopted as written** rather than left as a plan, and they were adopted by a person.

**The collection is extracted, and the assignments are on screen.** It sits at
`~/repos/kaykit-collection/`, beside the checkout rather than inside it, and the seven character models the
nine units need are imported into `client/Assets/Art/Characters/` — see
[§1](research/kaykit-collection-inventory.md#1-where-it-is-and-what-it-costs-to-keep) and
[§10](research/kaykit-collection-inventory.md#10-already-imported). Nothing in the project reads that folder;
imports are copied out of it by hand.

> ⚠️ **The one thing on the board that is not signed is the bow.** The models are per unit type; the *weapon*
> is still per delivery, so `bow_withString` goes into the hand of whichever row is a `projectile` — and the
> Mage is the only one. **So the Mage draws a bow rather than holding a book**, and the Archer and the Ranger,
> being hitscan, hold nothing and stand in their bind pose.
>
> **This was not chosen; it is what the delivery rule already did**, showing through now that the models are
> right. **All three decisions were taken on 5 September 2026** and they are on this page now: every block
> above names its model, what is in each hand, and the clip it is posed by. The Mage holds `spellbook_open`,
> the Soldier `sword_1handed` and `shield_square`, the archers a bow and a quiver. **What is left is the
> plumbing** — the prop is still chosen per *delivery* rather than per row, so wiring it to the row is view
> work the art tickets carry, not another decision.

**Size tells the two sides apart and nothing else.** It was the tier signal until 5 September 2026 and it is
**retired as one** — see [the tier signal](#the-tier-signal-is-never-size). Two multipliers remain, applied to
the model as it is drawn:

| What | Scale | Why |
|---|---|---|
| Towers | **1.0** | the baseline everything else is read against |
| Every creep | **0.5** | a creep is unmistakably smaller than the thing shooting it, at any camera angle |

> **`RangerScale` at 1.5 is gone**, and the two multipliers above are the whole of what size says. What
> replaced it on the Ranger is a colour and a prop, landed in the same commit so no build ever shipped two
> identical rungs. The edit-mode test that held the old number,
> `EveryUnitTypeIsDrawnAtItsRosterScale`, asserts two multipliers rather than three, and
> `TheTwoRowsOnOneModelAreToldApartWithoutSize` is what stands where it stood: the Archer and the Ranger
> share a model and a scale, so one of the three materials must separate them.

**Scale lives in `MatchArt` and never in `content/units.txt`.** Visual size is a view fact under
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md), and a column in the content tables would make every
art tweak cost a format version and a re-record. These numbers are expected to move once somebody has looked
at them, which is the whole reason they are stored somewhere free to change.

**Measured, rather than assumed.** With those multipliers the tallest body on the board is 1.40 m and the
shortest tower 2.20 m — the Prototype Dummy the twenty-three undressed rungs stand in as, ahead of the Archer's
2.45 m — so a creep is a little under two thirds the height of the thing shooting it. An edit-mode
test measures both off the geometry and fails if the gap closes to within a fifth, because comparing the two
multipliers would prove nothing — a half applied to a taller model is not smaller than a one applied to a
shorter one, and the creeps and the towers come from two different packs.

**There is no plinth, and no rule about which units are people and which are buildings.** That distinction was
considered and dropped: it is not a thing this page needs to have an opinion about.

## What this roster needs that the schema does not have

**Decided, fixed as a list, and built.** [#213](https://github.com/ssalter21/tower-defense-game/issues/213)
fixed the list; [#216](https://github.com/ssalter21/tower-defense-game/issues/216) landed it, and
`content/units.txt` is layout 3 as of 16 August 2026. **The schema does not lack these any more** — the
section title is kept because the table below is what every block above points at. The five levers became
**nine columns**, and three of the five collapsed into one mechanic, because a sweep, a blast and an aura are
all the same shape: a bubble that emits something. The reasoning is
[ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md).

| Column | Meaning |
|---|---|
| `shield` | A pool that absorbs first and raw. Armour does not apply to it, overkill carries through to health, and it does not regenerate. 0 = none |
| `targets` | Shots per attack, each its own damage roll, targets taken nearest-to-exit first. 1 = an ordinary single shot |
| `bubbleRadius` | Milli-hex, read as a sphere. 0 = the target alone; absent = no bubble |
| `bubbleOrigin` | `self` or `target`. The Soldier's sweep centres on the tower; a mortar's blast centres on what it hit |
| `bubbleAffects` | `friend` or `enemy` — and which units that is depends on the emitter's role, because a tower's enemy is what walks and a walker's enemy is what stands |
| `bubblePeriod` | Ticks. 0 = fires with the attack; positive = pulses on its own, which is what makes it an aura. An aura is centred on `self` and may not carry `damage` |
| `bubblePayload` | `damage`, or one of the modifiable stats — speed, cooldown, armour, shield. **Range is not modifiable**, because it would force coverage back into the tick loop. Damage is not modifiable *today* either, because the keyword is taken by the roll a damage bubble spreads — a narrowing of the signed column list, and [an open question](open-questions.md) rather than a decision |
| `bubbleMagnitude` | A percentage. A shield is a share of the health it stands in front of, and may not be negative. The signed table also allowed **a flat damage amount**, which nothing implements — same open question |
| `bubbleDuration` | Ticks. 0 = instant, and for a shield it means "until spent" |

**What that authors.** A slow needs no dedicated columns at all: it is a bubble of radius 0, origin `target`,
payload `speed`, negative magnitude, positive duration. Blessing is the same mechanic with a period and origin
`self`. A mortar is origin `target` with a real radius and payload `damage`. The
Necromancer grants shield to the creeps around it — origin `self`, affects `friend`, payload `shield` —
measured in hex distance rather than along the marching column, so it reaches the neighbouring leg of a fold. A
tower that pulses over the whole board is one row.

**Effects are one model**: a stat, a magnitude and a duration. Strongest-wins with the timer refreshed on
reapplication, resolved by a strict total order so that two effects landing in either order reach the same
state. An effect is in force for exactly its duration of ticks after the one it landed on. A creep never drops
below **10% of its authored speed** — a floor binding every effect at once, which is what makes a match that
cannot end unreachable by arithmetic rather than by careful authoring. Built in #217; the reasoning is
[ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md).

**The columns landed one ticket ahead of the machinery, deliberately.** #216 built the three columns the tick
loop could read on its own — `shield`, `targets`, and a damage bubble that fires with the attack and lands
instantly — and a row authoring anything else parsed, folded, stored and refused by name the moment a match
was built out of it. #217 built the rest and deleted that refusal, so **every shape these nine columns can
author now plays**. Every one of the twenty-three towers signed on 5 September 2026 authors under these nine
columns; the three that do not are creeps, and they are the three engine asks.

A new unit is still a row, and a new column is still a format version with every stored record made under the
old one retired. **This is the last widening the roster asks for before the map has been measured.**

**The upgrade edge is not on this list.** It is [`content/upgrades.txt`](../content/upgrades.txt), a file of its
own rather than a column, and the reasoning is in
[ADR-0043](adr/0043-a-tier-is-its-own-id-and-its-own-row.md) through
[ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md).

**And the purse buys the defense.** A run opens on an empty board and a build phase pays for its placements and
its upgrades out of the same wallet its wave comes out of — see
[ADR-0048](adr/0048-a-board-is-not-a-layout.md). So the tower costs above are *based* rather than arbitrary and
something spends them.

What is missing now is a measurement rather than a mechanism. The opening purse, the income curve and the
health pool were every one of them tuned against a six-tower defense the run was handed for free, and **the
income is the one of the three that has since been measured against an empty opening board** — 168 gold a wave
rather than a hundred, because a run pays for its wall and its wave out of that one row. The purse and the
pool are still where the free defense left them; `content/ruleset.txt`'s health block says what that leaves
open.

## Open questions

1. **Layout 3 and its machinery have both landed, and the queue is now a design queue rather than a schema
   one.** Every one of the twenty-three signed towers is *authorable* and *playable* — the columns exist, the
   fixtures prove each shape [parses](../sim.tests/ContentTests.cs) and [plays](../sim.tests/EffectTests.cs).
   What is left is what was always left: naming them and signing their numbers. The ladder itself is built.
2. **Towers get nine states and creeps get five flat rows** — and the answer, from
   [13 August](decision-log.md#13-august-2026-later--the-gates-come-out-and-the-client-comes-before-the-roster),
   is that **creeps deepen by being upgraded rather than by being replaced**: stat and speed upgrades on the
   rows that exist, not new unit types. An **arcane shield** is expected and is two things at once — a pool a
   creep carries in its own right, and a pool the Necromancer grants to creeps entering its range that would
   not otherwise have one. The second half is the aura in
   [what this roster needs](#what-this-roster-needs-that-the-schema-does-not-have). See [what the creep-variety
   survey found](open-questions.md#what-the-design-research-found). **Creeps get no prerequisite chain** —
   the gating came out precisely because it held back testing, and a chain on the sending side puts a version
   of it straight back.
3. **The Soldier keeps his hex.** Answered by [#213](https://github.com/ssalter21/tower-defense-game/issues/213): one hex of range, plus a self-centred bubble,
   so his swing strikes every creep touching him rather than one of them. A corner placement inside a fold
   reaches two legs at once — a positional value the flat corridor could not offer, and the reason he was kept
   rather than retired.
4. **The Mage's splash is on the row and its price is deliberately not.** Answered on 5 September 2026: the
   bubble lands — origin `target`, radius 1000, payload `damage` — and **the 92 stands untouched** until the
   automated balance sweeps are good enough to derive it. The rule says 30, the row says 92, and that gap is
   now held open on purpose. The Sorcerer and Unravel inherit it at 124, and Slam and Mortar are the same gap
   on a different line — every one of them is a bubble priced at one body. What the deferral has cost since the
   splash landed is [the tuning target](#the-tuning-target), which is a number a person now has to decide about
   rather than a tool.
5. **The three absent shapes have their models, and every unit on this page has a signed one.** Answered on
   5 September 2026 by a person, from a rendered sheet of all 32 candidates — and the Druid's weirwood, the
   one shape that sheet left open, was signed the same day from a sheet of its own:
   [`Tree_Bare_1_C_Color8`](https://github.com/ssalter21/tower-defense-game/issues/274#issuecomment-5552677475).
6. **A capstone token exists as a rule and not yet as code.** The cost section above states it; nothing
   grants or spends one. Until [the token ticket](https://github.com/ssalter21/tower-defense-game/issues/273)
   lands, the nine capstone rows can be authored and drawn but not bought.
7. **The beside slot is built and one rung wants two of it.** The Engineer's turret, the Paladin's statue,
   the Cleric's font and the Druid's weirwood each stand one tile from their tower's root, at a size written
   down per prop. The Artificer's look puts a crate beside the turret, which is two props beside one tower —
   the one look on this page the socket as built cannot draw whole.
