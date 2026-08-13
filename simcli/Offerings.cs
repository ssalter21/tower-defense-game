using System.Text;
using Sim;

namespace Sim.Cli;

/// <summary>
/// Every wave's public menu, printed, so that a command script can be written
/// for a run before the run is played.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is what makes a command file authorable.</b> A take names a kind and
/// an id off the round's offering, and an offering is drawn from the run's seed
/// and the wave -- so nobody can write a legal decision for a seed they have
/// not been shown the menus of. Every row below is spelled with the words a
/// <c>build</c> row uses, so writing the script is copying two columns.
/// </para>
/// <para>
/// <b>Nothing here is derived twice.</b> The menus come from
/// <see cref="Run.OfferingAt"/>, which is the same call the run makes when it
/// resolves a round and the same one the replay gate validates a stored stream
/// against. A listing computed any other way would be a second copy of a
/// derivation, free to show a menu nobody plays against.
/// </para>
/// </remarks>
internal static class Offerings
{
    /// <summary>What separates a column of the listing from the one after it.</summary>
    private const string ColumnGap = "  ";

    /// <summary>One header per wave, and one row per thing on that wave's menu.</summary>
    public static string ToText(Run run)
    {
        var text = new StringBuilder();
        int labelWidth = LabelWidth(run.Types, run.Schedule);

        for (int wave = 1; wave <= run.Waves; wave++)
        {
            Offering offering = run.OfferingAt(wave);

            text.Append("wave ")
                .Append(offering.Wave.ToString(PlainText.Culture).PadLeft(3))
                .Append(", ")
                .Append(offering.WaveSlots.ToString(PlainText.Culture))
                .Append(offering.WaveSlots == 1 ? " slot" : " slots")
                .Append(offering.IsAnchor ? ", an anchor\n" : "\n");

            for (int index = 0; index < offering.Count; index++)
            {
                Option option = offering.Options[index];

                text.Append("  ")
                    .Append(CommandScript.WordFor(option.Kind).PadRight(9))
                    .Append(option.Id.ToString(PlainText.Culture).PadLeft(4))
                    .Append(ColumnGap)
                    .Append(option.Label.PadRight(labelWidth))
                    .Append(ColumnGap)
                    .Append("fields type ")
                    .Append(option.TypeId.ToString(PlainText.Culture))
                    .Append('\n');
            }
        }

        return text.ToString();
    }

    /// <summary>How wide the name column is: the widest label the content holds.</summary>
    /// <remarks>
    /// <para>
    /// Measured over the authored content rather than over the rows this
    /// listing happens to print, because which rows it prints depends on
    /// <c>--waves</c>: a width taken off them would have the same seed's wave 1
    /// come out one way in a three-wave listing and another in a ten-wave one,
    /// and a menu copied out of either would stop lining up with the other.
    /// </para>
    /// <para>
    /// <b>Both halves of the column are measured, because two files fill it.</b>
    /// An ordinary option names a walking unit off the roster and a changer is a
    /// row of the anchor schedule, so a pool holding a longer name than any
    /// creep would otherwise carry its own row's tail right.
    /// </para>
    /// <para>
    /// This is <c>RoundFrame.LabelWidth</c>'s reckoning, over the same two
    /// files, so the listing a script is written from and the frame it is played
    /// against put their names in the same column.
    /// </para>
    /// </remarks>
    private static int LabelWidth(UnitTypeTable types, AnchorSchedule schedule)
    {
        int widest = 0;

        for (int index = 0; index < types.Count; index++)
        {
            UnitType type = types.Types[index];

            if (type.Role == UnitRole.Moving)
            {
                widest = Math.Max(widest, type.Label.Length);
            }
        }

        for (int index = 0; index < schedule.GameChangers.Count; index++)
        {
            widest = Math.Max(widest, schedule.GameChangers[index].Label.Length);
        }

        return widest;
    }
}
