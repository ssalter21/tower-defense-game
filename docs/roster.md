# The roster

**The design side of [`content/units.txt`](../content/units.txt).** That file holds the numbers the simulation
reads; this one holds what each unit is *for*, what it looks like, and what about it is still unsigned. Where a
number appears here it is a **proposal** until it appears there.

## How to edit this

- Write one block per unit, the same five lines every time, and leave a line blank when it is undecided: a
  blank is not an omission, it is the ask, and the blanks are the agenda for the next conversation.

| Line | What goes on it |
|---|---|
| `Does` | The mechanic, in the terms the simulation would have to implement |
| `Looks` | The art direction — model, silhouette, what reads at a glance |
| `Numbers` | `units.txt` on a live row; on a proposed one, only what has been decided, `_` for what has not |
| `Needs` | What the schema or the engine would have to gain; `nothing` means it is authorable today |
| `Open` | The question that has to be settled before it can be signed |

- Give every row one of four statuses: `proposed` (written here and nowhere else), `signed` (the numbers are
  agreed), `live` (there is a row in `content/units.txt`), `retired` (there was one, and there is not now).
- Take the next id from `units.txt`'s one global space and never reuse one: ids ascend forever, are not an
  index and are not reserved in advance — the next unit built takes id 50, whatever it is. A tier is its own
  id and its own row; the Sergeant is not the Soldier with a flag set.
- Write line by line, not as a wide table, because a wide table is unreadable in a diff.

## What things cost

**Neither side of the purse is authored. Both are arithmetic**, and they are priced in the same quantity so
that one wallet can buy both — which is what [§3's one purse](vision.md#one-purse) requires.

| | The rule | Which means |
|---|---|---|
| **A creep** | effective health ÷ 160 | You pay for the health a defense must spend to stop it. Effective health is the pool times the armour multiplier, and it does not include a shield |
| **A tower** | one gold per **5 damage a second**, times the bodies a shot hits | You pay for the health it removes |

Signing a creep therefore means signing **health, speed and armour**; the price follows. Signing a tower means
signing **damage, cooldown and how many bodies it hits**. A dead tie rounds up.

**A capstone carries no gold price.** A run is granted one capstone at rounds 3, 6 and 9, it banks, and it is
the whole price of a capstone edge — `show-ladder` prints `1 capstone token` where it prints gold on every
other edge, and seven of the nine capstone rungs read flat against the rung below because the rule prices the
capstone at what it replaces and nothing charges that. What the rule does not price — range, radius, shield,
duration, windup and backswing, the transforming pair, the spawner — and why no premium, coefficient or hand
price is authored to cover the gap, is [ADR-0063](adr/0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md).
The rule is provisional: a cost derived from the sweep may replace it.

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

**The wider roster populates both cases and neither is an accident.** The Shade's 84 is
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

**Nine lines, three tiers each, and one attack type per line.** It is what makes the three-way cycle readable
off the board: you know what a tower does to a body by knowing which line it came from, and it costs nothing,
because attack type is a column that already exists.

Impact ×3 — Knight, Barbarian, Engineer. Pierce ×2 — Archer, Rogue. Magic ×4 — Mage, Druid, Cleric, Paladin.
**Magic is over-represented on purpose**: the creep side is undead and mostly armoured, and magic is what
beats armoured. The creep table balances it back with swift and arcane bodies.

**The second stage is one stat. The third stage is a capstone that changes how the tower works**, and each
capstone is drawn from what its model is holding or wearing.

**A rung is told apart by what the body wears, holds or stands beside — never by how big the body is.** The
three materials, the two lines that bend the rule, and what every row is drawn as are
[the roster's art](roster-art.md).

**Every tier on this page is a row in `content/units.txt`**, at ids 15 to 37 in the order the lines are
written above; see [the column list](#the-columns-the-blocks-point-at). What each one
still needs is its art.

> **Windup and backswing are signed on every tower row, and on four of them the signed number is zero.**
> Both add to the cooldown — a tower spends the windup before its shot lands and the backswing after it, and
> the view stretches the swing clip across the windup and the rest clip across the backswing, so at zero a
> tower fires inside the tick it acquires and its swing is never drawn. The Paladin, Cleric, Druid and Engineer
> lines sit at the proportion the Knight, Barbarian, Mage and Archer lines do — windup about 0.45 of the
> cooldown, backswing about two thirds of the windup. **The Rogue line and Overwatch carry zero as a choice**:
> a knife fires the tick it sees you and a 7-tick cooldown has no room for a windup that reads, and the
> Overwatch holds an aiming pose in every slot with no swing to draw. **A pair belongs to a line, not a rung**
> — the Sergeant at cooldown 11 keeps the Soldier's 7 and 5, the Blessing keeps the Paladin's — so a rung's
> own cooldown never moves it. **The sixteen are holding answers**: the rework is
> [the animation score](vision.md#6-what-it-looks-like) — once a line's score is signed, its windup and
> backswing are derived from the release frame rather than authored, and these numbers move with it, line by
> line.

## The Knight line — impact, melee

### 11 · Soldier · tier 1 · status live

- **Does** — one hex of range, striking every creep touching him, fast. The adjacency floor means height never
  takes his neighbours away from him.
- **Looks** — knight, full helm down, short sword.
- **Numbers** — `units.txt`.
- **Needs** — nothing. `bubbleRadius` and `bubbleOrigin` landed with layout 3, so the sweep is a bubble on
  himself with no period: radius 1000, origin `self`, affects `enemy`, payload `damage`. **It is not authored
  in `content/units.txt`** — the row there still fires one shot at one creep, because giving the Soldier his
  sweep is a design decision and a balance change rather than a schema one.
- **Open** — none. A tower that strikes everything touching it is the one tower whose whole value is
  positional, which is exactly what a fold is for.

### 15 · Sergeant · tier 2 · status live

- **Does** — swings faster. One stat.
- **Looks** — `Knight`, `knight_texture_alt_A`, and a `shield_square` in the off hand.
- **Numbers** — `units.txt`.
- **Needs** — nothing. A cooldown is a column.
- **Open** — none.

### 16 · Shield Wall · tier 3 · status live

- **Does** — every creep touching him walks at half speed while it is touching him, and he keeps swinging.
- **Looks** — `Knight`, `knight_texture_alt_B`, shield raised (`Melee_Blocking`), visor closed. Every pulse
  leaves a ring on the ground at the hex the slow carries; the glow the section above reserves stays the
  Blessing's, because a slow that stops at one hex is read by where it stops.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

> **It is the one slow in the game that costs no range.** Purely positional, and it bunches bodies under
> whatever stands beside him. On a fold, that is the Barbarian.

## The Barbarian line — impact, melee, slow and heavy

### 17 · Barbarian · tier 1 · status live

- **Does** — one hex, slow, heavy, one target.
- **Looks** — `Barbarian`, `axe_2handed`, `Melee_2H_Attack_Chop`.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 18 · Berserker · tier 2 · status live

- **Does** — hits harder. One stat.
- **Looks** — `Barbarian`, `barbarian_texture_alt_A`, `axe_2handed_Large`.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 19 · Slam · tier 3 · status live

- **Does** — every swing hits everything touching him. The same roll, every body.
- **Looks** — the **`Barbarian_Large`** model, `Melee_2H_Slam`. This is the line's second model, and it is on
  the **Large rig**, so it needs that rig's clip bank. Every swing cracks the ground out from under him to the
  edge of what it reached.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

> **This is where the retired Hero's 360° sweep went** —
> on a model that ships a two-handed slam clip for it.

## The Paladin line — magic, melee

### 20 · Paladin · tier 1 · status live

- **Does** — one hex, holy damage, one target.
- **Looks** — `Paladin`, bare head, `paladin_hammer`, swinging **`Melee_1H_Attack_Chop`**.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 21 · Templar · tier 2 · status live

- **Does** — hits harder. One stat.
- **Looks** — the **`Paladin_with_Helmet`** model, `paladin_hammer` and `paladin_shield`,
  swinging **`Melee_1H_Attack_Chop`** as the rung below does.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — the second model lands at tier 2 here rather than tier 3, because the helmet is the smaller of
  the two changes this line has available and the statue is the larger.

### 22 · Blessing · tier 3 · status live

- **Does** — every tower within two hexes fires a quarter faster, always.
- **Looks** — `Paladin_with_Helmet`, `paladin_texture_B`, `paladin_book` open, and the gold `paladin_statue`
  standing on the tile beside him — **drawn at 1**, the size it imports at, which is 2.55 m tall and 1.60 across
  and stands level with the Paladin himself. Every pulse puts a ring over the head of each tower it reached,
  which is the glow the section above reserves and the one row it is reserved for. It casts with
  **`Ranged_Magic_Raise`** — arms raised rather than a hand thrown forward, which is the
  one of the five magic clips that reads as a blessing rather than a bolt.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

> **Two Blessings over one tower do not stack; the timer refreshes.** That is the rule the effect model
> already has, and it is what stops a ring of Paladins running away. This is where the Captain's attack-speed
> aura went when it was retired.

## The Cleric line — magic, ranged

### 23 · Cleric · tier 1 · status live

- **Does** — three hexes, holy bolt, one target.
- **Looks** — `Cleric`, `Cleric_Tome`, `Ranged_Magic_Shoot`. Every shot puts a short bolt in the air out of
  the tome, crossing to the body it found.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 24 · Bishop · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Cleric`, `cleric_texture_B`, `Cleric_Mace` in the melee hand and the off hand empty. The bolt
  is the Cleric's and it leaves the **open off hand**: a mace is a melee weapon and does not fire, and the
  tome was refused in every position — in the off hand, beside him, anchored — because it fouls the
  animation and the model.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 25 · Consecration · tier 3 · status live

- **Does** — every undead within two hexes loses a third of its armour while it is there.
- **Looks** — `Cleric`, `cleric_texture_B`, `Cleric_Mace`, and the `Cleric_Font` on the tile beside him, light
  on the ground — **drawn at 1**, which is 0.81 m tall and 1.44 across, a basin at knee height. The Cleric has
  **no second model anywhere in the collection**, so this line is colour and props at every rung. Every pulse
  lays a disc of light on the ground out to the edge of the aura, so what the font has claimed is the ground
  itself rather than a boundary round it; the bolt is the line's, off the open off hand. **The light is centred on
  the tower and not on the font**, which stands one tile away: the aura's own centre is the tower, and a disc
  drawn round the prop would report a reach the simulation never had.
- **Numbers** — `units.txt`.
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
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

**The line's identity is fast-and-modest**, and the home for slow-and-heavy is Overwatch at tier 3. Four of
the six committed defense slots are Archers, so retuning this row moves most of what the golden trace measures.

### 14 · Ranger · tier 2 · status live

- **Does** — +1 hex of range.
- **Looks** — `Ranger`, `ranger_texture_alt_A`, and a `quiver`. **The 1.5 scale is reverted** — size is no
  longer a tier signal anywhere on this page, and the colour and the quiver are what separate the rungs on
  sight. The revert and the replacement land in the same commit, so no build ever ships two identical rungs.
- **Numbers** — `units.txt`.
- **Needs** — nothing. It is the only tier on this page that is purely a number.
- **Open** — none. A tier that is one stat is the middle rung, and a second clause can be added to it later
  without moving its id.

### 31 · Overwatch · tier 3 · status live

- **Does** — sees the whole leg. Slow, enormous single shots from wherever he is stood. **This is where the
  line's slow-and-heavy tuning lives.**
- **Looks** — the **`Marksman`** model, prone-ish `Ranged_2H_Aiming`, holding **`crossbow_2handed`** from the
  Adventurers pack. Every shot draws one heavy bar from the crossbow to the body, the whole length of the leg
  it crossed — which is the line's own read, since eight hexes against the Archer's three is what this row is.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

> **The rifle is rejected and the crossbow is signed.** `Marksman_Rifle` is the only firearm in the collection
> the roster would touch, and it puts the top of the Archer line in a different century from every other body
> on the board. The crossbow is a different pack's art style, which is the smaller break of the two.

> **Multishot is the Rogue's**, because it belongs to the model
> that throws knives; the Marksman has the long single shot instead.

## The Mage line — magic

### 4 · Mage · tier 1 · status live

- **Does** — magic damage with splash of one additional hex.
- **Looks** — the mage, book in hand. The flash leaves the **point of the hat** and the shell is what crosses
  to the body; the splash it lands with draws the Mortar's burst, which is the open question below and not
  this rung's choice. The hat's point is the one origin the hat cannot cover — from the open spellbook, the
  Mage nearest the camera at the built player's framing shows no flash at all — and the cost, a tracer
  leaving from a hat, is one a sheet shows and a played frame does not. The Sorcerer keeps its staff tip.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The splash is on the row: origin `target`, radius 1000, payload `damage`.
- **Open** — the price, and only the price. **The cost stays 92 and is not re-derived.** The rule's bodies
  term reads `targets`, which is 1, so the rule says 30 and the row says 92; that is a known gap held open on
  purpose. Repricing a row whose value is a splash radius is exactly what the cost rule is worst at, and
  **the price waits for the automated balance sweeps to be trustworthy enough to derive it.** Until then 92
  stands and the sweep reports what it is worth.

### 26 · Sorcerer · tier 2 · status live

- **Does** — casts faster. One stat.
- **Looks** — `Mage`, `mage_texture_alt_A`, holding `staff` rather than the open book. The flash leaves the
  staff tip; the splash wears the Mortar's burst, as the Mage's does.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — the price, as the Mage's is. 124 is the Mage's 92 scaled by the cooldown it changed, so this rung
  inherits the deferral rather than making a second decision. The damage rule reads 41 against it.

### 27 · Unravel · tier 3 · status live

- **Does** — his bolt strips most of the armour off what it hits, for five seconds.
- **Looks** — the **`Lorekeeper`** model, `Lorekeeper_Tome` open. The Lorekeeper is the one character in the
  roster with **no alternate texture**, which costs nothing: this rung is a model swap. Where the bolt arrives,
  a band broken into plates lies on the ground out to the edge of the strip — the one shape on this page picked
  by a bubble's payload rather than by its row, because the blast names the body and never him.
- **Numbers** — `units.txt`.
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
- **Looks** — `Druid`, `druid_staff`, `Ranged_Magic_Shoot`. Every shot puts a short bolt in the air out of the
  staff tip, the same shape the Cleric line fires.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 29 · Elder · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Druid`, `druid_texture_alt_A`. The staff and the bolt are the Druid's, and nothing joins
  them: this rung is colour alone — *"Druid has staff only, it can have potions if it has an aura"*, and it
  has none.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 30 · Overgrowth · tier 3 · status live

- **Does** — the whole board slows a fifth while he stands. Every board.
- **Looks** — `Druid`, `druid_texture_alt_B`, and a **bare weirwood standing on the tile beside him** —
  **`Tree_Bare_1_C_Color8`** from the Forest Nature pack, signed from a rendered sheet of all six `Color8`
  bare trees turned through the game's own six camera angles. It is the largest of the six at
  936 triangles and the only silhouette that reads as an ancient tree rather than a dead stick from every
  angle. **Drawn at 0.55**: at its own size it spreads 3.74 m, which is nearly two tiles and reaches back
  through the Druid, and 0.55 brings that to the 2.06 m of the tile it stands on and leaves it 2.89 m tall,
  half again the Druid's own height. **The roots are drawn under every body the aura is holding** rather than
  at its radius, because that radius is sixty hexes and the board is nineteen — see the section above.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The beside slot is built and the tree is picked.
- **Open** — none.

> **The Druid keeps his own body, and the PlantWarrior is set aside.** It was proposed as this line's second
> model and it is rejected: of the six second models it was the only one that read as a *different creature*
> rather than the same person promoted. So this line has no model swap, and it is colour at every rung and a
> prop where the rung has one — the Overgrowth's weirwood, and nothing on the Elder.

> **A whole-board pulse is one row**, and this is where the retired elemental branch's area slow went. **A creep never drops below a tenth of its authored
> speed** — a floor binding every effect at once — so stacking Overgrowth with Shield Wall has a bounded
> bottom rather than an open one.

## The Rogue line — pierce, short range, very fast

### 32 · Rogue · tier 1 · status live

- **Does** — two hexes, three throws a second, light.
- **Looks** — `Rogue`, `dagger`, the `Throw` clip.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 33 · Cutthroat · tier 2 · status live

- **Does** — throws faster. One stat.
- **Looks** — the **`Rogue_Hooded`** model, `dagger`.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — the second model lands at tier 2 here, because the hood is this line's smaller change and the
  capstone is carried by a clip and a `targets` column rather than by a body.

### 34 · Fan of Knives · tier 3 · status live

- **Does** — three knives a throw, at the three bodies nearest the exit.
- **Looks** — `Rogue_Hooded`, `rogue_texture_alt_A`, dual `dagger`, `Melee_Dualwield_Attack_Slice` as the
  throw. Each shot draws a knife leaving the hand and crossing to the body it found, so a throw that finds
  three bodies puts three knives in the air at once.
- **Numbers** — `units.txt`.
- **Needs** — nothing. Target selection answers an ordered *n* under the same total order it answers one
  under.
- **Open** — **which hand the knives leave from.** Both daggers are the same asset under the same node name,
  the anchor names that node, and it resolves to whichever the lookup reaches first — measured as
  `handslot.l`, the off hand, where this row's own two rungs below throw from `handslot.r`. It is
  deterministic and it is not a decision: naming the other hand, or alternating them, is picking a hand and
  that is not the view's to pick. `ImportedArtTests` logs what every row's anchor resolved under, so a green
  run says which hand it is.

> **Three shots at one roll each, not one roll split three ways.** `targets` of *n* fires *n* shots at *n*
> creeps and draws *n* damage rolls; one shot split *n* ways is the other shape, and it is a bubble. This is
> where the Marksman's multishot went.

## The Engineer line — impact, projectile, long range

### 35 · Engineer · tier 1 · status live

- **Does** — four hexes, slow lobbed shot, one target.
- **Looks** — `Engineer`, `engineer_Wrench` in hand, a `turret_base` on the tile beside him doing the firing —
  **drawn at 1**, which is 1.13 m tall and 1.00 across, and the shell leaves the top of it at 0.77 m rather
  than leaving the man. He rests in `Idle_A` and works the turret with **`Use_Item`** — the same
  `Rig_Medium_General` bank, so a clip looks the same on him as on the Paladin — and all three rungs share
  both.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

### 36 · Artificer · tier 2 · status live

- **Does** — reaches further. One stat.
- **Looks** — `Engineer`, `engineer_texture_alt_A`, and the turret **drawn at 1.25**. No crate: the body
  already wears a gold box, so an `ammo_crate` would be the same box twice, and *"scale the tower twice"* is
  what tells the three rungs apart instead.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The beside slot is built.
- **Open** — none.

### 37 · Mortar · tier 3 · status live

- **Does** — the shell bursts across a hex and a half.
- **Looks** — `Engineer`, `engineer_texture_alt_B`, the turret **drawn at 1.5** and the lobbing arc drawn.
  The Engineer has **no second model anywhere in the collection**, so this line is colour and a turret that
  grows at every rung — the one rung signal that reads at 1600x900. The shell bursts in shards on the body it arrived at, out to the
  radius it landed in.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The beside slot is built.
- **Open** — **the burst is one of two shapes no row selects**, so the Mage's and the Sorcerer's splash wear
  it too. The Unravel's strip is the other, and it is told from this one by its payload. See the open question
  below.

> **Two blasts on the board, and they are not the same tool.** The Mage's is magic at radius 1000 and lands at
> tier 1; this one is impact at radius 1500 and costs a token. The impact one is the answer to arcane bodies
> the Mage cannot chew.

---

# Creeps

**Creeps never attack.** `dmgMin`, `dmgMax` and `attack` are zero and `none` on every walking row, and no aura
below is an exception to that — they buff, shield, hasten and hobble, and none of them deals damage.

**Creeps deepen by being upgraded rather than by being replaced** — stat and speed upgrades on the rows that
exist, not new unit types — and **creeps get no prerequisite chain**: gating on the sending side is a version
of the gating that held back testing.

> **Seventeen creeps are live.** Armour is spread deliberately: **seven armoured, five swift, five arcane**,
> which balances back a tower side that is four-ninths magic, so no column of the matrix has a single
> occupant.

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
| 38 | Necromancer | 2600 | 28 | arcane | 30 | — | 36 | 3380 | **21** |
| 39 | Bone Golem | 9000 | 14 | armoured | 60 | — | 48 | 14400 | **90** |
| 40 | Black Knight | 5000 | 22 | armoured | 80 | — | 48 | 9000 | **56** |
| 41 | Frost Wight | 6000 | 16 | arcane | 40 | — | 48 | 8400 | **53** |
| 42 | Abomination | 12000 | 12 | armoured | 0 | — | 48 | 12000 | **75** |
| 43 | Vampire | 2800 | 44 | swift | 20 | 1400 | 36 | 3360 | **21** |
| 44 | Witch | 2000 | 33 | arcane | 20 | — | 36 | 2400 | **15** |
| 45 | Fiend | 3200 | 33 | arcane | 45 | — | 36 | 4640 | **29** |
| 46 | Shade | 1200 | 84 | swift | 0 | — | 36 | 1200 | **8** |
| 47 | Cursed Villager | 1800 | 28 | swift | 0 | — | 36 | 1800 | **11** |
| 48 | Werewolf | 2600 | 50 | swift | 10 | — | 36 | 2860 | **18** |
| 49 | Grave Robber | 3000 | 22 | armoured | 30 | 2000 | 36 | 3900 | **24** |

> **`dying` is signed on every row, by rig.** A body on the medium rig dies over **36** ticks and one on the
> Large rig — the Bone Golem, the Black Knight, the Frost Wight and the Abomination — over **48**. At zero
> `CreepView` draws no death at all — the clip plays across exactly the ticks the simulation gives the state,
> so a corpse would be gone the tick it fell. A dying body is untargetable, raises nothing and pulses nothing,
> so the number moves no leak and no reading; what it moves is the content hash, and the frame a person sees.
> Signed by the rule rather than by eye, and open to being moved by eye later without touching anything else
> — the clips are already bound, `Death_A` on both rigs.

### 1 · Minion · status live

- **Does** — health and nothing else. The baseline body.
- **Looks** — the minion skin, no tools.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

**This is the row every other row is read against**, which is why nothing about it moves. Re-baselining it
would re-baseline every measurement in the sweep.

### 12 · Skeleton · status live

- **Does** — the Minion with a little armour. The low rung of the armoured ladder.
- **Looks** — the minion skin with shield and sword.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none, but see below.

> **This is knowingly the dullest row on the page** — the Minion at the same speed with a bigger pool and some
> armour. Speed is the only lever that would fix it, and moving it off 28 breaks the whole-multiple
> relationship with the Scout that makes the target-selection tiebreak get consulted at all. A boring middle
> rung was judged cheaper than deleting a test.

### 13 · Skeleton Warrior · status live

- **Does** — slow and genuinely armoured. The heavy.
- **Looks** — the warrior skeleton, full kit.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

**What separates it from the Skeleton is two axes, not one** — armour 45 against 20, and speed 18 against 28.

### 2 · Skeleton Scout · status live

- **Does** — fast, no armour value.
- **Looks** — the rogue skeleton.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

**Exactly twice the Minion's speed, and that is load-bearing rather than tidy.** See [the clock](#the-clock).

### 7 · Skeleton Mage · status live

- **Does** — **Haste**: every creep within two hexes walks a fifth faster.
- **Looks** — `Skeleton_Mage`, `Skeleton_Staff`, casting continuously.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Draws** — a flat translucent circle on the ground, in its own green, out to the reach of the haste. The
  shape is **signed**; how see-through it is is not. See
  [what a row is drawn as](roster-art.md#what-a-row-is-drawn-as).
- **Open** — none.

> **Three rules of a shield aura are the implementer's reading rather than a decision**, and any of them can
> be moved without another format version: the granted pool **persists until spent or until its duration
> ends**, whichever comes first, with a duration of zero meaning until spent; it does **not** move with its
> source, so killing the source stops the pulses and what is already granted is spent or times out rather
> than vanishing; and the magnitude is **a share of the health it stands in front of**, because a pool has no
> rate of its own for a percentage to be a percentage of. They apply to
> [the Necromancer](#38--necromancer--status-live). See
> [ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md).

### 38 · Necromancer · status live

- **Does** — **Ward**: grants creeps within two hexes a shield worth a quarter of their health, every three
  seconds. **Raise**: spawns a Minion beside itself every **150 ticks**, for as long as it lives.
- **Looks** — the pack's own `Necromancer` model, `Skeleton_Scythe`, **carried at no
  turn** — along the shaft, the way the pack authored it, and not stood upright the way
  the Mage's and the Druid's staffs are.
- **Numbers** — `units.txt`.
- **Needs** — nothing. Both are on the row and playing.
- **Draws** — a flat translucent circle on the ground out to the two hexes the ward covers, in the pool's own
  blue, for ten ticks. It is the moment the pool went out and not the pool: what a body then carries is the bar
  above it. The shape is **signed**; the alpha is not.
- **Open** — none. **The cost stays at its derived 21 until the levers are priced, by decision** — not by
  omission. A hand price (21 plus the eleven Minions it raises against the committed defense, about 131) is
  declined: it would be the first authored cost on a table that is derived everywhere else, guessed against
  one corridor, and overwritten the day the sweep-derived rule lands. A cap on the raise is out of scope — it
  reopens a row this page signed. **The 1200 here and the 1399 in the sweep are the acceptance test for that
  rule**: the day a creep price can see a pool, a reach and a raise, both readings come inside their bands or
  the rule is wrong.

> **The first raise is a whole period after it arrives**, and every one after that a period apart — where an
> aura pulses on the tick its emitter spawns. The two are deliberately different: a pulse costs a body nothing
> to have arrived, and a raise that fired on the arrival tick would mean a Necromancer that shows up already
> accompanied. *Every 150 ticks* is read as 150 ticks after it gets there.
>
> **The body arrives beside it**: at the Necromancer's own distance along the route, in the next lane offset,
> on a full Minion pool and carrying whatever a Minion's row authors. It is a body that spawned rather than a
> body that was handed anything, and it walks and is shot at from the tick after. It enters the creep array
> behind everything already standing, so **it loses every target-selection tie it is in** — a tower looking at
> both shoots the Necromancer.
>
> **There is no cap on how many it raises, and that is the decision rather than an omission.** It raises for
> as long as it is alive and walking, so the board is what bounds it: against the committed defense one
> Necromancer raises **11** before it leaks. A hundred and ten gold of bodies from a twenty-one gold row is a
> sweep finding, and it is left standing.
>
> **A leak of a raised Minion charges health at the Minion's own 10 gold**, so the defending half of the
> exchange is honest; what nobody paid is the sending half, and that gap is
> [open](open-questions.md#what-is-a-spawner-worth). It is why this row's band reading is 1200.
>
> **The arithmetic that guarantees a match ends covers it, and covers arrival rather than population.** A body
> raises only while it walks and a Minion raises nothing, so the last body raised is at the exit within one
> floored crossing of the latest its raiser could still have been walking — which is what
> `Match.RequireItArrives` proves at construction. How many arrive between here and there is deliberately
> unbounded. See [ADR-0060](adr/0060-a-creep-raises-a-creep-and-the-board-is-what-caps-it.md).

### 39 · Bone Golem · status live

- **Does** — nothing but mass. Half the Minion's speed.
- **Looks** — `Skeleton_Golem`, `Skeleton_Golem_Axe_Large`. On the Large rig, which walks and dies.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none. **14 is exactly half the Minion's 28**, so it is passed on whole ticks.

### 40 · Black Knight · status live

- **Does** — the Knight's twin. Nothing but armour.
- **Looks** — `BlackKnight`, `BlackKnight_Sword_Large`, `BlackKnight_Shield_Large`. Large rig.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 41 · Frost Wight · status live

- **Does** — **Frostbite**: towers within two hexes fire a third slower while it passes. The only creep aura
  that reaches the tower side.
- **Looks** — `FrostGolem`, `FrostGolem_Axe`. Large rig.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Draws** — a flat translucent circle on the ground out to the reach of the frostbite, in a pale frost blue.
  **It is the only thing on screen that says a tower is firing slower**, and that is true of every
  modifier: nothing is drawn on a body an aura found, on either side of the board. The shape is **signed**; the
  alpha is not.
- **Open** — none.

### 42 · Abomination · status live

- **Does** — the biggest body on the board. No armour: flesh, not bone.
- **Looks** — `Monstrosity`, `Monstrosity_BarndoorShield_Large`. Large rig.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 43 · Vampire · status live

- **Does** — **Blood**: a raw pool armour does not apply to, spent before health.
- **Looks** — `Vampire`, `Vampire_Sword`.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Draws** — nothing of its own. The pool is the blue segment of the bar drawn over the body, out of the
  snapshot, so it survives a scrub and there is no event a decoration could hang off. **Unsigned**, like every
  other mark.
- **Open** — **the shield is unpriced, and the sweep has measured what that is worth.** The cost rule has
  no term for a pool, so this row is cheaper than it should be: it returns 94 percent of a column's gold
  against the committed defense, a point under the band's edge, beside the Cursed Villager and one under the
  Skeleton Mage. Known gap, same family as radius and range; a sweep target, not something to hand-correct.

### 44 · Witch · status live

- **Does** — **Hex Ward**: creeps within two hexes gain 30 armour.
- **Looks** — `Witch`, `Broom`, **carried at no turn**, for the reason the Necromancer's
  scythe is: these two walk rather than swing, and a turn that reads in a still buries the shaft in
  the body mid-stride.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Draws** — a flat translucent circle on the ground out to two hexes, in the armour violet the Unravel's
  strip also uses; only the Unravel wears the broken band, because a blast is
  not an aura. The shape is **signed**; the alpha is not.
- **Open** — none.

### 45 · Fiend · status live

- **Does** — an arcane heavy; the Warrior's counterpart on another armour type.
- **Looks** — `Tiefling`, `Tiefling_SwordsBackpack`. A horned demon rather than an undead body — the theme is
  *undead, and the dark or hooded*, and this is the dark half, the same licence the Witch and the Vampire use.
- **Numbers** — `units.txt`.
- **Needs** — nothing.
- **Open** — none.

### 46 · Shade · status live

- **Does** — three times the Minion's speed. The fine end of the granularity axis.
- **Looks** — `Ninja`, `Ninja_Katana`, **in the darkest of the pack's four atlases**. Read as a silhouette at
  gameplay distance it stops being a ninja; that is the whole reason the model is admissible, and the atlas
  pick is not optional decoration.
- **Numbers** — `units.txt`.
- **Needs** — nothing. The atlas pick rides on the same per-row texture work the tier signal needs.
- **Open** — none. **84 is exactly three Minions**, so it passes on whole ticks like the Scout does.

### 47 · Cursed Villager · status live

- **Does** — a cheap body that is the Werewolf's first form. **On the first damage it takes, it becomes the
  Werewolf.**
- **Looks** — `Werewolf_Man`, `axe`.
- **Numbers** — `units.txt`.
- **Needs** — nothing. `content/units.txt` layout 4 carries the `becomes` column, and this is the one row on
  the roster that fills it in.
- **Open** — none.

### 48 · Werewolf · status live

- **Does** — fast and durable at once. What the Cursed Villager becomes.
- **Looks** — `Werewolf_Wolf`.
- **Numbers** — `units.txt`.
- **Needs** — nothing. Both rows still walk on their own, and nothing on the roster sends a Werewolf: it is
  what a Cursed Villager becomes.
- **Open** — none.

> **A lethal first hit does not kill the Villager; it produces a Werewolf at full health.** The trigger is the
> first damage taken, and the change resolves ahead of the damage — so the Werewolf enters on its own full
> 2600 rather than on whatever the Villager had left, and **no Cursed Villager can ever be one-shot**: the row
> that named a successor is already gone when the death check runs, so no hit of any size kills it. What else
> carries over at the change is
> [ADR-0059](adr/0059-a-creep-becomes-another-row-mid-lane.md).
>
> **The trigger is signed: first damage, not death.** The 7 gold between what the rule prices the pair
> at and what is charged is a pricing gap for the sweep-derived cost, not a reason to move the trigger; the
> arithmetic is in [ADR-0063](adr/0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md).

### 49 · Grave Robber · status live

- **Does** — the pack soaks hits: a raw pool in front of ordinary health. **Pays 12 gold to the defender that
  kills it**, mid-match, into the one purse.
- **Looks** — `Hoarder`, wearing `Hoarder_Backpack`. **The backpack, not the sword** — the pack is what the
  mechanic is about, and `Hoarder_Sword` stays out of the hand it would otherwise take. **The blade
  in the body's own front pouch stays**, signed against the same body with it
  hidden: it is a piece of `Hoarder.fbx` rather than something the row hands it, and a robber
  wearing a knife it never draws is a robber. The sheet that asked settled a second thing too —
  with the loose backpack held in a hand the two renders come out byte-identical, because a pack in
  front of a body already wearing one hides the belt entirely.
- **Numbers** — `units.txt`.
- **Draws** — nothing of its own, as the Vampire's pool draws nothing: the pack is the blue segment of the bar
  over the body.
- **Needs** — nothing. `content/units.txt` layout 6 carries the `bounty` column, and this is the one row on
  the roster that fills it in.
- **Open** — the shield is unpriced, as the Vampire's is.

> **A bounty may not exceed the row's cost, and the table refuses one that does**:
> the money is minted, so a body worth more dead than sent makes killing the field's wave a better income
> than the round's own, and no instrument would see it. Equal is allowed; the half below is this row's own
> argument.
>
> **Twelve is half its own price, and the half is the point.** Paying its full 24 back would make it free to
> send. Half means killing it refunds half of what the attacker laid out, so it is a body that rewards being
> killed without being one you are glad to see. **A leaked Grave Robber pays nothing** — reaching the exit is
> the opposite outcome and is already charged, at the cost column, against health.

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

### Proposals retired

**These never reached `units.txt`, so no id is burned and nothing is pinned to them.** They are recorded
because each was written down here for weeks and would otherwise be re-proposed.

| row | was | where it went |
|---|---|---|
| Captain | tower, tier 2, attack-speed aura | The aura is the **Paladin's Blessing**, on a model that ships a book and a statue for it. A tier 2 is one stat |
| Hero | tower, tier 3, 360° sweep | The sweep is the **Barbarian's Slam**, on a model that ships a two-handed slam clip |
| Pyromancer | tower, tier 2a, fire branch | Retired with the branch |
| Cryomancer | tower, tier 2b, frost branch | Retired with the branch. The area slow is the **Druid's Overgrowth** |
| Frostfire Archmage | tower, tier 3, both branches | Retired with the branch |

> **The branch is what was retired.** Three stages, no branch — *one line, three stages* is an invariant with
> no exception. It also stops two roads ending at one tower, which made the pick a tempo decision rather than
> a build decision.

**Nothing structural breaks.** Stored bundles carry their own copy of the unit table —
`content/golden/defense-0.units` is still in the fifteen-column layout 1 and still replays — so retiring a row
invalidates no record; it leaves those bundles pinned to an older roster, which is exactly what they are for.

## What is deliberately absent

> **All three are filled.** They were blocked on models, and the table below is kept as the record of what
> was absent and what closed it.

**Recorded so it is not silently re-proposed.** These were never design rejections — they were shapes that
were wanted and blocked on art rather than on argument.

| shape | what it was for | what filled it |
|---|---|---|
| **Fast and cheap, in numbers** | The fine end of the granularity axis — many light bodies, so a purse is a decision about *shape* rather than a lookup | The **Shade**, at speed 84 and 8 gold |
| **Slow, dear and very tough** | The coarse end — a few heavy bodies, priced the same | The **Bone Golem** at 9000 and the **Abomination** at 12000 |
| **Fast and durable at once** | The pairing `lancer` occupied without a design behind it | The **Werewolf**, and the design behind it is the transformation |

> **These are named by their levers on purpose.** *Swarm* and *wall* are rejected as names:
> speed, health and armour are the levers, and the two ends of the granularity axis are
> just the ends of it. A category name invites a category the schema does not have. Same reasoning as
> [§12's *ordinary* and *game changer*](vision.md).

## The tuning target

**A quarter to a half of the wave leaks — ten to twenty of forty — and the committed match is under it at
eight, by choice.** `sim.tests/MatchTests.cs` asserts both; the miss ends on a Unity playtest saying the leak
feels wrong or on the derived cost landing. What each row returns against the committed defense, and which
six sit outside their band, is [the tuning target's readings](research/the-tuning-target.md).

## Which pack is which side

**KayKit's Skeletons are the creeps and the Adventurers are the towers**; towers draw at 1.0 and every creep
at 0.5, and a creep on the Large rig may stand as tall as a tower. The assignments, the scale rule and what
nothing measures are [the roster's art](roster-art.md#which-pack-is-which-side).

## The columns the blocks point at

`content/units.txt` carries every column below; the blocks above are written in their terms. A sweep, a blast
and an aura are one bubble ([ADR-0055](adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md)); the
transformation is [ADR-0059](adr/0059-a-creep-becomes-another-row-mid-lane.md), the raise
[ADR-0060](adr/0060-a-creep-raises-a-creep-and-the-board-is-what-caps-it.md) and the bounty
[ADR-0061](adr/0061-a-kill-pays-the-defender.md).

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
| `becomes` | The id of the row a body of this one turns into the first time damage reaches its health, or `none`. The change resolves ahead of the damage; the named row must walk, must have a pool of its own, and may not name one in its turn |
| `raises` | The id of the row a body of this one puts on the corridor beside itself, or `none`. It arrives at the raiser's own distance, in the next lane, on a full pool, and behind everything already standing — so it loses every target-selection tie. The named row must walk, must have a pool, may not raise in its turn, and no row a body `becomes` may raise |
| `raisePeriod` | Ticks between one raise and the next, counted from the tick the body arrived, so the first comes a whole period in. 0 on a row that raises nothing, and a row that raises may not carry 0. **There is no cap on the total** — the board is what bounds a spawner |
| `bounty` | Gold paid, mid-match and into the one purse, to the defender that kills a body of this row. 0 = none. It may not exceed the row's `cost`, and a body that leaks pays nothing |

**What that authors.** A slow is a bubble of radius 0, origin `target`, payload `speed`, negative magnitude,
positive duration. Blessing is the same mechanic with a period and origin `self`. A mortar is origin `target`
with a real radius and payload `damage`. The Necromancer's ward is origin `self`, affects `friend`, payload
`shield`, measured in hex distance rather than along the column, so it reaches the neighbouring leg of a fold.
A tower that pulses over the whole board is one row.

**Effects are one model**: a stat, a magnitude and a duration, strongest-wins with the timer refreshed, and a
creep never drops below **10% of its authored speed**
([ADR-0056](adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md)). A new unit is a row; a new column
is a format version. The upgrade edge is [`content/upgrades.txt`](../content/upgrades.txt), not a column
([ADR-0043](adr/0043-a-tier-is-its-own-id-and-its-own-row.md) through
[ADR-0046](adr/0046-an-absent-ladder-folds-nothing.md)), and the purse that buys the defense is
[ADR-0048](adr/0048-a-board-is-not-a-layout.md).

## Open questions

Nothing on this page is open beyond a row's own `Open` line. A question bigger than a row goes to
[open questions](open-questions.md); the three the drawn shapes left — the burst two splashes share, what
"glow" meant for the Blessing, and whether a board-wide hold reads with no mark — are
[there](open-questions.md#what-the-drawn-shapes-left-open).
