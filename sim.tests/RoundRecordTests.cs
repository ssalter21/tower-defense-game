using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The record a folder of opponents is made of: a stage, a wall and a wave, and
/// the gates that stand between bytes on a disk and a round somebody meets.
/// </summary>
/// <remarks>
/// <para>
/// <b>Each of these was watched failing under a deliberately wrong input</b>,
/// which is the only way to know an assertion is load bearing. The wrong input
/// is written above each one, so the observation can be repeated.
/// </para>
/// <para>
/// The negative half is the shape <see cref="RecordNegativeTests"/> uses: take a
/// good record, damage one byte, and watch the specific refusal fire. What is
/// different here is that a refused stored round is skipped rather than fatal
/// -- but that is the folder reader's decision, out in <c>simcli</c>, and what
/// this file pins is that the refusal happens at all.
/// </para>
/// </remarks>
public class RoundRecordTests
{
    /// <summary>The stage the fixtures below are recorded at.</summary>
    private const int Stage = 7;

    [Fact]
    public void A_stored_round_survives_the_round_trip_and_writes_back_to_the_bytes_it_was_read_from()
    {
        // A pool is content-addressed, so the round trip is not a convenience:
        // an id is the hash of the bytes, and a reader that came back with
        // anything the writer would spell differently would give one round two
        // ids and let a field meet it twice.
        //
        // OBSERVED: write the stage after the two inner records rather than
        // before them. The read goes red on the magic tag, because the cursor
        // is two bytes into a defense record where a defense record's magic was
        // expected.
        UnitTypeTable types = TheMatch.Types();
        RoundRecord written = Recorded(types, Stage);
        byte[] bytes = written.ToBytes();

        RoundRecord read = RoundRecord.FromBytes("round", bytes);

        Assert.Equal(Stage, read.Stage);
        Assert.Equal(written.MapHash, read.MapHash);
        Assert.Equal(written.Header, read.Header);
        Assert.Equal(bytes, read.ToBytes());

        // And what comes out the far side is the pair a field is resolved
        // against, wall and wave, resolved against the roster it was stored
        // under.
        RoundOrders orders = read.ToOrders(types);

        Assert.Equal(TheMatch.Layout(types).Count, orders.Defense.Count);
        Assert.Equal(TheMatch.Wave(types).TotalUnits, orders.Wave.TotalUnits);
    }

    [Fact]
    public void Two_identical_rounds_have_one_id_and_a_different_stage_is_a_different_record()
    {
        // Content addressing is what makes a folder of these a set rather than a
        // list: the same round stored twice is one file. The stage is part of
        // the bytes, so the same wall and wave at another stage is another
        // record -- which is what stops a round played at wave one being drawn
        // against a run standing at wave ten.
        //
        // OBSERVED: drop the stage from ToBytes. The last assertion goes red,
        // two stages sharing one id.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            RecordId.Of(Recorded(types, Stage).ToBytes()),
            RecordId.Of(Recorded(types, Stage).ToBytes()));

        Assert.NotEqual(
            RecordId.Of(Recorded(types, Stage).ToBytes()),
            RecordId.Of(Recorded(types, Stage + 1).ToBytes()));
    }

    [Fact]
    public void A_record_of_another_kind_is_refused_by_the_kind_it_actually_is()
    {
        // A folder holds files somebody put there, and a defense record renamed
        // to look like a stored round is the ordinary accident. The magic is
        // four bytes and it buys the sentence that says what the bytes really
        // are.
        //
        // OBSERVED: drop the magic comparison in RecordHeader.Read. The read
        // runs on and refuses somewhere inside the defense's own fields, on a
        // message about a stage.
        UnitTypeTable types = TheMatch.Types();
        byte[] defense = GhostRecord
            .Of(TheMatch.Map(), TheMatch.Layout(types), types, GhostRecord.NoMapHandle)
            .ToBytes();

        RecordException thrown =
            Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", defense));

        Assert.Contains("'GHST'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("a stored round begins with 'RUND'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_format_version_this_reader_has_no_branch_for_is_refused_rather_than_guessed_at()
    {
        // The refusal a stale folder produces. A stored round from a later
        // format cannot be read best-effort -- the offsets have moved, so what
        // came back would be a wall and a wave made of noise that still
        // resolved.
        //
        // OBSERVED: return the version-0 branch for every version. The read
        // succeeds and hands back a record whose stage is whatever the next two
        // bytes happened to be.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = Recorded(types, Stage).ToBytes();

        bytes[RecordBytes.FormatVersionOffset] = 9;

        RecordException thrown =
            Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", bytes));

        Assert.Contains("format version 9", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_round_at_no_stage_at_all_is_refused_at_both_ends()
    {
        // Stages count from one, as waves do. Zero read as the first stage
        // would put a round nobody played into the population every opening
        // round is drawn from.
        //
        // OBSERVED: drop the stage guard in ReadVersion0. The read succeeds and
        // the record claims stage 0, which indexes a stage list at minus one
        // wherever it lands.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = Recorded(types, Stage).ToBytes();

        bytes[RecordFormat.HeaderBytes] = 0;
        bytes[RecordFormat.HeaderBytes + 1] = 0;

        RecordException read =
            Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", bytes));

        Assert.Contains("stage 0", read.Message, StringComparison.Ordinal);

        // And the writer refuses one before it can be stored, because a stage a
        // caller invented is this program's fault rather than somebody's bytes.
        SimulationException written = Assert.Throws<SimulationException>(
            () => Recorded(types, 0));

        Assert.Contains("recorded at stage 0", written.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wall_and_a_wave_from_two_rosters_stapled_together_are_refused_outright()
    {
        // The cross-check the replay bundle makes, one kind over. Both halves
        // carry their own content hash, so a record assembled out of two
        // rosters is a contradiction rather than an old record -- and it is
        // caught at the read, so that nothing can ever hold one.
        //
        // OBSERVED: drop the CrossCheck calls. The read succeeds and the round
        // resolves its wall against one roster's prices and its wave against
        // another's.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = Recorded(types, Stage).ToBytes();

        // The wave's own content hash, inside the record's second inner header.
        int wave = bytes.Length - WaveBytes(types) + RecordBytes.ContentHashOffset;

        bytes[wave] ^= 0xFF;

        RecordException thrown =
            Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", bytes));

        Assert.Contains("the wave inside it is stamped", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_truncated_record_is_refused_and_a_record_with_bytes_left_over_is_too()
    {
        // Reading is all-or-nothing, so neither end may be short. A truncation
        // is the failure a folder produces on a disk that filled up; trailing
        // bytes are what a file two records were appended to looks like.
        //
        // OBSERVED: drop the ExpectEnd call. The second assertion goes red --
        // a record with a whole second record stapled to it reads as the first
        // one and the rest is never looked at.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = Recorded(types, Stage).ToBytes();

        var truncated = new byte[bytes.Length - 1];
        var trailing = new byte[bytes.Length + 1];

        Array.Copy(bytes, truncated, truncated.Length);
        Array.Copy(bytes, trailing, bytes.Length);

        Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", truncated));
        Assert.Throws<RecordException>(() => RoundRecord.FromBytes("round", trailing));
    }

    [Fact]
    public void A_pool_takes_every_record_it_can_play_and_names_every_one_it_cannot()
    {
        // What a folder of these comes to. Nothing here throws: a pool is read
        // for a run that is about to start, so a record from a retired format
        // or another board is skipped with its name said rather than stopping
        // the run -- which is the whole difference between reading one record
        // and filling a population out of a directory somebody has been playing
        // into for months.
        //
        // OBSERVED: let the RetiredRecordException out of StoredRounds.Add
        // rather than catching it. The first Add throws and the pool is never
        // built, so one record made under other numbers is a run nobody can
        // play.
        UnitTypeTable types = TheMatch.Types();
        UnitTypeTable elsewhere = TheRun.UnkillableTypes();
        var pool = new StoredRounds(TheMatch.Map(), types);

        byte[] good = Recorded(types, Stage).ToBytes();

        // A record that reads perfectly and was made against another roster.
        // Every one of its three headers agrees with the other two, so nothing
        // about it is a contradiction -- it is a round this run cannot fight,
        // which is the replay gate's question rather than the reader's.
        byte[] retired = Recorded(elsewhere, Stage).ToBytes();
        byte[] truncated = new byte[good.Length - 1];

        Array.Copy(good, truncated, truncated.Length);

        pool.Add(StoredRounds.NameOf(retired), retired);
        pool.Add(StoredRounds.NameOf(truncated), truncated);
        pool.Add("not-the-id-of-anything", good);
        pool.Add(StoredRounds.NameOf(good), good);

        Assert.Equal(1, pool.Count);
        Assert.Equal(Stage, pool.Stages);
        Assert.Single(pool.ByStage[Stage - 1]);
        Assert.Empty(pool.ByStage[0]);
        Assert.Equal(3, pool.Refusals.Count);

        // Each refusal names the record it refused, which is the only way to
        // find it again in a folder of hundreds.
        Assert.Contains(StoredRounds.NameOf(retired), pool.Refusals[0], StringComparison.Ordinal);
        Assert.Contains("content hash gate failed", pool.Refusals[0], StringComparison.Ordinal);
        Assert.Contains(StoredRounds.NameOf(truncated), pool.Refusals[1], StringComparison.Ordinal);
        Assert.Contains("not-the-id-of-anything", pool.Refusals[2], StringComparison.Ordinal);

        // And what it took is the pair a field is resolved against.
        Assert.Equal(
            TheMatch.Layout(types).Count,
            pool.ByStage[Stage - 1][0].Defense.Count);
    }

    [Fact]
    public void A_stage_is_ordered_by_id_whatever_order_its_records_arrived_in()
    {
        // A draw is an index into a stage, so two callers whose directories
        // listed the same files in different orders would draw different fields
        // off one seed. The order is a fact about the bytes and it is settled
        // here rather than in each shell that walks a folder.
        //
        // OBSERVED: append in Take rather than inserting in place. The two
        // pools stop agreeing, because one of them holds the records in the
        // order they were offered.
        UnitTypeTable types = TheMatch.Types();
        byte[][] records =
        {
            Recorded(types, Stage, "tower   3     4    3").ToBytes(),
            Recorded(types, Stage, "tower   3     14   3").ToBytes(),
            Recorded(types, Stage, "tower   3     14   8").ToBytes(),
        };

        Assert.Equal(
            Spelled(Filled(types, records[0], records[1], records[2])),
            Spelled(Filled(types, records[2], records[0], records[1])));
    }

    /// <summary>A pool filled with these records, in this order.</summary>
    private static StoredRounds Filled(UnitTypeTable types, params byte[][] records)
    {
        var pool = new StoredRounds(TheMatch.Map(), types);

        foreach (byte[] record in records)
        {
            pool.Add(StoredRounds.NameOf(record), record);
        }

        return pool;
    }

    /// <summary>A stage's population, as the ids of what is in it, in order.</summary>
    private static string Spelled(StoredRounds pool)
    {
        var spelled = new System.Text.StringBuilder();

        for (int index = 0; index < pool.ByStage[Stage - 1].Count; index++)
        {
            spelled.Append(pool.Drawn(Stage, index)!.Value.ToString()).Append(' ');
        }

        return spelled.ToString();
    }

    /// <summary>The committed wall and the committed wave, recorded at a stage.</summary>
    private static RoundRecord Recorded(UnitTypeTable types, int stage) =>
        RoundRecord.Of(
            TheMatch.Map(),
            TheMatch.Layout(types),
            TheMatch.Wave(types),
            types,
            stage,
            GhostRecord.NoMapHandle);

    /// <summary>The same, behind a wall written out here rather than the committed one.</summary>
    private static RoundRecord Recorded(UnitTypeTable types, int stage, string wall) =>
        RoundRecord.Of(
            TheMatch.Map(),
            TowerLayout.Parse("wall", wall, types),
            TheMatch.Wave(types),
            types,
            stage,
            GhostRecord.NoMapHandle);

    /// <summary>How long the wave half of one of these records is.</summary>
    private static int WaveBytes(UnitTypeTable types) =>
        WaveRecord.Of(TheMatch.Wave(types), types).ToBytes().Length;
}
