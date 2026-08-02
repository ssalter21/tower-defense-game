using UnityEngine;
using UnityEngine.InputSystem;

namespace View
{
    /// <summary>
    /// A fixed isometric orthographic camera on a pivot, orbitable in six
    /// snapped steps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Six steps, because the floor is hexagonal.</b> Sixty degrees is the
    /// angle at which a hex grid maps onto itself, so every snap is a view of
    /// the same playfield from a side it genuinely has. Free orbit was never
    /// wanted: a snapped camera has six framings that can each be looked at and
    /// judged, and a free one has infinitely many that nobody ever looks at
    /// twice.
    /// </para>
    /// <para>
    /// <b>This class cannot reach the simulation, and that is checked rather
    /// than intended.</b> Nothing in this file names a type from
    /// <c>Sim.dll</c>: the rig is built from a <see cref="Bounds"/> and a
    /// <see cref="Transform"/>, both of which are engine types the simulation
    /// has never heard of. Where somebody is looking therefore cannot change
    /// what happens, by construction — there is no argument, field or method
    /// here through which a yaw could reach a tick. A test asserts the absence,
    /// and a second test runs a whole match at every snap and requires the
    /// per-tick state hashes to be identical.
    /// </para>
    /// <para>
    /// The orthographic size is fitted to the floor's circumscribed radius,
    /// which is the same at every yaw. That is deliberate: framing that changed
    /// as the camera orbited would make one snap the "real" one and the other
    /// five approximations of it.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        private int _snap;

        /// <summary>The camera this rig carries. There is exactly one.</summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// Which of the six snaps the camera is at, always in <c>0..5</c>.
        /// </summary>
        public int Snap => _snap;

        /// <summary>The point the camera orbits — the middle of the floor.</summary>
        public Vector3 Pivot => transform.position;

        /// <summary>
        /// Builds the rig and its camera under <paramref name="parent"/>,
        /// framed on <paramref name="floor"/>.
        /// </summary>
        public static IsometricCameraRig Build(Transform parent, Bounds floor)
        {
            var pivot = new GameObject("CameraPivot");
            pivot.transform.SetParent(parent, worldPositionStays: false);
            pivot.transform.position = new Vector3(floor.center.x, 0f, floor.center.z);

            var rig = pivot.AddComponent<IsometricCameraRig>();

            var host = new GameObject("Camera");
            host.transform.SetParent(pivot.transform, worldPositionStays: false);
            host.transform.localPosition = new Vector3(0f, 0f, -SceneFraming.CameraDistance);
            host.transform.localRotation = Quaternion.identity;

            var camera = host.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = SceneFraming.BackgroundColor;
            camera.nearClipPlane = SceneFraming.CameraNearClip;
            camera.farClipPlane = SceneFraming.CameraFarClip;
            camera.orthographicSize = FitOrthographicSize(floor, camera.aspect);

            rig.Camera = camera;
            rig.SnapTo(0);

            return rig;
        }

        /// <summary>
        /// The orthographic size that fits a floor of this size on screen from
        /// any yaw, with the committed margin.
        /// </summary>
        /// <remarks>
        /// The floor is framed by its circumscribed radius rather than its
        /// width and depth, because the circumscribed radius is what does not
        /// change when the camera orbits. Vertically the ground plane is
        /// foreshortened by the sine of the pitch; horizontally it is not
        /// foreshortened at all, so the wider of those two demands wins.
        /// </remarks>
        public static float FitOrthographicSize(Bounds floor, float aspect)
        {
            float radius = 0.5f * new Vector2(floor.size.x, floor.size.z).magnitude;
            float vertical = radius * Mathf.Sin(SceneFraming.CameraPitchDegrees * Mathf.Deg2Rad);
            float horizontal = radius / Mathf.Max(aspect, 0.01f);

            return SceneFraming.CameraFramingMargin * Mathf.Max(vertical, horizontal);
        }

        /// <summary>
        /// Puts the camera at a snap. Any integer is legal and wraps, so
        /// stepping past the last snap is stepping back to the first.
        /// </summary>
        public void SnapTo(int snap)
        {
            _snap = SceneFraming.Wrap(snap);
            transform.rotation = SceneFraming.CameraRotation(_snap);
        }

        /// <summary>Orbits by whole snaps. Negative goes the other way.</summary>
        public void Rotate(int steps) => SnapTo(_snap + steps);

        /// <summary>
        /// Reads the orbit keys. This is the only input in the view, and it
        /// reaches exactly one thing: this transform's rotation.
        /// </summary>
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
            {
                Rotate(-1);
            }

            if (keyboard.eKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
            {
                Rotate(1);
            }
        }
    }
}
