using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Renders candidate art to PNG contact sheets so a model or a clip can be
    /// chosen by looking at it rather than by reading its filename.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists so that no art gets picked from a name.</b> "Idle_A" and
    /// "Skeletons_Idle" are indistinguishable as strings and completely
    /// different as poses; "building_tower_A" and "building_tower_catapult" are
    /// two different silhouettes at the only six camera angles this game ever
    /// shows. The choosing is the developer's, and this is the thing put in
    /// front of him to choose from.
    /// </para>
    /// <para>
    /// Two kinds of sheet, one per kind of question:
    /// </para>
    /// <list type="bullet">
    /// <item><description>
    /// <b>Clips</b> become a horizontal strip of poses sampled evenly across the
    /// clip — through <see cref="SimDrivenAnimator"/>, the same graph the game
    /// uses, so what he is looking at is what the game will do rather than what
    /// the editor's own preview window would do.
    /// </description></item>
    /// <item><description>
    /// <b>Models</b> become a six-frame turntable at the six snapped camera
    /// angles from <see cref="SceneFraming"/>, lit by the same sun. A model that
    /// reads well from one side and vanishes from another is a fact about this
    /// game's camera, and it only shows up if the preview uses that camera.
    /// </description></item>
    /// </list>
    /// <para>
    /// It takes its work from a JSON spec named on the command line rather than
    /// from constants, because the candidates are scratch files that live
    /// outside the repository and must never be committed. The tool is
    /// permanent; every path it is ever pointed at is not.
    /// </para>
    /// <para>
    /// Runs headless, from a shell, with no editor session and nobody at a
    /// keyboard:
    /// </para>
    /// <code>
    /// Unity.exe -batchmode -quit -projectPath client \
    ///           -executeMethod View.Editor.ArtPreviewCapture.Run \
    ///           -artPreviewSpec C:\some\spec.json
    /// </code>
    /// </remarks>
    public static class ArtPreviewCapture
    {
        /// <summary>The command-line flag whose value is the spec file's path.</summary>
        public const string SpecArgument = "-artPreviewSpec";

        /// <summary>
        /// How far the rig is turned about Y for a clip strip. The match camera
        /// looks down <c>+Z</c> at snap zero, so a rig left at identity faces
        /// away from it; this is a three-quarter view instead, which is where an
        /// arm swing and a leg swing are both legible. A framing choice about
        /// the contact sheet, not about the game.
        /// </summary>
        private const float ClipRigYawDegrees = 205f;

        /// <summary>
        /// Fill light, so the side the sun does not reach is dark rather than
        /// black. Contact sheets are for comparing silhouettes, and a candidate
        /// half-lost to shadow would lose the comparison for the wrong reason.
        /// </summary>
        private static Color AmbientLight => new Color(0.34f, 0.36f, 0.42f, 1f);

        /// <summary>
        /// What the instance parented to a bone gets called, so the notes can
        /// count the weapon's triangles rather than the weapon's plus the whole
        /// rig it is being held by — a number that would quietly make every
        /// weapon look like it costs six thousand triangles.
        /// </summary>
        private const string AttachmentName = "PreviewAttachment";

        // -------------------------------------------------------------------
        // The spec, as it arrives
        // -------------------------------------------------------------------

        [Serializable]
        private sealed class Spec
        {
            /// <summary>Absolute directory the PNGs and the manifest are written to.</summary>
            public string outDir;

            /// <summary>How many poses a clip strip samples. Evenly spaced across the clip.</summary>
            public int stripFrames = 8;

            /// <summary>Pixel size of one square tile in a strip or a turntable.</summary>
            public int frameSize = 200;

            public SlotSpec[] slots = Array.Empty<SlotSpec>();
        }

        [Serializable]
        private sealed class SlotSpec
        {
            public string slot;
            public string title;

            /// <summary><c>clip</c> or <c>model</c>.</summary>
            public string kind;

            public CandidateSpec[] candidates = Array.Empty<CandidateSpec>();
        }

        [Serializable]
        private sealed class CandidateSpec
        {
            public string id;
            public string label;
            public string source;
            public string notes;

            /// <summary>The skinned character an animation is played on.</summary>
            public string rig;

            /// <summary>The FBX the clip was imported from.</summary>
            public string bank;

            /// <summary>The clip's name inside <see cref="bank"/>.</summary>
            public string clip;

            /// <summary>A static model, for a turntable.</summary>
            public string model;

            /// <summary>Atlas for <see cref="model"/>.</summary>
            public string texture;

            /// <summary>Atlas for <see cref="rig"/>.</summary>
            public string rigTexture;

            /// <summary>
            /// When set, <see cref="model"/> is parented to this bone of
            /// <see cref="rig"/> and the whole rig is what gets turned. This is
            /// how a weapon is looked at: in a hand, at the game's own angles,
            /// rather than floating on its own where nothing about the fit shows.
            /// </summary>
            /// <remarks>
            /// It applies to a clip candidate too, and there it answers a
            /// different question: a draw-and-loose animation with nothing in
            /// the hand is a mime, and whether the three clips read as one
            /// sequence can only be judged with the weapon present.
            /// </remarks>
            public string attachBone;

            /// <summary>Clip used to pose the rig behind an attached model.</summary>
            public string poseClip;

            public string poseBank;

            /// <summary>Phase of <see cref="poseClip"/> to hold, in [0,1].</summary>
            public float posePhase;
        }

        // -------------------------------------------------------------------
        // The manifest, as it leaves
        // -------------------------------------------------------------------

        [Serializable]
        private sealed class Manifest
        {
            public List<ManifestSlot> slots = new List<ManifestSlot>();
        }

        [Serializable]
        private sealed class ManifestSlot
        {
            public string slot;
            public string title;
            public string kind;
            public List<ManifestCandidate> candidates = new List<ManifestCandidate>();
        }

        [Serializable]
        private sealed class ManifestCandidate
        {
            public string id;
            public string label;
            public string png;
            public string source;
            public string notes;
        }

        public static void Run()
        {
            string specPath = ReadSpecPath();
            Spec spec = JsonUtility.FromJson<Spec>(File.ReadAllText(specPath));

            if (spec == null || spec.slots == null || spec.slots.Length == 0)
            {
                throw new InvalidDataException("The spec at " + specPath + " names no slots.");
            }

            Directory.CreateDirectory(spec.outDir);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientLight;

            GameObject sun = BuildSun();
            var manifest = new Manifest();
            var failures = new List<string>();

            try
            {
                foreach (SlotSpec slot in spec.slots)
                {
                    manifest.slots.Add(CaptureSlot(spec, slot, failures));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(sun);
            }

            string manifestPath = Path.Combine(spec.outDir, "manifest.json");
            File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
            Debug.Log("ArtPreviewCapture: wrote " + manifestPath);

            // Loud, and last: a sheet that silently did not render is a candidate
            // the developer never gets offered, and a missing option is invisible
            // in a way a broken one is not.
            if (failures.Count > 0)
            {
                // Distinct, because a candidate that cannot be built fails once
                // while the slot is being measured and again while it is being
                // rendered, and reporting the same broken thing twice makes the
                // count a worse number than no count at all.
                string[] distinct = failures.Distinct().ToArray();

                throw new InvalidOperationException(
                    "ArtPreviewCapture could not render " + distinct.Length + " candidate(s):\n  " +
                    string.Join("\n  ", distinct));
            }
        }

        /// <summary>
        /// Renders every candidate in one slot, on one shared scale, so the
        /// sheets can be laid side by side and compared rather than merely
        /// looked at one at a time.
        /// </summary>
        private static ManifestSlot CaptureSlot(Spec spec, SlotSpec slot, List<string> failures)
        {
            var result = new ManifestSlot { slot = slot.slot, title = slot.title, kind = slot.kind };
            bool isClip = string.Equals(slot.kind, "clip", StringComparison.OrdinalIgnoreCase);

            // Two passes. The first only measures, because a shared frame cannot
            // be known until every candidate in the slot has been posed at least
            // once -- and a per-candidate frame would make the biggest tower and
            // the smallest one look the same size, which is the one comparison
            // the developer most needs to be able to make.
            var bounds = new Bounds();
            bool haveBounds = false;

            foreach (CandidateSpec candidate in slot.candidates)
            {
                GameObject subject = null;

                try
                {
                    subject = BuildSubject(candidate, isClip, out AnimationClip clip, out SimDrivenAnimator animator);
                    Bounds measured = MeasureSubject(subject, animator, clip, isClip ? spec.stripFrames : 1);

                    if (haveBounds)
                    {
                        bounds.Encapsulate(measured);
                    }
                    else
                    {
                        bounds = measured;
                        haveBounds = true;
                    }
                }
                catch (Exception error)
                {
                    failures.Add(slot.slot + "/" + candidate.id + ": " + error.Message);
                }
                finally
                {
                    if (subject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(subject);
                    }
                }
            }

            if (!haveBounds)
            {
                return result;
            }

            // A turntable walks the camera all the way round, so it has to be
            // framed on the bounding SPHERE or the corners clip at the angles the
            // box was never measured from. A strip never moves the camera, so the
            // same treatment would just shrink every subject by the diagonal of a
            // box nothing is ever seen from -- which is how a contact sheet ends
            // up with a character too small to judge.
            float radius = isClip
                ? Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z))
                : bounds.extents.magnitude;

            float orthographicSize = Mathf.Max(radius, 0.01f) * SceneFraming.CameraFramingMargin;

            foreach (CandidateSpec candidate in slot.candidates)
            {
                GameObject subject = null;

                try
                {
                    subject = BuildSubject(candidate, isClip, out AnimationClip clip, out SimDrivenAnimator animator);

                    string file = slot.slot + "__" + candidate.id + ".png";
                    Texture2D sheet = isClip
                        ? CaptureClipStrip(subject, animator, clip, spec, bounds.center, orthographicSize)
                        : CaptureTurntable(spec, bounds.center, orthographicSize);

                    File.WriteAllBytes(Path.Combine(spec.outDir, file), sheet.EncodeToPNG());
                    UnityEngine.Object.DestroyImmediate(sheet);

                    result.candidates.Add(new ManifestCandidate
                    {
                        id = candidate.id,
                        label = candidate.label,
                        png = file,
                        source = candidate.source,
                        notes = Describe(candidate, subject, clip)
                    });

                    Debug.Log("ArtPreviewCapture: wrote " + file);
                }
                catch (Exception error)
                {
                    failures.Add(slot.slot + "/" + candidate.id + ": " + error.Message);
                }
                finally
                {
                    if (subject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(subject);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Instantiates whatever this candidate is: a posed rig, a static model,
        /// or a model hung off a rig's bone.
        /// </summary>
        private static GameObject BuildSubject(
            CandidateSpec candidate, bool isClip, out AnimationClip clip, out SimDrivenAnimator sampler)
        {
            clip = null;
            sampler = null;

            if (isClip)
            {
                clip = LoadClip(candidate.bank, candidate.clip);
                GameObject rig = InstantiateModel(candidate.rig, candidate.rigTexture);
                rig.transform.rotation = Quaternion.Euler(0f, ClipRigYawDegrees, 0f);
                AttachHeldModel(rig, candidate);
                sampler = AttachSampler(rig, clip);

                return rig;
            }

            // A model candidate is one of three things, and the spec says which
            // by what it fills in: a rig on its own (a character, posed), a mesh
            // on its own (a building), or a mesh hung off a named bone of a rig
            // (a weapon, in the hand, which is the only place a weapon can
            // actually be judged).
            bool hasRig = !string.IsNullOrEmpty(candidate.rig);
            GameObject host = hasRig
                ? InstantiateModel(candidate.rig, candidate.rigTexture)
                : InstantiateModel(candidate.model, candidate.texture);

            if (hasRig)
            {
                AttachHeldModel(host, candidate);
            }

            if (!string.IsNullOrEmpty(candidate.poseClip))
            {
                AnimationClip pose = LoadClip(candidate.poseBank, candidate.poseClip);
                SimDrivenAnimator poser = AttachSampler(host, pose);
                poser.SampleSingle(0, candidate.posePhase, pose.length);
            }

            return host;
        }

        /// <summary>
        /// Hangs <see cref="CandidateSpec.model"/> off the named bone of a rig
        /// that has already been instantiated, at zero local offset. Does
        /// nothing when the candidate names no model, so both the turntable and
        /// the clip path can call it unconditionally.
        /// </summary>
        /// <remarks>
        /// Zero offset on purpose. The pack authors a bone whose whole job is
        /// to be where the held thing goes, and a hand-tuned nudge here would
        /// make the sheet flatter the fit — hiding exactly the import failure
        /// the sheet is being rendered to catch.
        /// </remarks>
        private static void AttachHeldModel(GameObject host, CandidateSpec candidate)
        {
            if (string.IsNullOrEmpty(candidate.model))
            {
                return;
            }

            if (string.IsNullOrEmpty(candidate.attachBone))
            {
                UnityEngine.Object.DestroyImmediate(host);

                throw new InvalidDataException("A candidate naming both a rig and a model must name an attachBone.");
            }

            Transform bone = host.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == candidate.attachBone);

            if (bone == null)
            {
                UnityEngine.Object.DestroyImmediate(host);

                throw new InvalidDataException("No bone named '" + candidate.attachBone + "' on " + candidate.rig);
            }

            GameObject held = InstantiateModel(candidate.model, candidate.texture);
            held.name = AttachmentName;
            held.transform.SetParent(bone, worldPositionStays: false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Poses through the shipped component rather than through
        /// <c>AnimationMode</c> or a clip's own <c>SampleAnimation</c>. The whole
        /// claim being made by a contact sheet is "this is what the game will
        /// look like", and it is only true if the sheet went through the same
        /// graph the game does.
        /// </summary>
        private static SimDrivenAnimator AttachSampler(GameObject rig, AnimationClip clip)
        {
            Animator animator = rig.GetComponent<Animator>();

            if (animator == null)
            {
                animator = rig.AddComponent<Animator>();
            }

            animator.applyRootMotion = false;
            var sampler = rig.AddComponent<SimDrivenAnimator>();
            sampler.Build(animator, clip);

            return sampler;
        }

        private static AnimationClip LoadClip(string bankPath, string clipName)
        {
            // Unity hangs a "__preview__" duplicate off every clip it has ever
            // drawn a thumbnail for. They are editor bookkeeping, they never
            // match a real name, and left in they make the "no such clip, here
            // is what there is" message twice as long and half as useful.
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(bankPath)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();

            AnimationClip clip = clips.FirstOrDefault(c => c.name == clipName);

            if (clip == null)
            {
                throw new InvalidDataException(
                    "No clip named '" + clipName + "' in " + bankPath + ". Found: " +
                    string.Join(", ", clips.Select(c => c.name)));
            }

            return clip;
        }

        /// <summary>
        /// Instantiates an imported model and forces one flat unlit-ish material
        /// carrying the pack's atlas onto every renderer. Overriding rather than
        /// trusting the FBX's own materials, because an FBX whose material
        /// failed to resolve renders as flat magenta or flat white — which is a
        /// perfectly legible image of the wrong thing, and the failure a contact
        /// sheet is least able to survive.
        /// </summary>
        private static GameObject InstantiateModel(string assetPath, string texturePath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidDataException("Candidate names no model or rig.");
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                throw new FileNotFoundException("No model imported at " + assetPath);
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // A SkinnedMeshRenderer reports the bind pose's bounds unless told to
            // keep them current. Left alone it would both mis-size the frame and,
            // worse, cull itself out of shot the moment a pose left the bind
            // pose's box -- an empty tile that looks like a missing model.
            foreach (SkinnedMeshRenderer skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                skinned.updateWhenOffscreen = true;
            }

            if (!string.IsNullOrEmpty(texturePath))
            {
                Material material = BuildAtlasMaterial(texturePath);

                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = new Material[renderer.sharedMaterials.Length == 0 ? 1 : renderer.sharedMaterials.Length];

                    for (var i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }

                    renderer.sharedMaterials = materials;
                }
            }

            return instance;
        }

        private static Material BuildAtlasMaterial(string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

            if (texture == null)
            {
                throw new FileNotFoundException("No texture imported at " + texturePath);
            }

            // Point filtering, because these atlases are a few hundred pixels of
            // flat colour swatches. Bilinear samples across a swatch boundary and
            // fringes every edge with a colour that is in neither swatch.
            texture.filterMode = FilterMode.Point;

            Material material = ViewMaterials.Create(Path.GetFileNameWithoutExtension(texturePath), Color.white);
            material.mainTexture = texture;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.1f);
            }

            return material;
        }

        /// <summary>
        /// World bounds of everything visible, unioned over every pose the sheet
        /// will show, so a frame sized from this cannot clip a later pose.
        /// </summary>
        private static Bounds MeasureSubject(
            GameObject subject, SimDrivenAnimator sampler, AnimationClip clip, int frames)
        {
            var bounds = new Bounds();
            bool have = false;

            for (var frame = 0; frame < Mathf.Max(frames, 1); frame++)
            {
                if (sampler != null && clip != null)
                {
                    sampler.SampleSingle(0, Phase(frame, frames), clip.length);
                }

                foreach (Renderer renderer in subject.GetComponentsInChildren<Renderer>(true))
                {
                    if (have)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                    else
                    {
                        bounds = renderer.bounds;
                        have = true;
                    }
                }
            }

            if (!have)
            {
                throw new InvalidDataException("Nothing renderable on " + subject.name);
            }

            return bounds;
        }

        /// <summary>Even spacing across the clip, ending short of the loop point.</summary>
        private static float Phase(int frame, int frames) => frames <= 1 ? 0f : frame / (float)frames;

        private static Texture2D CaptureClipStrip(
            GameObject subject, SimDrivenAnimator sampler, AnimationClip clip,
            Spec spec, Vector3 pivot, float orthographicSize)
        {
            var tiles = new List<Texture2D>();
            GameObject camera = BuildCamera(pivot, orthographicSize, SceneFraming.CameraRotation(0));

            try
            {
                for (var frame = 0; frame < spec.stripFrames; frame++)
                {
                    sampler.SampleSingle(0, Phase(frame, spec.stripFrames), clip.length);
                    tiles.Add(Grab(camera.GetComponent<Camera>(), spec.frameSize));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camera);
            }

            return Stitch(tiles, spec.frameSize);
        }

        /// <summary>
        /// One frame per snapped camera angle. The sun stays put in world space
        /// exactly as it does in the match, because a light that orbits with the
        /// viewer would make all six frames look identical and turn the whole
        /// sheet into a formality.
        /// </summary>
        private static Texture2D CaptureTurntable(Spec spec, Vector3 pivot, float orthographicSize)
        {
            var tiles = new List<Texture2D>();

            for (var snap = 0; snap < SceneFraming.CameraSnapCount; snap++)
            {
                GameObject camera = BuildCamera(pivot, orthographicSize, SceneFraming.CameraRotation(snap));

                try
                {
                    tiles.Add(Grab(camera.GetComponent<Camera>(), spec.frameSize));
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(camera);
                }
            }

            return Stitch(tiles, spec.frameSize);
        }

        private static GameObject BuildCamera(Vector3 pivot, float orthographicSize, Quaternion rotation)
        {
            var go = new GameObject("PreviewCamera");
            var camera = go.AddComponent<Camera>();

            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneFraming.BackgroundColor;
            camera.nearClipPlane = SceneFraming.CameraNearClip;
            camera.farClipPlane = SceneFraming.CameraFarClip;

            go.transform.rotation = rotation;
            go.transform.position = pivot - (go.transform.forward * SceneFraming.CameraDistance);

            return go;
        }

        private static GameObject BuildSun()
        {
            var go = new GameObject("PreviewSun");
            var light = go.AddComponent<Light>();

            light.type = LightType.Directional;
            light.color = SceneFraming.SunColor;
            light.intensity = SceneFraming.SunIntensity;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = SceneFraming.SunShadowStrength;
            go.transform.rotation = SceneFraming.SunRotation;

            return go;
        }

        private static Texture2D Grab(Camera camera, int size)
        {
            var target = new RenderTexture(size, size, 24);
            camera.targetTexture = target;
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;

            var tile = new Texture2D(size, size, TextureFormat.RGB24, false);
            tile.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            tile.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(target);

            return tile;
        }

        /// <summary>Lays the tiles out left to right into one image.</summary>
        private static Texture2D Stitch(List<Texture2D> tiles, int size)
        {
            var sheet = new Texture2D(size * tiles.Count, size, TextureFormat.RGB24, false);

            for (var i = 0; i < tiles.Count; i++)
            {
                sheet.SetPixels(i * size, 0, size, size, tiles[i].GetPixels());
                UnityEngine.Object.DestroyImmediate(tiles[i]);
            }

            sheet.Apply();

            return sheet;
        }

        /// <summary>
        /// What the developer would want to know and cannot see in the picture:
        /// how long a clip runs, whether it carries root motion, how heavy a mesh
        /// is. Measured off the imported asset rather than copied from the spec,
        /// so the note cannot disagree with the thing it describes.
        /// </summary>
        private static string Describe(CandidateSpec candidate, GameObject subject, AnimationClip clip)
        {
            var parts = new List<string>();

            if (clip != null)
            {
                parts.Add(clip.length.ToString("0.##") + "s");
                parts.Add(Mathf.RoundToInt(clip.length * clip.frameRate) + " frames @ " + clip.frameRate.ToString("0") + "fps");
                parts.Add(clip.isLooping ? "looping" : "not looping");
                parts.Add(clip.hasRootCurves || clip.hasMotionCurves ? "CARRIES ROOT MOTION" : "no root motion");
                parts.Add(clip.humanMotion ? "HUMANOID" : "generic curves");
            }
            else if (subject != null)
            {
                bool attached = !string.IsNullOrEmpty(candidate.attachBone);

                // For a weapon the subject is the whole rig holding it, and the
                // rig is not what is being chosen. Count only what was attached.
                Transform counted = attached
                    ? subject.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == AttachmentName)
                    : subject.transform;

                if (counted != null)
                {
                    parts.Add(Triangles(counted.gameObject) + " triangles");
                    parts.Add(counted.GetComponentsInChildren<Renderer>(true).Length + " renderer(s)");
                }

                if (attached)
                {
                    parts.Add("attached to bone '" + candidate.attachBone + "'");
                }
            }

            // A clip strip reports the clip, but which weapon was in the hand
            // while it played is the other half of what the strip is showing --
            // and unlike a turntable, it is not obvious from the slot's title.
            if (clip != null && !string.IsNullOrEmpty(candidate.attachBone))
            {
                parts.Add("holding " + Path.GetFileNameWithoutExtension(candidate.model) +
                          " on bone '" + candidate.attachBone + "'");
            }

            if (!string.IsNullOrEmpty(candidate.notes))
            {
                parts.Add(candidate.notes);
            }

            return string.Join("; ", parts);
        }

        private static int Triangles(GameObject subject)
        {
            int triangles = subject.GetComponentsInChildren<MeshFilter>(true)
                .Where(f => f.sharedMesh != null)
                .Sum(f => f.sharedMesh.triangles.Length / 3);

            return triangles + subject.GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh != null)
                .Sum(r => r.sharedMesh.triangles.Length / 3);
        }

        private static string ReadSpecPath()
        {
            string[] args = Environment.GetCommandLineArgs();

            for (var i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], SpecArgument, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            throw new ArgumentException(
                "ArtPreviewCapture needs " + SpecArgument + " <path to a spec .json> on the command line.");
        }
    }
}
