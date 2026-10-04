# The board as it ships

The bare board — the skin in its shades of green, the road pieces and the shipped dressing, with nothing standing on it
and nobody walking it — from the match camera and from a plan view, and a check beside them. Rendered by
`tools/capture-board.ps1`, headless, with the editor closed. The decision these pictures show shipped is
[ADR-0065](../../adr/0065-the-ground-is-one-skin-regenerated-from-the-map.md); the sheet it was decided from
is [`docs/prototypes/smoothing/`](../../prototypes/smoothing/README.md).

```powershell
./tools/capture-board.ps1
```

| File | What it is |
|---|---|
| `board-match.png` | The shipped framing: yaw 0, pitch 35.26, field of view 40, framed on the floor as `OrbitCameraRig.Reframe` does. **Judge the look here** — this is what a player sees. |
| `board-plan.png` | Straight down, orthographic, the board fitted with the shipped margin — `tools/render-map.ps1`'s view, which has no Unity camera of its own. **Judge legibility here** — from overhead, height is invisible and the shades of green carry the whole reading. |
| `board-check.png` | The corridor's steepest cell — the one with the most height change around it, 4,4 on the committed board — cropped out of each frame at twice the size, match on the left and plan on the right. The render checked against itself: the level change at that cell has to be visible in both, with the shades alone. |

Every frame is 1600x900. `rendered-from.txt` beside them is what the capture leaves behind saying which
content it drew, written by the capture and never by hand; `tools/check-docs.ps1` reads it where a commit
date cannot answer.
