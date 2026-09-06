namespace View
{
    /// <summary>
    /// What one row's bubble is drawn as, so that a capstone reads as itself
    /// rather than as the shape every bubble in the game shares.
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
    /// <b>A signature is reached through the entity the event named, so only a
    /// tower's is reachable.</b> An aura pulses from its emitter and a sweep is
    /// centred on the tower that swung, so both name a row the view is holding
    /// art for. A blast centred on the body a shot arrived at names the body —
    /// the shooter is not in the event at all, deliberately, since an event
    /// carries an entity id and never a position or a reference to hold on to.
    /// That one case is drawn as a burst at the radius it reached, and is the
    /// one signature no row selects; see
    /// <see cref="MatchDecorations.BlastLanded"/>.
    /// </para>
    /// </remarks>
    public enum EffectSignature
    {
        /// <summary>
        /// The plain disc on the ground that every bubble had before any row
        /// had a signature of its own — as wide as the bubble reached and
        /// saying nothing else. Still what a row without a signature draws.
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
    }
}
