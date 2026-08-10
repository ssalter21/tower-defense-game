using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The build phase: a public offering, permanent unlocks, and a wave of scarce
/// slots.
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

    /// <summary>A ground cell of the committed map that these assertions leave empty.</summary>
    private const int FreeColumn = 0;

    /// <summary>Its row.</summary>
    private const int FreeRow = 0;

    /// <summary>The other ground cell: the one <see cref="Standing"/> puts an archer on.</summary>
    private const int StandingColumn = 3;

    /// <summary>Its row.</summary>
    private const int StandingRow = 2;

    /// <summary>How many creeps a slot sends where the assertion is about the bill rather than the wave.</summary>
    private const int Bodies = 3;

    [Fact]
    public void Every_player_in_a_match_sees_the_same_offering_for_a_given_round()
    {
        // The Mechabellum move: one public list, so a send is a read rather
        // than a guess. What makes it public is that it is drawn from the run's
        // seed and the wave and from nothing private -- not the purse, not what
        // has been unlocked, not what was sent. Two players of one match are
        // two runs on one seed that played differently, and they have to be
        // handed the same menu.
        //
        // OBSERVED: mix the purse into the offering's position -- pass
        // Purse.Gold as the opponent coordinate in Run.OfferingAt. The wave-2
        // assertion goes red, [(Ordinary, 5), (Ordinary, 2), (Ordinary, 8)]
        // against [(Ordinary, 8), (Ordinary, 7), (Ordinary, 2)], which is what
        // a shop that reads what somebody can afford looks like from the other
        // player's chair.
        //
        // OBSERVED: mix what has been unlocked in instead -- pass Unlocks.Count
        // there. The two runs still agree, because both of them took exactly
        // one thing; the assertion that goes red is the fresh run's, on the
        // second option of wave three's menu, 8 against 6. That is why reading
        // a round's offering early is asserted here as well as reading it
        // late: two players of one match hold the same private quantities
        // often enough that comparing them only to each other would miss it.
        Run mine = TheBuild.Fresh(waves: 4);
        Run theirs = TheBuild.Fresh(waves: 4);

        Assert.Equal(TheBuild.Named(mine.Offering), TheBuild.Named(theirs.Offering));

        // Two different opening rounds: different takes, and one of them sends
        // a wave while the other banks the round.
        mine.Advance(BuildPhase.Of(OptionKind.Ordinary, mine.Offering.Options[0].Id));
        theirs.Advance(
            BuildPhase.Of(
                OptionKind.Ordinary,
                theirs.Offering.Options[1].Id,
                WaveSlot.Of(theirs.Offering.Options[1].TypeId, 2)));

        Assert.NotEqual(mine.Unlocks.Taken[0].Id, theirs.Unlocks.Taken[0].Id);
        Assert.NotEqual(mine.Purse.Gold, theirs.Purse.Gold);

        // And the round after it, and the one after that, are the same list.
        Assert.Equal(TheBuild.Named(mine.Offering), TheBuild.Named(theirs.Offering));
        Assert.Equal(TheBuild.Named(mine.OfferingAt(3)), TheBuild.Named(theirs.OfferingAt(3)));

        // Read from a run that has played nothing at all, wave three's menu is
        // still the same menu. The offering is a function of the seed and the
        // wave, so where in the run it is read from cannot enter it.
        Assert.Equal(
            TheBuild.Named(TheBuild.Fresh(waves: 4).OfferingAt(3)),
            TheBuild.Named(mine.OfferingAt(3)));
    }

    [Fact]
    public void The_offering_is_drawn_fresh_each_round_from_a_derived_position()
    {
        // Most of a week's variety comes from the churn rather than from the
        // anchors -- ten draws a run against the anchors' three -- so the ten
        // menus of a run must not be one menu ten times.
        //
        // And the position is derived rather than continued, which is the
        // property that makes a run reproducible from its record: wave seven's
        // offering cannot depend on what waves one to six did.
        //
        // OBSERVED: draw every round from the same position -- pass 0 instead
        // of the wave in Run.OfferingAt's Derived call. The first assertion
        // goes red saying ten waves of a run drew 7,5,6 every time, which is an
        // offering that is fresh per run rather than per round.
        string[] menus = Menus(TheBuild.Fresh());

        Assert.True(
            menus.Distinct().Count() > 1,
            "Ten waves of a run drew the same ordinary options every time: " + menus[0] + ".");

        // Same seed, same menus. A different seed, different menus.
        Assert.Equal(menus, Menus(TheBuild.Fresh()));
        Assert.NotEqual(menus, Menus(TheBuild.Fresh(seed: TheRun.Seed + 1)));
    }

    [Fact]
    public void On_an_anchor_round_three_game_changers_merge_into_that_rounds_ordinary_offering()
    {
        // The menu is merged rather than additional: one thing is taken from
        // the whole list, so a game changer competes head to head with an
        // ordinary unlock. A free extra pick would end every run with everybody
        // holding all three, which leaves only when they field it unknown.
        //
        // OBSERVED: merge the first anchor's menu into every round -- look up
        // filling.Menus[0].Anchor.Wave instead of wave in Offering.Draw. The
        // ordinary-round assertion goes red on IsAnchor, and every wave of the
        // run becomes an anchor with six things on it.
        Run run = TheBuild.Fresh();
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);

        Assert.Equal(3, rules.OrdinaryOptionsPerRound);
        Assert.Equal(3, rules.GameChangersPerAnchor);

        foreach (int wave in new[] { 1, 2, 4, 5, 7, 8, 10 })
        {
            Offering ordinary = run.OfferingAt(wave);

            Assert.False(ordinary.IsAnchor);
            Assert.Equal(rules.OrdinaryOptionsPerRound, ordinary.Count);
            Assert.All(ordinary.Options, option => Assert.Equal(OptionKind.Ordinary, option.Kind));
        }

        foreach (int wave in new[] { 3, 6, 9 })
        {
            Offering anchored = run.OfferingAt(wave);

            Assert.True(anchored.IsAnchor);
            Assert.Equal(rules.OrdinaryOptionsPerRound + rules.GameChangersPerAnchor, anchored.Count);
            Assert.Equal(rules.OrdinaryOptionsPerRound, anchored.OrdinaryCount);

            // The three the run's filling drew onto that anchor, and no others.
            Assert.Equal(
                run.Filling.At(wave).GameChangers.Select(changer => changer.Id),
                anchored.Options
                    .Where(option => option.Kind == OptionKind.GameChanger)
                    .Select(option => option.Id));
        }

        // And a game changer is takeable off the merged list, which is the
        // whole of what "competes head to head" means.

        run.Advance(TheBuild.BuyingNothing(run.Offering));
        run.Advance(TheBuild.BuyingNothing(run.Offering));

        Option changerOption = run.Offering.Options.First(option => option.Kind == OptionKind.GameChanger);
        run.Advance(BuildPhase.Of(OptionKind.GameChanger, changerOption.Id, WaveSlot.Empty));

        Assert.Equal(OptionKind.GameChanger, run.Unlocks.Taken[2].Kind);
        Assert.Equal(3, run.Unlocks.Count);
    }

    [Fact]
    public void Taking_an_option_unlocks_a_creep_for_the_rest_of_the_run_and_unlocking_is_free()
    {
        // Free to unlock and paid to buy, so what may be fielded is bounded by
        // what was chosen rather than by which wallet somebody remembered to
        // save into. The take costs nothing: a round that took and sent nothing
        // has exactly the purse it opened with plus what the wave paid.
        //
        // OBSERVED: charge the take -- spend costs.PriceOf(Purchase.Unit(
        // taken.TypeId), 1) out of the purse in BuildPhase.Resolve before the
        // slots are priced. The free-unlock assertion goes red, 210 against
        // 188, and unlocking becomes a second price nobody authored -- charged
        // on top of the wave, out of the same wallet, at the cost of a creep
        // nobody sent.
        Run run = TheBuild.Fresh(waves: 4);
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);

        Assert.Equal(rules.StartingPurseGold, run.Purse.Gold);
        Assert.Equal(0, run.Unlocks.Count);

        Option first = run.Offering.Options[0];
        run.Advance(BuildPhase.Of(first.Kind, first.Id));

        // Nothing was bought, so the purse is what it opened with plus the wave.
        Assert.Equal(rules.StartingPurseGold + 10 + rules.IncomeBasePerWave, run.Purse.Gold);
        Assert.Equal(1, run.Unlocks.Count);
        Assert.True(run.Unlocks.Has(first.TypeId));

        // And it is permanent: three rounds later, with three other takes in
        // between, the first one is still fieldable.
        while (!run.IsOver)
        {
            run.Advance(TheBuild.BuyingNothing(run.Offering));
        }

        Assert.Equal(4, run.Unlocks.Count);
        Assert.True(run.Unlocks.Has(first.TypeId));
    }

    [Fact]
    public void A_wave_has_the_slots_the_schedule_derives_and_each_one_is_a_creep_type_and_a_count()
    {
        // Slots start at two and widen only at anchors, on the width the
        // schedule derives -- 2 2 3 3 3 4 4 4 5 5 -- so moving an anchor moves
        // the widths with it and the two cannot fall out of step. The offering
        // carries the number rather than recomputing it, so that a build phase
        // is checked against one width and not against a second opinion.
        //
        // OBSERVED: carry rules.StartingWaveSlots as the offering's width
        // instead of schedule.WaveSlotsAt in Offering.Draw. The series
        // assertion goes red, [2, 2, 3, 3, 3, ...] against
        // [2, 2, 2, 2, 2, ...], and the scarcity that stands in for a second
        // wallet stops widening at an anchor at all.
        Run run = TheBuild.Fresh();
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);
        AnchorSchedule schedule = TheSchedule.Committed();

        int[] widths = Enumerable.Range(1, 10).Select(wave => run.OfferingAt(wave).WaveSlots).ToArray();

        Assert.Equal(new[] { 2, 2, 3, 3, 3, 4, 4, 4, 5, 5 }, widths);
        Assert.Equal(Enumerable.Range(1, 10).Select(wave => schedule.WaveSlotsAt(rules, wave)), widths);

        // A slot is one creep type plus a count, and the wave is what the
        // filled ones compose.
        Offering opening = run.Offering;
        Build built = Resolved(
            run,
            BuildPhase.Of(
                opening.Options[0].Kind,
                opening.Options[0].Id,
                WaveSlot.Of(opening.Options[0].TypeId, 4)),
            Unlocks.None,
            1000);

        Assert.Equal(1, built.Wave.Count);
        Assert.Equal(4, built.Wave.TotalUnits);
        Assert.Equal(opening.Options[0].TypeId, built.Wave.Orders[0].TypeId);
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
        Option first = run.Offering.Options[0];

        // One filled, one empty.
        run.Advance(BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 2), WaveSlot.Empty));

        Assert.Equal(2, run.Sent[0].Wave.TotalUnits);
        Assert.Equal(1, run.Sent[0].Wave.Count);

        // Every slot empty, which is the whole round banked.
        run.Advance(TheBuild.TakeFirst(run.Offering, WaveSlot.Empty, WaveSlot.Empty));

        Assert.Equal(0, run.Sent[1].Wave.TotalUnits);
        Assert.Equal(0, run.Sent[1].Wave.Count);
        Assert.Equal(0, run.Outcome.Rounds[1].LeakCostDealt);

        // A slot nobody filled in at all is the empty one rather than one creep
        // of a type that does not exist.
        Assert.True(default(WaveSlot).IsEmpty);
        Assert.Equal(WaveSlot.Empty, default(WaveSlot));
        Assert.Equal(0, WaveSlot.Empty.TypeId);
    }

    [Fact]
    public void A_take_naming_an_option_the_offering_did_not_carry_is_refused()
    {
        // The offering is what everybody in the match was reading, so a take
        // against a different one is a decision made in a different game. A
        // refusal and never a skip: a run that partially validates produces a
        // confidently wrong result that still looks like a result.
        //
        // OBSERVED: return the first option instead of throwing in
        // Offering.Take when TryFind finds nothing. This goes red having caught
        // nothing -- no exception was thrown -- and every command naming an
        // option that was never offered silently unlocks whatever happened to
        // be drawn first.
        Run run = TheBuild.Fresh();
        Offering offering = run.Offering;
        int absent = offering.Options.Max(option => option.Id) + 1;

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, BuildPhase.Of(OptionKind.Ordinary, absent), Unlocks.None, 1000));

        Assert.Contains("which that round's offering does not carry", thrown.Message, StringComparison.Ordinal);

        // The kind is part of the identity, so an ordinary option's id taken as
        // a game changer is just as absent -- wave 1 has no game changers at all.
        Assert.Throws<SimulationException>(
            () => Resolved(
                run, BuildPhase.Of(OptionKind.GameChanger, offering.Options[0].Id), Unlocks.None, 1000));
    }

    [Fact]
    public void Buying_a_creep_that_was_never_unlocked_is_refused()
    {
        // The unlock gate, which is what makes what a player may field bounded
        // by what they chose. A gate that let one purchase through is a gate
        // nobody has.
        //
        // OBSERVED: drop the after.Has check in BuildPhase.Resolve. This goes
        // red on the message rather than on the throw: what fires instead is
        // "Type id 1 is unlocked and has no unit row behind it, which cannot
        // happen", one layer later and from a guard whose own comment says the
        // case is impossible. That is why the refusal is asserted by name --
        // the gate being gone reads as an internal contradiction rather than as
        // a creep the run never took.
        Run run = TheBuild.Fresh();
        Offering offering = run.Offering;
        Option taken = offering.Options[0];
        int never = TheMatch.Types().Types
            .First(type => type.Role == UnitRole.Moving && type.Id != taken.TypeId)
            .Id;

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(
                run, BuildPhase.Of(taken.Kind, taken.Id, WaveSlot.Of(never, 1)), Unlocks.None, 1000));

        Assert.Contains("which this run never unlocked", thrown.Message, StringComparison.Ordinal);

        // What this round took is fieldable this round: the take and the buy
        // are one decision over one purse.
        Build built = Resolved(
            run,
            BuildPhase.Of(taken.Kind, taken.Id, WaveSlot.Of(taken.TypeId, 1)),
            Unlocks.None,
            1000);

        Assert.Equal(1, built.Wave.TotalUnits);
    }

    [Fact]
    public void Filling_a_slot_beyond_the_rounds_width_is_refused()
    {
        // The scarcity that stands in for a second wallet. Dropping the extra
        // slot rather than refusing it would send a wave nobody composed, which
        // is the failure mode this whole surface exists to make impossible.
        //
        // OBSERVED: drop the width check in BuildPhase.Resolve. This goes red
        // having caught nothing -- no exception was thrown -- and a wave-1
        // build phase fills three slots in a round the schedule gave two.
        Run run = TheBuild.Fresh();
        Offering offering = run.Offering;
        Unlocks everything = Everything(offering);

        Assert.Equal(2, offering.WaveSlots);

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(
                run,
                BuildPhase.Of(
                    offering.Options[0].Kind,
                    offering.Options[0].Id,
                    WaveSlot.Empty,
                    WaveSlot.Empty,
                    WaveSlot.Empty),
                everything,
                1000));

        Assert.Contains("slots where that round has 2", thrown.Message, StringComparison.Ordinal);

        // Exactly the width is fine, and so is fewer than it.
        int[] creeps = offering.Options.Select(option => option.TypeId).OrderBy(id => id).ToArray();

        Assert.Equal(
            2,
            Resolved(
                run,
                BuildPhase.Of(
                    offering.Options[0].Kind,
                    offering.Options[0].Id,
                    WaveSlot.Of(creeps[0], 1),
                    WaveSlot.Of(creeps[1], 1)),
                everything,
                1000)
                .Wave.Count);
    }

    [Fact]
    public void Every_slot_a_build_phase_fills_releases_on_tick_zero()
    {
        // A build phase composes what is sent rather than when, so the whole
        // wave leaves at once and the ordering a wave record asserts falls to
        // the type ids alone.
        //
        // This is pinned because something outside the simulation now depends on
        // it: the command line refuses a --field file whose orders arrive over
        // time, on the ground that a field member stands in for a stored round
        // and a stored round is one of these. If the release tick ever moves,
        // that refusal starts rejecting real rounds -- so the rule goes red here
        // rather than out there.
        //
        // OBSERVED: move BuildPhase.ReleaseTick from 0 to 1. This goes red on
        // the first order, and CommandLineTests's wrong-field refusal stays
        // green, still refusing content/wave.txt for a reason that has stopped
        // being true.
        Run run = TheBuild.Fresh();
        Offering offering = run.Offering;
        int[] creeps = offering.Options.Select(option => option.TypeId).OrderBy(id => id).ToArray();

        WaveScript wave = Resolved(
            run,
            BuildPhase.Of(
                offering.Options[0].Kind,
                offering.Options[0].Id,
                WaveSlot.Of(creeps[0], 1),
                WaveSlot.Of(creeps[1], 1)),
            Everything(offering),
            1000)
            .Wave;

        Assert.Equal(2, wave.Count);

        for (int index = 0; index < wave.Count; index++)
        {
            Assert.Equal(0, wave.Orders[index].TickOffset);
        }
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
        Offering offering = run.Offering;
        Unlocks everything = Everything(offering);
        int[] creeps = offering.Options.Select(option => option.TypeId).OrderBy(id => id).ToArray();

        BuildPhase phase = BuildPhase.Of(
            offering.Options[0].Kind,
            offering.Options[0].Id,
            WaveSlot.Of(creeps[0], 4),
            WaveSlot.Of(creeps[1], 4));

        int bill = Resolved(run, phase, everything, int.MaxValue).Spent;

        Assert.True(bill > 1, "The two slots priced at nothing, so there is no affordability to test.");

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, everything, bill - 1));

        Assert.Contains("There is no credit in this economy", thrown.Message, StringComparison.Ordinal);

        // One gold more and the same wave is fine, and the purse is what is
        // left rather than what was there.
        Build built = Resolved(run, phase, everything, bill);

        Assert.Equal(bill, built.Spent);
        Assert.Equal(0, built.Purse.Gold);
    }

    [Fact]
    public void Two_slots_naming_one_creep_are_refused_rather_than_merged()
    {
        // Filled slots ascend strictly by type id, which is the rule the wave
        // they compose already lives by: orders ascend and are unique on
        // (tick, type), asserted rather than sorted, because sorting would
        // leave two identical waves with two different sets of bytes. It also
        // makes a slot spent twice on one creep a refusal rather than a slot
        // silently thrown away.
        //
        // OBSERVED: drop the previousTypeId check in BuildPhase.Resolve. This
        // goes red having caught nothing -- no exception was thrown -- and the
        // repeated-creep phase builds a wave with two orders on the same
        // (tick 0, type) key, which is a wave no loader in this repository
        // would accept back.
        Run run = TheBuild.Fresh();
        Offering offering = run.Offering;
        Unlocks everything = Everything(offering);
        int[] creeps = offering.Options.Select(option => option.TypeId).OrderBy(id => id).ToArray();

        SimulationException repeated = Assert.Throws<SimulationException>(
            () => Resolved(
                run,
                BuildPhase.Of(
                    offering.Options[0].Kind,
                    offering.Options[0].Id,
                    WaveSlot.Of(creeps[0], 1),
                    WaveSlot.Of(creeps[0], 1)),
                everything,
                1000));

        Assert.Contains("at or below the", repeated.Message, StringComparison.Ordinal);

        SimulationException descending = Assert.Throws<SimulationException>(
            () => Resolved(
                run,
                BuildPhase.Of(
                    offering.Options[0].Kind,
                    offering.Options[0].Id,
                    WaveSlot.Of(creeps[1], 1),
                    WaveSlot.Of(creeps[0], 1)),
                everything,
                1000));

        Assert.Contains("Filled slots ascend strictly by type id", descending.Message, StringComparison.Ordinal);
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
    public void A_roster_thinner_than_the_offerings_ratio_is_refused()
    {
        // An option unlocks a creep and appears on a menu once, so an offering
        // cannot be drawn out of fewer creeps than it carries options. Refused
        // rather than answered with the same creep twice, which would be one
        // option wearing two positions.
        //
        // OBSERVED: drop the roster check in Offering.Draw. This goes red on
        // the exception type, ArgumentOutOfRangeException against
        // SimulationException: the partial Fisher-Yates below it asks the dice
        // for a number below zero, and what a designer is handed says nothing
        // about the ratio, the roster or the file either was authored in.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Offering.Draw(
                TheBuild.RulesOffering(7),
                TheMatch.Types(),
                TheSchedule.Committed(),
                TheRun.Fresh(waves: 1).Filling,
                1,
                TheRun.Seed));

        Assert.Contains("out of a roster of 5 creeps", thrown.Message, StringComparison.Ordinal);

        // Five options out of five walkers is exactly enough, and it is the
        // whole roster on one menu -- which is the bound rather than the
        // tuning. The signed roster sits right up against it: the committed
        // ratio of three options is three fifths of the roster, so a menu is
        // most of what there is and the draw is a thin one. That is a known
        // cost of five creeps and it is written down in docs/roster.md.
        Assert.Equal(5, TheBuild.Fresh(waves: 1, ordinary: 5).Offering.Count);
        Assert.Equal(TheBuild.Ordinary, TheRuleset.Committed().OrdinaryOptionsPerRound);
    }

    [Fact]
    public void The_offering_ratio_and_the_slot_widths_come_from_the_ruleset_and_the_schedule()
    {
        // Neither number is a code constant, and both are swept by editing a
        // text file rather than by a compile.
        //
        // OBSERVED: draw a hard-coded three ordinary options in Offering.Draw
        // instead of rules.OrdinaryOptionsPerRound. The ratio assertion goes
        // red, 4 against 3, and the number that decides whether the merged menu
        // is a real trade stops being a sweep target.
        Assert.Equal(3, TheBuild.Fresh(waves: 1, ordinary: 3).Offering.Count);
        Assert.Equal(4, TheBuild.Fresh(waves: 1, ordinary: 4).Offering.Count);
        Assert.Equal(5, TheBuild.Fresh(waves: 1, ordinary: 5).Offering.Count);

        // And the widths move with the schedule's anchors rather than with a
        // series authored beside them.
        UnitTypeTable types = TheMatch.Types();
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);

        AnchorSchedule moved = AnchorSchedule.Parse(
            PlantedText.Replace(TheSchedule.CommittedText(), "anchor        6     2", "anchor        5     2"),
            types);

        var run = new Run(
            TheMatch.Map(),
            rules,
            types,
            moved,
            TheRun.Pool(types),
            TheRun.Seed,
            waves: 10,
            fieldSize: 4);

        Assert.Equal(
            new[] { 2, 2, 3, 3, 4, 4, 4, 4, 5, 5 },
            Enumerable.Range(1, 10).Select(wave => run.OfferingAt(wave).WaveSlots));
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

        Option first = run.Offering.Options[0];
        run.Advance(BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 1)));

        Assert.Equal(1, run.Sent[0].Wave.TotalUnits);
    }

    [Fact]
    public void What_a_prepared_counter_gets_against_a_fielded_game_changer_is_reachable_from_the_unlocks()
    {
        // The bonus is the anchor's and it is paid only to the unit type that
        // anchor named, so reading it needs the game changer rather than the
        // body it fields -- which a type id cannot say, because two game
        // changers can field one placeholder creep. The unlocks carry the take
        // itself for that reason.
        //
        // OBSERVED: have TryChangerFor always answer false, which is what
        // reducing Unlocks to the type ids it holds would leave. The first
        // assertion goes red, and what a run took becomes indistinguishable
        // from the list of creeps it may send -- at which point nothing can say
        // which of two game changers over one body is on the map.
        Run run = TheBuild.Fresh();
        AnchorSchedule schedule = TheSchedule.Committed();

        run.Advance(TheBuild.BuyingNothing(run.Offering));
        run.Advance(TheBuild.BuyingNothing(run.Offering));

        Option steepless = run.Offering.Options.First(option => option.Kind == OptionKind.GameChanger);
        run.Advance(BuildPhase.Of(OptionKind.GameChanger, steepless.Id, WaveSlot.Empty));

        Assert.True(run.Unlocks.TryChangerFor(steepless.TypeId, out GameChanger? fielded));
        Assert.Equal(steepless.Id, fielded!.Id);

        // Wave three's anchor is plain, so its counter gets nothing extra; wave
        // nine's is the steep one and its counter gets the whole bonus.
        Anchor plain = schedule.Anchors[0];
        Anchor steep = schedule.Anchors[2];

        Assert.Equal(0, schedule.BonusVsTag(plain.CounterTypeId, fielded));
        Assert.Equal(0, fielded.BonusVsTag);

        GameChanger late = schedule.GameChangers.First(changer => changer.Tier == steep.Tier);

        Assert.Equal(825, schedule.BonusVsTag(steep.CounterTypeId, late));
        Assert.Equal(0, schedule.BonusVsTag(plain.CounterTypeId, late));
    }

    [Fact]
    public void A_whole_run_played_through_its_build_phases_takes_one_thing_a_round_and_pays_for_what_it_sends()
    {
        // The loop, end to end: ten rounds, ten takes, a wave bought out of the
        // purse each time, and an outcome that is still a vector of per-round
        // pairs. Advance is one name and the build phase is an overload of it,
        // so a run played from decisions and a run played from orders are the
        // same call rather than two lifecycles.
        //
        // OBSERVED: leave the purse and the unlocks on the build rather than
        // taking them back -- hand Run.Play the run's own Unlocks and Purse
        // instead of the build's, in Run.Advance(BuildPhase). The
        // unlock-count assertion goes red, 10 against 0, and every round of the
        // run decides against a run that has never taken anything and never
        // spent a coin.
        //
        // Death is off, because this player spends every coin on creeps and
        // never builds: an empty board against this pool runs out of health in
        // the seventh round, and what is under test here is that ten decisions
        // are ten rounds rather than how long a purely offensive run lives.
        Run run = TheBuild.Fresh(deathEndsTheRun: false);
        var spent = new List<int>();

        while (!run.IsOver)
        {
            Offering offering = run.Offering;
            Option cheapest = offering.Options.OrderBy(option => option.Type.Cost).First();
            int affordable = run.Purse.Gold / (cheapest.Type.Cost < 1 ? 1 : cheapest.Type.Cost);

            BuildPhase phase = BuildPhase.Of(
                cheapest.Kind,
                cheapest.Id,
                affordable > 0 ? WaveSlot.Of(cheapest.TypeId, affordable) : WaveSlot.Empty);

            // What the wave cost comes off the round the phase was played into.
            // The build phase resolves once, where the round is played, and says
            // what it came to.
            spent.Add(run.Advance(phase).Build.Spent);
        }

        Assert.Equal(10, run.Round);
        Assert.Equal(10, run.Unlocks.Count);
        Assert.Equal(10, run.Sent.Count);
        Assert.Equal(10, run.Outcome.Rounds.Count);
        Assert.True(spent.Sum() > 0, "Ten build phases bought nothing at all.");
        Assert.All(spent, one => Assert.True(one >= 0, "A build phase gave gold back."));
    }

    [Fact]
    public void A_round_that_refuses_leaves_the_run_exactly_where_it_was()
    {
        // Everything that can refuse a round refuses before a coin moves. A
        // purse spent and an unlock taken on a round that then threw would be
        // paid for a wave nobody was in the run to send -- and nothing
        // downstream could tell that from a round somebody played.
        //
        // OBSERVED: take the purse and the unlocks back where the decision is
        // made rather than where the round is committed -- add
        // `Unlocks = build.Unlocks; Purse = build.Purse;` above the
        // RequireUnfinished call in Run.Advance(BuildPhase). The
        // first half goes red, 210 against 193: a finished run pays for a wave
        // it refused to send.
        //
        // OBSERVED: drop the null check at the top of Run.Advance. The second
        // half goes red on a NullReferenceException where an
        // ArgumentNullException was expected, and a round handed no decision
        // at all stops being refused by name.
        Run over = TheBuild.Fresh(waves: 1);

        over.Advance(TheBuild.BuyingNothing(over.Offering));
        Assert.True(over.IsOver);

        int purse = over.Purse.Gold;
        int unlocks = over.Unlocks.Count;

        // A phase that would have been perfectly legal on a run with a round
        // left in it: a take off wave two's menu, and a slot of the creep wave
        // one unlocked.
        Option next = over.Offering.Options[0];
        BuildPhase past = BuildPhase.Of(
            next.Kind, next.Id, WaveSlot.Of(over.Unlocks.Taken[0].TypeId, 1));

        Assert.Throws<SimulationException>(() => over.Advance(past));

        Assert.Equal(purse, over.Purse.Gold);
        Assert.Equal(unlocks, over.Unlocks.Count);

        // And a round handed no decision at all is refused the same way.
        Run alive = TheBuild.Fresh(waves: 2);

        Assert.Throws<ArgumentNullException>(() => alive.Advance(null!));

        Assert.Equal(0, alive.Unlocks.Count);
        Assert.Equal(TheBuild.RulesOffering(TheBuild.Ordinary).StartingPurseGold, alive.Purse.Gold);
    }

    [Fact]
    public void The_purse_walks_the_take_then_the_actions_then_the_slots()
    {
        // One decision over one wallet, spent in the order it was written: the
        // take, then the actions, then the wave's slots. That order is the
        // bytes' order and it is play order, and pricing the slots first would
        // quietly reorder what the author wrote.
        //
        // The take spends nothing, so its leg of the walk is not a purse event
        // and is not what this proves: that it comes first is what makes the
        // creep it unlocked fieldable in the same round, which
        // Buying_a_creep_that_was_never_unlocked_is_refused asserts. What is
        // proved here is the two legs that move gold.
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
        Option first = run.Offering.Options[0];
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));
        int wave = run.Costs.PriceOf(Purchase.Unit(first.TypeId), Bodies);
        int carried = (tower > wave ? tower : wave) + 1;

        Assert.True(tower + wave > carried, "The tower and the wave fit in one purse together.");

        BuildPhase phase = BuildPhase
            .Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, Bodies))
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, Unlocks.None, carried));

        Assert.Contains(
            "buys "
            + wave.ToString(CultureInfo.InvariantCulture)
            + " gold of creeps out of a purse holding "
            + (carried - tower).ToString(CultureInfo.InvariantCulture),
            thrown.Message,
            StringComparison.Ordinal);

        // The two together, out of a purse that holds them both: one bill, one
        // wallet, and the board the phase left behind carries the tower.
        Build built = Resolved(run, phase, Unlocks.None, tower + wave);

        Assert.Equal(tower + wave, built.Spent);
        Assert.Equal(0, built.Purse.Gold);
        Assert.Equal(run.Board.Count + 1, built.Board.Count);
        Assert.Equal(Bodies, built.Wave.TotalUnits);
    }

    [Fact]
    public void A_phase_that_cannot_afford_its_wave_after_building_is_refused_whole()
    {
        // Not silently emptied, and not sent short. A run where the towers ate
        // the wave is a decision, and the author's script has to add up -- so
        // the round refuses and the run is exactly where it was, purse, board
        // and unlocks alike.
        //
        // OBSERVED: drop the slot that overspends instead of throwing -- skip a
        // slot in BuildPhase.Resolve where the running cost would pass what is
        // left. This goes red having caught nothing, and the run resolves a
        // round whose wave is smaller than the one written down.
        Run run = TheBuild.Fresh();
        Option first = run.Offering.Options[0];
        int purse = run.Purse.Gold;
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));
        int creep = run.Costs.PriceOf(Purchase.Unit(first.TypeId));

        // Every body the purse could buy on its own, which is at least one more
        // than it can buy once the tower is paid for.
        int count = purse / creep;
        int affordable = (purse - tower) / creep;

        Assert.True(affordable >= 1 && affordable < count, "The tower left room for the whole wave.");

        BuildPhase phase = BuildPhase
            .Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, count))
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(() => run.Advance(phase));

        Assert.Contains("refused whole rather than sent short", thrown.Message, StringComparison.Ordinal);

        Assert.Equal(purse, run.Purse.Gold);
        Assert.Equal(0, run.Unlocks.Count);
        Assert.Equal(0, run.Round);
        Assert.Equal(0, run.Board.Count);

        // Buy what is left over instead and the same phase is a round: the
        // tower stands, the wave went, and one purse paid for both.
        Assert.Equal(
            1,
            run.Advance(
                BuildPhase
                    .Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, affordable))
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
            "A build phase at wave 1 upgrades at column 3, row 2 requiring a placed unit",
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
            Assert.Throws<SimulationException>(() => Acting(run, ActionKind.Place, Archer, 3, 1)).Message,
            StringComparison.Ordinal);

        Assert.Contains(
            "which is off a 15 by 9 map",
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
        Option first = run.Offering.Options[0];
        int tower = run.Costs.PriceOf(Purchase.Unit(Archer));

        BuildPhase phase = BuildPhase
            .Of(first.Kind, first.Id)
            .With(BuildAction.Of(ActionKind.Place, Archer, FreeColumn, FreeRow));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Resolved(run, phase, Unlocks.None, tower - 1));

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
            Assert.Throws<SimulationException>(() => Resolved(run, twice, Unlocks.None, tower)).Message,
            StringComparison.Ordinal);

        Assert.Equal(run.Board.Count + 2, Resolved(run, twice, Unlocks.None, tower * 2).Board.Count);
    }

    [Fact]
    public void An_upgrade_pays_the_targets_full_price_and_mints_no_new_placement_id()
    {
        // The full price of the row it names, and no ladder is read to get
        // there -- content/upgrades.txt's standing claim that the simulation
        // never walks one is not broken by a build phase that climbs a tower.
        // The placement keeps its identity, because that is what an upgrade
        // does to one.
        //
        // OBSERVED: price an upgrade at the difference between the two rows --
        // subtract the standing type's cost in BuildPhase.Applied. This goes
        // red on the spend, 52 against 92, and an upgrade becomes cheaper than
        // the tower it replaces by a rule nobody authored.
        Run run = TheBuild.Fresh();
        Option first = run.Offering.Options[0];
        int mage = run.Costs.PriceOf(Purchase.Unit(Mage));
        Board standing = Standing(run);

        Build built = Resolved(
            run,
            BuildPhase
                .Of(first.Kind, first.Id)
                .With(BuildAction.Of(ActionKind.Upgrade, Mage, StandingColumn, StandingRow)),
            Unlocks.None,
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

        Option first = run.Offering.Options[0];

        run.Advance(
            BuildPhase
                .Of(first.Kind, first.Id)
                .With(BuildAction.Of(ActionKind.Place, Soldier, FreeColumn, FreeRow)));

        Assert.Equal(1, run.Round);
        Assert.Equal(1, run.Board.Count);
        Assert.Equal(Soldier, run.Board.Placements[run.Board.Count - 1].Type.Id);
    }

    [Fact]
    public void What_a_phase_built_is_standing_when_that_rounds_waves_arrive()
    {
        // The purse walks the take, then the actions, then the slots, and the
        // board a round's incoming waves meet is the one that walk left behind.
        // A tower bought this round defends this round.
        //
        // OBSERVED: hand RoundOrders.Of the run's board rather than the build's
        // in Run.Advance. This goes red on the tower count of what was sent, 0
        // against 1, and every tower a run buys sits out the round it was
        // bought in.
        Run run = TheBuild.Fresh();
        Option first = run.Offering.Options[0];

        run.Advance(
            BuildPhase
                .Of(first.Kind, first.Id)
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
    /// A phase that takes the round's first option, does one thing to the
    /// board and sends nothing, out of a purse none of these assertions is
    /// about.
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
            BuildPhase
                .Of(run.Offering.Options[0].Kind, run.Offering.Options[0].Id)
                .With(BuildAction.Of(kind, typeId, column, row)),
            Unlocks.None,
            1000,
            board);

    /// <summary>
    /// A decision resolved against the round in front of a run, and against
    /// that run's own costs, roster, map and board.
    /// </summary>
    /// <remarks>
    /// The purse is an argument because most of these assertions are about what
    /// a decision costs, and the board is one because a run opens holding
    /// nothing; everything else comes off the run, because none of them are
    /// about where a roster or a map came from.
    /// </remarks>
    private static Build Resolved(Run run, BuildPhase phase, Unlocks unlocks, int gold, Board? board = null) =>
        phase.Resolve(
            run.Offering, unlocks, Purse.Holding(gold), run.Costs, run.Types, run.Map, board ?? run.Board);

    /// <summary>Every creep on a round's menu, unlocked, so a slot assertion is about the slot.</summary>
    private static Unlocks Everything(Offering offering)
    {
        Unlocks unlocks = Unlocks.None;

        for (int index = 0; index < offering.Count; index++)
        {
            unlocks = unlocks.With(offering.Options[index]);
        }

        return unlocks;
    }

    /// <summary>The ordinary half of every wave's menu, as text a comparison can read.</summary>
    private static string[] Menus(Run run) =>
        Enumerable.Range(1, 10)
            .Select(wave => string.Join(
                ",",
                run.OfferingAt(wave).Options
                    .Where(option => option.Kind == OptionKind.Ordinary)
                    .Select(option => option.Id.ToString(CultureInfo.InvariantCulture))))
            .ToArray();
}
