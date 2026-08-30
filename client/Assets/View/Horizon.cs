using Sim;
using UnityEngine;

namespace View
{
    /// <summary>
    /// The world the board sits in: a sky to clear to, a plain of land running
    /// out to the horizon, and the haze that joins the two.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists because the board was floating.</b> The camera cleared to
    /// a flat dark colour, so every frame was a slab of hexes hanging in a void
    /// with a hard silhouette all the way round it. Nothing about the terrain
    /// was wrong; there was simply nothing behind it, and a landscape with
    /// nothing behind it reads as a game piece rather than as country.
    /// </para>
    /// <para>
    /// <b>The land is a disc and not a box.</b> It is one flat fan of triangles
    /// laid at the height the board's rim falls to, wide enough that its edge is
    /// never on screen and far enough down that the cliff columns bury
    /// themselves in it. That last part is the whole trick: the board is not
    /// resting on the plain, it is cut out of it, which is what every diorama
    /// render in the pack's own gallery actually shows.
    /// </para>
    /// <para>
    /// <b>The haze is what makes the disc stop being a disc.</b> A flat plane
    /// of one colour meeting a sky of another draws a hard line across the
    /// frame — the same silhouette problem one step further out. Linear fog to
    /// the sky's own horizon colour dissolves it, and the numbers are in
    /// <see cref="SceneFraming"/> beside everything else that decides what the
    /// playfield looks like.
    /// </para>
    /// <para>
    /// <b>Nothing here reaches the simulation, and nothing here can be built
    /// on.</b> The plain carries no collider and is not a tile: it is never
    /// returned by <see cref="HexFloor.TileAt"/>, never picked and never
    /// counted, for the reason <see cref="HexFloor"/> gives about its cliff
    /// columns. A thing a player can click is a thing the simulation has to
    /// have an opinion about, and the simulation has no idea this exists.
    /// </para>
    /// <para>
    /// <b>The sky is global state and that is Unity's fault, not a
    /// choice.</b> <c>RenderSettings</c> is per-scene and there is no
    /// per-camera skybox short of a second camera stack, so building a horizon
    /// writes the scene's environment. One playfield is built at a time, so in
    /// practice this is the same lifetime as the root object it hangs off.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class Horizon : MonoBehaviour
    {
        /// <summary>The skybox this horizon put on the scene, or null.</summary>
        public Material Sky { get; private set; }

        /// <summary>The plain of land, or null where no material was available.</summary>
        public MeshRenderer Land { get; private set; }

        /// <summary>How high the plain stands, in metres.</summary>
        public float LandHeight { get; private set; }

        /// <summary>How far the plain reaches from the middle of the board.</summary>
        public float Radius { get; private set; }

        /// <summary>The middle of the board, which is what the plain is laid around.</summary>
        public Vector3 Middle { get; private set; }

        /// <summary>
        /// Builds a horizon under <paramref name="parent"/> and hangs it around
        /// <paramref name="map"/>.
        /// </summary>
        /// <remarks>
        /// Both materials are optional and separately so. A checkout with
        /// neither draws exactly what this project drew before there was a
        /// horizon at all, which is what keeps a test that builds a root from
        /// nothing working.
        /// </remarks>
        public static Horizon Build(
            Transform parent,
            HexMap map,
            Bounds board,
            DressingSettings settings,
            Material sky = null,
            Material land = null,
            SkySettings look = default)
        {
            var host = new GameObject("Horizon");
            host.transform.SetParent(parent, worldPositionStays: false);

            var horizon = host.AddComponent<Horizon>();
            horizon.Raise(map, board, settings, sky, land, look.OrDefault());

            return horizon;
        }

        /// <summary>
        /// Points a camera at the sky this horizon built, or leaves it clearing
        /// to the flat colour where there is no sky.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Build"/> because the camera is framed on
        /// the floor and so cannot exist before it, and the horizon is sized off
        /// the floor too. Whoever builds both says which camera looks at which
        /// sky rather than either guessing.
        /// </remarks>
        public void Frame(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.clearFlags = Sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
        }

        /// <summary>
        /// Takes the tinted sky back off the scene and out of memory.
        /// </summary>
        /// <remarks>
        /// The copy made in <see cref="Dome"/> belongs to nothing else, and a
        /// tool that builds seven boards in one editor session would otherwise
        /// leave six skies behind it.
        /// </remarks>
        private void OnDestroy()
        {
            // The haze went on with this horizon and comes off with it. Fog is
            // scene-wide, so a tool that builds one board after another would
            // otherwise leave the last one's air over the next one's board.
            RenderSettings.fog = false;

            if (Sky == null)
            {
                return;
            }

            if (ReferenceEquals(RenderSettings.skybox, Sky))
            {
                RenderSettings.skybox = null;
            }

            // DestroyImmediate is an error inside OnDestroy while playing and
            // the only thing that works outside it, so which one is asked for
            // depends on which the object is living through.
            if (Application.isPlaying)
            {
                Destroy(Sky);
            }
            else
            {
                DestroyImmediate(Sky);
            }

            Sky = null;
        }

        private void Raise(
            HexMap map,
            Bounds board,
            DressingSettings settings,
            Material sky,
            Material land,
            SkySettings look)
        {
            DressingSettings dressing = settings ?? DressingSettings.Default;

            // Centred on the board and not on the origin. Cell 0,0 is at the
            // world origin and the grid runs east and south off it, so the
            // middle of a board is a good twenty metres from the object
            // everything hangs off. It barely shows on a disc this wide, and it
            // is still the board the plain is meant to be laid around.
            Middle = new Vector3(board.center.x, 0f, board.center.z);

            LandHeight = (Floor(map) * HexGeometry.LevelStep) - dressing.RimDrop;
            Radius = Mathf.Min(
                Reach(map) * SceneFraming.HorizonReachFactor,
                SceneFraming.CameraFarClip * SceneFraming.HorizonFarClipShare);

            Sky = Dome(sky, look);
            Land = Plain(land, look);

            Haze(map, look);
        }

        /// <summary>
        /// Puts the skybox on the scene, tinted to the preset's own sky.
        /// </summary>
        /// <remarks>
        /// <b>The procedural skybox and not a painted one</b>, because a
        /// cubemap is six textures somebody has to author, licence and commit,
        /// and the thing being asked for is a graded blue with a sun in it.
        /// Unity's built-in one is that, it takes the sun's direction from the
        /// scene's own directional light so the bright part of the sky is never
        /// on the wrong side, and it is four numbers in a diff rather than a
        /// six-megabyte binary.
        /// </remarks>
        private Material Dome(Material supplied, SkySettings look)
        {
            // A copy where the material came from the scene, so tinting one
            // board's sky does not edit the committed asset and leave the
            // checkout dirty -- the same reason PrototypeCapture copies the tile
            // material before re-skinning it. Where there was no asset the one
            // built here is already nobody else's, and copying it again would
            // just be a second material to free.
            Material tinted = supplied != null
                ? new Material(supplied) { name = supplied.name + " (framed)" }
                : SkyMaterial.Create("Sky");

            if (tinted == null)
            {
                return null;
            }

            SkyMaterial.Tint(tinted, look);

            RenderSettings.skybox = tinted;

            return tinted;
        }

        /// <summary>The flat land, laid at the height the board's rim reaches.</summary>
        private MeshRenderer Plain(Material supplied, SkySettings look)
        {
            Material material = supplied != null
                ? supplied
                : ViewMaterials.Matte("Land", look.Land);

            if (material == null)
            {
                return null;
            }

            var plain = new GameObject("Land");
            plain.transform.SetParent(transform, worldPositionStays: false);
            plain.transform.localPosition = Middle + (Vector3.up * LandHeight);

            plain.AddComponent<MeshFilter>().sharedMesh = Disc(Radius);

            var renderer = plain.AddComponent<MeshRenderer>();

            // Casts and receives, like everything else drawn in this project
            // -- there is no exemption and a play-mode test refuses to let one
            // be invented. The first cut of this had casting off, on the
            // argument that a flat plane's own shadow is the plane and so costs
            // fill for nothing. True, and not the point: the rule is that every
            // shadow here is thrown by real geometry at a real light, and a
            // surface that quietly opts out of it is the first step to a
            // painted one.
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            renderer.sharedMaterial = material;

            Land = renderer;

            return renderer;
        }

        /// <summary>
        /// Turns on the distance haze, measured off the board rather than off
        /// the plain.
        /// </summary>
        /// <remarks>
        /// <b>Off the board, because the board is what the fog must not
        /// touch.</b> Tying the near plane to the plain's radius would put the
        /// far corner of a small map into the haze on one board and not on
        /// another, and a player reading the level a tile stands on should never
        /// be doing it through fog. Starting a board-and-a-half out means every
        /// hex is clear at every dolly stop and only the land beyond them
        /// fades.
        /// </remarks>
        private void Haze(HexMap map, SkySettings look)
        {
            float board = Reach(map);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = look.Haze;
            RenderSettings.fogStartDistance = board * SceneFraming.HazeNearBoards;
            RenderSettings.fogEndDistance = Mathf.Min(
                board * SceneFraming.HazeFarBoards, Radius * SceneFraming.HazeShareOfRadius);
        }

        /// <summary>
        /// The lowest level anything on the map stands at. What the plain is
        /// hung under.
        /// </summary>
        private static int Floor(HexMap map)
        {
            int lowest = int.MaxValue;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    lowest = Mathf.Min(lowest, map.LevelAt(column, row));
                }
            }

            return lowest == int.MaxValue ? 0 : lowest;
        }

        /// <summary>How far it is across the board, corner to corner, in metres.</summary>
        private static float Reach(HexMap map) =>
            Mathf.Max(
                1f,
                new Vector2(
                    map.Width * HexGeometry.AcrossFlats,
                    map.Height * HexGeometry.PointToPoint * 0.75f).magnitude);

        /// <summary>
        /// A flat fan of triangles, facing up.
        /// </summary>
        /// <remarks>
        /// A disc rather than the quad it could have been: a square's corners
        /// reach half again as far as its sides, so the haze that hides one
        /// edge leaves the other showing, and the fix is either a much bigger
        /// square or a shape whose edge is all the same distance away. This is
        /// the second one.
        /// </remarks>
        private static Mesh Disc(float radius)
        {
            const int Segments = 72;

            var vertices = new Vector3[Segments + 1];
            var normals = new Vector3[Segments + 1];
            var uv = new Vector2[Segments + 1];
            var triangles = new int[Segments * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.up;
            uv[0] = new Vector2(0.5f, 0.5f);

            for (int step = 0; step < Segments; step++)
            {
                float angle = step * Mathf.PI * 2f / Segments;
                float x = Mathf.Cos(angle);
                float z = Mathf.Sin(angle);

                vertices[step + 1] = new Vector3(x * radius, 0f, z * radius);
                normals[step + 1] = Vector3.up;
                uv[step + 1] = new Vector2((x + 1f) * 0.5f, (z + 1f) * 0.5f);

                // Wound clockwise seen from below, which is anticlockwise seen
                // from above -- the way round Unity draws a front face.
                triangles[step * 3] = 0;
                triangles[(step * 3) + 1] = ((step + 1) % Segments) + 1;
                triangles[(step * 3) + 2] = step + 1;
            }

            var mesh = new Mesh { name = "Land" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();

            return mesh;
        }
    }
}
