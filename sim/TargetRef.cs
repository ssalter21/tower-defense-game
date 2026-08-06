using System;
using System.Globalization;

namespace Sim
{
    public enum TargetKind
    {
        /// <summary>Nothing. A reference that has been let go of.</summary>
        None = 0,

        Creep = 1,

        Tower = 2,
    }

    /// <summary>
    /// What a projectile is aimed at: a kind and an id, and no position of any
    /// sort. The tower arm exists because towers and creeps share one id space.
    /// See <c>docs/adr/0016-target-references-carry-no-position.md</c>.
    /// </summary>
    public readonly struct TargetRef : IEquatable<TargetRef>
    {
        private TargetRef(TargetKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        public static TargetRef None => new TargetRef(TargetKind.None, 0);

        public TargetKind Kind { get; }

        /// <summary>The entity id, or zero when the kind is <see cref="TargetKind.None"/>.</summary>
        public int Id { get; }

        public static TargetRef Creep(int id) => Entity(TargetKind.Creep, id);

        public static TargetRef Tower(int id) => Entity(TargetKind.Tower, id);

        public static bool operator ==(TargetRef a, TargetRef b) => a.Kind == b.Kind && a.Id == b.Id;

        public static bool operator !=(TargetRef a, TargetRef b) => a.Kind != b.Kind || a.Id != b.Id;

        public bool Equals(TargetRef other) => Kind == other.Kind && Id == other.Id;

        public override bool Equals(object? obj) => obj is TargetRef other && Equals(other);

        public override int GetHashCode() => ((int)Kind << 24) ^ Id;

        public override string ToString() =>
            Kind == TargetKind.None
                ? "nothing"
                : Kind.ToString() + " #" + Id.ToString(CultureInfo.InvariantCulture);

        private static TargetRef Entity(TargetKind kind, int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(id),
                    "Entity ids start at one; zero is how a reference says it points at nothing, so it "
                    + "cannot also be an entity.");
            }

            return new TargetRef(kind, id);
        }
    }
}
