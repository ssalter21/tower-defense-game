using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The words somebody types at the prompt, read one at a time.
/// </summary>
/// <remarks>
/// <para>
/// <b>What is under test is the reading of a line and nothing else.</b> No run
/// is built here and none is needed: whether the cell is on the map, whether
/// the menu carries that option and whether the purse can pay are
/// <see cref="BuildPhase.Resolve"/>'s, tested where those rules live. What is
/// only true here is that each word becomes the value it names, and that a line
/// nobody can read comes back as a sentence rather than as a throw.
/// </para>
/// <para>
/// <b>Every refusal is asserted whole.</b> A case that only asked "was it
/// refused" passes just as well when the sentence names the wrong operand,
/// which at a prompt is the difference between being told what to type and
/// being told to guess.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong parser</b>,
/// and the wrong parser is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class TypedLineTests
{
    /// <summary>The committed roster, which is the only table a line is read against.</summary>
    private static readonly UnitTypeTable Roster = TheMatch.Types();

    [Fact]
    public void A_place_and_an_upgrade_become_the_action_they_name()
    {
        // The cell is written column then row, which is the order a script row
        // writes one in and the order the map's legend prints one in.
        //
        // OBSERVED: swap the column and the row. Both blocks below go red on a
        // BuildAction whose two coordinates are the other way round -- and
        // 'place 4 4', the square case, would not have noticed.
        TypedLine placed = TypedLine.Read("place 3 6 2", Roster);

        Assert.True(placed.Understood);
        Assert.Equal(Typed.Act, placed.Word);
        Assert.Equal(BuildAction.Of(ActionKind.Place, 3, 6, 2), placed.Action);

        TypedLine upgraded = TypedLine.Read("upgrade 4 6 2", Roster);

        Assert.True(upgraded.Understood);
        Assert.Equal(Typed.Act, upgraded.Word);
        Assert.Equal(BuildAction.Of(ActionKind.Upgrade, 4, 6, 2), upgraded.Action);
    }

    [Fact]
    public void A_send_fills_a_slot_with_a_creep_and_a_count()
    {
        // OBSERVED: build the slot as WaveSlot.Of(count, typeId). Twenty of
        // type 12 becomes twelve of type 20, which is a type this roster does
        // not have -- and nothing here would say so, because what a type id
        // names is not this file's question.
        TypedLine sent = TypedLine.Read("send 12 20", Roster);

        Assert.True(sent.Understood);
        Assert.Equal(Typed.Send, sent.Word);
        Assert.Equal(WaveSlot.Of(12, 20), sent.Slot);
    }

    [Fact]
    public void The_six_words_that_carry_nothing_are_words_on_their_own()
    {
        // Six words, one line each. They are the whole of what the loop does
        // that is not a decision: three reprints, an undo, a commit and an
        // exit.
        //
        // OBSERVED: fall through from the 'menu' case to the 'costs' one.
        // Typing 'menu' reprints the price list, which is a panel that reads
        // perfectly and answers a question nobody asked.
        Assert.Equal(Typed.Undo, Word("undo"));
        Assert.Equal(Typed.Map, Word("map"));
        Assert.Equal(Typed.Menu, Word("menu"));
        Assert.Equal(Typed.Costs, Word("costs"));
        Assert.Equal(Typed.Done, Word("done"));
        Assert.Equal(Typed.Quit, Word("quit"));
    }

    [Fact]
    public void A_blank_line_is_not_a_mistake()
    {
        // Somebody presses return at a prompt, and the prompt comes back. A
        // refusal here would print a sentence every time a person paused.
        //
        // OBSERVED: refuse a line with no words on it. The transcript in §5
        // cannot carry a blank line between two rounds, so the file that is
        // easiest to read is the one that no longer plays.
        Assert.Equal(Typed.Nothing, Word(string.Empty));
        Assert.Equal(Typed.Nothing, Word("   "));
        Assert.Equal(Typed.Nothing, Word("\t "));
    }

    [Fact]
    public void Spacing_and_letter_case_do_not_change_what_a_line_means()
    {
        // A person types, and a transcript is edited by hand. Neither is a
        // content file with a hash over it, so neither has anything to gain
        // from an alignment being meaning.
        //
        // OBSERVED: match the opening word with StringComparison.Ordinal.
        // 'PLACE' refuses as a word nobody has, and the refusal helpfully lists
        // 'place' among the words there are.
        TypedLine spaced = TypedLine.Read("   PLACE   Archer  6   2  ", Roster);

        Assert.True(spaced.Understood);
        Assert.Equal(BuildAction.Of(ActionKind.Place, 3, 6, 2), spaced.Action);

        Assert.Equal(Typed.Done, Word("  DoNe  "));
        Assert.Equal(Typed.Send, TypedLine.Read("SeNd ChAnGeR 3", Roster).Word);
    }

    [Fact]
    public void A_label_may_be_typed_where_a_type_id_is_expected()
    {
        // The roster carries labels, so the panels beside the prompt print
        // them and typing one back is free. What is stored is the id either
        // way: a script is the record's spelling and not the player's.
        //
        // OBSERVED: resolve the label against the label column but return the
        // row's index instead of its id. 'place archer 6 2' becomes type 2,
        // which is the skeleton scout -- a creep, on a cell, silently.
        Assert.Equal(
            BuildAction.Of(ActionKind.Place, 3, 6, 2),
            TypedLine.Read("place archer 6 2", Roster).Action);

        Assert.Equal(
            WaveSlot.Of(13, 2),
            TypedLine.Read("send skeleton-warrior 2", Roster).Slot);
    }

    [Fact]
    public void A_number_is_an_id_even_where_the_roster_carries_it_as_a_label()
    {
        // The rule that keeps 'place 3 6 2' meaning one thing forever. A roster
        // is authored text and a label may be digits, so without this the
        // meaning of a typed id would depend on what somebody called something
        // else.
        //
        // OBSERVED: look the label up before trying the number. The planted
        // roster below makes 'place 3 6 2' stand a ranger, because the ranger
        // is what "3" is the name of -- and the committed roster reads the same
        // line as an archer.
        UnitTypeTable planted = UnitTypeTable.Parse(PlantedText.Replace(
            File.ReadAllText(RepoLayout.UnitsFile), "unit  14   ranger", "unit  14   3     "));

        Assert.Equal(
            BuildAction.Of(ActionKind.Place, 3, 6, 2),
            TypedLine.Read("place 3 6 2", planted).Action);
    }

    [Fact]
    public void A_word_nobody_has_is_refused_with_the_words_there_are()
    {
        // The refusal lists the vocabulary because a prompt has no usage block
        // above it -- the frame is a map and two panels, and the words are only
        // ever printed by this sentence and by the specification.
        //
        // OBSERVED: give a refusal Typed.Nothing for its word. A typo becomes
        // indistinguishable from a pressed return, so a loop reaching for the
        // word without asking Understood first silently does nothing and
        // prints nothing -- which is the one outcome a person cannot tell from
        // a frame that came back unchanged on purpose.
        Assert.Equal(
            "'plce archer 6 2' opens with 'plce', which is not a word here. The words are place, "
            + "upgrade, send, undo, map, menu, costs, done and quit.",
            Refusal("plce archer 6 2"));

        Assert.Equal(Typed.Refused, TypedLine.Read("plce archer 6 2", Roster).Word);
    }

    [Fact]
    public void A_word_carrying_the_wrong_number_of_operands_is_refused_by_shape()
    {
        // Three shapes of the same mistake: too few, too many, and something
        // after a word that takes nothing. Each names the operands the word
        // does take, which is the whole of what a person needs back.
        //
        // OBSERVED: require at least the operands rather than exactly them.
        // 'place archer 6 2 4' reads as a place at 6,2 and the 4 is dropped --
        // a typed coordinate that goes nowhere, on the one word where a
        // mistyped cell is the expensive mistake.
        Assert.Equal(
            "'place 6 2' carries 2 words after 'place', which takes 3: the type, the column and the row.",
            Refusal("place 6 2"));

        Assert.Equal(
            "'send 12 20 30' carries 3 words after 'send', which takes 2: the type and the count.",
            Refusal("send 12 20 30"));

        Assert.Equal(
            "'undo that' carries 1 word after 'undo', which takes none.",
            Refusal("undo that"));
    }

    [Fact]
    public void A_number_that_is_not_one_is_refused_by_the_operand_it_was_typed_for()
    {
        // The operand is named rather than the position, because "the column"
        // is a thing on the frame and "the second word" is not.
        //
        // OBSERVED: name every unreadable number "the number". Both sentences
        // below come out identical, and a line with two of them tells you one
        // of the two is wrong.
        Assert.Equal(
            "'place archer x 2' names the column 'x', which is not a number written in digits.",
            Refusal("place archer x 2"));

        Assert.Equal(
            "'send 12 lots' names the count 'lots', which is not a number written in digits.",
            Refusal("send 12 lots"));

        Assert.Equal(
            "'take ordinary one' names the id 'one', which is not a number written in digits.",
            Refusal("take ordinary one"));
    }

    [Fact]
    public void A_label_nothing_on_the_roster_carries_is_refused_by_name()
    {
        // OBSERVED: resolve an unknown label to type id 0. BuildAction.Of
        // refuses it, so the line is still refused -- with a sentence about a
        // row of the unit table, which says nothing at all about the word that
        // was actually typed.
        Assert.Equal(
            "'place wizard 6 2' names the type 'wizard', which is neither a number nor the label of "
            + "anything on the roster.",
            Refusal("place wizard 6 2"));
    }

    [Fact]
    public void A_label_two_rows_carry_is_refused_rather_than_resolved_to_the_first()
    {
        // Labels are for people and nothing in the simulation branches on one,
        // so units.txt has never had to keep them unique. A convenience that
        // quietly picked the first match would turn that into a rule nobody
        // wrote down, and would do it by building the wrong tower.
        //
        // OBSERVED: return on the first label that matches. The line reads as
        // an archer, and the roster that made it ambiguous is the one place
        // nothing would ever look.
        UnitTypeTable planted = UnitTypeTable.Parse(PlantedText.Replace(
            File.ReadAllText(RepoLayout.UnitsFile), "unit  14   ranger", "unit  14   archer"));

        Assert.Equal(
            "'place archer 6 2' names the type 'archer', which 2 rows of the roster answer to. A label "
            + "picks one row by name, so a label two rows carry can only be meant by naming the id.",
            TypedLine.Read("place archer 6 2", planted).Refusal);
    }

    [Fact]
    public void A_value_outside_what_a_record_stores_is_refused_in_the_records_own_words()
    {
        // The ranges are not restated here. An action is built by
        // BuildAction.Of and a slot by WaveSlot.Of, so the floor under a type
        // id and under a count is the one the stored bytes have, and the
        // sentence is the one a script author already gets.
        //
        // OBSERVED: build the value with a private constructor rather than
        // through Of. 'place 0 6 2' parses, and the phase carries an action
        // naming a type nothing defines until something much later asks what
        // type 0 is.
        Assert.Equal(
            "'place 0 6 2' cannot be read. A build action names type id 0. An action names one row of "
            + "the unit table, and every row is identified from one.",
            Refusal("place 0 6 2"));

        Assert.Equal(
            "'send 12 0' cannot be read. A wave slot was filled with 0 of type id 12. A filled slot "
            + "sends between 1 and 65535 creeps; a slot that sends none is spelled WaveSlot.Empty, so "
            + "that leaving one empty and naming a creep zero times cannot be two spellings of one wave.",
            Refusal("send 12 0"));
    }

    [Fact]
    public void Nothing_a_run_holds_is_consulted()
    {
        // Three lines that are read perfectly and that no round could play: a
        // cell off the far edge of the committed map, a wave costing more gold
        // than a run ever holds, and a creep named where a tower has to stand.
        // Each is refused by BuildPhase.Resolve against a board, a purse and a
        // roster's roles, and none of the three is a question about text.
        //
        // OBSERVED: require UnitRole.Placed for a place's type. The third line
        // refuses here instead, which reads identically at the prompt and moves
        // one rule out of the one place that holds the rest of them -- so
        // 'place minion 6 2' and 'place archer 99 99' start being refused by
        // two different files for two versions of the same reason.
        Assert.True(TypedLine.Read("place archer 99 99", Roster).Understood);
        Assert.True(TypedLine.Read("send skeleton 60000", Roster).Understood);
        Assert.True(TypedLine.Read("place minion 6 2", Roster).Understood);
    }

    [Fact]
    public void The_words_for_an_action_are_the_ones_a_command_script_writes()
    {
        // One vocabulary, asserted rather than described: the same two words
        // and the same three operands, read once at a prompt and once out of a
        // row, and coming to the same action. Rename either word in
        // CommandScript and both sides move together -- a literal here would
        // instead leave the prompt spelling a word no file parses.
        //
        // OBSERVED: spell the prompt's words as ActionKind.ToString(). The
        // block below goes red on 'place 3 6 2' opening with a word nobody has,
        // while every other case in this file passes, because they all agree
        // with each other about a spelling that no command script carries.
        IReadOnlyList<RecordCommand> script = CommandScript.Parse(
            """
            build    1  ordinary  1  0 0
            place    1  3  6 2
            upgrade  1  4  6 2
            """);

        Assert.Equal(
            script[0].Actions[0],
            TypedLine.Read(CommandScript.WordFor(ActionKind.Place) + " 3 6 2", Roster).Action);

        Assert.Equal(
            script[0].Actions[1],
            TypedLine.Read(CommandScript.WordFor(ActionKind.Upgrade) + " 4 6 2", Roster).Action);
    }

    /// <summary>The word a line came to, asserted to have been read at all.</summary>
    private static Typed Word(string line)
    {
        TypedLine typed = TypedLine.Read(line, Roster);

        Assert.Null(typed.Refusal);

        return typed.Word;
    }

    /// <summary>The sentence a line was refused with, asserted to be one.</summary>
    private static string Refusal(string line)
    {
        TypedLine typed = TypedLine.Read(line, Roster);

        Assert.False(typed.Understood, "'" + line + "' was read rather than refused.");

        return typed.Refusal!;
    }
}
