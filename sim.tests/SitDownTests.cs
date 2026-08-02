using System.Text.RegularExpressions;

// Sim.Match and System.Text.RegularExpressions.Match are both in scope here,
// and this file needs both.
using RegexMatch = System.Text.RegularExpressions.Match;

namespace Sim.Tests;

/// <summary>
/// The sit-down checklist, held against the run it is written about.
/// </summary>
/// <remarks>
/// <para>
/// <b>Nothing here checks that the twelve things are true.</b> They are true or
/// not true on a screen, in front of a person, which is the entire point of the
/// artefact and the reason it is not a test suite. What is checkable without a
/// human is narrower and worth exactly as much as it claims: that every moment
/// the checklist sends somebody to is a moment of the match the build plays.
/// </para>
/// <para>
/// That is the failure the numbers exist to prevent, and it is silent. A
/// checklist that says "drag to tick 366" after a content change moved the first
/// overtake to 402 does not look broken — it reads perfectly, it sends somebody
/// to an unremarkable second of an unremarkable match, they see nothing wrong,
/// and the row passes. So the four bindings between the document and the run are
/// asserted here, in the engine-free gate, where a content change that moves a
/// landmark goes red on the same push that moved it.
/// </para>
/// </remarks>
public class SitDownTests
{
    /// <summary>A row of the twelve: a number, then two cells.</summary>
    private static readonly Regex NumberedRow = new(@"^\|\s*(\d+)\s*\|(.*)$", RegexOptions.Multiline);

    /// <summary>A row of the transcribed landmark table: a quoted name, then a tick.</summary>
    private static readonly Regex AnchorRow = new(@"^\|\s*`([a-z-]+)`\s*\|\s*tick (\d+)\s*\|", RegexOptions.Multiline);

    /// <summary>Every tick the document names, anywhere in it.</summary>
    private static readonly Regex AnyTick = new(@"\btick (\d+)\b");

    [Fact]
    public void The_checklist_has_the_twelve_rows_it_says_it_has()
    {
        int[] numbers = NumberedRow.Matches(Checklist())
            .Select(row => int.Parse(row.Groups[1].Value))
            .ToArray();

        Assert.Equal(Enumerable.Range(1, 12), numbers);
    }

    [Fact]
    public void The_transcribed_landmark_table_is_the_committed_one()
    {
        // A second copy of four numbers, pinned to the first. The document
        // needs them written out -- somebody reading it is not going to open a
        // generated file to find out which tick the overtake is on -- and this
        // is what stops the copy being the thing that goes stale.
        var transcribed = AnchorRow.Matches(Checklist())
            .ToDictionary(row => row.Groups[1].Value, row => int.Parse(row.Groups[2].Value));

        var committed = TheMatch.LandmarksOfTheCommittedRun().Rows
            .ToDictionary(landmark => landmark.Name, landmark => landmark.Tick);

        Assert.Equal(committed, transcribed);
    }

    [Fact]
    public void Every_tick_the_checklist_names_is_a_tick_the_match_has()
    {
        // The cheap, broad one: it catches a placeholder that was never filled
        // in, a typo with a digit too many, and a checklist left pointing past
        // the end of a match a content change made shorter.
        int finalTick = TheMatch.Fresh().Resolve().FinalTick;

        foreach (RegexMatch named in AnyTick.Matches(Checklist()))
        {
            int tick = int.Parse(named.Groups[1].Value);

            Assert.True(
                tick >= 0 && tick <= finalTick,
                $"docs/sit-down.md sends somebody to tick {tick}, and the match ends on {finalTick}.");
        }
    }

    [Theory]
    [InlineData(4, Landmarks.FirstLeak)]
    [InlineData(6, Landmarks.Orphaned)]
    [InlineData(9, Landmarks.LastCreepDies)]
    [InlineData(10, Landmarks.FirstOvertake)]
    public void The_row_written_about_a_landmark_names_that_landmarks_tick(int number, string landmark)
    {
        // The narrow, sharp one. Four of the twelve rows exist to put a person
        // in front of one specific moment, and a row that named a tick near it
        // rather than it would be the difference between watching a shell lose
        // its target and watching the corridor.
        //
        // Each of the four names a DIFFERENT landmark, which is the other thing
        // this pins. Row 4 and row 10 both being written against the overtake
        // would cost a row: the twelve are twelve because they are twelve
        // separate things to look at.
        int tick = TheMatch.LandmarksOfTheCommittedRun().Rows
            .Single(row => row.Name == landmark)
            .Tick;

        Assert.Contains(
            "tick " + tick.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Row(number));
    }

    [Fact]
    public void The_row_that_jumps_to_the_end_names_the_tick_the_match_ends_on()
    {
        int finalTick = TheMatch.Fresh().Resolve().FinalTick;

        Assert.Contains(
            "tick " + finalTick.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Row(7));
    }

    /// <summary>One numbered row of the twelve, by its number.</summary>
    private static string Row(int number)
    {
        foreach (RegexMatch row in NumberedRow.Matches(Checklist()))
        {
            if (int.Parse(row.Groups[1].Value) == number)
            {
                return row.Groups[2].Value;
            }
        }

        throw new InvalidOperationException($"docs/sit-down.md has no row {number}.");
    }

    private static string Checklist() => File.ReadAllText(RepoLayout.SitDownFile);
}
