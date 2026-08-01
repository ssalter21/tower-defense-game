using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spikes.Playables.Editor
{
    /// <summary>
    /// Renders the rig at a series of phases and writes them out as PNGs, so the
    /// scrub can be checked without launching anything. Proves the pose really is
    /// a function of phase, in pixels rather than in assertions.
    ///
    ///   Unity.exe -batchmode -quit -projectPath client \
    ///             -executeMethod Spikes.Playables.Editor.ScrubStripCapture.Run
    /// </summary>
    public static class ScrubStripCapture
    {
        private const string CharacterPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";
        private const string BankPath = "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx";
        private const string ClipName = "Walking_A";
        private const int Size = 320;

        public static void Run()
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(BankPath)
                .OfType<AnimationClip>().FirstOrDefault(c => c.name == ClipName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);

            if (clip == null || prefab == null)
            {
                Debug.LogError("ScrubStripCapture: missing clip or character");
                return;
            }

            var rig = Object.Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 150f, 0f));
            var animator = rig.GetComponent<Animator>();
            if (animator == null) animator = rig.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var view = rig.AddComponent<SimDrivenAnimator>();
            view.Build(animator, clip);

            var camGo = new GameObject("CaptureCam");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.20f);
            camGo.transform.position = new Vector3(4f, 3.2f, -4f);
            camGo.transform.LookAt(new Vector3(0f, 0.9f, 0f));

            var lightGo = new GameObject("Key");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);

            var outDir = Path.Combine(Directory.GetCurrentDirectory(), "..", "scrub-strip");
            Directory.CreateDirectory(outDir);

            var rt = new RenderTexture(Size, Size, 24);
            cam.targetTexture = rt;

            var bones = rig.GetComponentsInChildren<Transform>(true);
            var legL = bones.First(b => b.name == "upperleg.l");
            var legR = bones.First(b => b.name == "upperleg.r");
            float Pitch(Transform t)
            {
                var x = t.localRotation.eulerAngles.x;
                return x > 180f ? x - 360f : x;
            }

            var phases = new[] { 0.0f, 0.125f, 0.25f, 0.3125f, 0.375f, 0.5f, 0.625f, 0.75f, 0.8125f, 0.875f };
            foreach (var p in phases)
            {
                view.SampleSingle(0, p, clip.length);

                // Record the pose that this specific frame is being rendered from,
                // so a stale render can be told apart from a wrong sample.
                Debug.Log($"[strip] phase {p:0.0000}  L {Pitch(legL):0.00}  R {Pitch(legR):0.00}");
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(Size, Size, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
                tex.Apply();
                RenderTexture.active = null;

                var file = Path.Combine(outDir, $"phase-{p:0.000}.png");
                File.WriteAllBytes(file, tex.EncodeToPNG());
                Debug.Log($"[strip] wrote {file}");
                Object.DestroyImmediate(tex);
            }

            cam.targetTexture = null;
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(rig);
            Object.DestroyImmediate(camGo);
            Object.DestroyImmediate(lightGo);
            Debug.Log($"[strip] done -> {Path.GetFullPath(outDir)}");
        }
    }
}
