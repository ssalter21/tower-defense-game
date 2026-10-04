using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The floor: a piece per corridor cell and one skin of ground between
    /// them, with the hex dimensions measured off the mesh rather than
    /// asserted about a constant.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Measured from the mesh on purpose.</b> A test that checks
    /// <c>HexGeometry.AcrossFlats == 2.0f</c> checks that a constant equals
    /// itself. What matters is that the thing actually drawn is two metres
    /// across the flats and that consecutive rows really are 1.732 apart, so
    /// these read the vertices and the world positions. That is also what makes
    /// them survive the swap: when the generated blockout is replaced by an
    /// imported tile model, this file is what says the grid did not move.
    /// </para>
    /// </remarks>
    public class HexFloorTests
    {
        private const float Tolerance = 0.001f;

        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private MatchRoot BuildPlayfield()
        {
            _root = new GameObject(SceneFraming.RootObjectName);

            return _root.AddComponent<MatchRoot>();
        }

        // -----------------------------------------------------------------
        // The mesh
        // -----------------------------------------------------------------

        [Test]
        public void TheTileIsAPointyTopHexTwoMetresAcrossTheFlats()
        {
            Mesh tile = HexTileMesh.Create();

            try
            {
                Vector3[] corners = tile.vertices.Skip(1).ToArray();

                Assert.That(tile.vertices.Length, Is.EqualTo(7), "Six corners and a centre.");
                Assert.That(tile.triangles.Length, Is.EqualTo(18), "Six triangles in a fan.");

                // Across the flats is the X extent: for a pointy-top hex the two
                // vertical sides are flat, and they are what the width is
                // measured between.
                Assert.That(tile.bounds.size.x, Is.EqualTo(2.0f).Within(Tolerance), "across the flats");

                // Point to point is the Z extent, and it is the LARGER of the
                // two. A flat-top hex would have these the other way round,
                // which is the whole difference between the two orientations.
                Assert.That(tile.bounds.size.z, Is.EqualTo(2.3094f).Within(Tolerance), "point to point");
                Assert.That(tile.bounds.size.z, Is.GreaterThan(tile.bounds.size.x), "pointy-top, not flat-top");

                // A vertex at the top and one at the bottom -- that is what
                // "pointy-top" means, said as geometry rather than as a word.
                Assert.That(
                    corners.Count(c => Mathf.Abs(c.x) < Tolerance && Mathf.Abs(Mathf.Abs(c.z) - 1.1547f) < Tolerance),
                    Is.EqualTo(2),
                    "one vertex directly above the centre and one directly below");

                // And two corners on each flat side, at plus and minus half the
                // width.
                Assert.That(
                    corners.Count(c => Mathf.Abs(Mathf.Abs(c.x) - 1.0f) < Tolerance),
                    Is.EqualTo(4),
                    "two corners on each of the two flat sides");

                Assert.That(tile.bounds.size.y, Is.EqualTo(0f).Within(Tolerance), "the tile is flat");
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        /// <summary>
        /// Every triangle faces up. A tile whose winding was backwards would be
        /// invisible from above and perfectly visible from below, which reads
        /// as "the floor did not load".
        /// </summary>
        [Test]
        public void EveryTriangleOfTheTileFacesUpwards()
        {
            Mesh tile = HexTileMesh.Create();

            try
            {
                Vector3[] vertices = tile.vertices;
                int[] triangles = tile.triangles;

                for (int index = 0; index < triangles.Length; index += 3)
                {
                    Vector3 a = vertices[triangles[index]];
                    Vector3 b = vertices[triangles[index + 1]];
                    Vector3 c = vertices[triangles[index + 2]];

                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

                    Assert.That(normal.y, Is.GreaterThan(0.99f), "triangle at index " + index + " faces down");
                }

                foreach (Vector3 normal in tile.normals)
                {
                    Assert.That(normal, Is.EqualTo(Vector3.up).Using(new VectorComparer(Tolerance)));
                }
            }
            finally
            {
                Object.DestroyImmediate(tile);
            }
        }

        // -----------------------------------------------------------------
        // Where the tiles go
        // -----------------------------------------------------------------

        [Test]
        public void RowsAreOnePointSevenThreeTwoApartAndColumnsAreTwo()
        {
            Vector3 origin = HexGeometry.ToWorld(0, 0);

            Assert.That(
                Mathf.Abs(HexGeometry.ToWorld(0, 1).z - origin.z),
                Is.EqualTo(1.732f).Within(Tolerance),
                "row pitch");

            Assert.That(
                HexGeometry.ToWorld(1, 0).x - origin.x,
                Is.EqualTo(2.0f).Within(Tolerance),
                "column pitch, which for a pointy-top hex is the width across the flats");

            // Odd rows are the shifted ones -- odd-r, which is the simulation's
            // canonical convention and not a choice made here.
            Assert.That(
                HexGeometry.ToWorld(0, 1).x - origin.x,
                Is.EqualTo(1.0f).Within(Tolerance),
                "odd rows sit half a cell to the right");

            Assert.That(
                HexGeometry.ToWorld(0, 2).x - origin.x,
                Is.EqualTo(0f).Within(Tolerance),
                "even rows do not");
        }

        /// <summary>
        /// The strongest statement the layout can make: all six neighbours of
        /// any hex are exactly one tile-width away. Get the row pitch or the
        /// odd-row shift wrong by any amount and this fails, in a way no single
        /// spacing check does.
        /// </summary>
        [Test]
        public void EveryNeighbourIsExactlyOneTileWidthAway()
        {
            var hex = new Hex(4, -3);
            Vector3 centre = HexGeometry.ToWorld(hex);

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                float distance = Vector3.Distance(centre, HexGeometry.ToWorld(hex.Neighbour(direction)));

                Assert.That(
                    distance,
                    Is.EqualTo(HexGeometry.AcrossFlats).Within(Tolerance),
                    "neighbour " + direction);
            }
        }

        // -----------------------------------------------------------------
        // The floor itself
        // -----------------------------------------------------------------

        [Test]
        public void TheCorridorIsPiecesAndTheGroundIsOneSkin()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;
            int corridor = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    bool isCorridor = map.CellAt(column, row) != MapCell.Ground;
                    MeshRenderer piece = root.Floor.TileAt(column, row);

                    Assert.That(
                        piece != null,
                        Is.EqualTo(isCorridor),
                        "cell " + column + "," + row + " is " + map.CellAt(column, row)
                        + (isCorridor ? " and has no piece standing on it" : " and has a piece standing on it"));

                    if (!isCorridor)
                    {
                        continue;
                    }

                    corridor++;

                    Assert.That(
                        piece.transform.position,
                        Is.EqualTo(HexGeometry.ToWorld(column, row, map.LevelAt(column, row)))
                            .Using(new VectorComparer(Tolerance)),
                        "piece at " + column + "," + row + " is in the wrong place");
                }
            }

            Assert.That(root.Floor.TileCount, Is.EqualTo(corridor));
            Assert.That(root.Floor.Tiles.Count(), Is.EqualTo(corridor));
            Assert.That(root.Floor.Skin, Is.Not.Null, "no skin");
            Assert.That(root.Floor.Contours, Is.Not.Null, "no contours");
            Assert.That(
                root.Floor.transform.childCount,
                Is.EqualTo(corridor + 2),
                "the floor is the corridor's pieces, one skin and one set of contours, and nothing else");
            Assert.That(
                root.Floor.transform.Cast<Transform>().Any(child => child.name.StartsWith("Cliff")),
                Is.False,
                "something is standing under the board");
        }

        [Test]
        public void EveryGroundCellHasItsCentreOnTheSkinAtItsOwnLevel()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;
            HashSet<(int, int, int)> vertices = VerticesOf(root.Floor.Skin);

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (map.CellAt(column, row) != MapCell.Ground)
                    {
                        continue;
                    }

                    Vector3 centre = HexGeometry.ToWorld(column, row, map.LevelAt(column, row));

                    Assert.That(
                        vertices.Contains(Rounded(centre)),
                        Is.True,
                        "the skin has no vertex at the centre of cell " + column + "," + row
                        + " at level " + map.LevelAt(column, row) + ", so the cell does not stand at its own height");
                }
            }
        }

        [Test]
        public void TheSkinMeetsARampAtTheRampsOwnHeightAndNotAtTheCellsLevel()
        {
            HexMap map = StreamingContent.ReadMap();
            Mesh flat = HexTileMesh.Create();
            Mesh halfRamp = Lifted(flat, RoadTiling.RampHighEdge, HexGeometry.LevelStep);
            Mesh ramp = Lifted(flat, RoadTiling.RampHighEdge, 2f * HexGeometry.LevelStep);
            Material surface = ViewMaterials.Create("Tiles", Color.white);

            _root = new GameObject("Ramps");

            HexFloor floor = HexFloor.Build(
                _root.transform, map, TileSet.Of(flat, flat, flat, flat, flat, ramp, halfRamp, flat, surface));

            HashSet<(int, int, int)> vertices = VerticesOf(floor.Skin);
            int ramps = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    TilePiece piece = floor.PieceAt(column, row);

                    if (piece != TilePiece.StraightHalfRamp && piece != TilePiece.StraightRamp)
                    {
                        continue;
                    }

                    ramps++;
                    Transform tile = floor.TileAt(column, row).transform;
                    float level = map.LevelAt(column, row) * HexGeometry.LevelStep;

                    foreach (Vector3 vertex in tile.GetComponent<MeshFilter>().sharedMesh.vertices)
                    {
                        if (vertex.y < Tolerance)
                        {
                            continue;
                        }

                        Vector3 lifted = tile.TransformPoint(vertex);

                        if (!TouchesGround(map, column, row, lifted))
                        {
                            continue;
                        }

                        Assert.That(
                            vertices.Contains(Rounded(lifted)),
                            Is.True,
                            "the skin has no vertex at the high corner of the ramp at " + column + "," + row
                            + ", so the ground does not meet the piece at the piece's own height");

                        Assert.That(
                            vertices.Contains(Rounded(new Vector3(lifted.x, level, lifted.z))),
                            Is.False,
                            "the skin meets the ramp at " + column + "," + row + " at the cell's level, under the ramp's high edge");
                    }
                }
            }

            Assert.That(ramps, Is.GreaterThan(0), "the committed corridor has no ramp on it to meet");
        }

        [Test]
        public void AContourLiesOnEveryEdgeWhereTheLevelChanges()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;
            Mesh ribbons = root.Floor.Contours.GetComponent<MeshFilter>().sharedMesh;

            int edges = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    Hex hex = Hex.FromOddRowOffset(column, row);

                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        Hex.ToOddRowOffset(hex.Neighbour(direction), out int otherColumn, out int otherRow);

                        if (otherColumn < 0 || otherColumn >= map.Width || otherRow < 0 || otherRow >= map.Height)
                        {
                            continue;
                        }

                        if (map.LevelAt(column, row) > map.LevelAt(otherColumn, otherRow))
                        {
                            edges++;
                        }
                    }
                }
            }

            Assert.That(edges, Is.GreaterThan(0), "the committed board has no change of level on it");
            Assert.That(
                ribbons.triangles.Length / 3,
                Is.EqualTo(edges * 4),
                "two ribbons of two triangles per edge between two cells of different level");
            Assert.That(root.Floor.Contours.shadowCastingMode, Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off));
        }

        [Test]
        public void TheFootprintAgreesWithTheFloorThatWasActuallyBuilt()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;

            Bounds built = root.Floor.WorldBounds;
            Rect reckoned = HexGeometry.Footprint(map.Width, map.Height);

            // Two ways of saying where the board stops, and they have to be one
            // answer. The floor measures the cells it placed; the footprint
            // works it out from the map for the things that need the rim
            // without holding a floor -- a match drawn in a fixture, which is
            // most of them. Ground effects are clipped to this, so a
            // disagreement is auras cut in the wrong place, on the board where
            // no test builds a floor.
            Assert.That(
                reckoned.xMin, Is.EqualTo(built.min.x).Within(Tolerance), "left edge");
            Assert.That(
                reckoned.xMax, Is.EqualTo(built.max.x).Within(Tolerance), "right edge");
            Assert.That(
                reckoned.yMin, Is.EqualTo(built.min.z).Within(Tolerance), "near edge");
            Assert.That(
                reckoned.yMax, Is.EqualTo(built.max.z).Within(Tolerance), "far edge");
        }

        [Test]
        public void RoadIsOnTheCorridorAndGrassIsEverywhereElse()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    MapCell cell = map.CellAt(column, row);

                    Assert.That(
                        root.Floor.IsRoadTile(column, row),
                        Is.EqualTo(cell != MapCell.Ground),
                        "cell " + column + "," + row + " is " + cell);
                }
            }

            // The corridor the simulation traced, drawn as road -- so the
            // entrance and the exit are road too, not a third kind of tile.
            foreach (Hex hex in map.Route)
            {
                Hex.ToOddRowOffset(hex, out int column, out int row);

                Assert.That(root.Floor.IsRoadTile(column, row), Is.True, "route hex " + hex + " is not road");
            }
        }

        /// <summary>
        /// No decoration. Every piece is a mesh filter and a mesh renderer and
        /// nothing else, because the moment a piece can carry something extra
        /// there is a rule about when it should, and this renderer is supposed
        /// to have no rules in it at all.
        /// </summary>
        [Test]
        public void ATileIsAMeshAndNothingElse()
        {
            MatchRoot root = BuildPlayfield();

            foreach (MeshRenderer tile in root.Floor.Tiles.Append(root.Floor.Skin).Append(root.Floor.Contours))
            {
                Component[] components = tile.GetComponents<Component>();

                Assert.That(
                    components.Select(c => c.GetType().Name).OrderBy(n => n),
                    Is.EqualTo(new[] { "MeshFilter", "MeshRenderer", "Transform" }),
                    "tile " + tile.name + " carries something extra");

                Assert.That(tile.transform.childCount, Is.EqualTo(0), "tile " + tile.name + " has decoration on it");
            }
        }

        /// <summary>
        /// The map comes from the simulation's parser, and there is no second
        /// reader on the view side.
        /// </summary>
        /// <remarks>
        /// A second parser would be a second opinion about what a map says, and
        /// the maps it matters for are exactly the ones the two would disagree
        /// about — the malformed ones, where the simulation's corridor
        /// assertion is the thing standing between a bad grid and a pathfinder.
        /// Checked by reflection rather than by review: exactly one method in
        /// the whole view assembly produces a <see cref="HexMap"/>, and it is
        /// the one that hands bytes to <c>Sim</c>.
        /// </remarks>
        [Test]
        public void OnlyOneThingInTheViewProducesAMap()
        {
            MethodInfo[] producers = typeof(StreamingContent).Assembly
                .GetTypes()
                .SelectMany(type => type.GetMethods(
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Static | BindingFlags.Instance |
                    BindingFlags.DeclaredOnly))
                .Where(method => method.ReturnType == typeof(HexMap) && !method.IsSpecialName)
                .ToArray();

            Assert.That(
                producers.Select(m => m.DeclaringType.Name + "." + m.Name),
                Is.EqualTo(new[] { nameof(StreamingContent) + "." + nameof(StreamingContent.ReadMap) }),
                "Something in the view is producing a map other than by handing bytes to the "
                + "simulation's parser.");
        }

        private static Mesh Lifted(Mesh tile, int edge, float by)
        {
            Hex origin = Hex.FromOddRowOffset(0, 0);
            Vector3 towards = HexGeometry.ToWorld(origin.Neighbour(edge)) - HexGeometry.ToWorld(origin);
            int[] onTheEdge = Enumerable.Range(0, Hex.DirectionCount)
                .OrderByDescending(corner => Vector3.Dot(HexGeometry.Corner(corner), towards))
                .Take(2)
                .ToArray();

            Vector3[] vertices = tile.vertices;

            foreach (int corner in onTheEdge)
            {
                vertices[corner + 1] += Vector3.up * by;
            }

            var lifted = new Mesh { name = tile.name + " lifted" };
            lifted.vertices = vertices;
            lifted.normals = tile.normals;
            lifted.uv = tile.uv;
            lifted.triangles = tile.triangles;
            lifted.RecalculateBounds();

            return lifted;
        }

        private static HashSet<(int, int, int)> VerticesOf(MeshRenderer renderer)
        {
            Transform at = renderer.transform;
            var found = new HashSet<(int, int, int)>();

            foreach (Vector3 vertex in renderer.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                found.Add(Rounded(at.TransformPoint(vertex)));
            }

            return found;
        }

        private static (int, int, int) Rounded(Vector3 point) =>
            (Mathf.RoundToInt(point.x * 1000f), Mathf.RoundToInt(point.y * 1000f), Mathf.RoundToInt(point.z * 1000f));

        private static bool TouchesGround(HexMap map, int column, int row, Vector3 cornerXZ)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                Hex.ToOddRowOffset(hex.Neighbour(direction), out int otherColumn, out int otherRow);

                if (otherColumn < 0 || otherColumn >= map.Width || otherRow < 0 || otherRow >= map.Height)
                {
                    continue;
                }

                if (map.CellAt(otherColumn, otherRow) != MapCell.Ground)
                {
                    continue;
                }

                Vector3 centre = HexGeometry.ToWorld(otherColumn, otherRow);

                for (int corner = 0; corner < Hex.DirectionCount; corner++)
                {
                    Vector3 theirs = centre + HexGeometry.Corner(corner);

                    if (Mathf.Abs(theirs.x - cornerXZ.x) < Tolerance && Mathf.Abs(theirs.z - cornerXZ.z) < Tolerance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Component-wise vector comparison, because NUnit's default is exact.</summary>
        private sealed class VectorComparer : System.Collections.Generic.IEqualityComparer<Vector3>
        {
            private readonly float _tolerance;

            internal VectorComparer(float tolerance) => _tolerance = tolerance;

            public bool Equals(Vector3 a, Vector3 b) =>
                Mathf.Abs(a.x - b.x) < _tolerance
                && Mathf.Abs(a.y - b.y) < _tolerance
                && Mathf.Abs(a.z - b.z) < _tolerance;

            public int GetHashCode(Vector3 value) => value.GetHashCode();
        }
    }
}
