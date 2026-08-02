namespace Sim.Tests;

/// <summary>
/// Two gates, not one: reading needs a known format version, replaying needs
/// three more things to match, and a record that fails the second one is still
/// perfectly readable.
/// </summary>
public class ReplayGateTests
{
    [Fact]
    public void A_bundle_that_passes_both_gates_replays_the_match_it_recorded()
    {
        // The whole point of the format, in one assertion: the bytes reproduce
        // the match, tick for tick, including the rolling hash over internal
        // state that no snapshot ever carries.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        MatchResult fromRecord = bundle.Replay(types).Resolve();
        MatchResult live = TheMatch.Fresh().Resolve();

        Assert.Equal(live.RollingStateHash, fromRecord.RollingStateHash);
        Assert.Equal(TheMatch.LeakedInTheCommittedRun, fromRecord.Leaked);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, fromRecord.FinalTick);
    }

    [Fact]
    public void The_seed_in_the_record_is_the_seed_the_replay_runs_under()
    {
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle(TheMatch.Seed + 1).ToBytes());

        MatchResult fromRecord = bundle.Replay(types).Resolve();

        Assert.Equal(TheMatch.Fresh(TheMatch.Seed + 1).Resolve().RollingStateHash, fromRecord.RollingStateHash);
        Assert.NotEqual(TheMatch.Fresh().Resolve().RollingStateHash, fromRecord.RollingStateHash);
    }

    [Fact]
    public void The_simulation_version_gate_fails_on_its_own_and_names_both_versions()
    {
        // A record made under a ruleset that has since moved on. All three
        // copies of the header are stamped together, because a bundle that
        // contradicts itself is a different fault with a different answer.
        UnitTypeTable types = TheMatch.Types();
        byte[] bytes = StampedSimVersion(TheMatch.Bundle().ToBytes(), SimulationVersion.Current + 1);

        ReplayBundle bundle = ReplayBundle.FromBytes(bytes);

        // It read. That is the first gate passing, and it is what lets a
        // historical defense still be listed and drawn.
        Assert.Equal(6, bundle.Ghost.Count);
        Assert.Equal(SimulationVersion.Current + 1, bundle.Header.SimVersion);

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(types));

        Assert.Equal("simulation version", thrown.Gate);
        Assert.Contains((SimulationVersion.Current + 1).ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(SimulationVersion.Current.ToString(), thrown.Live, StringComparison.Ordinal);
    }

    [Fact]
    public void The_content_hash_gate_fails_on_its_own_and_names_both_hashes()
    {
        // Nothing is wrong with these bytes at all. The numbers moved underneath
        // them, which is the ordinary consequence of a balance patch and the
        // reason a retired record has to stay readable.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());
        UnitTypeTable retuned = TheMatch.RetunedTypes();

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(retuned));

        Assert.Equal("content hash", thrown.Gate);
        Assert.Contains(TheMatch.Types().ContentHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(retuned.ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);

        // Still readable, still a defense somebody could be shown.
        Assert.Equal(6, bundle.Ghost.Count);
        Assert.Equal(6, bundle.Wave.Count);
    }

    [Fact]
    public void The_map_hash_gate_fails_on_its_own_and_names_both_hashes()
    {
        // The defense says it was built on one grid and the grid inlined beside
        // it is another. The simulation version and the content hash are both
        // untouched, so this gate is firing alone.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle good = TheMatch.Bundle();
        byte[] bytes = RecordBytes.Flip(
            good.ToBytes(),
            RecordBytes.GhostIn(good) + RecordBytes.GhostMapHashOffset);

        ReplayBundle bundle = ReplayBundle.FromBytes(bytes);

        Assert.Equal(SimulationVersion.Current, bundle.Header.SimVersion);
        Assert.Equal(types.ContentHash, bundle.Header.ContentHash);

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(types));

        Assert.Equal("map hash", thrown.Gate);
        Assert.Contains(bundle.Ghost.MapHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(bundle.Map.MapHash.ToString(), thrown.Live, StringComparison.Ordinal);
    }

    [Fact]
    public void A_retired_record_refuses_by_name_rather_than_by_being_unopenable()
    {
        // The distinction the two gates exist for, stated as an assertion:
        // reading and replaying throw unrelated exception types, so no catch can
        // treat "these are not a record" and "this record is historical" as one
        // thing.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        Exception thrown = Assert.ThrowsAny<Exception>(() => bundle.Replay(TheMatch.RetunedTypes()));

        Assert.IsType<RetiredRecordException>(thrown);
        Assert.IsNotType<RecordException>(thrown);
        Assert.Contains("still readable", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Replaying_under_todays_numbers_is_a_different_operation_with_a_different_label()
    {
        // No field-rewriting migration, ever. "How would that defense hold up
        // under the current numbers?" is a real question and it gets a real
        // answer -- just never from inside Replay, and never labelled as this
        // record's result.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());
        UnitTypeTable retuned = TheMatch.RetunedTypes();

        Assert.Throws<RetiredRecordException>(() => bundle.Replay(retuned));

        Restaging restaged = bundle.RestageUnderCurrentRules(retuned);

        Assert.False(restaged.RulesetsCoincide);
        Assert.Equal(TheMatch.Types().ContentHash, restaged.RecordedContentHash);
        Assert.Equal(retuned.ContentHash, restaged.ContentHashUsed);
        Assert.Contains("not replayed", restaged.ToString(), StringComparison.Ordinal);
        Assert.Contains("not this record's result", restaged.ToString(), StringComparison.Ordinal);

        // And it really did run against the other numbers, which is the whole
        // reason it must never be mistaken for the record's own outcome.
        Assert.NotEqual(
            TheMatch.Fresh().Resolve().RollingStateHash,
            restaged.Match.Resolve().RollingStateHash);
    }

    [Fact]
    public void A_restaging_that_happens_to_match_is_still_labelled_a_restaging()
    {
        // The label follows what was asked for rather than what the numbers
        // turned out to be, so nothing downstream can learn to read a restaging
        // as a replay on the days they agree.
        UnitTypeTable types = TheMatch.Types();
        Restaging restaged = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes())
            .RestageUnderCurrentRules(types);

        Assert.True(restaged.RulesetsCoincide);
        Assert.Contains("Restaged, not replayed", restaged.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            TheMatch.Fresh().Resolve().RollingStateHash,
            restaged.Match.Resolve().RollingStateHash);
    }

    /// <summary>
    /// Rewrites the simulation version in all three headers a bundle carries, so
    /// that the record is internally consistent and only the replay gate has
    /// anything to say about it.
    /// </summary>
    private static byte[] StampedSimVersion(byte[] bytes, uint version)
    {
        ReplayBundle bundle = ReplayBundle.FromBytes(bytes);

        byte[] stamped = RecordBytes.WithU32(bytes, RecordBytes.SimVersionOffset, version);
        stamped = RecordBytes.WithU32(
            stamped,
            RecordBytes.GhostIn(bundle) + RecordBytes.SimVersionOffset,
            version);

        return RecordBytes.WithU32(
            stamped,
            RecordBytes.WaveIn(bundle) + RecordBytes.SimVersionOffset,
            version);
    }
}
