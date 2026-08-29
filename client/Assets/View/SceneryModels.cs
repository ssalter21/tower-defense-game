using System;
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
        [Tooltip("Low mounds, sat on the lip where the ground drops a level.")]
        private Mesh[] hills = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Clouds, above the board.")]
        private Mesh[] clouds = Array.Empty<Mesh>();

        [SerializeField]
        [Tooltip("Drawn on every piece of scenery. The pack's atlas, the same one the tiles wear.")]
        private Material surface;

        /// <summary>A set built in code rather than deserialized, for the editor tools.</summary>
        public static SceneryModels Of(
            Mesh[] rimProps,
            Mesh[] camp,
            Mesh[] groves,
            Mesh[] peaks,
            Mesh[] hills,
            Mesh[] clouds,
            Material surface) =>
            new SceneryModels
            {
                rimProps = rimProps ?? Array.Empty<Mesh>(),
                camp = camp ?? Array.Empty<Mesh>(),
                groves = groves ?? Array.Empty<Mesh>(),
                peaks = peaks ?? Array.Empty<Mesh>(),
                hills = hills ?? Array.Empty<Mesh>(),
                clouds = clouds ?? Array.Empty<Mesh>(),
                surface = surface,
            };

        /// <summary>The material every piece of scenery is drawn with.</summary>
        public Material Surface => surface;

        /// <summary>
        /// True when there is a material and at least one model to draw with it.
        /// A set that is not usable draws nothing, which is a bare board.
        /// </summary>
        public bool IsUsable =>
            surface != null
            && (rimProps.Length > 0
                || camp.Length > 0
                || groves.Length > 0
                || peaks.Length > 0
                || hills.Length > 0
                || clouds.Length > 0);

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
                SceneryGroup.Hill => hills,
                SceneryGroup.Cloud => clouds,
                _ => Array.Empty<Mesh>(),
            };
    }
}
