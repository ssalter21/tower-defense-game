using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// The decisions a session collected, written back out as a command script in
/// <c>content/commands.txt</c>'s grammar -- the one <c>record-run</c> compiles.
/// </summary>
/// <remarks>
/// <para>
/// <b>The record's own spelling, never the player's.</b> A label typed at a
/// prompt reached the decision as the id it resolved to, and the wave was never
/// asked for at all; both are written on every row from the decision itself. So
/// what was typed is a convenience and what is stored is the record's, and a
/// run somebody played can be replayed, committed and diffed.
/// </para>
/// <para>
/// <b>The three keywords come off <see cref="CommandScript"/>.</b> The parser
/// declares the word a decision row opens with and the word each kind of take
/// and each defensive action is spelled by, so nothing here restates the
/// grammar it is writing -- the writer and the reader cannot come to hold two
/// of them.
/// </para>
/// <para>
/// <b>The wave is stamped through the record.</b> A run opens on wave one and a
/// decision reaches a session's list when its round commits, so the nth
/// decision is the nth wave -- and handing that pair to
/// <see cref="RecordCommand.Of(int, BuildPhase)"/> is what checks a decision can
/// be stored at all before it is written down as though it could. A phase whose
/// filled slots do not ascend is refused there, in the record's own sentence,
/// rather than becoming a script whose own compiler turns it down.
/// </para>
/// <para>
/// <b>Nothing here is written to a file.</b> Text out, and whether it is fit to
/// keep is a question for whoever proves it: a session's script is compared
/// against a fresh run of itself before it lands anywhere. See
/// <c>docs/playing-a-run-from-a-shell.md</c> §4.
/// </para>
/// <para>
/// <b>A session that played no round writes nothing at all.</b> There is no
/// spelling in this grammar for a run nobody played, and
/// <see cref="CommandScript.Parse(string)"/> already refuses an empty script by
/// name -- so the rule keeps its one implementation and this does not grow a
/// second copy of it.
/// </para>
/// <para>
/// <b>The columns land where <c>content/commands.txt</c>'s land.</b> The
/// authored file is the only other thing written in this grammar, so a played
/// run pastes into it and diffs against it row by row instead of arriving as
/// the same decisions differently spaced.
/// </para>
/// </remarks>
internal static class PlayedScript
{
    /// <summary>
    /// The columns every row is laid out on: the keyword, then the wave
    /// right-aligned under two digits, then two spaces. A build row continues
    /// with the take kind, the take id right-aligned, five spaces and a column
    /// per slot; an action row with the type id and the cell.
    /// </summary>
    private const int WordWidth = 8;

    private const int WaveWidth = 2;

    private const int KindWidth = 10;

    private const int TakeIdWidth = 2;

    private const int SlotWidth = 6;

    private const int TypeIdWidth = 4;

    private const int ColumnWidth = 3;

    /// <summary>What stands between the wave and the rest of a row.</summary>
    private const string Gap = "  ";

    /// <summary>What stands between the take id and the first slot.</summary>
    private const string BeforeSlots = "     ";

    /// <summary>These decisions as a script, a round at a time, in wave order.</summary>
    public static string Of(IReadOnlyList<BuildPhase> decisions)
    {
        ArgumentNullException.ThrowIfNull(decisions);

        var text = new StringBuilder();

        for (int index = 0; index < decisions.Count; index++)
        {
            RecordCommand command = RecordCommand.Of(index + 1, decisions[index]);

            Row(text, Decision(command));

            // Under the decision row and in the order they were written, which
            // is the order they mean: a round may upgrade what it has just
            // placed, and the placement ordinals fall out of the sequence.
            for (int action = 0; action < command.Actions.Count; action++)
            {
                Row(text, Action(command.Wave, command.Actions[action]));
            }
        }

        return text.ToString();
    }

    /// <summary>What one round took and how it filled its wave's slots.</summary>
    private static StringBuilder Decision(RecordCommand command)
    {
        StringBuilder row = Opens(CommandScript.DecisionWord, command.Wave)
            .Append(Column(CommandScript.WordFor(command.Take), KindWidth))
            .Append(Number(command.TakeId).PadLeft(TakeIdWidth))
            .Append(BeforeSlots);

        for (int index = 0; index < command.Slots.Count; index++)
        {
            WaveSlot slot = command.Slots[index];

            // An empty slot is 0 0, which is the pair it already holds -- so a
            // slot left alone and a slot filled are one line of writing here,
            // exactly as they are one pair of fields on a row.
            row.Append(Column(Number(slot.TypeId) + " " + Number(slot.Count), SlotWidth));
        }

        return row;
    }

    /// <summary>One thing a round did to the board, under the round that did it.</summary>
    private static StringBuilder Action(int wave, BuildAction action) =>
        Opens(CommandScript.WordFor(action.Kind), wave)
            .Append(Column(Number(action.TypeId), TypeIdWidth))
            .Append(Column(Number(action.Column), ColumnWidth))
            .Append(Number(action.Row));

    /// <summary>The keyword and the wave every row of either kind opens with.</summary>
    private static StringBuilder Opens(string word, int wave) =>
        new StringBuilder()
            .Append(Column(word, WordWidth))
            .Append(Number(wave).PadLeft(WaveWidth))
            .Append(Gap);

    /// <summary>
    /// One field in its column, and never the field that follows it as well: a
    /// value that fills its column pushes the rest of the row along rather than
    /// running into what comes next. Padding is a layout, and a row whose fields
    /// had merged would be a decision nothing could read back.
    /// </summary>
    private static string Column(string value, int width) => value.PadRight(width - 1) + " ";

    /// <summary>
    /// One row, without the padding that ran off the end of its last field, and
    /// the newline that ends it.
    /// </summary>
    private static void Row(StringBuilder text, StringBuilder row) =>
        text.Append(row.ToString().TrimEnd()).Append('\n');

    private static string Number(int value) => value.ToString(PlainText.Culture);
}
