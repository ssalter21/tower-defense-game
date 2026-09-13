# The roster's art

**What each row is drawn as, and why it reads.** [The roster](roster.md) says what a unit is for and names its
model, props and clips on each row's `Looks` line; this page holds the rules those lines obey — how a rung is
told from the rung below, what shape every effect draws, which pack is which side, and what size means.
Every look here was signed by a person, and a change to one is an art decision, never an agent's.

## The tier signal is never the body's size

**Size is not a tier signal for the body.** A rung is told apart by
**what the body wears, holds or stands beside** — never by how big the body is. Three materials, in the order
they are reached for:

| Material | What it is | Where it applies |
|---|---|---|
| **Colour** | The pack's alternate texture for that character, applied per row | Every line. 8 of the 9 base characters ship one; only the Lorekeeper does not, and it is a model swap anyway |
| **A prop** | A different or additional thing in a hand, or standing on the tile beside the tower | Every line |
| **A second model** | A different character, the same person promoted | Knight, Cleric and Engineer have none anywhere in the collection; the other six do |

**Tier 2 is colour, plus a prop where the rung has something to hold. Tier 3 is the second model where one
exists, and colour plus a signature prop where one does not.** Knight, Cleric, Engineer and Druid take the
second road.

**Two lines bend that sentence, and both were signed by looking.** The Elder is colour alone: no prop drawn
in its hand could be told from an empty one at the shipped framing, and the rest joined a staff that already
reads as the Druid's. And the Engineer line is told apart by the **size of its turret** — 1, 1.25 and 1.5 up
the line — because the pack ships one turret and no second Engineer, the body already wears the gold box a
crate would have doubled, and the turret's step is the one rung signal that reads at 1600x900. A prop may be
a size; a body may not, because size is what tells a creep from a tower.

> **A glow is not a tier signal, and that is a reservation rather than an omission.** A persistent glow is
> reserved for reading *"this tower is projecting an aura"* — Shield Wall, Blessing, Consecration and
> Overgrowth. If it also meant "tier 3" the two readings would collide on exactly the rows that need the
> first one. Note that it is not free either way: nothing in the match may **billboard** — turn to face the
> camera — so a glow has to be real geometry, an emissive material, or particles drawn in mesh render mode.
> A default particle system is a stack of camera-facing cards and is refused.

**A line that shoots names where its shot leaves from, and that is part of choosing the prop.** `UnitArt`
carries an effect anchor per row — a bone, or a transform inside the held prop, optionally its far end — and
every flash and tracer is drawn from it. A row without one falls back to a fixed height above its own root,
which is the thing anchors replaced, so a tier that changes what a tower is holding changes where its shot
leaves too: a crossbow, a tome and a turret barrel are three different points on three different rigs.
Anchoring is a view fact and not a signed number, but it is set in the art ticket that chooses the prop,
because that ticket is the only one that knows what the prop is called.

**What `UnitArt` carries for it.** The atlas a row wears — the two views put it on the body before anything
goes in a hand, and the alternate atlases the rows above name are imported beside their own packs — and a
**beside prop**, a model and a size, which is a third socket and not a hand bone: `TowerView` stands it one
tile from the tower root, where it stays while the tower turns to aim. `turret_base`, `paladin_statue`,
`Cleric_Font` and the Druid's weirwood each have somewhere to stand. **When a tower is standing on that tile,
the prop moves to a free neighbour** — the nearest to the tower's right and away from the corridor, never the
corridor itself, and inside the tower's own hex when every neighbour is taken; that is `BesideStanding`, and
the frames are under [`docs/frames/beside-props/`](frames/beside-props/README.md). **The size is per prop and
it is a view fact**, never a column in `content/units.txt`: the three props authored in their characters' own
packs come in at the right size, and a Forest Nature tree does not. The quiver the Ranger carries is in its
fist, because that is a spine socket and not this one.

**One tower has one beside slot.** No rung names two things on the ground; the Engineer's own body mesh
already wears a gold box, so a crate beside the turret would be the same box twice.

## What a row is drawn as

**Ten shapes are signed, and the shape is nearly all that is signed** — every colour, size and duration is the
plainest thing that draws it and is declared a placeholder in `MatchTuning`, save four that a person took:
the disc's alpha, the knife at 0.85 m, a ground effect clipped where the board ends, and the Consecration's
light always on, its two-hex reach being a number in `content/units.txt` rather than a look.

**Every placeholder this page carries waits on one thing, and it is not argued here until then.** The
colours, sizes and durations in `MatchTuning`; the four creep aura shapes below, drawn as one interim disc;
the Vampire's and the Grave Robber's pools; the Mage's bolt and the Mortar's shell, unsignable without a
smoke cloud or a magical effect behind them. All of them are the same question — what the match may draw that
is not a flat mesh — and that is the billboarding prototype, #290. None of them is listed as open on a row
until it answers.

**The four creep auras below the table are a weaker claim than the ten above it.** A walking row carries
**no effect anchor at all** — `ImportedArtTests.EveryTowerFiresFromAPointOnItsOwnArt` asserts that it carries
none, because no creep fires and nothing would ever resolve one — so an aura leaves the **body**, where the
emitter id on the event resolves to, and not the staff, scythe, broom or axe. And with no shape signed, what
each of the four draws is the plainest thing that says what that row's aura *does*. **So on those four rows
the shape is as unsigned as the colour**, which is not true of any of the ten.

| Row | What it draws | Where it is drawn |
|---|---|---|
| 16 · Shield Wall | A ring lying on the ground at the edge of the slow, open in the middle so the bodies caught inside stay visible | Under the tower that pulsed |
| 19 · Slam | Cracks running out from the middle to the edge of the swing | Under the man who swung |
| 22 · Blessing | A ring over the head of every tower the pulse reached, the emitter included | On what the bubble found, not on the bubble |
| 25 · Consecration | A disc of light filling the ground out to the edge of the aura | Centred on the tower, not on the font — the aura is |
| 30 · Overgrowth | A patch of roots breaking the ground under every body the aura is holding | On what the bubble found, not on the bubble |
| 31 · Overwatch | One heavy bar the length of the leg the shot crossed | From the crossbow to the body it was aimed at |
| 34 · Fan of Knives | One knife per shot, crossing to the body it found — three knives where the throw found three bodies | From the hand to each body |
| 23, 24, 25, 28, 29, 30 · the Cleric and Druid lines | A short bolt crossing to the body the shot found | From the tome, the Bishop's open off hand or the staff tip |
| 27 · Unravel | A band broken into plates, lying on the ground out to the edge of the strip | On the hex the bolt arrived at |
| 37 · Mortar | A burst of shards at the radius the blast reached | On the body the shell arrived at |

And the four creep auras, whose shapes **nobody has signed**:

| Row | What it draws | Where it is drawn |
|---|---|---|
| 7 · Skeleton Mage | A flat translucent circle in its own green | On the ground out to the reach of the haste |
| 38 · Necromancer | The same circle, in the pool's blue | On the ground out to the two hexes the ward grants across |
| 41 · Frost Wight | The same circle, in a pale frost blue | On the ground out to the reach of the frostbite |
| 44 · Witch | The same circle, in the armour violet | On the ground out to the edge of the hex ward |

**Every aura on this roster is that one shape, and only the colour tells two apart.**

**Nothing is drawn on the bodies an aura found.** No ring over a hastened creep, no glow on a blessed tower, no
crown at a frostbitten one's feet, no roots under a held body, and no wash of colour on a body carrying a
modifier at all. Which bodies an aura caught is read off the circle they are standing in, and that is the whole
of it. **This is an interim look**: what these effects should finally be is animation and particle work nobody
has done, and this client cannot host it as written — see the reservation below.

**The Vampire's and the Grave Robber's pools get no shape, and that is by design rather than a gap.** A pool is a
`CreepSnapshot` field and not a moment — which is exactly why it survives a scrub — so there is no event to
draw one from, and the pool is already drawn as the second segment of the bar over the body. A second shape
saying "there is a pool here" could only be invented. What those two rows are still waiting on is the same
thing every other placeholder here is: somebody signing what a pool should look like.

**Twelve of the fourteen are bound per row and two cannot be.** A signature is reached through the entity the
event names: an aura pulses from its own emitter, a sweep is centred on the tower that swung, and a shot names
the tower that fired it, so those twelve name a row — four of them rows that walk, since an aura pulses from its emitter whichever side
the emitter is on. A blast centred on its target names the *body the shot
arrived at* — the shooter is not in the event and deliberately never will be, because an event carries an
entity id and nothing to hold on to. The Mortar's burst and the Unravel's strip are both that case, and what
tells them apart is the one thing on the event that is not the victim: the **payload**, `damage` against
`armour`. That is a shape chosen by the payload rather than by the row, and its weakness is stated rather than
hidden — a second row authoring a target-centred armour blast would wear the strip too, exactly as the Mage's
and the Sorcerer's damage splashes wear the burst. See [the open question](open-questions.md#what-the-drawn-shapes-left-open).

**A bubble's shape and a shot's shape are two fields on `UnitArt`, not one** — a `BubbleSignature` and a
`ShotSignature` — because Consecration and Overgrowth want an aura on the ground and a bolt out of the tome on
one row. **A bolt is the one signed shape a whole line
wears rather than one capstone**: six rows fire it, which is what makes the shot table read differently from
the bubble table.

**Neither pierce capstone is a bubble, so neither had the blast's problem.** The Overwatch is a long single
shot and the Fan of Knives is a `targets` of three; `content/units.txt` gives both of them `none` in the radius
column. Three shots at three bodies is three `TowerFired` events on one tick, each naming the tower and one
body, so three knives is what the event stream produces rather than something the view has to know how to fan
out. **The Mage line draws no shot shape at all**, and that is its delivery column: those three rows are
projectile, so the thing crossing to the body is the shell in the snapshot and a bolt drawn beside it would be
a second thing in the air saying what the shell already says.

**One aura draws nothing at all, and it is the Overgrowth.** Its aura reaches **sixty hexes** — the whole
board, every board — so the circle every other aura leaves would be a hundred and twenty hexes across on a
board nineteen wide: the screen washed flat rather than an area shown. An aura that covers everything has no
impact area worth outlining, so it gets none, and the hold reads through the creeps not moving.

**None of it is a `ParticleSystem`, though one is allowed to be.** Every shape here is a mesh of solid bars
generated in `EffectMeshes`, or — for the two that are simply straight — a stretched box, lit by the one
directional light everything else on the board is lit by. **What the rule forbids is billboarding, not
particles**: a particle system in `Mesh` render mode emits real geometry per particle and faces nothing, and
the play-mode guard checks the render mode rather than the component.

**A shape that stands for a distance does not shrink as it ages.** The shared ageing closes a tracer, a flash
and a spark down to nothing, because their size is how loud they are; a ring, a shock and a burst say how far
the bubble reached and the Overwatch's shot says how far the shot went, so one that shrank would report a
reach that was never had. Every shape is held to it by a test.

**Two things move rather than staying where they were drawn: the knife and the bolt.** Each carries the two
points it was drawn between and crosses between them on the tick — six of them for a knife, five for a bolt —
so a body that dies mid-flight leaves the throw finishing as it was drawn. The flight is a picture of a throw
and not the shot: every row that draws one is hitscan, and the damage landed on the tick it was fired.

## Which pack is which side

**KayKit's Skeletons are the creeps and the Adventurers are the towers.** Each skeleton was built as a specific
adventurer's deliberate twin, so **the two sides of the board are the two halves of one pack**, and a body reads
against the tower it is the shadow of. Quaternius's Ultimate Monsters are rejected.

**The Skeletons pack holds six models, and all six are assigned**: the Minion and the Skeleton share the
minion skin, the Warrior takes the warrior, the Scout the rogue, the Skeleton Mage the mage, the Necromancer
row the dedicated **Necromancer**, and the Bone Golem the **Skeleton Golem** the publisher sells as a boss.
The Minion and the Skeleton sharing is a **kit variation and not a shortage** — the Skeleton is that model
with shield and sword, and the pack ships the weapons for it. The
[collection inventory](research/kaykit-collection-inventory.md) counts all six.

**The two-halves-of-one-pack rule extends to the Mystery Monthly characters**, and the line it draws is the
same one: **the ones that read as heroes join the tower side; the ones that read as undead, dark or hooded
join the creeps.** That admits the Vampire, the Witch, the Tiefling and the Werewolf, which are not undead
but are unmistakably the dark half.

## The assignments are signed

The complete collection — 22 packs, CC0, 61 rigged characters, 159 clips — is on disk and catalogued from the
archive itself in [the collection inventory](research/kaykit-collection-inventory.md). The assignments above
are **adopted as written** rather than left as a plan, and they were adopted by a person. The archive is
extracted at `~/repos/kaykit-collection/`, beside the checkout, which nothing in the project reads; the
character models are imported under `client/Assets/Art/Characters/` and the whole collection under
`client/Assets/Art/Kaykit/`. Every block above names its model, what is in each hand, and the clip it is
posed by.

**Size tells the two sides apart and nothing else** — see
[the tier signal](#the-tier-signal-is-never-the-bodys-size). Two multipliers, applied to the model as it is
drawn:

| What | Scale | Why |
|---|---|---|
| Towers | **1.0** | the baseline everything else is read against |
| Every creep | **0.5** | a medium-rig creep reads as smaller than the thing shooting it, at any camera angle — a size-up rig does not, and that is allowed |

> `EveryUnitTypeIsDrawnAtItsRosterScale` asserts the two multipliers, and
> `TheTwoRowsOnOneModelAreToldApartWithoutSize` holds the consequence: the Archer and the Ranger share a
> model and a scale, so one of the three materials must separate them.

**Scale lives in `MatchArt` and never in `content/units.txt`.** Visual size is a view fact under
[ADR-0007](adr/0007-snapshot-is-the-only-view-input.md), and a column in the content tables would make every
art tweak cost a format version and a re-record. These numbers are expected to move once somebody has looked
at them, which is the whole reason they are stored somewhere free to change.

**A creep is not asserted to be shorter than a tower.** The two multipliers in the table above are the whole
of what this page signs about size, and **the 0.5 is a multiplier and not a promise about the result**: the
reason column's "unmistakably smaller than the thing shooting it" describes what it does to a medium body and
is not a rule anything holds it to.

**That is a deliberate opening.** A row on the pack's `Rig_Large` size-up draws at tower height while obeying
the creep multiplier exactly — the collection is authored at two scales, a `Rig_Medium` character at
2.3–2.9 m and a size-up at 4.2–5.1 m, so half of a size-up is a whole tower. Four shipped rows are there:
the **Black Knight at 2.56 m**, the Bone Golem at 2.32, the Abomination at 2.24 and the Frost Wight at 2.09,
against a shortest tower — the Unravel — of 2.31 m. A played run shows what it reads as: a Black Knight
walking past four towers with its helmet at their head height
(`docs/frames/played-run/black-knight-beside-towers.png`). That is **allowed**, and a boss larger than a
tower is a shape this page expects to sign later rather than a thing to design around.

**Nothing measures height, and that is the cost.** No test compares two packs' authoring scales against each
other, and the multipliers alone prove nothing, since a half applied to a taller model is not smaller than a
one applied to a shorter one. So a row imported at the wrong scale is caught by looking, not by a runner. If
a size rule ever returns, it returns as a *band* per rig rather than as an ordering between the two roles.

**There is no plinth, and no rule about which units are people and which are buildings.** That distinction was
considered and dropped: it is not a thing this page needs to have an opinion about.
