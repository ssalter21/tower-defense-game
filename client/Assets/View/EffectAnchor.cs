using System;
using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Where a unit's shots leave its art from: a transform named on the built
    /// body — a bone, or a node inside whatever the body is holding — and
    /// optionally the far end of that transform's own geometry, which is what
    /// makes a staff tip a staff tip rather than a fist.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The name is looked up across the whole built body, props
    /// included.</b> A held thing is parented onto a bone by
    /// <see cref="WeaponSocket"/> before this resolves, so a bone name and a
    /// node inside a prop are the same kind of lookup and take the same field.
    /// One name that finds nothing throws and names itself; the alternative —
    /// falling back to the root — draws the flash out of the tower's navel and
    /// reads as an art problem from every angle except the one that shows it is
    /// a misspelt string.
    /// </para>
    /// <para>
    /// <b>The tip is a direction, and the distance comes off the asset.</b>
    /// Authoring metres here would be this project holding an opinion about how
    /// long a staff is, which the staff already knows; a direction says which
    /// end, and the mesh says where that end is. So a re-exported prop moves its
    /// own tip and nothing here has to be re-measured.
    /// </para>
    /// <para>
    /// <b>This is for decorations only, and a projectile may not use it.</b>
    /// Muzzle flashes and tracers are events, drawn and forgotten, and an event
    /// may trigger only the purely decorative. A projectile is a real snapshot
    /// entity, and where a snapshot entity is drawn is not decorative — see
    /// ADR-0007 and <see cref="ProjectileView"/>. The snapshot does not say
    /// which tower fired a shell in any case: a projectile carries its type and
    /// its target and there are two mortars, so there is no anchor in the
    /// snapshot to fly out of. Reading the firing tower off the
    /// <c>TowerFired</c> event to start a shell here would put a scrub-visible
    /// position on an event stream that seeks discard, which is the one thing
    /// this architecture exists to make impossible. A shell's origin stays
    /// derived from its target's own position.
    /// </para>
    /// </remarks>
    [Serializable]
    public struct EffectAnchor
    {
        [SerializeField]
        [Tooltip("A transform on the model or on something it holds. Empty for a unit that never fires.")]
        private string transformName;

        [SerializeField]
        [Tooltip("Which way along that transform's own geometry its far end lies. Zero is the origin.")]
        private Vector3 tip;

        /// <summary>A unit whose shots have nowhere of their own to leave from.</summary>
        public static EffectAnchor None => default;

        /// <summary>The origin of the transform with this name.</summary>
        public static EffectAnchor At(string transformName) =>
            new EffectAnchor { transformName = transformName, tip = Vector3.zero };

        /// <summary>
        /// The far end of that transform's geometry, along one axis of its own
        /// local space. A direction that is not along an axis is refused when
        /// the anchor resolves.
        /// </summary>
        public static EffectAnchor AtTipOf(string transformName, Vector3 direction) =>
            new EffectAnchor { transformName = transformName, tip = direction };

        /// <summary>
        /// What the anchor is named after, or null when it names nothing.
        /// </summary>
        /// <remarks>
        /// Null for the empty name too, because those are the same anchor
        /// written down twice: a serialized string field holds <c>""</c> where
        /// an unset one in memory holds null, and a caller comparing the art in
        /// a generated asset against the art it was generated from would
        /// otherwise read a difference in every row that anchors nothing.
        /// </remarks>
        public string TransformName => IsSet ? transformName : null;

        /// <summary>
        /// Which way its far end lies, in that transform's local space. Zero is
        /// the origin, and an anchor that names nothing has no end to point at.
        /// </summary>
        public Vector3 Tip => IsSet ? tip : Vector3.zero;

        /// <summary>True when this names anything at all.</summary>
        public bool IsSet => !string.IsNullOrEmpty(transformName);

        /// <summary>
        /// Finds the anchor on an instantiated body, once, and works out the
        /// point on it. Everything after this is arithmetic on a transform that
        /// the rig moves.
        /// </summary>
        /// <param name="body">
        /// The instantiated model, with whatever it holds already parented onto
        /// its bones.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The name finds nothing, or a tip was asked for on something carrying
        /// no geometry to have a far end.
        /// </exception>
        public AnchoredPoint ResolveOn(GameObject body)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            if (!IsSet)
            {
                return default;
            }

            Transform at = WeaponSocket.FindBone(body, transformName);

            if (at == null)
            {
                throw new InvalidOperationException(
                    "No transform named '" + transformName + "' on " + body.name + ", so its shots have "
                    + "nowhere to leave from. An effect anchor names a bone or a node inside something the "
                    + "unit holds, and the thing it holds is parented on before this runs — so a name that "
                    + "finds nothing is a misspelling or a weapon that was never attached.");
            }

            return new AnchoredPoint(at, tip == Vector3.zero ? Vector3.zero : FarEndOf(at, tip));
        }

        /// <summary>
        /// The middle of the face of <paramref name="at"/>'s own geometry that
        /// <paramref name="direction"/> points at, in <paramref name="at"/>'s
        /// local space.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every mesh under the anchor, each one's own local bounds carried into
        /// the anchor's frame corner by corner. Taking a renderer's world
        /// <c>bounds</c> instead would be a box drawn round a box — an axis
        /// aligned box in world space, re-expressed in a frame it is not aligned
        /// to — which is how the staff's first measurement could not tell an orb
        /// from a sword tip.
        /// </para>
        /// <para>
        /// <b>The middle of a face and not a corner</b>, which is why the
        /// direction has to lie along an axis and is refused when it does not.
        /// A shaft's tip sits on the shaft's own centre line, and the corner of
        /// the box round it is off to one side by half the widest thing on the
        /// weapon — a few millimetres on a staff and most of a crossguard on a
        /// sword. Scaling the extents by a diagonal, meanwhile, lands
        /// <i>inside</i> the box and is not an end at all. Both are silent
        /// wrong answers, so the case that has a right one is the only case
        /// this takes.
        /// </para>
        /// <para>
        /// The subtree rather than the transform itself, because a prop arrives
        /// as an instantiated FBX whose root is an empty node and whose mesh is
        /// the child under it.
        /// </para>
        /// </remarks>
        private static Vector3 FarEndOf(Transform at, Vector3 direction)
        {
            if (!IsAlongOneAxis(direction))
            {
                throw new InvalidOperationException(
                    "The anchor on '" + at.name + "' points its far end along " + direction + ", which is "
                    + "not one of the six axes. An end is the middle of the face a direction points at, and "
                    + "a direction between two faces does not name one.");
            }

            Bounds? enclosing = null;

            foreach (Renderer renderer in at.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh = MeshOf(renderer);

                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 intoAnchor = at.worldToLocalMatrix * renderer.transform.localToWorldMatrix;

                foreach (Vector3 corner in CornersOf(mesh.bounds))
                {
                    Vector3 local = intoAnchor.MultiplyPoint3x4(corner);

                    enclosing = enclosing == null
                        ? new Bounds(local, Vector3.zero)
                        : Grown(enclosing.Value, local);
                }
            }

            if (enclosing == null)
            {
                throw new InvalidOperationException(
                    "The anchor on '" + at.name + "' asks for the far end of it, and nothing under it has "
                    + "a mesh to have ends. Anchor on the origin instead, or name the part that is drawn.");
            }

            Bounds box = enclosing.Value;

            return box.center + Vector3.Scale(box.extents, Sign(direction));
        }

        /// <summary>
        /// True when exactly one component is non-zero — a direction that names
        /// one face of a box rather than pointing between two of them.
        /// </summary>
        private static bool IsAlongOneAxis(Vector3 direction)
        {
            var axes = 0;

            if (direction.x != 0f) axes++;
            if (direction.y != 0f) axes++;
            if (direction.z != 0f) axes++;

            return axes == 1;
        }

        /// <summary>
        /// Which way along that one axis, as -1 or 1. Length is thrown away:
        /// how far the end is comes off the mesh, so the direction only ever
        /// says which end. That also keeps a direction too short for
        /// <c>Vector3.normalized</c> to survive from quietly becoming the
        /// middle of the box.
        /// </summary>
        private static Vector3 Sign(Vector3 direction) =>
            new Vector3(
                Mathf.Sign(direction.x) * (direction.x == 0f ? 0f : 1f),
                Mathf.Sign(direction.y) * (direction.y == 0f ? 0f : 1f),
                Mathf.Sign(direction.z) * (direction.z == 0f ? 0f : 1f));

        /// <summary>The mesh a renderer draws, through either import path, or null.</summary>
        private static Mesh MeshOf(Renderer renderer)
        {
            if (renderer is SkinnedMeshRenderer skinned)
            {
                return skinned.sharedMesh;
            }

            var filter = renderer.GetComponent<MeshFilter>();

            return filter == null ? null : filter.sharedMesh;
        }

        private static IEnumerable<Vector3> CornersOf(Bounds box)
        {
            Vector3 middle = box.center;
            Vector3 half = box.extents;

            for (var corner = 0; corner < 8; corner++)
            {
                yield return middle + new Vector3(
                    (corner & 1) == 0 ? -half.x : half.x,
                    (corner & 2) == 0 ? -half.y : half.y,
                    (corner & 4) == 0 ? -half.z : half.z);
            }
        }

        private static Bounds Grown(Bounds box, Vector3 point)
        {
            box.Encapsulate(point);

            return box;
        }
    }

    /// <summary>
    /// An <see cref="EffectAnchor"/> after it has been found on a built body:
    /// a transform the rig moves, and a fixed point in its local space.
    /// </summary>
    /// <remarks>
    /// Resolved once and then read every time something fires, so the lookup by
    /// name happens when the view is built and never in a tick. What it reads
    /// afterwards is the transform's current pose, so a drawn bow's anchor
    /// follows the arm.
    /// </remarks>
    public readonly struct AnchoredPoint
    {
        private readonly Transform _at;

        private readonly Vector3 _local;

        internal AnchoredPoint(Transform at, Vector3 local)
        {
            _at = at;
            _local = local;
        }

        /// <summary>The transform it was found on, or null when nothing was named.</summary>
        public Transform At => _at;

        /// <summary>True when there is a point to read.</summary>
        public bool IsSet => _at != null;

        /// <summary>Where it is in the world right now.</summary>
        /// <exception cref="InvalidOperationException">Nothing was named.</exception>
        public Vector3 Position => IsSet
            ? _at.TransformPoint(_local)
            : throw new InvalidOperationException(
                "This anchor names nothing, so it has no position. Ask IsSet first.");
    }
}
