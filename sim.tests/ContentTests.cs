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
    private const string ThreeTypedRows = """
        layout 2
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0
        """;

    /// <summary>
    /// The same three rows again in column layout 3, carrying nothing in any of
    /// the nine columns it adds -- which is what every committed row carries and
    /// is not the same thing as a row that has no such columns.
    /// </summary>
    private const string ThreeCurrentRows = """
        layout 3
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 none none none 0 none 0 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0 0 1 none none none 0 none 0 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0
        """;

    /// <summary>
    /// What the unit table pinned beside the version-0 golden bundle hashes to,
    /// and what its bundle's header carries.
    /// </summary>
    private const ulong LayoutOneHashOfTheOldestPin = 0x39B848CEFDDCC9CFUL;

    /// <summary>
    /// What <see cref="ThreeTypedRows"/> hashed to before layout 3 existed, and
    /// what it has to go on hashing to.
    /// </summary>
    /// <remarks>
    /// Layout 1's stability is pinned by a golden bundle that cannot be recorded
    /// again. Layout 2's is pinned by nothing at all now that the current
    /// version's golden has been re-recorded at layout 3, so it is pinned here
    /// instead -- one literal, in the one place a widening would move it.
    /// </remarks>
    private const ulong LayoutTwoHashOfThreeRows = 0x0EEC991C9FDDAF07UL;

    /// <summary>
    /// The Mage's id. Named because two tests below single the row out -- it is
    /// the one placed row the cost rule does not price, and why is a claim of
    /// its own.
    /// </summary>
    private const int Mage = 4;

    [Fact]
    public void The_committed_unit_table_parses()
    {
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.Equal(9, table.Count);
        Assert.Equal("minion", table.ById(1).Label);
        Assert.Equal(UnitRole.Moving, table.ById(2).Role);
        Assert.Equal(Delivery.Hitscan, table.ById(3).Delivery);
        Assert.Equal(Delivery.Projectile, table.ById(4).Delivery);
        Assert.Equal(33, table.ById(4).ProjectileFlightTicks);

        // Five of them walk, which is what an offering is drawn out of, and
        // four stand. The walker count is what lets the ruleset ask for three
        // ordinary options a round, and it is the tightest it has ever been:
        // five walkers against three options puts most of the roster on every
        // menu.
        //
        // The Ranger is the fourth thing that stands and it changed neither of
        // those, which is the point of a tier being a row: an offering is drawn
        // from the walkers alone, so a new tower does not enter a menu and
        // cannot move a draw.
        //
        // OBSERVED: change the skeleton's role from moving to placed in
        // content/units.txt. The walker count goes red, 5 against 4, and the
        // offering's own refusal follows it in BuildPhaseTests.
        Assert.Equal(5, table.Types.Count(row => row.Role == UnitRole.Moving));
        Assert.Equal(4, table.Types.Count(row => row.Role == UnitRole.Placed));

        // The five retired ids are gone and stay gone. Ids are never reused, so
        // these are not holes waiting to be filled -- a stored record pinning
        // one still means what it meant, and there is simply no live row for it.
        //
        // OBSERVED: re-add the wisp as `unit 5 ...` in content/units.txt. This
        // goes red on id 5, and the count assertion above goes red with it.
        foreach (int retired in new[] { 5, 6, 8, 9, 10 })
        {
            Assert.DoesNotContain(table.Types, row => row.Id == retired);
        }
    }

    [Fact]
    public void The_committed_roster_and_the_committed_ladder_have_no_faults()
    {
        // The enforcer. Its precedent is
        // No_committed_numeric_data_file_contains_a_decimal_point below, and that
        // test's own comment is the argument: the pass is the mechanism, and this
        // is the second half -- the committed files are checked directly, so a
        // fault cannot sit in a pair that nothing happens to walk today and
        // become somebody's problem the first time something does.
        //
        // DELIBERATELY NOT A CENSUS. No row count and no pinned edge list: the
        // ladder is expected to grow one row at a time, and a census would go red
        // on every legitimate authoring. The notes are not asserted on at all --
        // a note is a design statement and asserting one would make it a rule,
        // and the committed pair has three of them, including the flat price the
        // Ranger carries by way of the cost rule not pricing range.
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UpgradeLadder ladder = UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

        LadderReport report = ladder.Completeness(types);

        Assert.True(
            report.HasNoFaults,
            "content/upgrades.txt and content/units.txt disagree:\n  "
            + string.Join("\n  ", report.Faults.Select(fault => fault.Sentence)));
    }

    [Fact]
    public void The_roster_spans_the_matrix_and_every_shape_is_a_row()
    {
        // Eight units and no nineteenth column. Every attack type and every
        // armour type is on the roster, so nothing in the matrix is a cell no
        // committed unit can reach -- and under the signed roster's one-type-
        // per-tower-line rule the three attack types are covered exactly once
        // each rather than lopsidedly, which is what makes a tower's line
        // readable off the board.
        //
        // OBSERVED: give the Mage `pierce` instead of `magic` -- one word in
        // content/units.txt. The distinct-attack-types assertion goes red
        // straight away, because with three towers and three types there is no
        // second row carrying magic to hide behind. That is the whole gain of
        // one type per line: the roster cannot lose a type quietly.
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

        // The ends of each axis, named. The Skeleton Scout is the cheapest body
        // and the fastest; the Skeleton Warrior is the dearest, the slowest and
        // carries the most armour; the Mage outranges the other towers and is
        // the only one that puts anything in the air; the Soldier is the
        // shortest-ranged and the cheapest thing that stands.
        //
        // OBSERVED: take the Warrior's forty-five points of armour down to
        // zero. Its own row goes red, 45 against 0, and the dearest-walker
        // assertion goes red with it -- at zero armour the Warrior prices at 21
        // and the Necromancer's 19 is no longer the row it beats by much --
        // because armour points are half of what a heavy body is.
        Assert.Equal(table.Types.Where(row => row.Role == UnitRole.Moving).Min(row => row.Cost), table.ById(2).Cost);
        Assert.Equal(table.Types.Max(row => row.SpeedMilliHexPerTick), table.ById(2).SpeedMilliHexPerTick);
        Assert.Equal(table.Types.Where(row => row.Role == UnitRole.Moving).Max(row => row.Cost), table.ById(13).Cost);
        Assert.Equal(
            table.Types.Where(row => row.Role == UnitRole.Moving).Min(row => row.SpeedMilliHexPerTick),
            table.ById(13).SpeedMilliHexPerTick);
        Assert.Equal(45, table.ById(13).Armour);
        Assert.Equal(table.Types.Max(row => row.RangeMilliHex), table.ById(4).RangeMilliHex);
        Assert.Equal(AttackType.Magic, table.ById(4).AttackType);
        Assert.Equal(table.Types.Max(row => row.ProjectileFlightTicks), table.ById(4).ProjectileFlightTicks);
        Assert.Equal(AttackType.Impact, table.ById(11).AttackType);
        Assert.Equal(
            table.Types.Where(row => row.Role == UnitRole.Placed).Min(row => row.RangeMilliHex),
            table.ById(11).RangeMilliHex);

        // One attack type per tower line, spelled out. This is the rule the
        // roster was signed under and it is what lets a player read what a
        // tower does to a body from which line it came from.
        //
        // OBSERVED: give the Soldier `pierce`. This goes red on its own row,
        // Impact against Pierce, and the distinct-types assertion above goes
        // red with it because impact then belongs to nothing.
        Assert.Equal(AttackType.Impact, table.ById(11).AttackType);
        Assert.Equal(AttackType.Pierce, table.ById(3).AttackType);
        Assert.Equal(AttackType.Magic, table.ById(4).AttackType);
    }

    [Fact]
    public void Every_walking_row_costs_its_effective_health_over_a_hundred_and_sixty()
    {
        // What gold buys is health a defense has to spend, so the price of a
        // creep is the pool it carries times its armour multiplier, over one
        // constant. The type chart deliberately stays out of it: every row and
        // every column of the matrix is a permutation of the same three cells,
        // so averaged over the three attack types every armour type is worth
        // exactly the same and an armour type that moved the price would be
        // charging twice for a bet.
        //
        // OBSERVED: halve the Skeleton Warrior's cost, 31 to 15, in
        // content/units.txt. This goes red naming it -- "skeleton-warrior costs
        // 15 gold, which buys 2400 effective health at the roster's rate,
        // against the 4930 it actually carries" -- which is what the heaviest
        // body being twice the deal of everything else on the menu looks like
        // before anybody plays a round of it.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        foreach (UnitType creep in table.Types.Where(row => row.Role == UnitRole.Moving))
        {
            int effective = creep.MaxHp * (100 + creep.Armour) / 100;

            Assert.True(
                Math.Abs(effective - (creep.Cost * 160)) * 10 <= effective,
                creep.Label
                + " costs "
                + creep.Cost
                + " gold, which buys "
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
        Assert.Equal(AttackType.Magic, table.ById(4).AttackType);
    }

    [Fact]
    public void Every_placed_row_costs_its_damage_a_second_times_the_bodies_it_hits_over_five()
    {
        // The other half of the purse, and it is arithmetic for the same reason
        // the creep rule is: one wallet buys both sides of the board, so both
        // sides have to be priced in the same quantity. A tower is paid for by
        // the health it removes, which is what a creep's price is measured in.
        //
        // The constant is five damage a second per gold, and "a second" is
        // thirty ticks. That tie to the tick rate is the rule's one fragile
        // edge and it is written into content/units.txt beside the rule: the
        // clock has moved once already, and if it moves again every tower
        // silently stops being based until the constant is re-derived.
        //
        // THE BODIES TERM IS THE TARGETS COLUMN, and until layout 3 it was a
        // guess: three bodies for anything delivering by projectile and one for
        // everything else, because no column said how many bodies a shot hit.
        // One does now, so the rule reads the row instead of guessing at it.
        //
        // What that exposed is the Mage, and it is asserted below rather than
        // tuned away: see The_mage_is_priced_for_a_splash_nobody_has_authored.
        // Every other placed row is priced on the shots it actually fires.
        //
        // OBSERVED: halve the Archer's cost, 40 to 20, in content/units.txt.
        // This goes red naming it -- "archer costs 20 gold, which is 5 damage a
        // second per gold against the 40 it actually deals" -- which is what a
        // tower being twice the deal of everything else looks like before
        // anybody plays a round of it.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        foreach (UnitType tower in table.Types.Where(row => row.Role == UnitRole.Placed && row.Id != Mage))
        {
            int bodies = tower.Targets;
            int average = (tower.DamageMin + tower.DamageMax) / 2;

            // Damage a second, times bodies, at thirty ticks a second. Held as
            // one integer expression so no division rounds before the compare.
            int perSecondTimesBodies = average * bodies * Match.TicksPerSecond / tower.CooldownTicks;

            Assert.True(
                Math.Abs(perSecondTimesBodies - (tower.Cost * 5)) * 50 <= perSecondTimesBodies,
                tower.Label
                + " costs "
                + tower.Cost
                + " gold, which is 5 damage a second per gold against the "
                + (perSecondTimesBodies / 5)
                + " it actually deals. Every placed row is within two percent of the rule.");
        }
    }

    [Fact]
    public void A_multi_target_row_is_priced_on_every_shot_it_fires()
    {
        // The other half of the correction, and the half no committed row can
        // show: the rule's bodies term now reads a column, so a row that fires
        // three shots prices at three times a row that fires one and is
        // otherwise identical. A Marksman is priced on arrival rather than at
        // the price of a single-target Archer -- which is the whole of what the
        // guess it replaced got wrong.
        //
        // A fixture rather than a row in content/units.txt, deliberately:
        // naming and statting a tower is a design decision and this ticket took
        // none. What is checked here is the arithmetic.
        //
        // OBSERVED: put the bodies term back to Delivery == Projectile ? 3 : 1.
        // Both of these go red -- the hitscan trio prices at a third of what it
        // costs and the projectile pair at three times -- which is exactly the
        // pair of errors the column removes.
        UnitTypeTable table = UnitTypeTable.Parse("""
            layout 3
            unit 1 one   placed 0 0 3200 18 9 6 90 150 hitscan    0  0 40  pierce none 0 0 1 none none none 0 none 0 0
            unit 2 three placed 0 0 3200 18 9 6 90 150 hitscan    0  0 120 pierce none 0 0 3 none none none 0 none 0 0
            unit 3 lob   placed 0 0 3200 18 9 6 90 150 projectile 20 0 40  pierce none 0 0 1 none none none 0 none 0 0
            """);

        foreach (UnitType tower in table.Types)
        {
            int perSecondTimesBodies =
                (tower.DamageMin + tower.DamageMax) / 2 * tower.Targets * Match.TicksPerSecond / tower.CooldownTicks;

            Assert.True(
                Math.Abs(perSecondTimesBodies - (tower.Cost * 5)) * 50 <= perSecondTimesBodies,
                tower.Label + " prices at " + (perSecondTimesBodies / 5) + " against the " + tower.Cost + " it carries.");
        }
    }

    [Fact]
    public void The_mage_is_priced_for_a_splash_nobody_has_authored()
    {
        // THE FINDING, PINNED RATHER THAN TUNED AWAY. The Mage costs 92 gold,
        // which is three bodies' worth of the cost rule, and it fires one
        // projectile at one creep: its splash has been a design statement in
        // docs/roster.md -- "splash of one additional hex", radius 1000 -- and
        // never a thing the simulation did. The old bodies term guessed three
        // from the delivery column and hid that; the targets column does not.
        //
        // Layout 3 is the first schema that could carry the splash, as a bubble
        // on the target with a radius and a damage payload. Authoring it, or
        // repricing the row to what it does, is a design decision, and #216
        // took neither: this is the standing question in the artefact rather
        // than a number quietly moved to make an assertion green.
        //
        // OBSERVED: it goes red both ways. Author the Mage a bubble and the
        // ratio stops being three; reprice it to 30 and the same. Either edit
        // is somebody deciding what a Mage is, and either edit is meant to
        // arrive here.
        UnitType mage = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile)).ById(Mage);

        Assert.Equal(1, mage.Targets);
        Assert.False(mage.Bubble.Present);

        int perSecond = (mage.DamageMin + mage.DamageMax) / 2 * Match.TicksPerSecond / mage.CooldownTicks;

        Assert.Equal(30, perSecond / 5);
        Assert.Equal(92, mage.Cost);
    }

    [Fact]
    public void Every_damage_and_health_number_in_the_committed_table_is_at_the_tenfold_scale()
    {
        // The rescale, as the numbers rather than as a sentence. Shots-to-kill
        // is unchanged because both sides moved together, which is what makes
        // this a resolution change rather than a balance change.
        //
        // OBSERVED: put the Minion's max hp back to the pre-scale 155 without
        // touching the Archer's damage. This goes red naming the row -- "minion
        // carries 155 health and rolls 0 to 0" -- and so does every artefact
        // downstream of it, which is the point: health and damage have to move
        // together or shots-to-kill moves with them.
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
    public void Every_layout_hashes_differently_even_where_every_shared_number_agrees()
    {
        // Three tables holding the same thirteen numbers per row, and layouts 2
        // and 3 agreeing about the four columns after them as well. They fold
        // under different labels, so a record pinned to one is retired by the
        // others rather than being reinterpreted against shifted fields.
        //
        // OBSERVED: return the same label from two branches of HashLabelOf.
        // This goes red, and two records whose shared columns agree become
        // indistinguishable at the replay gate.
        //
        // The layout-2 row is the load-bearing one now that it is neither the
        // oldest nor the current: nine of the nine columns layout 3 adds carry
        // nothing in the fixture below, so if the widening had folded the
        // absent ones as zeroes under the old label, this pair would be equal.
        UnitTypeTable one = UnitTypeTable.Parse(ThreeGoodRows);
        UnitTypeTable two = UnitTypeTable.Parse(ThreeTypedRows);
        UnitTypeTable three = UnitTypeTable.Parse(ThreeCurrentRows);

        Assert.Equal(UnitTypeTable.DefaultLayout, one.Layout);
        Assert.Equal(2, two.Layout);
        Assert.Equal(UnitTypeTable.CurrentLayout, three.Layout);

        Assert.Equal(one.ById(1).MaxHp, two.ById(1).MaxHp);
        Assert.Equal(two.ById(1).MaxHp, three.ById(1).MaxHp);
        Assert.Equal(two.ById(7).Cost, three.ById(7).Cost);
        Assert.Equal(two.ById(7).AttackType, three.ById(7).AttackType);

        Assert.NotEqual(one.ContentHash, two.ContentHash);
        Assert.NotEqual(two.ContentHash, three.ContentHash);
        Assert.NotEqual(one.ContentHash, three.ContentHash);
    }

    [Fact]
    public void A_layout_two_table_still_parses_and_carries_exactly_what_it_always_did()
    {
        // The half of a widening that has to be checked every time: the older
        // branch is not touched by the newer one. A layout-2 file is what
        // content/units.txt was an hour ago, so a widening that quietly changed
        // how one reads would retire records nobody edited.
        //
        // THE HASH IS A LITERAL FOR THE REASON LAYOUT ONE'S IS. No golden pins a
        // layout-2 table any more -- content/golden/defense-1.units was
        // re-recorded at layout 3 with the bundle beside it -- so nothing else
        // in this repository would notice layout 2's fold moving, and the next
        // widening is exactly when that would happen. The number was read off
        // this build; what makes it evidence is that it is the number a
        // layout-2 record stamped before layout 3 existed.
        //
        // OBSERVED: fold Shield and Targets in UnitType.Fold's layout-2 branch
        // as well. This goes red, 8FE6E2B1D7D01FB4 against the literal, and
        // nothing else in the suite notices.
        UnitTypeTable table = UnitTypeTable.Parse(ThreeTypedRows);

        Assert.Equal(2, table.Layout);
        Assert.Equal(Hash64.FromValue(LayoutTwoHashOfThreeRows), table.ContentHash);
        Assert.Equal(3, table.Count);
        Assert.Equal(10, table.ById(1).Cost);
        Assert.Equal(ArmourType.Armoured, table.ById(1).ArmourType);
        Assert.Equal(AttackType.Pierce, table.ById(7).AttackType);

        // And what such a row carries in the columns it does not have: no
        // second pool, one shot an attack, and nothing radial. None of those is
        // a default standing in for a value the table stated -- they are what a
        // layout-2 row IS.
        Assert.Equal(0, table.ById(1).Shield);
        Assert.Equal(1, table.ById(7).Targets);
        Assert.False(table.ById(7).Bubble.Present);
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
        // The mistake a designer makes: columns added and the layout row
        // forgotten. It is refused rather than read against layout 1's field
        // order, and the message names both counts and the row that fixes it.
        //
        // OBSERVED: infer the layout from the field count instead of reading a
        // declaration. This goes red having caught nothing, and a file whose
        // columns were added in the wrong order parses as the current layout
        // with its fields transposed.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 3", "# the layout row, forgotten")));

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
    public void A_row_with_layout_twos_columns_under_a_layout_three_declaration_refuses_by_name()
    {
        // The widening's own version of the test above, and the acceptance
        // criterion in as many words: a layout-3 row with the wrong field count
        // is refused rather than read against shifted fields. Nine columns is a
        // lot to leave off, and leaving off any of them would otherwise slide a
        // bubble magnitude into a duration.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeTypedRows.Replace("layout 2", "layout 3")));

        Assert.Contains("column layout 3 has 28", thrown.Message, StringComparison.Ordinal);

        // And one column short of the twenty-eight, which is the shape a real
        // mistake takes: a row that lost a field rather than nine of them.
        ContentException short_ = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("none 0 none 0 0", "none 0 none 0")));

        Assert.Contains("column layout 3 has 28", short_.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_layout_this_reader_has_no_branch_for_refuses_by_name()
    {
        // Spelled out one layout at a time, so a layout that was skipped or a
        // branch somebody deleted arrives here rather than passing an
        // inequality and falling through a switch.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 3", "layout 4")));

        Assert.Contains("declares column layout 4", thrown.Message, StringComparison.Ordinal);
        Assert.False(UnitTypeTable.IsKnownLayout(0));
        Assert.False(UnitTypeTable.IsKnownLayout(4));
        Assert.True(UnitTypeTable.IsKnownLayout(1));
        Assert.True(UnitTypeTable.IsKnownLayout(2));
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
    public void All_nine_of_the_columns_layout_three_added_parse_and_arrive_on_the_row()
    {
        // Every one of the nine, read off a row that authors something in all
        // of them. The five units the roster has been blocked on are each a
        // shape of this row and nothing more -- see
        // The_five_blocked_units_are_all_authorable_as_rows below.
        UnitType captain = UnitTypeTable.Parse("""
            layout 3
            unit 1 body    moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 150 1 none none none 0 none 0 0
            unit 2 emitter placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 2 2500 self friend 30 cooldown -25 90
            """).ById(2);

        Assert.Equal(0, captain.Shield);
        Assert.Equal(2, captain.Targets);
        Assert.True(captain.Bubble.Present);
        Assert.Equal(2500, captain.Bubble.RadiusMilliHex);
        Assert.Equal(BubbleOrigin.Self, captain.Bubble.Origin);
        Assert.Equal(BubbleAffects.Friend, captain.Bubble.Affects);
        Assert.Equal(30, captain.Bubble.PeriodTicks);
        Assert.Equal(BubblePayload.Cooldown, captain.Bubble.Payload);
        Assert.Equal(-25, captain.Bubble.Magnitude);
        Assert.Equal(90, captain.Bubble.DurationTicks);

        // And the shield column on the row that carries one, because a creep is
        // where a shield goes.
        UnitType body = UnitTypeTable.Parse("""
            layout 3
            unit 1 body moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 150 1 none none none 0 none 0 0
            """).ById(1);

        Assert.Equal(150, body.Shield);
        Assert.False(body.Bubble.Present);
    }

    [Fact]
    public void A_bubble_payload_of_range_is_refused_with_the_reason()
    {
        // Not "range is not on the list" -- WHY it is not. Coverage is
        // intersected with the route once, at load, and handed to the tick loop
        // as intervals of distance; a payload that moved a range would drag
        // that back inside the tick, which is the one thing the whole
        // arrangement exists to avoid.
        //
        // OBSERVED: delete ReadPayload's first clause. The row is still
        // refused -- 'range' is not one of the six words -- and the refusal
        // says "which is not one of: none, damage, speed, cooldown, armour,
        // shield", which tells a designer that range is missing and not that it
        // is impossible. They then ask for it to be added.
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            layout 3
            unit 1 haste placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 1000 self friend 30 range 25 90
            """));

        Assert.Contains("payload of 'range'", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("intersected", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A layout-3 row up to its bubble, so the seven columns that describe one
    /// can be varied against everything else holding still.
    /// </summary>
    private const string UpToTheBubble =
        "layout 3\nunit 1 thing placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 ";

    [Theory]
    [InlineData("none self none 0 none 0 0")]
    [InlineData("none none enemy 0 none 0 0")]
    [InlineData("none none none 12 none 0 0")]
    [InlineData("none none none 0 none 5 0")]
    [InlineData("none none none 0 none 0 7")]
    [InlineData("1000 none enemy 0 damage 0 0")]
    [InlineData("1000 target none 0 damage 0 0")]
    [InlineData("1000 target enemy 0 none 0 0")]
    [InlineData("1000 target enemy 0 damage 40 0")]
    [InlineData("1000 target enemy 0 damage 0 60")]
    [InlineData("1000 self enemy 30 speed 0 60")]
    [InlineData("1000 self enemy 30 speed -25 0")]
    public void A_half_authored_bubble_refuses_to_load(string bubble)
    {
        // Every way seven columns that describe one thing can disagree, one
        // case each: no radius but an origin, an affects, a period, a magnitude
        // or a duration anyway; a radius with the origin, the affects or the
        // payload left off; a damage bubble carrying a second damage number or
        // a duration; and a stat bubble that modifies nothing, or that modifies
        // something for no time at all.
        //
        // The control is the test below, which parses the absent spelling and
        // the authored one -- so a case that is green because the fixture never
        // reached the parser is impossible.
        //
        // OBSERVED: delete RequireBubble's body. All twelve go red at once,
        // which is what says they are pointed at the rule rather than at twelve
        // typos.
        Assert.Throws<ContentException>(() => UnitTypeTable.Parse(UpToTheBubble + bubble));
    }

    [Theory]
    [InlineData("none none none 0 none 0 0")]
    [InlineData("0 target enemy 0 damage 0 0")]
    [InlineData("1000 self enemy 0 damage 0 0")]
    [InlineData("2500 self friend 45 cooldown -20 60")]
    [InlineData("2000 self friend 90 shield 300 0")]
    public void A_bubble_whose_seven_columns_agree_loads(string bubble)
    {
        // The other side of the theory above, and the reason it is a test
        // rather than a comment: a refusal that fired on everything would pass
        // all twelve cases up there and author nothing at all.
        //
        // The last row is the one duration rule that is not symmetrical: a
        // shield is a POOL, granted and then spent, so it may say nothing about
        // how long it lasts. The three that modify a stat may not.
        Assert.Equal(1, UnitTypeTable.Parse(UpToTheBubble + bubble).Count);
    }

    [Fact]
    public void A_bubble_of_no_radius_centred_on_the_emitter_refuses_to_load()
    {
        // Zero is the target alone, and the emitter's own hex is the one place
        // nothing that walks can ever be -- a tower may not stand in the
        // corridor. So the pair is a bubble that reaches nothing whatever it is
        // pointed at, and the refusal says which column to change.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(UpToTheBubble + "0 self enemy 0 damage 0 0"));

        Assert.Contains("reaches the emitter and nothing else", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("target", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_stat_bubble_that_lasts_no_ticks_refuses_to_load()
    {
        // A modifier is a magnitude AND a duration: applied and expired inside
        // one tick, it changes nothing and would still move the content hash.
        // That is the same rule the zero magnitude beside it is refused by.
        foreach (string stat in new[] { "speed", "cooldown", "armour" })
        {
            ContentException thrown = Assert.Throws<ContentException>(
                () => UnitTypeTable.Parse(UpToTheBubble + "1000 target enemy 0 " + stat + " -25 0"));

            Assert.Contains("for no ticks at all", thrown.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void The_two_shot_shapes_may_not_be_claimed_by_one_row()
    {
        // n targets is n shots drawing n rolls; a damage bubble is one shot
        // drawing one roll that lands on everything it encloses. A row claiming
        // both would draw one of them per body of the other, and how many
        // numbers an attack takes off the dice stream is part of what every
        // stored record replays through.
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            layout 3
            unit 1 both placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 3 1000 target enemy 0 damage 0 0
            """));

        Assert.Contains("two shot shapes", thrown.Message, StringComparison.Ordinal);

        // Either one alone is fine, which is what makes the refusal about the
        // pair rather than about either column.
        Assert.Equal(
            1,
            UnitTypeTable.Parse("""
                layout 3
                unit 1 volley placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 3 none none none 0 none 0 0
                """).Count);

        Assert.Equal(
            1,
            UnitTypeTable.Parse("""
                layout 3
                unit 1 sweep placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 1000 self enemy 0 damage 0 0
                """).Count);
    }

    [Fact]
    public void A_shield_on_a_unit_with_no_health_pool_refuses_to_load()
    {
        // A shield absorbs first and overkill carries through to health, so a
        // shield on a thing nothing can damage is a pool nothing can ever
        // spend -- and it would still move the content hash.
        ContentException thrown = Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            layout 3
            unit 1 tower placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 200 1 none none none 0 none 0 0
            """));

        Assert.Contains("no health pool underneath it", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_that_never_attacks_may_not_author_shots_or_a_bubble_on_one()
    {
        // Both halves of "a column nobody reads is a number that still moves
        // the content hash", for the two columns a walking row has no use for.
        Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            layout 3
            unit 1 walker moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 3 none none none 0 none 0 0
            """));

        Assert.Throws<ContentException>(() => UnitTypeTable.Parse("""
            layout 3
            unit 1 walker moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 1000 self enemy 0 damage 0 0
            """));

        // An aura on a walking row is legal, because an aura has a clock of its
        // own and needs no attack to hang off. The Necromancer is that row.
        Assert.Equal(
            1,
            UnitTypeTable.Parse("""
                layout 3
                unit 1 walker moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 2000 self friend 60 shield 200 300
                """).Count);
    }

    [Fact]
    public void The_five_blocked_units_are_all_authorable_as_rows()
    {
        // THE ACCEPTANCE CRITERION, as the five rows docs/roster.md has been
        // waiting on. They are fixtures and not content: naming and statting a
        // tower is a design decision and #216 took none, so what is proved here
        // is that the schema carries each shape -- the numbers below are
        // stand-ins and no id, label or price among them means anything.
        //
        // The Cryomancer needs no dedicated slow columns: she is a bubble of
        // radius zero carrying a negative speed percentage for a while. The
        // Marksman is the targets column alone. A mortar is a blast on what it
        // hit. The Captain is an aura on itself over its friends. The
        // Necromancer grants shield in HEX distance rather than along the
        // marching column, which is what lets it reach the neighbouring leg of
        // a fold. And a tower that pulses over the whole board is one row with
        // a big radius in it.
        UnitTypeTable roster = UnitTypeTable.Parse("""
            layout 3
            unit 1 cryo    placed 0 0 4000 40 10 8 100 200 hitscan    0  0 50 magic  none 0 0 1 0     target enemy 0  speed    -40 120
            unit 2 marks   placed 0 0 5000 30 10 8 100 200 hitscan    0  0 90 pierce none 0 0 3 none  none   none  0  none       0   0
            unit 3 mortar  placed 0 0 4000 60 20 10 200 300 projectile 40 0 60 impact none 0 0 1 1500 target enemy 0  damage     0   0
            unit 4 captain placed 0 0 1000 30 5 5 50 60 hitscan       0  0 40 impact none 0 0 1 3000 self   friend 45 cooldown -20 60
            unit 5 necro   moving 2400 33 0 0 0 0 0 0 none 0 36 19 none arcane 25 0 1 2000 self friend 90 shield 300 240
            unit 6 pulse   placed 0 0 1000 90 10 10 300 400 hitscan   0  0 80 magic  none 0 0 1 60000 self enemy 0 damage 0 0
            """);

        Assert.Equal(6, roster.Count);

        Assert.Equal(BubblePayload.Speed, roster.ById(1).Bubble.Payload);
        Assert.Equal(0, roster.ById(1).Bubble.RadiusMilliHex);
        Assert.True(roster.ById(1).Bubble.Present, "A radius of zero is the target alone, not the absence of a bubble.");
        Assert.Equal(-40, roster.ById(1).Bubble.Magnitude);

        Assert.Equal(3, roster.ById(2).Targets);
        Assert.False(roster.ById(2).Bubble.Present);

        Assert.Equal(BubbleOrigin.Target, roster.ById(3).Bubble.Origin);
        Assert.True(roster.ById(3).Bubble.IsAnInstantBlast);

        Assert.True(roster.ById(4).Bubble.IsAnAura);
        Assert.Equal(BubbleAffects.Friend, roster.ById(4).Bubble.Affects);

        Assert.Equal(BubblePayload.Shield, roster.ById(5).Bubble.Payload);
        Assert.Equal(UnitRole.Moving, roster.ById(5).Role);

        Assert.Equal(60000, roster.ById(6).Bubble.RadiusMilliHex);
        Assert.True(roster.ById(6).Bubble.IsAnInstantBlast);
    }

    [Fact]
    public void The_nine_columns_layout_three_added_all_fold()
    {
        // A column that is declared and not folded is silent: retune it and the
        // content hash does not move, so every stored record stamped against
        // the old value passes the gate against the new one. One case per
        // column, and the pair that cannot both be zero is spelled as two rows.
        //
        // OBSERVED: fold a constant in place of the bubble in UnitType.Fold's
        // layout-3 branch. The seven bubble cases all come out equal and go
        // red, which is a bubble that can be retuned without retiring anything
        // pinned to the old shape.
        //
        // A placed row for eight of the nine and a walking one for the shield,
        // because the two columns belong to opposite halves of the loop: a
        // tower cannot carry a shield and a creep cannot fire a shot.
        Hash64 tower = UnitTypeTable.Parse(UpToTheBubbleOf(1, "none none none 0 none 0 0")).ContentHash;

        foreach (string moved in new[]
        {
            "1000 self enemy 0 damage 0 0",
            "1000 target enemy 0 damage 0 0",
            "1000 self friend 0 damage 0 0",
            "1000 self enemy 30 speed -40 60",
            "2000 self enemy 0 damage 0 0",
            "1000 self enemy 0 speed -40 60",
            "1000 self enemy 0 speed -50 60",
            "1000 self enemy 0 speed -40 90",
        })
        {
            Assert.NotEqual(tower, UnitTypeTable.Parse(UpToTheBubbleOf(1, moved)).ContentHash);
        }

        // The targets column, on the same row.
        Assert.NotEqual(tower, UnitTypeTable.Parse(UpToTheBubbleOf(3, "none none none 0 none 0 0")).ContentHash);

        // And the shield, on a row that can carry one.
        const string Walker =
            "layout 3\nunit 1 body moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 ";

        Assert.NotEqual(
            UnitTypeTable.Parse(Walker + "0 1 none none none 0 none 0 0").ContentHash,
            UnitTypeTable.Parse(Walker + "150 1 none none none 0 none 0 0").ContentHash);

        // And the one pair the fold has to keep apart that is not a number: a
        // bubble of no radius is the target alone, and no bubble is no bubble.
        // They fold as 0 and as -1, which is a radius no row can author.
        Assert.NotEqual(
            UnitTypeTable.Parse(UpToTheBubbleOf(1, "0 target enemy 0 damage 0 0")).ContentHash,
            UnitTypeTable.Parse(UpToTheBubbleOf(1, "none none none 0 none 0 0")).ContentHash);
    }

    /// <summary>
    /// The fixture row with a shot count and a bubble written into it. The row
    /// is a tower, because eight of the nine columns belong to something that
    /// shoots.
    /// </summary>
    private static string UpToTheBubbleOf(int targets, string bubble) =>
        "layout 3\nunit 1 thing placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 "
        + targets.ToString(System.Globalization.CultureInfo.InvariantCulture)
        + " "
        + bubble;

    [Fact]
    public void An_empty_ladder_folds_nothing_and_hands_back_the_table_it_was_given()
    {
        // The identity every record made before content/upgrades.txt existed
        // rests on. content/golden/defense-0.replay cannot be recorded again and
        // its header carries the hash of the table pinned beside it; no ladder is
        // pinned there, so nothing folds and that hash stands forever.
        //
        // OBSERVED: drop the empty-ladder branch out of WithLadder, so that a
        // ladder with no edges folds its label and a zero count anyway. This goes
        // red and so does the committed-pair assertion below it, and nothing else
        // in the suite notices -- because nothing folds a ladder into a golden
        // yet. That is why the identity is asserted here, on the method, rather
        // than left to whichever gate happens to fold one first.
        UnitTypeTable table = UnitTypeTable.Parse(ThreeCurrentRows);
        UpgradeLadder empty = UpgradeLadder.Parse("layout 1", table);

        Assert.Same(table, table.WithLadder(empty));
        Assert.Equal(table.ContentHash, table.WithLadder(empty).ContentHash);
    }

    [Fact]
    public void One_edge_moves_the_content_hash()
    {
        // The other half: a ladder with something in it is content, and content
        // that changes what a roster means has to retire the records pinned to
        // the roster before it.
        UnitTypeTable table = UnitTypeTable.Parse(ThreeCurrentRows);
        UpgradeLadder rung = UpgradeLadder.Parse("layout 1\nupgrade 1 2", table);

        Assert.NotEqual(table.ContentHash, table.WithLadder(rung).ContentHash);

        // And the rows are untouched, because an edge is an annotation on a
        // roster rather than a column on a row.
        Assert.Equal(table.Count, table.WithLadder(rung).Count);
        Assert.Equal(table.ById(1).Cost, table.WithLadder(rung).ById(1).Cost);
    }

    [Fact]
    public void Two_different_ladders_over_one_table_do_not_hash_alike()
    {
        // Which edges there are is what the fold is over, so an edge set that
        // was retuned rather than added moves the hash too.
        UnitTypeTable table = UnitTypeTable.Parse(ThreeCurrentRows);

        Assert.NotEqual(
            table.WithLadder(UpgradeLadder.Parse("layout 1\nupgrade 1 2", table)).ContentHash,
            table.WithLadder(UpgradeLadder.Parse("layout 1\nupgrade 1 7", table)).ContentHash);
    }

    [Fact]
    public void The_committed_ladder_moves_the_committed_tables_hash_exactly_when_it_has_an_edge()
    {
        // The rule rather than today's answer, so that authoring the first edge
        // is not also the commit that has to rewrite this test. While
        // content/upgrades.txt is empty this says the committed hash has not
        // moved, which is what makes every commit that lands before the first
        // edge safe; the moment an edge exists it says the hash moved, which is
        // what the regeneration beside that commit answers.
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UpgradeLadder ladder = UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

        Assert.Equal(ladder.Count == 0, types.ContentHash == types.WithLadder(ladder).ContentHash);
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
