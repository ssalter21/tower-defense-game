using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Sim;
using UnityEngine;
using View;

namespace Tests.PlayMode
{
    /// <summary>
    /// The camera: fixed isometric, orthographic, six snapped steps, and unable
    /// to change what happens in the match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>"View-only" is asserted here, not intended.</b> Two independent
    /// checks, because they fail independently: a structural one that no
    /// signature on the rig so much as names a simulation type, and a
    /// behavioural one that runs the same match six times with the camera
    /// somewhere different each tick and requires the per-tick state hashes to
    /// be identical. The first would miss a camera read through a static; the
    /// second would miss a dependency that exists but has not been exercised.
    /// </para>
    /// <para>
    /// <b>The six snaps are a real check because nothing billboards.</b> If any
    /// part of the view turned to face the camera, yawing through all six would
    /// look the same from everywhere and prove nothing. So one of these tests
    /// yaws the whole rig and requires every other transform in the scene to be
    /// bit-for-bit where it was.
    /// </para>
    /// </remarks>
    public class CameraRigTests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }

        private MatchRoot BuildPlayfield()
        {
            _root = new GameObject(SceneFraming.RootObjectName);

            return _root.AddComponent<MatchRoot>();
        }

        [Test]
        public void TheCameraIsIsometricAndOrthographic()
        {
            MatchRoot root = BuildPlayfield();
            Camera camera = root.CameraRig.Camera;

            Assert.That(camera.orthographic, Is.True, "A perspective camera is not this game's camera.");
            Assert.That(camera.orthographicSize, Is.GreaterThan(0f));

            // The pitch is the true isometric angle and it does not change with
            // the snap; only the yaw does.
            for (int snap = 0; snap < SceneFraming.CameraSnapCount; snap++)
            {
                root.CameraRig.SnapTo(snap);

                Assert.That(
                    camera.transform.eulerAngles.x,
                    Is.EqualTo(SceneFraming.CameraPitchDegrees).Within(0.01f),
                    "snap " + snap + " changed the pitch");
            }

            Assert.That(
                root.Floor.WorldBounds.size.magnitude,
                Is.GreaterThan(0f),
                "The camera is framed on the floor, so the floor has to have a size.");
        }

        [Test]
        public void ThereAreSixSnapsAndTheyWrap()
        {
            MatchRoot root = BuildPlayfield();
            IsometricCameraRig rig = root.CameraRig;

            var yaws = new List<float>();

            rig.SnapTo(0);
            Quaternion first = rig.transform.rotation;

            for (int step = 0; step < SceneFraming.CameraSnapCount; step++)
            {
                Assert.That(rig.Snap, Is.EqualTo(step));
                yaws.Add(rig.transform.eulerAngles.y);
                rig.Rotate(1);
            }

            Assert.That(rig.Snap, Is.EqualTo(0), "Six steps is all the way round.");
            Assert.That(Quaternion.Angle(rig.transform.rotation, first), Is.LessThan(0.01f));

            Assert.That(yaws.Distinct().Count(), Is.EqualTo(6), "Six snaps, six distinct headings.");

            for (int step = 1; step < yaws.Count; step++)
            {
                float delta = Mathf.DeltaAngle(yaws[step - 1], yaws[step]);

                Assert.That(
                    Mathf.Abs(delta),
                    Is.EqualTo(SceneFraming.CameraSnapDegrees).Within(0.01f),
                    "step " + step + " is not a sixty-degree snap");
            }

            // Backwards, and past zero, because a negative modulus in C# is the
            // classic way an orbit sticks at one end.
            rig.Rotate(-1);
            Assert.That(rig.Snap, Is.EqualTo(5));
            rig.Rotate(-7);
            Assert.That(rig.Snap, Is.EqualTo(4));
        }

        /// <summary>
        /// No billboards, no flat cards. Yawing the camera moves the camera and
        /// nothing else — asserted over every transform under the root.
        /// </summary>
        [Test]
        public void YawingTheCameraMovesNothingButTheCamera()
        {
            MatchRoot root = BuildPlayfield();
            IsometricCameraRig rig = root.CameraRig;

            Transform[] others = root.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(t => !t.IsChildOf(rig.transform))
                .ToArray();

            Assert.That(others.Length, Is.GreaterThan(100), "The floor should be under here.");

            Matrix4x4[] before = others.Select(t => t.localToWorldMatrix).ToArray();

            for (int step = 0; step < SceneFraming.CameraSnapCount; step++)
            {
                rig.Rotate(1);

                for (int index = 0; index < others.Length; index++)
                {
                    Assert.That(
                        others[index].localToWorldMatrix,
                        Is.EqualTo(before[index]),
                        others[index].name + " moved when the camera did, at snap " + rig.Snap
                        + ". Something is facing the camera, and the six-snap check is a formality.");
                }
            }
        }

        /// <summary>
        /// Nothing in the view is a sprite, a billboard or a card standing on
        /// its edge, and the shadows are cast by a light rather than painted
        /// into a texture.
        /// </summary>
        [Test]
        public void EverythingDrawnIsRealGeometryLitByARealLight()
        {
            MatchRoot root = BuildPlayfield();

            Assert.That(
                root.GetComponentsInChildren<SpriteRenderer>(includeInactive: true),
                Is.Empty,
                "sprites are billboards with a friendlier name");

            Assert.That(
                root.GetComponentsInChildren<BillboardRenderer>(includeInactive: true),
                Is.Empty);

            Assert.That(
                root.GetComponentsInChildren<Canvas>(includeInactive: true),
                Is.Empty,
                "a world-space canvas is a flat card");

            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                Assert.That(renderer, Is.TypeOf<MeshRenderer>(), renderer.name + " is not a mesh renderer");

                Assert.That(
                    renderer.shadowCastingMode,
                    Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.On),
                    renderer.name + " casts no shadow");

                Assert.That(renderer.receiveShadows, Is.True, renderer.name + " receives no shadow");

                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;

                // Lying in the ground plane, which is the one orientation a
                // camera-facing card can never have.
                Assert.That(mesh.bounds.size.y, Is.EqualTo(0f).Within(0.001f), renderer.name + " is standing up");
                Assert.That(mesh.bounds.size.x, Is.GreaterThan(0f));
                Assert.That(mesh.bounds.size.z, Is.GreaterThan(0f));
            }

            Assert.That(root.Sun.type, Is.EqualTo(LightType.Directional));
            Assert.That(
                root.Sun.shadows,
                Is.Not.EqualTo(LightShadows.None),
                "The only shadows in this project are the ones this light casts.");
            Assert.That(root.Sun.shadowStrength, Is.GreaterThan(0f));

            // Fixed in world space rather than parented to the camera. A light
            // that orbits with the viewer makes all six snaps look identical.
            Assert.That(root.Sun.transform.IsChildOf(root.CameraRig.transform), Is.False);
        }

        // -----------------------------------------------------------------
        // The camera is never a simulation input
        // -----------------------------------------------------------------

        /// <summary>
        /// The structural half: nothing on the rig names a type that came out
        /// of <c>Sim.dll</c>, so there is no argument, field, property or
        /// return value through which a yaw could reach a tick.
        /// </summary>
        [Test]
        public void TheCameraRigCannotEvenNameASimulationType()
        {
            Assembly simulation = typeof(HexMap).Assembly;
            var offenders = new List<string>();

            const BindingFlags Everything =
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.Instance |
                BindingFlags.DeclaredOnly;

            foreach (MemberInfo member in typeof(IsometricCameraRig).GetMembers(Everything))
            {
                switch (member)
                {
                    case FieldInfo field when field.FieldType.Assembly == simulation:
                        offenders.Add("field " + field.Name);
                        break;

                    case PropertyInfo property when property.PropertyType.Assembly == simulation:
                        offenders.Add("property " + property.Name);
                        break;

                    case MethodBase method:
                        if (method is MethodInfo info && info.ReturnType.Assembly == simulation)
                        {
                            offenders.Add("return of " + method.Name);
                        }

                        offenders.AddRange(
                            method.GetParameters()
                                .Where(p => p.ParameterType.Assembly == simulation)
                                .Select(p => method.Name + "(" + p.Name + ")"));
                        break;
                }
            }

            Assert.That(
                offenders,
                Is.Empty,
                "The camera rig has grown a way to reach the simulation: " + string.Join(", ", offenders));
        }

        /// <summary>
        /// The behavioural half: the same match, run once per snap, with the
        /// camera moved every single tick — and every per-tick state hash
        /// identical.
        /// </summary>
        /// <remarks>
        /// The hash is over internal simulation state rather than over the
        /// snapshot, so a camera that had somehow reached a field the view
        /// never draws would still show up here, and would show up at the tick
        /// it happened.
        /// </remarks>
        [Test]
        public void TheCameraIsNeverASimulationInput()
        {
            MatchRoot root = BuildPlayfield();
            IsometricCameraRig rig = root.CameraRig;

            ulong[] still = RunMatch(root.Map, null);

            Assert.That(still.Length, Is.GreaterThan(30), "A match that short is not evidence of anything.");

            for (int snap = 0; snap < SceneFraming.CameraSnapCount; snap++)
            {
                rig.SnapTo(snap);

                ulong[] orbiting = RunMatch(root.Map, rig);

                Assert.That(
                    orbiting,
                    Is.EqualTo(still),
                    "Running the match while orbiting from snap " + snap
                    + " produced a different rolling state hash. Where somebody is looking changed "
                    + "what happened.");
            }
        }

        /// <summary>
        /// Plays a whole match, yawing <paramref name="rig"/> — if there is one
        /// — on every tick, and returns the rolling state hash per tick.
        /// </summary>
        /// <remarks>
        /// The content is written here rather than read from a file: two unit
        /// types, one tower and four creeps is enough for the dice to be rolled
        /// and for creeps to die, and this test is about the camera rather than
        /// about the committed tuning.
        /// </remarks>
        private static ulong[] RunMatch(HexMap map, IsometricCameraRig rig)
        {
            var types = UnitTypeTable.Parse(
                "unit 1 grunt moving 200 85 0 0 0 0 0 0 none 0 12\n"
                + "unit 3 bolt placed 0 0 3200 6 3 2 9 15 hitscan 0 0\n");

            var layout = TowerLayout.Parse("tower 3 3 2\n", types);
            var wave = WaveScript.Parse("order 0 1 4 0\n", types);

            var match = new Match(map, layout, wave, seed: 0x5EED1234u);
            var hashes = new List<ulong>();

            while (!match.IsFinished && hashes.Count < 4000)
            {
                match.Advance(1);
                hashes.Add(match.StateHash.Value);

                if (rig != null)
                {
                    rig.Rotate(1);
                }
            }

            return hashes.ToArray();
        }
    }
}
