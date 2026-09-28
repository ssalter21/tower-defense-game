using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Sim;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// PROTOTYPE. The two board-smoothing candidates of issue #329, drawn over
    /// a floor <see cref="HexFloor"/> has already built, plus the two
    /// legibility aids the same sheet asks for. Nothing here ships: the ground
    /// tiles and cliff posts are torn out of a built scene and redrawn, and the
    /// scene is destroyed once its frame is taken.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The skin</b> is one mesh. Every ground cell keeps a flat plateau at
    /// its own height, shrunk to <see cref="SkinInset"/> of the hex, and the
    /// ring between plateaus is planar slope: a rectangle between two
    /// neighbouring plateau edges and a triangle where three plateaus meet.
    /// Roads keep the pack's pieces and the skin runs up to their edges at the
    /// height the piece actually has there. The board's rim banks down by the
    /// dressing's rim drop and hangs a skirt of earth.
    /// </para>
    /// <para>
    /// <b>The pieces</b> keep one tile per cell. A ground cell whose neighbour
    /// stands one level above it lifts the corners it shares with that
    /// neighbour to the neighbour's height, so the slope lives in the lower
    /// cell and the higher cell stays a flat plateau, which is how the pack's
    /// own ramp works. A neighbour two or more levels up is a cliff and the
    /// corners stay put. A cell with nothing lifted wears the pack's flat grass
    /// tile unchanged; every other cell is an authored piece, and the census of
    /// distinct pieces is the candidate's cost.
    /// </para>
    /// <para>
    /// Both wear the grass and earth swatches sampled off the pack's own grass
    /// tile, so their colours come from whatever atlas the board is wearing.
    /// </para>
    /// </remarks>
    internal static class SmoothingCandidates
    {
        /// <summary>How much of a ground cell stays flat under the skin, as a share of the hex.</summary>
        public const float SkinInset = 0.6f;

        /// <summary>How far a contour ribbon floats above the ground, so it is not eaten by it.</summary>
        private const float ContourLift = 0.05f;

        /// <summary>How wide a contour ribbon is, in metres.</summary>
        private const float ContourWidth = 0.1f;

        private const float Epsilon = 0.001f;

        /// <summary>What one candidate cost to draw, for the sheet's index.</summary>
        public sealed class Bill
        {
            public int Objects;
            public int Meshes;
            public int Vertices;
            public int Triangles;
            public int PackPieces;
            public int AuthoredPieces;
            public int DistinctPatterns;
            public int WallsBetweenCells;
            public readonly List<string> Patterns = new List<string>();
        }

        /// <summary>The surface the candidates draw with, sampled off the pack's grass tile.</summary>
        public readonly struct Swatches
        {
            public Swatches(Vector2 grass, Vector2 earth)
            {
                Grass = grass;
                Earth = earth;
            }

            public Vector2 Grass { get; }

            public Vector2 Earth { get; }

            public static Swatches SampledFrom(Mesh grassTile)
            {
                Vector3[] normals = grassTile.normals;
                Vector2[] uvs = grassTile.uv;

                if (normals.Length != uvs.Length || uvs.Length == 0)
                {
                    throw new InvalidOperationException(
                        "The grass tile has no normals or UVs to sample a swatch from.");
                }

                Vector2 grass = Vector2.zero, earth = Vector2.zero;
                int grassCount = 0, earthCount = 0;

                for (int index = 0; index < normals.Length; index++)
                {
                    if (normals[index].y > 0.9f)
                    {
                        grass += uvs[index];
                        grassCount++;
                    }
                    else if (Mathf.Abs(normals[index].y) < 0.1f)
                    {
                        earth += uvs[index];
                        earthCount++;
                    }
                }

                if (grassCount == 0 || earthCount == 0)
                {
                    throw new InvalidOperationException(
                        "The grass tile has no top or no side to sample swatches from.");
                }

                return new Swatches(grass / grassCount, earth / earthCount);
            }
        }

        /// <summary>
        /// The board as the candidates see it: the parsed map, the heights of
        /// the road pieces that stay, and which cells meet at each hex corner.
        /// </summary>
        public sealed class Board
        {
            private readonly Dictionary<long, List<(int Column, int Row)>> _cellsAtCorner =
                new Dictionary<long, List<(int Column, int Row)>>();

            private readonly Dictionary<(int Column, int Row), Transform> _roadTiles =
                new Dictionary<(int Column, int Row), Transform>();

            private readonly (int First, int Second)[] _edgeCorners = new (int, int)[Hex.DirectionCount];

            public Board(HexMap map, HexFloor floor, float rimDrop)
            {
                Map = map;
                RimDrop = rimDrop;
                Floor = floor;

                for (int row = 0; row < map.Height; row++)
                {
                    for (int column = 0; column < map.Width; column++)
                    {
                        for (int corner = 0; corner < Hex.DirectionCount; corner++)
                        {
                            long key = Key(CornerXZ(column, row, corner));

                            if (!_cellsAtCorner.TryGetValue(key, out List<(int, int)> cells))
                            {
                                cells = new List<(int, int)>();
                                _cellsAtCorner[key] = cells;
                            }

                            cells.Add((column, row));
                        }

                        if (IsRoad(column, row))
                        {
                            Transform tile = floor.TileAt(column, row).transform;
                            _roadTiles[(column, row)] = tile;
                        }
                    }
                }

                Hex origin = Hex.FromOddRowOffset(0, 0);
                Vector3 centre = HexGeometry.ToWorld(origin);

                for (int direction = 0; direction < Hex.DirectionCount; direction++)
                {
                    Vector3 towards = HexGeometry.ToWorld(origin.Neighbour(direction)) - centre;

                    int[] ranked = Enumerable.Range(0, Hex.DirectionCount)
                        .OrderByDescending(index => Vector3.Dot(HexGeometry.Corner(index), towards))
                        .ToArray();

                    _edgeCorners[direction] = (ranked[0], ranked[1]);
                }
            }

            public HexMap Map { get; }

            public float RimDrop { get; }

            public HexFloor Floor { get; }

            public IEnumerable<KeyValuePair<long, List<(int Column, int Row)>>> Corners => _cellsAtCorner;

            public bool InBounds(int column, int row) =>
                column >= 0 && column < Map.Width && row >= 0 && row < Map.Height;

            public bool IsRoad(int column, int row) => Map.CellAt(column, row) != MapCell.Ground;

            public float Height(int column, int row) => Map.LevelAt(column, row) * HexGeometry.LevelStep;

            public Vector3 Centre(int column, int row) => HexGeometry.ToWorld(column, row);

            public Vector3 CornerXZ(int column, int row, int corner) =>
                Centre(column, row) + HexGeometry.Corner(corner);

            public (int First, int Second) EdgeCorners(int direction) => _edgeCorners[direction];

            public bool Neighbour(int column, int row, int direction, out int otherColumn, out int otherRow)
            {
                Hex hex = Hex.FromOddRowOffset(column, row);
                Hex.ToOddRowOffset(hex.Neighbour(direction), out otherColumn, out otherRow);

                return InBounds(otherColumn, otherRow);
            }

            public List<(int Column, int Row)> CellsAt(Vector3 cornerXZ) => _cellsAtCorner[Key(cornerXZ)];

            /// <summary>
            /// The height of a road piece's own surface at one of its hex
            /// corners, read off the mesh it wears so a ramp's tilt is honoured.
            /// </summary>
            public float RoadHeightAt(int column, int row, Vector3 cornerXZ)
            {
                Transform tile = _roadTiles[(column, row)];
                Mesh mesh = tile.GetComponent<MeshFilter>().sharedMesh;
                Vector3 local = tile.InverseTransformPoint(new Vector3(cornerXZ.x, 0f, cornerXZ.z));
                float best = float.NegativeInfinity;

                foreach (Vector3 vertex in mesh.vertices)
                {
                    float dx = vertex.x - local.x, dz = vertex.z - local.z;

                    if ((dx * dx) + (dz * dz) < 0.04f && vertex.y > best)
                    {
                        best = vertex.y;
                    }
                }

                return float.IsNegativeInfinity(best)
                    ? Height(column, row)
                    : tile.TransformPoint(new Vector3(local.x, best, local.z)).y;
            }

            /// <summary>The height of a cell's surface at one of its corners, before any smoothing.</summary>
            public float OwnHeightAt(int column, int row, Vector3 cornerXZ) =>
                IsRoad(column, row) ? RoadHeightAt(column, row, cornerXZ) : Height(column, row);

            /// <summary>Mean height of the real cells meeting at a corner.</summary>
            public float MeanAt(Vector3 cornerXZ)
            {
                List<(int Column, int Row)> cells = CellsAt(cornerXZ);
                float sum = 0f;

                foreach ((int column, int row) in cells)
                {
                    sum += OwnHeightAt(column, row, cornerXZ);
                }

                return sum / cells.Count;
            }

            /// <summary>How far the ground falls beyond the board's edge at a rim corner.</summary>
            public float RimHeightAt(Vector3 cornerXZ) => MeanAt(cornerXZ) - RimDrop;

            public bool IsRimCorner(Vector3 cornerXZ) => CellsAt(cornerXZ).Count < 3;

            private static long Key(Vector3 point) =>
                ((long)Mathf.RoundToInt(point.x * 1000f) << 32) ^ (uint)Mathf.RoundToInt(point.z * 1000f);
        }

        /// <summary>
        /// Takes the ground tiles and the cliff posts out of a built floor,
        /// leaving the roads and the scenery. Returns how many posts there were.
        /// </summary>
        public static int StripGround(HexFloor floor)
        {
            var doomed = new List<GameObject>();
            int posts = 0;

            foreach (Transform child in floor.transform)
            {
                if (child.name.StartsWith("Cliff ", StringComparison.Ordinal))
                {
                    posts++;
                    doomed.Add(child.gameObject);
                }
                else if (child.name.StartsWith("Cell ", StringComparison.Ordinal)
                    && child.name.EndsWith(" " + MapCell.Ground, StringComparison.Ordinal))
                {
                    doomed.Add(child.gameObject);
                }
            }

            foreach (GameObject victim in doomed)
            {
                UnityEngine.Object.DestroyImmediate(victim);
            }

            return posts;
        }

        /// <summary>Counts the cliff posts a built floor is standing on.</summary>
        public static int CountPosts(HexFloor floor) =>
            floor.transform.Cast<Transform>().Count(child => child.name.StartsWith("Cliff ", StringComparison.Ordinal));

        /// <summary>Every road tile the floor still draws, with the level it stands at.</summary>
        public static IEnumerable<(Renderer Renderer, int Level)> RoadTiles(Board board)
        {
            for (int row = 0; row < board.Map.Height; row++)
            {
                for (int column = 0; column < board.Map.Width; column++)
                {
                    if (board.IsRoad(column, row))
                    {
                        yield return (board.Floor.TileAt(column, row), board.Map.LevelAt(column, row));
                    }
                }
            }
        }

        /// <summary>The skin's height at a hex corner: the road's own if one meets there, the rim's if the board ends there, the mean otherwise.</summary>
        public static float SkinCornerHeight(Board board, Vector3 cornerXZ)
        {
            foreach ((int column, int row) in board.CellsAt(cornerXZ))
            {
                if (board.IsRoad(column, row))
                {
                    return board.RoadHeightAt(column, row, cornerXZ);
                }
            }

            return board.IsRimCorner(cornerXZ) ? board.RimHeightAt(cornerXZ) : board.MeanAt(cornerXZ);
        }

        /// <summary>
        /// Draws the skin over a floor that <see cref="StripGround"/> has cleared.
        /// One mesh, one submesh per level so a band can tint it.
        /// </summary>
        public static Bill BuildSkin(Board board, Swatches swatches, Material surface, out Renderer skin)
        {
            var mesh = new MeshBuilder();
            HexMap map = board.Map;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (board.IsRoad(column, row))
                    {
                        SkinRoadSkirt(board, mesh, swatches, column, row);
                        continue;
                    }

                    SkinCell(board, mesh, swatches, column, row);
                }
            }

            foreach (KeyValuePair<long, List<(int Column, int Row)>> corner in board.Corners)
            {
                SkinCorner(board, mesh, swatches, corner.Value);
            }

            var host = new GameObject("Skin");
            host.transform.SetParent(board.Floor.transform, worldPositionStays: false);
            host.AddComponent<MeshFilter>().sharedMesh = mesh.Build(HexMap.LevelCount);

            skin = host.AddComponent<MeshRenderer>();
            skin.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            skin.receiveShadows = true;
            skin.sharedMaterials = Enumerable.Repeat(surface, HexMap.LevelCount).ToArray();

            return new Bill
            {
                Objects = 1,
                Meshes = 1,
                Vertices = mesh.VertexCount,
                Triangles = mesh.TriangleCount,
            };
        }

        private static Vector3 Plateau(Board board, int column, int row, Vector3 cornerXZ)
        {
            Vector3 centre = board.Centre(column, row);

            if (board.IsRoad(column, row))
            {
                return WithY(cornerXZ, board.RoadHeightAt(column, row, cornerXZ));
            }

            return WithY(centre + ((cornerXZ - centre) * SkinInset), board.Height(column, row));
        }

        private static void SkinCell(Board board, MeshBuilder mesh, Swatches swatches, int column, int row)
        {
            int level = board.Map.LevelAt(column, row);
            float height = board.Height(column, row);
            Vector3 centre = WithY(board.Centre(column, row), height);

            var plateau = new Vector3[Hex.DirectionCount];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                plateau[corner] = Plateau(board, column, row, board.CornerXZ(column, row, corner));
            }

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                mesh.Triangle(level, centre, plateau[corner], plateau[(corner + 1) % Hex.DirectionCount], swatches.Grass, Vector3.up);
            }

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                (int first, int second) = board.EdgeCorners(direction);
                Vector3 firstXZ = board.CornerXZ(column, row, first);
                Vector3 secondXZ = board.CornerXZ(column, row, second);
                Vector3 outward = firstXZ + secondXZ - (2f * board.Centre(column, row));

                if (board.Neighbour(column, row, direction, out int otherColumn, out int otherRow))
                {
                    Vector3 farFirst = Plateau(board, otherColumn, otherRow, firstXZ);
                    Vector3 farSecond = Plateau(board, otherColumn, otherRow, secondXZ);

                    if (!board.IsRoad(otherColumn, otherRow))
                    {
                        farFirst = (plateau[first] + farFirst) * 0.5f;
                        farSecond = (plateau[second] + farSecond) * 0.5f;
                    }

                    mesh.Quad(level, plateau[first], plateau[second], farSecond, farFirst, swatches.Grass, Vector3.up);
                    continue;
                }

                float rimFirst = board.RimHeightAt(firstXZ);
                float rimSecond = board.RimHeightAt(secondXZ);

                mesh.Quad(
                    level, plateau[first], plateau[second],
                    WithY(secondXZ, rimSecond), WithY(firstXZ, rimFirst),
                    swatches.Earth, Vector3.up);

                mesh.Quad(
                    level, WithY(firstXZ, rimFirst), WithY(secondXZ, rimSecond),
                    WithY(secondXZ, rimSecond - HexGeometry.TileBody), WithY(firstXZ, rimFirst - HexGeometry.TileBody),
                    swatches.Earth, outward);
            }
        }

        private static void SkinRoadSkirt(Board board, MeshBuilder mesh, Swatches swatches, int column, int row)
        {
            int level = board.Map.LevelAt(column, row);

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if (board.Neighbour(column, row, direction, out _, out _))
                {
                    continue;
                }

                (int first, int second) = board.EdgeCorners(direction);
                Vector3 firstXZ = board.CornerXZ(column, row, first);
                Vector3 secondXZ = board.CornerXZ(column, row, second);
                Vector3 outward = firstXZ + secondXZ - (2f * board.Centre(column, row));

                mesh.Quad(
                    level,
                    WithY(firstXZ, board.RoadHeightAt(column, row, firstXZ)),
                    WithY(secondXZ, board.RoadHeightAt(column, row, secondXZ)),
                    WithY(secondXZ, board.RimHeightAt(secondXZ) - HexGeometry.TileBody),
                    WithY(firstXZ, board.RimHeightAt(firstXZ) - HexGeometry.TileBody),
                    swatches.Earth, outward);
            }
        }

        private static void SkinCorner(Board board, MeshBuilder mesh, Swatches swatches, List<(int Column, int Row)> cells)
        {
            Vector3 cornerXZ = CornerOf(board, cells);
            var grounds = cells.Where(cell => !board.IsRoad(cell.Column, cell.Row)).ToList();

            if (grounds.Count == 0)
            {
                return;
            }

            var points = new List<(Vector3 Point, int Level, bool Ground)>();

            foreach ((int column, int row) in cells)
            {
                points.Add((Plateau(board, column, row, cornerXZ), board.Map.LevelAt(column, row), !board.IsRoad(column, row)));
            }

            for (int missing = cells.Count; missing < 3; missing++)
            {
                points.Add((WithY(cornerXZ, board.RimHeightAt(cornerXZ)), -1, false));
            }

            points = points.OrderBy(point => Mathf.Atan2(point.Point.z - cornerXZ.z, point.Point.x - cornerXZ.x)).ToList();

            if (grounds.Count == 3)
            {
                Vector3 hub = WithY(cornerXZ, board.MeanAt(cornerXZ));

                for (int index = 0; index < 3; index++)
                {
                    Vector3 here = points[index].Point;
                    Vector3 toNext = (here + points[(index + 1) % 3].Point) * 0.5f;
                    Vector3 toPrevious = (here + points[(index + 2) % 3].Point) * 0.5f;

                    mesh.Quad(points[index].Level, here, toNext, hub, toPrevious, swatches.Grass, Vector3.up);
                }

                return;
            }

            if (grounds.Count == 2)
            {
                int a = points.FindIndex(point => point.Ground);
                int b = points.FindIndex(a + 1, point => point.Ground);
                int other = 3 - a - b;
                Vector3 middle = (points[a].Point + points[b].Point) * 0.5f;

                mesh.Triangle(points[a].Level, points[a].Point, middle, points[other].Point, SwatchFor(swatches, points[a].Point, middle, points[other].Point), Vector3.up);
                mesh.Triangle(points[b].Level, middle, points[b].Point, points[other].Point, SwatchFor(swatches, middle, points[b].Point, points[other].Point), Vector3.up);

                return;
            }

            int only = points.FindIndex(point => point.Ground);

            mesh.Triangle(
                points[only].Level, points[0].Point, points[1].Point, points[2].Point,
                SwatchFor(swatches, points[0].Point, points[1].Point, points[2].Point),
                points[only].Point - cornerXZ);
        }

        private static Vector3 CornerOf(Board board, List<(int Column, int Row)> cells)
        {
            (int column, int row) = cells[0];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                Vector3 candidate = board.CornerXZ(column, row, corner);

                if (ReferenceEquals(board.CellsAt(candidate), cells))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException("A corner's cell list does not belong to its first cell.");
        }

        private static Vector2 SwatchFor(Swatches swatches, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);

            return normal.sqrMagnitude < 1e-10f || Mathf.Abs(normal.normalized.y) < 0.5f
                ? swatches.Earth
                : swatches.Grass;
        }

        /// <summary>
        /// The corner heights the pieces candidate gives a ground cell: lifted
        /// to a neighbour exactly one level up, left alone for anything higher.
        /// </summary>
        public static float[] PieceCorners(Board board, int column, int row)
        {
            float own = board.Height(column, row);
            var claims = new float[Hex.DirectionCount];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                Vector3 cornerXZ = board.CornerXZ(column, row, corner);
                float claim = own;
                bool roadRules = false;

                foreach ((int otherColumn, int otherRow) in board.CellsAt(cornerXZ))
                {
                    if (!board.IsRoad(otherColumn, otherRow))
                    {
                        continue;
                    }

                    float road = board.RoadHeightAt(otherColumn, otherRow, cornerXZ);

                    if (Mathf.Abs(road - own) <= HexGeometry.LevelStep + Epsilon)
                    {
                        claim = road;
                        roadRules = true;
                    }
                }

                foreach ((int otherColumn, int otherRow) in board.CellsAt(cornerXZ))
                {
                    if (roadRules || board.IsRoad(otherColumn, otherRow) || (otherColumn == column && otherRow == row))
                    {
                        continue;
                    }

                    float theirs = board.Height(otherColumn, otherRow);

                    if (theirs > claim && theirs <= own + HexGeometry.LevelStep + Epsilon)
                    {
                        claim = theirs;
                    }
                }

                claims[corner] = claim;
            }

            return claims;
        }

        /// <summary>The pieces candidate's surface height at a cell's corner.</summary>
        public static float PieceHeightAt(Board board, Dictionary<(int, int), float[]> corners, int column, int row, Vector3 cornerXZ)
        {
            if (board.IsRoad(column, row))
            {
                return board.RoadHeightAt(column, row, cornerXZ);
            }

            float[] claims = corners[(column, row)];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                if ((board.CornerXZ(column, row, corner) - cornerXZ).sqrMagnitude < 1e-6f)
                {
                    return claims[corner];
                }
            }

            throw new InvalidOperationException("Asked for a corner the cell does not have.");
        }

        /// <summary>
        /// Draws the pieces over a floor that <see cref="StripGround"/> has
        /// cleared: one object per ground cell, the pack's grass tile where
        /// nothing lifts and an authored piece everywhere else.
        /// </summary>
        public static Bill BuildPieces(
            Board board, Swatches swatches, Material surface, Mesh grassTile,
            out List<(Renderer Renderer, int Level)> pieces,
            out Dictionary<(int, int), float[]> corners)
        {
            var bill = new Bill();
            var patterns = new HashSet<string>();
            pieces = new List<(Renderer, int)>();
            corners = new Dictionary<(int, int), float[]>();
            HexMap map = board.Map;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (!board.IsRoad(column, row))
                    {
                        corners[(column, row)] = PieceCorners(board, column, row);
                    }
                }
            }

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (board.IsRoad(column, row))
                    {
                        continue;
                    }

                    int level = map.LevelAt(column, row);
                    float own = board.Height(column, row);
                    float[] claims = corners[(column, row)];
                    bool flat = claims.All(claim => Mathf.Abs(claim - own) < Epsilon);
                    bool onRim = Enumerable.Range(0, Hex.DirectionCount).Any(direction => !board.Neighbour(column, row, direction, out _, out _));

                    var piece = new GameObject("Piece " + column.ToString(CultureInfo.InvariantCulture) + "," + row.ToString(CultureInfo.InvariantCulture));
                    piece.transform.SetParent(board.Floor.transform, worldPositionStays: false);

                    Renderer renderer;

                    if (flat && !onRim)
                    {
                        piece.transform.localPosition = WithY(board.Centre(column, row), own);
                        piece.AddComponent<MeshFilter>().sharedMesh = grassTile;
                        renderer = piece.AddComponent<MeshRenderer>();
                        bill.PackPieces++;
                    }
                    else
                    {
                        var mesh = new MeshBuilder();
                        PieceMesh(board, mesh, swatches, corners, column, row, bill);
                        piece.AddComponent<MeshFilter>().sharedMesh = mesh.Build(1);
                        renderer = piece.AddComponent<MeshRenderer>();
                        bill.Vertices += mesh.VertexCount;
                        bill.Triangles += mesh.TriangleCount;
                        bill.Meshes++;
                        bill.AuthoredPieces++;

                        string pattern = Pattern(claims, own);

                        if (patterns.Add(pattern))
                        {
                            bill.Patterns.Add(pattern);
                        }
                    }

                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    renderer.receiveShadows = true;
                    renderer.sharedMaterial = surface;
                    pieces.Add((renderer, level));
                    bill.Objects++;
                }
            }

            bill.DistinctPatterns = patterns.Count;

            return bill;
        }

        private static void PieceMesh(
            Board board, MeshBuilder mesh, Swatches swatches, Dictionary<(int, int), float[]> corners,
            int column, int row, Bill bill)
        {
            float own = board.Height(column, row);
            float[] claims = corners[(column, row)];
            Vector3 centreXZ = board.Centre(column, row);
            Vector3 centre = WithY(centreXZ, own);

            var top = new Vector3[Hex.DirectionCount];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                top[corner] = WithY(board.CornerXZ(column, row, corner), claims[corner]);
            }

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                mesh.Triangle(0, centre, top[corner], top[(corner + 1) % Hex.DirectionCount], swatches.Grass, Vector3.up);
            }

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                (int first, int second) = board.EdgeCorners(direction);
                Vector3 outward = top[first] + top[second] - (2f * centre);
                bool wall;

                if (!board.Neighbour(column, row, direction, out int otherColumn, out int otherRow))
                {
                    wall = true;
                }
                else
                {
                    float theirFirst = PieceHeightAt(board, corners, otherColumn, otherRow, board.CornerXZ(column, row, first));
                    float theirSecond = PieceHeightAt(board, corners, otherColumn, otherRow, board.CornerXZ(column, row, second));

                    wall = theirFirst < claims[first] - Epsilon || theirSecond < claims[second] - Epsilon;

                    if (wall)
                    {
                        bill.WallsBetweenCells++;
                    }
                }

                if (!wall)
                {
                    continue;
                }

                float bottom = own - HexGeometry.TileBody;

                mesh.Quad(
                    0, top[first], top[second],
                    WithY(top[second], bottom), WithY(top[first], bottom),
                    swatches.Earth, outward);
            }
        }

        /// <summary>A piece's shape as text: its six corner lifts in quarter levels, turned to whichever rotation reads smallest.</summary>
        private static string Pattern(float[] claims, float own)
        {
            var lifts = claims.Select(claim => Mathf.RoundToInt((claim - own) / (HexGeometry.LevelStep * 0.5f))).ToArray();
            string best = null;

            for (int turn = 0; turn < Hex.DirectionCount; turn++)
            {
                string candidate = string.Join(string.Empty, Enumerable.Range(0, Hex.DirectionCount).Select(index => lifts[(index + turn) % Hex.DirectionCount].ToString(CultureInfo.InvariantCulture)));

                if (best == null || string.CompareOrdinal(candidate, best) < 0)
                {
                    best = candidate;
                }
            }

            return best;
        }

        /// <summary>
        /// A dark ribbon along every edge between two cells of different level,
        /// lying on whichever surface the candidate drew there.
        /// </summary>
        public static Renderer DrawContours(
            Board board, Material ink,
            Func<int, int, Vector3, float> heightAt,
            Func<int, int, int, int, float?> midHeight)
        {
            var mesh = new MeshBuilder();
            HexMap map = board.Map;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        if (!board.Neighbour(column, row, direction, out int otherColumn, out int otherRow))
                        {
                            continue;
                        }

                        if ((otherRow * map.Width) + otherColumn < (row * map.Width) + column)
                        {
                            continue;
                        }

                        if (map.LevelAt(column, row) == map.LevelAt(otherColumn, otherRow))
                        {
                            continue;
                        }

                        bool hereLower = map.LevelAt(column, row) < map.LevelAt(otherColumn, otherRow);
                        int lowColumn = hereLower ? column : otherColumn;
                        int lowRow = hereLower ? row : otherRow;

                        (int first, int second) = board.EdgeCorners(direction);
                        Vector3 firstXZ = board.CornerXZ(column, row, first);
                        Vector3 secondXZ = board.CornerXZ(column, row, second);
                        Vector3 middleXZ = (firstXZ + secondXZ) * 0.5f;

                        float firstHeight = heightAt(lowColumn, lowRow, firstXZ);
                        float secondHeight = heightAt(lowColumn, lowRow, secondXZ);
                        float middle = midHeight(column, row, otherColumn, otherRow) ?? (firstHeight + secondHeight) * 0.5f;

                        Ribbon(mesh, WithY(firstXZ, firstHeight), WithY(middleXZ, middle), ink);
                        Ribbon(mesh, WithY(middleXZ, middle), WithY(secondXZ, secondHeight), ink);
                    }
                }
            }

            var host = new GameObject("Contours");
            host.transform.SetParent(board.Floor.transform, worldPositionStays: false);
            host.AddComponent<MeshFilter>().sharedMesh = mesh.Build(1);

            var renderer = host.AddComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = ink;

            return renderer;
        }

        private static void Ribbon(MeshBuilder mesh, Vector3 from, Vector3 to, Material ink)
        {
            Vector3 along = to - from;
            Vector3 side = Vector3.Cross(Vector3.up, new Vector3(along.x, 0f, along.z)).normalized * (ContourWidth * 0.5f);
            Vector3 lift = Vector3.up * ContourLift;

            mesh.Quad(0, from - side + lift, from + side + lift, to + side + lift, to - side + lift, Vector2.zero, Vector3.up);
        }

        /// <summary>
        /// One tint per level in use, from cool low ground to warm high, spread
        /// over the levels this board actually has so each step is a visible one.
        /// </summary>
        public static Color[] BandTints(HexMap map)
        {
            int lowest = int.MaxValue, highest = int.MinValue;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    lowest = Math.Min(lowest, map.LevelAt(column, row));
                    highest = Math.Max(highest, map.LevelAt(column, row));
                }
            }

            Color[] stops =
            {
                new Color(0.70f, 0.95f, 1.00f),
                new Color(0.85f, 1.00f, 0.85f),
                new Color(1.00f, 1.00f, 0.70f),
                new Color(1.00f, 0.90f, 0.55f),
                new Color(1.00f, 0.75f, 0.50f),
            };

            var tints = new Color[HexMap.LevelCount];

            for (int level = 0; level < HexMap.LevelCount; level++)
            {
                float t = highest > lowest ? Mathf.Clamp01((level - lowest) / (float)(highest - lowest)) : 0f;
                float scaled = t * (stops.Length - 1);
                int from = Mathf.Min(Mathf.FloorToInt(scaled), stops.Length - 2);

                tints[level] = Color.Lerp(stops[from], stops[from + 1], scaled - from);
            }

            return tints;
        }

        private static Vector3 WithY(Vector3 point, float y) => new Vector3(point.x, y, point.z);
    }

    /// <summary>Flat-shaded triangles, gathered into submeshes, built into one mesh.</summary>
    internal sealed class MeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly Dictionary<int, List<int>> _submeshes = new Dictionary<int, List<int>>();

        public int VertexCount => _vertices.Count;

        public int TriangleCount { get; private set; }

        /// <summary>Adds a triangle wound so its face points along <paramref name="outward"/>.</summary>
        public void Triangle(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector2 uv, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);

            if (normal.sqrMagnitude < 1e-10f)
            {
                return;
            }

            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }

            normal.Normalize();

            if (!_submeshes.TryGetValue(submesh, out List<int> indices))
            {
                indices = new List<int>();
                _submeshes[submesh] = indices;
            }

            int start = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);

            for (int index = 0; index < 3; index++)
            {
                _normals.Add(normal);
                _uvs.Add(uv);
                indices.Add(start + index);
            }

            TriangleCount++;
        }

        public void Quad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv, Vector3 outward)
        {
            Triangle(submesh, a, b, c, uv, outward);
            Triangle(submesh, a, c, d, uv, outward);
        }

        public Mesh Build(int submeshCount)
        {
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = submeshCount;

            for (int submesh = 0; submesh < submeshCount; submesh++)
            {
                mesh.SetTriangles(_submeshes.TryGetValue(submesh, out List<int> indices) ? indices : new List<int>(), submesh);
            }

            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
