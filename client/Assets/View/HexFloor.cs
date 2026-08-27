using System.Globalization;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The playfield you can look at: one tile per cell of the map grid, a piece
    /// of road along the corridor and ground everywhere else, each standing at
    /// the tier the map gives it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The renderer walks the grid, and that is the whole of it.</b> Every
    /// cell gets exactly one tile; which model, and how far it is turned, comes
    /// from <see cref="RoadTiling"/>; where it stands comes from
    /// <see cref="HexGeometry"/>. No decoration, no variation, no special case
    /// for the ends of the corridor — anything of that sort would be a second
    /// place the map is interpreted, and the point of this class is that there
    /// is not one. The choosing lives in <see cref="RoadTiling"/> rather than
    /// here precisely so that it can be tested without a scene.
    /// </para>
    /// <para>
    /// <b>Height is drawn, and it is not decoration.</b> A tier is worth half a
    /// hex of reach in the simulation, so a player who cannot see which tier a
    /// cell is on cannot read the range of a tower placed there. The floor
    /// lifts each tile by <see cref="HexGeometry.LevelStep"/> per tier and
    /// reports a bounding box that includes the climb, so the camera frames a
    /// board with tiers as a board with tiers.
    /// </para>
    /// <para>
    /// <b>The map arrives parsed.</b> This class never opens a file and never
    /// reads a character grid: it is handed a <see cref="HexMap"/> that the
    /// simulation's own parser produced, corridor assertion and all. A view-side
    /// reader would be a second opinion about what the map says, and the
    /// interesting maps are exactly the ones the two would disagree about.
    /// </para>
    /// </remarks>
    public sealed class HexFloor : MonoBehaviour
    {
        private MeshRenderer[] _tiles;

        private TilePiece[] _pieces;

        /// <summary>The map this floor was drawn from.</summary>
        public HexMap Map { get; private set; }

        /// <summary>The tile renderers, in row-major order — the grid's order.</summary>
        public MeshRenderer[] Tiles => _tiles;

        /// <summary>How many tiles there are. Always <c>width * height</c>.</summary>
        public int TileCount => _tiles.Length;

        /// <summary>The material a corridor cell is drawn with.</summary>
        public Material RoadMaterial { get; private set; }

        /// <summary>The material every other cell is drawn with.</summary>
        public Material GrassMaterial { get; private set; }

        /// <summary>
        /// The floor's extent in world space, taken from where the tiles
        /// actually are. The camera frames this, so a bug in
        /// <see cref="HexGeometry"/> shows up as a badly framed shot rather than
        /// as a number nobody checks.
        /// </summary>
        public Bounds WorldBounds { get; private set; }

        /// <summary>
        /// Draws a floor under <paramref name="parent"/>. The floor is a child
        /// of the one root object, like everything else, so the scene's root
        /// count does not depend on how big the map is.
        /// </summary>
        public static HexFloor Build(Transform parent, HexMap map, TileSet tiles)
        {
            var host = new GameObject("Floor");
            host.transform.SetParent(parent, worldPositionStays: false);

            var floor = host.AddComponent<HexFloor>();
            floor.Draw(map, tiles);

            return floor;
        }

        /// <summary>The tile at a column and row of the authored grid.</summary>
        public MeshRenderer TileAt(int column, int row) => _tiles[(row * Map.Width) + column];

        /// <summary>
        /// Which piece was drawn at a cell. Asked of the renderer rather than
        /// recomputed, so a test can catch the floor disagreeing with the grid
        /// it was drawn from.
        /// </summary>
        public TilePiece PieceAt(int column, int row) => _pieces[(row * Map.Width) + column];

        /// <summary>
        /// True if the tile at this cell is drawn as road.
        /// </summary>
        /// <remarks>
        /// Read off the piece rather than off the material, because a set of
        /// real tiles wears one atlas everywhere and the material stopped being
        /// able to tell road from ground the moment the blockout did.
        /// </remarks>
        public bool IsRoadTile(int column, int row) => PieceAt(column, row) != TilePiece.Ground;

        private void Draw(HexMap map, TileSet tiles)
        {
            Map = map;
            RoadMaterial = tiles.RoadMaterial;
            GrassMaterial = tiles.GrassMaterial;
            _tiles = new MeshRenderer[map.Width * map.Height];
            _pieces = new TilePiece[map.Width * map.Height];

            var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    TileChoice choice = RoadTiling.For(map, column, row);
                    Vector3 centre = HexGeometry.ToWorld(column, row, map.LevelAt(column, row));
                    MapCell cell = map.CellAt(column, row);

                    var cellObject = new GameObject(Name(column, row, cell));
                    cellObject.transform.SetParent(transform, worldPositionStays: false);
                    cellObject.transform.localPosition = centre + (Vector3.up * TileSet.FaceOffset);

                    // A sixth of a turn per step, and negative because Unity
                    // turns clockwise seen from above while the simulation
                    // counts its six directions the other way.
                    cellObject.transform.localRotation =
                        Quaternion.Euler(0f, -60f * choice.Rotation, 0f);

                    cellObject.AddComponent<MeshFilter>().sharedMesh = tiles.MeshFor(choice.Piece);

                    var renderer = cellObject.AddComponent<MeshRenderer>();

                    // Real shadows, cast and received by real geometry. Nothing
                    // in this project is allowed a painted-on one.
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.sharedMaterial = tiles.MaterialFor(choice.Piece);

                    _tiles[(row * map.Width) + column] = renderer;
                    _pieces[(row * map.Width) + column] = choice.Piece;

                    min = Vector3.Min(min, centre - HalfTile);
                    max = Vector3.Max(max, centre + HalfTile);
                }
            }

            WorldBounds = new Bounds((min + max) * 0.5f, max - min);
        }

        /// <summary>
        /// Half a tile, in all three axes. The Y term is what stops a board
        /// with tiers reporting a flat bounding box and being framed as though
        /// it had none.
        /// </summary>
        private static Vector3 HalfTile =>
            new Vector3(
                HexGeometry.AcrossFlats * 0.5f,
                HexGeometry.LevelStep * 0.5f,
                HexGeometry.PointToPoint * 0.5f);

        /// <summary>
        /// A name that says where the tile is and what it is, so a human
        /// clicking around the hierarchy can check the floor against the map
        /// file without counting.
        /// </summary>
        private static string Name(int column, int row, MapCell cell) =>
            "Cell "
            + column.ToString(CultureInfo.InvariantCulture)
            + ","
            + row.ToString(CultureInfo.InvariantCulture)
            + " "
            + cell;
    }
}
