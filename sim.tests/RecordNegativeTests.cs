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

        Assert.Contains("newer than the 0 this reader knows", thrown.Message, StringComparison.Ordinal);
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
        // Orders three and four are both at tick 700; making them the same type
        // makes them one order written down twice, and two waves sending
        // identical units would have two different sets of bytes.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = RecordBytes.WithU16(
            TheMatch.WaveOf(types).ToBytes(),
            RecordBytes.WaveOrdersOffset + (3 * RecordFormat.OrderBytes) + 4,
            1);

        RecordException thrown = Assert.Throws<RecordException>(() => WaveRecord.FromBytes(bytes));

        Assert.Contains("repeats the order key (tick 700, type 1)", thrown.Message, StringComparison.Ordinal);
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

        Assert.Contains("places type id 999", thrown.Message, StringComparison.Ordinal);
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
    public void A_map_the_bundle_does_not_carry_enough_bytes_for_refuses()
    {
        // The width and the height are read before the cells are, so a record
        // claiming a grid bigger than the bytes it has is caught by the shape
        // rather than by an allocation.
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.WithU16(
            good.ToBytes(),
            RecordFormat.HeaderBytes + 8,
            4000);

        RecordException thrown = Assert.Throws<RecordException>(() => ReplayBundle.FromBytes(bytes));

        Assert.Contains("bytes are left", thrown.Message, StringComparison.Ordinal);
    }
}
