using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders the committed board once per scenery preset, so a person can
    /// choose a dressing by looking at six pictures instead of by reading six
    /// sets of numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every frame is the same board.</b> The map, the corridor and the tier
    /// of every cell come from <c>content/map.txt</c> and no preset touches any
    /// of it — so anything that differs between two of these pictures is
    /// dressing, and nothing that differs is the playfield. That is the whole
    /// reason the comparison is worth making.
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
    /// <b>Two angles each, and the second one is the point.</b> The pack's own
    /// renders read as terrain from a low camera and as a floor plan from
    /// overhead, so a ledge judged only from the shipped pitch is judged from
    /// the angle least able to show it.
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

        /// <summary>
        /// The two headings each preset is drawn from: the shipped framing, and
        /// a lower, turned one that can actually see a cliff face.
        /// </summary>
        private static readonly (string Suffix, float Yaw, float Pitch)[] Angles =
        {
            ("high", SceneFraming.CameraDefaultYawDegrees, SceneFraming.CameraDefaultPitchDegrees),
            ("low", 34f, 19f),
        };

        [MenuItem("Tools/Capture scenery prototypes")]
        public static void CaptureDefault() => Run();

        public static void Run()
        {
            string outDir = BatchArguments.Value(OutDirArgument)
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "prototypes", "scenery"));

            int width = ParseInt(BatchArguments.Value(WidthArgument), 1600);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            IReadOnlyList<SceneryPresets.Preset> presets = Chosen(BatchArguments.Value(NamesArgument));

            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            HexMap map = StreamingContent.ReadRecordedMatch().Map;
            TileSet tiles = MatchSceneBuilder.Tiles();
            SceneryModels scenery = MatchSceneBuilder.Scenery();

            var written = new List<string>();

            foreach (SceneryPresets.Preset preset in presets)
            {
                var host = new GameObject("PrototypeRoot");

                try
                {
                    var root = host.AddComponent<MatchRoot>();

                    root.Build(map, tiles, scenery, preset.Settings);

                    Camera camera = root.CameraRig.Camera;
                    camera.backgroundColor = SceneFraming.BackgroundColor;
                    camera.aspect = FrameAspect;
                    root.CameraRig.Reframe(root.Floor.WorldBounds);

                    // The throwaway first render, for the reason
                    // MatchFrameCapture gives: the first frame out of a fresh
                    // batchmode editor is drawn before the shaders resolve and
                    // comes out looking like a failed import.
                    if (written.Count == 0)
                    {
                        root.CameraRig.PointAt(
                            SceneFraming.CameraDefaultYawDegrees,
                            SceneFraming.CameraDefaultPitchDegrees,
                            root.CameraRig.FramedDistance);

                        UnityEngine.Object.DestroyImmediate(Grab(camera, 32, 32));
                    }

                    foreach ((string suffix, float yaw, float pitch) in Angles)
                    {
                        root.CameraRig.PointAt(yaw, pitch, root.CameraRig.FramedDistance);

                        string path = Path.Combine(outDir, "board-" + preset.Name + "-" + suffix + ".png");

                        File.WriteAllBytes(path, Grab(camera, width, height).EncodeToPNG());
                        written.Add(path);

                        Debug.Log("PrototypeCapture: " + preset.Name + " " + suffix + " -> " + path);
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(host);
                }
            }

            if (written.Count == 0)
            {
                throw new InvalidDataException(
                    "No prototype frames were captured. A capture tool that silently writes nothing "
                    + "is worse than one that fails, because the frames it did not write look exactly "
                    + "like frames nobody asked for.");
            }

            WriteIndex(outDir, presets);

            Debug.Log("PrototypeCapture: wrote " + written.Count + " frames to " + outDir);
        }

        /// <summary>
        /// The presets named on the command line, or all of them.
        /// </summary>
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
        /// matched back to the numbers from memory.
        /// </summary>
        private static void WriteIndex(string outDir, IReadOnlyList<SceneryPresets.Preset> presets)
        {
            var lines = new List<string>
            {
                "# The scenery prototypes, and what each is trying to be.",
                "#",
                "# Generated by tools/capture-prototypes.ps1. Every frame is the same board:",
                "# content/map.txt, its corridor and its tiers, dressed six ways. Nothing here",
                "# changes the match -- the result, the landmark table and the per-tick hash are",
                "# identical under all of them.",
                string.Empty,
            };

            foreach (SceneryPresets.Preset preset in presets)
            {
                DressingSettings s = preset.Settings;

                lines.Add(preset.Name);
                lines.Add("    reference  " + preset.Reference);
                lines.Add("    intent     " + preset.Intent);
                lines.Add(
                    "    ledges     " + s.ApronCount.ToString(CultureInfo.InvariantCulture)
                    + " at spread " + s.ApronSpread.ToString("0.00", CultureInfo.InvariantCulture));
                lines.Add(
                    "    chances    grove " + s.GroveChance.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", peak " + s.PeakChance.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", border grove " + s.BorderGroveChance.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", prop " + s.PropChance.ToString("0.00", CultureInfo.InvariantCulture)
                    + ", camp " + s.CampChance.ToString("0.00", CultureInfo.InvariantCulture));
                lines.Add(string.Empty);
            }

            File.WriteAllLines(Path.Combine(outDir, "presets.txt"), lines);
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

        private static int ParseInt(string value, int fallback) =>
            string.IsNullOrWhiteSpace(value)
                ? fallback
                : int.Parse(value, CultureInfo.InvariantCulture);
    }
}
