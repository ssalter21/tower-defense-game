using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The six models a floor is built from, and the material they wear.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One shape of tile set, two fillings.</b> Either it holds imported
    /// models — a straight, a curve, a hairpin, a dead end, a ramp and plain
    /// ground — or it holds the generated blockout six times over. Everything
    /// downstream asks the same two questions of both, so
    /// <see cref="HexFloor"/> has one code path and no opinion about whether
    /// the art has arrived.
    /// </para>
    /// <para>
    /// <b>Rotating the blockout is a no-op, which is what makes the single path
    /// honest.</b> A regular hexagon is unchanged by a sixth of a turn, so the
    /// floor may apply <see cref="TileChoice.Rotation"/> unconditionally: it
    /// orients a road piece and does nothing at all to a blockout. There is no
    /// branch to get wrong because there is nothing to branch on.
    /// </para>
    /// <para>
    /// <b>The models carry no material of their own.</b> The meshes are pulled
    /// off the imported prefabs and drawn with a material this class hands out,
    /// so the atlas is bound in one place rather than in six importer settings.
    /// See ADR-0024: art reaches the view as serialized references.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class TileSet
    {
        [SerializeField]
        [Tooltip("Plain ground, no path. KayKit hex_grass.")]
        private Mesh ground;

        [SerializeField]
        [Tooltip("Corridor across opposite edges. KayKit hex_road_A.")]
        private Mesh straight;

        [SerializeField]
        [Tooltip("Corridor turning 120 degrees. KayKit hex_road_B.")]
        private Mesh curve;

        [SerializeField]
        [Tooltip("Corridor turning 60 degrees. KayKit hex_road_C.")]
        private Mesh hairpin;

        [SerializeField]
        [Tooltip("Corridor with one neighbour: the spawn and the exit. KayKit hex_road_M.")]
        private Mesh deadEnd;

        [SerializeField]
        [Tooltip("A straight climbing one tier. KayKit hex_road_A_sloped_high.")]
        private Mesh straightRamp;

        [SerializeField]
        [Tooltip("Drawn on every tile of an imported set. The pack's atlas.")]
        private Material surface;

        [SerializeField]
        [Tooltip("Drawn on corridor cells when this set is the generated blockout.")]
        private Material road;

        [SerializeField]
        [Tooltip("Drawn on other cells when this set is the generated blockout.")]
        private Material grass;

        /// <summary>
        /// A set that draws every cell with the generated hexagon, telling road
        /// from ground by colour alone. What the project falls back to when no
        /// tile models are wired, and what
        /// <see cref="HexTileMesh"/> exists for.
        /// </summary>
        public static TileSet Blockout(Mesh generated, Material roadMaterial, Material grassMaterial) =>
            new TileSet
            {
                ground = generated,
                straight = generated,
                curve = generated,
                hairpin = generated,
                deadEnd = generated,
                straightRamp = generated,
                road = roadMaterial,
                grass = grassMaterial,
            };

        /// <summary>
        /// True when every model and the surface material are present, which is
        /// what makes this a set of real tiles rather than a half-filled one.
        /// </summary>
        /// <remarks>
        /// A half-filled set is refused rather than patched, because a floor
        /// with five real tiles and one missing draws a hole exactly where the
        /// path bends and looks like a map bug.
        /// </remarks>
        public bool IsComplete =>
            ground != null
            && straight != null
            && curve != null
            && hairpin != null
            && deadEnd != null
            && straightRamp != null
            && surface != null;

        /// <summary>The model for a piece.</summary>
        public Mesh MeshFor(TilePiece piece) =>
            piece switch
            {
                TilePiece.Ground => ground,
                TilePiece.Straight => straight,
                TilePiece.Curve => curve,
                TilePiece.Hairpin => hairpin,
                TilePiece.DeadEnd => deadEnd,
                TilePiece.StraightRamp => straightRamp,
                _ => throw new ArgumentOutOfRangeException(nameof(piece), piece, "No tile for this piece."),
            };

        /// <summary>
        /// The material for a piece. One atlas for an imported set; road or
        /// grass for the blockout, which has no texture to tell them apart with.
        /// </summary>
        public Material MaterialFor(TilePiece piece)
        {
            if (surface != null)
            {
                return surface;
            }

            return piece == TilePiece.Ground ? grass : road;
        }

        /// <summary>
        /// The material corridor cells are drawn with, for a test that wants to
        /// name it. The same object as <see cref="MaterialFor"/> returns.
        /// </summary>
        public Material RoadMaterial => surface != null ? surface : road;

        /// <summary>The material other cells are drawn with.</summary>
        public Material GrassMaterial => surface != null ? surface : grass;

        /// <summary>
        /// How far a tile of this set must be lifted so that its walkable face
        /// lands on the tier, in metres.
        /// </summary>
        /// <remarks>
        /// Zero for both sets as they stand — the blockout is a flat fan at
        /// <c>y = 0</c> and the imported tiles are authored with their top face
        /// at <c>y = 0</c> too. It is written down rather than assumed because
        /// the two agreeing is a fact about the pack, not a rule, and a pack
        /// that put its origin at the tile's base would otherwise sink the whole
        /// board by a metre with nothing to say why.
        /// </remarks>
        public const float FaceOffset = 0f;
    }
}
