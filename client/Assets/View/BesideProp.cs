using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// A prop that stands on the ground beside a tower instead of being held by
    /// it: which model, how big it is drawn, and how far from the tower's own
    /// root it stands.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It hangs off the tower root and not off a bone.</b> The two hand
    /// sockets put a mesh in a fist, at the fist's own scale, turning with the
    /// arm. A turret, a statue, a font and a tree are none of those things —
    /// they are on the floor — so what positions them is an offset from the
    /// root and not a bone name.
    /// </para>
    /// <para>
    /// <b>The scale is per prop because the packs are per pack.</b> A hand prop
    /// is authored beside the character that holds it and comes in at 1; a
    /// Forest Nature tree is authored for a forest and comes in taller than the
    /// tower it is meant to stand beside. This is a view fact and lives here
    /// under ADR-0007, never in <c>content/units.txt</c> — a column there would
    /// make an art tweak a format version and retire every stored match.
    /// </para>
    /// <para>
    /// <b>The prop wears its own pack's atlas and nothing paints over it.</b>
    /// <see cref="DrawnModel.Wear"/> is called on the bare body, so a row's own
    /// colour never reaches out here. It has to be that way round: a
    /// <c>Cleric_Font</c> drawn against a cleric's character sheet is confetti
    /// rather than a slightly-wrong font.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct BesideProp
    {
        [SerializeField]
        [Tooltip("Stood on the ground beside the tower. Null for a tower with nothing beside it.")]
        private GameObject model;

        [SerializeField]
        [Tooltip("Multiplied into the imported prop's own scale, as UnitArt.Scale is for the body.")]
        private float scale;

        [SerializeField]
        [Tooltip("Metres from the tower's root, in the frame it rests in.")]
        private Vector3 offset;

        /// <summary>
        /// One tile to the tower's right, in the frame the tower rests in.
        /// </summary>
        /// <remarks>
        /// The board's own column pitch, so "the tile beside him" is the
        /// distance between two tiles rather than a number somebody liked the
        /// look of. Sideways rather than forward because a tower rests facing
        /// the corridor, and the tile in front of it is the one creeps walk
        /// through.
        /// </remarks>
        public static readonly Vector3 NextTile = new Vector3(HexGeometry.ColumnPitch, 0f, 0f);

        /// <summary>A prop at an offset of its own.</summary>
        public static BesideProp Standing(GameObject model, float scale, Vector3 offset) =>
            new BesideProp { model = model, scale = scale, offset = offset };

        /// <summary>A prop on <see cref="NextTile"/>, which is where all four signed ones stand.</summary>
        public static BesideProp OnTheNextTile(GameObject model, float scale) =>
            Standing(model, scale, NextTile);

        /// <summary>The prop, or null when nothing stands beside this row.</summary>
        public GameObject Model => model;

        /// <summary>How much bigger or smaller than the imported prop this draws.</summary>
        public float Scale => scale;

        /// <summary>Where it stands, relative to the tower's root and resting facing.</summary>
        public Vector3 Offset => offset;

        /// <summary>True when this names a prop at all.</summary>
        public bool IsSet => model != null;
    }
}
