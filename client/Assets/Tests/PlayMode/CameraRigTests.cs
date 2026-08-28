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
    /// The camera: perspective, freely orbited, flown anywhere, dollied close
    /// enough to read one model, and unable to change what happens in the
    /// match.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>"View-only" is asserted here, not intended.</b> Two independent
    /// checks, because they fail independently: a structural one that no
    /// signature on the rig so much as names a simulation type, and a
    /// behavioural one that runs the same match with the camera somewhere
    /// different every tick and requires the per-tick state hashes to be
    /// identical. The first would miss a camera read through a static; the
    /// second would miss a dependency that exists but has not been exercised.
    /// </para>
    /// <para>
    /// <b>Orbiting is a real check because nothing billboards.</b> If any part
    /// of the view turned to face the camera, turning the rig would look the
    /// same from everywhere and prove nothing. So one of these tests orbits and
    /// flies the whole rig and requires every other transform in the scene to
    /// be bit-for-bit where it was.
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

        /// <summary>
        /// Perspective, and framed so the whole floor is on screen at the
        /// default view — asserted by projecting the floor's own corners rather
        /// than by re-deriving the distance formula, which would only check
        /// that the arithmetic agrees with itself.
        /// </summary>
        [Test]
        public void TheCameraIsPerspectiveAndTheWholeFloorIsInFrame()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;
            Camera camera = rig.Camera;

            Assert.That(camera.orthographic, Is.False, "An orthographic camera cannot be dollied into a fight.");
            Assert.That(
                camera.fieldOfView,
                Is.EqualTo(SceneFraming.CameraFieldOfViewDegrees).Within(0.01f));

            Assert.That(rig.Distance, Is.EqualTo(rig.FramedDistance).Within(0.001f));
            Assert.That(rig.FramedDistance, Is.GreaterThan(0f));
            Assert.That(rig.Pitch, Is.EqualTo(SceneFraming.CameraDefaultPitchDegrees).Within(0.01f));

            Bounds floor = root.Floor.WorldBounds;

            Assert.That(floor.size.magnitude, Is.GreaterThan(0f), "The camera is framed on the floor.");

            foreach (Vector3 corner in GroundCorners(floor))
            {
                Vector3 viewport = camera.WorldToViewportPoint(corner);

                Assert.That(viewport.z, Is.GreaterThan(0f), corner + " is behind the camera");
                Assert.That(viewport.x, Is.InRange(0f, 1f), corner + " is off the side of the frame");
                Assert.That(viewport.y, Is.InRange(0f, 1f), corner + " is off the top or bottom of the frame");
            }
        }

        /// <summary>
        /// Yaw is free rather than snapped, and pitch has no limit at all: far
        /// enough round and the camera is underneath the floor looking up,
        /// which is the view a clamp would have taken away.
        /// </summary>
        [Test]
        public void YawIsFreeAndPitchIsNotClamped()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            var yaws = new List<float>();

            for (var step = 0; step < 12; step++)
            {
                rig.Orbit(7f, 0f);
                yaws.Add(rig.Yaw);
            }

            Assert.That(yaws.Distinct().Count(), Is.EqualTo(12), "Twelve seven-degree turns, twelve headings.");
            Assert.That(rig.Yaw, Is.EqualTo(84f).Within(0.01f), "The turns did not land where they were asked to.");

            // All the way round and back to where it started.
            rig.Orbit(276f, 0f);
            Assert.That(rig.Yaw, Is.EqualTo(0f).Within(0.01f));

            // Sixty degrees the wrong way from the default puts the camera
            // below the ground plane, tilted up at the underside of the board.
            rig.Orbit(0f, -SceneFraming.CameraDefaultPitchDegrees - 25f);

            Assert.That(rig.Pitch, Is.EqualTo(335f).Within(0.01f), "The pitch was clamped on the way past zero.");
            Assert.That(
                rig.Camera.transform.position.y,
                Is.LessThan(0f),
                "The camera cannot get under the floor, so the pitch is clamped somewhere.");
            Assert.That(
                rig.Camera.transform.forward.y,
                Is.GreaterThan(0f),
                "From under the floor the camera should be looking up.");

            // And the other way: over the top, past straight down, to the far
            // side of the board with the picture inverted.
            rig.PointAt(0f, 180f - SceneFraming.CameraDefaultPitchDegrees, rig.FramedDistance);

            Assert.That(rig.Camera.transform.position.y, Is.GreaterThan(0f));
            Assert.That(rig.Camera.transform.position.z, Is.GreaterThan(rig.Pivot.z));
            Assert.That(
                rig.Camera.transform.up.y,
                Is.LessThan(0f),
                "Past straight down the picture is upside down, and that is what unclamped means.");
        }

        /// <summary>
        /// The dolly walks the camera in until one creep would overflow the
        /// frame, and stops there rather than at the pivot.
        /// </summary>
        [Test]
        public void DollyingGoesInCloseAndStopsShortOfThePivot()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            float framed = rig.FramedDistance;

            rig.Dolly(0.5f);
            Assert.That(rig.Distance, Is.LessThan(framed), "A positive dolly goes in.");

            rig.Dolly(-0.5f);
            Assert.That(rig.Distance, Is.EqualTo(framed).Within(0.01f), "The dolly is not symmetric.");

            // Far more scrolling than anybody would do, to land on the stops.
            rig.Dolly(100f);
            Assert.That(rig.Distance, Is.EqualTo(SceneFraming.CameraMinDistance).Within(0.001f));
            Assert.That(
                rig.Distance,
                Is.GreaterThan(SceneFraming.CameraNearClip),
                "The closest stop is inside the near plane, so the world would vanish there.");

            // A frame under two metres tall where the camera is pointed: a
            // humanoid standing there does not fit in it.
            float frameHeight = 2f * rig.Distance
                * Mathf.Tan(0.5f * SceneFraming.CameraFieldOfViewDegrees * Mathf.Deg2Rad);

            Assert.That(
                frameHeight,
                Is.LessThan(2f),
                "The closest the camera goes still frames more than a whole creep.");

            rig.Dolly(-100f);
            Assert.That(
                rig.Distance,
                Is.EqualTo(framed * SceneFraming.CameraMaxDistanceFactor).Within(0.001f));

            // The far plane is a typed constant with a derivation in its
            // comment, and this is the derivation: from the outermost stop, the
            // whole floor is still in front of it.
            foreach (Vector3 corner in GroundCorners(root.Floor.WorldBounds))
            {
                Assert.That(
                    rig.Camera.WorldToViewportPoint(corner).z,
                    Is.LessThan(SceneFraming.CameraFarClip),
                    "The far plane cuts " + corner + " off at the outermost dolly stop.");
            }
        }

        /// <summary>
        /// The pivot flies, and it flies where the camera is looking: forward
        /// is the heading flattened into the ground plane, so a press still
        /// goes into the picture after a half turn. Up is world up at every
        /// heading, and nothing stops the camera leaving the board.
        /// </summary>
        [Test]
        public void FlyingMovesThePivotAlongTheCurrentHeading()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            // Yaw zero looks down +Z, so forward is +Z.
            Vector3 from = rig.Pivot;
            float distance = rig.Distance;
            rig.Fly(Vector3.forward);

            AssertVector(rig.Pivot - from, new Vector3(0f, 0f, distance), "forward at yaw 0");

            // Half a turn, and the same press goes the other way.
            rig.PointAt(180f, rig.Pitch, distance);
            from = rig.Pivot;
            rig.Fly(Vector3.forward);

            AssertVector(rig.Pivot - from, new Vector3(0f, 0f, -distance), "forward at yaw 180");

            // A quarter turn: forward is +X, and right is -Z.
            rig.PointAt(90f, rig.Pitch, distance);
            from = rig.Pivot;
            rig.Fly(Vector3.forward + Vector3.right);

            AssertVector(rig.Pivot - from, new Vector3(distance, 0f, -distance), "forward and right at yaw 90");

            // Up is world up however the rig is turned or tilted.
            rig.PointAt(213f, 250f, distance);
            from = rig.Pivot;
            rig.Fly(Vector3.up);

            AssertVector(rig.Pivot - from, new Vector3(0f, distance, 0f), "up at yaw 213, pitch 250");

            // Orbited over the top, the picture is upside down and the camera's
            // own forward has turned over with it. Flying follows the heading
            // instead, which is still the direction up the screen — so a press
            // goes into the picture rather than out of the back of it.
            rig.PointAt(0f, 180f - SceneFraming.CameraDefaultPitchDegrees, distance);
            from = rig.Pivot;
            rig.Fly(Vector3.forward);

            Vector3 upTheScreen = Vector3.ProjectOnPlane(rig.Camera.transform.up, Vector3.up);

            Assert.That(
                Vector3.Dot((rig.Pivot - from).normalized, upTheScreen.normalized),
                Is.GreaterThan(0.99f),
                "Inverted, flying forward left the picture instead of going up the screen.");

            // Off the board and under it. There are no bounds on the pivot, on
            // purpose: a limit would be a guess at which views are worth having.
            rig.PointAt(0f, rig.Pitch, distance);
            rig.Fly(new Vector3(0f, -4f, 20f));

            Bounds floor = root.Floor.WorldBounds;

            Assert.That(rig.Pivot.z, Is.GreaterThan(floor.max.z), "The camera cannot fly off the end of the board.");
            Assert.That(rig.Pivot.y, Is.LessThan(0f), "The camera cannot fly under the board.");
        }

        /// <summary>
        /// One press covers the same fraction of what is on screen at every
        /// zoom, which is the same reason the dolly is exponential: a fixed
        /// speed is glacial framed on the board and uncontrollable on a creep.
        /// </summary>
        [Test]
        public void FlightSpeedScalesWithTheDolly()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            rig.PointAt(0f, rig.Pitch, rig.FramedDistance);

            Vector3 from = rig.Pivot;
            rig.Fly(Vector3.forward);
            float framed = Vector3.Distance(rig.Pivot, from);

            Assert.That(framed, Is.EqualTo(rig.FramedDistance).Within(0.001f));

            rig.PointAt(0f, rig.Pitch, 0.5f * rig.FramedDistance);

            from = rig.Pivot;
            rig.Fly(Vector3.forward);

            Assert.That(
                Vector3.Distance(rig.Pivot, from),
                Is.EqualTo(0.5f * framed).Within(0.001f),
                "Dollying halfway in did not halve the step, so the flight is a speed rather than a fraction.");
        }

        /// <summary>
        /// Reframing puts the pivot back with the angle and the distance. The
        /// frame capture reframes against its own aspect, so anything less than
        /// this would let a capture inherit wherever somebody had flown to.
        /// </summary>
        [Test]
        public void ReframingPutsThePivotBackAsWellAsTheAngle()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            rig.PointAt(97f, 12f, 0.3f * rig.FramedDistance);
            rig.Fly(new Vector3(3f, 2f, -5f));

            rig.Reframe(root.Floor.WorldBounds);

            AssertVector(rig.Pivot, rig.FramedPivot, "the reframed pivot");
            Assert.That(rig.Yaw, Is.EqualTo(SceneFraming.CameraDefaultYawDegrees).Within(0.001f));
            Assert.That(rig.Pitch, Is.EqualTo(SceneFraming.CameraDefaultPitchDegrees).Within(0.001f));
            Assert.That(rig.Distance, Is.EqualTo(rig.FramedDistance).Within(0.001f));
        }

        /// <summary>Asserts a vector component by component.</summary>
        private static void AssertVector(Vector3 actual, Vector3 expected, string what)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), what + ": x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), what + ": y");
            Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f), what + ": z");
        }

        /// <summary>
        /// The reset key eases rather than cuts, brings the position back along
        /// with the angle and the distance, takes about a quarter of a second,
        /// and gives way to a hand on the mouse.
        /// </summary>
        /// <remarks>
        /// Easing the pivot is what makes the reset mean <i>the default view</i>
        /// rather than <i>the default angle</i>. It is also the only way home
        /// from far enough out that the board has left the frustum, which is
        /// what an unbounded flight buys.
        /// </remarks>
        [Test]
        public void TheResetEasesBackToTheDefaultViewInAQuarterOfASecond()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            rig.PointAt(140f, 8f, 0.4f * rig.FramedDistance);
            rig.Fly(new Vector3(6f, 3f, 9f));

            Assert.That(
                Vector3.Distance(rig.Pivot, rig.FramedPivot),
                Is.GreaterThan(rig.FramedDistance),
                "The camera has not been flown far enough for the reset to be worth measuring.");

            rig.ResetView();

            Assert.That(rig.IsEasing, Is.True);

            rig.Advance(0.1f);

            Assert.That(rig.IsEasing, Is.True, "A quarter of a second is not up after a tenth of one.");
            Assert.That(
                Vector3.Distance(rig.Pivot, rig.FramedPivot),
                Is.GreaterThan(0.01f),
                "The pivot arrived instantly, so the position is cut rather than eased.");
            Assert.That(
                Mathf.DeltaAngle(rig.Yaw, SceneFraming.CameraDefaultYawDegrees),
                Is.Not.EqualTo(0f).Within(1f),
                "The reset arrived instantly, so it is a cut rather than an ease.");
            Assert.That(
                Mathf.DeltaAngle(rig.Yaw, 140f),
                Is.Not.EqualTo(0f).Within(1f),
                "The reset has not moved anything.");

            rig.Advance(0.15f);

            Assert.That(rig.IsEasing, Is.False);
            Assert.That(rig.Yaw, Is.EqualTo(SceneFraming.CameraDefaultYawDegrees).Within(0.001f));
            Assert.That(rig.Pitch, Is.EqualTo(SceneFraming.CameraDefaultPitchDegrees).Within(0.001f));
            Assert.That(rig.Distance, Is.EqualTo(rig.FramedDistance).Within(0.001f));
            Assert.That(
                Vector3.Distance(rig.Pivot, rig.FramedPivot),
                Is.LessThan(0.001f),
                "The angle and the distance came home and the position stayed where it was flown to.");

            Bounds floor = root.Floor.WorldBounds;

            AssertVector(
                rig.FramedPivot,
                new Vector3(floor.center.x, 0f, floor.center.z),
                "home is the middle of the floor, in the ground plane");

            // A hand on the mouse wins: the ease stops where the drag put it
            // rather than dragging the camera back out from underneath.
            rig.PointAt(140f, 8f, rig.FramedDistance);
            rig.ResetView();
            rig.Advance(0.1f);
            rig.Orbit(10f, 0f);

            Assert.That(rig.IsEasing, Is.False, "Orbiting during a reset left the reset running.");

            float held = rig.Yaw;
            rig.Advance(1f);

            Assert.That(rig.Yaw, Is.EqualTo(held).Within(0.001f));
        }

        /// <summary>
        /// No billboards, no flat cards. Orbiting, flying and dollying move the
        /// camera and nothing else — asserted over every transform under the
        /// root.
        /// </summary>
        [Test]
        public void MovingTheCameraMovesNothingButTheCamera()
        {
            MatchRoot root = BuildPlayfield();
            OrbitCameraRig rig = root.CameraRig;

            Transform[] others = root.GetComponentsInChildren<Transform>(includeInactive: true)
                .Where(t => !t.IsChildOf(rig.transform))
                .ToArray();

            Assert.That(others.Length, Is.GreaterThan(100), "The floor should be under here.");

            Matrix4x4[] before = others.Select(t => t.localToWorldMatrix).ToArray();

            for (var step = 0; step < 12; step++)
            {
                rig.Orbit(31f, 13f);
                rig.Dolly(0.2f);
                rig.Fly(new Vector3(0.3f, 0.1f, 0.4f));

                for (var index = 0; index < others.Length; index++)
                {
                    Assert.That(
                        others[index].localToWorldMatrix,
                        Is.EqualTo(before[index]),
                        others[index].name + " moved when the camera did, at yaw " + rig.Yaw
                        + ". Something is facing the camera, and orbiting to look at it shows nothing.");
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
            // that orbits with the viewer lights every angle identically.
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

            foreach (MemberInfo member in typeof(OrbitCameraRig).GetMembers(Everything))
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
        /// The behavioural half: the same match, run once per starting angle,
        /// with the camera orbited, dollied and flown every single tick — and
        /// every per-tick state hash identical.
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
            OrbitCameraRig rig = root.CameraRig;

            ulong[] still = RunMatch(root.Map, null);

            Assert.That(still.Length, Is.GreaterThan(30), "A match that short is not evidence of anything.");

            foreach (float yaw in new[] { 0f, 47f, 113f, 250f })
            {
                rig.PointAt(yaw, SceneFraming.CameraDefaultPitchDegrees, rig.FramedDistance);

                ulong[] orbiting = RunMatch(root.Map, rig);

                Assert.That(
                    orbiting,
                    Is.EqualTo(still),
                    "Running the match while orbiting from yaw " + yaw
                    + " produced a different rolling state hash. Where somebody is looking changed "
                    + "what happened.");
            }
        }

        /// <summary>
        /// Plays a whole match, orbiting, dollying and flying
        /// <paramref name="rig"/> — if there is one — on every tick, and
        /// returns the rolling state hash per tick.
        /// </summary>
        /// <remarks>
        /// The content is written here rather than read from a file: two unit
        /// types, one tower and four creeps is enough for the dice to be rolled
        /// and for creeps to die, and this test is about the camera rather than
        /// about the committed tuning.
        /// </remarks>
        private static ulong[] RunMatch(HexMap map, OrbitCameraRig rig)
        {
            var types = UnitTypeTable.Parse(
                "unit 1 grunt moving 200 85 0 0 0 0 0 0 none 0 12\n"
                + "unit 3 bolt placed 0 0 3200 6 3 2 9 15 hitscan 0 0\n");

            var layout = TowerLayout.Parse("tower 3 3 3\n", types);
            var wave = WaveScript.Parse("order 0 1 4 0\n", types);

            // The rules are handed over and never consulted: this table is
            // written in the column layout that has no types in it, so every
            // shot in this match resolves to its roll.
            var match = new Match(map, StreamingContent.ReadRuleset(), layout, wave, seed: 0x5EED1234u);
            var hashes = new List<ulong>();

            while (!match.IsFinished && hashes.Count < 4000)
            {
                match.Advance(1);
                hashes.Add(match.StateHash.Value);

                if (rig != null)
                {
                    rig.Orbit(11f, 3f);
                    rig.Dolly(0.05f);
                    rig.Fly(new Vector3(0.2f, 0.05f, 0.3f));
                }
            }

            return hashes.ToArray();
        }

        /// <summary>The four corners of a bounds in the ground plane.</summary>
        private static IEnumerable<Vector3> GroundCorners(Bounds floor)
        {
            yield return new Vector3(floor.min.x, 0f, floor.min.z);
            yield return new Vector3(floor.min.x, 0f, floor.max.z);
            yield return new Vector3(floor.max.x, 0f, floor.min.z);
            yield return new Vector3(floor.max.x, 0f, floor.max.z);
        }
    }
}
