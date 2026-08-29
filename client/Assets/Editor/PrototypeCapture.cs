using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders one landscape per reference frame, so a board can be chosen by
    /// looking at pictures instead of by reading numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every frame is the same road.</b> Each preset names a board under
    /// <c>docs/prototypes/boards/</c>, and every one of those is the corridor of
    /// <c>content/map.txt</c> cell for cell under a different height map. So
    /// what differs between two of these pictures is the landscape and the
    /// dressing, and never the route — which is what makes them comparable at
    /// all.
    /// </para>
    /// <para>
    /// <b>No match runs.</b> These are pictures of an empty board, because what
    /// is being judged is the landscape and a creep walking through it is the
    /// one thing guaranteed to draw the eye away from the terrain. For the
    /// match itself see <see cref="MatchFrameCapture"/>, which this deliberately
    /// does not extend: that tool renders a run somebody can scrub to, and
    /// giving it a dressing argument would make its output depend on a setting
    /// that has nothing to do with the run.
    /// </para>
    /// <para>
    /// <b>Three angles each, and the low one is the point.</b> The pack's own
    /// renders read as terrain from a low camera and as a floor plan from
    /// overhead, so relief judged only from the shipped pitch is judged from the
    /// angle least able to show it.
    /// </para>
    /// <para>
    /// <b>The atlas is swapped onto a material made here and thrown away
    /// after.</b> The pack cuts four seasons against one set of UVs, so a
    /// re-skin is a texture and no geometry — but writing that texture into
    /// <c>Materials/Tiles.mat</c> to take a picture would leave the checkout
    /// dirty and the next test run reporting a change nobody made.
    /// </para>
    /// <para>
    /// Runs headless — <c>tools/capture-prototypes.ps1</c>.
    /// </para>
    /// </remarks>
    public static class PrototypeCapture
    {
        /// <summary>Where the frames land.</summary>
        public const string OutDirArgument = "-prototypeOut";

        /// <summary>Which presets to draw, comma separated. All of them by default.</summary>
        public const string NamesArgument = "-prototypeNames";

        /// <summary>How wide each frame is, in pixels.</summary>
        public const string WidthArgument = "-prototypeWidth";

        /// <summary>The shape of a frame, matching the match captures.</summary>
        private const float FrameAspect = 16f / 9f;

        /// <summary>Where the prototype boards are written, under the repository root.</summary>
        private const string BoardFolder = "docs/prototypes/boards";

        /// <summary>Where the seasonal atlases were imported.</summary>
        private const string AtlasFolder = "Assets/Art/Buildings/";

        /// <summary>
        /// The headings each board is drawn from. The shipped framing first,
        /// because that is the one the game will actually use; then two lower
        /// ones, because a cliff face is close to invisible from above.
        /// </summary>
        private static readonly (string Suffix, float Yaw, float Pitch)[] Angles =
        {
            ("high", SceneFraming.CameraDefaultYawDegrees, SceneFraming.CameraDefaultPitchDegrees),
            ("low", 34f, 19f),
            ("raking", -46f, 13f),
        };

        [MenuItem("Tools/Capture scenery prototypes")]
        public static void CaptureDefault() => Run();

        public static void Run()
        {
            string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));

            string outDir = BatchArguments.Value(OutDirArgument)
                ?? Path.Combine(root, "docs", "prototypes", "scenery");

            int width = ParseInt(BatchArguments.Value(WidthArgument), 1600);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            IReadOnlyList<SceneryPresets.Preset> presets = Chosen(BatchArguments.Value(NamesArgument));

            Directory.CreateDirectory(outDir);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;

            var written = new List<string>();
            var drawn = new List<(SceneryPresets.Preset Preset, HexMap Map)>();

            foreach (SceneryPresets.Preset preset in presets)
            {
                HexMap map = ReadBoard(root, preset.Board);
                Material surface = SurfaceFor(preset);

                var host = new GameObject("PrototypeRoot");

                try
                {
                    RenderSettings.ambientLight = new Color(
                        preset.Light.AmbientRed, preset.Light.AmbientGreen, preset.Light.AmbientBlue, 1f);

                    var scene = host.AddComponent<MatchRoot>();

                    // The committed dressing is deliberately not applied. It names
                    // cells of the shipped board by column and row, and on a
                    // board with a different height map those coordinates mean
                    // somewhere else -- a tent somebody placed on a shoulder
                    // would come back standing in the lake.
                    scene.Build(
                        map,
                        MatchSceneBuilder.Tiles(surface),
                        MatchSceneBuilder.Scenery(surface),
                        preset.Settings,
                        BoardDressing.Empty);

                    scene.Sun.transform.rotation = Quaternion.Euler(preset.Light.Pitch, preset.Light.Yaw, 0f);
                    scene.Sun.color = new Color(preset.Light.Red, preset.Light.Green, preset.Light.Blue, 1f);
                    scene.Sun.intensity = preset.Light.Intensity;

                    Camera camera = scene.CameraRig.Camera;
                    camera.backgroundColor = SceneFraming.BackgroundColor;
                    camera.aspect = FrameAspect;
                    scene.CameraRig.Reframe(scene.Floor.WorldBounds);

                    // The throwaway first render, for the reason
                    // MatchFrameCapture gives: the first frame out of a fresh
                    // batchmode editor is drawn before the shaders resolve and
                    // comes out looking like a failed import.
                    if (written.Count == 0)
                    {
                        scene.CameraRig.PointAt(
                            SceneFraming.CameraDefaultYawDegrees,
                            SceneFraming.CameraDefaultPitchDegrees,
                            scene.CameraRig.FramedDistance);

                        UnityEngine.Object.DestroyImmediate(Grab(camera, 32, 32));
                    }

                    foreach ((string suffix, float yaw, float pitch) in Angles)
                    {
                        scene.CameraRig.PointAt(yaw, pitch, scene.CameraRig.FramedDistance);

                        string path = Path.Combine(outDir, "board-" + preset.Name + "-" + suffix + ".png");

                        File.WriteAllBytes(path, Grab(camera, width, height).EncodeToPNG());
                        written.Add(path);

                        Debug.Log("PrototypeCapture: " + preset.Name + " " + suffix + " -> " + path);
                    }

                    drawn.Add((preset, map));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host);

                    if (surface != null)
                    {
                        UnityEngine.Object.DestroyImmediate(surface);
                    }
                }
            }

            if (written.Count == 0)
            {
                throw new InvalidDataException(
                    "No prototype frames were captured. A capture tool that silently writes nothing "
                    + "is worse than one that fails, because the frames it did not write look exactly "
                    + "like frames nobody asked for.");
            }

            WriteIndex(outDir, drawn);

            Debug.Log("PrototypeCapture: wrote " + written.Count + " frames to " + outDir);
        }

        /// <summary>
        /// One prototype board, read through the simulation's own parser.
        /// </summary>
        /// <remarks>
        /// <b>Through <c>HexMap.ParseUtf8</c> and nothing else</b>, so a
        /// generated board that the game would refuse fails here rather than
        /// rendering as a picture of a map nothing can load. The corridor
        /// assertion is part of that parse, which is what proves every one of
        /// these really is the committed route.
        /// </remarks>
        private static HexMap ReadBoard(string root, string board)
        {
            // The control reads the authored map file and not the streaming
            // copy or the recorded match's inlined one. Both of those go stale
            // between an edit and a sync, and a control drawn from a stale board
            // is a picture of something nobody can look at any more -- which is
            // exactly what happened the first time this ran.
            string path = board == null
                ? Path.Combine(root, "content", "map.txt")
                : Path.Combine(root, BoardFolder.Replace('/', Path.DirectorySeparatorChar), board + ".txt");

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "The preset names the board '" + board + "', and there is no such file. Boards live "
                    + "in " + BoardFolder + " and are generated -- see that folder's README.",
                    path);
            }

            return HexMap.ParseUtf8((board ?? "map") + ".txt", File.ReadAllBytes(path));
        }

        /// <summary>
        /// The material a preset is drawn with: a throwaway copy of the shipped
        /// one, wearing whichever seasonal atlas the preset names.
        /// </summary>
        private static Material SurfaceFor(SceneryPresets.Preset preset)
        {
            Material shipped = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Tiles.mat");

            if (shipped == null)
            {
                throw new FileNotFoundException("Assets/Materials/Tiles.mat is not in the project.");
            }

            var copy = new Material(shipped) { name = "Tiles (" + (preset.Atlas ?? "shipped") + ")" };

            if (preset.Atlas == null)
            {
                return copy;
            }

            string path = AtlasFolder + preset.Atlas + ".png";
            var atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (atlas == null)
            {
                throw new FileNotFoundException(
                    "The preset names the atlas '" + preset.Atlas + "' and " + path + " is not in the "
                    + "project. The pack ships four; importing one is a copy and nothing else.",
                    path);
            }

            copy.SetTexture("_BaseMap", atlas);
            copy.mainTexture = atlas;

            return copy;
        }

        /// <summary>The presets named on the command line, or all of them.</summary>
        private static IReadOnlyList<SceneryPresets.Preset> Chosen(string names)
        {
            if (string.IsNullOrWhiteSpace(names))
            {
                return SceneryPresets.All;
            }

            var chosen = new List<SceneryPresets.Preset>();

            foreach (string part in names.Split(','))
            {
                string trimmed = part.Trim();

                if (trimmed.Length > 0)
                {
                    chosen.Add(SceneryPresets.ByName(trimmed));
                }
            }

            return chosen;
        }

        /// <summary>
        /// A plain-text key beside the frames, so the pictures do not have to be
        /// matched back to the numbers from memory. The relief census is counted
        /// off the parsed map rather than typed, because the whole claim these
        /// prototypes make is about how often the ground changes height.
        /// </summary>
        private static void WriteIndex(
            string outDir, IReadOnlyList<(SceneryPresets.Preset Preset, HexMap Map)> drawn)
        {
            var lines = new List<string>
            {
                "# The landscape prototypes, and what each is trying to be.",
                "#",
                "# Generated by tools/capture-prototypes.ps1. Every board is the corridor of",
                "# content/map.txt, cell for cell, under a different height map -- so the route",
                "# is the same in all of them and only the landscape differs.",
                "#",
                "# A level is half a block. 'falls' counts the ordered pairs of touching cells",
                "# where one stands above the other; 'of a block' is how many of those are two",
                "# levels or more, which is the drop the old board could only ever make.",
                string.Empty,
            };

            foreach ((SceneryPresets.Preset preset, HexMap map) in drawn)
            {
                DressingSettings settings = preset.Settings;

                Census(map, out int falls, out int blocks, out int lowest, out int highest);

                lines.Add(preset.Name);
                lines.Add("    reference  " + preset.Reference);
                lines.Add("    intent     " + preset.Intent);
                lines.Add("    board      " + (preset.Board ?? "content/map.txt"));
                lines.Add("    atlas      " + (preset.Atlas ?? "hexagons_medieval (shipped)"));
                lines.Add(
                    "    relief     levels " + Letter(lowest) + " to " + Letter(highest)
                    + ", " + falls.ToString(CultureInfo.InvariantCulture) + " falls, "
                    + blocks.ToString(CultureInfo.InvariantCulture) + " of a block or more");
                lines.Add(
                    "    sun        " + Number(preset.Light.Yaw) + " degrees round, "
                    + Number(preset.Light.Pitch) + " down, at " + Number(preset.Light.Intensity));
                lines.Add(
                    "    water      "
                    + (settings.WaterLevel < 0 ? "none" : "level " + Letter(settings.WaterLevel) + " and below"));
                lines.Add(
                    "    chances    grove " + Number(settings.GroveChance)
                    + ", peak " + Number(settings.PeakChance)
                    + ", border grove " + Number(settings.BorderGroveChance)
                    + ", prop " + Number(settings.PropChance)
                    + ", camp " + Number(settings.CampChance)
                    + ", mound " + Number(settings.RidgeChance));
                lines.Add("    rim        falls " + Number(settings.RimDrop) + " metres");
                lines.Add(string.Empty);
            }

            File.WriteAllLines(Path.Combine(outDir, "presets.txt"), lines, new UTF8Encoding(false));
        }

        /// <summary>
        /// How often the ground changes height on a board, counted off the map
        /// the picture was drawn from.
        /// </summary>
        private static void Census(HexMap map, out int falls, out int blocks, out int lowest, out int highest)
        {
            falls = 0;
            blocks = 0;
            lowest = int.MaxValue;
            highest = int.MinValue;

            for (int row = 0; row < map.Height; row++)
            {
                for (int column = 0; column < map.Width; column++)
                {
                    int level = map.LevelAt(column, row);

                    lowest = Math.Min(lowest, level);
                    highest = Math.Max(highest, level);

                    Hex hex = Hex.FromOddRowOffset(column, row);

                    for (int direction = 0; direction < Hex.DirectionCount; direction++)
                    {
                        Hex.ToOddRowOffset(hex.Neighbour(direction), out int other, out int otherRow);

                        if (other < 0 || other >= map.Width || otherRow < 0 || otherRow >= map.Height)
                        {
                            continue;
                        }

                        int drop = level - map.LevelAt(other, otherRow);

                        if (drop > 0)
                        {
                            falls++;
                        }

                        if (drop >= 2)
                        {
                            blocks++;
                        }
                    }
                }
            }
        }

        private static string Letter(int level) => ((char)('a' + level)).ToString();

        private static string Number(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

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

        private static int ParseInt(string value, int fallback) =>
            string.IsNullOrWhiteSpace(value)
                ? fallback
                : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
