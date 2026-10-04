using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// PROTOTYPE. Renders the board today and the two smoothing candidates of
    /// <see cref="SmoothingCandidates"/>, each candidate bare and under each
    /// legibility aid, from the match camera and from a plan view, and stitches
    /// each camera's nine frames into one sheet — so issue #329's decision is
    /// made from one picture per camera.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every frame is a fresh build of the same board.</b> The scene is
    /// built, altered, photographed and destroyed nine times, because a scene
    /// re-dressed in place is how an earlier sheet ended up drawing two floors
    /// at once. <c>content/map.txt</c> is read through the simulation's own
    /// parser and nothing here writes to it.
    /// </para>
    /// <para>
    /// <b>The second camera is a plan.</b> <c>tools/render-map.ps1</c> draws
    /// the board as a top-down SVG and has no Unity camera to borrow, so its
    /// view is taken here as an orthographic camera looking straight down,
    /// which is the angle at which height is invisible and the aids have to
    /// carry the whole of the reading.
    /// </para>
    /// <para>
    /// <b>The render is checked against itself.</b> Beside each sheet goes a
    /// grid of crops around the corridor's steepest cell, one per frame, at
    /// twice the size, so whether the level change is visible in every frame
    /// is answered by a picture and not by a claim.
    /// </para>
    /// <para>
    /// Runs headless — <c>tools/capture-smoothing-sheet.ps1</c>.
    /// </para>
    /// </remarks>
    public static class SmoothingSheetCapture
    {
        public const string OutDirArgument = "-smoothingOut";

        public const string WidthArgument = "-smoothingWidth";

        private const float FrameAspect = 16f / 9f;

        private const int CropSize = 240;

        private const int CropZoom = 2;

        private const float PlanHeight = 60f;

        private static readonly string[] Cameras = { "match", "plan" };

        private static readonly (string Name, string Candidate, bool Contour, bool Band)[] Variants =
        {
            ("today", "today", false, false),
            ("skin-plain", "skin", false, false),
            ("pieces-plain", "pieces", false, false),
            ("skin-contour", "skin", true, false),
            ("skin-band", "skin", false, true),
            ("skin-both", "skin", true, true),
            ("pieces-contour", "pieces", true, false),
            ("pieces-band", "pieces", false, true),
            ("pieces-both", "pieces", true, true),
        };

        [MenuItem("Tools/Capture smoothing sheet")]
        public static void CaptureDefault() => Run();

        public static void Run()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string outDir = BatchArguments.Value(OutDirArgument) ?? Path.Combine(root, "docs", "prototypes", "smoothing");
            int width = ParseInt(BatchArguments.Value(WidthArgument), 1600);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            Directory.CreateDirectory(outDir);

            string mapPath = Path.Combine(root, "content", "map.txt");
            HexMap map = HexMap.ParseUtf8("map.txt", File.ReadAllBytes(mapPath));

            TileSet tiles = MatchSceneBuilder.Tiles();
            SceneryModels scenery = MatchSceneBuilder.Scenery();
            DressingSettings settings = AssetDatabase.LoadAssetAtPath<BoardDressingAsset>(MatchSceneBuilder.DressingAssetPath)?.Settings()
                ?? DressingSettings.Default;

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            (int column, int row) steepest = SteepestCorridorCell(map);
            Vector3 steepestPoint = HexGeometry.ToWorld(steepest.column, steepest.row, map.LevelAt(steepest.column, steepest.row));

            var materials = new List<Material>();
            Material surface = Throwaway(tiles.GrassMaterial, "Smoothing surface", materials);
            surface.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);

            Material ink = Throwaway(surface, "Contour ink", materials);
            ink.SetTexture("_BaseMap", null);
            ink.mainTexture = null;
            ink.SetColor("_BaseColor", new Color(0.13f, 0.09f, 0.06f, 1f));
            ink.color = new Color(0.13f, 0.09f, 0.06f, 1f);

            Color[] tintColours = SmoothingCandidates.BandTints(map);
            var tinted = new Material[HexMap.LevelCount];

            for (int level = 0; level < HexMap.LevelCount; level++)
            {
                tinted[level] = Throwaway(surface, "Band level " + level, materials);
                tinted[level].SetColor("_BaseColor", tintColours[level]);
                tinted[level].color = tintColours[level];
            }

            SmoothingCandidates.Swatches swatches = SmoothingCandidates.Swatches.SampledFrom(tiles.MeshFor(TilePiece.Ground));

            var frames = new Dictionary<string, List<Texture2D>>();
            var crops = new Dictionary<string, List<Texture2D>>();

            foreach (string camera in Cameras)
            {
                frames[camera] = new List<Texture2D>();
                crops[camera] = new List<Texture2D>();
            }

            var bills = new Dictionary<string, SmoothingCandidates.Bill>();
            int postsToday = -1;
            bool warmedUp = false;

            try
            {
                foreach ((string name, string candidate, bool contour, bool band) in Variants)
                {
                    var host = new GameObject("SmoothingRoot");

                    try
                    {
                        var scene = host.AddComponent<MatchRoot>();
                        scene.Build(map, tiles, scenery, settings);

                        if (postsToday < 0)
                        {
                            postsToday = SmoothingCandidates.CountPosts(scene.Floor);
                        }

                        Dress(scene, map, candidate, contour, band, settings, swatches, surface, ink, tinted, tiles, bills);

                        Camera camera = scene.CameraRig.Camera;
                        camera.backgroundColor = SceneFraming.BackgroundColor;
                        camera.aspect = FrameAspect;
                        scene.CameraRig.Reframe(scene.Floor.WorldBounds);
                        scene.CameraRig.PointAt(
                            SceneFraming.CameraDefaultYawDegrees,
                            SceneFraming.CameraDefaultPitchDegrees,
                            scene.CameraRig.FramedDistance);

                        if (!warmedUp)
                        {
                            UnityEngine.Object.DestroyImmediate(Grab(camera, 32, 32));
                            warmedUp = true;
                        }

                        Shoot(camera, width, height, outDir, name, "match", steepestPoint, frames, crops);

                        Bounds bounds = scene.Floor.WorldBounds;
                        camera.orthographic = true;
                        camera.orthographicSize = SceneFraming.CameraFramingMargin
                            * Mathf.Max(bounds.size.z * 0.5f, bounds.size.x * 0.5f / FrameAspect);
                        camera.transform.position = new Vector3(bounds.center.x, PlanHeight, bounds.center.z);
                        camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                        Shoot(camera, width, height, outDir, name, "plan", steepestPoint, frames, crops);
                    }
                    finally
                    {
                        UnityEngine.Object.DestroyImmediate(host);
                    }
                }

                foreach (string camera in Cameras)
                {
                    List<Texture2D> halves = frames[camera].Select(Half).ToList();
                    Save(Stitch(halves, width / 2, height / 2, 3), Path.Combine(outDir, "sheet-" + camera + ".png"));
                    Save(Stitch(crops[camera], CropSize * CropZoom, CropSize * CropZoom, 3), Path.Combine(outDir, "check-" + camera + ".png"));

                    foreach (Texture2D half in halves)
                    {
                        UnityEngine.Object.DestroyImmediate(half);
                    }
                }

                WriteIndex(outDir, map, steepest, postsToday, bills, width, height);
            }
            finally
            {
                foreach (Texture2D frame in frames.Values.SelectMany(list => list).Concat(crops.Values.SelectMany(list => list)))
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }

                foreach (Material material in materials)
                {
                    UnityEngine.Object.DestroyImmediate(material);
                }
            }

            Debug.Log("SmoothingSheetCapture: wrote " + (Variants.Length * Cameras.Length) + " frames, 2 sheets and 2 checks to " + outDir);
        }

        private static void Dress(
            MatchRoot scene, HexMap map, string candidate, bool contour, bool band, DressingSettings settings,
            SmoothingCandidates.Swatches swatches, Material surface, Material ink, Material[] tinted, TileSet tiles,
            Dictionary<string, SmoothingCandidates.Bill> bills)
        {
            if (candidate == "today")
            {
                return;
            }

            var board = new SmoothingCandidates.Board(map, scene.Floor, settings.RimDrop);
            SmoothingCandidates.StripGround(scene.Floor);

            Func<int, int, Vector3, float> heightAt;
            Func<int, int, int, int, float?> midHeight;
            var tintable = new List<(Renderer Renderer, int Level)>(SmoothingCandidates.RoadTiles(board));

            if (candidate == "skin")
            {
                SmoothingCandidates.Bill bill = SmoothingCandidates.BuildSkin(board, swatches, surface, out Renderer skin);
                bills[candidate] = bill;

                heightAt = (column, row, corner) => SmoothingCandidates.SkinCornerHeight(board, corner);
                midHeight = (column, row, otherColumn, otherRow) =>
                    board.IsRoad(column, row) || board.IsRoad(otherColumn, otherRow)
                        ? (float?)null
                        : (board.Height(column, row) + board.Height(otherColumn, otherRow)) * 0.5f;

                if (band)
                {
                    skin.sharedMaterials = tinted;
                }
            }
            else
            {
                SmoothingCandidates.Bill bill = SmoothingCandidates.BuildPieces(
                    board, swatches, surface, tiles.MeshFor(TilePiece.Ground),
                    out List<(Renderer Renderer, int Level)> pieces,
                    out Dictionary<(int, int), float[]> corners);
                bills[candidate] = bill;
                tintable.AddRange(pieces);

                heightAt = (column, row, corner) => SmoothingCandidates.PieceHeightAt(board, corners, column, row, corner);
                midHeight = (column, row, otherColumn, otherRow) => null;
            }

            if (band)
            {
                foreach ((Renderer renderer, int level) in tintable)
                {
                    renderer.sharedMaterial = tinted[level];
                }
            }

            if (contour)
            {
                SmoothingCandidates.DrawContours(board, ink, heightAt, midHeight);
            }
        }

        private static void Shoot(
            Camera camera, int width, int height, string outDir, string variant, string view, Vector3 steepestPoint,
            Dictionary<string, List<Texture2D>> frames, Dictionary<string, List<Texture2D>> crops)
        {
            Texture2D frame = Grab(camera, width, height);
            Save(frame, Path.Combine(outDir, variant + "-" + view + ".png"));
            frames[view].Add(frame);

            Vector3 viewport = camera.WorldToViewportPoint(steepestPoint);
            int x = Mathf.Clamp(Mathf.RoundToInt(viewport.x * width) - (CropSize / 2), 0, width - CropSize);
            int y = Mathf.Clamp(Mathf.RoundToInt(viewport.y * height) - (CropSize / 2), 0, height - CropSize);

            crops[view].Add(Zoom(frame, x, y, CropSize, CropZoom));

            Debug.Log("SmoothingSheetCapture: " + variant + " " + view + " (steepest cell at " + x + "," + y + ")");
        }

        /// <summary>
        /// The corridor cell with the most height change around it: the sum of
        /// its level differences to every neighbour on the board, first by
        /// index on a tie.
        /// </summary>
        public static (int Column, int Row) SteepestCorridorCell(HexMap map)
        {
            (int Column, int Row) best = (-1, -1);
            int bestRelief = -1;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (map.CellAt(column, row) == MapCell.Ground)
                    {
                        continue;
                    }

                    int relief = 0;
                    Hex hex = Hex.FromOddRowOffset(column, row);

                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        Hex.ToOddRowOffset(hex.Neighbour(direction), out int otherColumn, out int otherRow);

                        if (otherColumn < 0 || otherColumn >= map.Width || otherRow < 0 || otherRow >= map.Height)
                        {
                            continue;
                        }

                        relief += Math.Abs(map.LevelAt(column, row) - map.LevelAt(otherColumn, otherRow));
                    }

                    if (relief > bestRelief)
                    {
                        bestRelief = relief;
                        best = (column, row);
                    }
                }
            }

            return best;
        }

        private static void WriteIndex(
            string outDir, HexMap map, (int Column, int Row) steepest, int postsToday,
            Dictionary<string, SmoothingCandidates.Bill> bills, int width, int height)
        {
            Census(map, out int boundaries, out int corridor);

            var lines = new List<string>
            {
                "# The board-smoothing sheet: what each frame is, and what each candidate cost.",
                "#",
                "# Generated by tools/capture-smoothing-sheet.ps1 from content/map.txt, read through",
                "# HexMap.ParseUtf8 like any other map. Every frame is the same board: the cells, the",
                "# corridor and the level of every cell are untouched, so anything that differs between",
                "# two frames is the ground's surface and nothing that differs is the playfield.",
                string.Empty,
                "board",
                "    cells        " + (map.Width * map.Height).ToString(CultureInfo.InvariantCulture)
                    + " (" + map.Width.ToString(CultureInfo.InvariantCulture) + " by " + map.Height.ToString(CultureInfo.InvariantCulture) + ")",
                "    corridor     " + corridor.ToString(CultureInfo.InvariantCulture) + " cells",
                "    boundaries   " + boundaries.ToString(CultureInfo.InvariantCulture) + " edges between two cells of different level -- the edges each candidate smooths",
                "    cliff posts  " + postsToday.ToString(CultureInfo.InvariantCulture) + " drawn under the board today",
                "    steepest     corridor cell " + steepest.Column.ToString(CultureInfo.InvariantCulture) + "," + steepest.Row.ToString(CultureInfo.InvariantCulture)
                    + " (column,row), level " + ((char)('a' + map.LevelAt(steepest.Column, steepest.Row))).ToString()
                    + " -- the corridor cell with the most height change around it; check-*.png crops it",
                string.Empty,
                "cameras",
                "    match        the shipped framing: yaw " + SceneFraming.CameraDefaultYawDegrees.ToString("0", CultureInfo.InvariantCulture)
                    + ", pitch " + SceneFraming.CameraDefaultPitchDegrees.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", field of view " + SceneFraming.CameraFieldOfViewDegrees.ToString("0", CultureInfo.InvariantCulture)
                    + ", framed on the floor as OrbitCameraRig.Reframe does",
                "    plan         straight down, orthographic, the board fitted with the shipped margin -- render-map.ps1's view, which has no Unity camera of its own",
                "    frame        " + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture)
                    + "; sheets hold each frame at half size; checks hold a " + CropSize.ToString(CultureInfo.InvariantCulture)
                    + "px crop at " + CropZoom.ToString(CultureInfo.InvariantCulture) + "x",
                string.Empty,
                "sheet layout (both cameras, both checks), read left to right, top to bottom",
                "    today            skin-plain       pieces-plain",
                "    skin-contour     skin-band        skin-both",
                "    pieces-contour   pieces-band      pieces-both",
                string.Empty,
            };

            if (bills.TryGetValue("skin", out SmoothingCandidates.Bill skin))
            {
                lines.Add("skin");
                lines.Add("    what         one mesh over the whole board: a flat plateau per ground cell at "
                    + SmoothingCandidates.SkinInset.ToString("0.00", CultureInfo.InvariantCulture)
                    + " of the hex, planar slopes between plateaus, the pack's road pieces left standing and met at their own edge heights");
                lines.Add("    meshes       " + skin.Meshes.ToString(CultureInfo.InvariantCulture) + " (plus the " + corridor.ToString(CultureInfo.InvariantCulture) + " road tiles it runs up to)");
                lines.Add("    vertices     " + skin.Vertices.ToString(CultureInfo.InvariantCulture) + ", triangles " + skin.Triangles.ToString(CultureInfo.InvariantCulture) + ", flat shaded");
                lines.Add("    map change   nothing to author: the mesh is regenerated from the map at scene-build time");
                lines.Add("    cliff posts  gone; the rim banks down by the dressing's rim drop and hangs a skirt of earth");
                lines.Add("    scenery      stands where it stood; a prop placed off a cell's centre can sit on a slope");
                lines.Add(string.Empty);
            }

            if (bills.TryGetValue("pieces", out SmoothingCandidates.Bill pieces))
            {
                lines.Add("pieces");
                lines.Add("    what         one tile per cell: a ground cell lifts the corners it shares with a neighbour one level up to that neighbour's height, so the slope lives in the lower cell and the higher cell stays flat, as the pack's own ramp does; two levels or more is left as a cliff");
                lines.Add("    tiles        " + pieces.Objects.ToString(CultureInfo.InvariantCulture) + " ground tiles: "
                    + pieces.PackPieces.ToString(CultureInfo.InvariantCulture) + " wear the pack's flat grass tile unchanged, "
                    + pieces.AuthoredPieces.ToString(CultureInfo.InvariantCulture) + " are authored pieces");
                lines.Add("    patterns     " + pieces.DistinctPatterns.ToString(CultureInfo.InvariantCulture)
                    + " distinct authored pieces on this board (a pattern is the six corner lifts in quarter levels, rotations counted once); the pack ships three of the shapes a board can need (flat, half slope, full slope), none of the corner cases");
                lines.Add("    vertices     " + pieces.Vertices.ToString(CultureInfo.InvariantCulture) + ", triangles " + pieces.Triangles.ToString(CultureInfo.InvariantCulture) + " across the authored pieces");
                lines.Add("    walls        " + pieces.WallsBetweenCells.ToString(CultureInfo.InvariantCulture) + " cell edges where a wall still shows between two cells (a step of two levels or more, or a road the ground could not meet)");
                lines.Add("    map change   generated here, so nothing; hand-authored in the pack's style it is one model per new pattern, and a new map can introduce patterns this one never does");
                lines.Add("    cliff posts  gone between cells; the rim keeps the tile's own metre of body");
                lines.Add("    the pieces   " + string.Join(" ", pieces.Patterns));
                lines.Add(string.Empty);
            }

            lines.Add("aids");
            lines.Add("    contour      a dark ribbon " + "0.10".ToString(CultureInfo.InvariantCulture) + " m wide along every edge between two cells of different level, laid on the candidate's own surface; " + boundaries.ToString(CultureInfo.InvariantCulture) + " edges on this board");
            lines.Add("    band         one tint per level over the atlas, walking blue to green to yellow to orange from the lowest level in use to the highest, on ground and road alike; the slope between two plateaus changes colour at the cell boundary");
            lines.Add(string.Empty);

            File.WriteAllLines(Path.Combine(outDir, "smoothing.txt"), lines, new UTF8Encoding(false));
        }

        private static void Census(HexMap map, out int boundaries, out int corridor)
        {
            boundaries = 0;
            corridor = 0;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    if (map.CellAt(column, row) != MapCell.Ground)
                    {
                        corridor++;
                    }

                    int level = map.LevelAt(column, row);
                    Hex hex = Hex.FromOddRowOffset(column, row);

                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        Hex.ToOddRowOffset(hex.Neighbour(direction), out int otherColumn, out int otherRow);

                        if (otherColumn < 0 || otherColumn >= map.Width || otherRow < 0 || otherRow >= map.Height)
                        {
                            continue;
                        }

                        int drop = level - map.LevelAt(otherColumn, otherRow);

                        if (drop > 0)
                        {
                            boundaries++;
                        }
                    }
                }
            }
        }

        private static Material Throwaway(Material source, string name, List<Material> owned)
        {
            var copy = new Material(source) { name = name };
            owned.Add(copy);

            return copy;
        }

        private static Texture2D Grab(Camera camera, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            frame.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);

            return frame;
        }

        private static void Save(Texture2D texture, string path)
        {
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static Texture2D Half(Texture2D source)
        {
            int width = source.width / 2, height = source.height / 2;
            Color32[] pixels = source.GetPixels32();
            var halved = new Color32[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 a = pixels[((2 * y) * source.width) + (2 * x)];
                    Color32 b = pixels[((2 * y) * source.width) + (2 * x) + 1];
                    Color32 c = pixels[(((2 * y) + 1) * source.width) + (2 * x)];
                    Color32 d = pixels[(((2 * y) + 1) * source.width) + (2 * x) + 1];

                    halved[(y * width) + x] = new Color32(
                        (byte)((a.r + b.r + c.r + d.r) / 4),
                        (byte)((a.g + b.g + c.g + d.g) / 4),
                        (byte)((a.b + b.b + c.b + d.b) / 4),
                        255);
                }
            }

            var result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.SetPixels32(halved);
            result.Apply();

            return result;
        }

        private static Texture2D Zoom(Texture2D source, int x, int y, int size, int factor)
        {
            Color[] window = source.GetPixels(x, y, size, size);
            int side = size * factor;
            var zoomed = new Color[side * side];

            for (int row = 0; row < side; row++)
            {
                for (int column = 0; column < side; column++)
                {
                    zoomed[(row * side) + column] = window[((row / factor) * size) + (column / factor)];
                }
            }

            var result = new Texture2D(side, side, TextureFormat.RGB24, false);
            result.SetPixels(zoomed);
            result.Apply();

            return result;
        }

        private static Texture2D Stitch(List<Texture2D> tiles, int tileWidth, int tileHeight, int across)
        {
            int rows = Mathf.Max(1, Mathf.CeilToInt(tiles.Count / (float)across));
            int columns = Mathf.Min(across, Mathf.Max(1, tiles.Count));

            var sheet = new Texture2D(columns * tileWidth, rows * tileHeight, TextureFormat.RGB24, false);
            var background = new Color[tileWidth * tileHeight];

            for (int pixel = 0; pixel < background.Length; pixel++)
            {
                background[pixel] = SceneFraming.BackgroundColor;
            }

            for (int row = 0; row < rows; row++)
            {
                for (int column = 0; column < columns; column++)
                {
                    int index = (row * across) + column;

                    sheet.SetPixels(
                        column * tileWidth,
                        (rows - 1 - row) * tileHeight,
                        tileWidth,
                        tileHeight,
                        index < tiles.Count ? tiles[index].GetPixels() : background);
                }
            }

            sheet.Apply();

            return sheet;
        }

        private static int ParseInt(string value, int fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
