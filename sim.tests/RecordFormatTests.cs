namespace Sim.Tests;

/// <summary>
/// The format itself: the shared header, the two leaf record kinds, the round
/// trips, and the identity fields that own three non-overlapping things.
/// </summary>
public class RecordFormatTests
{
    [Fact]
    public void The_header_is_eighteen_bytes_and_names_itself_before_anything_else()
    {
        // Magic first so "these are not the bytes you thought" is answered
        // before anything is interpreted; format version next because it is the
        // field that says where every other field is, including where the
        // simulation version is.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = TheMatch.Ghost(types).ToBytes();

        Assert.Equal(18, RecordFormat.HeaderBytes);
        Assert.Equal((byte)'G', bytes[0]);
        Assert.Equal((byte)'H', bytes[1]);
        Assert.Equal((byte)'S', bytes[2]);
        Assert.Equal((byte)'T', bytes[3]);
        Assert.Equal(0, bytes[RecordBytes.FormatVersionOffset]);
        Assert.Equal(0, bytes[RecordBytes.FormatVersionOffset + 1]);
        Assert.Equal(SimulationVersion.Current, BitConverter.ToUInt32(bytes, RecordBytes.SimVersionOffset));
        Assert.Equal(types.ContentHash.Value, BitConverter.ToUInt64(bytes, RecordBytes.ContentHashOffset));
    }

    [Fact]
    public void The_three_kinds_have_three_different_magic_tags()
    {
        Assert.Equal("GHST", RecordFormat.MagicOf(RecordKind.Ghost));
        Assert.Equal("WAVE", RecordFormat.MagicOf(RecordKind.Wave));
        Assert.Equal("RPLY", RecordFormat.MagicOf(RecordKind.Replay));
    }

    [Fact]
    public void Format_versions_are_counted_per_record_kind()
    {
        // Three counters, three histories. A single global counter would mean
        // editing the wave layout bumped every stored defense's version too, so
        // every defense would look newer than it is and readers would branch on
        // versions that never changed a defense at all. What makes it real,
        // rather than three constants that happen to be equal, is that the gate
        // is asked per kind: when one of them gains a version 1 the other two
        // will still refuse it.
        Assert.True(RecordFormat.IsKnown(RecordKind.Ghost, RecordFormat.GhostVersion));
        Assert.True(RecordFormat.IsKnown(RecordKind.Wave, RecordFormat.WaveVersion));
        Assert.True(RecordFormat.IsKnown(RecordKind.Replay, RecordFormat.ReplayVersion));

        Assert.False(RecordFormat.IsKnown(RecordKind.Ghost, RecordFormat.GhostVersion + 1));
        Assert.False(RecordFormat.IsKnown(RecordKind.Wave, RecordFormat.WaveVersion + 1));
        Assert.False(RecordFormat.IsKnown(RecordKind.Replay, RecordFormat.ReplayVersion + 1));
    }

    [Fact]
    public void Version_zero_carries_no_map_handle_and_the_size_says_so()
    {
        // The defense is exactly header, map hash, count and towers -- nothing
        // else fits. That is the version-0 layout, deliberately without the
        // u16 map_id the developer actually wants: it is held back so that
        // adding it is a real format bump rather than a rehearsal on an invented
        // field. If this number grows by two without the format version moving,
        // that is the mistake this assertion exists to catch.
        UnitTypeTable types = TheMatch.Types();
        GhostRecord ghost = TheMatch.Ghost(types);

        Assert.Equal(6, ghost.Count);
        Assert.Equal(18 + 8 + 2 + (6 * 6), ghost.ToBytes().Length);
    }

    [Fact]
    public void Nothing_mutable_or_descriptive_is_in_the_binary_format()
    {
        // Rating, author, timestamps and progression are absent by construction
        // rather than by omission: every byte of both kinds is accounted for
        // below, so there is nowhere for one of them to be. A rating moves every
        // time a defense wins, and a record's id is the hash of its bytes, so
        // storing one would change the defense's identity on every match and
        // orphan every replay pointing at it.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(18 + 8 + 2 + (6 * RecordFormat.TowerBytes), TheMatch.Ghost(types).ToBytes().Length);
        Assert.Equal(18 + 2 + (6 * RecordFormat.OrderBytes), TheMatch.WaveOf(types).ToBytes().Length);
    }

    [Fact]
    public void Bytes_read_back_and_written_out_again_are_the_same_bytes()
    {
        // The byte round trip, and it is asserted on the current format only.
        // There is one writer and it emits only the current version, so
        // write(read(old_bytes)) deliberately produces different bytes -- the
        // historical formats get a semantic round trip instead, which is the
        // version-bump ticket's business.
        UnitTypeTable types = TheMatch.Types();
        byte[] ghost = TheMatch.Ghost(types).ToBytes();
        byte[] wave = TheMatch.WaveOf(types).ToBytes();

        Assert.Equal(ghost, GhostRecord.FromBytes(ghost).ToBytes());
        Assert.Equal(wave, WaveRecord.FromBytes(wave).ToBytes());
    }

    [Fact]
    public void A_record_read_back_equals_the_record_it_was_written_from()
    {
        // The value round trip, on every constructible record.
        UnitTypeTable types = TheMatch.Types();
        GhostRecord ghost = TheMatch.Ghost(types);
        WaveRecord wave = TheMatch.WaveOf(types);

        Assert.Equal(ghost, GhostRecord.FromBytes(ghost.ToBytes()));
        Assert.Equal(wave, WaveRecord.FromBytes(wave.ToBytes()));
    }

    [Fact]
    public void The_towers_in_the_record_are_the_towers_of_the_defense_in_the_same_order()
    {
        UnitTypeTable types = TheMatch.Types();
        TowerLayout layout = TheMatch.Layout(types);
        GhostRecord ghost = TheMatch.Ghost(types);

        Assert.Equal(layout.Count, ghost.Count);

        for (int index = 0; index < layout.Count; index++)
        {
            Assert.Equal(layout.Towers[index].Type.Id, ghost.Towers[index].TypeId);
            Assert.Equal(layout.Towers[index].Hex, ghost.Towers[index].Cell);
        }
    }

    [Fact]
    public void The_record_ascends_by_r_then_q_which_is_what_the_file_ascends_by()
    {
        // The authored file asserts ascending row then column and the record
        // asserts ascending r then q. They are the same order -- r is the row,
        // and the odd-r conversion is monotone in the column within a row -- so
        // one of them is not quietly a different rule wearing the same name.
        GhostRecord ghost = TheMatch.Ghost(TheMatch.Types());

        for (int index = 1; index < ghost.Count; index++)
        {
            Hex previous = ghost.Towers[index - 1].Cell;
            Hex current = ghost.Towers[index].Cell;

            Assert.True(current.R > previous.R || (current.R == previous.R && current.Q > previous.Q));
        }
    }

    [Fact]
    public void The_defense_survives_the_round_trip_as_a_defense()
    {
        UnitTypeTable types = TheMatch.Types();
        TowerLayout original = TheMatch.Layout(types);
        TowerLayout restored = GhostRecord.FromBytes(TheMatch.Ghost(types).ToBytes()).ToLayout(types);

        Assert.Equal(original.Count, restored.Count);

        for (int index = 0; index < original.Count; index++)
        {
            Assert.Equal(original.Towers[index].Type.Id, restored.Towers[index].Type.Id);
            Assert.Equal(original.Towers[index].Column, restored.Towers[index].Column);
            Assert.Equal(original.Towers[index].Row, restored.Towers[index].Row);
        }
    }

    [Fact]
    public void The_wave_survives_the_round_trip_as_a_wave()
    {
        UnitTypeTable types = TheMatch.Types();
        WaveScript original = TheMatch.Wave(types);
        WaveScript restored = WaveRecord.FromBytes(TheMatch.WaveOf(types).ToBytes()).ToScript(types);

        Assert.Equal(original.Count, restored.Count);
        Assert.Equal(original.TotalUnits, restored.TotalUnits);

        for (int index = 0; index < original.Count; index++)
        {
            Assert.Equal(original.Orders[index].TickOffset, restored.Orders[index].TickOffset);
            Assert.Equal(original.Orders[index].TypeId, restored.Orders[index].TypeId);
            Assert.Equal(original.Orders[index].Count, restored.Orders[index].Count);
            Assert.Equal(original.Orders[index].Corridor, restored.Orders[index].Corridor);
        }
    }

    [Fact]
    public void The_same_defense_authored_twice_yields_identical_bytes_and_an_identical_id()
    {
        // Id stability. Reindent the columns, rewrite the comments, change the
        // line endings and pad the ends: none of it reaches the bytes, because
        // what is recorded is the parsed defense and not the file it was typed
        // into.
        UnitTypeTable types = TheMatch.Types();
        HexMap map = TheMatch.Map();
        string original = File.ReadAllText(RepoLayout.DefenseFile);
        string reauthored = Reauthored(original);

        Assert.NotEqual(original, reauthored);

        byte[] first = GhostRecord.Of(map, TowerLayout.Parse(original, types), types).ToBytes();
        byte[] second = GhostRecord.Of(map, TowerLayout.Parse(reauthored, types), types).ToBytes();

        Assert.Equal(first, second);
        Assert.Equal(RecordId.Of(first), RecordId.Of(second));
    }

    [Fact]
    public void The_same_defense_typed_in_a_different_order_is_not_a_second_set_of_bytes_because_it_will_not_load()
    {
        // This is the other half of id stability, and it is why the ordering is
        // asserted rather than sorted. Sorting on load would accept the file
        // below, stabilise iteration, and leave two identical defenses with two
        // different sets of bytes -- at which point content-addressing a defense
        // stops meaning anything. Refusing it makes the canonical spelling the
        // only spelling there is.
        UnitTypeTable types = TheMatch.Types();

        ContentException thrown = Assert.Throws<ContentException>(() => TowerLayout.Parse(
            """
            tower   3     3    2
            tower   4     9    0
            tower   3     6    4
            tower   3     12   4
            tower   4     4    6
            tower   3     10   8
            """,
            types));

        Assert.Contains("canonical order", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_record_id_is_the_hash_of_its_own_bytes_and_is_stored_nowhere()
    {
        // "This wave goes with this defense" is a fact about bytes rather than a
        // field somebody filled in, so it cannot be faked by a filename or by an
        // envelope. And it survives the trip through bytes, which is what makes
        // it usable as a key for anything.
        UnitTypeTable types = TheMatch.Types();
        byte[] ghost = TheMatch.Ghost(types).ToBytes();
        byte[] wave = TheMatch.WaveOf(types).ToBytes();

        Assert.NotEqual(RecordId.Of(ghost), RecordId.Of(wave));
        Assert.Equal(RecordId.Of(ghost), RecordId.Of(GhostRecord.FromBytes(ghost).ToBytes()));
        Assert.Equal(RecordId.Of(wave), RecordId.Of(WaveRecord.FromBytes(wave).ToBytes()));
    }

    [Fact]
    public void One_byte_of_a_record_moves_its_id()
    {
        // Otherwise an id would be a label rather than an identity.
        UnitTypeTable types = TheMatch.Types();
        byte[] ghost = TheMatch.Ghost(types).ToBytes();

        Assert.NotEqual(
            RecordId.Of(ghost),
            RecordId.Of(RecordBytes.Flip(ghost, RecordBytes.GhostTowersOffset)));
    }

    [Fact]
    public void The_content_hash_is_recomputed_at_load_and_never_written_by_hand()
    {
        // Three identity fields, three owners. This one owns the numbers: it
        // arrives from the parsed tables, so a person retuning a tower does not
        // have to remember anything, and a person changing a rule cannot reach
        // for it by mistake.
        UnitTypeTable types = TheMatch.Types();
        UnitTypeTable retuned = TheMatch.RetunedTypes();

        Assert.NotEqual(types.ContentHash, retuned.ContentHash);
        Assert.Equal(types.ContentHash, TheMatch.Ghost(types).Header.ContentHash);
        Assert.Equal(retuned.ContentHash, TheMatch.Ghost(retuned).Header.ContentHash);

        // And the simulation version is untouched by all of it, because no rule
        // changed. Retuning a number is not a behaviour change and reaching for
        // this field would retire every record made under an unchanged ruleset.
        Assert.Equal(SimulationVersion.Current, TheMatch.Ghost(retuned).Header.SimVersion);
    }

    /// <summary>
    /// The same defense, typed by somebody with different habits: no comments,
    /// leading indentation, tabs between the columns, trailing spaces and CRLF
    /// line endings.
    /// </summary>
    private static string Reauthored(string original) =>
        "# a completely different comment\n\n"
        + string.Join(
            "\r\n",
            original
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("#", StringComparison.Ordinal))
                .Where(line => line.Trim().Length > 0)
                .Select(line => "  " + string.Join(
                    "\t",
                    line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) + "   "));
}
