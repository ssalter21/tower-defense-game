using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Sim;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders every unit in the roster holding what it holds, one PNG each.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> <see cref="MatchFrameCapture"/> photographs the
    /// recorded match, and the recorded match is one defense:
    /// <c>content/defense.txt</c> puts four archers and two mages on the board
    /// and nothing else. A Soldier, a Skeleton and a Skeleton Warrior are all
    /// units the game can draw and that defense never contains, so no frame of
    /// that match will ever show one — which is exactly how the Soldier's sword
    /// went unreviewed on 14 August 2026. Changing the defense to get a
    /// photograph would re-freeze every committed golden for the sake of a
    /// picture; this renders the roster instead.
    /// </para>
    /// <para>
    /// <b>It draws through the real views.</b> A tower goes through
    /// <see cref="TowerView"/> and a creep through <see cref="CreepView"/>,
    /// with the art the scene is wired from, so what comes out is what the
    /// match draws and not a second approximation of it. Nothing here is an
    /// oracle: no test compares these, they are for looking at.
    /// </para>
    /// <para>
    /// <c>-batchmode -executeMethod</c>, so it needs no editor session and no
    /// bridge — and therefore needs the editor CLOSED, because batchmode takes
    /// the project lock.
    /// </para>
    /// </remarks>
    public static class ArmedRosterCapture
    {
        private const string OutDirArgument = "-rosterOutDir";

        private const string WidthArgument = "-rosterWidth";

        private const string DefaultOutDir = "docs/frames/roster";

        /// <summary>Portrait, because a unit is taller than it is wide.</summary>
        private const float FrameAspect = 3f / 4f;

        /// <summary>
        /// Three-quarter front: the angle a unit is most often seen from, and
        /// the one that shows both hands at once.
        /// </summary>
        /// <remarks>
        /// Past 180, because these models face +Z and a camera standing on -Z
        /// looking back at them photographs the backs of their heads. Which is
        /// what the first run of this tool did, and a bow's orientation cannot
        /// be judged from behind.
        /// </remarks>
        private const float ViewYawDegrees = 215f;

        private const float ViewPitchDegrees = 12f;

        /// <summary>How far back to stand, as a multiple of the unit's height.</summary>
        private const float DistanceInHeights = 2.2f;

        [MenuItem("Tools/Capture the armed roster")]
        public static void Run()
        {
            string outDir = BatchArguments.Value(OutDirArgument) ?? DefaultOutDir;
            int width = ParseInt(BatchArguments.Value(WidthArgument), 700);
            int height = Mathf.Max(1, Mathf.RoundToInt(width / FrameAspect));

            Directory.CreateDirectory(outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.32f, 0.34f, 0.38f, 1f);

            MatchArt art = MatchSceneBuilder.Art();
            UnitTypeTable types = StreamingContent.ReadUnitTypes();

            var host = new GameObject("RosterCaptureRoot");
            var written = new List<string>();

            try
            {
                Camera camera = MakeCamera(host.transform, height, width);
                MakeSun(host.transform);

                foreach (UnitType type in types.Types)
                {
                    string path = Path.Combine(
                        outDir,
                        "unit-" + type.Id.ToString("00", CultureInfo.InvariantCulture)
                        + "-" + type.Label + ".png");

                    File.WriteAllBytes(path, Shoot(host.transform, camera, art, type, width, height));
                    written.Add(path);
                }
            }
            finally
            {
                Object.DestroyImmediate(host);
            }

            foreach (string path in written)
            {
                Debug.Log("[roster] wrote " + path);
            }

            Debug.Log("[roster] " + written.Count + " units drawn into " + outDir);
        }

        /// <summary>
        /// Builds one unit through its real view, frames it, and renders it.
        /// The unit is destroyed before the next one, so nothing shares a frame.
        /// </summary>
        private static byte[] Shoot(
            Transform parent, Camera camera, MatchArt art, UnitType type, int width, int height)
        {
            var stand = new GameObject(type.Label);
            stand.transform.SetParent(parent, worldPositionStays: false);

            try
            {
                UnitArt unit = art.ArtFor(type.Id);

                // Through the shipped views, both of them, because a capture
                // that instantiated the model itself would not be a picture of
                // what the match draws -- and attaching the weapons is the half
                // being reviewed.
                // Posed, not left in the bind pose. A character imports with
                // its arms straight out, and a weapon parented to a hand that
                // is pointing sideways tells you nothing about how the unit
                // stands holding it -- which is how a review of the first run
                // of this tool got no further than "hard to say".
                if (type.Role == UnitRole.Placed)
                {
                    var tower = stand.AddComponent<TowerView>();

                    if (unit.IsPosed)
                    {
                        tower.BuildAnimated(type.Id, type, unit, Quaternion.identity);
                        tower.Pose(TowerState.Idle, 0, null);
                    }
                    else
                    {
                        tower.BuildStatic(type.Id, type, unit, Quaternion.identity);
                    }
                }
                else
                {
                    CreepView creep = stand.AddComponent<CreepView>();
                    creep.Build(unit, art.CreepWalkClip, art.CreepDeathClip);

                    // A quarter through the walk cycle: mid-stride, both arms
                    // away from the body, which is where a carried shield stops
                    // being edge-on to everything.
                    creep.Pose(
                        Vector3.zero,
                        Quaternion.identity,
                        MatchTuning.HexesPerWalkCycle * 0.25f,
                        CreepState.Walking,
                        0f);
                }

                Frame(camera, Measured(stand));

                return Grab(camera, width, height).EncodeToPNG();
            }
            finally
            {
                Object.DestroyImmediate(stand);
            }
        }

        /// <summary>The world bounds of everything drawn under an object.</summary>
        private static Bounds Measured(GameObject drawn)
        {
            Renderer[] renderers = drawn.GetComponentsInChildren<Renderer>(true);

            if (renderers.Length == 0)
            {
                return new Bounds(drawn.transform.position, Vector3.one);
            }

            Bounds bounds = renderers[0].bounds;

            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        /// <summary>
        /// Stands the camera off the unit's own measured size, so a creep drawn
        /// at a half and a tower drawn at one and a half both fill the frame.
        /// A fixed distance would photograph the scale difference instead of
        /// the weapon, and the scale difference already has its own test.
        /// </summary>
        private static void Frame(Camera camera, Bounds subject)
        {
            float reach = Mathf.Max(subject.size.magnitude, 0.01f);

            Quaternion look = Quaternion.Euler(ViewPitchDegrees, ViewYawDegrees, 0f);

            camera.transform.position = subject.center - (look * Vector3.forward * (reach * DistanceInHeights));
            camera.transform.rotation = look;
        }

        private static Camera MakeCamera(Transform parent, int height, int width)
        {
            var holder = new GameObject("Camera");
            holder.transform.SetParent(parent, worldPositionStays: false);

            Camera camera = holder.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneFraming.BackgroundColor;
            camera.fieldOfView = SceneFraming.CameraFieldOfViewDegrees;
            camera.nearClipPlane = 0.01f;
            camera.aspect = FrameAspect;

            return camera;
        }

        /// <summary>
        /// The same sun the match uses, so a weapon's shading here is the
        /// shading it has on the board.
        /// </summary>
        private static void MakeSun(Transform parent)
        {
            var holder = new GameObject("Sun");
            holder.transform.SetParent(parent, worldPositionStays: false);

            Light sun = holder.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = SceneFraming.SunColor;
            sun.intensity = SceneFraming.SunIntensity;
            sun.shadowStrength = SceneFraming.SunShadowStrength;
            holder.transform.rotation = Quaternion.Euler(
                SceneFraming.SunPitchDegrees, SceneFraming.SunYawDegrees, 0f);
        }

        private static Texture2D Grab(Camera camera, int width, int height)
        {
            var target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;

            // A warm-up render, thrown away, for the same reason
            // MatchFrameCapture does one: the first render in a fresh batchmode
            // editor lands before shaders and textures have resolved.
            camera.Render();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var frame = new Texture2D(width, height, TextureFormat.RGB24, false);
            frame.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            frame.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            Object.DestroyImmediate(target);

            return frame;
        }

        private static int ParseInt(string value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;
    }
}
