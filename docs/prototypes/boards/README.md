# The prototype boards

Six height maps were drawn over one corridor; the four that were not chosen have been deleted, and what is
left is the one that was. Each is a whole map file — terrain block, blank line, level block — and is
loaded through `HexMap.ParseUtf8` by `PrototypeCapture`, so a board the game would refuse fails at render
rather than becoming a picture of a map nothing can load.

**The terrain block is `content/map.txt`'s, character for character, in all six.** Only the level block
differs. That was what made the rendered frames in [`../scenery/`](../scenery/) comparable: anything that
changed between two of them was landscape, and the route was a constant.

**Levels are half a block, `a` to `i`.** Two levels is one block of height and is worth half a hex of reach,
which is what a level was worth on its own before the grid was halved. See
[the decision log](../../decision-log.md), 29 August 2026.

They are generated rather than typed, from height fields written against where the route actually leaves room
on the board. The generator applies three rules the eye cannot check by reading letters:

- **The road may only change level where it runs straight through a cell**, because a straight is the only
  shape the tile pack cuts a ramp for. A climb on a bend would draw as a step in the middle of the lane.
- **A road climb is at most a whole block**, so it is always either the low ramp or the high one.
- **A cell touching the road stands no more than a block below it**, or the lane hangs in the air over its own
  shoulder.
- **On `rolling-country`, no two touching cells differ by more than one level at all.** A slope limiter walks
  the grid until that holds, with the road's staircase pinned. That board was chosen, and a whole-block face
  has no ramp and no slope cut for it — it draws as a sawn edge. The other five are left as they were: they
  exist to be compared against, and three of them are about a hard edge on purpose.

**`rolling-country` has been adopted**: its level block is `content/map.txt`, so that file and this one now
carry the same heights. The rest are not committed content. Adopting another means copying its level block
into `content/map.txt`, which retires every stored record because the map hash covers the levels — and then
regenerating them with `tools/run-headless-match.ps1 -Regenerate` and `tools/sync-streaming-content.ps1`.
