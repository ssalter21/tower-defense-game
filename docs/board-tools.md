# The board tools

**Three editor menus under `Tools > Board`, and the two content files they bake.** Each is a `public static
void` with no arguments, so an agent runs it with `-batchmode -executeMethod` like anything else — there is no
bridge and no session, per [rule 3 of `AGENTS.md`](../AGENTS.md). This page says what each one does and what
it must not do; it was rule 3's own text until 13 September 2026, moved here because it is reference and not
instruction.

## Edit Map — the board is drawn in Unity, and `content/map.txt` is still the artifact

`Tools > Board > Edit Map` opens a window that paints hexes and tiers in the scene view and bakes that file. It
does not hold a second copy of the map's rules: a draft is legal exactly when `HexMap.ParseUtf8` accepts it, so
the editor refuses in the simulation's own sentence and cannot drift from it. Loading the committed board and
baking it unchanged is asserted to be byte-for-byte identical — `BoardDraftTests` — because a bake that moved
the board by a space would invalidate `defense.txt`, `match.replay`, the landmark table and the cell coordinates
in seventeen `sim.tests` files for no visible reason. The bake names that chain and runs none of it.

## Scenery — the whole KayKit collection is imported, and a model is addressed by name

`client/Assets/Art/Kaykit/` holds all 4,247 models of the 21 packs that ship an `fbx(unity)` export.
`Tools > Board > Scenery` is the palette: search it, pick one, place it on a cell. In `content/dressing.txt`
that is a **`model`** line naming the path under that folder — `model 3 4 city-builder/building_A 0 0 0 100` —
as against a **`place`** line, which asks a *family* for its n-th and is what the generator writes. Both verbs
stay: a generator scattering a board it has never seen must be able to ask for "a grove", and a person who has
looked at the thing means that one.

**Each pack ships its own atlas**, so a named model carries its own material and only family pieces wear the
board's one surface; drawing a City Builder crate against the hexagon atlas produces confetti, not a
slightly-wrong crate. **The scene carries only the models the dressing file names**, resolved at scene-build
time — so a bake that adds a model needs `build-match-scene.ps1` after it, which the bake's own log line says.

## Dress, Bake, Clear — the dressing is the one thing edited by hand

`Tools > Board > Dress` draws the real floor and scenery into the open scene so a human can move things; `Bake`
writes what they left to `content/dressing.txt`; `Clear` takes it down.

**Drawing the board again carries unbaked work forward rather than discarding it** — both tools share one
preview and the map editor rebuilds it after every stroke, so a teardown that read the file back would mean
painting one hex threw away every tree somebody had moved. What is standing is measured against the map the
floor was *drawn from* (`HexFloor.Map`), never the one about to be drawn, or a stroke would pin the generator's
own scenery into the file as though a person had placed it. `Clear` is therefore the only way back to the
committed file, and the only way to lose the work in one click.

Every preview object carries `HideFlags.DontSave`, so none of it reaches `Match.unity` and the scene stays
generated. The entry point for an agent is `-batchmode -executeMethod View.Editor.BoardDressingTools.Bake`.
Where the scenery goes by default is `client/Assets/Settings/BoardDressing.asset`, which `build-match-scene.ps1`
creates once and then never touches again, because it is the one asset in this project a person tunes rather
than derives.
