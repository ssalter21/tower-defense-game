# Scenery prototypes

Six dressings of the committed board, rendered so one can be chosen by looking rather than by reading
numbers. Regenerate with `tools/capture-prototypes.ps1` (editor closed — it is batchmode).

**Every frame is the same playfield.** `content/map.txt`, its 51-cell corridor and the tier of every cell are
untouched by all six; the match's result, its landmark table and its per-tick hash are identical under each.
What differs is scenery density, where the tall things stand, and how many ledges break a tier's drop.

- `presets.txt` — the numbers behind each frame, written by the capture.
- `board-<preset>-high.png` — the shipped camera framing.
- `board-<preset>-low.png` — a lower, turned camera. **Judge the terracing here**: a cliff face is close to
  invisible from the shipped pitch.
- `board-schematic.png` — the map itself. Corridor in orange, tiers dark to light, a white ring on each of the
  100 cells that draws a ledge.

The presets live in `client/Assets/View/SceneryPresets.cs` and exist to be compared and then mostly deleted.
Once one is chosen its numbers move to `client/Assets/Settings/BoardDressing.asset`, which is the asset a
human actually slides, and the other five go.

Why the board reads flat in the first place, and what the ledges do and do not fix, is measured in
[What makes the board read flat](../../research/what-makes-the-board-read-flat.md).
