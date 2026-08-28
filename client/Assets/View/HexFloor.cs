using System.Collections.Generic;
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
    /// <see cref="HexGeometry"/>. No variation and no special case for the ends
    /// of the corridor — anything of that sort would be a second place the map
    /// is interpreted, and the point of this class is that there is not one. The
    /// choosing lives in <see cref="RoadTiling"/> rather than here precisely so
    /// that it can be tested without a scene.
    /// </para>
    /// <para>
    /// <b>Scenery is drawn here and decided elsewhere, for the same reason.</b>
    /// <see cref="BoardScenery"/> says what stands where; this class puts it
    /// there and keeps one host per cell, so that <see cref="ShowScenery"/> can
    /// clear a hex the moment a tower takes it. A floor with no scenery models
    /// wired draws a bare board rather than failing, because scenery is the one
    /// thing on the floor that carries no information about the match.
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

        private GameObject[] _scenery;

        private Transform _sky;

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
        public static HexFloor Build(
            Transform parent,
            HexMap map,
            TileSet tiles,
            SceneryModels scenery = null,
            DressingSettings settings = null,
            BoardDressing dressing = null)
        {
            var host = new GameObject("Floor");
            host.transform.SetParent(parent, worldPositionStays: false);

            var floor = host.AddComponent<HexFloor>();
            floor.Draw(map, tiles);
            floor.Scatter(map, scenery, settings, dressing);

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

        /// <summary>True if anything stands on this cell that a tower would displace.</summary>
        public bool HasSceneryAt(int column, int row) => SceneryAt(column, row) != null;

        /// <summary>
        /// The scenery standing on one cell, or null. Null for every cell of a
        /// board drawn without models.
        /// </summary>
        public GameObject SceneryAt(int column, int row) =>
            _scenery == null ? null : _scenery[(row * Map.Width) + column];

        /// <summary>
        /// Shows or hides the scenery on one cell.
        /// </summary>
        /// <remarks>
        /// <b>Hidden rather than destroyed, because a tower can be taken back.</b>
        /// The build phase adds and removes towers freely, and a felled grove
        /// that could not grow back would make the board's dressing depend on
        /// the order somebody tried placements in. A cell with nothing on it is
        /// a silent no-op, so a caller may tell the floor about every tower it
        /// has without first asking what is there.
        /// </remarks>
        public void ShowScenery(int column, int row, bool shown)
        {
            GameObject standing = SceneryAt(column, row);

            if (standing != null && standing.activeSelf != shown)
            {
                standing.SetActive(shown);
            }
        }

        /// <summary>
        /// Hides the scenery under every cell named and shows it everywhere
        /// else. What a caller holding a set of towers wants, rather than a call
        /// per cell and a memory of last time.
        /// </summary>
        public void ClearSceneryUnder(IEnumerable<(int Column, int Row)> occupied)
        {
            if (_scenery == null)
            {
                return;
            }

            for (int index = 0; index < _scenery.Length; index++)
            {
                if (_scenery[index] != null && !_scenery[index].activeSelf)
                {
                    _scenery[index].SetActive(true);
                }
            }

            if (occupied == null)
            {
                return;
            }

            foreach ((int column, int row) in occupied)
            {
                if (column >= 0 && column < Map.Width && row >= 0 && row < Map.Height)
                {
                    ShowScenery(column, row, false);
                }
            }
        }

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
        /// Stands the board's scenery on the tiles. Nothing at all when no
        /// models were wired, which is what a checkout without the art draws.
        /// </summary>
        private void Scatter(
            HexMap map, SceneryModels models, DressingSettings settings, BoardDressing dressing)
        {
            if (models == null || !models.IsUsable)
            {
                return;
            }

            _scenery = new GameObject[map.Width * map.Height];

            foreach (SceneryPlacement placement in BoardScenery.For(map, settings, dressing))
            {
                Mesh mesh = models.MeshFor(placement.Group, placement.Variant);

                if (mesh == null)
                {
                    continue;
                }

                Transform host = placement.Group == SceneryGroup.Cloud
                    ? Sky()
                    : SceneryHost(map, placement.Column, placement.Row);

                var piece = new GameObject(placement.Group + " " + mesh.name);
                piece.transform.SetParent(host, worldPositionStays: false);
                piece.AddComponent<ScenerySignature>().Wrote(placement.Group, placement.Variant);
                piece.transform.localPosition =
                    new Vector3(placement.OffsetX, placement.OffsetY, placement.OffsetZ);
                piece.transform.localRotation = Quaternion.Euler(0f, placement.Turn, 0f);
                piece.transform.localScale = Vector3.one * placement.Scale;

                piece.AddComponent<MeshFilter>().sharedMesh = mesh;

                var renderer = piece.AddComponent<MeshRenderer>();

                // A cloud's shadow crossing the board is the whole reason to
                // have a cloud; everything else casts because everything in this
                // project does.
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.sharedMaterial = models.Surface;
            }
        }

        /// <summary>
        /// The object one cell's scenery hangs off, made on first use and
        /// standing at the cell's centre, so that hiding a hex is one call and
        /// the pieces on it keep their offsets.
        /// </summary>
        private Transform SceneryHost(HexMap map, int column, int row)
        {
            int index = (row * map.Width) + column;

            if (_scenery[index] == null)
            {
                var host = new GameObject("Scenery " + column + "," + row);
                host.transform.SetParent(transform, worldPositionStays: false);
                host.transform.localPosition =
                    HexGeometry.ToWorld(column, row, map.LevelAt(column, row));

                _scenery[index] = host;
            }

            return _scenery[index].transform;
        }

        /// <summary>Where the clouds hang. Not a cell, so nothing can clear it.</summary>
        private Transform Sky()
        {
            if (_sky == null)
            {
                var host = new GameObject("Sky");
                host.transform.SetParent(transform, worldPositionStays: false);
                _sky = host.transform;
            }

            return _sky;
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
