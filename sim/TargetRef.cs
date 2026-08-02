using System;
using System.Globalization;

namespace Sim
{
    /// <summary>What kind of thing a target reference names.</summary>
    public enum TargetKind
    {
        /// <summary>Nothing. A reference that has been let go of.</summary>
        None = 0,

        /// <summary>A unit walking the corridor.</summary>
        Creep = 1,

        /// <summary>A unit standing where it was put.</summary>
        Tower = 2,
    }

    /// <summary>
    /// What a projectile is aimed at: a kind and an id, and no position of any
    /// sort.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The absence is the design.</b> A projectile that stored where it was
    /// going would need that point kept in step with a target that moves, which
    /// is either homing logic in the simulation or a projectile that flies at
    /// where its target used to be. Storing a reference instead makes homing
    /// free: the simulation only counts down, and the view interpolates toward
    /// wherever the target is in the snapshot it is drawing right now.
    /// </para>
    /// <para>
    /// It also keeps free 2D out permanently. There is no field here that could
    /// hold a point, so nobody can add one without changing this type, and
    /// changing this type is a change to the record format's shape.
    /// </para>
    /// <para>
    /// The union has a tower arm because the id space is shared -- towers and
    /// creeps are one kind of thing in this project, and a placed unit that
    /// shoots at another placed unit needs no new machinery. The skeleton never
    /// produces one, and that is fine: an arm nothing takes is cheaper than a
    /// migration.
    /// </para>
    /// </remarks>
    public readonly struct TargetRef : IEquatable<TargetRef>
    {
        private TargetRef(TargetKind kind, int id)
        {
            Kind = kind;
            Id = id;
        }

        /// <summary>A reference to nothing.</summary>
        public static TargetRef None => new TargetRef(TargetKind.None, 0);

        /// <summary>What kind of thing is being pointed at.</summary>
        public TargetKind Kind { get; }

        /// <summary>The entity id, or zero when the kind is <see cref="TargetKind.None"/>.</summary>
        public int Id { get; }

        /// <summary>Points at a creep.</summary>
        public static TargetRef Creep(int id) => Entity(TargetKind.Creep, id);

        /// <summary>Points at a tower.</summary>
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
