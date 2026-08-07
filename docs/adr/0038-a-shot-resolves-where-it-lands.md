# 0038 — A shot resolves where it lands, and the ruleset is a match's argument

The tick loop rolls damage when a tower fires and turns that roll into an amount when it reaches a creep.
`Match` is constructed with the `Ruleset` beside the map, the defense and the wave.

## What was decided

**The expression runs at the landing, not at the trigger.** `Match.Damage` is the one place a roll becomes an
amount: it already holds the target, and it is already where overkill, a dead target and every kind of stale
reference are discarded. Resolving there means a shot that reaches nothing evaluates nothing, and a shot that
reaches something evaluates [0033](0033-one-fused-damage-expression-and-a-named-pipeline.md)'s expression
exactly once.

A projectile therefore carries the roll and the row that fired it, and resolves on arrival. Resolving at launch
would need the target's armour type at a moment when the target may already be dying — a lookup that returns
nothing, against a shot that still has to exist.

**An untyped shot resolves untyped.** A unit table in column layout 1 carries no attack types and no armour
types, so a shot out of one has no row of the matrix and its target has no column. The roll is what lands. That
is not a fallback: it is what lets the golden bundle pinned to such a table replay to the numbers it was
recorded at, forever, without the ruleset it never knew about reaching into it.

**A shot typed on one side only is an unconditional throw.** One table cannot author it — a unit that attacks
carries an attack type and a unit that can be damaged carries an armour type, both checked at load — so it is a
defense and a wave read out of two tables that were never checked against each other, and there is no cell it
could mean.

**The ruleset arrives in the constructor and is never optional.** A match that could be built without it would
be a match that can reach a typed shot with nothing to resolve it through. A layout-1 replay is handed one and
never consults it, which is a cheaper arrangement than a nullable field and a branch on every landing.

**`bonusVsTag` reaches the tick loop as numbers.** Which game changer a wave order fields is a run-level fact:
a wave order carries a type id, a type id is a body, and two game changers may field one body. `Run` resolves
the pairing where the unlocks and the schedule are in hand and hands `Match` a `ShotBonus` — shooter type,
wave order, amount — so no run-level type enters the tick loop.

## What it costs

**Every construction site of `Match` gained an argument**, including the view and the headless command line, and
`ruleset.txt` now ships in the player's streaming assets. The alternative was an ambient default, which is a
number nobody authored folded into an outcome as though somebody had.

**The damage event carries the dealt amount rather than the roll.** What a view draws over a creep's head is
what the creep lost, which is the useful number; the roll is no longer observable from outside. The state hash
is unaffected — it folds health, not events.

**A golden bundle pins its unit table and not its ruleset.** The oldest goldens are untyped and never reach the
matrix, so a ruleset retune cannot move them; the current-version golden is re-recorded by the same switch that
retunes anything. A typed golden that had to survive a ruleset change would need the ruleset pinned beside it
the way the unit table already is.

## What was rejected

**Resolving inside `DamageModel`.** The untyped case is a fact about a *table*, not about the arithmetic, and
putting a `None` branch inside the expression would make the model answer a question it has no cell for.

**A nullable ruleset meaning "untyped".** It puts the same fact in two places — the table's layout and the
caller's argument — and lets them disagree.

**A `bonusVsTag` looked up inside the tick loop from the unlocks.** `Unlocks.TryChangerFor` is keyed on a type
id, and a type id is the body two changers can share. Doing the lookup per shot would also drag the whole
run-level vocabulary into `Match`.
