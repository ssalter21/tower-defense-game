namespace View
{
    /// <summary>
    /// What one row's <i>bubble</i> is drawn as, so that a row reads as itself
    /// rather than as the disc every bubble in the game shares.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is bound per unit, on <see cref="UnitArt"/>, by the scene
    /// builder</b> — the same place the model, the atlas, the props and the
    /// effect anchor are bound. So a tier three carries a signature its tier
    /// two does not and one walking row carries a signature the row beside it
    /// does not, and adding one to a row is editing a table rather than editing
    /// <see cref="MatchDecorations"/>.
    /// </para>
    /// <para>
    /// <b>The names here are shapes and not rows.</b> What each shape is made
    /// of is <see cref="EffectMeshes"/>'s business and how big and what colour
    /// is <see cref="MatchTuning"/>'s; a row picks one of these and says
    /// nothing else about how it looks.
    /// </para>
    /// <para>
    /// <b>A bubble's shape and a shot's shape are two fields of two types.</b>
    /// They were one enum over both moments while no row wanted a shape at each
    /// of them, and the Cleric's and the Druid's capstones want exactly that: a
    /// bolt leaving the tome and an aura on the ground, on one row. Two types
    /// means a switch over this one is a switch over the shapes a bubble can
    /// leave and nothing else, and a shot's shape cannot be written into a
    /// bubble's slot at all. See <see cref="ShotSignature"/>.
    /// </para>
    /// <para>
    /// <b>A signature is reached through the entity the event named, and a
    /// walking row is reachable exactly where it emits.</b> An aura pulses from
    /// its emitter and a sweep is centred on the tower that swung, so both name
    /// a row the view is holding art for; four of the auras on this roster are
    /// carried by creeps, and a creep emitter is looked up the same way a tower
    /// one is. What a creep still has no way of naming is a point on its own
    /// art, because a walking row carries no effect anchor by assertion — see
    /// <c>ImportedArtTests.EveryTowerFiresFromAPointOnItsOwnArt</c> — so a
    /// creep's aura is centred on the body and never on the staff, the scythe,
    /// the broom or the axe it is holding.
    /// </para>
    /// <para>
    /// A blast centred on the body a shot arrived at names the body —
    /// the shooter is not in the event at all, deliberately, since an event
    /// carries an entity id and never a position or a reference to hold on to.
    /// Those are drawn off the shape of the event instead, and no row selects
    /// them; see <see cref="MatchDecorations.BlastLanded"/>.
    /// </para>
    /// </remarks>
    public enum BubbleSignature
    {
        /// <summary>
        /// The plain disc every bubble left before any row had a signature of
        /// its own — as wide as the bubble reached, on the ground under
        /// whatever it was centred on. Still what a row without one draws.
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
        /// emitter included. The Blessing's, and the first signature that is
        /// drawn on the things a bubble found rather than on the bubble.
        /// </summary>
        TowerGlow = 3,

        /// <summary>
        /// A disc of light lying on the ground out to the edge of the aura. The
        /// Consecration's, whose read is the ground the font has claimed —
        /// which is where a body loses its armour by standing.
        /// </summary>
        ConsecrationLight = 4,

        /// <summary>
        /// Roots breaking the ground under every body the aura is holding. The
        /// Overgrowth's, and the second signature drawn on what a bubble found
        /// rather than on the bubble — because that aura reaches sixty hexes,
        /// so a shape at its radius would be a shape the size of ten boards.
        /// </summary>
        OvergrowthRoots = 5,

        /// <summary>
        /// A ring over the head of every creep the pulse reached. The Skeleton
        /// Mage's haste, and deliberately the shape the Blessing already wears
        /// on the other side of the board: both auras make their own side
        /// faster, so what is worth seeing is which bodies got it.
        /// </summary>
        HasteRing = 6,

        /// <summary>
        /// A cage of arcs standing over the emitter, as wide as the pulse
        /// reached. The Necromancer's ward, and the one shape here that stands
        /// for a pool rather than for a stat that has moved.
        /// </summary>
        WardDome = 7,

        /// <summary>
        /// A band broken into plates lying on the ground out to the edge of
        /// what the pulse reached. The Witch's hex ward, which is armour going
        /// on, drawn as the shape the Unravel's strip already uses for armour
        /// coming off.
        /// </summary>
        HexPlates = 8,

        /// <summary>
        /// A crown of upright shards at the feet of every tower the pulse
        /// reached. The Frost Wight's frostbite, the one aura on the roster
        /// that reaches the other side, and so the one signature a walking row
        /// draws on something that stands still.
        /// </summary>
        FrostSpikes = 9,
    }

    /// <summary>
    /// What one row's <i>shot</i> is drawn as on its way to the body it found.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Bound per unit beside <see cref="BubbleSignature"/> and read at the one
    /// moment a shot is released. <b>The shooter is in that event</b>, unlike a
    /// target-centred blast's, so every row on this enum is reachable from its
    /// own event and no row here is drawn off the shape of one.
    /// </para>
    /// <para>
    /// <b>Only a hitscan row names one.</b> A projectile row's shot is a real
    /// entity in the snapshot flying the same line over the ticks the row's
    /// flight column gives it, so a shape drawn here would be a second thing in
    /// the air saying what the shell already says.
    /// </para>
    /// </remarks>
    public enum ShotSignature
    {
        /// <summary>
        /// The thin tracer every hitscan row drew before any row had a shape of
        /// its own — muzzle to body, closing to nothing as it ages. Still what
        /// a row without one draws.
        /// </summary>
        None = 0,

        /// <summary>
        /// One heavy bar the whole length of the shot, held at that length for
        /// the whole of its life. The Overwatch's, whose read is the distance a
        /// single shot crossed — eight hexes of it, against the three the
        /// bottom of that line has.
        /// </summary>
        LongShot = 1,

        /// <summary>
        /// A knife that leaves the hand and crosses to the body the shot found.
        /// The Fan of Knives', which fires three shots at three bodies in one
        /// throw, so one throw draws three of these.
        /// </summary>
        ThrownKnife = 2,

        /// <summary>
        /// A short shaft that leaves the tome or the staff tip and crosses to
        /// the body. What every hitscan rung of the Cleric and Druid lines
        /// fires, so it is the one shape here that a whole line wears rather
        /// than one capstone.
        /// </summary>
        MagicBolt = 3,
    }

    /// <summary>
    /// The pair one row carries: what its bubble leaves and what its shot is
    /// drawn as.
    /// </summary>
    /// <remarks>
    /// <b>Handed over together because an event arrives at a row and not at a
    /// moment.</b> <see cref="MatchDecorations"/> turns the entity an event
    /// names into the row that emitted it, once, and then reads whichever half
    /// the event was about — so one lookup answers a shot, a sweep and a pulse
    /// rather than three.
    /// </remarks>
    public readonly struct RowSignature
    {
        /// <summary>The pair a row's art names.</summary>
        public RowSignature(BubbleSignature bubble, ShotSignature shot)
        {
            Bubble = bubble;
            Shot = shot;
        }

        /// <summary>What this row's bubble leaves.</summary>
        public BubbleSignature Bubble { get; }

        /// <summary>What this row's shot is drawn as.</summary>
        public ShotSignature Shot { get; }
    }
}
