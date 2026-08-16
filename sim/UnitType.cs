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
            int armour,
            int shield,
            int targets,
            Bubble bubble)
        {
            Cost = cost;
            AttackType = attackType;
            ArmourType = armourType;
            Armour = armour;
            Shield = shield;
            Targets = targets;
            Bubble = bubble;
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

        /// <summary>What one of these costs, in gold. Zero in column layout 1, which has no cost column.</summary>
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

        /// <summary>
        /// A pool that absorbs before health does, and absorbs raw. Zero is
        /// none, which is what every row of every layout before 3 carries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Armour does not apply to it and neither does the type chart.</b>
        /// That is what makes it a different lever from health rather than a
        /// second copy of it: a shield is worth exactly its number against every
        /// attack type there is, where a health pool is worth its number times a
        /// matrix cell and an armour multiplier. Overkill carries through to
        /// health, so a shield delays a body rather than granting it a free
        /// shot's worth of immunity.
        /// </para>
        /// <para>
        /// <b>It does not regenerate and there is nothing to carry over.</b>
        /// Creeps do not persist between rounds, so a shield is spent within the
        /// match that spawned it and the pool has no clock of its own.
        /// <see cref="Sim.ArmourType.Arcane"/> is unrelated and keeps its name.
        /// </para>
        /// </remarks>
        public int Shield { get; }

        /// <summary>
        /// How many shots one attack fires, each at its own creep and each with
        /// its own damage roll. One is an ordinary single shot, and every row of
        /// every layout before 3 is one.
        /// </summary>
        /// <remarks>
        /// The targets are taken nearest-to-exit first, by
        /// <see cref="Targeting.Chosen(System.ReadOnlySpan{WalkingTarget}, System.Span{int}, out int)"/>,
        /// which is the same total order a single shot is acquired by. <b>n
        /// shots draw exactly n rolls</b>, which is the half of the determinism
        /// contract this column is on the hook for -- a bubble is the other
        /// shape, and it is one shot and one roll however many bodies it lands
        /// on.
        /// </remarks>
        public int Targets { get; }

        /// <summary>
        /// The radial thing this row emits, or <see cref="Sim.Bubble.Absent"/>.
        /// A sweep, a blast and an aura are one mechanic; see <see cref="Sim.Bubble"/>.
        /// </summary>
        public Bubble Bubble { get; }

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
                    return TypedFold(hash);

                case 3:
                    return Bubble.Fold(TypedFold(hash).Add(Shield).Add(Targets));

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layout),
                        "Column layout "
                        + layout.ToString(CultureInfo.InvariantCulture)
                        + " has no fold in this row.");
            }
        }

        /// <summary>
        /// The four columns layout 2 added, in file order. Layout 3 folds them
        /// in the same places -- a widening moves no column that was already
        /// there -- so the two branches share this rather than each spelling it.
        /// </summary>
        private Hash64 TypedFold(Hash64 hash) =>
            hash
                .Add(Cost)
                .Add((int)AttackType)
                .Add((int)ArmourType)
                .Add(Armour);
    }
}
