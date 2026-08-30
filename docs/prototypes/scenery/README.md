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
| `rolling-country` | *The best landscape render in the collection* | **Chosen.** Gentle relief and heavy wood — the interest is what stands on the ground, not the ground. Regraded so nothing steps a whole block. |
| `three-deep-cliff` | *Three-deep cliff layering* | Three flat shelves parted by whole-block faces, with the wood pushed onto the lips. |
| `canyon-run` | *Canyon variant* | The road on the floor of a trench, walls stepping away, autumn atlas. |
| `diorama-plate` | *Clay render*, *Diorama scale* | A low plate with four vertical incidents and very little else. |

- `presets.txt` — the numbers behind each frame, and a relief census counted off the map it was drawn from.
- `board-<preset>-high.png` — the shipped camera framing.
- `board-<preset>-low.png` and `-raking.png` — **judge the terrain here.** A cliff face is close to invisible
  from the shipped pitch, which is exactly why the shipped pitch is not where the board was found to be flat.

## The second pass on `rolling-country`

`rolling-country` was picked, and then two things were wrong with it.

**Twenty-nine of its steps were a whole block.** A level is half a block and the pack cuts a ramp for a
half step, but the height field was quantised without anything stopping two touching cells landing two
levels apart — and a two-level face has no ramp and no slope, so it drew as a sawn edge. A slope limiter
now walks the grid until no two touching cells differ by more than one level, holding the road's own
staircase fixed while it does. It moved 44 of the board's 247 cells and cost 7 of its 275 falls. The relief
is the same `a` to `e` it always was and **no step on the board is a whole block any more** — the census in
`presets.txt` reads `0 of a block or more`, against the control's `121`.

**And the board was floating in a void.** The camera cleared to a flat dark colour, so every frame was a slab
of hexes hanging in nothing with a hard silhouette all the way round. There is now a horizon: a procedural
sky, a plain of land laid at the depth the board's own rim falls to, and linear haze joining the two. The
board is not resting on the plain — its cliff columns bury themselves in it, so it reads as a piece of
country cut out of a landscape rather than as a game piece on a table. It is `client/Assets/View/Horizon.cs`
and its numbers are in `SceneFraming`.

**The plain was planted, and the planting was rejected.** A treeline four to seventeen metres off the rim
and a range of hills behind it were built, tuned over a dozen renders and then taken out again on sight: with
the pack's models scattered on open ground the board stopped reading as a diorama and started reading as a
cluttered tabletop. The plain is bare again and that is the committed state. What the attempt was worth is
the finding below, which was found while chasing it and applies to the bare plain just as much.

**The white band along the horizon was not fog.** It looked exactly like haze piling up at the far clip, and
two rounds of moving the fog did nothing to it — which is what said it was something else. Sampling a column
of pixels showed the ground at 255 where the fog colour would have been 222: brighter than the thing it was
supposedly fading into, which no amount of fog can do. It was the specular term. Both lit shaders default to
half smoothness, which on a hex tile is a sheen nobody notices and on a plane two hundred metres across is a
blown highlight all along the grazing edge. `ViewMaterials.Matte` is the fix. Before it landed the horizon
was a white line and anything standing near it looked like it was floating, because the ground underneath was
brighter than the sky.

**The sky is not visible from the shipped camera, and that is geometry rather than a fault.** The horizon of
a flat plain sits at eye level; the shipped camera is pitched 35 degrees down through a 20-degree half lens,
so it looks between 15 and 55 degrees below horizontal and the horizon is above the top of the frame at any
radius. Shrinking the plain was tried and does nothing. What is behind the board at the shipped pitch is
land, which is what looking down at a landscape looks like — the sky arrives when the camera comes down, and
the low and raking frames are where to see it.

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
