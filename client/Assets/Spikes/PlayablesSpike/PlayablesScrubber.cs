#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Spikes.Playables
{
    /// <summary>
    /// The thing you can actually look at: a skeleton, a clip, and a slider that
    /// scrubs it. Press Play and it builds itself — there is no scene to author
    /// and nothing to wire in the inspector.
    ///
    /// Drag the slider backwards and the legs must walk backwards. That is
    /// Tier 3 row 4 of the acceptance table, and it is the one check in the whole
    /// suite that only a human can make.
    /// </summary>
    public sealed class PlayablesScrubber : MonoBehaviour
    {
        private const string CharacterPath = "Assets/Art/Characters/Skeleton_Warrior.fbx";
        private static readonly string[] Banks =
        {
            "Assets/Art/Animations/Rig_Medium_MovementBasic.fbx",
            "Assets/Art/Animations/Rig_Medium_General.fbx"
        };

        private SimDrivenAnimator _view;
        private AnimationClip[] _clips;
        private int _clipIndex;
        private float _phase;
        private bool _autoPlay;
        private float _rate = 1f;
        private float _lastPhase;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // Never gate-crash a test run: the test framework drives Play mode too,
            // and a stray skeleton plus camera in the scene would be a real hazard.
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (scene.Contains("InitTestScene") || scene.Contains("Test")) return;

            if (FindFirstObjectByType<PlayablesScrubber>() != null) return;
            new GameObject("PlayablesScrubber").AddComponent<PlayablesScrubber>();
        }

        private void Start()
        {
            _clips = Banks
                .SelectMany(AssetDatabase.LoadAllAssetsAtPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__") && c.length > 0.1f)
                .OrderBy(c => c.name)
                .ToArray();

            if (_clips.Length == 0)
            {
                Debug.LogError("PlayablesScrubber: no clips found — is Assets/Art imported?");
                enabled = false;
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterPath);
            var rig = Instantiate(prefab, Vector3.zero, Quaternion.Euler(0f, 150f, 0f));
            rig.name = "Skeleton";

            var animator = rig.GetComponent<Animator>();
            if (animator == null) animator = rig.AddComponent<Animator>();
            animator.applyRootMotion = false;

            _view = rig.AddComponent<SimDrivenAnimator>();
            _view.Build(animator, _clips);

            BuildStage();
            _clipIndex = System.Array.FindIndex(_clips, c => c.name == "Walking_A");
            if (_clipIndex < 0) _clipIndex = 0;
        }

        /// <summary>Camera and light in code — the scene stays empty, per the working-model decision.</summary>
        private static void BuildStage()
        {
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 1.6f;
            cam.backgroundColor = new Color(0.16f, 0.17f, 0.20f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            // Fixed isometric orbit, matching the perspective decision.
            camGo.transform.position = new Vector3(4f, 3.2f, -4f);
            camGo.transform.LookAt(new Vector3(0f, 0.9f, 0f));

            var lightGo = new GameObject("Key");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.55f);
        }

        private void Update()
        {
            if (_autoPlay)
            {
                _phase += Time.deltaTime * _rate / _clips[_clipIndex].length;
                _phase -= Mathf.Floor(_phase);
            }

            // The whole contract in one line: pose is a function of phase, nothing else.
            var times = new double[_clips.Length];
            var weights = new float[_clips.Length];
            times[_clipIndex] = _phase * _clips[_clipIndex].length;
            weights[_clipIndex] = 1f;
            _view.Sample(times, weights);
        }

        private void OnGUI()
        {
            const int w = 460;
            GUILayout.BeginArea(new Rect(12, 12, w, 420), GUI.skin.box);

            var clip = _clips[_clipIndex];
            GUILayout.Label($"<b>{clip.name}</b>   {clip.length:0.00}s @ {clip.frameRate:0}fps",
                new GUIStyle(GUI.skin.label) { richText = true, fontSize = 15 });

            var dir = Mathf.Approximately(_phase, _lastPhase) ? "—"
                : (_phase > _lastPhase ? "forwards ▶" : "◀ BACKWARDS");
            _lastPhase = _phase;

            GUILayout.Label($"phase {_phase:0.000}    time {_phase * clip.length:0.000}s    {dir}");
            _phase = GUILayout.HorizontalSlider(_phase, 0f, 1f);

            GUILayout.Space(6);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("|◀ 0.0")) _phase = 0f;
            if (GUILayout.Button("◀ step")) _phase = Mathf.Repeat(_phase - 0.02f, 1f);
            if (GUILayout.Button("step ▶")) _phase = Mathf.Repeat(_phase + 0.02f, 1f);
            if (GUILayout.Button("0.5 ▶|")) _phase = 0.5f;
            GUILayout.EndHorizontal();

            _autoPlay = GUILayout.Toggle(_autoPlay, "  auto-play (still just feeding it a phase)");
            GUILayout.Label($"rate  {_rate:0.00}×   — drag below 0 to run the clip in reverse");
            _rate = GUILayout.HorizontalSlider(_rate, -3f, 3f);

            GUILayout.Space(6);
            GUILayout.Label("clip:");
            _clipIndex = GUILayout.SelectionGrid(_clipIndex, _clips.Select(c => c.name).ToArray(), 4);

            GUILayout.EndArea();
        }
    }
}
#endif
