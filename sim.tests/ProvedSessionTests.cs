namespace Sim.Tests;

/// <summary>
/// A session held up against a fresh run of the script it wrote, and the script
/// that only comes back where the two agreed.
/// </summary>
/// <remarks>
/// <para>
/// <b>The refusal needs a lie to reach.</b> Played through any real caller, the
/// session and the fresh run are the same run by construction, so the refusal
/// could never be watched working. The seam is that what the player was shown
/// arrives as data: the prover is handed the decisions and the rounds, so a
/// test can hand over a session that says something the fresh run does not and
/// see what happens. Nothing about a caller is loosened to allow it -- a caller
/// hands over the rounds it was shown, and there is no other way in.
/// </para>
/// <para>
/// <b>Nothing here opens a file, because the prover cannot.</b> The write is the
/// caller's half -- <c>client/Assets/View/WrittenRun.cs</c> -- and that a
/// session which disagreed has no script to write is asserted here, over the
/// script itself. That the prover could not open a file even if it wanted to is
/// asserted where it can be, over the shipped image, by <c>IlScanTests</c>.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong prover</b>,
/// and the wrong prover is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class ProvedSessionTests
{
    /// <summary>
    /// The two labels a disagreement puts each side of itself behind, restated
    /// here rather than read off the prover, so that changing what a person is
    /// shown is a decision this goes red over.
    /// </summary>
    private const string Shown = "    played    ";

    private const string Replayed = "    replayed  ";

    /// <summary>Where the committed run's own decisions, played a round at a time, come to rest.</summary>
    private static readonly Lazy<Session> Committed = new(() =>
        Play(AgainstTheCannedField(TheMatch.Types()), TheCommands.Committed()));

    [Fact]
    public void A_session_that_replays_as_it_played_hands_back_the_script_it_proved()
    {
        // The whole claim, end to end: the committed run's rounds played one
        // at a time, compiled into a script, that script played into a run built
        // fresh on the same seed and shape, and every round and the outcome the
        // same on both sides. Only then is there a script to keep, and what
        // comes back is the decisions that were proved rather than text that
        // merely parses.
        //
        // OBSERVED: compare the fresh run's rounds against themselves --
        // replay them, then hold the replay against the replay. Everything here
        // stays green, which is the whole problem: the played path drops out of
        // the comparison entirely and the prover proves that a record equals
        // itself.
        Session played = Committed.Value;

        ProvedSession proved = ProvedSession.Of(
            played.Decisions, played.Rounds, played.Run, Fresh);

        Assert.True(proved.Agreed, proved.Disagreement);
        Assert.Null(proved.Disagreement);
        Assert.Equal(TheCommands.Committed().Count, played.Decisions.Count);
        Assert.Equal(played.Decisions.Count, proved.RoundsProved);

        Assert.Equal(
            played.Decisions.Select((decision, index) => RecordCommand.Of(index + 1, decision)),
            CommandScript.Parse("the proved script", proved.Script));
    }

    [Fact]
    public void A_round_the_fresh_run_plays_differently_is_named_with_both_sides_and_no_script_comes_back()
    {
        // The refusal. The session hands over its own rounds with the last two
        // swapped, so the script is the real one and the fresh run plays it
        // exactly as it was played -- and the two lists differ first at wave
        // three. Both sides of that round are printed, because a person reading
        // this has to be able to see which of the two is wrong.
        //
        // OBSERVED: compare the outcome alone, on the argument that a run whose
        // rounds differ ends differently. It does not: these are the same
        // rounds in a different order, every fold over them is identical, and
        // a caller that checked only the ending would keep the script and call
        // the session proved.
        Session played = Committed.Value;

        RoundReport[] lying = played.Rounds.ToArray();
        (lying[2], lying[3]) = (lying[3], lying[2]);

        ProvedSession proved = ProvedSession.Of(played.Decisions, lying, played.Run, Fresh);

        Assert.False(proved.Agreed);

        string disagreement = proved.Disagreement!;

        Assert.Contains("wave 3:", disagreement, StringComparison.Ordinal);
        Assert.Contains(
            Shown + played.Rounds[3] + "\n" + Replayed + played.Rounds[2],
            disagreement,
            StringComparison.Ordinal);

        // And it is the playing loop's fault, said in those words, because
        // nothing a player can do reaches here.
        Assert.Contains(
            "a bug in playing a run a round at a time rather than a decision anybody made badly",
            disagreement,
            StringComparison.Ordinal);

        // And there is no script to hand anybody, which is what keeps that
        // refusal the prover's now that the file is somebody else's.
        //
        // OBSERVED: hand the script back beside the disagreement, on the
        // argument that a session is worth keeping either way. A caller that
        // reads Script without reading Agreed writes down a run nobody played,
        // and the one sentence saying so has scrolled off.
        Assert.Equal(string.Empty, proved.Script);
        Assert.Equal(0, proved.RoundsProved);
    }

    [Fact]
    public void A_session_and_a_fresh_run_that_played_a_different_number_of_rounds_are_refused()
    {
        // The rounds are compared pairwise, so a comparison that walked only as
        // far as the shorter of the two would agree with a session missing its
        // last round -- every pair it looked at would match. Both lengths are
        // printed, because which of the two is short is the whole of what is
        // wrong.
        //
        // OBSERVED: walk the session's rounds and stop. The ones that were shown
        // all match the same number the script replays, the last is never looked
        // at, and a session whose last round vanished somewhere writes a whole
        // script and calls it proved.
        Session played = Committed.Value;
        int shown = played.Rounds.Count - 1;

        ProvedSession proved = ProvedSession.Of(
            played.Decisions,
            played.Rounds.Take(shown).ToArray(),
            played.Run,
            Fresh);

        Assert.False(proved.Agreed);
        Assert.Contains("how many rounds:", proved.Disagreement, StringComparison.Ordinal);
        Assert.Contains(
            Shown + shown + "\n" + Replayed + played.Rounds.Count,
            proved.Disagreement,
            StringComparison.Ordinal);
    }

    [Fact]
    public void An_outcome_the_fresh_run_folds_differently_is_refused_though_every_round_agreed()
    {
        // The other half of "every round report as well as the final outcome".
        // Nothing in this run is killable, so the pool is spent on the fourth
        // round and the death flag alone decides whether that ended it -- and
        // the flag is nowhere in a round report. Four identical rounds, and two
        // different runs.
        //
        // OBSERVED: stop at the rounds, on the argument that the outcome is a
        // fold over them and cannot differ where they do not. It is a fold over
        // the rounds AND the shape: the same four rounds end one run out of
        // health and leave the other unfinished, and the comparison waves it
        // through.
        Run dying = TheRun.Unstoppable(fieldSize: 1);
        Session played = Play(dying, DoingNothing(dying));

        ProvedSession proved = ProvedSession.Of(
            played.Decisions,
            played.Rounds,
            played.Run,
            () => TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1));

        Assert.False(proved.Agreed);
        Assert.Contains("the run:", proved.Disagreement, StringComparison.Ordinal);
        Assert.Contains(
            Shown + RunSummary.Outcome(played.Run) + "\n" + Replayed,
            proved.Disagreement,
            StringComparison.Ordinal);
        Assert.Contains("ended " + RunEnding.OutOfHealth, proved.Disagreement, StringComparison.Ordinal);
        Assert.Contains("ended " + RunEnding.Unfinished, proved.Disagreement, StringComparison.Ordinal);

        // The rounds agreed, which is what makes this a claim about the outcome
        // and not a second copy of the case above.
        Assert.DoesNotContain("wave ", proved.Disagreement, StringComparison.Ordinal);
    }

    [Fact]
    public void A_decision_the_record_cannot_store_is_the_same_refusal_and_says_whose_fault_it_is()
    {
        // A script that will not compile never reaches a fresh run, and it is
        // the same failure wearing a different sentence: the caller composed
        // something the record cannot carry. The record's own words are carried
        // through, because they say which decision and which slot.
        //
        // The decision used to be a descending pair, which #191 made legal: a
        // slot's position is its release order, so an arrangement is a decision
        // rather than a spelling. What the record still cannot carry is one
        // creep in two slots of one wave.
        //
        // OBSERVED: let it throw. The caller dies on "fills slot 2 with type id
        // 5, which a slot above it already sent" -- which is true, which is
        // unactionable, and which reads to whoever played the session as though
        // the fault were theirs.
        Run run = AgainstTheCannedField(TheMatch.Types());

        BuildPhase[] unstorable = { BuildPhase.Of(WaveSlot.Of(5, 1), WaveSlot.Of(5, 1)) };

        ProvedSession proved = ProvedSession.Of(
            unstorable, Array.Empty<RoundReport>(), run, Fresh);

        Assert.False(proved.Agreed);
        Assert.Contains(
            "A creep fills at most one slot of a wave",
            proved.Disagreement,
            StringComparison.Ordinal);
        Assert.Contains(
            "a bug in playing a run a round at a time",
            proved.Disagreement,
            StringComparison.Ordinal);
        Assert.Equal(string.Empty, proved.Script);
    }

    [Fact]
    public void A_session_that_committed_no_round_hands_back_nothing_and_plays_no_second_run()
    {
        // Which is what quitting at wave one leaves behind. There is no row in
        // this grammar for a run nobody played, so there is no script, nothing
        // to prove and nothing to write -- and that is not a disagreement, so
        // the session is not reported as a bug.
        //
        // OBSERVED: prove it anyway. CommandScript.Parse refuses the empty
        // script by name -- "decides nothing at all" -- so a player who quit
        // before their first round is told the caller has a bug in it.
        Run run = AgainstTheCannedField(TheMatch.Types());
        bool builtOne = false;

        ProvedSession proved = ProvedSession.Of(
            Array.Empty<BuildPhase>(),
            Array.Empty<RoundReport>(),
            run,
            () =>
            {
                builtOne = true;

                return run;
            });

        Assert.True(proved.Agreed);
        Assert.Equal(string.Empty, proved.Script);
        Assert.Equal(0, proved.RoundsProved);
        Assert.False(builtOne);
    }

    [Fact]
    public void A_run_a_build_policy_played_is_proved_the_same_way_and_names_no_content_file()
    {
        // The same claim over decisions nothing authored: a run advanced a round
        // at a time by a policy, on tables this suite builds, with no committed
        // script anywhere in it. So the prover is being asked about the shape of
        // a session rather than about the ten rounds content/commands.txt
        // happens to hold, and the case above is not the only run it has ever
        // seen.
        //
        // OBSERVED: hand the prover the fresh run's own rounds rather than the
        // ones this loop collected. Green, and green under any prover at all,
        // which is the shape a self-comparison takes.
        Run played = TheRun.Fresh(deathEndsTheRun: false);
        var decisions = new List<BuildPhase>();
        var shown = new List<RoundReport>();

        while (!played.IsOver && played.Round < played.Waves)
        {
            BuildPhase decision = TheBuild.Fortifying(played);

            decisions.Add(decision);
            shown.Add(played.Advance(decision));
        }

        ProvedSession proved = ProvedSession.Of(
            decisions,
            shown,
            played,
            () => TheRun.Fresh(deathEndsTheRun: false));

        Assert.True(proved.Agreed, proved.Disagreement);
        Assert.Equal(Run.DefaultWaves, proved.RoundsProved);

        // And what it comes back holding is those decisions rather than text
        // that merely parses.
        Assert.Equal(
            decisions.Select((decision, index) => RecordCommand.Of(index + 1, decision)),
            CommandScript.Parse("the proved script", proved.Script));
    }

    /// <summary>What one session came to: the run it moved, its decisions and the rounds it was shown.</summary>
    private sealed class Session
    {
        public Session(Run run, IReadOnlyList<BuildPhase> decisions, IReadOnlyList<RoundReport> rounds)
        {
            Run = run;
            Decisions = decisions;
            Rounds = rounds;
        }

        /// <summary>The run the session played, as it stands afterwards.</summary>
        public Run Run { get; }

        /// <summary>The phase each played round was played from, in wave order.</summary>
        public IReadOnlyList<BuildPhase> Decisions { get; }

        /// <summary>What each of those rounds came to, as the player was told it.</summary>
        public IReadOnlyList<RoundReport> Rounds { get; }
    }

    /// <summary>
    /// Plays these decisions into this run a round at a time, keeping what each
    /// round reported.
    /// </summary>
    /// <remarks>
    /// The shape every caller of the prover has: advance, keep what came back,
    /// stop where the run stops. It is deliberately not a second implementation
    /// of anything -- the run does the playing, and all this holds on to is the
    /// pair the prover is handed.
    /// </remarks>
    private static Session Play(Run run, IReadOnlyList<RecordCommand> decisions)
    {
        var made = new List<BuildPhase>();
        var shown = new List<RoundReport>();

        foreach (RecordCommand command in decisions)
        {
            if (run.IsOver)
            {
                break;
            }

            BuildPhase phase = command.ToPhase();

            made.Add(phase);
            shown.Add(run.Advance(phase));
        }

        return new Session(run, made, shown);
    }

    /// <summary>
    /// A decision per round that buys nothing at all, for as many rounds as a
    /// run can have.
    /// </summary>
    private static IReadOnlyList<RecordCommand> DoingNothing(Run run)
    {
        var decisions = new List<RecordCommand>();

        for (int wave = 1; wave <= run.Waves; wave++)
        {
            decisions.Add(RecordCommand.Of(wave, TheBuild.BuyingNothing()));
        }

        return decisions;
    }

    /// <summary>The fresh run the committed session is proved against: the same seed and the same shape.</summary>
    private static Run Fresh() => AgainstTheCannedField(TheMatch.Types());

    /// <summary>
    /// A fresh run on the committed content and the committed seed, against the
    /// canned field of one the command line builds.
    /// </summary>
    private static Run AgainstTheCannedField(UnitTypeTable types) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            FieldPool.Canned(
                TheMatch.Map(),
                TheRuleset.Committed(),
                types,
                TheLadder.Committed(types),
                TheMatch.Layout(types),
                TheRun.FieldWave(types)),
            TheRun.Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);
}
