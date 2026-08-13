using System.Globalization;
using System.Text;
using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The decisions a session collected, written back out as a command script.
/// </summary>
/// <remarks>
/// <para>
/// <b>The oracle is the committed script and never a string this file
/// writes.</b> <c>content/commands.txt</c> is a whole run's decisions in this
/// grammar, so reading it, handing the phases back to the writer and comparing
/// what comes out is a claim about the grammar rather than about a sentence
/// somebody typed twice. What is asserted is the commands, parsed back --
/// character comparison alone would go green on a writer that produced legible
/// text nothing could read.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong writer</b>,
/// and the wrong writer is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class PlayedScriptTests
{
    /// <summary>Where a run cut short in the middle of the committed script stops.</summary>
    private const int CutShortAt = 6;

    /// <summary>
    /// The committed run's decisions, read once for the whole class: every case
    /// here starts from the same ten, and a file parsed per assertion would be
    /// the same parse four times.
    /// </summary>
    private static readonly Lazy<IReadOnlyList<RecordCommand>> Committed = new(() =>
        CommandScript.Parse("commands.txt", File.ReadAllText(RepoLayout.CommandScriptFile)));

    [Fact]
    public void The_committed_scripts_own_decisions_are_written_back_as_the_committed_script()
    {
        // Ten rounds out and ten rounds back: the phases the committed file
        // decides, written by this writer, parse to the commands that file
        // parses to. Every take, every slot -- empties included -- and every
        // action in the order it was written.
        //
        // OBSERVED: write the actions before the build row they belong to. The
        // text still looks like a script and CommandScript.Parse refuses it
        // outright -- "acts on wave 1, and no build row stands above it" -- so
        // this goes red on the exception rather than on a comparison, which is
        // the shape a row in the wrong place takes.
        //
        // OBSERVED: drop a slot that is empty, on the argument that a slot
        // nobody filled is a slot nobody decided. Wave four sends its 20
        // skeleton-scouts out of the third slot, so the row comes back filling
        // the first -- a legal script, a different wave, and the commands go red
        // on it.
        IReadOnlyList<RecordCommand> committed = TheCommittedScript();

        string written = PlayedScript.Of(Phases(committed));

        Assert.Equal(committed, CommandScript.Parse("the written script", written));

        // And it is written the way the file it came from is written, column
        // for column, so a played run pastes into the authored script and diffs
        // against it row by row.
        //
        // OBSERVED: separate every field with one space. Both assertions above
        // stay green -- it is the same script -- and this goes red on row one,
        // which is the whole of what it is here to notice.
        Assert.Equal(TheCommittedRows(), written);
    }

    [Fact]
    public void Every_row_carries_its_wave_and_spells_everything_but_the_two_words_in_numbers()
    {
        // The claim of the ticket, read off the text: a decision row opens with
        // the grammar's own keyword and a take kind, an action row with one of
        // the two action words, and every other field on either is a number.
        // Nothing a person may type at a prompt -- a label, a round the prompt
        // never asked for -- survives into what is stored.
        //
        // OBSERVED: write the roster's label where a place names its type --
        // "place 5 archer 6 2", which is what echoing the typed word back
        // amounts to. CommandScript.Parse refuses it, so the round trip above
        // goes red too; this names which field it was.
        //
        // OBSERVED: leave the wave off the action rows, on the argument that
        // they sit under the build row that names it. Every action row then
        // reads as four fields and the parser refuses the arity, which is the
        // elision this row is here to catch.
        IReadOnlyList<RecordCommand> committed = TheCommittedScript();

        string[] rows = PlayedScript.Of(Phases(committed)).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        int wave = 0;

        foreach (string row in rows)
        {
            string[] fields = row.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            bool decides = fields[0] == CommandScript.DecisionWord;

            if (decides)
            {
                wave++;
            }
            else
            {
                Assert.Contains(
                    fields[0],
                    new[] { CommandScript.WordFor(ActionKind.Place), CommandScript.WordFor(ActionKind.Upgrade) });
            }

            Assert.Equal(wave.ToString(CultureInfo.InvariantCulture), fields[1]);

            // Every field after the wave is a number. The take used to put one
            // word among them and went with the offering it named, so a build
            // row is now the wave and pairs of integers all the way across.
            for (int index = 2; index < fields.Length; index++)
            {
                Assert.True(
                    int.TryParse(fields[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out _),
                    "'" + row + "' carries '" + fields[index] + "' where a stored script carries a number.");
            }
        }

        Assert.Equal(Run.DefaultWaves, wave);
    }

    [Fact]
    public void A_run_cut_short_writes_a_script_record_run_compiles_and_play_run_plays_as_it_was_played()
    {
        // A session abandoned part-way is the artefact
        // docs/playing-a-run-from-a-shell.md §8 most wants written down, so what
        // it writes has to stand on its own: six rounds, in the grammar, taken
        // through the actual verb -- read as bytes, played to the end and only
        // then written. Nothing about the six rows knows the four that never
        // happened.
        //
        // OBSERVED: number the waves from zero. record-run exits 1 rather than
        // writing anything -- "the wave is 0, and the least a wave may be is 1"
        // -- and the assertion carries the refusal, which is what an off-by-one
        // in the one field a prompt never asks for looks like from a shell.
        string script = PlayedScript.Of(Phases(TheCommittedScript()).Take(CutShortAt).ToArray());

        string scratch = TheCommandLine.Scratch("played-script");
        string source = Path.Combine(scratch, "commands.txt");
        string written = Path.Combine(scratch, "run.commands");

        File.WriteAllText(source, script);

        TheCommandLine.Invoke(
            new[]
            {
                "record-run",
                "--script", source,
                "--seed", TheCommandLine.RunSeed.ToString(CultureInfo.InvariantCulture),
                "--out", written,
            }.Concat(TheCommandLine.RunContent))
            .Succeeded();

        // Six rounds, and each of them the decision the committed script made
        // in that wave -- so a run cut short is the run it was up to the wave it
        // stopped on, rather than six rows that merely compile.
        CommandStream recorded = CommandStream.FromBytes(File.ReadAllBytes(written));

        Assert.Equal(CutShortAt, recorded.Commands.Count);
        Assert.Equal(TheCommittedScript().Take(CutShortAt), recorded.Commands);

        // And then it plays, which is the other half of what
        // docs/playing-a-run-from-a-shell.md §5 claims of a session that quit:
        // record-run compiles the short script AND play-run plays it. The six
        // rounds that come back are the first six of content/run-outcome.txt
        // character for character -- the rounds the session was shown -- so the
        // chain from a decision typed at a prompt to a committed round line is
        // closed rather than asserted a link at a time.
        //
        // OBSERVED: stop at the compile above, which is where this test stopped
        // until now. Every assertion stays green against a record that reads
        // back and would refuse to play: the compile proves the grammar and the
        // bytes, and says nothing about whether the six rounds are the six.
        string played = TheCommandLine.Invoke(
            new[] { "play-run", "--commands", written }.Concat(TheCommandLine.RunContent))
            .Succeeded()
            .Output;

        Assert.Contains(
            TheRoundsTheCommittedRunReported(CutShortAt), played, StringComparison.Ordinal);
    }

    [Fact]
    public void A_round_that_filled_no_slot_writes_a_row_that_fills_none()
    {
        // Which is what every round played at a prompt looks like: an empty slot
        // is a position a stored row spells 0 0, and at a prompt it is a `send`
        // nobody typed -- so a session's phases carry the slots it filled and
        // stop there. A row may fill fewer slots than its round has, and this is
        // the shape that says so.
        //
        // OBSERVED: pad every row out to the widest slot count in the script.
        // The empty pairs are legal and they are a decision nobody made: wave
        // one comes back deciding two empty slots where the player filled none,
        // so a script and the session it came from disagree about what was
        // decided in a round that read as fine.
        //
        // OBSERVED: leave the padding on the end of a row that stops early. The
        // commands still compare equal, because trailing blanks tokenise away;
        // the field count below is what notices.
        BuildPhase nothing = BuildPhase.Of();
        BuildPhase sending = BuildPhase.Of(WaveSlot.Of(1, 2))
            .With(BuildAction.Of(ActionKind.Place, 3, 6, 2));

        string written = PlayedScript.Of(new[] { nothing, sending });

        Assert.Equal(
            new[] { RecordCommand.Of(1, nothing), RecordCommand.Of(2, sending) },
            CommandScript.Parse("the written script", written));

        string[] rows = written.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal(3, rows.Length);
        Assert.Equal(CommandScript.DecisionWord + "    1", rows[0]);
    }

    [Fact]
    public void A_field_that_fills_its_column_keeps_the_space_that_separates_it_from_the_next()
    {
        // The layout is a layout and never a separator. A hundred of a
        // two-digit creep is six characters where a slot's column is six wide,
        // and a place of a five-digit type id fills the column an action's cell
        // follows -- both are decisions the record stores, so both have to come
        // back off the text as what went in rather than as one merged field.
        //
        // OBSERVED: pad each field with PadRight alone, which is what writing
        // the committed script -- where nothing reaches its column's width --
        // makes look correct. The wave row comes out as "12 10013 5", the
        // parser refuses seven fields on a build row, and the round trip dies on
        // a script this writer wrote.
        BuildPhase wide = BuildPhase.Of(WaveSlot.Of(12, 100), WaveSlot.Of(13, 5));

        BuildPhase acting = BuildPhase.Of()
            .With(BuildAction.Of(ActionKind.Place, 65535, 100, 2));

        string written = PlayedScript.Of(new[] { wide, acting });

        Assert.Equal(
            new[] { RecordCommand.Of(1, wide), RecordCommand.Of(2, acting) },
            CommandScript.Parse("the written script", written));
    }

    [Fact]
    public void A_session_that_played_no_round_writes_nothing_and_the_grammar_says_why()
    {
        // A run quit before its first round committed decided nothing, and this
        // grammar has no row for that. Writing nothing is what leaves the one
        // sentence about it where it already lives -- in the parser -- rather
        // than growing a second copy out here.
        //
        // OBSERVED: write a build row for wave one anyway, out of the phase the
        // abandoned round was holding. It parses, it compiles, and it is a round
        // the run never played: the record would replay a decision the player
        // declined to commit.
        Assert.Equal(string.Empty, PlayedScript.Of(Array.Empty<BuildPhase>()));

        ContentException refused = Assert.Throws<ContentException>(
            () => CommandScript.Parse("the written script", string.Empty));

        Assert.Contains("decides nothing at all", refused.Message, StringComparison.Ordinal);
    }

    /// <summary>The committed run's decisions, as the file that authored them spells them.</summary>
    private static IReadOnlyList<RecordCommand> TheCommittedScript() => Committed.Value;

    /// <summary>
    /// The rows of that file with its prose taken out: what a writer that wrote
    /// the same script the same way would produce, and nothing else.
    /// </summary>
    private static string TheCommittedRows()
    {
        var rows = new StringBuilder();

        foreach (string line in File.ReadAllText(RepoLayout.CommandScriptFile).Split('\n'))
        {
            string row = line.TrimEnd();

            if (row.Length > 0 && !row.TrimStart().StartsWith('#'))
            {
                rows.Append(row).Append('\n');
            }
        }

        return rows.ToString();
    }

    /// <summary>
    /// The opening rounds of <c>content/run-outcome.txt</c> as that file spells
    /// them: the decision, the arrow, and the round the committed run reported.
    /// </summary>
    /// <remarks>
    /// A prefix of a run is the run: the first six rounds of a six-round record
    /// and of the ten-round one it was cut from are resolved against the same
    /// seed, the same offerings and the same purse, so the committed file is an
    /// oracle for the short one and nothing here has to re-derive a round to
    /// have something to compare against.
    /// </remarks>
    private static string TheRoundsTheCommittedRunReported(int rounds)
    {
        var lines = new List<string>();

        foreach (string line in File.ReadAllText(RepoLayout.RunOutcomeFile).Split('\n'))
        {
            if (line.Contains("   ->   ", StringComparison.Ordinal))
            {
                lines.Add(line.TrimEnd());
            }
        }

        // The file holds a whole run, so a prefix of it is a prefix of
        // something -- a file that had lost rows would otherwise be compared
        // against happily.
        Assert.Equal(Run.DefaultWaves, lines.Count);

        return string.Join('\n', lines.Take(rounds));
    }

    /// <summary>
    /// Stored commands as the decisions a session hands back: the four things a
    /// command stores are the four things a phase is, so nothing is reshaped
    /// getting there.
    /// </summary>
    private static BuildPhase[] Phases(IReadOnlyList<RecordCommand> commands) =>
        commands.Select(command => command.ToPhase()).ToArray();
}
