using UnityEngine;

namespace View
{
    /// <summary>
    /// The floor tile, generated in code: a flat pointy-top hexagon lying in the
    /// XZ plane with its centre at the origin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the seam where a real tile model goes.</b> Nothing outside
    /// this file knows the tile is generated — <see cref="HexFloor"/> takes a
    /// <see cref="Mesh"/> and places it. Swapping the blockout for an imported
    /// model is deleting the call to <see cref="Create"/> and loading an asset
    /// instead, and it moves nothing, because the imported mesh has to hold the
    /// dimensions in <see cref="HexGeometry"/> and a test measures that it does.
    /// </para>
    /// <para>
    /// <b>It is generated because choosing a tile model is an art decision, and
    /// art decisions in this project are the developer's.</b> No agent session
    /// picks a pack. Until that question is answered this is a blockout: flat,
    /// untextured, and distinguished only by colour.
    /// </para>
    /// <para>
    /// The mesh is a seven-vertex fan rather than four triangles over six
    /// vertices, because a fan gives every triangle a vertex at the centre and
    /// therefore a sane place to put the centre UV. It is not a card and not a
    /// billboard: it lies in the ground plane, faces <c>+Y</c>, and never turns
    /// to face anything.
    /// </para>
    /// </remarks>
    public static class HexTileMesh
    {
        /// <summary>How many vertices the fan has: six corners and a centre.</summary>
        public const int VertexCount = Sim.Hex.DirectionCount + 1;

        /// <summary>
        /// Builds one tile mesh. Callers share a single instance across every
        /// tile on the floor — a hundred and thirty-five copies of the same
        /// seven vertices would be a hundred and thirty-four wasted meshes.
        /// </summary>
        public static Mesh Create()
        {
            var vertices = new Vector3[VertexCount];
            var normals = new Vector3[VertexCount];
            var uv = new Vector2[VertexCount];
            var triangles = new int[Sim.Hex.DirectionCount * 3];

            vertices[0] = Vector3.zero;

            for (int corner = 0; corner < Sim.Hex.DirectionCount; corner++)
            {
                vertices[corner + 1] = HexGeometry.Corner(corner);
            }

            for (int index = 0; index < VertexCount; index++)
            {
                normals[index] = Vector3.up;

                // The tile's own bounding box mapped to the unit square, so a
                // texture on an imported replacement lands the same way.
                uv[index] = new Vector2(
                    (vertices[index].x / HexGeometry.AcrossFlats) + 0.5f,
                    (vertices[index].z / HexGeometry.PointToPoint) + 0.5f);
            }

            for (int corner = 0; corner < Sim.Hex.DirectionCount; corner++)
            {
                // (centre, corner, next corner) with the corner index rising.
                // HexGeometry.Corner is wound so that this faces +Y.
                triangles[(corner * 3) + 0] = 0;
                triangles[(corner * 3) + 1] = corner + 1;
                triangles[(corner * 3) + 2] = ((corner + 1) % Sim.Hex.DirectionCount) + 1;
            }

            var mesh = new Mesh
            {
                name = "HexTile",
                vertices = vertices,
                normals = normals,
                uv = uv,
                triangles = triangles,
            };

            mesh.RecalculateBounds();
            mesh.RecalculateTangents();

            return mesh;
        }
    }
}
