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
    public void Every_round_of_the_committed_run_fields_every_creep_bought_before_it_and_pays_for_none_of_them()
    {
        // #207's "done when", over the run in content/ rather than over a
        // scenario written here: a creep bought in round three is still in the
        // wave in round ten, and is not paid for again.
        //
        // The second half is what makes this test rather than a restatement of
        // the file. Round N's wave containing round N-1's creeps is true of the
        // authored script by inspection -- the lines only grow -- so asserting
        // it alone would pass with the accumulation rule ripped out. What cannot
        // survive that is the PRICE: with the rule gone every round pays for its
        // whole column, and a round carrying thirty creeps and adding one costs
        // thirty-one creeps rather than one.
        //
        // OBSERVED: charge the full count in BuildPhase.Resolve -- push
        // slot.Count into `bought` rather than the increase. This goes red on
        // the purse itself: "A purse holding 17 gold was spent 38 on 2 of one
        // unit of type 7", because the committed run cannot afford to buy its
        // own wave a second time.
        //
        // OBSERVED, and it is the reason `adding` is now one local rather than
        // two subtractions: changing only the affordability sum and leaving the
        // charge alone leaves this test GREEN. The two had to be the same
        // number by construction before that mutation could be caught at all.
        Run run = Fresh();
        CommandStream stream = Committed();
        IReadOnlyList<RoundReport> rounds = stream.Replay(run);
        var carried = new Dictionary<int, int>();

        for (int index = 0; index < rounds.Count; index++)
        {
            WaveScript sent = rounds[index].Build.Wave;
            long added = 0;

            foreach (KeyValuePair<int, int> held in carried)
            {
                Assert.True(
                    sent.CountOf(held.Key) >= held.Value,
                    "Round "
                        + (index + 1)
                        + " fields "
                        + sent.CountOf(held.Key)
                        + " of type "
                        + held.Key
                        + " where the rounds before it bought "
                        + held.Value
                        + ".");
            }

            for (int order = 0; order < sent.Orders.Count; order++)
            {
                UnitOrder line = sent.Orders[order];
                carried.TryGetValue(line.TypeId, out int before);
                added += run.Costs.PriceOf(Purchase.Unit(line.TypeId), line.Count - before);
                carried[line.TypeId] = line.Count;
            }

            // What the round spent on its wave: everything it spent, less what
            // the board took. Equal to the increase and to nothing else.
            Assert.Equal(added, rounds[index].Build.Spent - rounds[index].Build.Defense);
        }

        // And the run really did accumulate rather than sending one round's
        // worth ten times -- otherwise every "increase" above is the whole wave
        // and the assertion is trivially true.
        Assert.True(
            rounds[rounds.Count - 1].Build.Wave.TotalUnits > rounds[0].Build.Wave.TotalUnits * 10,
            "The committed run's last wave is not meaningfully larger than its first.");
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
            FieldPool.Canned(
                TheMatch.Map(),
                TheRuleset.Committed(),
                types,
                TheLadder.Committed(types),
                defense,
                TheRun.FieldWave(types)),
            Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);
    }
}
