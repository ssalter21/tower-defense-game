using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One match: one authored defense, one authored wave, one seed, and the
    /// tick loop that resolves them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One surface, every scenario.</b> Construct from
    /// <c>(map, rules, layout, wave, seed)</c>, call <see cref="Advance(int, IMatchEvents)"/>
    /// as many times as you like, pull a <see cref="Snapshot"/> when you want
    /// one, and take the <see cref="Result"/> at the end. Normal playback is
    /// one tick at a time with a snapshot pulled each time; fast-forward is more
    /// ticks per call; a seek is a fresh match advanced to the tick asked for;
    /// instant-resolve is one call with a large number and nobody pulling
    /// anything; the headless command line and the parity run are the same again
    /// with a hash trace being read off; and a server re-validating a submitted
    /// record would be identical to instant-resolve. <b>None of those is a mode,
    /// a flag or a branch.</b> If any of them needed its own code path, this
    /// surface would be wrong -- which is a claim a test can check, and does.
    /// </para>
    /// <para>
    /// <b>The snapshot is pulled, not pushed.</b> A match that nobody asks for a
    /// picture of never builds one. That is the whole of what instant-resolve
    /// is: not a headless mode, just the ordinary loop with nothing calling
    /// <see cref="PullSnapshot"/>. Likewise the events -- they go to an object
    /// passed into <see cref="Advance(int, IMatchEvents)"/>, so a re-simulation
    /// that passes nothing discards them by construction rather than by
    /// remembering to.
    /// </para>
    /// <para>
    /// <b>The rolling state hash is always on.</b> There is no constructor
    /// argument for it and no property that turns it off, because a flag would
    /// create a configuration in which the central assertion of the whole
    /// architecture is not running, and that is the one configuration nobody
    /// should be able to reach. It costs a fold over a few dozen integers per
    /// tick, which is measured against the re-simulation budget alongside
    /// everything else.
    /// </para>
    /// <para>
    /// <b>A shot resolves through the ruleset exactly once, where it lands.</b>
    /// The dice give a roll; what that roll takes off a creep is the counter,
    /// the type chart and the target's armour fused into one multiply and one
    /// divide, evaluated once, in <see cref="DamageModel.Dealt"/>. A shot that
    /// reaches nothing resolves nothing, which is why the expression is at the
    /// landing and not at the trigger.
    /// </para>
    /// <para>
    /// <b>The dice are rolled exactly once per shot, for damage, and nowhere
    /// else.</b> Everything else in the match is determined. That makes the
    /// stream's position a running count of the shots fired so far, in order --
    /// so a unit-ordering difference between two runs, which is the desync
    /// nobody would otherwise notice, changes which shot got which number and
    /// diverges the state hash on the tick it happened.
    /// </para>
    /// <para>
    /// <b>The order of work inside a tick is part of the rules.</b> Whatever has
    /// run out expires, then creeps move, then dying creeps age, then
    /// projectiles fly and land, then towers act, then auras pulse, then the
    /// dead are cleared away, then the tick number advances, whatever raises
    /// raises and the wave releases whatever is due. Changing that
    /// order changes replays even
    /// though no number in any file moved, which is exactly what the simulation
    /// version exists to say.
    /// </para>
    /// <para>
    /// <b>Expiry opens the tick and emission closes it, and that is what makes
    /// a duration mean one thing.</b> An effect landing on tick <c>t</c> is in
    /// force for ticks <c>t + 1</c> through <c>t + duration</c> whichever phase
    /// emitted it -- a bubble that fires with an attack lands in the middle of
    /// a tick and one that pulses lands at the end of one, and neither is worth
    /// a different sentence. See <see cref="Effects"/>, which is where a
    /// modifier is held and where that arithmetic lives.
    /// </para>
    /// </remarks>
    public sealed class Match
    {
        /// <summary>
        /// How many ticks a second is worth. The simulation counts ticks and
        /// nothing else -- there is no clock in here and no elapsed anything --
        /// but a record, a view and a person asking how long a match is all need
        /// one number saying what a tick is worth, and it belongs to the
        /// simulation because changing it changes every replay.
        /// </summary>
        public const int TicksPerSecond = 30;

        /// <summary>
        /// How many ticks apart the units of one order are released. A count in
        /// the wave file is a column of units, not a pile of them: releasing
        /// them all on one tick would stack them at one point forever, since
        /// they share a speed and a route, and a stack is the one arrangement in
        /// which unit ordering cannot be observed.
        /// </summary>
        /// <remarks>
        /// Fifteen until 8 August 2026, when the clock dilation that tripled
        /// every duration in <c>units.txt</c>, divided every speed by three and
        /// tripled every tick in <c>wave.txt</c> was completed here. This was the
        /// one part of it that could not reach content: a cadence left at fifteen
        /// emptied a column of ten over the same hundred and fifty ticks while
        /// its units walked a third as far in them, which made columns three
        /// times denser in space -- a balance change smuggled inside a change
        /// that promised to be pure time. Forty-five restores the spacing, and
        /// because the cadence is a rule rather than a number in a file, moving
        /// it is a <see cref="SimulationVersion"/> bump.
        /// </remarks>
        public const int SpawnIntervalTicks = 45;

        /// <summary>
        /// Names the fold and its layout. The digit bumps when the set of fields
        /// hashed changes, which retires every golden trace pinned to the old
        /// set loudly rather than leaving them silently comparing fewer things.
        /// </summary>
        /// <remarks>
        /// <c>match-state/3</c> was the layout that folds what every unit is
        /// carrying -- the magnitude and the expiry of each timed effect on it,
        /// the pool a shield bubble granted it, and how long until its own aura
        /// pulses next. All of it is state a view never sees, which is exactly
        /// the kind of field this fold exists to watch: two runs that disagree
        /// about when a slow ends look identical for as long as it lasts and are
        /// already different matches. <c>match-state/4</c> adds the row each
        /// creep is, which stopped being a constant of a creep the moment a body
        /// could change into another row mid-lane. <c>match-state/5</c> adds the
        /// clock each creep raises on, which is state nothing else would ever
        /// notice drifting, and the running count of leaks nobody sent, which is
        /// what says how much of the total is priced the other way. Whether a
        /// body was raised is a constant of it and folds at the spawn, beside
        /// the row it is and the lane it walks in.
        /// </remarks>
        private const string HashLabel = "match-state/5";

        /// <summary>
        /// A match that has not ended by here is a match that is never going to,
        /// and something is wrong with a rule rather than with the tuning. Sixty
        /// times the length of the skeleton's match, so nothing legitimate can
        /// approach it.
        /// </summary>
        private const int TickCeiling = 120000;

        /// <summary>Thousandths of a hex. Speeds are authored in them.</summary>
        private const int MilliHexPerHex = 1000;

        /// <summary>Tenths of a hex, for the lateral offsets below.</summary>
        private const int LateralDenominator = 10;

        /// <summary>
        /// The lateral offsets creeps are given in turn, in tenths of a hex.
        /// This is why an overtake is visible: two creeps at the same distance
        /// along the corridor are the moment a pass happens, and without an
        /// offset they would be the same point. It is cycled by spawn order
        /// rather than drawn, because the damage roll is the only randomness in
        /// this simulation and a second draw would make it not so.
        /// </summary>
        private static readonly int[] LateralTenths = { 0, 3, -3 };

        private readonly TowerLayout _layout;

        private readonly WaveScript _wave;

        /// <summary>The matrix, the armour expression and the floor every hit goes through.</summary>
        private readonly Ruleset _rules;

        private readonly TowerCoverage _coverage;

        private readonly Pcg32 _dice;

        private readonly Fix64 _routeLength;

        private readonly Fix64[] _lateralOffsets;

        /// <summary>
        /// One entry per wave order: how far its units move each tick with
        /// nothing on them.
        /// </summary>
        /// <remarks>
        /// <b>What a creep walks at is on the creep, and this is where it
        /// starts.</b> A modifier is per unit rather than per order -- two
        /// Minions in one column are not slowed together -- so the tick loop
        /// reads <see cref="Creep.Step"/> and this is read once, when one
        /// spawns. It stays a field because it is also the one place the
        /// truncated remainder that the state hash exists to watch is created
        /// for an unmodified creep, and because the wave's speeds are what the
        /// termination invariant below is proved against.
        /// </remarks>
        private readonly Fix64[] _stepPerTick;

        /// <summary>One entry per wave order: how many of its units have been released.</summary>
        private readonly int[] _released;

        /// <summary>
        /// How many rows one wave order can put on the corridor: what it sends,
        /// the row that becomes, the row either of them raises, and the row that
        /// becomes in its turn.
        /// </summary>
        /// <remarks>
        /// <b>Four is the whole of it, and the table is what makes it so.</b> A
        /// raised row may not raise, and a row a body becomes may not raise -- so
        /// one order names one raised row and the generations stop at one, which
        /// is what lets every check the constructor makes be a walk over a fixed
        /// handful of rows rather than over a graph.
        /// </remarks>
        private const int RowsPerOrder = 4;

        /// <summary>
        /// One entry per wave order: how many of its units reached the exit. A
        /// total is not enough to price a leak, because what a leak costs is
        /// what the thing that leaked cost -- so what is counted is which order
        /// walked past, and the order is what carries the type.
        /// </summary>
        private readonly int[] _leakedByOrder;

        /// <summary>
        /// One entry per wave order: how many bodies raised by that order's
        /// units reached the exit.
        /// </summary>
        /// <remarks>
        /// <b>Counted apart because it is priced apart.</b> A leak charges what
        /// the thing that leaked cost, and nobody sent a raised body -- what it
        /// is worth is the price of the row that was raised, which the order's
        /// own type does not carry. The order still says which row that is: a
        /// raised body's lineage is the order that released the body that raised
        /// it, and the table refuses a second generation.
        /// </remarks>
        private readonly int[] _leakedRaisedByOrder;

        private readonly Tower[] _towers;

        private Creep[] _creeps;

        /// <summary>
        /// Scratch space for one acquisition: the walking creeps a tower can
        /// reach, refilled from <see cref="_creeps"/> every time a tower looks.
        /// </summary>
        /// <remarks>
        /// A field rather than a local, because seeking re-simulates: anything
        /// the tick path allocates is a cost every scrub of the slider pays. It
        /// is at least the wave's total, so an ordinary match never grows it,
        /// and never smaller than the creep array -- which is what bounds it now
        /// that a raised body, in no wave order at all, can be walking without
        /// having been released.
        /// </remarks>
        private WalkingTarget[] _reachable;

        /// <summary>
        /// Every tower's acquired targets, laid end to end: tower <c>t</c> owns
        /// the <see cref="_targetsPerTower"/> entries starting at
        /// <c>t * _targetsPerTower</c>, and holds
        /// <see cref="Tower.TargetCount"/> of them.
        /// </summary>
        /// <remarks>
        /// One flat array rather than an array per tower, for the reason
        /// <see cref="_reachable"/> is a field: seeking re-simulates, so
        /// anything the tick path allocates is a cost every scrub of the slider
        /// pays. The width is the widest row in this defense, so a board of
        /// single-target towers is exactly the one integer per tower it always
        /// was.
        /// </remarks>
        private readonly int[] _towerTargets;

        /// <summary>
        /// How many shots the widest row standing in this defense fires. Every
        /// tower's slice is this wide, so the arithmetic finding a tower's
        /// targets is a multiply rather than a walk.
        /// </summary>
        private readonly int _targetsPerTower;

        private Projectile[] _projectiles;

        private int _creepCount;

        private int _projectileCount;

        private int _nextEntityId;

        private int _spawnOrdinal;

        private int _releasedTotal;

        private int _leaked;

        /// <summary>How many of those leaks were bodies something raised.</summary>
        private int _leakedRaised;

        private int _killed;

        private int _shotsFired;

        /// <summary>
        /// Whether any tower standing here pulses on a clock of its own, and
        /// whether anything the wave sends does.
        /// </summary>
        /// <remarks>
        /// Read once, at construction, from rows that cannot change afterwards.
        /// A match whose content authors no aura -- which is every match the
        /// committed roster can produce -- then pays two comparisons a tick for
        /// the whole phase rather than a walk over everything on the board.
        /// </remarks>
        private readonly bool _towersPulse;

        private readonly bool _walkersPulse;

        /// <summary>
        /// Whether anything the wave can put on the corridor raises. Read once,
        /// at construction, from rows that cannot change afterwards -- so a
        /// match whose content authors no raise, which is every match the
        /// committed wave produces, pays one comparison a tick for the whole
        /// phase rather than a walk over everything on the board.
        /// </summary>
        private readonly bool _walkersRaise;

        /// <summary>
        /// How many times a target-selection tie has been broken. Internal, and
        /// in the hash for exactly that reason: it is the field that moves when
        /// two runs disagree about unit ordering and agree about everything a
        /// view can see.
        /// </summary>
        private int _tiebreaksBroken;

        /// <summary>
        /// A fold over every creep's constant properties at the moment it was
        /// spawned. Kept as a running value so the per-tick fold does not have
        /// to re-absorb things that cannot have changed.
        /// </summary>
        private Hash64 _spawnFold;

        private Hash64 _stateHash;

        /// <summary>
        /// Builds a match. Everything it will ever know arrives here: nothing in
        /// this assembly can open a file, read a clock or ask the machine
        /// anything.
        /// </summary>
        /// <param name="map">The board, and the corridor the route is walked along.</param>
        /// <param name="rules">
        /// The matrix, the armour expression and the floor. Required even where
        /// no row of the table carries a type: a match whose rules were optional
        /// would be a match that could resolve a typed shot against nothing.
        /// </param>
        /// <param name="layout">The towers that stand.</param>
        /// <param name="wave">The orders that walk.</param>
        /// <param name="seed">What the one dice stream is started from.</param>
        public Match(
            HexMap map,
            Ruleset rules,
            TowerLayout layout,
            WaveScript wave,
            ulong seed)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _wave = wave ?? throw new ArgumentNullException(nameof(wave));

            _coverage = TowerCoverage.For(map, layout);
            _dice = new Pcg32(seed);
            Map = map;
            Seed = seed;
            _routeLength = _coverage.RouteLength;

            _lateralOffsets = new Fix64[LateralTenths.Length];

            for (int index = 0; index < LateralTenths.Length; index++)
            {
                _lateralOffsets[index] = Fix64.FromRatio(LateralTenths[index], LateralDenominator);
            }

            _stepPerTick = new Fix64[wave.Count];
            _released = new int[wave.Count];
            _leakedByOrder = new int[wave.Count];
            _leakedRaisedByOrder = new int[wave.Count];

            bool walkersPulse = false;
            bool walkersRaise = false;
            var rows = new UnitType[RowsPerOrder];

            for (int index = 0; index < wave.Count; index++)
            {
                UnitOrder order = wave.Orders[index];
                UnitType type = order.Type;

                // Every row this order can put on the corridor, not only the row
                // it names: a body of any of them stands on this map as surely
                // as the one the wave sent, so all of them are held to the same
                // two things and any of them can be what pulses.
                int walkers = WalkersOf(type, rows);

                for (int row = 0; row < walkers; row++)
                {
                    RequireItWalks(rows[row]);
                    RequireResolvable(rows[row]);

                    walkersPulse = walkersPulse || rows[row].Bubble.IsAnAura;
                    walkersRaise = walkersRaise || rows[row].Raises is not null;
                }

                RequireItArrives(order);

                // Once, here, rather than in the tick loop: this is a division,
                // and it is also the one place the truncated remainder that the
                // state hash exists to watch is created.
                _stepPerTick[index] = Fix64.FromRatio(type.SpeedMilliHexPerTick, MilliHexPerHex);
            }

            _walkersPulse = walkersPulse;
            _walkersRaise = walkersRaise;

            _towers = new Tower[layout.Count];
            _targetsPerTower = 1;

            bool towersPulse = false;

            for (int index = 0; index < layout.Count; index++)
            {
                UnitType standing = layout.Towers[index].Type;

                RequireResolvable(standing);

                towersPulse = towersPulse || standing.Bubble.IsAnAura;

                if (standing.Targets > _targetsPerTower)
                {
                    _targetsPerTower = standing.Targets;
                }
            }

            _towersPulse = towersPulse;

            _towerTargets = new int[layout.Count * _targetsPerTower];

            for (int index = 0; index < layout.Count; index++)
            {
                // Tower ids are the defense's own order, counted from one, which
                // is what lets a snapshot carry no tower type and no tower
                // position and still be joinable to the static defense.
                _towers[index].Id = index + 1;
                _towers[index].State = TowerState.Idle;
            }

            _nextEntityId = layout.Count + 1;
            _creeps = new Creep[8];

            // At least as many candidates as the creep array can hold bodies,
            // and at least the wave's own total so an ordinary match never grows
            // it. Nothing walking is ever outside the creep array, so that is
            // the bound; the wave's total was it until a body could put another
            // body on the corridor.
            _reachable = new WalkingTarget[
                wave.TotalUnits < _creeps.Length ? _creeps.Length : wave.TotalUnits];
            _projectiles = new Projectile[8];

            _spawnFold = Hash64.Start("match-spawns/1");

            // The seed is deliberately NOT folded in here. It arrives every
            // tick anyway, as the position of the stream it started -- and
            // folding it separately as well would mean two runs with different
            // seeds differed from tick zero whether or not the stream position
            // was being hashed at all, which would make the check that the
            // hash covers the dice a check that cannot fail.
            _stateHash = Hash64.Start(HashLabel)
                .Add(unchecked((long)map.MapHash.Value))
                .Add(_routeLength.Raw)
                .Add(layout.Count)
                .Add(wave.TotalUnits);

            Release(0);
            Fold();
        }

        /// <summary>
        /// Every row one wave order can put on the corridor, written into
        /// <paramref name="rows"/>: what it sends, the row that becomes, the row
        /// it raises, and the row that becomes in its turn.
        /// </summary>
        /// <remarks>
        /// <b>It bottoms out because the table makes it.</b> A raised row may
        /// not raise, and a row a body becomes may not raise, so there is one
        /// raise in a lineage and no second generation to look for -- see
        /// <see cref="UnitType.Raises"/>. That is the difference between this and
        /// a graph walk, and it is what lets the termination bound below be
        /// arithmetic.
        /// </remarks>
        private static int WalkersOf(UnitType type, UnitType[] rows)
        {
            int count = AppendLineage(type, rows, 0);

            return AppendLineage(type.Raises, rows, count);
        }

        /// <summary>
        /// Appends a row and the row it becomes to <paramref name="rows"/> and
        /// hands back how many are in it. Nothing at all where the row is
        /// absent.
        /// </summary>
        private static int AppendLineage(UnitType? type, UnitType[] rows, int count)
        {
            if (type is null)
            {
                return count;
            }

            rows[count++] = type;

            if (type.Becomes is UnitType successor)
            {
                rows[count++] = successor;
            }

            return count;
        }

        /// <summary>
        /// A row that gets anywhere. Nothing walking at nothing per tick ever
        /// reaches the exit or dies, so a match holding one cannot end.
        /// </summary>
        private static void RequireItWalks(UnitType type)
        {
            if (type.SpeedMilliHexPerTick <= 0)
            {
                throw new SimulationException(
                    "The wave sends "
                    + type.ToString()
                    + ", which has no speed. A unit that walks a corridor at nothing per tick never "
                    + "reaches the exit and never dies, so the match it is in cannot end.");
            }
        }

        /// <summary>
        /// A row this tick loop can actually resolve, or a refusal naming what
        /// about it is not.
        /// </summary>
        /// <remarks>
        /// <b>What used to be here was the whole of the bubble.</b> Between
        /// #216 and #217 a row whose bubble carried a period or a payload that
        /// was not damage was refused right here, because the columns had
        /// landed and the machinery behind them had not -- and a Cryomancer
        /// standing on the board firing and slowing nothing, with nothing
        /// anywhere saying so, is the failure that refusal existed to prevent.
        /// <see cref="Effects"/> is that machinery, so the refusal is gone and
        /// the rows play. What is left is the one shape no schema check can
        /// catch, because it is about this loop rather than about the row.
        /// </remarks>
        private static void RequireResolvable(UnitType type)
        {
            // Nothing that walks attacks in this loop, so a shot count on a
            // walking row is a column read by nothing -- the same failure a
            // bubble nobody emits would be, and refused in the same place.
            if (type.Role == UnitRole.Moving && type.Targets > 1)
            {
                throw new SimulationException(
                    type.ToString()
                    + " walks and fires at "
                    + type.Targets.ToString(CultureInfo.InvariantCulture)
                    + " targets. Nothing that walks the corridor attacks anything in this simulation, so "
                    + "the count would be read by nothing at all.");
            }
        }

        /// <summary>
        /// That an order of this wave reaches the exit inside
        /// <see cref="TickCeiling"/> even walking at the slowest speed any
        /// combination of effects can leave it at.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the termination invariant, and it is arithmetic rather
        /// than care.</b> The constructor refuses a unit with no speed because
        /// a unit that walks a corridor at nothing per tick never reaches the
        /// exit and never dies, so the match it is in cannot end. A runtime
        /// modifier bypasses that guard entirely -- a slow of a hundred percent
        /// is a legal authoring of a legal column -- and what the match does
        /// instead of hanging is run to the ceiling and throw, thousands of
        /// ticks after the mistake.
        /// </para>
        /// <para>
        /// <b>So the floor is what makes a hung match unreachable, and this is
        /// the proof of it for this map and this wave.</b>
        /// <see cref="Effects.FloorSpeed"/> is the slowest a creep can ever be
        /// made to walk; the worst case is the last unit of the order leaking
        /// at that speed; and if that arrives before the ceiling then no
        /// arrangement of effects can produce a match that does not end. It is
        /// checked here rather than asserted in prose because the numbers it is
        /// true of are the map's route length and the wave's cadence, and both
        /// are arguments.
        /// </para>
        /// <para>
        /// <b>An uncapped spawner is bounded by the board rather than by a
        /// count, so the proof is over arrival and not over population.</b> How
        /// many bodies a raise puts on the corridor is unbounded in the only
        /// sense that matters to a designer -- a slowed spawner raises ten times
        /// as many -- and no arithmetic here caps it. What it is not is
        /// unbounded in time: a body raises only while it is walking, so no
        /// raise happens later than the tick its raiser would have left at, and
        /// what a raise puts down raises nothing in its turn. The last raised
        /// body is therefore at the exit within one floored crossing of the
        /// latest its raiser could still be walking, and that is the number
        /// checked below. The population between here and there is a finding
        /// for the sweep, not a termination risk.
        /// </para>
        /// </remarks>
        private void RequireItArrives(UnitOrder order)
        {
            UnitType type = order.Type;

            long latest = order.TickOffset
                + ((long)SpawnIntervalTicks * (order.Count - 1))
                + LastOut(type);

            if (latest >= TickCeiling)
            {
                throw new SimulationException(
                    "The wave sends "
                    + order.Count.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + type.ToString()
                    + ", whose slowest possible walk -- "
                    + Effects.FloorSpeed(Slowest(type)).ToString(CultureInfo.InvariantCulture)
                    + " thousandths of a hex a tick, which is the floor under every effect at once -- puts "
                    + "the last of them at the exit on tick "
                    + latest.ToString(CultureInfo.InvariantCulture)
                    + ", at or past the ceiling of "
                    + TickCeiling.ToString(CultureInfo.InvariantCulture)
                    + ". The floor is what makes a match that cannot end unreachable by arithmetic, and a "
                    + "route this long against a speed this small is where the arithmetic stops holding.");
            }

            // What those bodies put down. A raise fires only while its raiser is
            // walking, so the tick above is the latest any raise can happen; a
            // body raised then has a whole corridor in front of it, which is the
            // worst case whatever distance it was actually raised at.
            RequireWhatItRaisesArrives(type, type.Raises, latest);
        }

        /// <summary>
        /// That a body raised at the last moment its raiser could have raised
        /// one still reaches the exit inside <see cref="TickCeiling"/>.
        /// </summary>
        private void RequireWhatItRaisesArrives(UnitType raiser, UnitType? raised, long raiserLatest)
        {
            if (raised is null)
            {
                return;
            }

            long latest = raiserLatest + LastOut(raised);

            if (latest < TickCeiling)
            {
                return;
            }

            throw new SimulationException(
                raiser.ToString()
                + " raises "
                + raised.ToString()
                + ", and a body raised at the last moment its raiser could raise one is at the exit on "
                + "tick "
                + latest.ToString(CultureInfo.InvariantCulture)
                + ", at or past the ceiling of "
                + TickCeiling.ToString(CultureInfo.InvariantCulture)
                + ". How many a spawner raises is bounded by the board and deliberately not by this, but "
                + "when the last of them gets there has to be arithmetic or the match cannot be proved to "
                + "end.");
        }

        /// <summary>
        /// How many ticks after it appears a body of this row is off the map at
        /// the worst: a whole corridor at the slowest walk any combination of
        /// effects can leave it at, and then the longest death it can die.
        /// </summary>
        /// <remarks>
        /// <b>Taken against the row and the row it becomes together.</b> A body
        /// that changes row mid-lane finishes the corridor at the slower of the
        /// two speeds and dies for the longer of the two deaths, and which half
        /// it spends where is unknown here -- so the bound is the worst of both,
        /// which is a walk no body can be slower than.
        /// </remarks>
        private long LastOut(UnitType type)
        {
            int floor = Effects.FloorSpeed(Slowest(type));

            // The step the creep would actually take, and not the milli-hexes
            // it was worked out from. Those are two different numbers -- the
            // conversion into Q32.32 truncates -- and the difference is in the
            // direction that flatters the answer, so the bound has to be taken
            // against the smaller of the two. The route length is taken raw
            // for the same reason: a whole-hex count discards up to a hex of
            // corridor, which is hundreds of ticks at a floored speed.
            long step = Fix64.FromRatio(floor, MilliHexPerHex).Raw;

            // Ceiling division, so a remainder is a whole extra tick of walking
            // rather than a rounding that flatters it again. The leak test is
            // "at or past the route length", so this is exactly the number of
            // steps that reaches it.
            long crossing = ((_routeLength.Raw + step) - 1) / step;

            return crossing + LongestDeath(type);
        }

        /// <summary>The slower of a row's authored speed and its successor's.</summary>
        private static int Slowest(UnitType type) =>
            type.Becomes is UnitType next && next.SpeedMilliHexPerTick < type.SpeedMilliHexPerTick
                ? next.SpeedMilliHexPerTick
                : type.SpeedMilliHexPerTick;

        /// <summary>The longer of a row's authored death and its successor's.</summary>
        private static int LongestDeath(UnitType type) =>
            type.Becomes is UnitType next && next.DyingTicks > type.DyingTicks
                ? next.DyingTicks
                : type.DyingTicks;

        /// <summary>The seed the dice were started from.</summary>
        public ulong Seed { get; }

        /// <summary>The board this match is fought on.</summary>
        /// <remarks>
        /// <para>
        /// <b>The four things a match is made of, readable off the match.</b>
        /// A match already holds every one of them; what these add is the
        /// ability for something handed a match to build the same match again,
        /// which is what a view that seeks by re-simulating has to do. See
        /// <see cref="Run.MatchAt"/>, which hands one back for exactly that,
        /// and <c>client/Assets/View/MatchView.cs</c>, whose whole memory of a
        /// match is these and <see cref="Seed"/>.
        /// </para>
        /// <para>
        /// <b>They are readers and nothing more.</b> None of them is settable
        /// and none of the four types is mutable, so nothing reached through
        /// here can move a match -- <see cref="Advance"/> remains the only
        /// thing that does.
        /// </para>
        /// </remarks>
        public HexMap Map { get; }

        /// <summary>The matrix, the armour expression and the floor every hit goes through.</summary>
        public Ruleset Rules => _rules;

        /// <summary>The towers that stand for the whole of it.</summary>
        public TowerLayout Layout => _layout;

        /// <summary>The orders that walk.</summary>
        public WaveScript Wave => _wave;

        /// <summary>Which tick the match is on. Zero before it has been advanced.</summary>
        public int Tick { get; private set; }

        /// <summary>
        /// Whether everything the wave sends has been released and nothing is
        /// left on the map. A finished match ignores further advancing, so
        /// fast-forwarding past the end is the same call as any other.
        /// </summary>
        public bool IsFinished =>
            _releasedTotal == _wave.TotalUnits && _creepCount == 0 && _projectileCount == 0;

        /// <summary>
        /// The rolling hash of internal simulation state as of the current tick.
        /// Always computed. See the remarks on <see cref="Match"/> for why there
        /// is no way to switch it off.
        /// </summary>
        public Hash64 StateHash => _stateHash;

        /// <summary>How many creeps have reached the exit so far.</summary>
        public int Leaked => _leaked;

        /// <summary>
        /// The same leaks, split by the wave order that sent them. This is the
        /// half of a leak a count cannot carry: a leak is charged at the price
        /// of what leaked, and the order is what says which type that was.
        /// </summary>
        public IReadOnlyList<int> LeakedByOrder => _leakedByOrder;

        /// <summary>
        /// The leaks nobody sent, split by the wave order whose units raised
        /// them. What one of these is worth is the price of the row that order's
        /// type raises, which is the other half of pricing a leak that arrived
        /// out of a body rather than out of a purse.
        /// </summary>
        public IReadOnlyList<int> LeakedRaisedByOrder => _leakedRaisedByOrder;

        /// <summary>How many creeps have been killed so far.</summary>
        public int Killed => _killed;

        /// <summary>
        /// Where each tower can reach: the intervals this match acquires
        /// through, rather than a second table built to agree with them. A
        /// caller checking whether the defense makes sense, or whether a tower
        /// shot the creep the rule says it should have, is then holding the
        /// answer against the table that produced it.
        /// </summary>
        public TowerCoverage Coverage => _coverage;

        /// <summary>
        /// Runs the match forward. The one call every usage scenario is made of:
        /// pass one tick for playback, several for fast-forward, a large number
        /// for instant-resolve, and an event sink or nothing depending on
        /// whether anybody wants to hear about it.
        /// </summary>
        /// <param name="ticks">How many ticks to run. Stops early if the match ends.</param>
        /// <param name="events">Where to send events, or null to emit none at all.</param>
        /// <returns>How many ticks actually ran.</returns>
        public int Advance(int ticks, IMatchEvents? events = null)
        {
            if (ticks < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(ticks),
                    "A match runs forwards. Going back to a tick is re-simulating from the beginning, "
                    + "which is a new match rather than a negative number of ticks -- and that is on "
                    + "purpose, because it makes every seek a fresh determinism check.");
            }

            int ran = 0;

            while (ran < ticks && !IsFinished)
            {
                Step(events);
                ran++;
            }

            return ran;
        }

        /// <summary>
        /// Runs to the end of the match and returns what happened. Exactly
        /// <see cref="Advance(int, IMatchEvents)"/> with a number nothing can
        /// reach, spelled out because "resolve this instantly" is a thing people
        /// ask for by name.
        /// </summary>
        public MatchResult Resolve(IMatchEvents? events = null)
        {
            Advance(int.MaxValue, events);
            return Result();
        }

        /// <summary>
        /// What the match was. Only available once it is over: a result for a
        /// match still in progress would be a number that looks like an outcome
        /// and is not one.
        /// </summary>
        public MatchResult Result()
        {
            if (!IsFinished)
            {
                throw new SimulationException(
                    "The match is not over at tick "
                    + Tick.ToString(CultureInfo.InvariantCulture)
                    + ": "
                    + (_wave.TotalUnits - _releasedTotal).ToString(CultureInfo.InvariantCulture)
                    + " creeps are still to be released, "
                    + _creepCount.ToString(CultureInfo.InvariantCulture)
                    + " are on the map and "
                    + _projectileCount.ToString(CultureInfo.InvariantCulture)
                    + " projectiles are in the air. A result for a match in progress is a number that "
                    + "looks like an outcome and is not one.");
            }

            return new MatchResult(_leaked, _wave.TotalUnits, Tick, _stateHash);
        }

        /// <summary>
        /// Builds a picture of everything that moves, as of now. Nothing else in
        /// the simulation calls this, which is what makes a headless run free:
        /// no caller, no snapshot, no allocation.
        /// </summary>
        public Snapshot PullSnapshot()
        {
            var creeps = new CreepSnapshot[_creepCount];

            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                creeps[index] = new CreepSnapshot(
                    creep.Id,
                    creep.Type.Id,
                    creep.Distance,
                    creep.Lateral,
                    creep.Hp,
                    ShieldOf(ref creep),
                    creep.Phase == CreepPhase.Dying ? CreepState.Dying : CreepState.Walking,
                    creep.TicksInState,
                    creep.Effects.SpeedMagnitude,
                    creep.Effects.ArmourMagnitude);
            }

            var towers = new TowerSnapshot[_towers.Length];

            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];

                towers[index] = new TowerSnapshot(
                    tower.Id,
                    tower.State,
                    tower.TargetId,
                    tower.TicksInState,
                    tower.Effects.CooldownMagnitude);
            }

            var projectiles = new ProjectileSnapshot[_projectileCount];

            for (int index = 0; index < _projectileCount; index++)
            {
                ref Projectile projectile = ref _projectiles[index];

                projectiles[index] = new ProjectileSnapshot(
                    projectile.Id,
                    projectile.Type.Id,
                    projectile.Target,
                    projectile.TicksInFlight,
                    projectile.FlightDurationTicks);
            }

            return new Snapshot(Tick, creeps, towers, projectiles);
        }

        /// <summary>
        /// Everything standing in front of one creep's health: the pool its row
        /// authored and whatever a shield payload granted it.
        /// </summary>
        /// <remarks>
        /// Saturating, because both halves are bounded only by the ranges they
        /// come out of -- an authored column and a percentage of a health pool
        /// -- and a wrapped sum is a full shield reported as a negative one.
        /// The two are added only here: <see cref="Absorbed"/> spends them in
        /// order and the fold folds them apart, so nothing that decides
        /// anything reads them as one number.
        /// </remarks>
        private static int ShieldOf(ref Creep creep)
        {
            long pool = (long)creep.Shield + creep.Effects.GrantedShield;

            return pool > int.MaxValue ? int.MaxValue : (int)pool;
        }

        /// <summary>One tick. The order of these phases is part of the rules.</summary>
        private void Step(IMatchEvents? events)
        {
            ExpireEffects();
            MoveCreeps(events);
            ReportPasses(events);
            AgeDyingCreeps();
            FlyProjectiles(events);
            RunTowers(events);
            PulseAuras(events);
            ClearAwayTheGone();

            Tick++;

            if (Tick >= TickCeiling)
            {
                throw new SimulationException(
                    "The match has run "
                    + Tick.ToString(CultureInfo.InvariantCulture)
                    + " ticks without ending. A match that has not ended by here is never going to, so "
                    + "this is a rule that is wrong rather than a wave that is long.");
            }

            Raise(events);
            Release(Tick);
            Fold();
        }

        /// <summary>
        /// Releases whatever the wave is due to send on a tick. One unit per
        /// order per <see cref="SpawnIntervalTicks"/>, so a count is a column.
        /// </summary>
        private void Release(int tick)
        {
            for (int index = 0; index < _wave.Count; index++)
            {
                UnitOrder order = _wave.Orders[index];

                if (tick < order.TickOffset || _released[index] >= order.Count)
                {
                    continue;
                }

                if ((tick - order.TickOffset) % SpawnIntervalTicks != 0)
                {
                    continue;
                }

                Spawn(index, order.Type, Fix64.Zero, _stepPerTick[index], raised: false);
                _released[index]++;
                _releasedTotal++;
            }
        }

        /// <summary>
        /// Puts one body on the corridor and hands back its entity id.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The one place a creep starts existing, whichever put it there.</b>
        /// A wave release and a raise differ in three arguments -- where it
        /// starts, what it walks at, and whether anybody paid for it -- and in
        /// nothing else, so a body raised mid-lane is the same kind of thing as
        /// a body released at the mouth and every rule that reads a creep reads
        /// them alike.
        /// </para>
        /// <para>
        /// <b>It appends, and the array stays in ascending id order.</b> Ids are
        /// handed out in the order bodies arrive, so a raised body sits behind
        /// everything already standing -- which is the order acquisition walks,
        /// the order the fold folds, and the order the target-selection tiebreak
        /// settles on. A raised body therefore loses every tie it is in.
        /// </para>
        /// </remarks>
        private int Spawn(int orderIndex, UnitType type, Fix64 distance, Fix64 step, bool raised)
        {
            if (_creepCount == _creeps.Length)
            {
                Grow(ref _creeps);

                // The candidate buffer follows the array it is refilled from. A
                // raised body is in no order, so the wave's total is no longer
                // what bounds how many creeps one acquisition can see.
                if (_reachable.Length < _creeps.Length)
                {
                    _reachable = new WalkingTarget[_creeps.Length];
                }
            }

            ref Creep creep = ref _creeps[_creepCount];

            creep.Id = _nextEntityId++;
            creep.Type = type;
            creep.OrderIndex = orderIndex;
            creep.Raised = raised;
            creep.Distance = distance;
            creep.Lateral = _lateralOffsets[_spawnOrdinal % _lateralOffsets.Length];
            creep.Hp = type.MaxHp;

            // What it walks at, worked out by whoever put it here. Nothing is on
            // it yet, so this is the authored step exactly -- and every later
            // value of it is one fused expression evaluated where the modifier
            // moved, never the truncated number here multiplied a second time.
            creep.Step = step;
            creep.Effects = default;
            creep.PulseIn = 0;
            creep.RaiseIn = FirstRaiseIn(type);

            // The pool a row authored, and the whole of where one comes from
            // today: nothing grants a shield yet, and nothing regenerates one,
            // so a creep spawns with what its row says and spends it.
            creep.Shield = type.Shield;
            creep.Phase = CreepPhase.Walking;
            creep.TicksInState = 0;

            _creepCount++;
            _spawnOrdinal++;

            _spawnFold = _spawnFold
                .Add(creep.Id)
                .Add(type.Id)
                .Add(creep.Lateral.Raw)
                .Add(creep.Hp)
                .Add(raised ? 1 : 0);

            return creep.Id;
        }

        /// <summary>
        /// How long a body of this row waits before its first raise: a whole
        /// period, and nothing at all for a row that raises nothing.
        /// </summary>
        /// <remarks>
        /// <b>A full period, where an aura's counter starts at zero.</b> A pulse
        /// grants something to bodies that are already there and costs a body
        /// nothing to have arrived; a raise puts another body on the board, so
        /// firing it on the tick the raiser arrives would mean a spawner that
        /// shows up already accompanied. <c>docs/roster.md</c> signs "every 150
        /// ticks", and 150 ticks after it arrives is what that says. The counter
        /// is one short of the period because the tick it reaches zero on is the
        /// tick it fires on.
        /// </remarks>
        private static int FirstRaiseIn(UnitType type) =>
            type.Raises is null ? 0 : type.RaisePeriodTicks - 1;

        /// <summary>
        /// Raises whatever is due to raise, at the close of the tick the wave
        /// releases on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Beside the wave's own release, because it is one.</b> A raise puts
        /// a body on the corridor, which is what the wave does, so it happens
        /// where that happens: after everything on the board has moved, shot and
        /// been cleared away, and before the fold. A body raised on tick
        /// <c>t</c> is therefore in the picture of tick <c>t</c>, takes its first
        /// step on <c>t + 1</c>, and can be shot at from <c>t + 1</c> -- exactly
        /// as one the wave released on the same tick.
        /// </para>
        /// <para>
        /// <b>Immediately before that release rather than after it, so a body
        /// does not spend a tick of its cadence on the tick it arrived.</b> This
        /// walks what is already standing; a body the wave puts down a line
        /// later is therefore first counted down on the tick after it arrives,
        /// which is what makes the wait a whole period for every body rather
        /// than a whole one for the column's first and one short for the rest.
        /// </para>
        /// <para>
        /// <b>Only a walking body raises.</b> The dead are cleared away one
        /// phase earlier and a dying one is still here and skipped, so killing a
        /// spawner stops the raises on the tick it dies rather than at the end
        /// of its corpse.
        /// </para>
        /// <para>
        /// <b>The count is taken before the loop and the array is read through
        /// its index.</b> A body raised in here joins the array behind the ones
        /// being walked and does not raise on the tick it arrived; and the array
        /// can be reallocated by a spawn, so holding a <c>ref</c> across one
        /// would write the counter into an array nothing reads afterwards.
        /// </para>
        /// <para>
        /// <b>No die is rolled.</b> Where the body goes is the raiser's own
        /// distance and the next lateral offset in the cycle, both determined --
        /// so the position of the dice stream is untouched by a raise, and the
        /// stream stays a running count of the shots fired.
        /// </para>
        /// </remarks>
        private void Raise(IMatchEvents? events)
        {
            if (!_walkersRaise)
            {
                return;
            }

            int standing = _creepCount;

            for (int index = 0; index < standing; index++)
            {
                UnitType type = _creeps[index].Type;

                if (_creeps[index].Phase != CreepPhase.Walking || type.Raises is not UnitType raised)
                {
                    continue;
                }

                if (_creeps[index].RaiseIn > 0)
                {
                    _creeps[index].RaiseIn--;
                    continue;
                }

                _creeps[index].RaiseIn = type.RaisePeriodTicks - 1;

                int raiser = _creeps[index].Id;
                int body = Spawn(
                    _creeps[index].OrderIndex,
                    raised,
                    _creeps[index].Distance,
                    StepUnder(raised, 0),
                    raised: true);

                events?.CreepRaised(raiser, body);
            }
        }

        private void MoveCreeps(IMatchEvents? events)
        {
            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                if (creep.Phase != CreepPhase.Walking)
                {
                    continue;
                }

                creep.Distance += creep.Step;
                creep.TicksInState++;

                if (creep.Distance >= _routeLength)
                {
                    creep.Phase = CreepPhase.Gone;
                    _leaked++;

                    // Which of the two counts it lands in is what says how it is
                    // priced: the order's own type for a body somebody sent, and
                    // the row that order raises for a body nobody did.
                    if (creep.Raised)
                    {
                        _leakedRaised++;
                        _leakedRaisedByOrder[creep.OrderIndex]++;
                    }
                    else
                    {
                        _leakedByOrder[creep.OrderIndex]++;
                    }

                    events?.CreepLeaked(creep.Id);
                }
            }
        }

        /// <summary>
        /// Tells a listener about every pass that completed on this tick: a
        /// creep that is now further along the corridor than one which spawned
        /// before it, and was not a tick ago.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It reports and decides nothing.</b> No field is written, no die is
        /// rolled and the rolling state hash cannot tell this ran, which is why
        /// it can sit between two phases whose order is part of the rules
        /// without being part of them. A match nobody is listening to leaves on
        /// the first line.
        /// </para>
        /// <para>
        /// <b>Where a creep was a tick ago is arithmetic, not memory.</b> A
        /// walking creep moved exactly one step this tick and a dying one did
        /// not move at all, so the previous distance is the current one less
        /// that step -- which is what makes this fire on the tick the order
        /// flipped rather than on every tick the two stay flipped. No previous
        /// tick is kept anywhere, and there is nothing to keep in step.
        /// </para>
        /// <para>
        /// Creeps are held in ascending id order, so the outer index is always
        /// the later-spawned of the pair. Anything that stopped existing this
        /// tick is skipped: a creep that reached the exit has left the corridor
        /// rather than been passed on it.
        /// </para>
        /// </remarks>
        private void ReportPasses(IMatchEvents? events)
        {
            if (events is null)
            {
                return;
            }

            for (int index = 1; index < _creepCount; index++)
            {
                ref Creep passer = ref _creeps[index];

                if (passer.Phase == CreepPhase.Gone)
                {
                    continue;
                }

                Fix64 passerWas = passer.Distance - StepThisTick(ref passer);

                for (int other = 0; other < index; other++)
                {
                    ref Creep passed = ref _creeps[other];

                    if (passed.Phase == CreepPhase.Gone)
                    {
                        continue;
                    }

                    if (passer.Distance <= passed.Distance)
                    {
                        continue;
                    }

                    if (passerWas <= passed.Distance - StepThisTick(ref passed))
                    {
                        events.CreepOvertook(passer.Id, passed.Id);
                    }
                }
            }
        }

        /// <summary>How far a creep moved on the tick just run. Nothing if it is not walking.</summary>
        /// <remarks>
        /// <b>The creep's own step and not its order's.</b> A modifier is per
        /// unit, so two creeps released by one order can be walking at
        /// different speeds -- and this is where "where it was a tick ago" is
        /// worked out, so reading the order's step would silently mis-report
        /// every overtake involving a creep anything had landed on. Nothing
        /// between <see cref="MoveCreeps"/> and here moves a step: expiry opens
        /// the tick and every emitter runs after both.
        /// </remarks>
        private static Fix64 StepThisTick(ref Creep creep) =>
            creep.Phase == CreepPhase.Walking ? creep.Step : Fix64.Zero;

        /// <summary>
        /// Clears every modifier that has run out, and puts whatever it was on
        /// back where the row it came from says it should be.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It opens the tick, which is what makes a duration mean one
        /// thing.</b> An effect is stored with the last tick it is in force on
        /// rather than with a countdown, so one that landed on tick <c>t</c>
        /// with a duration of <c>n</c> is cleared at the top of tick
        /// <c>t + n + 1</c> and has therefore been on for exactly <c>n</c>
        /// ticks of walking, shooting and being shot -- whichever phase of tick
        /// <c>t</c> emitted it.
        /// </para>
        /// <para>
        /// <b>The step is recomputed here and only here, and only when the
        /// speed slot actually moved.</b> That is the other half of truncating
        /// once: the conversion into Q32.32 is a division, and doing it per
        /// tick would be spending the re-simulation budget re-deriving a number
        /// that changes a handful of times in a match.
        /// </para>
        /// </remarks>
        private void ExpireEffects()
        {
            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                if (creep.Effects.Any && creep.Effects.Expire(Tick))
                {
                    creep.Step = StepUnder(creep.Type, creep.Effects.SpeedMagnitude);
                }
            }

            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];

                if (tower.Effects.Any)
                {
                    tower.Effects.Expire(Tick);
                }
            }
        }

        /// <summary>
        /// Fires every bubble that is due to pulse on its own clock.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>An aura is a bubble with a period and nothing else</b>, so this
        /// phase emits exactly what a shot emits and differs only in what set
        /// the clock. The counter is per emitter and counts down, so a tower
        /// that has stood since tick zero pulses on ticks 0, <c>period</c>,
        /// <c>2 * period</c> and so on, and a creep pulses on the same cadence
        /// measured from the tick it spawned -- which is a fact about the unit
        /// rather than about the wall clock, and is therefore the same in every
        /// replay of the same record.
        /// </para>
        /// <para>
        /// <b>It runs after the towers, so an aura never acts on the tick it
        /// landed.</b> Together with expiry opening the tick, that is what
        /// makes a duration mean exactly the ticks after the one it was emitted
        /// on.
        /// </para>
        /// <para>
        /// <b>This is where creep positions come back into the tick loop, and
        /// it is paid for knowingly.</b> A radius is measured in hexes so that
        /// it reaches the neighbouring leg of a fold rather than only the
        /// creeps behind it in the column -- route distance was the free
        /// alternative and was not taken -- so a pulse turns the one dimension
        /// the loop keeps back into two. It is a table lookup per body rather
        /// than a search, and it is amortised against the period: a tower
        /// pulsing every second costs it once in thirty ticks and never on the
        /// other twenty-nine. <see cref="TowerCoverage"/> is untouched, and
        /// range is still intersected with the route at load.
        /// </para>
        /// </remarks>
        private void PulseAuras(IMatchEvents? events)
        {
            if (_towersPulse)
            {
                for (int index = 0; index < _towers.Length; index++)
                {
                    ref Tower tower = ref _towers[index];
                    UnitType type = _layout.Towers[index].Type;

                    if (!type.Bubble.IsAnAura)
                    {
                        continue;
                    }

                    if (tower.PulseIn > 0)
                    {
                        tower.PulseIn--;
                        continue;
                    }

                    tower.PulseIn = type.Bubble.PeriodTicks - 1;
                    events?.AuraPulsed(tower.Id, type.Bubble.RadiusMilliHex, type.Bubble.Payload);

                    // The pulse is announced and the spread is silent. A pulse
                    // has no roll to spread -- the draw belongs to an attack
                    // and an aura is not one -- so the only thing a sink would
                    // hear from in there is a landing that cannot happen.
                    Spread(type, _layout.Towers[index].Hex, roll: 0, events: null);
                }
            }

            if (!_walkersPulse)
            {
                return;
            }

            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                if (creep.Phase != CreepPhase.Walking || !creep.Type.Bubble.IsAnAura)
                {
                    continue;
                }

                if (creep.PulseIn > 0)
                {
                    creep.PulseIn--;
                    continue;
                }

                creep.PulseIn = creep.Type.Bubble.PeriodTicks - 1;
                events?.AuraPulsed(creep.Id, creep.Type.Bubble.RadiusMilliHex, creep.Type.Bubble.Payload);
                Spread(creep.Type, CellUnder(creep.Distance), roll: 0, events: null);
            }
        }

        private void AgeDyingCreeps()
        {
            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                if (creep.Phase != CreepPhase.Dying)
                {
                    continue;
                }

                creep.TicksInState++;

                if (creep.TicksInState >= creep.Type.DyingTicks)
                {
                    creep.Phase = CreepPhase.Gone;
                }
            }
        }

        /// <summary>
        /// Flies every projectile one tick, and lands the ones that arrive.
        /// </summary>
        /// <remarks>
        /// The hardest case in the sim-to-view contract -- a projectile whose
        /// target dies mid-flight -- is the first three lines, and it needed no
        /// special handling at all: the target lookup that would have found
        /// somebody to damage does not find them, so the projectile stops
        /// existing and therefore stops appearing in the snapshot. There is no
        /// path by which it can linger, because there is no state it could
        /// linger in.
        /// </remarks>
        private void FlyProjectiles(IMatchEvents? events)
        {
            for (int index = 0; index < _projectileCount; index++)
            {
                ref Projectile projectile = ref _projectiles[index];
                int target = FindWalkingCreep(projectile.Target);

                if (target < 0)
                {
                    projectile.Gone = true;
                    events?.ProjectileOrphaned(projectile.Id);
                    continue;
                }

                projectile.TicksInFlight++;

                if (projectile.TicksInFlight >= projectile.FlightDurationTicks)
                {
                    // The creep the lookup above already found, handed on rather
                    // than looked up a second time: an arriving projectile has a
                    // live target by construction, because one without is
                    // orphaned three lines up.
                    Land(
                        projectile.Type,
                        projectile.Origin,
                        projectile.EmitterId,
                        target,
                        projectile.Damage,
                        events);
                    projectile.Gone = true;
                }
            }
        }

        private void RunTowers(IMatchEvents? events)
        {
            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];
                UnitType type = _layout.Towers[index].Type;

                switch (tower.State)
                {
                    case TowerState.Idle:
                        tower.TicksInState++;

                        if (tower.Cooldown > 0)
                        {
                            tower.Cooldown--;
                            break;
                        }

                        int acquired = Acquire(index, type);

                        if (acquired == 0)
                        {
                            break;
                        }

                        tower.TargetCount = acquired;
                        tower.TargetId = TargetOf(index, 0);
                        tower.State = TowerState.Windup;
                        tower.TicksInState = 0;

                        if (type.WindupTicks == 0)
                        {
                            Fire(index, ref tower, type, events);
                        }

                        break;

                    case TowerState.Windup:
                        tower.TicksInState++;

                        if (tower.TicksInState >= type.WindupTicks)
                        {
                            Fire(index, ref tower, type, events);
                        }

                        break;

                    case TowerState.Backswing:
                        tower.TicksInState++;

                        if (tower.TicksInState >= type.BackswingTicks)
                        {
                            GoIdle(ref tower, type);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// What a tower shoots at: its shots' worth of creeps, written into its
        /// own slice of <see cref="_towerTargets"/> nearest the exit first, and
        /// how many it found.
        /// </summary>
        /// <remarks>
        /// The rule is <see cref="Targeting.Chosen(ReadOnlySpan{WalkingTarget}, Span{int}, out int)"/>
        /// and none of it is spelled here. This is the projection onto it: walk
        /// the creeps once, keep the walking ones this tower's coverage reaches,
        /// hand them over in the order the array is kept in -- which is
        /// ascending id -- and add the ties it broke to the running count the
        /// state hash folds. A row firing one shot asks for one answer, which is
        /// the same call with a narrower span.
        /// </remarks>
        private int Acquire(int tower, UnitType type)
        {
            int reachable = 0;

            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                if (creep.Phase != CreepPhase.Walking || !_coverage.Covers(tower, creep.Distance))
                {
                    continue;
                }

                _reachable[reachable] = new WalkingTarget(creep.Id, creep.Distance);
                reachable++;
            }

            var chosen = new Span<int>(_towerTargets, tower * _targetsPerTower, type.Targets);

            int found = Targeting.Chosen(
                new ReadOnlySpan<WalkingTarget>(_reachable, 0, reachable),
                chosen,
                out int tiebreaks);

            _tiebreaksBroken += tiebreaks;

            // The slice holds indices into the candidate span; what a tower
            // keeps is entity ids, because a candidate's position in a span is
            // gone by the time the shot is released.
            for (int index = 0; index < found; index++)
            {
                chosen[index] = _reachable[chosen[index]].Id;
            }

            return found;
        }

        /// <summary>One of a tower's acquired targets, by entity id.</summary>
        private int TargetOf(int tower, int shot) => _towerTargets[(tower * _targetsPerTower) + shot];

        /// <summary>
        /// Releases the shots a tower committed to when it started winding up:
        /// one per target it acquired, each with its own draw.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The shots happen whatever became of the targets.</b> A tower that
        /// commits and then finds its target dead still fires, still rolls, and
        /// still wastes the shot -- which is what makes overkill real: two towers
        /// covering one stretch of corridor can both commit to the same creep,
        /// and the second one's damage lands on something already dying and is
        /// discarded. Re-checking the targets here would quietly make that
        /// impossible, and with it the whole reason the ranges were made to
        /// overlap.
        /// </para>
        /// <para>
        /// <b>n targets is n draws, in acquisition order.</b> That is the half
        /// of the determinism contract this loop keeps: the dice stream's
        /// position is folded every tick, so how many numbers an attack takes
        /// off it and in what order is part of what every stored record replays
        /// through. The other half is the bubble, which is one shot and one draw
        /// however many bodies it lands on -- and a row cannot be both, which is
        /// settled where the columns are read.
        /// </para>
        /// </remarks>
        private void Fire(int tower, ref Tower state, UnitType type, IMatchEvents? events)
        {
            for (int shot = 0; shot < state.TargetCount; shot++)
            {
                ReleaseShot(tower, type, TargetOf(tower, shot), events);
            }

            state.State = TowerState.Backswing;
            state.TicksInState = 0;

            if (type.BackswingTicks == 0)
            {
                GoIdle(ref state, type);
            }
        }

        /// <summary>
        /// One shot of one attack: one draw, delivered the way this row delivers
        /// damage.
        /// </summary>
        private void ReleaseShot(int tower, UnitType type, int targetId, IMatchEvents? events)
        {
            // The one and only draw. Once per shot, on the one stream, whether
            // or not the shot is going to land on anything.
            int damage = _dice.NextInRange(type.DamageMin, type.DamageMax + 1);
            _shotsFired++;

            events?.TowerFired(_towers[tower].Id, targetId);

            switch (type.Delivery)
            {
                case Delivery.Hitscan:
                    // No snapshot entity of any kind: a hitscan shot exists as an
                    // event and as whatever the view draws and forgets, and
                    // nothing else.
                    Land(
                        type,
                        _layout.Towers[tower].Hex,
                        _towers[tower].Id,
                        FindWalkingCreep(TargetRef.Creep(targetId)),
                        damage,
                        events);
                    break;

                case Delivery.Projectile:
                    Launch(type, _layout.Towers[tower].Hex, _towers[tower].Id, targetId, damage);
                    break;

                case Delivery.None:
                    throw new SimulationException(
                        "Tower "
                        + _towers[tower].Id.ToString(CultureInfo.InvariantCulture)
                        + " is "
                        + type.ToString()
                        + ", which delivers no damage, and it has fired. A unit that cannot attack should "
                        + "never have acquired a target.");
            }
        }

        /// <summary>
        /// Where a shot's damage actually goes: onto the creep it was aimed at,
        /// or -- where the row carries a bubble -- onto everything the bubble
        /// encloses, at the same roll.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One roll, applied whole to every body, with no falloff and no
        /// friendly fire.</b> The roll was drawn once when the shot was
        /// released; a bubble spreads it rather than re-rolling it, which is
        /// exactly what makes a sweep a different shape from a volley and not a
        /// cheaper spelling of one.
        /// </para>
        /// <para>
        /// <b>A blast centred on a target that is no longer there lands on
        /// nothing.</b> That is the same rule a single shot at a dead creep
        /// follows and it is not a special case: the centre of the bubble is a
        /// fact about where the shot arrived, and a shot that arrived nowhere
        /// has none. A sweep centred on the tower has one whatever happened to
        /// the creep that provoked it, because a tower is still standing where
        /// it stands.
        /// </para>
        /// <para>
        /// <b>A bubble of no radius is the target alone</b>, and that is
        /// answered here rather than by the sphere. A radius column spells its
        /// absence as the word <c>none</c>, so zero in it is an authoring --
        /// the Cryomancer's single-target slow -- where zero in a range column
        /// is no reach at all. <see cref="Reach.Encloses"/> answers the range
        /// column's question and would say this bubble reaches nothing
        /// whatever, which is why <see cref="Bubble.ReachesOnlyItsCentre"/> is
        /// asked first.
        /// </para>
        /// </remarks>
        /// <param name="emitterId">The tower that fired it, which is what a self-centred bubble is centred on.</param>
        /// <param name="target">Where the creep it was aimed at is in the live array, or -1 for nothing.</param>
        private void Land(UnitType type, Hex origin, int emitterId, int target, int roll, IMatchEvents? events)
        {
            Bubble bubble = type.Bubble;

            // A damage bubble fired with the attack is the one shape that
            // replaces the single landing, because it IS the roll spread over
            // everything it encloses. Every other row lands its shot where it
            // was aimed exactly as an unadorned shot does -- a row with no
            // bubble, a bubble carrying a stat, and a bubble on a clock of its
            // own, which is not part of this shot at all.
            if (!bubble.FiresWithTheAttack || bubble.Payload != BubblePayload.Damage)
            {
                Damage(target, roll, type, events);
            }

            if (!bubble.FiresWithTheAttack)
            {
                return;
            }

            // A bubble centred on a body that is no longer there lands on
            // nothing and says nothing -- the same rule a single shot at a dead
            // creep follows, and not a special case: the centre is a fact about
            // where the shot arrived, and a shot that arrived nowhere has none.
            // A self-centred bubble always has one, because a tower is still
            // standing where it stands.
            if (bubble.Origin == BubbleOrigin.Target && target < 0)
            {
                return;
            }

            // Said out loud before it is resolved, as the entity it is centred
            // on rather than as the cell: the tower for a sweep, the body the
            // shot arrived at for a blast. A radius of zero is announced like
            // any other, because which radii are worth drawing is a question
            // for whatever is listening and not a shape the match has.
            events?.BlastLanded(
                bubble.Origin == BubbleOrigin.Self ? emitterId : _creeps[target].Id,
                bubble.RadiusMilliHex,
                bubble.Payload);

            // A bubble of no radius is the one body the shot landed on, and
            // that is what makes it different from a sphere of no size: two
            // creeps can stand on the same hex, so "the cell the shot arrived
            // at" would reach both of them. The sphere is never asked -- it
            // answers false at no radius, deliberately and for the range
            // column's sake.
            if (bubble.ReachesOnlyItsCentre)
            {
                if (bubble.Payload == BubblePayload.Damage)
                {
                    Damage(target, roll, type, events);
                }
                else if (target >= 0)
                {
                    Afflict(target, bubble);
                }

                return;
            }

            Spread(type, CentreOf(bubble, origin, target), roll, events);
        }

        /// <summary>
        /// Where a bubble fired with an attack is centred: on the shooter, or
        /// on the cell the shot arrived at.
        /// </summary>
        /// <remarks>
        /// A blast centred on a target that is no longer there has no centre at
        /// all, and the caller settles that before asking -- so a target-centred
        /// bubble reaching here has a live one. A self-centred bubble always
        /// does, because a tower is still standing where it stands.
        /// </remarks>
        private Hex CentreOf(Bubble bubble, Hex origin, int target) =>
            bubble.Origin == BubbleOrigin.Self ? origin : CellUnder(_creeps[target].Distance);

        /// <summary>
        /// Puts what a bubble carries on everything of the right side that its
        /// sphere encloses: a damage roll, or a timed modifier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One walk, whichever clock fired it and whatever it carries.</b> A
        /// bubble that goes off with an attack, one that pulses on a period,
        /// one spreading a roll and one putting a magnitude on a stat are the
        /// same mechanic and differ only in what set them off and what they
        /// hand over -- so they all spread through this and there is no second
        /// copy of "who is inside" to disagree with the first.
        /// </para>
        /// <para>
        /// <b>Which side it reaches is a relationship</b>, exactly as a height
        /// is: a tower's enemy is what walks and a walker's enemy is what
        /// stands. <see cref="Bubble.ReachesInto"/> answers it from the
        /// emitter's own role rather than from an argument beside it, so a
        /// caller cannot hand it a side the row disagrees with -- and the same
        /// call is made at load, where a payload the other side has no use for
        /// is refused.
        /// </para>
        /// <para>
        /// <b>The firing was said out loud and the landing is not.</b> Whoever
        /// set this off announced it -- a blast where the shot resolved, an
        /// aura where its period came round -- and what a modifier then does to
        /// one body is not reported: a stat arriving on a unit is that unit's
        /// own picture rather than a moment. The roll a blast spreads goes out
        /// through <see cref="Damage"/> as any other landing does, and a pulse
        /// has no roll to spread. What a slow does is in the state hash, where
        /// a run that drifts in it is caught. See
        /// <c>docs/adr/0008-match-events-are-decorative.md</c>.
        /// </para>
        /// </remarks>
        /// <param name="roll">What the dice gave the shot that fired it, and nothing to a pulse.</param>
        private void Spread(UnitType emitter, Hex centre, int roll, IMatchEvents? events)
        {
            Bubble bubble = emitter.Bubble;
            bool spreadsTheRoll = bubble.Payload == BubblePayload.Damage;
            int level = Map.LevelAt(centre);
            int radius = bubble.RadiusMilliHex;

            if (bubble.ReachesInto(emitter.Role) == UnitRole.Placed)
            {
                // Nothing in this loop damages a tower, so a roll pointed at
                // the standing side lands on nothing at all. That is the same
                // silence a pool or an armour granted to one meets, and it is a
                // fact about the rows in content/units.txt -- every placed one
                // of which authors no health pool -- rather than about what a
                // placed unit is.
                if (spreadsTheRoll)
                {
                    return;
                }

                for (int tower = 0; tower < _towers.Length; tower++)
                {
                    Hex hex = _layout.Towers[tower].Hex;

                    if (Reach.Encloses(centre, level, radius, hex, Map.LevelAt(hex)))
                    {
                        _towers[tower].Effects.Land(bubble, Tick, _layout.Towers[tower].Type.MaxHp);
                    }
                }

                return;
            }

            for (int creep = 0; creep < _creepCount; creep++)
            {
                if (_creeps[creep].Phase != CreepPhase.Walking)
                {
                    continue;
                }

                Hex cell = CellUnder(_creeps[creep].Distance);

                if (!Reach.Encloses(centre, level, radius, cell, Map.LevelAt(cell)))
                {
                    continue;
                }

                if (spreadsTheRoll)
                {
                    Damage(creep, roll, emitter, events);
                }
                else
                {
                    Afflict(creep, bubble);
                }
            }
        }

        /// <summary>
        /// Lands one bubble's payload on one walking creep, and puts its step
        /// back together if that moved its speed.
        /// </summary>
        private void Afflict(int creep, Bubble bubble)
        {
            ref Creep body = ref _creeps[creep];

            if (body.Phase != CreepPhase.Walking)
            {
                return;
            }

            bool speedMoved = body.Effects.Land(bubble, Tick, body.Type.MaxHp);

            if (speedMoved)
            {
                body.Step = StepUnder(body.Type, body.Effects.SpeedMagnitude);
            }
        }

        /// <summary>
        /// How far a unit of this row walks in a tick with that modifier on it.
        /// </summary>
        /// <remarks>
        /// <b>One truncation, not two.</b> The percentage is applied to the
        /// authored milli-hexes as one integer expression and the result is
        /// converted into Q32.32 once -- which is a different function from
        /// multiplying the already-truncated step by a fixed-point percentage,
        /// and the same hazard <see cref="DamageModel"/>'s remarks name for a
        /// stat pipeline. It is evaluated where the modifier moved rather than
        /// per tick, so the division happens a handful of times in a match
        /// instead of once per creep per tick.
        /// </remarks>
        private static Fix64 StepUnder(UnitType type, int magnitude) =>
            Fix64.FromRatio(
                Effects.ModifiedSpeed(type.SpeedMilliHexPerTick, magnitude),
                MilliHexPerHex);

        /// <summary>
        /// The route cell a creep is standing on, which is what a bubble
        /// measures its sphere against.
        /// </summary>
        /// <remarks>
        /// Distance along the route is the only position a creep has, and a hex
        /// is what a radius is measured in -- so this is where the one dimension
        /// the tick loop keeps is turned back into two, at the moment a bubble
        /// goes off and nowhere else. <see cref="TowerCoverage"/> is still where
        /// range lives, and it is still evaluated at load: a bubble costs a
        /// walk over the creeps that are actually on the map, on the ticks a
        /// bubble actually fires.
        /// </remarks>
        private Hex CellUnder(Fix64 distance)
        {
            int step = distance.ToIntFloor();

            if (step < 0 || step >= Map.Route.Count)
            {
                throw new SimulationException(
                    "A walking creep is "
                    + distance.ToString()
                    + " hexes along a route of "
                    + Map.Route.Count.ToString(CultureInfo.InvariantCulture)
                    + " cells. A creep past the end of the route has left the map and is not walking.");
            }

            return Map.Route[step];
        }

        /// <summary>
        /// Puts a tower back to idle and starts its wait. The wait is the one
        /// authored on the row, displaced by whatever is on the tower.
        /// </summary>
        /// <remarks>
        /// <b>Read where the counter is set rather than where it is spent.</b> A
        /// rally that lands halfway through a wait does not shorten the wait it
        /// is already in; it shortens the next one. That is the same rule the
        /// windup and the backswing follow -- a tower commits to a timing when
        /// it enters a state -- and it is the reason a cooldown modifier needs
        /// no cached value the way a walking speed does.
        /// </remarks>
        private static void GoIdle(ref Tower tower, UnitType type)
        {
            tower.State = TowerState.Idle;
            tower.TicksInState = 0;
            tower.TargetId = 0;
            tower.TargetCount = 0;
            tower.Cooldown = Effects.Modified(type.CooldownTicks, tower.Effects.CooldownMagnitude);
        }

        private void Launch(UnitType type, Hex origin, int emitterId, int targetId, int damage)
        {
            if (_projectileCount == _projectiles.Length)
            {
                Grow(ref _projectiles);
            }

            ref Projectile projectile = ref _projectiles[_projectileCount];

            projectile.Id = _nextEntityId++;
            projectile.Type = type;

            // Where it was fired from, which a bubble centred on the shooter
            // needs when the shot lands a second and a bit later. Not a position
            // the projectile has and not one anything draws: a tower does not
            // move, so this is the same fact as the tower's own hex, carried the
            // way the row that fired it is carried.
            projectile.Origin = origin;

            // Who fired it, which is the same fact again as an entity rather
            // than as a cell -- what a sweep's own event names, because a
            // listener resolves a position from an id in the snapshot and not
            // from a hex the simulation handed it.
            projectile.EmitterId = emitterId;

            // A reference and a countdown. No position, now or ever: where it
            // appears to be is a question the view answers from where its target
            // is in the snapshot it is drawing.
            projectile.Target = TargetRef.Creep(targetId);
            projectile.TicksInFlight = 0;
            projectile.FlightDurationTicks = type.ProjectileFlightTicks;
            projectile.Damage = damage;
            projectile.Gone = false;

            _projectileCount++;
        }

        /// <summary>
        /// Lands a shot on a creep, or does not. A shot aimed at a creep that is
        /// already dying, already gone, or never existed is discarded here, which
        /// is the single place overkill and every kind of stale reference are
        /// dealt with -- and the single place a roll becomes an amount.
        /// </summary>
        /// <remarks>
        /// <b>A row that names a successor changes into it here, between the
        /// pool and the matrix.</b> The order is the whole of the mechanic:
        /// nothing has come off health yet when the change resolves, so the new
        /// row enters on the same share of its own pool the old one was holding
        /// -- a full one, this being the first damage the body has taken -- and
        /// the roll then lands on the body now standing there, through its
        /// armour and against its pool. A hit big enough to have killed the old
        /// row therefore kills nothing: the old row is already gone by the time
        /// the death check runs. See
        /// <c>docs/adr/0058-a-creep-becomes-another-row-mid-lane.md</c>.
        /// </remarks>
        /// <param name="creep">Where the target is in the live array, or -1 for nothing.</param>
        /// <param name="roll">What the dice gave when the shot was fired.</param>
        /// <param name="shooter">The row that fired it, which carries its attack type.</param>
        /// <param name="events">Where to say what landed, or null.</param>
        private void Damage(int creep, int roll, UnitType shooter, IMatchEvents? events)
        {
            if (creep < 0)
            {
                return;
            }

            ref Creep target = ref _creeps[creep];

            if (target.Phase != CreepPhase.Walking)
            {
                return;
            }

            int past = Absorbed(ref target, roll);

            if (past == 0)
            {
                return;
            }

            // After the pool and before the matrix. A roll a shield swallowed
            // whole never reaches here, which is the right silence: nothing was
            // taken off health, so no damage was taken and there is nothing to
            // change on.
            if (target.Type.Becomes is UnitType next)
            {
                Become(ref target, next, events);
            }

            int amount = Resolved(shooter, past, ref target);

            events?.CreepDamaged(target.Id, amount);
            target.Hp -= amount;

            if (target.Hp > 0)
            {
                return;
            }

            target.Hp = 0;
            target.Phase = CreepPhase.Dying;
            target.TicksInState = 0;
            _killed++;
            events?.CreepDied(target.Id);
        }

        /// <summary>
        /// Turns one body into the row its own row names, in place.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The body is the same body.</b> It keeps its id, its distance along
        /// the route, its lateral offset, the order that released it and every
        /// effect standing on it, so nothing that was pointed at it stops being
        /// pointed at it and nothing about where it is moves.
        /// </para>
        /// <para>
        /// <b>Health carries as a share of the pool and never as a number.</b>
        /// Two rows have two pools, so a raw carry-over would be a fraction of
        /// one pool read as a fraction of the other. The share is taken in
        /// integers and floored, and floored to at least one, because a body
        /// that changed row is a body the change did not kill.
        /// </para>
        /// <para>
        /// <b>The pool in front of that health carries raw.</b> A shield is
        /// spent rather than scaled -- it has no rate for a share to be a share
        /// of -- so what is left of it is what is left of it, and the new row's
        /// own authored shield is not granted: a pool arrives when a body
        /// spawns, and this body did not.
        /// </para>
        /// <para>
        /// The step is re-derived because the new row walks at its own speed,
        /// and the aura and raise counters are put back to what a body of the
        /// new row starts with, because it pulses and raises on its own clock --
        /// all three exactly as a spawn does them.
        /// </para>
        /// </remarks>
        private static void Become(ref Creep body, UnitType next, IMatchEvents? events)
        {
            long share = (long)body.Hp * next.MaxHp / body.Type.MaxHp;

            body.Type = next;
            body.Hp = share < 1 ? 1 : (int)share;
            body.Step = StepUnder(next, body.Effects.SpeedMagnitude);
            body.PulseIn = 0;
            body.RaiseIn = FirstRaiseIn(next);

            events?.CreepTransformed(body.Id, next.Id);
        }

        /// <summary>
        /// Spends a creep's shield against a roll and hands back what is left of
        /// the roll.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It absorbs first and it absorbs raw.</b> This runs before
        /// <see cref="Resolved"/> and therefore before the matrix cell and the
        /// armour denominator are looked at at all, which is the whole of what
        /// makes a shield a different lever from health rather than a second
        /// copy of it: a point of shield is worth exactly one point against
        /// every attack type there is, where a point of health is worth a cell
        /// and a multiplier. A shield mitigated like health would just be a
        /// bigger pool.
        /// </para>
        /// <para>
        /// <b>Overkill carries through.</b> What the shield could not eat goes
        /// on to health and is typed there, so a shield delays a body by exactly
        /// its own size and never by a whole shot -- and a hit that empties a
        /// shield is not a hit that was wasted on it.
        /// </para>
        /// <para>
        /// <b>A roll the shield swallowed whole deals nothing, floor and
        /// all.</b> The damage floor is a guarantee that a hit resolved through
        /// the matrix is never rounded away to nothing; a hit that never reached
        /// the matrix has nothing to guarantee, and running it through the floor
        /// would leak a point of health past a pool that stopped it.
        /// </para>
        /// </remarks>
        private static int Absorbed(ref Creep target, int roll)
        {
            // A granted pool goes first, because it is the one that can be
            // taken away: a pool with a clock on it is worth less than a pool
            // without one, so spending it first is the arrangement in which
            // nothing is wasted. Both are spent raw and both carry overkill
            // through, so which one a point came off changes nothing about
            // what the next point meets.
            roll = target.Effects.Spend(roll);

            if (roll == 0 || target.Shield <= 0)
            {
                return roll;
            }

            if (target.Shield >= roll)
            {
                target.Shield -= roll;
                return 0;
            }

            roll -= target.Shield;
            target.Shield = 0;

            return roll;
        }

        /// <summary>
        /// What a roll actually takes off that creep: the counter, the type
        /// chart and the target's armour, as the one fused expression the
        /// ruleset is made of, evaluated once.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>An untyped shot resolves untyped.</b> A table written in the
        /// column layout that has no type columns carries neither an attack type
        /// nor an armour type, so there is no row and no column to resolve
        /// through and the roll is what lands. That is what lets a record made
        /// against such a table replay to the numbers it was recorded at,
        /// forever, without the ruleset it never knew about reaching into it.
        /// </para>
        /// <para>
        /// <b>Typed on one side only is a refusal.</b> One table cannot produce
        /// it -- a unit that attacks carries an attack type and a unit that can
        /// be damaged carries an armour type, both checked at load -- so it is a
        /// defense and a wave parsed out of two tables that were never checked
        /// against each other, and there is no cell it could mean.
        /// </para>
        /// </remarks>
        private int Resolved(UnitType shooter, int roll, ref Creep target)
        {
            ArmourType armour = target.Type.ArmourType;

            if (shooter.AttackType == AttackType.None && armour == ArmourType.None)
            {
                return roll;
            }

            if (shooter.AttackType == AttackType.None || armour == ArmourType.None)
            {
                throw new SimulationException(
                    shooter.ToString()
                    + " is shooting "
                    + target.Type.ToString()
                    + ", and exactly one of them is in the damage matrix. A table types both halves of a "
                    + "shot or neither, so this is a defense and a wave read out of two tables that were "
                    + "never checked against each other.");
            }

            // Nothing counters anything: the anchor schedule that named a
            // shooter as some threat's answer is gone, and the roster has no
            // other route to a counter yet. The term stays in the damage model
            // because the model is what it belongs to -- see DamageModel.Dealt.
            // The armour it is carrying rather than the armour it was authored
            // with. One fused expression, evaluated here rather than cached,
            // because it is read once per landing and a landing is already the
            // rarest thing in the tick.
            return DamageModel.Dealt(
                _rules,
                roll,
                0,
                shooter.AttackType,
                armour,
                Effects.Modified(target.Type.Armour, target.Effects.ArmourMagnitude));
        }

        /// <summary>
        /// Finds a live creep by reference, or returns -1. A linear scan on
        /// purpose: the alternative is a dictionary, whose enumeration order is
        /// an implementation detail and which the scan over the compiled
        /// assembly refuses outright.
        /// </summary>
        private int FindWalkingCreep(TargetRef target)
        {
            if (target.Kind != TargetKind.Creep)
            {
                return -1;
            }

            for (int index = 0; index < _creepCount; index++)
            {
                if (_creeps[index].Id == target.Id)
                {
                    return _creeps[index].Phase == CreepPhase.Walking ? index : -1;
                }
            }

            return -1;
        }

        /// <summary>
        /// Compacts out everything that stopped existing this tick, keeping what
        /// is left in ascending id order -- which is the order everything else
        /// iterates in, and therefore part of the rules.
        /// </summary>
        private void ClearAwayTheGone()
        {
            int kept = 0;

            for (int index = 0; index < _creepCount; index++)
            {
                if (_creeps[index].Phase == CreepPhase.Gone)
                {
                    continue;
                }

                if (kept != index)
                {
                    _creeps[kept] = _creeps[index];
                }

                kept++;
            }

            _creepCount = kept;
            kept = 0;

            for (int index = 0; index < _projectileCount; index++)
            {
                if (_projectiles[index].Gone)
                {
                    continue;
                }

                if (kept != index)
                {
                    _projectiles[kept] = _projectiles[index];
                }

                kept++;
            }

            _projectileCount = kept;
        }

        /// <summary>
        /// Folds this tick's internal state into the rolling hash.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>What is folded is internal state, not the snapshot.</b> No
        /// <see cref="Snapshot"/> is built here and none is consulted: the fold
        /// reaches straight into the arrays, and it deliberately includes the
        /// three kinds of field a view never sees -- the raw Q32.32 remainder
        /// under every creep's distance, the position of the dice stream, and
        /// the running count of target-selection ties. A run that drifts in one
        /// of those looks identical on screen for a while and is already a
        /// different match.
        /// </para>
        /// <para>
        /// Each creep's constant properties are folded once, when it spawns,
        /// into a running value that is absorbed here. Re-absorbing a number
        /// that cannot have changed, once a tick for two thousand ticks, would
        /// be spending the re-simulation budget on proving nothing.
        /// </para>
        /// <para>
        /// <b>Which row a creep is is not one of those.</b> A body can change
        /// row mid-lane, so the type id is folded here beside the health and the
        /// phase rather than once at the spawn: a run that changed a body one
        /// tick earlier than another run is a different match from that tick on,
        /// and this is the only thing that would ever notice.
        /// </para>
        /// </remarks>
        private void Fold()
        {
            Hash64 hash = _stateHash
                .Add(unchecked((long)_dice.State))
                .Add(unchecked((long)_spawnFold.Value))
                .Add(Tick, _shotsFired)
                .Add(_tiebreaksBroken, _releasedTotal)
                .Add(_leaked, _killed)
                .Add(_leakedRaised)
                .Add(_creepCount, _projectileCount);

            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                hash = creep.Effects.Fold(
                    hash
                        .Add(creep.Distance.Raw)
                        .Add(creep.Id, creep.Hp)
                        .Add(creep.Shield, (int)creep.Phase)
                        .Add(creep.TicksInState, creep.PulseIn)
                        .Add(creep.Type.Id, creep.RaiseIn));
            }

            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];

                hash = tower.Effects.Fold(
                    hash
                        .Add(tower.Id, tower.TargetCount)
                        .Add((int)tower.State, tower.TicksInState)
                        .Add(tower.Cooldown, tower.PulseIn));

                // Every target it is holding, not the first of them. A row that
                // fires three shots commits to three creeps at once, and a fold
                // that watched only the first would be blind to two thirds of
                // what an acquisition decided.
                for (int shot = 0; shot < tower.TargetCount; shot++)
                {
                    hash = hash.Add(TargetOf(index, shot));
                }
            }

            for (int index = 0; index < _projectileCount; index++)
            {
                ref Projectile projectile = ref _projectiles[index];

                hash = hash
                    .Add(projectile.Id, projectile.Damage)
                    .Add((int)projectile.Target.Kind, projectile.Target.Id)
                    .Add(projectile.TicksInFlight, projectile.FlightDurationTicks);
            }

            _stateHash = hash;
        }

        private static void Grow<T>(ref T[] array)
        {
            var grown = new T[array.Length * 2];

            for (int index = 0; index < array.Length; index++)
            {
                grown[index] = array[index];
            }

            array = grown;
        }

        /// <summary>
        /// A creep's phase, including the one the snapshot has no name for.
        /// <c>Gone</c> lives for the rest of a tick after a creep leaks or
        /// finishes dying, so that everything in that tick sees the same world;
        /// it is cleared away before anybody can pull a picture of it.
        /// </summary>
        private enum CreepPhase
        {
            Walking = 0,
            Dying = 1,
            Gone = 2,
        }

        private struct Creep
        {
            internal int Id;

            internal UnitType Type;

            /// <summary>
            /// Which wave order it descends from, which is how it is priced when
            /// it leaks: the order released it, or released the body that raised
            /// it.
            /// </summary>
            internal int OrderIndex;

            /// <summary>
            /// Whether something raised it rather than the wave releasing it.
            /// Constant for the life of the body, and what says which of the two
            /// prices a leak of it charges.
            /// </summary>
            internal bool Raised;

            /// <summary>
            /// How far it walks in a tick, as this creep rather than as its
            /// order.
            /// </summary>
            /// <remarks>
            /// <b>Per creep because a modifier is per creep.</b> Two Minions
            /// released by one order are not slowed together, so an array
            /// indexed by the order would be a slow that reached everything
            /// the Cryomancer never pointed at -- and
            /// <see cref="StepThisTick"/> reads the same number, so it would
            /// also have mis-reported every overtake. It is a converted value
            /// rather than a speed because the conversion is a division: it is
            /// re-derived where the modifier moves and nowhere else.
            /// </remarks>
            internal Fix64 Step;

            internal Fix64 Distance;

            internal Fix64 Lateral;

            internal int Hp;

            /// <summary>
            /// What is left of the pool this row authored, which absorbs raw
            /// and after the granted one. A snapshot adds the two together and
            /// the state hash folds them apart.
            /// </summary>
            internal int Shield;

            internal CreepPhase Phase;

            internal int TicksInState;

            /// <summary>What is on it, and for how much longer.</summary>
            internal Effects Effects;

            /// <summary>
            /// Ticks until its own bubble pulses, for a row that carries an
            /// aura. Zero on every other row and read by nothing there.
            /// </summary>
            internal int PulseIn;

            /// <summary>
            /// Ticks until it raises, for a row that raises. Zero on every other
            /// row and read by nothing there.
            /// </summary>
            internal int RaiseIn;
        }

        private struct Tower
        {
            internal int Id;

            internal TowerState State;

            internal int TicksInState;

            /// <summary>
            /// The first creep it is shooting at, which is the one a snapshot
            /// carries. A row firing one shot has this and nothing else; the
            /// rest of them live in <see cref="_towerTargets"/>, because a
            /// snapshot field is a view contract and the view draws one line.
            /// </summary>
            internal int TargetId;

            /// <summary>
            /// How many of its shots found a creep. Zero while idle, and the
            /// width of this tower's slice of <see cref="_towerTargets"/> that
            /// is worth reading.
            /// </summary>
            internal int TargetCount;

            /// <summary>
            /// Ticks left before it may attack again. Internal: a tower between
            /// shots looks idle, so the snapshot says Idle, and this is one of
            /// the fields the state hash exists to watch precisely because
            /// nothing else would ever notice it drifting.
            /// </summary>
            internal int Cooldown;

            /// <summary>What is on it, and for how much longer.</summary>
            /// <remarks>
            /// A tower carries the same four slots a creep does and can only
            /// ever be handed one of them: nothing that stands walks, has a
            /// health pool or can be damaged here, so a bubble reaching towers
            /// with any payload but a cooldown is refused where the columns are
            /// read. One type rather than two, because the rule that resolves a
            /// slot is the same rule whichever side is holding it.
            /// </remarks>
            internal Effects Effects;

            /// <summary>
            /// Ticks until its own bubble pulses, for a row that carries an
            /// aura. Zero on every other row and read by nothing there.
            /// </summary>
            internal int PulseIn;
        }

        private struct Projectile
        {
            internal int Id;

            /// <summary>
            /// What fired it. Carried rather than looked up because the shot is
            /// resolved where it lands, and the row is what says which row of
            /// the damage matrix it lands through.
            /// </summary>
            internal UnitType Type;

            /// <summary>
            /// The hex it was fired from, which a bubble centred on the shooter
            /// is measured from when it lands. Not a position the projectile
            /// has and not one anything draws -- a tower does not move, so this
            /// is the shooter's own cell, carried for the same reason its row
            /// is.
            /// </summary>
            internal Hex Origin;

            /// <summary>
            /// The tower that fired it, which is <see cref="Origin"/> as an
            /// entity: what a sweep's own event names when the shot lands, so
            /// that whatever is listening resolves the position itself. Not in
            /// the state hash -- it cannot change and nothing about the match
            /// reads it.
            /// </summary>
            internal int EmitterId;

            internal TargetRef Target;

            internal int TicksInFlight;

            internal int FlightDurationTicks;

            /// <summary>
            /// Rolled when the shot was fired, not when it lands, because the
            /// draw is once per shot. Internal, and in the state hash.
            /// </summary>
            internal int Damage;

            internal bool Gone;
        }
    }
}
