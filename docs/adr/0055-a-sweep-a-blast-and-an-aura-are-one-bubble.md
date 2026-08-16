# 0055 — A sweep, a blast and an aura are one bubble, and n shots are not one

`content/units.txt` layout 3 adds **nine columns**, and three of the five levers the roster was blocked on
turned out to be one mechanic. The column count is identical either way; what collapsing them saved is three
sets of rules with nothing holding them together.

| Column | Meaning |
|---|---|
| `shield` | A pool that absorbs first and raw. Armour does not apply, overkill carries through to health, it does not regenerate. 0 = none |
| `targets` | Shots per attack, each its own damage roll, taken nearest-to-exit first. 1 = an ordinary single shot |
| `bubbleRadius` | Milli-hex, read as a sphere. 0 = the target alone; `none` = no bubble |
| `bubbleOrigin` | `self` — a sweep, on the emitter — or `target` — a blast, on what the shot hit |
| `bubbleAffects` | `friend` or `enemy` |
| `bubblePeriod` | Ticks. 0 fires with the attack; positive pulses on its own, which is what makes it an aura |
| `bubblePayload` | `damage`, or one of speed, cooldown, armour, shield. **`range` is refused by name** |
| `bubbleMagnitude` | A percentage, for every payload that is not damage |
| `bubbleDuration` | Ticks. 0 is instant, which is what damage always is |

The list was decided in [#213](https://github.com/ssalter21/tower-defense-game/issues/213) and is fixed. Built
in #216. `SimulationVersion` 8 → 9, the table's hash label `unit-types/2` → `unit-types/3`, and the match's
state-hash label `match-state/1` → `match-state/2`.

## What was decided

**A radial shot, a timed modifier and a periodic aura are the same thing.** Each is a bubble that emits
something, and they differ only in where it centres, how often it fires and what it carries. The Soldier's
sweep is `self`, no period, `damage`. A mortar's blast is `target`, no period, `damage`. The Cryomancer's slow
is `target`, radius 0, `speed`, a negative percentage and a duration — *no dedicated slow columns exist*. The
Captain is `self`, `friend`, a period. The Necromancer's shield is `self`, `friend`, `shield`, and because a
radius is measured in hexes rather than along the marching column it reaches the neighbouring leg of a fold. A
tower that pulses over the whole board is one row with a big number in it.

Authored as three mechanics these would have needed the same nine columns and three chances to disagree about
what "in range" means. As one they share `Reach.Encloses`, which is the sphere rule of
[ADR-0054](0054-height-is-a-relationship-and-a-radius-is-a-sphere.md) and was written before it had a caller
for exactly this reason: a bubble authored against the shot's signed rule would be a cliff-blanketing aura
found after it shipped.

**`range` is not a payload, and the refusal says why.** A tower's coverage is intersected with the route once,
at load, and handed to the tick loop as intervals of distance — that is what
[`TowerCoverage`](../../sim/TowerCoverage.cs) is and it is where the two dimensions stop. A payload that moved
a range would have to rebuild those intervals inside the tick. Refusing the word with its reason attached is
worth a branch, because the alternative refusal — "not one of: none, damage, speed, cooldown, armour, shield"
— reads as an omission and invites somebody to add it.

**The two shot shapes are distinct, and a row is one of them.** `targets` of *n* fires *n* shots at *n* creeps
and draws ***n*** rolls. A damage bubble is **one** shot drawing **one** roll, applied whole to everything it
encloses — no falloff, no friendly fire. How many numbers an attack takes off the dice stream is part of the
determinism contract: the stream's position is folded into the state hash every tick, so a row claiming both
shapes is refused where the columns are read rather than reconciled at the landing.

**A shield absorbs first and raw.** It is spent before the matrix cell and the armour denominator are looked
at at all, which is the whole of what makes it a different lever from health rather than a bigger pool: a
point of shield is worth exactly one point against every attack type there is. Overkill carries through to
health and is typed there, so a shield delays a body by its own size and never by a whole shot. A roll the
shield swallows whole deals nothing — the damage floor is a guarantee about hits that resolved through the
matrix, and a hit that never reached it would otherwise leak a point past a pool that stopped it.
`ArmourType.Arcane` is unrelated and keeps its name.

**A bubble turns one dimension back into two, at the moment it fires and nowhere else.** Range stays where it
was: intersected with the route at load. A bubble is evaluated over the creeps actually on the map, on the
ticks a bubble actually goes off, which is a walk of a few dozen integer comparisons a few times a second
rather than a per-tick cost. Precomputing an enclosure table per route cell was the alternative and it buys
nothing — the centre of a blast is a creep's cell and moves every tick, so the table would be indexed by the
thing that varies.

## What the tick loop does not do yet

**Only one bubble shape resolves: damage, against the enemy, fired with the attack, landing instantly.**
Everything else — a period, or any payload that is not damage — is a per-creep timed effect: a modifier that
lasts a duration and expires, strongest-wins with the timer refreshed. That is #217's and half-building it
here would mean building it twice.

**A row authoring one is refused when a match is built from it, by name.** Not at the landing, and not
silently: `Match` walks its layout and its wave at construction and refuses a row whose bubble it cannot
resolve. The failure being engineered out is the quiet one — a Cryomancer standing on the board, firing, and
slowing nothing, with a column that parsed perfectly and nothing anywhere saying so. Such a row is authorable,
hashable and storable; it does not play, and it says which of those it is.

## What was rejected

**Three mechanics with three sets of columns.** Same count, three chances to diverge. The moment "does a slow
reach uphill?" and "does a blast reach uphill?" have two answers, one of them is a bug nobody wrote down.

**A bubble measured along the route.** Cheaper — a creep's distance is already a number on a line — and wrong
where the corridor doubles back: a Necromancer would shield the creeps behind it and not the ones standing a
hex away on the next leg of the fold, which is the case the mechanic exists for.

**A second damage number on a damage bubble, and this one is a deviation from #213's column table.** That
table reads `bubbleMagnitude` as "a damage amount, **or** a percentage"; here it is a percentage and nothing
else, refused as non-zero beside a `damage` payload. The two clauses of the ticket collide: a bubble is
declared to be one shot drawing one roll, so the damage it carries is that roll, and a column holding a second
amount would be a second damage source with a draw of its own — which is precisely the determinism contract
the same ticket says to get right the first time. The column is kept (it is not dropped, and the list is
fixed); what narrowed is the set of values it may hold beside one payload.

**Splitting "absent" from "zero" with a flag column.** `bubbleRadius` carries the word `none`, and the six
columns after it are then required to say the same. A radius of zero means the target alone and is a real
authoring — the Cryomancer's — so the two cannot share a spelling. Absence folds as `-1`, which is not a
radius any row can author, so "no bubble" and "a bubble of no radius" cannot hash equal.

**And that is why a bubble's zero is answered before [`Reach.Encloses`](../../sim/Reach.cs) is asked.** That
rule answers *false* at a radius of zero, deliberately and for the range column's sake:
[ADR-0054](0054-height-is-a-relationship-and-a-radius-is-a-sphere.md) settled that no reach is not a short
reach, because every walking row authors zero in `range` and the signed term alone would otherwise hand a
creep two tiers up a whole hex of reach. A range column has no other spelling for "none"; a bubble does. So
`Bubble.ReachesOnlyItsCentre` takes the degenerate case and the sphere keeps the one meaning it has —
softening the shared comparison to suit a bubble would have reopened a decision that has nothing to do with
bubbles.

**Pricing any of it.** The placed-unit cost rule's `bodies` term now reads `targets` — it guessed 3 from
`Delivery == Projectile` before, so a Marksman would have been priced at a single-target Archer's price.
Range, bubble radius, shield and duration stay **unpriced**, deliberately, and that silence is recorded in
[open questions](../open-questions.md): a coefficient guessed against the one-hex corridor is a coefficient
priced against geometry that is going away.

## What it cost

**Every stored record made under simulation version 8 is retired**, and every record pinned to the layout-2
roster with it. Both were expected: a column is a format version, which is what
[ADR-0044](0044-a-new-unit-is-a-row-never-a-column.md) says a column costs, and this is the widening the
roster was told it could have.

**Nothing about the committed match moved.** Every row of `content/units.txt` authors one shot, no shield and
no bubble, so the same wave leaks the same twelve creeps on the same tick 5283, the landmark ticks are
unchanged and `content/sweep.csv` is byte for byte what it was. What moved is the hashes: the table's under a
new label, the match's under a new fold, and every artefact that carries one of them.

**One correction landed with it and it is visible rather than tidy.** The Mage costs 92 gold, which is three
bodies' worth of the cost rule, and it fires one projectile at one creep — its splash has been a design
statement in [the roster](../roster.md) and never a thing the simulation did. The old `bodies` guess hid that;
`targets` does not. #216 authored no bubble on the Mage and moved no price: `ContentTests` pins the finding
instead, because deciding what a Mage is belongs to whoever signs the roster.
