using System;
using System.Collections.Generic;

namespace View
{
    /// <summary>
    /// Named dressings of the committed board, each one a proposal to look at
    /// rather than a setting to ship.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These exist to be rendered side by side and then mostly deleted.</b>
    /// The board's real dressing lives in
    /// <c>Assets/Settings/BoardDressing.asset</c>, which is the thing a human
    /// slides and which nothing here writes to. A preset is a whole set of
    /// numbers under one name so that <c>tools/capture-prototypes.ps1</c> can
    /// draw the same corridor six ways in one run and a person can choose.
    /// Once one is chosen the numbers move to that asset and this file loses
    /// the other five.
    /// </para>
    /// <para>
    /// <b>Every preset dresses the same map and changes nothing about it.</b>
    /// The corridor, its 51 cells and the tier of each of them come from
    /// <c>content/map.txt</c> and no preset touches them — so the match, its
    /// result, its landmark table and its per-tick hash are identical under all
    /// of these. What differs is scenery density, where the tall things stand,
    /// and how many ledges break a tier's drop.
    /// </para>
    /// <para>
    /// <b>Each is named for the reference frame it was read off.</b> The
    /// references are gathered in the hex landscape board; the note against each
    /// preset says which picture it is trying to be, because a set of numbers
    /// with no source is a set of numbers nobody can argue with.
    /// </para>
    /// </remarks>
    public static class SceneryPresets
    {
        /// <summary>The name a caller gets when it asks for nothing.</summary>
        public const string DefaultName = "as-it-ships";

        /// <summary>
        /// One proposal: its name, what it is trying to look like, and the
        /// numbers that get it there.
        /// </summary>
        public readonly struct Preset
        {
            public Preset(string name, string reference, string intent, DressingSettings settings)
            {
                Name = name;
                Reference = reference;
                Intent = intent;
                Settings = settings;
            }

            /// <summary>The name the tools take on the command line.</summary>
            public string Name { get; }

            /// <summary>The reference image this was read off.</summary>
            public string Reference { get; }

            /// <summary>What it is trying to prove, in one sentence.</summary>
            public string Intent { get; }

            /// <summary>The numbers.</summary>
            public DressingSettings Settings { get; }
        }

        /// <summary>Every preset, in the order they are worth looking at.</summary>
        public static IReadOnlyList<Preset> All => Build();

        /// <summary>The names, for a tool that wants to list them.</summary>
        public static IReadOnlyList<string> Names
        {
            get
            {
                var names = new List<string>();

                foreach (Preset preset in Build())
                {
                    names.Add(preset.Name);
                }

                return names;
            }
        }

        /// <summary>
        /// One preset by name, case insensitively.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If no preset has that name. A tool asked for a dressing that does not
        /// exist should stop rather than render the default under the wrong
        /// filename, which is the one failure that survives into a comparison
        /// and is never noticed.
        /// </exception>
        public static Preset ByName(string name)
        {
            foreach (Preset preset in Build())
            {
                if (string.Equals(preset.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return preset;
                }
            }

            throw new ArgumentException(
                "No scenery preset is named '" + name + "'. Known: " + string.Join(", ", Names) + ".",
                nameof(name));
        }

        private static IReadOnlyList<Preset> Build() => new[]
        {
            // The one the platform already gives, included so the comparison has
            // a floor. A set of five alternatives with nothing to beat is five
            // opinions; with this in the row it is a measurement.
            new Preset(
                "as-it-ships",
                "none -- the committed BoardDressing",
                "The board as it draws today: one bare metre of cliff at every tier change.",
                DressingSettings.Default),

            // The half step on its own, with every other number held still, so
            // that what the ledge does is separable from what a re-dressing does.
            new Preset(
                "half-step",
                "Medieval Hexagon Pack -- the nature usage guide",
                "Only change: a tier's drop is broken into two half-metre steps. Everything else is as it ships.",
                With(DressingSettings.Default, apron: 1, spread: 0.06f)),

            // Kay's own composition for the pack: high ground on one flank, a
            // road reading straight through the middle, and the mass of the
            // dressing pulled off the centre so the corridor stays legible.
            new Preset(
                "terraced-ridge",
                "Medieval Hexagon Pack -- ridge, lake, road",
                "The ridge reads as a hillside: ledged steps, tall things gathered on the high flank, "
                    + "the middle of the board left open.",
                new DressingSettings
                {
                    GroveChance = 0.22f,
                    PeakChance = 0.46f,
                    BorderGroveChance = 0.30f,
                    PropChance = 0.38f,
                    SecondPropChance = 0.26f,
                    CampChance = 0.16f,
                    ApronCount = 1,
                    ApronSpread = 0.07f,
                }),

            // The Builder Pack's wide render is mostly empty ground: perhaps six
            // built things across a hundred tiles. This is that ratio applied to
            // this board, and it is the one most likely to look too bare.
            new Preset(
                "sparse-country",
                "Medieval Builder Pack -- the wide rolling landscape",
                "Kay's ratio: far fewer stands of trees, more open grass, and the few tall things "
                    + "spaced far enough apart to be landmarks.",
                new DressingSettings
                {
                    GroveChance = 0.12f,
                    PeakChance = 0.30f,
                    BorderGroveChance = 0.22f,
                    PropChance = 0.26f,
                    SecondPropChance = 0.14f,
                    CampChance = 0.10f,
                    ApronCount = 1,
                    ApronSpread = 0.06f,
                }),

            // Every published render has a tall mass at the far edge that the
            // eye stops against. This turns the border up hard and thins the
            // middle, which is the density gradient the forest renders use.
            new Preset(
                "back-wall",
                "Medieval Hexagon Pack -- the three-biome strip; Forest Nature Pack -- cliff layering",
                "A rim of rock the eye stops against, with the board thinning towards the camera.",
                new DressingSettings
                {
                    GroveChance = 0.16f,
                    PeakChance = 0.62f,
                    BorderGroveChance = 0.30f,
                    PropChance = 0.34f,
                    SecondPropChance = 0.22f,
                    CampChance = 0.12f,
                    ApronCount = 2,
                    ApronSpread = 0.05f,
                }),

            // The nine-tile diorama: almost nothing standing, and what is there
            // is posted along the path. The test of whether this board needs
            // scenery at all or just needs shape.
            new Preset(
                "camp-road",
                "Medieval Hexagon Pack -- the nine-tile scene",
                "Almost bare, with the little that stands posted along the corridor. "
                    + "Tests whether the depth is terrain rather than dressing.",
                new DressingSettings
                {
                    GroveChance = 0.08f,
                    PeakChance = 0.26f,
                    BorderGroveChance = 0.14f,
                    PropChance = 0.20f,
                    SecondPropChance = 0.10f,
                    CampChance = 0.30f,
                    ApronCount = 1,
                    ApronSpread = 0.08f,
                }),
        };

        /// <summary>
        /// A copy of a set with the ledges changed and nothing else, so a preset
        /// that means "the shipped board plus one ledge" says exactly that
        /// instead of restating every other number and drifting from it.
        /// </summary>
        private static DressingSettings With(DressingSettings basis, int apron, float spread)
        {
            DressingSettings copy = basis.Copy();

            copy.ApronCount = apron;
            copy.ApronSpread = spread;

            return copy;
        }
    }
}
