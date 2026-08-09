# The character roster: KayKit and Quaternius

**The question:** what character models do [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete)
and [Quaternius Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) actually contain, so the
tower and creep rosters can be drafted against real assets rather than against a number?

**The answer: 57 KayKit characters across five packs**, plus a shared 133-clip animation library that every one
of them can play, **and 45 catalogued Quaternius monsters** that fill the gaps KayKit leaves. This note lists
all 102.

> **There is an illustrated version.** Every entry below with its render, kit list and role tag, filterable by
> source, pack and tower/creep: **[the roster report](https://claude.ai/code/artifact/4b8c7b02-5397-4f73-b9fd-afaef967857c)**.
> The renders are the publishers' promotional images and are not committed to this repo.

> **Provenance, and its limits.** KayKit's pack contents were read from the publisher's own itch.io listings and
> the CC0 GitHub mirrors; the Quaternius entries came from the per-model pages on
> [poly.pizza](https://poly.pizza/bundle/Ultimate-Monsters-Bundle-5oyGWAmOB6), which is where that pack is
> catalogued model by model. Both were pulled in August 2026 and the descriptions were written from those
> renders. **Neither pack is on this machine** — `client/Assets/Art/Characters/` holds exactly two imported FBX
> files, `Ranger.fbx` and `Skeleton_Warrior.fbx`, both from KayKit's free tiers. So the **names and renders** are
> the publishers'; the **TD readings are inferred**. Treat them as a drafting aid and confirm against the real
> files once both packs are downloaded. Two KayKit characters — **The Monster Costume** and **The Cleric** — have
> no published render anywhere, and four more (The Survivalist, The Witch, The Clanker, the Skeleton Warrior) had
> to be cropped from pack banners or taken from the GitHub mirror. Quaternius advertises **50** monsters;
> poly.pizza catalogues **45**, so five are described nowhere and are absent here.

## What the KayKit bundle contains

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

## Quaternius Ultimate Monsters — 45 catalogued of 50

Free, CC0, released October 2022, shipped in FBX, OBJ, glTF and `.blend`, every model animated. This is the
supplement [Part IV](../archive/art-direction-and-assets.md) already named, and it is where KayKit's missing
beasts, insects and fliers come from.

The pack is built on one idea: **a creature starts as a floating head and evolves into a body.** Fifteen
families ship in more than one stage — fourteen as a base-and-evolved pair, and Mushnub three deep, ending in
the Mushroom King. A gold crown is the pack's consistent marker for the elite stage.

Triangle counts are poly.pizza's. Six pairs are listed there under a single shared name each; the full-bodied
one of each pair is suffixed *Evolved* below to match how the rest of the pack labels itself.

### The multi-stage lines — 31 models

| Model | Tris | Reads as | Natural TD role |
|---|---|---|---|
| Mushnub | 1.2k | A bare tan stalk under a plain blue cap, two eye-slits, no limbs, no mouth | Wave-one trash — the simplest silhouette in the pack |
| Mushnub Evolved | 2.3k | Same mushroom; the cap is now spiked in gold and the face split by a toothy mouth | Wave two of the same line. The cheapest escalation here |
| Mushroom King | 5.1k | Full-bodied at last — teal gloves and boots, holding a blue orb | Caster, not creep. Closes the line as an unofficial stage three |
| Green Blob | 1.5k | A dark green lump, one enormous central eye, ragged rim | Swarm creep |
| Green Spiky Blob | 4.5k | The same lump wearing a crown of long white spikes | The armoured version — spikes say "do not melee this" |
| Orc Enemy | 2.2k | A green goblin head, teal mohawk, gold hoop earring, tongue out | Mid-wave rusher; the mohawk gives it a directional silhouette |
| Orc | 4.3k | The full-bodied goblin, striped, with a spiked knuckle | Elite creep — or a melee tower if the defenders are monsters too |
| Ninja | 2.1k | Black hood and mask, two gold sword hilts crossed behind | Fast, low-health creep |
| Ninja Evolved | 5.8k | Full body in black, curved sword drawn | The most tower-shaped model in the pack, and the obvious assassin creep |
| Cactoro | 2k | A cactus head under an oversized straw sombrero | Themed wave — the sombrero *is* the silhouette |
| Cactoro Evolved | 5.3k | The full cactus body, same sombrero | Elite version. Same read, more mass |
| Alien | 2.6k | A purple head with four curling green tentacles, one eye slit | Breaks the round-blob monotony |
| Alien Evolved | 6k | Full-bodied and grinning, white belly, tentacles intact | Boss-adjacent scale for a mid-game wave leader |
| Yeti | 2.1k | A huge teal head, buck teeth, mouth hanging open | High-health, low-speed creep — it reads slow |
| Yeti Evolved | 4.3k | Full-bodied teal-and-white, small horns | Tank creep; the white belly makes a good health-bar anchor |
| Fish | 3.3k | Teal fish head, yellow side fins, dorsal spike, blunt teeth | Water-lane creep, or a biter |
| Fish Evolved | 5.1k | The same fish given arms and legs | Murloc-shaped elite that can plausibly melee a tower |
| Glub | **672** | A blue nub with a stalk and two grey wings — the lightest model in the pack | The swarm flier. Thirty in the air without a frame-rate conversation |
| Glub Evolved | 2.4k | The same creature stacked taller, with a third eye | Escalation by literally growing. Reads as a rank-up |
| Armabee | 1.5k | A black-and-yellow bee head, antennae, grey wings, mouth shut | The insect KayKit has none of |
| Armabee Evolved | 1.8k | The same bee, mouth open, two fangs showing | A 300-triangle upgrade — the cheapest tier-two model here |
| Alpaking | 2.1k | A golden head under a tan five-point crown, wings spread | Wave captain; the crown does the tiering for you |
| Alpaking Evolved | 4.6k | Three golden heads stacked under the same crown | The only multi-headed model. Argues for a split-on-death creep |
| Goleling | 3.6k | A green bat head, purple muzzle, dark folded wings | Standard air creep, legible over a bright map |
| Goleling Evolved | 4.1k | The same bat, gold crown, mouth wide open | Crowned again — the pack's universal elite marker |
| Dragon | 3.8k | An orange dragon head, membrane wings, small white horns | Late-wave flier, recognisable at thumbnail size |
| Dragon Evolved | **6.7k** | Full body, long horns, wings fully spread — the heaviest model in the pack | The pack's boss, and still only 6.7k triangles |
| Ghost | 2.1k | A black bat-shaped shade, clawed hands, blank face | Physical-immune or phasing creep — it already looks unhittable |
| Ghost Skull | 2k | The same shade with a bone skull for a face | A reskin, not a rebuild. Exactly how to do a cheap variant |
| Demon | 3.7k | A red winged head, black horns, claws — and a halo | Air creep with a joke built in; the halo is a separate piece |
| Demon Evolved | 5.8k | The full red devil, standing, pitchfork in hand, halo still overhead | Armed and standing — works as tower or boss creep |

### The standalone models — 14

| Model | Tris | Reads as | Natural TD role |
|---|---|---|---|
| Pink Slime | 982 | A pink teardrop with two oversized eyes and nothing else | The canonical swarm unit |
| Cat | 1.7k | An orange tabby head with ears and whiskers, no body | Critter filler — a neutral or bonus target |
| Pigeon | 2k | A round purple bird head, yellow beak, feather-tuft wings | Air creep. This pack fixes KayKit's flying gap outright |
| Chicken | 2.6k | White head, red comb and wattle, feather ruff | Comic air creep — reads harmless, good for a bonus round |
| Wizard | 1.7k | A purple floppy pointed hat over a dark body; the hat is most of the model | The one unambiguous caster, and the only model tagged NPC not enemy |
| Birb | 2.7k | A blue head, tall pointed ears, big flat red tongue lolling out | Cheap, silly early creep. Tagged dragon *and* chicken |
| Hywirl | 2.6k | A small purple imp, four spindly limbs, tiny horns | Skirmisher — thin limbs read as fast against all the round tanks |
| Tribal | 3k | A green tiki mask crowned with white feathers, red clawed arms | The only model that reads as a totem — the pack's natural tower |
| Blue Demon | 4.5k | A teal imp with pointed ears, yellow loincloth and a wooden club | A club is a free melee animation; pairs with the Orc line |
| Squidle | 3.6k | A magenta flier, teal-lined bat wings, small horns, tongue out | A second air family in a different colour — two flying waves, told apart |
| Frog | 4.3k | A gold frog with black markings, on all fours | Erratic-pathing creep; hopping is implied by the shape |
| Monkroose | 4.6k | A green monkey, wide round ears, orange snout and hands | Bipedal and expressive enough to serve as a non-human tower |
| Dino | 4.3k | A magenta dinosaur, yellow nose horn, yellow belly, mouth open | Beast creep; the horn sells a charge attack |
| Bunny | 5.9k | A white rabbit with long ears, pink belly, carrot in one paw | Holds a prop, so it can be re-armed with anything |

## Reading it as a tower and creep roster

Five facts do most of the work:

1. **Adventurers and Skeletons are built as mirrors** — Warrior/Knight, Rogue/Rogue, Mage/Mage. A tower line
   and its counterpart creep line already exist as matched silhouettes.
2. **The variant pairs are transformation mechanics in disguise.** Werewolf (human + wolf), Animatronic
   (normal + creepy), and the several two-character drops give a creep that changes state without a second art
   commission.
3. **KayKit's gaps are exactly what Quaternius fills.** Flying in KayKit is The Avian Swordsman and The Witch
   on her broom, and that is the list; there are no true beasts and no insects. Ultimate Monsters answers all
   three — sixteen of its forty-five models fly, and it brings bees, bats, fish, frogs and a dinosaur.
4. **Quaternius ships upgrade tiers as art.** Fifteen families exist in more than one stage, and a gold crown
   marks the elite one every time. A tower upgrade path or an escalating creep wave is already modelled; the
   work is choosing between stages, not commissioning them. The pack is also cheap enough to swarm — Glub is
   672 triangles, Pink Slime 982, and the heaviest model in the pack is 6.7k.
5. **But the two packs do not mix.** This is the real decision. KayKit is flat-shaded, muted, human-proportioned
   and rigged on one shared humanoid skeleton. Quaternius monsters are smooth-shaded, saturated, mostly head,
   and rigged individually. In one scene they read as two games. Using Quaternius for creeps and KayKit for
   towers turns that into a deliberate contrast; mixing them *within* either role turns it into a mess. Worth
   settling before anything is imported.

## Sources

- [The Complete KayKit](https://kaylousberg.itch.io/kaykit-complete) — bundle contents
- [Character Pack: Adventurers](https://kaylousberg.itch.io/kaykit-adventurers)
- [Character Pack: Skeletons](https://kaylousberg.itch.io/kaykit-skeletons)
- [Mystery Monthly Series 4](https://kaylousberg.itch.io/kaykit-series-4)
- [Mystery Monthly Series 5](https://kaylousberg.itch.io/kaykit-series-5)
- [Mystery Monthly Series 6](https://kaylousberg.itch.io/kaykit-series-6)
- [Character Animations](https://kaylousberg.itch.io/kaykit-character-animations) —
  [update 1.1 devlog](https://kaylousberg.itch.io/kaykit-character-animations/devlog/1139588/character-animations-update-11)
- [Quaternius · Ultimate Monsters](https://quaternius.com/packs/ultimatemonsters.html) — pack page, download and licence
- [Ultimate Monsters on poly.pizza](https://poly.pizza/bundle/Ultimate-Monsters-Bundle-5oyGWAmOB6) — the per-model
  names, renders, tags and triangle counts used above
