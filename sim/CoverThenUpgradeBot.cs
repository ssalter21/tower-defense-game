using System;
using System.Collections.Generic;

namespace Sim
{
    /// <summary>
    /// The defensive half of a scripted player: what one round builds, out of
    /// half the purse, against the board that is already standing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule, in one sentence.</b> Each round, with up to half the purse:
    /// while any route hex is unshot at, buy the tower type that covers the most
    /// of them per gold; once nothing more can be covered, buy whichever scores
    /// most damage over the route per gold -- an upgrade, or a second tower on
    /// route something already watches. Stop at the first action the half will
    /// not pay for, and bank the rest.
    /// </para>
    /// <para>
    /// <b>Half is a constant here and not in <c>content/ruleset.txt</c>.</b>
    /// Tuning the bot is not tuning the game: every row of the balance report is
    /// played by this player, so what it does has to move with the player rather
    /// than with the rules the report is about.
    /// </para>
    /// <para>
    /// <b>Both walls in the report are built by this rule.</b> A run's own board
    /// is one caller and <see cref="FieldPool.Canned"/>'s stand-in is the other,
    /// so the wall a growing wave is measured against grows the way the player's
    /// does. There is no second build rule for an opponent to be strong or weak
    /// by, which is what stops "the wave outgrew the defense" meaning "the
    /// opponent was written to lose".
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
    /// Five rangers cover all fifty-one route hexes of <c>content/map.txt</c>
    /// for a hundred and twenty gold and fourteen soldiers cover them for four
    /// hundred and twenty, so a rule that started at the bottom of the price list
    /// would spend most of a run's gold on the worse of the two walls and send a
    /// wave that could not reach the far side of either. What the board costs is
    /// gold the wave does not get, and every balance column in the report is a
    /// statement about the wave.
    /// </para>
    /// <para>
    /// <b>What a covered route is worth is one score, and both halves of the
    /// second phase are read off it.</b> A row standing on a cell scores the
    /// middle of its damage roll, divided by the ticks between its shots, times
    /// the bodies one shot hits, times the route hexes it reaches from there,
    /// over the gold it costs above whatever stands on that cell -- nothing, for
    /// a cell that is empty. An upgrade and a second tower are therefore
    /// comparable numbers rather than two rules, and the higher one is bought.
    /// </para>
    /// <para>
    /// <b>The reach in that score is the reach of the row being bought</b>, so an
    /// upgrade is scored on what the new row would watch from the cell and never
    /// on what the old one watches. The two differ wherever a rung reaches
    /// further than the rung below it, which on this roster is every one of
    /// them.
    /// </para>
    /// <para>
    /// <b>The second phase opens when nothing more <i>can</i> be covered</b>,
    /// which is not the same as every route hex being shot at: a hex no legal
    /// cell reaches would otherwise hold the bot in the first phase forever and
    /// leave it unable to buy anything at all.
    /// </para>
    /// <para>
    /// <b>Nothing here can buy gold's worth of nothing.</b> Every place either
    /// reaches a route hex nothing reaches yet or scores on the route it does
    /// reach, and an upgrade only ever names a cell a placement already stands
    /// on. A tower that reaches no part of the route is a legal decision -- see
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

            return Decide(run.Map, run.Types, run.Costs, run.Ladder, run.Board, run.Purse);
        }

        /// <summary>
        /// The same rule, over a board and a purse that belong to no run.
        /// </summary>
        /// <remarks>
        /// Everything the rule reads, named one by one. A run holds all six and
        /// hands them over; the canned field pool holds a board and a purse of
        /// its own and no run at all, and a wall built by a second copy of this
        /// rule is a wall that can disagree with the one a player builds.
        /// </remarks>
        /// <param name="map">The board's map: where a cell is, and where the route runs.</param>
        /// <param name="types">The roster every placeable row is read out of.</param>
        /// <param name="costs">What every row is priced at.</param>
        /// <param name="ladder">The edges that say which rows are reached by upgrading rather than placed.</param>
        /// <param name="board">What stands before this round builds.</param>
        /// <param name="purse">What is held before this round builds, of which the wall takes a share.</param>
        public static IReadOnlyList<BuildAction> Decide(
            HexMap map,
            UnitTypeTable types,
            CostTable costs,
            UpgradeLadder ladder,
            Board board,
            Purse purse)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (costs is null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            if (ladder is null)
            {
                throw new ArgumentNullException(nameof(ladder));
            }

            if (board is null)
            {
                throw new ArgumentNullException(nameof(board));
            }

            if (purse is null)
            {
                throw new ArgumentNullException(nameof(purse));
            }

            UnitType[] byPrice = ByPrice(types, costs);
            UnitType[] placeable = Placeable(byPrice, ladder);
            bool[] covered = CoveredBy(map, board);
            var actions = new List<BuildAction>();
            int left = BudgetOf(purse);

            // Cover: the type that reaches the most route nothing reaches for
            // what it costs, until nothing on any free cell reaches any more of it.
            //
            // The rungs are left out of this half and only this half. A type
            // some edge of the ladder points at is refused to `place` -- it is
            // reached by standing the rung below it -- so a bot that chose one
            // here would compose a phase the rules refuse, however good the
            // cell was. The upgrade loop below still reaches them, which is the
            // only way anything does.
            while (true)
            {
                (UnitType? type, int column, int row) = BestValue(map, board, placeable, costs, covered);

                if (type is null)
                {
                    break;
                }

                int price = costs.PriceOf(Purchase.Unit(type.Id));

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
                Reach(covered, map, type, column, row);
                left -= price;
            }

            // Then buy by value: the highest-scoring of an upgrade and a second
            // tower on route something already watches, until nothing scores or
            // the half will not pay for what won.
            //
            // The purse is charged the full price of the row bought -- what a
            // ladder edge costs is its target's own price -- while the score
            // divides by what that row costs above the one standing on the cell.
            while (true)
            {
                (UnitType? type, ActionKind kind, int column, int row) =
                    BestBuy(map, board, placeable, byPrice, costs, ladder);

                if (type is null)
                {
                    break;
                }

                int price = costs.PriceOf(Purchase.Unit(type.Id));

                if (price > left)
                {
                    break;
                }

                actions.Add(BuildAction.Of(kind, type.Id, column, row));
                board = kind == ActionKind.Place
                    ? board.Place(type, column, row)
                    : board.Upgrade(type, column, row);
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
        /// The best-scoring thing to buy on a route nothing can cover any more
        /// of: a second tower on a free cell, or an upgrade of a placement. A
        /// null type is a board where nothing scores at all, and it is where the
        /// bot stops -- there is no second fallback, because a player with
        /// nothing left to do should read as one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two candidates are compared by cross-multiplying the two fractions,
        /// which is their order in integers that never round. The score opens at
        /// nothing for one gold, so the comparison admits the first candidate
        /// worth anything at all.
        /// </para>
        /// <para>
        /// Places are walked before upgrades, each by price and then by the
        /// canonical order of the cells or the placements, and a candidate has
        /// to beat the score standing to take it -- so a tie goes to the first
        /// of them, which is a total order over the candidates with no sort and
        /// no comparator.
        /// </para>
        /// </remarks>
        private static (UnitType? Type, ActionKind Kind, int Column, int Row) BestBuy(
            HexMap map,
            Board board,
            UnitType[] placeable,
            UnitType[] byPrice,
            CostTable costs,
            UpgradeLadder ladder)
        {
            UnitType? bestType = null;
            ActionKind bestKind = ActionKind.Place;
            int bestColumn = 0;
            int bestRow = 0;
            long bestDamage = 0;
            long bestGold = 1;

            for (int index = 0; index < placeable.Length; index++)
            {
                UnitType type = placeable[index];
                long gold = (long)type.CooldownTicks * costs.PriceOf(Purchase.Unit(type.Id));

                // A row that never shoots, or one priced at nothing, divides by
                // zero rather than scoring badly.
                if (gold <= 0)
                {
                    continue;
                }

                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        if (!board.IsFree(column, row) || !Footing.Of(map, type, column, row).Possible)
                        {
                            continue;
                        }

                        long damage = DamageOverRoute(map, type, column, row);

                        if (damage * bestGold > bestDamage * gold)
                        {
                            (bestType, bestKind, bestColumn, bestRow) =
                                (type, ActionKind.Place, column, row);
                            (bestDamage, bestGold) = (damage, gold);
                        }
                    }
                }
            }

            for (int index = 0; index < board.Placements.Count; index++)
            {
                Placement placement = board.Placements[index];
                int standing = costs.PriceOf(Purchase.Unit(placement.Type.Id));

                for (int other = 0; other < byPrice.Length; other++)
                {
                    UnitType into = byPrice[other];
                    int over = costs.PriceOf(Purchase.Unit(into.Id)) - standing;

                    if (over <= 0 || !Climbs(ladder, placement.Type, into))
                    {
                        continue;
                    }

                    long gold = (long)into.CooldownTicks * over;

                    if (gold <= 0)
                    {
                        continue;
                    }

                    long damage = DamageOverRoute(map, into, placement.Column, placement.Row);

                    if (damage * bestGold > bestDamage * gold)
                    {
                        (bestType, bestKind, bestColumn, bestRow) =
                            (into, ActionKind.Upgrade, placement.Column, placement.Row);
                        (bestDamage, bestGold) = (damage, gold);
                    }
                }
            }

            return (bestType, bestKind, bestColumn, bestRow);
        }

        /// <summary>
        /// Whether a placement of one type may be swapped for another. A row no
        /// edge points at is reached from anything standing; a row some edge
        /// points at is reached only along an edge, which is the prerequisite
        /// <see cref="BuildPhase"/> refuses an upgrade for breaking.
        /// </summary>
        private static bool Climbs(UpgradeLadder ladder, UnitType standing, UnitType into) =>
            !ladder.IsTargetOfAnEdge(into.Id) || ladder.HasEdge(standing.Id, into.Id);

        /// <summary>
        /// The damage a row standing on this cell puts on the route, over the
        /// ticks it takes to put it there: the width of its damage roll times
        /// the bodies one shot hits times the route hexes it reaches. Divided by
        /// the row's cooldown and by the gold it costs, this is the score
        /// <see cref="BestBuy"/> compares.
        /// </summary>
        /// <remarks>
        /// The width of the roll is twice its middle, and every candidate is
        /// doubled alike, so the factor cancels out of the comparison and the
        /// middle is never halved into an integer that rounds.
        /// </remarks>
        private static long DamageOverRoute(HexMap map, UnitType type, int column, int row) =>
            (long)(type.DamageMin + type.DamageMax) * type.Targets * Reaching(map, type, column, row);

        /// <summary>How many route hexes a type standing on this cell reaches, covered or not.</summary>
        private static int Reaching(HexMap map, UnitType type, int column, int row)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int reaching = 0;

            for (int step = 0; step < map.Route.Count; step++)
            {
                if (Footing.Reaches(map, hex, type.RangeMilliHex, map.Route[step]))
                {
                    reaching++;
                }
            }

            return reaching;
        }

        /// <summary>
        /// The rows of an ordered roster that may be placed outright: every one
        /// no edge of the ladder points at, in the order they came.
        /// </summary>
        /// <remarks>
        /// A separate list rather than a filter inside <see cref="ByPrice"/>,
        /// because the upgrade half wants the whole ordering: what a placement
        /// climbs into is exactly a row this leaves out.
        /// </remarks>
        private static UnitType[] Placeable(UnitType[] byPrice, UpgradeLadder ladder)
        {
            var placeable = new List<UnitType>();

            for (int index = 0; index < byPrice.Length; index++)
            {
                if (!ladder.IsTargetOfAnEdge(byPrice[index].Id))
                {
                    placeable.Add(byPrice[index]);
                }
            }

            return placeable.ToArray();
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

        /// <summary>
        /// How many unreached route hexes a type standing on this cell would
        /// reach: <see cref="Reaching"/> narrowed to the steps nothing covers yet.
        /// </summary>
        private static int Gained(HexMap map, UnitType type, int column, int row, bool[] covered)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            int gained = 0;

            for (int step = 0; step < map.Route.Count; step++)
            {
                if (!covered[step] && Footing.Reaches(map, hex, type.RangeMilliHex, map.Route[step]))
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
                covered[step] = covered[step] || Footing.Reaches(map, hex, type.RangeMilliHex, map.Route[step]);
            }
        }
    }
}
