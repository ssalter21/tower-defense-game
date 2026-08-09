# 0033 — One fused damage expression, evaluated once, behind a named pipeline

A shot's dealt amount is one expression with one multiply, one divide and one truncation:

```
dealt = (base + bonusVsTag) * cell / (denominator + coefficient * armour)
if (dealt < floor) dealt = floor;
```

Every stage a damage number passes through is a named entry on `DamageModel.Stages`, an ordered list. Today
it has exactly one entry on it.

## What was decided

**The fused form is the rule, and the two-step form is not the same rule.** Applying the type chart and then
the armour as two separate divisions is algebraically identical and arithmetically different: in integers it
truncates twice. Swept over the 411,600 triples the prototype swept — base damage 1 to 100, matrix cell 5% to
200%, armour 0 to 20 — the two forms disagree on 175,759 of them, 42.7%, and the fused form is never the
lower of the two. `DamageTests` re-measures that rather than citing it.

**`bonusVsTag` joins the hit before typing and mitigation.** A counter is an addition to the base, so a
high-armour target blunts the thing built to kill it along with everything else, and armour keeps meaning
something against its counter.

**The floor is applied after the pipeline, not inside a stage.** It is a guarantee about the amount dealt
rather than a transformation of it, and a floor inside each stage would be applied once per stage.

**The stat pipeline is a list, and applying a stage means being on it.** `DamageModel.Dealt` walks
`Stages` and applies nothing that is not on it. A stage declared in `StatStage` and left off the list changes
no damage number anywhere, which the build gate catches by enumerating the declared values against the list.

## What it costs

**A second stage is a real decision and it is inconvenient on purpose.** Adding one means editing the enum,
the list and the switch, and turning a test red that asserts the list has one thing on it. That is the point:
each stage is a truncation, so the number of them is the integer contract.

**The matrix and the expression's shape are data, so the arithmetic cannot be read off the source alone.**
The cells, the coefficient, the denominator and the floor all live in `content/ruleset.txt`. What the source
pins is the *shape* — one multiply, one divide, one truncation — and what the file pins is the numbers. A
sweep can move the numbers; moving the shape is this file.

**The intermediate is a `long` and the result is an `int`.** The product of a hit and a cell is the one place
this expression leaves the range of an `int`. A cell is bounded where the ruleset is parsed so the product
cannot leave a `long`, and a dealt amount that will not fit back into an `int` throws rather than wrapping —
a wrapped product deals a negative amount, and a negative amount heals.

## What was rejected

**Flat subtraction of armour.** Intuitive, and it punishes many small hits quadratically against few big ones:
five five-damage archers lose the armour five times where one twenty-five-damage cannon loses it once. The
prototype quantifies it.

**A wide matrix.** A 4:1 or 40:1 spread deletes small hits through rounding before any formula is involved,
and a floor that rescues them blinds the table — every cell producing the same output means the type chart
has stopped existing for that hit. 140 against 70 moves shots-to-kill by exactly double, which is a read
rather than an unwinnable draw.

**A keyed collection for the matrix.** Enumeration order is an implementation detail and hashed collections
are banned in this assembly. Nine integers in a flat array indexed `cells[attack * 3 + armour]` are also what
a sweep wants to walk.

**Letting a unit fall outside the matrix.** A row with no attack type and a delivery, or a health pool and no
armour type, is refused at load. The alternative — defaulting to a cell — silently gives every untyped unit
one specific column of the table.

## Where it lives

`sim/DamageModel.cs`, `sim/DamageMatrix.cs`, `content/ruleset.txt`, `sim.tests/DamageTests.cs`, and
`docs/prototypes/damage-matrix-arithmetic.py` for the sweep the decision came out of.
