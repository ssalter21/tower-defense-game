using System.Linq;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Hangs a held mesh — a bow, a sword — off a named bone of an already
    /// instantiated rig.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This exists because a weapon is the part of the asset pipeline that a
    /// single drag of a single file never exercises. A character is one import
    /// and a building is one import; a weapon is two imports that have to agree
    /// about a bone name, a local transform and an atlas, and every one of those
    /// three is a way for it to arrive floating beside the hand, inside the
    /// torso, or magenta.
    /// </para>
    /// <para>
    /// Zero local offset on purpose. The pack authors a bone whose entire job is
    /// to be where the held thing goes, so a hand-tuned nudge here would be this
    /// project quietly disagreeing with the artist — and would hide the import
    /// failure it looks like it is fixing.
    /// </para>
    /// </remarks>
    public static class WeaponSocket
    {
        /// <summary>
        /// The bone the bow goes on, and it is the LEFT hand.
        /// </summary>
        /// <remarks>
        /// Measured, not assumed: on <c>handslot.r</c> the bow's bounding box
        /// sits 100% inside the Ranger's own — an archer holding nothing —
        /// because every earlier weapon candidate on this rig was melee and every
        /// melee weapon is right-handed. On <c>handslot.l</c> the fist closes on
        /// the wrapped grip at the correct scale and roll. Both slots are keyed
        /// 1.0 throughout every clip, so this is not a scale bug. See issue #44.
        /// </remarks>
        public const string BowHand = "handslot.l";

        /// <summary>
        /// The bone a melee weapon goes on, and it is the RIGHT hand.
        /// </summary>
        /// <remarks>
        /// The other half of the measurement above. Every melee weapon on this
        /// rig is right-handed — which is why the bow, the one thing that is
        /// not, had to be measured onto the left before it would sit in a fist
        /// at all. A staff counts as melee here: it is gripped, not drawn.
        /// </remarks>
        public const string MeleeHand = "handslot.r";

        /// <summary>
        /// The bone an off-hand item goes on. Same bone as <see cref="BowHand"/>,
        /// named for what it means rather than for the one weapon that found it.
        /// </summary>
        /// <remarks>
        /// A shield and a bow never appear on the same unit, so one bone serves
        /// both. Two names because a reader of <c>OffHand</c> at a shield's call
        /// site should not have to know that the constant is spelled after a bow.
        /// </remarks>
        public const string OffHand = BowHand;

        /// <summary>
        /// Instantiates <paramref name="weapon"/> and parents it to the bone
        /// named <paramref name="boneName"/> on <paramref name="host"/>, at zero
        /// local offset. Returns the instantiated weapon.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">
        /// Either argument is null.
        /// </exception>
        /// <exception cref="System.InvalidOperationException">
        /// The host carries no bone by that name. Loud on purpose: a weapon
        /// silently parented to the rig's root reads as "the artist drew it
        /// wrong" from every angle except the one that would show it is a
        /// misspelt string.
        /// </exception>
        public static GameObject Attach(GameObject host, GameObject weapon, string boneName)
        {
            if (host == null) throw new System.ArgumentNullException(nameof(host));
            if (weapon == null) throw new System.ArgumentNullException(nameof(weapon));

            Transform bone = FindBone(host, boneName);

            if (bone == null)
            {
                throw new System.InvalidOperationException(
                    "No bone named '" + boneName + "' on " + host.name + ". Hand-like bones it does have: "
                    + string.Join(", ", HandLikeBoneNames(host)));
            }

            GameObject held = Object.Instantiate(weapon);
            held.name = weapon.name;
            held.transform.SetParent(bone, worldPositionStays: false);
            held.transform.localPosition = Vector3.zero;
            held.transform.localRotation = Quaternion.identity;

            // The prefab's own scale, NOT one. Measured on 14 August 2026: the
            // bow imports with a root scale of 100 and every other weapon in
            // this project with 1, so forcing one drew the bow at a hundredth
            // of its size -- two centimetres across, in the hand, invisible
            // from any camera. It had been that way since the bow was the only
            // weapon there was, and it never showed up because nobody had
            // opened the editor to look. Position and rotation are still the
            // bone's; size is the asset's.
            held.transform.localScale = weapon.transform.localScale;

            return held;
        }

        /// <summary>
        /// The transform named <paramref name="boneName"/> anywhere under
        /// <paramref name="host"/>, or null. Inactive children included: a rig
        /// arrives from an FBX with whatever active flags the artist left on it.
        /// </summary>
        public static Transform FindBone(GameObject host, string boneName) =>
            host.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == boneName);

        /// <summary>What to print when the named bone was not there.</summary>
        private static string[] HandLikeBoneNames(GameObject host) =>
            host.GetComponentsInChildren<Transform>(true)
                .Select(t => t.name)
                .Where(n => n.IndexOf("hand", System.StringComparison.OrdinalIgnoreCase) >= 0)
                .DefaultIfEmpty("(none)")
                .ToArray();
    }
}
