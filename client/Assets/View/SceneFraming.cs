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
        /// How long the reset key takes to ease the camera back to the default
        /// angle and the framed distance, in seconds.
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

        /// <summary>What the camera clears to where there is no floor.</summary>
        public static Color BackgroundColor => new Color(0.11f, 0.13f, 0.16f, 1f);

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
