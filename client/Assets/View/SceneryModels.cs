using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The models behind each <see cref="SceneryGroup"/>, and the material they
    /// wear.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A group is a list, not a model.</b> <see cref="BoardScenery"/> asks
    /// for "a grove" and a variant number; which of the six stands of trees that
    /// turns out to be is decided here. So adding a tree to the pack is an edit
    /// to a path list in the scene builder and nothing else — no new enum
    /// member, no new field, no change to the placement rule.
    /// </para>
    /// <para>
    /// <b>The variant wraps.</b> The chooser hands over an arbitrary number
    /// because it does not know how many models a group has, and must not: it
    /// would be a second place the art is counted, and the two would disagree
    /// the first time somebody added a rock.
    /// </para>
    /// <para>
    /// <b>Absent is a valid state.</b> A project with no scenery imported draws
    /// a bare board rather than throwing, because scenery is the one thing on
    /// the floor that carries no information — every rock could vanish and the
    /// match would still be legible. That is the opposite of the rule
    /// <see cref="TileSet"/> lives under, where a missing model is a hole in the
    /// path, and the difference is deliberate.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class SceneryModels
    {
        [SerializeField]
        [Tooltip("Small things that stand near a rim: rocks, single trees, a barrel, a crate, a haybale.")]
        private Mesh[] rimProps = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("A defended camp: tent, weapon rack, target, wheelbarrow.")]
        private Mesh[] camp = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Stands of trees that fill a hex.")]
        private Mesh[] groves = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Mountains. Drawn on the border, where they frame rather than block.")]
        private Mesh[] peaks = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Clouds, above the board.")]
        private Mesh[] clouds = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Drawn on every piece of scenery. The pack's atlas, the same one the tiles wear.")]
        private Material surface;

        [SerializeField]
        [Tooltip("Models named one by one from content/dressing.txt, each with the atlas its own pack ships.")]
        private CataloguedModel[] catalogue = Array.Empty<CataloguedModel>();

        /// <summary>
        /// One model addressed by name, carrying the material it is drawn with.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The material rides with the model because the packs do not share
        /// an atlas.</b> <see cref="SceneryModels.Surface"/> is one material for
        /// the whole board and that was right while every piece of scenery came
        /// out of Medieval Hexagon. It stops being right the moment a City
        /// Builder crate stands next to a hex tree: the crate's UVs address
        /// <c>citybits_texture</c>, and drawn against the hexagon atlas it is
        /// not a slightly-wrong crate, it is confetti. So a catalogued model
        /// brings its own.
        /// </para>
        /// <para>
        /// <b>Only what <c>content/dressing.txt</c> actually names is in
        /// here.</b> The import is four thousand models; serializing all of them
        /// into the scene would load four thousand meshes to draw the six
        /// somebody placed. The bake fills this from the file it just wrote,
        /// which is why baking is one button and not two.
        /// </para>
        /// </remarks>
        [Serializable]
        public struct CataloguedModel
        {
            [SerializeField]
            [Tooltip("The name a dressing line uses: the path under Assets/Art/Kaykit, without the extension.")]
            private string name;

            [SerializeField]
            [Tooltip("The mesh.")]
            private Mesh mesh;

            [SerializeField]
            [Tooltip("The atlas material of the pack this model came out of.")]
            private Material material;

            /// <summary>Builds one entry.</summary>
            public CataloguedModel(string name, Mesh mesh, Material material)
            {
                this.name = name;
                this.mesh = mesh;
                this.material = material;
            }

            /// <summary>The name a dressing line uses.</summary>
            public string Name => name;

            /// <summary>The mesh.</summary>
            public Mesh Mesh => mesh;

            /// <summary>The material it is drawn with.</summary>
            public Material Material => material;
        }

        /// <summary>A set built in code rather than deserialized, for the editor tools.</summary>
        public static SceneryModels Of(
            Mesh[] rimProps,
            Mesh[] camp,
            Mesh[] groves,
            Mesh[] peaks,
            Mesh[] clouds,
            Material surface) =>
            new SceneryModels
            {
                rimProps = rimProps ?? Array.Empty<Mesh>(),
                camp = camp ?? Array.Empty<Mesh>(),
                groves = groves ?? Array.Empty<Mesh>(),
                peaks = peaks ?? Array.Empty<Mesh>(),
                clouds = clouds ?? Array.Empty<Mesh>(),
                surface = surface,
            };

        /// <summary>
        /// The same set with a named catalogue attached. What the scene builder
        /// hands over once it has resolved the names the dressing file uses.
        /// </summary>
        public SceneryModels With(CataloguedModel[] models) =>
            new SceneryModels
            {
                rimProps = rimProps,
                camp = camp,
                groves = groves,
                peaks = peaks,
                clouds = clouds,
                surface = surface,
                catalogue = models ?? Array.Empty<CataloguedModel>(),
            };

        /// <summary>The material every piece of scenery is drawn with.</summary>
        public Material Surface => surface;

        /// <summary>Every named model this set can draw.</summary>
        public IReadOnlyList<CataloguedModel> Catalogue => catalogue;

        /// <summary>
        /// True when there is a material and at least one model to draw with it.
        /// A set that is not usable draws nothing, which is a bare board.
        /// </summary>
        /// <remarks>
        /// A catalogue on its own counts, because a board whose every piece was
        /// placed by hand out of the imported art has no family models at all
        /// and is still a dressed board.
        /// </remarks>
        public bool IsUsable =>
            (surface != null
                && (rimProps.Length > 0
                    || camp.Length > 0
                    || groves.Length > 0
                    || peaks.Length > 0
                    || clouds.Length > 0))
            || catalogue.Length > 0;

        /// <summary>
        /// The catalogued model of a given name, or an entry with no mesh where
        /// nothing is bound to that name.
        /// </summary>
        /// <remarks>
        /// A miss draws nothing rather than throwing, for the reason the whole
        /// class is tolerant: scenery carries no information, and a checkout
        /// part-way through an import should show a thinner board rather than a
        /// stack trace. The bake is where a name that binds to nothing is
        /// reported, because that is where somebody is standing.
        /// </remarks>
        public CataloguedModel Named(string name)
        {
            foreach (CataloguedModel model in catalogue)
            {
                if (string.Equals(model.Name, name, StringComparison.Ordinal))
                {
                    return model;
                }
            }

            return default;
        }

        /// <summary>How many models stand behind one group.</summary>
        public int CountOf(SceneryGroup group) => ListFor(group).Length;

        /// <summary>
        /// One model of a group, by a variant number of any size. Null where the
        /// group is empty, which the floor reads as nothing to draw.
        /// </summary>
        public Mesh MeshFor(SceneryGroup group, int variant)
        {
            Mesh[] list = ListFor(group);

            if (list.Length == 0)
            {
                return null;
            }

            // Non-negative regardless of what arrived, so a variant that came
            // from a hash cannot index backwards off the front of the list.
            int index = (int)((uint)variant % (uint)list.Length);

            return list[index];
        }

        private Mesh[] ListFor(SceneryGroup group) =>
            group switch
            {
                SceneryGroup.RimProp => rimProps,
                SceneryGroup.Camp => camp,
                SceneryGroup.Grove => groves,
                SceneryGroup.Peak => peaks,
                SceneryGroup.Cloud => clouds,
                _ => Array.Empty<Mesh>(),
            };
    }
}
