# Match frames

Rendered by `tools/capture-match-frames.ps1`, headless, with the editor closed.

**These are documentation, not an oracle.** Nothing compares them to anything
and nothing fails if they change. That call was made deliberately on this
project, after two frames whose bones were definitively swapped rendered
pixel-identical, reproducibly — a screenshot comparison here would be a check
that cannot fail, which is the species this project keeps deleting.

What catches a broken view is the assertions in `Tests.PlayMode/MatchViewTests`
and the sit-down landmark table. What these are for is letting a human see the
match at a named tick without opening the editor.

They are drawn through the real thing: the real `MatchRoot`, the real hex floor,
the real `OrbitCameraRig` pointed where the arguments say, and the real
`MatchView` stepping the real simulation. A capture path that built its own
approximation of the scene would be a picture of something this project does not
ship.

**And it is the real match**, read out of `content/match.replay` — the same
bytes the command line replays and the player plays, seed included. The tick in
each filename is only worth anything because of that: it is a tick of the run
`content/landmarks.txt` was made from, so a frame named 1096 and the checklist
row that says "drag to tick 1096" are about the same moment.

## Regenerating

```powershell
./tools/capture-match-frames.ps1                       # the default ticks
./tools/capture-match-frames.ps1 -Ticks "366,900"      # named ticks
./tools/capture-match-frames.ps1 -Yaw 120 -Width 1920  # another heading, wider
./tools/capture-match-frames.ps1 -Distance 25          # down among the creeps

./tools/capture-match-frames.ps1 -Ticks "1229,1546" -Distance 22 -Width 1600

# The same board, defense, wave and seed, played against a roster of your own
./tools/capture-match-frames.ps1 -Units "docs/frames/effects-roster.txt" `
    -Ticks "700" -Distance 20 -Width 1600

# The same board, wave and seed, with a defense of your own standing on it
./tools/capture-match-frames.ps1 -Defense "docs/frames/four-lines.txt" `
    -Ticks "572,780" -Width 1600

./tools/capture-match-frames.ps1 -Defense "docs/frames/pierce-lines.txt" `
    -Ticks "673" -Distance 18 -Width 1600

./tools/capture-match-frames.ps1 -Defense "docs/frames/magic-lines.txt" `
    -Ticks "311,342,344" -Distance 22 -Width 1600

# The same board, defense and seed, with a wave of your own walking it
./tools/capture-match-frames.ps1 -Wave "docs/frames/creep-auras.txt" `
    -Ticks "272" -Distance 20 -Width 1600
```

**Finding the tick a capstone went off on is what the log line is for.** A
signature is drawn on one tick and gone six or eight later, so hunting one by
opening pictures means opening most of them. Every kept tick's line in
`capture-match-frames.log` carries the running slow-ring, ground-shock, glow,
burst, long-shot, knife, bolt, light, root, strip, haste-ring, ward-dome,
hex-plate and frost-crown counts; ask for a run of
consecutive ticks, read the line where one of those numbers moves, and then
capture that tick on its own at the size you want. **The knife count is the one
that says how many bodies a throw found** — it goes up by three where the Fan of
Knives had three in range and by one or two where it had fewer, so a frame of
*three* knives is found by looking for a step of three.

**A frame is a function of its tick AND of the tick list it was asked for**, and
the second half of that is a trap. The capture draws every tick it steps through
rather than only the ones it keeps, because where a tower is pointing and where
its shot leaves from are both read off the pose it was last drawn in. It used to
draw only at the kept ticks, which made a sparse tick list a photograph of a
match whose towers had never moved — and, once effects started leaving the model
rather than a fixed height above it, put the muzzle flash on a rig still standing
in its bind pose.

**Drawing every tick did not make the kept ones independent of each other.**
Measured twice, reproducibly: `-Ticks "342,344"` and `-Ticks "311,342,344"` give
different bytes for 0342 and 0344, and only the three-tick list reproduces what
is committed. So **re-capture a fixture with the whole tick list above and not
one tick at a time**, and expect a frame to move if you do not. Where a frame is
listed on a line of its own below — 0813, 0674, 0331 — that line is its whole
list.

## What is committed

**`-Units`, `-Defense` and `-Wave` are three switches for three different
absences, and which one a picture needs is decided by what the record is missing
rather than by preference.** The record carries a board, a defense, a wave and a
seed. `-Defense` replaces what is standing, and is what photographs a tower row
the bot's six never build. `-Wave` replaces what is walking, and is what
photographs a creep row the recorded wave never sends — with its own shipped
numbers, so nothing about it goes stale. `-Units` replaces the roster itself,
which is the heaviest of the three and the only one that needs a fixture table
kept in step with `content/units.txt`; it is for photographing something *no*
shipped row does at all.

Four frames, kept as a record of what the match looks like:

- `match-tick-1096.png` — the tick the committed landmark table names as the
  first overtake, with the wave strung out along the corridor and a shell in
  flight.
- `match-tick-2700.png` — the wave spread along the corridor, both kinds of
  tower engaged.
- `match-tick-1229.png` — an Archer releasing: the muzzle flash sits on the bow
  in its hand and the tracer runs from there to the creep it hit. Captured with
  `-Distance 22 -Width 1600`, close enough to see which part of the model the
  shot left.
- `match-tick-1546.png` — a Mage casting, captured the same way. The flash is on
  the head of the staff, which the Mage raises beside its own head — and **the
  hat covers the staff at the pitch this camera is fixed at**, so what the frame
  shows is a flash and a tracer at the right place rather than a staff with a
  light on the end of it. That the anchor is on the staff and not on a height
  above the root is asserted in `ImportedArtTests`, which logs the measurement
  for every tower; whether the Mage's silhouette should read better than this is
  a question for whoever owns the art.

The last two are the pair the effect anchors landed with: before them every
tower fired from one fixed height above its own root, whatever it was holding.

And one that is **not** a frame of the recorded match:

- `effects-roster-tick-0700.png` — **a placeholder, and the thing it is
  showing has not been signed.** A creep the snapshot says is slowed is washed
  in one colour, and the pool standing in front of a creep's health is a second
  segment of a bar above it. Captured with `-Distance 20 -Width 1600` against
  [`effects-roster.txt`](effects-roster.txt), which is the shipped roster with a
  bubble added to two of its rows — the recorded wave sends Minions and Skeleton
  Scouts against Archers and Mages, and not one of those four rows authors a
  bubble that lasts. The Mage's splash is damage and lands instantly, and the
  four creep auras the roster carries are on rows the recorded wave does not
  send, so nothing in the recorded match is ever slowed or shielded and a frame
  of it shows none of this. **What a slowed, hastened, cursed or
  shielded body should actually look like is Sam's to sign**, and this is what
  the plainest first answer looks like on the real board. Five things about it
  are placeholder answers to questions nobody has taken: one colour covers both
  a slow and a haste, a body carrying a speed modifier *and* an armour one shows
  only the speed, a tower carrying a modifier is not drawn at all, the bar does
  not turn to face the camera and so is read end-on from two of the four
  quadrants of the orbit, and both segments are shares of the health the row
  authored — so a creep at full health with a pool worth two fifths of it draws
  one and two fifths of a bar rather than one.

And three that are frames of the recorded board with **somebody else's defense
standing on it** — the twelve rows of the Knight, Barbarian, Paladin and
Engineer lines, out of
[`four-lines.txt`](four-lines.txt), all at `-Width 1600`. **Every shape in them
is a placeholder and none of it is signed.** What issue #263 signed is four
shapes — a ring for the Shield Wall's slow, a shock across the ground for the
Slam, a glow on every tower the Blessing reaches and a burst at the radius the
Mortar landed in — and every colour, size and duration they are drawn at is the
plainest thing that draws that shape, declared as a placeholder in
`MatchTuning`.

- `four-lines-tick-0813.png` — **the one to look at first**, at `-Distance 22`,
  close enough to read what each shape is made of. Three signatures at once: the
  blue ring on the left is the Shield Wall's slow, lying on the ground at the
  one hex it carries; the orange cracks under the Barbarian on the Large rig are
  the Slam's swing landing on everything touching him; and the two gold rings
  hanging over the heads on the right are the Blessing's, on itself and on the
  Templar standing one hex away. The Paladin, six hexes off, is wearing none —
  that is the aura's reach and not an oversight.

- `four-lines-tick-0572.png` — the same three signatures at the framing that
  fits the whole floor, so all twelve rows are in one picture. The Engineer's
  shell is the black sphere in mid-air over the corridor.

- `four-lines-tick-0780.png` — the Mortar's burst, on the body its shell arrived
  at, at the whole-floor framing. **It is as wide as the blast is: three hexes
  across**, because the row authors a radius of 1500 and a shape that stands for
  a radius may not report a smaller one. Whether something that size reads as a
  burst or as a windmill is exactly the question this frame is asking.

**Three things about them are results rather than questions.** The Paladin and
Engineer lines are bound with no clips at all — `roster.md` names none on either
— so what a firing frame shows for those six rows is a body standing in its bind
pose with the effect leaving the right place on it: the Paladin's hammer head,
the Blessing's book, and the top of the turret standing on the tile beside each
Engineer rung. **The Mortar's burst is the one signature no row selects**: a
blast centred on the body a shell arrived at names the body and not the shooter,
so the burst is what every target-centred blast draws — the Mage's and the
Sorcerer's splash included. And **no frame shows all twelve rows firing at
once**, because a hitscan row fires when a body is beside it and the corridor
does not put a body beside all twelve at any tick; that each row fires from a
point on its own art rather than from a height above its root is measured for
every one of them by `ImportedArtTests`, which logs the distance, and asserted
on this board by `MatchViewTests`.

And three of the recorded board with the **six rows of the Archer and Rogue
lines** standing on it, out of [`pierce-lines.txt`](pierce-lines.txt), all at
`-Width 1600`. **Every shape in them is a placeholder and none of it is
signed.** What issue #264 signed is two shapes — the Overwatch's single shot
drawing a tracer the length of the leg it crossed, and the Fan of Knives
throwing three knives at three bodies — and every colour, size and duration they
are drawn at is the plainest thing that draws that shape, declared as a
placeholder in `MatchTuning`.

- `pierce-lines-tick-0673.png` — **the one to look at first**, at
  `-Distance 18`. The Fan of Knives, hooded in blue, has just thrown: three
  pale knives are strung out across the corridor, one to each of the three
  skeletons nearest the exit, two ticks into a six-tick crossing. The long pale
  bar running out of the top-left corner is the Overwatch's shot, fired one tick
  earlier from a crossbow the frame does not quite hold, and ending on the
  skeleton furthest along the corridor.

- `pierce-lines-tick-0516.png` — the lower rungs at work, at `-Distance 18`. The
  Archer and the Ranger are both at full draw on the bow, one tick of the
  nine-tick windup they share, and knives are crossing the corridor beside the
  skeletons walking it. **The four rungs that are not capstones draw the thin
  tracer every hitscan row has always drawn**, and four ticks of it is short
  enough that catching one in a still is luck — what is worth reading here is
  that the bow is drawn and the shot leaves it, which
  `ImportedArtTests` measures for every row and `MatchViewTests` asserts on this
  board.

- `pierce-lines-tick-0674.png` — the whole-floor framing, so all six rows are in
  one picture with the Overwatch's shot crossing it. **It is the length of the
  leg**, which is what that row is for: eight hexes of range against the
  Archer's three, and the bar says so by being that long rather than by being
  any other colour.

**Three things about them are results rather than questions.** **The Rogue's
`Throw` and the Fan of Knives' slice are bound into a nought-tick windup**, so
neither ever plays on the board and a firing frame of those rows is a body in
its resting pose with a knife leaving its hand; **the Overwatch's one signed
clip is a stance** — `Ranged_2H_Aiming`, held through all three states, because
its windup and backswing are unsigned — so its firing frame is that same
sighted pose whatever the tick. Both are `docs/roster.md` speaking rather than
an omission here. And **the Fan of Knives carries two identically named daggers,
so all three knives leave one hand — `handslot.l`, the off hand**, which is
whichever of the two the lookup reaches first and not a hand anybody picked. Its
own two rungs below throw from `handslot.r`. `ImportedArtTests` logs what every
row's anchor was found under, which is where that measurement comes from; which
hand the capstone *should* throw from is on `roster.md` as a question.

And four of the recorded board with the **nine rows of the Mage, Cleric and
Druid lines** standing on it, out of [`magic-lines.txt`](magic-lines.txt), all at
`-Width 1600`. **Every colour, size and duration in them is a placeholder and
none of those is signed.** What issue #265 signed is four shapes — a bolt leaving the tome or the
staff tip, the Consecration's light on the ground, the Overgrowth's roots on
every hex it slows, and the Unravel's armour strip on the hex his bolt landed
on — and every colour, size and duration they are drawn at is the plainest thing
that draws that shape, declared as a placeholder in `MatchTuning`.

- `magic-lines-tick-0344.png` — **the one to look at first**, at `-Distance 22`.
  The violet band broken into plates, lying on the ground around the skeleton at
  the left of the light, is the Unravel's armour strip, drawn on the tick his
  bolt arrived. The three pale bars in the air are the bolts fired two ticks
  earlier, two ticks into a five-tick crossing. The wide pale disc under the towers is
  the Consecration's light, and the small green sprigs under the bodies standing
  in it are the Overgrowth's roots.

- `magic-lines-tick-0342.png` — the same corner two ticks earlier, so the three
  bolts of that tick are freshly out of the tome and the staff tip rather than
  most of the way across. **The Mage line draws no bolt at all** and that is the
  delivery column rather than an omission: those three rows are projectile, so
  what crosses to the body is the shell in the snapshot, and a bolt drawn beside
  it would be a second thing in the air saying what the shell already says.

- `magic-lines-tick-0311.png` — **the Mage line firing**, which is what the
  three frames above cannot show. The bald Lorekeeper is the Unravel: the flash
  and the shared tracer are leaving his open tome, and the dark speck near the
  top of the frame is the shell that leaves with them, thirty-three ticks from
  the body it strips at tick 344. So this frame and
  `magic-lines-tick-0344.png` are the two ends of one shot.

- `magic-lines-tick-0331.png` — the whole-floor framing, so the reach of both
  auras is in one picture. The Consecration's light covers three hexes round the
  font; the Overgrowth's roots are under every body on the board, because that
  aura reaches sixty hexes and the board is nineteen across. **The orange burst
  in the middle of it is open question 8 in one picture**: it is the Mage's or
  the Sorcerer's splash landing, wearing the Mortar's capstone shape, because a
  blast centred on the body a shot arrived at names the body and never the
  shooter.

**Two things about them are results rather than questions.** **The Cleric and
Druid lines carry a nought-tick windup and a nought-tick backswing** in
`content/units.txt` — six of the nine rows — so `Ranged_Magic_Shoot` never plays
on the board and a firing frame of those rows is a body in its resting pose with
a bolt leaving its tome or its staff. The Mage line is the one of the three
whose cast is posed, at a signed windup of 21 and a backswing of 15. Both are
`docs/roster.md` speaking rather than an omission here. And **the Bishop and the
Consecration fire from the head of a mace**, because the tier-2 line names
`Cleric_Mace` and never says where `Cleric_Tome` goes, so the mace took the
tome's hand — #259's open question, visible in these frames.

And four of the recorded board with a **wave of the six creep rows that carry
an aura or a pool** walking it, out of [`creep-auras.txt`](creep-auras.txt), all
at `-Width 1600`. **Every colour, size, duration and — unlike the ten signed
shapes above — every *shape* in them is a placeholder, and none of it is
signed.** Issue #266 asked for four creep effects leaving the staff, the scythe,
the broom and the axe, and named no shape; a walking row carries no effect
anchor, which `ImportedArtTests` asserts because nothing would ever resolve one.
So each aura is centred on the body, and what each one is drawn as is the
plainest thing that says what that row's aura does — which is a weaker claim
than any of #263 to #265 made, and `MatchTuning`'s own header says so.

- `creep-auras-tick-0272.png` — **the one to look at first**, at `-Distance 20`.
  All four auras pulsed on tick 271 and this is the tick after. The pale blue
  cages are the Necromancer's ward, three of them overlapping, at the two hexes
  it grants a pool across; the green rings hanging over the heads inside them
  are the Skeleton Mage's haste, one per body it reached; and the violet plates
  scattered across the ground are the Witch's hex ward, three bands of them out
  to two hexes each. The green-and-blue bars over the bodies are **not** an
  effect of this ticket: they are the two-segment bar #254 already draws, and
  the blue half is a pool — some of it the Vampire's and the Grave Robber's own
  and some of it what the Necromancer just granted. **The Minions in the knot
  were not sent**: the wave releases six rows and none of them is a Minion, so
  every one on screen is a body a Necromancer raised.

- `creep-auras-tick-0276.png` — the same knot at `-Distance 14`, down among the
  bodies, four ticks later. What is worth reading here is that a haste ring
  hangs above every body inside the aura and that the bar and the ring are two
  different statements about one creep.

- `creep-auras-tick-0271.png` — the whole-floor framing, so the reach of all
  four is in one picture against a board nineteen hexes wide. Two hexes is what
  every creep aura on the roster carries, and this is what two hexes looks like
  from where a player sits.

- `creep-auras-tick-0094.png` — **the Frost Wight's frostbite**, at
  `-Distance 18`, which the three frames above cannot show. The pale shards
  round the feet of the Archer standing among the wave are it: frostbite is the
  one aura on the roster whose `affects` column reaches the *other* side, so it
  is the one creep shape drawn on a tower. It is also the smallest of the four
  and the question this frame asks — a frozen tower wears nothing else, because
  the wash `EffectMarks` puts on a modified body is a creep's and there is no
  tower equivalent, so this crown is the whole of what says that tower is firing
  a third slower.

**Three things about them are results rather than questions.** **The two rows
with a pool of their own draw no effect at all**, and that is what #254 already
built rather than an omission: a pool is a `CreepSnapshot` field and not a
moment, which is why it survives a scrub, and the Vampire's blood and the Grave
Robber's pack are the blue segment of the bar over the body in every frame here.
**A hastened body is washed the same colour a slowed one is** — `SpeedEffectTint`
covers both signs, which is a placeholder decision #254 recorded and this ticket
did not reopen — so the haste ring over its head is what tells the two apart.
And **the Necromancer's cage is a moment and the haste ring is a state**: the
ward's duration is zero, so the cage stands ten ticks and the pool it granted
goes on being drawn on the bar, where haste, hex ward and frostbite last exactly
as long as the gap to the next pulse and are drawn for twenty-six of those
thirty ticks.

**A tick number in a filename is a claim about the committed match**, and the
overtake has moved twice already — re-capture the pair whenever it does. The
caption is worth keeping attached to the *landmark* rather than to the number,
because a superseded tick is usually still a tick of the match, so a stale frame
goes on looking perfectly reasonable.

**`rendered-from.txt` is what the capture leaves behind saying which content it
drew.** A re-capture that comes out pixel-identical — which is what happens
whenever the content that moved is not in this particular frame — leaves nothing
for a date to see, so a date alone would call a current frame stale.
[`check-docs.ps1`](../../tools/check-docs.ps1) reads the record where the date
says no. It is written by the capture and never by hand; see
[`_rendered-from.ps1`](../../tools/_rendered-from.ps1).

## And six sheets that are not frames at all

- [`roster/beside-props-sheet.png`](roster/beside-props-sheet.png) — the four props that stand on the ground
  beside a tower, drawn through the real `TowerView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-expansion-beside-candidates.txt`. Ten tiles: the six
  `Color8` bare trees beside the Druid, then the turret, the ammo crate, the Paladin's statue and the Cleric's
  font beside theirs. **It is here because a size is the one thing a turntable cannot show.** The Druid's tree
  was picked from a turntable of the six trunk forms at uniform framing, which says nothing about how big a
  tree should be next to a standing man; this is the same six at the size the signed one is drawn at.

  **The trees come out dark grey-brown, and that is the model and not a broken import.** This pack ships
  the same `forest_texture.png` in all eight of its colourway folders — the eight files are byte-identical, so
  a colourway is where a model's UVs land on one shared sheet rather than a sheet of its own — and this
  model's trunk lands on a dark swatch. That the atlas binds, and binds the one in the model's own `Color8`
  folder, is asserted in `ImportedArtTests`. The Overgrowth line in [`roster.md`](../roster.md) described the
  weirwood as a cream-white trunk before anyone had rendered one; whether this is the tree that line meant is
  the question the sheet is asking.

  It draws no board, no roster row and no price — every character on it is a model no row in
  `content/units.txt` points at yet — which is why `check-docs.ps1` exempts it from being dated against the
  authored content. Regenerate it with the command above and copy `candidates-sheet.png` over it; the tool's
  own output name is deliberately not the committed one, because every candidate run rewrites it.

- [`roster/melee-lines-sheet.png`](roster/melee-lines-sheet.png) — the nine rungs of the Knight, the Barbarian
  and the Paladin, drawn through the real `TowerView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-melee-lines.txt`. Nine tiles, three lines of three, in
  the order [`roster.md`](../roster.md) writes them: Soldier, Sergeant, Shield Wall; Barbarian, Berserker,
  Slam; Paladin, Templar, Blessing. **It is here because a rung is told apart from the rung below it by
  colour, by a prop or by a second model**, and the earlier candidate sheet drew one character per model — so
  it could show whether the Barbarian was the right barbarian and could not show whether the Berserker reads
  as a rung above him.

  Each tile transcribes what `MatchSceneBuilder` and `Tests.Fixtures.ChosenArt` bind for that row, with one
  exception the set file names at its own head: **the three Paladin rows are bound with no clips**, because
  `roster.md` names a clip on every rung of the other two lines and none on any rung of that one. They stand
  in their bind pose in the game, which is not a photograph of anything, so on this sheet they are posed in
  the clips the earlier candidate sheets already put those two models up in. That is framing and not a
  binding, and the `_` on that line's windup and backswing still stands.

  **Two things on it are questions rather than results.** The Berserker's `axe_2handed_Large` measures 2.58 m
  from grip to head on a body about two tall, and lies level across him the way the Soldier's sword does —
  issue #204 looked at the sword and recorded it as reading correctly, and nobody has looked at an axe that
  size in the same position. And **a held prop keeps its own atlas while the body changes colour**, because a
  row's atlas covers the body and never what the body is holding: the `shield_square` on tiles two and three
  stays on the pack's base `knight_texture` while the Sergeant and the Shield Wall wear `alt_A` and `alt_B`
  over it, and the Blessing's `paladin_shield` stays gold on `paladin_texture_A` while its body turns silver
  on `_B`. That is the rule working as written, and whether it reads as one figure is the eye check.

  Like the sheet above it, it draws no board, no price and no roster row — a set sheet is drawn from its set
  file — which is why `check-docs.ps1` exempts it from being dated against the authored content. Regenerate
  it with the command above and copy `candidates-sheet.png` over it.

- [`roster/caster-lines-sheet.png`](roster/caster-lines-sheet.png) — the nine rungs of the Cleric, the Mage
  and the Druid, drawn through the real `TowerView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-caster-lines.txt`. Nine tiles in the order
  [`roster.md`](../roster.md) writes them: Cleric, Bishop, Consecration; Mage, Sorcerer, Unravel; Druid,
  Elder, Overgrowth. It is the melee sheet's question asked of the other three lines — whether a rung reads
  as a rung above the one below it once it is wearing its atlas and holding its props.

  **Every tile is a transcription, and this sheet needs no framing exception.** All nine rows are posed, so
  unlike the three Paladin rows on the sheet above, none of them had to be given a clip it is not bound with
  in order to photograph. What is shown is each row's idle.

  **The windup and backswing numbers are the one thing this pass was asked for and did not answer.**
  `roster.md` puts the `_` on the Cleric and Druid blocks and says the art ticket that picks a line's clips
  is where a real number is signed. This is that ticket, and how long a tower winds up is how it feels — a
  number a person signs and not one a binding table derives. The clips are bound; the two numbers are still
  open.

  **Six things on it are questions rather than results, and two of them are asks for
  [`roster.md`](../roster.md) rather than for the eye.**
  **The Bishop puts the tome down.** That page's tier-2 line names `Cleric_Mace` and does not say where
  `Cleric_Tome` goes; a hand holds one thing, so the mace takes the hand the tome was in, the way the
  Blessing's book takes the Templar's hammer's. Moving the tome to the off hand instead would be inventing an
  assignment rather than reading one. The visible consequence is that a *hitscan magic* bolt now leaves the
  head of a blunt weapon, because the anchor follows the prop.
  **The Elder is colour and nothing else.** That page says "tier 2 is colour plus a prop", and its own Elder
  block names `druid_texture_alt_A` and no prop, with `Open — none`. The row is bound as its block is
  written; the block and the rule disagree, and which gives is not this sheet's to settle.
  The **Lorekeeper's `Lorekeeper_Tome`** is a lectern rather than a hand book — it hangs off the fist and
  covers the body from the chest down, which is the whole silhouette of the Unravel tile.
  The **Mage's `spellbook_open`** comes to rest edge-on at the hip and reads as a closed book at the camera's
  fixed pitch, which is the same complaint issue #252 recorded about the staff the hat covers.
  The **Cleric's font** stands a tile away at knee height, 0.81 m against a body of 2.35, and whether that
  carries as a tier-3 signal at match framing is an eye check and not a measurement.
  And **the weirwood comes out dark grey-brown**, which is the beside-props sheet's finding above,
  unchanged: this pack's eight colourway folders hold byte-identical atlases, so a colourway is where a
  model's UVs land and not a sheet of its own.

  Like the two sheets above it, it draws no board, no price and no roster row — a set sheet is drawn from
  its set file — which is why `check-docs.ps1` exempts it from being dated against the authored content.
  Regenerate it with the command above and copy `candidates-sheet.png` over it.

- [`roster/pierce-turret-lines-sheet.png`](roster/pierce-turret-lines-sheet.png) — the nine rungs of the
  Archer, the Rogue and the Engineer, drawn through the real `TowerView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-pierce-turret-lines.txt`. Nine tiles in the order
  [`roster.md`](../roster.md) writes them: Archer, Ranger, Overwatch; Rogue, Cutthroat, Fan of Knives;
  Engineer, Artificer, Mortar. It closes the set of three — the other six lines are on the two sheets above —
  and asks the same question of these: whether a rung reads as a rung above the one below it once it is
  wearing its atlas and holding its props.

  **The Engineer line needed the framing exception the Paladin line needed.** Its three rows are bound with
  no clips, because `roster.md` names one on every rung of the Archer and Rogue lines and none on any rung of
  that one, so they stand in their bind pose in the game. They are posed here in `Idle_A`, which is the clip
  both earlier candidate sheets already put that model up in. That is framing and not a binding, and the `_`
  on that line's windup and backswing still stands.

  **Ten things on it are questions rather than results, and five of them are asks for
  [`roster.md`](../roster.md) rather than for the eye.**
  **The Artificer draws no ammo crate.** That page's tier-2 line puts one beside the turret, a tower has one
  beside slot, and that is written on the rung's own `Needs` line as a thing the engine would have to gain.
  The slot holds the turret, because the turret is what the Engineer's shell leaves from at every rung; so
  the Artificer is told from the Engineer by colour alone, which is thinner than the rule that page sets for
  a tier 2.
  **The Mortar's turret is not a heavier one.** That page asks for "a heavier `turret_base`", and
  `turret_base` is the only turret in the whole collection while size is retired as a tier signal everywhere
  else on the page — so there is nothing a heavier one could be bound to and it is the same prop at the same
  size. What "heavier" means is that page's to say.
  **The Overwatch is posed by one clip in all three of its states.** Its `Looks` line names a stance,
  `Ranged_2H_Aiming`, and no swing; a row is animated only when it carries three clips, so an idle on its own
  would leave that stance unreachable, and carrying the Ranger's bow draw and release up onto a body holding
  a crossbow would pose him with another weapon's action. What is unsigned there is a second clip.
  **The Rogue's throw is bound and never plays.** `roster.md` names `Throw` on the Rogue and
  `Melee_Dualwield_Attack_Slice` on the Fan of Knives, and those are attacks, so each is that row's windup —
  but every row on these three lines carries `windup 0 backswing 0` in `content/units.txt`, which is that
  page's `_` showing through. A tower that winds up for no time never enters the state, so the one clip the
  line's identity rests on is wired and unreachable on the board. The same is true of the Cleric's and the
  Druid's casts, landed one ticket earlier. The number is the ask, and it is the same ask that page's
  windup-and-backswing note has been carrying since 5 September 2026.
  **The Mortar's lobbing arc is already drawn, and it is not a Mortar signal.** That rung's `Looks` asks for
  "the lobbing arc drawn"; `ProjectileView` bulges every shell along one project-wide constant, so any row
  with `projectile` delivery already arcs — which the Engineer and the Artificer are too. Nothing on this
  sheet distinguishes the three, and whether the capstone wanted a heavier arc of its own is that page's to
  say.
  **The Marksman is a modern soldier.** The body comes with a respirator and green nightvision goggles under
  its hood, and the ghillie wrap is the whole of what tells this rung from the two below it. `roster.md`
  rejected `Marksman_Rifle` for putting the top of this line in a different century; the crossbow in his hand
  does not take that century off the rest of him, and whether it should is a question for whoever owns the
  art.
  The **crossbow is held one-handed.** `Ranged_2H_Aiming` is a two-handed aim and only the right hand carries
  anything, so the off hand comes up beside a weapon it is not on.
  The **Rogue's and the Cutthroat's dagger reads weakly** — it hangs at the hip and is mostly behind the leg
  at the camera's fixed pitch, which is the Ranger's quiver complaint again. The Cutthroat is a hood and
  nothing else against the Rogue, so at that rung the body is carrying the whole read on its own.
  The **Fan of Knives throws from one of two identical hands.** He carries two daggers, both named after the
  same asset, so the anchor that names `dagger` resolves to whichever the lookup reaches first — a point on
  the art either way, but which hand the three knives leave from is not decided by anything.
  And the **Engineer's wrench reads weakly for the same reason**, at every rung. It costs this line little,
  because the turret beside him is loud and is what the shell leaves from — but a player looking at the man
  is not looking at what fires.

  Like the three sheets above it, it draws no board, no price and no roster row — a set sheet is drawn from
  its set file — which is why `check-docs.ps1` exempts it from being dated against the authored content.
  Regenerate it with the command above and copy `candidates-sheet.png` over it.

- [`roster/creep-bodies-sheet.png`](roster/creep-bodies-sheet.png) — the six creep bodies of ids 39 to 46,
  drawn through the real `CreepView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-creep-bodies.txt`. Ten tiles: the Bone Golem, the
  Black Knight, the Frost Wight and the Abomination on the top row with the Fiend and the Shade, then the
  Shade's other three atlases and the Frost Wight's other axe on the second. **It is the first sheet of
  creeps**, and the first drawn through the walking view rather than the standing one — every tile is posed
  halfway through the walk cycle its row is actually bound with, so nothing here is a framing clip.

  **Four of the six are on the Large rig and take that rig's walk.** `Walking_A` and `Death_A` are in both
  rigs' banks, so a Large body handed the shared medium pair drives bones that skeleton has not got and
  slides down the corridor in its bind pose. `UnitArt` gained a per-row walk and death for that, and the
  Golem, the Black Knight, the Frost Wight and the Abomination name
  `Rig_Large_MovementBasic/Walking_A` and `Rig_Large_General/Death_A` in both binding tables, and
  `EveryClipComesOutOfTheBankForItsRowsRig` holds each of the four to naming one at all — the silent failure
  here is not a misspelt name but an override nobody wrote.

  **Five things on it are questions rather than results.**
  **The four Large-rig bodies are as tall as the towers, and that broke a test.** At the signed creep scale
  of 0.5 the Black Knight draws **2.56 m**, the Bone Golem 2.32, the Abomination 2.24 and the Frost Wight
  2.09, against a shortest tower — the Unravel — of **2.31 m**. `EveryCreepStandsUnmistakablyLowerThanEveryTower`
  asks for a fifth of clear air and is red over it. Nothing here is miswired: these four are the pack's
  size-up rig, authored at 4.2 to 5.1 m where a medium character is 2.3 to 2.9, and half of a size-up is a
  tower. [`roster.md`](../roster.md) signs two multipliers and no exceptions and gives the 0.5 the reason
  *"a creep is unmistakably smaller than the thing shooting it, at any camera angle"* — which these four do
  not satisfy. Which moves is that page's call and not this sheet's.
  **The Shade's atlas is unpicked.** That row asks for "the darkest of the pack's four" and names none of
  them. The bound tile is `ninja_texture_A`, the sheet the model imports wearing: black gi, red sash, bare
  tan face and forearms. The three below it are `B`, `C` and `D` on the same body — blue over brown, green,
  and orange over tan. Which reads as a silhouette at gameplay distance is the answer, and whole-atlas mean
  luminance (116, 118, 123, 135 for A to D) is not it: an atlas is a swatch sheet and how dark a body comes
  out depends where its UVs land.
  **The Frost Wight's axe is the medium export, as signed.** That row names `FrostGolem_Axe` where the other
  three Large bodies are each signed for their own `_Large` weapon, and the pack ships
  `FrostGolem_Axe_Large` too. Both are on the sheet, and `EverythingHeldIsOnItsBoneAndBigEnoughToSee`
  measures the difference: the medium axe spans **30%** of the body holding it, against **52%**, **54%** and
  **47%** for the Golem's axe, the Black Knight's sword and the Abomination's barndoor. It clears the test's
  tenth, so nothing fails — it is simply the smallest thing any of these four carries. At this camera it is
  largely behind the body either way.
  **The Fiend's backpack is held rather than worn.** `Tiefling_SwordsBackpack` is a scabbarded pair authored
  for the back and this project has two bone sockets rather than three — the same gap
  [`roster.md`](../roster.md) names on the Ranger's quiver. So it hangs off the melee hand and reads as a
  bundle of blades at the hip, in front of the body, rather than as anything worn. The pack's own
  `Tiefling_Sword` is what a hand would take.
  **The Fiend is the brightest thing on the sheet.** Red skin, teal hair and a white vest, against a theme
  that page states as *undead, and the dark or hooded*. That row already argues the licence — it is the dark
  half rather than the undead half — and whether the colour carries it is an eye check.

  Like the four sheets above it, it draws no board, no price and no roster row — a set sheet is drawn from
  its set file — which is why `check-docs.ps1` exempts it from being dated against the authored content.
  Regenerate it with the command above and copy `candidates-sheet.png` over it.

- [`roster/creep-bodies-rest-sheet.png`](roster/creep-bodies-rest-sheet.png) — the last six creep bodies,
  ids 38, 43, 44, 47, 48 and 49, drawn through the real `CreepView` by
  `tools/capture-armed-roster.ps1 -SetFile docs/roster-creep-bodies-rest.txt`. Nine tiles: the Necromancer,
  the Vampire, the Witch, the Cursed Villager, the Werewolf and the Grave Robber on the top row, then the
  Grave Robber holding its backpack, the Witch with her broom turned upright and the Necromancer with his
  scythe turned the same way on the second. **With these bound the stand-in list is empty** — every row of
  `content/units.txt` draws its own model, and no row is drawn as the Prototype Dummy any more.

  All six are on `Rig_Medium`, so every tile takes the shared bare `Walking_A`; the four bodies that had to
  name the Large rig's own banks are on the sheet above. Every tile is posed a quarter of the way through
  that walk, which is the clip the match draws these rows with, so nothing here is a framing choice.

  **Seven things on it are questions rather than results, and three of them are asks for
  [`roster.md`](../roster.md) rather than for the eye.**
  **The Grave Robber's body already wears its pack.** That row is signed "`Hoarder`, wearing
  `Hoarder_Backpack` — the backpack, not the sword", and `Hoarder.fbx` carries `Hoarder_Backpack` as a
  skinned piece of itself: one of that body's thirteen materials is the pack, and the bedroll and hip pouches
  come with it. The pack ships the same piece again as a model of its own, and tile seven is what that model
  does in a hand — a second backpack that covers the body from the chin down and hides the face entirely. So
  the row is bound with empty hands. Whether the worn pack reads loudly enough for a row whose whole
  mechanic is the pack is the eye check.
  **And that body wears a sword as well, which the same line does not want.** "A sword on a creep that never
  attacks reads as a lie" is why that row picks the pack over `Hoarder_Sword` — but
  `Hoarder_FrontPouch_Sword` is another of the thirteen materials, sheathed at the belt, and it cannot come
  off without editing a model. Nothing was put in a hand; the sword on the tile is the one the character was
  authored with. Whether that is the lie the line meant is that page's to say.
  **The Witch's broom lies along the arm.** `roster.md` names "`Witch`, `Broom`" and no turn, and this
  collection authors a shaft along the hand bone's local +Y — the same measurement that put the Mage's staff
  flat with its orb by the feet. Bound at the bone's own rotation, the bristles come to rest out beside the
  hip and read as a bundle. Tile eight is the same broom with the quarter turn the three staffs carry, and it
  stands on end with the bristles at the floor, unmistakably a broom. Which is right is the developer's call
  the way the staffs' was — `MatchSceneBuilder`'s own note on that tilt says to change a read like this on a
  ticket and not by noticing the paragraph.
  **The Necromancer's scythe is flat for the same reason**, blade curling up in front of the chest and shaft
  out level behind. `Skeleton_Scythe` measures 2.09 m along that same bone axis against 1.34 across, and
  `roster.md` names no turn on that row either. It is the largest thing any of these six carries at 69% of
  the body and it does read; tile nine is the turned version, so the broom and the scythe can be answered
  together.
  **The Cursed Villager's axe is nearly invisible.** It lies level across the chest with the head edge-on and
  the haft behind the arm, so at this camera it reads as a pale sash rather than as a weapon. It measures 46%
  of the body, so nothing fails; it is the Rogue's dagger and the Engineer's wrench complaint again.
  **The Villager and the Werewolf are the same figure in the same clothes.** Both draw at 1.25 m, both wear
  the red shirt and the blue jeans, and the wolf head is the whole of what tells them apart — which is
  exactly right for a pair [#267](https://github.com/ssalter21/tower-defense-game/issues/267) will join, and
  is worth knowing before that ticket, because at gameplay distance a player is being asked to read a head.
  **Three of these six are neither undead nor dark nor hooded.** The Witch is an orange pointed hat over a
  green skirt, and the Villager and the Werewolf are a red shirt and jeans. `roster.md` states the theme as
  *undead, and the dark or hooded*, and argues the licence on the Fiend's row rather than on any of these
  three. Whether a village girl in orange belongs in that wave is an eye check, and the Necromancer, the
  Vampire and the Grave Robber beside her are not the problem — they are bone, black-and-red and hooded
  respectively.

  Like the five sheets above it, it draws no board, no price and no roster row — a set sheet is drawn from
  its set file — which is why `check-docs.ps1` exempts it from being dated against the authored content.
  Regenerate it with the command above and copy `candidates-sheet.png` over it.

The rest of the default set is regenerable and not committed — and that is
arranged by [`.gitignore`](.gitignore) rather than by whoever runs the capture
next remembering to delete four files. Left to memory, a plain
`tools/capture-match-frames.ps1` leaves them in the tree and the build gate's
last step fails the next push for a dirty repository.

## What to look at

Both frames were worth capturing before they were worth committing: the first
capture of this ticket is what found the hitscan towers lying on their side on
the road. Every test in the suite was green at the time — the atlas had bound,
the mesh was there, the tower fired on schedule — because the view was forcing
each model's root rotation to identity, which throws away the axis-conversion
rotation an FBX root can carry. The characters' roots happen to be identity, so
they stood up perfectly and hid it.

That is the argument for these existing at all, and it is why the fix ships with
a test (`EveryModelStandsTheWayItWasImported`) rather than only with a picture.
