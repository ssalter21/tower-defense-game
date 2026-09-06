using UnityEngine;

namespace View
{
    /// <summary>
    /// Every number that decides what the <i>match</i> looks like, in one file
    /// a <c>git diff</c> can show.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="SceneFraming"/> is the playfield — camera, light, floor
    /// colours. This is everything that moves on it. The split is where the
    /// numbers come from rather than what they are for: those frame a static
    /// scene once, these are consumed every frame by things the simulation
    /// drives.
    /// </para>
    /// <para>
    /// <b>Nothing in here is a simulation input.</b> Change every constant in
    /// this file and the match's result, its per-tick hash and its landmark
    /// table are byte-for-byte identical; only the picture changes. That is the
    /// test of whether a number belongs here, and it is the same test
    /// <see cref="SceneFraming"/> applies.
    /// </para>
    /// </remarks>
    public static class MatchTuning
    {
        // ---------------------------------------------------------------
        // Locomotion
        // ---------------------------------------------------------------

        /// <summary>
        /// How far a creep travels, in hexes, during one full cycle of the walk
        /// clip.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the only number in the project that has to be set by
        /// eye.</b> It is the ratio between an artist's stride and this
        /// project's hex, and neither of them knows about the other: the clips
        /// carry no root motion — measured, not assumed, across all twenty-six
        /// of them — so nothing in the asset says how far a step covers, and
        /// nothing in the map says how big a stride should be.
        /// </para>
        /// <para>
        /// Getting it wrong does not fail: it slides. Too small and the feet
        /// skate forwards, too large and the creep moonwalks, and either way
        /// every test in this repository stays green. That is exactly why it is
        /// row three of the sit-down landmark table — "foot sliding" — and why
        /// the table names what broken looks like rather than trusting an
        /// assertion. A human watching the walk is the only instrument that can
        /// read this number.
        /// </para>
        /// <para>
        /// One cycle per hex is the starting value, chosen because a hex is two
        /// metres across the flats and a two-metre stride for a humanoid is
        /// about right for a walk that covers ground. It is a starting value
        /// and not a finding.
        /// </para>
        /// </remarks>
        public const float HexesPerWalkCycle = 1.0f;

        /// <summary>
        /// How far above the floor a creep's feet sit, in metres. Zero: the
        /// tile mesh is at <c>y = 0</c> and the rigs are authored with their
        /// feet at their own origin, so anything else here would be this
        /// project disagreeing with the artist about where the ground is.
        /// </summary>
        public const float CreepGroundOffset = 0f;

        // ---------------------------------------------------------------
        // Towers
        // ---------------------------------------------------------------

        /// <summary>
        /// How high above a tower's base its shots leave from, in metres, when
        /// its art names nowhere better.
        /// </summary>
        /// <remarks>
        /// A tower with an <see cref="EffectAnchor"/> fires from a bone or from
        /// a point on what it holds, and every unit whose art is chosen has
        /// one. This is what a row drawn as the stand-in gets: a mannequin has
        /// no staff tip to name, and one number for the whole roster is the
        /// thing anchors replaced.
        /// </remarks>
        public const float TowerMuzzleHeight = 1.4f;

        // ---------------------------------------------------------------
        // Projectiles
        // ---------------------------------------------------------------

        /// <summary>
        /// How big the mortar shell is, in metres.
        /// </summary>
        public const float ProjectileRadius = 0.16f;

        /// <summary>
        /// The height the shell falls from, in metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The shell descends onto its target, and that is forced by the
        /// contract rather than chosen.</b> A projectile in the snapshot is a
        /// countdown and a target reference and carries no position at all —
        /// deliberately, so that homing is free and free 2D never enters the
        /// simulation. It does not carry the tower that fired it either, so
        /// there is no muzzle in the snapshot for the view to fly out of, and
        /// the only position it can be a function of is its target's.
        /// </para>
        /// <para>
        /// Taking the firing tower from the <c>TowerFired</c> event instead
        /// would break the governing rule outright: events may only trigger the
        /// purely decorative, and where a snapshot entity is drawn is not
        /// decorative. So the shell arcs down onto wherever its target is now,
        /// which is a pure function of the snapshot, scrubs backwards
        /// correctly, and reads as a mortar because a mortar is what type 4 is.
        /// </para>
        /// </remarks>
        public const float ProjectileApexHeight = 5.5f;

        /// <summary>
        /// How far back along the corridor the shell starts, in hexes, so it
        /// falls at an angle rather than straight down a wire.
        /// </summary>
        public const float ProjectileLeadHexes = 1.6f;

        /// <summary>
        /// How far the flight bulges above a straight line from origin to
        /// target, at its midpoint, in metres.
        /// </summary>
        /// <remarks>
        /// Zero at both ends of the flight by construction, so it changes the
        /// shape of the arc and never where the shell leaves from or lands.
        /// </remarks>
        public const float ProjectileArcBulge = 1.2f;

        // ---------------------------------------------------------------
        // Decoration — everything an event triggers
        // ---------------------------------------------------------------

        /// <summary>
        /// How long a hitscan tracer stays up, in simulation ticks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Ticks, not seconds — so there is no clock in this client that the
        /// simulation does not drive.</b> Wall-clock lifetimes were the obvious
        /// first choice and they are wrong twice. They leak: effects only age
        /// where somebody remembers to age them, so a view driven a tick at a
        /// time by a capture tool or a scrub bar accumulates every tracer the
        /// match ever fired. And they are inconsistent under fast-forward,
        /// where the match runs at ten times speed and the decoration does not,
        /// so the picture fills up with effects belonging to ten different
        /// moments.
        /// </para>
        /// <para>
        /// In ticks both problems disappear at once, and the effect lasts the
        /// same slice of <i>match</i> regardless of how fast anybody is
        /// watching it. Aging then belongs where the simulation advances, which
        /// is the one place it cannot be forgotten.
        /// </para>
        /// <para>
        /// Four ticks is about an eighth of a second at thirty ticks per
        /// second — long enough to read, short enough that a tracer is never
        /// mistaken for a thing that is there.
        /// </para>
        /// </remarks>
        public const int TracerTicks = 4;

        /// <summary>How thick a tracer is, in metres.</summary>
        public const float TracerThickness = 0.05f;

        /// <summary>How long a muzzle flash lasts, in simulation ticks.</summary>
        public const int MuzzleFlashTicks = 3;

        /// <summary>How big a muzzle flash is at its largest, in metres.</summary>
        public const float MuzzleFlashRadius = 0.34f;

        /// <summary>How long a hit spark lasts, in simulation ticks.</summary>
        public const int HitSparkTicks = 5;

        /// <summary>How big a hit spark is at its largest, in metres.</summary>
        public const float HitSparkRadius = 0.3f;

        /// <summary>How high up a creep a hit spark appears, in metres.</summary>
        public const float HitSparkHeight = 0.9f;

        /// <summary>
        /// How long the ring a blast or an aura leaves behind lasts, in
        /// simulation ticks.
        /// </summary>
        /// <remarks>
        /// Longer than a spark because it is bigger and slower: a bubble that
        /// covers three hexes wants a moment to be read across, where a spark
        /// is a point and is read at once.
        /// </remarks>
        public const int BubbleRingTicks = 8;

        /// <summary>How thick the ring is, in metres.</summary>
        /// <remarks>
        /// <b>The ring is a placeholder and this is what makes it one.</b> It
        /// is a flat cylinder — a disc — lying on the floor at the size of the
        /// bubble that made it, because Unity's primitives have no torus and
        /// nothing here may billboard. What a blast and an aura should actually
        /// look like is an art decision nobody has taken, and a disc is
        /// deliberately the plainest thing that says "the bubble reached this
        /// far" without pretending to be one.
        /// </remarks>
        public const float BubbleRingThickness = 0.04f;

        /// <summary>
        /// How far above the floor anything that lies flat on it is drawn, in
        /// metres — the disc, the shapes a bubble leaves on the ground and the
        /// roots under a body. Enough to clear the tile it covers rather than
        /// fight it for the same depth.
        /// </summary>
        public const float FloorClearance = 0.03f;

        // ---------------------------------------------------------------
        // Capstone signatures — the shapes a row's own effects are drawn as
        // ---------------------------------------------------------------
        //
        // TEN SHAPES ARE SIGNED, FOUR ARE NOT, AND EVERY NUMBER AND COLOUR
        // BELOW IS A PLACEHOLDER. What was signed is that the Shield Wall's
        // slow leaves a ring, the Slam's swing shocks the ground across the
        // hex, the Blessing glows on every tower it reaches, the Mortar bursts
        // at the radius it landed in, the Overwatch's single shot draws a
        // tracer the length of the leg it crossed, the Fan of Knives throws
        // three knives at three bodies, the Cleric and Druid lines fire a bolt
        // out of the tome or the staff tip, Consecration lays light on the
        // ground, Overgrowth puts roots under everything it slows and Unravel
        // strips the armour off the hex its bolt landed on. How wide a band, how many
        // cracks, how long a knife or a bolt is, how many roots, how long any
        // of it lasts and what colour it comes out are nobody's decision yet --
        // the same standing rule the bubble ring above and the marks below are
        // held to. A number here is the plainest thing that draws the signed
        // shape, and is not a proposal about how it should look.
        //
        // THE FOUR CREEP AURAS AT THE BOTTOM OF THIS SECTION ARE WEAKER THAN
        // THAT, AND THE DIFFERENCE MATTERS. Issue #266 named where those
        // effects should leave from -- the creep's staff, scythe, broom or axe
        // -- and named no shape at all, and a walking row carries no effect
        // anchor, so neither half of what it asked for could be built as
        // written. What is drawn instead is one shape per row, centred on the
        // body, picked as the plainest thing that says what that row's aura
        // does; the aura and its payload are signed in docs/roster.md and the
        // shape is not. SO A SHAPE DOWN THERE IS AS UNSIGNED AS THE COLOUR IT
        // IS DRAWN IN, which is not true of any of the ten above.
        //
        // THE ONES THAT STAND FOR A DISTANCE DO NOT SHRINK, and that is not a
        // number here but a flag on the effect: a ring, a shock and a burst all
        // say how far the bubble reached and the long shot says how far the
        // shot went, so closing one down over its life would report a reach
        // that was never had.

        /// <summary>
        /// How many bars the ring is made of. Enough that it reads as a circle
        /// rather than as a polygon at the size a two-hex aura is drawn at.
        /// </summary>
        public const int SignatureRingSides = 32;

        /// <summary>
        /// How wide the ring's band is, as a share of its own radius. A share
        /// rather than a distance because one mesh is scaled to whatever radius
        /// the bubble reached.
        /// </summary>
        public const float SignatureRingBandFraction = 0.18f;

        /// <summary>
        /// How far the ring stands off the surface it lies on, in metres. Not
        /// scaled with the radius: it is what keeps the ring solid rather than
        /// a stripe seen edge-on, and that does not depend on how wide it is.
        /// </summary>
        public const float SignatureRingThickness = 0.06f;

        /// <summary>How long the Shield Wall's slow ring lasts, in ticks.</summary>
        /// <remarks>
        /// Shorter than the period the aura it stands for pulses on, so two
        /// pulses never draw two rings on top of each other and the ring reads
        /// as a beat rather than as a thing that is always there.
        /// </remarks>
        public const int SlowRingTicks = 10;

        /// <summary>How many cracks the Slam's ground shock runs out.</summary>
        public const int GroundShockCracks = 9;

        /// <summary>
        /// How far out from the centre a crack starts, as a share of the radius
        /// it reaches. Not zero, so the cracks do not all pile into one blob
        /// under the man who swung.
        /// </summary>
        public const float GroundShockInnerFraction = 0.18f;

        /// <summary>How wide one crack is, as a share of the radius.</summary>
        public const float GroundShockWidthFraction = 0.07f;

        /// <summary>How far a crack stands off the floor, in metres.</summary>
        public const float GroundShockThickness = 0.08f;

        /// <summary>How long the ground shock lasts, in ticks.</summary>
        public const int GroundShockTicks = 8;

        /// <summary>How wide the ring over a blessed tower's head is, in metres.</summary>
        public const float BlessingGlowDiameter = 1.2f;

        /// <summary>
        /// How high over a tower's feet that ring hangs, in metres. Above the
        /// tallest thing on the roster, so it is a halo rather than a collar.
        /// </summary>
        public const float BlessingGlowHeight = 2.9f;

        /// <summary>How long a blessed tower's ring lasts, in ticks.</summary>
        public const int BlessingGlowTicks = 12;

        /// <summary>How many shards the Mortar's burst throws out.</summary>
        public const int MortarBurstShards = 14;

        /// <summary>How thick one shard is, as a share of the radius it reaches.</summary>
        public const float MortarBurstWidthFraction = 0.06f;

        /// <summary>How long the burst lasts, in ticks.</summary>
        public const int MortarBurstTicks = 8;

        /// <summary>
        /// How thick the Overwatch's shot is, in metres.
        /// </summary>
        /// <remarks>
        /// Thicker than <see cref="TracerThickness"/>, which is the whole of
        /// what separates this row's shot from the one every other hitscan row
        /// draws: both run muzzle to body, and eight hexes of the ordinary
        /// tracer would read as a longer thread rather than as a heavier shot.
        /// </remarks>
        public const float LongShotThickness = 0.13f;

        /// <summary>
        /// How long the Overwatch's shot stays up, in ticks.
        /// </summary>
        /// <remarks>
        /// Longer than <see cref="TracerTicks"/> for the reason the ring lasts
        /// longer than a spark: it is eight hexes long and wants a moment to be
        /// read along, where a tracer three hexes long is taken in at once.
        /// </remarks>
        public const int LongShotTicks = 12;

        /// <summary>How long one of the Fan of Knives' knives is, in metres.</summary>
        public const float KnifeLength = 0.55f;

        /// <summary>How wide the blade is, as a share of that length.</summary>
        public const float KnifeBladeWidthFraction = 0.13f;

        /// <summary>How far the crossguard reaches, as a share of that length.</summary>
        public const float KnifeGuardFraction = 0.3f;

        /// <summary>
        /// How deep the blade, the guard and the grip are, as a share of that
        /// length.
        /// </summary>
        public const float KnifeThicknessFraction = 0.05f;

        /// <summary>
        /// How many ticks a knife spends crossing from the hand to the body.
        /// </summary>
        /// <remarks>
        /// <b>The flight is decoration and not the shot.</b> The row is hitscan
        /// — the damage landed on the tick it was fired, and the spark on the
        /// body is already drawn — so this is how long the knife is <i>seen</i>
        /// crossing and says nothing about when anything arrived. Six ticks is
        /// a fifth of a second at thirty ticks per second, which is long enough
        /// to read as a throw and short enough that two throws of a row on a
        /// seven-tick cooldown do not overlap.
        /// </remarks>
        public const int KnifeFlightTicks = 6;

        /// <summary>How long one of the magic lines' bolts is, in metres.</summary>
        public const float MagicBoltLength = 0.45f;

        /// <summary>
        /// How thick that bolt is, in metres, in both of the directions that
        /// are not its length.
        /// </summary>
        public const float MagicBoltThickness = 0.12f;

        /// <summary>
        /// How many ticks a bolt spends crossing from the anchor to the body.
        /// </summary>
        /// <remarks>
        /// <b>The flight is decoration and not the shot</b>, the same as the
        /// thrown knife's: every row that draws one is hitscan, so the damage
        /// landed on the tick it was fired and the spark on the body is already
        /// drawn. Five ticks is a sixth of a second at thirty ticks per second,
        /// short enough that the Cleric's thirty-tick cooldown never has two
        /// bolts of one tower in the air at once.
        /// </remarks>
        public const int MagicBoltFlightTicks = 5;

        /// <summary>
        /// How far the Consecration's light stands off the ground it lies on,
        /// in metres.
        /// </summary>
        public const float ConsecrationLightThickness = 0.03f;

        /// <summary>
        /// How long that light lasts, in ticks.
        /// </summary>
        /// <remarks>
        /// Nearly the whole of the thirty-tick period the aura pulses on, so
        /// the ground under the font reads as claimed rather than as flashing
        /// — and still short of it, so two pulses never lay two discs on top of
        /// each other.
        /// </remarks>
        public const int ConsecrationLightTicks = 26;

        /// <summary>How many roots the Overgrowth breaks the ground with under one body.</summary>
        public const int OvergrowthRootCount = 7;

        /// <summary>How wide one root is, as a share of the patch's radius.</summary>
        public const float OvergrowthRootWidthFraction = 0.1f;

        /// <summary>How far a root stands off the floor, in metres.</summary>
        public const float OvergrowthRootThickness = 0.07f;

        /// <summary>
        /// How far a root's outer half swings off the line it left the middle
        /// on, as a share of the patch's radius.
        /// </summary>
        public const float OvergrowthRootKink = 0.35f;

        /// <summary>
        /// How wide the patch of roots under one body is, in metres.
        /// </summary>
        /// <remarks>
        /// <b>A fixed size and not the radius, because that aura reaches sixty
        /// hexes.</b> Every other shape on this page is scaled by the reach the
        /// bubble reported; a shape scaled by this one would be a hundred and
        /// twenty hexes across and the board is nineteen. So the roots are
        /// drawn on each body the aura is holding, at the size of the ground a
        /// body stands on — the same reason the Blessing's halo is a fixed size
        /// over each tower rather than a ring at the edge of the pulse.
        /// </remarks>
        public const float OvergrowthRootPatchDiameter = 1.1f;

        /// <summary>How long a patch of roots lasts, in ticks.</summary>
        /// <remarks>
        /// Just under the thirty-tick period the aura pulses on, for the reason
        /// <see cref="ConsecrationLightTicks"/> is: what it says is that this
        /// body is being held, which is true for as long as it is inside.
        /// </remarks>
        public const int OvergrowthRootTicks = 26;

        /// <summary>
        /// How many bars a whole ring of the Unravel's armour strip would take.
        /// Every other one is drawn, so the band comes out in half this many
        /// pieces with gaps as wide as the pieces.
        /// </summary>
        public const int ArmourStripSides = 24;

        /// <summary>How wide the strip's band is, as a share of its own radius.</summary>
        public const float ArmourStripBandFraction = 0.2f;

        /// <summary>How far the strip stands off the ground it lies on, in metres.</summary>
        public const float ArmourStripThickness = 0.07f;

        /// <summary>How long the strip lasts, in ticks.</summary>
        /// <remarks>
        /// Longer than a spark and far shorter than the hundred and fifty ticks
        /// the strip itself lasts on the body. What a unit is wearing is drawn
        /// from the snapshot for the whole of its duration — see the marks
        /// below — so this is the moment the armour came off and not the five
        /// seconds it stays off.
        /// </remarks>
        public const int ArmourStripTicks = 10;

        // The four creep auras. Every row above pulses or fires from something
        // that stands still; these four walk, and what each one is drawn as is
        // this section's own paragraph rather than #263's.

        /// <summary>How wide the ring over a hastened creep's head is, in metres.</summary>
        /// <remarks>
        /// Narrower than <see cref="BlessingGlowDiameter"/> because a creep is
        /// a narrower body than a tower, and the two are the same shape saying
        /// the same thing on opposite sides of the board.
        /// </remarks>
        public const float HasteRingDiameter = 1f;

        /// <summary>
        /// How high over a creep's feet that ring hangs, in metres. Above the
        /// tallest body on the creep roster, so it is a halo rather than a
        /// collar.
        /// </summary>
        public const float HasteRingHeight = 2.8f;

        /// <summary>How long a hastened creep's ring lasts, in ticks.</summary>
        /// <remarks>
        /// Just under the thirty-tick period the aura pulses on, for the reason
        /// <see cref="ConsecrationLightTicks"/> is: what it says is that this
        /// body is walking faster, which is true for as long as it is inside.
        /// </remarks>
        public const int HasteRingTicks = 26;

        /// <summary>How many arcs the ward's cage is made of.</summary>
        public const int WardDomeRibs = 9;

        /// <summary>
        /// How many straight bars one of those arcs is broken into. Enough that
        /// a rib reads as a curve rather than as a bent stick at the size a
        /// two-hex ward is drawn at.
        /// </summary>
        public const int WardDomeSegments = 5;

        /// <summary>
        /// How thick one rib is, as a share of the ward's own radius. A share
        /// rather than a distance because the whole cage is scaled uniformly to
        /// whatever radius the pulse reached.
        /// </summary>
        public const float WardDomeRibWidthFraction = 0.05f;

        /// <summary>
        /// How long the cage stands, in ticks.
        /// </summary>
        /// <remarks>
        /// <b>Far shorter than the ninety-tick period the ward pulses on, and
        /// that is the point of it.</b> The ward's duration is zero -- a pool
        /// is spent rather than timed -- so what lasts is the pool, and the
        /// pool is drawn on each body that got one for as long as it holds it,
        /// out of the snapshot. This is the moment the ward went out, on the
        /// same terms as <see cref="ArmourStripTicks"/>.
        /// </remarks>
        public const int WardDomeTicks = 10;

        /// <summary>
        /// How many bars a whole ring of the hex ward's plates would take.
        /// Every other one is drawn, so the band comes out in half this many
        /// pieces.
        /// </summary>
        public const int HexPlateSides = 28;

        /// <summary>How wide the band of plates is, as a share of its own radius.</summary>
        public const float HexPlateBandFraction = 0.15f;

        /// <summary>How far the plates stand off the ground they lie on, in metres.</summary>
        public const float HexPlateThickness = 0.07f;

        /// <summary>How long the plates last, in ticks.</summary>
        /// <remarks>
        /// Just under the thirty-tick period, for the reason
        /// <see cref="HasteRingTicks"/> is: the armour it stands for lasts
        /// exactly as long as the gap to the next pulse.
        /// </remarks>
        public const int HexPlateTicks = 26;

        /// <summary>How many shards stand round the edge of the frostbite.</summary>
        public const int FrostSpikeCount = 18;

        /// <summary>
        /// How tall a full shard stands, in metres. Every other one is half
        /// this, which is <see cref="EffectMeshes.Spikes"/>'s own rule.
        /// </summary>
        /// <remarks>
        /// A distance and not a share, because a crown is scaled across by
        /// <see cref="FrostCrownDiameter"/> and not at all upwards -- the rule
        /// every flat shape here is drawn under. Its width is a share for the
        /// same reason a ring's band is: the horizontal axes carry the scale.
        /// </remarks>
        public const float FrostSpikeHeight = 0.6f;

        /// <summary>How thick one shard is, as a share of the crown's radius.</summary>
        public const float FrostSpikeWidthFraction = 0.09f;

        /// <summary>
        /// How wide the crown standing at a frostbitten tower's feet is, in
        /// metres.
        /// </summary>
        /// <remarks>
        /// <b>A fixed size and not the reach</b>, for the reason
        /// <see cref="OvergrowthRootPatchDiameter"/> is one and
        /// <see cref="BlessingGlowDiameter"/> is one: it stands for a tower
        /// caught rather than for a distance. Wide enough to stand outside the
        /// tile a tower is drawn on rather than through the body.
        /// </remarks>
        public const float FrostCrownDiameter = 1.6f;

        /// <summary>How long the crown of shards stands, in ticks.</summary>
        public const int FrostSpikeTicks = 26;

        /// <summary>The Shield Wall's slow ring. Cold, because it is a slow.</summary>
        public static Color SlowRingColor => new Color(0.45f, 0.72f, 1f, 1f);

        /// <summary>The Slam's ground shock.</summary>
        public static Color GroundShockColor => new Color(0.95f, 0.62f, 0.28f, 1f);

        /// <summary>The ring over a tower the Blessing has reached.</summary>
        public static Color BlessingGlowColor => new Color(1f, 0.9f, 0.5f, 1f);

        /// <summary>The Mortar's burst.</summary>
        public static Color MortarBurstColor => new Color(1f, 0.55f, 0.2f, 1f);

        /// <summary>The Overwatch's shot.</summary>
        public static Color LongShotColor => new Color(0.95f, 0.98f, 0.75f, 1f);

        /// <summary>One of the Fan of Knives' knives.</summary>
        public static Color KnifeColor => new Color(0.78f, 0.82f, 0.88f, 1f);

        /// <summary>A bolt off the Cleric's tome or the Druid's staff.</summary>
        /// <remarks>
        /// One colour for both lines, and that is part of the placeholder: a
        /// holy bolt and a nature bolt reading differently is a decision nobody
        /// has taken, and inventing two colours to have had the distinction
        /// would be taking it.
        /// </remarks>
        public static Color MagicBoltColor => new Color(0.85f, 0.92f, 1f, 1f);

        /// <summary>The light the Consecration lays on the ground.</summary>
        public static Color ConsecrationLightColor => new Color(1f, 0.94f, 0.68f, 1f);

        /// <summary>The roots the Overgrowth breaks the ground with.</summary>
        public static Color OvergrowthRootColor => new Color(0.42f, 0.52f, 0.26f, 1f);

        /// <summary>The Unravel's armour strip.</summary>
        public static Color ArmourStripColor => new Color(0.78f, 0.55f, 1f, 1f);

        /// <summary>The ring over a creep the Skeleton Mage's haste has reached.</summary>
        public static Color HasteRingColor => new Color(0.55f, 0.95f, 0.5f, 1f);

        /// <summary>
        /// The Necromancer's ward.
        /// </summary>
        /// <remarks>
        /// <see cref="ShieldSegmentColor"/>, deliberately: the cage is the
        /// moment a pool was granted and that segment is the pool it granted,
        /// so the two say one thing in one colour. It is still a placeholder,
        /// because that segment is.
        /// </remarks>
        public static Color WardDomeColor => ShieldSegmentColor;

        /// <summary>The Witch's hex ward.</summary>
        /// <remarks>
        /// The violet <see cref="ArmourStripColor"/> and
        /// <see cref="ArmourEffectTint"/> are, because all three are armour
        /// moving; that the Unravel takes armour off and the Witch puts it on
        /// is not distinguished, which is the same restraint the wash keeps
        /// between a slow and a haste.
        /// </remarks>
        public static Color HexPlateColor => new Color(0.66f, 0.48f, 0.98f, 1f);

        /// <summary>The Frost Wight's frostbite.</summary>
        public static Color FrostSpikeColor => new Color(0.7f, 0.92f, 1f, 1f);

        // ---------------------------------------------------------------
        // What a unit is carrying — the marks, not the decoration
        // ---------------------------------------------------------------
        //
        // EVERY NUMBER AND EVERY COLOUR IN THIS SECTION IS A PLACEHOLDER, and
        // that is a standing rule rather than a caveat: what a slowed, hastened,
        // cursed or shielded unit should look like is Sam's to sign, and nothing
        // here is a proposal. These are drawn from the snapshot rather than from
        // an event -- see ADR-0007 -- so unlike the decoration above them they
        // are still correct after a scrub, which is the whole reason they are
        // state and not a moment.

        /// <summary>
        /// How high above a creep's feet the bar sits, in metres.
        /// </summary>
        /// <remarks>
        /// Above the hit spark, so a shot landing on a shielded creep does not
        /// go off inside the thing that says it has one.
        /// </remarks>
        public const float UnitBarHeight = 1.15f;

        /// <summary>How long a full bar is, in metres — one whole health pool.</summary>
        public const float UnitBarLength = 0.8f;

        /// <summary>
        /// How thick a bar is, in metres, in both of the directions that are
        /// not its length.
        /// </summary>
        /// <remarks>
        /// <b>A stretched box and not a flat quad, for the same reason a tracer
        /// is one</b>: the camera orbits and nothing here may turn to face it,
        /// so a bar with no thickness would vanish entirely at a quarter turn.
        /// It is still read end-on from two of the four quadrants, which is the
        /// plainest thing that can be said for a first look and is one of the
        /// things this placeholder exists to be judged on.
        /// </remarks>
        public const float UnitBarThickness = 0.09f;

        /// <summary>
        /// The colour a unit is washed with while a speed modifier is on it.
        /// </summary>
        /// <remarks>
        /// <b>One colour per payload, and the direction is not distinguished.</b>
        /// A slow and a haste are one field with opposite signs, and telling
        /// them apart by their look is a design decision nobody has taken —
        /// the same restraint the bubble ring keeps between a blast and a
        /// pulse. Inventing a second colour to have used the sign would be
        /// taking it.
        /// </remarks>
        public static Color SpeedEffectTint => new Color(0.55f, 0.78f, 1f, 1f);

        /// <summary>
        /// The colour a unit is washed with while an armour modifier is on it,
        /// and nothing has moved its speed.
        /// </summary>
        public static Color ArmourEffectTint => new Color(0.82f, 0.62f, 1f, 1f);

        /// <summary>The health segment of the bar.</summary>
        public static Color HealthSegmentColor => new Color(0.42f, 0.78f, 0.4f, 1f);

        /// <summary>The segment that stands for the pool in front of that health.</summary>
        public static Color ShieldSegmentColor => new Color(0.55f, 0.85f, 0.95f, 1f);

        // ---------------------------------------------------------------
        // Decoration colours
        // ---------------------------------------------------------------

        /// <summary>The hitscan tracer's colour.</summary>
        public static Color TracerColor => new Color(1f, 0.86f, 0.45f, 1f);

        /// <summary>The muzzle flash's colour.</summary>
        public static Color MuzzleFlashColor => new Color(1f, 0.78f, 0.3f, 1f);

        /// <summary>The hit spark's colour.</summary>
        public static Color HitSparkColor => new Color(1f, 0.35f, 0.25f, 1f);

        /// <summary>The mortar shell's colour.</summary>
        public static Color ProjectileColor => new Color(0.22f, 0.2f, 0.19f, 1f);

        /// <summary>
        /// The colour of a blast's or an aura's ring. One colour for both, and
        /// that is part of the placeholder: telling a blast from a pulse by its
        /// look is a design decision nobody has taken, so the ring says how far
        /// the bubble reached and nothing else.
        /// </summary>
        public static Color BubbleRingColor => new Color(0.62f, 0.72f, 1f, 1f);

        // ---------------------------------------------------------------
        // The board being built
        // ---------------------------------------------------------------

        /// <summary>
        /// How far above the floor the hex under the pointer is drawn, in
        /// metres. Enough to clear the tile it covers without floating off it.
        /// </summary>
        /// <remarks>
        /// Here rather than in <see cref="SceneFraming"/> because the floor is
        /// framed once and this moves every time the pointer does — which is the
        /// split those two files are divided on. It is not a simulation input by
        /// the same test as everything else in this file: change it and the
        /// match's result, its per-tick hash and its landmark table do not move.
        /// </remarks>
        public const float BuildLightHeight = 0.02f;

        /// <summary>
        /// The colour of the hex under the pointer when the selected tower could
        /// stand on it.
        /// </summary>
        public static Color BuildLightColor => new Color(0.55f, 0.82f, 1f, 1f);
    }
}
