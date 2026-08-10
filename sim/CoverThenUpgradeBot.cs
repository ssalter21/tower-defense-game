using System;
using System.Collections.Generic;

namespace Sim
{
    /// <summary>
    /// The defensive half of a scripted player: what one round builds, out of
    /// half the purse, against the board the run already has standing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule, in one sentence.</b> Each round, with up to half the purse:
    /// while any route hex is unshot at, buy the tower type that covers the most
    /// of them per gold; once nothing more can be covered, upgrade the
    /// lowest-ordinal placement that is not already the dearest type. Stop at the
    /// first action the half will not pay for, and bank the rest.
    /// </para>
    /// <para>
    /// <b>Half is a constant here and not in <c>content/ruleset.txt</c>.</b>
    /// Tuning the bot is not tuning the game: every row of the balance report is
    /// played by this player, so what it does has to move with the player rather
    /// than with the rules the report is about.
    /// </para>
    /// <para>
    /// <b>What the defense does not spend banks.</b> It is not handed to the
    /// wave, which is what leaves the interest and the unspent gold anything to
    /// say -- a bot that always emptied its purse would report a run nobody
    /// could distinguish from one with no banking rule at all.
    /// </para>
    /// <para>
    /// <b>Uncovered is counted in route hexes rather than in time.</b> How long a
    /// stretch of route goes unshot at depends on how fast the creep walking it
    /// is, so a bot that counted seconds would build a differently-shaped board
    /// for every row of the report. A hex is the unit range is already answered
    /// in, and <see cref="Footing.Reaches"/> is the one range test in the
    /// simulation, so what covers what is asked here exactly as a match asks it.
    /// </para>
    /// <para>
    /// <b>Per gold rather than by price, because the report is the point.</b>
    /// Three rangers cover all forty-seven route hexes of <c>content/map.txt</c>
    /// for a hundred and twenty gold and fourteen soldiers cover them for four
    /// hundred and twenty, so a rule that started at the bottom of the price list
    /// would spend most of a run's gold on the worse of the two walls and send a
    /// wave that could not reach the far side of either. What the board costs is
    /// gold the wave does not get, and every balance column in the report is a
    /// statement about the wave.
    /// </para>
    /// <para>
    /// <b>Nothing here can buy gold's worth of nothing.</b> A place is only ever
    /// made on a cell that reaches a route hex nothing reaches yet, and an
    /// upgrade only ever names a cell a placement already stands on. A tower that
    /// reaches no part of the route is a legal decision -- see
    /// <see cref="Footing"/> -- and one bought by mistake would never show up as
    /// unspent gold, because the gold <i>was</i> spent.
    /// </para>
    /// </remarks>
    public static class CoverThenUpgradeBot
    {
        /// <summary>How many shares a purse is cut into, of which the defense takes one.</summary>
        private const int Shares = 2;

        /// <summary>What this bot may spend out of a purse: half of it, rounded down.</summary>
        public static int BudgetOf(Purse purse)
        {
            if (purse is null)
            {
                throw new ArgumentNullException(nameof(purse));
            }

            return purse.Gold / Shares;
        }

        /// <summary>
        /// What this round builds, in the order the actions are written.
        /// </summary>
        /// <remarks>
        /// The board is folded forward as the actions are decided, through
        /// <see cref="Board.Place"/> and <see cref="Board.Upgrade"/>, so what the
        /// bot believes is standing is worked out by the same type the payer
        /// walks the actions through afterwards.
        /// </remarks>
        /// <param name="run">The run as it stands before this round, board and purse included.</param>
        public static IReadOnlyList<BuildAction> Decide(Run run)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            UnitType[] byPrice = ByPrice(run.Types, run.Costs);
            bool[] covered = CoveredBy(run.Map, run.Board);
            Board board = run.Board;
            var actions = new List<BuildAction>();
            int left = BudgetOf(run.Purse);

            // Cover: the type that reaches the most route nothing reaches for
            // what it costs, until nothing on any free cell reaches any more of it.
            while (true)
            {
                (UnitType? type, int column, int row) = BestValue(run.Map, board, byPrice, run.Costs, covered);

                if (type is null)
                {
                    break;
                }

                int price = run.Costs.PriceOf(Purchase.Unit(type.Id));

                // The rule is one sequence of actions and it stops at the first
                // one the half will not pay for. What the half cannot afford is
                // banked rather than spent on a cheaper type that would still
                // cover something, so a round can end holding gold a tower it
                // did not ask about costs.
                if (price > left)
                {
                    return actions;
                }

                actions.Add(BuildAction.Of(ActionKind.Place, type.Id, column, row));
                board = board.Place(type, column, row);
                Reach(covered, run.Map, type, column, row);
                left -= price;
            }

            // Then upgrade: the placement that has stood longest becomes the
            // next type up, until every one of them is as dear as the roster goes.
            while (true)
            {
                (Placement placement, UnitType? into) = Dearer(board, byPrice, run.Costs);

                if (into is null)
                {
                    break;
                }

                int price = run.Costs.PriceOf(Purchase.Unit(into.Id));

                if (price > left)
                {
                    break;
                }

                actions.Add(BuildAction.Of(ActionKind.Upgrade, into.Id, placement.Column, placement.Row));
                board = board.Upgrade(into, placement.Column, placement.Row);
                left -= price;
            }

            return actions;
        }

        /// <summary>
        /// The type that reaches the most unreached route hexes for each gold it
        /// costs, and the free cell it reaches them from. A null type is the end
        /// of the first phase: no type on any free cell covers anything new.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two types are compared by cross-multiplying their counts and their
        /// prices, which is the same order as the two fractions in integers that
        /// never round. The score opens at nothing gained for one gold, so the
        /// comparison admits the first type with anything to gain at all.
        /// </para>
        /// <para>
        /// The whole price list is walked, and a type has to beat the score
        /// standing to take it, so a tie goes to the row the list reaches first
        /// -- cheaper, and the lower id where the price is the same. That is a
        /// total order over the types with no sort and no comparator, matching
        /// how <see cref="Best"/> settles a tie between cells.
        /// </para>
        /// </remarks>
        private static (UnitType? Type, int Column, int Row) BestValue(
            HexMap map,
            Board board,
            UnitType[] byPrice,
            CostTable costs,
            bool[] covered)
        {
            UnitType? bestType = null;
            int bestColumn = 0;
            int bestRow = 0;
            int bestGained = 0;
            int bestPrice = 1;

            for (int index = 0; index < byPrice.Length; index++)
            {
                (int column, int row, int gained) = Best(map, board, byPrice[index], covered);
                int price = costs.PriceOf(Purchase.Unit(byPrice[index].Id));

                if (gained * bestPrice > bestGained * price)
                {
                    bestType = byPrice[index];
                    bestColumn = column;
                    bestRow = row;
                    bestGained = gained;
                    bestPrice = price;
                }
            }

            return (bestType, bestColumn, bestRow);
        }

        /// <summary>
        /// The free cell this type reaches the most unreached route hexes from,
        /// and how many that is. Zero is a type with nothing left to add.
        /// </summary>
        /// <remarks>
        /// The cells are walked by row and then by column -- the canonical order
        /// a board's placements are read in -- and a cell has to beat the count
        /// standing to take it, so the first cell to reach the best score keeps
        /// it. That is a total order over the cells with no sort and no
        /// comparator, which is how ties are settled everywhere else here.
        /// </remarks>
        private static (int Column, int Row, int Gained) Best(
            HexMap map,
            Board board,
            UnitType type,
            bool[] covered)
        {
            int bestColumn = 0;
            int bestRow = 0;
            int best = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (!board.IsFree(column, row) || !Footing.Of(map, type, column, row).Possible)
                    {
                        continue;
                    }

                    int gained = Gained(map, type, column, row, covered);

                    if (gained > best)
                    {
                        best = gained;
                        bestColumn = column;
                        bestRow = row;
                    }
                }
            }

            return (bestColumn, bestRow, best);
        }

        /// <summary>
        /// The lowest-ordinal placement with a dearer type to become, and the
        /// cheapest type that costs more than the one standing on it.
        /// </summary>
        /// <remarks>
        /// A null type is a board every placement of which is already as dear as
        /// the roster goes, and it is where the bot stops: there is no second
        /// fallback, because a player with nothing left to do should read as one.
        /// The price list ascends by price and then by id, so the first row above
        /// the current price is the cheapest one, ties settled by the lower id.
        /// </remarks>
        private static (Placement Placement, UnitType? Into) Dearer(
            Board board,
            UnitType[] byPrice,
            CostTable costs)
        {
            for (int index = 0; index < board.Placements.Count; index++)
            {
                Placement placement = board.Placements[index];
                int standing = costs.PriceOf(Purchase.Unit(placement.Type.Id));

                for (int other = 0; other < byPrice.Length; other++)
                {
                    if (costs.PriceOf(Purchase.Unit(byPrice[other].Id)) > standing)
                    {
                        return (placement, byPrice[other]);
                    }
                }
            }

            return (default, null);
        }

        /// <summary>
        /// The roster's placeable rows, ascending by price and then by type id.
        /// </summary>
        /// <remarks>
        /// The ordering is an insertion by hand because the framework's sorts are
        /// unstable and banned here.
        /// </remarks>
        private static UnitType[] ByPrice(UnitTypeTable types, CostTable costs)
        {
            var ordered = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                UnitType type = types.Types[index];

                if (type.Role != UnitRole.Placed)
                {
                    continue;
                }

                int place = ordered.Count;

                ordered.Add(type);

                while (place > 0 && Ahead(costs, type, ordered[place - 1]))
                {
                    ordered[place] = ordered[place - 1];
                    place--;
                }

                ordered[place] = type;
            }

            return ordered.ToArray();
        }

        /// <summary>Whether a type is bought ahead of one already ordered: cheaper, or as cheap with a lower id.</summary>
        private static bool Ahead(CostTable costs, UnitType type, UnitType ordered)
        {
            int price = costs.PriceOf(Purchase.Unit(type.Id));
            int already = costs.PriceOf(Purchase.Unit(ordered.Id));

            return price < already || (price == already && type.Id < ordered.Id);
        }

        /// <summary>Which route hexes the board already reaches, one flag per step of the route.</summary>
        private static bool[] CoveredBy(HexMap map, Board board)
        {
            var covered = new bool[map.Route.Count];

            for (int index = 0; index < board.Placements.Count; index++)
            {
                Placement placement = board.Placements[index];

                Reach(covered, map, placement.Type, placement.Column, placement.Row);
            }

            return covered;
        }

        /// <summary>How many unreached route hexes a type standing on this cell would reach.</summary>
        private static int Gained(HexMap map, UnitType type, int column, int row, bool[] covered)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int gained = 0;

            for (int step = 0; step < map.Route.Count; step++)
            {
                if (!covered[step] && Footing.Reaches(hex, type.RangeMilliHex, map.Route[step]))
                {
                    gained++;
                }
            }

            return gained;
        }

        /// <summary>Marks every route hex a type standing on this cell reaches.</summary>
        private static void Reach(bool[] covered, HexMap map, UnitType type, int column, int row)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);

            for (int step = 0; step < map.Route.Count; step++)
            {
                covered[step] = covered[step] || Footing.Reaches(hex, type.RangeMilliHex, map.Route[step]);
            }
        }
    }
}
