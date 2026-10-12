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

            Assert.That(
                others.Length,
                Is.GreaterThanOrEqualTo(root.Floor.TileCount + 1),
                "The floor should be under here: every road piece and the skin.");

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
