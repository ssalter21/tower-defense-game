using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// Two gates, not one: reading needs a known format version, replaying needs
/// every table and version the record was made under to match what is in front
/// of it, and a record that fails the second gate is still perfectly readable.
/// </summary>
/// <remarks>
/// Which things the second gate compares is the record kind's business, and each
/// kind declares them to one walk: a bundle declares the simulation version, the
/// unit table, the ruleset and the map it inlined; a command stream declares the
/// simulation version, the unit table, the ruleset and the anchor schedule,
/// because those are what a stored decision means anything against.
/// </remarks>
public class ReplayGateTests
{
    [Fact]
    public void The_gate_walks_the_declared_stamps_in_order_and_names_the_first_that_disagrees()
    {
        // What a record kind hands the gate is a list, and the list's order is
        // what a refusal reports. Two rows wrong at once names the one declared
        // first, so a record whose whole world has moved is described by the
        // coarsest thing that moved.
        //
        // OBSERVED: have ReplayGate.Require walk the whole list and throw on
        // the last mismatch rather than the first. This goes red naming
        // "ruleset hash" for a record the simulation version retires whatever
        // any ruleset says.
        Hash64 recorded = TheRuleset.Committed().ContentHash;
        Hash64 live = TheRuleset.RetunedDamage().ContentHash;

        RetiredRecordException thrown = Assert.Throws<RetiredRecordException>(() => ReplayGate.Require(
            Stamp.Of("simulation version", SimulationVersion.Current + 1, SimulationVersion.Current),
            Stamp.Of("content", recorded, live),
            Stamp.Of("ruleset", recorded, live)));

        Assert.Equal("simulation version", thrown.Gate);
        Assert.Equal(
            "simulation version " + (SimulationVersion.Current + 1).ToString(CultureInfo.InvariantCulture),
            thrown.Recorded);
        Assert.Equal(
            "simulation version " + SimulationVersion.Current.ToString(CultureInfo.InvariantCulture),
            thrown.Live);
    }

    [Fact]
    public void A_stamp_the_record_does_not_carry_agrees_with_no_live_value()
    {
        // The row a kind declares for a stamp its older records may not have.
        // Absence is not a value that happens to differ, it is the record
        // making no claim, so it refuses against the hash it was actually
        // recorded under as firmly as against any other.
        //
        // OBSERVED: let a null recorded value pass. This goes red having caught
        // nothing, and every bundle written before the ruleset field existed
        // replays under whatever numbers happen to be loaded.
        Hash64 live = TheRuleset.Committed().ContentHash;

        RetiredRecordException thrown = Assert.Throws<RetiredRecordException>(
            () => ReplayGate.Require(Stamp.Of("ruleset", (Hash64?)null, live)));

        Assert.Equal("ruleset hash", thrown.Gate);
        Assert.Equal("no ruleset stamp", thrown.Recorded);
        Assert.Equal("ruleset " + live, thrown.Live);
    }

    [Fact]
    public void The_restagings_label_is_the_same_walk_as_the_refusal()
    {
        // A restaging asks the gate for a label rather than a refusal, over the
        // three stamps it sets aside. One walk, so the label and the refusal
        // cannot come to different answers about one pair -- which is what a
        // second hand-written chain of comparisons was free to do.
        //
        // OBSERVED: drop the ruleset row from RulesetsCoincide. This goes red,
        // and a restaging run under a retuned ruleset reports that today's
        // rules are the record's own. The version-0 retirement assertion goes
        // with it, which is what a second list of comparisons costs.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        Assert.True(bundle.RestageUnderCurrentRules(types, TheRuleset.Committed()).RulesetsCoincide);
        Assert.False(bundle.RestageUnderCurrentRules(types, TheRuleset.RetunedDamage()).RulesetsCoincide);
        Assert.False(
            bundle.RestageUnderCurrentRules(TheMatch.RetunedTypes(), TheRuleset.Committed()).RulesetsCoincide);
    }

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
    public void The_bundles_ruleset_hash_gate_fails_on_its_own_and_names_both_hashes()
    {
        // Nothing is wrong with these bytes either. A number moved underneath
        // them -- the armour denominator, which every landing divides by -- and
        // the three gates a bundle used to have have nothing whatever to say
        // about it.
        //
        // OBSERVED: drop the ruleset comparison from ReplayBundle.Replay. This
        // goes red having caught nothing -- no exception was thrown -- and the
        // committed bundle replays to 5A47152F5A40D790 where the record's own
        // result is B58DBED2315303D2, with the simulation version, the content
        // hash and the map hash all agreeing that it is the recorded match.
        // The bundle on disk rather than one built here, because what the stamp
        // has to protect is a record somebody stored months ago and a rule
        // somebody retunes today.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.BundleFile));
        Ruleset retuned = TheRuleset.RetunedDamage();

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(types, retuned));

        Assert.Equal("ruleset hash", thrown.Gate);
        Assert.Contains(TheRuleset.Committed().ContentHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(retuned.ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);

        // The other three gates are untouched, so this one is firing alone.
        Assert.Equal(SimulationVersion.Current, bundle.Header.SimVersion);
        Assert.Equal(types.ContentHash, bundle.Header.ContentHash);
        Assert.Equal(bundle.Map.MapHash, bundle.Ghost.MapHash);

        // Still readable, still a defense somebody could be shown.
        Assert.Equal(6, bundle.Ghost.Count);
        Assert.Equal(6, bundle.Wave.Count);

        // And this is what the gate is refusing: the same bytes, the same
        // defense and the same wave, coming to a different match. Restaging is
        // the only operation that will run it, and it says so.
        Assert.NotEqual(
            TheMatch.Fresh().Resolve().RollingStateHash,
            bundle.RestageUnderCurrentRules(types, retuned).Match.Resolve().RollingStateHash);
    }

    [Fact]
    public void A_bundle_recorded_before_the_ruleset_stamp_is_retired_at_that_gate()
    {
        // The decision ADR 0047 records, as an assertion. A version-0 bundle
        // names no ruleset, every number a landing resolves through lives in
        // one, and there is no value this gate could accept on the record's
        // behalf that would not be an input the recorded run never had. So it
        // reads, and it does not replay.
        //
        // OBSERVED: let an unstamped record through the gate -- guard the
        // comparison in ReplayBundle.Replay with `RulesetHash is not null`,
        // which is the other half of the decision written out. This goes red
        // having caught nothing, no exception was thrown, and the bundle
        // replays under whatever numbers happen to be loaded and reports the
        // result as the record's own. That is the failure the gate exists for,
        // wearing the format bump as a disguise.
        UnitTypeTable types = TheMatch.Types();
        byte[] older = RecordBytes.WithoutTheRulesetStamp(TheMatch.Bundle().ToBytes());

        ReplayBundle bundle = ReplayBundle.FromBytes(older);

        // It read, through the branch that claims to know that version, and
        // everything a version-0 bundle carries is there.
        Assert.Equal(0, bundle.Header.FormatVersion);
        Assert.Null(bundle.RulesetHash);
        Assert.Equal(TheMatch.Seed, bundle.Seed);
        Assert.Equal(6, bundle.Ghost.Count);
        Assert.Equal(6, bundle.Wave.Count);

        RetiredRecordException thrown = Assert.Throws<RetiredRecordException>(
            () => bundle.Replay(types, TheRuleset.Committed()));

        Assert.Equal("ruleset hash", thrown.Gate);
        Assert.Contains("no ruleset stamp", thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(TheRuleset.Committed().ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);

        // It refuses against the ruleset it was actually recorded under as
        // firmly as against any other, which is the whole of the decision: what
        // is missing is the record's claim, not a match between two hashes.
        Assert.Throws<RetiredRecordException>(() => bundle.Replay(types, TheRuleset.RetunedDamage()));

        // And restaging still runs it, still labelled, which is what keeps the
        // irreplaceable version-0 golden alive.
        Restaging restaged = bundle.RestageUnderCurrentRules(types, TheRuleset.Committed());

        Assert.Null(restaged.RecordedRulesetHash);
        Assert.False(restaged.RulesetsCoincide);
        Assert.Contains("no ruleset stamp", restaged.ToString(), StringComparison.Ordinal);
        Assert.Equal(TheMatch.Fresh().Resolve().RollingStateHash, restaged.Match.Resolve().RollingStateHash);
    }

    [Fact]
    public void A_bundle_that_names_no_ruleset_cannot_be_written_back_out()
    {
        // The writer emits the current format version and only that, and the
        // current one stamps a ruleset. There is nothing honest to put in that
        // field for a record that never named one, so asking is refused rather
        // than answered with a zero -- which would be a bundle claiming numbers
        // it could then pass a gate against.
        ReplayBundle bundle = ReplayBundle.FromBytes(
            RecordBytes.WithoutTheRulesetStamp(TheMatch.Bundle().ToBytes()));

        SimulationException thrown = Assert.Throws<SimulationException>(() => bundle.ToBytes());

        Assert.Contains("cannot be rewritten", thrown.Message, StringComparison.Ordinal);
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
        //
        // Both retuned together, the table and the ruleset, because the two are
        // what "the current numbers" means and the operation has to set aside
        // both or the distinction it draws is only half drawn.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());
        UnitTypeTable retuned = TheMatch.RetunedTypes();
        Ruleset retunedRules = TheRuleset.RetunedDamage();

        Assert.Throws<RetiredRecordException>(() => bundle.Replay(retuned, retunedRules));

        Restaging restaged = bundle.RestageUnderCurrentRules(retuned, retunedRules);

        Assert.False(restaged.RulesetsCoincide);
        Assert.Equal(TheMatch.Types().ContentHash, restaged.RecordedContentHash);
        Assert.Equal(retuned.ContentHash, restaged.ContentHashUsed);
        Assert.Equal(TheRuleset.Committed().ContentHash, restaged.RecordedRulesetHash);
        Assert.Equal(retunedRules.ContentHash, restaged.RulesetHashUsed);
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
    public void Restaging_sets_the_ruleset_aside_and_still_enforces_the_map_hash()
    {
        // The two halves of the operation, in one place. What it sets aside is
        // "were these the same rules?" -- so a ruleset the bundle was never
        // recorded against runs. What it does not set aside is "are these bytes
        // internally consistent?" -- so a defense claiming one grid beside an
        // inlined other refuses here exactly as it does at the replay gate.
        //
        // OBSERVED: drop the map hash comparison from
        // RestageUnderCurrentRules. The second half goes red having caught
        // nothing, and a bundle whose defense and whose grid are two different
        // playfields runs to an outcome somebody keeps.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle good = TheMatch.Bundle();

        Assert.NotNull(
            ReplayBundle.FromBytes(good.ToBytes())
                .RestageUnderCurrentRules(types, TheRuleset.RetunedDamage())
                .Match);

        ReplayBundle bent = ReplayBundle.FromBytes(RecordBytes.Flip(
            good.ToBytes(),
            RecordBytes.GhostIn(good) + RecordBytes.GhostMapHashOffset));

        RetiredRecordException thrown = Assert.Throws<RetiredRecordException>(
            () => bent.RestageUnderCurrentRules(types, TheRuleset.Committed()));

        Assert.Equal("map hash", thrown.Gate);
    }

    [Fact]
    public void A_command_stream_that_passes_both_gates_plays_the_run_it_recorded()
    {
        // The command stream's half of the first assertion in this file: the
        // bytes reproduce the run, round for round, through the same two gates
        // a bundle goes through.
        //
        // OBSERVED: have CommandStream.Replay hand back its empty list of rounds
        // without the loop that plays the commands. This goes red, 4 against 0
        // -- a gate that opened onto nothing, reporting a run that never
        // started.
        CommandStream stream = CommandStream.FromBytes(TheCommands.Stream().ToBytes());
        Run run = TheCommands.Fresh();

        IReadOnlyList<RoundReport> rounds = stream.Replay(run);

        Assert.Equal(TheCommands.Waves, rounds.Count);
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
            Assert.Throws<RetiredRecordException>(() => stream.Replay(against));

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

        Run committed = TheCommands.Fresh();
        Run against = TheCommands.Against(reformatted);

        CommandStream.FromBytes(TheCommands.Stream().ToBytes()).Replay(committed);
        CommandStream.FromBytes(TheCommands.Stream().ToBytes()).Replay(against);

        Assert.Equal(committed.Outcome.Rounds, against.Outcome.Rounds);
        Assert.Equal(committed.Outcome.HealthRemaining, against.Outcome.HealthRemaining);
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
            Assert.Throws<RetiredRecordException>(() => stream.Replay(against));

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
            () => older.Replay(TheCommands.Fresh()));

        Assert.Equal("simulation version", version.Gate);

        // And the unit table, which the shared header has always carried.
        UnitTypeTable retuned = TheMatch.RetunedTypes();
        Run against = new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            retuned,
            TheSchedule.Committed(retuned),
            TheRun.Pool(retuned),
            TheBuild.Standing(retuned),
            TheRun.Seed,
            TheCommands.Waves,
            fieldSize: 2);

        RetiredRecordException content = Assert.Throws<RetiredRecordException>(
            () => CommandStream.FromBytes(TheCommands.Bytes()).Replay(against));

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
            () => stream.Replay(TheCommands.Against(TheRuleset.Retuned())));

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
