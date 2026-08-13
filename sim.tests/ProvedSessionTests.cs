using System.Globalization;
using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// A session held up against a fresh run of the script it wrote, and the file
/// that is only written where the two agreed.
/// </summary>
/// <remarks>
/// <para>
/// <b>Both halves of the claim are here, and one of them needs a lie to
/// reach.</b> Played through the program, the session and the fresh run are the
/// same run by construction, so the refusal could never be watched working --
/// which is the one thing <c>docs/playing-a-run-from-a-shell.md</c> §4 asks for
/// by name. The seam is that what the player was shown arrives as data:
/// <see cref="Played"/> carries the rounds, so a test can hand over a session
/// that says something the fresh run does not and see what happens. Nothing
/// about the verb is loosened to allow it -- the verb hands over the rounds it
/// was shown, and there is no other way in.
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

    /// <summary>Where the committed run's own decisions, played at a prompt, come to rest.</summary>
    private static readonly Lazy<Session> Committed = new(() =>
        Play(AgainstTheCannedField(TheMatch.Types()), TheCommands.TypedAtAPrompt()));

    [Fact]
    public void A_session_that_replays_as_it_played_writes_one_file_and_it_is_the_script_it_proved()
    {
        // The whole claim, end to end: ten rounds typed at a prompt, compiled
        // into a script, that script played into a run built fresh on the same
        // seed and shape, and every round and the outcome the same on both
        // sides. Only then does anything reach a disk, and what reaches it is
        // the script that was proved rather than a second rendering of it.
        //
        // OBSERVED: compare the fresh run's rounds against themselves --
        // replay them, then hold the replay against the replay. Everything here
        // stays green, which is the whole problem: the interactive path drops
        // out of the comparison entirely and the verb proves that a record
        // equals itself.
        Session played = Committed.Value;

        ProvedSession proved = ProvedSession.Of(played.Result, played.Run, Fresh);

        Assert.True(proved.Agreed, proved.Disagreement);
        Assert.Null(proved.Disagreement);
        Assert.Equal(Run.DefaultWaves, played.Result.Decisions.Count);

        string scratch = TheCommandLine.Scratch("proved-session-agrees");
        string path = Path.Combine(scratch, "run.commands.txt");
        var writer = new StringWriter();

        Assert.True(proved.Written(path, writer));

        // Exactly one file, at the path it was given, holding the script and
        // nothing else.
        Assert.Equal(new[] { path }, Directory.GetFiles(scratch, "*", SearchOption.AllDirectories));
        Assert.Equal(proved.Script, File.ReadAllText(path));
        Assert.Contains("wrote      " + path, writer.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void A_round_the_fresh_run_plays_differently_is_named_with_both_sides_and_nothing_is_written()
    {
        // The refusal. The session hands over its own ten rounds with the
        // fourth and fifth swapped, so the script is the real one and the fresh
        // run plays it exactly as it was played -- and the two lists differ
        // first at wave four. Both sides of that round are printed, because a
        // person reading this has to be able to see which of the two is wrong.
        //
        // OBSERVED: compare the outcome alone, on the argument that a run whose
        // rounds differ ends differently. It does not: these are the same ten
        // rounds in a different order, every fold over them is identical, and
        // a verb that checked only the ending would write the file and call the
        // session proved.
        Session played = Committed.Value;

        RoundReport[] lying = played.Result.Rounds.ToArray();
        (lying[3], lying[4]) = (lying[4], lying[3]);

        ProvedSession proved = ProvedSession.Of(
            new Played(played.Result.Decisions, lying, played.Result.Ending),
            played.Run,
            Fresh);

        Assert.False(proved.Agreed);

        string disagreement = proved.Disagreement!;

        Assert.Contains("wave 4:", disagreement, StringComparison.Ordinal);
        Assert.Contains(
            Shown + played.Result.Rounds[4] + "\n" + Replayed + played.Result.Rounds[3],
            disagreement,
            StringComparison.Ordinal);

        // And it is this verb's fault, said in those words, because nothing a
        // player can type reaches here.
        Assert.Contains(
            "a bug in playing a run at a prompt rather than a decision anybody made badly",
            disagreement,
            StringComparison.Ordinal);

        // OBSERVED: write the script and report the disagreement beside it, on
        // the argument that a session is worth keeping either way. The file
        // lands, the next thing to read it replays a run nobody played, and the
        // one sentence saying so has scrolled off.
        string scratch = TheCommandLine.Scratch("proved-session-round");
        string path = Path.Combine(scratch, "run.commands.txt");
        var writer = new StringWriter();

        Assert.False(proved.Written(path, writer));
        Assert.Empty(Directory.GetFiles(scratch, "*", SearchOption.AllDirectories));
        Assert.Contains("Nothing was written to " + path + ".", writer.ToString(), StringComparison.Ordinal);
        Assert.Contains(disagreement, writer.ToString(), StringComparison.Ordinal);
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
        // OBSERVED: walk the session's rounds and stop. The nine that were
        // shown all match the first nine the script replays, the tenth is never
        // looked at, and a session whose last round vanished somewhere writes a
        // ten-round script and calls it proved.
        Session played = Committed.Value;

        ProvedSession proved = ProvedSession.Of(
            new Played(
                played.Result.Decisions,
                played.Result.Rounds.Take(Run.DefaultWaves - 1).ToArray(),
                played.Result.Ending),
            played.Run,
            Fresh);

        Assert.False(proved.Agreed);
        Assert.Contains("how many rounds:", proved.Disagreement, StringComparison.Ordinal);
        Assert.Contains(Shown + "9\n" + Replayed + "10", proved.Disagreement, StringComparison.Ordinal);
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
            played.Result,
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
        // the same failure wearing a different sentence: the prompt composed
        // something the record cannot carry. The record's own words are carried
        // through, because they say which decision and which slot.
        //
        // OBSERVED: let it throw. The verb dies on "fills slot 2 with type id
        // 2, at or below the 5 a slot above it already sent" -- which is true,
        // which is unactionable, and which reads to whoever typed the session
        // as though they had sent the creeps in the wrong order.
        Run run = AgainstTheCannedField(TheMatch.Types());

        var unstorable = new Played(
            new[] { BuildPhase.Of(WaveSlot.Of(5, 1), WaveSlot.Of(2, 1)) },
            Array.Empty<RoundReport>(),
            Ended.Quit);

        ProvedSession proved = ProvedSession.Of(unstorable, run, Fresh);

        Assert.False(proved.Agreed);
        Assert.Contains("Filled slots ascend strictly by type id", proved.Disagreement, StringComparison.Ordinal);
        Assert.Contains("a bug in playing a run at a prompt", proved.Disagreement, StringComparison.Ordinal);

        string scratch = TheCommandLine.Scratch("proved-session-unstorable");
        var writer = new StringWriter();

        Assert.False(proved.Written(Path.Combine(scratch, "run.commands.txt"), writer));
        Assert.Empty(Directory.GetFiles(scratch, "*", SearchOption.AllDirectories));
    }

    [Fact]
    public void A_session_that_committed_no_round_writes_nothing_and_plays_no_second_run()
    {
        // Which is what quitting at wave one leaves behind. There is no row in
        // this grammar for a run nobody played, so there is no script, nothing
        // to prove and nothing to write -- and that is not a disagreement, so
        // the session is not reported as a bug.
        //
        // OBSERVED: prove it anyway. CommandScript.Parse refuses the empty
        // script by name -- "decides nothing at all" -- so a player who quit
        // before their first round is told the verb has a bug in it.
        Run run = AgainstTheCannedField(TheMatch.Types());
        bool builtOne = false;

        ProvedSession proved = ProvedSession.Of(
            new Played(Array.Empty<BuildPhase>(), Array.Empty<RoundReport>(), Ended.Quit),
            run,
            () =>
            {
                builtOne = true;

                return run;
            });

        Assert.True(proved.Agreed);
        Assert.Equal(string.Empty, proved.Script);
        Assert.False(builtOne);

        string scratch = TheCommandLine.Scratch("proved-session-nothing");
        string path = Path.Combine(scratch, "run.commands.txt");
        var writer = new StringWriter();

        Assert.True(proved.Written(path, writer));
        Assert.Empty(Directory.GetFiles(scratch, "*", SearchOption.AllDirectories));
        Assert.Contains("No round was played", writer.ToString(), StringComparison.Ordinal);
    }

    /// <summary>What one canned session came to: the run it moved and the decisions.</summary>
    private sealed class Session
    {
        public Session(Run run, Played result)
        {
            Run = run;
            Result = result;
        }

        /// <summary>The run the session played, as it stands afterwards.</summary>
        public Run Run { get; }

        /// <summary>What the loop handed back.</summary>
        public Played Result { get; }
    }

    /// <summary>Plays these lines into this run, with the screen thrown away.</summary>
    private static Session Play(Run run, params string[] typed) =>
        new Session(
            run,
            RunPrompt.Play(
                run,
                TheMatch.Ladder(run.Types),
                new StringReader(string.Join('\n', typed)),
                new StringWriter()));

    /// <summary>
    /// A transcript that finishes every round having done nothing at all, for
    /// as many rounds as a run can have.
    /// </summary>
    /// <remarks>
    /// It took the first thing on each round menu before #179; there is no menu
    /// and nothing a round must do, so the shortest legal round is one word.
    /// </remarks>
    private static string[] DoingNothing(Run run)
    {
        var typed = new List<string>();

        for (int wave = 1; wave <= run.Waves; wave++)
        {
            typed.Add("done");
        }

        return typed.ToArray();
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
            FieldPool.Canned(TheMatch.Layout(types), TheRun.FieldWave(types)),
            TheRun.Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
