using System;
using System.Collections.Generic;

namespace View
{
    /// <summary>
    /// One landscape per reference frame: a board to draw, an atlas to draw it
    /// in, a light to draw it under and a dressing to scatter over it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These existed to be compared and then mostly deleted, and mostly
    /// they have been.</b> Six landscapes were drawn over one road;
    /// <c>rolling-country</c> was chosen, its numbers moved to
    /// <c>client/Assets/Settings/BoardDressing.asset</c>, its board to
    /// <c>content/map.txt</c> and its atlas onto <c>Materials/Tiles.mat</c>, and
    /// the other four are gone from this file and from
    /// <c>docs/prototypes/</c>. What is left is the control, which draws
    /// whatever <c>content/map.txt</c> currently is, and the preset it was
    /// adopted from. Nothing in the game reads this file — only
    /// <see cref="Editor.PrototypeCapture"/> does.
    /// </para>
    /// <para>
    /// <b>Each one is a reading of a particular picture, not a taste.</b> The
    /// reference is named on every preset, because "which of these six do you
    /// like" is a much worse question than "which of these six is the picture
    /// you pointed at". Where a preset fails to be its reference that is a
    /// finding, and it is one that can only be had by putting them side by
    /// side.
    /// </para>
    /// <para>
    /// <b>The road is the same road in all of them.</b> Every board is
    /// <c>content/map.txt</c>'s corridor, cell for cell, under a different
    /// height map — so what differs between two of these pictures is the
    /// landscape and never the route. See <c>docs/prototypes/boards/</c>.
    /// </para>
    /// </remarks>
    public static class SceneryPresets
    {
        /// <summary>The name of the one landscape that ships, meaning none of these.</summary>
        public const string Shipped = "as-it-ships";

        private static readonly Preset[] Presets = Build();

        /// <summary>Every preset, in the order they are worth looking at.</summary>
        public static IReadOnlyList<Preset> All => Presets;

        /// <summary>Their names, for an error message that has to list them.</summary>
        public static IReadOnlyList<string> Names
        {
            get
            {
                var names = new string[Presets.Length];

                for (int index = 0; index < Presets.Length; index++)
                {
                    names[index] = Presets[index].Name;
                }

                return names;
            }
        }

        /// <summary>One preset by name.</summary>
        /// <exception cref="ArgumentException">If nothing is called that.</exception>
        public static Preset ByName(string name)
        {
            foreach (Preset preset in Presets)
            {
                if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            throw new ArgumentException(
                "There is no scenery preset called '" + name + "'. They are: "
                + string.Join(", ", Names) + ".",
                nameof(name));
        }

        private static Preset[] Build() =>
            new[]
            {
                // The board as it stands. It used to be the thing to beat --
                // three heights and a whole block between each -- and it is now
                // rolling-country, adopted. It stays in the set because a
                // control drawn from content/map.txt is the one frame that
                // cannot go stale, and because it is where the shipped atlas
                // and the shipped dressing are seen rather than a preset's --
                // the capture swaps the committed BoardDressing.asset in for
                // this one rather than using the settings written here.
                new Preset(
                    Shipped,
                    "none -- the committed board",
                    "The board as it is today, which since the adoption is rolling-country: gentle "
                    + "relief, every change of height a half block, nothing stepping a whole one.",
                    board: null,
                    atlas: null,
                    Sunlight.Default,
                    DressingSettings.Default),

                // "The best landscape render in the collection" -- Medieval
                // Builder Pack. Six built things per hundred tiles, and the rest
                // forest, rock and empty grass.
                new Preset(
                    "rolling-country",
                    "The best landscape render in the collection",
                    "Gentle relief and nothing dramatic. The interest is meant to come from what "
                    + "stands on the ground rather than from the ground, so the wood is heavy and "
                    + "the built things are few.",
                    board: "rolling-country",
                    atlas: "hexagons_medieval_Summer",
                    new Sunlight(-24f, 56f, 1.05f, 1f, 0.98f, 0.93f, 0.36f, 0.38f, 0.40f),
                    new DressingSettings
                    {
                        GroveChance = 0.46f,
                        PeakChance = 0.30f,
                        BorderGroveChance = 0.44f,
                        PropChance = 0.24f,
                        SecondPropChance = 0.22f,
                        CampChance = 0.08f,
                        RidgeChance = 0.44f,
                        RimDrop = 0.8f,
                        CloudCount = 4,
                    }),
            };

        /// <summary>
        /// The sun a board is lit by. Part of a preset because half of what
        /// separates two of these pictures is where the shadows fall: the same
        /// cliff is a cliff or a smudge depending on whether anything is casting
        /// across it.
        /// </summary>
        public readonly struct Sunlight
        {
            public Sunlight(
                float yaw,
                float pitch,
                float intensity,
                float red,
                float green,
                float blue,
                float ambientRed,
                float ambientGreen,
                float ambientBlue)
            {
                Yaw = yaw;
                Pitch = pitch;
                Intensity = intensity;
                Red = red;
                Green = green;
                Blue = blue;
                AmbientRed = ambientRed;
                AmbientGreen = ambientGreen;
                AmbientBlue = ambientBlue;
            }

            /// <summary>The light the project ships with, from <see cref="SceneFraming"/>.</summary>
            public static Sunlight Default =>
                new Sunlight(
                    SceneFraming.SunYawDegrees,
                    SceneFraming.SunPitchDegrees,
                    SceneFraming.SunIntensity,
                    1f,
                    0.97f,
                    0.91f,
                    0.32f,
                    0.34f,
                    0.38f);

            public float Yaw { get; }

            public float Pitch { get; }

            public float Intensity { get; }

            public float Red { get; }

            public float Green { get; }

            public float Blue { get; }

            public float AmbientRed { get; }

            public float AmbientGreen { get; }

            public float AmbientBlue { get; }
        }

        /// <summary>One named landscape: what to draw, and what it is trying to be.</summary>
        public readonly struct Preset
        {
            public Preset(
                string name,
                string reference,
                string intent,
                string board,
                string atlas,
                Sunlight light,
                DressingSettings settings,
                SkySettings sky = default)
            {
                Name = name;
                Reference = reference;
                Intent = intent;
                Board = board;
                Atlas = atlas;
                Light = light;
                Settings = settings;
                Sky = sky;
            }

            /// <summary>What it is called on the command line and in the file names.</summary>
            public string Name { get; }

            /// <summary>The reference frame it is a reading of.</summary>
            public string Reference { get; }

            /// <summary>What it is trying to do, in a sentence.</summary>
            public string Intent { get; }

            /// <summary>
            /// The board file under <c>docs/prototypes/boards/</c>, without its
            /// extension. Null draws the committed <c>content/map.txt</c>.
            /// </summary>
            public string Board { get; }

            /// <summary>
            /// The texture under <c>client/Assets/Art/Buildings/</c>, without its
            /// extension. Null wears the atlas the project ships.
            /// </summary>
            /// <remarks>
            /// The pack cuts four atlases against one set of UVs, so a whole
            /// season is one texture swap and no geometry at all. It is worth
            /// knowing before anybody models a second biome.
            /// <para>
            /// <b>And the shipped one is the worst of the four for this.</b>
            /// <c>hexagons_medieval</c> puts an olive-yellow in the swatch the
            /// grass tiles sample, which at board scale reads as scorched
            /// rather than green; <c>_Summer</c> puts an actual green there and
            /// is the same geometry and the same UVs. Five of the six presets
            /// below name an atlas for that reason, and only the control wears
            /// the shipped one -- so if these pictures look greener than the
            /// game does, this field is why.
            /// </para>
            /// </remarks>
            public string Atlas { get; }

            /// <summary>The sun it is lit by.</summary>
            public Sunlight Light { get; }

            /// <summary>How heavily it is dressed.</summary>
            public DressingSettings Settings { get; }

            /// <summary>
            /// What is behind and beneath it. Left at its default on a preset
            /// that wants the committed sky, which both of these do.
            /// </summary>
            /// <remarks>
            /// <b>Nothing overrides it now, and the hook is kept on purpose.</b>
            /// The two presets that did were the two not wearing a green atlas,
            /// and they are gone -- but a summer sky over an autumn board is
            /// exactly the join that gives a re-skin away, so the next seasonal
            /// board will want this and it is one defaulted argument to leave
            /// standing.
            /// </remarks>
            public SkySettings Sky { get; }
        }
    }
}
