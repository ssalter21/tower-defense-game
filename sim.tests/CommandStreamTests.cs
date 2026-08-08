using System.Reflection;

namespace Sim.Tests;

/// <summary>
/// The command stream: a run's build phases as <c>(wave index, decision)</c>
/// pairs, and the run that consumes them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The shape is the point.</b> The view emits a command, the command goes
/// into the record, and the record is what the run consumes -- so a played run
/// is a replayable record and every playtest is also a determinism test. The
/// assertions below are about that route: what the record carries, that nothing
/// is lost on the way through it, and that there is no second door.
/// </para>
/// <para>
/// <b>Every refusal is asserted by name</b>, for the reason
/// <see cref="BuildPhaseTests"/> gives: a suite that only asserted "it threw"
/// passes just as well when a stream is refused for the wrong reason.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class CommandStreamTests
{
    [Fact]
    public void A_fourth_record_kind_holds_wave_index_and_decision_pairs()
    {
        // Its own magic, its own format version counter, and a layout in which
        // every byte is accounted for: the shared header, the two content
        // hashes this kind adds, the seed, the count, and the pairs.
        //
        // OBSERVED: drop the schedule hash -- take it out of ToBytes, out of the
        // size, out of ReadVersion0 and out of the gate. The schedule assertion
        // goes red, 8851628563585393197 against 20260807, because what sits at
        // that offset is now the seed. The bytes it leaves behind read and
        // replay perfectly, which is what a stamp nobody carries looks like from
        // the outside.
        Run run = TheCommands.Fresh();
        CommandStream stream = TheCommands.Stream(run);
        byte[] bytes = stream.ToBytes();

        Assert.Equal("CMDS", RecordFormat.MagicOf(RecordKind.Command));
        Assert.Equal((byte)'C', bytes[0]);
        Assert.Equal((byte)'M', bytes[1]);
        Assert.Equal((byte)'D', bytes[2]);
        Assert.Equal((byte)'S', bytes[3]);

        Assert.Equal(RecordFormat.CommandVersion, BitConverter.ToUInt16(bytes, RecordBytes.FormatVersionOffset));
        Assert.Equal(SimulationVersion.Current, BitConverter.ToUInt32(bytes, RecordBytes.SimVersionOffset));
        Assert.Equal(run.Types.ContentHash.Value, BitConverter.ToUInt64(bytes, RecordBytes.ContentHashOffset));
        Assert.Equal(run.Rules.ContentHash.Value, BitConverter.ToUInt64(bytes, RecordBytes.CommandRulesetHashOffset));
        Assert.Equal(
            run.Schedule.ContentHash.Value,
            BitConverter.ToUInt64(bytes, RecordBytes.CommandScheduleHashOffset));
        Assert.Equal(run.Seed, BitConverter.ToUInt64(bytes, RecordBytes.CommandSeedOffset));

        // Header, ruleset hash, schedule hash, seed, count, and four build
        // phases of one slot each. Nothing else fits.
        Assert.Equal(TheCommands.Waves, stream.Count);
        Assert.Equal(
            RecordFormat.HeaderBytes
            + 8
            + 8
            + 8
            + 2
            + (TheCommands.Waves * (RecordFormat.CommandBytes + RecordFormat.SlotBytes)),
            bytes.Length);

        // And a pair is a wave index and a decision, in that order.
        for (int index = 0; index < stream.Count; index++)
        {
            Assert.Equal(index + 1, stream.Commands[index].Wave);
            Assert.Single(stream.Commands[index].Slots);
        }
    }

    [Fact]
    public void A_decision_survives_the_round_trip_with_nothing_reshaped()
    {
        // The three things a decision is -- the take's kind, the take's id and
        // the slots -- are the three things the record carries, so the phase
        // that comes back out is the phase that went in. A record that summarised
        // a decision would be a record that replays something else.
        //
        // OBSERVED: have RecordCommand.ToPhase drop the slots -- call
        // BuildPhase.Of(Take, TakeId) with none. The slots assertion goes red,
        // [empty, 3 of type 5] against [], and a stored run replays as a run
        // that took the right things every round and sent nothing at all.
        Run run = TheCommands.Fresh();
        BuildPhase decided = BuildPhase.Of(
            OptionKind.Ordinary,
            run.Offering.Options[0].Id,
            WaveSlot.Empty,
            WaveSlot.Of(run.Offering.Options[0].TypeId, 3));

        RecordCommand command = RecordCommand.Of(1, decided);
        BuildPhase restored = CommandStream.FromBytes(TheCommands.Stream(run).ToBytes())
            .Commands[0]
            .ToPhase();

        Assert.Equal(decided.Take, command.ToPhase().Take);
        Assert.Equal(decided.TakeId, command.ToPhase().TakeId);
        Assert.Equal(decided.Slots, command.ToPhase().Slots);

        Assert.Equal(run.Offering.Options[0].Id, restored.TakeId);
        Assert.Single(restored.Slots);
    }

    [Fact]
    public void The_run_consumes_the_record_and_gets_the_run_the_decisions_played()
    {
        // The whole point of the kind, in one assertion: the bytes reproduce the
        // run, round for round -- the same outcome vector, the same waves sent,
        // the same unlocks and the same purse -- and the decisions went through
        // a serialisation in between.
        //
        // OBSERVED: play the stream backwards -- reverse the loop in
        // CommandStream.Replay. This goes red on an exception rather than on a
        // comparison: "A build phase at wave 2 takes ordinary option 5, which
        // that round's offering does not carry", thrown by the run after it had
        // already resolved wave four's decision as round one. The walk passed,
        // because a walk in the stored order is a walk of a legal stream; what
        // the order is load-bearing for is the playing.
        Run recorded = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(recorded);
        TowerLayout defense = TheCommands.Defense();

        Run live = TheCommands.Fresh();

        for (int index = 0; index < decisions.Count; index++)
        {
            live.Advance(decisions[index].ToPhase(), defense);
        }

        Run fromRecord = TheCommands.Fresh();
        RunOutcome outcome = CommandStream
            .FromBytes(CommandStream.Of(recorded, decisions).ToBytes())
            .Replay(fromRecord, defense);

        Assert.Equal(live.Outcome.Rounds, outcome.Rounds);
        Assert.Equal(live.Health, fromRecord.Health);
        Assert.Equal(live.Purse.Gold, fromRecord.Purse.Gold);
        Assert.Equal(live.Unlocks.Count, fromRecord.Unlocks.Count);
        Assert.Equal(TheCommands.Waves, fromRecord.Round);

        for (int index = 0; index < live.Sent.Count; index++)
        {
            Assert.Equal(live.Sent[index].Wave.TotalUnits, fromRecord.Sent[index].Wave.TotalUnits);
            Assert.Equal(live.Sent[index].Wave.Count, fromRecord.Sent[index].Wave.Count);
        }
    }

    [Fact]
    public void Nothing_reaches_a_run_or_a_match_except_as_a_value_a_record_can_carry()
    {
        // The structural half of "no input reaches the simulation". A run moves
        // forward through two overloads of one name and nothing else, and every
        // parameter of every public member of both surfaces is a defense, a
        // wave, a decision or a number. There is no delegate to call back into,
        // no reader, no path, and the one interface either of them accepts is
        // the decorative event listener, whose every method returns void -- so
        // it can be told things and can answer nothing.
        //
        // OBSERVED: add `public RoundOutcome Advance(Func<Offering, BuildPhase>
        // choose, TowerLayout defense)` to Run, which is exactly the shape a
        // view would reach for. The member list goes red naming it --
        // "Advance(Func`2, TowerLayout)" at position 1 -- and a run that asks a
        // caller what to do mid-round is a run whose input never went through a
        // record and never could.
        //
        // OBSERVED, on the decision surface below: add a fourth public property
        // to BuildPhase. It goes red naming the new member, which is the case
        // that matters -- a decision carrying something the record has no field
        // for is a decision that reaches a run and cannot be written down.
        string[] carried = typeof(BuildPhase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Everything a build phase is, and every one of the three is a field of
        // a stored command. So a decision handed straight to Advance is a
        // decision a command could have carried, and the direct overload is the
        // record's own shape rather than a way around it.
        Assert.Equal(new[] { "Slots", "Take", "TakeId" }, carried);

        string[] moves = typeof(Run)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name + "(" + string.Join(
                ", ",
                method.GetParameters().Select(parameter => parameter.ParameterType.Name)) + ")")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "Advance(BuildPhase, TowerLayout)", "Advance(RoundOrders)", "OfferingAt(Int32)" },
            moves);

        foreach (Type surface in new[] { typeof(Run), typeof(Match) })
        {
            foreach (MethodInfo method in surface.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                foreach (ParameterInfo parameter in method.GetParameters())
                {
                    Type taken = parameter.ParameterType;

                    Assert.False(
                        typeof(Delegate).IsAssignableFrom(taken),
                        surface.Name + "." + method.Name + " takes a delegate, which is a route into it.");

                    if (!taken.IsInterface)
                    {
                        continue;
                    }

                    Assert.Equal(typeof(IMatchEvents), taken);
                    Assert.All(
                        typeof(IMatchEvents).GetMethods(),
                        listener => Assert.Equal(typeof(void), listener.ReturnType));
                }
            }
        }
    }

    [Fact]
    public void A_take_naming_an_option_that_rounds_offering_did_not_carry_is_refused_at_load()
    {
        // Refused where the stream is read rather than where the round is
        // played, and refused rather than skipped: a run that partially
        // validates produces a confidently wrong result that still looks like
        // one. The run has not moved when it comes back.
        //
        // OBSERVED: delete the Check call from CommandStream.Replay. The refusal
        // still fires and it still names the option, so a suite that stopped at
        // the message would stay green; the round-count assertion goes red, 0
        // against 3. Three rounds of a stored run resolved and folded into an
        // outcome before the fourth was found to be a decision nobody could have
        // made -- which is the whole difference between refusing at load and
        // refusing wherever the run happens to get to.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);
        Offering last = run.OfferingAt(TheCommands.Waves);
        int absent = last.Options.Max(option => option.Id) + 1;

        CommandStream stream = Recomposed(
            run,
            decisions,
            TheCommands.Waves - 1,
            RecordCommand.Of(TheCommands.Waves, OptionKind.Ordinary, absent, WaveSlot.Empty));

        Run into = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => stream.Replay(into, TheCommands.Defense()));

        Assert.Contains("which that round's offering does not carry", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, into.Round);
        Assert.Equal(0, into.Unlocks.Count);
    }

    [Fact]
    public void A_slot_naming_a_creep_the_run_never_unlocked_is_refused_at_load()
    {
        // The unlock gate, reached through the record. What may be fielded is
        // bounded by what was taken, and a gate that let one stored purchase
        // through is a gate nobody has.
        //
        // OBSERVED: drop the after.Has check in BuildPhase.Resolve, which is the
        // gate this reaches through. This goes red on the message rather than on
        // the throw: what fires instead is "Type id 1 has no unit row among 1
        // taken", one layer later, from a guard whose own comment says the case
        // cannot happen. Asserting by name is what keeps a missing gate from
        // reading as an internal contradiction.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);
        Offering opening = run.Offering;
        int never = TheMatch.Types().Types
            .First(type => type.Role == UnitRole.Moving && type.Id != opening.Options[0].TypeId)
            .Id;

        CommandStream stream = Recomposed(
            run,
            decisions,
            0,
            RecordCommand.Of(1, opening.Options[0].Kind, opening.Options[0].Id, WaveSlot.Of(never, 1)));

        Run into = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => stream.Replay(into, TheCommands.Defense()));

        Assert.Contains("which this run never unlocked", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, into.Round);
    }

    [Fact]
    public void A_slot_beyond_the_rounds_width_is_refused_at_load()
    {
        // Slot width is the scarcity that stands in for a second wallet, and it
        // is derived from the anchor schedule. Wave one has two slots on the
        // committed shape, so a stored decision filling three is a wave nobody
        // could have composed.
        //
        // OBSERVED: drop the width check in BuildPhase.Resolve, which is the
        // gate this reaches through. This goes red having caught nothing -- no
        // exception was thrown -- and a stored wave-one build phase fills three
        // slots in a round the schedule gave two.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);
        Offering opening = run.Offering;

        Assert.Equal(2, opening.WaveSlots);

        CommandStream stream = Recomposed(
            run,
            decisions,
            0,
            RecordCommand.Of(
                1,
                opening.Options[0].Kind,
                opening.Options[0].Id,
                WaveSlot.Empty,
                WaveSlot.Empty,
                WaveSlot.Empty));

        Run into = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => stream.Replay(into, TheCommands.Defense()));

        Assert.Contains("slots where that round has 2", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, into.Round);
    }

    [Fact]
    public void A_stored_wave_index_that_is_not_the_round_being_played_is_refused()
    {
        // The one check the build phase surface cannot make. Resolve is handed
        // an offering and has no way to know which round is about to be played,
        // so a decision made at wave four and stored at wave two resolves
        // perfectly against wave two's menu -- against a menu it was never
        // shown, at a slot width it never had, out of a purse it never held.
        //
        // OBSERVED: drop the wave-index check from CommandStream.Check. This
        // goes red having caught nothing -- no exception was thrown -- and the
        // stream below plays three rounds and comes back with an outcome, one
        // of whose rounds was decided against a menu it was never shown. That
        // is precisely the confidently wrong result this surface exists to make
        // impossible.
        Run run = TheCommands.Fresh();
        var decisions = new List<RecordCommand>(TheCommands.Decisions(run));

        // A stream of three build phases whose third is filed under wave five.
        // The bytes are canonical -- the waves ascend strictly -- and the run
        // is about to play round three.
        Option fifth = run.OfferingAt(5).Options[0];

        decisions.RemoveAt(3);
        decisions[2] = RecordCommand.Of(5, fifth.Kind, fifth.Id, WaveSlot.Empty);

        Run into = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => CommandStream.Of(run, decisions).Replay(into, TheCommands.Defense()));

        Assert.Contains(
            "is stored for wave 5 where the run is about to play round 3",
            thrown.Message,
            StringComparison.Ordinal);
        Assert.Equal(0, into.Round);
    }

    [Fact]
    public void A_stream_holding_more_build_phases_than_the_run_has_rounds_is_refused()
    {
        // Refused before the first round rather than at the round that has
        // nowhere to go. A run that resolved four of six commands and then
        // refused has an outcome, and an outcome is a thing somebody keeps.
        //
        // OBSERVED: delete the RequireRoundsLeftFor call from Check. This goes
        // red on the message rather than on the throw: what fires instead is
        // "This run is over: 4 rounds resolved and 1...", from Run.Advance,
        // after four rounds of a six-round stream had already been resolved and
        // folded into a vector.
        Run run = TheCommands.Fresh(waves: 6);
        CommandStream stream = CommandStream.Of(run, TheCommands.Decisions(run, waves: 6));

        Run shorter = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => stream.Replay(shorter, TheCommands.Defense()));

        Assert.Contains("holds 6 build phases and the run has 4 rounds left", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, shorter.Round);
    }

    [Fact]
    public void A_stream_held_up_against_a_run_it_does_not_store_is_refused_by_its_seed()
    {
        // Every offering, filling and field in a run is derived from its seed,
        // so a stream played into a differently seeded run is a set of decisions
        // read off somebody else's menus. It refuses differently from the
        // content gates because it is not a record that has gone historical.
        //
        // OBSERVED: drop the seed check from CommandStream.Replay. This goes red
        // on the message: what fires instead is "A build phase at wave 2 takes
        // ordinary option...", from the offering check, which sends whoever
        // reads it looking at the commands rather than at the run they handed
        // in. It also only fires at all because two seeds happened to draw
        // different menus.
        CommandStream stream = TheCommands.Stream();
        Run elsewhere = TheCommands.Fresh(seed: TheRun.Seed + 1);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => stream.Replay(elsewhere, TheCommands.Defense()));

        Assert.Contains("these are two different runs", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, elsewhere.Round);
    }

    [Fact]
    public void A_stream_is_read_back_and_replayed_before_its_bytes_are_handed_over()
    {
        // The rule the record verb of the command line already follows: nothing
        // is written that will not replay. The bytes come back only after they
        // have been parsed from their own output, taken through the gate and
        // played to the end, so a stored stream that cannot be played is a thing
        // that never reaches a disk.
        //
        // OBSERVED: have Recorded return Of(run, commands).ToBytes() without the
        // FromBytes and Replay in between. The first assertion goes red, 4
        // against 0: the bytes come back having proved nothing, and the run they
        // were recorded into has not played a round.
        //
        // OBSERVED, on the second half: wrap Recorded's Replay in a catch that
        // swallows a SimulationException. The good stream still plays, so the
        // first three assertions stay green; Assert.Throws goes red having
        // caught nothing, and a stream that refuses to play is written out
        // anyway -- which is the whole failure this surface exists to prevent.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);

        byte[] bytes = CommandStream.Recorded(run, TheCommands.Defense(), decisions);

        Assert.Equal(TheCommands.Waves, run.Round);
        Assert.Equal(CommandStream.Of(TheCommands.Fresh(), decisions).ToBytes(), bytes);
        Assert.Equal(CommandStream.FromBytes(bytes).Commands, decisions);

        // And a stream that will not replay yields no bytes at all.
        Run doomed = TheCommands.Fresh();
        var wrong = new List<RecordCommand>(decisions)
        {
            RecordCommand.Of(TheCommands.Waves + 1, OptionKind.Ordinary, 1, WaveSlot.Empty),
        };

        Assert.Throws<SimulationException>(
            () => CommandStream.Recorded(doomed, TheCommands.Defense(), wrong));

        Assert.Equal(0, doomed.Round);
    }

    [Fact]
    public void The_load_walk_folds_the_unlocks_the_way_a_played_round_does_and_the_purse_at_its_ceiling()
    {
        // Check applies nothing, so it has to fold the two things a round moves
        // -- what has been taken and what is in the purse -- through values of
        // its own. The unlocks it predicts are the run's exactly; the purse
        // cannot be, because a wave's income now includes the band its offense
        // reached and a walk has not played the round. So the walk closes every
        // wave at the top band, and what it carries is a ceiling: strictly above
        // the run's own purse for a run that did not top every band, which is
        // this one.
        //
        // OBSERVED: fold the purse forward without the wave's payment -- assign
        // build.Purse straight to purse in Check. This goes red on an exception:
        // "A build phase at wave 4 buys 90 gold of creeps out of a purse
        // holding 52", refusing at load a wave the run affords perfectly well,
        // because the walk stopped paying the rounds it was walking.
        //
        // The ceiling is observed by what it admits. This run holds 357 gold
        // when its fourth build phase stands and the walk carries 423 -- three
        // waves of the top band it did not reach, and the interest on them --
        // so a fourth wave costing 400 is a decision the walk has to let past
        // and the round itself has to refuse. That ordering is the whole design:
        // everything refused at load was unaffordable however the run played.
        //
        // OBSERVED, on the ceiling: close the walk's waves at
        // CloseWave(run.Rules, PerformanceField.Absent, 0) instead. The Check
        // above goes red on an exception -- "A build phase at wave 4 buys 400
        // gold of creeps out of a purse holding 357" -- refusing at load rather
        // than at the round, which is what a floor does to every decision a
        // run's own bonus paid for.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);
        CommandStream stream = CommandStream.Of(run, decisions);

        IReadOnlyList<Build> walked = stream.Check(TheCommands.Fresh());

        Run played = TheCommands.Fresh();
        stream.Replay(played, TheCommands.Defense());

        Assert.Equal(TheCommands.Waves, walked.Count);
        Assert.Equal(played.Unlocks.Count, walked[walked.Count - 1].Unlocks.Count);

        Option fourth = run.OfferingAt(TheCommands.Waves).Options[0];
        var overspent = new List<RecordCommand>(decisions)
        {
            [TheCommands.Waves - 1] = RecordCommand.Of(
                TheCommands.Waves,
                BuildPhase.Of(fourth.Kind, fourth.Id, WaveSlot.Of(fourth.TypeId, 40))),
        };

        Assert.Equal(10, run.Costs.PriceOf(Purchase.Unit(fourth.TypeId)));

        CommandStream beyond = CommandStream.Of(run, overspent);

        beyond.Check(TheCommands.Fresh());

        SimulationException refused = Assert.Throws<SimulationException>(
            () => beyond.Replay(TheCommands.Fresh(), TheCommands.Defense()));

        Assert.Contains("400", refused.Message, StringComparison.Ordinal);

        // The walk moved nothing. A stream can be checked and then refused
        // without the run having taken a step. No mutation is written above
        // these three, and the reason is that they are guarded by construction:
        // Check is handed no defense to play a round with, and a run's purse and
        // unlocks have private setters, so nothing outside a run can move one.
        // They are here to go red the day either of those stops being true.
        Run untouched = TheCommands.Fresh();
        stream.Check(untouched);

        Assert.Equal(0, untouched.Round);
        Assert.Equal(0, untouched.Unlocks.Count);
        Assert.Equal(TheRuleset.Committed().StartingPurseGold, untouched.Purse.Gold);
    }

    /// <summary>
    /// The same decisions with one of them replaced, recorded against the run
    /// they were composed for. Building the whole stream and swapping a member
    /// keeps every other check passing, so the assertion is about the one thing
    /// that moved.
    /// </summary>
    private static CommandStream Recomposed(
        Run run,
        IReadOnlyList<RecordCommand> decisions,
        int index,
        RecordCommand replacement)
    {
        var commands = new List<RecordCommand>(decisions);
        commands[index] = replacement;

        return CommandStream.Of(run, commands);
    }
}
