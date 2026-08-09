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
        TowerLayout defense = TheBuild.Defense();

        Assert.Equal(TheBuild.Named(mine.Offering), TheBuild.Named(theirs.Offering));

        // Two different opening rounds: different takes, and one of them sends
        // a wave while the other banks the round.
        mine.Advance(BuildPhase.Of(OptionKind.Ordinary, mine.Offering.Options[0].Id), defense);
        theirs.Advance(
            BuildPhase.Of(
                OptionKind.Ordinary,
                theirs.Offering.Options[1].Id,
                WaveSlot.Of(theirs.Offering.Options[1].TypeId, 2)),
            defense);

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
        TowerLayout defense = TheBuild.Defense();

        run.Advance(TheBuild.TakeFirst(run.Offering), defense);
        run.Advance(TheBuild.TakeFirst(run.Offering), defense);

        Option changerOption = run.Offering.Options.First(option => option.Kind == OptionKind.GameChanger);
        run.Advance(BuildPhase.Of(OptionKind.GameChanger, changerOption.Id, WaveSlot.Empty), defense);

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
        TowerLayout defense = TheBuild.Defense();
        Ruleset rules = TheBuild.RulesOffering(TheBuild.Ordinary);

        Assert.Equal(rules.StartingPurseGold, run.Purse.Gold);
        Assert.Equal(0, run.Unlocks.Count);

        Option first = run.Offering.Options[0];
        run.Advance(BuildPhase.Of(first.Kind, first.Id), defense);

        // Nothing was bought, so the purse is what it opened with plus the wave.
        Assert.Equal(rules.StartingPurseGold + 10 + rules.IncomeBasePerWave, run.Purse.Gold);
        Assert.Equal(1, run.Unlocks.Count);
        Assert.True(run.Unlocks.Has(first.TypeId));

        // And it is permanent: three rounds later, with three other takes in
        // between, the first one is still fieldable.
        while (!run.IsOver)
        {
            run.Advance(TheBuild.TakeFirst(run.Offering), defense);
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
        Build built = BuildPhase.Of(
            opening.Options[0].Kind,
            opening.Options[0].Id,
            WaveSlot.Of(opening.Options[0].TypeId, 4))
            .Resolve(opening, Unlocks.None, Purse.Holding(1000), run.Costs);

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
        TowerLayout defense = TheBuild.Defense();
        Option first = run.Offering.Options[0];

        // One filled, one empty.
        run.Advance(
            BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 2), WaveSlot.Empty),
            defense);

        Assert.Equal(2, run.Sent[0].Wave.TotalUnits);
        Assert.Equal(1, run.Sent[0].Wave.Count);

        // Every slot empty, which is the whole round banked.
        run.Advance(TheBuild.TakeFirst(run.Offering, WaveSlot.Empty, WaveSlot.Empty), defense);

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
            () => BuildPhase.Of(OptionKind.Ordinary, absent)
                .Resolve(offering, Unlocks.None, Purse.Holding(1000), run.Costs));

        Assert.Contains("which that round's offering does not carry", thrown.Message, StringComparison.Ordinal);

        // The kind is part of the identity, so an ordinary option's id taken as
        // a game changer is just as absent -- wave 1 has no game changers at all.
        Assert.Throws<SimulationException>(
            () => BuildPhase.Of(OptionKind.GameChanger, offering.Options[0].Id)
                .Resolve(offering, Unlocks.None, Purse.Holding(1000), run.Costs));
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
            () => BuildPhase.Of(taken.Kind, taken.Id, WaveSlot.Of(never, 1))
                .Resolve(offering, Unlocks.None, Purse.Holding(1000), run.Costs));

        Assert.Contains("which this run never unlocked", thrown.Message, StringComparison.Ordinal);

        // What this round took is fieldable this round: the take and the buy
        // are one decision over one purse.
        Build built = BuildPhase.Of(taken.Kind, taken.Id, WaveSlot.Of(taken.TypeId, 1))
            .Resolve(offering, Unlocks.None, Purse.Holding(1000), run.Costs);

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
            () => BuildPhase.Of(
                offering.Options[0].Kind,
                offering.Options[0].Id,
                WaveSlot.Empty,
                WaveSlot.Empty,
                WaveSlot.Empty)
                .Resolve(offering, everything, Purse.Holding(1000), run.Costs));

        Assert.Contains("slots where that round has 2", thrown.Message, StringComparison.Ordinal);

        // Exactly the width is fine, and so is fewer than it.
        int[] creeps = offering.Options.Select(option => option.TypeId).OrderBy(id => id).ToArray();

        Assert.Equal(
            2,
            BuildPhase.Of(
                offering.Options[0].Kind,
                offering.Options[0].Id,
                WaveSlot.Of(creeps[0], 1),
                WaveSlot.Of(creeps[1], 1))
                .Resolve(offering, everything, Purse.Holding(1000), run.Costs)
                .Wave.Count);
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
        CostTable costs = run.Costs;

        BuildPhase phase = BuildPhase.Of(
            offering.Options[0].Kind,
            offering.Options[0].Id,
            WaveSlot.Of(creeps[0], 4),
            WaveSlot.Of(creeps[1], 4));

        int bill = phase.Resolve(offering, everything, Purse.Holding(int.MaxValue), costs).Spent;

        Assert.True(bill > 1, "The two slots priced at nothing, so there is no affordability to test.");

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => phase.Resolve(offering, everything, Purse.Holding(bill - 1), costs));

        Assert.Contains("There is no credit in this economy", thrown.Message, StringComparison.Ordinal);

        // One gold more and the same wave is fine, and the purse is what is
        // left rather than what was there.
        Build built = phase.Resolve(offering, everything, Purse.Holding(bill), costs);

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
            () => BuildPhase.Of(
                offering.Options[0].Kind,
                offering.Options[0].Id,
                WaveSlot.Of(creeps[0], 1),
                WaveSlot.Of(creeps[0], 1))
                .Resolve(offering, everything, Purse.Holding(1000), run.Costs));

        Assert.Contains("at or below the", repeated.Message, StringComparison.Ordinal);

        SimulationException descending = Assert.Throws<SimulationException>(
            () => BuildPhase.Of(
                offering.Options[0].Kind,
                offering.Options[0].Id,
                WaveSlot.Of(creeps[1], 1),
                WaveSlot.Of(creeps[0], 1))
                .Resolve(offering, everything, Purse.Holding(1000), run.Costs));

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
        run.Advance(BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 1)), defense: TheBuild.Defense());

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
        TowerLayout defense = TheBuild.Defense();
        AnchorSchedule schedule = TheSchedule.Committed();

        run.Advance(TheBuild.TakeFirst(run.Offering), defense);
        run.Advance(TheBuild.TakeFirst(run.Offering), defense);

        Option steepless = run.Offering.Options.First(option => option.Kind == OptionKind.GameChanger);
        run.Advance(BuildPhase.Of(OptionKind.GameChanger, steepless.Id, WaveSlot.Empty), defense);

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
        // instead of the build's, in Run.Advance(BuildPhase, TowerLayout). The
        // unlock-count assertion goes red, 10 against 0, and every round of the
        // run decides against a run that has never taken anything and never
        // spent a coin.
        Run run = TheBuild.Fresh();
        TowerLayout defense = TheBuild.Defense();
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

            // The same surface a stored command stream is checked against,
            // called here for what the wave came to before the round pays out.
            spent.Add(phase.Resolve(offering, run.Unlocks, run.Purse, run.Costs).Spent);

            run.Advance(phase, defense);
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
        // RequireUnfinished call in Run.Advance(BuildPhase, TowerLayout). The
        // first half goes red, 210 against 193: a finished run pays for a wave
        // it refused to send.
        //
        // OBSERVED: put those two assignments below RequireUnfinished instead,
        // with the RoundOrders.Of call moved down past them. The first half
        // stays green and the second goes red, 0 against 1, because the defense
        // is the last thing that can refuse a round and it is the only refusal
        // that mutation leaves standing.
        TowerLayout defense = TheBuild.Defense();
        Run over = TheBuild.Fresh(waves: 1);

        over.Advance(TheBuild.TakeFirst(over.Offering), defense);
        Assert.True(over.IsOver);

        int purse = over.Purse.Gold;
        int unlocks = over.Unlocks.Count;

        // A phase that would have been perfectly legal on a run with a round
        // left in it: a take off wave two's menu, and a slot of the creep wave
        // one unlocked.
        Option next = over.Offering.Options[0];
        BuildPhase past = BuildPhase.Of(
            next.Kind, next.Id, WaveSlot.Of(over.Unlocks.Taken[0].TypeId, 1));

        Assert.Throws<SimulationException>(() => over.Advance(past, defense));

        Assert.Equal(purse, over.Purse.Gold);
        Assert.Equal(unlocks, over.Unlocks.Count);

        // And a round with no defense standing is refused the same way.
        Run alive = TheBuild.Fresh(waves: 2);
        Option first = alive.Offering.Options[0];

        Assert.Throws<ArgumentNullException>(
            () => alive.Advance(
                BuildPhase.Of(first.Kind, first.Id, WaveSlot.Of(first.TypeId, 1)),
                defense: null!));

        Assert.Equal(0, alive.Unlocks.Count);
        Assert.Equal(TheBuild.RulesOffering(TheBuild.Ordinary).StartingPurseGold, alive.Purse.Gold);
    }

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
