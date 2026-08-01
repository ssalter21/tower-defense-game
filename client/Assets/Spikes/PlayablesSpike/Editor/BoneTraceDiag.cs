using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spikes.Playables.Editor
{
    /// <summary>
    /// Diagnostic: dumps the actual bone motion across a clip, so "the render at
    /// phase 0.25 looks like the render at phase 0.75" can be settled on transforms
    /// rather than on pixels.
    /// </summary>
    public static class BoneTraceDiag
    {
        public static void Run()
        {
            const string bank = "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx";
            const string clipName = "Walking_A";

            var clip = AssetDatabase.LoadAllAssetsAtPath(bank)
                .OfType<AnimationClip>().First(c => c.name == clipName);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Characters/Skeleton_Warrior.fbx");

            var rig = Object.Instantiate(prefab);
            var animator = rig.GetComponent<Animator>();
            if (animator == null) animator = rig.AddComponent<Animator>();
            animator.applyRootMotion = false;

            var view = rig.AddComponent<SimDrivenAnimator>();
            view.Build(animator, clip);

            var bones = rig.GetComponentsInChildren<Transform>(true);
            Debug.Log($"[trace] clip={clipName} length={clip.length} frameRate={clip.frameRate} " +
                      $"frames={clip.length * clip.frameRate:0} bones={bones.Length}");

            // Which curves does the clip actually bind, and to what?
            var binds = AnimationUtility.GetCurveBindings(clip);
            Debug.Log($"[trace] curve bindings: {binds.Length}; distinct paths: {binds.Select(b => b.path).Distinct().Count()}");
            foreach (var p in binds.Select(b => b.path).Distinct().Take(8))
                Debug.Log($"[trace]   path: {p}");

            // Find the bone that travels furthest, and trace it.
            var phases = Enumerable.Range(0, 17).Select(i => i / 16f).ToArray();
            var samples = new Vector3[phases.Length][];
            for (var i = 0; i < phases.Length; i++)
            {
                view.SampleSingle(0, phases[i], clip.length);
                samples[i] = bones.Select(b => b.localPosition).ToArray();
            }

            var ranges = Enumerable.Range(0, bones.Length).Select(bi =>
            {
                var pts = samples.Select(s => s[bi]).ToArray();
                var span = 0f;
                for (var a = 0; a < pts.Length; a++)
                for (var b = a + 1; b < pts.Length; b++)
                    span = Mathf.Max(span, (pts[a] - pts[b]).magnitude);
                return (bi, span);
            }).OrderByDescending(t => t.span).ToArray();

            Debug.Log("[trace] most-travelled bones:");
            foreach (var (bi, span) in ranges.Take(5))
                Debug.Log($"[trace]   {bones[bi].name}: span {span:0.0000}");

            // The legs swing by ROTATION, not position, so trace both thighs. In a
            // proper walk, left at phase p should mirror right at phase p+0.5.
            var legL = bones.First(b => b.name == "upperleg.l");
            var legR = bones.First(b => b.name == "upperleg.r");
            Debug.Log("[trace] thigh pitch (localRotation.x, signed):");
            for (var i = 0; i < phases.Length; i++)
            {
                view.SampleSingle(0, phases[i], clip.length);
                float Pitch(Transform t)
                {
                    var x = t.localRotation.eulerAngles.x;
                    return x > 180f ? x - 360f : x;
                }
                Debug.Log($"[trace]   phase {phases[i]:0.0000}  L {Pitch(legL),8:0.00}   R {Pitch(legR),8:0.00}");
            }

            Object.DestroyImmediate(rig);
        }
    }
}
