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
        /// A unit table to draw the match against instead of the shipped one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>For photographing something no shipped row does.</b> Every row of
        /// <c>content/units.txt</c> authors no bubble at all, so nothing in the
        /// recorded match is ever slowed, hastened, cursed or shielded — and a
        /// frame of it is a frame of none of that happening. A fixture table
        /// with the same ids and a bubble on two of them plays the same board,
        /// the same defense, the same wave and the same seed, with the effects
        /// switched on.
        /// </para>
        /// <para>
        /// <b>The record's gate is skipped and has to be.</b> A bundle stamps
        /// the content hash it was made against, so a table that is not the
        /// shipped one is refused by name — correctly, and that refusal is what
        /// keeps the ordinary path honest. What this reads out of the record
        /// instead is the four things a match is made of: the board, the
        /// defense, the wave and the seed. The frames it writes are named after
        /// the fixture rather than after the match, because the tick in a
        /// <c>match-tick-</c> filename is a claim about the run
        /// <c>content/landmarks.txt</c> was made from and this is not that run.
        /// </para>
        /// </remarks>
        public const string UnitsArgument = "-matchFrameUnits";

        /// <summary>
        /// A defense to stand on the recorded board instead of the one the
        /// record carries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>For photographing rows the recorded match does not stand.</b> The
        /// recorded defense is six towers of two types, computed by a bot's own
        /// rule, and a frame of it can only ever show those two rows firing. A
        /// line whose look somebody has to sign is a line somebody has to see
        /// swing, and there is no way to reach it through the record.
        /// </para>
        /// <para>
        /// <b>It is a defense and not a board.</b> The map, the wave and the
        /// seed still come out of the record, so the corridor and the bodies
        /// walking it are the ones every other frame shows and the only thing
        /// that has moved is what is standing beside them. The loader still
        /// refuses a tower off the grid, a tower inside the corridor and a
        /// tower whose range cannot reach the route — so a melee row has to be
        /// put next to the corridor rather than wherever an archer stood.
        /// </para>
        /// <para>
        /// Frames from such a run are named after the defense for the same
        /// reason a fixture roster's are named after the roster: the tick in a
        /// <c>match-tick-</c> filename is a claim about the run
        /// <c>content/landmarks.txt</c> was made from, and this is not that
        /// run.
        /// </para>
        /// </remarks>
        public const string DefenseArgument = "-matchFrameDefense";

        /// <summary>
        /// A wave to send down the recorded board instead of the one the record
        /// carries.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>For photographing rows the recorded wave does not send.</b> The
        /// record's wave releases Minions and Skeleton Scouts, and neither of
        /// those two rows carries an aura or a pool — so the creep rows that do
        /// never walk onto the board a frame is taken of, whatever roster or
        /// defense is standing.
        /// </para>
        /// <para>
        /// <b>It is a wave and not a roster.</b> The rows it sends are the
        /// shipped rows out of <c>content/units.txt</c>, with their own
        /// authored auras and their own authored pools, so a frame of one is a
        /// frame of the row as it ships rather than of a row invented to have
        /// something to draw. That is the whole difference from
        /// <see cref="UnitsArgument"/>, whose fixture table goes stale the
        /// moment the roster moves.
        /// </para>
        /// <para>
        /// Frames from such a run are named after the wave, for the reason a
        /// fixture defense's are named after the defense.
        /// </para>
        /// </remarks>
        public const string WaveArgument = "-matchFrameWave";

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

            string units = BatchArguments.Value(UnitsArgument);
            string defense = BatchArguments.Value(DefenseArgument);
            string wave = BatchArguments.Value(WaveArgument);
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

                MatchView view = BeginMatch(root, record, units, defense, wave);

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
                        NameOf(units, defense, wave)
                        + view.Current.Tick.ToString("D4", CultureInfo.InvariantCulture)
                        + ".png");

                    File.WriteAllBytes(path, Grab(camera, width, height).EncodeToPNG());
                    written.Add(path);

                    // The running effect counts go in the line, because they
                    // are what a person hunting a frame of a capstone is
                    // hunting. A signature goes off on one tick and is gone
                    // eight later, so finding one by opening pictures means
                    // opening most of them; over consecutive ticks these
                    // numbers say which tick to ask for.
                    Debug.Log(
                        "MatchFrameCapture: tick " + view.Current.Tick
                        + " -- " + view.Current.Creeps.Count + " creeps, "
                        + view.Current.Projectiles.Count + " shells, effects "
                        + view.Decorations.SlowRingsDrawn + " slow / "
                        + view.Decorations.ShocksDrawn + " shock / "
                        + view.Decorations.GlowsDrawn + " glow / "
                        + view.Decorations.BurstsDrawn + " burst / "
                        + view.Decorations.LongShotsDrawn + " long shot / "
                        + view.Decorations.KnivesDrawn + " knife / "
                        + view.Decorations.BoltsDrawn + " bolt / "
                        + view.Decorations.LightsDrawn + " light / "
                        + view.Decorations.RootsDrawn + " roots / "
                        + view.Decorations.StripsDrawn + " strip / "
                        + view.Decorations.HasteRingsDrawn + " haste / "
                        + view.Decorations.WardDomesDrawn + " ward / "
                        + view.Decorations.HexPlatesDrawn + " hex / "
                        + view.Decorations.FrostSpikesDrawn + " frost -> " + path);

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

        /// <summary>
        /// The match to photograph: the recorded one, or the same board and
        /// seed played against the roster, the defense or the wave an argument
        /// named.
        /// </summary>
        private static MatchView BeginMatch(
            MatchRoot root, ReplayBundle record, string units, string defense, string wave)
        {
            Ruleset rules = StreamingContent.ReadRuleset();

            if (string.IsNullOrWhiteSpace(units)
                && string.IsNullOrWhiteSpace(defense)
                && string.IsNullOrWhiteSpace(wave))
            {
                return root.BeginMatch(StreamingContent.ReadUnitTypes(), rules, record, art: LoadArt());
            }

            UnitTypeTable types = string.IsNullOrWhiteSpace(units)
                ? StreamingContent.ReadUnitTypes()
                : UnitTypeTable.ParseUtf8(units, Read(units, UnitsArgument, "unit table"));

            TowerLayout layout = string.IsNullOrWhiteSpace(defense)
                ? record.Ghost.ToLayout(types)
                : TowerLayout.ParseUtf8(defense, Read(defense, DefenseArgument, "defense"), types);

            WaveScript script = string.IsNullOrWhiteSpace(wave)
                ? record.Wave.ToScript(types)
                : WaveScript.ParseUtf8(wave, Read(wave, WaveArgument, "wave"), types);

            return root.BeginMatch(types, rules, layout, script, record.Seed, LoadArt());
        }

        /// <summary>
        /// The bytes of a file an argument named, or a throw saying which
        /// argument named a file that is not there.
        /// </summary>
        /// <remarks>
        /// Loud rather than a fall back to the shipped content, which would be
        /// a picture of the wrong match written under the filename of the right
        /// one.
        /// </remarks>
        private static byte[] Read(string path, string argument, string what)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    "No " + what + " at " + path + ". " + argument + " names a " + what + " to draw the "
                    + "recorded match against instead of the shipped one, and a capture that quietly "
                    + "fell back would be a picture of the wrong match.",
                    path);
            }

            return File.ReadAllBytes(path);
        }

        /// <summary>
        /// What a frame is called before its tick. The recorded match's frames
        /// are <c>match-tick-</c>, because that tick is a tick of the run the
        /// landmark table was made from; a fixture roster's, a fixture
        /// defense's or a fixture wave's are named after the fixture, because
        /// they are ticks of a match nobody can scrub to. The roster wins over
        /// the other two, because a roster changes what every row on the board
        /// is; the wave wins over the defense, because what is walking is the
        /// larger change to a picture of a corridor.
        /// </summary>
        private static string NameOf(string units, string defense, string wave)
        {
            if (!string.IsNullOrWhiteSpace(units))
            {
                return Path.GetFileNameWithoutExtension(units) + "-tick-";
            }

            if (!string.IsNullOrWhiteSpace(wave))
            {
                return Path.GetFileNameWithoutExtension(wave) + "-tick-";
            }

            if (!string.IsNullOrWhiteSpace(defense))
            {
                return Path.GetFileNameWithoutExtension(defense) + "-tick-";
            }

            return "match-tick-";
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
