using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Everything the event stream is allowed to draw: tracers, muzzle flashes,
    /// hit sparks and the shapes a bubble leaves — drawn, and then forgotten.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing in this file is load-bearing for what the match looks like at
    /// a given tick.</b> That is the rule events exist under, and it is what
    /// makes this whole class safe to delete at any moment. Delete every
    /// effect mid-flight and the playfield still shows the same creeps in the
    /// same places doing the same things, because all of that comes from the
    /// snapshot. If anything here ever became necessary to understand the
    /// match, it would have to move into the snapshot instead.
    /// </para>
    /// <para>
    /// <b>The rule is enforced by the interface's shape, not by this
    /// paragraph.</b> Every parameter of every event is an entity id, a count,
    /// or a value off the emitter's row — no positions, no durations, no
    /// references to hold on to. There is nothing here to build state out of,
    /// and where a decoration needs a position it looks the id up in the
    /// snapshot the view is drawing.
    /// </para>
    /// <para>
    /// <b>Effects age in simulation ticks, so nothing here runs on a clock of
    /// its own.</b> That was not the first design and the first design was
    /// wrong twice: wall-clock lifetimes leak, because effects only age where
    /// somebody remembers to age them, and they are inconsistent under
    /// fast-forward, where the match speeds up and the decoration does not.
    /// Aging on the tick puts it in the one place it cannot be forgotten.
    /// <see cref="Clear"/> still exists for seeks: a seek re-simulates, the
    /// events of the re-run ticks are discarded by nobody subscribing, and any
    /// effect still fading from before the seek belongs to a tick that is now
    /// in the future.
    /// </para>
    /// <para>
    /// <b>Real geometry only.</b> A tracer is a thin stretched box, a spark is
    /// a small sphere, a bubble's ring is a flat cylinder and a capstone's
    /// signature is a mesh of solid bars out of <see cref="EffectMeshes"/>,
    /// because the camera orbits freely and the standing art rule is that
    /// nothing may turn to face it. Unity's line renderers and default
    /// particles both billboard, so neither is used here — which is a
    /// constraint that showed up as a choice of primitive rather than as a
    /// problem.
    /// </para>
    /// <para>
    /// <b>A row's bubble is drawn as its own row says.</b> Every bubble drew
    /// one shared disc until the capstones needed telling apart; now the entity
    /// an event names is turned into the row that emitted it and that row's
    /// <see cref="EffectSignature"/> picks the shape. What a row with no
    /// signature draws is still the disc, unchanged.
    /// </para>
    /// </remarks>
    public sealed class MatchDecorations : IMatchEvents
    {
        private readonly Transform _parent;

        private readonly Func<int, Vector3?> _creepPosition;

        private readonly Func<int, Vector3?> _towerMuzzle;

        private readonly Func<int, Vector3?> _entityGround;

        private readonly Func<int, EffectSignature?> _towerSignature;

        private readonly Func<int, float, IReadOnlyList<Vector3>> _towersWithin;

        private readonly List<Effect> _active = new List<Effect>();

        private readonly Dictionary<Piece, Stack<Transform>> _idle =
            new Dictionary<Piece, Stack<Transform>>();

        private readonly Dictionary<Piece, Material> _materials = new Dictionary<Piece, Material>();

        private readonly Dictionary<Piece, Mesh> _meshes = new Dictionary<Piece, Mesh>();

        private readonly Dictionary<Piece, int> _drawn = new Dictionary<Piece, int>();

        private Transform _host;

        /// <summary>
        /// Builds the decorations under <paramref name="parent"/>.
        /// </summary>
        /// <param name="parent">Where the effect objects hang.</param>
        /// <param name="creepPosition">
        /// Where a creep is right now, or null if it is not in the snapshot.
        /// Asked of the view, so an effect aimed at something that has already
        /// gone simply does not appear.
        /// </param>
        /// <param name="towerMuzzle">Where a tower's shots leave from.</param>
        /// <param name="entityGround">
        /// Where a creep or a tower is standing, or null if it is not on the
        /// board. One lookup for both, because the simulation gives towers,
        /// creeps and projectiles ids out of one space and a bubble's centre
        /// can be a tower or a creep depending on which column the row filled
        /// in.
        /// </param>
        /// <param name="towerSignature">
        /// What the tower with this id draws its bubble as, or null when the id
        /// is not a tower the view is holding. <b>The null is load-bearing and
        /// is not the same answer as <see cref="EffectSignature.None"/>:</b> a
        /// tower that draws the plain disc and a creep that a shell arrived at
        /// are different cases and <see cref="BlastLanded"/> draws them
        /// differently.
        /// </param>
        /// <param name="towersWithin">
        /// Where every tower standing within so many metres of an entity is —
        /// what the Blessing's glow is drawn on. Positions rather than ids,
        /// because nothing here would do anything with an id but ask for the
        /// position.
        /// </param>
        public MatchDecorations(
            Transform parent,
            Func<int, Vector3?> creepPosition,
            Func<int, Vector3?> towerMuzzle,
            Func<int, Vector3?> entityGround,
            Func<int, EffectSignature?> towerSignature,
            Func<int, float, IReadOnlyList<Vector3>> towersWithin)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _creepPosition = creepPosition ?? throw new ArgumentNullException(nameof(creepPosition));
            _towerMuzzle = towerMuzzle ?? throw new ArgumentNullException(nameof(towerMuzzle));
            _entityGround = entityGround ?? throw new ArgumentNullException(nameof(entityGround));
            _towerSignature = towerSignature ?? throw new ArgumentNullException(nameof(towerSignature));
            _towersWithin = towersWithin ?? throw new ArgumentNullException(nameof(towersWithin));

            _materials[Piece.Tracer] = ViewMaterials.Create("Tracer", MatchTuning.TracerColor);
            _materials[Piece.MuzzleFlash] = ViewMaterials.Create("MuzzleFlash", MatchTuning.MuzzleFlashColor);
            _materials[Piece.Spark] = ViewMaterials.Create("HitSpark", MatchTuning.HitSparkColor);
            _materials[Piece.BubbleRing] = ViewMaterials.Create("BubbleRing", MatchTuning.BubbleRingColor);
            _materials[Piece.SlowRing] = ViewMaterials.Create("SlowRing", MatchTuning.SlowRingColor);
            _materials[Piece.GroundShock] =
                ViewMaterials.Create("GroundShock", MatchTuning.GroundShockColor);
            _materials[Piece.TowerGlow] = ViewMaterials.Create("TowerGlow", MatchTuning.BlessingGlowColor);
            _materials[Piece.MortarBurst] =
                ViewMaterials.Create("MortarBurst", MatchTuning.MortarBurstColor);
        }

        /// <summary>
        /// The pooled objects, one pool each. A signature is its own kind
        /// rather than its mesh's kind — the slow ring and the tower glow are
        /// the same ring at two sizes in two colours, and keeping them apart is
        /// what lets anything looking at the playfield tell which one it found.
        /// </summary>
        private enum Piece
        {
            Tracer,
            MuzzleFlash,
            Spark,
            BubbleRing,
            SlowRing,
            GroundShock,
            TowerGlow,
            MortarBurst,
        }

        /// <summary>How many effects are on screen. For tests.</summary>
        public int ActiveCount => _active.Count;

        /// <summary>
        /// How many events this has ever been told about. For tests, and
        /// deliberately not reset by <see cref="Clear"/>.
        /// </summary>
        /// <remarks>
        /// The counter a seek has to answer to. What a seek claims is that the
        /// re-run ticks' events never arrived — which is a different and much
        /// stronger thing than their effects having been tidied up afterwards,
        /// and the two are indistinguishable from anything that resets here.
        /// </remarks>
        public int EventsHeard { get; private set; }

        /// <summary>How many tracers have been drawn since the last clear. For tests.</summary>
        /// <remarks>
        /// <b>Every count below reads one kind out of one tally, and the tally
        /// is kept where an effect is registered rather than beside each place
        /// that draws one.</b> A counter incremented at the drawing site is a
        /// counter the next shape can be written without, and a shape that
        /// draws correctly while reporting nothing is invisible to every test
        /// that would have caught it.
        /// </remarks>
        public int TracersDrawn => Drawn(Piece.Tracer);

        /// <summary>
        /// How many muzzle flashes have been drawn since the last clear. For
        /// tests, and the count that says a row fired from its anchor at all.
        /// </summary>
        public int FlashesDrawn => Drawn(Piece.MuzzleFlash);

        /// <summary>How many hit sparks have been drawn since the last clear. For tests.</summary>
        public int SparksDrawn => Drawn(Piece.Spark);

        /// <summary>How many plain bubble discs have been drawn since the last clear. For tests.</summary>
        public int RingsDrawn => Drawn(Piece.BubbleRing);

        /// <summary>How many slow rings have been drawn since the last clear. For tests.</summary>
        public int SlowRingsDrawn => Drawn(Piece.SlowRing);

        /// <summary>How many ground shocks have been drawn since the last clear. For tests.</summary>
        public int ShocksDrawn => Drawn(Piece.GroundShock);

        /// <summary>
        /// How many tower glows have been drawn since the last clear. For
        /// tests. One per tower reached, so a single pulse over four towers
        /// counts four.
        /// </summary>
        public int GlowsDrawn => Drawn(Piece.TowerGlow);

        /// <summary>How many bursts have been drawn since the last clear. For tests.</summary>
        public int BurstsDrawn => Drawn(Piece.MortarBurst);

        /// <summary>A tower released a shot: a tracer if it is hitscan, a flash either way.</summary>
        public void TowerFired(int towerId, int targetId)
        {
            EventsHeard++;

            Vector3? muzzle = _towerMuzzle(towerId);

            if (!muzzle.HasValue)
            {
                return;
            }

            Sphere(
                Piece.MuzzleFlash,
                muzzle.Value,
                MatchTuning.MuzzleFlashRadius,
                MatchTuning.MuzzleFlashTicks);

            Vector3? target = _creepPosition(targetId);

            if (target.HasValue)
            {
                Tracer(muzzle.Value, target.Value + (Vector3.up * MatchTuning.HitSparkHeight));
            }
        }

        /// <summary>Damage landed: a spark on the creep it landed on.</summary>
        public void CreepDamaged(int creepId, int amount)
        {
            EventsHeard++;

            Vector3? at = _creepPosition(creepId);

            if (!at.HasValue)
            {
                return;
            }

            Sphere(
                Piece.Spark,
                at.Value + (Vector3.up * MatchTuning.HitSparkHeight),
                MatchTuning.HitSparkRadius,
                MatchTuning.HitSparkTicks);
        }

        /// <summary>
        /// A creep began dying. Nothing is drawn: the death is the death clip,
        /// and the death clip is driven by the snapshot's <c>Dying</c> state
        /// for exactly the ticks the simulation gave it. Decorating it here
        /// would be a second, shorter opinion about how long a death lasts.
        /// </summary>
        public void CreepDied(int creepId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// A creep reached the exit. Nothing yet — the exit has no marker to
        /// flash, and inventing one would be an art decision this ticket does
        /// not get to make.
        /// </summary>
        public void CreepLeaked(int creepId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// A shot lost its target mid-flight. Nothing is drawn, and that is the
        /// interesting case rather than an omission: the projectile stops being
        /// in the snapshot on this tick, so its object is already going back in
        /// the pool by subtraction. A fizzle here would be decoration on top of
        /// a disappearance that has already happened correctly without it.
        /// </summary>
        public void ProjectileOrphaned(int projectileId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// One creep drew ahead of another. Nothing is drawn: the overtake is
        /// visible because the two are on different lateral offsets and both
        /// are being drawn from the snapshot, which is the point of the
        /// landmark row.
        /// </summary>
        public void CreepOvertook(int creepId, int overtakenCreepId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// A bubble went off with a shot: the emitting row's signature, at the
        /// size the bubble reached.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A sweep names its shooter and a blast names its victim, and that
        /// is the whole of why this method is two cases.</b> A bubble centred
        /// on itself is centred on the tower that fired it, so the centre is
        /// the emitter and its row's signature is reachable — that is the
        /// Slam's ground shock. A bubble centred on its target is centred on
        /// the body the shot arrived at, and the shooter is not in the event at
        /// all: a mortar's shell carries its type and its target, there are
        /// several mortars, and reading the firing tower off an earlier
        /// <c>TowerFired</c> would be building state out of an event stream
        /// that seeks discard. So a blast that arrived on a body is drawn as a
        /// burst at the radius it reached, which is the shape the Mortar's line
        /// signs — and which every target-centred blast in the game therefore
        /// wears, the Mage's and the Sorcerer's splash included.
        /// </para>
        /// <para>
        /// <b><paramref name="payload"/> is read by nothing here, on purpose.</b>
        /// A bubble carrying damage gets the same look as one carrying a slow:
        /// what a payload should look like is a decision nobody has taken, and
        /// inventing four colours to have used the parameter would be taking
        /// it. What the payload then does to a unit is that unit's own picture
        /// and not this one's — see <see cref="EffectMarks"/>.
        /// </para>
        /// </remarks>
        public void BlastLanded(int centreId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;

            EffectSignature? emitter = _towerSignature(centreId);

            if (emitter.HasValue)
            {
                Signature(emitter.Value, centreId, radiusMilliHex);

                return;
            }

            Burst(centreId, radiusMilliHex);
        }

        /// <summary>
        /// A bubble pulsed on its own clock: the emitting row's signature,
        /// under or over whatever is emitting it.
        /// </summary>
        /// <remarks>
        /// The emitter is the centre here in every case, so the row is always
        /// reachable — except that a creep can carry an aura too, and no creep
        /// has a signature. Those draw the plain disc, which is what every
        /// bubble drew before any row had one.
        /// </remarks>
        public void AuraPulsed(int emitterId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;

            Signature(
                _towerSignature(emitterId) ?? EffectSignature.None, emitterId, radiusMilliHex);
        }

        /// <summary>
        /// Ages every effect by one simulation tick and retires the ones that
        /// are done.
        /// </summary>
        /// <remarks>
        /// Called from where the simulation advances, which is the only place
        /// that cannot forget to call it. A view driven a tick at a time by a
        /// capture tool or a scrub bar therefore ages its decoration correctly
        /// without knowing it has any.
        /// </remarks>
        public void AgeOneTick()
        {
            for (int index = _active.Count - 1; index >= 0; index--)
            {
                Effect effect = _active[index];
                effect.Elapsed++;

                if (effect.Elapsed >= effect.Lifetime)
                {
                    Retire(effect);
                    _active.RemoveAt(index);

                    continue;
                }

                // Shrinking to nothing rather than fading to nothing, because
                // fading needs a transparent material and transparency needs a
                // sort order, and a sort order is one more thing that can
                // disagree with itself as the camera yaws.
                if (effect.Shrinks)
                {
                    float remaining = 1f - (effect.Elapsed / (float)effect.Lifetime);
                    effect.Transform.localScale = effect.FullScale * remaining;
                }

                _active[index] = effect;
            }
        }

        /// <summary>
        /// Drops every effect immediately. What a seek does, because the events
        /// of the ticks a seek re-runs were discarded and anything still on
        /// screen belongs to a tick that has not happened yet.
        /// </summary>
        public void Clear()
        {
            foreach (Effect effect in _active)
            {
                Retire(effect);
            }

            _active.Clear();
            _drawn.Clear();
        }

        /// <summary>
        /// Destroys the materials and the meshes this made. What the view calls
        /// when it is destroyed.
        /// </summary>
        /// <remarks>
        /// <b>Whoever made it destroys it.</b> A material and a mesh are both
        /// asset instances and destroying the object that draws with one does
        /// not destroy it, so these outlive the match unless somebody says
        /// otherwise. It never showed while one match was the whole session and
        /// three orphans were a constant; a run begins a match a round, and
        /// thirty over ten waves is a leak with a shape. Same rule and the same
        /// reasoning as <see cref="PlaybackControls"/>'s panel settings and
        /// <see cref="BuildBoard"/>'s hex light.
        /// </remarks>
        public void DestroyAssets()
        {
            foreach (Material material in _materials.Values)
            {
                UnityEngine.Object.Destroy(material);
            }

            foreach (Mesh mesh in _meshes.Values)
            {
                UnityEngine.Object.Destroy(mesh);
            }

            _materials.Clear();
            _meshes.Clear();
        }

        private void Tracer(Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            float length = along.magnitude;

            if (length < 1e-4f)
            {
                return;
            }

            Transform box = Take(Piece.Tracer);
            box.SetPositionAndRotation(
                (from + to) * 0.5f,
                Quaternion.LookRotation(along / length, Vector3.up));

            var scale = new Vector3(MatchTuning.TracerThickness, MatchTuning.TracerThickness, length);

            Draw(Piece.Tracer, box, scale, MatchTuning.TracerTicks, shrinks: true);
        }

        private void Sphere(Piece piece, Vector3 at, float radius, int lifetimeTicks)
        {
            Transform sphere = Take(piece);
            sphere.position = at;

            Draw(piece, sphere, Vector3.one * (radius * 2f), lifetimeTicks, shrinks: true);
        }

        /// <summary>
        /// The shape one row's bubble leaves, at the size the bubble reached.
        /// </summary>
        private void Signature(EffectSignature signature, int centreId, int radiusMilliHex)
        {
            switch (signature)
            {
                // The Shield Wall's: an open ring at the edge of the slow, so
                // the bodies caught inside it stay visible through it.
                case EffectSignature.SlowRing:
                    Flat(Piece.SlowRing, centreId, radiusMilliHex, MatchTuning.SlowRingTicks);
                    break;

                // The Slam's: cracks running out from under the man who swung
                // to the edge of what the swing reached.
                case EffectSignature.GroundShock:
                    Flat(Piece.GroundShock, centreId, radiusMilliHex, MatchTuning.GroundShockTicks);
                    break;

                case EffectSignature.TowerGlow:
                    TowerGlow(centreId, radiusMilliHex);
                    break;

                default:
                    Ring(centreId, radiusMilliHex);
                    break;
            }
        }

        /// <summary>
        /// The disc a bubble leaves when its row has no signature of its own: a
        /// flat cylinder on the ground under the entity it was centred on, as
        /// wide as the bubble reached.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing is drawn for a bubble that reached only its centre.</b>
        /// A radius of zero is a real authoring in the simulation — the
        /// single-target slow — and it is a ring of no size, so the ring is the
        /// wrong instrument for it and drawing a speck would be a worse answer
        /// than drawing nothing. What that bubble did to the one body it found
        /// is that body's own picture.
        /// </para>
        /// <para>
        /// <b>Nothing is drawn for an id the view is not holding either.</b>
        /// Same rule as a spark aimed at a creep that has gone: the position
        /// comes from the snapshot the view is drawing, so a centre it does not
        /// carry has nowhere to be.
        /// </para>
        /// <para>
        /// <b>It is under the body and the sphere was measured from the cell
        /// under the body</b>, which are up to half a hex apart while a creep
        /// is walking between two of them. A tower's are the same point. The
        /// disc is deliberately not an accurate footprint — the event carries
        /// an id and never a position, so drawing the exact circle would mean
        /// re-deriving the route cell here to agree with a rule that lives in
        /// the simulation, which is a second opinion about what a bubble
        /// enclosed and a much larger thing than a placeholder.
        /// </para>
        /// </remarks>
        private void Ring(int centreId, int radiusMilliHex)
        {
            if (!Reached(centreId, radiusMilliHex, out Vector3 at, out float diameter))
            {
                return;
            }

            Transform disc = Take(Piece.BubbleRing);
            disc.position = at + (Vector3.up * MatchTuning.BubbleRingHeight);

            // A Unity cylinder is one unit across and two tall, so a diameter
            // goes into x and z unchanged and the thickness is halved into y.
            var scale = new Vector3(diameter, MatchTuning.BubbleRingThickness * 0.5f, diameter);

            Draw(Piece.BubbleRing, disc, scale, MatchTuning.BubbleRingTicks, shrinks: false);
        }

        /// <summary>
        /// The Blessing's: a ring over the head of every tower the pulse
        /// reached, the emitter included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The one signature drawn on what a bubble found rather than on the
        /// bubble.</b> A ring on the ground would say how far the blessing
        /// carries, which is true and is not what this line is for: what the
        /// Blessing does is make other towers fire faster, so the thing worth
        /// seeing is which towers those are. It is a fixed size over each of
        /// them, because it stands for a tower being blessed and not for a
        /// distance.
        /// </para>
        /// <para>
        /// <b>Who is inside is measured flat, against where the towers are
        /// drawn.</b> The simulation reads the same radius as a sphere and adds
        /// half a hex per level of height; nothing that draws a bubble asks how
        /// tall the ground is, the same silence the disc keeps. A glow is
        /// decoration, so a tower a level up that the pulse just missed is a
        /// halo drawn on a tower whose cooldown did not move — visible, and
        /// cheaper than a second opinion about a simulation rule.
        /// </para>
        /// </remarks>
        private void TowerGlow(int emitterId, int radiusMilliHex)
        {
            if (radiusMilliHex <= 0)
            {
                return;
            }

            IReadOnlyList<Vector3> reached =
                _towersWithin(emitterId, SimUnits.MetresFromMilliHex(radiusMilliHex));

            for (var index = 0; index < reached.Count; index++)
            {

                Transform glow = Take(Piece.TowerGlow);
                glow.position = reached[index] + (Vector3.up * MatchTuning.BlessingGlowHeight);

                Draw(
                    Piece.TowerGlow,
                    glow,
                    Flattened(MatchTuning.BlessingGlowDiameter),
                    MatchTuning.BlessingGlowTicks,
                    shrinks: false);
            }
        }

        /// <summary>
        /// The Mortar's: shards thrown out of the body the shell arrived at, to
        /// the edge of what the blast reached.
        /// </summary>
        private void Burst(int centreId, int radiusMilliHex)
        {
            if (!Reached(centreId, radiusMilliHex, out Vector3 at, out float diameter))
            {
                return;
            }

            Transform burst = Take(Piece.MortarBurst);
            burst.position = at + (Vector3.up * MatchTuning.HitSparkHeight);

            // Uniform, unlike the flat shapes: a burst leaves the ground as
            // well as crossing it, so its height is part of the radius it is
            // reporting.
            Draw(
                Piece.MortarBurst,
                burst,
                Vector3.one * diameter,
                MatchTuning.MortarBurstTicks,
                shrinks: false);
        }

        /// <summary>
        /// One of the shapes that lies on the floor, laid flat under whatever
        /// the bubble was centred on and as wide as the bubble reached.
        /// </summary>
        private void Flat(Piece piece, int centreId, int radiusMilliHex, int lifetimeTicks)
        {
            if (!Reached(centreId, radiusMilliHex, out Vector3 at, out float diameter))
            {
                return;
            }

            Transform drawn = Take(piece);
            drawn.position = at + (Vector3.up * MatchTuning.BubbleRingHeight);

            Draw(piece, drawn, Flattened(diameter), lifetimeTicks, shrinks: false);
        }

        /// <summary>
        /// The scale that takes a mesh built at
        /// <see cref="EffectMeshes.OuterRadius"/> out to
        /// <paramref name="diameter"/> across while leaving how far it stands
        /// off the floor alone.
        /// </summary>
        private static Vector3 Flattened(float diameter) => new Vector3(diameter, 1f, diameter);

        /// <summary>
        /// Where a bubble went off and how wide it was, or false for one there
        /// is nothing to draw for.
        /// </summary>
        private bool Reached(int centreId, int radiusMilliHex, out Vector3 at, out float diameter)
        {
            at = default;
            diameter = 0f;

            if (radiusMilliHex <= 0)
            {
                return false;
            }

            Vector3? ground = _entityGround(centreId);

            if (!ground.HasValue)
            {
                return false;
            }

            at = ground.Value;
            diameter = 2f * SimUnits.MetresFromMilliHex(radiusMilliHex);

            return true;
        }

        /// <summary>
        /// Sizes, surfaces and registers one effect that has already been
        /// placed.
        /// </summary>
        private void Draw(Piece piece, Transform drawn, Vector3 scale, int lifetimeTicks, bool shrinks)
        {
            drawn.localScale = scale;
            drawn.GetComponent<MeshRenderer>().sharedMaterial = _materials[piece];

            _drawn.TryGetValue(piece, out int already);
            _drawn[piece] = already + 1;

            _active.Add(new Effect
            {
                Transform = drawn,
                FullScale = scale,
                Lifetime = lifetimeTicks,
                Piece = piece,
                Shrinks = shrinks,
            });
        }

        /// <summary>How many of one kind have been drawn since the last clear.</summary>
        private int Drawn(Piece piece) => _drawn.TryGetValue(piece, out int count) ? count : 0;

        private Transform Take(Piece piece)
        {
            if (_idle.TryGetValue(piece, out Stack<Transform> idle) && idle.Count > 0)
            {
                Transform reused = idle.Pop();
                reused.gameObject.SetActive(true);

                return reused;
            }

            return Make(piece);
        }

        /// <summary>
        /// The mesh one signature is drawn with, built the first time it is
        /// asked for.
        /// </summary>
        /// <remarks>
        /// Lazily, because a match whose roster authors no bubble should not
        /// pay for three meshes it never draws — which is every match of the
        /// shipped content that stands no capstone.
        /// </remarks>
        private Mesh MeshFor(Piece piece)
        {
            if (_meshes.TryGetValue(piece, out Mesh built))
            {
                return built;
            }

            Mesh made = piece switch
            {
                Piece.GroundShock => EffectMeshes.Cracks(
                    MatchTuning.GroundShockCracks,
                    MatchTuning.GroundShockInnerFraction * EffectMeshes.OuterRadius,
                    MatchTuning.GroundShockWidthFraction * EffectMeshes.OuterRadius,
                    MatchTuning.GroundShockThickness),

                Piece.MortarBurst => EffectMeshes.Burst(
                    MatchTuning.MortarBurstShards,
                    MatchTuning.MortarBurstWidthFraction * EffectMeshes.OuterRadius),

                // The slow ring and the tower glow are one ring at two sizes,
                // so they are one mesh. Their pools stay separate because their
                // colours and lifetimes are not the same.
                _ => EffectMeshes.Ring(
                    MatchTuning.SignatureRingSides,
                    MatchTuning.SignatureRingBandFraction * EffectMeshes.OuterRadius,
                    MatchTuning.SignatureRingThickness),
            };

            _meshes[piece] = made;

            return made;
        }

        private Transform Make(Piece piece)
        {
            if (_host == null)
            {
                var host = new GameObject("Decorations");
                host.transform.SetParent(_parent, worldPositionStays: false);
                _host = host.transform;
            }

            GameObject instance = piece switch
            {
                Piece.Tracer => GameObject.CreatePrimitive(PrimitiveType.Cube),
                Piece.MuzzleFlash => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                Piece.Spark => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                Piece.BubbleRing => GameObject.CreatePrimitive(PrimitiveType.Cylinder),
                _ => Built(piece),
            };

            instance.name = piece.ToString();
            instance.transform.SetParent(_host, worldPositionStays: false);

            Collider collider = instance.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            // Decoration casts no shadow. A tenth-of-a-second box throwing a
            // hard shadow across the floor reads as a bug in the lighting, and
            // real shadows are what the floor and the units are for.
            instance.GetComponent<MeshRenderer>().shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            return instance.transform;
        }

        /// <summary>
        /// One object carrying a generated mesh, for the shapes no Unity
        /// primitive is.
        /// </summary>
        private GameObject Built(Piece piece)
        {
            var instance = new GameObject();
            instance.AddComponent<MeshFilter>().sharedMesh = MeshFor(piece);
            instance.AddComponent<MeshRenderer>();

            return instance;
        }

        private void Retire(Effect effect)
        {
            if (effect.Transform == null)
            {
                return;
            }

            effect.Transform.gameObject.SetActive(false);

            if (!_idle.TryGetValue(effect.Piece, out Stack<Transform> idle))
            {
                idle = new Stack<Transform>();
                _idle[effect.Piece] = idle;
            }

            idle.Push(effect.Transform);
        }

        private struct Effect
        {
            public Transform Transform;

            public Vector3 FullScale;

            public int Lifetime;

            public int Elapsed;

            /// <summary>
            /// Which pool this goes back into when it retires. Carried rather
            /// than worked out from the object, because a shape on screen
            /// cannot be asked which pool it came out of.
            /// </summary>
            public Piece Piece;

            /// <summary>
            /// Whether it closes down to nothing as it ages.
            /// </summary>
            /// <remarks>
            /// A tracer, a flash and a spark do: their size is how loud they
            /// are, so it going away is them going away. Everything a bubble
            /// leaves does not, and that is the one place the two differ — its
            /// size is the whole of what it says, so one that shrank would be
            /// reporting a reach the bubble did not have for every tick but its
            /// first.
            /// </remarks>
            public bool Shrinks;
        }
    }
}
