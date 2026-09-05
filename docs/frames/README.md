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
```

**A frame is a function of its tick and nothing else.** The capture draws every
tick it steps through rather than only the ones it keeps, because where a tower
is pointing and where its shot leaves from are both read off the pose it was
last drawn in. It used to draw only at the kept ticks, which made a sparse tick
list a photograph of a match whose towers had never moved — and, once effects
started leaving the model rather than a fixed height above it, put the muzzle
flash on a rig still standing in its bind pose.

## What is committed

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

## And three sheets that are not frames at all

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
