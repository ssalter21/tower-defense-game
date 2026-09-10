using UnityEngine;

namespace View
{
    /// <summary>
    /// The four colours and two numbers that decide what is behind and beneath
    /// the board: the sky, the haze at its horizon, the land running out to it,
    /// and how bright the whole of that is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A struct with a default, so a caller may say nothing.</b> Every field
    /// left at zero means "the committed look", which is what
    /// <see cref="OrDefault"/> substitutes — so a board built without an opinion
    /// about its sky gets the one in <see cref="SceneFraming"/> rather than a
    /// black dome and a black plain.
    /// </para>
    /// <para>
    /// <b>The haze is one colour doing two jobs.</b> It is the fog the land
    /// fades into and it is the bottom of the sky, and those have to be the same
    /// colour or the join between them is the hard line the horizon exists to
    /// remove. Keeping it as one field means it cannot be half-changed.
    /// </para>
    /// </remarks>
    public readonly struct SkySettings
    {
        public SkySettings(
            Color zenith, Color haze, Color land, float exposure, float atmosphere)
        {
            Zenith = zenith;
            Haze = haze;
            Land = land;
            Exposure = exposure;
            Atmosphere = atmosphere;
        }

        /// <summary>The committed look, and what an unwired board draws.</summary>
        public static SkySettings Default =>
            new SkySettings(
                SceneFraming.SkyZenithColor,
                SceneFraming.SkyHazeColor,
                SceneFraming.LandColor,
                SceneFraming.SkyExposure,
                SceneFraming.SkyAtmosphere);

        /// <summary>The tint of the sky overhead.</summary>
        public Color Zenith { get; }

        /// <summary>The colour at the sky's horizon, and of the distance haze.</summary>
        public Color Haze { get; }

        /// <summary>The plain of land the board is cut out of.</summary>
        public Color Land { get; }

        /// <summary>How bright the sky is drawn. Around one; higher is hazier.</summary>
        public float Exposure { get; }

        /// <summary>
        /// How much air the light is coming through. Low is a thin, hard,
        /// high-altitude sky; high is a heavy, washed one near the horizon.
        /// </summary>
        public float Atmosphere { get; }

        /// <summary>
        /// This look, or the committed one where nothing was said. A zero
        /// exposure is the tell: no sky anybody meant has one.
        /// </summary>
        public SkySettings OrDefault() => Exposure <= 0f ? Default : this;
    }
}
