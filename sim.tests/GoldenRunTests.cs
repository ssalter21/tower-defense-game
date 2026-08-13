namespace Sim.Tests;

/// <summary>
/// The committed run: a command file, the script it was compiled from, and the
/// vector a real play of it produced.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the golden trace's rule applied one level up.</b> The trace pins
/// a match tick by tick; these pin a run round by round, so a lifecycle
/// regression -- an offering that moved, an interest rate that compounds
/// differently, a slot width that widens at the wrong anchor, a field drawn
/// from another position -- is a diff rather than an argument.
/// </para>
/// <para>
/// <b>Nothing here regenerates anything it checks.</b> The oracle is the
/// committed file; the run is played fresh and held against it. A file written
/// by the thing that validates it is a test that cannot fail, which is exactly
/// why <c>content/run-outcome.txt</c> comes out of
/// <c>tools/run-headless-match.ps1 -Regenerate</c> and out of nothing else.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class GoldenRunTests
{
    /// <summary>
    /// The seed the committed stream carries, and the run it is about. One
    /// constant for the whole suite: two copies of it would let a re-recorded
    /// golden agree with half the tests.
    /// </summary>
    private const ulong Seed = TheCommandLine.RunSeed;

    [Fact]
    public void The_committed_command_file_is_stamped_with_the_committed_content()
    {
        // Three hashes and a seed, each compared against the table this build
        // parses rather than against a number written in here. A stream stamped
        // with one ruleset and made under another is the failure the stamps
        // exist for, and a test carrying its own copy of the expected hash
        // would go green through exactly that.
        //
        // OBSERVED: move the interest rate in content/ruleset.txt from 10 to 11.
        // The ruleset assertion goes red, 13A7F80C5AB79BDD against the record's
        // AE57B5051EF890D0, and the replay below goes red too -- refused at the
        // gate by name rather than played into a confidently wrong answer.
        //
        // OBSERVED, on the unit table's half of it: move grunt's max hp in
        // content/units.txt from 1550 to 1551. The content assertion goes red,
        // CEC08139EC85B6B3 against the record's 3CDE522BEF0F334A. The three
        // hashes are compared separately because each retires the record on its
        // own, and a single combined comparison would say only that something
        // moved.
        CommandStream stream = Committed();
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(RecordFormat.CommandVersion, stream.Header.FormatVersion);
        Assert.Equal(SimulationVersion.Current, stream.Header.SimVersion);
        Assert.Equal(Seed, stream.Seed);
        Assert.Equal(types.ContentHash, stream.Header.ContentHash);
        Assert.Equal(TheRuleset.Committed().ContentHash, stream.RulesetHash);
        Assert.Equal(TheLadder.Committed(types).ContentHash, stream.LadderHash);
    }

    [Fact]
    public void The_committed_script_and_the_committed_record_are_the_same_decisions()
    {
        // The authored file is the source and the record is what a run consumes,
        // and the two going quietly apart is the whole hazard of having both:
        // an edit to the script that nobody compiled leaves a record playing
        // decisions no longer written down anywhere.
        //
        // OBSERVED: change wave 7's runner count in content/commands.txt from 8
        // to 9 and do not regenerate. This goes red on index 6 -- "8 of type 2"
        // against "9 of type 2" -- and the record and the outcome beside it stay
        // perfectly self-consistent, which is what a stale source looks like
        // from every other angle. The byte comparison in CommandLineTests goes
        // red with it, at position 167 of the two files.
        IReadOnlyList<RecordCommand> authored = CommandScript.Parse(
            "commands.txt", File.ReadAllText(RepoLayout.CommandScriptFile));

        CommandStream stream = Committed();

        Assert.Equal(stream.Count, authored.Count);

        for (int index = 0; index < authored.Count; index++)
        {
            Assert.Equal(stream.Commands[index], authored[index]);
        }
    }

    [Fact]
    public void The_committed_command_file_replays_to_the_committed_outcome()
    {
        // The run is played here and the numbers are read out of the committed
        // file, one round at a time. Per round rather than at the end, for the
        // reason the trace is compared per tick: a run that diverges at wave
        // four says so at wave four, before the rounds after it have had the
        // difference to compound.
        //
        // Every comparison is anchored on both sides -- the decision on the
        // left, the round on the right -- because "dealt 0, took 12" sits inside
        // "dealt 0, took 128", so a number that lost a digit would be found in
        // the committed line and pass.
        //
        // The round is the whole of what a round reported: the pair, what the
        // wave cost and what it paid the purse. The economy is pinned round by
        // round for the reason the pair is -- an interest rate that compounded
        // differently or a band that paid the wrong share moves a number here
        // long before it moves health.
        //
        // OBSERVED: doctor content/run-outcome.txt. Wave six's "dealt 206" to
        // "dealt 260" reddens the round-six assertion and nothing else; the
        // summary line's "249 of 1500 health left" to "250 of 1500" reddens the
        // summary assertion alone. Without watching those, a Contains against a
        // file nobody regenerates is a test that passes because the substring is
        // short.
        //
        // OBSERVED, on the economy half: wave four's "spent 90" to "spent 91"
        // reddens the round-four assertion and nothing else -- so what a round
        // cost is pinned to the same standard as what it dealt rather than
        // riding along on a line checked for its other half.
        Run run = Fresh();
        CommandStream stream = Committed();
        IReadOnlyList<RoundReport> rounds = stream.Replay(run);
        string committed = File.ReadAllText(RepoLayout.RunOutcomeFile);

        Assert.Equal(stream.Count, rounds.Count);

        for (int index = 0; index < stream.Count; index++)
        {
            Assert.Contains(
                stream.Commands[index].ToString() + "   ->   " + rounds[index].ToString() + "\n",
                committed,
                StringComparison.Ordinal);
        }

        Assert.Contains(
            "outcome    " + run.Outcome.ToString() + ", ended " + run.Outcome.Ending.ToString() + "\n",
            committed,
            StringComparison.Ordinal);
    }

    [Fact]
    public void The_committed_outcome_names_the_run_it_is_about()
    {
        // The vector above is compared, which proves the file is current. This
        // proves it is about the right run: an outcome regenerated against
        // another seed would go on agreeing with a play of that other seed
        // forever, and nothing in the numbers themselves says which run they
        // came from.
        //
        // OBSERVED: doctor the seed in content/run-outcome.txt's header line,
        // 20260807 to the match's 20260801. Every assertion above stays green --
        // the rounds and the summary are untouched -- and this one goes red,
        // which is the whole of what a header naming its own record buys.
        string committed = File.ReadAllText(RepoLayout.RunOutcomeFile);

        Assert.Contains(Committed().ToString(), committed, StringComparison.Ordinal);
    }

    /// <summary>The committed command file, read through the read gate.</summary>
    private static CommandStream Committed() =>
        CommandStream.FromBytes("run.commands", File.ReadAllBytes(RepoLayout.CommandFile));

    /// <summary>
    /// The run the command line builds for that record: the committed tables
    /// and shape, and the canned field of one the committed defense and the
    /// committed field file make.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Composed here rather than shared with the command line, which is a
    /// second arrangement of the same content and is meant to be: if the two
    /// ever describe different runs, the vector this replays to stops matching
    /// the committed one and the run above goes red naming the round.
    /// </para>
    /// <para>
    /// <b>The pool is <c>content/field.txt</c> and never <c>content/wave.txt</c></b>,
    /// which is the same distinction the command line's <c>--field</c> draws.
    /// The wave file is a whole authored match and a round of it costs several
    /// times what any purse composes, so a run against one takes about a hundred
    /// gold a round from an opponent no player could be -- and every number it
    /// produces is self-consistent, which is why the committed outcome is what
    /// says which of the two this was.
    /// </para>
    /// </remarks>
    private static Run Fresh()
    {
        UnitTypeTable types = TheMatch.Types();
        TowerLayout defense = TheMatch.Layout(types);

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            FieldPool.Canned(defense, TheRun.FieldWave(types)),
            Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);
    }
}
