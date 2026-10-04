using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace View
{
    internal sealed class FlatMesh
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();

        private readonly List<Vector3> _normals = new List<Vector3>();

        private readonly List<Vector2> _uvs = new List<Vector2>();

        private readonly List<int>[] _submeshes;

        public FlatMesh(int submeshCount)
        {
            _submeshes = Enumerable.Range(0, submeshCount).Select(_ => new List<int>()).ToArray();
        }

        public int VertexCount => _vertices.Count;

        public int TriangleCount => _submeshes.Sum(indices => indices.Count) / 3;

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

            int start = _vertices.Count;

            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);

            for (int index = 0; index < 3; index++)
            {
                _normals.Add(normal);
                _uvs.Add(uv);
                _submeshes[submesh].Add(start + index);
            }
        }

        public void Quad(int submesh, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 uv, Vector3 outward)
        {
            Triangle(submesh, a, b, c, uv, outward);
            Triangle(submesh, a, c, d, uv, outward);
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };

            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.subMeshCount = _submeshes.Length;

            for (int submesh = 0; submesh < _submeshes.Length; submesh++)
            {
                mesh.SetTriangles(_submeshes[submesh], submesh);
            }

            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
