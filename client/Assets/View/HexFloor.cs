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
    /// <b>Height is drawn, and it is not decoration.</b> A level is worth a
    /// quarter of a hex of reach in the simulation, so a player who cannot see
    /// which level a cell is on cannot read the range of a tower placed there.
    /// The floor lifts each tile by <see cref="HexGeometry.LevelStep"/> per
    /// level and reports a bounding box that includes the climb, so the camera
    /// frames a board with relief as a board with relief.
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
            floor.Draw(map, tiles, settings);
            floor.Underpin(map, tiles, settings);
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
        /// <para>
        /// Read off the piece rather than off the material, because a set of
        /// real tiles wears one atlas everywhere and the material stopped being
        /// able to tell road from ground the moment the blockout did.
        /// </para>
        /// <para>
        /// <b>And read off the edge table rather than by comparing against
        /// <see cref="TilePiece.Ground"/>, which is what it used to do.</b> A
        /// piece has road on it exactly when its road meets an edge, and that
        /// table is already the one place the answer lives. The comparison was
        /// right for as long as <c>Ground</c> was the only pathless piece; the
        /// grass slopes made it wrong, and every ground cell on a hillside
        /// started reporting itself as corridor.
        /// </para>
        /// </remarks>
        public bool IsRoadTile(int column, int row) => RoadTiling.EdgesOf(PieceAt(column, row)) != 0;

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

        private void Draw(HexMap map, TileSet tiles, DressingSettings settings)
        {
            int waterLine = settings?.WaterLevel ?? DressingSettings.Default.WaterLevel;

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
                    int level = map.LevelAt(column, row);

                    // The water line is a dressing decision, so it is applied
                    // here rather than in RoadTiling, which answers only what
                    // the map says. Flat ground under the line goes to water; a
                    // slope keeps its slope, which is what a shore looks like.
                    if (choice.Piece == TilePiece.Ground && level <= waterLine)
                    {
                        choice = new TileChoice(TilePiece.Water, 0);
                    }

                    Vector3 centre = HexGeometry.ToWorld(column, row, level);
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
        /// Stacks bare columns of earth under any tile whose drop to a
        /// neighbour is deeper than the tile's own body, so that a cliff is a
        /// cliff rather than a plate hanging over a hole.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A tile carries one metre of earth and a level is now half of
        /// one.</b> That covers a drop of two levels exactly and anything
        /// shallower with room to spare, which is most of a graded board and
        /// costs nothing. It stops covering at three levels, and the daylight
        /// under the rim is the failure this closes: one more copy of
        /// <c>hex_grass_bottom</c> per further metre, hung straight down from
        /// where the body ran out.
        /// </para>
        /// <para>
        /// <b>The depth is measured against the lowest neighbour and not
        /// against the board.</b> A ridge standing four levels over the valley
        /// on one side and one level over the shelf on the other needs the
        /// column the valley asks for, and running every cell down to the
        /// board's floor instead would bury the whole map in a solid block that
        /// nothing can see into and every shadow lands on.
        /// </para>
        /// <para>
        /// <b>The rim is a separate number.</b> Off the grid there is no
        /// neighbour to measure against, so how far the board's edge falls away
        /// is a decision rather than a consequence, and
        /// <see cref="DressingSettings.RimDrop"/> is where it is made. It is
        /// what makes the board read as a piece of country lifted out of a
        /// landscape rather than as a sheet of tiles.
        /// </para>
        /// <para>
        /// <b>The columns are not tiles.</b> They are not counted in
        /// <see cref="TileCount"/>, never returned by <see cref="TileAt"/> and
        /// never picked, because a thing a player can click is a thing the
        /// simulation has to have an opinion about, and the simulation has no
        /// idea these exist.
        /// </para>
        /// </remarks>
        private void Underpin(HexMap map, TileSet tiles, DressingSettings settings)
        {
            Mesh mesh = tiles.MeshFor(TilePiece.Cliff);
            Material surface = tiles.MaterialFor(TilePiece.Cliff);

            if (mesh == null || surface == null)
            {
                return;
            }

            float rim = settings?.RimDrop ?? DressingSettings.Default.RimDrop;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    int level = map.LevelAt(column, row);
                    float floorHeight = Lowest(map, column, row, level, rim);
                    float covered = (level * HexGeometry.LevelStep) - HexGeometry.TileBody;

                    for (int index = 0; covered > floorHeight; index++)
                    {
                        var post = new GameObject(
                            "Cliff " + column.ToString(CultureInfo.InvariantCulture)
                            + "," + row.ToString(CultureInfo.InvariantCulture)
                            + " -" + index.ToString(CultureInfo.InvariantCulture));

                        post.transform.SetParent(transform, worldPositionStays: false);
                        post.transform.localPosition =
                            HexGeometry.ToWorld(column, row) + (Vector3.up * covered);

                        post.AddComponent<MeshFilter>().sharedMesh = mesh;

                        var renderer = post.AddComponent<MeshRenderer>();
                        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                        renderer.receiveShadows = true;
                        renderer.sharedMaterial = surface;

                        covered -= HexGeometry.TileBody;
                    }
                }
            }
        }

        /// <summary>
        /// How far down the earth under a cell has to reach, in metres: the
        /// face of its lowest neighbour, or <paramref name="rim"/> below the
        /// cell where it is on the board's edge.
        /// </summary>
        /// <remarks>
        /// Adjacency comes from the simulation's own neighbour walk rather than
        /// from an offset table typed out here, for the reason
        /// <see cref="RoadTiling.CorridorEdges"/> gives: a second opinion about
        /// which cells touch is a bug that only shows on the odd rows.
        /// </remarks>
        private static float Lowest(HexMap map, int column, int row, int level, float rim)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            float here = level * HexGeometry.LevelStep;
            float lowest = here;

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex neighbour = hex.Neighbour(direction);
                Hex.ToOddRowOffset(neighbour, out int otherColumn, out int otherRow);

                float face = otherColumn < 0 || otherColumn >= map.Width
                    || otherRow < 0 || otherRow >= map.Height
                    ? here - rim
                    : map.LevelAt(otherColumn, otherRow) * HexGeometry.LevelStep;

                if (face < lowest)
                {
                    lowest = face;
                }
            }

            return lowest;
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
                Stand(placement, models);
            }
        }

        /// <summary>
        /// Draws one piece of scenery and returns it, or null where nothing is
        /// bound to draw it with.
        /// </summary>
        /// <remarks>
        /// <b>Public so the scenery palette can add one without a redraw.</b>
        /// The alternative was for the palette to write the line and re-dress
        /// the whole board, which would be a second path that draws pieces --
        /// and the first thing a second path does is disagree with this one
        /// about a material or a signature.
        /// </remarks>
        public GameObject Stand(SceneryPlacement placement, SceneryModels models)
        {
            if (models == null || _scenery == null)
            {
                return null;
            }

            SceneryModels.CataloguedModel named =
                placement.IsNamed ? models.Named(placement.Model) : default;

            Mesh mesh = placement.IsNamed
                ? named.Mesh
                : models.MeshFor(placement.Group, placement.Variant);

            if (mesh == null)
            {
                return null;
            }

            // A named model wears the atlas of the pack it came out of; only a
            // family piece can assume the board's one surface, because only
            // families are built out of the one pack the tiles come from.
            Material material = placement.IsNamed ? named.Material : models.Surface;

            if (material == null)
            {
                return null;
            }

            Transform host = !placement.IsNamed && placement.Group == SceneryGroup.Cloud
                ? Sky()
                : SceneryHost(Map, placement.Column, placement.Row);

            var piece = new GameObject(
                placement.IsNamed ? placement.Model : placement.Group + " " + mesh.name);
            piece.transform.SetParent(host, worldPositionStays: false);

            var signature = piece.AddComponent<ScenerySignature>();
            signature.Wrote(placement.Group, placement.Variant);

            if (placement.IsNamed)
            {
                signature.WroteNamed(placement.Model);
            }

            piece.transform.localPosition =
                new Vector3(placement.OffsetX, placement.OffsetY, placement.OffsetZ);
            piece.transform.localRotation = Quaternion.Euler(0f, placement.Turn, 0f);
            piece.transform.localScale = Vector3.one * placement.Scale;

            piece.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = piece.AddComponent<MeshRenderer>();

            // A cloud's shadow crossing the board is the whole reason to have a
            // cloud; everything else casts because everything in this project
            // does.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.sharedMaterial = material;

            return piece;
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
