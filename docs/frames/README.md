# Match frames

Rendered by `tools/capture-match-frames.ps1`, headless, with the editor closed.

**These are documentation, not an oracle.** Nothing compares them to anything
and nothing fails if they change. That call was made deliberately on this
project, after two frames whose bones were definitively swapped rendered
pixel-identical, reproducibly — a screenshot comparison here would be a check
that cannot fail, which is the species this project keeps deleting.

What catches a broken view is the assertions in `Tests.PlayMode/MatchViewTests`
and the sit-down landmark table. What these are for is letting a human see the
match at a named tick without opening the editor.

They are drawn through the real thing: the real `MatchRoot`, the real hex floor,
the real `IsometricCameraRig` at its real snaps, and the real `MatchView`
stepping the real simulation. A capture path that built its own approximation of
the scene would be a picture of something this project does not ship.

**And it is the real match**, read out of `content/match.replay` — the same
bytes the command line replays and the player plays, seed included. The tick in
each filename is only worth anything because of that: it is a tick of the run
`content/landmarks.txt` was made from, so a frame named 366 and the checklist row
that says "drag to tick 366" are about the same moment. These frames were
captured against a seed of the capture tool's own until #48, which happened to
agree on the overtake and disagreed by eleven ticks on the last creep to die.

## Regenerating

```powershell
./tools/capture-match-frames.ps1                       # the default ticks
./tools/capture-match-frames.ps1 -Ticks "366,900"      # named ticks
./tools/capture-match-frames.ps1 -Snap 3 -Size 1080    # another camera snap, bigger
```

## What is committed

Two frames, kept as a record of what the match looked like when
[#45](https://github.com/ssalter21/tower-defense-game/issues/45) landed:

- `match-tick-0366.png` — the tick the committed landmark table names as the
  first overtake, with the wave bunched around the first corner.
- `match-tick-0900.png` — the wave spread along the corridor, both kinds of
  tower engaged.

The rest of the default set is regenerable and not committed.

## What to look at

Both frames were worth capturing before they were worth committing: the first
capture of this ticket is what found the hitscan towers lying on their side on
the road. Every test in the suite was green at the time — the atlas had bound,
the mesh was there, the tower fired on schedule — because the view was forcing
each model's root rotation to identity, which throws away the axis-conversion
rotation an FBX root can carry. The characters' roots happen to be identity, so
they stood up perfectly and hid it.

That is the argument for these existing at all, and it is why the fix ships with
a test (`EveryModelStandsTheWayItWasImported`) rather than only with a picture.
