using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>What a creep is doing. Two states, and both of them are real.</summary>
    public enum CreepState
    {
        /// <summary>Walking the corridor.</summary>
        Walking = 0,

        /// <summary>
        /// Dying. A real simulation state with an integer tick duration, not a
        /// courtesy extended by the view: seeking into a death has to show a
        /// death, and the alternative is a corpse the simulation has forgotten
        /// about and the view is keeping alive on a clock of its own.
        /// </summary>
        Dying = 1,
    }

    /// <summary>What a tower is doing, as far as anything drawing it is concerned.</summary>
    /// <remarks>
    /// There is deliberately no <c>Cooling</c> state. A tower between shots is
    /// idle to look at, and the cooldown counter that decides when it stops
    /// being idle is internal -- one of exactly the fields the rolling state
    /// hash exists to cover, because the view never sees it and so nothing else
    /// would notice it drifting.
    /// </remarks>
    public enum TowerState
    {
        /// <summary>Not attacking. Either nothing is in range, or it is between shots.</summary>
        Idle = 0,

        /// <summary>Committed to a shot, which has not landed yet.</summary>
        Windup = 1,

        /// <summary>Recovering from a shot that has landed.</summary>
        Backswing = 2,
    }

    /// <summary>One creep, as of one tick.</summary>
    /// <remarks>
    /// Position is <see cref="DistanceAlongPath"/> plus
    /// <see cref="LateralOffset"/> and never a point in a plane. Turning that
    /// into somewhere to stand is the view's job and needs the route, which the
    /// view already has because the map is static data loaded once.
    /// </remarks>
    public readonly struct CreepSnapshot
    {
        internal CreepSnapshot(
            int id,
            int typeId,
            Fix64 distanceAlongPath,
            Fix64 lateralOffset,
            int hp,
            CreepState state,
            int ticksInState)
        {
            Id = id;
            TypeId = typeId;
            DistanceAlongPath = distanceAlongPath;
            LateralOffset = lateralOffset;
            Hp = hp;
            State = state;
            TicksInState = ticksInState;
        }

        /// <summary>The entity id. An entity that vanished is an id that stopped appearing.</summary>
        public int Id { get; }

        /// <summary>Which unit type, by its stable id.</summary>
        public int TypeId { get; }

        /// <summary>How far along the corridor, in hexes. Zero is the entrance.</summary>
        public Fix64 DistanceAlongPath { get; }

        /// <summary>How far to the side of the corridor's centre line, in hexes.</summary>
        public Fix64 LateralOffset { get; }

        /// <summary>Health remaining.</summary>
        public int Hp { get; }

        /// <summary>Walking or dying.</summary>
        public CreepState State { get; }

        /// <summary>How many ticks it has been in that state.</summary>
        public int TicksInState { get; }
    }

    /// <summary>One tower, as of one tick.</summary>
    /// <remarks>
    /// There is no type id and no position here. A tower is static for the whole
    /// match, so both are in the authored defense the view loaded once; the
    /// snapshot carries only what moves. The id is the tower's position in that
    /// defense, counted from one, which is what lets the view join the two
    /// without either repeating the other.
    /// </remarks>
    public readonly struct TowerSnapshot
    {
        internal TowerSnapshot(int id, TowerState state, int targetId, int ticksInState)
        {
            Id = id;
            State = state;
            TargetId = targetId;
            TicksInState = ticksInState;
        }

        /// <summary>The entity id, which is the tower's one-based place in the defense.</summary>
        public int Id { get; }

        /// <summary>Idle, winding up or recovering.</summary>
        public TowerState State { get; }

        /// <summary>What it is aimed at, or zero. So the view can turn it to face something.</summary>
        public int TargetId { get; }

        /// <summary>How many ticks it has been in that state.</summary>
        public int TicksInState { get; }
    }

    /// <summary>One projectile in flight, as of one tick.</summary>
    /// <remarks>
    /// A projectile is a countdown and a reference: <see cref="TicksInFlight"/>
    /// out of <see cref="FlightDurationTicks"/> says how far along it is, and
    /// <see cref="Target"/> says what it is going to. The view has everything it
    /// needs to draw it anywhere between the two, and the simulation has never
    /// computed a position for it.
    /// </remarks>
    public readonly struct ProjectileSnapshot
    {
        internal ProjectileSnapshot(
            int id,
            int typeId,
            TargetRef target,
            int ticksInFlight,
            int flightDurationTicks)
        {
            Id = id;
            TypeId = typeId;
            Target = target;
            TicksInFlight = ticksInFlight;
            FlightDurationTicks = flightDurationTicks;
        }

        /// <summary>The entity id.</summary>
        public int Id { get; }

        /// <summary>The type of the unit that fired it, by its stable id.</summary>
        public int TypeId { get; }

        /// <summary>What it is going to. Carries no position at all.</summary>
        public TargetRef Target { get; }

        /// <summary>How long it has been flying.</summary>
        public int TicksInFlight { get; }

        /// <summary>How long it flies for in total.</summary>
        public int FlightDurationTicks { get; }
    }

    /// <summary>
    /// Everything that moves, as of one tick, and nothing else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The snapshot is the only thing a view may draw game state from</b>, and
    /// it is <b>pulled, not pushed</b>: a run that never asks for one never
    /// builds one, which is the whole of what instant-resolve is. There is no
    /// mode and no flag behind that -- a headless match and a watched match run
    /// the identical loop, and the only difference is whether anybody calls
    /// <see cref="Match.PullSnapshot"/>.
    /// </para>
    /// <para>
    /// Static data is absent on purpose. The map, the defense, the type tables
    /// and the wave were loaded once and have not changed since, so repeating
    /// them sixty times a second would be sixty times a second spent proving
    /// they had not.
    /// </para>
    /// <para>
    /// The deliberate asymmetry between the two towers lives here: a hitscan
    /// tower's shot produces <b>no entity in this snapshot at all</b> -- it
    /// exists only as an event and whatever tracer the view draws and forgets --
    /// while a projectile tower's shot produces a real
    /// <see cref="ProjectileSnapshot"/> that can be scrubbed backwards through.
    /// Same seam, opposite treatments, on purpose.
    /// </para>
    /// </remarks>
    public sealed class Snapshot
    {
        private readonly CreepSnapshot[] _creeps;

        private readonly TowerSnapshot[] _towers;

        private readonly ProjectileSnapshot[] _projectiles;

        internal Snapshot(
            int tick,
            CreepSnapshot[] creeps,
            TowerSnapshot[] towers,
            ProjectileSnapshot[] projectiles)
        {
            Tick = tick;
            _creeps = creeps;
            _towers = towers;
            _projectiles = projectiles;
        }

        /// <summary>The tick this is a picture of.</summary>
        public int Tick { get; }

        /// <summary>Every creep on the map, in ascending id order.</summary>
        public IReadOnlyList<CreepSnapshot> Creeps => _creeps;

        /// <summary>Every tower, in the defense's canonical order.</summary>
        public IReadOnlyList<TowerSnapshot> Towers => _towers;

        /// <summary>Every projectile in flight, in ascending id order.</summary>
        public IReadOnlyList<ProjectileSnapshot> Projectiles => _projectiles;

        public override string ToString() =>
            "tick "
            + Tick.ToString(CultureInfo.InvariantCulture)
            + ": "
            + _creeps.Length.ToString(CultureInfo.InvariantCulture)
            + " creeps, "
            + _projectiles.Length.ToString(CultureInfo.InvariantCulture)
            + " projectiles";
    }
}
