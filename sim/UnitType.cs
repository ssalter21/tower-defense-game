using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// Which half of the loop a unit type plays. A filter, not a class -- towers
    /// and creeps share one id space.
    /// </summary>
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
    /// One row of the unit type table: a stable numeric id and the integers that
    /// describe it. Every number is an integer in a named unit -- milli-hexes,
    /// ticks. The id is assigned once, never reused, and never an index;
    /// <see cref="Label"/> carries no identity and nothing branches on it.
    /// </summary>
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
            int dyingTicks,
            int cost,
            AttackType attackType,
            ArmourType armourType,
            int armour)
        {
            Cost = cost;
            AttackType = attackType;
            ArmourType = armourType;
            Armour = armour;
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

        public int Id { get; }

        public string Label { get; }

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

        public Delivery Delivery { get; }

        /// <summary>Ticks in flight. Zero unless <see cref="Delivery"/> is a projectile.</summary>
        public int ProjectileFlightTicks { get; }

        /// <summary>Ticks spent in the dying state before the unit is cleared away.</summary>
        public int DyingTicks { get; }

        /// <summary>What one of these costs, in sauce. Zero in column layout 1, which has no cost column.</summary>
        public int Cost { get; }

        /// <summary>
        /// Which row of the damage matrix this unit's shots are resolved
        /// through. <see cref="Sim.AttackType.None"/> for a unit that never
        /// attacks, and for every row of column layout 1.
        /// </summary>
        public AttackType AttackType { get; }

        /// <summary>
        /// Which column of the damage matrix shots at this unit are resolved
        /// through. <see cref="Sim.ArmourType.None"/> for a unit with no health
        /// pool, and for every row of column layout 1.
        /// </summary>
        public ArmourType ArmourType { get; }

        /// <summary>
        /// Armour, in percent of base effective health added per point. Zero
        /// where there is no armour type to apply it through.
        /// </summary>
        public int Armour { get; }

        public override string ToString() =>
            Label + " (#" + Id.ToString(CultureInfo.InvariantCulture) + ")";

        /// <summary>
        /// Folds this row into a hash in the field order of that column layout.
        /// The order of these calls is the layout the content hash pins, so
        /// moving a line is a new layout with a label of its own.
        /// </summary>
        internal Hash64 Fold(Hash64 hash, int layout)
        {
            hash = hash
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

            switch (layout)
            {
                case 1:
                    return hash;

                case 2:
                    return hash
                        .Add(Cost)
                        .Add((int)AttackType)
                        .Add((int)ArmourType)
                        .Add(Armour);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layout),
                        "Column layout "
                        + layout.ToString(CultureInfo.InvariantCulture)
                        + " has no fold in this row.");
            }
        }
    }
}
