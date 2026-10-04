using System;
using System.Collections.Generic;
using System.Linq;
using Sim;
using UnityEngine;

namespace View
{
    public sealed class HexSkin
    {
        private const float Inset = 0.6f;

        private const float ContourWidth = 0.1f;

        public const float ContourLift = 0.05f;

        private const float NearCornerSquared = 0.04f;

        private readonly HexMap _map;

        private readonly float _rimDrop;

        private readonly float[][] _pieceCorners;

        private readonly Dictionary<long, List<(int Column, int Row)>> _cellsAtCorner =
            new Dictionary<long, List<(int Column, int Row)>>();

        private readonly Dictionary<long, Vector3> _cornerAt = new Dictionary<long, Vector3>();

        private readonly (int First, int Second)[] _edgeCorners = new (int, int)[Hex.DirectionCount];

        public readonly struct Swatches
        {
            public Swatches(Vector2 grass, Vector2 earth)
            {
                Grass = grass;
                Earth = earth;
            }

            public Vector2 Grass { get; }

            public Vector2 Earth { get; }

            public static Swatches SampledFrom(Mesh groundTile)
            {
                if (groundTile == null)
                {
                    return new Swatches(Vector2.zero, Vector2.zero);
                }

                Vector3[] normals = groundTile.normals;
                Vector2[] uvs = groundTile.uv;

                if (normals.Length != uvs.Length || uvs.Length == 0)
                {
                    return new Swatches(Vector2.zero, Vector2.zero);
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

                if (grassCount == 0)
                {
                    return new Swatches(Vector2.zero, Vector2.zero);
                }

                grass /= grassCount;

                return new Swatches(grass, earthCount == 0 ? grass : earth / earthCount);
            }
        }

        public HexSkin(HexMap map, MeshRenderer[] standing, float rimDrop)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _rimDrop = rimDrop;
            _pieceCorners = new float[map.Width * map.Height][];

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    for (int corner = 0; corner < Hex.DirectionCount; corner++)
                    {
                        Vector3 at = CornerXZ(column, row, corner);
                        long key = Key(at);

                        if (!_cellsAtCorner.TryGetValue(key, out List<(int, int)> cells))
                        {
                            cells = new List<(int, int)>();
                            _cellsAtCorner[key] = cells;
                            _cornerAt[key] = at;
                        }

                        cells.Add((column, row));
                    }

                    MeshRenderer piece = standing?[(row * map.Width) + column];

                    if (piece != null)
                    {
                        _pieceCorners[(row * map.Width) + column] = PieceCorners(piece, column, row);
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

        public bool StandsAPiece(int column, int row) => _pieceCorners[(row * _map.Width) + column] != null;

        public float LevelHeight(int column, int row) => _map.LevelAt(column, row) * HexGeometry.LevelStep;

        public float HeightAt(int column, int row, Vector3 cornerXZ)
        {
            float[] corners = _pieceCorners[(row * _map.Width) + column];

            if (corners == null)
            {
                return LevelHeight(column, row);
            }

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                if ((CornerXZ(column, row, corner) - WithY(cornerXZ, 0f)).sqrMagnitude < 1e-6f)
                {
                    return corners[corner];
                }
            }

            throw new InvalidOperationException("Asked for a corner the cell does not have.");
        }

        public float CornerHeight(Vector3 cornerXZ)
        {
            List<(int Column, int Row)> cells = CellsAt(cornerXZ);

            foreach ((int column, int row) in cells)
            {
                if (StandsAPiece(column, row))
                {
                    return HeightAt(column, row, cornerXZ);
                }
            }

            return cells.Count < 3 ? RimHeightAt(cornerXZ) : MeanAt(cornerXZ);
        }

        public Mesh Ground(Swatches swatches)
        {
            var mesh = new FlatMesh();

            for (int row = 0; row < _map.Height; row++)
            {
                for (int column = 0; column < _map.Width; column++)
                {
                    if (StandsAPiece(column, row))
                    {
                        SkirtPiece(mesh, swatches, column, row);
                    }
                    else
                    {
                        SkinCell(mesh, swatches, column, row);
                    }
                }
            }

            foreach (KeyValuePair<long, List<(int Column, int Row)>> corner in _cellsAtCorner)
            {
                SkinCorner(mesh, swatches, _cornerAt[corner.Key], corner.Value);
            }

            return mesh.Build("Skin");
        }

        public Mesh Contours()
        {
            var mesh = new FlatMesh();

            for (int row = 0; row < _map.Height; row++)
            {
                for (int column = 0; column < _map.Width; column++)
                {
                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        if (!Neighbour(column, row, direction, out int otherColumn, out int otherRow))
                        {
                            continue;
                        }

                        if ((otherRow * _map.Width) + otherColumn < (row * _map.Width) + column)
                        {
                            continue;
                        }

                        if (_map.LevelAt(column, row) == _map.LevelAt(otherColumn, otherRow))
                        {
                            continue;
                        }

                        (int first, int second) = _edgeCorners[direction];
                        Vector3 firstXZ = CornerXZ(column, row, first);
                        Vector3 secondXZ = CornerXZ(column, row, second);
                        Vector3 middleXZ = (firstXZ + secondXZ) * 0.5f;

                        float firstHeight = CornerHeight(firstXZ);
                        float secondHeight = CornerHeight(secondXZ);
                        float middle = StandsAPiece(column, row) || StandsAPiece(otherColumn, otherRow)
                            ? (firstHeight + secondHeight) * 0.5f
                            : (LevelHeight(column, row) + LevelHeight(otherColumn, otherRow)) * 0.5f;

                        Ribbon(mesh, WithY(firstXZ, firstHeight), WithY(middleXZ, middle));
                        Ribbon(mesh, WithY(middleXZ, middle), WithY(secondXZ, secondHeight));
                    }
                }
            }

            return mesh.Build("Contours");
        }

        private float[] PieceCorners(MeshRenderer piece, int column, int row)
        {
            var heights = new float[Hex.DirectionCount];
            Transform at = piece.transform;
            MeshFilter filter = piece.GetComponent<MeshFilter>();
            Vector3[] vertices = filter != null && filter.sharedMesh != null
                ? filter.sharedMesh.vertices
                : Array.Empty<Vector3>();

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                Vector3 cornerXZ = CornerXZ(column, row, corner);
                Vector3 local = at.InverseTransformPoint(cornerXZ);
                float best = float.NegativeInfinity;

                foreach (Vector3 vertex in vertices)
                {
                    float dx = vertex.x - local.x, dz = vertex.z - local.z;

                    if ((dx * dx) + (dz * dz) < NearCornerSquared && vertex.y > best)
                    {
                        best = vertex.y;
                    }
                }

                heights[corner] = float.IsNegativeInfinity(best)
                    ? LevelHeight(column, row)
                    : at.TransformPoint(new Vector3(local.x, best, local.z)).y;
            }

            return heights;
        }

        private void SkinCell(FlatMesh mesh, Swatches swatches, int column, int row)
        {
            float height = LevelHeight(column, row);
            Vector3 centreXZ = Centre(column, row);
            Vector3 centre = WithY(centreXZ, height);
            var plateau = new Vector3[Hex.DirectionCount];

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                plateau[corner] = Plateau(column, row, CornerXZ(column, row, corner));
            }

            for (int corner = 0; corner < Hex.DirectionCount; corner++)
            {
                mesh.Triangle(
                    centre, plateau[corner], plateau[(corner + 1) % Hex.DirectionCount], swatches.Grass, Vector3.up);
            }

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                (int first, int second) = _edgeCorners[direction];
                Vector3 firstXZ = CornerXZ(column, row, first);
                Vector3 secondXZ = CornerXZ(column, row, second);
                Vector3 outward = firstXZ + secondXZ - (2f * centreXZ);

                if (Neighbour(column, row, direction, out int otherColumn, out int otherRow))
                {
                    Vector3 farFirst = Plateau(otherColumn, otherRow, firstXZ);
                    Vector3 farSecond = Plateau(otherColumn, otherRow, secondXZ);

                    if (!StandsAPiece(otherColumn, otherRow))
                    {
                        farFirst = (plateau[first] + farFirst) * 0.5f;
                        farSecond = (plateau[second] + farSecond) * 0.5f;
                    }

                    mesh.Quad(plateau[first], plateau[second], farSecond, farFirst, swatches.Grass, Vector3.up);

                    continue;
                }

                float rimFirst = RimHeightAt(firstXZ);
                float rimSecond = RimHeightAt(secondXZ);

                mesh.Quad(
                    plateau[first], plateau[second],
                    WithY(secondXZ, rimSecond), WithY(firstXZ, rimFirst),
                    swatches.Earth, Vector3.up);

                mesh.Quad(
                    WithY(firstXZ, rimFirst), WithY(secondXZ, rimSecond),
                    WithY(secondXZ, rimSecond - HexGeometry.TileBody), WithY(firstXZ, rimFirst - HexGeometry.TileBody),
                    swatches.Earth, outward);
            }
        }

        private void SkirtPiece(FlatMesh mesh, Swatches swatches, int column, int row)
        {
            Vector3 centreXZ = Centre(column, row);

            for (int direction = 0; direction < Hex.DirectionCount; direction++)
            {
                if (Neighbour(column, row, direction, out _, out _))
                {
                    continue;
                }

                (int first, int second) = _edgeCorners[direction];
                Vector3 firstXZ = CornerXZ(column, row, first);
                Vector3 secondXZ = CornerXZ(column, row, second);
                Vector3 outward = firstXZ + secondXZ - (2f * centreXZ);

                mesh.Quad(
                    WithY(firstXZ, HeightAt(column, row, firstXZ)),
                    WithY(secondXZ, HeightAt(column, row, secondXZ)),
                    WithY(secondXZ, RimHeightAt(secondXZ) - HexGeometry.TileBody),
                    WithY(firstXZ, RimHeightAt(firstXZ) - HexGeometry.TileBody),
                    swatches.Earth, outward);
            }
        }

        private void SkinCorner(FlatMesh mesh, Swatches swatches, Vector3 cornerXZ, List<(int Column, int Row)> cells)
        {
            int grounds = cells.Count(cell => !StandsAPiece(cell.Column, cell.Row));

            if (grounds == 0)
            {
                return;
            }

            var points = new List<(Vector3 Point, bool Ground)>();

            foreach ((int column, int row) in cells)
            {
                points.Add((Plateau(column, row, cornerXZ), !StandsAPiece(column, row)));
            }

            for (int missing = cells.Count; missing < 3; missing++)
            {
                points.Add((WithY(cornerXZ, RimHeightAt(cornerXZ)), false));
            }

            points = points
                .OrderBy(point => Mathf.Atan2(point.Point.z - cornerXZ.z, point.Point.x - cornerXZ.x))
                .ToList();

            if (grounds == 3)
            {
                Vector3 hub = WithY(cornerXZ, MeanAt(cornerXZ));

                for (int index = 0; index < 3; index++)
                {
                    Vector3 here = points[index].Point;
                    Vector3 toNext = (here + points[(index + 1) % 3].Point) * 0.5f;
                    Vector3 toPrevious = (here + points[(index + 2) % 3].Point) * 0.5f;

                    mesh.Quad(here, toNext, hub, toPrevious, swatches.Grass, Vector3.up);
                }

                return;
            }

            if (grounds == 2)
            {
                int a = points.FindIndex(point => point.Ground);
                int b = points.FindIndex(a + 1, point => point.Ground);
                int other = 3 - a - b;
                Vector3 middle = (points[a].Point + points[b].Point) * 0.5f;

                mesh.Triangle(
                    points[a].Point, middle, points[other].Point,
                    SwatchFor(swatches, points[a].Point, middle, points[other].Point), Vector3.up);
                mesh.Triangle(
                    middle, points[b].Point, points[other].Point,
                    SwatchFor(swatches, middle, points[b].Point, points[other].Point), Vector3.up);

                return;
            }

            int only = points.FindIndex(point => point.Ground);
            Vector3 inward = WithY(points[only].Point - cornerXZ, 0f).normalized;

            mesh.Triangle(
                points[0].Point, points[1].Point, points[2].Point,
                SwatchFor(swatches, points[0].Point, points[1].Point, points[2].Point),
                Vector3.up + inward);
        }

        private Vector3 Plateau(int column, int row, Vector3 cornerXZ)
        {
            if (StandsAPiece(column, row))
            {
                return WithY(cornerXZ, HeightAt(column, row, cornerXZ));
            }

            Vector3 centre = Centre(column, row);

            return WithY(centre + ((cornerXZ - centre) * Inset), LevelHeight(column, row));
        }

        private float MeanAt(Vector3 cornerXZ)
        {
            List<(int Column, int Row)> cells = CellsAt(cornerXZ);
            float sum = 0f;

            foreach ((int column, int row) in cells)
            {
                sum += HeightAt(column, row, cornerXZ);
            }

            return sum / cells.Count;
        }

        private float RimHeightAt(Vector3 cornerXZ) => MeanAt(cornerXZ) - _rimDrop;

        private List<(int Column, int Row)> CellsAt(Vector3 cornerXZ) => _cellsAtCorner[Key(cornerXZ)];

        private bool Neighbour(int column, int row, int direction, out int otherColumn, out int otherRow)
        {
            Hex hex = Hex.FromOddRowOffset(column, row);
            Hex.ToOddRowOffset(hex.Neighbour(direction), out otherColumn, out otherRow);

            return otherColumn >= 0 && otherColumn < _map.Width && otherRow >= 0 && otherRow < _map.Height;
        }

        private static Vector3 Centre(int column, int row) => HexGeometry.ToWorld(column, row);

        private static Vector3 CornerXZ(int column, int row, int corner) =>
            Centre(column, row) + HexGeometry.Corner(corner);

        private static void Ribbon(FlatMesh mesh, Vector3 from, Vector3 to)
        {
            Vector3 along = to - from;
            Vector3 side = Vector3.Cross(Vector3.up, new Vector3(along.x, 0f, along.z)).normalized * (ContourWidth * 0.5f);
            Vector3 lift = Vector3.up * ContourLift;

            mesh.Quad(from - side + lift, from + side + lift, to + side + lift, to - side + lift, Vector2.zero, Vector3.up);
        }

        private static Vector2 SwatchFor(Swatches swatches, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);

            return normal.sqrMagnitude < 1e-10f || Mathf.Abs(normal.normalized.y) < 0.5f
                ? swatches.Earth
                : swatches.Grass;
        }

        private static Vector3 WithY(Vector3 point, float y) => new Vector3(point.x, y, point.z);

        private static long Key(Vector3 point) =>
            ((long)Mathf.RoundToInt(point.x * 1000f) << 32) ^ (uint)Mathf.RoundToInt(point.z * 1000f);
    }
}
