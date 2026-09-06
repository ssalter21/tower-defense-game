namespace View
{
    /// <summary>
    /// What one row's effects are drawn as, so that a capstone reads as itself
    /// rather than as the shape every row in the game shares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is bound per unit, on <see cref="UnitArt"/>, by the scene
    /// builder</b> — the same place the model, the atlas, the props and the
    /// effect anchor are bound. So a tier three carries a signature its tier
    /// two does not, and adding one to a row is editing a table rather than
    /// editing <see cref="MatchDecorations"/>.
    /// </para>
    /// <para>
    /// <b>The names here are shapes and not rows.</b> What each shape is made
    /// of is <see cref="EffectMeshes"/>'s business and how big and what colour
    /// is <see cref="MatchTuning"/>'s; a row picks one of these and says
    /// nothing else about how it looks.
    /// </para>
    /// <para>
    /// <b>One field, two moments.</b> A shape below is either what a row's
    /// <i>bubble</i> leaves or what its <i>shot</i> is drawn as, and that
    /// decides which event reaches it: a bubble shape is picked when a blast or
    /// an aura names the row, a shot shape when the row fires. A row selecting
    /// the shape of the other moment draws the shared one, which is what a row
    /// with no signature at all draws. One field rather than two because no row
    /// on this roster carries a bubble and a shot worth telling apart at once —
    /// the four rows with a bubble signature author no shots of their own shape
    /// and the two with a shot signature author no bubble.
    /// </para>
    /// <para>
    /// <b>A signature is reached through the entity the event named, so only a
    /// tower's is reachable.</b> An aura pulses from its emitter, a sweep is
    /// centred on the tower that swung, and a shot names the tower that fired
    /// it, so all three name a row the view is holding art for. A blast centred
    /// on the body a shot arrived at names the body — the shooter is not in the
    /// event at all, deliberately, since an event carries an entity id and
    /// never a position or a reference to hold on to. That one case is drawn as
    /// a burst at the radius it reached, and is the one signature no row
    /// selects; see <see cref="MatchDecorations.BlastLanded"/>.
    /// </para>
    /// </remarks>
    public enum EffectSignature
    {
        /// <summary>
        /// The plain shapes every row had before any row had a signature of its
        /// own — a disc on the ground as wide as the bubble reached, and a thin
        /// tracer from the muzzle to the body. Still what a row without a
        /// signature draws.
        /// </summary>
        None = 0,

        /// <summary>
        /// A ring lying on the ground at the edge of what the bubble reached,
        /// open in the middle so the bodies inside it stay visible. The Shield
        /// Wall's, whose whole read is where the slow stops.
        /// </summary>
        SlowRing = 1,

        /// <summary>
        /// Cracks running out from under the emitter to the edge of what the
        /// bubble reached. The Slam's, which is one swing landing on everything
        /// touching him.
        /// </summary>
        GroundShock = 2,

        /// <summary>
        /// A ring hanging over the head of every tower the bubble reached, the
        /// emitter included. The Blessing's, and the one signature that is
        /// drawn on the things a bubble found rather than on the bubble.
        /// </summary>
        TowerGlow = 3,

        /// <summary>
        /// One heavy bar the whole length of the shot, held at that length for
        /// the whole of its life. The Overwatch's, whose read is the distance a
        /// single shot crossed — eight hexes of it, against the three the
        /// bottom of that line has.
        /// </summary>
        LongShot = 4,

        /// <summary>
        /// A knife that leaves the hand and crosses to the body the shot found.
        /// The Fan of Knives', which fires three shots at three bodies in one
        /// throw, so one throw draws three of these.
        /// </summary>
        ThrownKnife = 5,
    }
}
