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
    /// them, each standing where the map says, and the board's edge agreeing
    /// with what was built.
    /// </summary>
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
            Assert.That(
                root.Floor.transform.childCount,
                Is.EqualTo(corridor + 1),
                "the floor is the corridor's pieces and one skin, and nothing else");
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
