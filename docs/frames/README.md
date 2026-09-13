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

# The same board, defense, wave and seed, played against a roster of your own
./tools/capture-match-frames.ps1 -Units "docs/frames/effects-roster.txt" `
    -Ticks "700" -Distance 20 -Width 1600

# The same board, wave and seed, with a defense of your own standing on it
./tools/capture-match-frames.ps1 -Defense "docs/frames/four-lines.txt" `
    -Ticks "572,780" -Width 1600

./tools/capture-match-frames.ps1 -Defense "docs/frames/pierce-lines.txt" `
    -Ticks "516,673" -Distance 18 -Width 1600

./tools/capture-match-frames.ps1 -Defense "docs/frames/magic-lines.txt" `
    -Ticks "311,342,344" -Distance 22 -Width 1600

# The same board, defense and seed, with a wave of your own walking it
./tools/capture-match-frames.ps1 -Wave "docs/frames/creep-auras.txt" `

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
| `effects-roster-tick-0700.png` | The two-segment bar over a creep and a slow's circle — a placeholder nobody has signed | `-Units effects-roster.txt`, `-Distance 20` |
| `four-lines-tick-0813.png` | Shield Wall, Slam and Blessing circles at once; why the alpha is 0.45 | `-Defense four-lines.txt`, `-Distance 22` |
| `four-lines-tick-0572.png` | The same three at the whole-floor framing, all twelve rows in one picture | `-Defense four-lines.txt` |
| `four-lines-tick-0780.png` | The Mortar's burst, three hexes across because the radius is 1500 | `-Defense four-lines.txt` |
| `pierce-lines-tick-0673.png` | Three knives from the Fan of Knives, the Overwatch's leg-long shot | `-Defense pierce-lines.txt`, `-Distance 18` |
| `pierce-lines-tick-0516.png` | The Archer and Ranger at full draw, knives crossing | `-Defense pierce-lines.txt`, `-Distance 18` |
| `pierce-lines-tick-0674.png` | All six pierce rows, the Overwatch's shot the length of the leg | `-Defense pierce-lines.txt` |
| `magic-lines-tick-0344.png` | The Unravel's armour strip on the ground; three bolts; the Consecration's light | `-Defense magic-lines.txt`, `-Distance 22` |
| `magic-lines-tick-0342.png` | The same corner two ticks earlier, bolts fresh out of tome and staff | `-Defense magic-lines.txt`, `-Distance 22` |
| `magic-lines-tick-0311.png` | The Mage line firing: the Unravel's shell leaving, thirty-three ticks from 0344 | `-Defense magic-lines.txt`, `-Distance 22` |
| `magic-lines-tick-0331.png` | Both auras' reach; the Overgrowth nowhere in it; the Mage's splash wearing the Mortar's burst | `-Defense magic-lines.txt` |
| `creep-auras-tick-0272.png` | Ward, haste and hex-ward circles overlapping; raised Minions in the knot | `-Wave creep-auras.txt`, `-Distance 20` |
| `creep-auras-tick-0276.png` | Down among the bodies: nothing hangs above a body inside an aura | `-Wave creep-auras.txt`, `-Distance 14` |
| `creep-auras-tick-0271.png` | All four creep auras' reach against a board nineteen wide | `-Wave creep-auras.txt` |
| `creep-auras-tick-0094.png` | The Frost Wight's frostbite lying across the Archers — the one aura reaching the other side | `-Wave creep-auras.txt`, `-Distance 18` |

## The folders

| Folder | What it holds |
|---|---|
| [`roster/`](roster/README.md) | Contact sheets drawn from set files: the nine tower lines on three sheets, the twelve creep bodies on two, and the four under-served rungs twice each — at `-Width 700` and at the twenty-four pixels a body gets at 1600x900 |
| [`beside-props/`](beside-props/README.md) | The four props that stand on the tile beside a tower, and where each moves when a tower takes the tile |
| [`effect-candidates/`](effect-candidates/README.md) | The alternatives to every effect look that ships on nobody's signature, each rendered at play framing and close; a candidate is a file, never an edit to `MatchTuning` |
| [`mage-anchor/`](mage-anchor/README.md) | Where the Mage's flash leaves from — the shipped anchor and three others, at two ticks and three yaws |
| [`rung-candidates/`](rung-candidates/README.md) | The four under-served rungs' candidates on the real board at the fixed camera, with how much of the frame each moves |
| [`played-run/`](played-run/README.md) | Ten screen captures of the built player driven through a whole run by synthetic input — the only pictures here no capture tool drew, dated by that session |
