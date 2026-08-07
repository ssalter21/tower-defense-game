# Why the golden trace moved when the balance did not

**Question.** Every damage and health number in `content/units.txt` was multiplied by ten. The design intent
is that this changes nothing about how the match plays. `content/golden-trace.txt`, `content/landmarks.txt`,
`content/match.replay` and the result beside the current golden all moved anyway. Is that the rescale working,
or is it a desync?

**Answer: it is the rescale working, and the mechanism is the damage roll's range.** Recorded here so that a
moved trace after a rescale is recognised rather than investigated.

## The claim, measured

Multiplying a health pool and the damage that eats it by the same factor leaves shots-to-kill where it was.
That is true at the ends of every roll in the committed content, and this is the measurement:

| attacker against target | damage roll | shots to kill | distinct outcomes |
|---|---|---|---|
| bolt against grunt, before | 9–15, 7 values | 14–23 | 7 |
| bolt against grunt, after | 90–150, 61 values | 14–23 | 10 |
| bolt against runner, before | 9–15, 7 values | 8–13 | 5 |
| bolt against runner, after | 90–150, 61 values | 8–13 | 6 |
| mortar against grunt, before | 21–34, 14 values | 6–10 | 5 |
| mortar against grunt, after | 210–340, 131 values | 6–10 | 5 |
| mortar against runner, before | 21–34, 14 values | 4–6 | 3 |
| mortar against runner, after | 210–340, 131 values | 4–6 | 3 |

**The bounds are identical in all four pairings.** A best-case volley kills in the same number of shots it
always did and so does a worst-case one. Nothing about the balance of the committed match moved.

**The number of values the roll can take did not stay identical, and that is the whole of it.** The bolt drew
from seven values and now draws from sixty-one; the mortar drew from fourteen and now draws from a hundred
and thirty-one. `Match.Fire` rolls once per shot with `Pcg32.NextInRange(min, max + 1)`, so the stream's
position stays a count of shots fired, and the *value* that comes out of each draw is a different number
because the span it is being folded into is ten times wider. Every shot lands a different amount, a creep
crosses zero on a different tick, and the match is a different match from the first kill onwards.

One detail worth having written down, because it is the one way a wider span *could* move the stream's
position rather than only its values: `Pcg32.NextBelow` is rejection-sampled, and its threshold is
`2^32 mod span`, so a shot occasionally consumes two underlying draws instead of one. The thresholds here are
4 for the bolt's old span of 7 and 57 for its new span of 61, and 4 and 117 for the mortar's 14 and 131 — a
rejection probability of 9.3 × 10⁻¹⁰ rising to 2.7 × 10⁻⁸. Across the few hundred shots of this match neither
scale rejects anything. The stream advances once per shot before the rescale and once per shot after it, and
what changed is only what each draw was folded into.

## What that does to the committed run, landmark by landmark

| landmark | before | after |
|---|---|---|
| `first-overtake` | tick 366 | tick 366 |
| `first-leak` | tick 551 | tick 551 |
| `projectile-orphaned` | tick 224 | tick 231 |
| `last-creep-dies` | tick 1840 | tick 1829 |
| result | 12 of 40 leaked, tick 1852 | 13 of 40 leaked, tick 1841 |

**The two that did not move are the two that do not depend on anything dying.** An overtake is two speeds and
a release cadence; the first leak is the creep that walked the corridor without being shot enough to stop it.
Neither reads a damage number. The two that moved are both about the moment something died — a shell losing
its target mid-flight, and the last creep starting its death — and both of those are downstream of a roll.

The state hash moves earlier still, on tick 0, before a shot is fired at all: it folds a creep's hit points,
and the first creep of the wave is released on that tick carrying 2000 of them where it used to carry 200.
A trace that diverged at tick 0 after a rescale is therefore expected, and says nothing on its own.

## What the wider range was bought for

Resolution, at the scale the numbers are actually authored at. Armour reads as one percent of base effective
health per point, so the question is how many distinct results a hit can produce across armour's range:

| hit | distinct results, armour 0–20 | distinct results, armour 0–100 |
|---|---|---|
| 9 (bolt, low roll, before) | 3 of 21 | 6 of 101 |
| 34 (mortar, high roll, before) | 7 of 21 | 18 of 101 |
| 90 (bolt, low roll, after) | 16 of 21 | 46 of 101 |
| 340 (mortar, high roll, after) | 21 of 21 | 99 of 101 |

At the pre-rescale scale a nine-damage bolt deals the same number for eleven consecutive points of armour.
That is armour a player cannot feel and a sweep cannot tune, and it is the reason for the rescale: it buys
back the resolution without touching either the matrix or the armour expression.

## How to tell this apart from a real desync

A desync is a divergence between two runs that should be the same run. This is a divergence between two runs
of *different content*, and the content hash says so: the committed bundle's header moved from
`39B848CEFDDCC9CF` to `7226DE14812FDBEA`, and the replay gate refuses the old bundle against the new table by
name. If a trace moves and the content hash has **not** moved, that is the desync — and
`GoldenTraceTests` names the tick it started on.

## Sources

Measured in this repository at the commit that landed the rescale.

- `content/units.txt`, `content/landmarks.txt`, `content/golden-trace.txt` — before and after.
- `sim/Match.cs`, `Fire` — one draw per shot, on the one stream.
- `sim/Pcg32.cs`, `NextInRange` — how a draw is folded into a range.
- `docs/prototypes/damage-matrix-arithmetic.py`, section 8 — the resolution argument the rescale was decided on.
- `docs/adr/0003-dice-rolled-once-per-shot.md` — why the stream's position is a shot count.
