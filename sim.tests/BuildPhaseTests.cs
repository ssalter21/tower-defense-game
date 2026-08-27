using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The build phase: a board built out of one purse, and a wave that keeps
/// everything it has ever bought.
/// </summary>
/// <remarks>
/// <para>
/// <b>The draw assertions are fought over the committed roster.</b> Six walkers
/// against three ordinary options is what makes a menu a draw rather than the
/// whole roster read back -- see <see cref="TheBuild"/>.
/// </para>
/// <para>
/// <b>Every refusal is asserted by name</b>, because a suite that only asserted
/// "it threw" would pass just as well when the build phase is refused for the
/// wrong reason, and a player reading "invalid build phase" learns nothing they
/// can act on.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class BuildPhaseTests
{
    /// <summary>The committed archer: a tower, and the type these assertions stand one of at the cell below.</summary>
    private const int Archer = 3;

    /// <summary>The committed mage: a tower, and the priciest row on the roster.</summary>
    private const int Mage = 4;

    /// <summary>The committed soldier: a tower whose one hex of range reaches the route from very few cells.</summary>
    private const int Soldier = 11;

    /// <summary>The committed minion: a creep, so a row no cell may hold.</summary>
    private const int Minion = 1;

    /// <summary>
    /// The committed ranger: the target of the one edge <c>content/upgrades.txt</c>
    /// carries, so the one row on the roster that may not be placed outright.
    /// </summary>
    private const int Ranger = 14;

    /// <summary>A ground cell of the committed map that these assertions leave empty.</summary>
    private const int FreeColumn = 0;

    /// <summary>Its row.</summary>
    private const int FreeRow = 0;

    /// <summary>The other ground cell: the one <see cref="Standing"/> puts an archer on.</summary>
    private const int StandingColumn = 6;

    /// <summary>Its row.</summary>
    private const int StandingRow = 2;

    /// <summary>How many creeps a slot sends where the assertion is about the bill rather than the wave.</summary>
    private const int Bodies = 3;

    [Fact]
    public void A_wave_is_any_number_of_slots_and_each_one_is_a_creep_type_and_a_count()
    {
        // Nothing rations how wide a wave may be. The slot count used to be
        // derived from how many anchors a run had passed, and a phase wider
        // than that was refused; #179 deleted the anchors and the width with
        // them, so what bounds a wave now is the purse and nothing else.
        //
        // OBSERVED: reinstate a width bound -- refuse a phase in
        // BuildPhase.Resolve whose filled slots outnumber two. The five-slot
        // half goes red on the refusal, and the purse stops being the only
        // thing a player is spending against.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        // A slot is one creep type plus a count, and the wave is what the
        // filled ones compose.
        Build built = Resolved(run, BuildPhase.Of(WaveSlot.Of(creeps[0], 4)), 1000);

        Assert.Equal(1, built.Wave.Count);
        Assert.Equal(4, built.Wave.TotalUnits);
        Assert.Equal(creeps[0], built.Wave.Orders[0].TypeId);

        // And five of them in one round, on a run one wave old, which no width
        // in the old rules would have allowed until wave nine.
        Assert.True(creeps.Length >= 5, "The roster is too thin for this to have proved anything.");

        Build wide = Resolved(
            run,
            BuildPhase.Of(
                WaveSlot.Of(creeps[0], 1),
                WaveSlot.Of(creeps[1], 1),
                WaveSlot.Of(creeps[2], 1),
                WaveSlot.Of(creeps[3], 1),
                WaveSlot.Of(creeps[4], 1)),
            1000);

        Assert.Equal(5, wide.Wave.Count);
        Assert.Equal(5, wide.Wave.TotalUnits);
    }

    [Fact]
    public void Sending_replaces_a_phases_wave_and_leaves_the_actions_it_already_carries_alone()
    {
        // The wave half of With. An action's position is the order it was
        // written in, so appending is the whole of that verb; a slot's position
        // is the release order, so a wave is rearranged and emptied as well as
        // grown and there is no one edit an append could stand for.
        //
        // It exists for the screen that composes a wave -- ADR-0051 -- which
        // otherwise had to rebuild a candidate phase out of Of() and a replay
        // of Actions, which is a view that knows how this class is assembled
        // and silently drops whatever a phase gains that those two do not
        // carry.
        //
        // OBSERVED: return `new BuildPhase(copied, NoActions)` instead, which
        // is what Of() does. The action assertions go red, and a wave edited on
        // screen quietly forgets every tower the same round placed.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        BuildPhase placing = BuildPhase.Of(WaveSlot.Of(creeps[0], 2))
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        BuildPhase resent = placing.Sending(WaveSlot.Of(creeps[1], 3), WaveSlot.Of(creeps[0], 1));

        Assert.Equal(new[] { WaveSlot.Of(creeps[1], 3), WaveSlot.Of(creeps[0], 1) }, resent.Slots);
        Assert.Single(resent.Actions);
        Assert.Equal(ActionKind.Place, resent.Actions[0].Kind);
        Assert.Equal(Archer, resent.Actions[0].TypeId);

        // And the phase it came from did not move. Every verb on this class
        // hands back a new one, which is what lets a candidate be resolved and
        // thrown away without the decision it was derived from changing.
        //
        // OBSERVED: keep the array the caller passed rather than copying it,
        // and mutate it after the call. This half goes red, and a composed
        // round's phase changes under a candidate nobody kept.
        Assert.Equal(new[] { WaveSlot.Of(creeps[0], 2) }, placing.Slots);

        // Resolving the result is resolving both halves: the tower stands and
        // the new wave is what leaves.
        Build built = Resolved(run, resent, 1000);

        Assert.Equal(1, built.Board.Count);
        Assert.Equal(2, built.Wave.Count);
        Assert.Equal(creeps[1], built.Wave.Orders[0].TypeId);
        Assert.Equal(creeps[0], built.Wave.Orders[1].TypeId);
        Assert.Equal(4, built.Wave.TotalUnits);

        // A wave of nothing is spelled by sending no slots at all, which is the
        // arrangement the screen's wave bar produces when every box is emptied.
        Assert.Empty(placing.Sending().Slots);
        Assert.Throws<ArgumentNullException>(() => placing.Sending(null!));
    }

    [Fact]
    public void A_slot_may_be_left_empty_and_an_empty_slot_is_a_legal_wave_rather_than_an_error()
    {
        // Not sending is a position rather than an omission: an empty slot
        // banks its gold at the ruleset's interest, so leaving one empty is an
        // investment measured against the purchase that would have used it.
        //
        // OBSERVED: refuse an empty slot in BuildPhase.Resolve -- throw instead
        // of `continue` where the slot IsEmpty. The first half goes red on the
        // refusal, and with it every round in this file that banked rather than
        // sent: leaving a slot empty stops being a position and becomes a
        // command nobody may write down.
        Run run = TheBuild.Fresh(waves: 3);
        UnitType first = TheBuild.FirstCreep(run.Types);

        // Every slot empty, which is the whole round banked. It is round one,
        // because a creep is bought once and attacks every round after -- so a
        // round that carries something and sends nothing is a creep left at
        // home rather than a round banked, and that is refused below.
        run.Advance(TheBuild.Filling(WaveSlot.Empty, WaveSlot.Empty));

        Assert.Equal(0, run.Sent[0].Wave.TotalUnits);
        Assert.Equal(0, run.Sent[0].Wave.Count);
        Assert.Equal(0, run.Outcome.Rounds[0].LeakCostDealt);

        // One filled, one empty.
        run.Advance(BuildPhase.Of(WaveSlot.Of(first.Id, 2), WaveSlot.Empty));

        Assert.Equal(2, run.Sent[1].Wave.TotalUnits);
        Assert.Equal(1, run.Sent[1].Wave.Count);

        // Emptying every slot now is not banking the round -- it is trying to
        // leave two creeps at home, and there is no doing that. Banking a later
        // round is sending the same slots again, which is the test below.
        Assert.Throws<SimulationException>(
            () => run.Advance(TheBuild.Filling(WaveSlot.Empty, WaveSlot.Empty)));

        // A slot nobody filled in at all is the empty one rather than one creep
        // of a type that does not exist.
        Assert.True(default(WaveSlot).IsEmpty);
        Assert.Equal(WaveSlot.Empty, default(WaveSlot));
        Assert.Equal(0, WaveSlot.Empty.TypeId);
    }

    [Fact]
    public void A_creep_is_bought_once_and_attacks_every_round_after()
    {
        // #207, found by playing it: a wave was whatever the round in front of
        // it happened to buy, so round seven could field fewer creeps than
        // round six and every round paid for its whole column again.
        //
        // OBSERVED: price the slots at their full count in BuildPhase.Resolve
        // instead of at the increase over what is carried. The second round
        // below is charged twice -- once for the creeps it is adding and once
        // for the ones it already owns -- and the third round's purse is short
        // by the whole of the first round's wave.
        Run run = TheBuild.Fresh(waves: 4);
        UnitType creep = TheBuild.FirstCreep(run.Types);
        int price = run.Costs.PriceOf(Purchase.Unit(creep.Id));

        // Nothing is carried into round one, so round one pays for all of it.
        Assert.Equal(0, run.Carrying.TotalUnits);

        RoundReport first = run.Advance(BuildPhase.Of(WaveSlot.Of(creep.Id, 2)));

        Assert.Equal(price * 2, first.Build.Spent);
        Assert.Equal(2, run.Carrying.CountOf(creep.Id));

        // Round two sends the same two and adds a third. Two of the three walk
        // for free, because they were paid for in round one.
        RoundReport second = run.Advance(BuildPhase.Of(WaveSlot.Of(creep.Id, 3)));

        Assert.Equal(price, second.Build.Spent);
        Assert.Equal(3, second.Build.Wave.TotalUnits);
        Assert.Equal(3, run.Carrying.CountOf(creep.Id));

        // Round three adds nothing at all: it sends the slots it carries, pays
        // nothing for the wave, and still sends every creep.
        RoundReport third = run.Advance(BuildPhase.Of(WaveSlot.Of(creep.Id, 3)));

        Assert.Equal(0, third.Build.Spent);
        Assert.Equal(3, third.Build.Wave.TotalUnits);

        // What is NOT asserted here: that each round's wave contains the one
        // before it. Every count above is written down in this test, so a loop
        // comparing them would hold the fixture against itself and pass with the
        // rule ripped out. That claim is made over the committed run instead,
        // where the price is what carries it -- see GoldenRunTests.
    }

    [Fact]
    public void A_wave_may_only_grow_and_both_ways_of_shrinking_one_are_refused()
    {
        // There is no selling a creep back and no leaving one at home, so a bad
        // early purchase is a lasting commitment. The two spellings of taking
        // one back -- a smaller count, and a slot dropped altogether -- are one
        // rule and both name what is carried.
        Run run = TheBuild.Fresh(waves: 4);
        UnitType creep = TheBuild.FirstCreep(run.Types);

        run.Advance(BuildPhase.Of(WaveSlot.Of(creep.Id, 3)));

        SimulationException fewer = Assert.Throws<SimulationException>(
            () => run.Advance(BuildPhase.Of(WaveSlot.Of(creep.Id, 2))));

        Assert.Contains("sends 2 of type id " + creep.Id, fewer.Message);
        Assert.Contains("already carries 3", fewer.Message);

        SimulationException none = Assert.Throws<SimulationException>(
            () => run.Advance(BuildPhase.Of()));

        Assert.Contains("sends none of type id " + creep.Id, none.Message);
        Assert.Contains("already carries 3", none.Message);

        // Refused whole: neither attempt moved the run or its purse.
        Assert.Equal(1, run.Round);
        Assert.Equal(3, run.Carrying.CountOf(creep.Id));
    }

    [Fact]
    public void The_whole_carried_wave_is_reordered_by_a_later_round_and_costs_nothing_to_reorder()
    {
        // A round's decision is over everything it fields, not only over what
        // it just bought -- so a phase names the whole wave and may put the
        // creeps it carries anywhere in the column. That is why a stored
        // command holds the whole wave rather than the round's additions.
        Run run = TheBuild.Fresh(waves: 4);
        UnitType[] creeps = Walkers(run.Types);
        UnitType first = creeps[0];
        UnitType second = creeps[1];

        run.Advance(BuildPhase.Of(WaveSlot.Of(first.Id, 2), WaveSlot.Of(second.Id, 1)));

        Assert.Equal(first.Id, run.Sent[0].Wave.Orders[0].TypeId);

        // The same creeps, the other way round, adding nothing.
        RoundReport swapped = run.Advance(
            BuildPhase.Of(WaveSlot.Of(second.Id, 1), WaveSlot.Of(first.Id, 2)));

        Assert.Equal(0, swapped.Build.Spent);
        Assert.Equal(second.Id, swapped.Build.Wave.Orders[0].TypeId);
        Assert.Equal(first.Id, swapped.Build.Wave.Orders[1].TypeId);

        // And the release order moved with it: what is at the front walks out
        // first, whoever paid for it and whenever they did.
        Assert.Equal(0, swapped.Build.Wave.Orders[0].TickOffset);
        Assert.True(swapped.Build.Wave.Orders[1].TickOffset > swapped.Build.Wave.Orders[0].TickOffset);
    }

    [Fact]
    public void A_slots_position_is_its_release_order_and_an_empty_slot_takes_no_place_in_the_column()
    {
        // The vision, under "You choose the order they come out in": a wave is
        // a sequence and not a bag. A build phase composed one wave and gave
        // every slot the same release tick until #191, so the columns all began
        // together and a slot's position meant nothing.
        //
        // The whole wave is one column at one cadence. Slot one's creeps walk
        // out first, one every Match.SpawnIntervalTicks, and slot two's first
        // creep stands behind the last of slot one rather than beside its
        // first -- so an order's offset is the count of every creep ahead of
        // it, and nothing ever arrives two abreast.
        //
        // OBSERVED: space the slots by their position instead of by the creeps
        // ahead of them -- index * SpawnIntervalTicks. This goes red on the
        // second order, 45 where 135 was expected, and every count above one
        // starts overlapping the slot behind it.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        WaveScript wave = Resolved(
            run,
            BuildPhase.Of(
                WaveSlot.Of(creeps[0], Bodies),
                WaveSlot.Empty,
                WaveSlot.Of(creeps[1], 1),
                WaveSlot.Of(creeps[2], 2)),
            1000)
            .Wave;

        Assert.Equal(3, wave.Count);

        Assert.Equal(0, wave.Orders[0].TickOffset);
        Assert.Equal(Bodies * Match.SpawnIntervalTicks, wave.Orders[1].TickOffset);
        Assert.Equal((Bodies + 1) * Match.SpawnIntervalTicks, wave.Orders[2].TickOffset);

        // The empty slot in the middle took no place in the column. It is a
        // player banking rather than sending, and banking closes the gap
        // instead of leaving a hole the defense gets for free.
        //
        // OBSERVED: advance the offset past an empty slot as though it sent
        // one. The third order moves to 225 and a banked slot silently buys the
        // defense forty-five ticks of quiet.
        Assert.Equal(creeps[1], wave.Orders[1].TypeId);

        // And the offsets ascend strictly, which is what keeps the record
        // canonical now that nothing asserts an order over the type ids: every
        // filled slot sends at least one creep, so no two orders can share a
        // tick.
        for (int index = 1; index < wave.Count; index++)
        {
            Assert.True(
                wave.Orders[index].TickOffset > wave.Orders[index - 1].TickOffset,
                "Order "
                    + (index + 1).ToString(CultureInfo.InvariantCulture)
                    + " releases on tick "
                    + wave.Orders[index].TickOffset.ToString(CultureInfo.InvariantCulture)
                    + ", at or below the one above it. A wave record asserts that its orders ascend by "
                    + "(tick, type), and since #191 nothing else makes them.");
        }
    }

    [Fact]
    public void The_same_two_creeps_in_the_other_order_are_a_different_wave_and_a_different_fight()
    {
        // #191's acceptance criterion, and the reason the ascending-by-type-id
        // rule had to go rather than being kept as a tidiness. The arrangement
        // is the decision: send the fast creep first and the fight is not the
        // fight you get sending it second, so canonicalising the arrangement
        // was deleting a lever rather than spelling one.
        //
        // Both halves are asserted, because either alone proves less than it
        // looks. That the composed waves differ is a statement about
        // BuildPhase; that the matches differ is the statement the player
        // cares about, and a rule that changed the bytes without changing the
        // fight would pass the first and fail the point.
        //
        // OBSERVED: give every order the same release tick, which is what this
        // did before #191. Both waves come out identical, both matches leak
        // identically, and this goes red on the first comparison of the two.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        WaveSlot first = WaveSlot.Of(creeps[0], Bodies);
        WaveSlot second = WaveSlot.Of(creeps[1], Bodies);

        WaveScript forward = Resolved(run, BuildPhase.Of(first, second), 1000).Wave;
        WaveScript backward = Resolved(run, BuildPhase.Of(second, first), 1000).Wave;

        // The same two creeps and the same two counts, so nothing separates
        // these but the order they were written in.
        Assert.Equal(forward.TotalUnits, backward.TotalUnits);
        Assert.Equal(forward.Count, backward.Count);

        Assert.Equal(creeps[0], forward.Orders[0].TypeId);
        Assert.Equal(creeps[1], backward.Orders[0].TypeId);
        Assert.Equal(0, forward.Orders[0].TickOffset);
        Assert.Equal(0, backward.Orders[0].TickOffset);

        // Which creep is at the front of the column is the whole difference,
        // and it is a difference the record carries.
        Assert.NotEqual(
            forward.Orders[0].TypeId,
            backward.Orders[0].TypeId);

        // And the fight. The same defense, the same seed, the same map, the
        // same bodies -- and two different traces, because what the towers
        // shoot at first is not the same creep.
        Assert.NotEqual(Fought(forward), Fought(backward));
    }

    [Fact]
    public void A_wave_nobody_can_afford_is_refused_where_the_decision_is_read()
    {
        // There is no credit in this economy. The whole wave is priced before a
        // coin moves, so a purse is never left part-spent on a wave that was
        // never legal -- which is the arrangement Purse.Spend's own message
        // says it is downstream of.
        //
        // OBSERVED: drop the spent-against-purse check in BuildPhase.Resolve
        // and let Purse.Spend catch it. This goes red on the message rather
        // than on the throw: what fires is "A purse holding 99 gold was spent
        // 100", from a purse already part-spent on the earlier slots of a wave
        // that was never legal -- and its own text says that reaching there
        // means an unaffordable command was let through.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        BuildPhase phase = BuildPhase.Of(
            WaveSlot.Of(creeps[0], 4),
            WaveSlot.Of(creeps[1], 4));

        int bill = Resolved(run, phase, int.MaxValue).Spent;

        Assert.True(bill > 1, "The two slots priced at nothing, so there is no affordability to test.");

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, bill - 1));

        Assert.Contains("There is no credit in this economy", thrown.Message, StringComparison.Ordinal);

        // One gold more and the same wave is fine, and the purse is what is
        // left rather than what was there.
        Build built = Resolved(run, phase, bill);

        Assert.Equal(bill, built.Spent);
        Assert.Equal(0, built.Purse.Gold);
    }

    [Fact]
    public void Two_slots_naming_one_creep_are_refused_rather_than_merged()
    {
        // A creep fills at most one slot of a wave. The same wave is spelled
        // by putting the whole count in one slot, so a second slot on the same
        // creep is a slot spent twice on one thing rather than a second
        // spelling somebody might have meant.
        //
        // This is all that is left of the rule that filled slots ascend
        // strictly by type id. The ascending half went in #191, when a slot's
        // position became its release order and the arrangement stopped being
        // something to canonicalise; this half was never about canonical bytes
        // in the first place.
        //
        // OBSERVED: drop the repeat check in BuildPhase.Resolve. This goes red
        // having caught nothing -- no exception was thrown -- and the phase
        // builds a wave that sends the same creep twice at two different ticks,
        // which is a purse spent twice on one decision.
        Run run = TheBuild.Fresh();
        int[] creeps = Creeps(run);

        SimulationException repeated = Assert.Throws<SimulationException>(
            () => Resolved(
                run,
                BuildPhase.Of(WaveSlot.Of(creeps[0], 1), WaveSlot.Of(creeps[0], 1)),
                1000));

        Assert.Contains("which a slot above it already sent", repeated.Message, StringComparison.Ordinal);
        Assert.Contains("A creep fills at most one slot of a wave", repeated.Message, StringComparison.Ordinal);

        // And a descending pair is accepted, which is the half that changed:
        // the slots name their creeps in whatever order the player arranged
        // them, and the arrangement is the decision.
        //
        // OBSERVED: reinstate the ascending assertion. This goes red on the
        // descending pair, and a player loses the ability to send the dearer
        // creep first.
        Build descending = Resolved(
            run,
            BuildPhase.Of(WaveSlot.Of(creeps[1], 1), WaveSlot.Of(creeps[0], 1)),
            1000);

        Assert.Equal(creeps[1], descending.Wave.Orders[0].TypeId);
        Assert.Equal(creeps[0], descending.Wave.Orders[1].TypeId);
    }

    [Fact]
    public void A_slot_that_sends_none_of_a_creep_is_refused_because_empty_already_has_a_spelling()
    {
        // Two spellings of one wave is what content-addressing stops meaning
        // anything the moment it is true, so a slot sends at least one or it is
        // WaveSlot.Empty.
        //
        // OBSERVED: widen the count guard in WaveSlot.Of to `count < -1`. This
        // goes red having caught nothing -- no exception was thrown -- and
        // WaveSlot.Of(1, 0) builds a slot that IsEmpty reports as empty while
        // carrying a type id: two values for one position, differing in a field
        // nothing downstream reads.
        SimulationException thrown = Assert.Throws<SimulationException>(() => WaveSlot.Of(1, 0));

        Assert.Contains("spelled WaveSlot.Empty", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<SimulationException>(() => WaveSlot.Of(1, -1));
        Assert.Throws<SimulationException>(() => WaveSlot.Of(1, WaveSlot.Largest + 1));

        // And a filled slot names a row rather than nothing.
        SimulationException nameless = Assert.Throws<SimulationException>(() => WaveSlot.Of(0, 1));

        Assert.Contains("A wave slot was filled with type id 0", nameless.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_opens_on_the_purse_the_ruleset_authored_so_the_first_round_is_a_round()
    {
        // Nothing has been earned when the first build phase stands. A run that
        // opened on nothing would have an opening round whose only affordable
        // wave is the empty one -- ten waves with nine build phases in them.
        //
        // OBSERVED: open the purse at Purse.Empty in the Run constructor. The
        // opening assertion goes red, 100 against 0, and the wave-1 build phase
        // below is refused for having no credit in this economy.
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);
        Run run = TheBuild.Fresh(waves: 2);

        Assert.Equal(100, rules.StartingPurseGold);
        Assert.Equal(rules.StartingPurseGold, run.Purse.Gold);

        UnitType first = TheBuild.FirstCreep(run.Types);
        run.Advance(BuildPhase.Of(WaveSlot.Of(first.Id, 1)));

        Assert.Equal(1, run.Sent[0].Wave.TotalUnits);
    }

    [Fact]
    public void A_whole_run_played_through_its_build_phases_pays_for_what_it_sends()
    {
        // The loop, end to end: ten rounds, ten takes, a wave bought out of the
        // purse each time, and an outcome that is still a vector of per-round
        // pairs. Advance is one name and the build phase is an overload of it,
        // so a run played from decisions and a run played from orders are the
        // same call rather than two lifecycles.
        //
        // OBSERVED: leave the purse on the build rather than taking it back --
        // hand Run.Play the run's own Purse instead of the build's, in
        // Run.Advance(BuildPhase). The spend assertion goes red, every round
        // deciding against a run that has never spent a coin, so the ten
        // rounds bill the opening purse ten times over.
        //
        // Death is off, because this player spends every coin on creeps and
        // never builds: an empty board against this pool runs out of health in
        // the seventh round, and what is under test here is that ten decisions
        // are ten rounds rather than how long a purely offensive run lives.
        Run run = TheBuild.Fresh(deathEndsTheRun: false);
        var spent = new List<int>();

        UnitType cheapest = Walkers(run.Types).OrderBy(type => type.Cost).First();

        while (!run.IsOver)
        {
            int affordable = run.Purse.Gold / (cheapest.Cost < 1 ? 1 : cheapest.Cost);

            BuildPhase phase = BuildPhase.Of(
                affordable > 0 ? WaveSlot.Of(cheapest.Id, affordable) : WaveSlot.Empty);

            // What the wave cost comes off the round the phase was played into.
            // The build phase resolves once, where the round is played, and says
            // what it came to.
            spent.Add(run.Advance(phase).Build.Spent);
        }

        Assert.Equal(10, run.Round);
        Assert.Equal(10, run.Sent.Count);
        Assert.Equal(10, run.Outcome.Rounds.Count);
        Assert.True(spent.Sum() > 0, "Ten build phases bought nothing at all.");
        Assert.All(spent, one => Assert.True(one >= 0, "A build phase gave gold back."));
    }

    [Fact]
    public void A_round_that_refuses_leaves_the_run_exactly_where_it_was()
    {
        // Everything that can refuse a round refuses before a coin moves. A
        // purse spent on a round that then threw would be paid for a wave
        // nobody was in the run to send -- and nothing downstream could tell
        // that from a round somebody played.
        //
        // OBSERVED: take the purse back where the decision is made rather than
        // where the round is committed -- add `Purse = build.Purse;` above the
        // RequireUnfinished call in Run.Advance(BuildPhase). The first half
        // goes red, 210 against 193: a finished run pays for a wave it refused
        // to send.
        //
        // OBSERVED: drop the null check at the top of Run.Advance. The second
        // half goes red on a NullReferenceException where an
        // ArgumentNullException was expected, and a round handed no decision
        // at all stops being refused by name.
        Run over = TheBuild.Fresh(waves: 1);

        over.Advance(TheBuild.BuyingNothing());
        Assert.True(over.IsOver);

        int purse = over.Purse.Gold;

        // A phase that would have been perfectly legal on a run with a round
        // left in it: one slot of the roster's first creep.
        BuildPhase past = BuildPhase.Of(WaveSlot.Of(TheBuild.FirstCreep(over.Types).Id, 1));

        Assert.Throws<SimulationException>(() => over.Advance(past));

        Assert.Equal(purse, over.Purse.Gold);

        // And a round handed no decision at all is refused the same way.
        Run alive = TheBuild.Fresh(waves: 2);

        Assert.Throws<ArgumentNullException>(() => alive.Advance(null!));

        Assert.Equal(TheBuild.RulesOffering(TheBuild.Ordinary).StartingPurseGold, alive.Purse.Gold);
    }

    [Fact]
    public void The_purse_walks_the_actions_then_the_slots()
    {
        // One decision over one wallet, spent in the order it was written: the
        // actions, then the wave's slots. That order is the bytes' order and it
        // is play order, and pricing the slots first would quietly reorder what
        // the author wrote.
        //
        // The phase below can afford its tower, and can afford its wave, and
        // cannot afford both, so it is refused whichever way round it is priced
        // -- and which refusal lands is the order. Walked in the written order
        // the tower is paid for and the wave is what runs out, so the refusal
        // names the creeps and the purse the tower left behind. Priced the
        // other way round it would name the tower and a purse of 14.
        //
        // OBSERVED: price the slots against `purse` rather than `left` in
        // BuildPhase.Resolve, so the wave is measured against a purse the
        // towers have not come out of. This goes red having caught nothing: the
        // phase resolves, and a run spends 67 gold out of a purse holding 41.
        Run run = TheBuild.Fresh();
        UnitType first = TheBuild.FirstCreep(run.Types);
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));
        int wave = run.Costs.PriceOf(Purchase.Unit(first.Id), Bodies);
        int carried = (tower > wave ? tower : wave) + 1;

        Assert.True(tower + wave > carried, "The tower and the wave fit in one purse together.");

        BuildPhase phase = BuildPhase
            .Of(WaveSlot.Of(first.Id, Bodies))
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, carried));

        Assert.Contains(
            "buys "
            + wave.ToString(CultureInfo.InvariantCulture)
            + " gold of creeps out of a purse holding "
            + (carried - tower).ToString(CultureInfo.InvariantCulture),
            thrown.Message,
            StringComparison.Ordinal);

        // The two together, out of a purse that holds them both: one bill, one
        // wallet, and the board the phase left behind carries the tower.
        Build built = Resolved(run, phase, tower + wave);

        Assert.Equal(tower + wave, built.Spent);
        Assert.Equal(0, built.Purse.Gold);
        Assert.Equal(run.Board.Count + 1, built.Board.Count);
        Assert.Equal(Bodies, built.Wave.TotalUnits);
    }

    [Fact]
    public void A_phase_that_cannot_afford_its_wave_after_building_is_refused_whole()
    {
        // Not silently emptied, and not sent short.         // A run where the towers ate
        // the wave is a decision, and the author's script has to add up -- so
        // the round refuses and the run is exactly where it was, purse and
        // board alike.
        //
        // OBSERVED: drop the slot that overspends instead of throwing -- skip a
        // slot in BuildPhase.Resolve where the running cost would pass what is
        // left. This goes red having caught nothing, and the run resolves a
        // round whose wave is smaller than the one written down.
        Run run = TheBuild.Fresh();
        UnitType first = TheBuild.FirstCreep(run.Types);
        int purse = run.Purse.Gold;
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));
        int creep = run.Costs.PriceOf(Purchase.Unit(first.Id));

        // Every body the purse could buy on its own, which is at least one more
        // than it can buy once the tower is paid for.
        int count = purse / creep;
        int affordable = (purse - tower) / creep;

        Assert.True(affordable >= 1 && affordable < count, "The tower left room for the whole wave.");

        BuildPhase phase = BuildPhase
            .Of(WaveSlot.Of(first.Id, count))
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(() => run.Advance(phase));

        Assert.Contains("refused whole rather than sent short", thrown.Message, StringComparison.Ordinal);

        Assert.Equal(purse, run.Purse.Gold);
        Assert.Equal(0, run.Round);
        Assert.Equal(0, run.Board.Count);

        // Buy what is left over instead and the same phase is a round: the
        // tower stands, the wave went, and one purse paid for both.
        Assert.Equal(
            1,
            run.Advance(
                BuildPhase
                    .Of(WaveSlot.Of(first.Id, affordable))
                    .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow)))
                .Build.Board.Count);

        Assert.Equal(1, run.Round);
    }

    [Fact]
    public void An_action_naming_a_creep_where_a_tower_belongs_is_refused()
    {
        // The roster refusal, and the reason a build phase is where it lands:
        // the parser holds no unit table, so what a type id names is a question
        // only something holding the roster can answer.
        //
        // OBSERVED: pass null as the role to UnitTypeTable.Require in
        // BuildPhase.Applied, which is "either half of the loop will do". This
        // goes red having caught nothing -- and a minion stands on the map as a
        // tower with no attack type, no range and a walking speed.
        Run run = TheBuild.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Acting(run, ActionKind.Place, Minion, FreeColumn, FreeRow));

        Assert.Contains(
            "A build phase at wave 1 places at column 0, row 0 requiring a placed unit names minion (#1)",
            thrown.Message,
            StringComparison.Ordinal);
        Assert.Contains("which is a moving unit", thrown.Message, StringComparison.Ordinal);

        // An upgrade names a row of the same half of the table.
        Assert.Contains(
            "A build phase at wave 1 upgrades at column 6, row 2 requiring a placed unit",
            Assert.Throws<SimulationException>(
                () => Acting(run, ActionKind.Upgrade, Minion, StandingColumn, StandingRow)).Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_place_on_an_occupied_cell_is_refused_rather_than_inferred_as_an_upgrade()
    {
        // Two verbs rather than one the board disambiguates. Inferring the kind
        // from whether the cell is taken would turn a mistyped hex into the
        // other action, at a different price, with nothing refusing.
        //
        // OBSERVED: send a place on an occupied cell to Board.Upgrade in
        // BuildPhase.Applied. This goes red having caught nothing, and the
        // archer already standing there becomes a mage nobody asked for.
        //
        // OBSERVED: return the board's refusal unwrapped from
        // BuildPhase.Standing. The round assertion goes red, and a ten-wave
        // script is told a cell is taken without being told which of its rounds
        // was refused.
        Run run = TheBuild.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Acting(run, ActionKind.Place, Mage, StandingColumn, StandingRow, Standing(run)));

        Assert.Contains("A build phase at wave 1 cannot act", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("One cell holds one placement", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_upgrade_of_a_cell_nothing_stands_on_is_refused()
    {
        // The other half of that sentence: an upgrade swaps the type of a
        // placement that is already standing, so an empty cell names none to
        // swap.
        //
        // OBSERVED: send an upgrade of an empty cell to Board.Place instead.
        // This goes red having caught nothing, and a phase that meant to climb
        // a tower it never built mints a new placement at the full price.
        Run run = TheBuild.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Acting(run, ActionKind.Upgrade, Mage, FreeColumn, FreeRow));

        Assert.Contains("A build phase at wave 1 cannot act", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("where nothing stands", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_upgrade_to_the_type_already_standing_is_refused()
    {
        // An upgrade pays the full price of the row it names, so one that swaps
        // a type for itself is a purchase that changes nothing -- a typo where
        // it meant another row, and a line nobody needs where it meant this one.
        //
        // OBSERVED: drop the same-type check in Board.Upgrade. This goes red
        // having caught nothing, and forty gold buys the archer that was
        // already there.
        Run run = TheBuild.Fresh();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Acting(run, ActionKind.Upgrade, Archer, StandingColumn, StandingRow, Standing(run)));

        Assert.Contains("A build phase at wave 1 cannot act", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("already stands as that type", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_place_off_the_ground_or_inside_the_corridor_is_refused()
    {
        // The map-aware predicate, asked by the payer exactly as the defense
        // loader asks it. A cell off the grid and a cell inside the corridor are
        // both positions that could not have happened: this simulation traces
        // its route rather than searching for one, so a tower in the corridor
        // would be a wall it has nothing to reroute around.
        //
        // OBSERVED: ask footing.Sound instead of footing.Possible in
        // BuildPhase.Applied. Both halves here stay green and the legal
        // placement below goes red, refusing a player for building somewhere
        // useless.
        Run run = TheBuild.Fresh();

        Assert.Contains(
            "which is a corridor cell",
            Assert.Throws<SimulationException>(() => Acting(run, ActionKind.Place, Archer, 4, 1)).Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "which is off a 19 by 13 map",
            Assert.Throws<SimulationException>(() => Acting(run, ActionKind.Place, Archer, 20, 0)).Message,
            StringComparison.Ordinal);

        // And a cell on the ground is a cell a tower may stand on.
        Assert.Equal(
            run.Board.Count + 1,
            Acting(run, ActionKind.Place, Archer, FreeColumn, FreeRow).Board.Count);
    }

    [Fact]
    public void An_action_nobody_can_afford_is_refused()
    {
        // There is no credit in this economy on either half of the board. A
        // phase pays for what it builds as it builds it, so the second of two
        // towers is priced against the purse the first one left.
        //
        // OBSERVED: drop the price check in BuildPhase.Applied and let
        // Purse.Spend catch it. This goes red on the message rather than on the
        // throw: what fires is "A purse holding 39 gold was spent 40", from a
        // guard whose own text says reaching it means an unaffordable command
        // was let through.
        Run run = TheBuild.Fresh();
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));

        BuildPhase phase = BuildPhase
            .Of()
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, tower - 1));

        Assert.Contains(
            "A build phase at wave 1 places at column 0, row 0 for "
            + tower.ToString(CultureInfo.InvariantCulture)
            + " gold out of a purse holding "
            + (tower - 1).ToString(CultureInfo.InvariantCulture),
            thrown.Message,
            StringComparison.Ordinal);

        // Two towers out of a purse that holds one: the first is paid for and
        // the second is what runs out.
        BuildPhase twice = phase.With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn + 1, FreeRow));

        Assert.Contains(
            "places at column 1, row 0 for "
            + tower.ToString(CultureInfo.InvariantCulture)
            + " gold out of a purse holding 0",
            Assert.Throws<SimulationException>(() => Resolved(run, twice, tower)).Message,
            StringComparison.Ordinal);

        Assert.Equal(run.Board.Count + 2, Resolved(run, twice, tower * 2).Board.Count);
    }

    [Fact]
    public void A_type_that_is_some_edges_target_is_refused_to_place_and_reached_by_upgrading()
    {
        // The one prerequisite this game has, and #179's whole acceptance
        // criterion. An edge in content/upgrades.txt says the ranger follows the
        // archer, and a rung is only worth being one if the rung below has to be
        // stood first -- so the ranger is refused to `place` rather than priced
        // out of reach, and the only way onto a cell it stands on is an archer
        // that was upgraded.
        //
        // This is the ladder deciding what a simulation does, which is the
        // sentence content/upgrades.txt used to deny. It is refused rather than
        // priced because a tier that can be bought without the tier under it is
        // not a tier, it is a second row at a higher price.
        //
        // OBSERVED: return false unconditionally from
        // UpgradeLadder.IsTargetOfAnEdge. The refusal half goes red having
        // caught nothing, the ranger is placed outright for its own 40 gold,
        // and the ladder becomes an annotation again.
        Run run = TheBuild.Fresh();
        int archer = run.Costs.PriceOf(Purchase.Unit(Archer));
        int ranger = run.Costs.PriceOf(Purchase.Unit(Ranger));

        Assert.True(run.Ladder.IsTargetOfAnEdge(Ranger), "The committed ladder points no edge at the ranger.");
        Assert.False(run.Ladder.IsTargetOfAnEdge(Archer), "The committed ladder points an edge at the archer.");

        SimulationException refused = Assert.Throws<SimulationException>(
            () => Acting(run, ActionKind.Place, Ranger, FreeColumn, FreeRow));

        Assert.Contains("is the target of an upgrade edge", refused.Message, StringComparison.Ordinal);
        Assert.Contains("reached by upgrading the rung below it", refused.Message, StringComparison.Ordinal);

        // And the archer under it is placed for its own price, because nothing
        // points at the archer.
        Build stood = Resolved(
            run,
            BuildPhase.Of().With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow)),
            archer);

        Assert.Equal(archer, stood.Spent);

        // A ranger costs the archer plus the ranger, in that order, and one
        // gold short of the pair does not reach it.
        BuildPhase climbing = BuildPhase
            .Of()
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, Ranger, FreeColumn, FreeRow));

        Assert.Equal(40, archer);
        Assert.Equal(40, ranger);
        Assert.Throws<SimulationException>(() => Resolved(run, climbing, archer + ranger - 1));

        Build climbed = Resolved(run, climbing, archer + ranger);

        Assert.Equal(archer + ranger, climbed.Spent);
        Assert.Equal(run.Board.Count + 1, climbed.Board.Count);
        Assert.Equal(
            Ranger,
            climbed.Board.Placements.First(
                placement => placement.Column == FreeColumn && placement.Row == FreeRow).Type.Id);
    }

    [Fact]
    public void A_rung_is_climbed_only_from_the_rung_under_it()
    {
        // The other half of #179's acceptance criterion, and the half that was
        // missing: "a Ranger is reachable only through an Archer". Refusing the
        // `place` alone does not say that. The ladder carries one edge, archer
        // to ranger, so a soldier or a mage standing on a cell is not a rung
        // below the ranger and cannot be upgraded into one -- otherwise the
        // ranger is reachable for 30 gold and an upgrade, the cheapest tower on
        // the roster is the prerequisite for the dearest, and the ladder ranks
        // nothing.
        //
        // OBSERVED: return true unconditionally from UpgradeLadder.HasEdge.
        // Both refusals below go red having caught nothing, and a soldier
        // becomes a ranger for 30 + 40 -- ten gold under the 40 + 40 the
        // criterion names, which is how the hole shows up in the price.
        Run run = TheBuild.Fresh();
        int soldier = run.Costs.PriceOf(Purchase.Unit(Soldier));
        int mage = run.Costs.PriceOf(Purchase.Unit(Mage));
        int ranger = run.Costs.PriceOf(Purchase.Unit(Ranger));

        Assert.True(run.Ladder.HasEdge(Archer, Ranger), "The committed ladder carries no archer-to-ranger edge.");
        Assert.False(run.Ladder.HasEdge(Soldier, Ranger), "The committed ladder carries a soldier-to-ranger edge.");

        BuildPhase fromASoldier = BuildPhase
            .Of()
            .With(BuildAction.Of(ActionKind.Place, Soldier, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, Ranger, FreeColumn, FreeRow));

        SimulationException refused = Assert.Throws<SimulationException>(
            () => Resolved(run, fromASoldier, soldier + ranger));

        Assert.Contains("The ladder carries no edge from that row to this one", refused.Message, StringComparison.Ordinal);

        // The mage is the same refusal from the other direction: dearer than the
        // ranger, so this is not about price.
        BuildPhase fromAMage = BuildPhase
            .Of()
            .With(BuildAction.Of(ActionKind.Place, Mage, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, Ranger, FreeColumn, FreeRow));

        Assert.Throws<SimulationException>(() => Resolved(run, fromAMage, mage + ranger));

        // And the archer still climbs, so the refusal is the ladder speaking and
        // not the upgrade verb being shut.
        BuildPhase fromAnArcher = BuildPhase
            .Of()
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow))
            .With(BuildAction.Of(ActionKind.Upgrade, Ranger, FreeColumn, FreeRow));

        int archer = run.Costs.PriceOf(Purchase.Unit(Archer));

        Assert.Equal(archer + ranger, Resolved(run, fromAnArcher, archer + ranger).Spent);
    }

    [Fact]
    public void An_upgrade_pays_the_targets_full_price_and_mints_no_new_placement_id()
    {
        // The full price of the row it names, read off the roster and not off
        // the ladder. The ladder decides what may be placed outright; it prices
        // nothing, which is why an edge carries no cost column. The placement
        // keeps its identity, because that is what an upgrade does to one.
        //
        // OBSERVED: price an upgrade at the difference between the two rows --
        // subtract the standing type's cost in BuildPhase.Applied. This goes
        // red on the spend, 52 against 92, and an upgrade becomes cheaper than
        // the tower it replaces by a rule nobody authored.
        Run run = TheBuild.Fresh();
        int mage = run.Costs.PriceOf(Purchase.Unit(Mage));
        Board standing = Standing(run);

        Build built = Resolved(
            run,
            BuildPhase
                .Of()
                .With(BuildAction.Of(ActionKind.Upgrade, Mage, StandingColumn, StandingRow)),
            mage,
            standing);

        Assert.Equal(mage, built.Spent);
        Assert.Equal(0, built.Purse.Gold);

        // The same count of placements, the same ids, and the cell that was
        // named standing as the type it was upgraded to.
        Assert.Equal(standing.Count, built.Board.Count);
        Assert.Equal(
            standing.Placements.Select(placement => placement.Id),
            built.Board.Placements.Select(placement => placement.Id));

        Placement climbed = built.Board.Placements.First(
            placement => placement.Column == StandingColumn && placement.Row == StandingRow);

        Assert.Equal(Mage, climbed.Type.Id);
        Assert.Equal(Archer, standing.Placements.First(
            placement => placement.Column == StandingColumn && placement.Row == StandingRow).Type.Id);
    }

    [Fact]
    public void A_placement_that_reaches_no_part_of_the_route_is_accepted()
    {
        // The split the shared predicate exists for. A cell nothing walks past
        // describes a position that is merely bad, and a player is allowed to
        // build somewhere useless -- the refusal for a tower that can never fire
        // is a rule about an authored file, which a placement made at a wave is
        // not.
        //
        // OBSERVED: refuse !footing.ReachesRoute in BuildPhase.Applied. This
        // goes red on the round below, and a soldier two hexes from the corridor
        // stops being a bad decision and becomes an illegal one.
        Run run = TheBuild.Fresh();
        Footing footing = Footing.Of(run.Map, run.Types.ById(Soldier), FreeColumn, FreeRow);

        Assert.True(footing.Possible);
        Assert.False(footing.ReachesRoute);

        run.Advance(
            BuildPhase
                .Of()
                .With(BuildAction.Of(ActionKind.Place, Soldier, FreeColumn, FreeRow)));

        Assert.Equal(1, run.Round);
        Assert.Equal(1, run.Board.Count);
        Assert.Equal(Soldier, run.Board.Placements[run.Board.Count - 1].Type.Id);
    }

    [Fact]
    public void What_a_phase_built_is_standing_when_that_rounds_waves_arrive()
    {
        // The purse walks the actions, then the slots, and the board a round's
        // incoming waves meet is the one that walk left behind. A tower bought
        // this round defends this round.
        //
        // OBSERVED: hand RoundOrders.Of the run's board rather than the build's
        // in Run.Advance. This goes red on the tower count of what was sent, 0
        // against 1, and every tower a run buys sits out the round it was
        // bought in.
        Run run = TheBuild.Fresh();

        run.Advance(
            BuildPhase
                .Of()
                .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow)));

        Assert.Equal(1, run.Sent[0].Defense.Count);
        Assert.Equal(run.Board.Count, run.Sent[0].Defense.Count);
    }

    /// <summary>
    /// The board an assertion about a taken cell acts on: a run opens with
    /// nothing standing, so the archer these upgrade and collide with is put
    /// there here rather than read off an authored file.
    /// </summary>
    private static Board Standing(Run run) =>
        run.Board.Place(run.Types.ById(Archer), StandingColumn, StandingRow);

    /// <summary>
    /// A phase that does one thing to the board and sends nothing, out of a
    /// purse none of these assertions is about.
    /// </summary>
    private static Build Acting(
        Run run,
        ActionKind kind,
        int typeId,
        int column,
        int row,
        Board? board = null) =>
        Resolved(
            run,
            BuildPhase.Of().With(BuildAction.Of(kind, typeId, column, row)),
            1000,
            board);

    /// <summary>Every walking row of a roster, in the order the file names them.</summary>
    private static UnitType[] Walkers(UnitTypeTable types) =>
        types.Types.Where(type => type.Role == UnitRole.Moving).ToArray();

    /// <summary>
    /// The creep type ids a run may send, ascending.
    /// </summary>
    /// <remarks>
    /// Every one of them, from wave one. This used to be read off the round's
    /// offering, which rationed the roster three creeps at a time; #179 deleted
    /// the ration, so what a run may send is the roster and the ascending order
    /// is the roster's own.
    /// </remarks>
    private static int[] Creeps(Run run) =>
        Walkers(run.Types).Select(type => type.Id).OrderBy(id => id).ToArray();

    /// <summary>
    /// A decision resolved against the round in front of a run, and against
    /// that run's own ladder, costs, roster, map and board.
    /// </summary>
    /// <remarks>
    /// The purse is an argument because most of these assertions are about what
    /// a decision costs, and the board is one because a run opens holding
    /// nothing; everything else comes off the run, because none of them are
    /// about where a roster or a map came from.
    /// </remarks>
    /// <summary>
    /// What a composed wave actually does: the rolling state hash of the match
    /// it fights against the committed defense.
    /// </summary>
    /// <remarks>
    /// The whole match rather than its leak count, because two arrangements can
    /// leak the same number of creeps down two entirely different fights, and
    /// what is being asserted is that the fight moved.
    /// </remarks>
    private static Hash64 Fought(WaveScript wave)
    {
        UnitTypeTable types = TheMatch.Types();
        var match = new Match(TheMatch.Map(), TheRuleset.Committed(), TheMatch.Layout(types), wave, 20260813UL);

        while (!match.IsFinished)
        {
            match.Advance(1);
        }

        return match.Result().RollingStateHash;
    }

    private static Build Resolved(Run run, BuildPhase phase, int gold, Board? board = null) =>
        phase.Resolve(
            run.Round + 1,
            run.Carrying,
            run.Ladder,
            Purse.Holding(gold),
            run.Costs,
            run.Types,
            run.Map,
            board ?? run.Board);
}
