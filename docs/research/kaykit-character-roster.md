# The KayKit character roster

**The question:** what character models does [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete)
actually contain, so the tower and creep rosters can be drafted against real assets rather than against a
number?

**The answer: 57 characters across five packs**, plus a shared 133-clip animation library that every one of
them can play. This note lists all 57.

> **There is an illustrated version.** Every entry below with its render, kit list and role tag, filterable by
> pack and by tower/creep: **[the KayKit roster report](https://claude.ai/code/artifact/4b8c7b02-5397-4f73-b9fd-afaef967857c)**.
> The renders are the publisher's promotional images and are not committed to this repo.

> **Provenance, and its one limit.** The pack contents were read from the publisher's own itch.io listings and
> the CC0 GitHub mirrors in August 2026, and the descriptions below were written from those renders.
> **The paid bundle is not on this machine** — `client/Assets/Art/Characters/` holds exactly two imported FBX
> files, `Ranger.fbx` and `Skeleton_Warrior.fbx`, both from the free tiers. So the **names and renders** are the
> publisher's; the **TD readings are inferred**. Treat them as a drafting aid and confirm against the real files
> once the bundle is downloaded. Two characters — **The Monster Costume** and **The Cleric** — have no published
> render anywhere, and four more (The Survivalist, The Witch, The Clanker, the Skeleton Warrior) had to be
> cropped from pack banners or taken from the GitHub mirror.

## What the bundle contains

| Pack | Characters | Style note |
|---|---|---|
| [Adventurers](https://kaylousberg.itch.io/kaykit-adventurers) | 8 | The classic fantasy party. 25+ swappable weapons/accessories, 3 alt textures per character at the Extra tier |
| [Skeletons](https://kaylousberg.itch.io/kaykit-skeletons) | 6 | The undead counterpart. 10+ weapons; own skeleton-specific animation set |
| [Mystery Monthly Series 4](https://kaylousberg.itch.io/kaykit-series-4) | 15 | Genre-mixed — fantasy, sci-fi, modern, horror |
| [Mystery Monthly Series 5](https://kaylousberg.itch.io/kaykit-series-5) | 14 | Newer art style (characters have legs); ships `.blend` sources |
| [Mystery Monthly Series 6](https://kaylousberg.itch.io/kaykit-series-6) | 14 | Patreon monthlies, July 2025 – June 2026 |
| **Total** | **57** | |

Plus [Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) — **133 humanoid clips**,
free and standalone, retargetable across every character above.

Non-character packs in the same bundle (Dungeon, Medieval Hexagon, Forest Nature, Platformer, City Builder
Bits, Space Base Bits, Resource/Block/Halloween/Prototype/Restaurant/Furniture/Holiday/RPG Tools/Fantasy
Weapon/Board Game Bits) are environment and prop kits — no characters, but the same 1024×1024 gradient atlas,
so they composite without a seam.

## Adventurers — 8

The most tower-shaped pack: humanoid, weapon-swappable, unmistakable archetypes at a glance.

| Model | Reads as | Natural TD role |
|---|---|---|
| Knight | Plate armour, removable helmet with a closing visor | Melee/blocker tower, or a front-rank defensive unit |
| Barbarian | Bare-chested brute, heavy two-hander | Slow, heavy single-target tower |
| Rogue | Light armour, hooded variation included | Fast low-damage or crit tower; the hood variant reads as a distinct tier |
| Mage | Robe and staff | Splash/elemental tower — the obvious magic-damage silhouette |
| Ranger | Bow with an animated bowstring (blendshape) | Long-range single-target. **Already imported** |
| Engineer | Toolbelt/mechanical kit | Support, buff, or build-modifying tower |
| Druid | Nature-themed caster | Slow/root/DoT tower — visually distinct from the Mage |
| Barbarian_Large | Oversized barbarian, removable "Smackarang" helmet | Boss-tier tower, or the top upgrade of the Barbarian line |

## Skeletons — 6

The creep pack. Deliberately built as the Adventurers' mirror, which makes tower-vs-creep parity cheap.

| Model | Reads as | Natural TD role |
|---|---|---|
| The Warrior | Horned helmet, sword and shield | Standard armoured creep. **Already imported** |
| The Rogue | Red hood, light frame | Fast creep |
| The Mage | Red hat and robe | Caster creep — an aura or shield carrier |
| The Minion | Plain, unadorned skeleton | The trash creep; the one you spawn in tens |
| Skeleton Golem | Big and bulky; the publisher names it "perfect for use as a boss enemy" | Boss creep |
| Necromancer | Robed, wicked | Summoner creep — spawns Minions mid-lane |

## Mystery Monthly Series 4 — 15

| Model | Reads as | Natural TD role |
|---|---|---|
| The Orc Raider | Green, tusked, topknotted, red war paint, fur skirt. Ships a war drum, spiked club, double axe | Mid-tier armoured creep; the drum makes a war-band leader |
| The Driver | Sunglasses and a red bomber jacket, plus a complete hatchback with a roof rack | The car is the interesting half — a vehicle creep with its own armour class |
| The Monster Costume | **No published render.** A person in a monster suit | A fake boss: reads as a threat, dies like trash |
| The Werewolf | Two models: a flannel-shirted lumberjack, and the same figure wolf-headed | **Transforming creep** — one entity, two states |
| The Animatronic | Two models: a ukulele-playing bear, and the same bear torn open and glitching | Same trick — an enrage or phase-two state |
| The Action Figure | Toy-scaled commando, headband and dog tag, four swappable face plates | Ranged tower; the muzzle-flash meshes are already authored |
| The Spaceranger | Orange-and-white EVA suit, removable bubble helmet, energy sword and jetpack | Only fits if the setting tolerates tech |
| The Ninja | Masked and headbanded, **four colourways** out of the box | The cheapest tiered line in the pack — same mesh, four ranks |
| The Survivalist | Eyepatch, grey stubble, shotgun, tactical pack with shell loops | Ranged tower with a scavenger read |
| The Paladin | Gilded winged helm and sunburst shield — plus a solid-gold statue of himself | Aura/buff tower; the statue is a ready-made shrine or plinth |
| The Clown | Blue wig and motley, and the largest prop set here: balloons, clubs, hoop, mallet, drum, bomb | Chaos/debuff creep; the bomb suggests a suicide runner |
| The Robots (two models) | Two numbered chassis, red visor slits, cannon arms, plus a charging station | Mechanical creep with two armour classes; the station is a spawner prop |

*(12 entries; the variant pairs bring the count to 15.)*

## Mystery Monthly Series 5 — 14

| Model | Reads as | Natural TD role |
|---|---|---|
| The Combat Mech | Piloted mecha, horned crest, red visor band, separate gatling and shield arms | Heavy tower or late boss; the arms upgrade visibly |
| The Superhero | Red cape and mask over blue, sculpted flying as well as standing; ships rubble props | High-damage single target; the rubble is a free impact effect |
| The Black Knight | Horned great helm with a red plume, purple cape — taller and heavier than the Knight | Elite armoured creep, and the Knight's direct evil twin |
| The Vampire | Red frock coat, widow's peak, fangs; bat-hilted sword, goblet, gems, armchair | Lifesteal creep; the gems make a treasure-carrier |
| The Witch | Wide orange hat, round spectacles, **riding a broom** | Debuff tower — and one of the bundle's few airborne silhouettes |
| The Helpers (two models) | Two bell-hatted elves, plus a workbench, toy train and tools | Swarm creeps or summoned adds; the bench reads as a production building |
| The Frost Golem | White ice construct with pale blue crystal spikes through the shoulders | Cold elemental — armoured creep, or a slow/freeze tower |
| The Caveman | Barrel-chested and red-bearded in a fur wrap; spear, club, stone axe, fire pit | Early-wave basic creep; the fire pit is a map prop |
| The Clanker | Riveted brass-and-olive automaton with glowing lamp eyes and a crown plate | Mechanical mid-boss — bulkier than the Robots and lit from within |
| The Protagonists (two models) | Two modern kids in caps, backpacks and pastel jackets | The closest thing here to a player avatar |
| The Hiker | Hi-vis vest, cap, loaded expedition pack, pitchable tent | Civilian/utility; the tent is a placeable in its own right |
| The Tiefling | Red skin, teal hair, long curved horns, twin curved blades | Demon-faction tower — dual-wield clips are already in the library |

## Mystery Monthly Series 6 — 14

| Model | Reads as | Natural TD role |
|---|---|---|
| The Lorekeeper | White-bearded and spectacled in a teal robe; crook staff, scrolls, writing desk | Support or economy tower; the desk matches him exactly |
| The Orc Brute | Far heavier than the Series 4 Raider — bone necklace, skull trophies, cleaver axe | Tanky mid-boss; put it behind Raiders and the size does the tiering |
| The Cleric | **No published render.** Healer in vestments | **Healer creep** — the unit that forces a wave to be answered in order |
| The Monstrosity | Stitched, flat-headed, bolt-necked, in a brown coat. A Frankenstein, not a blob; pitchfork and a barn door | Boss creep — the barn door is a shield, already dressed for you |
| The Plant Warrior | A hooded seed pod with two white eyes; lily-pad shield, bud spear **and a leaf bow** | Nature creep, and the only one shipping both melee and ranged loadouts |
| The Toy Soldier | Nutcracker red with gold frogging and a shako; musket, bugle, and a gift box it unpacks from | Uniform swarm creep; the gift box is a spawn point |
| The 4-GTN (two models) | The same robot chassis twice: clean white, and overgrown with moss and mushrooms | A mechanical creep and its derelict variant — a tier for free |
| The Hoarder | A small scarf-masked adventurer under an enormous pack — bedroll, pans, books, spare sword | The bounty creep. It visibly carries loot |
| The Avian Swordsman | Blue-feathered bird-person in a green tunic; also ships as a straw training dummy | **The clearest air unit in the roster** |
| The Marksman | A ghillie suit of hanging leaves over a hood, one green optic where a face would be | Long-range tower — same role as the Ranger, completely different read |
| The Magical Girls | One body, four hair and dress colourways, bow-tied hair, star wand | A four-rank tower line where the colour is the rank |
| The Farmers (two models) | A pair in dungarees and straw hats, with wheelbarrows, a pitchfork and produce | Economy/worker units; the produce doubles as a resource prop |

## The animation library

All 57 share one rig, so a clip authored once plays on any of them —
[133 humanoid animations](https://kaylousberg.itch.io/kaykit-character-animations), free and CC0, in six
groups:

- **General** — idling, getting hit, death, spawning, interacting
- **Movement** — walking, running, jumping, crawling, sneaking, dodging, crouching
- **Melee combat** — one-handed, two-handed, unarmed, dual-wielding, blocking
- **Ranged combat** — shooting, aiming and reloading for one- and two-handed weapons and bows, plus spellcasting
- **Simulation** — emotes: waving, cheering, sitting, lying down
- **Special** — skeleton-specific clips, for the Skeletons pack and mechanical characters
- **Tools** (1.1 update) — `Chop`, `Fishing_Cast`, `Hammer`, `Lockpick`, and `Work` variants

**What this buys the design.** A tower firing arrows, casting, blocking or reloading is a clip swap, not new
art. And because towers and creeps come from the same rig, a creep that reaches the end and a tower that kills
it can share an animation vocabulary for free.

## Reading it as a tower and creep roster

Three facts do most of the work:

1. **Adventurers and Skeletons are built as mirrors** — Warrior/Knight, Rogue/Rogue, Mage/Mage. A tower line
   and its counterpart creep line already exist as matched silhouettes.
2. **The variant pairs are transformation mechanics in disguise.** Werewolf (human + wolf), Animatronic
   (normal + creepy), and the several two-character drops give a creep that changes state without a second art
   commission.
3. **The gaps are the honest part.** Flying is thin — The Avian Swordsman, and The Witch on her broom, and that
   is the list — and there are no true beasts and no insects.
   [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) (free, 50 models) is the
   named supplement in [Part IV](../archive/art-direction-and-assets.md), and this is precisely where it earns
   its place.

## Sources

- [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — bundle contents
- [Character Pack: Adventurers](https://kaylousberg.itch.io/kaykit-adventurers)
- [Character Pack: Skeletons](https://kaylousberg.itch.io/kaykit-skeletons)
- [Mystery Monthly Series 4](https://kaylousberg.itch.io/kaykit-series-4)
- [Mystery Monthly Series 5](https://kaylousberg.itch.io/kaykit-series-5)
- [Mystery Monthly Series 6](https://kaylousberg.itch.io/kaykit-series-6)
- [Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) —
  [update 1.1 devlog](https://kaylousberg.itch.io/kaykit-character-animations/devlog/1139588/character-animations-update-11)
