namespace Sim.Tests;

/// <summary>
/// The negative suite. One test per loud failure: take a good fixture, flip one
/// byte, assert the specific error.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the least optional group in the record format, which is
/// counterintuitive.</b> Every decision in this format chose loud failure over
/// silent wrongness -- three identity fields, three load assertions, two gates,
/// a self-cross-checking bundle. An untested loud-failure path is a green build
/// that is not checking anything, and it converts an unenforced rule into a
/// believed one, which is strictly worse than having no rule: nobody audits a
/// check they think is running.
/// </para>
/// <para>
/// Each test asserts the <i>specific</i> message rather than merely that
/// something was thrown. A test that accepts any exception would pass on a
/// null reference from a reader that fell over before it reached its check, and
/// would go on passing after the check was deleted.
/// </para>
/// </remarks>
public class RecordNegativeTests
{
    [Fact]
    public void Corrupt_magic_refuses()
    {
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.With(TheMatch.Ghost(types).ToBytes(), 0, (byte)'X');

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("'XHST'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("'GHST'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_handed_to_the_defense_reader_refuses_and_says_what_it_actually_is()
    {
        // Four bytes of magic buy an unambiguous answer to the commonest mistake
        // anyone will ever make with this format.
        UnitTypeTable types = TheMatch.Types();

        RecordException thrown = Assert.Throws<RecordException>(
            () => GhostRecord.FromBytes(TheMatch.WaveOf(types).ToBytes()));

        Assert.Contains("Those are the bytes of a wave record", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_format_version_newer_than_the_reader_knows_refuses()
    {
        // It cannot know what it is missing. Reading the fields it recognises
        // would mean reading them at offsets that have moved.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.Ghost(types).ToBytes(),
            RecordBytes.FormatVersionOffset,
            RecordFormat.GhostVersion + 1);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains(
            "newer than the "
            + RecordFormat.GhostVersion.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " this reader knows",
            thrown.Message,
            StringComparison.Ordinal);
        Assert.Contains("cannot know what it is missing", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_at_a_newer_format_version_refuses_independently_of_the_defense()
    {
        // Format versions are counted per kind, so the wave's gate is the wave's
        // own. A defense at version 0 beside it reads perfectly well.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.WaveOf(types).ToBytes(),
            RecordBytes.FormatVersionOffset,
            RecordFormat.WaveVersion + 1);

        RecordException thrown = Assert.Throws<RecordException>(() => WaveRecord.FromBytes(bytes));

        Assert.Contains("wave record format version 1", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(6, GhostRecord.FromBytes(TheMatch.Ghost(types).ToBytes()).Count);
    }

    [Fact]
    public void A_truncation_in_the_middle_of_an_array_refuses_and_names_the_element()
    {
        // Four bytes short: the last tower's q was read and its r is not there.
        // A reader that shrugged and took zero would be inventing a tower.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.Truncated(TheMatch.Ghost(types).ToBytes(), 4);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("ran out of bytes", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("tower 6 of 6", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bytes_left_over_after_a_record_refuses()
    {
        // Trailing bytes mean the reader and the writer disagree about the
        // layout, which is exactly what the format version exists to prevent.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = TheMatch.Ghost(types).ToBytes();

        RecordException thrown = Assert.Throws<RecordException>(
            () => GhostRecord.FromBytes([.. bytes, (byte)0]));

        Assert.Contains("left over", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Towers_out_of_canonical_order_refuse()
    {
        // Two towers exchanged. Sorting them here would have made this file
        // legal and left two identical defenses with two different sets of
        // bytes, which is the moment content-addressing stops meaning anything.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.Swap(
            TheMatch.Ghost(types).ToBytes(),
            RecordBytes.GhostTowersOffset,
            RecordBytes.GhostTowersOffset + RecordFormat.TowerBytes,
            RecordFormat.TowerBytes);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("out of canonical order", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("asserted rather than sorted", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_towers_on_one_cell_refuse()
    {
        // The same assertion, from the other side: equal coordinates are not
        // ascending, and a record could not tell the two towers apart anyway.
        UnitTypeTable types = TheMatch.Types();
        byte[] good = TheMatch.Ghost(types).ToBytes();
        byte[] bytes = RecordBytes.Splice(
            good,
            RecordBytes.GhostTowersOffset + RecordFormat.TowerBytes,
            good[RecordBytes.GhostTowersOffset..(RecordBytes.GhostTowersOffset + RecordFormat.TowerBytes)]);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("out of canonical order", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_duplicate_order_key_refuses()
    {
        // Orders three and four are both at tick 2100; making them the same
        // type makes them one order written down twice, and two waves sending
        // identical units would have two different sets of bytes.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.WaveOf(types).ToBytes(),
            RecordBytes.WaveOrdersOffset + (3 * RecordFormat.OrderBytes) + 4,
            1);

        RecordException thrown = Assert.Throws<RecordException>(() => WaveRecord.FromBytes(bytes));

        Assert.Contains("repeats the order key (tick 2100, type 1)", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Orders_out_of_canonical_order_refuse()
    {
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.Swap(
            TheMatch.WaveOf(types).ToBytes(),
            RecordBytes.WaveOrdersOffset,
            RecordBytes.WaveOrdersOffset + RecordFormat.OrderBytes,
            RecordFormat.OrderBytes);

        RecordException thrown = Assert.Throws<RecordException>(() => WaveRecord.FromBytes(bytes));

        Assert.Contains("out of canonical order", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_branching_corridor_in_the_inlined_map_refuses()
    {
        // One ground cell turned into corridor, three rows down the middle of
        // the map. It is the same corridor assertion the text parser runs, on
        // the same code, so a replay cannot carry geometry that a map file could
        // not -- and the pathfinder this simulation is never going to have is
        // still not needed.
        ReplayBundle good = TheMatch.Bundle();
        int cell = (2 * good.Map.Width) + 3;
        byte[] bytes = RecordBytes.With(
            good.ToBytes(),
            RecordBytes.BundleCellsOffset + cell,
            (byte)MapCell.Route);

        ContentException thrown = Assert.Throws<ContentException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("branches", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_cell_byte_that_is_not_a_cell_refuses()
    {
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.With(good.ToBytes(), RecordBytes.BundleCellsOffset, 7);

        ContentException thrown = Assert.Throws<ContentException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("A cell byte is 0 for ground", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bundle_whose_header_disagrees_with_an_inner_record_refuses()
    {
        // One byte of the wave's content hash, inside the bundle. Three copies
        // of eighteen bytes exist precisely so that this is catchable, and it is
        // a hard read error rather than a replay refusal: this is not a record
        // from an older ruleset, it is a record assembled from two of them.
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.Flip(
            good.ToBytes(),
            RecordBytes.WaveIn(good) + RecordBytes.ContentHashOffset);

        RecordException thrown = Assert.Throws<RecordException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("the wave inside it is stamped", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bundle_whose_header_disagrees_with_the_defense_inside_it_refuses()
    {
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.WithU32(
            good.ToBytes(),
            RecordBytes.GhostIn(good) + RecordBytes.SimVersionOffset,
            SimulationVersion.Current + 1);

        RecordException thrown = Assert.Throws<RecordException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("the defense inside it is stamped", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_from_one_ruleset_stapled_to_a_defense_from_another_refuses()
    {
        // Not a flipped byte this time: a genuinely valid wave record, written
        // against a retuned ruleset, spliced over the wave in a bundle built
        // against the committed one. Both halves are real records and the pair
        // is unrunnable, which is the whole reason the header is carried three
        // times.
        ReplayBundle good = TheMatch.Bundle();
        byte[] elsewhere = TheMatch.WaveOf(TheMatch.RetunedTypes()).ToBytes();

        Assert.Equal(good.Wave.ToBytes().Length, elsewhere.Length);

        byte[] bytes = RecordBytes.Splice(good.ToBytes(), RecordBytes.WaveIn(good), elsewhere);

        RecordException thrown = Assert.Throws<RecordException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("stapled to a defense from another", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_defense_naming_a_type_the_table_does_not_define_refuses()
    {
        // Not a read error -- the bytes are a perfectly well-formed defense.
        // It refuses when it is resolved against a ruleset, and it refuses
        // rather than skipping the tower, because a replay that drops what it
        // cannot read produces a confidently wrong result that still validates.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.Ghost(types).ToBytes(),
            RecordBytes.GhostTowersOffset,
            999);

        GhostRecord ghost = GhostRecord.FromBytes(bytes);

        RecordException thrown = Assert.Throws<RecordException>(() => ghost.ToLayout(types));

        Assert.Contains(
            "a defense requiring a placed unit names type id 999",
            thrown.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_defense_made_of_creeps_refuses()
    {
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.Ghost(types).ToBytes(),
            RecordBytes.GhostTowersOffset,
            1);

        RecordException thrown = Assert.Throws<RecordException>(
            () => GhostRecord.FromBytes(bytes).ToLayout(types));

        Assert.Contains("which is a moving unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_made_of_towers_refuses()
    {
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.WaveOf(types).ToBytes(),
            RecordBytes.WaveOrdersOffset + 4,
            3);

        RecordException thrown = Assert.Throws<RecordException>(
            () => WaveRecord.FromBytes(bytes).ToScript(types));

        Assert.Contains("which is a placed unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_type_id_of_zero_refuses()
    {
        // Zero means no unit, so a record carrying one is a record with a hole
        // in it rather than a record naming an unknown type.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.Ghost(types).ToBytes(),
            RecordBytes.GhostTowersOffset,
            0);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("zero means no unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_defense_with_no_towers_refuses()
    {
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.Truncated(
            RecordBytes.WithU16(TheMatch.Ghost(types).ToBytes(), RecordBytes.GhostTowerCountOffset, 0),
            6 * RecordFormat.TowerBytes);

        RecordException thrown = Assert.Throws<RecordException>(() => GhostRecord.FromBytes(bytes));

        Assert.Contains("no towers in it at all", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_stream_handed_the_defense_reader_refuses_and_says_what_it_actually_is()
    {
        // The fourth magic tag, doing the same job the other three do: the
        // commonest mistake anybody will make with a folder of records is
        // opening one as another.
        //
        // OBSERVED: drop the Command branch from RecordFormat.TryKindOfMagic.
        // The first assertion goes red -- "defense record: begins with 'CMDS'
        // where ..." with nothing after it -- and the reader stops being able to
        // say what the bytes actually are, which is the whole of what four bytes
        // of magic buy.
        RecordException thrown = Assert.Throws<RecordException>(
            () => GhostRecord.FromBytes(TheCommands.Bytes()));

        Assert.Contains("Those are the bytes of a command stream", thrown.Message, StringComparison.Ordinal);

        // And the other way, which is the case a hexdump answers on its own.
        byte[] corrupt = RecordBytes.With(TheCommands.Bytes(), 0, (byte)'X');
        RecordException magic = Assert.Throws<RecordException>(() => CommandStream.FromBytes(corrupt));

        Assert.Contains("'XMDS'", magic.Message, StringComparison.Ordinal);
        Assert.Contains("'CMDS'", magic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_stream_at_a_newer_format_version_refuses_independently_of_the_other_kinds()
    {
        // Format versions are counted per kind, so the command stream's gate is
        // its own. A defense, a wave and a bundle beside it read perfectly well.
        //
        // OBSERVED: make IsKnown accept one version above the current one. The
        // "cannot know what it is missing" assertion goes red: the read gate
        // waves the record through and the reader's own switch refuses it
        // instead, with a message that says the fault is in this build rather
        // than in the bytes.
        //
        // The version is named off RecordFormat.CommandVersion rather than
        // written out, because what this asserts is that the gate sits one above
        // whatever the current version is. It was written when that was 1 and
        // has been true at 2 and at 3 without being edited for either.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.Bytes(),
            RecordBytes.FormatVersionOffset,
            RecordFormat.CommandVersion + 1);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains(
            "command stream format version "
            + (RecordFormat.CommandVersion + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
            thrown.Message,
            StringComparison.Ordinal);
        Assert.Contains("cannot know what it is missing", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(6, GhostRecord.FromBytes(TheMatch.Ghost(types).ToBytes()).Count);
        Assert.Equal(6, WaveRecord.FromBytes(TheMatch.WaveOf(types).ToBytes()).Count);
    }

    [Fact]
    public void A_build_phase_stored_for_wave_zero_refuses()
    {
        // Waves are counted from one, so zero is a round no run ever plays
        // rather than a round out of order -- and it is named as such.
        //
        // OBSERVED: drop the wave-zero check from ReadVersion0. The stream is
        // still refused, by the canonical-order check below it, and the second
        // half of the assertion is the clause that tells the two apart: without
        // it this test stays green on "is stored for wave 0, at or below the 0
        // above it", which is a first command being compared against a command
        // above it that does not exist.
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.Bytes(),
            RecordBytes.CommandAt(0) + RecordBytes.CommandWaveOffset,
            0);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains(
            "is stored for wave 0, and waves are counted from one",
            thrown.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Build_phases_out_of_canonical_order_refuse()
    {
        // A run plays its build phases in the order they are stored, so the
        // order is asserted rather than sorted -- for the reason a wave's orders
        // are: sorting would leave two identical runs with two different sets of
        // bytes, at which point content-addressing one stops meaning anything.
        //
        // OBSERVED: drop the wave-order check from ReadVersion0. This goes red
        // having caught nothing -- no exception was thrown -- and a stream
        // holding waves 1, 2, 1, 4 reads back as a stream, to be refused much
        // later by a run that is about to play round three.
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.Bytes(),
            RecordBytes.CommandAt(2) + RecordBytes.CommandWaveOffset,
            1);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("at or below the 2 above it", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("ascend strictly by wave", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_action_of_a_kind_no_board_has_a_branch_for_refuses()
    {
        // One byte, and it is the byte that says which of the two things an
        // action does. A place onto a taken cell and an upgrade of an empty one
        // are each the other's mistake, so a third kind is an instruction
        // nothing could carry out.
        //
        // OBSERVED: drop the rewrap from ReadActions -- let BuildAction.Of's
        // refusal out as it is. This goes red on the exception type,
        // SimulationException against RecordException, so damaged bytes are
        // reported as a fault in this program rather than in the record. The
        // sentence naming the kind survives either way; what is lost is which
        // action of which build phase it was, which on a ten-round record is
        // the whole of what a person needs.
        byte[] bytes = RecordBytes.With(
            TheCommands.ActingBytes(),
            RecordBytes.CommandsOffset + RecordBytes.CommandActionsOffset,
            2);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("action 1 of build phase 1 of 4 cannot be read", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("is of kind 2", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_action_naming_no_type_at_all_refuses()
    {
        // Rows of the unit table are identified from one, so zero is not a type
        // this build has never heard of -- it is the subject of the instruction
        // missing.
        //
        // The second rule the rewrap carries, and it is here as well as above
        // because a rewrap that caught one refusal and not the other would look
        // exactly the same from the outside.
        //
        // OBSERVED: drop the rewrap from ReadActions. This goes red on the
        // exception type for the reason the kind above does.
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.ActingBytes(),
            RecordBytes.CommandsOffset + RecordBytes.CommandActionsOffset + 1,
            0);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("action 1 of build phase 1 of 4 cannot be read", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("names type id 0", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncation_in_the_middle_of_an_action_run_refuses_and_names_the_action()
    {
        // A stream that says it built two things and carries one and a half.
        // Reading it as far as it goes would be a run whose second placement
        // stood on whatever the following bytes happened to spell -- here, the
        // slot count of the wave behind it.
        //
        // The cut takes the slots, the slot count and the last two bytes of the
        // second action, so what runs out is that action's row.
        //
        // OBSERVED: have ReadActions stop at the bytes it has -- return early
        // when the cursor has fewer than ActionBytes left. The record is still
        // refused and it still says it ran out of bytes, so a case that stopped
        // at that clause would stay green; what goes red is the element,
        // "the count of slot 1 of build phase 1 of 1". The reader took the
        // second action's kind and type id for a slot count of 1025 and went
        // looking for slots inside an action, which is what reading short does
        // to everything behind it.
        Run run = TheCommands.Fresh();

        // One command, so the action run is the last thing in the record that
        // can be cut into.
        byte[] bytes = CommandStream.Of(run, new[] { TheCommands.Acting(run)[0] }).ToBytes();

        RecordException thrown = Assert.Throws<RecordException>(
            () => CommandStream.FromBytes(RecordBytes.Truncated(bytes, RecordFormat.SlotBytes + 2 + 2)));

        Assert.Contains("ran out of bytes", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("the row of action 2 of build phase 1 of 1", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Two_slots_of_one_build_phase_naming_one_creep_refuse()
    {
        // The same rule one level down: a creep fills at most one slot of a
        // wave, checked over the bytes as well as where a command is made.
        //
        // The reader stopped asserting that the slots ascend in #191 -- a slot's
        // position is its release order now, so an arrangement is a decision --
        // and this is the half of the old rule that stayed. It has to: two slots
        // on one creep would compose a wave that spends one decision twice.
        //
        // OBSERVED: drop the repeat check from ReadSlots. This goes red on the
        // exception type, SimulationException against RecordException, and the
        // message is RecordCommand.Of's -- the writer-side half of the same
        // rule, catching bytes the reader waved through.
        Run run = TheCommands.Fresh();
        int[] creeps = run.Types.Types
            .Where(type => type.Role == UnitRole.Moving)
            .Select(type => type.Id)
            .OrderBy(id => id)
            .ToArray();

        byte[] good = CommandStream.Of(
            run,
            new[] { RecordCommand.Of(1, WaveSlot.Of(creeps[0], 1), WaveSlot.Of(creeps[1], 1)) })
            .ToBytes();

        // The second slot dragged down onto the first slot's creep. One u16,
        // and the whole wave becomes a slot spent twice on one thing.
        byte[] bytes = RecordBytes.WithU16(
            good,
            RecordBytes.CommandsOffset + RecordBytes.CommandSlotsOffset + RecordFormat.SlotBytes,
            creeps[0]);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("which a slot above it already sent", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("A creep fills at most one slot of a wave", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_slot_that_sends_none_of_a_creep_refuses()
    {
        // Leaving a slot empty already has exactly one spelling, (0, 0). A
        // creep named zero times would be a second one, and two spellings of one
        // wave is two sets of bytes for one run.
        //
        // OBSERVED: drop the zero-count check from ReadSlots. This goes red on
        // the exception type, SimulationException against RecordException: what
        // fires is WaveSlot.Of's own guard, which is a fault in this program
        // rather than a report about damaged bytes.
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.Bytes(),
            RecordBytes.CommandAt(0) + RecordBytes.CommandSlotsOffset + 2,
            0);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("sends none of type id", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_slot_counting_creeps_of_no_type_refuses()
    {
        // The other half of the same spelling: a count against type id 0 is a
        // slot with a hole in it rather than an empty one.
        //
        // OBSERVED: drop the zero-type check from ReadSlots. The message
        // assertion goes red: the slot is refused by the canonical-order check
        // instead, for sending a type id at or below the zero above it, which
        // says nothing about the hole that is actually in the record.
        byte[] bytes = RecordBytes.WithU16(
            TheCommands.Bytes(),
            RecordBytes.CommandAt(0) + RecordBytes.CommandSlotsOffset,
            0);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("of type id 0, and zero means no unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncation_in_the_middle_of_a_build_phase_refuses_and_names_the_element()
    {
        // Two bytes short: the last slot's type id was read and its count is not
        // there. A reader that shrugged and took zero would be inventing an
        // empty slot in a wave somebody paid for.
        //
        // OBSERVED: name the slot "a slot" in ReadSlots instead of building the
        // name out of its index and its build phase. The element assertion goes
        // red, and a truncated stream reports that it ran out of bytes without
        // saying where -- which on a four-round record is four places to look.
        byte[] bytes = RecordBytes.Truncated(TheCommands.Bytes(), 2);

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("ran out of bytes", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("slot 1 of build phase 4 of 4", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bytes_left_over_after_a_command_stream_refuses()
    {
        // Trailing bytes mean the reader and the writer disagree about the
        // layout, which is what the format version exists to prevent.
        //
        // OBSERVED: drop the ExpectEnd call from CommandStream.FromBytes. This
        // goes red having caught nothing -- no exception was thrown -- and a
        // stream with a byte glued to the end of it reads as a stream.
        RecordException thrown = Assert.Throws<RecordException>(
            () => CommandStream.FromBytes([.. TheCommands.Bytes(), (byte)0]));

        Assert.Contains("left over", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_stream_that_decides_nothing_refuses()
    {
        // A stream is what a run consumes, and a run of no build phases is a run
        // nobody played.
        //
        // OBSERVED: drop the zero-count check from ReadVersion0. This goes red
        // having caught nothing -- no exception was thrown -- and a header with
        // a count of zero reads back as a valid record of nothing.
        byte[] bytes = RecordBytes.Truncated(
            RecordBytes.WithU16(TheCommands.Bytes(), RecordBytes.CommandCountOffset, 0),
            TheCommands.Waves * (RecordFormat.CommandBytes + RecordFormat.SlotBytes));

        RecordException thrown = Assert.Throws<RecordException>(() => CommandStream.FromBytes(bytes));

        Assert.Contains("decides nothing at all", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void One_byte_of_a_command_streams_ruleset_hash_retires_it()
    {
        // Not a read error -- the bytes are a perfectly well-formed stream, and
        // it is still readable afterwards. This is the gate the ruleset's hash
        // exists to make possible, reached by damaging the stamp rather than by
        // retuning the file.
        //
        // OBSERVED: drop the ruleset comparison from CommandStream.Replay. This
        // goes red having caught nothing -- no exception was thrown -- and a
        // stream whose stamp says one ruleset plays four rounds against
        // another.
        byte[] bytes = RecordBytes.Flip(TheCommands.Bytes(), RecordBytes.CommandRulesetHashOffset);

        CommandStream stream = CommandStream.FromBytes(bytes);

        Assert.Equal(TheCommands.Waves, stream.Count);

        RetiredRecordException thrown = Assert.Throws<RetiredRecordException>(
            () => stream.Replay(TheCommands.Fresh()));

        Assert.Equal("ruleset hash", thrown.Gate);
    }

    [Fact]
    public void A_map_the_bundle_does_not_carry_enough_bytes_for_refuses()
    {
        // The width and the height are read before the cells are, so a record
        // claiming a grid bigger than the bytes it has is caught by the shape
        // rather than by an allocation.
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.WithU16(
            good.ToBytes(),
            RecordBytes.BundleMapWidthOffset,
            4000);

        RecordException thrown = Assert.Throws<RecordException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("bytes are left", thrown.Message, StringComparison.Ordinal);
    }
}
