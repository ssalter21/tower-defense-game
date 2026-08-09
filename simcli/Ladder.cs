using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// The upgrade ladder, printed: one line per edge with the price of the tier it
/// leads to, then everything a walk over the whole file had to say about it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This reads and prints and enforces nothing.</b> It exits zero with faults
/// in its output, and that is deliberate: the gate that fails a build on a fault
/// is a test in <c>sim.tests</c>, and a second enforcer here would be a rule
/// with two homes and no way to tell which one an author had met. What this is
/// for is looking at a roster -- reading a line off it and seeing what an
/// upgrade costs.
/// </para>
/// <para>
/// <b>Two files, not three.</b> A tier's price is the <see cref="UnitType.Cost"/>
/// on its own row, so nothing here needs a <see cref="Ruleset"/>:
/// <c>CostTable.From</c> wants one for the snapshot price, and no edge touches
/// that.
/// </para>
/// <para>
/// The notes come before the faults on purpose. A note names the shape of a
/// roster -- where its lines start, where they stop, what an upgrade buys -- and
/// that is the reading somebody ran this for; a fault is the thing they will
/// have gone to fix, and it is last so it is the text still on screen.
/// </para>
/// </remarks>
internal static class Ladder
{
    /// <summary>One line per edge, then the notes, then the faults.</summary>
    public static string ToText(UnitTypeTable types, UpgradeLadder ladder)
    {
        var text = new StringBuilder();

        for (int index = 0; index < ladder.Count; index++)
        {
            UpgradeEdge edge = ladder.Edges[index];
            UnitType from = types.ById(edge.From);
            UnitType to = types.ById(edge.To);

            text.Append("edge   ")
                .Append(from.Label.PadRight(18))
                .Append("-> ")
                .Append(to.Label.PadRight(18))
                .Append(to.Cost.ToString(PlainText.Culture).PadLeft(4))
                .Append(" gold\n");
        }

        LadderReport report = ladder.Completeness(types);

        Append(text, "note   ", report.Notes);
        Append(text, "fault  ", report.Faults);

        return text.ToString();
    }

    private static void Append(StringBuilder text, string label, IReadOnlyList<LadderFinding> findings)
    {
        for (int index = 0; index < findings.Count; index++)
        {
            text.Append(label).Append(findings[index].Sentence).Append('\n');
        }
    }
}
