using System;
using System.Globalization;
using System.IO;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    public static class BoardFrameCapture
    {
        public const string OutDirArgument = "-boardOut";

        public const string WidthArgument = "-boardWidth";

        public const string MatchFrameName = "board-match.png";

        public const string PlanFrameName = "board-plan.png";

        public const string CheckName = "board-check.png";

        private const float FrameAspect = 16f / 9f;

        private const int CropSize = 240;

        private const int CropZoom = 2;

        private const float PlanHeight = 60f;

        [MenuItem("Tools/Capture the board")]
        public static void CaptureDefault() => Run();

        public static void Run()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            string outDir = BatchArguments.Value(OutDirArgument) ?? Path.Combine(root, "docs", "frames", "board");
            int width = ParseInt(BatchArguments.Value(WidthArgument), 1600);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            Directory.CreateDirectory(outDir);

            HexMap map = StreamingContent.ReadMap();
            (int column, int row) steepest = SteepestCorridorCell(map);
            Vector3 steepestPoint = HexGeometry.ToWorld(steepest.column, steepest.row, map.LevelAt(steepest.column, steepest.row));

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            var host = new GameObject("BoardCaptureRoot");
            Texture2D match = null, plan = null, matchCrop = null, planCrop = null, check = null;

            try
            {
                var scene = host.AddComponent<MatchRoot>();
                scene.Build(map, MatchSceneBuilder.Tiles(), MatchSceneBuilder.Scenery(), MatchSceneBuilder.Dressing());

                Camera camera = scene.CameraRig.Camera;
                camera.backgroundColor = SceneFraming.BackgroundColor;
                camera.aspect = FrameAspect;
                scene.CameraRig.Reframe(scene.Floor.WorldBounds);
                scene.CameraRig.PointAt(
                    SceneFraming.CameraDefaultYawDegrees,
                    SceneFraming.CameraDefaultPitchDegrees,
                    scene.CameraRig.FramedDistance);

                UnityEngine.Object.DestroyImmediate(Grab(camera, 32, 32));

                match = Grab(camera, width, height);
                matchCrop = CropAround(camera, match, steepestPoint, width, height, "match");

                Bounds bounds = scene.Floor.WorldBounds;
                camera.orthographic = true;
                camera.orthographicSize = SceneFraming.CameraFramingMargin
                    * Mathf.Max(bounds.size.z * 0.5f, bounds.size.x * 0.5f / FrameAspect);
                camera.transform.position = new Vector3(bounds.center.x, PlanHeight, bounds.center.z);
                camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                plan = Grab(camera, width, height);
                planCrop = CropAround(camera, plan, steepestPoint, width, height, "plan");

                check = SideBySide(matchCrop, planCrop);

                Save(match, Path.Combine(outDir, MatchFrameName));
                Save(plan, Path.Combine(outDir, PlanFrameName));
                Save(check, Path.Combine(outDir, CheckName));
            }
            finally
            {
                foreach (Texture2D texture in new[] { match, plan, matchCrop, planCrop, check })
                {
                    if (texture != null)
                    {
                        UnityEngine.Object.DestroyImmediate(texture);
                    }
                }

                UnityEngine.Object.DestroyImmediate(host);
            }

            Debug.Log(
                "BoardFrameCapture: wrote " + MatchFrameName + ", " + PlanFrameName + " and " + CheckName + " to " + outDir
                + "; the check crops corridor cell " + steepest.column.ToString(CultureInfo.InvariantCulture)
                + "," + steepest.row.ToString(CultureInfo.InvariantCulture)
                + " (level " + map.LevelAt(steepest.column, steepest.row).ToString(CultureInfo.InvariantCulture)
                + ") out of each frame at " + CropZoom.ToString(CultureInfo.InvariantCulture) + "x");
        }

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

        private static Texture2D CropAround(
            Camera camera, Texture2D frame, Vector3 point, int width, int height, string view)
        {
            Vector3 viewport = camera.WorldToViewportPoint(point);
            int x = Mathf.Clamp(Mathf.RoundToInt(viewport.x * width) - (CropSize / 2), 0, width - CropSize);
            int y = Mathf.Clamp(Mathf.RoundToInt(viewport.y * height) - (CropSize / 2), 0, height - CropSize);

            Debug.Log("BoardFrameCapture: " + view + " crop at " + x + "," + y);

            return Zoom(frame, x, y, CropSize, CropZoom);
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

        private static Texture2D SideBySide(Texture2D left, Texture2D right)
        {
            var sheet = new Texture2D(left.width + right.width, Mathf.Max(left.height, right.height), TextureFormat.RGB24, false);
            var background = new Color[sheet.width * sheet.height];

            for (int pixel = 0; pixel < background.Length; pixel++)
            {
                background[pixel] = SceneFraming.BackgroundColor;
            }

            sheet.SetPixels(background);
            sheet.SetPixels(0, 0, left.width, left.height, left.GetPixels());
            sheet.SetPixels(left.width, 0, right.width, right.height, right.GetPixels());
            sheet.Apply();

            return sheet;
        }

        private static int ParseInt(string value, int fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
