# 0041 — The sweep computes rows and the shell writes them, and every rate arrives with its operands

The balance harness is a CLI mode and a comma-separated file rather than a project, because a match resolves in
under three milliseconds and a ten-thousand-matchup sweep is therefore seconds of compute rather than a night
of it. That makes the tool worth building before the roster is large, and it is why it lands here rather than
after the content it is meant to check.

## What was decided

**The harness computes and the command line writes.** `Sweep.Of(SweepPlan)` is a pure function from parameters
to rows; `SweepCsv` turns rows into text and performs no arithmetic. The split is forced rather than chosen —
the simulation may not reference `System.IO` (ADR-0018) and the build gate scans the compiled image for it — and
it is the reason the whole harness needs one behavioural seam instead of a second program. A number that
appeared in the writer and nowhere in the library would be a rule living in the shell.

**Every one of its inputs is a parameter of one plan.** The map, the ruleset, the roster, the anchor schedule,
the defense, the canned field, N, K, the death flag, the offering ratio, the free-snapshot count, the snapshot
price and the build policy. Pointing the sweep at another board to score it, or at another damage matrix, is
then an argument rather than a retrofit across every call site — which is the one thing that is cheap now and
expensive in a year. The offering ratio's two halves, the free-snapshot count and the snapshot price reach the
rules through `Ruleset.With`, which recomputes the content hash over the parsed integers exactly as a parse
does (ADR-0011), so a retuned sweep plays a real ruleset that a stored record is loudly refused against, rather
than a set of overrides carried alongside one.

**Who plays the runs is one of those parameters.** A row's runs favour the creep the row is about, and *how*
they favour it is a strategy: take that creep off the offering where the menu carries it, fill every slot the
round has with an equal share of the purse, bank what is left. That is a scripted player, it is the only
producer of build phases that is not a command stream, and until it had a name every committed number in
`content/sweep.csv` depended on it and on the fold together with nothing separating the two. It is now
`BuildPolicy` — one operation from a run and a preferred type id to a build phase — carried on the plan and
defaulting to `EvenShareBot`, so "score this roster under a greedier build instead" is an argument like every
other input rather than an edit inside the harness. The fold underneath it knows nothing about any of it: its
ingredient bins are sized from what the runs it just played came back holding, not from the wave count, because
a bin width derived from N is a claim about the player that a policy-blind fold cannot check.

> **Amended by [#202](https://github.com/ssalter21/tower-defense-game/issues/202), 16 August 2026.** The
> parameter was reachable only from C#: `RunContent.Sweep` passed no policy, so every sweep a shell could ask
> for was played by `EvenShareBot`. It is `--policy` now, over `even-share` and `all-in`, and an unrecognised
> name is refused rather than defaulted — a fallback would produce a complete, correct-looking report about a
> player nobody asked for. The name is written into the file as a parameter row, because two reports played by
> different strategies share every other parameter and differ in every number.
>
> The same issue gives the report a fourth kind of row. `--per-run` keeps a row per run under the folded ones,
> filling the creep row's own headings plus the seed it was played on, so grouping them lands on the fold
> exactly. That does not move this decision: the harness still computes the rows and the shell still only
> writes them.

**A rate is basis points, beside the two integers it came from.** There is no floating point in the simulation
and the gate scans for it, so a ratio has to be an integer. Basis points rather than per-mille because a sweep
of a few hundred runs a cell distinguishes cells the coarser scale rounds together, and rather than `Fix64`
because that type's truncation is part of the simulation version (ADR-0001) and a report's rounding has no
business being pinned to the tick loop's arithmetic. The numerator and the denominator are on the row as well,
so a spreadsheet can recompute the rate exactly and nobody is stuck with this code's division.

**Coverage is always stated, bounded or not.** A truncated sweep that said nothing would read exactly like a
complete one — same columns, same shape, fewer rows — and nobody opening the second one would know to ask. So
completeness is a value in the output rather than the absence of a warning: the roster axis says how many rows
of it were scored out of how many exist, and the seed axis says it is a sample whatever its size, because the
seed space is 2^64 wide and no run count enumerates it.

**The canned field is part of the economy rather than a tool pointed at it.** The percentile bands are measured
against a distribution of other players' rounds and no such pool exists until runs are stored, so the pool a
sweep is handed is what the bands are computed against — literally, since ADR-0042 measures it.

> **Amended by [#209](https://github.com/ssalter21/tower-defense-game/issues/209), 14 August 2026.** The bands
> are gone and nothing prices off the distribution, so the pool is no longer measured to pay anybody. The
> paragraph's conclusion survives on a different argument: a round is still resolved against K members of this
> pool, so what the sweep is handed is still what a run's damage is dealt to, and the two file choices below
> are unaffected.

`content/field.txt` is that stand-in, and it is deliberately not `content/wave.txt`: the
authored match is forty creeps and three hundred and eighty gold, and a run's purse is paid one wave's base a
wave, so a field member composed of it outspends every opponent it faces three and a half times over. A sweep
against one reports a total loss on every row and names nothing.

**Cost efficiency is documented for what it measures and what it does not.** A leak charges health equal to what
the creep cost to send, one for one, so leak cost dealt over gold spent is the cost-weighted leak rate of what
was sent and the price level cancels out of it exactly — halving a creep's price doubles how many of it a purse
buys and halves what each leak charges. The column therefore **cannot** say a creep is overpriced; the
measurement behind that is in
[`cost-is-not-a-balance-lever-under-a-one-for-one-leak.md`](../research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md).
It is kept anyway, and it earns its place for a different reason: winning is one bit a run and it saturates
against a canned field of one, while this column stays graded and goes on separating creeps after the win rate
has flattened. It is named `cost_efficiency_dealt_per_100_gold` so that the unit travels with it.

## Consequences

The whole roster is scored from a shell with no engine, no licence, no editor and no session in it, and 38,400
matches take about thirty seconds against the committed Debug image.

`content/sweep.csv` is committed and is the golden trace's rule two levels up: the trace pins a match tick by
tick, `run-outcome.txt` pins a run round by round, and this pins a population of runs — so a retune that
reorders the roster is a diff rather than an argument. Nothing that checks it regenerates it;
`tools/run-sweep.ps1 -Regenerate` is the only writer and its `-Verify` mode sweeps into scratch space to
compare.

The report is the first file this project writes whose separator is a character a locale uses inside a number,
so the writer refuses any cell carrying a comma, a quote or a line break rather than quoting it. Every value in
it is an integer under the invariant culture or a label off a content file, and a content file's parser refuses
both characters on a data line before it tokenises — so the guard should never fire, and the failure it exists
for is the quiet one: a stray separator shifting every column from one row downwards, which reads as a balance
finding rather than as a broken file.

Death being a flag rather than a rule finally has a consumer. The sweep defaults to runs that carry on past
zero health, so every row is N rounds of data rather than however far a build got, and `--no-death` exposes the
same flag to every other run verb — which no shell could reach before.
