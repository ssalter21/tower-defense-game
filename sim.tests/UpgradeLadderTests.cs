namespace Sim.Tests;

/// <summary>
/// The upgrade ladder, parsed from the committed file and from text planted to
/// break each rule.
/// </summary>
/// <remarks>
/// Every parse in here is handed text or bytes, exactly as
/// <see cref="ContentTests"/> is: the test opens the file and the simulation
/// never learns it exists.
/// </remarks>
public class UpgradeLadderTests
{
    /// <summary>
    /// A roster to author ladders against: one creep, the Archer line's first
    /// two rungs, and a Mage line that forks and rejoins. Every tower is
    /// <c>placed</c>, the creep walks, and the prices ascend except where a test
    /// wants them not to.
    /// </summary>
    private const string Roster = """
        layout 2
        unit 1 minion     moving 240 34 0    0  0  0  0   0   none       0  12 10  none   armoured 0
        unit 2 archer     placed 0   0  3200 14 3  2  90  150 hitscan    0  0  40  pierce none     0
        unit 3 ranger     placed 0   0  4200 14 3  2  90  150 hitscan    0  0  40  pierce none     0
        unit 4 mage       placed 0   0  4600 54 21 15 210 340 projectile 33 0  92  magic  none     0
        unit 5 pyromancer placed 0   0  4600 54 21 15 260 400 projectile 33 0  120 magic  none     0
        unit 6 cryomancer placed 0   0  4600 54 21 15 260 400 projectile 33 0  120 magic  none     0
        unit 7 archmage   placed 0   0  5000 54 21 15 400 600 projectile 33 0  200 magic  none     0
        """;

    /// <summary>The Archer's one rung, which is the shape the committed file grows into.</summary>
    private const string OneRung = """
        layout 1
        upgrade 2 3
        """;

    /// <summary>
    /// The Mage line's fork and its rejoin: two routes from the Mage to the
    /// Archmage, each two edges long.
    /// </summary>
    private const string Diamond = """
        layout 1
        upgrade 4 5
        upgrade 4 6
        upgrade 5 7
        upgrade 6 7
        """;

    private static UnitTypeTable Types() => UnitTypeTable.Parse("planted roster", Roster);

    private static UpgradeLadder Parse(string text) => UpgradeLadder.Parse(text, Types());

    [Fact]
    public void The_committed_ladder_parses_against_the_committed_types()
    {
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));
        UpgradeLadder ladder = UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

        Assert.Equal(UpgradeLadder.CurrentLayout, ladder.Layout);

        // The count is deliberately not pinned. The ladder is expected to grow
        // one row at a time and a census would go red on every legitimate
        // authoring; what the committed pair is held to is having no faults,
        // which ContentTests asserts.
        Assert.Equal(ladder.Count, ladder.Edges.Count);
    }

    [Fact]
    public void A_ladder_with_no_edges_is_legal()
    {
        // The state content/upgrades.txt landed in, and the state a roster
        // mid-edit is in. An empty ladder is not a file nobody finished; it is a
        // roster with no tiers yet.
        UpgradeLadder ladder = Parse("layout 1");

        Assert.Equal(0, ladder.Count);
        Assert.Empty(ladder.Edges);
    }

    [Fact]
    public void An_edge_carries_its_two_ids_in_the_order_they_were_written()
    {
        UpgradeEdge edge = Assert.Single(Parse(OneRung).Edges);

        Assert.Equal(2, edge.From);
        Assert.Equal(3, edge.To);
    }

    [Fact]
    public void The_edges_come_back_in_file_order()
    {
        // File order is canonical order, and it is what the fold that carries
        // these edges into a content hash walks.
        Assert.Equal(
            new[] { "4 -> 5", "4 -> 6", "5 -> 7", "6 -> 7" },
            Parse(Diamond).Edges.Select(edge => edge.ToString()));
    }

    [Fact]
    public void The_ladder_takes_bytes_as_well_as_text_and_agrees_with_itself()
    {
        // A caller that read a file holds bytes, so the parser takes them --
        // which keeps the one decision that can differ between platforms, which
        // encoding, inside the assembly whose version owns every such decision.
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        UpgradeLadder fromText = UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);
        UpgradeLadder fromBytes = UpgradeLadder.ParseUtf8(File.ReadAllBytes(RepoLayout.UpgradesFile), types);

        Assert.Equal(fromText.Count, fromBytes.Count);
        Assert.Equal(fromText.Layout, fromBytes.Layout);
    }

    [Fact]
    public void A_row_with_more_than_two_ids_refuses_to_load()
    {
        // Fixed arity, so a row with a third field is refused rather than read
        // against shifted fields. A wider row is how a tier number or a cost
        // would arrive, and neither belongs on an edge.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 2 3 40")));

        Assert.Contains("has 4 fields where the layout has 3", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, thrown.Line);
    }

    [Fact]
    public void A_row_with_one_id_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 2")));
    }

    [Fact]
    public void A_decimal_point_in_the_ladder_refuses_to_load()
    {
        // Refused before the line is tokenised, by the same check that refuses
        // one in every other data file, so this is one defence rather than a
        // second copy of one.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 2 3.0")));

        Assert.Contains("'.'", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(2, thrown.Line);
    }

    [Fact]
    public void A_source_id_naming_no_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 99 3")));

        Assert.Contains("an upgrade's source names type id 99", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("does not define", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_target_id_naming_no_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 2 99")));

        Assert.Contains("an upgrade's target names type id 99", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_target_id_below_its_source_refuses_to_load()
    {
        // The rule that makes a cycle unstateable. There is no cycle detection
        // anywhere in this file, and this is why there does not need to be.
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 3 2")));

        Assert.Contains("has to exceed its source", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unit_that_upgrades_into_itself_refuses_to_load()
    {
        // The shortest cycle there is, refused by the same comparison rather
        // than by a clause of its own.
        Assert.Throws<ContentException>(() => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "upgrade 2 2")));
    }

    [Fact]
    public void A_repeated_edge_refuses_to_load()
    {
        // Caught as a comparison against the row above, which is the whole
        // reason the order is asserted rather than sorted.
        ContentException thrown = Assert.Throws<ContentException>(() => Parse("""
            layout 1
            upgrade 2 3
            upgrade 2 3
            """));

        Assert.Contains("a second time", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, thrown.Line);
    }

    [Fact]
    public void Rows_that_go_backwards_refuse_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => Parse("""
            layout 1
            upgrade 4 6
            upgrade 4 5
            """));

        Assert.Contains("out of canonical order", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_source_id_that_goes_backwards_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => Parse("""
            layout 1
            upgrade 5 7
            upgrade 4 6
            """));
    }

    [Fact]
    public void An_unknown_keyword_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "upgrade 2 3", "tier 2 3")));

        Assert.Contains("starts with 'tier'", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ladder_with_no_layout_row_refuses_to_load()
    {
        // Unlike the unit table, an absent layout row is not layout 1 here.
        // There is no ladder written before the row existed, so there is nothing
        // for this reader to be lenient toward and a default would be a number
        // nobody authored.
        ContentException thrown = Assert.Throws<ContentException>(() => Parse("# no rows at all\n"));

        Assert.Contains("has no 'layout' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_edge_above_the_layout_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => Parse("""
            upgrade 2 3
            layout 1
            """));

        Assert.Contains("above any 'layout' row", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(1, thrown.Line);
    }

    [Fact]
    public void A_second_layout_row_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(() => Parse("layout 1\n" + OneRung));

        Assert.Contains("second 'layout' row", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_layout_row_below_an_edge_is_refused_as_the_second_one()
    {
        // The unit table has a refusal of its own for this, because a table with
        // no layout row is layout 1 there and rows above the declaration are
        // legitimately readable. Here they are not: an edge above the first
        // layout row is already refused, so the only way to reach a layout row
        // with edges above it is to have declared one already.
        ContentException thrown = Assert.Throws<ContentException>(() => Parse(OneRung + "\nlayout 1"));

        Assert.Contains("second 'layout' row", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(3, thrown.Line);
    }

    [Fact]
    public void A_layout_this_reader_has_no_branch_for_refuses_to_load()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => Parse(PlantedText.Replace(OneRung, "layout 1", "layout 2")));

        Assert.Contains("declares row layout 2", thrown.Message, StringComparison.Ordinal);
        Assert.False(UpgradeLadder.IsKnownLayout(0));
        Assert.False(UpgradeLadder.IsKnownLayout(2));
        Assert.True(UpgradeLadder.IsKnownLayout(UpgradeLadder.CurrentLayout));
    }

    [Fact]
    public void A_diamond_whose_sides_are_equal_reports_no_fault()
    {
        // Two routes from the Mage to the Archmage, two edges each. A fork that
        // rejoins is the shape the ladder is a graph rather than a list FOR, so
        // it has to be the case that reports nothing.
        LadderReport report = Parse(Diamond).Completeness(Types());

        Assert.True(report.HasNoFaults);
        Assert.Empty(report.Faults);
    }

    [Fact]
    public void A_diamond_with_a_shortcut_across_it_reports_unequal_roads()
    {
        // The same diamond plus a direct Mage-to-Archmage edge: one route costs
        // one upgrade and the other costs two, so a player who took the long way
        // paid more for the same tower.
        //
        // OBSERVED: compare only the shortest paths. This goes red having caught
        // nothing, and a ladder where one branch is strictly a worse deal than
        // another passes the build gate.
        LadderReport report = Parse("""
            layout 1
            upgrade 4 5
            upgrade 4 6
            upgrade 4 7
            upgrade 5 7
            upgrade 6 7
            """).Completeness(Types());

        LadderFinding fault = Assert.Single(report.Faults);

        Assert.Equal(LadderRemark.UnequalRoads, fault.Remark);
        Assert.Equal(4, fault.Subject);
        Assert.Equal(7, fault.Other);
        Assert.Contains("paths of 1 upgrade and 2 upgrades", fault.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void An_edge_whose_ends_have_different_roles_reports_mixed_roles()
    {
        // A tower that upgrades into a creep is not a tier, and the parser lets
        // it load on purpose -- the edge joins two unit ids. This is where it is
        // caught.
        LadderReport report = Parse("layout 1\nupgrade 1 2").Completeness(Types());

        LadderFinding fault = Assert.Single(report.Faults);

        Assert.Equal(LadderRemark.MixedRoles, fault.Remark);
        Assert.Equal(1, fault.Subject);
        Assert.Equal(2, fault.Other);
        Assert.Contains("walks", fault.Sentence, StringComparison.Ordinal);
        Assert.Contains("stands", fault.Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_ladder_reports_the_roots_and_the_leaves_it_has()
    {
        // The Mage is the root of its line and the Archmage is its leaf; the two
        // middle rungs are neither. Every other row of the roster is in no edge
        // at all and is therefore not mentioned -- an empty ladder saying every
        // unit is both a root and a leaf would be noise standing in for a
        // reading.
        LadderReport report = Parse(Diamond).Completeness(Types());

        Assert.Equal(new[] { 4 }, Subjects(report, LadderRemark.Root));
        Assert.Equal(new[] { 7 }, Subjects(report, LadderRemark.Leaf));
    }

    [Fact]
    public void An_empty_ladder_says_nothing_at_all_about_a_roster()
    {
        LadderReport report = Parse("layout 1").Completeness(Types());

        Assert.True(report.HasNoFaults);
        Assert.Empty(report.Notes);
    }

    [Fact]
    public void An_upgrade_that_costs_no_more_than_its_source_is_a_note_and_never_a_fault()
    {
        // The Ranger's own case: one extra hex of range at the Archer's price,
        // because the cost rule does not price range. It is printed and it is
        // not judged, which is the whole difference between the two lists.
        LadderReport report = Parse(OneRung).Completeness(Types());

        Assert.True(report.HasNoFaults);
        Assert.Equal(new[] { 2 }, Subjects(report, LadderRemark.FlatOrFallingPrice));
        Assert.Contains("40 gold", Assert.Single(report.Notes, note =>
            note.Remark == LadderRemark.FlatOrFallingPrice).Sentence, StringComparison.Ordinal);
    }

    [Fact]
    public void A_rising_price_is_not_remarked_on()
    {
        LadderReport report = Parse("layout 1\nupgrade 4 5").Completeness(Types());

        Assert.Empty(Subjects(report, LadderRemark.FlatOrFallingPrice));
    }

    [Fact]
    public void A_long_line_with_one_route_reports_no_fault_however_many_rungs_it_has()
    {
        // Unequal roads is about two routes to one unit, not about length. A
        // three-rung line reaches its top by exactly one path, so the shortest
        // and the longest agree at every pair along it.
        LadderReport report = Parse("""
            layout 1
            upgrade 4 5
            upgrade 5 6
            upgrade 6 7
            """).Completeness(Types());

        Assert.True(report.HasNoFaults);
    }

    private static int[] Subjects(LadderReport report, LadderRemark remark) =>
        report.Notes.Concat(report.Faults)
            .Where(finding => finding.Remark == remark)
            .Select(finding => finding.Subject)
            .ToArray();

    [Fact]
    public void An_edge_between_two_walking_units_is_not_refused_by_the_parser()
    {
        // Deliberately legal. The edge is between two unit IDS rather than
        // between two towers, so a creep ladder stays structurally possible --
        // and whether the two ends agree on a role is a question the
        // completeness pass answers by returning a fault, not one the loader
        // answers by refusing.
        UnitTypeTable types = UnitTypeTable.Parse("planted roster", """
            layout 2
            unit 1 minion   moving 240 34 0 0 0 0 0 0 none 0 12 10 none armoured 0
            unit 2 skeleton moving 440 34 0 0 0 0 0 0 none 0 12 20 none armoured 0
            """);

        Assert.Single(UpgradeLadder.Parse("layout 1\nupgrade 1 2", types).Edges);
    }
}
