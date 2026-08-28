namespace View
{
    /// <summary>
    /// Every number that decides how heavily a board is dressed. Plain, pure,
    /// and copied out of the asset a human slides — so
    /// <see cref="BoardScenery"/> stays a function of its arguments and can be
    /// tested without an editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These are chances and bands, never positions.</b> Where one particular
    /// tree goes is not tunable and never will be: that is what
    /// <see cref="BoardDressing"/> is for. Turning a knob here re-dresses the
    /// whole board and is expected to; moving one thing is an override and is
    /// expected to survive every knob.
    /// </para>
    /// <para>
    /// <b>Nothing here reaches the simulation.</b> Change every field and the
    /// match's result, its per-tick hash and its landmark table are identical —
    /// the same test <see cref="MatchTuning"/> and <see cref="SceneFraming"/>
    /// apply to themselves.
    /// </para>
    /// </remarks>
    public sealed class DressingSettings
    {
        /// <summary>The dressing as it ships, and what an unwired scene draws.</summary>
        public static DressingSettings Default => new DressingSettings();

        /// <summary>
        /// The chance a cell away from the corridor is filled by a stand of
        /// trees, in <c>[0, 1]</c>.
        /// </summary>
        public float GroveChance { get; set; } = 0.30f;

        /// <summary>
        /// The chance a cell on the board's border is filled by a mountain.
        /// Tried before <see cref="BorderGroveChance"/>.
        /// </summary>
        /// <remarks>
        /// Mountains are border-only. One standing mid-board is 1.8 metres of
        /// rock in front of whatever the player was looking at.
        /// </remarks>
        public float PeakChance { get; set; } = 0.34f;

        /// <summary>The chance a border cell that did not get a mountain gets trees.</summary>
        public float BorderGroveChance { get; set; } = 0.32f;

        /// <summary>The chance an undressed cell gets at least one small prop.</summary>
        public float PropChance { get; set; } = 0.42f;

        /// <summary>The chance a cell that got one prop gets a second.</summary>
        public float SecondPropChance { get; set; } = 0.30f;

        /// <summary>The chance a cell touching the corridor carries a camp instead.</summary>
        public float CampChance { get; set; } = 0.14f;

        /// <summary>
        /// How much bigger than authored a small prop is drawn.
        /// </summary>
        /// <remarks>
        /// The pack authors props for a camera standing on the board and this
        /// one frames the whole of it: a barrel is 20 centimetres against a
        /// 2-metre tile, which at that distance is about two pixels. Groves and
        /// mountains are left alone, being hex-sized already.
        /// </remarks>
        public float PropScale { get; set; } = 1.7f;

        /// <summary>How far out a prop stands, as a fraction of the circumradius.</summary>
        /// <remarks>
        /// The near end has to clear the middle of the hex, because that is
        /// where a tower is drawn and a prop inside one cannot be fixed by
        /// hiding it — the build phase's own preview would still show it.
        /// </remarks>
        public float RimNear { get; set; } = 0.52f;

        /// <summary>The outer end of that band. Under 1.0, or a prop hangs over the next cell.</summary>
        public float RimFar { get; set; } = 0.70f;

        /// <summary>How many clouds there are.</summary>
        public int CloudCount { get; set; } = 5;

        /// <summary>How high the lowest cloud floats above the board, in metres.</summary>
        public float CloudHeight { get; set; } = 6f;

        /// <summary>How much higher than that a cloud may be, in metres.</summary>
        public float CloudSpread { get; set; } = 2.5f;

        /// <summary>A copy, so a caller cannot edit the set another one is reading.</summary>
        public DressingSettings Copy() =>
            new DressingSettings
            {
                GroveChance = GroveChance,
                PeakChance = PeakChance,
                BorderGroveChance = BorderGroveChance,
                PropChance = PropChance,
                SecondPropChance = SecondPropChance,
                CampChance = CampChance,
                PropScale = PropScale,
                RimNear = RimNear,
                RimFar = RimFar,
                CloudCount = CloudCount,
                CloudHeight = CloudHeight,
                CloudSpread = CloudSpread,
            };
    }
}
