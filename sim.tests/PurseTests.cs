using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The purse: one currency, banking, interest, the flat base and the bonus.
/// </summary>
/// <remarks>
/// <para>
/// <b>The economy is tested as folds over a run's data.</b> A purse is a value,
/// so a run of waves is a sequence of purses and every assertion here is a
/// statement about that sequence rather than about a mutation somewhere. No
/// match is simulated: the economy is arithmetic over what a round recorded.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class PurseTests
{
    [Fact]
    public void One_currency_buys_defense_and_offense_out_of_one_wallet()
    {
        // A tower and a creep bought one after the other, and the second
        // purchase is measured against what the first left behind. That single
        // running balance is the design: there is no second wallet anywhere in
        // this surface for the offense to be funded out of.
        //
        // OBSERVED: price every unit at a literal 10 in CostTable.PriceOf. The
        // creeps assertion goes red, 20 against 50, because a tower that cost
        // the same as a grunt leaves a wallet nothing was traded out of.
        CostTable costs = TheRuleset.Costs();
        Purse purse = Purse.Holding(100);

        Purse afterTower = purse.Spend(costs, Purchase.Unit(3), 1);
        Assert.Equal(60, afterTower.Gold);

        Purse afterCreeps = afterTower.Spend(costs, Purchase.Unit(1), 4);
        Assert.Equal(20, afterCreeps.Gold);

        // And the thing that is not a unit at all comes out of the same twenty,
        // which is not enough for it. Scouting competes with the board for
        // gold because there is nowhere else for it to be funded from.
        Assert.Equal(25, costs.PriceOf(Purchase.Snapshot));
        Assert.Throws<SimulationException>(() => afterCreeps.Spend(costs, Purchase.Snapshot, 1));
    }

    [Fact]
    public void Spending_more_than_the_purse_holds_is_refused_rather_than_borrowed()
    {
        // OBSERVED: let Spend return Holding(Gold - price) with no comparison.
        // This goes red having caught nothing, and the refusal that fires
        // instead is Holding's -- one layer late, naming a negative purse rather
        // than the purchase that emptied it.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Purse.Holding(39).Spend(TheRuleset.Costs(), Purchase.Unit(3), 1));

        Assert.Contains("was spent 40", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_purse_cannot_hold_less_than_nothing()
    {
        // OBSERVED: drop the guard in Purse.Holding. This goes red having caught
        // nothing, and a purse of -1 gold is a debt in an economy that has no
        // credit in it.
        Assert.Throws<SimulationException>(() => Purse.Holding(-1));
        Assert.Equal(0, Purse.Empty.Gold);
    }

    [Fact]
    public void One_gold_banked_earns_one_and_not_none()
    {
        // The rounding direction, at the only boundary where the two directions
        // differ by the whole of the effect. Ten percent of one gold is a
        // tenth: rounded up it is a coin, truncated it is nothing at all and a
        // small bank never grows.
        //
        // OBSERVED: compute the interest as `bank * percent / 100` -- truncating
        // -- in Purse.InterestOn. This goes red, 1 against 0, and the
        // compounding fold below goes red at its third wave, 134 against 133.
        Ruleset rules = InterestOnly();

        Assert.Equal(1, Purse.Holding(1).CloseWave(rules, 0, 0).Interest);
        Assert.Equal(1, Purse.Holding(10).CloseWave(rules, 0, 0).Interest);
        Assert.Equal(2, Purse.Holding(11).CloseWave(rules, 0, 0).Interest);

        // And nothing at all earns nothing, which is the one case rounding up
        // must not invent a coin for.
        Assert.Equal(0, Purse.Empty.CloseWave(rules, 0, 0).Interest);
    }

    [Fact]
    public void Unspent_gold_compounds_wave_after_wave()
    {
        // The fold. A hundred gold left alone for ten waves at ten percent, and
        // the sequence written out rather than recomputed by the same expression
        // the simulation uses -- an oracle that calls the thing under test is not
        // an oracle.
        //
        // OBSERVED: leave the interest out of the closing balance in
        // Purse.CloseWave -- itemised on the payment and never banked, which is
        // the shape of a report that agrees with nothing. This goes red at the
        // first wave, [110, 121, 134, ...] against [100, 100, 100, ...], and
        // takes four other tests in this file with it.
        Ruleset rules = InterestOnly();
        var banked = new List<int>();
        Purse purse = Purse.Holding(100);

        for (int wave = 0; wave < 10; wave++)
        {
            purse = purse.CloseWave(rules, 0, 0).Purse;
            banked.Add(purse.Gold);
        }

        Assert.Equal(
            new[] { 110, 121, 134, 148, 163, 180, 198, 218, 240, 264 },
            banked);

        // Compounding is what makes an empty wave slot an investment: ten waves
        // of it is more than ten waves of simple interest on the opening bank.
        Assert.True(
            banked[9] > 100 + (10 * 10),
            "A hundred gold left alone for ten waves grew to "
            + banked[9].ToString(CultureInfo.InvariantCulture)
            + ", which is no more than simple interest, so nothing is compounding.");
    }

    [Fact]
    public void The_interest_rate_is_read_from_the_ruleset_and_not_from_the_code()
    {
        // The same purse through two rulesets that differ in one authored digit.
        //
        // OBSERVED: replace rules.InterestPercentPerWave with a literal 10 in
        // Purse.InterestOn. The second assertion goes red, 200 against 100,
        // which is a rate nobody can tune without a compile.
        Assert.Equal(
            100,
            Purse.Holding(1000).CloseWave(InterestOnly(), 0, 0).Interest);

        Assert.Equal(
            200,
            Purse.Holding(1000)
                .CloseWave(
                    Ruleset.Parse(PlantedText.Replace(InterestOnlyText(), "interest 10 0", "interest 20 0")),
                    0, 0)
                .Interest);
    }

    [Fact]
    public void The_flat_base_lands_once_a_wave_and_comes_out_of_the_ruleset()
    {
        // OBSERVED: pay `rules.IncomeBasePerWave / 2` in Purse.CloseWave. The
        // first assertion goes red, 168 against 84, and every run in the project
        // is quietly poorer than the file it was authored in says.
        Ruleset rules = TheRuleset.Committed();
        WavePayment paid = Purse.Empty.CloseWave(rules, 0, 0);

        Assert.Equal(rules.IncomeBasePerWave, paid.IncomeBase);
        Assert.Equal(168, paid.IncomeBase);
        Assert.Equal(168, paid.Purse.Gold);

        Assert.Equal(
            250,
            Purse.Empty
                .CloseWave(
                    Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "income 100", "income 250")),
                    0, 0)
                .IncomeBase);
    }

    [Fact]
    public void A_wave_that_got_nothing_past_is_paid_its_base_and_no_bonus()
    {
        // An empty wave, or a wave every opponent's wall stopped whole. The
        // base still arrives in full, which is the half that distinguishes
        // "this wave did nothing" from "the payment is broken".
        //
        // OBSERVED: pay the base as the bonus in Purse.BonusOn when nothing was
        // dealt. The bonus assertion goes red, 0 against 168 -- a build paying
        // a performance bonus for a wave that performed.
        Ruleset rules = TheRuleset.Committed();
        WavePayment paid = Purse.Holding(500).CloseWave(rules, 0, 0);

        Assert.Equal(0, paid.Bonus);
        Assert.Equal(rules.IncomeBasePerWave, paid.IncomeBase);
        Assert.Equal(50, paid.Interest);
        Assert.Equal(218, paid.Total);
        Assert.Equal(718, paid.Purse.Gold);
    }

    [Fact]
    public void A_wave_is_paid_for_the_damage_it_dealt_and_twice_the_damage_pays_twice()
    {
        // The whole of the rule. These are the leak costs the committed run's
        // rounds 4, 6, 9 and 10 dealt: under the four percentile bands this
        // replaced, all four were paid the same 33 gold, and eighteen times the
        // damage bought nothing.
        //
        // OBSERVED: pay `rules.IncomeBasePerWave * rules.BonusPercentOfLeakCost
        // / 100` in Purse.BonusOn -- a share of the base rather than of what the
        // wave dealt. Every assertion below goes red at a flat 42, which is the
        // failure this ticket is about wearing a different number.
        Ruleset rules = TheRuleset.Committed();

        Assert.Equal(0, Bonus(rules, 0));
        Assert.Equal(9, Bonus(rules, 36));
        Assert.Equal(18, Bonus(rules, 72));
        Assert.Equal(49, Bonus(rules, 198));
        Assert.Equal(104, Bonus(rules, 416));
        Assert.Equal(168, Bonus(rules, 673));

        // Truncated where it is paid, so a wave dealing less than one bonus
        // gold's worth is paid nothing rather than rounded up into a coin.
        Assert.Equal(0, Bonus(rules, 3));
        Assert.Equal(1, Bonus(rules, 4));

        // And the rate is authored rather than compiled.
        //
        // OBSERVED: replace rules.BonusPercentOfLeakCost with a literal 25 in
        // Purse.BonusOn. The second assertion goes red, 9 against 18.
        Assert.Equal(9, Bonus(Ruleset.Parse(TheRuleset.Minimal), 36));
        Assert.Equal(
            18,
            Bonus(Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "bonus 25", "bonus 50")), 36));
    }

    [Fact]
    public void The_whole_price_of_a_wave_is_a_ceiling_on_what_that_wave_can_be_paid()
    {
        // What a walk over a stored stream folds, because it has not played the
        // rounds and cannot know what any of them dealt. An uncapped bonus still
        // has a bound: a leak costs its creep's price one for one, so a round
        // deals at most the price of every creep it sent, which is a number the
        // stored decision carries. Bounded above, a walk refuses only decisions
        // no run could have afforded however well it played; bounded below it
        // would refuse waves a run's own bonus paid for.
        //
        // OBSERVED: sum `order.Count` rather than the order's price in
        // WaveScript.FullPrice. The price assertion goes red at 14 against 130,
        // and with that assertion taken out the sweep goes red on its own at 16
        // gold dealt, 222 against a ceiling of 221 -- a ceiling a real round of
        // this wave walks straight through.
        //
        // THE SWEEP'S BOUND IS THE LITERAL AND NOT THE METHOD, which is the
        // whole of why it can see that. Walked to wave.FullPrice(costs) it
        // sweeps only as far as the number under test says a round can reach,
        // so a ceiling that collapsed to 14 would be swept to 14 and agree with
        // itself; the edit above then goes red on the price assertion alone.
        Ruleset rules = TheRuleset.Committed();
        CostTable costs = TheRuleset.Costs();
        WaveScript wave = WaveScript.Parse(
            """
            order   0  1   4  0
            order  45  2  10  0
            """,
            TheMatch.Types());

        // Four minions at ten gold and ten scouts at nine, added up here rather
        // than asked for -- an oracle that calls the thing under test is not one.
        Assert.Equal(130, wave.FullPrice(costs));

        WavePayment best = Purse.Holding(500).CloseWaveAtBest(rules, wave.FullPrice(costs), 0);

        for (int dealt = 0; dealt <= 130; dealt++)
        {
            WavePayment paid = Purse.Holding(500).CloseWave(rules, dealt, 0);

            Assert.True(
                paid.Total <= best.Total,
                "A wave that dealt "
                + dealt.ToString(CultureInfo.InvariantCulture)
                + " was paid "
                + paid.Total.ToString(CultureInfo.InvariantCulture)
                + " against a ceiling of "
                + best.Total.ToString(CultureInfo.InvariantCulture)
                + ", so the ceiling is not one.");

            Assert.Equal(best.Interest, paid.Interest);
            Assert.Equal(best.IncomeBase, paid.IncomeBase);
        }

        // Reached rather than merely never exceeded: a wave every creep of which
        // leaked against every opponent is paid exactly the ceiling.
        Assert.Equal(32, best.Bonus);
        Assert.Equal(best.Total, Purse.Holding(500).CloseWave(rules, 130, 0).Total);
    }

    [Fact]
    public void What_a_run_earned_for_its_offense_is_a_fold_over_its_outcome_vector()
    {
        // The whole reason the outcome is a vector: what every round of a run
        // paid is arithmetic over what was stored, so a retrospective replays no
        // tick and resolves no match. The vector here is written out rather than
        // played, which is the point -- nothing below is simulated.
        //
        // OBSERVED: fold LeakCostTaken rather than LeakCostDealt in
        // Purse.BonusOver. This goes red, 75 against 20: the bonus starts paying
        // for what got past the run rather than for what the run got past the
        // field.
        Ruleset rules = TheRuleset.Committed();

        var rounds = new[]
        {
            new RoundOutcome(45, 100, 0),
            new RoundOutcome(35, 100, 0),
            new RoundOutcome(5, 100, 0),
        };

        RunOutcome outcome = RunOutcome.Of(1500, rounds, 3, deathEndsTheRun: true);

        Assert.Equal(20, Purse.BonusOver(rules, outcome));

        // Round by round rather than over the total, because a round's bonus is
        // truncated where it is paid. Eleven, eight and one, which is one gold
        // short of the rate applied once to the eighty-five they add up to.
        //
        // OBSERVED: fold outcome.LeakCostDealt once instead of walking the
        // rounds. This goes red, 21 against 20 -- a retrospective that says a
        // run earned a coin nobody handed it.
        Assert.Equal(11, Bonus(rules, 45));
        Assert.Equal(8, Bonus(rules, 35));
        Assert.Equal(1, Bonus(rules, 5));
        Assert.Equal(21, Bonus(rules, 85));

        // The same answer off a vector rebuilt from stored rounds, which is what
        // a stored population of runs is made of.
        Assert.Equal(
            20,
            Purse.BonusOver(rules, RunOutcome.Of(1500, outcome.Rounds, 3, deathEndsTheRun: true)));
    }

    [Fact]
    public void Doing_better_never_pays_less_and_no_wave_is_ever_charged_for_attacking()
    {
        // A thousand leak costs, swept. The bonus never falls as a wave does
        // better and never goes below nothing, which is what "the run's own
        // offense cannot take gold off it" means once the payment is a
        // multiplication rather than a lookup.
        //
        // OBSERVED, in two steps because the content refuses the wrong input on
        // its own -- which is the first line of this defence. Open the bonus
        // rate's range at int.MinValue, then author "bonus         -25" in
        // content/ruleset.txt. The first assertion goes red at one gold dealt,
        // saying a wave that dealt 4 was paid -1, and a run gets poorer the
        // better its wave does.
        Ruleset rules = TheRuleset.Committed();
        int previous = int.MinValue;

        for (int dealt = 0; dealt <= 1000; dealt++)
        {
            int bonus = Bonus(rules, dealt);

            Assert.True(
                bonus >= 0,
                "A wave that dealt "
                + dealt.ToString(CultureInfo.InvariantCulture)
                + " was paid "
                + bonus.ToString(CultureInfo.InvariantCulture)
                + ", which is a penalty written as a bonus.");

            Assert.True(
                bonus >= previous,
                "A wave that dealt "
                + dealt.ToString(CultureInfo.InvariantCulture)
                + " was paid less than one that dealt less.");

            previous = bonus;
        }

        // And a round recorded below zero is refused rather than multiplied into
        // a negative payment, which is the one input the sweep above cannot see.
        //
        // OBSERVED: drop the guard in Purse.BonusOn. This goes red having caught
        // nothing, and a round stored at -100 pays -25.
        SimulationException thrown = Assert.Throws<SimulationException>(() => Bonus(rules, -1));

        Assert.Contains("charged for attacking", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_field_ranks_a_wave_by_the_spread_and_never_by_who_is_first()
    {
        // A field is a spread of amounts with no identities in it: the order its
        // rounds arrive in cannot change where a wave sits, and reading it takes
        // nothing off anybody -- the same field answers the same question the
        // same way however many waves are ranked against it.
        //
        // NOTHING IN THIS BUILD PRICES ANYTHING OFF IT. A wave is paid a share
        // of what it dealt, so this is a measurement with no consumer; see
        // docs/adr/0042, which is open for a decision on whether it is kept.
        //
        // OBSERVED: compare against `_dealt[0]` rather than `_dealt[index]` in
        // PerformanceField.PercentileOf -- a named opponent rather than a
        // distribution. The reordering assertion goes red, 0 against 75.
        PerformanceField ascending = PerformanceField.Of(new[] { 10, 20, 30, 40 });
        PerformanceField shuffled = PerformanceField.Of(new[] { 40, 10, 30, 20 });

        Assert.Equal(ascending.PercentileOf(35), shuffled.PercentileOf(35));

        // Measured twice, and again after somebody else has been measured
        // against it.
        Assert.Equal(75, ascending.PercentileOf(35));
        Assert.Equal(100, ascending.PercentileOf(45));
        Assert.Equal(75, ascending.PercentileOf(35));
        Assert.Equal(4, ascending.Size);
    }

    [Fact]
    public void A_field_of_nobody_has_no_percentile_to_report()
    {
        // OBSERVED: drop the IsPresent guard in PerformanceField.PercentileOf.
        // This goes red with a DivideByZeroException where a named refusal
        // should be -- the runtime saying what happened rather than the
        // simulation saying what it means.
        SimulationException thrown =
            Assert.Throws<SimulationException>(() => PerformanceField.Absent.PercentileOf(10));

        Assert.Contains("field of nobody", thrown.Message, StringComparison.Ordinal);

        // A field built from no rounds is the absent one rather than a second
        // empty thing that behaves slightly differently.
        Assert.False(PerformanceField.Of(new int[0]).IsPresent);
    }

    [Fact]
    public void A_round_recorded_as_having_dealt_less_than_nothing_is_refused()
    {
        // A leak costs its creep's price one for one and a price is never
        // negative, so a negative round is a subtraction performed twice.
        //
        // OBSERVED: drop the guard in PerformanceField.Amount. Both rows go red
        // having caught nothing, and a round of -1 sits below every real one in
        // the distribution it was never meant to enter.
        Assert.Throws<SimulationException>(() => PerformanceField.Of(new[] { 10, -1 }));
        Assert.Throws<SimulationException>(() => PerformanceField.Of(new[] { 10 }).PercentileOf(-1));
    }

    [Fact]
    public void A_run_with_the_round_cap_lifted_and_no_interest_cap_is_refused_by_name()
    {
        // Interest is a share of the bank paid every wave, so the bank grows
        // geometrically and the only thing bounding it is how many waves there
        // are. Lifting the round cap therefore forces a ceiling on the interest,
        // and a run configured with neither is refused before it resolves
        // anything rather than discovered later as an exploding number.
        //
        // OBSERVED: return early from Purse.RequireBoundedCompounding before the
        // throw. This goes red having caught nothing -- and one single gold,
        // banked at ten percent and never spent, leaves the range of a 32-bit
        // purse in 207 waves. That is the failure this refusal exists instead
        // of.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Purse.RequireBoundedCompounding(TheRuleset.Committed(), Purse.RoundCapLifted));

        Assert.Contains("no round cap", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("no interest cap", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_run_is_allowed_when_either_the_rounds_or_the_interest_is_bounded()
    {
        // The three configurations that are fine, so that the refusal above is
        // known to be about the pair rather than about either half.
        //
        // OBSERVED: refuse unconditionally in Purse.RequireBoundedCompounding.
        // This goes red on the first of the three, which is what a rule that had
        // been widened into "no run may compound at all" looks like.
        Ruleset uncapped = TheRuleset.Committed();
        Ruleset capped = Ruleset.Parse(
            PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 500"));

        Purse.RequireBoundedCompounding(uncapped, 10);
        Purse.RequireBoundedCompounding(capped, Purse.RoundCapLifted);
        Purse.RequireBoundedCompounding(capped, 10);

        Assert.Equal(Ruleset.NoInterestCeiling, uncapped.InterestCapGold);
        Assert.Equal(500, capped.InterestCapGold);
    }

    [Fact]
    public void A_run_of_a_negative_number_of_rounds_is_refused()
    {
        // The cap being lifted is written as zero, so a negative length is a
        // sentinel somebody invented rather than the one this rule knows.
        //
        // OBSERVED: drop the negative guard. This goes red having caught
        // nothing, and a run of -1 rounds passes the compounding check by not
        // being the lifted-cap value.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Purse.RequireBoundedCompounding(TheRuleset.Committed(), -1));

        Assert.Contains("-1 rounds", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_interest_cap_is_the_ceiling_on_what_one_wave_pays()
    {
        // What the cap buys: growth that is linear rather than geometric, which
        // is what makes an uncapped run's refusal answerable by authoring one.
        //
        // OBSERVED: ignore rules.InterestCapGold in Purse.InterestOn. The first
        // assertion goes red, 5 against 100, and the ceiling a lifted round cap
        // forces somebody to author does nothing at all.
        Ruleset capped = Ruleset.Parse(PlantedText.Replace(
            PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 5"),
            "income 100",
            "income 0"));

        Assert.Equal(5, Purse.Holding(1000).CloseWave(capped, 0, 0).Interest);

        // Under the ceiling it is the rate that answers, not the cap.
        Assert.Equal(4, Purse.Holding(40).CloseWave(capped, 0, 0).Interest);

        var banked = new List<int>();
        Purse purse = Purse.Holding(1000);

        for (int wave = 0; wave < 4; wave++)
        {
            purse = purse.CloseWave(capped, 0, 0).Purse;
            banked.Add(purse.Gold);
        }

        Assert.Equal(new[] { 1005, 1010, 1015, 1020 }, banked);
    }

    [Fact]
    public void A_bank_that_compounds_out_of_a_purse_is_a_throw_and_not_a_wrap()
    {
        // The hazard the compounding refusal above exists about, met head on.
        // The interest is taken in a long and the purse is an int, so this is
        // the one place the arithmetic leaves its range -- and a wrapped balance
        // is a purse that went bankrupt by getting rich.
        //
        // OBSERVED: drop the range check and hand the closing balance to
        // Holding as an unchecked cast. The refusal fires one layer later, from
        // Holding, naming a purse of -1932735284 -- so this assertion goes red
        // on the message rather than on the throw. That is why the refusal is
        // asserted by name: a run that got rich enough to go bankrupt should say
        // which arithmetic did it.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => Purse.Holding(int.MaxValue).CloseWave(InterestOnly(), 0, 0));

        Assert.Contains("does not fit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_pays_its_interest_its_base_and_its_bonus_and_the_purse_is_the_sum_of_them()
    {
        // The payment is itemised rather than summed, because the three lines
        // answer different questions -- and this is the assertion that the
        // itemisation and the balance cannot drift apart.
        //
        // OBSERVED: leave the interest out of the closing balance in
        // Purse.CloseWave. The last assertion goes red, 546 against 512: the
        // three lines still add up and the purse no longer agrees with them.
        Ruleset rules = TheRuleset.Committed();
        WavePayment paid = Purse.Holding(333).CloseWave(rules, 45, 0);

        Assert.Equal(333, paid.Opening);
        Assert.Equal(34, paid.Interest);
        Assert.Equal(168, paid.IncomeBase);
        Assert.Equal(11, paid.Bonus);
        Assert.Equal(213, paid.Total);
        Assert.Equal(paid.Opening + paid.Total, paid.Purse.Gold);
    }

    /// <summary>What a wave that dealt this much is paid on top of the base.</summary>
    private static int Bonus(Ruleset rules, int leakCostDealt) =>
        Purse.Empty.CloseWave(rules, leakCostDealt, 0).Bonus;

    /// <summary>
    /// The minimal ruleset with the income base taken to nothing, so that a fold
    /// over waves shows the bank compounding and nothing else.
    /// </summary>
    private static string InterestOnlyText() =>
        PlantedText.Replace(TheRuleset.Minimal, "income 100", "income 0");

    private static Ruleset InterestOnly() => Ruleset.Parse(InterestOnlyText());
}
