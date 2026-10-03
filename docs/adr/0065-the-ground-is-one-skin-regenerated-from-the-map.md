# 0065 — The ground is one skin regenerated from the map

**Decided.** The board's ground is one mesh, `HexSkin`, generated from `content/map.txt` at scene-build time:
a flat top per cell at the cell's own height, shrunk to six tenths of the hex; a planar slope between two
cells of different level; the pack's road pieces left standing and the skin met to each at the height the
piece actually has at that corner, ramps included; a bank of earth at the rim, falling by the dressing's rim
drop. Each level's ground wears its own shade of the grass's green, and nothing draws a line on a change of
level. Decided on [issue #330](https://github.com/ssalter21/tower-defense-game/issues/330) from the sheet under
`docs/prototypes/smoothing/`; shipped on
[issue #331](https://github.com/ssalter21/tower-defense-game/issues/331) and
[issue #348](https://github.com/ssalter21/tower-defense-game/issues/348).

- **Generated, never authored.** The mesh is a function of the parsed map and the standing pieces, so a new
  map costs nothing to draw and nothing can drift between the map and the ground drawn under it. Ground cells
  are no longer tiles: `HexFloor` instances the corridor's pieces and any cell under the water line, and the
  skin is everything else.
- **A piece's edge is the skin's edge.** Where a corridor cell or a water cell meets the ground, the skin's
  corner stands at the height read off that piece's own vertices. There is no special case for the corridor:
  it is the same rule as between two ground cells, with the piece's height standing in for a plateau's.
- **A shade of green is the legibility aid, and it is the only one.** A level is a quarter of a hex of reach
  in the simulation, so a slope that smoothed the level out of sight would have smoothed away the range a
  player is pricing. The skin is one submesh per level and one bare submesh for the earth; `HexFloor` gives
  each level in use a copy of the grass material multiplied by a grey, so the hue is the grass's own and only
  the lightness moves. The lowest level in use is `SceneFraming.LowestGroundShade` (0.70) of the grass, the
  highest `HighestGroundShade` (1.30), linear between. A plateau, the half-slope beside it and its share of
  each corner take the cell's level, so the shade changes halfway down a slope. The road, the earth bank, the
  scenery and the units are never shaded.
- **A road piece meets its neighbour flush.** Every KayKit road piece's flat top stops short of the hex edge
  and a bevel drops to it; lit at an angle, that bevel drew a dark outline round each corridor hex.
  `SeamlessPiece` reshapes a copy of each piece: every vertex outside the flat top's hex is pulled in beneath
  it, then the piece is stretched until the flat top reaches the hex edge. Scaling the piece alone was tried
  and pushes the bevel past the edge, where it stands above the skin's slope down from a raised road.
- **Picking reads the map, not the mesh.** `HexPicking` was never told; a plateau sits at the cell's own
  height, so nothing a player clicks has moved.
- **The cliff posts are gone, and so are the grass slope pieces.** A metre of earth under a raised tile was
  what closed the seam between two levels; there is no seam to close under a continuous surface, and the
  pack's `hex_grass_sloped_*` pieces were a tile-set answer to the question the skin answers. `TilePiece`
  holds the eight pieces the floor still lays. The pack's flat grass tile stays in the set as the thing the
  skin samples its grass and earth swatches off, so the skin's colours are whatever atlas the board wears.
- **The dressing's ridge mounds go with the drops they marked.** `ridgeChance` is zero in the shipped asset,
  in `DressingSettings.Default` and in the asset's field default, so the three places the number is declared
  agree and a capture drawing the defaults draws what the game draws. The generator stays, as a knob at zero.

**Cost.** The ground is flat shaded on purpose, so a plateau reads flat; smooth normals are one line if anyone
wants the blob. A prop the dressing places off a cell's centre can now stand on a slope. The build-phase
highlight is still a flat hexagon at the cell's height, so on a sloped cell its uphill half is under the
ground; that is chrome, and it is a ticket of its own.

**Rejected.** A contour line on every edge where the level changes: it shipped, and Sam rejected it on the
ship frame. A piece per neighbour case, authored in the pack's style: 146 of this board's 196 ground cells
would be authored pieces of **13 distinct shapes**, rotations counted once, of which the pack ships none — and
a new map can need shapes this one never did. The census is in `docs/prototypes/smoothing/smoothing.txt`,
counted off the meshes by the sheet that decided it. The sheet's colour band, which walked blue-green to
yellow-orange and tinted the road with the ground: its frames go muddy, and a hue per level is a colour taken
from every unit and effect. The shades keep the grass's hue for that reason. Keeping the ground as
tiles under the skin: two surfaces at one height is how an earlier sheet drew two floors at once.
