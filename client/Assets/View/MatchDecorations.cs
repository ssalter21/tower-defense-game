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
    /// <b>A row's bubble and a row's shot are drawn as its own row says.</b>
    /// Every bubble drew one shared disc and every shot one shared tracer until
    /// the capstones needed telling apart; now the entity an event names is
    /// turned into the row that emitted it and that row's
    /// <see cref="BubbleSignature"/> or <see cref="ShotSignature"/> picks the
    /// shape. What a row with neither draws is still the disc and the tracer,
    /// unchanged.
    /// </para>
    /// <para>
    /// <b>A row that walks is one of those rows.</b> Four of the auras on this
    /// roster are carried by creeps, and a pulse names its emitter whichever
    /// side the emitter is on, so a creep's own shape is reached the same way a
    /// tower's is. Where it is <i>centred</i> is the body, because an event
    /// carries an entity id and a walking row names no point on its own art —
    /// see <see cref="AuraPulsed"/>.
    /// </para>
    /// </remarks>
    public sealed class MatchDecorations : IMatchEvents
    {
        private readonly Transform _parent;

        private readonly Func<int, Vector3?> _creepPosition;

        private readonly Func<int, Vector3?> _towerMuzzle;

        private readonly Func<int, Vector3?> _entityGround;

        private readonly Func<int, RowSignature?> _towerSignature;

        private readonly Func<int, RowSignature?> _creepSignature;

        private readonly Func<int, float, IReadOnlyList<Vector3>> _towersWithin;

        private readonly Func<int, float, IReadOnlyList<Vector3>> _creepsWithin;

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
        /// What the tower with this id draws its own bubble and its own shot
        /// as, or null when the id is not a tower the view is holding. <b>The
        /// null is load-bearing and is not the same answer as a pair of
        /// <c>None</c>s:</b> a tower that draws the plain disc and a creep that
        /// a shell arrived at are different cases and <see cref="BlastLanded"/>
        /// draws them differently.
        /// </param>
        /// <param name="creepSignature">
        /// The same, for the id of a creep the view is holding, and null for
        /// anything else. <b>A second lookup rather than one over both</b>,
        /// because the two answers are asked at different moments and one of
        /// them is load-bearing by being empty: <see cref="BlastLanded"/> reads
        /// a centre that is not a tower as the body a shot arrived at, and a
        /// lookup that answered for creeps as well would draw a walking row's
        /// own aura shape under a body a mortar shell had just landed on.
        /// </param>
        /// <param name="towersWithin">
        /// Where every tower standing within so many metres of an entity is —
        /// what the Blessing's glow is drawn on. Positions rather than ids,
        /// because nothing here would do anything with an id but ask for the
        /// position.
        /// </param>
        /// <param name="creepsWithin">
        /// Where every creep within so many metres of an entity is — what the
        /// Overgrowth's roots are drawn under, on the same terms.
        /// </param>
        public MatchDecorations(
            Transform parent,
            Func<int, Vector3?> creepPosition,
            Func<int, Vector3?> towerMuzzle,
            Func<int, Vector3?> entityGround,
            Func<int, RowSignature?> towerSignature,
            Func<int, RowSignature?> creepSignature,
            Func<int, float, IReadOnlyList<Vector3>> towersWithin,
            Func<int, float, IReadOnlyList<Vector3>> creepsWithin)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            _creepPosition = creepPosition ?? throw new ArgumentNullException(nameof(creepPosition));
            _towerMuzzle = towerMuzzle ?? throw new ArgumentNullException(nameof(towerMuzzle));
            _entityGround = entityGround ?? throw new ArgumentNullException(nameof(entityGround));
            _towerSignature = towerSignature ?? throw new ArgumentNullException(nameof(towerSignature));
            _creepSignature = creepSignature ?? throw new ArgumentNullException(nameof(creepSignature));
            _towersWithin = towersWithin ?? throw new ArgumentNullException(nameof(towersWithin));
            _creepsWithin = creepsWithin ?? throw new ArgumentNullException(nameof(creepsWithin));

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
            _materials[Piece.LongShot] = ViewMaterials.Create("LongShot", MatchTuning.LongShotColor);
            _materials[Piece.ThrownKnife] = ViewMaterials.Create("ThrownKnife", MatchTuning.KnifeColor);
            _materials[Piece.MagicBolt] = ViewMaterials.Create("MagicBolt", MatchTuning.MagicBoltColor);
            _materials[Piece.ConsecrationLight] =
                ViewMaterials.Create("ConsecrationLight", MatchTuning.ConsecrationLightColor);
            _materials[Piece.OvergrowthRoots] =
                ViewMaterials.Create("OvergrowthRoots", MatchTuning.OvergrowthRootColor);
            _materials[Piece.ArmourStrip] =
                ViewMaterials.Create("ArmourStrip", MatchTuning.ArmourStripColor);
            _materials[Piece.HasteRing] = ViewMaterials.Create("HasteRing", MatchTuning.HasteRingColor);
            _materials[Piece.WardDome] = ViewMaterials.Create("WardDome", MatchTuning.WardDomeColor);
            _materials[Piece.HexPlates] = ViewMaterials.Create("HexPlates", MatchTuning.HexPlateColor);
            _materials[Piece.FrostSpikes] =
                ViewMaterials.Create("FrostSpikes", MatchTuning.FrostSpikeColor);
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
            LongShot,
            ThrownKnife,
            MagicBolt,
            ConsecrationLight,
            OvergrowthRoots,
            ArmourStrip,
            HasteRing,
            WardDome,
            HexPlates,
            FrostSpikes,
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

        /// <summary>
        /// How many of the Overwatch's shots have been drawn since the last
        /// clear. For tests, and counted apart from
        /// <see cref="TracersDrawn"/> — a row falling back to the plain tracer
        /// is exactly the failure this reports.
        /// </summary>
        public int LongShotsDrawn => Drawn(Piece.LongShot);

        /// <summary>
        /// How many knives have been drawn since the last clear. For tests. One
        /// per shot, so one throw of the Fan of Knives counts three.
        /// </summary>
        public int KnivesDrawn => Drawn(Piece.ThrownKnife);

        /// <summary>
        /// How many bolts have been drawn since the last clear. For tests, and
        /// counted apart from <see cref="TracersDrawn"/> — a magic row falling
        /// back to the plain tracer is exactly the failure this reports.
        /// </summary>
        public int BoltsDrawn => Drawn(Piece.MagicBolt);

        /// <summary>How many discs of the Consecration's light have been drawn since the last clear.</summary>
        public int LightsDrawn => Drawn(Piece.ConsecrationLight);

        /// <summary>
        /// How many patches of the Overgrowth's roots have been drawn since the
        /// last clear. For tests. One per body the aura is holding, so a single
        /// pulse over four bodies counts four.
        /// </summary>
        public int RootsDrawn => Drawn(Piece.OvergrowthRoots);

        /// <summary>How many of the Unravel's armour strips have been drawn since the last clear.</summary>
        public int StripsDrawn => Drawn(Piece.ArmourStrip);

        /// <summary>
        /// How many rings over a hastened creep's head have been drawn since
        /// the last clear. For tests. One per body the Skeleton Mage's pulse
        /// reached, so a single pulse over four bodies counts four.
        /// </summary>
        public int HasteRingsDrawn => Drawn(Piece.HasteRing);

        /// <summary>How many of the Necromancer's ward cages have been drawn since the last clear.</summary>
        public int WardDomesDrawn => Drawn(Piece.WardDome);

        /// <summary>How many of the Witch's bands of plates have been drawn since the last clear.</summary>
        public int HexPlatesDrawn => Drawn(Piece.HexPlates);

        /// <summary>
        /// How many crowns of frost have been drawn since the last clear. For
        /// tests, and counted apart from every other aura shape because the
        /// Frost Wight is the one row whose aura reaches the tower side.
        /// </summary>
        public int FrostSpikesDrawn => Drawn(Piece.FrostSpikes);

        /// <summary>
        /// A tower released a shot: a flash at the point on its art the shot
        /// leaves from, and — where the shot has a body to reach — the emitting
        /// row's own shape crossing to it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The flash is the same for every row and the crossing is not.</b>
        /// A muzzle flash says a row fired and where from, which is the same
        /// sentence whatever the row is; what crosses to the body is where a
        /// line can read as itself, so it is picked off the shooter's
        /// <see cref="ShotSignature"/>. A row with none draws the thin tracer
        /// every hitscan row drew before any row had a signature.
        /// </para>
        /// <para>
        /// <b>The shooter is in this event, unlike a blast's.</b> A shot names
        /// the tower that fired it and the body it was aimed at, so the row is
        /// reachable and a signature can be bound to it — which is exactly what
        /// <see cref="BlastLanded"/> cannot do for a bubble centred on its
        /// victim. Nothing is held on to across the call: the ids are turned
        /// into two positions out of the frame the view last drew, and the
        /// effect knows nothing else.
        /// </para>
        /// <para>
        /// <b>A projectile row draws the shared tracer, exactly as it always
        /// has.</b> Its shell is a real snapshot entity flying the same line,
        /// so the two overlap; that is how this has drawn since before any row
        /// had a signature, and no projectile row carries one, so nothing here
        /// is a new opinion about a shell.
        /// </para>
        /// </remarks>
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

            if (!target.HasValue)
            {
                return;
            }

            Crossing(
                _towerSignature(towerId)?.Shot ?? ShotSignature.None,
                muzzle.Value,
                target.Value + (Vector3.up * MatchTuning.HitSparkHeight));
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
        /// A creep became another row. Nothing is drawn here, and the two halves
        /// of why are both worth saying.
        /// </summary>
        /// <remarks>
        /// <b>The body swap is not a decoration.</b> Which row a creep is is a
        /// field of the snapshot, so the model changes on the tick and a scrub
        /// back across it draws the old body again without this method being
        /// called at all -- which is the point of the field being in the
        /// snapshot rather than on this stream.
        /// <b>And what would go on top of it has not been chosen.</b> A puff, a
        /// flash or a shockwave at the moment of the change is an art decision,
        /// and inventing one here is not this ticket's to make.
        /// </remarks>
        public void CreepTransformed(int creepId, int typeId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// A creep put another body on the corridor. Nothing is drawn here, for
        /// the two reasons the transformation is not drawn either.
        /// </summary>
        /// <remarks>
        /// <b>The body arriving is not a decoration.</b> A raised creep is an
        /// entity in the snapshot from the tick it is raised, so it is claimed,
        /// posed and given its bar by the ordinary draw -- and a scrub back
        /// across the tick takes it off screen again without this method being
        /// called at all.
        /// <b>And what would go on top of it has not been chosen.</b> A grave
        /// bursting, a green flash or a column of light at the raise is an art
        /// decision, and inventing one here is not this ticket's to make.
        /// </remarks>
        public void CreepRaised(int creepId, int raisedCreepId)
        {
            EventsHeard++;
        }

        /// <summary>
        /// A body that pays for being killed was killed. Nothing is drawn here,
        /// for the two reasons the transformation and the raise are not drawn
        /// either.
        /// </summary>
        /// <remarks>
        /// <b>The gold itself is not a decoration.</b> What a match has paid is
        /// a number on the match, and a seek re-simulates it from tick zero
        /// rather than replaying a stream -- so a scrub either side of this tick
        /// reads the running total without this method being called at all.
        /// <b>And what would go on top of it has not been chosen.</b> A coin, a
        /// number floating off the body or a flash on the purse is an art
        /// decision, and inventing one here is not this ticket's to make.
        /// </remarks>
        public void BountyPaid(int creepId, int gold)
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
        /// that seeks discard. So a blast that arrived on a body is drawn off
        /// the shape of the event rather than off a row — a burst at the radius
        /// it reached, which is the shape the Mortar's line signs and which the
        /// Mage's and the Sorcerer's splash therefore wear too.
        /// </para>
        /// <para>
        /// <b>What tells those unreachable cases apart is
        /// <paramref name="payload"/>, which is the only thing on the event
        /// that is not the victim.</b> An armour blast on a body is the
        /// Unravel's bolt stripping the hex and a damage blast is the Mortar's
        /// shell arriving, so the two draw different shapes without anything
        /// having to know which tower fired. It is a shape chosen by the
        /// payload rather than by the row and that is the whole of its
        /// weakness: a second row authoring a target-centred armour blast would
        /// wear the strip too, exactly as the Mage's and the Sorcerer's splash
        /// wear the burst. <b>The payload still says nothing about
        /// colour.</b> What a payload should look like in general is a decision
        /// nobody has taken; this is two signed shapes told apart by the one
        /// handle that exists, not four colours invented to have used the
        /// parameter. What the payload then does to a unit is that unit's own
        /// picture and not this one's — see <see cref="EffectMarks"/>.
        /// </para>
        /// </remarks>
        public void BlastLanded(int centreId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;

            RowSignature? emitter = _towerSignature(centreId);

            if (emitter.HasValue)
            {
                Signature(emitter.Value.Bubble, centreId, radiusMilliHex);

                return;
            }

            Arrived(centreId, radiusMilliHex, payload);
        }

        /// <summary>
        /// A bubble pulsed on its own clock: the emitting row's signature,
        /// under or over whatever is emitting it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The emitter is the centre here in every case, so the row is
        /// always reachable</b> — and it is reachable on both sides, because
        /// four of the auras on this roster are carried by creeps. A tower is
        /// looked up first and a creep second; a row neither lookup answers for
        /// draws the plain disc, which is what every bubble drew before any row
        /// had a signature.
        /// </para>
        /// <para>
        /// <b>A creep's pulse leaves the body and not the staff it is
        /// holding.</b> An event carries an entity id, so the position comes
        /// out of the snapshot the view is drawing, and a walking row names no
        /// effect anchor at all — <c>ImportedArtTests</c> asserts it carries
        /// none, since nothing would ever resolve one. Where an aura should
        /// leave a creep's art from is therefore an open question and not
        /// something this method quietly answers.
        /// </para>
        /// </remarks>
        public void AuraPulsed(int emitterId, int radiusMilliHex, BubblePayload payload)
        {
            EventsHeard++;

            RowSignature? emitter = _towerSignature(emitterId) ?? _creepSignature(emitterId);

            Signature(emitter?.Bubble ?? BubbleSignature.None, emitterId, radiusMilliHex);
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

                if (effect.Travels)
                {
                    // Over one tick fewer than the lifetime, so the last tick
                    // it is drawn on is the tick it is on the body. Dividing by
                    // the lifetime would retire it a step short of arriving,
                    // every time.
                    effect.Transform.position = Vector3.Lerp(
                        effect.From,
                        effect.To,
                        effect.Elapsed / (float)Mathf.Max(1, effect.Lifetime - 1));
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

        /// <summary>
        /// What one row's shot is drawn as on its way to the body: the emitting
        /// row's own shape, or the thin tracer every row shares.
        /// </summary>
        private void Crossing(ShotSignature signature, Vector3 from, Vector3 to)
        {
            switch (signature)
            {
                // The Overwatch's: one heavy bar the whole length of the leg
                // the shot crossed. It stands for a distance, so it holds that
                // length for its whole life -- the rule every shape reporting a
                // reach is held to.
                case ShotSignature.LongShot:
                    Bar(
                        Piece.LongShot,
                        from,
                        to,
                        MatchTuning.LongShotThickness,
                        MatchTuning.LongShotTicks,
                        shrinks: false);
                    break;

                // The Fan of Knives': one knife leaving the hand and crossing
                // to the body. That row fires three shots at three bodies in
                // one throw, so one throw arrives here three times.
                case ShotSignature.ThrownKnife:
                    Crosses(
                        Piece.ThrownKnife,
                        from,
                        to,
                        Vector3.one * MatchTuning.KnifeLength,
                        MatchTuning.KnifeFlightTicks);
                    break;

                // The Cleric and Druid lines': a short shaft leaving the tome
                // or the staff tip. Six rows draw it, which is what makes it
                // the one shape here worn by whole lines rather than by one
                // capstone.
                case ShotSignature.MagicBolt:
                    Crosses(
                        Piece.MagicBolt,
                        from,
                        to,
                        new Vector3(
                            MatchTuning.MagicBoltThickness,
                            MatchTuning.MagicBoltThickness,
                            MatchTuning.MagicBoltLength),
                        MatchTuning.MagicBoltFlightTicks);
                    break;

                default:
                    Bar(
                        Piece.Tracer,
                        from,
                        to,
                        MatchTuning.TracerThickness,
                        MatchTuning.TracerTicks,
                        shrinks: true);
                    break;
            }
        }

        /// <summary>
        /// A box stretched from one point to the other, as thick as it is deep.
        /// </summary>
        private void Bar(
            Piece piece, Vector3 from, Vector3 to, float thickness, int lifetimeTicks, bool shrinks)
        {
            Vector3 along = to - from;
            float length = along.magnitude;

            if (length < 1e-4f)
            {
                return;
            }

            Transform box = Take(piece);
            box.SetPositionAndRotation(
                (from + to) * 0.5f,
                Quaternion.LookRotation(along / length, Vector3.up));

            Stays(piece, box, new Vector3(thickness, thickness, length), lifetimeTicks, shrinks);
        }

        /// <summary>
        /// One object leaving <paramref name="from"/> pointed at
        /// <paramref name="to"/> and crossing to it as it ages — a thrown knife
        /// or a bolt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It is the same size wherever it goes</b>, unlike everything a
        /// bubble leaves: these are objects rather than a reach being reported,
        /// so a longer flight draws the same knife or the same bolt covering
        /// more ground and not a bigger one.
        /// </para>
        /// <para>
        /// <b>Both ends are read once, here.</b> The object carries the two
        /// points it was drawn between and crosses between them on the tick, so
        /// it never asks the snapshot anything again — a body that dies
        /// mid-flight leaves the throw finishing as it was drawn, which is
        /// decoration behaving as decoration rather than a second opinion about
        /// where a creep is.
        /// </para>
        /// <para>
        /// <b>And the flight is not the shot.</b> Every row that draws one is
        /// hitscan: the damage landed on the tick it was fired and the spark on
        /// the body is already drawn. What crosses here is a picture of the
        /// shot, which is why it may take ticks the shot did not.
        /// </para>
        /// </remarks>
        private void Crosses(Piece piece, Vector3 from, Vector3 to, Vector3 scale, int lifetimeTicks)
        {
            Vector3 along = to - from;

            if (along.sqrMagnitude < 1e-8f)
            {
                return;
            }

            Transform thrown = Take(piece);
            thrown.SetPositionAndRotation(from, Quaternion.LookRotation(along.normalized, Vector3.up));

            Flies(piece, thrown, scale, lifetimeTicks, from, to);
        }

        private void Sphere(Piece piece, Vector3 at, float radius, int lifetimeTicks)
        {
            Transform sphere = Take(piece);
            sphere.position = at;

            Stays(piece, sphere, Vector3.one * (radius * 2f), lifetimeTicks, shrinks: true);
        }

        /// <summary>
        /// The shape one row's bubble leaves, at the size the bubble reached.
        /// </summary>
        private void Signature(BubbleSignature signature, int centreId, int radiusMilliHex)
        {
            switch (signature)
            {
                // The Shield Wall's: an open ring at the edge of the slow, so
                // the bodies caught inside it stay visible through it.
                case BubbleSignature.SlowRing:
                    Flat(Piece.SlowRing, centreId, radiusMilliHex, MatchTuning.SlowRingTicks);
                    break;

                // The Slam's: cracks running out from under the man who swung
                // to the edge of what the swing reached.
                case BubbleSignature.GroundShock:
                    Flat(Piece.GroundShock, centreId, radiusMilliHex, MatchTuning.GroundShockTicks);
                    break;

                case BubbleSignature.TowerGlow:
                    TowerGlow(centreId, radiusMilliHex);
                    break;

                // The Consecration's: light filling the ground the font has
                // claimed, rather than a band at the edge of it, because what
                // that aura does happens to a body for standing anywhere
                // inside.
                case BubbleSignature.ConsecrationLight:
                    Disc(
                        Piece.ConsecrationLight,
                        centreId,
                        radiusMilliHex,
                        MatchTuning.ConsecrationLightThickness,
                        MatchTuning.ConsecrationLightTicks);
                    break;

                case BubbleSignature.OvergrowthRoots:
                    Roots(centreId, radiusMilliHex);
                    break;

                // The Skeleton Mage's: a ring over the head of every body the
                // haste reached, which is the Blessing's shape on the other
                // side of the board and drawn on what the pulse found for the
                // same reason -- what that aura does is make other bodies
                // faster.
                case BubbleSignature.HasteRing:
                    HasteRings(centreId, radiusMilliHex);
                    break;

                // The Necromancer's: a cage standing over the ground the ward
                // covered. It reports a radius in all three directions, so it
                // is scaled uniformly rather than laid flat.
                case BubbleSignature.WardDome:
                    Dome(centreId, radiusMilliHex);
                    break;

                // The Witch's: plates lying on the ground out to the edge of
                // the hex ward.
                case BubbleSignature.HexPlates:
                    Flat(Piece.HexPlates, centreId, radiusMilliHex, MatchTuning.HexPlateTicks);
                    break;

                // The Frost Wight's: a crown of shards at the feet of every
                // tower the frost reached. The one aura on the roster that
                // reaches the other side, so it is the one whose shape lands on
                // something nothing else draws on.
                case BubbleSignature.FrostSpikes:
                    FrostCrowns(centreId, radiusMilliHex);
                    break;

                default:
                    Disc(
                        Piece.BubbleRing,
                        centreId,
                        radiusMilliHex,
                        MatchTuning.BubbleRingThickness,
                        MatchTuning.BubbleRingTicks);
                    break;
            }
        }

        /// <summary>
        /// What a blast that arrived on a body draws: the Unravel's strip where
        /// the payload says armour came off, and the Mortar's burst otherwise.
        /// </summary>
        /// <remarks>
        /// <b>Neither is bound to a row and neither can be.</b> The event names
        /// the body the shot arrived at, so there is no shooter here to look a
        /// signature up on — see <see cref="BlastLanded"/>. The payload is the
        /// only other thing the event carries, so it is what these two are told
        /// apart by; the Mage's and the Sorcerer's splash are damage blasts on
        /// a body like the Mortar's and are drawn as the burst for that reason
        /// rather than because anybody asked for it.
        /// </remarks>
        private void Arrived(int centreId, int radiusMilliHex, BubblePayload payload)
        {
            if (payload == BubblePayload.Armour)
            {
                Flat(Piece.ArmourStrip, centreId, radiusMilliHex, MatchTuning.ArmourStripTicks);

                return;
            }

            Burst(centreId, radiusMilliHex);
        }

        /// <summary>
        /// A flat cylinder on the ground under the entity a bubble was centred
        /// on, as wide as the bubble reached — the plain disc a row with no
        /// signature leaves, and the Consecration's light.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One shape and two readings.</b> The disc is the placeholder that
        /// says only "the bubble reached this far"; the Consecration's is the
        /// same solid of light in its own colour, because what that aura does
        /// is claim ground rather than draw a boundary. They are separate
        /// pieces so that anything looking at the playfield can tell which one
        /// it found.
        /// </para>
        /// <para>
        /// <b>Nothing is drawn for a bubble that reached only its centre.</b>
        /// A radius of zero is a real authoring in the simulation — the
        /// single-target slow — and a disc of no size is a speck, so drawing
        /// one would be a worse answer than drawing nothing. What that bubble
        /// did to the one body it found is that body's own picture.
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
        private void Disc(
            Piece piece, int centreId, int radiusMilliHex, float thickness, int lifetimeTicks)
        {
            if (!Reached(centreId, radiusMilliHex, out Vector3 at, out float diameter))
            {
                return;
            }

            Transform disc = Take(piece);
            disc.position = at + (Vector3.up * MatchTuning.FloorClearance);

            // A Unity cylinder is one unit across and two tall, so a diameter
            // goes into x and z unchanged and the thickness is halved into y.
            var scale = new Vector3(diameter, thickness * 0.5f, diameter);

            Stays(piece, disc, scale, lifetimeTicks, shrinks: false);
        }

        /// <summary>
        /// The Overgrowth's: a patch of roots breaking the ground under every
        /// body the aura is holding.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drawn on what the bubble found rather than on the bubble, and
        /// that is forced by the row.</b> Overgrowth's aura reaches sixty
        /// hexes — the whole board, every board — so a shape at its radius
        /// would be a hundred and twenty hexes across on a board nineteen
        /// wide. What is worth seeing is which bodies are being held, which is
        /// the same answer the Blessing's halo reached from the other side.
        /// </para>
        /// <para>
        /// <b>Each patch is a fixed size</b>, for the same reason: it stands
        /// for a body caught rather than for a distance, so nothing here is
        /// scaled by the reach.
        /// </para>
        /// </remarks>
        private void Roots(int emitterId, int radiusMilliHex)
        {
            if (radiusMilliHex <= 0)
            {
                return;
            }

            OnEachFound(
                Piece.OvergrowthRoots,
                _creepsWithin(emitterId, SimUnits.MetresFromMilliHex(radiusMilliHex)),
                MatchTuning.FloorClearance,
                MatchTuning.OvergrowthRootPatchDiameter,
                MatchTuning.OvergrowthRootTicks);
        }

        /// <summary>
        /// The Skeleton Mage's: a ring over the head of every creep its haste
        /// reached, the emitter included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The Blessing's shape, on the other side of the board.</b> Both
        /// auras make their own side faster, so what is worth seeing is the
        /// same thing in both cases: which bodies got it. Its own pool and its
        /// own colour, because the two are told apart by nothing else.
        /// </para>
        /// <para>
        /// <b>It is the one of the three friend-side creep auras drawn on the
        /// bodies.</b> The Skeleton Mage's haste, the Necromancer's ward and
        /// the Witch's hex ward all reach creeps within two hexes, so all three
        /// drawn that way would stack three shapes on one walking body, over
        /// the bar <see cref="EffectMarks"/> already puts there. The other two
        /// are drawn at their reach for that reason.
        /// </para>
        /// </remarks>
        private void HasteRings(int emitterId, int radiusMilliHex)
        {
            if (radiusMilliHex <= 0)
            {
                return;
            }

            OnEachFound(
                Piece.HasteRing,
                _creepsWithin(emitterId, SimUnits.MetresFromMilliHex(radiusMilliHex)),
                MatchTuning.HasteRingHeight,
                MatchTuning.HasteRingDiameter,
                MatchTuning.HasteRingTicks);
        }

        /// <summary>
        /// The Frost Wight's: a crown of shards at the feet of every tower its
        /// frostbite reached.
        /// </summary>
        /// <remarks>
        /// <b>The one shape in this file drawn on a tower by a creep.</b>
        /// Frostbite is the only aura on the roster whose <c>affects</c> column
        /// reaches the other side, and a tower carrying a modifier wears
        /// nothing of its own — <see cref="EffectMarks"/> is a creep's — so
        /// this is the whole of what says a tower is firing slower.
        /// </remarks>
        private void FrostCrowns(int emitterId, int radiusMilliHex)
        {
            if (radiusMilliHex <= 0)
            {
                return;
            }

            OnEachFound(
                Piece.FrostSpikes,
                _towersWithin(emitterId, SimUnits.MetresFromMilliHex(radiusMilliHex)),
                MatchTuning.FloorClearance,
                MatchTuning.FrostCrownDiameter,
                MatchTuning.FrostSpikeTicks);
        }

        /// <summary>
        /// The Necromancer's: a cage of arcs standing over the ground its ward
        /// covered, as wide as the pulse reached.
        /// </summary>
        /// <remarks>
        /// <b>Scaled uniformly and not laid flat</b>, because it is as tall as
        /// it is wide by construction: what it reports is a radius in all three
        /// directions, the way the Mortar's burst does, where every shape lying
        /// on the floor reports one across and keeps a thickness of its own.
        /// </remarks>
        private void Dome(int centreId, int radiusMilliHex) =>
            Uniform(
                Piece.WardDome,
                centreId,
                radiusMilliHex,
                MatchTuning.FloorClearance,
                MatchTuning.WardDomeTicks);

        /// <summary>
        /// The Blessing's: a ring over the head of every tower the pulse
        /// reached, the emitter included.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Drawn on what a bubble found rather than on the bubble, which
        /// the Overgrowth's roots are too.</b> A ring on the ground would say
        /// how far the blessing carries, which is true and is not what this
        /// line is for: what the
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

            OnEachFound(
                Piece.TowerGlow,
                _towersWithin(emitterId, SimUnits.MetresFromMilliHex(radiusMilliHex)),
                MatchTuning.BlessingGlowHeight,
                MatchTuning.BlessingGlowDiameter,
                MatchTuning.BlessingGlowTicks);
        }

        /// <summary>
        /// One flat shape of a fixed size at each of the places a bubble found,
        /// <paramref name="height"/> above each of them.
        /// </summary>
        /// <remarks>
        /// <b>The size is fixed and not the reach, which is what makes this the
        /// other kind of bubble shape.</b> Everything drawn on the bubble is
        /// scaled by how far it went; these are drawn on the things it found,
        /// so each one stands for one thing being caught and none of them
        /// reports a distance. Nothing here shrinks, for the same reason.
        /// </remarks>
        private void OnEachFound(
            Piece piece,
            IReadOnlyList<Vector3> found,
            float height,
            float diameter,
            int lifetimeTicks)
        {
            for (var index = 0; index < found.Count; index++)
            {
                Transform drawn = Take(piece);
                drawn.position = found[index] + (Vector3.up * height);

                Stays(piece, drawn, Flattened(diameter), lifetimeTicks, shrinks: false);
            }
        }

        /// <summary>
        /// The Mortar's: shards thrown out of the body the shell arrived at, to
        /// the edge of what the blast reached.
        /// </summary>
        private void Burst(int centreId, int radiusMilliHex) =>
            Uniform(
                Piece.MortarBurst,
                centreId,
                radiusMilliHex,
                MatchTuning.HitSparkHeight,
                MatchTuning.MortarBurstTicks);

        /// <summary>
        /// One of the shapes that is as tall as it is wide, centred
        /// <paramref name="height"/> above whatever the bubble was centred on
        /// and scaled in all three directions by the reach.
        /// </summary>
        /// <remarks>
        /// <b>The other kind of shape drawn on a bubble, against
        /// <see cref="Flat"/>.</b> A flat one reports a reach across and keeps
        /// a thickness of its own; these leave the ground as well as crossing
        /// it, so their height is part of the radius they are reporting and the
        /// scale goes into all three axes. Two shapes are of this kind: the
        /// Mortar's burst and the Necromancer's cage.
        /// </remarks>
        private void Uniform(
            Piece piece, int centreId, int radiusMilliHex, float height, int lifetimeTicks)
        {
            if (!Reached(centreId, radiusMilliHex, out Vector3 at, out float diameter))
            {
                return;
            }

            Transform drawn = Take(piece);
            drawn.position = at + (Vector3.up * height);

            Stays(piece, drawn, Vector3.one * diameter, lifetimeTicks, shrinks: false);
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
            drawn.position = at + (Vector3.up * MatchTuning.FloorClearance);

            Stays(piece, drawn, Flattened(diameter), lifetimeTicks, shrinks: false);
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
        /// Draws one effect that has been placed and stays where it was put.
        /// </summary>
        private void Stays(Piece piece, Transform drawn, Vector3 scale, int lifetimeTicks, bool shrinks) =>
            Draw(new Effect
            {
                Transform = drawn,
                FullScale = scale,
                Lifetime = lifetimeTicks,
                Piece = piece,
                Shrinks = shrinks,
            });

        /// <summary>
        /// Draws one effect that crosses from <paramref name="from"/> to
        /// <paramref name="to"/> over its life instead.
        /// </summary>
        private void Flies(
            Piece piece, Transform drawn, Vector3 scale, int lifetimeTicks, Vector3 from, Vector3 to) =>
            Draw(new Effect
            {
                Transform = drawn,
                FullScale = scale,
                Lifetime = lifetimeTicks,
                Piece = piece,
                Shrinks = false,
                Travels = true,
                From = from,
                To = to,
            });

        /// <summary>
        /// Sizes, surfaces and registers one effect. <b>The single place an
        /// effect joins the tally</b>, so a shape written next cannot draw
        /// correctly while reporting nothing.
        /// </summary>
        private void Draw(Effect effect)
        {
            effect.Transform.localScale = effect.FullScale;
            effect.Transform.GetComponent<MeshRenderer>().sharedMaterial = _materials[effect.Piece];

            _drawn.TryGetValue(effect.Piece, out int already);
            _drawn[effect.Piece] = already + 1;

            _active.Add(effect);
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
        /// Lazily, because a match that stands no capstone should not pay for
        /// meshes it never draws — which is every match of the shipped
        /// defense.
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

                Piece.OvergrowthRoots => EffectMeshes.Roots(
                    MatchTuning.OvergrowthRootCount,
                    MatchTuning.OvergrowthRootWidthFraction * EffectMeshes.OuterRadius,
                    MatchTuning.OvergrowthRootThickness,
                    MatchTuning.OvergrowthRootKink),

                Piece.HexPlates => EffectMeshes.BrokenRing(
                    MatchTuning.HexPlateSides,
                    MatchTuning.HexPlateBandFraction * EffectMeshes.OuterRadius,
                    MatchTuning.HexPlateThickness),

                Piece.WardDome => EffectMeshes.Dome(
                    MatchTuning.WardDomeRibs,
                    MatchTuning.WardDomeSegments,
                    MatchTuning.WardDomeRibWidthFraction * EffectMeshes.OuterRadius),

                Piece.FrostSpikes => EffectMeshes.Spikes(
                    MatchTuning.FrostSpikeCount,
                    MatchTuning.FrostSpikeHeight,
                    MatchTuning.FrostSpikeWidthFraction * EffectMeshes.OuterRadius),

                Piece.ArmourStrip => EffectMeshes.BrokenRing(
                    MatchTuning.ArmourStripSides,
                    MatchTuning.ArmourStripBandFraction * EffectMeshes.OuterRadius,
                    MatchTuning.ArmourStripThickness),

                // The one mesh here built a unit long rather than at an outer
                // radius, because a knife is an object and not a reach.
                Piece.ThrownKnife => EffectMeshes.Knife(
                    MatchTuning.KnifeBladeWidthFraction,
                    MatchTuning.KnifeGuardFraction,
                    MatchTuning.KnifeThicknessFraction),

                // The slow ring, the tower glow and the haste ring are one
                // ring at three sizes, so they are one mesh. Their pools stay
                // separate because their colours and lifetimes are not.
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
                Piece.Tracer or Piece.LongShot or Piece.MagicBolt =>
                    GameObject.CreatePrimitive(PrimitiveType.Cube),
                Piece.MuzzleFlash => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                Piece.Spark => GameObject.CreatePrimitive(PrimitiveType.Sphere),
                Piece.BubbleRing or Piece.ConsecrationLight =>
                    GameObject.CreatePrimitive(PrimitiveType.Cylinder),
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
            /// first. The Overwatch's long shot is the same case with a
            /// distance in place of a radius.
            /// </remarks>
            public bool Shrinks;

            /// <summary>
            /// Whether it crosses from <see cref="From"/> to <see cref="To"/>
            /// as it ages rather than staying where it was drawn.
            /// </summary>
            /// <remarks>
            /// One thing does: the thrown knife, whose whole read is a body
            /// being crossed to. Both ends were read out of the frame the view
            /// last drew, once, when the event arrived — so this is arithmetic
            /// on two numbers the effect owns and not a second look at the
            /// snapshot.
            /// </remarks>
            public bool Travels;

            /// <summary>Where it was drawn, for one that travels.</summary>
            public Vector3 From;

            /// <summary>Where it is going, for one that travels.</summary>
            public Vector3 To;
        }
    }
}
