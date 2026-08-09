namespace Sim.Tests;

/// <summary>
/// The authoring form of a command stream: rows of text that become the
/// <c>(wave index, decision)</c> pairs a run consumes.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is the reading of rows.</b> Every rule about a
/// decision -- what a take may name, how slots order, what an empty one is --
/// belongs to <see cref="RecordCommand"/> and <see cref="BuildPhase"/> and is
/// tested there. What is only true here is that a row becomes the decision it
/// says, that a row nobody can read is refused with the line it is on, and that
/// the text form and the byte form describe the same run.
/// </para>
/// <para>
/// <b>Every refusal is asserted by name.</b> A suite that only asserted "it
/// threw" passes just as well when a script is refused for the wrong reason,
/// which for a file somebody is editing is the difference between being told
/// what to fix and being told to look.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class CommandScriptTests
{
    /// <summary>Two rounds, both halves of a menu, a filled slot and an empty one.</summary>
    private const string Two = """
        build   1  ordinary   5   0 0   5 2
        build   2  changer    4   1 3   5 1
        """;

    [Fact]
    public void A_row_becomes_the_decision_it_spells()
    {
        // The wave, the half of the menu, the id, and the slots in the order
        // they were written -- read back off the value rather than off a
        // re-parse, so nothing here can agree with itself.
        //
        // OBSERVED: read the take id out of fields[1] instead of fields[3]. The
        // first row parses as wave 1 taking ordinary option 1, the TakeId
        // assertion goes red, 5 against 1, and every slot still lands where it
        // should -- a script silently playing a different menu.
        //
        // OBSERVED: reverse CommandScript's TakeKinds. The kind assertion goes
        // red, Ordinary against GameChanger, which is what pins the words to the
        // halves -- the list's position IS the OptionKind, and a listing printed
        // off the same list would agree with a parser that had them backwards.
        IReadOnlyList<RecordCommand> commands = CommandScript.Parse(Two);

        Assert.Equal(2, commands.Count);

        Assert.Equal(1, commands[0].Wave);
        Assert.Equal(OptionKind.Ordinary, commands[0].Take);
        Assert.Equal(5, commands[0].TakeId);
        Assert.Equal(new[] { WaveSlot.Empty, WaveSlot.Of(5, 2) }, commands[0].Slots);

        Assert.Equal(2, commands[1].Wave);
        Assert.Equal(OptionKind.GameChanger, commands[1].Take);
        Assert.Equal(4, commands[1].TakeId);
        Assert.Equal(new[] { WaveSlot.Of(1, 3), WaveSlot.Of(5, 1) }, commands[1].Slots);
    }

    [Fact]
    public void Comments_blank_lines_and_spacing_are_not_decisions()
    {
        // The same freedom units.txt, ruleset.txt and schedule.txt have. A file
        // somebody cannot annotate is a file whose reasons live somewhere that
        // does not travel with it.
        //
        // OBSERVED: take the continue out of DataText.Rows, so a blank line and
        // a comment are yielded as rows. The comment line is refused as a row
        // starting with '#', so this goes red on an exception rather than a
        // comparison, and content/commands.txt stops loading at all.
        IReadOnlyList<RecordCommand> annotated = CommandScript.Parse(
            "# what this run is doing\n"
            + "\n"
            + "build      1     ordinary      5      0 0      5 2\n"
            + "\n"
            + "\t# and the anchor\n"
            + "build 2 changer 4 1 3 5 1\n");

        IReadOnlyList<RecordCommand> plain = CommandScript.Parse(Two);

        Assert.Equal(plain.Count, annotated.Count);

        for (int index = 0; index < plain.Count; index++)
        {
            Assert.Equal(plain[index], annotated[index]);
        }
    }

    [Fact]
    public void A_row_that_is_not_a_build_is_refused_by_name()
    {
        // The rule every parser in this repository applies to a keyword it does
        // not know: refuse, rather than skip. A skipped row is a round the run
        // never decides, which is a shorter run that still produces an outcome.
        //
        // OBSERVED: replace the keyword comparison with a `continue`. The
        // 'order' row is skipped, the script parses to one command, and this
        // goes red having caught nothing -- a file half of which was ignored.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 ordinary 5 5 2\norder 0 1 4 0\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("starts with 'order'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("the rows this file has: build", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_slot_with_half_of_it_missing_is_refused()
    {
        // A slot is a type id and a count, and a row with an odd number of
        // trailing fields is missing one of them. Guessing which -- a count of
        // one, a type of nothing -- is how a wave nobody composed gets sent.
        //
        // OBSERVED: drop the parity clause and keep only the length one. The
        // row parses, the trailing 5 is silently dropped as a partial pair, and
        // this goes red having caught nothing at all.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 ordinary 5 5\n"));

        Assert.Equal(1, thrown.Line);
        Assert.Contains("plus two per slot", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("build 1 ordinary 5 0 2", "type id 0")]
    [InlineData("build 1 ordinary 5 5 0", "0 of type id 5")]
    public void A_slot_that_is_neither_filled_nor_empty_is_refused(string row, string named)
    {
        // An empty slot is spelled 0 0 and nothing else. A count against no
        // creep and a creep sent none times would each be a second spelling of
        // one wave, and two spellings is two sets of bytes for one run.
        //
        // OBSERVED: make the empty test an `or` instead of an `and` -- treat any
        // zero as empty. Both rows parse to an empty slot, both assertions go
        // red having caught nothing, and a wave loses a creep the file asked
        // for without anything saying so.
        ContentException thrown = Assert.Throws<ContentException>(() => CommandScript.Parse(row));

        Assert.Equal(1, thrown.Line);
        Assert.Contains(named, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_refusal_from_the_record_carries_the_line_it_happened_on()
    {
        // The rule is the record's -- filled slots ascend strictly by type id --
        // and it is not restated here. What is added is where: a person editing
        // a file needs the line, and a SimulationException escaping a parser
        // would name the slot and leave them searching for it.
        //
        // OBSERVED: let the SimulationException through -- delete the catch.
        // Assert.Throws<ContentException> goes red having caught a
        // SimulationException instead, whose message is word for word the same
        // and says nothing about line 2. It takes both rows of the
        // slot-spelling theory above with it, for the same reason.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 ordinary 5 5 2\nbuild 2 ordinary 5 5 2 1 1\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("Filled slots ascend strictly by type id", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_script_that_decides_nothing_is_refused()
    {
        // A run consumes build phases. A file of nothing but comments is one
        // somebody did not finish, and reading it as a run of zero rounds would
        // produce an outcome nobody played for.
        //
        // OBSERVED: return the empty list instead of throwing. CommandStream.Of
        // refuses it a moment later with its own sentence, so the command line
        // still exits 1 -- and this goes red, because a parser that hands back
        // an empty script has said the file was fine.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("# nothing but a note\n"));

        Assert.Equal(0, thrown.Line);
        Assert.Contains("decides nothing at all", thrown.Message, StringComparison.Ordinal);
    }
}
