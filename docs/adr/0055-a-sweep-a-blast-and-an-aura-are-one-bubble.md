# 0055 — A sweep, a blast and an aura are one bubble, and n shots are not one

**Decided.** `content/units.txt` layout 3 adds nine columns (`shield`, `targets`, and the seven `bubble*`
columns), and a radial shot, a timed modifier and a periodic aura are **one mechanic**: a bubble that emits
something, differing only in where it centres (`self` or `target`), how often it fires (`bubblePeriod` 0 fires
with the attack; positive pulses, which is what makes it an aura) and what it carries (`damage`, or a
percentage of speed, cooldown, armour or shield). A slow is a bubble of radius 0 on the target; a whole-board
pulse is one row with a big number in it. Every bubble measures its sphere with `Reach.Encloses`
([ADR-0054](0054-height-is-a-relationship-and-a-radius-is-a-sphere.md)), so "in range" has one answer.

- `targets` of *n* fires *n* shots at *n* creeps and draws *n* rolls; a damage bubble is **one** shot drawing
  **one** roll, applied whole to everything it encloses. A row claiming both is refused where the columns are
  read, because how many numbers an attack takes off the dice stream is the determinism contract.
- `range` is refused as a payload by name: coverage is intersected with the route once at load
  (`TowerCoverage`), and a payload that moved a range would rebuild those intervals inside the tick.
- A shield absorbs first and raw — before the matrix and the armour denominator — and overkill carries through
  to health, typed there. A roll the shield swallows whole deals nothing.
- A bubble is evaluated over the creeps on the map on the ticks it goes off, and nowhere else; range stays
  one-dimensional.
- `bubbleRadius` spells absence `none` and folds it as −1; a radius of 0 is a real authoring (the target
  alone) and `Bubble.ReachesOnlyItsCentre` answers it before `Reach.Encloses`, whose *false* at zero is the
  range column's rule and is not softened for bubbles.

**Cost.** `SimulationVersion` 8 → 9, `unit-types/2` → `/3`, `match-state/1` → `/2`; every stored record under
version 8 is retired. The committed match did not move — no row authored a bubble — but every hash did. The
cost rule's `bodies` term now reads `targets` (it guessed 3 from projectile delivery), which exposed the
Mage's 92 as three bodies' worth of a splash nobody had authored; `ContentTests` pins that rather than hiding
it. Range, radius, shield and duration stay unpriced
([ADR-0063](0063-a-price-is-derived-from-the-row-and-its-silences-are-named.md)).

**Rejected.** Three mechanics with three column sets (same count, three chances to disagree about uphill). A
bubble measured along the route (cheaper, and wrong on a fold: the Necromancer would shield the column behind
it and not the leg beside it). A second damage amount in `bubbleMagnitude` beside a `damage` payload — #213's
table allowed it; it would be a second draw. A flag column for absent-versus-zero. Pricing any of it.

Decided in #213, built in #216, the effect model in #217 ([ADR-0056](0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md)); the
narrative is [the decision log](../decision-log/2026-08.md#16-august-2026-later--one-format-version-and-the-map-it-is-for).
