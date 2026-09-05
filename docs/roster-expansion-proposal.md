# Roster expansion proposal — 5 September 2026

> ## Status: signed on 5 September 2026, with five changes
>
> **Sam reviewed this page in full and took it.** All 23 tower names, all 12 creep names and the id 7 relabel
> are kept as written; every model is confirmed from a rendered sheet of all 32 candidates. All five reversals
> in the table below are taken.
>
> **[`roster.md`](roster.md) is now the signed record** — every row on this page is there with `status:
> signed`, its model and props on its `Looks` line, and its numbers on its `Numbers` line. The reasoning for
> each reversal is in
> [the decision log](decision-log.md#5-september-2026-last--sam-signs-the-roster-and-six-standing-proposals-move).
> **This page is kept as the argument, not as the record.** Where the two disagree, `roster.md` wins.
>
> **The five things the review changed:**
>
> | On this page | What was signed |
> |---|---|
> | A capstone is bought with a **gate token** | A capstone token, granted at rounds 3, 6 and 9 — but **as a plain currency, not a gate**. No capacity schedule comes back with it |
> | Tier signal: **texture at 2, model at 3, size where the pack ships neither** | **Size is retired entirely**, including the live Ranger's 1.5. A rung is colour, a prop, or a second model — never size |
> | The Mage's splash is authored **and the 92 is kept** | The splash is authored; **the price is deferred** until the automated balance sweeps can derive it, rather than kept on the old reasoning |
> | The Druid's tier 3 is the **`PlantWarrior`** model | **Set aside.** It read as a different creature rather than the same person promoted. The Druid keeps his body and gains a **bare weirwood** beside him — `Tree_Bare_*_Color8`, Forest Nature pack |
> | The Grave Robber wears **`Hoarder_Backpack`**; the Marksman *may* hold a crossbow | Backpack confirmed and **the sword dropped**; the **crossbow is signed** over the rifle |
>
> **The three engine asks were answered too:** the Villager transforms on the **first damage taken** and a
> lethal first hit produces a full-health Werewolf rather than a corpse; the Necromancer raises every **150
> ticks with no cap**; the Grave Robber pays **12**. The Cleric's capstone is **Consecration**, with Zeal kept
> as its named successor and the payload keyword's naming deliberately deferred.

**Written as a proposal.** Below this line the page is unchanged from the version Sam reviewed: every name a
placeholder, every model a candidate, every number a suggestion. It is left that way on purpose, so the
argument that was actually put is legible beside the decision that came out of it.

**What it answers.** The build changes direction: the game is to be fun to play *now*, which means breadth —
as many towers and creeps as the collection supports. Every one of the 61 rigged KayKit characters is
accounted for below: 31 are assigned to a unit and 30 are set aside with a reason. Every tower line has three
stages. **The second stage is one stat.** **The third stage is a capstone that changes how the tower works**,
and each capstone is drawn from what its model is holding or wearing.

**The result: 9 tower lines (27 rows) and 17 creeps**, against 3 lines and 5 creeps today.

## What it keeps, and what it moves

Kept, because it is signed or load-bearing:

- **Skeletons are creeps, Adventurers are towers.** Extended, not replaced: the Mystery Monthly characters that
  read as heroes join the tower side and the ones that read as undead or dark and hooded join the creeps.
- **One attack type per line**, so what a line does to a body is read off the line.
- **Creeps never attack.** Every creep mechanic below is an aura or a pool, never damage.
- **A tier is its own id and row**, joined by an edge in `content/upgrades.txt`; ids ascend and are never reused.
- **The Minion's 28 and the Scout's 56.** No proposed speed touches them.
- **A capstone is bought with a gate token, not gold.** Nine capstones against three tokens a run is what
  makes the token a decision.

Moved, and each of these is a decision for Sam rather than a thing this page does:

| Was | Proposed | Why |
|---|---|---|
| Captain (tier 2, attack-speed aura), Hero (tier 3, 360° sweep) | Retired as proposals. The aura moves to the **Paladin** capstone; the sweep to the **Barbarian** capstone | A tier 2 is one stat now, and both mechanics fit a model that ships the prop for them |
| Pyromancer / Cryomancer branch, Frostfire Archmage | Retired as proposals. The area slow moves to the **Druid** capstone | Three stages, no branch. The branch made the tier-2 pick temporary, which the roster already flagged |
| Mage splash: designed, unauthored, priced at 92 for 30 of damage | **Author it** — bubble on target, radius 1000, magic — and keep the 92 | Of the three ways out in `open-questions.md` this is the one that makes the Mage fun. It accepts an unpriced radius, which the cost rule already accepts for every bubble |
| id 7 `necromancer` wears `Skeleton_Mage`; the dedicated Necromancer model is unused | id 7 is relabelled **Skeleton Mage** and gets a haste aura; a new row **Necromancer** wears the Necromancer model and gets the shield aura already designed for id 7 | Relabelling moves no hash. The pack's own necromancer should be the necromancer |
| Size is the only tier signal | Size stays for lines with no second model; **a model or texture swap is the signal where the pack ships one** | Six lines have a second model in the collection. Scaling a Paladin 1.5× when the pack ships a helmeted Paladin wastes the pack |

## What is authorable today

Layout 3 plays every shape below marked **now**. Three shapes need engine work and are marked; they are
proposed anyway because they are the right capstone for the model, with an authorable fallback beside each.

| Tag | Meaning |
|---|---|
| **now** | A row in `units.txt` and an edge in `upgrades.txt`. No code |
| **keyword** | Needs the sixth `bubblePayload` value `open-questions.md` names — a damage *modifier* as distinct from the roll a damage bubble spreads. No format version; Sam names the word |
| **engine** | A new mechanic: a creep changing into another row, a creep spawning creeps, gold paid on a kill. Each is a ticket |

---

# Towers

Nine lines. Impact ×3 (Knight, Barbarian, Engineer), pierce ×2 (Archer, Rogue), magic ×4 (Mage, Druid,
Cleric, Paladin). Magic is over-represented on purpose: the creep side is undead and mostly armoured, and magic
is what beats armoured. The creep table balances it back by adding swift and arcane bodies.

Every Rig_Large model proposed (Barbarian_Large only, on this side) is used for melee, which is all that rig
can do. No proposed tower on the Large rig shoots or casts.

Numbers are at the ×10 scale, durations in ticks at 30 a second, and the cost is what the rule prices — one
gold per five damage a second, times bodies hit. `_` is undecided.

## Knight line — impact, melee

Model `Knight`, already imported. Sword and shield from the Adventurers pack.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | **Soldier** (live, id 11) | One hex, fast, one target | Visor down, `sword_1handed` | As live: range 1000, cooldown 15, 60–90, cost 30 | — |
| 2 | Sergeant | Swings faster | `knight_texture_alt_A`, `shield_square` | Cooldown 15 → **11**. Cost ~41 | now |
| 3 | **Shield Wall** | Every creep touching him walks at half speed while it is touching him. He keeps swinging | Shield raised (`Melee_Blocking`), visor closed | Aura: origin self, radius 1000, affects enemy, payload speed, magnitude −50, period 15, duration 20 | now |

**Why it is a capstone.** It is the one slow in the game that costs no range: purely positional, and it bunches
bodies under whatever stands beside him. On a fold, that is the Barbarian.

## Barbarian line — impact, melee, slow and heavy

Models `Barbarian` then `Barbarian_Large`. The pack ships the size-up as a second model with the Large rig.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Barbarian | One hex, slow, heavy, one target | `axe_2handed`, `Melee_2H_Attack_Chop` | Range 1000, cooldown 45, 200–300, windup 20, backswing 12. Cost ~33 | now |
| 2 | Berserker | Hits harder | `axe_2handed_Large`, `barbarian_texture_alt_A` | 200–300 → **300–450**. Cost ~50 | now |
| 3 | **Slam** | Every swing hits everything touching him | `Barbarian_Large` model, `Melee_2H_Slam` (a Large-rig clip) | Bubble: origin self, radius 1000, payload damage. Same roll, every body | now |

## Paladin line — magic, melee

Models `Paladin` then `Paladin_with_Helmet`. Ships `paladin_hammer`, `paladin_shield`, `paladin_book` and a
gold `paladin_statue` of himself.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Paladin | One hex, holy damage, one target | Bare head, `paladin_hammer` | Range 1000, cooldown 24, 120–180. Cost ~37 | now |
| 2 | Templar | Hits harder | `Paladin_with_Helmet` model, `paladin_shield` | 120–180 → **180–270**. Cost ~56 | now |
| 3 | **Blessing** | Every tower within two hexes fires a quarter faster, always | `paladin_book` open; the `paladin_statue` stands beside him | Aura: origin self, radius 2000, affects friend, payload cooldown, magnitude −25, period 30, duration 30 | now |

Two Blessings over one tower do not stack; the timer refreshes. That is the rule the effect model already has,
and it is what stops a ring of Paladins running away.

## Cleric line — magic, ranged

Model `Cleric`. Ships `Cleric_Mace`, `Cleric_Tome`, `Cleric_Shield` and a `Cleric_Font`.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Cleric | Three hexes, holy bolt, one target | `Cleric_Tome`, `Ranged_Magic_Shoot` | Range 3200, cooldown 30, 130–190, hitscan. Cost ~32 | now |
| 2 | Bishop | Reaches further | Alt texture; `Cleric_Mace` | Range 3200 → **4200**. Cost 32 (range is unpriced) | now |
| 3 | **Consecration** | Every undead within three hexes loses a third of its armour while it is there | The `Cleric_Font` beside him, light on the ground | Aura: origin self, radius 3000, affects enemy, payload armour, magnitude −30, period 30, duration 30 | now |
| 3, later | Zeal | Every tower within two hexes deals more damage | Same | Aura, payload *damage-modifier*, magnitude +_ | **keyword** |

Consecration is the capstone proposed now. Zeal is the better holy aura and is written down so it is not
re-invented once the payload keyword exists; Sam picks one.

## Mage line — magic, projectile, splash

Models `Mage` (imported) then `Lorekeeper`. The Lorekeeper ships `Lorekeeper_Staff` and `Lorekeeper_Tome`.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | **Mage** (live, id 4) | Bolt with splash of one hex | `spellbook_open` in hand, not the bow | As live, **plus** bubble origin target, radius 1000, payload damage. Cost stays 92 | now — and a decision, see above |
| 2 | Sorcerer | Casts faster | `mage_texture_alt_A`, `staff` | Cooldown 54 → **40**. Cost ~124 | now |
| 3 | **Unravel** | His bolt strips most of the armour off what it hits, for five seconds. The splash stays | `Lorekeeper` model, `Lorekeeper_Tome` open | Bubble on target: radius 1000, payload **armour**, magnitude −60, duration 150 | now, but see the note |

**The note.** One row carries one bubble, so Unravel's bubble replaces the splash's: the roll lands on one
body and the armour strip lands on the hex around it. If the splash must survive to the capstone, Unravel needs
a second bubble column, which is a format version. Proposed as written: the capstone trades the splash for the
strip, and that is the choice the token buys.

## Druid line — magic, ranged

Models `Druid` then `PlantWarrior`. The Plant Warrior ships a leaf bow, a spear and a lily-pad shield.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Druid | Three and a half hexes, nature bolt, one target | `druid_staff`, `Ranged_Magic_Shoot` | Range 3600, cooldown 36, 150–210, hitscan. Cost ~30 | now |
| 2 | Elder | Reaches further | `druid_texture_alt_A` | Range 3600 → **4600**. Cost 30 | now |
| 3 | **Overgrowth** | The whole board slows a fifth while he stands. Every board. Roots on every hex | `PlantWarrior` model, `PlantWarrior_Bow` | Aura: origin self, radius 60000, affects enemy, payload speed, magnitude −20, period 30, duration 30 | now |

A whole-board pulse is one row — the roster has said so since layout 3, and nobody has built one. The Druid is
where it belongs: a slow you feel everywhere and see nowhere, until the roots are drawn.

## Archer line — pierce, ranged

Models `Ranger` (imported) for the first two rungs, then `Marksman`. The Marksman ships a rifle; the proposal
is that he holds `crossbow_2handed` from the Adventurers pack instead, which is Sam's call.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | **Archer** (live, id 3) | Three hexes, fast, modest | As live | As live, cost 40 | — |
| 2 | **Ranger** (live, id 14) | +1 hex | As live, 1.5 scale — or `ranger_texture_alt_A` at 1.0 once texture is a tier signal | As live, cost 40 | — |
| 3 | **Overwatch** | Sees the whole leg. Slow, enormous single shots from wherever he is stood | `Marksman` model, prone-ish `Ranged_2H_Aiming`, crossbow | Range **8000**, cooldown 60, 500–700, hitscan. Cost ~60 | now |

This is where the line's slow-and-heavy tuning lives, as the roster already said of the Marksman. Multishot
moves to the Rogue, whose model throws knives.

## Rogue line — pierce, short range, very fast

Models `Rogue` then `Rogue_Hooded`. Ships `dagger`, `smokebomb`, `crossbow_1handed`.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Rogue | Two hexes, three throws a second, light | `dagger`, `Throw` clip | Range 2200, cooldown 9, 40–60, hitscan. Cost ~33 | now |
| 2 | Cutthroat | Throws faster | `Rogue_Hooded` model | Cooldown 9 → **7**. Cost ~43 | now |
| 3 | **Fan of Knives** | Three knives a throw, at the three bodies nearest the exit | Hooded, `Melee_Dualwield_Attack_Slice` as the throw | `targets` 3. Cost ~129, since bodies are priced | now |

## Engineer line — impact, projectile, long range

Model `Engineer`. Ships `engineer_Wrench`, `turret_base`, `ammo_crate`.

| Tier | Name | Does | Looks | Numbers | Needs |
|---|---|---|---|---|---|
| 1 | Engineer | Four hexes, slow lobbed shot, one target | Wrench in hand, `turret_base` beside him firing | Range 4000, cooldown 60, 250–350, projectile, flight 45. Cost ~30 | now |
| 2 | Artificer | Reaches further | `engineer_texture_alt_A`, `ammo_crate` beside the turret | Range 4000 → **5000**. Cost 30 | now |
| 3 | **Mortar** | The shell bursts across a hex and a half | A bigger `turret_base`, lobbing arc drawn | Bubble on target: radius 1500, payload damage | now |

Two blasts on the board — the Mage's at tier 1 and this one at tier 3 — are different types at different radii,
and the impact one is the answer to arcane bodies the Mage cannot chew.

---

# Creeps

Undead, and the dark or hooded. Armour types are spread to balance the tower side: seven armoured, five swift,
five arcane. Cost is effective health over 160, so it follows from health and armour and is never authored.
Every Rig_Large model here only walks and dies, which the Large rig does.

Where a creep carries a shield, note that **the cost rule does not price a shield**, so the Vampire and the
Grave Robber are cheaper than they should be until the rule grows a term — the same known gap as radius and
range, and a sweep target rather than something to hand-correct.

| Name | Model | Armour | maxHp | speed | armourValue | shield | Mechanic | Cost | Needs |
|---|---|---|---|---|---|---|---|---|---|
| **Minion** (live, 1) | `Skeleton_Minion` | armoured | 1550 | 28 | 0 | — | The baseline body | 10 | — |
| **Skeleton** (live, 12) | `Skeleton_Minion` + `Skeleton_Blade`, `Skeleton_Shield_Small_A` | armoured | 2200 | 28 | 20 | — | The Minion with armour | 17 | — |
| **Skeleton Scout** (live, 2) | `Skeleton_Rogue` | swift | 1500 | 56 | 0 | — | Twice the Minion's speed | 9 | — |
| **Skeleton Warrior** (live, 13) | `Skeleton_Warrior` | armoured | 3400 | 18 | 45 | — | The heavy | 31 | — |
| **Skeleton Mage** (live, 7, relabelled) | `Skeleton_Mage`, `Skeleton_Staff` | arcane | 2400 | 33 | 25 | — | **Haste**: every creep within two hexes walks a fifth faster. Aura, friend, speed +20, radius 2000, period 30, duration 30 | 19 | now |
| Necromancer | `Necromancer`, `Skeleton_Scythe` | arcane | 2600 | 28 | 30 | — | **Ward**: grants creeps within two hexes a shield worth a quarter of their health, every three seconds. Aura, friend, shield 25, radius 2000, period 90, duration 0 | 21 | now |
| Necromancer, later | same | | | | | | **Raise**: spawns a Minion beside itself every n ticks | | **engine** |
| Bone Golem | `Skeleton_Golem`, `Skeleton_Golem_Axe_Large` | armoured | 9000 | 14 | 60 | — | Nothing but mass. Half the Minion's speed | 90 | now |
| Black Knight | `BlackKnight`, `BlackKnight_Sword_Large`, `_Shield_Large` | armoured | 5000 | 22 | 80 | — | The Knight's twin. Nothing but armour | 56 | now |
| Frost Wight | `FrostGolem`, `FrostGolem_Axe` | arcane | 6000 | 16 | 40 | — | **Frostbite**: towers within two hexes fire a third slower while it passes. Aura, **enemy**, cooldown +30, radius 2000, period 30, duration 30 | 53 | now |
| Abomination | `Monstrosity`, `Monstrosity_BarndoorShield_Large` | armoured | 12000 | 12 | 0 | — | The biggest body. No armour: flesh, not bone | 75 | now |
| Vampire | `Vampire`, `Vampire_Sword` | swift | 2800 | 44 | 20 | 1400 | **Blood**: a raw pool armour does not apply to, spent before health | 21 (shield unpriced) | now |
| Vampire, later | same | | | | | | Regains health on a leak, or drains | | **engine** |
| Witch | `Witch`, `Broom` | arcane | 2000 | 33 | 20 | — | **Hex Ward**: creeps within two hexes gain 30 armour. Aura, friend, armour +30, radius 2000, period 30, duration 30 | 15 | now |
| Fiend | `Tiefling`, `Tiefling_SwordsBackpack` | arcane | 3200 | 33 | 45 | — | Arcane heavy; the Warrior's counterpart on another armour type | 29 | now |
| Shade | `Ninja`, `Ninja_Katana` | swift | 1200 | 84 | 0 | — | Three times the Minion's speed. Four atlases in the pack for later variety | 8 | now |
| Cursed Villager | `Werewolf_Man`, `axe` | swift | 1800 | 28 | 0 | — | A cheap body that is the Werewolf's first form | 11 | now |
| Werewolf | `Werewolf_Wolf` | swift | 2600 | 50 | 10 | — | Fast and durable at once — the pairing `lancer` occupied with no design behind it. Now the design is the transformation | 18 | now |
| Werewolf, later | both | | | | | | The Villager **becomes** the Werewolf mid-lane — on a tick, on a hex, or on taking damage | | **engine** |
| Grave Robber | `Hoarder`, `Hoarder_Backpack` | armoured | 3000 | 22 | 30 | 2000 | The pack soaks hits: a raw pool in front of ordinary health | 24 (shield unpriced) | now |
| Grave Robber, later | same | | | | | | Pays gold to the defender that kills it | | **engine** |

**Speeds, checked against the clock.** 84 is exactly three Minions, so the Shade passes on whole ticks like the
Scout does; 14 is exactly half, so the Golem is passed on whole ticks. 22, 16, 12, 44, 50 and 33 deliberately
are not multiples, which keeps the between-ticks case populated. Nothing moves 28 or 56.

**What this fills that was deliberately absent.** The roster's table of absent shapes — fast and cheap in
numbers (the Shade), slow and dear and very tough (the Golem and the Abomination), fast and durable at once (the
Werewolf) — was blocked on models. These are the models.

---

# Every character, accounted for

The 31 assigned above, then the 30 set aside. "Set aside" means it does not read as medieval, heroic, undead
or dark; each has one line saying what it could be if the theme ever widens, so nobody has to re-derive it.

**Assigned — towers (15 models, 9 lines):** Knight, Barbarian, Barbarian_Large, Paladin, Paladin_with_Helmet,
Cleric, Mage, Lorekeeper, Druid, PlantWarrior, Ranger, Marksman, Rogue, Rogue_Hooded, Engineer.

**Assigned — creeps (16 models, 17 rows):** Skeleton_Minion, Skeleton_Rogue, Skeleton_Warrior, Skeleton_Mage,
Necromancer, Skeleton_Golem, BlackKnight, FrostGolem, Monstrosity, Vampire, Witch, Tiefling, Ninja,
Werewolf_Man, Werewolf_Wolf, Hoarder.

| Set aside | Pack | Why, and what it could be |
|---|---|---|
| Survivalist, ActionFigure, Driver | Series 4 | Modern firearms and a hatchback. A gun-era roster, if ever |
| SpaceRanger, SpaceRanger_FlightMode | Series 4 | Sci-fi. Out with the Space Base pack |
| Clown | Series 4 | Could be a dark-carnival creep; the props are balloons and a mallet, and it reads as comedy |
| Robot_One, Robot_Two, CombatMech, Clanker, 4GTN, 4GTN_Forgotten | Series 4, 5, 6 | Machines. Clanker is the one that half-reads as animated armour; the brass and lamp eyes say steampunk |
| Animatronic_Normal, Animatronic_Creepy | Series 4 | Horror, but a modern mascot bear. The transform pair is worth remembering for the Werewolf mechanic |
| Monster, MonsterCostume | Series 4 | A person in a suit. Reads as a joke about bosses, and the joke needs a boss to land against |
| OrcRaider, OrcBrute | Series 4, 6 | A greenskin faction, whole, if the creep side ever has a second theme. Not undead |
| Superhero, Protagonist_A, Protagonist_B, Hiker, Helper_A, Helper_B | Series 5 | Modern civilians and a cape |
| Caveman | Series 5 | Prehistoric; the fire pit is a decent map prop |
| Farmer_A, Farmer_B | Series 6 | Villagers. Could stand on the board as scenery, or be what a Cursed Villager was before |
| ToySoldier, MagicalGirl | Series 6 | Toy-box and anime |
| AvianSwordsman | Series 6 | The clearest air unit in the collection, and the game has no air. If it ever does, start here |
| Mannequin_Medium, Mannequin_Large, Dummy | Animations, Prototype | Rigs and a target, not characters |

---

# What Sam decides

In the order it unblocks things.

1. **Names.** All 23 new tower names and 12 new creep names above are placeholders. Keep, strike, or replace.
   The eight live names are untouched; the one relabel is id 7.
2. **Models.** Confirm or swap each model named. The ones most worth a look: `Marksman` with a crossbow
   instead of his rifle; `Ninja` as an undead-side creep; `Hoarder` at all; the dedicated `Necromancer` taking
   over from `Skeleton_Mage`. `tools/capture-armed-roster.ps1` can render a sheet of any set for approval.
3. **The tier signal.** Texture swap at tier 2 and model swap at tier 3 where the pack has one, size where it
   does not — or size everywhere, as today. This is a view fact and costs no format version either way.
4. **The five reversals** in the table at the top: Captain/Hero, the elemental branch, the Mage splash, the
   Necromancer relabel, the tier signal. Each is a `decision-log.md` entry and an edit to `roster.md` when
   taken.
5. **Which Cleric capstone**, Consecration now or Zeal after the payload keyword — and the keyword itself.
6. **The three engine asks**, in the order they are worth building: transform (Werewolf), spawn (Necromancer),
   gold on kill (Grave Robber). Each is a ticket; none blocks a single row above from being authored.
7. **The slowed-creep view field** already in `open-questions.md`. Four proposed rows slow something, and none
   of it is visible until a `CreepSnapshot` field exists. It becomes urgent the day the first one is signed.

## What an agent can do once those are answered

Author the rows and edges, run the sweep, and bring back the leak counts. The pricing rule derives every cost;
the golden trace moves and is regenerated deliberately; `show-ladder.ps1` prints the nine ladders. The unpriced
levers — shield, radius, range — will show up in the sweep as rows that are too good for their gold, and that is
the finding to bring back rather than tune away.
