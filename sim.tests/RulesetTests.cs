using System.Text;

namespace Sim.Tests;

/// <summary>
/// The ruleset, parsed from the committed file and from text planted to break
/// each rule.
/// </summary>
/// <remarks>
/// <para>
/// Every parse in here is handed <b>text or bytes</b>, exactly as
/// <see cref="ContentTests"/> is: the test opens the file and the simulation
/// never learns it exists.
/// </para>
/// <para>
/// <b>Every refusal is asserted by name.</b> A suite that only asserted "it
/// threw" would pass just as well when the ruleset refuses the whole file for
/// the wrong reason -- and a designer reading "could not load ruleset" learns
/// nothing they can act on. Each assertion below names the substring the
/// message has to carry.
/// </para>
/// </remarks>
public class RulesetTests
{
    [Fact]
    public void The_committed_ruleset_parses_to_the_numbers_it_was_authored_with()
    {
        // Every field, spelled out. A ruleset nothing reads yet is a ruleset
        // whose parse has never been checked against what a person typed, and
        // the tickets that consume these fields arrive one at a time.
        Ruleset rules = TheRuleset.Committed();

        Assert.Equal(140, rules.Matrix.Cell(AttackType.Pierce, ArmourType.Swift));
        Assert.Equal(1, rules.ArmourPercentPerPoint);
        Assert.Equal(100, rules.ArmourDenominator);
        Assert.Equal(1, rules.DamageFloor);
        Assert.Equal(10, rules.InterestPercentPerWave);
        Assert.Equal(Ruleset.NoInterestCeiling, rules.InterestCapSauce);
        Assert.Equal(100, rules.IncomeBasePerWave);
        Assert.Equal(100, rules.StartingPurseSauce);
        Assert.Equal(1500, rules.HealthPoolSauce);
        Assert.Equal(2, rules.StartingWaveSlots);
        Assert.Equal(1, rules.WaveSlotsPerAnchor);

        // Two ordinary options against a roster of two walkers. An option
        // unlocks a creep and appears on a menu once, so this number is bounded
        // by how many creeps there are to draw from, and it rises with them.
        Assert.Equal(2, rules.OrdinaryOptionsPerRound);
        Assert.Equal(3, rules.GameChangersPerAnchor);
        Assert.Equal(10, rules.FreeSnapshotsPerRun);
        Assert.Equal(25, rules.SnapshotPriceSauce);

        Assert.Equal(4, rules.Bands.Count);
        Assert.Equal(0, rules.Bands[0].PercentileThreshold);
        Assert.Equal(0, rules.Bands[0].BonusPercentOfBase);
        Assert.Equal(90, rules.Bands[3].PercentileThreshold);
        Assert.Equal(20, rules.Bands[3].BonusPercentOfBase);
    }

    [Fact]
    public void The_slot_widths_are_derived_from_the_anchors_and_not_authored_beside_them()
    {
        // The series the design names -- 2 2 3 3 3 4 4 4 5 5 across ten waves
        // with anchors at 3, 6 and 9 -- computed from the two numbers in the
        // file rather than read out of a second list that could drift from it.
        //
        // OBSERVED: change "slots 2 1" to "slots 2 2" in content/ruleset.txt.
        // The series becomes 2 2 4 4 4 6 6 6 8 8 and this goes red on wave 3,
        // which is what a widening step that had been retuned without anybody
        // re-reading this looks like.
        Ruleset rules = TheRuleset.Committed();
        int[] anchors = { 3, 6, 9 };
        var widths = new List<int>();

        for (int wave = 1; wave <= 10; wave++)
        {
            widths.Add(rules.WaveSlotsAt(anchors.Count(anchor => anchor <= wave)));
        }

        Assert.Equal(new[] { 2, 2, 3, 3, 3, 4, 4, 4, 5, 5 }, widths);
    }

    [Fact]
    public void The_simulation_takes_the_ruleset_as_bytes_as_well_as_text_and_agrees_with_itself()
    {
        Assert.Equal(
            Ruleset.Parse(File.ReadAllText(RepoLayout.RulesetFile)).ContentHash,
            Ruleset.ParseUtf8(File.ReadAllBytes(RepoLayout.RulesetFile)).ContentHash);
    }

    [Fact]
    public void A_byte_order_mark_is_not_a_content_change_to_the_ruleset()
    {
        string text = File.ReadAllText(RepoLayout.RulesetFile);
        byte[] withMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text))
            .ToArray();

        Assert.Equal(Ruleset.Parse(text).ContentHash, Ruleset.ParseUtf8(withMark).ContentHash);
    }

    [Fact]
    public void The_minimal_ruleset_the_planted_texts_are_built_from_parses()
    {
        // Without this, every refusal below could be firing on a fault the
        // fixture always had rather than on the one the test planted.
        Assert.Equal(1, Ruleset.Parse(TheRuleset.Minimal).DamageFloor);
    }

    [Theory]
    [InlineData("matrix")]
    [InlineData("armour")]
    [InlineData("floor")]
    [InlineData("interest")]
    [InlineData("income")]
    [InlineData("band")]
    [InlineData("health")]
    [InlineData("slots")]
    [InlineData("offering")]
    [InlineData("snapshot")]
    public void A_ruleset_missing_any_row_refuses_to_load_naming_the_row(string keyword)
    {
        // OBSERVED: delete the call to RequireEverything in Ruleset.Parse. Rows
        // of this theory go red having caught nothing at all, and a ruleset
        // with no interest rate, no income base and no health pool in it loads
        // to a mixture of what the file says and what the reader assumed,
        // folded into one content hash as though somebody had authored it.
        ContentException thrown =
            Assert.Throws<ContentException>(() => Ruleset.Parse(TheRuleset.Without(keyword)));

        Assert.Contains(keyword, thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("armour 1 100")]
    [InlineData("floor 1")]
    [InlineData("interest 10 0")]
    [InlineData("income 100")]
    [InlineData("health 1500")]
    [InlineData("slots 2 1")]
    [InlineData("offering 3 3")]
    [InlineData("snapshot 10 25")]
    public void A_rule_stated_twice_refuses_to_load(string row)
    {
        // Two rows claiming one rule means the ruleset in force is whichever of
        // them was read last, which is a coin flip nobody can see in a diff.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\n" + row));

        Assert.Contains("second '", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_the_ruleset_does_not_have_refuses_to_load_rather_than_being_skipped()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\ncompounding 1"));

        Assert.Contains("'compounding'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_with_the_wrong_number_of_fields_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1 100 7")));

        Assert.Contains("'armour' row has 4 fields", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_interest_row_that_states_a_rate_and_no_ceiling_refuses_to_load()
    {
        // Both fields or neither. A rate with no cap beside it would be read
        // against shifted fields or defaulted to a ceiling nobody authored, and
        // the ceiling is the whole answer to a run whose round cap was lifted.
        //
        // OBSERVED: read a two-field 'interest' row as a rate with a cap of
        // zero. This goes red having caught nothing, and a file that never
        // mentions a ceiling loads with "no ceiling" folded into its content
        // hash as though somebody had chosen it.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10")));

        Assert.Contains("'interest' row has 2 fields", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_interest_cap_refuses_to_load()
    {
        // A ceiling below zero is interest that takes sauce out of the bank.
        //
        // OBSERVED: open the cap's range at int.MinValue. This goes red having
        // caught nothing, and a bank that earns -1 a wave is authored without
        // a word from the reader.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 -1")));
    }

    [Fact]
    public void A_matrix_row_out_of_attack_type_order_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.Replace(TheRuleset.Minimal, "matrix pierce 140 70 100", "matrix magic 100 140 70")));

        Assert.Contains("where pierce was expected", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fourth_matrix_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\nmatrix pierce 140 70 100"));

        Assert.Contains("fourth 'matrix' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_row_naming_an_attack_type_outside_the_cycle_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.Replace(TheRuleset.Minimal, "matrix pierce 140 70 100", "matrix holy 140 70 100")));

        Assert.Contains("pierce, impact, magic", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_whose_rows_are_not_a_permutation_of_the_same_cells_refuses_to_load()
    {
        // Every COLUMN of this matrix is a permutation of (140, 70, 100) and
        // two of its rows are not: impact reads (70, 140, 140) and magic
        // (100, 100, 70). A reader that checked columns alone would load it,
        // and impact would be 140 against two of the three armour types --
        // globally better than everything, which is the failure the Latin
        // square exists to make impossible.
        //
        // OBSERVED: delete RequireLatinSquare's row loop. This goes red having
        // caught nothing while the column test below stays green, which is the
        // whole reason both halves are here.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.Replace(
                TheRuleset.Replace(TheRuleset.Minimal, "matrix impact 70 100 140", "matrix impact 70 140 140"),
                "matrix magic 100 140 70",
                "matrix magic 100 100 70")));

        Assert.Contains("permutation of the same three cells", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_whose_columns_are_not_a_permutation_of_the_same_cells_refuses_to_load()
    {
        // Every row here is a permutation of (140, 70, 100) and the columns are
        // not: swift reads (140, 140, 70) and armoured (70, 70, 140). A reader
        // that checked rows alone would load this, and swift armour would be
        // globally worse than armoured against two of the three attack types.
        //
        // OBSERVED: delete RequireLatinSquare's column loop. This goes red
        // having caught nothing while the row test above stays green, which is
        // the whole reason both halves are here.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.Replace(TheRuleset.Minimal, "matrix impact 70 100 140", "matrix impact 140 70 100")));

        Assert.Contains("column", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_row_that_repeats_a_cell_refuses_to_load()
    {
        // Two armour types the attacker cannot tell apart is a smaller table
        // wearing a three-by-three one's clothes.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.WithCells(100, 100, 140)));

        Assert.Contains("repeats a value", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bands_that_do_not_open_at_the_zeroth_percentile_refuse_to_load()
    {
        // A wave below the first threshold would fall in no band at all, and
        // what it earns would be whatever the reader supplied.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "band 0 0", "band 10 0")));

        Assert.Contains("first band starts at zero", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Bands_that_do_not_ascend_refuse_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "band 50 5", "band 0 5")));

        Assert.Contains("ascend strictly", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_band_paying_less_than_the_one_below_it_refuses_to_load()
    {
        // The bands are progressive and never negative: performing below
        // average earns a smaller bonus and never a penalty. A band that pays
        // less than the one under it is a penalty written as a bonus.
        //
        // OBSERVED: drop the comparison against the band below in
        // Draft.AddBand. This goes red having caught nothing, and a ruleset in
        // which the 50th percentile pays less than the 0th loads quietly.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.Replace(TheRuleset.Replace(TheRuleset.Minimal, "band 0 0", "band 0 10"), "band 50 5", "band 50 4")));

        Assert.Contains("doing better never pays less", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_band_bonus_refuses_to_load()
    {
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "band 50 5", "band 50 -5")));
    }

    [Fact]
    public void A_damage_floor_of_zero_refuses_to_load()
    {
        // A floor of zero is a hit the type chart is allowed to delete, which
        // is the one outcome the floor exists to make impossible.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "floor 1", "floor 0")));
    }

    [Fact]
    public void An_armour_denominator_of_zero_refuses_to_load()
    {
        // It is the divisor of every hit in the game.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1 0")));
    }

    [Fact]
    public void A_matrix_cell_of_zero_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => Ruleset.Parse(TheRuleset.WithCells(0, 70, 140)));
    }

    [Fact]
    public void A_decimal_point_in_the_ruleset_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10", "interest 10.5")));

        Assert.Contains("'.'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_decimal_comma_in_the_ruleset_is_the_same_mistake_and_is_refused_too()
    {
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "interest 10", "interest 10,5")));
    }

    [Fact]
    public void An_empty_ruleset_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => Ruleset.Parse("# nothing but a comment"));
    }

    [Fact]
    public void The_ruleset_hash_is_not_the_unit_table_hash_of_the_same_numbers()
    {
        // Both folds start from a label naming the table and its layout, so two
        // tables cannot collide by holding coincidentally equal integers.
        Assert.NotEqual(TheRuleset.Committed().ContentHash, TheMatch.Types().ContentHash);
        Assert.NotEqual(TheRuleset.Committed().ContentHash, TheMatch.Map().MapHash);
    }
}
