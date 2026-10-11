using System;
using System.Collections.Generic;
using Sim;

namespace View
{
    public sealed class HexWheelWedge
    {
        private static readonly IReadOnlyList<HexWheelWedge> Nothing = Array.Empty<HexWheelWedge>();

        public HexWheelWedge(UnitType type, string price, bool refused, IReadOnlyList<HexWheelWedge> outward, bool sent = false)
        {
            Type = type ?? throw new ArgumentNullException(nameof(type));
            Word = RosterNames.Of(type);
            Price = price ?? string.Empty;
            Refused = refused;
            Sent = sent;
            Outward = outward ?? Nothing;
        }

        private HexWheelWedge(string word)
        {
            Word = word;
            Price = string.Empty;
            Undesigned = true;
            Outward = Nothing;
        }

        public UnitType Type { get; }

        public string Word { get; }

        public string Price { get; }

        public bool Refused { get; }

        public bool Undesigned { get; }

        public bool Sent { get; }

        public IReadOnlyList<HexWheelWedge> Outward { get; }

        public static HexWheelWedge Unfilled(string word) => new HexWheelWedge(word);
    }

    public static class HexWheelChoices
    {
        public const string CapstonePlace = "capstone";

        public static IReadOnlyList<HexWheelWedge> EveryRoot(ComposedRound round)
        {
            var wedges = new List<HexWheelWedge>();

            foreach (UnitType root in Required(round).Palette)
            {
                wedges.Add(new HexWheelWedge(root, RosterNames.Gold(round.PriceOf(root)), !round.CanAfford(root), null));
            }

            return wedges;
        }

        public static IReadOnlyList<HexWheelWedge> AffordableRoots(ComposedRound round)
        {
            var wedges = new List<HexWheelWedge>();

            foreach (UnitType root in Required(round).Palette)
            {
                if (round.CanAfford(root))
                {
                    wedges.Add(new HexWheelWedge(root, RosterNames.Gold(round.PriceOf(root)), false, null));
                }
            }

            return wedges;
        }

        public static IReadOnlyList<HexWheelWedge> RootsWithLadders(ComposedRound round, UpgradeLadder ladder, UnitTypeTable types)
        {
            if (ladder is null) throw new ArgumentNullException(nameof(ladder));
            if (types is null) throw new ArgumentNullException(nameof(types));

            var wedges = new List<HexWheelWedge>();

            foreach (UnitType root in Required(round).Palette)
            {
                wedges.Add(new HexWheelWedge(
                    root,
                    RosterNames.Gold(round.PriceOf(root)),
                    !round.CanAfford(root),
                    Climb(round, ladder, types, root)));
            }

            return wedges;
        }

        public static IReadOnlyList<HexWheelWedge> Upgrades(ComposedRound round, UpgradeLadder ladder, int column, int row)
        {
            if (ladder is null) throw new ArgumentNullException(nameof(ladder));

            var wedges = new List<HexWheelWedge>();
            UnitType standing = Required(round).StandingOn(column, row);

            if (standing is null)
            {
                return wedges;
            }

            bool capstoneOffered = false;

            foreach (UnitType rung in round.UpgradesOn(column, row))
            {
                bool capstone = round.CostsACapstoneToken(column, row, rung);
                capstoneOffered |= capstone;

                wedges.Add(new HexWheelWedge(
                    rung,
                    capstone ? RosterNames.CapstoneToken() : RosterNames.Gold(round.PriceOf(rung)),
                    false,
                    null));
            }

            if (!capstoneOffered && CapstoneAbove(ladder, standing.Id))
            {
                wedges.Add(HexWheelWedge.Unfilled(CapstonePlace));
            }

            return wedges;
        }

        public static IReadOnlyList<HexWheelWedge> Creeps(ComposedRound round)
        {
            var wedges = new List<HexWheelWedge>();

            for (int index = 0; index < Required(round).Slots.Count; index++)
            {
                wedges.Add(new HexWheelWedge(round.CreepIn(index), RosterNames.Count(round.Slots[index].Count), false, null, sent: true));
            }

            foreach (UnitType creep in round.Sendable(round.Slots.Count))
            {
                wedges.Add(new HexWheelWedge(creep, RosterNames.Gold(round.PriceOf(creep)), false, null));
            }

            return wedges;
        }

        private static IReadOnlyList<HexWheelWedge> Climb(
            ComposedRound round, UpgradeLadder ladder, UnitTypeTable types, UnitType root)
        {
            var rungs = new List<HexWheelWedge>();
            var seen = new HashSet<int> { root.Id };
            int from = root.Id;

            while (NextEdge(ladder, from, out UpgradeEdge edge) && seen.Add(edge.To))
            {
                UnitType rung = types.ById(edge.To);

                rungs.Add(new HexWheelWedge(
                    rung,
                    ladder.IsCapstoneEdge(edge.From, edge.To)
                        ? RosterNames.CapstoneToken()
                        : RosterNames.Gold(round.PriceOf(rung)),
                    false,
                    null));

                from = edge.To;
            }

            return rungs;
        }

        private static bool CapstoneAbove(UpgradeLadder ladder, int typeId)
        {
            var seen = new HashSet<int> { typeId };
            var frontier = new Queue<int>();
            frontier.Enqueue(typeId);

            while (frontier.Count > 0)
            {
                int from = frontier.Dequeue();

                foreach (UpgradeEdge edge in ladder.Edges)
                {
                    if (edge.From != from)
                    {
                        continue;
                    }

                    if (ladder.IsCapstoneEdge(edge.From, edge.To))
                    {
                        return true;
                    }

                    if (seen.Add(edge.To))
                    {
                        frontier.Enqueue(edge.To);
                    }
                }
            }

            return false;
        }

        private static bool NextEdge(UpgradeLadder ladder, int from, out UpgradeEdge found)
        {
            foreach (UpgradeEdge edge in ladder.Edges)
            {
                if (edge.From == from)
                {
                    found = edge;

                    return true;
                }
            }

            found = default;

            return false;
        }

        private static ComposedRound Required(ComposedRound round) =>
            round ?? throw new ArgumentNullException(nameof(round));
    }
}
