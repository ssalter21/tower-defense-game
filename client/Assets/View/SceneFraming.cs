using UnityEngine;

namespace View
{
    /// <summary>
    /// Every number that decides what the playfield looks like, in one file a
    /// <c>git diff</c> can show.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This file exists so the scene does not.</b> The alternative is a
    /// camera and a light dragged into a scene asset, where their framing lives
    /// in serialized YAML — a format nobody may hand-edit, whose diffs are
    /// unreadable, and which merges by luck. Moving the camera three metres back
    /// should be a one-line change somebody can review. Here it is.
    /// </para>
    /// <para>
    /// Nothing in here is a simulation input. Changing every constant in this
    /// file changes what the match looks like and nothing about what happens in
    /// it, which is the point of <see cref="OrbitCameraRig"/> being
    /// view-only.
    /// </para>
    /// </remarks>
    public static class SceneFraming
    {
        /// <summary>
        /// The name the one root object carries, in the scene and at runtime.
        /// </summary>
        public const string RootObjectName = "Match";

        // ---------------------------------------------------------------
        // The camera
        // ---------------------------------------------------------------

        /// <summary>
        /// How wide the lens is, in degrees of vertical field of view. Narrow
        /// enough that the board is not bowed at the edges, wide enough that
        /// dollying in on one creep still shows the ground it stands on.
        /// </summary>
        public const float CameraFieldOfViewDegrees = 40f;

        /// <summary>
        /// The downward tilt the camera starts at and resets to, in degrees.
        /// <c>atan(1 / sqrt(2))</c> — the true isometric angle, at which the
        /// three world axes project to equal screen lengths. Chosen as a number
        /// with a derivation rather than a number that looked right, so that
        /// "why 35 and not 30" has an answer. Under perspective it is only
        /// where the camera starts: the projection converges, so the board is
        /// isometric-looking rather than isometric.
        /// </summary>
        public const float CameraDefaultPitchDegrees = 35.264390f;

        /// <summary>
        /// The heading the camera starts at and resets to, in degrees of yaw.
        /// Zero looks down the world's <c>+Z</c> axis, which is the direction
        /// the map's first row is drawn towards.
        /// </summary>
        public const float CameraDefaultYawDegrees = 0f;

        public const float CameraNearClip = 0.3f;

        /// <summary>
        /// The far plane. Has to clear the far edge of the floor seen from the
        /// dolly's outermost stop, which is <see cref="CameraMaxDistanceFactor"/>
        /// times the framed distance plus the floor's own diagonal. That cannot
        /// be derived here, because this file has never seen a floor — so a test
        /// measures it against the committed one instead.
        /// </summary>
        public const float CameraFarClip = 600f;

        /// <summary>
        /// How much bigger than the floor the view is. 1.0 would put the
        /// corner tiles exactly on the screen edge; the margin is the breathing
        /// room.
        /// </summary>
        public const float CameraFramingMargin = 1.12f;

        /// <summary>
        /// The closest the camera may get to the pivot, in metres. Under two
        /// metres of frame height at this lens, so one humanoid overflows it —
        /// which is what "close enough to read a model" means.
        /// </summary>
        public const float CameraMinDistance = 2f;

        /// <summary>
        /// How far out the dolly goes, as a multiple of the distance the whole
        /// floor fits at.
        /// </summary>
        public const float CameraMaxDistanceFactor = 2f;

        /// <summary>Degrees the rig turns per pixel the mouse is dragged.</summary>
        public const float CameraOrbitDegreesPerPixel = 0.2f;

        /// <summary>
        /// Exponential dolly steps per unit of scroll. Platforms disagree
        /// wildly about what one wheel notch reports — 120 on Windows, single
        /// digits elsewhere — so this is a feel number rather than a derived
        /// one, and it is here to be changed in one line.
        /// </summary>
        public const float CameraDollyPerScrollUnit = 0.002f;

        /// <summary>
        /// How far one second of a held flight key moves the pivot, as a
        /// fraction of the camera's current distance. A fraction rather than a
        /// speed in metres, so a press covers the same part of the picture
        /// whether the whole board is on screen or one creep is. A feel number,
        /// here to be changed in one line.
        /// </summary>
        public const float CameraFlyDistanceFractionPerSecond = 1f;

        /// <summary>
        /// How far one pixel of a middle-button drag moves the pivot, as a
        /// fraction of the camera's current distance.
        /// </summary>
        public const float CameraPanDistanceFractionPerPixel = 0.001f;

        /// <summary>
        /// How long the reset key takes to ease the camera back to the middle
        /// of the floor, the default angle and the framed distance, in seconds.
        /// </summary>
        public const float CameraResetSeconds = 0.25f;

        // ---------------------------------------------------------------
        // The light
        // ---------------------------------------------------------------

        /// <summary>
        /// The sun's tilt and heading, in degrees. Fixed in world space, not
        /// parented to the camera: a light that orbited with the viewer would
        /// make every angle look identically lit, and orbiting to see how a
        /// thing is shaped would show nothing.
        /// </summary>
        public const float SunPitchDegrees = 50f;

        public const float SunYawDegrees = -30f;

        public const float SunIntensity = 1.1f;

        /// <summary>
        /// Shadow strength. Non-zero on purpose: the shadows on this floor are
        /// cast by this light, and there are no painted-on ones anywhere.
        /// </summary>
        public const float SunShadowStrength = 0.7f;

        // ---------------------------------------------------------------
        // The two plain materials
        // ---------------------------------------------------------------

        /// <summary>
        /// The corridor's colour. Deliberately drab: this is a blockout, and
        /// anything prettier would be an art decision.
        /// </summary>
        public static Color RoadColor => new Color(0.44f, 0.38f, 0.31f, 1f);

        /// <summary>Everything that is not corridor.</summary>
        public static Color GrassColor => new Color(0.29f, 0.44f, 0.25f, 1f);

        /// <summary>
        /// What the camera clears to where there is no floor and no sky. Only
        /// reached in a checkout whose skybox shader is missing; see
        /// <see cref="SkyMaterial"/>.
        /// </summary>
        public static Color BackgroundColor => new Color(0.11f, 0.13f, 0.16f, 1f);

        // ---------------------------------------------------------------
        // The world behind and beneath the board
        // ---------------------------------------------------------------

        /// <summary>
        /// The tint of the sky overhead. A blue with some green in it rather
        /// than a pure one, so it sits against the atlas's grass instead of
        /// vibrating against it.
        /// </summary>
        public static Color SkyZenithColor => new Color(0.42f, 0.55f, 0.78f, 1f);

        /// <summary>
        /// The colour at the sky's horizon, and of the haze the land fades
        /// into. One colour for both on purpose — see <see cref="SkySettings"/>.
        /// </summary>
        public static Color SkyHazeColor => new Color(0.72f, 0.79f, 0.86f, 1f);

        /// <summary>
        /// The plain of land the board is cut out of. Much duller and much
        /// darker than the atlas's grass, and the first cut of it was neither:
        /// a plain mixed to match the tiles filled two thirds of every frame
        /// with a flat bright green, and the board read as a raft on a sea
        /// rather than as a piece of country. What is wanted is the same land
        /// seen far off, and distance takes the light and the saturation out of
        /// everything.
        /// </summary>
        public static Color LandColor => new Color(0.42f, 0.48f, 0.32f, 1f);

        /// <summary>How brightly the sky is drawn.</summary>
        public static float SkyExposure => 1.05f;

        /// <summary>
        /// How much air the sun's light comes through. Under one is a thin,
        /// hard, high-altitude sky; over one is a heavy washed one. Just under,
        /// because the board is lit as a clear day.
        /// </summary>
        public static float SkyAtmosphere => 0.7f;

        /// <summary>Which sun the sky draws: 0 none, 1 simple, 2 high quality.</summary>
        public static float SkySunQuality => 1f;

        /// <summary>How big the sun's disk is, as a fraction of the sky.</summary>
        public static float SkySunSize => 0.03f;

        /// <summary>How hard the sun's edge is. Higher is tighter.</summary>
        public static float SkySunConvergence => 6f;

        /// <summary>
        /// How many board-widths of land there are around the board.
        /// </summary>
        /// <remarks>
        /// Big enough that the plain's edge is off screen at every stop of the
        /// dolly, and no bigger: the disc is drawn every frame and the part of
        /// it past the haze is invisible by construction.
        /// <para>
        /// <b>Shrinking it does not put the horizon in the shipped shot, and it
        /// was tried.</b> The horizon of a flat plain sits at eye level, and the
        /// shipped camera is pitched 35 degrees down with a 20-degree half
        /// lens — so it is looking between 15 and 55 degrees below horizontal
        /// and the horizon is above the top of the frame whatever the disc's
        /// radius is. Land behind the board at that angle is what looking down
        /// at a landscape looks like; the sky arrives when the camera comes
        /// down, which is what the low and raking frames are for.
        /// </para>
        /// </remarks>
        public static float HorizonReachFactor => 6f;

        /// <summary>
        /// The most of the camera's far plane the land may use. Under one, or
        /// the plain is clipped and the hole in it is the sky.
        /// </summary>
        public static float HorizonFarClipShare => 0.6f;

        /// <summary>
        /// How far out the haze begins, in board-widths.
        /// </summary>
        /// <remarks>
        /// Fog is measured from the camera and not from the board, so this
        /// cannot be pushed out until no tile is ever touched by it without the
        /// plain staying a flat green sheet all the way to the sky. It is set
        /// just past the near half of the board instead: the front of the board
        /// is clear, the back of it carries a fifth of the haze, and that is
        /// aerial perspective rather than a fault — it is the cue that tells the
        /// eye the far edge is far.
        /// </remarks>
        public static float HazeNearBoards => 2.2f;

        /// <summary>How far out the haze is total, in board-widths.</summary>
        public static float HazeFarBoards => 5f;

        /// <summary>
        /// The most of the land's own radius the haze may take to close, so a
        /// small board's plain still ends in haze rather than in an edge.
        /// </summary>
        public static float HazeShareOfRadius => 0.85f;

        // ---------------------------------------------------------------
        // What stands on the plain
        // ---------------------------------------------------------------

        /// <summary>
        /// How far clear of the board's edge the treeline begins, in metres.
        /// </summary>
        /// <remarks>
        /// <b>Set off the camera rather than off taste.</b> A tree of this size
        /// standing this far out hides about three and a half metres of ground
        /// behind it at the shipped 35-degree pitch, which is less than the gap
        /// -- so no cell is occluded from the angle the game plays at. Drop the
        /// camera to the raking frames and the wood does cross in front of the
        /// near rim, which is what a treeline seen from ground level does.
        /// </remarks>
        public static float TreelineGap => 4f;

        /// <summary>How deep the band of wood is, in metres.</summary>
        public static float TreelineDepth => 13f;

        /// <summary>How far apart the wood's candidate positions are, in metres.</summary>
        public static float TreelineStep => 3f;

        /// <summary>
        /// The chance a candidate position at the near edge of the band is
        /// taken. It thins outward, so the wood frays instead of stopping on a
        /// line.
        /// </summary>
        public static float TreelineChance => 0.78f;

        /// <summary>
        /// How much bigger than authored a distant tree is drawn. Just over
        /// one: the groves are cut to fill a hex and these are standing on open
        /// ground where nothing sets their scale for the eye.
        /// </summary>
        public static float TreelineScale => 1.15f;

        /// <summary>
        /// How far clear of the board the hills begin, in metres.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The hills are a ring, not a scatter, and the first cut was a
        /// scatter.</b> Spread evenly from just off the board out to the edge of
        /// the plain they read as rubble strewn over open country: some of them
        /// beside the board where they looked like debris, most of them so far
        /// out that the haze had eaten them. Gathered into a band well beyond
        /// the wood they read as what they are, which is the far side of a
        /// valley.
        /// </para>
        /// <para>
        /// Nearly thirty hexes out, so a hill is something seen through air
        /// rather than a mountain parked beside the board.
        /// </para>
        /// </remarks>
        public static float DistantHillGap => 13f;

        /// <summary>How far apart the hills' candidate positions are, in metres.</summary>
        public static float DistantHillStep => 8f;

        /// <summary>
        /// The chance one of those positions carries a hill. High, because a
        /// range has to be continuous.
        /// </summary>
        /// <remarks>
        /// <b>Isolated hills in haze look like debris in the sky.</b> The first
        /// cut placed them sparsely and far out, where the land under them was
        /// already fully hazed -- so each one had no visible ground beneath it
        /// and read as a chunk floating over the horizon. Close enough that the
        /// ground still reads, and dense enough to form a line, they are the far
        /// side of a valley instead.
        /// </remarks>
        public static float DistantHillChance => 0.55f;

        /// <summary>
        /// What share of them are mountains rather than low mounds. Under half,
        /// so the skyline has a couple of peaks in it and is not a row of them.
        /// </summary>
        public static float DistantPeakShare => 0.5f;

        /// <summary>
        /// How far out the hills go, as a share of the plain's radius. Short of
        /// the whole, because past the haze they are haze.
        /// </summary>
        public static float DistantHillReach => 0.13f;

        /// <summary>How much bigger than authored the nearest hill is drawn.</summary>
        public static float DistantHillNearScale => 2.6f;

        /// <summary>
        /// How much bigger than authored the furthest one is. Much bigger,
        /// because a model cut to fill a two-metre hex is four pixels at a
        /// hundred and fifty metres, and a hill nobody can see is a hill nobody
        /// drew.
        /// </summary>
        public static float DistantHillFarScale => 4.2f;

        /// <summary>The sun's colour — very slightly warm, and otherwise white.</summary>
        public static Color SunColor => new Color(1f, 0.97f, 0.91f, 1f);

        /// <summary>The world rotation the sun is built with.</summary>
        public static Quaternion SunRotation => Quaternion.Euler(SunPitchDegrees, SunYawDegrees, 0f);

        /// <summary>
        /// The rotation a pivot carries at a heading and a tilt. Fixes the
        /// Euler convention in one place: tilt about <c>X</c>, heading about
        /// <c>Y</c>, and never any roll.
        /// </summary>
        public static Quaternion CameraRotation(float yawDegrees, float pitchDegrees) =>
            Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
    }
}
