using System.Linq;
using UnityEngine;

namespace View
{
    public static class SeamlessPiece
    {
        private const float TopFaceTolerance = 0.001f;

        private const float HexEdge = HexGeometry.AcrossFlats / 2f;

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
                .Max(HexDistance);

            for (int index = 0; index < vertices.Length; index++)
            {
                float distance = HexDistance(vertices[index]);
                float pulledUnderTopFace = distance > topFaceEdge ? topFaceEdge / distance : 1f;
                float stretchedToHexEdge = HexEdge / topFaceEdge;

                vertices[index] = Spread(vertices[index], pulledUnderTopFace * stretchedToHexEdge);
            }

            piece.vertices = vertices;
            piece.RecalculateBounds();

            return piece;
        }

        public static float HexDistance(Vector3 point)
        {
            float furthest = 0f;

            for (int edge = 0; edge < 6; edge++)
            {
                float radians = (edge + 0.5f) * (Mathf.PI / 3f);
                furthest = Mathf.Max(furthest, (point.x * Mathf.Sin(radians)) + (point.z * Mathf.Cos(radians)));
            }

            return furthest;
        }

        private static Vector3 Spread(Vector3 point, float by) => new Vector3(point.x * by, point.y, point.z * by);
    }
}
