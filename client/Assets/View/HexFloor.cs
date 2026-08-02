using System.Globalization;
using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The playfield you can look at: one tile per cell of the map grid, road on
    /// the corridor and grass everywhere else.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The renderer walks the grid, and that is the whole of it.</b> There is
    /// no rule here to get wrong: every cell gets exactly one tile, the tile's
    /// material is decided by the cell's own kind, and its position comes from
    /// <see cref="HexGeometry"/>. No decoration, no variation, no special case
    /// for the ends of the corridor — anything of that sort would be a second
    /// place the map is interpreted, and the point of this class is that there
    /// is not one.
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
        public static HexFloor Build(Transform parent, HexMap map, Mesh tile, Material road, Material grass)
        {
            var host = new GameObject("Floor");
            host.transform.SetParent(parent, worldPositionStays: false);

            var floor = host.AddComponent<HexFloor>();
            floor.Draw(map, tile, road, grass);

            return floor;
        }

        /// <summary>The tile at a column and row of the authored grid.</summary>
        public MeshRenderer TileAt(int column, int row) => _tiles[(row * Map.Width) + column];

        /// <summary>
        /// True if the tile at this cell is drawn as road. Asked of the
        /// renderer rather than of the map, so a test can catch the floor
        /// disagreeing with the grid it was drawn from.
        /// </summary>
        public bool IsRoadTile(int column, int row) => TileAt(column, row).sharedMaterial == RoadMaterial;

        private void Draw(HexMap map, Mesh tile, Material road, Material grass)
        {
            Map = map;
            RoadMaterial = road;
            GrassMaterial = grass;
            _tiles = new MeshRenderer[map.Width * map.Height];

            var min = new Vector3(float.MaxValue, 0f, float.MaxValue);
            var max = new Vector3(float.MinValue, 0f, float.MinValue);

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    Vector3 centre = HexGeometry.ToWorld(column, row);
                    MapCell cell = map.CellAt(column, row);

                    var cellObject = new GameObject(Name(column, row, cell));
                    cellObject.transform.SetParent(transform, worldPositionStays: false);
                    cellObject.transform.localPosition = centre;

                    cellObject.AddComponent<MeshFilter>().sharedMesh = tile;

                    var renderer = cellObject.AddComponent<MeshRenderer>();

                    // Real shadows, cast and received by real geometry. Nothing
                    // in this project is allowed a painted-on one.
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.sharedMaterial = cell == MapCell.Ground ? grass : road;

                    _tiles[(row * map.Width) + column] = renderer;

                    min = Vector3.Min(min, centre - HalfTile);
                    max = Vector3.Max(max, centre + HalfTile);
                }
            }

            WorldBounds = new Bounds((min + max) * 0.5f, max - min);
        }

        private static Vector3 HalfTile =>
            new Vector3(HexGeometry.AcrossFlats * 0.5f, 0f, HexGeometry.PointToPoint * 0.5f);

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
