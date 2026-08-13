using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The listing a command script is written from, against the committed content.
/// </summary>
/// <remarks>
/// <para>
/// <b>The whole listing is asserted, not a property of it.</b> What can be wrong
/// with a drawing is where its characters are -- a name run into the column
/// after it, a heading over the wrong rows -- and none of that is visible to an
/// assertion that looks for a substring. It is all visible in a diff of the
/// block. The sixteen-character <c>skeleton-warrior</c> printed
/// <c>skeleton-warriorfields type 13</c> for as long as nothing pinned this,
/// while every <c>Assert.Contains</c> aimed at the verb stayed green.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong drawing</b>,
/// and the wrong drawing is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class OfferingsTests
{
    /// <summary>
    /// How many waves the pinned listing covers. Three, because wave three is
    /// the first anchor and the only rows that come off the anchor schedule
    /// rather than the roster are on it.
    /// </summary>
    private const string PinnedWaves = "3";

    /// <summary>
    /// A game changer's label, planted long enough to be the widest thing the
    /// name column can print: twenty characters against the sixteen of
    /// <c>skeleton-warrior</c>, so the four the column gains are countable.
    /// </summary>
    private const string LongChanger = "thermal-riser-mk-two";

    /// <summary>The changer label in the committed schedule that is replaced.</summary>
    private const string PlantedOver = "thermal-riser";

    [Fact]
    public void The_whole_listing_puts_every_row_in_the_same_columns()
    {
        // What makes a command file authorable is copying two columns off this
        // into a build row, so the columns have to be there. Wave three carries
        // both halves of the vocabulary -- ordinary options off the roster and
        // changers off the anchor schedule -- and the widest label the content
        // holds, skeleton-warrior, is on wave one.
        //
        // OBSERVED: put the name column back on a constant 16 with no gap after
        // it, which is what this verb did until #178. Wave one's third row
        // becomes "skeleton-warriorfields type 13" and this goes red on that
        // line, while every Assert.Contains in CommandLineTests stays green.
        CommandLineResult listed = TheCommandLine.Invoke(
            new[] { "offerings", "--seed", "20260807", "--waves", PinnedWaves }
                .Concat(TheCommandLine.RunContent))
            .Succeeded();

        Assert.Equal(
            """
            wave   1, 2 slots
              ordinary   12  skeleton          fields type 12
              ordinary    1  minion            fields type 1
              ordinary   13  skeleton-warrior  fields type 13
            wave   2, 2 slots
              ordinary   12  skeleton          fields type 12
              ordinary    2  skeleton-scout    fields type 2
              ordinary    1  minion            fields type 1
            wave   3, 3 slots, an anchor
              ordinary    1  minion            fields type 1
              ordinary    7  necromancer       fields type 7
              ordinary    2  skeleton-scout    fields type 2
              changer     1  swift-column      fields type 2
              changer     3  split-push        fields type 2
              changer     4  long-column       fields type 1

            """.Replace("\r\n", "\n", StringComparison.Ordinal),
            listed.Output);
    }

    [Fact]
    public void The_name_column_is_measured_off_the_content_and_not_off_the_rows_printed()
    {
        // The width comes from the authored files rather than from the rows this
        // listing happens to print, and the two are separable: a changer pool
        // holding a twenty-character name widens wave one, whose rows are all
        // ordinary options off a roster nothing was planted in and none of whose
        // labels reach past sixteen.
        //
        // That is the property the verb needs, because which rows it prints
        // depends on --waves: a width taken off them would have one seed's wave
        // one come out one way here and another in a ten-wave listing, and a
        // menu copied out of either would stop lining up with the other.
        //
        // OBSERVED: drop the GameChangers loop from Offerings.LabelWidth, so the
        // column is measured off the roster alone. This goes red on the gap it
        // asks for, and the pinned listing above stays green -- because the
        // committed pool's longest name is shorter than skeleton-warrior, which
        // is exactly why the second file's half of the column needs its own
        // assertion.
        string scratch = TheCommandLine.Scratch("offerings-label-width");

        foreach (ContentFile file in RunContentFiles.All)
        {
            string planted = File.ReadAllText(RepoLayout.InContent(file))
                .Replace(PlantedOver, LongChanger, StringComparison.Ordinal);

            File.WriteAllText(Path.Combine(scratch, file.FileName), planted);
        }

        CommandLineResult listed = TheCommandLine.Invoke(
            "offerings", "--seed", "20260807", "--waves", "1", "--content", scratch)
            .Succeeded();

        Assert.DoesNotContain(LongChanger, listed.Output, StringComparison.Ordinal);

        Assert.Contains(
            "  ordinary   13  skeleton-warrior      fields type 13\n",
            listed.Output,
            StringComparison.Ordinal);
    }
}
