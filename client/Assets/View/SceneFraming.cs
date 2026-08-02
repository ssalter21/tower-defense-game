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
    /// it, which is the point of <see cref="IsometricCameraRig"/> being
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
        /// The camera's downward tilt, in degrees. <c>atan(1 / sqrt(2))</c> —
        /// the true isometric angle, at which the three world axes project to
        /// equal screen lengths. Chosen as a number with a derivation rather
        /// than a number that looked right, so that "why 35 and not 30" has an
        /// answer.
        /// </summary>
        public const float CameraPitchDegrees = 35.264390f;

        /// <summary>
        /// Where the orbit starts, in degrees of yaw. Zero looks down the
        /// world's <c>+Z</c> axis, which is the direction the map's first row is
        /// drawn towards.
        /// </summary>
        public const float CameraBaseYawDegrees = 0f;

        /// <summary>
        /// How many snapped positions the orbit has. Six, because the floor is
        /// hexagonal and six is the number of ways a hex grid looks the same.
        /// </summary>
        public const int CameraSnapCount = 6;

        /// <summary>
        /// Degrees between snaps. Derived rather than typed, so it cannot
        /// disagree with <see cref="CameraSnapCount"/>.
        /// </summary>
        public const float CameraSnapDegrees = 360f / CameraSnapCount;

        /// <summary>
        /// How far back along its own axis the camera sits from the pivot. An
        /// orthographic camera does not change size with distance, so this only
        /// has to be far enough that nothing crosses the near plane.
        /// </summary>
        public const float CameraDistance = 60f;

        public const float CameraNearClip = 0.3f;

        public const float CameraFarClip = 240f;

        /// <summary>
        /// How much bigger than the floor the view is. 1.0 would put the
        /// corner tiles exactly on the screen edge at every snap; the margin is
        /// the breathing room.
        /// </summary>
        public const float CameraFramingMargin = 1.12f;

        // ---------------------------------------------------------------
        // The light
        // ---------------------------------------------------------------

        /// <summary>
        /// The sun's tilt and heading, in degrees. Fixed in world space, not
        /// parented to the camera: a light that orbits with the viewer makes
        /// every snap look identical, which would turn the six-snap check into
        /// a formality.
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
        /// The camera's rotation at a given snap. Snaps wrap, so any integer is
        /// a legal snap and stepping past five is stepping back to zero.
        /// </summary>
        public static Quaternion CameraRotation(int snap) =>
            Quaternion.Euler(CameraPitchDegrees, CameraBaseYawDegrees + (Wrap(snap) * CameraSnapDegrees), 0f);

        /// <summary>Reduces any integer to the snap it means, including negatives.</summary>
        public static int Wrap(int snap)
        {
            int wrapped = snap % CameraSnapCount;

            return wrapped < 0 ? wrapped + CameraSnapCount : wrapped;
        }
    }
}
