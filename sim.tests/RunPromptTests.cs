using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// A whole run played round by round at a prompt, from canned transcripts with
/// no terminal anywhere.
/// </summary>
/// <remarks>
/// <para>
/// <b>The headline case is the committed run, typed.</b>
/// <c>content/commands.txt</c> is a whole run's decisions, and the same
/// decisions spelled as words somebody types have to produce the same ten
/// rounds -- so <c>content/run-outcome.txt</c> is the oracle here exactly as it
/// is for a replay. That is the claim
/// <c>docs/playing-a-run-from-a-shell.md</c> §5 puts first, and it is what makes
/// the committed script an input to this verb rather than only to
/// <c>play-run</c>.
/// </para>
/// <para>
/// <b>What is under test is the lifecycle and never the round.</b> Composing a
/// round -- the words, the pricing after each of them, the refusals a word can
/// raise -- is <see cref="BuildPrompt"/>'s and is asserted in
/// <c>BuildPromptTests</c>. What goes red here is a round committed twice, a
/// round line that is not the report's own words, a run that carried on past its
/// end or stopped before it, or an ending block that says something the
/// committed file does not.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong loop</b>, and
/// the wrong loop is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class RunPromptTests
{
    /// <summary>What stands between a decision and its round on a line of the outcome file.</summary>
    private const string Arrow = "   ->   ";

    /// <summary>
    /// What composing stands in front of a line waiting to be typed, restated
    /// here rather than read off the loop, so that changing it is a decision
    /// this goes red over.
    /// </summary>
    private const string Prompt = "> ";

    /// <summary>The archer's row on the committed roster, and the ranger it upgrades into.</summary>
    private const int Archer = 3;

    private const int Ranger = 14;

    /// <summary>The creep rows the committed run's eighth wave fields, in the order it fills its slots.</summary>
    private static readonly WaveSlot[] WaveEight =
    {
        WaveSlot.Of(1, 2), WaveSlot.Of(2, 8), WaveSlot.Of(7, 1), WaveSlot.Of(12, 2),
    };

    /// <summary>
    /// The committed run's ten decisions, spelled as somebody would type them:
    /// the take, then what the round builds, then the slots it fills, then
    /// <c>done</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Written out rather than compiled from <c>content/commands.txt</c>,
    /// because what is being claimed is that these words and that file are the
    /// same run -- and a transcript generated from the file could only ever
    /// agree with it. The bodies are named by label where the script names them
    /// by id, which is the one convenience §3 grants a prompt, so this is also
    /// where a label that stopped resolving would be found.
    /// </para>
    /// <para>
    /// The script's empty slots are absent: <c>0 0</c> is how a stored row says
    /// a slot was left alone, and at a prompt a slot nobody filled is a
    /// <c>send</c> nobody typed.
    /// </para>
    /// </remarks>
    private const string TheCommittedRun = """
        take ordinary 1
        place archer 6 2
        done
        take ordinary 2
        place archer 7 4
        done
        take changer 4
        place archer 7 6
        done
        take ordinary 12
        place archer 4 4
        send skeleton-scout 20
        done
        take ordinary 13
        upgrade ranger 6 2
        done
        take changer 7
        send minion 10
        send skeleton-scout 20
        done
        take ordinary 7
        send minion 2
        send skeleton-scout 8
        send skeleton 2
        done
        take ordinary 13
        send minion 2
        send skeleton-scout 8
        send necromancer 1
        send skeleton 2
        done
        take changer 9
        send minion 5
        send skeleton-scout 4
        done
        take ordinary 1
        send minion 6
        send skeleton-scout 9
        done
        """;

    /// <summary>
    /// The committed run played once for the whole class. Ten rounds resolve two
    /// hundred matches and measuring the field costs a hundred more, and every
    /// test below reads the same session.
    /// </summary>
    private static readonly Lazy<Session> Committed = new(PlayTheCommittedRun);

    [Fact]
    public void A_canned_transcript_plays_a_whole_run_from_its_first_wave_to_the_cap()
    {
        // Ten rounds out of one string of words, with nobody at a keyboard: the
        // loop opened each round, committed it and drew the next, and stopped
        // because the run stopped rather than because the words ran out.
        //
        // OBSERVED: loop on `while (true)` rather than on `while (!run.IsOver)`.
        // The tenth `done` is this transcript's last line, so the eleventh round
        // reads no words and the session comes back OutOfLines instead of Over
        // -- a finished run reported as a transcript that stopped short. The
        // death session below goes red harder, because it still has lines to
        // read: Advance refuses the fifth round by name, "This run is over: 4
        // rounds resolved and 0 of 1500 health left", as a stack trace at the
        // prompt.
        Session played = Committed.Value;

        Assert.Equal(Ended.Over, played.Result.Ending);
        Assert.Equal(RunEnding.OutOfWaves, played.Run.Ending);
        Assert.Equal(Run.DefaultWaves, played.Run.Round);
        Assert.Equal(Run.DefaultWaves, played.Result.Rounds.Count);
        Assert.Equal(Run.DefaultWaves, played.Result.Decisions.Count);

        // The decisions came back in wave order and each is the one that round
        // was played from: the take the transcript named, and the board actions
        // under it. Wave five is the one that upgrades rather than places.
        Assert.Equal(OptionKind.GameChanger, played.Result.Decisions[2].Take);
        Assert.Equal(4, played.Result.Decisions[2].TakeId);
        Assert.Equal(
            new[] { BuildAction.Of(ActionKind.Upgrade, Ranger, 6, 2) },
            played.Result.Decisions[4].Actions);
        Assert.Equal(WaveEight, played.Result.Decisions[7].Slots);
    }

    [Fact]
    public void Every_round_line_is_the_round_reports_own_text_and_the_committed_files()
    {
        // The words a player is shown when a round resolves are the words
        // content/run-outcome.txt carries for that round -- all ten of them, in
        // order, compared against the committed file rather than against
        // anything this run produced. So a run typed at a prompt and a run
        // replayed from the record are the same run said the same way.
        //
        // OBSERVED: print the round as "wave 4 dealt 36, took 30" instead of the
        // report's own words. This is the only test that notices -- the rounds
        // resolve, the run ends and the board is the committed one -- and the
        // one thing the round line is for, being findable in the committed file,
        // is gone.
        Session played = Committed.Value;

        string[] committed = CommittedRoundLines();

        Assert.Equal(Run.DefaultWaves, committed.Length);
        Assert.Equal(committed, played.Result.Rounds.Select(round => round.ToString()).ToArray());

        // And each was printed where the round resolved: straight after the
        // prompt the `done` was typed at -- nothing echoes a typed line, so in a
        // terminal the round line lands under the player's own `done` -- and
        // before the frame the next round opens on.
        for (int index = 0; index < committed.Length; index++)
        {
            Assert.Contains(Prompt + committed[index] + "\n", played.Text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_end_prints_the_outcome_and_the_ending_board_and_nothing_of_its_own()
    {
        // Two blocks, both read off the run through the code the committed
        // outcome file is written by, and both found in that file character for
        // character. A run played at a prompt therefore ends on the same two
        // paragraphs a regenerated content/run-outcome.txt opens and closes
        // with.
        //
        // OBSERVED: close on a summary written for a terminal -- "you survived
        // 10 waves with 1022 health". It reads perfectly and it is nowhere in
        // the committed file, so this and the quit session below both go red on
        // the block they end on, and the two spellings of one outcome become a
        // thing to keep in step by hand.
        Session played = Committed.Value;
        string committed = File.ReadAllText(RepoLayout.RunOutcomeFile);

        Assert.EndsWith(
            "\n" + RunSummary.Outcome(played.Run) + "\n\n" + played.Run.Board.ToReportText() + "\n",
            played.Text,
            StringComparison.Ordinal);

        Assert.Contains(RunSummary.Outcome(played.Run), committed, StringComparison.Ordinal);
        Assert.Contains(played.Run.Board.ToReportText(), committed, StringComparison.Ordinal);
    }

    [Fact]
    public void Done_before_a_take_refuses_and_the_run_does_not_move()
    {
        // The refusal is composing's -- one take, no skip, settled at
        // docs/playing-a-run-from-a-shell.md §8 -- and what belongs to the
        // lifecycle is what follows from it: a round that was never composed is
        // a round the run was never advanced through. The session goes on to
        // quit, and the run has resolved nothing.
        //
        // OBSERVED: commit whatever the loop was holding when a round came back,
        // whatever ended it. A round that opened on `done` has nothing composed,
        // so Advance is handed a null phase and the session dies on an
        // ArgumentNullException -- the one word a player is likeliest to try
        // first ends the run in a stack trace.
        Session played = Play(Fresh(TheMatch.Types()), "done", "quit");

        Assert.Contains(
            "There is no phase to be done with until one is named.",
            played.Text,
            StringComparison.Ordinal);

        Assert.Equal(0, played.Run.Round);
        Assert.Empty(played.Result.Decisions);
        Assert.Empty(played.Result.Rounds);
    }

    [Fact]
    public void A_wave_the_phase_cannot_afford_is_refused_before_done_and_the_round_still_commits()
    {
        // Four skeletons cost 68 out of the 60 an archer left, and the whole
        // phase is refused -- Resolve walks take, then actions, then slots, so a
        // phase whose towers ate its wave does not resolve at all. It is refused
        // at the `send` and not at the `done`, which is the point of pricing
        // after every word: the take and the archer are still composed, so
        // `done` commits the round that was legal instead of losing it.
        //
        // OBSERVED: have composing keep a candidate that did not resolve rather
        // than the phase already composed -- which is what pricing the round
        // once at `done` amounts to. The 68-gold wave lands, and the next thing
        // that touches the decision throws the refusal out of the loop, so a
        // send a player could simply have undone ends the run instead.
        Run run = Fresh(TheMatch.Types(), fieldSize: 1);

        Session played = Play(
            run, "take ordinary 12", "place archer 6 2", "send skeleton 4", "done", "quit");

        Assert.Contains(
            "buys 68 gold of creeps out of a purse holding 60",
            played.Text,
            StringComparison.Ordinal);

        Assert.Equal(1, run.Round);
        Assert.Equal(1, run.Board.Count);
        Assert.Single(played.Result.Rounds);
        Assert.Empty(played.Result.Decisions[0].Slots);
        Assert.Equal(
            new[] { BuildAction.Of(ActionKind.Place, Archer, 6, 2) },
            played.Result.Decisions[0].Actions);
    }

    [Fact]
    public void Quit_and_a_transcript_that_runs_out_both_leave_the_round_they_stopped_in_unplayed()
    {
        // Two ways to stop short, told apart because one was chosen, and neither
        // of them commits the half-composed round it was holding. Both still
        // print the outcome and the board, because a run abandoned at wave one
        // is the artefact §8 wants written down rather than a session with
        // nothing to show.
        //
        // OBSERVED: commit whatever was composed when the loop stops. The quit
        // session plays the round the player was in the middle of deciding not
        // to play -- one round where this expects none -- and the transcript
        // that ran out hands Advance a null phase.
        Session quit = Play(Fresh(TheMatch.Types()), "take ordinary 12", "place archer 6 2", "quit");

        Assert.Equal(Ended.Quit, quit.Result.Ending);
        Assert.Equal(0, quit.Run.Round);
        Assert.Empty(quit.Result.Decisions);
        Assert.Contains(
            "Quit at wave 1 of 10, which is a round nobody played.",
            quit.Text,
            StringComparison.Ordinal);

        Session ran = Play(Fresh(TheMatch.Types()), "take ordinary 12");

        Assert.Equal(Ended.OutOfLines, ran.Result.Ending);
        Assert.Equal(0, ran.Run.Round);
        Assert.Contains(
            "The lines ran out at wave 1 of 10, which is a round nobody played.",
            ran.Text,
            StringComparison.Ordinal);

        // Both end on the two blocks a finished run ends on, over an empty
        // board, because the ending block is about the run and not about how
        // the session left it.
        Assert.EndsWith(
            "\n" + RunSummary.Outcome(quit.Run) + "\n\n" + quit.Run.Board.ToReportText() + "\n",
            quit.Text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Death_ends_the_run_where_the_shape_says_it_does_and_does_not_where_it_says_it_does_not()
    {
        // Nothing this loop can be killed by is killable, so every wave leaks in
        // full and the pool is spent on the fourth round. Whether that ends the
        // session is the run's own fold and never a test this loop makes: the
        // same transcript, the same rounds, and the flag alone decides whether
        // the fifth wave is offered.
        //
        // OBSERVED: stop the loop on `run.Health > 0 && run.Round < run.Waves`
        // instead of on `run.IsOver`. The death row still passes and the
        // no-death row comes back four rounds in and Unfinished -- a second copy
        // of the death rule, living in a prompt, quietly overruling the argument
        // the run was built with.
        Run dying = TheRun.Unstoppable(fieldSize: 1);
        Run living = TheRun.Unstoppable(deathEndsTheRun: false, fieldSize: 1);

        Session dies = Play(dying, TakingTheFirstOption(dying));
        Session lives = Play(living, TakingTheFirstOption(living));

        Assert.Equal(Ended.Over, dies.Result.Ending);
        Assert.Equal(RunEnding.OutOfHealth, dies.Run.Ending);
        Assert.Equal(4, dies.Result.Rounds.Count);

        Assert.Equal(Ended.Over, lives.Result.Ending);
        Assert.Equal(RunEnding.OutOfWaves, lives.Run.Ending);
        Assert.Equal(Run.DefaultWaves, lives.Result.Rounds.Count);
        Assert.Equal(0, lives.Run.Health);
    }

    /// <summary>What one canned session came to: the run it moved, the decisions, and the screen.</summary>
    private sealed class Session
    {
        public Session(Run run, Played result, string text)
        {
            Run = run;
            Result = result;
            Text = text;
        }

        /// <summary>The run the session played, as it stands afterwards.</summary>
        public Run Run { get; }

        /// <summary>What the loop handed back.</summary>
        public Played Result { get; }

        /// <summary>Everything the session printed.</summary>
        public string Text { get; }
    }

    /// <summary>The committed run's words, played into a run built as the command line builds one.</summary>
    private static Session PlayTheCommittedRun() =>
        Play(Fresh(TheMatch.Types()), TheCommittedRun.Split('\n'));

    /// <summary>Plays these lines into this run, and collects the screen.</summary>
    private static Session Play(Run run, params string[] typed)
    {
        var writer = new StringWriter();

        Played result = RunPrompt.Play(
            run,
            TheMatch.Ladder(run.Types),
            new StringReader(string.Join('\n', typed)),
            writer);

        return new Session(run, result, writer.ToString());
    }

    /// <summary>
    /// A transcript that takes the first thing on every round's menu and does
    /// nothing else, for as many rounds as a run can have.
    /// </summary>
    /// <remarks>
    /// The takes are read off the offerings rather than written out, because
    /// what these sessions are about is where a run stops -- and an offering is
    /// drawn from the seed and the wave, so a transcript with the ids in it
    /// would be a second statement of what the menus hold.
    /// </remarks>
    private static string[] TakingTheFirstOption(Run run)
    {
        var typed = new List<string>();

        for (int wave = 1; wave <= run.Waves; wave++)
        {
            Option first = run.OfferingAt(wave).Options[0];

            typed.Add("take " + CommandScript.WordFor(first.Kind) + " " + first.Id);
            typed.Add("done");
        }

        return typed.ToArray();
    }

    /// <summary>
    /// A fresh run on the committed content and the committed seed, against the
    /// canned field of one the command line builds.
    /// </summary>
    private static Run Fresh(UnitTypeTable types, int fieldSize = Run.DefaultFieldSize) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheSchedule.Committed(types),
            FieldPool.Canned(TheMatch.Layout(types), TheRun.FieldWave(types)),
            TheRun.Seed,
            Run.DefaultWaves,
            fieldSize);

    /// <summary>
    /// The round lines of the committed outcome file: what stands to the right
    /// of the arrow on every row that has one.
    /// </summary>
    /// <remarks>
    /// Read out of the file rather than replayed, so the oracle is the committed
    /// bytes and not a second play of the same record. Nothing that checks this
    /// file regenerates it.
    /// </remarks>
    private static string[] CommittedRoundLines()
    {
        var lines = new List<string>();

        foreach (string line in File.ReadAllText(RepoLayout.RunOutcomeFile).Split('\n'))
        {
            int at = line.IndexOf(Arrow, StringComparison.Ordinal);

            if (at >= 0)
            {
                lines.Add(line.Substring(at + Arrow.Length));
            }
        }

        return lines.ToArray();
    }
}
