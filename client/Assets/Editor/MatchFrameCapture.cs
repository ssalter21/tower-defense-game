using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders the match at chosen ticks to PNGs, so the thing this effort is
    /// building can be looked at without anybody opening the editor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>These frames are documentation, not an oracle.</b> Nothing compares
    /// them to anything, and nothing fails if they change — that decision was
    /// made on this project after two frames whose bones were definitively
    /// swapped rendered pixel-identical, reproducibly. What catches a broken
    /// view is the assertions in <c>MatchViewTests</c> and the sit-down
    /// landmark table; what these are for is letting a human see the match at a
    /// named tick without a mouse.
    /// </para>
    /// <para>
    /// It draws through the real thing: the real <see cref="MatchRoot"/>, the
    /// real floor, the real <see cref="IsometricCameraRig"/> at its real snaps,
    /// and the real <see cref="MatchView"/> stepping the real simulation. A
    /// capture path that built its own approximation of the scene would be a
    /// picture of something this project does not ship.
    /// </para>
    /// <para>
    /// Runs headless, from a shell, with no editor session and nobody at a
    /// keyboard — <c>tools/capture-match-frames.ps1</c>.
    /// </para>
    /// </remarks>
    public static class MatchFrameCapture
    {
        /// <summary>Where the frames land, relative to the repository root.</summary>
        public const string DefaultOutDirArgument = "-matchFrameOut";

        /// <summary>Which ticks to grab, comma separated.</summary>
        public const string TicksArgument = "-matchFrameTicks";

        /// <summary>Which camera snap to grab from.</summary>
        public const string SnapArgument = "-matchFrameSnap";

        /// <summary>How big each frame is, in pixels.</summary>
        public const string SizeArgument = "-matchFrameSize";

        /// <summary>
        /// The ticks worth looking at, if none are named.
        /// </summary>
        /// <remarks>
        /// Chosen to show one of each thing the ticket is about rather than at
        /// even intervals: the first creeps walking, towers engaged, the first
        /// overtake the committed landmark table names, and the tail of the
        /// match. Even intervals would mostly show an empty corridor, because
        /// the wave is deliberately not uniform.
        /// </remarks>
        private static readonly int[] DefaultTicks = { 60, 200, 366, 700, 900, 1400 };

        [MenuItem("Tools/Capture match frames")]
        public static void CaptureDefault() => Run();

        public static void Run()
        {
            string outDir = ArgumentValue(DefaultOutDirArgument)
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "frames"));

            int[] ticks = ParseTicks(ArgumentValue(TicksArgument)) ?? DefaultTicks;
            int snap = ParseInt(ArgumentValue(SnapArgument), 0);
            int size = ParseInt(ArgumentValue(SizeArgument), 720);

            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            var host = new GameObject("CaptureRoot");
            var written = new List<string>();

            try
            {
                var root = host.AddComponent<MatchRoot>();
                root.Build(StreamingContent.ReadMap());

                UnitTypeTable types = StreamingContent.ReadUnitTypes();

                MatchView view = root.BeginMatch(
                    types,
                    StreamingContent.ReadDefense(types),
                    StreamingContent.ReadWave(types),
                    seed: 1,
                    art: LoadArt());

                Camera camera = root.CameraRig.Camera;
                camera.backgroundColor = SceneFraming.BackgroundColor;
                root.CameraRig.SnapTo(snap);

                // A warm-up render, thrown away. The first render in a fresh
                // batchmode editor happens before shaders and textures have
                // finished resolving, and it comes out looking like an import
                // failure -- the floor drawn in somebody else's atlas. Measured
                // here: the first captured frame was a rainbow checkerboard and
                // every later one was correct. Rendering once and discarding it
                // is the whole fix, and it costs one frame.
                UnityEngine.Object.DestroyImmediate(Grab(camera, 32));

                int[] wanted = ticks.OrderBy(t => t).ToArray();
                int next = 0;

                while (next < wanted.Length && !view.IsFinished)
                {
                    view.StepOneTick();

                    if (view.Current.Tick < wanted[next])
                    {
                        continue;
                    }

                    view.Draw(1f);

                    string path = Path.Combine(
                        outDir,
                        "match-tick-" + view.Current.Tick.ToString("D4", CultureInfo.InvariantCulture) + ".png");

                    File.WriteAllBytes(path, Grab(camera, size).EncodeToPNG());
                    written.Add(path);

                    Debug.Log(
                        "MatchFrameCapture: tick " + view.Current.Tick
                        + " -- " + view.Current.Creeps.Count + " creeps, "
                        + view.Current.Projectiles.Count + " shells -> " + path);

                    next++;
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }

            if (written.Count == 0)
            {
                throw new InvalidDataException(
                    "No frames were captured. A capture tool that silently writes nothing is worse than "
                    + "one that fails, because the frames it did not write look exactly like frames "
                    + "nobody asked for.");
            }

            Debug.Log("MatchFrameCapture: wrote " + written.Count + " frames to " + outDir);
        }

        /// <summary>
        /// The art, from the same paths the scene builder wires. Loaded rather
        /// than read off a scene so the capture works on a project whose scene
        /// has not been regenerated yet.
        /// </summary>
        private static MatchArt LoadArt() =>
            MatchArt.Of(
                LoadModel("Assets/Art/Characters/Skeleton_Warrior.fbx"),
                LoadClip("Walking_A"),
                LoadClip("Death_A"),
                LoadModel("Assets/Art/Characters/Ranger.fbx"),
                LoadModel("Assets/Art/Weapons/bow_withString.fbx"),
                LoadClip("Ranged_Bow_Idle"),
                LoadClip("Ranged_Bow_Draw"),
                LoadClip("Ranged_Bow_Release"),
                LoadModel("Assets/Art/Buildings/building_tower_A_blue.fbx"));

        private static GameObject LoadModel(string path)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                throw new IOException("Nothing imported at " + path + ".");
            }

            return model;
        }

        private static AnimationClip LoadClip(string name)
        {
            string[] banks =
            {
                "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx",
                "Assets/Art/Animations/Rig_Medium_General.fbx",
                "Assets/Art/Animations/Rig_Medium_CombatRanged.fbx",
            };

            foreach (string bank in banks)
            {
                AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(bank)
                    .OfType<AnimationClip>()
                    .FirstOrDefault(c => c.name == name && !c.name.StartsWith("__preview__"));

                if (clip != null)
                {
                    return clip;
                }
            }

            throw new IOException("No clip called '" + name + "' in any of the three banks.");
        }

        private static Texture2D Grab(Camera camera, int size)
        {
            var target = new RenderTexture(size, size, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var frame = new Texture2D(size, size, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            frame.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);

            return frame;
        }

        private static int[] ParseTicks(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value
                .Split(',')
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .Select(part => int.Parse(part, CultureInfo.InvariantCulture))
                .ToArray();
        }

        private static int ParseInt(string value, int fallback) =>
            string.IsNullOrWhiteSpace(value)
                ? fallback
                : int.Parse(value, CultureInfo.InvariantCulture);

        private static string ArgumentValue(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], flag, StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}
