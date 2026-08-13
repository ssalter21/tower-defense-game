# What is actually inside The Complete KayKit Collection v6.1

**The question:** the bundle is on the machine now. What does it *actually* contain — pack by pack, model by
model, clip by clip — so that art decisions are made against the files rather than against a publisher's
listing?

**The answer: 22 packs, 28,221 files, 606 MB, all CC0.** Inside that: **61 rigged character models**, **159
animation clips split across two incompatible rigs**, **2,252 distinct prop and environment models**, and a
hex-tile kit whose road pieces are already a tower-defense path vocabulary.

> **This note was read from the archive, not from a web page.** Every count, name, triangle figure and texture
> dimension below was extracted from `The Complete KayKit Collection v6.1.zip` itself — the file listing from
> the zip central directory, the animation and mesh data from the glTF JSON chunks. Where it disagrees with
> [The character roster](kaykit-character-roster.md) — compiled from itch.io listings before the bundle was
> downloaded — **this note is the one that is true.** The disagreements are listed in
> [§9](#9-what-this-corrects).

The full model-name listing is the companion file: **[The KayKit model index](kaykit-model-index.md)**.

## 1. Where it is, and what it costs to keep

The archive sits at `~/Downloads/The Complete KayKit Collection v6.1.zip` — 635,630,650 bytes, 28,903 zip
entries, 28,221 of them files. **It is not in this repository and should not go in whole.** The tripwire in
`tools/check-file-sizes.ps1` caps a tracked file at 5 MB, and the reason it exists — no large-file storage,
ever — applies with more force to 606 MB of art whose four redundant export formats are 90% of the weight.

The import pattern already in `client/Assets/Art/` is the right one: pull the individual files a scene needs,
commit those, leave the rest in the zip.

## 2. Licence

`License.txt` at the archive root, dated 01/07/2026:

> License: (Creative Commons Zero, CC0) — This content is free to use in personal, educational and commercial
> projects. […] If you wish to credit me you can do so by crediting "Kay Lousberg, www.kaylousberg.com" (this
> is not mandatory)

**CC0, commercial use included, attribution optional.** There is no per-pack licence file and no pack carries
a different term — one licence covers all 22. `Changelog.txt` dates the bundle build to 16/07/2026.

## 3. The shape of every pack

Every pack ships the same set of exports, which is why the file count is four times the model count:

| Folder | What it is | Use it? |
|---|---|---|
| `Assets/fbx/` | FBX, publisher's default axis convention | No |
| `Assets/fbx(unity)/` | FBX, pre-rotated for Unity. **20 of 22 packs ship this** | **Yes — this is the one** |
| `Assets/gltf/` | `.gltf` + sidecar `.bin` per model | Only for tooling |
| `Assets/obj/` | `.obj` + `.mtl`, static geometry only | No |
| `Textures/` | The shared atlas, plus recolours | Yes |
| `SOURCE/` | `.blend` originals — 130 of them | For editing, not import |
| `Samples/` | Publisher renders, `contents.png` | Reference only |

Rigged characters break the pattern: they ship **FBX and `.glb` only** — no OBJ, because OBJ cannot carry a
skeleton. Furniture Bits and Character Animations are the two packs with no `fbx(unity)` folder.

Format totals across the bundle: 8,756 `.fbx`, 4,659 `.obj`, 4,324 `.gltf`, 109 `.glb`, 130 `.blend`,
1,216 `.png`.

## 4. Two rigs, not one — and this is the finding that matters

The animation library is **not** one shared skeleton. It is **`Rig_Medium` and `Rig_Large`**, and they do not
carry the same clips:

| Clip group | Rig_Medium | Rig_Large |
|---|---:|---:|
| CombatMelee | 21 | 15 |
| **CombatRanged** | **19** | **— none** |
| General | 14 | 5 |
| MovementAdvanced | 12 | 4 |
| MovementBasic | 10 | 2 |
| Simulation | 13 | 1 |
| Special | 14 | 1 |
| **Tools** | **28** | **— none** |
| **Total** | **131** | **28** |

*(Each FBX also contains a `T-Pose` stack, excluded above. 14 animation files, 173 stacks, 159 real clips.)*

**Rig_Large characters cannot shoot, aim, reload, cast or use a tool out of the box.** Its entire vocabulary is
melee, two idles, a hit, a death, four dodges, a walk, a run, `Flexing`, and
`EXPERIMENTAL_Large_Transform`. Nine of the 61 characters are on that rig — including four of the most
tower-shaped models in the bundle:

**On `Rig_Large`:** `Barbarian_Large`, `Skeleton_Golem`, `BlackKnight`, `FrostGolem`, `Clanker`, `OrcBrute`,
`Monstrosity`, `4GTN`, `4GTN_Forgotten`. Everything else is `Rig_Medium`.

This lands directly on the tower roster. The evil-mirror decision — Skeletons plus The Black Knight as towers —
puts **Black Knight and Skeleton Golem on the rig with no ranged clips**. A melee tower is fine; a Black Knight
that is meant to throw or cast is a retarget job, not a clip swap. Worth settling before the roster is drafted.

The pack also ships `Mannequin_Medium` and `Mannequin_Large` — untextured rigs for previewing clips without
committing to a character.

## 5. The animation clips, by name

These are the exact stack names, which are what Unity imports as clip names.

**Rig_Medium — General (14):** `Death_A`, `Death_A_Pose`, `Death_B`, `Death_B_Pose`, `Hit_A`, `Hit_B`,
`Idle_A`, `Idle_B`, `Interact`, `PickUp`, `Spawn_Air`, `Spawn_Ground`, `Throw`, `Use_Item`

**Rig_Medium — MovementBasic (10):** `Jump_Full_Long`, `Jump_Full_Short`, `Jump_Idle`, `Jump_Land`,
`Jump_Start`, `Running_A`, `Running_B`, `Walking_A`, `Walking_B`, `Walking_C`

**Rig_Medium — MovementAdvanced (12):** `Crawling`, `Crouching`, `Dodge_Backward`, `Dodge_Forward`,
`Dodge_Left`, `Dodge_Right`, `Running_HoldingBow`, `Running_HoldingRifle`, `Running_Strafe_Left`,
`Running_Strafe_Right`, `Sneaking`, `Walking_Backwards`

**Rig_Medium — CombatMelee (21):** `Melee_1H_Attack_Chop`, `Melee_1H_Attack_Jump_Chop`,
`Melee_1H_Attack_Slice_Diagonal`, `Melee_1H_Attack_Slice_Horizontal`, `Melee_1H_Attack_Stab`,
`Melee_2H_Attack_Chop`, `Melee_2H_Attack_Slice`, `Melee_2H_Attack_Spin`, `Melee_2H_Attack_Spinning`,
`Melee_2H_Attack_Stab`, `Melee_2H_Idle`, `Melee_Block`, `Melee_Block_Attack`, `Melee_Block_Hit`,
`Melee_Blocking`, `Melee_Dualwield_Attack_Chop`, `Melee_Dualwield_Attack_Slice`,
`Melee_Dualwield_Attack_Stab`, `Melee_Unarmed_Attack_Kick`, `Melee_Unarmed_Attack_Punch_A`,
`Melee_Unarmed_Idle`

**Rig_Medium — CombatRanged (19):** `Ranged_1H_Aiming`, `Ranged_1H_Reload`, `Ranged_1H_Shoot`,
`Ranged_1H_Shooting`, `Ranged_2H_Aiming`, `Ranged_2H_Reload`, `Ranged_2H_Shoot`, `Ranged_2H_Shooting`,
`Ranged_Bow_Aiming_Idle`, `Ranged_Bow_Draw`, `Ranged_Bow_Draw_Up`, `Ranged_Bow_Idle`, `Ranged_Bow_Release`,
`Ranged_Bow_Release_Up`, `Ranged_Magic_Raise`, `Ranged_Magic_Shoot`, `Ranged_Magic_Spellcasting`,
`Ranged_Magic_Spellcasting_Long`, `Ranged_Magic_Summon`

The `_Up` bow variants matter for a tower on a raised tile firing down a lane, and `Ranged_Magic_Summon` is a
summoner creep's animation already authored.

**Rig_Medium — Special (14):** `EXPERIMENTAL_Medium_Transform`, `Skeletons_Awaken_Floor`,
`Skeletons_Awaken_Floor_Long`, `Skeletons_Awaken_Standing`, `Skeletons_Death`, `Skeletons_Death_Pose`,
`Skeletons_Death_Resurrect`, `Skeletons_Idle`, `Skeletons_Inactive_Floor_Pose`,
`Skeletons_Inactive_Standing_Pose`, `Skeletons_Spawn_Ground`, `Skeletons_Taunt`, `Skeletons_Taunt_Longer`,
`Skeletons_Walking`

A skeleton tower that rises from the ground when built, stands inert until a wave arrives, and collapses when
sold is four clips that already exist.

**Rig_Medium — Simulation (13):** `Cheering`, `Lie_Down`, `Lie_Idle`, `Lie_StandUp`, `Push_Ups`,
`Sit_Chair_Down`, `Sit_Chair_Idle`, `Sit_Chair_StandUp`, `Sit_Floor_Down`, `Sit_Floor_Idle`,
`Sit_Floor_StandUp`, `Sit_Ups`, `Waving`

**Rig_Medium — Tools (28):** `Chop`, `Chopping`, `Dig`, `Digging`, `Fishing_Bite`, `Fishing_Cast`,
`Fishing_Catch`, `Fishing_Idle`, `Fishing_Reeling`, `Fishing_Struggling`, `Fishing_Tug`, `Hammer`, `Hammering`,
`Holding_A`, `Holding_B`, `Holding_C`, `Lockpick`, `Lockpicking`, `Pickaxe`, `Pickaxing`, `Saw`, `Sawing`,
`Work_A`, `Work_B`, `Work_C`, `Working_A`, `Working_B`, `Working_C`

**Rig_Large — all 28:** `Melee_1H_Slash`, `Melee_1H_Stab`, `Melee_2H_Attack`, `Melee_2H_Idle`,
`Melee_2H_Slam`, `Melee_Block`, `Melee_Block_Attack`, `Melee_Block_Hit`, `Melee_Blocking`,
`Melee_Dualwield_Slash`, `Melee_Dualwield_SlashCombo`, `Melee_Unarmed_Idle`, `Melee_Unarmed_Kick`,
`Melee_Unarmed_Punch`, `Melee_Unarmed_Smash`, `Death_A`, `Death_A_Pose`, `Hit_A`, `Idle_A`, `Idle_B`,
`Dodge_Backwards`, `Dodge_Forward`, `Dodge_Left`, `Dodge_Right`, `Running_A`, `Walking_A`, `Flexing`,
`EXPERIMENTAL_Large_Transform`

Note the naming drift between rigs — `Melee_1H_Slash` against `Melee_1H_Attack_Slice_Horizontal`,
`Dodge_Backwards` against `Dodge_Backward`. Any table mapping a sim action to a clip name has to key on the rig
as well as the action.

## 6. The 61 characters, with rig and triangle count

Triangles are from the `.glb` index accessors and include whatever accessories that file bundles, so treat them
as an upper bound for the bare model. Names are the file stems — what the FBX will be called on import.

### Adventurers 2.0 — 9

| Model | Rig | Tris |
|---|---|---:|
| Knight | Medium | 5,800 |
| Mage | Medium | 6,668 |
| Barbarian | Medium | 7,123 |
| Rogue_Hooded | Medium | 7,185 |
| Engineer | Medium | 7,500 |
| Rogue | Medium | 7,562 |
| Druid | Medium | 7,784 |
| Ranger | Medium | 8,900 |
| Barbarian_Large | **Large** | 11,677 |

Nine, not eight — `Rogue` and `Rogue_Hooded` are separate models, not a texture variant. Each of the seven
base characters ships four textures (`_texture`, `_alt_A`, `_alt_B`, `_alt_C`), so the pack is **28 distinct
looks for 9 meshes**. That is a tower tier line for free.

### Skeletons 1.1 — 6

| Model | Rig | Tris |
|---|---|---:|
| Skeleton_Mage | Medium | 4,588 |
| Skeleton_Rogue | Medium | 5,278 |
| Skeleton_Minion | Medium | 5,288 |
| Skeleton_Golem | **Large** | 5,822 |
| Skeleton_Warrior | Medium | 5,934 |
| Necromancer | Medium | 6,032 |

Two atlases only — `skeleton_texture_A` and `_B`. The pack is the cheapest in the bundle to swarm: the whole
six-model roster averages 5.5k triangles, and `Skeleton_Golem` is *smaller* than `Barbarian_Large`.

### Mystery Monthly Series 4 (1.1) — 18

Twelve monthly drops, some shipping variant pairs. Folders are named `N - Month Year - Subject`.

| Model | Rig | Tris | Drop |
|---|---|---:|---|
| Werewolf_Man | Medium | 3,408 | 4 – Oct 2023 |
| Driver | Medium | 3,568 | 2 – Aug 2023 |
| Werewolf_Wolf | Medium | 3,684 | 4 – Oct 2023 |
| Monster | Medium | 3,776 | 3 – Sep 2023 |
| Animatronic_Normal | Medium | 3,834 | 5 – Nov 2023 |
| MonsterCostume | Medium | 4,245 | 3 – Sep 2023 |
| SpaceRanger | Medium | 4,292 | 7 – Jan 2024 |
| Animatronic_Creepy | Medium | 4,574 | 5 – Nov 2023 |
| Survivalist | Medium | 5,106 | 9 – Mar 2024 |
| Paladin | Medium | 5,152 | 10 – Apr 2024 |
| Clown | Medium | 5,190 | 11 – May 2024 |
| Robot_Two | Medium | 5,282 | 12 – Jun 2024 |
| Ninja | Medium | 5,403 | 8 – Feb 2024 |
| ActionFigure | Medium | 5,416 | 6 – Dec 2023 |
| Paladin_with_Helmet | Medium | 5,658 | 10 – Apr 2024 |
| Robot_One | Medium | 5,718 | 12 – Jun 2024 |
| OrcRaider | Medium | 5,849 | 1 – Jul 2023 |
| SpaceRanger_FlightMode | Medium | 6,554 | 7 – Jan 2024 |

`Monster` and `MonsterCostume` are two separate meshes — the suit and what is inside it. The Driver drop also
ships a drivable `car` (4,737 tris) and a `roofrack_empty`, which are props, not characters.

### Mystery Monthly Series 5 (1.1) — 14

| Model | Rig | Tris | Drop |
|---|---|---:|---|
| Caveman | Medium | 4,414 | 8 – Feb 2025 |
| Superhero | Medium | 4,538 | 2 – Aug 2024 |
| Helper_B | Medium | 5,174 | 6 – Dec 2024 |
| Helper_A | Medium | 5,396 | 6 – Dec 2024 |
| Vampire | Medium | 6,004 | 4 – Oct 2024 |
| Witch | Medium | 6,210 | 5 – Nov 2024 |
| CombatMech | Medium | 7,688 | 1 – Jul 2024 |
| Protagonist_B | Medium | 7,842 | 10 – Apr 2025 |
| BlackKnight | **Large** | 8,696 | 3 – Sep 2024 |
| Protagonist_A | Medium | 8,750 | 10 – Apr 2025 |
| Tiefling | Medium | 9,040 | 12 – Jun 2025 |
| FrostGolem | **Large** | 9,214 | 7 – Jan 2025 |
| Hiker | Medium | 10,407 | 11 – May 2025 |
| Clanker | **Large** | 17,050 | 9 – Mar 2025 |

`FrostGolem` is the one character with a **256×256** texture rather than 1024². `Clanker` carries a 529 KB
atlas, twenty times the bundle norm, and is the second-heaviest character here.

### Mystery Monthly Series 6 (1.1) — 14

| Model | Rig | Tris | Drop |
|---|---|---:|---|
| Farmer_A | Medium | 5,758 | 12 – Jun 2026 |
| PlantWarrior | Medium | 6,138 | 5 – Nov 2025 |
| AvianSwordsman | Medium | 6,394 | 9 – Mar 2026 |
| Farmer_B | Medium | 6,874 | 12 – Jun 2026 |
| Marksman | Medium | 7,655 | 10 – Apr 2026 |
| ToySoldier | Medium | 7,850 | 6 – Dec 2025 |
| Cleric | Medium | 8,217 | 3 – Sep 2025 |
| MagicalGirl | Medium | 8,548 | 11 – May 2026 |
| Lorekeeper | Medium | 10,738 | 1 – Jul 2025 |
| Monstrosity | **Large** | 10,898 | 4 – Oct 2025 |
| OrcBrute | **Large** | 12,314 | 2 – Aug 2025 |
| 4GTN | **Large** | 15,476 | 7 – Jan 2026 |
| 4GTN_Forgotten | **Large** | 19,488 | 7 – Jan 2026 |
| Hoarder | Medium | 20,450 | 8 – Feb 2026 |

**`Hoarder` is the heaviest character in the bundle at 20,450 triangles** — 3.5× the Knight — because the pack
it carries is modelled in full. `MagicalGirl` is **one mesh with four atlases** (`_A` … `_D`), so its four
"colourways" cost one model.

Series 6 is where the budget stops being free: five of its fourteen models are over 10k triangles, against
zero in Skeletons and one in Adventurers.

### Also rigged

`Mannequin_Medium` (6,916, Medium) and `Mannequin_Large` (9,148, Large) in Character Animations; `Dummy`
(3,760, Medium) in Prototype Bits.

## 7. Textures

The house style holds: **1024×1024 PNG, one atlas per pack or per character**, no normal maps, no roughness
maps, no second UV set. Verified by reading PNG headers across Forest Nature, Medieval Hexagon, Dungeon,
Adventurers, Skeletons, Platformer, City Builder, Black Knight and Clanker — all 1024².

Two exceptions found: `frostgolem_texture.png` is 256×256, and the Marksman's `marksman_foliage_texture.png` is
1024×335. Board Game Bits is the outlier pack — it ships 60+ card-face and badge textures at up to 370 KB,
because playing cards need readable faces.

Recolours ship as extra atlases against the same UVs, which is what makes the atlas swap free:

| Pack | Recolours |
|---|---|
| Adventurers | `_alt_A/B/C` per character — 7 characters × 4 |
| Skeletons | `_A`, `_B` |
| Dungeon | `_Golden`, `_BlackAndWhite`, `_SepiaA/B`, `_NightA/B` |
| Medieval Hexagon | `_Fall`, `_Summer`, `_Winter` |
| Furniture, Prototype | `_alt_A/B/C` |
| Space Base, Robot, Combat Mech, Superhero, Black Knight, Clanker | `_alt` |

## 8. The packs, and what each is for here

| Pack | Distinct models | Read for this project |
|---|---:|---|
| **Medieval Hexagon 1.0.1** | 236 | **The map kit.** See below |
| **Dungeon 1.1** | 262 | Interiors, doors, traps, chests, stairs; six recolour atlases |
| **Forest Nature 1.0** | 202 | Trees, bushes, rocks, logs — **each in 8 colourways**, so 1,588 files |
| **Platformer 1.0** | 171 | Blocks, arches, barriers in 4 team colours + 53 neutral |
| **City Builder Bits** | 73 | Modern buildings; the Monster Costume drop reuses its atlas |
| **Prototype Bits 1.1** | 85 | Greybox shapes, plus the rigged `Dummy` |
| **Fantasy Weapons Bits** | 48 | Standalone weapons for hand slots |
| **RPG Tools Bits** | 69 | Maps, blueprints, tabletop kit |
| **Resource Bits** | 132 | Ore, wood, crops — economy iconography |
| **Block Bits** | 49 | Primitive blocks |
| **Board Game Bits** | 165 | Meeples, dice, cards; badge art of the Adventurer and Skeleton rosters |
| **Furniture Bits** | 74 | Interior dressing |
| **Restaurant Bits** | 223 | Food and kitchen |
| **Halloween Bits** | 101 | Pumpkins, graves, bones |
| **Holiday Bits** | 102 | Winter and gifts |
| **Space Base Bits** | 69 | Sci-fi modules |
| Adventurers, Skeletons, Series 4/5/6 | 190 props | Weapons and accessories beside the characters |
| Character Animations 1.1 | — | The clip library and two mannequins |

Every name is in [the model index](kaykit-model-index.md).

### The Medieval Hexagon pack is the interesting one

It is organised the way a strategy map wants to be:

- **`tiles/base` (6)** — `hex_grass`, `hex_grass_bottom`, `hex_grass_sloped_high`, `hex_grass_sloped_low`,
  `hex_transition`, `hex_water`
- **`tiles/roads` (15)** — `hex_road_A` … `hex_road_M`, plus `hex_road_A_sloped_high` / `_low`
- **`tiles/rivers` (15 + 15 waterless)** — `hex_river_A` … `_L`, `_A_curvy`, two crossings
- **`tiles/coast` (5 + 5 waterless)** — `hex_coast_A` … `_E`
- **`buildings`** — 21 neutral + 27 in each of blue/red/green/yellow
- **`units`** — 25 neutral + 28 in each of four colours, including `bow_`, `cannon_`, `banner_`, each in an
  `_accent` and a `_full` colouring
- **`decoration`** — 42 nature, 35 props
- A `Medieval_Hexagon_UserGuide_v1.pdf` explaining the tile system

**The 13 road pieces A–M are a complete hex path vocabulary, and they average 141 triangles.** The whole tile
set — all 61 tiles including rivers and coasts — spans 20 to 384 triangles. A generated map is essentially free
to render at this cost. The pack's `building_tower_A_blue` is already imported into
`client/Assets/Art/Buildings/`.

Whole-pack triangle spread: Hexagon 16–5,659 (avg 1,006, pulled up by buildings); Dungeon 16–3,499 (avg 451).

## 9. What this corrects

[The character roster](kaykit-character-roster.md) was written against itch.io listings before the bundle
existed on disk, and said so. Six of its
claims do not survive contact with the files:

1. **"All 57 share one rig."** There are two rigs, and `Rig_Large` has no ranged or tool clips at all. This is
   the correction that changes a design decision rather than a number.
2. **"57 KayKit characters."** There are **61** rigged character models — 9 + 6 + 18 + 14 + 14 — plus two
   mannequins and a prototype dummy.
3. **"Adventurers — 8."** Nine. `Rogue` and `Rogue_Hooded` are separate meshes.
4. **"133 humanoid clips."** 159 real clips across the two rigs (131 Medium + 28 Large), or 173 animation
   stacks if the per-file `T-Pose` is counted.
5. **"The Magical Girls — one body, four hair and dress colourways"** and **"The Ninja — four colourways"**:
   both are one mesh with multiple atlases, which is *better* than four models, but it is a texture swap and
   not four prefabs.
6. **"Two KayKit characters — The Monster Costume and The Cleric — have no published render."** Both models are
   present and importable; `MonsterCostume` and `Monster` are in fact two meshes.

What the roster note got right and is worth keeping: the Adventurers/Skeletons mirror, the transformation pairs
(`Werewolf_Man`/`Werewolf_Wolf`, `Animatronic_Normal`/`Animatronic_Creepy`, `4GTN`/`4GTN_Forgotten`), and the
observation that KayKit has almost no fliers. That last one is **not** re-verified here — nothing in the file
listing says whether a model reads as airborne, and no model was opened and looked at. It stands as the roster
note left it: read from renders, and to be confirmed by eye.

The creep source remains unchosen. Nothing in this note decides it.

## 10. Already imported

`client/Assets/Art/` currently holds, from this bundle:

- `Characters/Ranger.fbx`, `Characters/Skeleton_Warrior.fbx` with `ranger_texture.png`, `skeleton_texture.png`
- `Weapons/bow_withString.fbx`
- `Buildings/building_tower_A_blue.fbx` with `hexagons_medieval.png`
- `Animations/Rig_Medium_General.fbx`, `Rig_Medium_MovementBasic.fbx`, `Rig_Medium_CombatRanged.fbx`

All four `Rig_Medium` files present are the right rig for both imported characters. Nothing on `Rig_Large` has
been brought in.

## Sources

- `The Complete KayKit Collection v6.1.zip` — `License.txt`, `Changelog.txt`, the zip central directory, and
  the glTF JSON chunks of all 109 `.glb` and 687 map-pack `.gltf` files
- [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — the bundle's listing page
- [Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) — the standalone clip library
