using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The floor: one tile per map-grid cell, road on the corridor and grass
    /// everywhere else, with the hex dimensions measured off the mesh rather
    /// than asserted about a constant.
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
        public void ThereIsExactlyOneTilePerGridCell()
        {
            MatchRoot root = BuildPlayfield();
            HexMap map = root.Map;

            Assert.That(root.Floor.TileCount, Is.EqualTo(map.Width * map.Height));
            Assert.That(root.Floor.transform.childCount, Is.EqualTo(map.Width * map.Height));

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    MeshRenderer tile = root.Floor.TileAt(column, row);

                    Assert.That(tile, Is.Not.Null, "no tile at " + column + "," + row);
                    Assert.That(
                        tile.transform.position,
                        Is.EqualTo(HexGeometry.ToWorld(column, row, map.LevelAt(column, row)))
                            .Using(new VectorComparer(Tolerance)),
                        "tile at " + column + "," + row + " is in the wrong place");
                }
            }
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
        /// No decoration. Every tile is a mesh filter and a mesh renderer and
        /// nothing else, because the moment a tile can carry something extra
        /// there is a rule about when it should, and this renderer is supposed
        /// to have no rules in it at all.
        /// </summary>
        [Test]
        public void ATileIsAMeshAndNothingElse()
        {
            MatchRoot root = BuildPlayfield();

            foreach (MeshRenderer tile in root.Floor.Tiles)
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
