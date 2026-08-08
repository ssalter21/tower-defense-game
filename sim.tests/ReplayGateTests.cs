namespace Sim.Tests;

/// <summary>
/// Two gates, not one: reading needs a known format version, replaying needs
/// every table and version the record was made under to match what is in front
/// of it, and a record that fails the second gate is still perfectly readable.
/// </summary>
/// <remarks>
/// How many things the second gate compares is the record kind's business: a
/// bundle checks the simulation version, the unit table and the map it inlined;
/// a command stream checks the simulation version, the unit table, the ruleset
/// and the anchor schedule, because those are what a stored decision means
/// anything against.
/// </remarks>
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

        MatchResult fromRecord = bundle.Replay(types, TheRuleset.Committed()).Resolve();
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

        MatchResult fromRecord = bundle.Replay(types, TheRuleset.Committed()).Resolve();

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
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(types, TheRuleset.Committed()));

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
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(retuned, TheRuleset.Committed()));

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
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(types, TheRuleset.Committed()));

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

        Exception thrown = Assert.ThrowsAny<Exception>(() => bundle.Replay(TheMatch.RetunedTypes(), TheRuleset.Committed()));

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

        Assert.Throws<RetiredRecordException>(() => bundle.Replay(retuned, TheRuleset.Committed()));

        Restaging restaged = bundle.RestageUnderCurrentRules(retuned, TheRuleset.Committed());

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
            .RestageUnderCurrentRules(types, TheRuleset.Committed());

        Assert.True(restaged.RulesetsCoincide);
        Assert.Contains("Restaged, not replayed", restaged.ToString(), StringComparison.Ordinal);
        Assert.Equal(
            TheMatch.Fresh().Resolve().RollingStateHash,
            restaged.Match.Resolve().RollingStateHash);
    }

    [Fact]
    public void A_command_stream_that_passes_both_gates_plays_the_run_it_recorded()
    {
        // The command stream's half of the first assertion in this file: the
        // bytes reproduce the run, round for round, through the same two gates
        // a bundle goes through.
        //
        // OBSERVED: have CommandStream.Replay return run.Outcome without the
        // loop that plays the commands. This goes red, 4 against 0 -- a gate
        // that opened onto nothing, handing back the outcome of a run that never
        // started.
        CommandStream stream = CommandStream.FromBytes(TheCommands.Stream().ToBytes());
        Run run = TheCommands.Fresh();

        RunOutcome outcome = stream.Replay(run, TheCommands.Defense());

        Assert.Equal(TheCommands.Waves, outcome.Rounds.Count);
        Assert.Equal(TheCommands.Waves, run.Round);
        Assert.Equal(TheRun.Seed, stream.Seed);
    }

    [Fact]
    public void The_ruleset_hash_gate_fails_on_its_own_and_names_both_hashes()
    {
        // A command stream is the first record kind whose meaning depends on
        // the ruleset -- it prices the wave, opens the purse and pays the
        // interest -- so a stream replayed against a retuned one is a
        // confidently wrong result. Nothing is wrong with these bytes; the
        // numbers moved underneath them.
        //
        // OBSERVED: drop the ruleset comparison from CommandStream.Replay. This
        // goes red having caught nothing -- no exception was thrown -- and the
        // stream plays four rounds against an income base it never earned and
        // hands back an outcome. Two other assertions in the suite go with it,
        // which is what a stamp nobody compares costs.
        CommandStream stream = CommandStream.FromBytes(TheCommands.Stream().ToBytes());
        Ruleset retuned = TheRuleset.Retuned();
        Run against = TheCommands.Against(retuned);

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => stream.Replay(against, TheCommands.Defense()));

        Assert.Equal("ruleset hash", thrown.Gate);
        Assert.Contains(TheRuleset.Committed().ContentHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(retuned.ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);

        // The other three gates are untouched, so this one is firing alone.
        Assert.Equal(SimulationVersion.Current, stream.Header.SimVersion);
        Assert.Equal(TheMatch.Types().ContentHash, stream.Header.ContentHash);
        Assert.Equal(TheSchedule.Committed().ContentHash, stream.ScheduleHash);

        // Still readable, still a run somebody could be shown.
        Assert.Equal(TheCommands.Waves, stream.Count);
        Assert.Equal(0, against.Round);
    }

    [Fact]
    public void Reformatting_the_ruleset_does_not_retire_a_stored_command_stream()
    {
        // The other half of the pair, and the half that separates a hash over
        // the parsed integers from a hash over the file. Rewrite the comments,
        // respace the columns and turn the line endings over: the ruleset is a
        // different file and the same rules, so the stream replays to exactly
        // the run it always did.
        //
        // OBSERVED: absorb the file's own bytes into the fold in Ruleset.Parse,
        // on top of the parsed fields. The hash assertion goes red,
        // FA24F949A18F9B90 against C2EDDB0C345FDD12, and every stored stream is
        // retired by a rewrapped comment -- at which point retiring a record
        // means "somebody touched ruleset.txt", which is a signal nobody can act
        // on.
        Ruleset reformatted = TheRuleset.Reformatted();

        Assert.NotEqual(TheRuleset.CommittedText(), TheRuleset.ReformattedText());
        Assert.Equal(TheRuleset.Committed().ContentHash, reformatted.ContentHash);

        RunOutcome committed = CommandStream
            .FromBytes(TheCommands.Stream().ToBytes())
            .Replay(TheCommands.Fresh(), TheCommands.Defense());

        RunOutcome against = CommandStream
            .FromBytes(TheCommands.Stream().ToBytes())
            .Replay(TheCommands.Against(reformatted), TheCommands.Defense());

        Assert.Equal(committed.Rounds, against.Rounds);
        Assert.Equal(committed.HealthRemaining, against.HealthRemaining);
    }

    [Fact]
    public void The_schedule_hash_gate_fails_on_its_own_and_names_both_hashes()
    {
        // The shape decides where the anchors are, which decides how wide a
        // round's slots are and which rounds merge a game changer in. A stream
        // replayed against a moved anchor is the same failure the ruleset gate
        // catches, one table over.
        //
        // OBSERVED: drop the schedule comparison from CommandStream.Replay. This
        // goes red having caught nothing -- no exception was thrown -- and that
        // is the whole argument for the stamp: moving the second anchor from
        // wave six to wave five leaves waves one to four's menus and slot widths
        // exactly where they were, so a four-round stream plays through a
        // rotation it was never recorded against and nothing downstream can tell.
        CommandStream stream = CommandStream.FromBytes(TheCommands.Stream().ToBytes());
        AnchorSchedule moved = TheSchedule.Reshaped();
        Run against = TheCommands.Against(TheRuleset.Committed(), moved);

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => stream.Replay(against, TheCommands.Defense()));

        Assert.Equal("schedule hash", thrown.Gate);
        Assert.Contains(TheSchedule.Committed().ContentHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(moved.ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_streams_simulation_version_and_unit_table_gates_each_fail_on_their_own()
    {
        // The two gates every record kind has, on the fourth kind. The
        // simulation version is stamped into the bytes so the stream is
        // internally consistent and only the gate has anything to say; the unit
        // table is moved underneath a stream that is untouched.
        //
        // OBSERVED: drop the simulation version comparison from
        // CommandStream.Replay. This goes red having caught nothing -- no
        // exception was thrown -- and a stream recorded under another tick order
        // plays under this one and reports the result as its own.
        //
        // OBSERVED: drop the unit table comparison from CommandStream.Replay.
        // The second half goes red the same way, and a stream plays four rounds
        // of creeps whose health pools moved underneath it.
        byte[] stamped = RecordBytes.WithU32(
            TheCommands.Bytes(),
            RecordBytes.SimVersionOffset,
            SimulationVersion.Current + 1);

        CommandStream older = CommandStream.FromBytes(stamped);

        Assert.Equal(TheCommands.Waves, older.Count);

        RetiredRecordException version = Assert.Throws<RetiredRecordException>(
            () => older.Replay(TheCommands.Fresh(), TheCommands.Defense()));

        Assert.Equal("simulation version", version.Gate);

        // And the unit table, which the shared header has always carried.
        UnitTypeTable retuned = TheMatch.RetunedTypes();
        Run against = new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            retuned,
            TheSchedule.Committed(retuned),
            TheRun.Pool(retuned),
            TheRun.Seed,
            TheCommands.Waves,
            fieldSize: 2);

        RetiredRecordException content = Assert.Throws<RetiredRecordException>(
            () => CommandStream.FromBytes(TheCommands.Bytes()).Replay(against, TheCommands.Defense()));

        Assert.Equal("content hash", content.Gate);
        Assert.Contains(retuned.ContentHash.ToString(), content.Live, StringComparison.Ordinal);
    }

    [Fact]
    public void A_command_stream_retired_by_its_ruleset_is_still_perfectly_readable()
    {
        // The distinction the two gates exist for, on the fourth kind: reading
        // and replaying throw unrelated exception types, so no catch can treat
        // "these are not a command stream" and "this stream is historical" as
        // one thing.
        //
        // OBSERVED: throw a RecordException from the ruleset gate instead of a
        // RetiredRecordException. This goes red on the exact type, and a stream
        // whose ruleset moved under it becomes indistinguishable from a stream
        // that will not parse -- so whatever lists stored runs stops being able
        // to show it as historical.
        CommandStream stream = CommandStream.FromBytes(TheCommands.Stream().ToBytes());

        Exception thrown = Assert.ThrowsAny<Exception>(
            () => stream.Replay(TheCommands.Against(TheRuleset.Retuned()), TheCommands.Defense()));

        Assert.IsType<RetiredRecordException>(thrown);
        Assert.IsNotType<RecordException>(thrown);
        Assert.Contains("still readable", thrown.Message, StringComparison.Ordinal);

        Assert.Equal(TheCommands.Waves, stream.Count);
        Assert.Equal(1, stream.Commands[0].Wave);
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
