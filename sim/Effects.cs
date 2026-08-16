namespace Sim
{
    /// <summary>
    /// The timed modifiers one unit is carrying: one slot per modifiable stat,
    /// strongest-wins, with the timer refreshed when the same effect lands
    /// again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An effect is a stat, a magnitude and a duration. One model, not one
    /// per mechanic.</b> A slow, a haste, a rally, a curse and a granted pool
    /// are the same four fields with different numbers in them, which is the
    /// same collapse <see cref="Bubble"/> made of a sweep, a blast and an aura.
    /// What emits one is a bubble; what holds one is this; and nothing else in
    /// the simulation knows how a modifier is stored.
    /// </para>
    /// <para>
    /// <b>Range is not on the list and never will be.</b> A tower's coverage is
    /// intersected with the route once, at load, and handed to the tick loop as
    /// intervals of distance -- see <see cref="TowerCoverage"/>. The refusal is
    /// where the column is read; the absence is here.
    /// </para>
    /// <para>
    /// <b>Stacking is strongest-wins, and that is the only rule that cannot
    /// run away.</b> A player may build the same tower as many times as gold
    /// allows, so a rule that added magnitudes would hand a big enough board an
    /// arbitrarily large modifier -- and a slow of more than a hundred percent
    /// walks a creep backwards. One slot per stat makes the ceiling the
    /// strongest single row rather than the count of them.
    /// </para>
    /// <para>
    /// <b>The comparison is a strict total order, so applying two effects in
    /// either order lands on the same state.</b> Strength is distance from
    /// zero, and two magnitudes equally far from it are ordered by sign, with
    /// the lower one winning -- a debuff and a buff of the same size do not
    /// depend on which reached the creep first. The timer of the surviving
    /// magnitude is the later of the two expiries, for the same reason: a
    /// maximum is commutative and "the last one wins" is not.
    /// </para>
    /// <para>
    /// <b>A weaker effect is discarded rather than queued.</b> When the strong
    /// one expires the unit returns to its authored value and not to the weak
    /// one, because a queue is a stack wearing a different hat: it would make
    /// the total duration of a stat's displacement grow with the number of
    /// sources, which is what strongest-wins exists to stop.
    /// </para>
    /// <para>
    /// <b>Absence is a magnitude of zero</b>, which is not a magnitude any row
    /// can author -- <c>UnitTypeTable</c> refuses a bubble that modifies a stat
    /// by nothing at all. So an empty slot and an authored one cannot be
    /// confused, and no flag is needed beside the number.
    /// </para>
    /// <para>
    /// <b>Expiry is an absolute tick, not a countdown.</b> A countdown has to
    /// decide whether the tick an effect landed on is one of the ticks it
    /// lasts, and the answer depends on which phase of the tick emitted it --
    /// which would make "expires exactly on its duration" a different sentence
    /// for a bubble that fires with an attack and a bubble that pulses. An
    /// absolute expiry of <c>tick + duration</c>, cleared at the top of a tick,
    /// gives every effect exactly its duration of ticks after the one it landed
    /// on, whichever phase that was.
    /// </para>
    /// </remarks>
    public struct Effects
    {
        /// <summary>
        /// The share of its authored speed a walking unit can never drop below,
        /// in percent.
        /// </summary>
        /// <remarks>
        /// <b>A safety rail rather than a balance number.</b>
        /// <see cref="Match"/> refuses a unit with no speed at construction,
        /// because a unit that walks a corridor at nothing per tick never
        /// reaches the exit and never dies, so the match it is in cannot end --
        /// and a runtime modifier bypasses that guard entirely. The floor makes
        /// a hung match unreachable by arithmetic instead of by careful
        /// authoring: no combination of effects, authored or adversarial, can
        /// stop a creep, so the match cannot fail to terminate however the
        /// numbers are set.
        /// </remarks>
        public const int SpeedFloorPercent = 10;

        /// <summary>A whole, in the percentages every magnitude is authored in.</summary>
        private const int Whole = 100;

        /// <summary>
        /// The expiry a payload with no duration gets. A granted pool is the
        /// one payload that may say nothing about how long it lasts, because
        /// how long it lasts is how long it takes to be spent.
        /// </summary>
        private const int NeverExpires = int.MaxValue;

        private int _speedMagnitude;

        private int _speedExpiry;

        private int _cooldownMagnitude;

        private int _cooldownExpiry;

        private int _armourMagnitude;

        private int _armourExpiry;

        private int _shieldMagnitude;

        private int _shieldExpiry;

        /// <summary>
        /// What is left of the pool a shield payload granted, which is spent
        /// before the pool the row authored.
        /// </summary>
        /// <remarks>
        /// The granted one goes first because it is the one that can be taken
        /// away: a pool with a clock on it is worth less than a pool without
        /// one, so spending it first is the arrangement in which nothing is
        /// wasted.
        /// </remarks>
        private int _shieldPool;

        /// <summary>Whether this unit is carrying anything at all.</summary>
        /// <remarks>
        /// <b>The pool is in it as well as the four magnitudes</b>, and that is
        /// not belt and braces: this predicate decides whether
        /// <see cref="Fold"/> folds its second half, so anything it misses is a
        /// field that could differ between two runs with the fold saying
        /// nothing. A pool standing beside a magnitude of zero cannot be
        /// authored -- <c>UnitTypeTable</c> refuses a bubble that modifies
        /// nothing -- but this type is reachable without going through that
        /// refusal, and a fold's completeness should not rest on a check in
        /// another file.
        /// </remarks>
        public readonly bool Any =>
            _speedMagnitude != 0
            || _cooldownMagnitude != 0
            || _armourMagnitude != 0
            || _shieldMagnitude != 0
            || _shieldPool != 0;

        /// <summary>The percentage its walking speed is displaced by. Zero is unmodified.</summary>
        public readonly int SpeedMagnitude => _speedMagnitude;

        /// <summary>The percentage its cooldown is displaced by. Zero is unmodified.</summary>
        public readonly int CooldownMagnitude => _cooldownMagnitude;

        /// <summary>The percentage its armour is displaced by. Zero is unmodified.</summary>
        public readonly int ArmourMagnitude => _armourMagnitude;

        /// <summary>What is left of the pool a shield payload granted.</summary>
        public readonly int GrantedShield => _shieldPool;

        /// <summary>
        /// What a stat authored at <paramref name="authored"/> is worth while a
        /// modifier of <paramref name="magnitude"/> percent is on it.
        /// </summary>
        /// <remarks>
        /// <b>One fused integer expression, evaluated once.</b> Not a stage and
        /// then another stage: an integer division truncates, so two of them
        /// compute a different function from the same algebra written as one --
        /// which is the hazard <see cref="DamageModel"/>'s remarks name and the
        /// one this shares. The intermediate is a <c>long</c> because an
        /// adversarial magnitude times a large stat leaves the range of an
        /// <c>int</c>, and a wrapped product is a modifier that turns into its
        /// own opposite.
        /// </remarks>
        public static int Modified(int authored, int magnitude)
        {
            if (magnitude == 0)
            {
                return authored;
            }

            long scaled = ((long)authored * (Whole + (long)magnitude)) / Whole;

            if (scaled < 0)
            {
                return 0;
            }

            return scaled > int.MaxValue ? int.MaxValue : (int)scaled;
        }

        /// <summary>
        /// The slowest a unit authored at <paramref name="authored"/>
        /// milli-hexes a tick may ever walk, in the same units.
        /// </summary>
        /// <remarks>
        /// <b>At least one milli-hex, whatever the percentage truncates to.</b>
        /// A tenth of nine milli-hexes is zero in integer arithmetic, and zero
        /// is precisely the value the whole floor exists to make unreachable --
        /// so the floor of the floor is the smallest step the fixed-point
        /// representation has a name for. <see cref="Match"/> proves at
        /// construction that a wave walking at this speed still crosses the
        /// route inside the tick ceiling, so the guarantee is arithmetic rather
        /// than assumed.
        /// </remarks>
        public static int FloorSpeed(int authored)
        {
            int floor = (int)(((long)authored * SpeedFloorPercent) / Whole);

            return floor < 1 ? 1 : floor;
        }

        /// <summary>
        /// How fast a unit authored at <paramref name="authored"/> milli-hexes a
        /// tick actually walks under a modifier of <paramref name="magnitude"/>
        /// percent: the fused expression, with the floor binding it afterwards.
        /// </summary>
        /// <remarks>
        /// <b>The floor binds every effect at once and is applied last.</b> It
        /// is a property of the creep rather than of any one modifier, so
        /// clamping each effect as it landed would let two of them arrive under
        /// the floor together -- and strongest-wins means only one is ever on
        /// the slot anyway, which is what makes "after all modifiers" and
        /// "after the modifier" the same sentence here and a different one the
        /// moment a second slot is ever added.
        /// </remarks>
        public static int ModifiedSpeed(int authored, int magnitude)
        {
            int modified = Modified(authored, magnitude);
            int floor = FloorSpeed(authored);

            return modified < floor ? floor : modified;
        }

        /// <summary>
        /// The pool a shield payload of <paramref name="magnitude"/> percent
        /// grants a unit whose health pool is <paramref name="maxHp"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A percentage of the health it stands in front of, and not of the
        /// shield the row authored.</b> A shield is a pool rather than a rate,
        /// so there is no authored number of its own for a percentage to be a
        /// percentage of -- and the alternative reading, a share of the
        /// recipient's own shield column, is inert on every row the mechanic
        /// was designed for: the roster's walking rows author no shield at all,
        /// so an aura granting a share of it would grant nothing to everybody.
        /// See <c>docs/adr/0056-an-effect-is-a-stat-a-magnitude-and-a-duration.md</c>.
        /// </para>
        /// <para>
        /// A unit with no health pool is granted nothing, which needs no clause
        /// of its own: nothing can damage it, so a pool in front of its health
        /// is a pool nothing could ever spend.
        /// </para>
        /// </remarks>
        public static int Granted(int maxHp, int magnitude)
        {
            long pool = ((long)maxHp * magnitude) / Whole;

            if (pool <= 0)
            {
                return 0;
            }

            return pool > int.MaxValue ? int.MaxValue : (int)pool;
        }

        /// <summary>
        /// Lands what a bubble carries on a unit whose health pool is
        /// <paramref name="maxHp"/>, and says whether the walking speed moved.
        /// </summary>
        /// <remarks>
        /// <b>The bubble rather than four of its columns.</b> Everything this
        /// needs is on one value, and handing a checker three of its fields
        /// plus a number computed from a fourth is three chances to pass the
        /// wrong one -- the same reason <c>UnitTypeTable</c> builds a
        /// <see cref="Bubble"/> before asking anything about it. The grant is
        /// worked out here rather than by every caller, and only for the one
        /// payload that has any use for it.
        /// </remarks>
        public bool Land(Bubble bubble, int tick, int maxHp) =>
            Landed(
                bubble.Payload,
                bubble.Magnitude,
                bubble.DurationTicks,
                tick,
                bubble.Payload == BubblePayload.Shield ? Granted(maxHp, bubble.Magnitude) : 0);

        private bool Landed(BubblePayload payload, int magnitude, int durationTicks, int tick, int grant)
        {
            // Saturating rather than wrapping. A duration is bounded only by
            // the range of the column it is authored in, so tick + duration can
            // leave an int -- and a wrapped sum comes back negative, which
            // reads as an effect that ran out before it landed. A duration
            // nothing can reach and a duration that never ends are the same
            // thing to a match with a tick ceiling, and this is the one of the
            // two that cannot be mistaken for its opposite.
            int expiry = durationTicks == 0 || durationTicks > int.MaxValue - tick
                ? NeverExpires
                : tick + durationTicks;

            switch (payload)
            {
                case BubblePayload.Speed:
                {
                    int before = _speedMagnitude;
                    Apply(ref _speedMagnitude, ref _speedExpiry, magnitude, expiry);

                    return _speedMagnitude != before;
                }

                case BubblePayload.Cooldown:
                    Apply(ref _cooldownMagnitude, ref _cooldownExpiry, magnitude, expiry);

                    return false;

                case BubblePayload.Armour:
                    Apply(ref _armourMagnitude, ref _armourExpiry, magnitude, expiry);

                    return false;

                case BubblePayload.Shield:
                    Grant(magnitude, expiry, grant);

                    return false;

                default:
                    throw new SimulationException(
                        "A bubble carrying "
                        + payload.ToString().ToLowerInvariant()
                        + " landed as a timed effect. Damage is not a modifier and no bubble carries "
                        + "nothing at all: both are settled where the columns are read, so arriving here "
                        + "means a payload was added without a slot to hold it.");
            }
        }

        /// <summary>
        /// Clears whatever has run out as of <paramref name="tick"/>, and says
        /// whether the walking speed moved.
        /// </summary>
        /// <remarks>
        /// The expiry is the last tick the effect is in force, so an effect that
        /// landed on tick <c>t</c> with a duration of <c>n</c> is on for ticks
        /// <c>t + 1</c> through <c>t + n</c> and off on <c>t + n + 1</c> --
        /// exactly <c>n</c> ticks, and the same <c>n</c> whichever phase of tick
        /// <c>t</c> emitted it.
        /// </remarks>
        public bool Expire(int tick)
        {
            bool speedMoved = _speedMagnitude != 0 && tick > _speedExpiry;

            if (speedMoved)
            {
                _speedMagnitude = 0;
                _speedExpiry = 0;
            }

            if (_cooldownMagnitude != 0 && tick > _cooldownExpiry)
            {
                _cooldownMagnitude = 0;
                _cooldownExpiry = 0;
            }

            if (_armourMagnitude != 0 && tick > _armourExpiry)
            {
                _armourMagnitude = 0;
                _armourExpiry = 0;
            }

            if (_shieldMagnitude != 0 && tick > _shieldExpiry)
            {
                _shieldMagnitude = 0;
                _shieldExpiry = 0;
                _shieldPool = 0;
            }

            return speedMoved;
        }

        /// <summary>
        /// Spends the granted pool against a roll and hands back what is left of
        /// the roll.
        /// </summary>
        /// <remarks>
        /// Raw, exactly as the authored pool is spent: a granted point is worth
        /// one point against every attack type there is, and overkill carries
        /// through. See <see cref="Match"/>'s remarks on absorption, which this
        /// is the first half of.
        /// </remarks>
        public int Spend(int roll)
        {
            if (_shieldPool <= 0)
            {
                return roll;
            }

            if (_shieldPool >= roll)
            {
                _shieldPool -= roll;

                return 0;
            }

            roll -= _shieldPool;
            _shieldPool = 0;

            return roll;
        }

        /// <summary>
        /// Folds what this unit is carrying into the rolling state hash.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The four magnitudes always, and the rest only when one of them is
        /// non-zero.</b> Whether the second half was folded is decided entirely
        /// by numbers the first half already folded, so the fold has no
        /// ambiguity about its own length -- and a match whose content authors
        /// no bubble pays two integers a creep a tick rather than nine, which
        /// is what keeps the re-simulation budget where it was.
        /// </para>
        /// <para>
        /// The expiries are in it because they are exactly the sort of field
        /// nothing draws: two runs that disagree about when a slow ends look
        /// identical for as long as it lasts and are already different matches.
        /// </para>
        /// </remarks>
        internal readonly Hash64 Fold(Hash64 hash)
        {
            hash = hash
                .Add(_speedMagnitude, _cooldownMagnitude)
                .Add(_armourMagnitude, _shieldMagnitude);

            if (!Any)
            {
                return hash;
            }

            return hash
                .Add(_speedExpiry, _cooldownExpiry)
                .Add(_armourExpiry, _shieldExpiry)
                .Add(_shieldPool);
        }

        /// <summary>
        /// One slot, resolved: the stronger magnitude wins, an equal one
        /// refreshes the timer, and a weaker one is discarded.
        /// </summary>
        private static void Apply(ref int magnitude, ref int expiry, int landed, int landedExpiry)
        {
            if (magnitude == 0 || Stronger(landed, magnitude))
            {
                magnitude = landed;
                expiry = landedExpiry;

                return;
            }

            // The same effect again. The magnitude does not stack -- that is
            // the whole rule -- and the timer is refreshed, which is the half a
            // player can see.
            if (landed == magnitude && landedExpiry > expiry)
            {
                expiry = landedExpiry;
            }
        }

        /// <summary>
        /// The granted-pool slot, resolved by the same rule, with one extra
        /// clause: the same grant landing again refills the pool rather than
        /// adding to it.
        /// </summary>
        private void Grant(int magnitude, int expiry, int pool)
        {
            if (_shieldMagnitude == 0 || Stronger(magnitude, _shieldMagnitude))
            {
                _shieldMagnitude = magnitude;
                _shieldExpiry = expiry;
                _shieldPool = pool;

                return;
            }

            if (magnitude != _shieldMagnitude)
            {
                return;
            }

            if (expiry > _shieldExpiry)
            {
                _shieldExpiry = expiry;
            }

            // Restored to what the effect grants, never past it. A pulse that
            // added to what it had left would be a stack with extra steps, and
            // an aura is the shape a player can build many of.
            if (pool > _shieldPool)
            {
                _shieldPool = pool;
            }
        }

        /// <summary>
        /// Whether one magnitude beats another. A strict total order on the
        /// integers, so that landing two effects in either order reaches the
        /// same state.
        /// </summary>
        /// <remarks>
        /// Strength is distance from zero, because a magnitude is a
        /// displacement and the sign says which way. Two that are equally far
        /// from it are ordered by the sign, lower first -- an arbitrary
        /// tiebreak in the sense that either answer would do, and not an
        /// arbitrary one in the sense that matters: without it a curse and a
        /// blessing of the same size would resolve by whichever landed last,
        /// and the fold would disagree between two runs that differed only in
        /// the order two towers were built.
        /// </remarks>
        private static bool Stronger(int magnitude, int than)
        {
            long size = magnitude < 0 ? -(long)magnitude : magnitude;
            long other = than < 0 ? -(long)than : than;

            return size != other ? size > other : magnitude < than;
        }
    }
}
