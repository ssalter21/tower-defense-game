using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What kind of thing a coin buys. One purse pays for every one of them, so
    /// this is what the cost table is keyed on -- a scouting snapshot is not a
    /// unit and comes out of the same sauce.
    /// </summary>
    /// <remarks>
    /// A kind is declared here, named in <see cref="Purchase.NameOf"/> and given
    /// a row in <see cref="CostTable"/>, and all three or none. A kind with no
    /// row is a thing bought for nothing.
    /// </remarks>
    public enum PurchaseKind
    {
        /// <summary>One unit of a type, priced by the cost column of its row.</summary>
        Unit = 0,

        /// <summary>One scouting snapshot of an incoming wave, priced by the ruleset.</summary>
        Snapshot = 1,
    }

    /// <summary>
    /// One purchasable thing: a kind, and which one of that kind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the cost table's key, and it is deliberately not a unit type
    /// id.</b> A unit carries the id of its row in the unit table; a kind that
    /// has exactly one line item in it carries <see cref="NoId"/>. Adding the
    /// third line item is a member of <see cref="PurchaseKind"/>, a row appended
    /// in <see cref="CostTable.From"/> and an authored price -- and never a
    /// second table, because a second table is how a second wallet gets built.
    /// </para>
    /// <para>
    /// A <c>default</c> value is a unit whose id is <see cref="NoId"/>, which is
    /// no row of any unit table -- so it prices at nothing anywhere rather than
    /// resolving to something plausible.
    /// </para>
    /// </remarks>
    public readonly struct Purchase : IEquatable<Purchase>
    {
        /// <summary>What a kind with exactly one line item in it carries.</summary>
        public const int NoId = 0;

        private Purchase(PurchaseKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        /// <summary>Which kind of thing this is.</summary>
        public PurchaseKind Kind { get; }

        /// <summary>Which one of that kind, or <see cref="NoId"/> where there is only one.</summary>
        public int Id { get; }

        /// <summary>One unit of the type carrying this id.</summary>
        public static Purchase Unit(int unitTypeId) => new Purchase(PurchaseKind.Unit, unitTypeId);

        /// <summary>One scouting snapshot. There is one kind of snapshot, so it carries no id.</summary>
        public static Purchase Snapshot => new Purchase(PurchaseKind.Snapshot, NoId);

        public static bool operator ==(Purchase a, Purchase b) => a.Equals(b);

        public static bool operator !=(Purchase a, Purchase b) => !a.Equals(b);

        public bool Equals(Purchase other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object? obj) => obj is Purchase other && Equals(other);

        public override int GetHashCode() => ((int)Kind << 24) ^ Id;

        /// <summary>What a kind is called, in a message.</summary>
        public static string NameOf(PurchaseKind kind)
        {
            switch (kind)
            {
                case PurchaseKind.Unit:
                    return "one unit";

                case PurchaseKind.Snapshot:
                    return "one scouting snapshot";

                default:
                    throw new SimulationException(
                        "Purchase kind "
                        + ((int)kind).ToString(CultureInfo.InvariantCulture)
                        + " is not one this build declares. A kind is declared, named here and priced on "
                        + "the cost table, and all three or none.");
            }
        }

        /// <summary>How this reads in a refusal. Only a kind with more than one line item carries an id.</summary>
        public override string ToString() =>
            Id == NoId
                ? NameOf(Kind)
                : NameOf(Kind) + " of type " + Id.ToString(CultureInfo.InvariantCulture);
    }
}
