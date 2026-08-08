# A purse in one column beats the same purse in two

**Research note** · 8 August 2026 · measured with the sweep harness in
[`sim/Sweep.cs`](../../sim/Sweep.cs), against the committed map, defense and ruleset

**Question:** wave slots are the design's stand-in for a second wallet — a slot spent on a cheap column is a
slot not spent on a heavy unit — and the width widens at every anchor from two to five. What does spending a
purse across more slots actually buy against the board that exists?

**Inputs:** the committed corridor, the committed six-tower defense and `content/ruleset.txt`. Every run is ten
waves against a field of ten, played in no-death mode, with the build phase taking one preferred creep off each
round's public offering and dividing the purse evenly across the slots it fills.

---

## Bottom line

**How many columns a purse is spread across decides whether anything leaks, and it decides it by nearly three
orders of magnitude.** The cleanest form of the measurement changes one thing only — the shape of the wave the
canned field sends, holding its total gold, the defense, the seeds and the run's own waves fixed:

| what the field sends | gold | columns | leak cost taken, 8 runs of 10 rounds |
|---|---|---|---|
| 10 grunts | 100 | 1 | **1406** |
| 5 grunts + 5 runners | 95 | 2 | **2** |
| 3 grunts + 3 runners + 3 wisps + 1 drifter | 97 | 4 | **0** |

Everything else on those three sweeps is byte-identical — the runs deal 1188 and spend 7945 in all three,
because only the opponent's *wave* moved and the defense it stands is the same file.

The mechanism is release density against tower saturation. The simulation releases the units of one order one
every fifteen ticks, and a build phase sends every slot on tick zero — so *n* filled slots put *n* bodies on
the corridor every fifteen ticks instead of one. What decides a leak is whether bodies arrive faster than six
towers can chew them: a column of ten does, and two columns of five are killed as they come.

## The same effect from the player's side

Win rate, binned by how many distinct creeps a run ended up able to field, over 192 runs — thirty-two seeds for
each of the roster's six creeps, against the one-column field:

| ingredients taken | runs | won |
|---|---|---|
| 2 | 7 | 43% |
| 3 | 42 | 43% |
| 4 | 73 | 22% |
| 5 | 52 | 12% |
| 6 | 17 | 24% |

**The fall from three ingredients to five is the finding**; the tick up at six is seventeen runs and should not
be read as a shape. The per-creep leak rate tells the same story less cleanly — the bulwark falls 36, 26, 16,
15, 11 gold dealt per hundred spent across bins two to six, and the drifter 20, 18, 15, 11, 8, but the grunt,
the runner and the lancer are flat or rise at the tail. Win rate is the column to read this on; cost efficiency
is not, and under a one-for-one leak charge it never could be — see
[the note on that](cost-is-not-a-balance-lever-under-a-one-for-one-leak.md).

## What follows

- **The slot width is not currently the scarcity it was designed to be.** It is closer to a liability: a run
  holding five different creeps divides its purse five ways and wins a third as often as one holding three.
  The anchor cadence widens the slots at waves 3, 6 and 9, so the run's own progression makes it worse at the
  thing being progressed.
- **This is a property of the one-hex corridor and a six-tower line, and both are provisional.** A maze with
  branches and elevation is seam 9, and a board where two columns can arrive somewhere different is a board
  where spreading is not obviously a loss. Nothing here should be retuned before that lands — and nothing
  should assume a slot is a cost either.
- **A canned field of one member cannot say more than this.** Win rate against a single fixed opponent is a
  step function, which is why `content/field.txt` is calibrated against the measurement above rather than
  averaged into existence. A field drawn from a real distribution of stored runs is what makes the column
  continuous.
