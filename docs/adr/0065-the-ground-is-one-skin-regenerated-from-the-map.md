# 0065 — The ground is one skin regenerated from the map

**Decided.** The board's ground is one mesh, `HexSkin`, generated from `content/map.txt` at scene-build time:
a flat top per cell at the cell's own height, shrunk to six tenths of the hex; a planar slope between two
cells of different level; the pack's road pieces left standing and the skin met to each at the height the
piece actually has at that corner, ramps included; a bank of earth at the rim, falling by the dressing's rim
drop. Along every edge between two cells of different level lies a dark ribbon a tenth of a metre wide,
mid-slope, as part of the floor and not as a toggle. Decided on
[issue #330](https://github.com/ssalter21/tower-defense-game/issues/330) from the sheet under
`docs/prototypes/smoothing/`; shipped on
[issue #331](https://github.com/ssalter21/tower-defense-game/issues/331).

- **Generated, never authored.** The mesh is a function of the parsed map and the standing pieces, so a new
  map costs nothing to draw and nothing can drift between the map and the ground drawn under it. Ground cells
  are no longer tiles: `HexFloor` instances the corridor's pieces and any cell under the water line, and the
  skin is everything else.
- **A piece's edge is the skin's edge.** Where a corridor cell or a water cell meets the ground, the skin's
  corner stands at the height read off that piece's own vertices. There is no special case for the corridor:
  it is the same rule as between two ground cells, with the piece's height standing in for a plateau's.
- **The contour is the legibility aid, and it is the only one.** A level is a quarter of a hex of reach in the
  simulation, so a slope that smoothed the level out of sight would have smoothed away the range a player is
  pricing. The ribbon lies where the level changes and nowhere else. No tint per level: a colour per level is
  a colour taken from every unit and effect.
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

**Rejected.** A piece per neighbour case, authored in the pack's style: 146 of this board's 196 ground cells
would be authored pieces of **13 distinct shapes**, rotations counted once, of which the pack ships none — and
a new map can need shapes this one never did. The census is in `docs/prototypes/smoothing/smoothing.txt`,
counted off the meshes by the sheet that decided it. A colour band per level: the sheet's band frames go
muddy, and the cost is paid by the whole art direction for as long as the band is on. Keeping the ground as
tiles under the skin: two surfaces at one height is how an earlier sheet drew two floors at once.
