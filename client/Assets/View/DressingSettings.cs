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

        /// <summary>
        /// The chance a cell standing over a lower neighbour carries a mound on
        /// the lip of the drop, in <c>[0, 1]</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the other half of grading a slope, and it does the half
        /// geometry cannot.</b> Levels are half blocks now, so a hillside can
        /// step twice where it used to step once -- but a step is still a step,
        /// and the line where one tile's face ends and the next begins is still
        /// a clean hexagonal edge. A mound sitting astride that edge is what
        /// stops the eye finding it. The reference frames all do this: nothing
        /// in them changes height on a bare rim.
        /// </para>
        /// <para>
        /// <b>It is rolled per cell and placed toward the fall.</b> A mound is
        /// put on the side the ground drops away, not in the middle, so it
        /// reads as the shoulder of the hill rather than as a lump standing on
        /// top of it.
        /// </para>
        /// </remarks>
        public float RidgeChance { get; set; } = 0.5f;

        /// <summary>
        /// How far a cell on the board's edge hangs below its own face, in
        /// metres. What the board sits on.
        /// </summary>
        /// <remarks>
        /// A cell in the middle of the board has neighbours to measure its
        /// cliff against; a cell on the rim has nothing on one side, so how far
        /// the world falls away there is a decision. One metre reads as a piece
        /// of country lifted out of a landscape, which is what every diorama in
        /// the pack's own gallery is; zero reads as a sheet of tiles with the
        /// edges sawn off.
        /// </remarks>
        public float RimDrop { get; set; } = 1f;

        /// <summary>
        /// The level at and below which ground is drawn as water. Negative is a
        /// board with no water on it, which is what ships.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A lake is a hollow and a water line, and nothing else.</b> There
        /// is no water in the map format and there is not going to be: a cell
        /// the simulation cannot tell apart from grass is a cell every rule
        /// already handles, and adding a kind of terrain to
        /// <c>Sim.MapCell</c> would mean every reader of a map, every stored
        /// record and every test coordinate having an opinion about it. So the
        /// board is dug out where the lake goes and this says how high the
        /// water in it stands.
        /// </para>
        /// <para>
        /// <b>It follows that a tower may be built in the lake.</b> That is a
        /// true statement about the simulation and drawing it otherwise would
        /// be the view lying about the rules. A board that does not want it
        /// puts its water where nothing would want to build -- which is what a
        /// corner is for.
        /// </para>
        /// <para>
        /// <b>The corridor is never flooded.</b> A road cell keeps its road
        /// piece whatever its level, because the route is the one thing on the
        /// board a player has to be able to trace.
        /// </para>
        /// </remarks>
        public int WaterLevel { get; set; } = -1;

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
                RidgeChance = RidgeChance,
                RimDrop = RimDrop,
                WaterLevel = WaterLevel,
            };
    }
}
