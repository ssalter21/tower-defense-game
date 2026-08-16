namespace Sim
{
    /// <summary>Where a bubble is centred.</summary>
    public enum BubbleOrigin
    {
        /// <summary>Nowhere. The row carries no bubble.</summary>
        None = 0,

        /// <summary>On the thing that emitted it. The Soldier's sweep.</summary>
        Self = 1,

        /// <summary>On what the shot landed on. A mortar's blast.</summary>
        Target = 2,
    }

    /// <summary>Which side of the board a bubble reaches into.</summary>
    public enum BubbleAffects
    {
        /// <summary>Nobody. The row carries no bubble.</summary>
        None = 0,

        /// <summary>The emitter's own side.</summary>
        Friend = 1,

        /// <summary>The other side.</summary>
        Enemy = 2,
    }

    /// <summary>
    /// What a bubble carries into whatever it encloses: damage, or one of the
    /// four modifiable stats.
    /// </summary>
    /// <remarks>
    /// <b>Range is not on this list and never will be.</b> A tower's coverage is
    /// intersected with the route once, at load, and handed to the tick loop as
    /// intervals of distance -- see <see cref="TowerCoverage"/>. A payload that
    /// moved a range would have to rebuild those intervals inside the tick,
    /// which is the one thing that arrangement exists to avoid, so <c>range</c>
    /// is refused where the column is read rather than discovered later as a
    /// performance problem.
    /// </remarks>
    public enum BubblePayload
    {
        /// <summary>Nothing. The row carries no bubble.</summary>
        None = 0,

        /// <summary>The shot's own damage roll, applied to everything enclosed.</summary>
        Damage = 1,

        /// <summary>How fast a walking unit walks.</summary>
        Speed = 2,

        /// <summary>How long a placed unit waits between attacks.</summary>
        Cooldown = 3,

        /// <summary>How many points of armour a unit carries.</summary>
        Armour = 4,

        /// <summary>The raw pool that absorbs before health does.</summary>
        Shield = 5,
    }

    /// <summary>
    /// The seven columns a sweep, a blast and an aura all turned out to be: a
    /// bubble that emits something, distinguished only by where it centres, how
    /// often it fires and what it carries.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One mechanic rather than three, and the column count is identical
    /// either way.</b> A radial shot is a bubble centred on the shooter that
    /// fires with the attack; a blast is one centred on what the shot hit; a
    /// timed slow is one carrying a stat and a duration; an aura is one with a
    /// period, which is the whole of what makes it pulse on its own. Authored as
    /// three mechanics they would have cost the same seven columns each and left
    /// three sets of rules to diverge. See
    /// <c>docs/adr/0055-a-sweep-a-blast-and-an-aura-are-one-bubble.md</c>.
    /// </para>
    /// <para>
    /// <b>A bubble is one shot and draws one roll.</b> That is what separates it
    /// from <see cref="UnitType.Targets"/>, which fires n shots at n creeps and
    /// draws n rolls. The number of draws per attack is part of the determinism
    /// contract -- the dice stream's position is folded into the state hash
    /// every tick -- so the two shapes are kept apart at load rather than
    /// reconciled at the landing.
    /// </para>
    /// <para>
    /// <b>A radius is a sphere, and the rule is <see cref="Reach.Encloses"/>.</b>
    /// Height only ever costs a radius, which is what stops a bubble centred on
    /// a cliff blanketing the board, and there is exactly one spelling of that
    /// comparison. A bubble measured along the marching column instead would
    /// stop at the fold in a corridor that doubles back, and reaching the
    /// neighbouring leg of a fold is the case the Necromancer's shield exists
    /// for.
    /// </para>
    /// <para>
    /// <b><see cref="Absent"/> is <c>default</c>.</b> A row that carries no
    /// bubble writes <c>none</c> in its radius column and the six columns after
    /// it have to agree, because a number nobody reads would still move the
    /// content hash. So every enum here spells its absence first and a bubble
    /// nobody authored is the zero value rather than a flag beside one.
    /// </para>
    /// <para>
    /// <b>Absence is spelled in the radius column, which is why a radius of
    /// zero is a real authoring.</b> A range column spells "no reach" as zero
    /// and <see cref="Reach"/> answers accordingly -- nothing with zero range
    /// reaches anything, including the hex it stands on. A bubble spells "no
    /// bubble" as the word <c>none</c>, so its zero is free to mean something,
    /// and what it means is the centre alone. The two zeros are different
    /// questions and <see cref="ReachesOnlyItsCentre"/> is where this one is
    /// answered, rather than by softening the rule a range depends on.
    /// </para>
    /// </remarks>
    public readonly struct Bubble
    {
        /// <summary>
        /// What an absent radius folds as. Not a radius any row can author, so
        /// "no bubble" and "a bubble of no radius" -- which mean different
        /// things, the second being the centre alone -- cannot hash equal.
        /// </summary>
        private const int AbsentRadius = -1;

        private Bubble(
            int radiusMilliHex,
            BubbleOrigin origin,
            BubbleAffects affects,
            int periodTicks,
            BubblePayload payload,
            int magnitude,
            int durationTicks)
        {
            RadiusMilliHex = radiusMilliHex;
            Origin = origin;
            Affects = affects;
            PeriodTicks = periodTicks;
            Payload = payload;
            Magnitude = magnitude;
            DurationTicks = durationTicks;
        }

        /// <summary>No bubble at all, which is what every committed row carries.</summary>
        public static Bubble Absent => default;

        /// <summary>A bubble, from the seven columns that describe one.</summary>
        public static Bubble Of(
            int radiusMilliHex,
            BubbleOrigin origin,
            BubbleAffects affects,
            int periodTicks,
            BubblePayload payload,
            int magnitude,
            int durationTicks) =>
            new Bubble(radiusMilliHex, origin, affects, periodTicks, payload, magnitude, durationTicks);

        /// <summary>Whether this row carries a bubble at all.</summary>
        public bool Present => Payload != BubblePayload.None;

        /// <summary>
        /// How far it reaches, in thousandths of a hex, read as a sphere. Zero
        /// is the centre alone.
        /// </summary>
        public int RadiusMilliHex { get; }

        public BubbleOrigin Origin { get; }

        public BubbleAffects Affects { get; }

        /// <summary>
        /// Ticks between pulses. Zero fires with the attack; a positive value is
        /// what makes a bubble an aura.
        /// </summary>
        public int PeriodTicks { get; }

        public BubblePayload Payload { get; }

        /// <summary>A percentage, for every payload that is not damage.</summary>
        public int Magnitude { get; }

        /// <summary>Ticks the payload lasts. Zero is instant.</summary>
        public int DurationTicks { get; }

        /// <summary>
        /// Whether this bubble goes off as part of an attack rather than on a
        /// clock of its own. The two are the same mechanic and a different
        /// emitter.
        /// </summary>
        public bool FiresWithTheAttack => Present && PeriodTicks == 0;

        /// <summary>
        /// Whether this bubble pulses on its own, which is the whole of what an
        /// aura is.
        /// </summary>
        public bool IsAnAura => Present && PeriodTicks > 0;

        /// <summary>
        /// Whether this bubble reaches the one thing it is centred on and
        /// nothing else. A radius of zero is the target alone -- the
        /// Cryomancer's single-target slow -- and it is an authoring rather
        /// than an absence, because a bubble spells absence as the word
        /// <c>none</c> in the same column.
        /// </summary>
        /// <remarks>
        /// <b>It is asked instead of <see cref="Reach.Encloses"/> and never
        /// beside it.</b> That rule answers false at a radius of zero, on
        /// purpose and for the range column's sake: nothing with no reach
        /// reaches anything, including the hex it stands on. Softening it here
        /// would hand every walking row a hex of reach for no reason but the
        /// ground under it, which is exactly what
        /// <c>docs/adr/0054-height-is-a-relationship-and-a-radius-is-a-sphere.md</c>
        /// settled. So the zero a bubble authors is answered before the sphere
        /// is asked, and the sphere keeps the one meaning it has.
        /// </remarks>
        public bool ReachesOnlyItsCentre => Present && RadiusMilliHex == 0;

        /// <summary>
        /// Whether this is the one shape the tick loop resolves today: a damage
        /// bubble that goes off with the attack, instantly, against the other
        /// side.
        /// </summary>
        /// <remarks>
        /// Everything else -- a period, a stat, a duration -- is a timed
        /// per-creep effect, which belongs to #217 and is deliberately not
        /// half-built here. What a row like that gets is a refusal naming it at
        /// the moment a match is built out of it, rather than a column that
        /// parses and then quietly does nothing.
        /// </remarks>
        public bool IsAnInstantBlast =>
            Present
            && Payload == BubblePayload.Damage
            && Affects == BubbleAffects.Enemy
            && PeriodTicks == 0
            && DurationTicks == 0;

        public override string ToString() =>
            Present
                ? "a bubble carrying " + Payload.ToString().ToLowerInvariant()
                    + ", centred on " + Origin.ToString().ToLowerInvariant()
                : "no bubble";

        /// <summary>
        /// Folds the seven columns in file order. Called from
        /// <see cref="UnitType.Fold"/>'s layout-3 branch and from nowhere else.
        /// </summary>
        internal Hash64 Fold(Hash64 hash) =>
            hash
                .Add(Present ? RadiusMilliHex : AbsentRadius)
                .Add((int)Origin)
                .Add((int)Affects)
                .Add(PeriodTicks)
                .Add((int)Payload)
                .Add(Magnitude)
                .Add(DurationTicks);
    }
}
