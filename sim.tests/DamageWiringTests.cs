namespace Sim.Tests;

/// <summary>
/// The tick loop's half of the damage model: that a shot is resolved through
/// the ruleset where it lands, once, and that a table with no types in it still
/// plays.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="DamageTests"/> is the expression as arithmetic. This is the
/// expression as the thing a match actually does with a roll, which is a
/// separate claim: a pure function nothing calls changes no damage number
/// anywhere, and that was the state of this model before it was wired.
/// </para>
/// <para>
/// <b>Every assertion here was watched failing under a deliberately wrong
/// input</b>, and the wrong input is written above it so the observation can be
/// repeated.
/// </para>
/// </remarks>
public class DamageWiringTests
{
    /// <summary>One bolt, which is a pierce hitscan tower, on the first tower cell.</summary>
    private const string OneBolt = "tower 3 6 2";

    /// <summary>One mortar, which is the impact tower wave nine's anchor is answered by.</summary>
    private const string OneMortar = "tower 4 7 9";

    /// <summary>The same mortar with a bolt beside it, which answers no anchor.</summary>
    private const string MortarAndBolt = "tower 3 6 2\ntower 4 7 9";

    [Fact]
    public void A_shot_lands_the_roll_resolved_through_the_fused_expression()
    {
        // One Archer against a column of Skeleton Warriors: one attack type,
        // one armour type, and forty-five points of armour, so the divisor is
        // 145 and the fused form is a different function from the two-step one
        // on nearly half the rolls. Reconstructing the stream and resolving
        // each roll here reproduces every amount the match reported.
        //
        // OBSERVED: subtract the roll instead of the dealt amount -- assign
        // `roll` to `amount` in Match.Damage and report that to the events. The
        // first landing goes red, 56 against 117, and 117 is the raw roll:
        // which is what a fully tested model nothing calls looks like from the
        // outside.
        //
        // OBSERVED: write the same algebra in two steps -- divide by the armour
        // denominator once on each side of it in DamageModel.Mitigate. A
        // landing where the two truncations disagree goes red, 56 against 55,
        // which is the 42.7% of triples the prototype swept.
        UnitTypeTable types = TheMatch.Types();
        Ruleset rules = TheRuleset.Committed();
        UnitType bolt = types.ById(3);
        UnitType bulwark = types.ById(13);

        var events = new TheMatch.EventLog();

        new Match(
            TheMatch.Map(),
            rules,
            TowerLayout.Parse(OneBolt, types),
            WaveScript.Parse("order 0 13 20 0", types),
            TheMatch.Seed)
            .Resolve(events);

        var dice = new Pcg32(TheMatch.Seed);
        int landings = 0;
        int truncationsThatDisagree = 0;

        for (int index = 0; index < events.Count; index++)
        {
            if (events.Kinds[index] != "fired")
            {
                continue;
            }

            int roll = dice.NextInRange(bolt.DamageMin, bolt.DamageMax + 1);

            // A hitscan shot lands inside the call that fired it, so its damage
            // event is the very next thing in the stream, or the shot found
            // nothing and there is no event at all.
            if (index + 1 >= events.Count || events.Kinds[index + 1] != "damaged")
            {
                continue;
            }

            // Written out here rather than taken from DamageModel, so that a
            // second truncation introduced inside the model moves one side of
            // this comparison and not both.
            int cell = rules.Matrix.Cell(bolt.AttackType, bulwark.ArmourType);
            int denominator = rules.ArmourDenominator + (rules.ArmourPercentPerPoint * bulwark.Armour);
            int fused = roll * cell / denominator;
            int twoStep = roll * cell / rules.ArmourDenominator * rules.ArmourDenominator / denominator;

            Assert.Equal(fused, events.Amounts[index + 1]);

            landings++;
            truncationsThatDisagree += fused == twoStep ? 0 : 1;
        }

        Assert.True(landings > 30, "Too few landings for this to have proved anything: " + landings + ".");

        Assert.True(
            truncationsThatDisagree > 0,
            "The fused and the two-step forms agreed on every one of the "
            + landings
            + " landings, so this proved nothing about which of them the tick loop runs.");
    }

    [Fact]
    public void An_untyped_table_still_simulates_and_its_shots_resolve_untyped()
    {
        // The oldest golden bundle is pinned to a table written before the type
        // columns existed, and it is replayed forever. A shot out of such a
        // table has no row of the matrix and its target has no column, so there
        // is no cell to resolve through and the roll is what lands -- which is
        // the whole of why those bytes still produce the numbers they were
        // recorded at. The ruleset is in the match's hand throughout and is
        // never consulted.
        //
        // OBSERVED: delete the untyped branch from Match.Dealt, so that an
        // untyped shot falls through to the half-typed refusal below it. This
        // goes red with "bolt (#3) is shooting grunt (#1), and exactly one of
        // them is in the damage matrix", and content/golden/defense-0 becomes a
        // record nothing can play.
        UnitTypeTable untyped = UnitTypeTable.Parse(
            "golden/defense-0.units",
            File.ReadAllText(RepoLayout.GoldenUnitsFile(0)));

        Assert.Equal(UnitTypeTable.DefaultLayout, untyped.Layout);
        Assert.All(untyped.Types, row => Assert.Equal(AttackType.None, row.AttackType));
        Assert.All(untyped.Types, row => Assert.Equal(ArmourType.None, row.ArmourType));

        TowerLayout defense = TowerLayout.Parse(OneBolt, untyped);
        WaveScript wave = WaveScript.Parse("order 0 1 12 0", untyped);
        UnitType bolt = untyped.ById(3);

        var events = new TheMatch.EventLog();

        new Match(TheMatch.Map(), TheRuleset.Committed(), defense, wave, TheMatch.Seed)
            .Resolve(events);

        var dice = new Pcg32(TheMatch.Seed);
        int landings = 0;

        for (int index = 0; index < events.Count; index++)
        {
            if (events.Kinds[index] != "fired")
            {
                continue;
            }

            int roll = dice.NextInRange(bolt.DamageMin, bolt.DamageMax + 1);

            if (index + 1 >= events.Count || events.Kinds[index + 1] != "damaged")
            {
                continue;
            }

            Assert.Equal(roll, events.Amounts[index + 1]);
            landings++;
        }

        Assert.True(landings > 30, "Too few landings for this to have proved anything: " + landings + ".");
    }

    [Fact]
    public void A_shot_typed_on_one_side_only_is_refused()
    {
        // One table cannot author it: a unit that attacks carries an attack type
        // and a unit that can be damaged carries an armour type, both checked at
        // load. Two tables can, because a defense and a wave are parsed
        // separately -- and a half-typed shot has no cell it could mean, so it
        // is a throw rather than a number somebody picked.
        //
        // OBSERVED: return the roll whenever either side is untyped -- make the
        // first branch of Match.Dealt an `||`. This goes red on the exception
        // that never arrives, "Assert.Throws() Failure: No exception was
        // thrown", and a defense read out of one table shooting a wave read out
        // of another quietly resolves to raw rolls.
        UnitTypeTable typed = TheMatch.Types();
        UnitTypeTable untyped = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.GoldenUnitsFile(0)));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => new Match(
                TheMatch.Map(),
                TheRuleset.Committed(),
                TowerLayout.Parse(OneBolt, typed),
                WaveScript.Parse("order 0 1 12 0", untyped),
                TheMatch.Seed)
                .Resolve());

        Assert.Contains("exactly one of them is in the damage matrix", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("never checked against each other", thrown.Message, StringComparison.Ordinal);
    }

    private static int Dealt(Ruleset rules, int roll, int bonus, UnitType shooter, UnitType target) =>
        DamageModel.Dealt(rules, roll, bonus, shooter.AttackType, target.ArmourType, target.Armour);

    /// <summary>Every amount a match reported landing, in the order it landed.</summary>
    private static int[] Landings(Ruleset rules, TowerLayout defense, WaveScript wave)
    {
        var events = new TheMatch.EventLog();

        new Match(TheMatch.Map(), rules, defense, wave, TheMatch.Seed).Resolve(events);

        return events.Kinds
            .Select((kind, index) => (Kind: kind, Amount: events.Amounts[index]))
            .Where(entry => entry.Kind == "damaged")
            .Select(entry => entry.Amount)
            .ToArray();
    }
}
