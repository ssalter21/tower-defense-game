namespace View
{
    /// <summary>
    /// Which aura one row's <i>bubble</i> is, so that its circle reads as that
    /// row's rather than as the plain one every bubble in the game shares.
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
    /// <b>These used to be shapes and are now identities.</b> Each name below
    /// picked a shape of its own — a ring, cracks, a halo, a light, roots, a
    /// cage, plates, a crown of shards — until Sam replaced all of them with
    /// one flat translucent circle on 7 Sep 2026; see
    /// <c>docs/decision-log.md</c>. What a member selects now is a colour and
    /// how long the circle stays, both out of <see cref="MatchTuning"/>. The
    /// names are kept because they still say which row's aura it is, and
    /// because the shapes are expected back when the effects are animated
    /// properly.
    /// </para>
    /// <para>
    /// <b>Nothing is drawn on the bodies an aura found.</b> Four of the members
    /// below were: the Blessing's halo over each tower, the haste's ring over
    /// each creep, the frostbite's crown at each tower's feet and the
    /// Overgrowth's roots under each held body. Which bodies an aura caught is
    /// read off the circle they are standing in.
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
        /// No aura of its own: the plain circle, in the colour every bubble
        /// shared before any row was told apart from another. Still what a row
        /// without a signature draws.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Shield Wall's slow, whose whole read is where it stops. It was
        /// an open ring at that edge, for the same reason the circle is
        /// translucent: so the bodies caught inside stay visible through it.
        /// </summary>
        SlowRing = 1,

        /// <summary>
        /// The Slam's swing, landing on everything touching him. Drawn at the
        /// reach like every other bubble, though it is a blow and not an aura
        /// that stands — the one member here where that is arguable.
        /// </summary>
        GroundShock = 2,

        /// <summary>
        /// The Blessing's, which makes every tower inside it fire faster. It
        /// used to hang a ring over each of those towers; the circle says the
        /// same thing by covering the ground they stand on.
        /// </summary>
        TowerGlow = 3,

        /// <summary>
        /// The Consecration's, whose read is the ground the font has claimed —
        /// where a body loses its armour by standing. The one aura the new rule
        /// changed least, since it was already a disc out to its reach.
        /// </summary>
        ConsecrationLight = 4,

        /// <summary>
        /// The Overgrowth's, and <b>the one aura that draws nothing at all</b>.
        /// It reaches sixty hexes, so a circle at its radius is a hundred and
        /// twenty across on a board nineteen wide — the screen washed flat
        /// rather than an area shown. Sam's call; see
        /// <c>docs/decision-log.md</c>. The hold reads through the creeps not
        /// moving.
        /// </summary>
        OvergrowthRoots = 5,

        /// <summary>
        /// The Skeleton Mage's haste — the Blessing's opposite number, since
        /// both auras make their own side faster. It used to put a ring over
        /// each creep it reached.
        /// </summary>
        HasteRing = 6,

        /// <summary>
        /// The Necromancer's ward, the one member here that stands for a pool
        /// rather than for a stat that has moved. It used to be a cage of arcs
        /// standing over the emitter, and lies flat like everything else now.
        /// </summary>
        WardDome = 7,

        /// <summary>
        /// The Witch's hex ward, which is armour going on. It used to borrow
        /// the broken band the Unravel's strip still uses for armour coming
        /// off; only the Unravel keeps that shape now, because a blast is not
        /// an aura.
        /// </summary>
        HexPlates = 8,

        /// <summary>
        /// The Frost Wight's frostbite, the one aura on the roster that reaches
        /// the other side. It used to stand a crown of shards at the feet of
        /// every tower it caught; the circle covers those towers instead.
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
