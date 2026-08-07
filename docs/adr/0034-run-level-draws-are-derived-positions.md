# 0034 — Run-level draws come from derived positions; the match keeps its one stream

A run makes draws a match does not: which K opponents a round's field is made of, and later which options the
offering carries and which game changers fill an anchor. None of them happens on the match's stream. Each
starts a fresh generator at a position derived from the run's seed and from where in the run the draw is
wanted:

```
position = fold("run-field/1")
             .add(run seed)
             .add(round, opponent)
             .add(side)
```

Every match a run resolves is then seeded the same way, from `"run-match/1"` and the pairing it belongs to.

## What was decided

**ADR-0031's rule is scoped to the match, not widened.** Inside a match there is still exactly one stream,
taking exactly one input — the seed the record carries — and the damage roll is still the only thing that
draws from it (ADR-0003). What changed is that the seed a match runs on is now itself derived rather than
authored, and that the run above it draws elsewhere. The property ADR-0031 was protecting is untouched: a
match's stream position is a running count of the shots fired in it, so a unit-ordering desync still diverges
the state hash on the tick it happened.

**A derived position is not a stream selector.** The thing ADR-0031 rejected was a knob a subsystem picks at
runtime, where two subsystems can disagree about which stream they are on and the symptom is a desync with no
bad line to point at. A derived position is a pure function of the run's seed and of coordinates the run
already has — the round, the pairing, the side. Nothing chooses; the coordinates are where the draw is.

**Derived rather than continued, so a run is reproducible from its record.** Continuing one generator across
a run would make round seven's field depend on how many values rounds one to six consumed, and the number of
values a round consumes depends on what was played. A server re-validating one round, a sweep resuming a row
and a replay of a submitted run would each need every preceding round to have been re-simulated identically
first. Derived, round seven's field is the same whatever came before it.

**The fold is the same one the content hashes use.** `Hash64` is nine lines of specified integer arithmetic
producing identical results under Mono, IL2CPP and CoreCLR — the same reason ADR-0011 folds with it. A
purpose label starts each fold, so a field draw and a match seed at the same coordinates cannot collide, and
the digit in the label is a layout version: changing what goes into a derivation bumps it and retires every
run recorded against the old one.

## What it costs

**A run's seed is now load-bearing in more places than a match's ever was.** One number decides the field of
every round and the dice of every match in it. That is what makes a run reproducible from its record and it
is also what makes the seed the single thing a stored run cannot be missing.

**Two labels are two things to keep distinct.** `"run-field/1"` and `"run-match/1"` are strings, and two
draws that accidentally shared one would be correlated in a way nothing would report. They are constants on
`Run` for that reason, next to each other, rather than written at the call sites.

**A derivation is not free the way advancing a stream is.** Every match a round resolves starts a generator
of its own. At a field of ten that is twenty constructions a round, which is nothing beside the twenty
matches themselves.

## What was rejected

**One generator carried across the run.** Cheaper, and it makes the position ambient in the sense that
matters: where the stream sits is a consequence of everything played so far. `RunTests` has the assertion
that distinguishes the two — two runs on one seed play different openings and then the same third round, and
the third round has to come back identical.

**Drawing the field on the match's stream.** It would dilute the one diagnostic property the match stream
has, because its position would stop being a count of shots.

**Authoring a seed per match.** A run would then carry N×2K seeds in its record instead of one, and every one
of them would be a number somebody could get wrong.

## Where it lives

`sim/Run.cs` — `FieldSeed`, `MatchSeed` and `Derived` — and `sim.tests/RunTests.cs`.
