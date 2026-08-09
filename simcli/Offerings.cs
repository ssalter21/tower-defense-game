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
    /// <summary>One header per wave, and one row per thing on that wave's menu.</summary>
    public static string ToText(Run run)
    {
        var text = new StringBuilder();

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
                    .Append("  ")
                    .Append(option.Label.PadRight(16))
                    .Append("fields type ")
                    .Append(option.TypeId.ToString(PlainText.Culture))
                    .Append('\n');
            }
        }

        return text.ToString();
    }
}
