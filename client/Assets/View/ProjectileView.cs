using UnityEngine;

namespace View
{
    /// <summary>
    /// One mortar shell in the air.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A projectile is a real snapshot entity, and that is the point of
    /// having one.</b> The obvious implementation — spawn a particle when a
    /// tower fires and let it fly on its own — produces something that cannot
    /// be scrubbed backwards through, because the event that created it is in
    /// the past and nothing will re-emit it. So the projectile is state, the
    /// snapshot carries it, and only its trail would ever be an event.
    /// </para>
    /// <para>
    /// <b>Homing is free because the simulation never computed a
    /// position.</b> A projectile in the snapshot is a countdown and a
    /// reference to a target. The view draws it somewhere between here and
    /// wherever that target is <i>in the snapshot being drawn right now</i>, so
    /// a creep that moved is tracked without a line of homing code, and a creep
    /// that died is not tracked at all — the projectile simply stopped
    /// appearing, and the pool took its object back by subtraction.
    /// </para>
    /// <para>
    /// <b>Where it comes from is derived, not remembered.</b> The snapshot does
    /// not say which tower fired a shell — a projectile carries its <i>type</i>
    /// and its target, and there are two mortars — so there is no muzzle in the
    /// snapshot to fly out of. Reading the firing tower off the
    /// <c>TowerFired</c> event would break the governing rule outright: events
    /// may trigger only the purely decorative, and where a snapshot entity is
    /// drawn is not decorative. So the origin is computed from the target's own
    /// position, up and back along the corridor, and the shell arcs down onto
    /// it. That is a pure function of the snapshot, it scrubs correctly, and it
    /// reads as a mortar because a mortar is what fired it.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ProjectileView : MonoBehaviour
    {
        /// <summary>The shell mesh, once built.</summary>
        public GameObject Model { get; private set; }

        /// <summary>Where the last <see cref="Pose"/> call put it. For tests.</summary>
        public Vector3 LastPosition { get; private set; }

        /// <summary>
        /// Builds the shell: real geometry at a real size, because the camera
        /// yaws through six snaps and anything flat would turn to face it.
        /// </summary>
        public void Build(Material material)
        {
            Model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Model.name = "Shell";
            Model.transform.SetParent(transform, worldPositionStays: false);
            Model.transform.localPosition = Vector3.zero;
            Model.transform.localScale = Vector3.one * (MatchTuning.ProjectileRadius * 2f);

            // The primitive arrives with a collider. Nothing in this project
            // uses physics -- every range, hit and splash question is an
            // interval test on a line inside the simulation -- so a collider
            // here would be a second, silent opinion about what touches what.
            Collider collider = Model.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Model.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        /// <summary>
        /// Puts the shell where its countdown says it is, on the way to
        /// <paramref name="target"/>.
        /// </summary>
        /// <param name="origin">
        /// Where the flight started, derived from the target's position rather
        /// than remembered from when the shot was fired.
        /// </param>
        /// <param name="target">Where its target is now.</param>
        /// <param name="fraction">
        /// How far through its flight it is, as
        /// <c>ticksInFlight / flightDurationTicks</c>.
        /// </param>
        public void Pose(Vector3 origin, Vector3 target, float fraction)
        {
            float travelled = Mathf.Clamp01(fraction);

            Vector3 position = Vector3.Lerp(origin, target, travelled);

            // A parabola that is zero at both ends, so the shell leaves and
            // arrives exactly where it should and only bulges in between.
            position.y += MatchTuning.ProjectileArcBulge * 4f * travelled * (1f - travelled);

            LastPosition = position;
            transform.position = position;
        }

        /// <summary>
        /// Where a shell aimed at <paramref name="target"/> starts from: up,
        /// and back along the corridor so it falls at an angle rather than down
        /// a wire.
        /// </summary>
        /// <remarks>
        /// Static and pure, so the caller can compute it from the same route it
        /// used to place the target, and so a test can check the flight without
        /// a game object in it.
        /// </remarks>
        public static Vector3 OriginFor(Vector3 target, Vector3 corridorTangent) =>
            target
            + (Vector3.up * MatchTuning.ProjectileApexHeight)
            - (corridorTangent * (MatchTuning.ProjectileLeadHexes * SimUnits.MetresPerHex));
    }
}
