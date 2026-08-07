using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What every purchasable thing costs, in sauce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Keyed on <see cref="Purchase"/> rather than on unit type.</b> A creep
    /// and a scouting snapshot are two rows of one table paid out of one purse,
    /// which is what makes a build phase a single decision over a single wallet.
    /// The second non-unit line item is a row here; it is not a second table and
    /// it is not a rebuild of this one.
    /// </para>
    /// <para>
    /// <b>Not one price in this file is a number this file chose.</b> A unit
    /// costs what the cost column of its row says; a snapshot costs what the
    /// ruleset's snapshot price says. Retuning either is an edit to a text file.
    /// </para>
    /// <para>
    /// The rows are two parallel arrays walked in order. A keyed collection is a
    /// banned type -- enumeration order is an implementation detail and it has
    /// moved between runtimes before -- and a table of this size is walked in
    /// less time than it is hashed.
    /// </para>
    /// </remarks>
    public sealed class CostTable
    {
        private readonly Purchase[] _items;

        private readonly int[] _prices;

        private CostTable(Purchase[] items, int[] prices)
        {
            _items = items;
            _prices = prices;
        }

        /// <summary>
        /// The whole table: every unit type in the table, then the line items
        /// that are not units.
        /// </summary>
        /// <param name="rules">Where the prices of the things that are not units are authored.</param>
        /// <param name="types">The unit table, whose cost column prices the things that are.</param>
        public static CostTable From(Ruleset rules, UnitTypeTable types)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var items = new Purchase[types.Count + 1];
            var prices = new int[items.Length];

            for (int index = 0; index < types.Count; index++)
            {
                UnitType type = types.Types[index];
                items[index] = Purchase.Unit(type.Id);
                prices[index] = type.Cost;
            }

            // Every line item that is not a unit goes below this line, one row
            // each, priced from the ruleset. Adding one does not disturb a
            // single row above it.
            items[types.Count] = Purchase.Snapshot;
            prices[types.Count] = rules.SnapshotPriceSauce;

            return new CostTable(items, prices);
        }

        /// <summary>Every purchasable thing this table prices, in the order it was built.</summary>
        public IReadOnlyList<Purchase> LineItems => _items;

        /// <summary>What one of these costs, in sauce.</summary>
        public int PriceOf(Purchase what)
        {
            for (int index = 0; index < _items.Length; index++)
            {
                if (_items[index].Equals(what))
                {
                    return _prices[index];
                }
            }

            throw new SimulationException(
                "Nothing on the cost table prices "
                + what.ToString()
                + ". Every purchasable thing carries a price, because cost-per-effect is what makes a "
                + "thing good or bad; a thing with no price is a thing taken for free out of the only "
                + "wallet there is.");
        }

        /// <summary>What this many of them cost, in sauce, all at once.</summary>
        public int PriceOf(Purchase what, int count)
        {
            if (count < 0)
            {
                throw new SimulationException(
                    "A purse was asked the price of "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what.ToString()
                    + ". A negative count is a sale, and nothing in this economy sells anything back.");
            }

            long total = (long)PriceOf(what) * count;

            if (total > int.MaxValue)
            {
                throw new SimulationException(
                    count.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what.ToString()
                    + " costs "
                    + total.ToString(CultureInfo.InvariantCulture)
                    + " sauce, which does not fit in the 32-bit integer a purse is kept in.");
            }

            return (int)total;
        }
    }
}
