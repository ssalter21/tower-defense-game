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
    /// <summary>Two rounds, a filled slot and an empty one.</summary>
    private const string Two = """
        build   1   0 0   5 2
        build   2   1 3   5 1
        """;

    [Fact]
    public void A_row_becomes_the_decision_it_spells()
    {
        // The wave and the slots in the order they were written -- read back
        // off the value rather than off a re-parse, so nothing here can agree
        // with itself.
        //
        // OBSERVED: read the wave out of fields[1] instead of fields[0]. Both
        // rows parse as the wave their first slot's type id names, the wave
        // assertions go red, 0 against 1, and every slot still lands where it
        // should -- a script silently played against the wrong rounds.
        //
        // OBSERVED: swap the pair order in CommandScript's slot loop, so a pair
        // reads as `count type-id`. The slot assertions go red, (5, 2) against
        // (2, 5), and a script would send two of whatever id 5 names rather than
        // five of it.
        IReadOnlyList<RecordCommand> commands = CommandScript.Parse(Two);

        Assert.Equal(2, commands.Count);

        Assert.Equal(1, commands[0].Wave);
        Assert.Equal(new[] { WaveSlot.Empty, WaveSlot.Of(5, 2) }, commands[0].Slots);

        Assert.Equal(2, commands[1].Wave);
        Assert.Equal(new[] { WaveSlot.Of(1, 3), WaveSlot.Of(5, 1) }, commands[1].Slots);
    }

    [Fact]
    public void Comments_blank_lines_and_spacing_are_not_decisions()
    {
        // The same freedom units.txt, ruleset.txt and upgrades.txt have. A file
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
            + "build      1      0 0      5 2\n"
            + "\n"
            + "\t# and the second round\n"
            + "build 2 1 3 5 1\n");

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
            () => CommandScript.Parse("build 1 5 2\norder 0 1 4 0\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("starts with 'order'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains(
            "the rows this file has: build, place, upgrade",
            thrown.Message,
            StringComparison.Ordinal);
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
    [InlineData("build 1 0 2", "type id 0")]
    [InlineData("build 1 5 0", "0 of type id 5")]
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
            () => CommandScript.Parse("build 1 5 2\nbuild 2 5 2 1 1\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("Filled slots ascend strictly by type id", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_action_row_becomes_an_action_on_its_waves_command()
    {
        // Two keywords, one shape: the wave, the type, then the cell as a
        // column and a row, which is how content/map.txt and content/defense.txt
        // spell one. Read off the value rather than off a re-parse.
        //
        // OBSERVED: read the column out of fields[4] and the row out of
        // fields[3]. Both rows parse, both actions land on the command, and the
        // comparisons go red -- column 0, row 9 against column 9, row 0, which
        // is a tower somewhere nobody put one.
        //
        // OBSERVED: reverse CommandScript's ActionWords. The kind assertions go
        // red both ways round, Upgrade against Place, which is what pins the
        // words to the kinds -- the list's position IS the ActionKind.
        IReadOnlyList<RecordCommand> commands = CommandScript.Parse(
            "build 1 5 2\nplace 1 3 9 0\nupgrade 1 4 9 0\n");

        RecordCommand command = Assert.Single(commands);

        Assert.Equal(2, command.Actions.Count);
        Assert.Equal(BuildAction.Of(ActionKind.Place, 3, 9, 0), command.Actions[0]);
        Assert.Equal(BuildAction.Of(ActionKind.Upgrade, 4, 9, 0), command.Actions[1]);
        Assert.Equal(new[] { WaveSlot.Of(5, 2) }, command.Slots);
    }

    [Fact]
    public void The_same_two_actions_in_the_other_order_are_a_different_parse()
    {
        // Actions have no canonical order and must not get one. A phase may
        // upgrade what it just placed and the placement ordinals fall out of the
        // sequence, so these two scripts are two runs rather than two spellings
        // of one -- which is the opposite of the slots beside them, where an
        // order is asserted precisely so that two spellings cannot exist.
        //
        // OBSERVED: sort the actions by type id as they are appended. Both
        // scripts parse to the same command, the inequality goes red, and the
        // second placement silently becomes the first.
        IReadOnlyList<RecordCommand> written = CommandScript.Parse(
            "build 1 5 2\nplace 1 3 9 0\nplace 1 4 3 2\n");

        IReadOnlyList<RecordCommand> reversed = CommandScript.Parse(
            "build 1 5 2\nplace 1 4 3 2\nplace 1 3 9 0\n");

        Assert.NotEqual(written[0], reversed[0]);

        Assert.Equal(BuildAction.Of(ActionKind.Place, 3, 9, 0), written[0].Actions[0]);
        Assert.Equal(BuildAction.Of(ActionKind.Place, 4, 3, 2), reversed[0].Actions[0]);
    }

    [Theory]
    [InlineData("place 1 3 9")]
    [InlineData("place 1 3 9 0 0")]
    [InlineData("upgrade 1 3 9")]
    public void An_action_row_of_another_length_is_refused(string action)
    {
        // An action row has fixed arity, unlike a build row's four plus two per
        // slot: it names one cell and one type, so there is nothing on it that
        // repeats. A row of another length is a coordinate missing or a field
        // nobody reads.
        //
        // OBSERVED: check the action row against the build row's arity instead
        // -- at least four, then pairs. The four-field rows pass it and then
        // reach for a row that is not there, so the refusal is an
        // IndexOutOfRangeException with no line in it; the six-field row passes
        // as "four plus a pair", its trailing two fields are read by nothing,
        // and no exception is thrown at all.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 5 2\n" + action + "\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("5 fields, always", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("place 0 3 9 0", "the wave")]
    [InlineData("place 65536 3 9 0", "the wave")]
    [InlineData("place 1 0 9 0", "the type id")]
    [InlineData("place 1 65536 9 0", "the type id")]
    [InlineData("place 1 3 32768 0", "the column")]
    [InlineData("place 1 3 9 -32769", "the row")]
    public void A_number_an_action_row_could_not_store_is_refused_by_name(string action, string named)
    {
        // The third and last of the parser's refusals. The wave and the type id
        // are u16 and the cell is a pair of i16, which is what a record stores
        // them as -- a row that could be authored and not written down would be
        // a file its own writer refuses.
        //
        // OBSERVED: widen the cell to the u16 range. Column 32768 rides through
        // the parser and is caught by BuildAction.Of instead, so the case goes
        // red having caught a SimulationException -- the right refusal with the
        // line a person editing the file needed stripped off it.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 5 2\n" + action + "\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains(named + " is", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Nothing_an_action_row_says_is_checked_against_a_roster()
    {
        // Parse takes text and nothing else. What a type id names, whether that
        // row is a tower, whether the run unlocked it, whether the cell is on
        // the map, whether anything stands there and whether the round can
        // afford it all need the roster, the map, the board or the purse, and a
        // parser that held one would be a second implementation of a rule that
        // has to be applied when the stream is played anyway.
        //
        // OBSERVED: refuse a type id above the committed roster's largest. This
        // goes red on an exception, and a script naming a creep type or a cell
        // off the map is refused twice with two sentences instead of once.
        RecordCommand command = Assert.Single(
            CommandScript.Parse("build 1 5 2\nplace 1 65535 -1 -1\n"));

        Assert.Equal(BuildAction.Of(ActionKind.Place, 65535, -1, -1), Assert.Single(command.Actions));
    }

    [Theory]
    [InlineData("build 2 5 2\nbuild 1 5 2\n", 2, "decides wave 1")]
    [InlineData("build 1 5 2\nbuild 1 5 2\n", 2, "decides wave 1")]
    [InlineData("build 1 5 2\nbuild 2 5 2\nplace 1 3 9 0\n", 3, "acts on wave 1")]
    public void Rows_ascend_by_wave_across_the_whole_file(string script, int line, string named)
    {
        // The first of the three rules about the file's own shape. A run plays
        // its rounds in the order they are written, so a row that goes backwards
        // is a round decided twice or an action for a round the file has already
        // left. CommandStream asserts the same over the bytes; only a file has a
        // line to name, and a person editing one needs to be told which row.
        //
        // OBSERVED: drop the ascent check the build rows are held to. The first
        // two scripts parse clean and both cases go red having caught nothing;
        // what refuses them afterwards is CommandStream.Of, with a
        // SimulationException naming no line at all.
        //
        // OBSERVED: let an action row attach to whatever phase is open whatever
        // wave it names. The third case goes red having caught nothing, and
        // wave 1's placement is hung on wave 2's phase -- made and paid for a
        // round late, with nothing downstream in a position to notice.
        ContentException thrown = Assert.Throws<ContentException>(() => CommandScript.Parse(script));

        Assert.Equal(line, thrown.Line);
        Assert.Contains(named, thrown.Message, StringComparison.Ordinal);
        Assert.Contains("ascend by wave across the whole file", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_waves_action_rows_follow_its_build_row()
    {
        // The second rule. A round's take is decided before what the rest of its
        // gold is spent on, so an action written above its own build row would
        // be paid for by the round before it.
        //
        // OBSERVED: attach an action to the last command whatever wave it names.
        // The row parses onto wave 1's phase, this goes red having caught
        // nothing, and wave 2's placement is made and paid for a round early.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("build 1 5 2\nplace 2 3 9 0\nbuild 2 5 2\n"));

        Assert.Equal(2, thrown.Line);
        Assert.Contains("acts on wave 2 where the build row above it decided wave 1",
            thrown.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void An_action_row_for_a_wave_with_no_build_row_is_refused()
    {
        // The third rule. An action is paid for out of its round's purse and
        // applied in its round's order, so one belonging to no build phase
        // belongs to no round either.
        //
        // OBSERVED: skip the check when no command is open. The parse walks off
        // the end of an empty list with an ArgumentOutOfRangeException, which
        // says nothing about a file and nothing about a line.
        ContentException thrown = Assert.Throws<ContentException>(
            () => CommandScript.Parse("place 1 3 9 0\nbuild 1 5 2\n"));

        Assert.Equal(1, thrown.Line);
        Assert.Contains("no build row stands above it", thrown.Message, StringComparison.Ordinal);
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
