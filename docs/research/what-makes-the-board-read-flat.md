# What makes the board read flat

**The question:** the committed board looks like a layer cake rather than a landscape. Before dressing it
differently, what is actually causing that — and is a half-height step the fix?

**The answer: the board has 121 one-metre cliff faces and ramps exactly three of them.** Breaking each drop
into half steps is buildable, costs no new art and no simulation change, and it measurably improves the
board's outline. It does not add tonal separation, which is the reason a second ledge starts to look like a
contour map instead of a hillside.

## 1. The board, counted

Read from `content/map.txt` — 19 by 13, 247 cells, a 51-cell corridor.

| | |
|---|---|
| cells at tier `a` / `b` / `c` | 121 / 107 / 19 |
| adjacent pairs where one cell stands above the other | **121** |
| corridor cells that climb a tier, and so draw a ramp | **3** |
| cells standing above a neighbour, or on the board's edge | 100 of 247 |

The last row is the number of cells a ledge would be drawn under, and the third row is the whole of the
board's current answer to height: `RoadTiling.TryRamp` selects `hex_road_A_sloped_high` where a straight run
of corridor stands one tier below a corridor neighbour, which on this map happens three times. Every one of
the other 118 drops is a bare vertical metre.

That is the finding. It is not that the tiers are too few or too shallow — it is that 118 of 121 height
changes are drawn as an unmodulated cliff.

## 2. The pack ships half-height geometry already

Measured from the glTF accessor bounds in
`KayKit Medieval Hexagon Pack 1.0.1/Assets/gltf/tiles/`, in pack units, which are metres — the pack's hex is
2.000 across the flats and `HexGeometry.AcrossFlats` is 2.0, so nothing is scaled on import:

| model | Y range | rises |
|---|---|---|
| `hex_grass`, `hex_road_A` | −1.000 → 0.000 | flat, one metre of body |
| `hex_grass_sloped_low`, `hex_road_A_sloped_low` | −1.000 → **+0.500** | **half a tier** |
| `hex_grass_sloped_high`, `hex_road_A_sloped_high` | −1.000 → +1.000 | a full tier |

Kay authored the ramps as a matched pair, and the `_low` half is not imported. This is worth knowing before
anyone models anything: half-step geometry at exactly the granularity `HexGeometry.LevelStep` implies is
already in the pack, free.

## 3. Why the step itself must not move

`HexGeometry.LevelStep` is 1.0 because the tile models hang a metre of body below their walkable face, so a
raised tile's underside meets the top of the tile beside it with no gap and no overlap. Halving it opens
daylight in every step on the board. The tier is also a simulation quantity — it is worth half a hex of reach,
`Reach.MilliHexPerLevel`, ADR-0023 — so a half tier would be a rules change, not a look.

So the ledges drawn by `HexFloor.Terrace` are neither: they are extra copies of the ground tile set below the
face and slightly wider, carrying no cell, no tier and no build slot. Where a neighbour is level the
neighbour's own body hides the ledge; where the ground falls away its rim shows as a shelf. The match's
result, its landmark table and its per-tick hash are unchanged, which is what makes this safe to try.

## 4. What the ledges do, and what they do not

Rendered by `tools/capture-prototypes.ps1`; frames in [`docs/prototypes/scenery/`](../prototypes/scenery/).

Sampling the grass-hued pixels of the front rim in the captured frames, separating top faces from side faces
by luminance:

| preset | ledges | top face | side face | side / top |
|---|---|---|---|---|
| `as-it-ships` | 0 | 176.9 | 110.0 | 0.62 |
| `half-step` | 1 | 177.1 | 110.6 | 0.62 |
| `back-wall` | 2 | 177.2 | 111.0 | 0.63 |

**The ratio does not move.** Every ledge is the same material at the same angle as the face above it, so
adding one multiplies the number of edges in the silhouette without adding a single new tone. That is why one
ledge reads as a softened step and two begin to read as contour lines: the eye is being given more outline and
no more shading.

The consequence for anyone tuning this: `ApronCount` is a silhouette control, not a depth control. If the
board still reads flat at one ledge, the next lever is not a second ledge — it is something that changes tone
or hue at the step, which on this board means the rock and tree masses that `PeakChance` and `GroveChance`
already place, or geometry from a pack whose cliff faces are a different material.

## 5. What this corrects

An earlier reference note suggested the pack's `hill_single_*` models as padding for height gaps, on the
strength of Kay's own usage guide. That is already refuted in this repository:
`MatchSceneBuilder.cs` records that those models were imported, looked at on a contact sheet and dropped,
because they are shells authored to cap a hex that is already raised and drawing one on flat ground shows its
inside. The usage guide is describing a board built the pack's way, where the hex under the hill is itself
lifted; it is not advice that transfers to scattering them.
