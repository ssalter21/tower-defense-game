using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What a prepared shooter adds to its roll against the creeps one wave
    /// order sends, before the type chart and the target's armour.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A counter is paid to one unit type against one fielded game changer
    /// and to nothing else.</b> The bonus belongs to an anchor, the anchor
    /// named the unit type that answers it, and anything else shooting the same
    /// creep is unprepared and gets nothing -- which is the whole of what
    /// preparing buys.
    /// </para>
    /// <para>
    /// <b>The pairing is worked out before the match rather than inside it.</b>
    /// Which game changer a wave order fields is something only the sender's
    /// unlocks know: a wave order carries a type id, and a type id is a body.
    /// So the lookup happens where the unlocks and the schedule are in hand and
    /// reaches the tick loop as numbers, which is also what keeps the tick loop
    /// clear of every run-level type.
    /// </para>
    /// <para>
    /// <b>An entry is identified by the game changer and not by the body.</b>
    /// Two game changers may field the same creep row, so a bonus filed under
    /// the type id would pay one anchor's counter against the other anchor's
    /// unit.
    /// </para>
    /// <para>
    /// <see cref="None"/> is a match nobody fielded a changer in, and every
    /// bonus in it is zero. That is the ordinary case and it is a shared empty
    /// value rather than a branch anywhere else.
    /// </para>
    /// </remarks>
    public sealed class ShotBonus
    {
        /// <summary>Nothing is countered. Every lookup answers zero.</summary>
        public static readonly ShotBonus None = new ShotBonus(new Entry[0]);

        private readonly Entry[] _entries;

        private ShotBonus(Entry[] entries) => _entries = entries;

        /// <summary>
        /// What this defense's shooters get against the game changers this wave
        /// fields.
        /// </summary>
        /// <param name="wave">The wave being sent. Its orders are what carry the changers.</param>
        /// <param name="defense">The towers shooting at it, which is where a shooter type comes from.</param>
        /// <param name="unlocks">The sender's unlocks, which is what says a body is a changer.</param>
        /// <param name="schedule">The shape, which is what says a shooter answered that anchor.</param>
        public static ShotBonus Fielded(
            WaveScript wave,
            TowerLayout defense,
            Unlocks unlocks,
            AnchorSchedule schedule)
        {
            if (wave is null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            if (defense is null)
            {
                throw new ArgumentNullException(nameof(defense));
            }

            if (unlocks is null)
            {
                throw new ArgumentNullException(nameof(unlocks));
            }

            if (schedule is null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            var entries = new List<Entry>();

            for (int order = 0; order < wave.Count; order++)
            {
                if (!unlocks.TryChangerFor(wave.Orders[order].TypeId, out GameChanger? changer))
                {
                    continue;
                }

                for (int tower = 0; tower < defense.Count; tower++)
                {
                    int shooter = defense.Towers[tower].Type.Id;
                    int bonus = schedule.BonusVsTag(shooter, changer!);

                    if (bonus == 0 || Find(entries, shooter, order) != 0)
                    {
                        continue;
                    }

                    entries.Add(new Entry(shooter, order, changer!.Id, bonus));
                }
            }

            return entries.Count == 0 ? None : new ShotBonus(entries.ToArray());
        }

        /// <summary>
        /// What a shot from this unit type adds against the creeps of that wave
        /// order. Zero unless the two were paired, which is the ordinary case.
        /// </summary>
        /// <remarks>
        /// A linear scan over a handful of entries, on purpose: the obvious
        /// keyed collection is a banned type whose enumeration order is an
        /// implementation detail, and <see cref="None"/> leaves on the first
        /// line.
        /// </remarks>
        public int Against(int shooterTypeId, int waveOrder) => Find(_entries, shooterTypeId, waveOrder);

        public override string ToString()
        {
            if (_entries.Length == 0)
            {
                return "nothing countered";
            }

            var described = new string[_entries.Length];

            for (int index = 0; index < _entries.Length; index++)
            {
                described[index] = _entries[index].ToString();
            }

            return string.Join(", ", described);
        }

        /// <summary>
        /// What this pairing is worth, or zero where there is no entry for it.
        /// A bonus of zero is never stored, so an absent entry and a zero entry
        /// are the same answer.
        /// </summary>
        private static int Find(IReadOnlyList<Entry> entries, int shooterTypeId, int waveOrder)
        {
            for (int index = 0; index < entries.Count; index++)
            {
                if (entries[index].ShooterTypeId == shooterTypeId && entries[index].WaveOrder == waveOrder)
                {
                    return entries[index].Bonus;
                }
            }

            return 0;
        }

        /// <summary>One shooter, one wave order, and what the pairing is worth.</summary>
        private readonly struct Entry
        {
            internal Entry(int shooterTypeId, int waveOrder, int changerId, int bonus)
            {
                ShooterTypeId = shooterTypeId;
                WaveOrder = waveOrder;
                ChangerId = changerId;
                Bonus = bonus;
            }

            internal int ShooterTypeId { get; }

            internal int WaveOrder { get; }

            /// <summary>Which game changer this was paid against, rather than which body.</summary>
            internal int ChangerId { get; }

            internal int Bonus { get; }

            public override string ToString() =>
                "unit "
                + ShooterTypeId.ToString(CultureInfo.InvariantCulture)
                + " against game changer "
                + ChangerId.ToString(CultureInfo.InvariantCulture)
                + " in order "
                + WaveOrder.ToString(CultureInfo.InvariantCulture)
                + " for "
                + Bonus.ToString(CultureInfo.InvariantCulture);
        }
    }
}
