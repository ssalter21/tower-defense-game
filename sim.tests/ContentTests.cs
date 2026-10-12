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
    /// the nine columns it adds -- which is not the same thing as a row that has
    /// no such columns.
    /// </summary>
    private const string ThreeRadialRows = """
        layout 3
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 none none none 0 none 0 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0 0 1 none none none 0 none 0 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0
        """;

    /// <summary>
    /// The same three rows once more in column layout 4, carrying nothing in the
    /// one column it adds.
    /// </summary>
    private const string ThreeSuccessorRows = """
        layout 4
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 none none none 0 none 0 0 none
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0 0 1 none none none 0 none 0 0 none
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0 none
        """;

    /// <summary>
    /// And once more in column layout 5, carrying nothing in either of the two
    /// columns it adds.
    /// </summary>
    private const string ThreeRaisingRows = """
        layout 5
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 none none none 0 none 0 0 none none 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0 0 1 none none none 0 none 0 0 none none 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0 none none 0
        """;

    /// <summary>
    /// And once more in column layout 6, paying nothing for being killed --
    /// which is what every committed row but the Grave Robber carries.
    /// </summary>
    private const string ThreeCurrentRows = """
        layout 6
        unit 1 grunt  moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0 0 1 none none none 0 none 0 0 none none 0 0
        unit 2 runner moving 130 61 0 0 0 0 0 0 none 0 12 15 none swift 0 0 1 none none none 0 none 0 0 none none 0 0
        unit 7 bolt   placed 0 0 3200 14 3 2 9 15 hitscan 0 0 40 pierce none 0 0 1 none none none 0 none 0 0 none none 0 0
        """;

    /// <summary>
    /// The Mage's id. Named because two tests below single the row out -- it is
    /// the one placed row the cost rule does not price, and why is a claim of
    /// its own.
    /// </summary>
    private const int Mage = 4;

    /// <summary>
    /// The effective health one gold buys a sender at the creep rule's rate.
    /// </summary>
    private const int EffectiveHealthPerGold = 160;

    /// <summary>
    /// The Mage line, which is the one line the cost rule does not price. The
    /// Mage is priced for a splash the bodies term cannot count, and the two
    /// rungs above it carry that price scaled by the cooldown they changed, so
    /// all three sit outside the rule together.
    /// </summary>
    private static readonly int[] TheMageLine = { Mage, 26 /* sorcerer */, 27 /* unravel */ };

    /// <summary>
    /// The nine tower lines: an attack type and three rungs each, in ascending
    /// order. Transcribed from docs/roster.md's index rather than walked out of
    /// content/upgrades.txt, because reading the lines off the ladder would be
    /// checking that file against itself.
    /// </summary>
    private static readonly (string Name, AttackType Attack, int[] Rungs)[] TheNineLines =
    {
        ("Knight", AttackType.Impact, new[] { 11, 15, 16 }),
        ("Barbarian", AttackType.Impact, new[] { 17, 18, 19 }),
        ("Engineer", AttackType.Impact, new[] { 35, 36, 37 }),
        ("Archer", AttackType.Pierce, new[] { 3, 14, 31 }),
        ("Rogue", AttackType.Pierce, new[] { 32, 33, 34 }),
        ("Mage", AttackType.Magic, new[] { 4, 26, 27 }),
        ("Paladin", AttackType.Magic, new[] { 20, 21, 22 }),
        ("Cleric", AttackType.Magic, new[] { 23, 24, 25 }),
        ("Druid", AttackType.Magic, new[] { 28, 29, 30 }),
    };

    [Fact]
    public void The_committed_unit_table_parses()
    {
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        Assert.Equal(44, table.Count);
        Assert.Equal("minion", table.ById(1).Label);
        Assert.Equal(UnitRole.Moving, table.ById(2).Role);
        Assert.Equal(Delivery.Hitscan, table.ById(3).Delivery);
        Assert.Equal(Delivery.Projectile, table.ById(4).Delivery);
        Assert.Equal(33, table.ById(4).ProjectileFlightTicks);

        // Seventeen of them walk, which is what an offering is drawn out of,
        // and twenty-seven stand -- nine tower lines of three rungs each. The
        // walker count is what lets the ruleset ask for three ordinary options
        // a round, and twelve more of them is the loosest it has ever been:
        // seventeen walkers against three options puts most of the roster off
        // any one menu.
        //
        // The twenty-three rows the nine lines added moved only the second of
        // those numbers, which is the point of a tier being a row: an offering
        // is drawn from the walkers alone, so a new tower does not enter a menu
        // and cannot move a draw. The twelve creep rows move the first, and
        // that is the same rule read from the other side.
        //
        // OBSERVED: change the skeleton's role from moving to placed in
        // content/units.txt. The walker count goes red, 17 against 16, and the
        // offering's own refusal follows it in BuildPhaseTests.
        Assert.Equal(17, table.Types.Count(row => row.Role == UnitRole.Moving));
        Assert.Equal(27, table.Types.Count(row => row.Role == UnitRole.Placed));

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
    public void The_committed_ladder_prices_the_top_of_every_line_in_tokens_and_nothing_else()
    {
        // Nine lines, nine capstones, nine capstone edges -- and no other edge
        // priced in tokens. The lines are read out of the roster.md
        // transcription above rather than out of the ladder, for the reason
        // TheNineLines exists: reading them off content/upgrades.txt would be
        // checking that file against itself.
        //
        // This IS a census and the fault check above deliberately is not, and
        // the difference is which way the file is expected to grow. A tenth
        // tower line is a tenth entry in TheNineLines, which docs/roster.md's
        // index already has to gain; a token price appearing on a middle rung is
        // a capstone somebody spelled onto the wrong row, and nothing else in
        // this repository would say so.
        //
        // OBSERVED: change one `capstone` row of content/upgrades.txt back to
        // `upgrade`. This goes red naming the pair, and that line's top starts
        // charging its cost column -- 41 gold for a Shield Wall, out of the
        // purse, in any round that can afford it.
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UpgradeLadder ladder = UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

        var tops = new List<(int From, int To)>();

        foreach ((string name, AttackType attack, int[] rungs) in TheNineLines)
        {
            Assert.True(
                ladder.IsCapstoneEdge(rungs[1], rungs[2]),
                "The " + name + " line's top rung is not priced in capstone tokens.");
            Assert.False(
                ladder.IsCapstoneEdge(rungs[0], rungs[1]),
                "The " + name + " line's second rung is priced in capstone tokens.");

            tops.Add((rungs[1], rungs[2]));
        }

        Assert.Equal(
            tops.OrderBy(top => top.From).ToArray(),
            ladder.Edges
                .Where(edge => edge.Price == EdgePrice.CapstoneToken)
                .Select(edge => (edge.From, edge.To))
                .OrderBy(top => top.From)
                .ToArray());
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
            int effective = EffectiveHealth(creep);

            Assert.True(
                Math.Abs(effective - (creep.Cost * EffectiveHealthPerGold)) * 10 <= effective,
                creep.Label
                + " costs "
                + creep.Cost
                + " gold, which buys "
                + (creep.Cost * EffectiveHealthPerGold)
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

        // The two speeds nothing is allowed to move. The Scout is exactly twice
        // the Minion, so the two are level for exactly one tick as one passes
        // the other -- which is the case the target-selection tiebreak exists
        // for, and the only case that consults it. Every creep row added since
        // is authored around this pair rather than over it.
        Assert.Equal(28, table.ById(1).SpeedMilliHexPerTick);
        Assert.Equal(56, table.ById(2).SpeedMilliHexPerTick);

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
        // What that exposed is the Mage line, and it is excluded below rather
        // than tuned away. The Mage is priced for a splash the
        // bodies term cannot count, and the Sorcerer and Unravel are that price
        // scaled by the cooldown they changed -- so the three of them are the
        // one line the rule does not reach, and every other placed row is
        // priced on the shots it actually fires.
        //
        // OBSERVED: halve the Archer's cost, 40 to 20, in content/units.txt.
        // This goes red naming it -- "archer costs 20 gold, which is 5 damage a
        // second per gold against the 40 it actually deals" -- which is what a
        // tower being twice the deal of everything else looks like before
        // anybody plays a round of it.
        UnitTypeTable table = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        foreach (UnitType tower in table.Types
                     .Where(row => row.Role == UnitRole.Placed && !TheMageLine.Contains(row.Id)))
        {
            int perSecondTimesBodies = DamageASecondTimesBodies(tower);

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
            int perSecondTimesBodies = DamageASecondTimesBodies(tower);

            Assert.True(
                Math.Abs(perSecondTimesBodies - (tower.Cost * 5)) * 50 <= perSecondTimesBodies,
                tower.Label + " prices at " + (perSecondTimesBodies / 5) + " against the " + tower.Cost + " it carries.");
        }
    }

    /// <summary>
    /// The top of the cost rule for one placed row: the middle of its damage
    /// roll, times the bodies one shot hits, over the ticks between its shots,
    /// at thirty ticks a second. The gold the rule asks for is this over five.
    /// </summary>
    /// <remarks>
    /// One integer expression, so no division rounds before a comparison, and
    /// one copy of it, so the rule and the two tests that single rows out of it
    /// cannot drift apart.
    /// </remarks>
    private static int DamageASecondTimesBodies(UnitType tower) =>
        (tower.DamageMin + tower.DamageMax) / 2 * tower.Targets * Match.TicksPerSecond / tower.CooldownTicks;

    /// <summary>
    /// The pool a walking row stands on against the damage matrix: its health
    /// times its armour multiplier. It is what the creep price is derived from.
    /// </summary>
    /// <remarks>
    /// <b>A shield is deliberately not in it.</b> A shield is raw -- armour and
    /// the type chart do not touch it -- and the cost rule has no term for one,
    /// so the two rows carrying a pool are priced as though they did not. One
    /// expression, so the rule and the note about what it cannot see are
    /// reading the same number.
    /// </remarks>
    private static int EffectiveHealth(UnitType creep) =>
        creep.MaxHp * (100 + creep.Armour) / 100;

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
        UnitTypeTable three = UnitTypeTable.Parse(ThreeRadialRows);
        UnitTypeTable four = UnitTypeTable.Parse(ThreeSuccessorRows);
        UnitTypeTable five = UnitTypeTable.Parse(ThreeRaisingRows);
        UnitTypeTable six = UnitTypeTable.Parse(ThreeCurrentRows);

        Assert.Equal(UnitTypeTable.DefaultLayout, one.Layout);
        Assert.Equal(2, two.Layout);
        Assert.Equal(3, three.Layout);
        Assert.Equal(4, four.Layout);
        Assert.Equal(5, five.Layout);
        Assert.Equal(UnitTypeTable.CurrentLayout, six.Layout);

        Assert.Equal(one.ById(1).MaxHp, two.ById(1).MaxHp);
        Assert.Equal(two.ById(1).MaxHp, three.ById(1).MaxHp);
        Assert.Equal(three.ById(1).MaxHp, four.ById(1).MaxHp);
        Assert.Equal(four.ById(1).MaxHp, five.ById(1).MaxHp);
        Assert.Equal(five.ById(1).MaxHp, six.ById(1).MaxHp);
        Assert.Equal(two.ById(7).Cost, three.ById(7).Cost);
        Assert.Equal(three.ById(7).Cost, four.ById(7).Cost);
        Assert.Equal(four.ById(7).Cost, five.ById(7).Cost);
        Assert.Equal(five.ById(7).Cost, six.ById(7).Cost);
        Assert.Equal(five.ById(7).AttackType, six.ById(7).AttackType);

        var hashes = new[]
        {
            one.ContentHash,
            two.ContentHash,
            three.ContentHash,
            four.ContentHash,
            five.ContentHash,
            six.ContentHash,
        };

        Assert.Equal(hashes.Length, hashes.Distinct().Count());
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

        // And the one column layout 4 added. A row that names the row it becomes
        // is a row that plays differently from the day it was authored, so a
        // record made against the roster before it has to be retired.
        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("armoured 0 0 1 none none none 0 none 0 0 none none 0 0", "armoured 0 0 1 none none none 0 none 0 0 2 none 0 0")).ContentHash);

        // And the two layout 5 added, one at a time. The period moves the hash
        // on its own, so a spawner retuned from one raise a second to one every
        // five retires the records made under the faster one.
        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("armoured 0 0 1 none none none 0 none 0 0 none none 0 0", "armoured 0 0 1 none none none 0 none 0 0 none 2 30 0")).ContentHash);

        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("armoured 0 0 1 none none none 0 none 0 0 none none 0 0", "armoured 0 0 1 none none none 0 none 0 0 none 2 30 0")).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("armoured 0 0 1 none none none 0 none 0 0 none none 0 0", "armoured 0 0 1 none none none 0 none 0 0 none 2 31 0")).ContentHash);

        // And the one layout 6 added. A row that pays for being killed is a row
        // that funds a defense from the day it was authored, so a record made
        // against the roster before it has to be retired.
        Assert.NotEqual(
            UnitTypeTable.Parse(ThreeCurrentRows).ContentHash,
            UnitTypeTable.Parse(ThreeCurrentRows.Replace("armoured 0 0 1 none none none 0 none 0 0 none none 0 0", "armoured 0 0 1 none none none 0 none 0 0 none none 0 2")).ContentHash);
    }

    [Fact]
    public void A_row_with_layout_fives_columns_under_a_layout_six_declaration_refuses_by_name()
    {
        // Layout 6's own version of it, and the one column is the easiest of
        // all of them to leave off: a table that gained a single column looks
        // finished the moment the layout row is edited.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeRaisingRows.Replace("layout 5", "layout 6")));

        Assert.Contains("column layout 6 has 32", thrown.Message, StringComparison.Ordinal);

        ContentException over = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 6", "layout 5")));

        Assert.Contains("column layout 5 has 31", over.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_layout_this_reader_has_no_branch_for_refuses_by_name()
    {
        // Spelled out one layout at a time, so a layout that was skipped or a
        // branch somebody deleted arrives here rather than passing an
        // inequality and falling through a switch.
        ContentException thrown = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse(ThreeCurrentRows.Replace("layout 6", "layout 7")));

        Assert.Contains("declares column layout 7", thrown.Message, StringComparison.Ordinal);
        Assert.False(UnitTypeTable.IsKnownLayout(0));
        Assert.False(UnitTypeTable.IsKnownLayout(7));
        Assert.True(UnitTypeTable.IsKnownLayout(1));
        Assert.True(UnitTypeTable.IsKnownLayout(2));
        Assert.True(UnitTypeTable.IsKnownLayout(3));
        Assert.True(UnitTypeTable.IsKnownLayout(4));
        Assert.True(UnitTypeTable.IsKnownLayout(5));
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
