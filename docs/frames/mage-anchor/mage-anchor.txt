# played/, yaw-plus-45/ and yaw-minus-45/

**What they are.** Two ticks of the recorded match — a Mage releasing a shot — at the framing the
game ships: `-Distance 22 -Width 1600`, the committed `docs/frames/match-tick-1546.png`'s and the
pitch sweep's. Tick **1540** is the Mage at 7,9 firing, the one in the middle-left of the frame; tick
**1580** is the Mage at 13,10, the one in the bottom-right corner nearest the camera, which is the
largest a Mage ever is at 1600x900. `played/` holds the shipped anchor and three candidates for
where the flash leaves from; the two `yaw-*` directories hold the shipped anchor with the camera
swung 45° either way. Issue #289. Nothing here picks an anchor.

**The question they ask.** Where the Mage's flash should leave from, given that
`roster/mage-hat-pitch.txt` showed a camera pitch will not clear the hat and the pack ships no
hatless Mage. The shipped anchor is `EffectAnchor.At(spellbook_open)` — the book's own origin, which
is the point it hangs from the hand bone — and the hand is raised beside the head, under the brim.

**Tick 1546 is gone, and that is the first finding.** The pitch sweep's tick was a Mage casting on
the board of 7 September. `content/map.txt` moved under it on 10 September with the landscape merge,
the recorded match plays differently on the new board, and at 1546 neither Mage is doing anything: a
first pass at that tick came back **byte-identical across all four anchors**, which is exactly the
picture-that-answers-nothing this project keeps getting bitten by. The ticks here came off the
simulation's own event stream — a throwaway listener on `IMatchEvents.TowerFired` filtered to row 4,
which is what the rung candidates did with the capture log — and a Mage fires every 91 ticks, so
1540 is the last shot before the old tick and 1580 the next.

**The ticket was written against a staff the Mage no longer holds, and that is the second.** It says
the flash sits on the head of a staff the Mage raises beside its head. Since `a101ba29` on
6 September — the day before the pitch sweep — row 4 is bound with `spellbook_open` in the right hand
and the anchor at the book, and `roster.md` says so: *"the mage, book in hand. The flash leaves the
open spellbook."* The staff and `StaffTip` belong to row 26, the Sorcerer, which stands on no
committed board. So the four candidates the ticket named are translated to the book:

| candidate | file | what it spells |
|---|---|---|
| the book as bound | none — `match-tick-*.png` | the baseline |
| the far edge of the book, the book's tip against its grip | `mage-anchor-book-edge.txt` | `anchor 4 tip-of spellbook_open 0,0,1` |
| a height above the root, what every tower did before the anchors | `mage-anchor-muzzle-height.txt` | `anchor 4 -` |
| the Mage turned, with the anchor untouched | `yaw-plus-45/`, `yaw-minus-45/` | `-Yaw 45`, `-Yaw -45` |
| the point of the hat, the one place a hat cannot cover | `mage-anchor-hat-top.txt` | `anchor 4 tip-of Mage 0,1,0` |

The fifth is not on the ticket. It is on the sheet because §6 of the prototype skill says an
enumerable vocabulary goes up whole, and "somewhere the hat cannot hide" is the whole question.

**"The Mage turned" is the camera and not the body, on purpose.** A tower turns to face the creep it
is shooting — `TowerView.FacingToward` — so there is no yaw in any table to move, and a fixed turn
would be a picture of a pose the game never holds. What the brim covers is a relation between the
raised hand and the camera, and `-Yaw` is the argument that already moves it.

**`anchor <id> -` is new, and it is the one line this ticket added to the machinery.** Everything
else was `-matchFrameArt`, which #281 built and which already carried `anchor`. A `-` takes the
row's anchor off, and `TowerView.Muzzle` then falls back to `MatchTuning.TowerMuzzleHeight` — 1.4 m
above the root — which is the fixed height the anchors replaced, spelled the way an empty hand and
an empty tile already are. `UnitArtFileTests.AnAnchorOfNothingUnsetsTheRowsAnchor` covers it.

**What the frames measure.** Against the baseline at the same tick and the same camera, on a
1,440,000-pixel frame. The tracer moves with the flash, so every figure is the orb *and* the line:

| candidate | tick 1540, the Mage at 7,9 | tick 1580, the Mage at 13,10 |
|---|---|---|
| `mage-anchor-book-edge` | 4,229 px · 0.29% | 3,774 px · 0.26% |
| `mage-anchor-muzzle-height` | 5,485 px · 0.38% | 7,043 px · 0.49% |
| `mage-anchor-hat-top` | 7,095 px · 0.49% | 9,550 px · 0.66% |

Issue #280 put anything crossing the air at 0.004–0.030% of the frame; every one of these clears
that band by an order of magnitude, because a flash is drawn on the body and not in the air. So this
is a question a played frame *can* decide, unlike three of #281's four.

**What the read-back says, at 2x, before anybody decides.** Crop `played/` at `(420,380)–(900,660)`
for tick 1540 and `(1150,330)–(1600,700)` for 1580 and put the four side by side:

- **The book as bound, 1540** — half the orb shows, on the brim's lower-right edge, the other half
  under the hat. **1580** — nothing: the Mage nearest the camera is seen from behind, the raised hand
  is on the far side of the hat, and the whole flash is under it. Only the tracer comes out from the
  brim. **This is the picture a player gets today of the largest Mage on the screen, and it has no
  flash in it.**
- **The far edge of the book** — at 1540 the orb moves about fifteen pixels down and right, wholly
  out from under the hat but still touching the brim. At 1580 it is still under the hat: the book
  is small and its far edge is not far.
- **1.4 m above the root** — at 1540 the orb is **gone**, under the hat, with the tracer emerging
  from the brim's upper edge; at 1580 the same. What the anchors replaced was not a flash on a hand
  that the hat sometimes covers; it was a flash inside the hat at every angle. **The pitch sweep's
  premise — that the anchor is what put the flash under the hat — is backwards: the anchor is what
  brought it half out.**
- **The point of the hat** — the one candidate with an orb in the frame at both ticks. At 1540 it
  sits on the crown between the peak and the band; at 1580, on the crown, in front of the body, as
  the largest orb on the sheet. The tracer leaves from the hat, which is the cost.
- **The camera at +45°** — the Mage at 7,9 is seen from dead behind: the hat is a full disc, the
  orb is wholly under it and the tracer leaves from under the brim. Strictly worse than shipped.
- **The camera at −45°** — the hat clears the hand for the first time, the face shows, and a
  **tree on the next cell covers the orb instead**: a sliver of yellow at the tree's edge and a
  tracer. The board's right edge leaves the frame at this yaw, as the far half did at pitch 20.
  So the yaw that shows the flash on the Mage trades the board for it *and* is at the mercy of the
  dressing, which is the pitch sweep's finding again from the other side.

**Redraw.**

```powershell
$common = @{ Ticks = '1540,1580'; Distance = 22; Width = 1600 }
./tools/capture-match-frames.ps1 @common -OutDir docs/frames/mage-anchor/played
foreach ($c in 'mage-anchor-book-edge', 'mage-anchor-hat-top', 'mage-anchor-muzzle-height') {
    ./tools/capture-match-frames.ps1 @common -Art "docs/frames/mage-anchor/$c.txt" -OutDir docs/frames/mage-anchor/played
}
./tools/capture-match-frames.ps1 @common -Yaw 45  -OutDir docs/frames/mage-anchor/yaw-plus-45
./tools/capture-match-frames.ps1 @common -Yaw -45 -OutDir docs/frames/mage-anchor/yaw-minus-45
```

Each directory carries the `rendered-from.txt` the capture wrote beside its pictures, so
`tools/check-docs.ps1` can date them against the content rather than against a commit.
