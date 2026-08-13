using UnityEngine;
using UnityEngine.InputSystem;

namespace View
{
    /// <summary>
    /// A perspective camera on a pivot at the middle of the floor, orbited
    /// freely with the mouse and dollied in and out along its own axis.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Perspective, so that dollying goes into the scene.</b> Zooming an
    /// orthographic camera narrows the window onto the board and crops it;
    /// moving a perspective camera towards the pivot walks it in until one
    /// creep fills the frame. The cost is that the board is no longer
    /// isometric-exact — the far end of the corridor converges — and that is
    /// accepted.
    /// </para>
    /// <para>
    /// <b>Pitch is not clamped.</b> Orbiting past the top or under the floor
    /// is allowed and produces the upside-down and from-below views it
    /// implies. A limit here would be a guess at which angles are worth having,
    /// and the point of a free camera is that nobody has to make that guess.
    /// </para>
    /// <para>
    /// <b>This class cannot reach the simulation, and that is checked rather
    /// than intended.</b> Nothing in this file names a type from
    /// <c>Sim.dll</c>: the rig is built from a <see cref="Bounds"/> and a
    /// <see cref="Transform"/>, both of which are engine types the simulation
    /// has never heard of. Where somebody is looking therefore cannot change
    /// what happens, by construction — there is no argument, field or method
    /// here through which a yaw could reach a tick. A test asserts the absence,
    /// and a second test runs a whole match while orbiting and requires the
    /// per-tick state hashes to be identical.
    /// </para>
    /// <para>
    /// <b>The rig has no clock of its own.</b> The reset ease is stepped by
    /// <see cref="Advance"/>, which is handed the seconds that have passed;
    /// <see cref="Update"/> is the only thing that passes it a frame's worth,
    /// and the frame capture and the tests drive it by hand.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class OrbitCameraRig : MonoBehaviour
    {
        private float _yaw;
        private float _pitch;
        private float _distance;
        private float _framedDistance;

        private bool _easing;
        private float _easeSeconds;
        private float _easeFromYaw;
        private float _easeFromPitch;
        private float _easeFromDistance;

        /// <summary>The camera this rig carries. There is exactly one.</summary>
        public Camera Camera { get; private set; }

        /// <summary>The heading the camera orbits at, in degrees, in <c>0..360</c>.</summary>
        public float Yaw => _yaw;

        /// <summary>
        /// The camera's downward tilt, in degrees, in <c>0..360</c>. Values
        /// past 90 look at the board from behind the top of the arc, and values
        /// past 180 look at it from underneath.
        /// </summary>
        public float Pitch => _pitch;

        /// <summary>How far the camera sits from the pivot, in metres.</summary>
        public float Distance => _distance;

        /// <summary>
        /// The distance the whole floor fits at. Where <see cref="ResetView"/>
        /// returns to, and the scale the dolly's far limit is a multiple of.
        /// </summary>
        public float FramedDistance => _framedDistance;

        /// <summary>Whether a <see cref="ResetView"/> is still running.</summary>
        public bool IsEasing => _easing;

        /// <summary>The point the camera orbits — the middle of the floor.</summary>
        public Vector3 Pivot => transform.position;

        /// <summary>
        /// Builds the rig and its camera under <paramref name="parent"/>,
        /// framed on <paramref name="floor"/>.
        /// </summary>
        public static OrbitCameraRig Build(Transform parent, Bounds floor)
        {
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(parent, worldPositionStays: false);
            pivot.transform.position = new Vector3(floor.center.x, 0f, floor.center.z);

            var rig = pivot.AddComponent<OrbitCameraRig>();

            var host = new GameObject("Camera");
            host.transform.SetParent(pivot.transform, worldPositionStays: false);
            host.transform.localRotation = Quaternion.identity;

            var camera = host.AddComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = SceneFraming.CameraFieldOfViewDegrees;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneFraming.BackgroundColor;
            camera.nearClipPlane = SceneFraming.CameraNearClip;
            camera.farClipPlane = SceneFraming.CameraFarClip;

            rig.Camera = camera;
            rig._framedDistance = FitDistance(floor, camera.aspect, camera.fieldOfView);
            rig.PointAt(
                SceneFraming.CameraDefaultYawDegrees,
                SceneFraming.CameraDefaultPitchDegrees,
                rig._framedDistance);

            return rig;
        }

        /// <summary>
        /// How far back the camera has to sit for a floor of this size to fit
        /// on screen at the default pitch, with the committed margin.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The floor is framed by its circumscribed radius rather than its
        /// width and depth, because the circumscribed radius is what does not
        /// change when the camera orbits, and the margin is applied to that
        /// radius so both axes get it.
        /// </para>
        /// <para>
        /// Under perspective the demand is set by the near edge of the floor,
        /// not by its middle. A ground point <c>r</c> in front of the pivot
        /// sits <c>r·cos(pitch)</c> nearer the camera along the view axis and
        /// <c>r·sin(pitch)</c> below its centre line, so it clears the frustum
        /// when <c>r·sin(pitch) / (d − r·cos(pitch)) ≤ tan(halfAngle)</c>. That
        /// rearranges to the sum below: the <c>r·cos(pitch)</c> term buys back
        /// the depth the near edge takes away, and the larger of the two
        /// half-angle demands sets the rest. Horizontally the ground plane is
        /// not foreshortened at all, so its demand uses the full radius.
        /// </para>
        /// </remarks>
        public static float FitDistance(Bounds floor, float aspect, float fieldOfViewDegrees)
        {
            float radius = SceneFraming.CameraFramingMargin
                * 0.5f * new Vector2(floor.size.x, floor.size.z).magnitude;

            float pitch = SceneFraming.CameraDefaultPitchDegrees * Mathf.Deg2Rad;
            float halfVertical = 0.5f * fieldOfViewDegrees * Mathf.Deg2Rad;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * Mathf.Max(aspect, 0.01f));

            float vertical = radius * Mathf.Sin(pitch) / Mathf.Tan(halfVertical);
            float horizontal = radius / Mathf.Tan(halfHorizontal);

            return (radius * Mathf.Cos(pitch)) + Mathf.Max(vertical, horizontal);
        }

        /// <summary>
        /// Puts the camera at an angle and a distance at once, cancelling any
        /// reset in flight. The distance is held inside the dolly's limits.
        /// </summary>
        public void PointAt(float yawDegrees, float pitchDegrees, float distance)
        {
            _easing = false;
            _yaw = Mathf.Repeat(yawDegrees, 360f);
            _pitch = Mathf.Repeat(pitchDegrees, 360f);
            _distance = ClampDistance(distance);

            Apply();
        }

        /// <summary>
        /// Turns the rig. Both angles are free: yaw wraps and pitch is not
        /// clamped, so orbiting far enough goes over the top and underneath.
        /// </summary>
        public void Orbit(float yawDegrees, float pitchDegrees) =>
            PointAt(_yaw + yawDegrees, _pitch + pitchDegrees, _distance);

        /// <summary>
        /// Moves the camera along its own axis. Positive goes in. The step is
        /// exponential, so one notch covers the same fraction of the remaining
        /// distance whether the camera is across the board or on top of a
        /// creep.
        /// </summary>
        public void Dolly(float steps) => PointAt(_yaw, _pitch, _distance * Mathf.Exp(-steps));

        /// <summary>
        /// Starts the ease back to the default angle and the framed distance.
        /// Any orbit or dolly before it finishes cancels it.
        /// </summary>
        public void ResetView()
        {
            _easing = true;
            _easeSeconds = 0f;
            _easeFromYaw = _yaw;
            _easeFromPitch = _pitch;
            _easeFromDistance = _distance;
        }

        /// <summary>
        /// Advances a reset in flight by <paramref name="seconds"/>. Does
        /// nothing when there is no reset running.
        /// </summary>
        /// <remarks>
        /// Both angles are interpolated the short way round, which is what
        /// stops an unclamped pitch of 350 degrees unwinding the long way back
        /// to 35.
        /// </remarks>
        public void Advance(float seconds)
        {
            if (!_easing)
            {
                return;
            }

            _easeSeconds += Mathf.Max(seconds, 0f);

            float progress = SceneFraming.CameraResetSeconds <= 0f
                ? 1f
                : Mathf.Clamp01(_easeSeconds / SceneFraming.CameraResetSeconds);

            // Smoothstep: zero slope at both ends, so the camera leaves and
            // arrives without a visible kick.
            float eased = progress * progress * (3f - (2f * progress));

            _yaw = Mathf.Repeat(
                Mathf.LerpAngle(_easeFromYaw, SceneFraming.CameraDefaultYawDegrees, eased), 360f);
            _pitch = Mathf.Repeat(
                Mathf.LerpAngle(_easeFromPitch, SceneFraming.CameraDefaultPitchDegrees, eased), 360f);
            _distance = Mathf.Lerp(_easeFromDistance, _framedDistance, eased);

            if (progress >= 1f)
            {
                _easing = false;
                _yaw = Mathf.Repeat(SceneFraming.CameraDefaultYawDegrees, 360f);
                _pitch = Mathf.Repeat(SceneFraming.CameraDefaultPitchDegrees, 360f);
                _distance = _framedDistance;
            }

            Apply();
        }

        /// <summary>
        /// Holds the distance between the closest the camera may get to the
        /// pivot and the committed multiple of the framed distance.
        /// </summary>
        private float ClampDistance(float distance) =>
            Mathf.Clamp(
                distance,
                SceneFraming.CameraMinDistance,
                Mathf.Max(SceneFraming.CameraMinDistance, _framedDistance * SceneFraming.CameraMaxDistanceFactor));

        /// <summary>
        /// Writes the three numbers onto the two transforms: the pivot carries
        /// the angle and the camera hangs behind it at the distance.
        /// </summary>
        private void Apply()
        {
            transform.rotation = SceneFraming.CameraRotation(_yaw, _pitch);

            if (Camera != null)
            {
                Camera.transform.localPosition = new Vector3(0f, 0f, -_distance);
            }
        }

        /// <summary>
        /// Reads the mouse and the reset key. This is the only input in the
        /// view outside the playback bar, and it reaches exactly three numbers:
        /// this rig's yaw, pitch and distance.
        /// </summary>
        /// <remarks>
        /// Orbit is on the right button so that the left one stays free for
        /// picking a hex.
        /// </remarks>
        private void Update()
        {
            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                {
                    Vector2 drag = mouse.delta.ReadValue();

                    if (drag != Vector2.zero)
                    {
                        Orbit(
                            drag.x * SceneFraming.CameraOrbitDegreesPerPixel,
                            drag.y * SceneFraming.CameraOrbitDegreesPerPixel);
                    }
                }

                float scroll = mouse.scroll.ReadValue().y;

                if (scroll != 0f)
                {
                    Dolly(scroll * SceneFraming.CameraDollyPerScrollUnit);
                }
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.fKey.wasPressedThisFrame)
            {
                ResetView();
            }

            Advance(Time.deltaTime);
        }
    }
}
