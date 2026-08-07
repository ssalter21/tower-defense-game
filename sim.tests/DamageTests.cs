using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The damage model as arithmetic: the Latin square, the fused expression
/// against the two-step form of the same algebra, the floor, the counter's
/// place in the order, and the pipeline that has one stage in it.
/// </summary>
/// <remarks>
/// <para>
/// <b>The sweep is reproduced here rather than trusted.</b> The number the
/// design was decided on -- that the two forms disagree on 42.7% of 411,600
/// triples -- came out of a throwaway prototype in another language. An oracle
/// nobody re-ran is a number, not evidence, so the sweep is run again in the
/// arithmetic that actually ships and the count it produces is what is
/// asserted.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class DamageTests
{
    /// <summary>The prototype's sweep, exactly: base damage, matrix cell, armour.</summary>
    private const int SweptDamageFrom = 1;

    private const int SweptDamageTo = 100;

    private const int SweptCellFrom = 5;

    private const int SweptCellTo = 200;

    private const int SweptArmourTo = 20;

    /// <summary>How many triples that is.</summary>
    private const int SweptTriples = 411600;

    /// <summary>
    /// What the prototype measured, and what this file re-measures. A count
    /// rather than a percentage: 42.7% is a rounding of this number and a
    /// rounding is not an oracle.
    /// </summary>
    private const int SweptDisagreements = 175759;

    [Fact]
    public void Every_row_and_every_column_of_the_matrix_is_a_permutation_of_the_same_three_cells()
    {
        // OBSERVED: delete RequireLatinSquare's column loop -- so that the file
        // loads -- and swap the last two cells of the magic row in
        // content/ruleset.txt, "matrix magic 100 140 70" to
        // "matrix magic 100 70 140". Every row is still a permutation of
        // (70, 100, 140), and this goes red on the armoured column, which comes
        // out (70, 70, 100). The columns are the half a row check cannot see.
        Ruleset rules = TheRuleset.Committed();
        int[] expected = { 70, 100, 140 };

        for (int attack = 0; attack < DamageMatrix.AttackTypes; attack++)
        {
            var row = new List<int>();

            for (int armour = 0; armour < DamageMatrix.ArmourTypes; armour++)
            {
                row.Add(rules.Matrix.Cell((AttackType)attack, (ArmourType)armour));
            }

            row.Sort();
            Assert.Equal(expected, row);
        }

        for (int armour = 0; armour < DamageMatrix.ArmourTypes; armour++)
        {
            var column = new List<int>();

            for (int attack = 0; attack < DamageMatrix.AttackTypes; attack++)
            {
                column.Add(rules.Matrix.Cell((AttackType)attack, (ArmourType)armour));
            }

            column.Sort();
            Assert.Equal(expected, column);
        }

        // The spread, read off the matrix rather than off the file: the best
        // cell is exactly twice the worst, so type moves shots-to-kill by
        // double.
        Assert.Equal(2 * rules.Matrix.Cells.Min(), rules.Matrix.Cells.Max());
    }

    [Fact]
    public void The_matrix_is_a_flat_nine_element_array_indexed_by_attack_and_armour()
    {
        // The claim the whole shape rests on. A keyed collection is a banned
        // type, and the index arithmetic is the thing a sweep walks.
        //
        // The index convention is pinned on a matrix that is NOT symmetric
        // about its diagonal, and that is load-bearing. The committed one is
        // symmetric -- pierce against armoured and impact against swift are
        // both 70 -- so reading it armour-major instead of attack-major
        // produces exactly the same nine numbers, and every assertion against
        // it passes either way.
        //
        // OBSERVED, both ways round. Index DamageMatrix.Cell as
        // `_cells[(column * AttackTypes) + row]` -- the transpose -- and the
        // first assertion below goes red, 100 against 70. Point the same
        // assertions at the committed matrix instead and the transposed index
        // is green, which is what this test looked like before the asymmetric
        // one was built.
        Ruleset asymmetric = Ruleset.Parse(TheRuleset.Replace(
            TheRuleset.Replace(TheRuleset.Minimal, "matrix impact 70 100 140", "matrix impact 100 140 70"),
            "matrix magic 100 140 70",
            "matrix magic 70 100 140"));

        Assert.Equal(70, asymmetric.Matrix.Cell(AttackType.Pierce, ArmourType.Armoured));
        Assert.Equal(100, asymmetric.Matrix.Cell(AttackType.Impact, ArmourType.Swift));

        for (int attack = 0; attack < DamageMatrix.AttackTypes; attack++)
        {
            for (int armour = 0; armour < DamageMatrix.ArmourTypes; armour++)
            {
                Assert.Equal(
                    asymmetric.Matrix.Cells[(attack * DamageMatrix.ArmourTypes) + armour],
                    asymmetric.Matrix.Cell((AttackType)attack, (ArmourType)armour));
            }
        }

        Ruleset rules = TheRuleset.Committed();

        Assert.Equal(9, rules.Matrix.Cells.Count);

        // And the authored table, cell by cell.
        Assert.Equal(140, rules.Matrix.Cell(AttackType.Pierce, ArmourType.Swift));
        Assert.Equal(70, rules.Matrix.Cell(AttackType.Pierce, ArmourType.Armoured));
        Assert.Equal(100, rules.Matrix.Cell(AttackType.Pierce, ArmourType.Arcane));
        Assert.Equal(70, rules.Matrix.Cell(AttackType.Impact, ArmourType.Swift));
        Assert.Equal(100, rules.Matrix.Cell(AttackType.Impact, ArmourType.Armoured));
        Assert.Equal(140, rules.Matrix.Cell(AttackType.Impact, ArmourType.Arcane));
        Assert.Equal(100, rules.Matrix.Cell(AttackType.Magic, ArmourType.Swift));
        Assert.Equal(140, rules.Matrix.Cell(AttackType.Magic, ArmourType.Armoured));
        Assert.Equal(70, rules.Matrix.Cell(AttackType.Magic, ArmourType.Arcane));
    }

    [Fact]
    public void The_two_step_form_is_a_different_function_and_the_sweep_says_by_how_many()
    {
        // The prototype's oracle, re-measured rather than believed. Both forms
        // are written out here as integers, so what is being compared is the
        // arithmetic and not two calls into the same helper.
        //
        // OBSERVED: write TwoStep as `damage * cell / (100 + armour)` -- the
        // fused form under the other name, which is what a reader who trusts
        // the algebra would write. The disagreement count goes to 0 and the
        // count assertion goes red, 175,759 against 0. That is what this test
        // looks like when the distinction it exists for has been optimised
        // away.
        int swept = 0;
        int disagreements = 0;
        int fusedWasLower = 0;

        for (int damage = SweptDamageFrom; damage <= SweptDamageTo; damage++)
        {
            for (int cell = SweptCellFrom; cell <= SweptCellTo; cell++)
            {
                for (int armour = 0; armour <= SweptArmourTo; armour++)
                {
                    int fused = Fused(damage, cell, armour);
                    int twoStep = TwoStep(damage, cell, armour);

                    swept++;

                    if (fused != twoStep)
                    {
                        disagreements++;
                    }

                    if (fused < twoStep)
                    {
                        fusedWasLower++;
                    }
                }
            }
        }

        Assert.Equal(SweptTriples, swept);
        Assert.Equal(SweptDisagreements, disagreements);

        // Never lower, because the fused form truncates once where the two-step
        // form truncates twice.
        Assert.Equal(0, fusedWasLower);

        // And the percentage the design was decided on, to one decimal place,
        // computed from the count rather than carried beside it.
        Assert.Equal(
            "42.7",
            (disagreements * 1000L / swept / 10L).ToString(CultureInfo.InvariantCulture)
            + "."
            + (disagreements * 1000L / swept % 10L).ToString(CultureInfo.InvariantCulture));
    }

    [Fact]
    public void The_expression_the_damage_model_evaluates_is_the_fused_one()
    {
        // The bridge from the arithmetic above to the shipped code: the same
        // sweep, run through the public surface, with the matrix cell moved by
        // building a ruleset per multiplier. Every triple has to land on the
        // fused answer, and the triples where the two forms still differ after
        // the floor is applied have to be a real number of them rather than
        // none.
        //
        // OBSERVED: change DamageModel.Mitigate to
        // `amount * cell / rules.ArmourDenominator * rules.ArmourDenominator /
        // denominator` -- the two-step form, algebraically identical. This goes
        // red naming damage 41, cell 5, armour 1, where the fused form deals 2
        // and the two-step form deals 1.
        int differedAfterTheFloor = 0;
        string? firstMismatch = null;

        for (int cell = SweptCellFrom; cell <= SweptCellTo; cell++)
        {
            Ruleset rules = Ruleset.Parse(TheRuleset.WithCells(cell, cell + 1, cell + 2));

            for (int damage = SweptDamageFrom; damage <= SweptDamageTo; damage++)
            {
                for (int armour = 0; armour <= SweptArmourTo; armour++)
                {
                    int dealt = DamageModel.Dealt(
                        rules,
                        damage,
                        0,
                        AttackType.Pierce,
                        ArmourType.Swift,
                        armour);

                    int fused = Floored(Fused(damage, cell, armour), rules.DamageFloor);
                    int twoStep = Floored(TwoStep(damage, cell, armour), rules.DamageFloor);

                    if (dealt != fused && firstMismatch is null)
                    {
                        firstMismatch = Triple(damage, cell, armour)
                            + " dealt "
                            + dealt.ToString(CultureInfo.InvariantCulture)
                            + " where the fused form gives "
                            + fused.ToString(CultureInfo.InvariantCulture)
                            + " and the two-step form gives "
                            + twoStep.ToString(CultureInfo.InvariantCulture)
                            + ".";
                    }

                    if (twoStep != dealt)
                    {
                        differedAfterTheFloor++;
                    }
                }
            }
        }

        Assert.Null(firstMismatch);

        Assert.True(
            differedAfterTheFloor > 0,
            "The floor swallowed every disagreement between the two forms, so this sweep proved nothing "
            + "about which of them the simulation evaluates.");
    }

    [Fact]
    public void The_floor_holds_for_every_combination_of_type_armour_and_bonus()
    {
        // Nothing the type chart and the armour can do between them deletes a
        // hit. The sweep is deliberately weighted at the small end, because
        // that is the only end where the floor can be reached at all.
        //
        // OBSERVED: delete the floor clause at the end of DamageModel.Dealt.
        // This goes red immediately -- base 0, bonus 0, any type, any armour,
        // which deals nothing at all and would be a shot the simulation fired
        // and threw away.
        Ruleset rules = TheRuleset.Committed();
        int[] bonuses = { 0, 1, 7, 90, 270 };
        int floored = 0;

        for (int attack = 0; attack < DamageMatrix.AttackTypes; attack++)
        {
            for (int armourType = 0; armourType < DamageMatrix.ArmourTypes; armourType++)
            {
                foreach (int bonus in bonuses)
                {
                    for (int damage = 0; damage <= 40; damage++)
                    {
                        for (int armour = 0; armour <= 300; armour++)
                        {
                            int dealt = DamageModel.Dealt(
                                rules,
                                damage,
                                bonus,
                                (AttackType)attack,
                                (ArmourType)armourType,
                                armour);

                            if (dealt < rules.DamageFloor)
                            {
                                Assert.Fail(
                                    "A hit of "
                                    + damage.ToString(CultureInfo.InvariantCulture)
                                    + " plus "
                                    + bonus.ToString(CultureInfo.InvariantCulture)
                                    + " through cell "
                                    + rules.Matrix.Cell((AttackType)attack, (ArmourType)armourType)
                                        .ToString(CultureInfo.InvariantCulture)
                                    + " against "
                                    + armour.ToString(CultureInfo.InvariantCulture)
                                    + " armour dealt "
                                    + dealt.ToString(CultureInfo.InvariantCulture)
                                    + ".");
                            }

                            if (dealt == rules.DamageFloor)
                            {
                                floored++;
                            }
                        }
                    }
                }
            }
        }

        Assert.True(
            floored > 0,
            "Nothing in the sweep ever reached the floor, so the sweep never tested it.");
    }

    [Fact]
    public void The_counter_is_added_to_the_base_before_typing_and_mitigation()
    {
        // Where bonusVsTag joins the hit is the whole of the rule, and the two
        // orders are different numbers. Added first, a high-armour target
        // blunts its own counter along with everything else; added last, armour
        // would stop meaning anything against the thing built to kill it.
        //
        // OBSERVED: move the bonus out of the pipeline's input and add it to
        // the result instead -- `return dealt + bonusVsTag` at the end of
        // DamageModel.Dealt, with the pipeline fed baseDamage alone. The
        // armoured assertion goes red, 225 against 326, and the blunting
        // assertion below goes red too because the delivered counter stops
        // depending on armour at all.
        Ruleset rules = TheRuleset.Committed();

        const int Base = 90;
        const int Bonus = 270;

        // Cell 100, so the only thing between the hit and the target is armour.
        Assert.Equal(
            (Base + Bonus) * 100 / (100 + 0),
            DamageModel.Dealt(rules, Base, Bonus, AttackType.Pierce, ArmourType.Arcane, 0));

        Assert.Equal(
            (Base + Bonus) * 100 / (100 + 60),
            DamageModel.Dealt(rules, Base, Bonus, AttackType.Pierce, ArmourType.Arcane, 60));

        // The blunting, as an inequality rather than as a sentence: what the
        // counter is worth in delivered damage falls as the target's armour
        // rises.
        int bare = DamageModel.Dealt(rules, Base, Bonus, AttackType.Pierce, ArmourType.Arcane, 0)
            - DamageModel.Dealt(rules, Base, 0, AttackType.Pierce, ArmourType.Arcane, 0);

        int armoured = DamageModel.Dealt(rules, Base, Bonus, AttackType.Pierce, ArmourType.Arcane, 100)
            - DamageModel.Dealt(rules, Base, 0, AttackType.Pierce, ArmourType.Arcane, 100);

        Assert.True(
            armoured < bare,
            "The counter delivered "
            + armoured.ToString(CultureInfo.InvariantCulture)
            + " extra damage against 100 armour and "
            + bare.ToString(CultureInfo.InvariantCulture)
            + " against none, so armour is not blunting it.");

        // And it is still steep. Prepared beats unprepared by a wide margin
        // against a target built to survive being unprepared for.
        Assert.True(
            DamageModel.Dealt(rules, Base, Bonus, AttackType.Pierce, ArmourType.Arcane, 60)
            >= 3 * DamageModel.Dealt(rules, Base, 0, AttackType.Pierce, ArmourType.Arcane, 60),
            "The counter is not steep enough to be worth preparing for.");
    }

    [Fact]
    public void The_stat_pipeline_is_a_named_ordered_list_with_exactly_one_stage_on_it()
    {
        // The mechanism, asserted: every stage this build declares is on the
        // list, and the list has one thing on it. A second stage is a second
        // truncation, so adding one is a decision about the integer contract
        // and this is where it has to be taken.
        //
        // OBSERVED: add `Rounding = 1` to StatStage and leave DamageModel's
        // StageOrder alone. The first assertion goes red naming "Rounding",
        // which is exactly the failure a stage added and never applied should
        // produce. Adding it to StageOrder instead reddens the count assertion,
        // which is the other half.
        var declared = (StatStage[])Enum.GetValues(typeof(StatStage));

        foreach (StatStage stage in declared)
        {
            Assert.True(
                DamageModel.Stages.Contains(stage),
                "Stat stage "
                + stage.ToString()
                + " is declared and is not on the pipeline, so nothing applies it and no damage number "
                + "anywhere is affected by it. A stage is declared, listed and given a branch, and all "
                + "three or none.");
        }

        Assert.Equal(declared.Length, DamageModel.Stages.Count);

        Assert.Single(DamageModel.Stages);
        Assert.Equal(StatStage.TypedMitigation, DamageModel.Stages[0]);
        Assert.Equal("typed mitigation", DamageModel.NameOf(DamageModel.Stages[0]));

        // Every stage on the list has a branch behind it. A listed stage with
        // no branch throws rather than passing the value through untouched.
        foreach (StatStage stage in DamageModel.Stages)
        {
            Assert.NotEqual(string.Empty, DamageModel.NameOf(stage));
        }
    }

    [Fact]
    public void A_stage_the_pipeline_does_not_know_is_refused_by_name()
    {
        // The default branch, reached the only way a test can reach it: with a
        // value that is not one of the declared stages. This is the shape of
        // what a listed-but-unimplemented stage would hit.
        SimulationException thrown =
            Assert.Throws<SimulationException>(() => DamageModel.NameOf((StatStage)7));

        Assert.Contains("Stat stage 7", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_shot_that_falls_outside_the_matrix_is_refused_by_name()
    {
        // OBSERVED: return cells[0] from DamageMatrix.Cell when the row or the
        // column is None, which is the plausible-looking default. Both
        // assertions go red having caught nothing, and every untyped unit in
        // the game would silently be Pierce against Swift -- the best cell in
        // the table.
        Ruleset rules = TheRuleset.Committed();

        SimulationException noAttack = Assert.Throws<SimulationException>(
            () => DamageModel.Dealt(rules, 100, 0, AttackType.None, ArmourType.Swift, 0));

        Assert.Contains("not a row of the damage matrix", noAttack.Message, StringComparison.Ordinal);

        SimulationException noArmour = Assert.Throws<SimulationException>(
            () => DamageModel.Dealt(rules, 100, 0, AttackType.Pierce, ArmourType.None, 0));

        Assert.Contains("not a column of the damage matrix", noArmour.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1, 0, 0)]
    [InlineData(0, -1, 0)]
    [InlineData(0, 0, -1)]
    public void A_negative_base_bonus_or_armour_is_refused(int damage, int bonus, int armour)
    {
        // Each of the three is an unconditional throw rather than a clamp. A
        // negative armour is a target that takes more damage the tougher it is,
        // and a negative base arrives underneath the floor the floor exists to
        // guarantee.
        //
        // OBSERVED: replace each guard with a clamp to zero. Every row of this
        // theory goes red having caught nothing at all.
        Assert.Throws<SimulationException>(
            () => DamageModel.Dealt(
                TheRuleset.Committed(),
                damage,
                bonus,
                AttackType.Pierce,
                ArmourType.Swift,
                armour));
    }

    [Fact]
    public void A_hit_that_will_not_fit_in_a_health_pool_is_a_throw_and_not_a_wrap()
    {
        // The intermediate is a long and the result is an int, so the one place
        // the arithmetic can leave its range is the way out. A wrapped product
        // deals a negative amount, and a negative amount heals.
        //
        // OBSERVED: return `(int)dealt` without the range check. The throw
        // stops happening and the call returns a negative number, so
        // Assert.Throws goes red and the value it would have returned is a heal.
        Ruleset rules = Ruleset.Parse(TheRuleset.WithCells(1000000, 999999, 999998));

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => DamageModel.Dealt(
                rules,
                int.MaxValue,
                int.MaxValue,
                AttackType.Pierce,
                ArmourType.Swift,
                0));

        Assert.Contains("does not fit", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The rule: one multiply, one divide, one truncation. Written out here in
    /// integers rather than called into the simulation, so that this file's
    /// oracle is arithmetic rather than the thing being tested.
    /// </summary>
    private static int Fused(int damage, int cell, int armour) => damage * cell / (100 + armour);

    /// <summary>
    /// The same algebra applied as two truncating steps. Not the rule, and the
    /// only reason it is written here is to be compared against the rule.
    /// </summary>
    private static int TwoStep(int damage, int cell, int armour) =>
        damage * cell / 100 * 100 / (100 + armour);

    private static int Floored(int dealt, int floor) => dealt < floor ? floor : dealt;

    private static string Triple(int damage, int cell, int armour) =>
        "damage "
        + damage.ToString(CultureInfo.InvariantCulture)
        + ", cell "
        + cell.ToString(CultureInfo.InvariantCulture)
        + ", armour "
        + armour.ToString(CultureInfo.InvariantCulture);
}
