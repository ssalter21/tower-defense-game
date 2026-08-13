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
    /// <b>The order of work inside a tick is part of the rules.</b> Creeps move,
    /// then dying creeps age, then projectiles fly and land, then towers act,
    /// then the dead are cleared away, then the tick number advances and the
    /// wave releases whatever is due. Changing that order changes replays even
    /// though no number in any file moved, which is exactly what the simulation
    /// version exists to say.
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
        private const string HashLabel = "match-state/1";

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

        /// <summary>One entry per wave order: how far its units move each tick.</summary>
        private readonly Fix64[] _stepPerTick;

        /// <summary>One entry per wave order: how many of its units have been released.</summary>
        private readonly int[] _released;

        /// <summary>
        /// One entry per wave order: how many of its units reached the exit. A
        /// total is not enough to price a leak, because what a leak costs is
        /// what the thing that leaked cost -- so what is counted is which order
        /// walked past, and the order is what carries the type.
        /// </summary>
        private readonly int[] _leakedByOrder;

        private readonly Tower[] _towers;

        private Creep[] _creeps;

        /// <summary>
        /// Scratch space for one acquisition: the walking creeps a tower can
        /// reach, refilled from <see cref="_creeps"/> every time a tower looks.
        /// </summary>
        /// <remarks>
        /// A field rather than a local, and sized once at the whole wave rather
        /// than grown, because seeking re-simulates: anything the tick path
        /// allocates is a cost every scrub of the slider pays. Nothing can be
        /// walking that has not been released, so a wave's total is the most
        /// candidates one acquisition can ever have.
        /// </remarks>
        private readonly WalkingTarget[] _reachable;

        private Projectile[] _projectiles;

        private int _creepCount;

        private int _projectileCount;

        private int _nextEntityId;

        private int _spawnOrdinal;

        private int _releasedTotal;

        private int _leaked;

        private int _killed;

        private int _shotsFired;

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

            for (int index = 0; index < wave.Count; index++)
            {
                UnitType type = wave.Orders[index].Type;

                if (type.SpeedMilliHexPerTick <= 0)
                {
                    throw new SimulationException(
                        "The wave sends "
                        + type.ToString()
                        + ", which has no speed. A unit that walks a corridor at nothing per tick never "
                        + "reaches the exit and never dies, so the match it is in cannot end.");
                }

                // Once, here, rather than in the tick loop: this is a division,
                // and it is also the one place the truncated remainder that the
                // state hash exists to watch is created.
                _stepPerTick[index] = Fix64.FromRatio(type.SpeedMilliHexPerTick, MilliHexPerHex);
            }

            _towers = new Tower[layout.Count];

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
            _reachable = new WalkingTarget[wave.TotalUnits];
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

        /// <summary>The seed the dice were started from.</summary>
        public ulong Seed { get; }

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
                    creep.Phase == CreepPhase.Dying ? CreepState.Dying : CreepState.Walking,
                    creep.TicksInState);
            }

            var towers = new TowerSnapshot[_towers.Length];

            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];
                towers[index] = new TowerSnapshot(tower.Id, tower.State, tower.TargetId, tower.TicksInState);
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

        /// <summary>One tick. The order of these phases is part of the rules.</summary>
        private void Step(IMatchEvents? events)
        {
            MoveCreeps(events);
            ReportPasses(events);
            AgeDyingCreeps();
            FlyProjectiles(events);
            RunTowers(events);
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

                Spawn(index, order.Type);
                _released[index]++;
                _releasedTotal++;
            }
        }

        private void Spawn(int orderIndex, UnitType type)
        {
            if (_creepCount == _creeps.Length)
            {
                Grow(ref _creeps);
            }

            ref Creep creep = ref _creeps[_creepCount];

            creep.Id = _nextEntityId++;
            creep.Type = type;
            creep.OrderIndex = orderIndex;
            creep.Distance = Fix64.Zero;
            creep.Lateral = _lateralOffsets[_spawnOrdinal % _lateralOffsets.Length];
            creep.Hp = type.MaxHp;
            creep.Phase = CreepPhase.Walking;
            creep.TicksInState = 0;

            _creepCount++;
            _spawnOrdinal++;

            _spawnFold = _spawnFold
                .Add(creep.Id)
                .Add(type.Id)
                .Add(creep.Lateral.Raw)
                .Add(creep.Hp);
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

                creep.Distance += _stepPerTick[creep.OrderIndex];
                creep.TicksInState++;

                if (creep.Distance >= _routeLength)
                {
                    creep.Phase = CreepPhase.Gone;
                    _leaked++;
                    _leakedByOrder[creep.OrderIndex]++;
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
        private Fix64 StepThisTick(ref Creep creep) =>
            creep.Phase == CreepPhase.Walking ? _stepPerTick[creep.OrderIndex] : Fix64.Zero;

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
                    Damage(target, projectile.Damage, projectile.Type, events);
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

                        int target = Acquire(index);

                        if (target == 0)
                        {
                            break;
                        }

                        tower.TargetId = target;
                        tower.State = TowerState.Windup;
                        tower.TicksInState = 0;

                        if (type.WindupTicks == 0)
                        {
                            Fire(ref tower, type, events);
                        }

                        break;

                    case TowerState.Windup:
                        tower.TicksInState++;

                        if (tower.TicksInState >= type.WindupTicks)
                        {
                            Fire(ref tower, type, events);
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
        /// What a tower shoots at, as the id of a creep, or zero when nothing
        /// it can reach is walking.
        /// </summary>
        /// <remarks>
        /// The rule is <see cref="Targeting.Chosen"/> and none of it is spelled
        /// here. This is the projection onto it: walk the creeps once, keep the
        /// walking ones this tower's coverage reaches, hand them over in the
        /// order the array is kept in -- which is ascending id -- and add the
        /// ties it broke to the running count the state hash folds.
        /// </remarks>
        private int Acquire(int tower)
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

            int chosen = Targeting.Chosen(
                new ReadOnlySpan<WalkingTarget>(_reachable, 0, reachable),
                out int tiebreaks);

            _tiebreaksBroken += tiebreaks;

            return chosen < 0 ? 0 : _reachable[chosen].Id;
        }

        /// <summary>
        /// Releases the shot a tower committed to when it started winding up.
        /// </summary>
        /// <remarks>
        /// <b>The shot happens whatever became of the target.</b> A tower that
        /// commits and then finds its target dead still fires, still rolls, and
        /// still wastes the shot -- which is what makes overkill real: two towers
        /// covering one stretch of corridor can both commit to the same creep,
        /// and the second one's damage lands on something already dying and is
        /// discarded. Re-checking the target here would quietly make that
        /// impossible, and with it the whole reason the ranges were made to
        /// overlap.
        /// </remarks>
        private void Fire(ref Tower tower, UnitType type, IMatchEvents? events)
        {
            // The one and only draw. Once per shot, on the one stream, whether
            // or not the shot is going to land on anything.
            int damage = _dice.NextInRange(type.DamageMin, type.DamageMax + 1);
            _shotsFired++;

            events?.TowerFired(tower.Id, tower.TargetId);

            switch (type.Delivery)
            {
                case Delivery.Hitscan:
                    // No snapshot entity of any kind: a hitscan shot exists as an
                    // event and as whatever the view draws and forgets, and
                    // nothing else.
                    Damage(FindWalkingCreep(TargetRef.Creep(tower.TargetId)), damage, type, events);
                    break;

                case Delivery.Projectile:
                    Launch(type, tower.TargetId, damage);
                    break;

                case Delivery.None:
                    throw new SimulationException(
                        "Tower "
                        + tower.Id.ToString(CultureInfo.InvariantCulture)
                        + " is "
                        + type.ToString()
                        + ", which delivers no damage, and it has fired. A unit that cannot attack should "
                        + "never have acquired a target.");
            }

            tower.State = TowerState.Backswing;
            tower.TicksInState = 0;

            if (type.BackswingTicks == 0)
            {
                GoIdle(ref tower, type);
            }
        }

        private void GoIdle(ref Tower tower, UnitType type)
        {
            tower.State = TowerState.Idle;
            tower.TicksInState = 0;
            tower.TargetId = 0;
            tower.Cooldown = type.CooldownTicks;
        }

        private void Launch(UnitType type, int targetId, int damage)
        {
            if (_projectileCount == _projectiles.Length)
            {
                Grow(ref _projectiles);
            }

            ref Projectile projectile = ref _projectiles[_projectileCount];

            projectile.Id = _nextEntityId++;
            projectile.Type = type;

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

            int amount = Resolved(shooter, roll, ref target);

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
            return DamageModel.Dealt(
                _rules,
                roll,
                0,
                shooter.AttackType,
                armour,
                target.Type.Armour);
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
        /// </remarks>
        private void Fold()
        {
            Hash64 hash = _stateHash
                .Add(unchecked((long)_dice.State))
                .Add(unchecked((long)_spawnFold.Value))
                .Add(Tick, _shotsFired)
                .Add(_tiebreaksBroken, _releasedTotal)
                .Add(_leaked, _killed)
                .Add(_creepCount, _projectileCount);

            for (int index = 0; index < _creepCount; index++)
            {
                ref Creep creep = ref _creeps[index];

                hash = hash
                    .Add(creep.Distance.Raw)
                    .Add(creep.Id, creep.Hp)
                    .Add((int)creep.Phase, creep.TicksInState);
            }

            for (int index = 0; index < _towers.Length; index++)
            {
                ref Tower tower = ref _towers[index];

                hash = hash
                    .Add(tower.Id, tower.TargetId)
                    .Add((int)tower.State, tower.TicksInState)
                    .Add(tower.Cooldown);
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

            /// <summary>Which wave order released it, which is how its speed is found.</summary>
            internal int OrderIndex;

            internal Fix64 Distance;

            internal Fix64 Lateral;

            internal int Hp;

            internal CreepPhase Phase;

            internal int TicksInState;
        }

        private struct Tower
        {
            internal int Id;

            internal TowerState State;

            internal int TicksInState;

            internal int TargetId;

            /// <summary>
            /// Ticks left before it may attack again. Internal: a tower between
            /// shots looks idle, so the snapshot says Idle, and this is one of
            /// the fields the state hash exists to watch precisely because
            /// nothing else would ever notice it drifting.
            /// </summary>
            internal int Cooldown;
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
