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
        Assert.Equal(Ruleset.NoInterestCeiling, rules.InterestCapGold);
        Assert.Equal(168, rules.IncomeBasePerWave);

        // What a run opens holding, which is deliberately short of one wave's base.
        // OBSERVED: change "purse         100" to "purse         150" in
        // content/ruleset.txt. This goes red, 100 against 150, which is what a
        // retuned opening balance nobody re-read here looks like.
        Assert.Equal(100, rules.StartingPurseGold);
        Assert.Equal(800, rules.HealthPoolGold);
        Assert.Equal(10, rules.FreeSnapshotsPerRun);
        Assert.Equal(25, rules.SnapshotPriceGold);

        // What a wave is paid for the damage it does, as a share of the leak
        // cost it dealt. OBSERVED: change "bonus          25" to
        // "bonus          40" in content/ruleset.txt. This goes red, 25 against
        // 40, which is what a retuned bonus rate nobody re-read here looks like.
        Assert.Equal(25, rules.BonusPercentOfLeakCost);
    }

    [Fact]
    public void The_simulation_takes_the_ruleset_as_bytes_as_well_as_text_and_agrees_with_itself()
    {
        // OBSERVED: strip a byte-order mark unconditionally in the byte path --
        // a .Substring(1) on what DataText.FromUtf8 decoded, as though every
        // file handed to it carried one. This goes red on the throw: the first
        // line loses its '#', " The ruleset." reaches the field splitter and the
        // parse refuses on the '.' at column 13. The text path is untouched,
        // which is what a second entry point drifting from the first looks like.
        Assert.Equal(
            Ruleset.Parse(File.ReadAllText(RepoLayout.RulesetFile)).ContentHash,
            Ruleset.ParseUtf8(File.ReadAllBytes(RepoLayout.RulesetFile)).ContentHash);
    }

    [Fact]
    public void A_byte_order_mark_is_not_a_content_change_to_the_ruleset()
    {
        // OBSERVED: delete the byte-order-mark strip in DataText.SplitLines.
        // This goes red on the throw -- "carries a character outside printable
        // ASCII at column 1 (code point 65279)" -- so a ruleset any Windows
        // text writer produced refuses to load rather than parsing to what it
        // says.
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
        //
        // OBSERVED: change "floor 1" to "floor 2" in TheRuleset.Minimal. This
        // goes red, 1 against 2 -- the fixture read back, rather than the
        // fixture assumed.
        Assert.Equal(1, Ruleset.Parse(TheRuleset.Minimal).DamageFloor);
    }

    [Theory]
    [MemberData(nameof(TheRuleset.EveryRule), MemberType = typeof(TheRuleset))]
    public void A_ruleset_missing_any_row_refuses_to_load_naming_the_row(string keyword)
    {
        // One case per rule the committed file states, taken off the file rather
        // than listed here, so a rule added to the ruleset is covered without
        // anybody adding a case for it.
        //
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
    [MemberData(nameof(TheRuleset.EveryRuleStatedOnce), MemberType = typeof(TheRuleset))]
    public void A_rule_stated_twice_refuses_to_load(string keyword)
    {
        // Two rows claiming one rule means the ruleset in force is whichever of
        // them was read last, which is a coin flip nobody can see in a diff.
        // The matrix is not here because it is authored as several rows on
        // purpose, and it has its own refusal for a row too many.
        //
        // OBSERVED: delete the duplicate loop in Draft.Once, leaving the Add.
        // All nine rows go red having caught nothing, and a file stating the
        // health pool twice loads on the second one.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\n" + TheRuleset.MinimalRow(keyword)));

        Assert.Contains("second '", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_the_ruleset_does_not_have_refuses_to_load_rather_than_being_skipped()
    {
        // OBSERVED: return from ReadRow's default branch instead of throwing.
        // This goes red having caught nothing, and a row somebody misspelled is
        // silently dropped -- the rule it was meant to state supplied by the
        // reader and folded into the hash as though somebody had authored it.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\ncompounding 1"));

        Assert.Contains("'compounding'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_with_the_wrong_number_of_fields_refuses_to_load()
    {
        // OBSERVED: make DataText.RequireFieldCount a no-op. This goes red
        // having caught nothing: "armour 1 100 7" is read off its first two
        // fields and the extra one is dropped, so a row somebody added a column
        // to loads as the row they meant to replace.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1 100 7")));

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
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10")));

        Assert.Contains("'interest' row has 2 fields", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_interest_cap_refuses_to_load()
    {
        // A ceiling below zero is interest that takes gold out of the bank.
        //
        // OBSERVED: open the cap's range at int.MinValue. This goes red having
        // caught nothing, and a bank that earns -1 a wave is authored without
        // a word from the reader.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10 0", "interest 10 -1")));
    }

    [Fact]
    public void A_matrix_row_out_of_attack_type_order_refuses_to_load()
    {
        // OBSERVED: drop the attack-against-row-count comparison in
        // Draft.AddMatrixRow. This goes red on the message: each row is written
        // at its own attack index, so pierce's cells are never written at all
        // and the Latin square refuses "a first matrix row of (0, 0, 0)" --
        // naming a repeat, and saying nothing about the row that went missing.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            PlantedText.Replace(TheRuleset.Minimal, "matrix pierce 140 70 100", "matrix magic 100 140 70")));

        Assert.Contains("where pierce was expected", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_fourth_matrix_row_refuses_to_load()
    {
        // OBSERVED: drop the row-count check at the top of Draft.AddMatrixRow.
        // This goes red on the exception type: the order check underneath it
        // reads DamageMatrix.AttackWords[3], which is off the end of a
        // three-word array, and an IndexOutOfRangeException surfaces instead of
        // a refusal a designer could act on.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(TheRuleset.Minimal + "\nmatrix pierce 140 70 100"));

        Assert.Contains("fourth 'matrix' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_row_naming_an_attack_type_outside_the_cycle_refuses_to_load()
    {
        // OBSERVED: have DataText.Keyword return 0 for a word it does not know,
        // which is the plausible-looking default. This goes red having caught
        // nothing: "matrix holy" reads as pierce, the three rows line up in
        // order, and a ruleset naming an attack type the game does not have
        // loads as though it named the first one.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            PlantedText.Replace(TheRuleset.Minimal, "matrix pierce 140 70 100", "matrix holy 140 70 100")));

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
            PlantedText.Replace(
                PlantedText.Replace(TheRuleset.Minimal, "matrix impact 70 100 140", "matrix impact 70 140 140"),
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
            PlantedText.Replace(TheRuleset.Minimal, "matrix impact 70 100 140", "matrix impact 140 70 100")));

        Assert.Contains("column", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_matrix_row_that_repeats_a_cell_refuses_to_load()
    {
        // Two armour types the attacker cannot tell apart is a smaller table
        // wearing a three-by-three one's clothes.
        //
        // OBSERVED: delete the repeat check at the top of RequireLatinSquare.
        // This goes red having caught nothing: (100, 100, 140) cycled is a
        // permutation of itself down every row and every column, so the square
        // rule underneath waves it through.
        ContentException thrown = Assert.Throws<ContentException>(() => Ruleset.Parse(
            TheRuleset.WithCells(100, 100, 140)));

        Assert.Contains("repeats a value", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_bonus_rate_refuses_to_load()
    {
        // The bonus is proportional to what a wave dealt, so a negative rate is
        // a wave charged for attacking -- the run's own offense taking gold off
        // it, which nothing in this economy does.
        //
        // OBSERVED: open the bonus rate's range at int.MinValue. This goes red
        // having caught nothing, and a ruleset paying -25% of leak cost dealt
        // loads: the more a wave gets past, the poorer the run that sent it.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "bonus 25", "bonus -25")));

        Assert.Contains("outside the allowed range", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_damage_floor_of_zero_refuses_to_load()
    {
        // A floor of zero is a hit the type chart is allowed to delete, which
        // is the one outcome the floor exists to make impossible.
        //
        // OBSERVED: open the damage floor's range at 0 in Ruleset.ReadRow. This
        // goes red having caught nothing, and a ruleset that lets a hit come
        // out at nothing loads.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "floor 1", "floor 0")));
    }

    [Fact]
    public void An_armour_denominator_of_zero_refuses_to_load()
    {
        // It is the divisor of every hit in the game.
        //
        // OBSERVED: open the armour denominator's range at 0 in
        // Ruleset.ReadRow. This goes red having caught nothing, and the file
        // loads carrying a denominator every unarmoured shot in the game would
        // divide by.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "armour 1 100", "armour 1 0")));
    }

    [Fact]
    public void A_matrix_cell_of_zero_refuses_to_load()
    {
        // OBSERVED: open a cell's range at 0 in Ruleset.Cell. This goes red
        // having caught nothing: the three cells are still distinct and every
        // row and column is still a permutation of them, so a matrix with a
        // cell that deletes a whole attack-armour pairing loads.
        Assert.Throws<ContentException>(() => Ruleset.Parse(TheRuleset.WithCells(0, 70, 140)));
    }

    [Fact]
    public void A_decimal_point_in_the_ruleset_refuses_to_load()
    {
        // OBSERVED: drop the '.' and ',' refusal in DataText.Fields and have
        // DataText.Integer stop at the first character that is not a digit
        // rather than refuse it -- a fraction truncated to its whole part.
        // This goes red having caught nothing, and "interest 10.5 0" loads as a
        // rate of 10 with the half nobody can represent quietly discarded.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10", "interest 10.5")));

        Assert.Contains("'.'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_decimal_comma_in_the_ruleset_is_the_same_mistake_and_is_refused_too()
    {
        // OBSERVED: the same edit the decimal point above was watched under.
        // This goes red having caught nothing, and "interest 10,5 0" loads as a
        // rate of 10 -- the spelling a comma-decimal locale produces, read as a
        // number nobody typed.
        Assert.Throws<ContentException>(
            () => Ruleset.Parse(PlantedText.Replace(TheRuleset.Minimal, "interest 10", "interest 10,5")));
    }

    [Fact]
    public void An_empty_ruleset_refuses_to_load()
    {
        // OBSERVED: delete the RequireEverything call in Ruleset.Parse. This
        // goes red on the exception type: a NullReferenceException out of
        // Fold(), because the matrix nothing authored is null and the fold
        // reaches it before anybody has been told which row was missing.
        Assert.Throws<ContentException>(() => Ruleset.Parse("# nothing but a comment"));
    }

    [Fact]
    public void The_ruleset_hash_is_not_the_unit_table_hash_of_the_same_numbers()
    {
        // Both folds start from a label naming the table and its layout, so two
        // tables cannot collide by holding coincidentally equal integers.
        //
        // OBSERVED: stop Hash64 distinguishing anything -- skip the label loop
        // in Start and return `this` from Add(long). Both assertions go red,
        // every table in the project coming back as the bare FNV offset basis
        // CBF29CE484222325, which is what a fold that has stopped folding looks
        // like from here.
        Assert.NotEqual(TheRuleset.Committed().ContentHash, TheMatch.Types().ContentHash);
        Assert.NotEqual(TheRuleset.Committed().ContentHash, TheMatch.Map().MapHash);
    }

    [Fact]
    public void Retuning_the_scouting_line_moves_the_hash_and_nothing_else()
    {
        // The sweep's two dials, turned through the one seam that turns them.
        // Everything the retune did not name is carried across untouched --
        // asserted rather than assumed, because a copy constructor over sixteen
        // fields is exactly where a field goes missing quietly.
        //
        // OBSERVED: carry StartingPurseGold across as HealthPoolGold in
        // Ruleset's retuning constructor. The purse assertion goes red, 1500
        // where 100 was expected, and nothing else in the suite notices -- which
        // is what a field crossed in a sixteen-line copy looks like.
        Ruleset authored = TheRuleset.Committed();
        Ruleset retuned = authored.With(6, 8);

        Assert.Equal(6, retuned.FreeSnapshotsPerRun);
        Assert.Equal(8, retuned.SnapshotPriceGold);
        Assert.NotEqual(authored.ContentHash, retuned.ContentHash);

        Assert.Equal(authored.Matrix.Cells, retuned.Matrix.Cells);
        Assert.Equal(authored.ArmourPercentPerPoint, retuned.ArmourPercentPerPoint);
        Assert.Equal(authored.ArmourDenominator, retuned.ArmourDenominator);
        Assert.Equal(authored.DamageFloor, retuned.DamageFloor);
        Assert.Equal(authored.InterestPercentPerWave, retuned.InterestPercentPerWave);
        Assert.Equal(authored.InterestCapGold, retuned.InterestCapGold);
        Assert.Equal(authored.IncomeBasePerWave, retuned.IncomeBasePerWave);
        Assert.Equal(authored.StartingPurseGold, retuned.StartingPurseGold);
        Assert.Equal(authored.HealthPoolGold, retuned.HealthPoolGold);
        Assert.Equal(authored.BonusPercentOfLeakCost, retuned.BonusPercentOfLeakCost);
    }

    [Fact]
    public void Retuning_to_the_numbers_already_authored_leaves_the_hash_where_it_was()
    {
        // The other half of a derivation, and the half that says the fold is
        // over the values rather than over the act of retuning. A sweep left at
        // AsAuthored plays the committed rules under the committed hash, so a
        // record stamped against them still replays.
        //
        // OBSERVED: fold an extra Add(1) into the retuning constructor to mark a
        // ruleset as retuned. This goes red on a hash that moved for no number
        // anybody authored, which retires every stored record on a sweep having
        // run.
        Ruleset authored = TheRuleset.Committed();

        Assert.Equal(
            authored.ContentHash,
            authored.With(
                authored.FreeSnapshotsPerRun,
                authored.SnapshotPriceGold).ContentHash);
    }

    [Theory]
    [InlineData(-1, 25, "the free snapshot count")]
    [InlineData(10, -1, "the snapshot price")]
    public void A_retuned_number_outside_the_authored_column_is_refused(
        int free,
        int price,
        string named)
    {
        // A number that reaches the rules through the retuning door has had no
        // file to be refused at, so it is held to the range the authored column
        // is held to. Without that, a sweep is the one caller in the project
        // able to build a ruleset no text file could express -- and every
        // finding it produced would be about a game nobody can author.
        //
        // OBSERVED: drop the RequireInRange calls from Ruleset.With. Both rows
        // go red having thrown nothing at all -- a snapshot at minus one gold
        // builds a perfectly ordinary ruleset with a perfectly ordinary hash.
        SimulationException refused = Assert.Throws<SimulationException>(
            () => TheRuleset.Committed().With(free, price));

        Assert.Contains(named, refused.Message, StringComparison.Ordinal);
    }
}
