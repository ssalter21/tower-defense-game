using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The match on screen: every pixel of it a pure function of an immutable
    /// snapshot pulled from the simulation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The governing rule, and this class is where it is enforced rather
    /// than described: the snapshot is the only thing the view may draw game
    /// state from, and events may only trigger the purely decorative.</b>
    /// Every creep, tower and projectile on screen is placed by
    /// <see cref="Draw"/> out of <see cref="Current"/> and
    /// <see cref="Previous"/>. Nothing else in this file reads the simulation,
    /// and the event stream goes to <see cref="MatchDecorations"/> and nowhere
    /// else.
    /// </para>
    /// <para>
    /// <b>Two snapshots, matched by id.</b> The view holds the last two and
    /// nothing older. That is enough to interpolate between ticks, and — much
    /// more importantly — it is what makes a vanished entity require no
    /// handling at all: it is an id that stopped appearing, and the pool
    /// releases it by subtraction. There is no despawn message anywhere in this
    /// project, and a projectile whose target died mid-flight is not a special
    /// case in any file.
    /// </para>
    /// <para>
    /// <b>Interpolation is a pure function of three arguments</b> — the two
    /// snapshots and an alpha — so it is not a playback head. It cannot drift,
    /// it holds no accumulated position, and drawing the same alpha twice draws
    /// the same picture. <b>There is no clock in this class at all</b>: which
    /// tick is on screen is decided entirely by
    /// <see cref="PlaybackController"/>, which calls <see cref="StepOneTick"/>,
    /// <see cref="ReSimulateTo"/> and <see cref="Draw"/> and is the only thing
    /// that does.
    /// </para>
    /// <para>
    /// <b>Draw order is by construction, not by sorting.</b> Everything on the
    /// playfield is opaque geometry in a depth buffer, and each entity's view
    /// object is bound to its id for as long as that id keeps appearing — so
    /// two creeps overtaking swap places in the world and never swap objects.
    /// Nothing re-sorts per frame, so there is nothing to flicker.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class MatchView : MonoBehaviour
    {
        private readonly Dictionary<int, Vector3> _drawnCreepPositions = new Dictionary<int, Vector3>();

        private readonly Dictionary<int, CreepSnapshot> _previousCreeps =
            new Dictionary<int, CreepSnapshot>();

        private readonly Dictionary<int, TowerSnapshot> _previousTowers =
            new Dictionary<int, TowerSnapshot>();

        private readonly Dictionary<int, ProjectileSnapshot> _previousProjectiles =
            new Dictionary<int, ProjectileSnapshot>();

        private readonly Dictionary<int, TowerView> _towers = new Dictionary<int, TowerView>();

        private EntityViewPool<CreepView> _creepPool;

        private EntityViewPool<ProjectileView> _projectilePool;

        private MatchArt _art;

        private RoutePath _route;

        private UnitTypeTable _types;

        private Material _projectileMaterial;

        private Transform _creepParent;

        private Transform _projectileParent;

        private HexMap _map;

        private TowerLayout _layout;

        private WaveScript _wave;

        private ulong _seed;

        /// <summary>The match being drawn.</summary>
        public Match Match { get; private set; }

        /// <summary>The snapshot before <see cref="Current"/>, or null on the first tick.</summary>
        public Snapshot Previous { get; private set; }

        /// <summary>The snapshot being drawn.</summary>
        public Snapshot Current { get; private set; }

        /// <summary>The corridor, in world space.</summary>
        public RoutePath Route => _route;

        /// <summary>The effects the event stream drives, and the only thing it drives.</summary>
        public MatchDecorations Decorations { get; private set; }

        /// <summary>The creep views, live and idle.</summary>
        public EntityViewPool<CreepView> Creeps => _creepPool;

        /// <summary>The projectile views, live and idle.</summary>
        public EntityViewPool<ProjectileView> Projectiles => _projectilePool;

        /// <summary>The six towers, by the id the snapshot calls them.</summary>
        public IReadOnlyDictionary<int, TowerView> Towers => _towers;

        /// <summary>True once <see cref="Begin"/> has run.</summary>
        public bool IsRunning => Match != null;

        /// <summary>
        /// The tick the match ends on, known before anybody has watched a
        /// single frame of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Measured at <see cref="Begin"/> by resolving a throwaway match —
        /// which costs one instant-resolve, is the same call as every other
        /// scenario, and allocates nothing because nobody pulls a snapshot from
        /// it. The build gate holds that run under ten milliseconds.
        /// </para>
        /// <para>
        /// A scrub bar needs a length before the first drag. The alternatives
        /// are guessing one or growing it as the match plays, and the second is
        /// a slider whose far end moves while you are dragging towards it.
        /// </para>
        /// </remarks>
        public int FinalTick { get; private set; }

        /// <summary>
        /// True when the simulation will advance no further. The view keeps
        /// drawing the last snapshot rather than clearing, because a finished
        /// match still has a picture.
        /// </summary>
        public bool IsFinished => Match != null && Match.IsFinished;

        /// <summary>
        /// Starts drawing a match. Builds the towers, which are static for its
        /// whole length, and pulls the first snapshot.
        /// </summary>
        public void Begin(
            HexMap map,
            UnitTypeTable types,
            TowerLayout layout,
            WaveScript wave,
            ulong seed,
            MatchArt art)
        {
            if (map is null) throw new ArgumentNullException(nameof(map));
            if (layout is null) throw new ArgumentNullException(nameof(layout));
            if (wave is null) throw new ArgumentNullException(nameof(wave));

            _types = types ?? throw new ArgumentNullException(nameof(types));
            _art = art ?? throw new ArgumentNullException(nameof(art));

            // Kept so a seek can build the match again from nothing but these.
            // A seek re-simulates, so these four are the whole of what the view
            // has to remember about a match -- there is no cache below them.
            _map = map;
            _layout = layout;
            _wave = wave;
            _seed = seed;

            _route = RoutePath.For(map);
            _projectileMaterial = ViewMaterials.Create("Projectile", MatchTuning.ProjectileColor);

            _creepParent = MakeGroup("Creeps");
            _projectileParent = MakeGroup("Projectiles");
            Transform towerParent = MakeGroup("Towers");

            _creepPool = new EntityViewPool<CreepView>(MakeCreepView);
            _projectilePool = new EntityViewPool<ProjectileView>(MakeProjectileView);

            BuildTowers(layout, towerParent);

            Decorations = new MatchDecorations(transform, CreepPositionOf, TowerMuzzleOf);

            // Instant-resolve, and it is the same call as everything else:
            // construct, run, and never pull a snapshot.
            FinalTick = new Match(map, layout, wave, seed).Resolve().FinalTick;

            Match = new Match(map, layout, wave, seed);

            Previous = null;
            Current = Match.PullSnapshot();
            RememberPrevious(Current);

            Draw(0f);
        }

        /// <summary>
        /// Advances the simulation one tick and pulls the snapshot for it.
        /// </summary>
        /// <remarks>
        /// The pull is what makes this a watched match. A run that never calls
        /// it never builds a snapshot and never allocates one — which is the
        /// whole of what instant-resolve is, and why the headless command line
        /// is the same code path with nobody looking.
        /// </remarks>
        public void StepOneTick()
        {
            if (Match == null || Match.IsFinished)
            {
                return;
            }

            RememberPrevious(Current);
            Previous = Current;

            // Events go to the decorations and nowhere else. Nothing the
            // simulation says here changes where anything is drawn.
            Match.Advance(1, Decorations);
            Current = Match.PullSnapshot();

            // Decoration ages on the tick, not on a wall clock. This is the one
            // place that cannot forget to do it, so a view driven a tick at a
            // time -- by a capture tool, or by a scrub bar -- ages its effects
            // correctly without knowing it has any.
            Decorations.AgeOneTick();
        }

        /// <summary>
        /// Draws everything, <paramref name="alpha"/> of the way from
        /// <see cref="Previous"/> to <see cref="Current"/>.
        /// </summary>
        /// <remarks>
        /// The order is creeps, then towers, then projectiles, and it is not
        /// arbitrary: a tower faces where its target is <i>now</i> and a shell
        /// falls onto where its target is <i>now</i>, so both read positions
        /// this frame's creep pass has already computed. Nothing reads a
        /// position from last frame.
        /// </remarks>
        public void Draw(float alpha)
        {
            if (Current == null)
            {
                return;
            }

            float blend = Mathf.Clamp01(alpha);

            DrawCreeps(blend);
            DrawTowers(blend);
            DrawProjectiles(blend);
        }

        /// <summary>
        /// Drops every effect. What a seek does: the events of the ticks a seek
        /// re-runs are discarded, so anything still fading belongs to a tick
        /// that has not happened yet.
        /// </summary>
        public void ClearDecorations() => Decorations?.Clear();

        /// <summary>
        /// Puts the match on <paramref name="tick"/> by playing it again from
        /// tick zero, and draws it. The mechanism behind every seek.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>There is no snapshot cache and this is what happens instead.</b>
        /// A cache can never disagree with itself, so scrubbing one would prove
        /// nothing; re-simulating makes every drag of the scrub bar a fresh
        /// determinism check that either produces the same match or does not.
        /// </para>
        /// <para>
        /// <b>The events of the re-run ticks are discarded — by nobody
        /// subscribing, rather than by anybody filtering.</b>
        /// <see cref="Match.Advance(int, IMatchEvents)"/> takes the sink as an
        /// argument and this call does not pass one, so the whole match's
        /// tracers, flashes and sparks are never built in the first place.
        /// Seeking to the end therefore does not detonate them in one frame.
        /// </para>
        /// <para>
        /// <b>Only the decorations are cleared</b>, because effects are the one
        /// remaining thing in this client that owns a clock. Audio would be the
        /// other and there is none yet; when there is, it clears here, next to
        /// this line, and for the same reason.
        /// </para>
        /// <para>
        /// <b>The object pool is deliberately left alone.</b> The draw at the
        /// end of this method syncs it against the new snapshot, and everything
        /// whose id is not in that snapshot goes back in the pool by
        /// subtraction — the same path every ordinary frame uses. Clearing it
        /// here would be a second opinion about what exists, and the two would
        /// disagree exactly when something interesting happened.
        /// </para>
        /// </remarks>
        public void ReSimulateTo(int tick)
        {
            if (Match == null)
            {
                throw new InvalidOperationException(
                    "There is no match to seek in. Begin one first.");
            }

            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tick), "There is no tick before the first one.");
            }

            Match = new Match(_map, _layout, _wave, _seed);
            Match.Advance(tick);

            // No previous snapshot, because the tick before this one was not
            // watched. Interpolating across a discontinuity would draw a frame
            // belonging to neither side of it.
            Previous = null;
            Current = Match.PullSnapshot();
            RememberPrevious(Current);

            Decorations.Clear();

            Draw(0f);
        }

        // ---------------------------------------------------------------
        // Drawing
        // ---------------------------------------------------------------

        private void DrawCreeps(float alpha)
        {
            _drawnCreepPositions.Clear();
            _creepPool.BeginSync();

            foreach (CreepSnapshot creep in Current.Creeps)
            {
                CreepView view = _creepPool.Claim(creep.Id);

                bool paired = _previousCreeps.TryGetValue(creep.Id, out CreepSnapshot before);

                float distance = paired
                    ? Mathf.Lerp(SimUnits.ToFloat(before.DistanceAlongPath), SimUnits.ToFloat(creep.DistanceAlongPath), alpha)
                    : SimUnits.ToFloat(creep.DistanceAlongPath);

                float lateral = paired
                    ? Mathf.Lerp(SimUnits.ToFloat(before.LateralOffset), SimUnits.ToFloat(creep.LateralOffset), alpha)
                    : SimUnits.ToFloat(creep.LateralOffset);

                Vector3 position = _route.PointAt(distance, lateral);

                _drawnCreepPositions[creep.Id] = position;

                view.Pose(
                    position,
                    _route.FacingAt(distance),
                    distance,
                    creep.State,
                    DyingFraction(creep, before, paired, alpha));
            }

            _creepPool.EndSync();
        }

        /// <summary>
        /// How far through its death a creep is. Interpolated only when both
        /// snapshots agree it was dying — the tick it starts dying, there is no
        /// previous dying state to come from and blending would start the clip
        /// part-way through.
        /// </summary>
        private float DyingFraction(CreepSnapshot creep, CreepSnapshot before, bool paired, float alpha)
        {
            if (creep.State != CreepState.Dying)
            {
                return 0f;
            }

            int dyingTicks = _types.ById(creep.TypeId).DyingTicks;

            if (dyingTicks <= 0)
            {
                return 1f;
            }

            float ticks = paired && before.State == CreepState.Dying
                ? Mathf.Lerp(before.TicksInState, creep.TicksInState, alpha)
                : creep.TicksInState;

            return ticks / dyingTicks;
        }

        private void DrawTowers(float alpha)
        {
            foreach (TowerSnapshot tower in Current.Towers)
            {
                if (!_towers.TryGetValue(tower.Id, out TowerView view))
                {
                    continue;
                }

                bool paired = _previousTowers.TryGetValue(tower.Id, out TowerSnapshot before);

                int ticksInState = paired && before.State == tower.State
                    ? Mathf.RoundToInt(Mathf.Lerp(before.TicksInState, tower.TicksInState, alpha))
                    : tower.TicksInState;

                Vector3? target = tower.TargetId > 0 && _drawnCreepPositions.TryGetValue(tower.TargetId, out Vector3 at)
                    ? at
                    : (Vector3?)null;

                view.Pose(tower.State, ticksInState, target);
            }
        }

        private void DrawProjectiles(float alpha)
        {
            _projectilePool.BeginSync();

            foreach (ProjectileSnapshot shell in Current.Projectiles)
            {
                // A shell whose target is not in this snapshot is a shell whose
                // target has gone. It is not drawn, and it needed no handling
                // to not be drawn -- the simulation will have dropped it on the
                // same tick, so this is belt and braces rather than the
                // mechanism.
                if (shell.Target.Kind != TargetKind.Creep
                    || !_drawnCreepPositions.TryGetValue(shell.Target.Id, out Vector3 target))
                {
                    continue;
                }

                ProjectileView view = _projectilePool.Claim(shell.Id);

                bool paired = _previousProjectiles.TryGetValue(shell.Id, out ProjectileSnapshot before);

                float ticks = paired
                    ? Mathf.Lerp(before.TicksInFlight, shell.TicksInFlight, alpha)
                    : shell.TicksInFlight;

                float fraction = shell.FlightDurationTicks <= 0
                    ? 1f
                    : ticks / shell.FlightDurationTicks;

                // The corridor's direction at the target, so the shell comes
                // down the line of the path rather than from a fixed compass
                // bearing.
                Vector3 tangent = _route.TangentAt(TargetDistance(shell.Target.Id));

                view.Pose(ProjectileView.OriginFor(target, tangent), target, fraction);
            }

            _projectilePool.EndSync();
        }

        /// <summary>
        /// How far along the corridor a creep is, out of the snapshot being
        /// drawn. Zero when it is not there, which only happens on the frame it
        /// left.
        /// </summary>
        private float TargetDistance(int creepId)
        {
            foreach (CreepSnapshot creep in Current.Creeps)
            {
                if (creep.Id == creepId)
                {
                    return SimUnits.ToFloat(creep.DistanceAlongPath);
                }
            }

            return 0f;
        }

        // ---------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------

        private void BuildTowers(TowerLayout layout, Transform parent)
        {
            for (int index = 0; index < layout.Count; index++)
            {
                PlacedTower placed = layout.Towers[index];

                // The id is the tower's one-based place in the defense, which is
                // exactly what the snapshot calls it. Joining the two on that
                // number is what lets the snapshot carry no position and no type
                // for a thing that never moves and never changes.
                int id = index + 1;

                var host = new GameObject("Tower " + id + " " + placed.Type.Label);
                host.transform.SetParent(parent, worldPositionStays: false);
                host.transform.localPosition = HexGeometry.ToWorld(placed.Hex);

                var view = host.AddComponent<TowerView>();
                Quaternion resting = RestingRotationFor(host.transform.localPosition);

                if (placed.Type.Delivery == Delivery.Projectile)
                {
                    view.BuildAnimated(
                        id,
                        placed.Type,
                        _art.ProjectileTowerModel,
                        _art.BowModel,
                        _art.TowerIdleClip,
                        _art.TowerWindupClip,
                        _art.TowerBackswingClip,
                        resting);
                }
                else
                {
                    view.BuildStatic(id, placed.Type, _art.HitscanTowerModel, resting);
                }

                _towers.Add(id, view);
            }
        }

        /// <summary>
        /// Which way a tower faces with nothing to shoot at: towards the
        /// nearest point of the corridor.
        /// </summary>
        /// <remarks>
        /// Derived rather than authored, so a tower moved in the defense file
        /// faces sensibly without anybody editing a second file — and so there
        /// is no per-tower rotation to get out of step with a per-tower
        /// position.
        /// </remarks>
        private Quaternion RestingRotationFor(Vector3 position)
        {
            float best = float.MaxValue;
            Vector3 nearest = _route.Entrance;

            for (int step = 0; step <= _route.StepCount; step++)
            {
                Vector3 point = _route.Step(step);
                float distance = (point - position).sqrMagnitude;

                if (distance < best)
                {
                    best = distance;
                    nearest = point;
                }
            }

            Vector3 toward = nearest - position;
            toward.y = 0f;

            return toward.sqrMagnitude < 1e-6f
                ? Quaternion.identity
                : Quaternion.LookRotation(toward.normalized, Vector3.up);
        }

        private CreepView MakeCreepView()
        {
            var host = new GameObject("Creep");
            host.transform.SetParent(_creepParent, worldPositionStays: false);

            var view = host.AddComponent<CreepView>();
            view.Build(_art.CreepModel, _art.CreepWalkClip, _art.CreepDeathClip);

            return view;
        }

        private ProjectileView MakeProjectileView()
        {
            var host = new GameObject("Shell");
            host.transform.SetParent(_projectileParent, worldPositionStays: false);

            var view = host.AddComponent<ProjectileView>();
            view.Build(_projectileMaterial);

            return view;
        }

        private Transform MakeGroup(string name)
        {
            var group = new GameObject(name);
            group.transform.SetParent(transform, worldPositionStays: false);

            return group.transform;
        }

        // ---------------------------------------------------------------
        // Bookkeeping
        // ---------------------------------------------------------------

        /// <summary>
        /// Indexes a snapshot by id so the next frame can pair entities up.
        /// </summary>
        /// <remarks>
        /// Dictionaries reused rather than rebuilt, because this runs thirty
        /// times a second for the length of the match and a fresh allocation
        /// per tick is garbage the collector has to find later, in the middle
        /// of somebody watching.
        /// </remarks>
        private void RememberPrevious(Snapshot snapshot)
        {
            _previousCreeps.Clear();
            _previousTowers.Clear();
            _previousProjectiles.Clear();

            foreach (CreepSnapshot creep in snapshot.Creeps)
            {
                _previousCreeps[creep.Id] = creep;
            }

            foreach (TowerSnapshot tower in snapshot.Towers)
            {
                _previousTowers[tower.Id] = tower;
            }

            foreach (ProjectileSnapshot shell in snapshot.Projectiles)
            {
                _previousProjectiles[shell.Id] = shell;
            }
        }

        /// <summary>
        /// Where a creep was last drawn — what the decorations aim at.
        /// </summary>
        /// <remarks>
        /// Events arrive during <see cref="Match.Advance(int, IMatchEvents)"/>,
        /// before the snapshot for that tick has been pulled, so this answers
        /// with the frame that was drawn a moment ago. At most one tick behind,
        /// and it is allowed to be: everything that reads it is decoration, and
        /// decoration is by rule not load-bearing for what the match looks like
        /// at a given tick.
        /// </remarks>
        private Vector3? CreepPositionOf(int creepId) =>
            _drawnCreepPositions.TryGetValue(creepId, out Vector3 at) ? at : (Vector3?)null;

        private Vector3? TowerMuzzleOf(int towerId) =>
            _towers.TryGetValue(towerId, out TowerView view) ? view.Muzzle : (Vector3?)null;
    }
}
