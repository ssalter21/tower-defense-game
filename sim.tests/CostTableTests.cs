using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The cost table: one table, keyed on purchasable thing, priced entirely out
/// of authored content.
/// </summary>
/// <remarks>
/// <para>
/// The claim under test is a shape rather than a number. A unit and a scouting
/// snapshot are two rows of one table paid out of one purse, so the second
/// non-unit line item is a row rather than a rebuild -- and the first test here
/// is the one that goes red when a kind is declared and never priced.
/// </para>
/// <para>
/// <b>Each assertion was watched failing under a deliberately wrong input</b>,
/// and the wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class CostTableTests
{
    [Fact]
    public void Every_kind_of_purchasable_thing_this_build_declares_is_priced_on_the_table()
    {
        // The mechanism, asserted. A kind is declared in PurchaseKind, named in
        // Purchase.NameOf and given a row in CostTable.From at an authored
        // price, and all of that or none -- a declared kind with no row is a
        // thing taken for free out of the only wallet there is.
        //
        // OBSERVED: add `Ghost = 2` to PurchaseKind and leave CostTable.From
        // alone. This goes red naming Ghost, which is exactly the failure a
        // line item added and never priced should produce.
        CostTable costs = TheRuleset.Costs();
        var declared = (PurchaseKind[])Enum.GetValues(typeof(PurchaseKind));

        foreach (PurchaseKind kind in declared)
        {
            Assert.True(
                costs.LineItems.Any(item => item.Kind == kind),
                "Purchase kind "
                + kind.ToString()
                + " is declared and nothing on the cost table prices one, so it would be bought for "
                + "nothing.");
        }
    }

    [Fact]
    public void A_creep_a_tower_and_a_snapshot_come_off_the_same_table_at_their_authored_prices()
    {
        // Two units on opposite sides of the board and one thing that is not a
        // unit at all, priced by one call each. That the third is on this table
        // is the whole design: a scouting snapshot competes for sauce with a
        // creep rather than living in a budget of its own.
        //
        // OBSERVED: size CostTable's arrays at types.Count and write the
        // snapshot over the last unit's row. This goes red on the mortar --
        // "Nothing on the cost table prices one unit of type 4" -- which is what
        // a line item that displaced a unit instead of joining it looks like.
        CostTable costs = TheRuleset.Costs();

        Assert.Equal(10, costs.PriceOf(Purchase.Unit(1)));
        Assert.Equal(9, costs.PriceOf(Purchase.Unit(2)));
        Assert.Equal(40, costs.PriceOf(Purchase.Unit(3)));
        Assert.Equal(90, costs.PriceOf(Purchase.Unit(4)));
        Assert.Equal(25, costs.PriceOf(Purchase.Snapshot));

        // And the snapshot's price is the ruleset's, read back rather than
        // repeated: a number typed twice is a number that can disagree with
        // itself.
        Assert.Equal(TheRuleset.Committed().SnapshotPriceSauce, costs.PriceOf(Purchase.Snapshot));
    }

    [Fact]
    public void Every_price_is_authored_content_and_none_of_them_is_a_code_constant()
    {
        // Retune the two files the prices live in and watch both halves of the
        // table move. Nothing in the simulation may hold a price of its own.
        //
        // OBSERVED: return a literal 10 from CostTable.PriceOf for a unit. The
        // first assertion goes red, 77 against 10, which is what a table that
        // had quietly acquired a default looks like.
        CostTable dearer = CostTable.From(
            TheRuleset.Committed(),
            UnitTypeTable.Parse(OneUnit(cost: 77)));

        Assert.Equal(77, dearer.PriceOf(Purchase.Unit(1)));

        CostTable retunedRules = CostTable.From(
            Ruleset.Parse(TheRuleset.Replace(TheRuleset.Minimal, "snapshot 10 25", "snapshot 10 26")),
            TheMatch.Types());

        Assert.Equal(26, retunedRules.PriceOf(Purchase.Snapshot));
    }

    [Fact]
    public void A_thing_the_table_does_not_price_is_refused_by_name()
    {
        // OBSERVED: return 0 from CostTable.PriceOf instead of throwing when the
        // walk falls off the end -- the plausible-looking default. Both
        // assertions go red having caught nothing, and every unit id nobody
        // authored becomes free.
        CostTable costs = TheRuleset.Costs();

        SimulationException unknown =
            Assert.Throws<SimulationException>(() => costs.PriceOf(Purchase.Unit(9999)));

        Assert.Contains("type 9999", unknown.Message, StringComparison.Ordinal);

        // The default value of the struct: a unit whose id is no row of any
        // table. It has to price at nothing rather than at the first row.
        SimulationException blank =
            Assert.Throws<SimulationException>(() => costs.PriceOf(default));

        Assert.Contains("Nothing on the cost table prices", blank.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_kind_and_an_id_together_are_what_a_row_is_keyed_on()
    {
        // A snapshot and unit type zero are not the same line item even though
        // both carry no meaningful id, and two units of different types are not
        // each other. This is the property that lets the table hold non-units
        // beside units at all.
        //
        // OBSERVED: compare only Id in Purchase.Equals. The first assertion goes
        // red, because Purchase.Snapshot and the unpriced default both carry
        // id 0 and the snapshot's price answers for both.
        Assert.NotEqual(Purchase.Snapshot, default);
        Assert.NotEqual(Purchase.Unit(1), Purchase.Unit(2));
        Assert.Equal(Purchase.Unit(3), Purchase.Unit(3));
        Assert.Equal(Purchase.Snapshot, Purchase.Snapshot);

        Assert.Equal(25, TheRuleset.Costs().PriceOf(Purchase.Snapshot));
        Assert.Throws<SimulationException>(() => TheRuleset.Costs().PriceOf(default));
    }

    [Fact]
    public void A_number_of_one_thing_costs_the_price_that_many_times()
    {
        // OBSERVED: return the unit price from CostTable.PriceOf(what, count),
        // ignoring the count. The first assertion goes red, 0 against 10, and
        // buying nothing costs the same as buying one.
        CostTable costs = TheRuleset.Costs();

        Assert.Equal(0, costs.PriceOf(Purchase.Unit(1), 0));
        Assert.Equal(70, costs.PriceOf(Purchase.Unit(2), 0) + costs.PriceOf(Purchase.Unit(1), 7));
        Assert.Equal(75, costs.PriceOf(Purchase.Snapshot, 3));
    }

    [Fact]
    public void A_negative_count_is_refused_because_nothing_here_sells_anything_back()
    {
        // OBSERVED: drop the count guard in CostTable.PriceOf. This goes red
        // having caught nothing, and four grunts unbought come back as a bill of
        // -40 -- which a purse would take as being paid to spend.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => TheRuleset.Costs().PriceOf(Purchase.Unit(1), -4));

        Assert.Contains("negative count is a sale", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bill_that_will_not_fit_in_a_purse_is_a_throw_and_not_a_wrap()
    {
        // The one place the table's arithmetic can leave its range. A wrapped
        // product is a negative bill, and a negative bill pays the buyer.
        //
        // OBSERVED: compute the total as an unchecked `PriceOf(what) * count` in
        // ints. The throw stops happening and the call returns 410065408 where
        // the bill is nine billion -- a purse with a fifth of a billion in it
        // would buy a hundred million mortars.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => TheRuleset.Costs().PriceOf(Purchase.Unit(4), 100000000));

        Assert.Contains("does not fit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_table_carries_one_row_for_every_unit_type_plus_the_line_items_that_are_not_units()
    {
        // The count, so that a line item quietly overwriting a unit's row would
        // be visible here rather than as a missing price somewhere later.
        //
        // OBSERVED: size the arrays at types.Count and write the snapshot into
        // the last one. This goes red on the count, and the mortar's price is
        // gone -- three other tests in this file go red naming a snapshot "of
        // type 1", which is the shape of a table one row too small.
        UnitTypeTable types = TheMatch.Types();
        CostTable costs = CostTable.From(TheRuleset.Committed(), types);

        Assert.Equal(types.Count + 1, costs.LineItems.Count);

        for (int index = 0; index < types.Count; index++)
        {
            Assert.Equal(Purchase.Unit(types.Types[index].Id), costs.LineItems[index]);
            Assert.Equal(
                types.Types[index].Cost,
                costs.PriceOf(Purchase.Unit(types.Types[index].Id)));
        }
    }

    [Fact]
    public void A_purchase_says_what_it_is_in_a_sentence_a_refusal_can_carry()
    {
        // OBSERVED: swap the two names in Purchase.NameOf. This goes red, and
        // every refusal in the economy starts naming the wrong thing -- which
        // is worse than an unhelpful message, because it sends the reader to
        // the wrong file.
        Assert.Equal("one unit of type 7", Purchase.Unit(7).ToString());
        Assert.Equal("one scouting snapshot", Purchase.Snapshot.ToString());
    }

    [Fact]
    public void A_kind_this_build_does_not_declare_is_refused_by_name()
    {
        // The default branch, reached the only way a test can reach it: with a
        // value that is not one of the declared kinds. This is the shape of what
        // a kind added to the enum and named nowhere would hit.
        //
        // OBSERVED: return a placeholder from the default branch instead of
        // throwing. This goes red having caught nothing, and a line item nobody
        // named prints as though it had been.
        SimulationException thrown =
            Assert.Throws<SimulationException>(() => Purchase.NameOf((PurchaseKind)9));

        Assert.Contains("Purchase kind 9", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A one-row unit table at the current layout, priced to order.</summary>
    private static string OneUnit(int cost) =>
        "layout 2\nunit 1 grunt moving 2000 85 0 0 0 0 0 0 none 0 12 "
        + cost.ToString(CultureInfo.InvariantCulture)
        + " none armoured 0";
}
