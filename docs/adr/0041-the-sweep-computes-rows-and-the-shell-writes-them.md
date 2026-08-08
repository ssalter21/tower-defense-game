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
the defense, the canned field, N, K, the death flag, the offering ratio, the free-snapshot count and the
snapshot price. Pointing the sweep at another board to score it, or at another damage matrix, is then an
argument rather than a retrofit across every call site — which is the one thing that is cheap now and expensive
in a year. The last four reach the rules through `Ruleset.With`, which recomputes the content hash over the
parsed integers exactly as a parse does (ADR-0011), so a retuned sweep plays a real ruleset that a stored record
is loudly refused against, rather than a set of overrides carried alongside one.

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
`content/field.txt` is that stand-in, and it is deliberately not `content/wave.txt`: the
authored match is forty creeps and three hundred and eighty sauce, and a run's purse is paid one wave's base a
wave, so a field member composed of it outspends every opponent it faces three and a half times over. A sweep
against one reports a total loss on every row and names nothing.

**Cost efficiency is documented for what it measures and what it does not.** A leak charges health equal to what
the creep cost to send, one for one, so leak cost dealt over sauce spent is the cost-weighted leak rate of what
was sent and the price level cancels out of it exactly — halving a creep's price doubles how many of it a purse
buys and halves what each leak charges. The column therefore **cannot** say a creep is overpriced; the
measurement behind that is in
[`cost-is-not-a-balance-lever-under-a-one-for-one-leak.md`](../research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md).
It is kept anyway, and it earns its place for a different reason: winning is one bit a run and it saturates
against a canned field of one, while this column stays graded and goes on separating creeps after the win rate
has flattened. It is named `cost_efficiency_dealt_per_100_sauce` so that the unit travels with it.

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
