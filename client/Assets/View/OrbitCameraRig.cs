using UnityEngine;
using UnityEngine.InputSystem;

namespace View
{
    /// <summary>
    /// A perspective camera on a pivot that starts at the middle of the floor,
    /// orbited freely with the mouse, dollied in and out along its own axis,
    /// and flown anywhere with the keyboard.
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
    /// <b>Pitch is not clamped, and neither is the pivot.</b> Orbiting past the
    /// top or under the floor is allowed and produces the upside-down and
    /// from-below views it implies; flying goes off the edge of the board and
    /// under it. A limit on either would be a guess at which views are worth
    /// having, and the point of a free camera is that nobody has to make that
    /// guess. The cost is that a long enough flight leaves the board outside
    /// the far plane with nothing on screen to say which way home is, and
    /// <see cref="ResetView"/> is the answer to that.
    /// </para>
    /// <para>
    /// <b>This class cannot reach the simulation, and that is checked rather
    /// than intended.</b> Nothing in this file names a type from
    /// <c>Sim.dll</c>: the rig is built from a <see cref="Bounds"/> and a
    /// <see cref="Transform"/>, both of which are engine types the simulation
    /// has never heard of. Where somebody is looking therefore cannot change
    /// what happens, by construction — there is no argument, field or method
    /// here through which a yaw could reach a tick. A test asserts the absence,
    /// and a second test runs a whole match while orbiting and flying and
    /// requires the per-tick state hashes to be identical.
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
        private Vector3 _position;
        private float _yaw;
        private float _pitch;
        private float _distance;

        private Vector3 _framedPivot;
        private float _framedDistance;

        private bool _easing;
        private float _easeSeconds;
        private Vector3 _easeFromPosition;
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

        /// <summary>
        /// The point the camera orbits. Starts at the middle of the floor and
        /// goes wherever <see cref="Fly"/> takes it, on or off the board.
        /// </summary>
        public Vector3 Pivot => _position;

        /// <summary>
        /// The middle of the floor, in the ground plane. Where
        /// <see cref="ResetView"/> brings the pivot back to.
        /// </summary>
        public Vector3 FramedPivot => _framedPivot;

        /// <summary>
        /// Builds the rig and its camera under <paramref name="parent"/>,
        /// framed on <paramref name="floor"/>.
        /// </summary>
        public static OrbitCameraRig Build(Transform parent, Bounds floor)
        {
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(parent, worldPositionStays: false);

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
            rig.Reframe(floor);

            return rig;
        }

        /// <summary>
        /// Fits the framed distance to <paramref name="floor"/> at the camera's
        /// current aspect and puts the camera back at the default view — the
        /// middle of that floor, the default angle, and the distance the whole
        /// of it fits at.
        /// </summary>
        /// <remarks>
        /// Called once by <see cref="Build"/>, and again by anything that sets
        /// the camera's aspect for itself afterwards — the frame capture renders
        /// into a texture of its own shape, and a framed distance still measured
        /// against the aspect a headless editor happened to report would be
        /// wrong by whatever the two disagree by. It puts the pivot back as well
        /// as the angle, so a capture taken after somebody has flown somewhere
        /// is the same picture as one taken before.
        /// </remarks>
        public void Reframe(Bounds floor)
        {
            _framedPivot = new Vector3(floor.center.x, 0f, floor.center.z);
            _framedDistance = FitDistance(floor, Camera.aspect);

            PointAt(
                _framedPivot,
                SceneFraming.CameraDefaultYawDegrees,
                SceneFraming.CameraDefaultPitchDegrees,
                _framedDistance);
        }

        /// <summary>
        /// How far back the camera has to sit for a floor of this size to fit
        /// on screen at the default angle, with the committed margin.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It frames the floor's footprint at the default heading, not its
        /// circumscribed circle.</b> Fitting the circle would frame every
        /// heading at once, and on a corridor four times longer than it is
        /// deep that costs a third of the picture to hold room for a view
        /// nobody has opened on. Orbiting a quarter turn from here does push
        /// the near end of the corridor off the bottom of the frame — and the
        /// answer to that is the dolly, which is the whole point of the camera
        /// being free.
        /// </para>
        /// <para>
        /// <b>Under perspective the demand is set by the near edge of the
        /// floor, not by its middle.</b> The floor's near edge, half its depth
        /// <c>z</c> in front of the pivot, sits <c>z·cos(pitch)</c> nearer the
        /// camera along the view axis and <c>z·sin(pitch)</c> below its centre
        /// line, so it clears the frustum when
        /// <c>z·sin(pitch) / (d − z·cos(pitch)) ≤ tan(halfAngle)</c>. That
        /// rearranges to the sum below: the <c>z·cos(pitch)</c> term buys back
        /// the depth the near edge takes away, and the larger of the two
        /// half-angle demands sets the rest. The horizontal demand is measured
        /// on that same near edge, because that is where the floor is widest on
        /// screen, but it uses the full half-width — the ground plane is not
        /// foreshortened sideways at all.
        /// </para>
        /// </remarks>
        private static float FitDistance(Bounds floor, float aspect)
        {
            float halfWidth = SceneFraming.CameraFramingMargin * 0.5f * floor.size.x;
            float halfDepth = SceneFraming.CameraFramingMargin * 0.5f * floor.size.z;

            float pitch = SceneFraming.CameraDefaultPitchDegrees * Mathf.Deg2Rad;
            float halfVertical = 0.5f * SceneFraming.CameraFieldOfViewDegrees * Mathf.Deg2Rad;
            float halfHorizontal = Mathf.Atan(Mathf.Tan(halfVertical) * Mathf.Max(aspect, 0.01f));

            float vertical = halfDepth * Mathf.Sin(pitch) / Mathf.Tan(halfVertical);
            float horizontal = halfWidth / Mathf.Tan(halfHorizontal);

            return (halfDepth * Mathf.Cos(pitch)) + Mathf.Max(vertical, horizontal);
        }

        /// <summary>
        /// Puts the camera at an angle and a distance at once, leaving the
        /// pivot where it is.
        /// </summary>
        public void PointAt(float yawDegrees, float pitchDegrees, float distance) =>
            PointAt(_position, yawDegrees, pitchDegrees, distance);

        /// <summary>
        /// Writes the whole of the rig's state at once, cancelling any reset in
        /// flight. The distance is held inside the dolly's limits; the pivot is
        /// held nowhere, and may be off the board or under it.
        /// </summary>
        public void PointAt(Vector3 pivot, float yawDegrees, float pitchDegrees, float distance)
        {
            _easing = false;
            _position = pivot;
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
        /// Moves the pivot. <paramref name="step"/> is read as right, up and
        /// forward, where forward is the heading the camera is looking along
        /// flattened into the ground plane — so flying stays intuitive after a
        /// half turn — and each component is a fraction of the current distance
        /// rather than a length in metres, so one press covers the same part of
        /// the picture at every zoom.
        /// </summary>
        /// <remarks>
        /// The heading rotation is about <c>Y</c> alone, which is what leaves
        /// <c>step.y</c> pointing at the sky however far the camera has been
        /// tilted or turned.
        /// </remarks>
        public void Fly(Vector3 step) =>
            PointAt(
                _position + (SceneFraming.CameraRotation(_yaw, 0f) * step * _distance),
                _yaw,
                _pitch,
                _distance);

        /// <summary>
        /// Starts the ease back to the default view: the middle of the floor,
        /// the default angle and the framed distance, all three together. Any
        /// orbit, dolly or flight before it finishes cancels it.
        /// </summary>
        public void ResetView()
        {
            _easing = true;
            _easeSeconds = 0f;
            _easeFromPosition = _position;
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

            _position = Vector3.Lerp(_easeFromPosition, _framedPivot, eased);
            _yaw = Mathf.Repeat(
                Mathf.LerpAngle(_easeFromYaw, SceneFraming.CameraDefaultYawDegrees, eased), 360f);
            _pitch = Mathf.Repeat(
                Mathf.LerpAngle(_easeFromPitch, SceneFraming.CameraDefaultPitchDegrees, eased), 360f);
            _distance = Mathf.Lerp(_easeFromDistance, _framedDistance, eased);

            if (progress >= 1f)
            {
                _easing = false;
                _position = _framedPivot;
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
        /// Writes the state onto the two transforms: the pivot carries the
        /// position and the angle, and the camera hangs behind it at the
        /// distance.
        /// </summary>
        private void Apply()
        {
            transform.position = _position;
            transform.rotation = SceneFraming.CameraRotation(_yaw, _pitch);

            if (Camera != null)
            {
                Camera.transform.localPosition = new Vector3(0f, 0f, -_distance);
            }
        }

        /// <summary>
        /// Reads the mouse and the keyboard. This is the only input in the view
        /// outside the playback bar, and it reaches exactly this rig's pivot,
        /// yaw, pitch and distance.
        /// </summary>
        /// <remarks>
        /// Orbit is on the right button so that the left one stays free for
        /// picking a hex, and flight is on the letter keys because the number
        /// row is the tower palette. Middle-drag pans as well, for a hand
        /// already on the mouse.
        /// </remarks>
        private void Update()
        {
            Mouse mouse = Mouse.current;

            if (mouse != null)
            {
                Vector2 drag = mouse.delta.ReadValue();

                if (mouse.rightButton.isPressed && drag != Vector2.zero)
                {
                    Orbit(
                        drag.x * SceneFraming.CameraOrbitDegreesPerPixel,
                        drag.y * SceneFraming.CameraOrbitDegreesPerPixel);
                }

                // Negated, so the board follows the cursor rather than running
                // away from it.
                if (mouse.middleButton.isPressed && drag != Vector2.zero)
                {
                    Fly(
                        new Vector3(-drag.x, 0f, -drag.y)
                        * SceneFraming.CameraPanDistanceFractionPerPixel);
                }

                float scroll = mouse.scroll.ReadValue().y;

                if (scroll != 0f)
                {
                    Dolly(scroll * SceneFraming.CameraDollyPerScrollUnit);
                }
            }

            Keyboard keyboard = Keyboard.current;

            if (keyboard != null)
            {
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    ResetView();
                }

                Vector3 held = HeldFlight(keyboard);

                if (held != Vector3.zero)
                {
                    Fly(
                        held * (SceneFraming.CameraFlyDistanceFractionPerSecond * Time.deltaTime));
                }
            }

            Advance(Time.deltaTime);
        }

        /// <summary>
        /// The flight direction the held keys add up to, as right, up and
        /// forward: <c>WASD</c> across the ground plane and <c>E</c> and
        /// <c>Q</c> up and down. Clamped to a unit length, so holding two keys
        /// goes diagonally rather than half again as fast.
        /// </summary>
        private static Vector3 HeldFlight(Keyboard keyboard)
        {
            var step = Vector3.zero;

            step.x += keyboard.dKey.isPressed ? 1f : 0f;
            step.x -= keyboard.aKey.isPressed ? 1f : 0f;
            step.y += keyboard.eKey.isPressed ? 1f : 0f;
            step.y -= keyboard.qKey.isPressed ? 1f : 0f;
            step.z += keyboard.wKey.isPressed ? 1f : 0f;
            step.z -= keyboard.sKey.isPressed ? 1f : 0f;

            return Vector3.ClampMagnitude(step, 1f);
        }
    }
}
