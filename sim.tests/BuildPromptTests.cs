using System.Globalization;
using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// One round composed at a prompt, played from canned transcripts with no
/// terminal anywhere.
/// </summary>
/// <remarks>
/// <para>
/// <b>Every session here is a string of lines and a string of output.</b> That
/// is the claim §5 of <c>docs/playing-a-run-from-a-shell.md</c> makes about the
/// whole verb, and it is the reason this ticket handed the reader and the writer
/// in rather than reaching for the console: a test that needed a keyboard would
/// be a test nobody runs.
/// </para>
/// <para>
/// <b>What is under test is the loop and never the drawing.</b> The frames these
/// sessions print are asserted by comparing them against
/// <see cref="RoundFrame.ToText(Run, UpgradeLadder, BuildPhase?)"/>'s own answer,
/// so what goes red here is a word printing the wrong thing at the wrong moment
/// -- a frame that did not come back, a panel drawn for a decision that was
/// refused -- and a layout change reddens <c>RoundFrameTests</c>, which is where
/// the characters live.
/// </para>
/// <para>
/// <b>The run is read after every session.</b> Nothing in this ticket commits,
/// so a session that composed four towers and a wave has to leave the purse, the
/// board, the unlocks and the round count exactly where it found them. That is
/// the difference between pricing a decision and playing one.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong loop</b>, and
/// the wrong loop is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class BuildPromptTests
{
    /// <summary>
    /// What stands in front of a line waiting to be typed, restated here rather
    /// than read off the loop, so that changing it is a decision this goes red
    /// over.
    /// </summary>
    private const string Prompt = "> ";

    /// <summary>The archer's row on the committed roster.</summary>
    private const int Archer = 3;

    /// <summary>The option wave one's menu carries the skeleton at, and the skeleton's own row.</summary>
    private const int SkeletonOption = 12;

    private const int Skeleton = 12;

    /// <summary>The creep rows a run has unlocked by its fifth wave, and the one that round takes.</summary>
    private const int Minion = 1;

    private const int Scout = 2;

    private const int WarriorOption = 13;

    /// <summary>
    /// The committed run four rounds in, played once for the whole class.
    /// </summary>
    /// <remarks>
    /// Wave five is the earliest round this content reaches where the bodies a
    /// run may field outnumber the slots it has -- three slots, and four creeps
    /// once the round has taken its own. It is therefore the only place a send
    /// with no room can be got to at a prompt, which is the refusal
    /// <c>docs/playing-a-run-from-a-shell.md</c> §3 states outright. Nothing
    /// here composes against it more than once over, because composing reads the
    /// run and never writes it.
    /// </remarks>
    private static readonly Lazy<Run> WaveFive = new(FourRoundsIn);

    [Fact]
    public void The_frame_opens_the_round_and_comes_back_after_every_word_that_changes_it()
    {
        // Two frames for one word: the one the round opens on, and the one the
        // placement leaves. Each is the drawing's own answer for the decision
        // as far as it had been composed, and the prompt stands between them.
        //
        // OBSERVED: print the frame once, when the round opens. Every other
        // assertion in this file still passes -- the phase composes, the
        // refusals print, the run is untouched -- and the one thing the verb
        // exists for, a number that moves as you type, is gone.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);
        BuildPhase built = BuildPhase.Of().With(BuildAction.Of(ActionKind.Place, Archer, 6, 2));

        Session session = Play(run, ladder, "place archer 6 2", "done");

        Assert.Equal(
            RoundFrame.ToText(run, ladder, null) + "\n"
            + Prompt + RoundFrame.ToText(run, ladder, built) + "\n"
            + Prompt,
            session.Text);
    }

    [Fact]
    public void Nothing_is_committed_and_the_run_is_where_the_session_found_it()
    {
        // A whole round composed -- a tower and a wave -- and the run has not
        // moved: same round, same purse, same empty board. Resolve was called
        // after every word and every Build it returned was dropped.
        //
        // OBSERVED: have the loop call run.Advance(phase) on `done`. The
        // composed phase that comes back is identical and this is the only
        // thing that notices -- so whoever commits it next commits the round a
        // second time.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);
        int gold = run.Purse.Gold;

        Session session = Play(run, ladder, "place archer 6 2", "send skeleton 2", "done");

        Assert.Equal(0, run.Round);
        Assert.Equal(gold, run.Purse.Gold);
        Assert.Equal(0, run.Board.Count);

        BuildPhase phase = Composed(session);

        Assert.Equal(Stopped.Done, session.Composed.Stopped);
        Assert.Equal(new[] { BuildAction.Of(ActionKind.Place, Archer, 6, 2) }, phase.Actions);
        Assert.Equal(new[] { WaveSlot.Of(Skeleton, 2) }, phase.Slots);
    }

    [Fact]
    public void Gold_left_and_towers_standing_come_back_after_every_word()
    {
        // The two numbers docs/playing-a-run-from-a-shell.md §3 names, read off
        // the headers the session printed:
        // the opening hundred, the hundred a free take leaves, the sixty an
        // archer leaves, and the twenty-six two skeletons leave out of that.
        // The refused mage prints no frame at all, so the sequence has four
        // entries for five words.
        //
        // OBSERVED: resolve the candidate but reprint the frame from the phase
        // already composed. The gold column reads 100, 100, 100, 100 -- every
        // word lands, nothing is refused, and the screen never says what
        // anything cost.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(
            run,
            ladder,
            "take ordinary 12",
            "place archer 6 2",
            "send skeleton 2",
            "place mage 4 4",
            "done");

        Assert.Equal(new[] { 100, 60, 26 }, Gold(session.Text));

        // The tower is standing on the frames the session printed, in the
        // legend and on the grid, out of a board the run itself never grew.
        Assert.Contains("1  a  archer   6,2", session.Text, StringComparison.Ordinal);
        Assert.Equal(0, run.Board.Count);
    }

    [Fact]
    public void A_refusal_prints_its_own_sentence_and_the_loop_carries_on()
    {
        // Three refusals of three different kinds -- a misspelling the parser
        // turns down, a cell in the corridor the footing turns down, and a
        // tower the purse turns down -- and a legal word after them that lands.
        // None of them ends the session and none of them is added.
        //
        // OBSERVED: let the SimulationException out of the loop. The session
        // dies on the corridor cell, which at a prompt means a typo ends a run
        // -- and the sentence that would have said what was wrong is printed by
        // a stack trace instead.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(
            run,
            ladder,
            "take ordinary 12",
            "plaec archer 4 4",
            "place archer 3 1",
            "place archer 6 2",
            "place mage 4 4",
            "done");

        // The parser's refusal, which is a value rather than a throw and names
        // the word it could not read.
        Assert.Contains(
            "'plaec archer 4 4' opens with 'plaec', which is not a word here.",
            session.Text,
            StringComparison.Ordinal);

        // The simulation's own two, reprinted whole. Both already name the
        // round, the verb and the cell, because they were written for somebody
        // authoring a command file.
        Assert.Contains(
            "A build phase at wave 1 places at column 3, row 1,",
            session.Text,
            StringComparison.Ordinal);

        Assert.Contains(
            "A build phase at wave 1 places at column 4, row 4 for 92 gold out of a purse holding 60.",
            session.Text,
            StringComparison.Ordinal);

        // What was legal is what composed: the archer the last word placed, and
        // nothing the three refusals named.
        Assert.Equal(
            new[] { BuildAction.Of(ActionKind.Place, Archer, 6, 2) },
            Composed(session).Actions);
    }

    [Fact]
    public void Undo_drops_the_last_accepted_thing_even_where_the_word_before_it_was_refused()
    {
        // The mage is refused for 92 gold out of the 60 the archer left, and so
        // is never a thing that was added. The undo after it therefore drops the
        // archer -- the last word that was accepted -- and the round is back to
        // its take with a hundred gold.
        //
        // OBSERVED: keep the words typed rather than the phases accepted, and
        // drop the last of those. The undo takes back the mage, which was never
        // there, and the archer nobody undid stays standing and stays paid for.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(
            run, ladder, "place archer 6 2", "place mage 4 4", "undo", "done");

        BuildPhase phase = Composed(session);

        Assert.Empty(phase.Actions);
        Assert.Equal(new[] { 100, 60, 100 }, Gold(session.Text));
    }

    [Fact]
    public void Sends_fill_slots_in_the_order_they_were_typed_and_undo_drops_the_last_of_them()
    {
        // A slot is filled by naming a creep, and the next one by naming
        // another; the order is the order they were typed and not the order the
        // wave record wants them in. Undoing takes back the second send and
        // leaves the first, exactly as it does for a tower.
        //
        // OBSERVED: have Filling put the new slot in front of the ones already
        // filled. Both sends land, the wave is the same two creeps, and the undo
        // drops the minion the session asked to keep.
        Run run = WaveFive.Value;
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(run, ladder, "send minion 2", "send skeleton 1", "undo", "done");

        Assert.Equal(new[] { WaveSlot.Of(Minion, 2) }, Composed(session).Slots);
    }

    [Fact]
    public void A_send_out_of_order_is_refused_at_the_prompt()
    {
        // The refusal a send can still raise, printed in the record's own
        // sentence and not ending the session: a creep at or below the one a
        // slot above it already sent. The legal sends stand.
        //
        // The other half of this test was a fourth slot in a round that had
        // three. #179 deleted the widths with the anchors that derived them, so
        // what bounds a wave is the purse; there is no width left to exceed.
        //
        // OBSERVED: sort the slots into the ascending order Resolve asks for.
        // The out-of-order send lands, the wave sends what nobody composed, and
        // the round quietly reorders a decision on its author's behalf.
        Run run = WaveFive.Value;
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(
            run,
            ladder,
            "send minion 1",
            "send skeleton-scout 1",
            "send minion 1",
            "send skeleton 1",
            "done");

        Assert.Contains(
            "fills slot 3 with type id 1, at or below the 2 a slot above it already sent",
            session.Text,
            StringComparison.Ordinal);

        Assert.Equal(
            new[] { WaveSlot.Of(Minion, 1), WaveSlot.Of(Scout, 1), WaveSlot.Of(Skeleton, 1) },
            Composed(session).Slots);
    }

    [Fact]
    public void Undo_with_nothing_accepted_says_so_and_leaves_the_round_where_it_was()
    {
        // A round that has taken nothing has no last word to drop, and being
        // told so is the only thing that can happen: there is no state below
        // this one.
        //
        // OBSERVED: pop the list unguarded. The session throws on an empty
        // list, which is a crash at the prompt for a word whose whole promise
        // is that it costs nothing.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(run, ladder, "undo", "place archer 6 2", "done");

        Assert.Contains("There is nothing to undo.", session.Text, StringComparison.Ordinal);
        Assert.Single(Composed(session).Actions);
    }

    [Fact]
    public void Map_menu_and_costs_reprint_a_part_of_the_frame_and_change_nothing()
    {
        // Three words, three blocks, and a decision that is what it was before
        // them. Each block is the frame's own part for the phase as composed.
        //
        // OBSERVED: print the whole frame for all three. Every one of them
        // reads perfectly and the three words become one word spelled three
        // ways, which is a vocabulary that has to be learned to no purpose.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);
        BuildPhase built = BuildPhase.Of().With(BuildAction.Of(ActionKind.Place, Archer, 6, 2));

        Session session = Play(run, ladder, "place archer 6 2", "map", "menu", "costs", "done");

        Assert.Equal(
            RoundFrame.ToText(run, ladder, null) + "\n"
            + Prompt + RoundFrame.ToText(run, ladder, built) + "\n"
            + Prompt + RoundFrame.ToText(run, ladder, built, Panel.Map) + "\n"
            + Prompt + RoundFrame.ToText(run, ladder, built, Panel.Menu) + "\n"
            + Prompt + RoundFrame.ToText(run, ladder, built, Panel.Costs) + "\n"
            + Prompt,
            session.Text);

        BuildPhase phase = Composed(session);

        Assert.Single(phase.Actions);
        Assert.Empty(phase.Slots);
    }

    [Fact]
    public void Quit_and_a_transcript_that_runs_out_are_two_different_endings()
    {
        // Both hand back what was composed and neither is `done`, because what
        // the caller does next differs: one ends the run early on purpose and
        // the other is a transcript that stopped mid-round.
        //
        // OBSERVED: return Stopped.Done when the reader runs out. A transcript
        // truncated halfway through a session silently commits a half-composed
        // round, which is the one ending nobody chose.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session quit = Play(run, ladder, "place archer 6 2", "quit");

        Assert.Equal(Stopped.Quit, quit.Composed.Stopped);
        Assert.Single(Composed(quit).Actions);

        Session ran = Play(run, ladder, "send skeleton 2");

        Assert.Equal(Stopped.OutOfLines, ran.Composed.Stopped);
        Assert.Equal(new[] { WaveSlot.Of(Skeleton, 2) }, Composed(ran).Slots);

        // A session that typed nothing at all still hands back the empty phase
        // the round opened holding: there is nothing a round must do, so doing
        // nothing is a decision rather than an absence.
        Session silent = Play(run, ladder);

        Assert.Equal(Stopped.OutOfLines, silent.Composed.Stopped);
        Assert.NotNull(silent.Composed.Phase);
        Assert.Empty(silent.Composed.Phase!.Actions);
        Assert.Empty(silent.Composed.Phase!.Slots);
    }

    [Fact]
    public void A_blank_line_is_a_pressed_return_and_prints_nothing()
    {
        // Not a mistake and not a word: the prompt comes back and the frame does
        // not, because nothing about the decision changed.
        //
        // OBSERVED: treat a blank line as the parser's refusal. Holding return
        // fills the screen with a sentence about a word nobody typed.
        Run run = TheRun.Fresh();
        UpgradeLadder ladder = TheMatch.Ladder(run.Types);

        Session session = Play(run, ladder, string.Empty, "   ", "quit");

        Assert.Equal(
            RoundFrame.ToText(run, ladder, null) + "\n" + Prompt + Prompt + Prompt,
            session.Text);
    }

    /// <summary>What one canned session came to: the decision, and what it printed.</summary>
    private sealed class Session
    {
        public Session(Composed composed, string text)
        {
            Composed = composed;
            Text = text;
        }

        public Composed Composed { get; }

        public string Text { get; }
    }

    /// <summary>
    /// The committed run with its first four rounds played out of
    /// <c>content/commands.txt</c>, so that the round these sessions compose is
    /// one a play of the committed script actually reaches.
    /// </summary>
    /// <remarks>
    /// The pool is canned for the reason <c>RoundFrameTests</c> gives: a round
    /// resolves twenty matches and measuring the field costs a hundred more, and
    /// what is under test here is a loop rather than an economy.
    /// </remarks>
    private static Run FourRoundsIn()
    {
        UnitTypeTable types = TheMatch.Types();

        var run = new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            FieldPool.Canned(TheMatch.Layout(types), TheRun.FieldWave(types)),
            TheRun.Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);

        IReadOnlyList<RecordCommand> script = CommandScript.Parse(
            "commands.txt", File.ReadAllText(RepoLayout.CommandScriptFile));

        for (int index = 0; index < 4; index++)
        {
            run.Advance(script[index].ToPhase());
        }

        return run;
    }

    /// <summary>Plays these lines into the round the run is at, and collects the screen.</summary>
    private static Session Play(Run run, UpgradeLadder ladder, params string[] typed)
    {
        var writer = new StringWriter();

        Composed composed = BuildPrompt.Compose(
            run, ladder, new StringReader(string.Join('\n', typed)), writer);

        return new Session(composed, writer.ToString());
    }

    /// <summary>The phase a session composed, which every case here expects it to have one of.</summary>
    private static BuildPhase Composed(Session session)
    {
        Assert.NotNull(session.Composed.Phase);

        return session.Composed.Phase!;
    }

    /// <summary>
    /// The gold off every header the session printed, in the order it printed
    /// them -- which is one per frame, and therefore one per word that changed
    /// the decision, plus the frame the round opened on.
    /// </summary>
    /// <remarks>
    /// Only the header line is read, because a refusal about a purse names gold
    /// too and counting those would make a refused word look like an accepted
    /// one.
    /// </remarks>
    private static int[] Gold(string text)
    {
        var found = new List<int>();

        foreach (string line in text.Split('\n'))
        {
            if (!line.Contains("health ", StringComparison.Ordinal))
            {
                continue;
            }

            string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            for (int index = 0; index + 1 < words.Length; index++)
            {
                if (words[index] == "gold")
                {
                    found.Add(int.Parse(words[index + 1], CultureInfo.InvariantCulture));
                }
            }
        }

        return found.ToArray();
    }

    /// <summary>How many times a sentence appears in what a session printed.</summary>
    private static int Occurrences(string text, string sentence)
    {
        int found = 0;
        int at = text.IndexOf(sentence, StringComparison.Ordinal);

        while (at >= 0)
        {
            found++;
            at = text.IndexOf(sentence, at + sentence.Length, StringComparison.Ordinal);
        }

        return found;
    }
}
