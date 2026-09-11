# Candidates for a beside prop whose tile is taken

What the Mortar's turret, the Consecration's font and the Overgrowth's weirwood could do when a tower is
standing on the tile they stand on, drawn through the real match at the framing the game is played at.
Issue [#282](https://github.com/ssalter21/tower-defense-game/issues/282), question 4, on the roster
sign-off map. **Nothing here decides anything** — AGENTS.md rule 6 puts anything a player sees on the
human side of the line, and this renders the alternatives and stops. Signing one is
[#284](https://github.com/ssalter21/tower-defense-game/issues/284)'s.

```powershell
./tools/capture-beside-props.ps1
```

## The question

`BesideProp.NextTile` puts a prop one column pitch to its tower's right, in the frame the tower rests
in — sideways rather than forward so a prop never stands on the corridor. Nothing says what it does
when the tile beside is occupied, and #270 played it: with towers in a row, all three props were drawn
inside the neighbouring tower's hex.

## The board

[`../beside-props.txt`](../beside-props.txt) stands the three capstones with an Archer on the exact
tile each prop lands on, three more Archers around each, and two neighbours of each left free. Which
tile a prop lands on was **computed from the resting facing** (`RoutePath.FacingFrom` — the nearest
point of the route) rather than guessed, because "one tile to its right" is a different board cell for
every tower:

| capstone | stands at | faces | prop lands on |
|---|---|---|---|
| the Mortar | (9, 3) | (0.50, −0.87) | (8, 3) |
| the Consecration | (13, 3) | (0.00, −1.00) | (12, 3) |
| the Overgrowth | (12, 9) | (0.00, 1.00) | (13, 9) |

## The candidates

One file each, applied through `-matchFrameArt` to a copy of the wired art for the length of one run.
`MatchSceneBuilder`, `MatchArt.asset`, `BesideProp.NextTile` and the generated scene are untouched.

| file | what it draws | what it would cost to build |
|---|---|---|
| *(none — the baseline)* | the prop on the next tile whatever is standing there | nothing; this is what ships |
| [`tucked-in.txt`](tucked-in.txt) | the prop 0.8 m sideways, inside its own hex | one number in `BesideProp.NextTile` |
| [`free-neighbour.txt`](free-neighbour.txt) | the prop on a neighbour nothing stands on | a view that knows the board — `BesideProp` would need what stands around it; the file spells the rule's **outcome** for this board by hand and builds no rule |
| [`no-prop.txt`](no-prop.txt) | nothing beside the tower | the three capstones told from their rung below by colour and pose alone |

**`stand` is the directive that made the first two drawable.** `UnitArtFile` could move *what* stood
beside a row (`beside`) and refused an offset on a lone prop, because an offset there was a change to
the socket and not to the candidate. This question is precisely about the socket, so `stand <id> <dx>,<dz>`
moves where the row's prop stands — metres from the tower's root, sideways then forward, in the frame it
rests in. `BesideProp.Standing` already carried an offset; nothing had ever set one.

## The frames

`played/wide/` is the framing the built player uses and the one that decides; `played/close/` is a
`-Distance 22` pass. **The close pass earns its place on this board where it did not on #281's**: the
rig crops toward the middle of the corridor, and the three clusters here stand in the upper and right
middle rather than in a corner, so every one of them is in the close frame with the prop legible.

## What the frames say, measured

Pixels that differ from the baseline at the same tick, counted over the whole 1600×900 frame and
inside each cluster's box, at a threshold of 24 on any channel:

| candidate | wide, whole frame | close, whole frame | close: Mortar / Consecration / Overgrowth |
|---|---|---|---|
| tucked-in | 0.11 % | 0.52 % | 1538 / 2075 / 3845 px |
| free-neighbour | 0.13 % | 0.40 % | 2470 / 1998 / 1232 px |
| no-prop | 0.07 % | 0.25 % | 1106 / 1418 / 1036 px |

Three findings came off the pictures rather than out of the ask:

- **Every candidate sits in #281's band, not #280's.** A beside prop moving or vanishing changes 0.07–0.13 %
  of the 1× frame — the same order as the Mortar's turret at twice its size (0.19 %) and well under the
  1–2 % anything drawn on the ground moves. At the framing the game ships this question is decided at
  magnification or not at all, which is why the close pass is kept.
- **The Overgrowth's weirwood is hidden by its neighbours' hats.** The purple dome that reads as a tree
  in the wide frame is a Mage's hat seen from above on a raised tile; the weirwood is a small bare tree
  behind it, visible between two hat brims in the close baseline and gone in `no-prop`. Removing it
  changes 194 px of the wide frame. Whatever is decided about the tile it stands on, the prop that tells
  an Overgrowth from an Elder is already the one a crowd hides.
- **`no-prop` costs the Mortar a second change.** His shells leave from the turret's muzzle — the anchor
  is a node inside the beside prop — so the capture refuses a turret-less Mortar by name until the anchor
  is moved; the candidate moves it to the tip of his wrench. The Consecration and the Overgrowth anchor
  on their bodies and pay nothing.

And one about the board: **the ring is Mages because Archers did not reach.** An Archer's 3200 is three
hexes on the flat and the loader refused `(9, 2)` because the climb between levels is counted against
reach. The refusal is the loader's own sentence, and it is why the ring is not the cheapest tower.

Frames are named after the board (`beside-props-tick-*.png`) for the baseline and after the candidate
for the rest, which is `capture-match-frames.ps1`'s own rule. `rendered-from.txt` beside each set is
written by the capture and dates the frames against the content they drew.
