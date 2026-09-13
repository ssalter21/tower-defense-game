# What each committed frame shows

The long captions for the nineteen frames [`README.md`](README.md) indexes: what each was captured to show,
what in it is a result and what is a question, and the framing it was taken at. Every ruling quoted here is
[the decision log's](../decision-log.md); every placeholder named here waits on the billboarding prototype.

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
  for every tower. The staff has since gone — row 4 holds the spellbook, and on
  11 September 2026 the flash was moved to leave the point of the hat, the one
  place seen from behind that puts an orb in frame at all
  ([`mage-anchor/`](mage-anchor/README.md)); this frame is the picture of the
  staff it replaced.

The last two are the pair the effect anchors landed with: before them every
tower fired from one fixed height above its own root, whatever it was holding.

And one that is **not** a frame of the recorded match:

- `effects-roster-tick-0700.png` — **a placeholder, and the thing it is
  showing has not been signed.** The pool standing in front of a creep's health
  is a second segment of a bar above it. A creep the snapshot says is slowed
  used to be washed in one colour as well; that came off on 7 September 2026,
  so what a slow looks like here is the Shield Wall's circle and nothing on the
  body. Captured with `-Distance 20 -Width 1600` against
  `effects-roster.txt`, which is the shipped roster with a
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
`four-lines.txt`, all at `-Width 1600`. **Three of the four are the same shape now.** Issue #263 signed a
ring for the Shield Wall's slow, a shock across the ground for the Slam, a glow
on every tower the Blessing reaches and a burst at the radius the Mortar landed
in; Sam replaced the first three with one flat translucent circle at the reach
on 7 September 2026, because all three are auras. The Mortar's burst is a blast
and keeps its shards. Every colour and duration is still the plainest thing that
draws it, declared as a placeholder in `MatchTuning`, and so is how see-through
the circles are.

- `four-lines-tick-0813.png` — **the one to look at first**, at `-Distance 22`,
  close enough to read what each shape is made of. Three signatures at once: the
  pale blue circle on the left is the Shield Wall's slow, lying on the ground at
  the one hex it carries; the warm circle under the Barbarian on the Large rig
  is the Slam's swing landing on everything touching him; and the gold circle on
  the right is the Blessing's, covering itself and the Templar standing one hex
  away. The Paladin, six hexes off, is outside it — that is the aura's reach and
  not an oversight, and **it is the only thing on screen that says which towers
  got the blessing**, because nothing is drawn on the towers themselves.

  **This frame is why the alpha is 0.45 and not 0.28.** The gold circle is a warm
  colour on a warm floor, so it was the faintest of the three and close to
  invisible at play size while the two cold ones read cleanly. At the signed
  alpha it carries a readable edge. It is still the softest of the three, which
  is a fact about the colour rather than the number — see
  [`effect-candidates/`](effect-candidates/README.md) for the bracket it was
  chosen from.

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
lines** standing on it, out of `pierce-lines.txt`, all at
`-Width 1600`. **Every shape in them is a placeholder and none of it is
signed.** What issue #264 signed is two shapes — the Overwatch's single shot
drawing a tracer the length of the leg it crossed, and the Fan of Knives
throwing three knives at three bodies — and every colour, size and duration they
are drawn at is the plainest thing that draws that shape, declared as a
placeholder in `MatchTuning`.

- `pierce-lines-tick-0673.png` — **the one to look at first**, at
  `-Distance 18`, and it shares its list with 0516: capturing 673 on its own
  reproducibly gives other bytes, which is the coupling this section names.
  The Fan of Knives, hooded in blue, has just thrown: three
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
Druid lines** standing on it, out of `magic-lines.txt`, all at
`-Width 1600`. **Every colour, size and duration in them is a placeholder and
none of those is signed.** What issue #265 signed is four shapes — a bolt leaving the tome or the
staff tip, the Consecration's light on the ground, the Overgrowth's roots on
every hex it slows, and the Unravel's armour strip on the hex his bolt landed
on. **Two of those moved on 7 September 2026**: the Consecration's light is the
one flat translucent circle every aura now draws, and the Overgrowth draws
nothing at all, because its sixty-hex reach makes a circle at its radius a
screen washed flat. The bolt and the strip are a shot and a blast, so neither
was touched. Every colour and duration is still the plainest thing that draws
the shape, declared as a placeholder in `MatchTuning`.

- `magic-lines-tick-0344.png` — **the one to look at first**, at `-Distance 22`.
  The violet band broken into plates, lying on the ground around the skeleton at
  the left of the light, is the Unravel's armour strip, drawn on the tick his
  bolt arrived. The three pale bars in the air are the bolts fired two ticks
  earlier, two ticks into a five-tick crossing. The wide pale circle under the
  towers is the Consecration's light. **Nothing marks the bodies the Overgrowth
  is holding** — that aura draws no decoration at all now, so what says the
  board is held is the creeps not moving.

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
  font; the Overgrowth is **nowhere in this picture**, which is the point of
  keeping the frame — it reaches sixty hexes and draws nothing, so a board-wide
  hold has to read through the creeps not moving. **The orange burst in the
  middle of it is open question 8 in one picture**: it is the Mage's or
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
an aura or a pool** walking it, out of `creep-auras.txt`, all
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
  circles are the Necromancer's ward, three of them overlapping, at the two hexes
  it grants a pool across; the green circle blending with them is the Skeleton
  Mage's haste; and the violet ones are the Witch's hex ward, out to two hexes
  each. **Overlapping circles are what this frame is for**: three translucent
  surfaces blending is the case the alpha is judged on, and it is the reason
  nothing is drawn on the bodies — three shapes stacked on one walking creep is
  exactly what this replaced. The green-and-blue bars over the bodies are **not** an
  effect of this ticket: they are the two-segment bar #254 already draws, and
  the blue half is a pool — some of it the Vampire's and the Grave Robber's own
  and some of it what the Necromancer just granted. **The Minions in the knot
  were not sent**: the wave releases six rows and none of them is a Minion, so
  every one on screen is a body a Necromancer raised.

- `creep-auras-tick-0276.png` — the same knot at `-Distance 14`, down among the
  bodies, four ticks later. What is worth reading here is that **nothing hangs
  above the bodies inside the aura**: the circle they are standing in is the
  whole of what says the haste reached them, and the only thing over a creep is
  the pool bar, which is not an effect.

- `creep-auras-tick-0271.png` — the whole-floor framing, so the reach of all
  four is in one picture against a board nineteen hexes wide. Two hexes is what
  every creep aura on the roster carries, and this is what two hexes looks like
  from where a player sits.

- `creep-auras-tick-0094.png` — **the Frost Wight's frostbite**, at
  `-Distance 18`, which the three frames above cannot show. The pale circle
  lying across the Archers standing among the wave is it: frostbite is the one
  aura on the roster whose `affects` column reaches the *other* side. The
  question this frame asks is whether a tower inside that circle reads as
  frostbitten, because **the circle is the whole of what says so** — nothing is
  drawn on a body an aura found, on either side of the board.

**Three things about them are results rather than questions.** **The two rows
with a pool of their own draw no effect at all**, and that is what #254 already
built rather than an omission: a pool is a `CreepSnapshot` field and not a
moment, which is why it survives a scrub, and the Vampire's blood and the Grave
Robber's pack are the blue segment of the bar over the body in every frame here.
**Nothing on a body says a modifier is in force**, which stopped being a
question on 7 September 2026 and became the answer: the wash came off with every
other mark on a body an aura found, so the circle on the floor is the only thing
that says anything happened. And **the Necromancer's ward is a moment where the
haste is a state**: the ward's duration is zero, so its circle stands ten ticks
and the pool it granted goes on being drawn on the bar, where haste, hex ward and
frostbite last exactly as long as the gap to the next pulse and are drawn for
twenty-six of those thirty ticks.

**A tick number in a filename is a claim about the committed match**, and the
overtake has moved twice already — re-capture the pair whenever it does. The
caption is worth keeping attached to the *landmark* rather than to the number,
because a superseded tick is usually still a tick of the match, so a stale frame
goes on looking perfectly reasonable.
