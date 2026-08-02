using System.Globalization;

namespace Sim
{
    /// <summary>Which half of the loop a unit type plays. A filter, not a class.</summary>
    /// <remarks>
    /// Towers and creeps are one kind of thing here. A placed unit that moves
    /// and a moving unit that shoots are both reachable without a new type,
    /// which is the reason the id space is shared.
    /// </remarks>
    public enum UnitRole
    {
        /// <summary>Stands where it was put. What everyone else calls a tower.</summary>
        Placed = 0,

        /// <summary>Walks the corridor. What everyone else calls a creep.</summary>
        Moving = 1,
    }

    /// <summary>How a unit's damage reaches its target.</summary>
    public enum Delivery
    {
        /// <summary>It does not. Anything that never attacks.</summary>
        None = 0,

        /// <summary>Damage lands on the tick it is fired, with no entity in between.</summary>
        Hitscan = 1,

        /// <summary>A countdown and a target reference; damage lands when the countdown ends.</summary>
        Projectile = 2,
    }

    /// <summary>
    /// One row of the unit type table: a stable numeric id and the integers
    /// that describe it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="Id"/> is the identity and <see cref="Label"/> is not.</b>
    /// The id is assigned once, is never reused, and is never an index into
    /// anything -- so a record that pins type 2 can never come back years later
    /// and resolve to whatever moved into slot 2. The label exists for humans
    /// reading the data file and for messages; nothing in the simulation
    /// branches on it, and renaming one does not move the content hash.
    /// </para>
    /// <para>
    /// Every number here is an integer in an explicitly named unit --
    /// milli-hexes, ticks -- because "0.3 hexes per second" has no
    /// representation the simulation can hold and no parser here would accept
    /// the text of one.
    /// </para>
    /// </remarks>
    public sealed class UnitType
    {
        internal UnitType(
            int id,
            string label,
            UnitRole role,
            int maxHp,
            int speedMilliHexPerTick,
            int rangeMilliHex,
            int cooldownTicks,
            int windupTicks,
            int backswingTicks,
            int damageMin,
            int damageMax,
            Delivery delivery,
            int projectileFlightTicks,
            int dyingTicks)
        {
            Id = id;
            Label = label;
            Role = role;
            MaxHp = maxHp;
            SpeedMilliHexPerTick = speedMilliHexPerTick;
            RangeMilliHex = rangeMilliHex;
            CooldownTicks = cooldownTicks;
            WindupTicks = windupTicks;
            BackswingTicks = backswingTicks;
            DamageMin = damageMin;
            DamageMax = damageMax;
            Delivery = delivery;
            ProjectileFlightTicks = projectileFlightTicks;
            DyingTicks = dyingTicks;
        }

        /// <summary>The stable numeric id, unique across the one global id space.</summary>
        public int Id { get; }

        /// <summary>A human-readable name. Never an identity, never branched on.</summary>
        public string Label { get; }

        /// <summary>Placed or moving.</summary>
        public UnitRole Role { get; }

        /// <summary>Health pool. Zero means the unit has none and cannot be damaged.</summary>
        public int MaxHp { get; }

        /// <summary>Thousandths of a hex travelled per tick.</summary>
        public int SpeedMilliHexPerTick { get; }

        /// <summary>Attack range, in thousandths of a hex.</summary>
        public int RangeMilliHex { get; }

        /// <summary>Ticks between the end of one attack and the start of the next.</summary>
        public int CooldownTicks { get; }

        /// <summary>Ticks between committing to an attack and the damage landing.</summary>
        public int WindupTicks { get; }

        /// <summary>Ticks of recovery after the damage lands.</summary>
        public int BackswingTicks { get; }

        /// <summary>Lowest damage roll, inclusive.</summary>
        public int DamageMin { get; }

        /// <summary>Highest damage roll, inclusive.</summary>
        public int DamageMax { get; }

        /// <summary>How the damage reaches the target.</summary>
        public Delivery Delivery { get; }

        /// <summary>Ticks a projectile spends in flight. Zero unless <see cref="Delivery"/> is a projectile.</summary>
        public int ProjectileFlightTicks { get; }

        /// <summary>Ticks spent dying, so a seek into a death shows a death.</summary>
        public int DyingTicks { get; }

        public override string ToString() =>
            Label + " (#" + Id.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>
        /// Folds this row into a hash, in field order. The order of these calls
        /// is the layout the content hash pins: moving one line is a layout
        /// change and bumps the label's version digit.
        /// </summary>
        internal Hash64 Fold(Hash64 hash) =>
            hash
                .Add(Id)
                .Add((int)Role)
                .Add(MaxHp)
                .Add(SpeedMilliHexPerTick)
                .Add(RangeMilliHex)
                .Add(CooldownTicks)
                .Add(WindupTicks)
                .Add(BackswingTicks)
                .Add(DamageMin)
                .Add(DamageMax)
                .Add((int)Delivery)
                .Add(ProjectileFlightTicks)
                .Add(DyingTicks);
    }
}
