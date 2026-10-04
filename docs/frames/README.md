# Match frames

**Documentation, not an oracle.** Nothing compares these pictures to anything and nothing fails if they change;
what catches a broken view is `Tests.PlayMode/MatchViewTests` and the sit-down landmark table. Every frame is
drawn through the real `MatchRoot`, hex floor, `OrbitCameraRig` and `MatchView` stepping the real simulation,
out of `content/match.replay` — so a tick in a filename is a tick of the run `content/landmarks.txt` was made
from. A frame is a function of its tick **and of the tick list it was asked for**, so a fixture is re-captured
with the whole list in the recipe below and never one tick at a time. `rendered-from.txt` beside the pictures
says which content each was drawn from, written by the capture and never by hand; `check-docs.ps1` reads it
where a date says a picture is older than the content. The reasoning is
[ADR-0064](../adr/0064-a-committed-frame-is-documentation-not-an-oracle.md); the long captions are
[`notes.md`](notes.md).

## Regenerating

```powershell
./tools/capture-match-frames.ps1                       # the default ticks
./tools/capture-match-frames.ps1 -Ticks "366,900"      # named ticks
./tools/capture-match-frames.ps1 -Yaw 120 -Width 1920  # another heading, wider
./tools/capture-match-frames.ps1 -Distance 25          # down among the creeps

# The two committed frames at the default framing. NOT the default ticks:
# MatchFrameCapture.DefaultTicks is 60, 200, 366, 700, 900 and 1400, none of
# which is committed, and a plain run writes six pictures the .gitignore keeps
# out of the tree.
./tools/capture-match-frames.ps1 -Ticks "1096,2700"

./tools/capture-match-frames.ps1 -Ticks "1229,1546" -Distance 22 -Width 1600

`-Units` replaces the roster (a fixture table kept in step with `content/units.txt`), `-Defense` what is
standing, `-Wave` what is walking; which one a picture needs is decided by what the record is missing. Every
kept tick's line in `capture-match-frames.log` carries the running count of each shape, which is how the tick a
capstone went off on is found. Frames the capture writes that are not listed below are ignored by
[`.gitignore`](.gitignore), by construction.

## What is committed

| Frame | Shows | Framing |
|---|---|---|
| `match-tick-1096.png` | The first overtake the landmark table names; a shell in flight | default |
| `match-tick-2700.png` | The wave spread along the corridor, both tower kinds engaged | default |
| `match-tick-1229.png` | An Archer releasing: flash on the bow, tracer to the creep | `-Distance 22 -Width 1600` |
| `match-tick-1546.png` | A Mage casting from the staff it has since lost; the hat covers the staff at this pitch | `-Distance 22 -Width 1600` |

## The folders

| Folder | What it holds |
|---|---|
| [`board/`](board/README.md) | The bare board as it ships — the skin in its shades of green and the road pieces — from the match camera and from a plan view, with the corridor's steepest cell cropped out of each so the level change is shown to be visible |
| [`roster/`](roster/README.md) | Contact sheets drawn from set files: the nine tower lines on three sheets, the twelve creep bodies on two, and the four under-served rungs twice each — at `-Width 700` and at the twenty-four pixels a body gets at 1600x900 |
| [`beside-props/`](beside-props/README.md) | The four props that stand on the tile beside a tower, and where each moves when a tower takes the tile |
| [`mage-anchor/`](mage-anchor/README.md) | Where the Mage's flash leaves from — the shipped anchor and three others, at two ticks and three yaws |
| [`played-run/`](played-run/README.md) | Ten screen captures of the built player driven through a whole run by synthetic input — the only pictures here no capture tool drew, dated by that session |
