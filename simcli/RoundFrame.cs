using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// One round drawn as the frame that stands above the prompt: what the run is
/// at, the playfield, this wave's menu, what a slot may be filled with, and
/// where the decision being composed has got to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing on the frame is a forecast.</b> No predicted damage, no
/// assessment that a board will hold, no number that came out of resolving a
/// wave nobody has sent. What a tower costs, what a creep costs, where the
/// corridor runs and how many slots the round has are mechanism, and mechanism
/// is free and total -- that is vision §12, and a prompt is exactly where the
/// temptation to helpfully compute an outcome shows up. Everything here is read
/// off the run, the round's offering and the cost table.
/// </para>
/// <para>
/// <b>The menu is spelled in the words a command script uses.</b>
/// <c>ordinary 12</c> on the screen and <c>ordinary 12</c> in the file, because
/// the word comes from <see cref="CommandScript.WordFor"/> -- the list a script
/// is parsed against -- rather than from a second list beside it. There is one
/// vocabulary, and this reads it.
/// </para>
/// <para>
/// <b>Pure: a run and the phase being composed in, a string out.</b> No
/// console, nothing written down and nothing moved. The phase is resolved here
/// to price it, which <see cref="BuildPhase.Resolve"/> does without touching the
/// run -- so the gold, the board and the unlocks on the frame are the ones the
/// decision would leave, and they are the same call's answer the loop refuses a
/// word with rather than a second reckoning of it.
/// </para>
/// <para>
/// <b>The wave is the run's own next one.</b> An offering is drawn from the seed
/// and the wave, so a wave handed in beside the run is a second statement of
/// where the run is, free to disagree with it and to draw a menu nobody is
/// playing against.
/// </para>
/// </remarks>
internal static class RoundFrame
{
    /// <summary>What separates two readings on the header line.</summary>
    private const int HeaderGap = 8;

    /// <summary>
    /// How wide an id column is on the panels here. Three, so that the ids of a
    /// roster an order of magnitude larger still sit in it, and one width across
    /// the frame so that reading down a column is reading ids.
    /// </summary>
    private const int IdWidth = 3;

    /// <summary>What separates a column of a panel from the one after it.</summary>
    private const string ColumnGap = "  ";

    /// <summary>How far the menu's rows sit under the word heading them.</summary>
    private const string RowIndent = "  ";

    /// <summary>How wide the menu's take-kind column is: the longest of the words a script spells one with.</summary>
    private const int WordWidth = 9;

    /// <summary>How wide the creep-name column is, on the menu and on the sendable panel alike.</summary>
    private const int LabelWidth = 16;

    /// <summary>How wide the buildable panel's tower-name column is.</summary>
    private const int NameWidth = 9;

    /// <summary>How wide the price beside a tower's name is.</summary>
    private const int TowerPriceWidth = 3;

    /// <summary>How wide the price beside a creep's name is.</summary>
    private const int CreepPriceWidth = 2;

    /// <summary>What separates the menu from the sendable panel beside it.</summary>
    private const int MenuGap = 2;

    /// <summary>What the panel of things that may be built calls itself.</summary>
    private const string BuildableHeading = "you may build";

    /// <summary>What the panel of this round's offering calls itself.</summary>
    private const string MenuHeading = "this wave's menu";

    /// <summary>What the panel of things a slot may be filled with calls itself.</summary>
    private const string SendableHeading = "what you may send";

    /// <summary>
    /// The whole frame, as one block of lines with no trailing newline: the
    /// header, the map and its two panels, the menu and the sendable panel
    /// beside it, and the status line under them.
    /// </summary>
    /// <param name="run">The run the round belongs to, as it stands before the round.</param>
    /// <param name="ladder">Which unit follows which, which is how the map cases its letters.</param>
    /// <param name="composing">
    /// The decision as far as it has been composed, or nothing where the round
    /// has not been given its take yet.
    /// </param>
    public static string ToText(Run run, UpgradeLadder ladder, BuildPhase? composing)
    {
        ArgumentNullException.ThrowIfNull(run);

        Offering offering = run.Offering;
        Build? composed = composing?.Resolve(
            offering, run.Unlocks, run.Purse, run.Costs, run.Types, run.Map, run.Board);

        return new StringBuilder()
            .Append(Header(run, offering, composed))
            .Append("\n\n")
            .Append(BoardMap.ToText(
                run.Map, composed?.Board ?? run.Board, ladder, Buildable(run), IdWidth))
            .Append("\n\n")
            .Append(Menus(run, offering, composed?.Unlocks ?? run.Unlocks))
            .Append("\n\n")
            .Append(Status(composing, composed))
            .ToString();
    }

    /// <summary>
    /// The four readings a decision is made against: which round of how many,
    /// what is left of the pool, what there is to spend, and how many slots the
    /// wave has.
    /// </summary>
    private static string Header(Run run, Offering offering, Build? composed) =>
        string.Join(
            new string(' ', HeaderGap),
            "wave " + Number(offering.Wave) + " of " + Number(run.Waves),
            "health " + Number(run.Health) + " of " + Number(run.Rules.HealthPoolGold),
            "gold " + Number((composed?.Purse ?? run.Purse).Gold),
            Number(offering.WaveSlots) + (offering.WaveSlots == 1 ? " slot" : " slots"));

    /// <summary>
    /// Every tower the roster can stand on a cell, cheapest first, with what one
    /// costs.
    /// </summary>
    /// <remarks>
    /// Cheapest first because what the panel is read for is what this round can
    /// afford, and the price is asked of the cost table rather than read off the
    /// unit row -- the table is what a purchase is actually priced by, and the
    /// things it prices are not all units.
    /// </remarks>
    private static string[] Buildable(Run run)
    {
        var towers = new List<UnitType>();

        for (int index = 0; index < run.Types.Count; index++)
        {
            if (run.Types.Types[index].Role == UnitRole.Placed)
            {
                towers.Add(run.Types.Types[index]);
            }
        }

        towers.Sort(ByPriceThenId(run.Costs));

        var lines = new string[towers.Count + 1];
        lines[0] = BuildableHeading;

        for (int index = 0; index < towers.Count; index++)
        {
            UnitType tower = towers[index];

            lines[index + 1] = new StringBuilder()
                .Append(Number(tower.Id).PadLeft(IdWidth))
                .Append(ColumnGap)
                .Append(tower.Label.PadRight(NameWidth))
                .Append(Number(PriceOf(run.Costs, tower.Id)).PadLeft(TowerPriceWidth))
                .ToString();
        }

        return lines;
    }

    /// <summary>
    /// Cheapest first, and by id where two cost the same, so that the order is
    /// total and a panel of it cannot come out two ways.
    /// </summary>
    private static Comparison<UnitType> ByPriceThenId(CostTable costs) => (left, right) =>
    {
        int cheaper = PriceOf(costs, left.Id).CompareTo(PriceOf(costs, right.Id));

        return cheaper != 0 ? cheaper : left.Id.CompareTo(right.Id);
    };

    /// <summary>This wave's offering, and what a slot may be filled with, side by side.</summary>
    private static string Menus(Run run, Offering offering, Unlocks unlocks)
    {
        string[] menu = Menu(offering);
        var lines = new List<string>(menu);

        TextPanel.Beside(lines, Sendable(run, unlocks), 0, TextPanel.Widest(menu) + MenuGap);

        return string.Join('\n', lines);
    }

    /// <summary>
    /// One row per thing on this round's menu: the word a script takes it with,
    /// its id, what it is called and the creep it unlocks.
    /// </summary>
    /// <remarks>
    /// The type id is on the row because it is the other half of the mechanism:
    /// a take is spelled by the option's id and a send by the creep's, and on an
    /// anchor's menu those are two different numbers over one body.
    /// </remarks>
    private static string[] Menu(Offering offering)
    {
        var lines = new string[offering.Count + 1];
        lines[0] = MenuHeading;

        for (int index = 0; index < offering.Count; index++)
        {
            Option option = offering.Options[index];

            lines[index + 1] = new StringBuilder()
                .Append(RowIndent)
                .Append(CommandScript.WordFor(option.Kind).PadRight(WordWidth))
                .Append(Number(option.Id).PadLeft(IdWidth))
                .Append(ColumnGap)
                .Append(option.Label.PadRight(LabelWidth))
                .Append(ColumnGap)
                .Append("type ")
                .Append(Number(option.TypeId))
                .ToString();
        }

        return lines;
    }

    /// <summary>
    /// One row per creep this run may field, in the order it took them, with
    /// what one costs.
    /// </summary>
    /// <remarks>
    /// A body is listed once however many takes reached it: two game changers
    /// can field one creep, and a run that took an ordinary option and then a
    /// changer over the same body may send that creep by one type id either way.
    /// </remarks>
    private static string[] Sendable(Run run, Unlocks unlocks)
    {
        var lines = new List<string> { SendableHeading };
        var listed = new List<int>();

        for (int index = 0; index < unlocks.Count; index++)
        {
            UnitType creep = unlocks.Taken[index].Type;

            if (listed.Contains(creep.Id))
            {
                continue;
            }

            listed.Add(creep.Id);

            lines.Add(new StringBuilder()
                .Append(Number(creep.Id).PadLeft(IdWidth))
                .Append(ColumnGap)
                .Append(creep.Label.PadRight(LabelWidth))
                .Append(Number(PriceOf(run.Costs, creep.Id)).PadLeft(CreepPriceWidth))
                .Append(" each")
                .ToString());
        }

        return lines.ToArray();
    }

    /// <summary>
    /// Where the decision has got to: what it took, how many things it does to
    /// the board, and how many slots it has filled.
    /// </summary>
    /// <remarks>
    /// The three are counted off the phase and off what resolving it came to,
    /// never off a second walk of what was typed. What was built is the phase's
    /// own actions rather than the difference between two boards, because an
    /// upgrade leaves the count of what is standing where it was.
    /// </remarks>
    private static string Status(BuildPhase? composing, Build? composed)
    {
        int actions = composing is null ? 0 : composing.Actions.Count;
        int filled = composed is null ? 0 : composed.Wave.Count;

        return string.Join(
            ", ",
            composed is null
                ? "nothing taken"
                : "took "
                    + CommandScript.WordFor(composed.Taken.Kind)
                    + " "
                    + Number(composed.Taken.Id)
                    + " "
                    + composed.Taken.Label,
            actions == 0 ? "nothing built" : Number(actions) + " built",
            filled == 0
                ? "no slot filled"
                : Number(filled) + (filled == 1 ? " slot filled" : " slots filled"))
            + ".";
    }

    /// <summary>What one of a unit costs, out of the table a purchase is priced by.</summary>
    private static int PriceOf(CostTable costs, int typeId) => costs.PriceOf(Purchase.Unit(typeId));

    private static string Number(int value) => value.ToString(PlainText.Culture);
}
