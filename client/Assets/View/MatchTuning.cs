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
        /// How high above a tower's base its shots leave from, in metres. Where
        /// the tracer starts and the muzzle flash sits.
        /// </summary>
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
    }
}
