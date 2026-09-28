# The board-smoothing sheet

Two ways of smoothing the board's steps, rendered on the committed board beside the board as it ships, each
bare and under each legibility aid, from two cameras. **A prototype for one decision**, issue #329 on the
[board-smoothing map](https://github.com/ssalter21/tower-defense-game/issues/315): which candidate ships, and how
a level stays readable on it. Nothing here is the game. The code that draws the candidates lives in
`client/Assets/Editor/SmoothingCandidates.cs` and is torn out once the choice is made; regenerate the pictures
with `tools/capture-smoothing-sheet.ps1` (editor closed — it is batchmode).

**Every frame is the same board.** `content/map.txt`'s 247 cells, its 51-cell corridor and the level of every
cell are read through `HexMap.ParseUtf8` and never touched, so anything that differs between two frames is the
ground's surface and nothing that differs is the playfield. The trace, the landmark table and the golden do not
move. The scenery is the shipped dressing in every frame too, which matters below.

## Read the sheets first

| File | What it is |
|---|---|
| `sheet-match.png` | Nine frames from the shipped camera, at half size. **Judge the look here** — this is what a player sees. |
| `sheet-plan.png` | The same nine from straight above. **Judge legibility here** — from overhead, height is invisible and the aids carry the whole reading. |
| `check-match.png`, `check-plan.png` | The same nine, cropped around the corridor's steepest cell at twice the size. The render checked against itself: the level change at that cell has to be visible in every one. |
| `<variant>-<camera>.png` | The eighteen frames at 1600x900, under the names below. |
| `smoothing.txt` | What each candidate cost, counted off the meshes by the capture rather than typed; the cell the checks crop; the cameras. |

Both sheets and both checks read the same way, left to right, top to bottom:

```
today            skin-plain       pieces-plain
skin-contour     skin-band        skin-both
pieces-contour   pieces-band      pieces-both
```

The plan camera stands in for `tools/render-map.ps1`, which draws the board as a top-down SVG and has no Unity
camera of its own.

## The two candidates

**The skin** is one mesh over the whole board. Every ground cell keeps a flat plateau at its own height, shrunk
to six tenths of the hex, and everything between plateaus is planar slope: a rectangle between two
neighbouring plateau edges, a triangle where three meet. The corridor keeps the pack's road pieces and the skin
runs up to each at the height the piece actually has there, ramps included. The board's rim banks down by the
dressing's rim drop and hangs a skirt of earth. It is flat shaded on purpose, so a plateau reads flat; smooth
normals are one line if anyone wants the blob.

**The pieces** keep one tile per cell. A ground cell lifts the corners it shares with a neighbour one level up
to that neighbour's height, so the slope lives in the lower cell and the higher cell stays a flat plateau —
which is how the pack's own ramp works. A neighbour two or more levels up is left as a cliff, though this board
has none. A cell with nothing lifted and no rim edge wears the pack's flat grass tile unchanged; every other cell
is an authored piece, and **the census of distinct pieces is the candidate's cost**: on this board, thirteen
shapes (rotations counted once), of which the pack ships none — its slopes are planar and lip a quarter level at
their sides, which is the disagreement at three-way corners the sheet exists to remove.

Both wear the grass and earth swatches sampled off the pack's own grass tile, so their colours are whatever
atlas the board is wearing — the Summer one.

The costs in words, for the sitting; the numbers are in `smoothing.txt`:

| | Skin | Pieces |
|---|---|---|
| Meshes | One, plus the 51 road tiles it runs up to | One per ground cell: 50 pack tiles, 146 authored pieces of 13 shapes |
| When the map changes | Nothing to author; regenerated at scene-build | Generated here, so nothing; hand-authored in the pack's style it is one model per new shape, and a new map can need shapes this one never does |
| The cliff posts | Gone; the rim is a bank of earth | Gone between cells; the rim keeps the tile's metre of body |
| Looks like | One landscape | The tile set |
| Scenery | Stands where it stood; a prop placed off a cell's centre can sit on a slope | Stands where it stood |

The board today draws **no cliff posts at all** — every step on it is one level, and the rim drop is shallower
than a tile body — so what the candidates remove is the vertical earth face of every raised tile, not a post.

## The two aids

**Contour**: a dark ribbon a tenth of a metre wide along every edge between two cells of different level, laid on
the candidate's own surface — mid-slope on the skin, along the top of the bank on the pieces. 268 such edges on
this board.

**Band**: one tint per level over the atlas, walking blue-green to yellow-orange from the lowest level in use to
the highest, on ground and road alike. It is multiplied over the atlas, so it takes colour away from everything
else; that is the cost the ticket names, and the band frames show it.

## What the frames do not decide

**The mounds.** The raised blocks with earth sides that stand in every frame, including the smoothed ones, are
not ground: they are the dressing's ridge mounds, placed by `BoardDressing.asset` on the lip of a drop so that
the step between two levels has something growing over it. On a smoothed board they are the one jagged thing
left. Whether they stay once the drops they mark are gone is a dressing decision the sitting may want to take
beside this one; the sheet leaves them in so the two candidates are compared under the shipped dressing and
nothing else.

**Picking.** Neither candidate changes what a cell is or where a tower stands; `HexPicking` reads the map, not
the mesh, and the skin's plateaus and the pieces' flat tops are both at the cell's own height.
