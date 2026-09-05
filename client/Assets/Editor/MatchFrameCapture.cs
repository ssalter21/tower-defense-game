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
    /// real floor, the real <see cref="OrbitCameraRig"/> pointed where the
    /// arguments say, and the real <see cref="MatchView"/> stepping the real
    /// simulation. A capture path that built its own approximation of the scene
    /// would be a picture of something this project does not ship.
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

        /// <summary>Which heading to grab from, in degrees of yaw.</summary>
        public const string YawArgument = "-matchFrameYaw";

        /// <summary>
        /// How far the camera sits from the middle of the floor, in metres.
        /// Zero, the default, means the distance the whole floor fits at.
        /// </summary>
        public const string DistanceArgument = "-matchFrameDistance";

        /// <summary>How wide each frame is, in pixels.</summary>
        public const string WidthArgument = "-matchFrameWidth";

        /// <summary>
        /// The shape of a frame. Sixteen by nine, the same shape the playback
        /// bar lays itself out for, because these are pictures of what a player
        /// sees.
        /// <para>
        /// <b>The board this was reasoned about is gone.</b> The justification
        /// was that a square frame fits the old 15-by-9, 47-hex corridor across
        /// its width and then leaves half its height empty. The committed map
        /// is 19 by 13 now, which is far closer to square, so whether sixteen
        /// by nine still frames it -- at what <see cref="DistanceArgument"/> --
        /// is unmeasured. Nothing here was changed on a guess; run the capture
        /// and look before touching this number.
        /// </para>
        /// </summary>
        private const float FrameAspect = 16f / 9f;

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
            string outDir = BatchArguments.Value(DefaultOutDirArgument)
                ?? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "docs", "frames"));

            int[] ticks = ParseTicks(BatchArguments.Value(TicksArgument)) ?? DefaultTicks;
            float yaw = ParseFloat(BatchArguments.Value(YawArgument), SceneFraming.CameraDefaultYawDegrees);
            float distance = ParseFloat(BatchArguments.Value(DistanceArgument), 0f);
            int width = ParseInt(BatchArguments.Value(WidthArgument), 1280);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            var host = new GameObject("CaptureRoot");
            var written = new List<string>();

            try
            {
                // The recorded match, exactly as the player plays it. A capture
                // of some other seed would be a picture of a match nobody can
                // scrub to: the tick in each filename is only meaningful because
                // it is a tick of the run content/landmarks.txt was made from.
                ReplayBundle record = StreamingContent.ReadRecordedMatch();

                var root = host.AddComponent<MatchRoot>();
                // The tiles the scene carries. Without this the capture draws
                // the blockout, which on a board with tiers is a flat hexagon
                // with no sides and shows the background through every step.
                root.Build(record.Map, MatchSceneBuilder.Tiles(), MatchSceneBuilder.Scenery());

                MatchView view = root.BeginMatch(
                    StreamingContent.ReadUnitTypes(),
                    StreamingContent.ReadRuleset(),
                    record,
                    art: LoadArt());

                Camera camera = root.CameraRig.Camera;
                camera.backgroundColor = SceneFraming.BackgroundColor;

                // A camera built against whatever aspect a headless editor
                // reports would be framed for a window that is never rendered.
                // Fixing the aspect to the frame's own and re-framing against it
                // is what puts both ends of the corridor in the picture.
                camera.aspect = FrameAspect;
                root.CameraRig.Reframe(root.Floor.WorldBounds);

                root.CameraRig.PointAt(
                    yaw,
                    SceneFraming.CameraDefaultPitchDegrees,
                    distance > 0f ? distance : root.CameraRig.FramedDistance);

                // A warm-up render, thrown away. The first render in a fresh
                // batchmode editor happens before shaders and textures have
                // finished resolving, and it comes out looking like an import
                // failure -- the floor drawn in somebody else's atlas. Measured
                // here: the first captured frame was a rainbow checkerboard and
                // every later one was correct. Rendering once and discarding it
                // is the whole fix, and it costs one frame.
                UnityEngine.Object.DestroyImmediate(Grab(camera, 32, 32));

                int[] wanted = ticks.OrderBy(t => t).ToArray();
                int next = 0;

                while (next < wanted.Length && !view.IsFinished)
                {
                    view.StepOneTick();

                    // Drawn on every tick and not only on the ones that are
                    // kept. Where a tower is pointing and where its shot leaves
                    // from are both read off the pose it was last drawn in — a
                    // flash leaves the staff tip the tower is holding, and the
                    // staff is wherever the last Draw put its arm. Stepping a
                    // thousand ticks without drawing and then photographing the
                    // next one is a picture of a match whose towers have never
                    // moved, with the effects of the tick that was kept hanging
                    // off a rig still standing in its bind pose. It also makes
                    // the picture a function of the tick alone rather than of
                    // which other ticks were asked for in the same run.
                    //
                    // It costs one Draw per stepped tick rather than one per
                    // frame kept -- 2,700 of them to reach the late frames
                    // instead of six -- which is the work the game itself does
                    // every frame and is not what a capture spends its time on.
                    view.Draw(1f);

                    if (view.Current.Tick < wanted[next])
                    {
                        continue;
                    }

                    string path = Path.Combine(
                        outDir,
                        "match-tick-" + view.Current.Tick.ToString("D4", CultureInfo.InvariantCulture) + ".png");

                    File.WriteAllBytes(path, Grab(camera, width, height).EncodeToPNG());
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
        /// The art, from the scene builder's own tables rather than off the
        /// generated scene, so the capture works on a checkout whose scene has
        /// not been rebuilt yet.
        /// </summary>
        private static MatchArt LoadArt() => MatchSceneBuilder.Art();

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

        private static float ParseFloat(string value, float fallback) =>
            string.IsNullOrWhiteSpace(value)
                ? fallback
                : float.Parse(value, CultureInfo.InvariantCulture);
    }
}
