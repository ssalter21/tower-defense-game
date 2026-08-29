# Landscape prototypes

Six landscapes over one road, each a reading of one reference frame from
[the KayKit board](../../research/what-makes-the-board-read-flat.md), plus the committed board as a control.
Regenerate with `tools/capture-prototypes.ps1` (editor closed — it is batchmode).

**Every board is the same corridor.** `content/map.txt`'s 51 cells, unmoved, cell for cell. What differs is
the height map under them, the atlas they wear and how heavily they are dressed. The boards themselves are in
[`../boards/`](../boards/) and are parsed by `HexMap.ParseUtf8` like any other map, corridor assertion
included — which is what proves each one really is the committed route.

| Preset | Reference | The idea |
|---|---|---|
| `as-it-ships` | none — the control | Three heights, every change of height a whole block, no half step anywhere. |
| `ridge-lake-road` | *Ridge, lake, road* | Road low and unbroken through the middle, high ground on one flank, a lake eating a corner. |
| `signature-strip` | *The signature composition* | One continuous climb from the near corner to the far one; everything tall at the far end. |
| `rolling-country` | *The best landscape render in the collection* | Gentle relief and heavy wood — the interest is what stands on the ground, not the ground. |
| `three-deep-cliff` | *Three-deep cliff layering* | Three flat shelves parted by whole-block faces, with the wood pushed onto the lips. |
| `canyon-run` | *Canyon variant* | The road on the floor of a trench, walls stepping away, autumn atlas. |
| `diorama-plate` | *Clay render*, *Diorama scale* | A low plate with four vertical incidents and very little else. |

- `presets.txt` — the numbers behind each frame, and a relief census counted off the map it was drawn from.
- `board-<preset>-high.png` — the shipped camera framing.
- `board-<preset>-low.png` and `-raking.png` — **judge the terrain here.** A cliff face is close to invisible
  from the shipped pitch, which is exactly why the shipped pitch is not where the board was found to be flat.

## What was learned drawing these

**The shipped atlas is the wrong one.** `hexagons_medieval.png` maps the grass tiles to an olive-yellow that
reads as scorched at board scale; `hexagons_medieval_Summer.png` is the same geometry against the same UVs
with an actual green in that swatch. Five presets name an atlas and only the control wears the shipped one, so
the difference is visible in one glance across the contact sheet. This is a one-line change to
`Materials/Tiles.mat` and is the largest single improvement here.

**`hex_grass_bottom` is not the cliff piece, despite the name.** A drop deeper than the metre of earth a tile
carries needs a column stacked under it, and that piece is mapped to the grass swatch on every side — a stack
of them draws a column of lawn under the ridge. `hex_grass` has the metre of earth and its green cap is buried
inside the tile above it, so it is the piece, and the pack's own bottom tile is not imported.

**A pillar is not a hill.** A single hex standing two levels clear of everything touching it reads as rubble,
and it is also the shape that stops a slope tile fitting — a slope needs a low side and a high side facing
each other, and a pillar has six low sides. Four of the six boards run a pass that files those off. The two
that do not are the two whose whole point is a hard edge.

The presets live in `client/Assets/View/SceneryPresets.cs` and exist to be compared and then mostly deleted.
Once one is chosen its numbers move to `client/Assets/Settings/BoardDressing.asset`, its board to
`content/map.txt`, and the others go.
