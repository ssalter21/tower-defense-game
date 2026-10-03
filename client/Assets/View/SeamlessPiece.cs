using System.Linq;
using UnityEngine;

namespace View
{
    public static class SeamlessPiece
    {
        private const float TopFaceTolerance = 0.001f;

        public static Mesh Of(Mesh art)
        {
            Mesh piece = Object.Instantiate(art);
            piece.name = art.name + " (seamless)";

            Vector3[] vertices = piece.vertices;

            if (vertices.Length == 0)
            {
                return piece;
            }

            float top = vertices.Max(vertex => vertex.y);
            float topFaceEdge = vertices
                .Where(vertex => vertex.y > top - TopFaceTolerance)
                .Max(HexGeometry.HexDistance);

            if (topFaceEdge <= 0f)
            {
                return piece;
            }

            float stretchedToHexEdge = HexGeometry.Apothem / topFaceEdge;

            for (int index = 0; index < vertices.Length; index++)
            {
                float distance = HexGeometry.HexDistance(vertices[index]);
                float pulledUnderTopFace = distance > topFaceEdge ? topFaceEdge / distance : 1f;

                vertices[index] = Spread(vertices[index], pulledUnderTopFace * stretchedToHexEdge);
            }

            piece.vertices = vertices;
            piece.RecalculateBounds();

            return piece;
        }

        private static Vector3 Spread(Vector3 point, float by) => new Vector3(point.x * by, point.y, point.z * by);
    }
}
