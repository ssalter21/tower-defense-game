# 0042 — The distribution the bands are read off is measured from the pool, and a walk folds a ceiling

The economy pays a wave a flat base plus a performance bonus in progressive percentile bands. The bands, the
percentile lookup and the payment were built with the purse (ADR-0035 and the ruleset's `band` rows); what they
had nothing to be measured against was a **distribution of other players' rounds**, and no population of stored
runs exists until ghosts are recorded. This is how the missing half was supplied without either waiting for the
ghosts or inventing a second concept to carry it.

## What was decided

**The pool a run is handed is the population its percentile is a percentile of.** `Run` already takes a
`FieldPool` — who a round is fought against — and it now measures that same pool to get what a round of it is
worth. There is no second argument, so a run cannot be handed a pool and a distribution that disagree, and
**swapping the canned stand-in for a real ghost pool is that one argument and nothing else**. The alternative
considered and rejected was a `PerformanceField` parameter beside the pool: it reads as more flexible and is
strictly worse, because the two would be free to describe different populations and nothing would say so.

**The measurement is the offense half of `FieldSamples` of the pool's own rounds.** Each sample is one member's
wave sent at the field that sample's draw put in front of it, and each is the **average of its K resolutions**,
exactly as the run's own round is (ADR-0035's rule, symmetrically with the damage rule). A percentile compares
one number against a spread of numbers, so both sides have to be the same measurement — scoring an averaged
round against unaveraged single matches would widen the field's tails and pin every honest run to the middle
band. Only the attacking direction is resolved, because leak cost dealt is the whole of what the bands read, so
the measurement costs half a round rather than a whole one.

**It is fixed for the whole run, and that is what keeps the payment a fold.** Every round is placed against the
same spread, so what a round paid is `BandFor(field.PercentileOf(round.LeakCostDealt))` — arithmetic over the
outcome vector and a value the run already carries. `Purse.BonusOver` is that fold and the sweep reports its
result, so what a run earned for its offense needs no tick replayed and no match resolved. A field that changed
per round would have made a retrospective need K stored numbers a round that the vector does not carry.

**It is measured on first use rather than in the constructor.** Measuring plays matches; a caller that only
wants to read a run's offerings should not pay for them. What it measures depends on the seed, the pool and K
alone — nothing a round moves is in it — so when it happens cannot change what it says.

**A walk over a stored command stream folds a ceiling, not the run's own purse.** `CommandStream.Check` folds
the purse forward to decide whether each stored decision was affordable, and a wave's income now includes the
band its offense reached, which is a number only a resolved round has. So the walk closes every wave at
`Purse.CloseWaveAtBest` — the top band, whatever the run did. The bound is above rather than below on purpose:
**every decision the walk refuses was unaffordable however well the run played**, and a decision the ceiling
admits is checked again against the purse the round really holds by the same
`BuildPhase.Resolve` when the round is played. Bounded below — at no bonus, which is what the walk folded while
the bonus was zero — it would refuse at load exactly those waves a run's own bonus paid for.

What that costs is honest and small: the promise that a stream passing `Check` always replays is now the
promise that everything settleable without playing the run is settled before the first round. Affordability
under a bonus the run has not yet earned is the one refusal that can still land mid-run, and it is not a
property any walk can have.

## Why not the alternatives

**Pay against the K opponents of that round.** Free — those numbers are already computed inside `Advance` — and
rejected: the K per-opponent values are not on the outcome vector, so a retrospective would need them stored
beside it and the bands would stop being a fold over what a run records.

**Derive the field from the pool without simulating** — the cost of each member's wave, say. Rejected: leak
cost dealt is what the bands measure, a wave's price is its ceiling rather than its result, and a stand-in that
measures a different quantity is not a stand-in.

**Keep paying zero until step 6.** This is the named trap in the step-1 spec: the cost column and the base
income are the computable half of the economy, and shipping them alone leaves attacking a pure tempo loss and
the whole percentile mechanism unexercised. A canned distribution that exercises it is worth more than an
accurate one that does not exist.

## Consequences

The bonus is money. Against the committed sweep it comes to **7.8% to 10.4% of the flat base** across the
roster, and the extra income buys creeps, so the report moved: the win-rate column went from a 16%–34% band to
a 41%–66% band and every creep's leak cost dealt rose by half again. The ordering between creeps carried over,
which is the part that says the report is still an instrument.

**It costs half a run per run.** A round is `FieldSize` opponents in both directions and the measurement is
`FieldSamples` samples in one, so the committed sweep goes from 9,600 matches to 14,400 and from about eight
seconds to about thirteen. Measuring once per sweep rather than once per run would have been cheaper and would
have made the distribution a property of the harness rather than of the run's own seed; a run is the thing that
is paid, so it is the thing that measures.

**The four authored bands behave as two against a population of one.** Ten samples of the canned opponent land
between 14 and 19 gold, so a round is above the field or below it and almost never inside it; the 50th and
75th thresholds are reachable only by a round dealing exactly 18 or 19. That is a property of the stand-in and
not of the thresholds — the same code against a pool of four members uses all four bands — so the numbers are
deliberately **not** retuned here. The measurement is in
[`a-canned-field-of-one-collapses-the-bands.md`](../research/a-canned-field-of-one-collapses-the-bands.md).

`content/sweep.csv` gains two columns, `income_base_gold` and `bonus_gold`, as two integers rather than a
ratio — the rule ADR-0041 set for every rate in that file. A reader can see the second is not zero and divide
one by the other without trusting anything this code computed.

The zero-bonus note that steps 1 to 3 were supposed to carry is retired everywhere it was written: the remarks
on `PerformanceField.Absent` and `WavePayment.Bonus`, the comment on the `band` rows in `content/ruleset.txt`,
the `Run.Field` and `Run.Purse` remarks, the `Sweep` class remark, the paragraph in `PurseTests`, and the two
paragraphs in `docs/vision.md`. A build-order fact that outlives its cause is worse than none, because it is
the sentence a reader lands on when they suspect a bug.
