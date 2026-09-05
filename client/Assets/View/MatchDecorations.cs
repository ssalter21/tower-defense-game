using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Everything the event stream is allowed to draw: tracers, muzzle flashes,
    /// hit sparks and the rings a bubble leaves — drawn, and then forgotten.
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
    /// a small sphere and a bubble's ring is a flat cylinder, because the
    /// camera orbits freely and the standing art rule is that nothing may turn
    /// to face it. Unity's line renderers and default particles both billboard,
    /// so neither is used here — which is a constraint that showed up as a
    /// choice of primitive rather than as a problem.
    /// </para>
    /// </remarks>
    public sealed class MatchDecorations : IMatchEvents
    {
        private readonly Transform _parent;

        private readonly Func<int, Vector3?> _creepPosition;

        private readonly Func<int, Vector3?> _towerMuzzle;

        private readonly Func<int, Vector3?> _entityGround;

        private readonly List<Effect> _active = new List<Effect>();

        private readonly Stack<Transform> _idleBoxes = new Stack<Transform>();

        private readonly Stack<Transform> _idleSpheres = new Stack<Transform>();

        private readonly Stack<Transform> _idleDiscs = new Stack<Transform>();

        private readonly Material _tracerMaterial;

        private readonly Material _muzzleMaterial;

        private readonly Material _sparkMaterial;

        private readonly Material _ringMaterial;

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
        public MatchDecorations(
            Transform parent,
            Func<int, Vector3?> creepPosition,
            Func<int, Vector3?> towerMuzzle,
            Func<int, Vector3?> entityGround)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _creepPosition = creepPosition ?? throw new ArgumentNullException(nameof(creepPosition));
            _towerMuzzle = towerMuzzle ?? throw new ArgumentNullException(nameof(towerMuzzle));
            _entityGround = entityGround ?? throw new ArgumentNullException(nameof(entityGround));

            _tracerMaterial = ViewMaterials.Create("Tracer", MatchTuning.TracerColor);
            _muzzleMaterial = ViewMaterials.Create("MuzzleFlash", MatchTuning.MuzzleFlashColor);
            _sparkMaterial = ViewMaterials.Create("HitSpark", MatchTuning.HitSparkColor);
            _ringMaterial = ViewMaterials.Create("BubbleRing", MatchTuning.BubbleRingColor);
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
        public int TracersDrawn { get; private set; }

        /// <summary>How many hit sparks have been drawn since the last clear. For tests.</summary>
        public int SparksDrawn { get; private set; }

        /// <summary>How many bubble rings have been drawn since the last clear. For tests.</summary>
        public int RingsDrawn { get; private set; }

        /// <summary>A tower released a shot: a tracer if it is hitscan, a flash either way.</summary>
        public void TowerFired(int towerId, int targetId)
        {
            EventsHeard++;

            Vector3? muzzle = _towerMuzzle(towerId);

            if (!muzzle.HasValue)
            {
                return;
            }

            Sphere(muzzle.Value, MatchTuning.MuzzleFlashRadius, MatchTuning.MuzzleFlashTicks, _muzzleMaterial);

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

            SparksDrawn++;
            Sphere(
                at.Value + (Vector3.up * MatchTuning.HitSparkHeight),
                MatchTuning.HitSparkRadius,
                MatchTuning.HitSparkTicks,
                _sparkMaterial);
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
        /// A bubble went off with a shot: a ring on the ground under whatever
        /// it was centred on, at the size the bubble reached.
        /// </summary>
        /// <remarks>
        /// <b><paramref name="payload"/> is read by nothing here, on purpose.</b>
        /// A blast and a pulse get one look, and a bubble carrying damage gets
        /// the same look as one carrying a slow: telling them apart is a design
        /// decision nobody has taken, and inventing four colours to have used
        /// the parameter would be taking it. What the payload then does to a
        /// unit is that unit's own picture and not this one's.
        /// </remarks>
        public void BlastLanded(int centreId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;
            Ring(centreId, radiusMilliHex);
        }

        /// <summary>
        /// A bubble pulsed on its own clock: the same ring, under the emitter,
        /// and the same silence about what it carried.
        /// </summary>
        public void AuraPulsed(int emitterId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;
            Ring(emitterId, radiusMilliHex);
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
            TracersDrawn = 0;
            SparksDrawn = 0;
            RingsDrawn = 0;
        }

        /// <summary>
        /// Destroys the four materials this made. What the view calls when it
        /// is destroyed.
        /// </summary>
        /// <remarks>
        /// <b>Whoever made it destroys it.</b> A material is an asset instance
        /// and destroying the object that draws with it does not destroy it, so
        /// these outlive the match unless somebody says otherwise. It never
        /// showed while one match was the whole session and three orphans were a
        /// constant; a run begins a match a round, and thirty over ten waves is
        /// a leak with a shape. Same rule and the same reasoning as
        /// <see cref="PlaybackControls"/>'s panel settings and
        /// <see cref="BuildBoard"/>'s hex light.
        /// </remarks>
        public void DestroyMaterials()
        {
            UnityEngine.Object.Destroy(_tracerMaterial);
            UnityEngine.Object.Destroy(_muzzleMaterial);
            UnityEngine.Object.Destroy(_sparkMaterial);
            UnityEngine.Object.Destroy(_ringMaterial);
        }

        private void Tracer(Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            float length = along.magnitude;

            if (length < 1e-4f)
            {
                return;
            }

            TracersDrawn++;

            Transform box = TakeBox();
            box.SetPositionAndRotation(
                (from + to) * 0.5f,
                Quaternion.LookRotation(along / length, Vector3.up));

            var scale = new Vector3(MatchTuning.TracerThickness, MatchTuning.TracerThickness, length);
            box.localScale = scale;
            box.GetComponent<MeshRenderer>().sharedMaterial = _tracerMaterial;

            _active.Add(new Effect
            {
                Transform = box,
                FullScale = scale,
                Lifetime = MatchTuning.TracerTicks,
                Idle = _idleBoxes,
                Shrinks = true,
            });
        }

        private void Sphere(Vector3 at, float radius, int lifetimeTicks, Material material)
        {
            Transform sphere = TakeSphere();
            sphere.position = at;

            var scale = Vector3.one * (radius * 2f);
            sphere.localScale = scale;
            sphere.GetComponent<MeshRenderer>().sharedMaterial = material;

            _active.Add(new Effect
            {
                Transform = sphere,
                FullScale = scale,
                Lifetime = lifetimeTicks,
                Idle = _idleSpheres,
                Shrinks = true,
            });
        }

        /// <summary>
        /// The ring a bubble leaves: a disc on the ground under the entity it
        /// was centred on, as wide as the bubble reached.
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
            if (radiusMilliHex <= 0)
            {
                return;
            }

            Vector3? at = _entityGround(centreId);

            if (!at.HasValue)
            {
                return;
            }

            RingsDrawn++;

            Transform disc = TakeDisc();
            disc.position = at.Value + (Vector3.up * MatchTuning.BubbleRingHeight);

            // A Unity cylinder is one unit across and two tall, so a diameter
            // goes into x and z unchanged and the thickness is halved into y.
            float diameter = 2f * SimUnits.MetresFromMilliHex(radiusMilliHex);
            var scale = new Vector3(diameter, MatchTuning.BubbleRingThickness * 0.5f, diameter);

            disc.localScale = scale;
            disc.GetComponent<MeshRenderer>().sharedMaterial = _ringMaterial;

            _active.Add(new Effect
            {
                Transform = disc,
                FullScale = scale,
                Lifetime = MatchTuning.BubbleRingTicks,
                Idle = _idleDiscs,
                Shrinks = false,
            });
        }

        private Transform TakeBox() =>
            _idleBoxes.Count > 0 ? Reactivate(_idleBoxes.Pop()) : Make(PrimitiveType.Cube, "Tracer");

        private Transform TakeSphere() =>
            _idleSpheres.Count > 0 ? Reactivate(_idleSpheres.Pop()) : Make(PrimitiveType.Sphere, "Spark");

        private Transform TakeDisc() =>
            _idleDiscs.Count > 0 ? Reactivate(_idleDiscs.Pop()) : Make(PrimitiveType.Cylinder, "BubbleRing");

        private static Transform Reactivate(Transform transform)
        {
            transform.gameObject.SetActive(true);

            return transform;
        }

        private Transform Make(PrimitiveType shape, string name)
        {
            if (_host == null)
            {
                var host = new GameObject("Decorations");
                host.transform.SetParent(_parent, worldPositionStays: false);
                _host = host.transform;
            }

            GameObject instance = GameObject.CreatePrimitive(shape);
            instance.name = name;
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

        private void Retire(Effect effect)
        {
            if (effect.Transform == null)
            {
                return;
            }

            effect.Transform.gameObject.SetActive(false);
            effect.Idle.Push(effect.Transform);
        }

        private struct Effect
        {
            public Transform Transform;

            public Vector3 FullScale;

            public int Lifetime;

            public int Elapsed;

            /// <summary>
            /// The pool this goes back into when it retires. Carried rather
            /// than worked out from the shape, because a primitive on screen
            /// cannot be asked which primitive it was made from.
            /// </summary>
            public Stack<Transform> Idle;

            /// <summary>
            /// Whether it closes down to nothing as it ages.
            /// </summary>
            /// <remarks>
            /// A tracer, a flash and a spark do: their size is how loud they
            /// are, so it going away is them going away. A bubble's ring does
            /// not, and that is the one place the two differ — its diameter is
            /// the whole of what it says, so a ring that shrank would be
            /// reporting a reach the bubble did not have for every tick but its
            /// first.
            /// </remarks>
            public bool Shrinks;
        }
    }
}
