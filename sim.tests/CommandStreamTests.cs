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
            run.Ladder.ContentHash.Value,
            BitConverter.ToUInt64(bytes, RecordBytes.CommandLadderHashOffset));
        Assert.Equal(run.Seed, BitConverter.ToUInt64(bytes, RecordBytes.CommandSeedOffset));

        // Header, ruleset hash, ladder hash, seed, count, and four build
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
        // What a decision is -- the slots, in the order they were written, and
        // what the phase built -- is what the record carries, so the phase that
        // comes back out is the phase that went in. A record that summarised a
        // decision would be a record that replays something else.
        //
        // The take used to be two of these fields and is gone with the offering
        // it named. An empty slot is still carried rather than dropped, because
        // a position a player left alone is a position and not an omission.
        //
        // OBSERVED: have RecordCommand.ToPhase drop the slots -- call
        // BuildPhase.Of() with none. The slots assertion goes red,
        // [empty, 3 of type 1] against [], and a stored run replays as a run
        // that sent nothing at all.
        Run run = TheCommands.Fresh();
        UnitType first = TheBuild.FirstCreep(run.Types);
        BuildPhase decided = BuildPhase.Of(WaveSlot.Empty, WaveSlot.Of(first.Id, 3));

        RecordCommand command = RecordCommand.Of(1, decided);
        BuildPhase restored = CommandStream.FromBytes(TheCommands.Stream(run).ToBytes())
            .Commands[0]
            .ToPhase();

        Assert.Equal(decided.Slots, command.ToPhase().Slots);
        Assert.Equal(2, command.ToPhase().Slots.Count);

        Assert.Single(restored.Slots);
    }

    [Fact]
    public void What_a_phase_built_survives_the_round_trip_through_bytes_in_the_order_it_was_written()
    {
        // The other half of a decision, which format version 1 is the bump for:
        // what the round took, and what it built. The actions come back in the
        // order they went in -- a phase may upgrade what it has just placed, so
        // the sequence is meaning rather than spelling -- and writing what was
        // read produces the same bytes, which is the round trip asserted on the
        // current format.
        //
        // OBSERVED: write the action count and skip the action loop in ToBytes.
        // The length assertion goes red, 110 against 96, on a stream that says
        // it built two things and carries neither.
        //
        // OBSERVED: reverse the actions as ReadActions returns them. The length
        // stays green, because the same seven bytes came back either way, and
        // the comparison against the decisions that went in goes red naming
        // both commands in full -- a phase upgrading a cell before it stood
        // anything on it.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Acting(run);
        byte[] bytes = CommandStream.Of(run, decisions).ToBytes();

        // Two actions on the first phase and none on the other three, so the
        // action run is exactly what the length grew by.
        Assert.Equal(
            RecordFormat.HeaderBytes
            + 8
            + 8
            + 8
            + 2
            + (TheCommands.Waves * (RecordFormat.CommandBytes + RecordFormat.SlotBytes))
            + (2 * RecordFormat.ActionBytes),
            bytes.Length);

        CommandStream read = CommandStream.FromBytes(bytes);

        Assert.Equal(RecordFormat.CommandVersion, read.Header.FormatVersion);
        Assert.Equal(decisions, read.Commands);
        Assert.Equal(bytes, read.ToBytes());

        Assert.Equal(new[] { TheCommands.Placed, TheCommands.Upgraded }, read.Commands[0].Actions);
        Assert.Empty(read.Commands[1].Actions);

        // And the same two the other way round are different bytes, because
        // nothing here sorts them: two orderings of one pair of actions are two
        // runs rather than two spellings of one.
        //
        // OBSERVED, on this clause: sort the actions by type id as ToBytes
        // writes them. Everything above stays green -- the place is type 3 and
        // the upgrade type 4, so sorting leaves the forward order alone -- and
        // this goes red, the two orderings having become one set of bytes.
        var reversed = new List<RecordCommand>(TheCommands.Decisions(run));
        reversed[0] = reversed[0].With(TheCommands.Upgraded).With(TheCommands.Placed);

        Assert.NotEqual(bytes, CommandStream.Of(run, reversed).ToBytes());
    }

    [Fact]
    public void A_decision_prints_its_actions_before_the_slots()
    {
        // The decision half of a round line, which a run's report prints
        // whole: what the phase built in the order it was written, then the
        // wave's slots. One line, because the column header over it promises
        // one per round.
        //
        // The cell is the column and row an action row of a command script
        // names, so what is printed reads back into the file a person would
        // write.
        //
        // OBSERVED: print the actions after the slots in
        // RecordCommand.ToString. The action assertion goes red -- the run no
        // longer has the slots' comma behind it -- on a line reading slots then
        // actions, which is an order no purse ever walked.
        //
        // OBSERVED, on the spelling: swap the two coordinates for
        // Hex.FromOddRowOffset(Column, Row) in BuildAction.ToString. The
        // action assertion goes red, "at column 9, row 0" having become
        // "at (9, 0)" -- and on any row but the top the numbers move as well,
        // so one cell would be one pair here and another in
        // content/defense.txt.
        RecordCommand acting = TheCommands.Acting(TheCommands.Fresh())[0];
        string line = acting.ToString();

        Assert.Contains(
            "place type 3 at column 9, row 0, upgrade type 4 at column 9, row 0, ",
            line,
            StringComparison.Ordinal);

        Assert.True(
            line.IndexOf("place type 3", StringComparison.Ordinal)
                < line.IndexOf("upgrade type 4", StringComparison.Ordinal)
            && line.IndexOf("upgrade type 4", StringComparison.Ordinal)
                < line.IndexOf(" of type ", StringComparison.Ordinal),
            line + " does not put its actions before its slots.");
    }

    [Fact]
    public void The_run_consumes_the_record_and_gets_the_run_the_decisions_played()
    {
        // The whole point of the kind, in one assertion: the bytes reproduce the
        // run, round for round -- the same outcome vector, the same waves sent
        // and the same purse -- and the decisions went through a serialisation
        // in between.
        //
        // OBSERVED: play the stream backwards -- reverse the loop in
        // CommandStream.Replay. This goes red on an exception rather than on a
        // comparison: "A build phase is stored for wave 4 where the run is
        // about to play round 1", thrown by the run's own wave-index check. The
        // walk passed, because a walk in the stored order is a walk of a legal
        // stream; what the order is load-bearing for is the playing.
        Run recorded = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(recorded);

        Run live = TheCommands.Fresh();

        for (int index = 0; index < decisions.Count; index++)
        {
            live.Advance(decisions[index].ToPhase());
        }

        Run fromRecord = TheCommands.Fresh();
        IReadOnlyList<RoundReport> rounds = CommandStream
            .FromBytes(CommandStream.Of(recorded, decisions).ToBytes())
            .Replay(fromRecord);

        Assert.Equal(live.Outcome.Rounds, fromRecord.Outcome.Rounds);
        Assert.Equal(live.Outcome.Rounds, rounds.Select(round => round.Outcome));
        Assert.Equal(live.Health, fromRecord.Health);
        Assert.Equal(live.Purse.Gold, fromRecord.Purse.Gold);
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
        // forward through one method and nothing else, and every parameter of
        // every public member of both surfaces is a decision, a defense a
        // record carries or a number. There is no delegate to call back into,
        // no reader, no path, and the one interface either of them accepts is
        // the decorative event listener, whose every method returns void -- so
        // it can be told things and can answer nothing.
        //
        // The one route in is a fact about the type rather than about this
        // list: a wave a view composed cannot be handed to a run at all, because
        // the only thing Advance takes is the decision a command carries. What
        // the list still catches is a second route being added beside it.
        //
        // MatchAt is on the list and is not one. #192 gave it to the client so
        // a round that has already resolved can be drawn, and its whole
        // signature is two integers naming a pairing the run has already fought
        // and a bool naming which of that pairing's two directions: there is
        // nothing in it for a view to compose, and what comes back is a copy of
        // a fight rather than a handle on the run --
        // RunTests.Watching_a_round_moves_nothing is the behavioural half of
        // that. It is here because the list is what makes admitting a member
        // deliberate.
        //
        // The bool arrived with #206 and is a primitive on purpose, which is
        // this page's rule rather than a preference: the sim's Side enum is
        // private, and handing it out would put a simulation type on the one
        // surface this test exists to keep made of things a stored command
        // could carry. It also has a third member -- the stream the field is
        // measured on -- which is not a fight anybody watched, and two
        // directions spelled as two values cannot name it.
        //
        // OBSERVED: add `public RoundOutcome Advance(Func<Offering, BuildPhase>
        // choose)` to Run, which is exactly the shape a view would reach for.
        // The member list goes red naming it -- "Advance(Func`2)" at position 1
        // -- and a run that asks a caller what to do mid-round is a run whose
        // input never went through a record and never could.
        //
        // OBSERVED: put `public RoundOutcome Advance(RoundOrders orders)` back
        // on Run -- the route this suite was written around, which took a wave
        // nobody was charged for. The list goes red naming it at position 1.
        //
        // OBSERVED: hand the defense back in -- `Advance(BuildPhase phase,
        // TowerLayout defense)`, the shape this took before the run owned its
        // board. The list goes red at position 0, and a defense composed by
        // anybody and applied against no map is back inside the tick loop.
        //
        // OBSERVED, on the decision surface below: add a fifth public property
        // to BuildPhase. It goes red naming the new member, which is the case
        // that matters -- a decision carrying something the record has no field
        // for is a decision that reaches a run and cannot be written down.
        string[] carried = typeof(BuildPhase)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        // Everything a build phase is, and every one of the four is a field of
        // a stored command. So a decision handed straight to Advance is a
        // decision a command could have carried, and the direct overload is the
        // record's own shape rather than a way around it.
        Assert.Equal(new[] { "Actions", "Slots" }, carried);

        string[] moves = typeof(Run)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Select(method => method.Name + "(" + string.Join(
                ", ",
                method.GetParameters().Select(parameter => parameter.ParameterType.Name)) + ")")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            new[] { "Advance(BuildPhase)", "MatchAt(Int32, Int32, Boolean)" },
            moves);

        // And Advance is still the only one of them that takes anything a view
        // could have composed. A member that took a wave, a layout or a board
        // would be a second route in whatever it claimed to be for; two integers
        // naming a pairing are a question about a round that already happened.
        //
        // It fires only once the list above has been updated, because any
        // signature at all trips the list first. That is what it is for: the
        // list makes admitting a member deliberate, and this decides what may
        // be admitted.
        //
        // OBSERVED: add `public Match Watch(TowerLayout defense)` to Run -- the
        // shape a view would reach for, putting a defense nobody paid for in
        // front of a tick loop -- and admit it to the list above, as somebody
        // adding it would. This goes red: "Run.Watch takes a TowerLayout, so
        // something a view composed reaches a run by a route that is not
        // Advance."
        foreach (MethodInfo reader in typeof(Run)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName && method.Name != nameof(Run.Advance)))
        {
            foreach (ParameterInfo parameter in reader.GetParameters())
            {
                Assert.True(
                    parameter.ParameterType.IsPrimitive,
                    "Run." + reader.Name + " takes a " + parameter.ParameterType.Name
                    + ", so something a view composed reaches a run by a route that is not Advance.");
            }
        }

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
    public void A_stored_wave_index_that_is_not_the_round_being_played_is_refused()
    {
        // The one check the build phase surface cannot make. Resolve is handed
        // a wave number and has no way to know which round is about to be
        // played, so a decision made at wave four and stored at wave two
        // resolves perfectly -- out of a purse it never held.
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
        decisions.RemoveAt(3);
        decisions[2] = RecordCommand.Of(5, WaveSlot.Empty);

        Run into = TheCommands.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => CommandStream.Of(run, decisions).Replay(into));

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
            () => stream.Replay(shorter));

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
            () => stream.Replay(elsewhere));

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
        //
        // OBSERVED, on the rounds: hand back a fresh empty list of them beside
        // the bytes while still replaying. Everything about the bytes stays
        // green and the round count goes red, 4 against 0 -- proving the stream
        // by playing it and then throwing away what the playing worked out is
        // exactly how a caller ends up playing it a second time.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);

        (byte[] bytes, IReadOnlyList<RoundReport> rounds) =
            CommandStream.Recorded(run, decisions);

        Assert.Equal(TheCommands.Waves, run.Round);
        Assert.Equal(CommandStream.Of(TheCommands.Fresh(), decisions).ToBytes(), bytes);
        Assert.Equal(CommandStream.FromBytes(bytes).Commands, decisions);

        // The rounds the proving played come back with the bytes, so nothing
        // that wants what the stored run cost has to play it a second time.
        Assert.Equal(TheCommands.Waves, rounds.Count);
        Assert.Equal(run.Purse.Gold, rounds[rounds.Count - 1].Payment.Purse.Gold);

        // And a stream that will not replay yields no bytes at all.
        Run doomed = TheCommands.Fresh();
        var wrong = new List<RecordCommand>(decisions)
        {
            RecordCommand.Of(TheCommands.Waves + 1, WaveSlot.Empty),
        };

        Assert.Throws<SimulationException>(
            () => CommandStream.Recorded(doomed, wrong));

        Assert.Equal(0, doomed.Round);
    }

    [Fact]
    public void The_load_walk_folds_the_purse_forward_at_its_ceiling()
    {
        // Check applies nothing, so it has to fold what a round moves -- the
        // purse -- through a value of its own. It cannot fold it exactly,
        // because a wave's income includes a share of what its offense got past
        // and a walk has not played the round. So the walk closes every wave on
        // the most that wave could conceivably have dealt -- its own full price,
        // every creep of it leaking against every opponent -- and what it
        // carries is a ceiling: at or above the run's own purse, and strictly
        // above it for a run whose waves did not all leak whole, which is this
        // one.
        //
        // OBSERVED: fold the purse forward without the wave's payment -- assign
        // build.Purse straight to purse in Check. The ceiling collapses to 80
        // gold and the Check below goes red on an exception, "A build phase at
        // wave 4 buys 680 gold of creeps out of a purse holding 80": a walk
        // that stopped paying the rounds it was walking refuses at load what it
        // has no way of knowing the run could afford.
        //
        // The ceiling is observed by what it admits. This run holds 670 gold
        // when its fourth build phase stands and the walk carries 681 -- three
        // waves whose whole price it credited them with leaking -- so a fourth
        // wave costing 680 is a decision the walk has to let past and the round
        // itself has to refuse. That ordering is the whole design: everything
        // refused at load was unaffordable however the run played.
        //
        // OBSERVED, on the ceiling: close the walk's waves at
        // CloseWaveAtBest(run.Rules, 0) instead -- a bound of nothing dealt. The
        // walk then holds less than the 670 the run itself does, and the Check
        // below goes red -- "A build phase at wave 4 buys 680 gold of creeps
        // out of a purse holding" that lesser figure -- refusing at load rather than at
        // the round, which is what a floor does to every decision a run's own
        // bonus paid for.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);
        CommandStream stream = CommandStream.Of(run, decisions);

        IReadOnlyList<Build> walked = stream.Check(TheCommands.Fresh());

        Run played = TheCommands.Fresh();
        stream.Replay(played);

        Assert.Equal(TheCommands.Waves, walked.Count);

        // The walk moved the run nowhere: it is the played run beside it that
        // has a purse and rounds.
        Assert.Equal(TheCommands.Waves, played.Round);

        UnitType fourth = TheBuild.FirstCreep(run.Types);

        // What the last wave carries, with 68 more of one creep on the end of
        // it. The overspend has to be an increase over what the round already
        // fields, because a creep is bought once and only the increase is
        // charged -- and every carried slot has to stay in, because a wave may
        // only grow. Sixty-eight of them is 680 gold: over the 670 the round
        // holds and under the 681 the walk carries, which is the window the
        // ordering below is about and which the tighter ceiling narrowed.
        var overspent = new List<RecordCommand>(decisions)
        {
            [TheCommands.Waves - 1] = RecordCommand.Of(
                TheCommands.Waves,
                BuildPhase.Of(Adding(walked[TheCommands.Waves - 2].Wave, fourth.Id, 68))),
        };

        Assert.Equal(10, run.Costs.PriceOf(Purchase.Unit(fourth.Id)));

        CommandStream beyond = CommandStream.Of(run, overspent);

        beyond.Check(TheCommands.Fresh());

        // Admitted by the walk and refused by the round, which is what a
        // refusal surviving to mid-run looks like from the outside: three
        // rounds resolved and folded into an outcome before the fourth is
        // found to be a wave this run could not pay for. Affordability is the
        // only decision a stream can be refused for here, because everything
        // else the walk can settle it has settled.
        //
        // OBSERVED: the round count is what makes that a claim rather than a
        // comment. Assert 0 instead and it goes red, 3 against 0.
        Run partway = TheCommands.Fresh();

        SimulationException refused = Assert.Throws<SimulationException>(
            () => beyond.Replay(partway));

        Assert.Contains("680", refused.Message, StringComparison.Ordinal);
        Assert.Equal(TheCommands.Waves - 1, partway.Round);

        // The purse the round really held, which is the lower half of the
        // ordering: the walk let 680 past on a ceiling of 681 and the round
        // turned it down on 670. Asserted rather than described, so that a
        // change to either number is a red test and not a stale comment.
        Assert.Equal(670, partway.Purse.Gold);

        // And the upper half: a wave over the ceiling is refused at load, before
        // a round is played, because no run could have afforded it however well
        // it played. Seventy of the same creep is 700 gold against a walk
        // carrying 681 -- twenty more than the wave above, which is the width of
        // the window the whole ordering lives in.
        //
        // OBSERVED: close the walk's waves at CloseWaveAtBest(run.Rules,
        // 1000000) -- a bound nothing on this roster can reach. The Assert.Throws
        // below goes red saying no exception was thrown: an uncapped bonus with
        // no real bound on what a wave could deal admits every decision anybody
        // ever stored, which is the failure WaveScript.FullPrice exists to
        // prevent. A million rather than int.MaxValue because the latter closes
        // the walk's purse past the range gold is counted in and refuses for
        // arithmetic reasons instead.
        var unaffordable = new List<RecordCommand>(decisions)
        {
            [TheCommands.Waves - 1] = RecordCommand.Of(
                TheCommands.Waves,
                BuildPhase.Of(Adding(walked[TheCommands.Waves - 2].Wave, fourth.Id, 70))),
        };

        Run never = TheCommands.Fresh();

        SimulationException atLoad = Assert.Throws<SimulationException>(
            () => CommandStream.Of(run, unaffordable).Check(never));

        Assert.Contains("700 gold of creeps", atLoad.Message, StringComparison.Ordinal);
        Assert.Contains("purse holding 681", atLoad.Message, StringComparison.Ordinal);
        Assert.Equal(0, never.Round);

        // The walk moved nothing. A stream can be checked and then refused
        // without the run having taken a step. No mutation is written above
        // these three, and the reason is that they are guarded by construction:
        // a run's purse and board both have private setters, and the board the
        // walk folds is a value of its own, so nothing outside a run can move
        // one. They are here to go red the day that stops being true.
        Run untouched = TheCommands.Fresh();
        stream.Check(untouched);

        Assert.Equal(0, untouched.Round);
        Assert.Equal(TheRuleset.Committed().StartingPurseGold, untouched.Purse.Gold);
        Assert.Equal(0, untouched.Board.Count);
    }

    [Fact]
    public void The_load_walk_folds_the_board_forward_the_way_a_played_round_does()
    {
        // The third thing a round moves, folded through a value of the walk's
        // own beside the unlocks and the purse. Held against the run's opening
        // board instead, every phase is checked against what stands now rather
        // than against what the phase before it built -- which is wrong in both
        // directions, and both are asserted here.
        //
        // OBSERVED: hand run.Board to Resolve in Check rather than the folded
        // one. The upgrade below goes red on an exception -- "An upgrade names
        // column 0, row 0, where nothing stands" -- refusing at load a phase
        // upgrading a cell its own stream took two rounds earlier; and the
        // place-on-place below goes red having caught nothing at load, the run
        // instead refusing at round four with three rounds already resolved
        // and folded into an outcome.
        Run run = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = TheCommands.Decisions(run);

        // Wave two puts an archer on a cell the opening board leaves empty.
        var placing = new List<RecordCommand>(decisions)
        {
            [1] = decisions[1].With(TheCommands.PlacedOnFreeCell),
        };

        // Wave four upgrades it, which is only legal because wave two placed.
        var upgrading = new List<RecordCommand>(placing)
        {
            [3] = placing[3].With(TheCommands.UpgradedOnFreeCell),
        };

        CommandStream ahead = CommandStream.Of(run, upgrading);

        IReadOnlyList<Build> walked = ahead.Check(TheCommands.Fresh());

        Run played = TheCommands.Fresh();
        ahead.Replay(played);

        // One more placement than the run opened with, on both sides: the walk
        // ends on the board the play ends on.
        Assert.Equal(1, walked[walked.Count - 1].Board.Count);
        Assert.Equal(played.Board.Count, walked[walked.Count - 1].Board.Count);

        // And the other direction. Wave four places on the cell wave two took,
        // which the walk refuses before round one resolves.
        var twice = new List<RecordCommand>(placing)
        {
            [3] = placing[3].With(TheCommands.PlacedOnFreeCell),
        };

        CommandStream doubled = CommandStream.Of(run, twice);
        Run into = TheCommands.Fresh();

        SimulationException refused = Assert.Throws<SimulationException>(() => doubled.Replay(into));

        Assert.Contains("A build phase at wave 4 cannot act.", refused.Message, StringComparison.Ordinal);
        Assert.Contains("A place puts a second thing on", refused.Message, StringComparison.Ordinal);
        Assert.Equal(0, into.Round);
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

    [Fact]
    public void A_run_that_spends_a_capstone_token_replays_from_its_record()
    {
        // The token rides in the record without a byte of its own. What the
        // schedule granted is a function of the wave index the stream already
        // carries, and what a decision spent is a function of the actions it
        // already stores -- so a stored count would be a second copy of a
        // derivation, and there is none. The proof is that a stream whose third
        // round climbs a capstone comes back off its own bytes as the same run.
        //
        // OBSERVED: hand every phase the tokens the run opened with -- drop the
        // fold at the bottom of CommandStream.Check's loop and pass `tokens`
        // unchanged. The walk then admits a stream that climbs a capstone in
        // round one, and the round-one clause below goes red having caught
        // nothing.
        Run recorded = TheCommands.Fresh();
        IReadOnlyList<RecordCommand> decisions = Climbing(recorded, GrantRound);

        Run live = TheCommands.Fresh();

        for (int index = 0; index < decisions.Count; index++)
        {
            live.Advance(decisions[index].ToPhase());
        }

        Run fromRecord = TheCommands.Fresh();
        CommandStream.FromBytes(CommandStream.Of(recorded, decisions).ToBytes()).Replay(fromRecord);

        Assert.Equal(live.Outcome.Rounds, fromRecord.Outcome.Rounds);
        Assert.Equal(live.Purse.Gold, fromRecord.Purse.Gold);
        Assert.Equal(live.Board.Count, fromRecord.Board.Count);

        // One token granted at round three and one spent there, so both runs end
        // holding nothing -- and they hold nothing for the same reason, which is
        // the half a purse comparison cannot see.
        Assert.Equal(live.CapstoneTokens, fromRecord.CapstoneTokens);
        Assert.Equal(0, fromRecord.CapstoneTokens);

        // The same three actions one round too early. Nothing about the bytes
        // says a token was involved, so this is the walk deriving the schedule
        // off the wave index the stream stores and refusing before a round is
        // played.
        Run early = TheCommands.Fresh();

        Assert.Contains(
            "costs a capstone token the round does not hold",
            Assert.Throws<SimulationException>(
                () => CommandStream
                    .FromBytes(
                        CommandStream.Of(early, Climbing(early, GrantRound - 1)).ToBytes())
                    .Replay(TheCommands.Fresh()))
                .Message,
            StringComparison.Ordinal);
    }

    /// <summary>The first round <see cref="Run.CapstoneTokenRounds"/> grants at.</summary>
    private const int GrantRound = 3;

    /// <summary>The committed archer, the root of the line these decisions climb.</summary>
    private const int ArcherId = 3;

    /// <summary>Its second rung, bought with gold.</summary>
    private const int RangerId = 14;

    /// <summary>Its capstone, bought with the token.</summary>
    private const int OverwatchId = 31;

    /// <summary>A ground cell of the committed map these decisions build on.</summary>
    private const int FreeColumn = 0;

    /// <summary>Its row.</summary>
    private const int FreeRow = 0;

    /// <summary>
    /// The stream's usual decisions, with a whole line climbed to its top in one
    /// named round: place, upgrade, capstone, on one cell.
    /// </summary>
    private static IReadOnlyList<RecordCommand> Climbing(Run run, int wave)
    {
        var commands = new List<RecordCommand>(TheCommands.Decisions(run));

        commands[wave - 1] = commands[wave - 1]
            .With(BuildAction.Of(ActionKind.Place, ArcherId, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, RangerId, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, OverwatchId, FreeColumn, FreeRow));

        return commands;
    }

    /// <summary>
    /// A carried wave as slots, with <paramref name="more"/> extra of one creep
    /// in it. What a round that adds to its wave and gives nothing up looks
    /// like.
    /// </summary>
    private static WaveSlot[] Adding(WaveScript carried, int typeId, int more)
    {
        WaveSlot[] slots = carried.AsSlots();
        var grown = new List<WaveSlot>(slots);
        bool raised = false;

        for (int index = 0; index < grown.Count; index++)
        {
            if (grown[index].TypeId == typeId)
            {
                grown[index] = WaveSlot.Of(typeId, grown[index].Count + more);
                raised = true;
            }
        }

        if (!raised)
        {
            grown.Add(WaveSlot.Of(typeId, more));
        }

        return grown.ToArray();
    }
}
