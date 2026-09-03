# 0042 — The distribution the bands are read off is measured from the pool, and a walk folds a ceiling

> **Largely superseded by [#209](https://github.com/ssalter21/tower-defense-game/issues/209), 14 August 2026,
> and the question of what to do about it is open.** The bands are gone: gold is paid for the health damage a
> wave does, at a rate on leak cost dealt, so nothing reads a percentile any more. What survives from this ADR
> is the walk's ceiling, and it survives with a better bound — the full price of the wave a stored decision
> composes, rather than the top band.
>
> **What has no consumer left**: `PerformanceField`, `Run.Field`, `Run.FieldSamples`, `MeasureField`, the
> `run-measure/1` draw, and the percentile lookup this ADR exists to justify. They all still compile, are all
> still tested, and nothing in the build calls any of them — a played run no longer touches `Run.Field` at all,
> so the **half a run per run** this ADR records is no longer being spent.
>
> **Whether the machinery is deleted or kept is a decision for a human, not a cleanup.** Deleting a working
> capability is not an agent's call. What it costs to keep is one measurement's worth of code that nothing
> exercises in anger; what it costs to delete is the only implementation of "where does a run sit against the
> field", which a placing or a ladder would want back. See
> [the open question](../open-questions.md#is-the-field-measurement-kept-now-that-nothing-prices-off-it).
>
> **Amended by [#208](https://github.com/ssalter21/tower-defense-game/issues/208), 15 August 2026: the pool is
> a population per round, and the measurement stays flat.** A round is fought against the members recorded at
> that round, so an opponent starts small and grows as a run does; the measurement below draws its samples and
> their opponents over the whole population at once, round structure flattened away.
>
> **That is resolution 2 of the two the ticket put up, and it is the alternative this ADR rejected.** The
> rejection stands as written — a pool and a distribution that describe different populations, with nothing
> saying so — and what makes it the right answer now is that the distribution has no consumer: since #209 a
> wave is paid a share of what it dealt, so nothing reads a percentile and the population the measurement
> describes is a population nothing prices off. Resolution 1 — measure per round, compare like with like — is
> the answer if anything ever prices off it again, and it costs the sweep a multiple of the round count.
>
> **What it costs to sweep, measured rather than estimated:** the committed sweep goes from **42 seconds to
> 78**, on the same machine and the same 9,600 matches. The match count does not move — a played run has not
> touched `Run.Field` since #209 — and what nearly doubles is how long a match takes, because a round-ten
> column is ten times as deep and runs 6,098 ticks against 1,913. Resolution 1 would have multiplied the match
> count instead.
>
> **What still holds from the sentence below:** the payment is a fold, the field is measured once per run, and
> there is still exactly one pool argument, so swapping the canned stand-in for a real ghost pool is still that
> argument and nothing else. What no longer holds is that the spread is the spread of the opponents this run
> will actually be scored against; a round-seven opponent is not in the population any single round fights.
>
> **Amended by [#222](https://github.com/ssalter21/tower-defense-game/issues/222), 29 August 2026: a member of
> the pool builds its wall out of a purse, by the rule a run builds by.** The stand-in was a layout and a wave
> script; it is now a recorded round with an economy. It opens holding the ruleset's starting purse, hands what
> stands and what it holds to `CoverThenUpgradeBot`, pays through `BuildPhase.Resolve`, and closes on
> `Purse.CloseWave`. `content/defense.txt` is the wall it opens with rather than the wall it stands behind in
> every round.
>
> **The reason it reuses the run's own arithmetic rather than getting its own is that two economies can
> disagree**, and a stand-in whose wall was priced by a second rule would drift from the one a run is scored
> against without anything failing. There is one buy policy and one purse, called twice.
>
> **Four things a recorded round cannot have the same as a played one**, each a modelling choice rather than an
> omission. It is paid **no bonus**, because a wave is paid a share of the leak cost it dealt and nothing here
> resolves a round of its own. Its **wave is not priced**, because `content/field.txt` is calibrated as what a
> round's wave costs *after* a wall, so pricing it would price one wave twice — but the offensive share still
> leaves the purse, so only what the wall declined to spend compounds. Its **opening wall is handed over rather
> than bought**, because the committed six cost 344 gold against an opening purse of 100 and charging for it
> would refuse most layouts anybody could author. And it therefore banks **less** than a run does, so "the
> income a run has had" means the income *line* and not the whole of what a run holds.
>
> **What it bought, measured rather than claimed:** six rounds of ten. The wall grows 344 → 396 → 448 → 448 →
> 500 → 552 gold and then stops, because the route is covered so nothing can be placed and every placement is
> the dearest row so nothing can be upgraded. Worse, the dearer wall is a **weaker** wall — the committed run's
> total dealt rose 331 → 375 — because the bot upgrades archers into mages without knowing what either is
> worth.
>
> **Amended by [#236](https://github.com/ssalter21/tower-defense-game/issues/236), 3 September 2026: the bot
> buys by value on a covered route.** Both halves of the paragraph above are gone. It scores a second tower on
> covered route and an upgrade by one number and buys the higher, so the stand-in's wall grows in nine rounds
> of ten — 384 gold to 872 — and the committed run's total dealt fell 375 → **0**. The numbers are in
> [the decision-log entry](../decision-log.md).
>
> **Amended by [#237](https://github.com/ssalter21/tower-defense-game/issues/237), 3 September 2026: the pool
> is a folder of stored rounds and the canned field is what stands in where one is thin.** The sentence this
> ADR still stood on — *swapping the canned stand-in for a real ghost pool is that one argument and nothing
> else* — has been spent, and it held: `Run` still takes one `FieldPool` and nothing beside it, and what
> changed is what a pool is made of. See
> [ADR-0057](0057-a-stored-round-is-a-wall-and-a-wave-at-a-stage.md).
>
> **The stand-in did not go and could not.** A stage nobody has stored a round at still has to resolve, so the
> canned opponent fills the slots a stage cannot — which is why every measurement below, and every committed
> artefact taken under it, is unmoved by a folder nobody has seeded.
>
> **What the measurement now reads is wider.** `MeasureField` draws over the whole population with the stage
> structure flattened away, and that population is now the stored rounds *and* the stand-ins rather than the
> stand-ins alone. Nothing prices off it, so nothing moved; what it costs is that the spread a populated pool
> reports is a spread over a mixture, and resolution 1 above is still the answer if anything ever prices off
> it again.
>
> **The open question is untouched.** `PerformanceField`, `Run.Field`, `Run.FieldSamples`, `MeasureField` and
> the `run-measure/1` draw are all still here and still have no consumer; #237 neither deleted them nor gave
> them one. Whether the machinery is kept is still
> [a decision for a human](../open-questions.md#is-the-field-measurement-kept-now-that-nothing-prices-off-it).
>
> The rest of this file is left as it was written. It describes rules that were in force and is a record of
> them, not a description of the build.

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
