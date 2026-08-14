# 0035 — A run's outcome is a vector, and health is a clock denominated in gold

A run records one pair per round — what its wave got past the field, and what the field's waves got past its
defense, both priced in gold. Health, waves survived, how the run ended and every score are folds over that
vector, computed in `RunOutcome` and carried nowhere else.

## What was decided

**The vector is the outcome and a score is a fold.** Nothing keeps a running total alongside it. A
percentile band, a placing or a retrospective computed weeks later is arithmetic over a stored vector, which
is what lets the economy be priced against a distribution without re-simulating a single tick. `RunOutcome.Of`
is public for exactly that: a harness holding stored pairs rebuilds the outcome and reads health and waves
survived off it.

**Health is denominated in gold, and a leaked creep costs its price one for one.** The exchange rate is
legible without a table, and it makes underbuilding a defense to fund an offense literally spending health.
Pricing a leak needs to know which creep leaked, which is why a match counts its leaks by the wave order that
sent them rather than as a total: one wave sends two types at two prices.

**A round costs the field's average and not its sum, and scores the average of its K resolutions and not the
best of them.** Summed, a field of ten would cost ten rounds' worth of health and being in a large field
would be a punishment. Taken at its best, a run's luckiest pairing would decide its score. The two rules are
the same rule pointed in two directions, which is what makes a field average a bad draw away rather than
multiply it.

**Gold cannot repair health.** There is no member anywhere that adds to the pool; `Advance` is the only
thing on `Run` that moves, and it only subtracts. The pool is a clock, and nobody is sold a way to stay in a
run they are losing.

**Runs place by waves survived, then health remaining.** The offense is on the vector, it is folded into
`LeakCostDealt`, and it is absent from `CompareTo`. What a wave earns its sender is gold; the ranking has
one meaning.

**Death is an argument and not a rule.** A sweep runs with it off and gets N rounds of data out of every row
rather than a row as long as its luck. The outcome's ending fold reports `OutOfHealth` in preference to
`OutOfWaves`, because a run ends the moment health reaches zero.

## What it costs

**Every read of the outcome folds the whole vector again.** At ten rounds that is nothing. With the round cap
lifted it is quadratic in the number of rounds, and the answer if it ever matters is to fold incrementally
inside `RunOutcome` — not to keep a total on `Run`, which is the arrangement this decision exists to refuse.

**`HealthRemaining` floors at zero, so how far past death a no-death row went is not on that fold.** It is
still in the vector: the per-round pairs keep every point of overkill, and a harness that wants the depth
sums them itself.

**Waves survived is a prefix count and not a round count.** The round that spends the last of the pool is not
one the player survived, and no round after it is either — so in no-death mode the two numbers deliberately
disagree, which is the whole reason both are reported.

## What was rejected

**A single score.** It cannot be recomputed against a distribution that did not exist when the run was played,
and a placing is exactly that. (The performance bonus was a second such consumer until
[#209](https://github.com/ssalter21/tower-defense-game/issues/209) made it a share of what a wave dealt; what
that changed is where the bonus is read from, not that the vector is what it is read from.)

**Health as its own currency.** A second denomination needs an exchange rate, and an exchange rate is a table
nobody can check by eye. Gold is the only denomination in the game and health is spent in it.

**Repairing health with gold.** It turns the wall at the bottom of the pool into a resource, and the pool
stops being a clock the moment it can be wound back.

## Where it lives

`sim/RunOutcome.cs`, `sim/Run.cs`, `sim/Match.cs` (`LeakedByOrder`), and `sim.tests/RunTests.cs`.
