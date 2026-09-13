# A beside prop whose tile is taken stands on a free neighbour

What the Mortar's turret, the Consecration's font and the Overgrowth's weirwood do when a tower is standing
on the tile they ask for, drawn through the real match at the framing the game is played at. Issue
[#282](https://github.com/ssalter21/tower-defense-game/issues/282), question 4, rendered three candidates;
sitting [#284](https://github.com/ssalter21/tower-defense-game/issues/284) signed **the free neighbour** on
11 September 2026 and the rule is built — `View/BesideStanding.cs`. What is here now is the rule acting on
the board that was built to make it act.

```powershell
./tools/capture-beside-props.ps1
```

## The rule

`BesideProp.Offset` is where a row's prop would like to stand — one tile to its tower's right, in the frame
the tower rests in, for all four signed props — and nothing in the art knows what is standing there. With
towers in a row #270 played it: all three props were drawn inside the neighbouring tower's hex. The board is
the one thing that knows what stands where, so the two views that draw one (`BuildBoard`, `MatchView`) ask
`BesideStanding.On` and hand `TowerView` the answer:

- **The asked tile, while nothing is on it.** Snapped to the cell's centre, at the cell's own height, so a
  prop on a raised neighbour stands on it rather than floating beside it.
- **Otherwise a free ground neighbour**, ranked by how far toward the asked direction it lies, with a
  smaller weight for lying away from the corridor — so the cell sixty degrees behind the taken one beats the
  one sixty degrees in front, and both beat the far side. The corridor is never a candidate.
- **With no free neighbour, inside the tower's own hex**, 0.8 m sideways — the fallback the sitting did not
  choose as an answer, kept as the thing a prop does when there is nowhere else.
- **An offset the art keeps inside the hex is left as asked**, since it reaches no neighbour.

In build mode the rule runs on every board change, because the tower placed this click is what makes a
neighbour's asked tile taken. `Tests.EditMode/BesideStandingTests` holds each line above.

## The board

[`../beside-props.txt`](../beside-props.txt) stands the three capstones with a Mage on the exact tile each
prop asks for, three more Mages around each, and two neighbours of each left free. Which tile a prop asks for
was **computed from the resting facing** (`RoutePath.FacingFrom` — the nearest point of the route) rather
than guessed, because "one tile to its right" is a different board cell for every tower:

| capstone | stands at | faces | asks for | free neighbours |
|---|---|---|---|---|
| the Mortar | (9, 3) | (0.50, −0.87) | (8, 3) | (10, 2), (10, 4) |
| the Consecration | (13, 3) | (0.00, −1.00) | (12, 3) | (14, 2), (14, 4) |
| the Overgrowth | (12, 9) | (0.00, 1.00) | (13, 9) | (13, 8), (13, 10) |

The ring is Mages because Archers did not reach: an Archer's 3200 is three hexes on the flat and the loader
refused `(9, 2)` because the climb between levels is counted against reach.

## The frames

`played/wide/` is the framing the built player uses and the one that decides; `played/close/` is a
`-Distance 22` pass. The close pass earns its place on this board: the rig crops toward the middle of the
corridor, and the three clusters stand in the upper and right middle, so every one of them is in the close
frame with the prop legible. Both are committed — see [`.gitignore`](.gitignore).

Frames are named after the board (`beside-props-tick-*.png`), which is `capture-match-frames.ps1`'s own
rule. `rendered-from.txt` beside each set is written by the capture and dates the frames against the content
they drew.

## What the candidates measured, kept so nobody measures it again

The three alternatives #282 drew — the prop tucked 0.8 m inside its own hex, the prop on a free neighbour
(spelled by hand through the `stand` directive), and no prop at all — differed from the next-tile baseline by
this much, counted at a threshold of 24 on any channel:

| candidate | wide, whole frame | close, whole frame | close: Mortar / Consecration / Overgrowth |
|---|---|---|---|
| tucked-in | 0.11 % | 0.52 % | 1538 / 2075 / 3845 px |
| free-neighbour | 0.13 % | 0.40 % | 2470 / 1998 / 1232 px |
| no-prop | 0.07 % | 0.25 % | 1106 / 1418 / 1036 px |

Three findings came off those pictures and still stand:

- **A beside prop moving or vanishing changes 0.07–0.13 % of the 1× frame** — the same order as the
  Mortar's turret at twice its size, and well under the 1–2 % anything drawn on the ground moves. At the
  framing the game ships this question is decided at magnification or not at all.
- **The Overgrowth's weirwood is hidden by its neighbours' hats.** The purple dome that reads as a tree in
  the wide frame is a Mage's hat seen from above on a raised tile; the weirwood is a small bare tree behind
  it. Whatever tile it stands on, the prop that tells an Overgrowth from an Elder is one a crowd hides.
- **No prop would have cost the Mortar a second change**: his shells leave from a node inside the turret.
  Not taken, and not needed now.
