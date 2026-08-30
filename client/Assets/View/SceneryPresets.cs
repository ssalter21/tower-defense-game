using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// One landscape per reference frame: a board to draw, an atlas to draw it
    /// in, a light to draw it under and a dressing to scatter over it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These exist to be compared and then mostly deleted.</b> Once one is
    /// chosen its numbers move to <c>client/Assets/Settings/BoardDressing.asset</c>
    /// and its board to <c>content/map.txt</c>, and the rest go. Nothing in the
    /// game reads this file — only <see cref="Editor.PrototypeCapture"/> does.
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
    /// <b>The road is the same road in all six.</b> Every board is
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
                // and the shipped dressing are seen rather than a preset's.
                new Preset(
                    Shipped,
                    "none -- the committed board",
                    "The board as it is today, which since the adoption is rolling-country: gentle "
                    + "relief, every change of height a half block, nothing stepping a whole one.",
                    board: null,
                    atlas: null,
                    Sunlight.Default,
                    DressingSettings.Default),

                // "Ridge, lake, road" -- itch.io gallery, Medieval Hexagon Pack.
                new Preset(
                    "ridge-lake-road",
                    "Ridge, lake, road",
                    "The road kept low and unbroken through the middle, a bank of high ground owning "
                    + "one flank, and a lake eating a corner so no part of the board is uniformly busy.",
                    board: "ridge-lake-road",
                    atlas: "hexagons_medieval_Summer",
                    new Sunlight(-34f, 44f, 1.15f, 0.99f, 0.95f, 0.86f, 0.34f, 0.36f, 0.40f),
                    new DressingSettings
                    {
                        WaterLevel = 0,
                        GroveChance = 0.26f,
                        PeakChance = 0.40f,
                        BorderGroveChance = 0.30f,
                        PropChance = 0.34f,
                        CampChance = 0.20f,
                        RidgeChance = 0.58f,
                        RimDrop = 1f,
                        CloudCount = 6,
                    }),

                // "The signature composition" -- the pack's cover render.
                new Preset(
                    "signature-strip",
                    "The signature composition",
                    "One continuous climb from the near corner to the far one rather than a grid of "
                    + "plateaus, with everything tall gathered at the far end.",
                    board: "signature-strip",
                    atlas: "hexagons_medieval_Summer",
                    new Sunlight(-52f, 38f, 1.2f, 1f, 0.96f, 0.88f, 0.33f, 0.36f, 0.42f),
                    new DressingSettings
                    {
                        GroveChance = 0.22f,
                        PeakChance = 0.52f,
                        BorderGroveChance = 0.26f,
                        PropChance = 0.30f,
                        CampChance = 0.16f,
                        RidgeChance = 0.62f,
                        RimDrop = 1f,
                        CloudCount = 8,
                        CloudHeight = 7f,
                    }),

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

                // "Three-deep cliff layering" -- Forest Nature Pack. Every level
                // change has a visible rock face, and the tree clusters straddle
                // the edges instead of sitting neatly on one level.
                new Preset(
                    "three-deep-cliff",
                    "Three-deep cliff layering",
                    "Three flat shelves parted by whole-block faces, and the wood pushed onto the "
                    + "lips of them rather than centred on the shelves.",
                    board: "three-deep-cliff",
                    atlas: "hexagons_medieval_Summer",
                    new Sunlight(-62f, 32f, 1.25f, 1f, 0.95f, 0.87f, 0.30f, 0.33f, 0.40f),
                    new DressingSettings
                    {
                        GroveChance = 0.30f,
                        PeakChance = 0.44f,
                        BorderGroveChance = 0.34f,
                        PropChance = 0.28f,
                        CampChance = 0.14f,
                        RidgeChance = 0.82f,
                        RimDrop = 1.2f,
                        CloudCount = 5,
                    }),

                // "Canyon variant" -- the arid cut of the builder-pack
                // landscape, whose stepped walls read as a much stronger drop
                // than the green version's.
                new Preset(
                    "canyon-run",
                    "Canyon variant",
                    "The road on the floor of a trench with the walls stepping away from it. The "
                    + "autumn atlas, because the arid read is what makes the reference land.",
                    board: "canyon-run",
                    atlas: "hexagons_medieval_Fall",
                    new Sunlight(-18f, 28f, 1.3f, 1f, 0.93f, 0.80f, 0.34f, 0.31f, 0.30f),
                    new DressingSettings
                    {
                        GroveChance = 0.10f,
                        PeakChance = 0.46f,
                        BorderGroveChance = 0.12f,
                        PropChance = 0.38f,
                        SecondPropChance = 0.34f,
                        CampChance = 0.22f,
                        RidgeChance = 0.70f,
                        RimDrop = 1.4f,
                        CloudCount = 3,
                    },
                    // A dusty sky over a dusty board. The haze is warm, so the
                    // land runs out into the same air the autumn atlas is lit
                    // by rather than into a summer afternoon.
                    new SkySettings(
                        new Color(0.62f, 0.66f, 0.74f, 1f),
                        new Color(0.86f, 0.79f, 0.67f, 1f),
                        new Color(0.47f, 0.41f, 0.31f, 1f),
                        1.30f,
                        1.15f)),

                // "Clay render -- read the silhouette" and "Diorama scale". A low
                // flat plate with three or four vertical incidents rising off it.
                new Preset(
                    "diorama-plate",
                    "Clay render, and diorama scale",
                    "A low flat plate with four vertical incidents rising off it and very little "
                    + "else. The winter atlas caps them, which is what turns an incident into a "
                    + "landmark.",
                    board: "diorama-plate",
                    atlas: "hexagons_medieval_Winter",
                    new Sunlight(-44f, 34f, 1.15f, 0.96f, 0.97f, 1f, 0.38f, 0.41f, 0.48f),
                    new DressingSettings
                    {
                        GroveChance = 0.16f,
                        PeakChance = 0.58f,
                        BorderGroveChance = 0.14f,
                        PropChance = 0.20f,
                        CampChance = 0.12f,
                        RidgeChance = 0.40f,
                        RimDrop = 1.4f,
                        CloudCount = 6,
                        CloudHeight = 7.5f,
                    },
                    // A cold, thin, high sky over the snow caps, and a land
                    // the colour of the plate rather than of grass.
                    new SkySettings(
                        new Color(0.60f, 0.70f, 0.86f, 1f),
                        new Color(0.84f, 0.87f, 0.90f, 1f),
                        new Color(0.42f, 0.46f, 0.42f, 1f),
                        1.25f,
                        0.75f)),
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
            /// that wants the committed sky, which is most of them: the two that
            /// name one are the two not wearing a green atlas, and a summer sky
            /// over an autumn board is the join that gives a re-skin away.
            /// </summary>
            public SkySettings Sky { get; }
        }
    }
}
