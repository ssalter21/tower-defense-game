using System.Text;

namespace Sim.Tests;

/// <summary>
/// The unit type table and the wave, parsed from the committed files and from
/// text planted to break each rule.
/// </summary>
/// <remarks>
/// Every parse in here is handed <b>text or bytes</b>. The test opens the file
/// and the simulation never learns it exists, which is the arrangement the
/// whole no-ambient-IO position rests on -- and which the IL scan enforces on
/// the compiled assembly rather than trusting these tests to have been honest.
/// </remarks>
public class ContentTests
{
    /// <summary>
    /// Three rows in column layout 1, which is what a table with no
    /// <c>layout</c> row is. It has no cost column and no place in the damage
    /// matrix, and that is the truth about every table written before those
    /// columns existed.
    /// </summary>
    private const string ThreeGoodRows = """
        # a comment, which changes nothing
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0
        """;

    /// <summary>The same three rows in column layout 2, with the four columns it adds.</summary>
    private const string ThreeCurrentRows = """
        layout 2
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0
        """;

    /// <summary>
    /// What the unit table pinned beside the version-0 golden bundle hashes to,
    /// and what its bundle's header carries.
    /// </summary>
    private const ulong LayoutOneHashOfTheOldestPin = 0x39B848CEFDDCC9CFUL;

    [Fact]
    public void The_committed_unit_table_parses()
    {
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.Equal(10, table.Count);
        Assert.Equal("grunt", table.ById(1).Label);
        Assert.Equal(UnitRole.Moving, table.ById(2).Role);
        Assert.Equal(Delivery.Hitscan, table.ById(3).Delivery);
        Assert.Equal(Delivery.Projectile, table.ById(4).Delivery);
        Assert.Equal(11, table.ById(4).ProjectileFlightTicks);

        // Six of them walk, which is what an offering is drawn out of, and four
        // stand. The ratio is what lets the ruleset ask for three ordinary
        // options a round.
        //
        // OBSERVED: change the bulwark's role from moving to placed in
        // content/units.txt. The walker count goes red, 6 against 5, and the
        // offering's own refusal follows it in BuildPhaseTests.
        Assert.Equal(6, table.Types.Count(row => row.Role == UnitRole.Moving));
        Assert.Equal(4, table.Types.Count(row => row.Role == UnitRole.Placed));
    }

    [Fact]
    public void The_roster_spans_the_matrix_and_every_shape_is_a_row()
    {
        // Ten units and no eleventh column: a swarm, a wall, a sniper and a
        // sieger are all authored out of the fields that were already here.
        // Every attack type and every armour type is on the roster, so nothing
        // in the matrix is a cell no committed unit can reach.
        //
        // OBSERVED: give the sieger `pierce` instead of `impact` -- one word in
        // content/units.txt. The distinct-attack-types assertion still passes,
        // because the sniper still carries magic; what goes red is the sieger's
        // own row, Impact against Pierce. Both are here because the first says
        // the matrix is covered and the second says which row covers what, and
        // a roster that lost a whole attack type would go red on the first.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.Equal(UnitTypeTable.CurrentLayout, table.Layout);

        Assert.Equal(
            new[] { AttackType.Pierce, AttackType.Impact, AttackType.Magic },
            table.Types.Where(row => row.Delivery != Delivery.None)
                .Select(row => row.AttackType)
                .Distinct()
                .OrderBy(type => (int)type));

        Assert.Equal(
            new[] { ArmourType.Swift, ArmourType.Armoured, ArmourType.Arcane },
            table.Types.Where(row => row.MaxHp > 0)
                .Select(row => row.ArmourType)
                .Distinct()
                .OrderBy(type => (int)type));

        // The swarm is the cheapest body and the fastest; the wall is the
        // dearest and the only one carrying armour points beside the drifter;
        // the sniper outranges every other tower; the sieger's shell spends the
        // longest in the air.
        //
        // OBSERVED: take the bulwark's forty-five points of armour down to
        // zero. Its own row goes red, 45 against 0, and the cost band above
        // goes red with it -- 7200 effective health bought against the 5000 it
        // would then carry -- because armour points are half of what a wall is.
        Assert.Equal(table.Types.Where(row => row.Role == UnitRole.Moving).Min(row => row.Cost), table.ById(5).Cost);
        Assert.Equal(table.Types.Max(row => row.SpeedMilliHexPerTick), table.ById(5).SpeedMilliHexPerTick);
        Assert.Equal(table.Types.Where(row => row.Role == UnitRole.Moving).Max(row => row.Cost), table.ById(6).Cost);
        Assert.Equal(45, table.ById(6).Armour);
        Assert.Equal(table.Types.Max(row => row.RangeMilliHex), table.ById(9).RangeMilliHex);
        Assert.Equal(AttackType.Magic, table.ById(9).AttackType);
        Assert.Equal(AttackType.Impact, table.ById(10).AttackType);
        Assert.Equal(table.Types.Max(row => row.ProjectileFlightTicks), table.ById(10).ProjectileFlightTicks);
    }

    [Fact]
    public void Every_walking_row_costs_its_effective_health_over_a_hundred_and_sixty()
    {
        // What a sauce buys is health a defense has to spend, so the price of a
        // creep is the pool it carries times its armour multiplier, over one
        // constant. The type chart deliberately stays out of it: every row and
        // every column of the matrix is a permutation of the same three cells,
        // so averaged over the three attack types every armour type is worth
        // exactly the same and an armour type that moved the price would be
        // charging twice for a bet.
        //
        // OBSERVED: halve the bulwark's cost, 45 to 22, in content/units.txt.
        // This goes red naming it -- "bulwark costs 22 sauce, which buys 3520
        // effective health at the roster's rate, against the 7250 it actually
        // carries" -- which is what the wall being twice the deal of everything
        // else on the menu looks like before anybody plays a round of it.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        foreach (UnitType creep in table.Types.Where(row => row.Role == UnitRole.Moving))
        {
            int effective = creep.MaxHp * (100 + creep.Armour) / 100;

            Assert.True(
                Math.Abs(effective - (creep.Cost * 160)) * 10 <= effective,
                creep.Label
                + " costs "
                + creep.Cost
                + " sauce, which buys "
                + (creep.Cost * 160)
                + " effective health at the roster's rate, against the "
                + effective
                + " it actually carries. Every walking row is within a tenth of it.");
        }
    }

    [Fact]
    public void The_committed_unit_table_is_in_the_current_column_layout_and_carries_its_columns()
    {
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.Equal(UnitTypeTable.CurrentLayout, table.Layout);

        // A creep: a cost, an armour type, and no attack type because it never
        // attacks.
        Assert.Equal(10, table.ById(1).Cost);
        Assert.Equal(ArmourType.Armoured, table.ById(1).ArmourType);
        Assert.Equal(AttackType.None, table.ById(1).AttackType);
        Assert.Equal(0, table.ById(1).Armour);
        Assert.Equal(ArmourType.Swift, table.ById(2).ArmourType);

        // A tower: an attack type, and no armour type because it has no health
        // pool to be protected.
        Assert.Equal(40, table.ById(3).Cost);
        Assert.Equal(AttackType.Pierce, table.ById(3).AttackType);
        Assert.Equal(ArmourType.None, table.ById(3).ArmourType);
        Assert.Equal(AttackType.Impact, table.ById(4).AttackType);
    }

    [Fact]
    public void Every_damage_and_health_number_in_the_committed_table_is_at_the_tenfold_scale()
    {
        // The rescale, as the numbers rather than as a sentence. Shots-to-kill
        // is unchanged because both sides moved together, which is what makes
        // this a resolution change rather than a balance change.
        //
        // OBSERVED: put grunt max hp back to the pre-scale 155 without touching
        // the bolt's damage. This goes red naming the row -- "grunt carries 155
        // health and rolls 0 to 0" -- and so does every artefact downstream of
        // it, which is the point: health and damage have to move together or
        // shots-to-kill moves with them.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        foreach (UnitType row in table.Types)
        {
            Assert.True(
                row.MaxHp % 10 == 0 && row.DamageMin % 10 == 0 && row.DamageMax % 10 == 0,
                row.Label
                + " carries "
                + row.MaxHp
                + " health and rolls "
                + row.DamageMin
                + " to "
                + row.DamageMax
                + ". Every health and damage number in this table is at the tenfold scale.");
        }

        Assert.Equal(1550, table.ById(1).MaxHp);
        Assert.Equal(1500, table.ById(2).MaxHp);
        Assert.Equal(90, table.ById(3).DamageMin);
        Assert.Equal(150, table.ById(3).DamageMax);
        Assert.Equal(210, table.ById(4).DamageMin);
        Assert.Equal(340, table.ById(4).DamageMax);

        // The damage columns against the pre-scale numbers themselves, which
        // are frozen in the table pinned beside the oldest golden bundle and
        // cannot be rewritten by anything. The health pools are not compared:
        // they were re-tuned against live typing after the scale and are
        // deliberately not ten times what they were.
        UnitTypeTable preScale = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.GoldenUnitsFile(0)));

        Assert.Equal(preScale.ById(3).DamageMin * 10, table.ById(3).DamageMin);
        Assert.Equal(preScale.ById(3).DamageMax * 10, table.ById(3).DamageMax);
        Assert.Equal(preScale.ById(4).DamageMin * 10, table.ById(4).DamageMin);
        Assert.Equal(preScale.ById(4).DamageMax * 10, table.ById(4).DamageMax);
    }

    [Fact]
    public void A_table_with_no_layout_row_is_layout_one_and_keeps_the_hash_it_always_had()
    {
        // The load-bearing half of the layout branch. content/golden/ holds one
        // unit table per golden bundle, each one the table that bundle was
        // recorded against, and the bundle's header carries that table's hash.
        // Adding columns to content/units.txt may not move it: nothing can
        // re-record a retired format version, so a moved hash there deletes the
        // only version-0 defense record that will ever exist.
        //
        // OBSERVED: fold layout 1 under HashLabelOf(2) -- one character in
        // UnitTypeTable -- and this goes red, BED71DB9C933345E against
        // 39B848CEFDDCC9CF, along with every golden in the runner. The literal
        // is here rather than computed so that the number this asserts is the
        // one a bundle written years ago is holding.
        UnitTypeTable pinned = UnitTypeTable.Parse(
            "golden/defense-0.units",
            File.ReadAllText(RepoLayout.GoldenUnitsFile(0)));

        Assert.Equal(UnitTypeTable.DefaultLayout, pinned.Layout);
        Assert.Equal(Hash64.FromValue(LayoutOneHashOfTheOldestPin), pinned.ContentHash);
        Assert.Equal(4, pinned.Count);
    }

    [Fact]
    public void A_layout_one_row_carries_no_cost_and_no_place_in_the_damage_matrix()
    {
        // There is no value this branch could supply that the table it is
        // reading ever stated, so it supplies none. A cost of zero and a type
        // of none are what a layout-1 row is, not defaults standing in for
        // something.
        UnitType grunt = UnitTypeTable.Parse(ThreeGoodRows).ById(1);

        Assert.Equal(0, grunt.Cost);
        Assert.Equal(AttackType.None, grunt.AttackType);
        Assert.Equal(ArmourType.None, grunt.ArmourType);
        Assert.Equal(0, grunt.Armour);

        // And a layout-1 table's rows are refused by the matrix rather than
        // resolved through a cell nobody authored.
        Assert.Throws<SimulationException>(
            () => TheRuleset.Committed().Matrix.Cell(grunt.AttackType, ArmourType.Swift));
    }

    [Fact]
    public void The_two_layouts_hash_differently_even_where_every_shared_number_agrees()
    {
        // Both tables hold the same thirteen numbers per row. They fold under
        // different labels, so a record pinned to one is retired by the other
        // rather than being reinterpreted against shifted fields.
        //
        // OBSERVED: return the same label from both branches of HashLabelOf.
        // This goes red, and a layout-1 record and a layout-2 record whose
        // shared columns agree become indistinguishable at the replay gate.
        UnitTypeTable one = UnitTypeTable.Parse(ThreeGoodRows);
        UnitTypeTable two = UnitTypeTable.Parse(ThreeCurrentRows);

        Assert.Equal(UnitTypeTable.DefaultLayout, one.Layout);
        Assert.Equal(UnitTypeTable.CurrentLayout, two.Layout);
        Assert.Equal(one.ById(1).MaxHp, two.ById(1).MaxHp);
        Assert.NotEqual(one.ContentHash, two.ContentHash);
    }

    [Fact]
    public void The_current_layout_folds_the_columns_it_added()
    {
        // A layout that declared four columns and folded none of them would let
        // a retune of any of them pass every gate in the project.
        //
        // OBSERVED: fold a constant in place of the cost in UnitType.Fold's
        // layout-2 branch. The first assertion goes red with both tables on
        // 87926632A0DCC30A, which is a cost column that can be retuned without
        // retiring anything pinned to the old number.
        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace(" 40 pierce", " 41 pierce")).ContentHash);

        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("none armoured 0", "none arcane 0")).ContentHash);

        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("none armoured 0", "none armoured 7")).ContentHash);

        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("0 40 pierce", "0 40 magic")).ContentHash);
    }

    [Fact]
    public void A_row_with_the_current_layouts_columns_and_no_layout_row_refuses_by_name()
    {
        // The mistake a designer makes: four columns added and the layout row
        // forgotten. It is refused rather than read against layout 1's field
        // order, and the message names both counts and the row that fixes it.
        //
        // OBSERVED: infer the layout from the field count instead of reading a
        // declaration. This goes red having caught nothing, and a file whose
        // columns were added in the wrong order parses as layout 2 with its
        // fields transposed.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 2", "# the layout row, forgotten")));

        Assert.Contains("column layout 1 has 15", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("'layout' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_with_layout_ones_columns_under_a_layout_two_declaration_refuses_by_name()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("layout 2\n" + ThreeGoodRows));

        Assert.Contains("column layout 2 has 19", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_layout_this_reader_has_no_branch_for_refuses_by_name()
    {
        // Spelled out one layout at a time, so a layout that was skipped or a
        // branch somebody deleted arrives here rather than passing an
        // inequality and falling through a switch.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 2", "layout 3")));

        Assert.Contains("declares column layout 3", thrown.Message, StringComparison.Ordinal);
        Assert.False(UnitTypeTable.IsKnownLayout(0));
        Assert.False(UnitTypeTable.IsKnownLayout(3));
        Assert.True(UnitTypeTable.IsKnownLayout(1));
        Assert.True(UnitTypeTable.IsKnownLayout(UnitTypeTable.CurrentLayout));
    }

    [Fact]
    public void A_second_layout_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("layout 2\n" + ThreeCurrentRows));

        Assert.Contains("second 'layout' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_layout_row_after_the_first_unit_refuses_to_load()
    {
        // The layout says how to read a row, so it cannot arrive after rows
        // have already been read against another one.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeGoodRows + "\nlayout 2"));

        Assert.Contains("after 3 rows", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unit_that_attacks_with_no_attack_type_refuses_to_load()
    {
        // OBSERVED: delete RequireTyping's first clause. This goes red having
        // caught nothing, and a tower that fires every six ticks resolves its
        // shots through a row of the matrix that does not exist.
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("hitscan 0 0 40 pierce", "hitscan 0 0 40 none")));

        Assert.Contains("outside the damage matrix", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unit_with_a_health_pool_and_no_armour_type_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("12 10 none armoured 0", "12 10 none none 0")));

        Assert.Contains("outside the damage matrix", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unit_that_never_attacks_carrying_an_attack_type_refuses_to_load()
    {
        // The mirror of the flight-ticks rule: a number read by nothing that
        // would still move the content hash.
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("12 10 none armoured 0", "12 10 magic armoured 0")));

        Assert.Contains("delivers no damage", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unit_with_no_health_pool_carrying_an_armour_type_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("40 pierce none 0", "40 pierce swift 0")));

        Assert.Contains("no health pool", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Armour_with_no_armour_type_to_apply_it_through_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("40 pierce none 0", "40 pierce none 12")));

        Assert.Contains("no armour type", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_attack_type_outside_the_fixed_three_way_cycle_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("40 pierce none 0", "40 holy none 0")));

        Assert.Contains("pierce, impact, magic, none", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_armour_type_outside_the_fixed_three_way_cycle_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse(
            ThreeCurrentRows.Replace("none armoured 0", "none ethereal 0")));

        Assert.Contains("swift, armoured, arcane, none", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_cost_refuses_to_load()
    {
        Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("0 12 10 none armoured", "0 12 -10 none armoured")));
    }

    [Fact]
    public void The_committed_wave_parses_against_the_committed_types()
    {
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        WaveScript wave = WaveScript.Parse(File.ReadAllText(RepoLayout.WaveFile), types);

        Assert.Equal(6, wave.Count);
        Assert.Equal(40, wave.TotalUnits);
        Assert.Equal(0, wave.Orders[0].TickOffset);
    }

    [Fact]
    public void The_simulation_takes_bytes_as_well_as_text_and_agrees_with_itself()
    {
        // A caller that read a file holds bytes. Making it decode first would
        // put the one decision that can differ between platforms -- which
        // encoding -- outside the assembly whose version is supposed to own
        // every such decision.
        UnitTypeTable fromText = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UnitTypeTable fromBytes = UnitTypeTable.ParseUtf8(File.ReadAllBytes(RepoLayout.UnitsFile));

        Assert.Equal(fromText.ContentHash, fromBytes.ContentHash);
    }

    [Fact]
    public void A_byte_order_mark_is_not_a_content_change()
    {
        string text = File.ReadAllText(RepoLayout.UnitsFile);
        byte[] withMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text))
            .ToArray();

        Assert.Equal(
            UnitTypeTable.Parse(text).ContentHash,
            UnitTypeTable.ParseUtf8(withMark).ContentHash);
    }

    [Fact]
    public void No_committed_numeric_data_file_contains_a_decimal_point()
    {
        // The parser refuses one, which is the mechanism. This is the second
        // half: the committed files are checked directly, so a decimal point
        // cannot sit in a file that nothing happens to load today and become a
        // load failure the first time something does.
        foreach (string file in RepoLayout.NumericContentFiles)
        {
            string[] lines = File.ReadAllLines(file);

            for (int index = 0; index < lines.Length; index++)
            {
                if (lines[index].TrimStart().StartsWith('#'))
                {
                    continue;
                }

                Assert.False(
                    lines[index].Contains('.') || lines[index].Contains(','),
                    $"{Path.GetFileName(file)}({index + 1}) carries a decimal point or comma on a data "
                    + $"line: {lines[index]}");
            }
        }
    }

    [Fact]
    public void A_decimal_point_in_a_data_file_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("unit 1 grunt moving 240 34.5 0 0 0 0 0 0 none 0 12"));

        Assert.Contains("'.'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, thrown.Line);
    }

    [Fact]
    public void A_decimal_comma_is_the_same_mistake_and_is_refused_too()
    {
        // The locale that writes 34,5 is exactly the locale the hostile-culture
        // test below runs under. Refusing the character means the two defences
        // are independent rather than one defence counted twice.
        Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("unit 1 grunt moving 240 34,5 0 0 0 0 0 0 none 0 12"));
    }

    [Fact]
    public void An_exponent_is_not_an_integer_either()
    {
        Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("unit 1 grunt moving 240 1e3 0 0 0 0 0 0 none 0 12"));
    }

    [Fact]
    public void A_digit_that_is_not_an_ascii_digit_is_not_a_digit()
    {
        // Arabic-Indic four. int.Parse accepts this under some cultures; the
        // hand-rolled reader refuses it as a character, before the question of
        // what it means as a number ever arises.
        Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("unit 1 grunt moving 240 \u0664 0 0 0 0 0 0 none 0 12"));
    }

    [Fact]
    public void A_duplicate_type_id_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12
            unit 1 runner moving 130 61 0 0 0 0 0 0 none 0 12
            """));

        Assert.Contains("reuses type id 1", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, thrown.Line);
    }

    [Fact]
    public void Ids_that_go_backwards_refuse_to_load()
    {
        Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            unit 4 grunt  moving 240 34 0 0 0 0 0 0 none 0 12
            unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12
            """));
    }

    [Fact]
    public void A_row_with_the_wrong_number_of_fields_refuses_to_load()
    {
        Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("unit 1 grunt moving 240 34 0 0 0 0 0 0 none 0"));
    }

    [Fact]
    public void An_unknown_type_id_in_a_wave_refuses_to_load()
    {
        UnitTypeTable types = UnitTypeTable.Parse(ThreeGoodRows);

        ContentException thrown = Assert.Throws<ContentException>(
            () => WaveScript.Parse("order 0 99 4 0", types));

        Assert.Contains("does not define", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_wave_that_sends_a_placed_unit_refuses_to_load()
    {
        UnitTypeTable types = UnitTypeTable.Parse(ThreeGoodRows);

        Assert.Throws<ContentException>(() => WaveScript.Parse("order 0 7 4 0", types));
    }

    [Fact]
    public void Wave_orders_out_of_canonical_order_refuse_to_load()
    {
        UnitTypeTable types = UnitTypeTable.Parse(ThreeGoodRows);

        Assert.Throws<ContentException>(() => WaveScript.Parse("""
            order 40 1 4 0
            order 10 2 4 0
            """, types));
    }

    [Fact]
    public void A_repeated_wave_order_key_refuses_to_load()
    {
        UnitTypeTable types = UnitTypeTable.Parse(ThreeGoodRows);

        Assert.Throws<ContentException>(() => WaveScript.Parse("""
            order 40 1 4 0
            order 40 1 4 0
            """, types));
    }

    [Fact]
    public void Reformatting_the_file_does_not_move_the_content_hash()
    {
        // This is the whole reason the hash is over parsed integers rather than
        // over bytes. Every edit below changes the file and none of them
        // changes a number, so a byte hash would retire every stored record
        // that pinned this table and the signal would be worthless.
        string original = File.ReadAllText(RepoLayout.UnitsFile);
        Hash64 hash = UnitTypeTable.Parse(original).ContentHash;

        Assert.Equal(hash, UnitTypeTable.Parse(Respaced(original)).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(CommentsRewritten(original)).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(original.Replace("\r\n", "\n")).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(original.Replace("\n", "\r\n")).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(original + "\n\n\n").ContentHash);
    }

    [Fact]
    public void Renaming_a_label_does_not_move_the_content_hash()
    {
        // A label is not an identity, so it is not in the fold. If it were,
        // fixing a spelling mistake would be a balance patch.
        Assert.Equal(
            UnitTypeTable.Parse(ThreeGoodRows).ContentHash,
            UnitTypeTable.Parse(ThreeGoodRows.Replace("runner", "sprinter")).ContentHash);
    }

    [Fact]
    public void Changing_one_number_does_move_the_content_hash()
    {
        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeGoodRows).ContentHash,
            UnitTypeTable.Parse(ThreeGoodRows.Replace(" 240 ", " 241 ")).ContentHash);
    }

    [Fact]
    public void Two_rows_swapping_their_numbers_move_the_content_hash()
    {
        // Field order is part of the fold, so a table whose numbers are the
        // same multiset but in different places is a different table.
        string swapped = """
            unit 1 grunt  moving 130 61 0 0 0 0 0 0 none 0 12
            unit 2 runner moving 240 34 0 0 0 0 0 0 none 0 12
            unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0
            """;

        Assert.NotEqual(UnitTypeTable.Parse(ThreeGoodRows).ContentHash, UnitTypeTable.Parse(swapped).ContentHash);
    }

    [Fact]
    public void The_content_hash_is_not_the_map_hash_of_the_same_numbers()
    {
        // Both folds start from a label naming the table and its layout, so two
        // tables cannot collide by holding coincidentally equal integers.
        HexMap map = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.NotEqual(types.ContentHash, map.MapHash);
    }

    private static string Respaced(string text) =>
        string.Join(
            "\n",
            text.Split('\n').Select(line => line.StartsWith('#') ? line : line.Replace("  ", " ")));

    private static string CommentsRewritten(string text) =>
        string.Join(
            "\n",
            text.Split('\n').Select(line => line.TrimStart().StartsWith('#') ? "# something else entirely" : line));
}
