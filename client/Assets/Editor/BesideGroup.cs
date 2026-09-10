using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// The beside-prop field of a candidate file: one prop, or several standing
    /// together on the tile beside a tower.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why more than one, when the game draws exactly one.</b>
    /// <see cref="BesideProp"/> holds a single model and
    /// <see cref="TowerView"/> places a single instance of it, which is the
    /// whole of the Artificer's problem: <c>docs/roster.md</c> puts an
    /// <c>ammo_crate</c> beside his turret, one socket holds one prop, and the
    /// turret wins because it is where the shell leaves from — so that rung is
    /// told from the Engineer by colour alone. Three things could answer that
    /// and two of them are two props on one tile. A candidate that cannot be
    /// drawn cannot be put up for signature.
    /// </para>
    /// <para>
    /// <b>And it is still one prop by the time the view sees it.</b> Several
    /// props joined with <c>+</c> are parented under one throwaway
    /// <see cref="GameObject"/> here, and that object is what goes into the
    /// <see cref="BesideProp"/>. Nothing in <see cref="BesideProp"/>,
    /// <see cref="UnitArt"/>, <see cref="TowerView"/>, <c>MatchArt.asset</c> or
    /// the generated scene moves, so a candidate sheet and a candidate frame
    /// are pictures of the shipped view drawing something extra rather than
    /// pictures of a changed view. Which of the two mechanisms a signed
    /// candidate would actually be built as — a second socket on every tower,
    /// or one authored model that is both — is the sitting's to say, and the
    /// two look identical from the camera, which is a finding rather than a
    /// shortcut.
    /// </para>
    /// <para>
    /// <b>The offsets are the candidate's, not the view's.</b>
    /// <see cref="BesideProp.NextTile"/> puts the group's root where a single
    /// prop would stand; a <c>~dx,dz</c> on a member moves that member within
    /// the tile, in metres, in the frame the tower rests in. Two props at the
    /// same offset are one prop inside another, so a group whose members do not
    /// carry offsets is refused rather than drawn as a collision.
    /// </para>
    /// </remarks>
    public static class BesideGroup
    {
        /// <summary>The root every path in a candidate file hangs off.</summary>
        public const string ArtRoot = "Assets/Art/";

        /// <summary>What a beside field holds when nothing stands beside the tower.</summary>
        public const string Empty = "-";

        /// <summary>Joins the members of a group.</summary>
        public const char Join = '+';

        /// <summary>Introduces a member's size.</summary>
        public const char Size = '*';

        /// <summary>Introduces a member's offset within the tile.</summary>
        public const char Shift = '~';

        /// <summary>
        /// The name a composite is given, so a log line or a hierarchy dump
        /// says what it is rather than showing the first member's name twice.
        /// </summary>
        public const string CompositeName = "beside-group";

        /// <summary>
        /// Parses a beside spec and builds what stands there.
        /// </summary>
        /// <remarks>
        /// Returns null for <see cref="Empty"/> and for a spec with a fault in
        /// it; every fault found is appended to <paramref name="faults"/> so a
        /// whole file is reported at once, which is the rule
        /// <see cref="CandidateSet"/> already works to.
        /// </remarks>
        /// <param name="where">File and line, for a fault that names itself.</param>
        /// <param name="spec">The field as the file spelled it.</param>
        /// <param name="faults">Where anything wrong with it is written.</param>
        /// <param name="scale">
        /// What the resulting object is drawn at. A lone prop carries its own
        /// size here, exactly as it did before groups existed; a composite is
        /// drawn at 1 and its members carry their own sizes, because a size on
        /// the group would multiply into every member and no longer be the
        /// number the file wrote against a member.
        /// </param>
        public static GameObject Parse(
            string where, string spec, List<string> faults, out float scale)
        {
            scale = 1f;

            if (spec == Empty)
            {
                return null;
            }

            // A turn belongs on a held prop, whose bone decides where it points.
            // Something on the floor takes the rotation its importer gave it, so
            // a '@' here is refused rather than quietly dropped -- the rule the
            // single-prop reader has always enforced, kept for every member.
            if (spec.IndexOf('@') >= 0)
            {
                faults.Add(
                    where + ": beside prop '" + spec + "' carries a turn. A '@x,y,z' belongs on a held "
                    + "prop; a thing standing on the ground keeps the rotation it was imported with.");

                return null;
            }

            string[] parts = spec.Split(Join, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
            {
                faults.Add(
                    where + ": beside prop '" + spec + "' names nothing. A group is "
                    + "'a.fbx*1~0,0+b.fbx*1~0.6,0.4', and '-' is how a tower stands beside nothing.");

                return null;
            }

            var before = faults.Count;
            var members = new List<(GameObject model, float scale, Vector3 offset, string spelled)>();

            foreach (string part in parts)
            {
                Member(where, part, faults, members);
            }

            if (faults.Count != before)
            {
                return null;
            }

            if (members.Count == 1)
            {
                // ONE PROP IS THE OBJECT ITSELF, not a group of one. A wrapper
                // would change what every existing set file draws -- an extra
                // transform under the tower root -- for no picture anybody
                // asked for, and the committed sheets would stop being
                // comparable with the ones that come after.
                (GameObject only, float size, Vector3 offset, string spelled) = members[0];

                if (offset != Vector3.zero)
                {
                    faults.Add(
                        where + ": beside prop '" + spelled + "' is the only thing on the tile and "
                        + "carries a '" + Shift + "'. An offset moves a member within a group; a lone "
                        + "prop stands where the socket puts it.");

                    return null;
                }

                scale = size;

                return only;
            }

            return Composite(where, members, faults);
        }

        /// <summary>
        /// One member's spec, resolved and appended, or a fault naming what was
        /// wrong with it.
        /// </summary>
        private static void Member(
            string where,
            string spec,
            List<string> faults,
            List<(GameObject, float, Vector3, string)> members)
        {
            var before = faults.Count;
            string rest = spec;
            var offset = Vector3.zero;

            int shift = rest.IndexOf(Shift);

            if (shift >= 0)
            {
                if (!Offset(rest.Substring(shift + 1), out offset))
                {
                    faults.Add(
                        where + ": beside prop '" + spec + "' — the offset after '" + Shift + "' is not "
                        + "two comma-separated metres, as in '" + Shift + "0.6,0.4'. It is sideways then "
                        + "forward, in the frame the tower rests in.");
                }

                rest = rest.Substring(0, shift);
            }

            var size = 1f;
            int star = rest.IndexOf(Size);
            string relative = star < 0 ? rest : rest.Substring(0, star);

            // One mistake, one fault. A size that will not parse leaves nothing
            // to say anything about, so the range check is the else and not the
            // next statement -- two lines about one typo is how a file of these
            // stops being readable. A member naming no size is drawn as
            // imported, which is the default `size` already holds.
            if (star >= 0)
            {
                if (!float.TryParse(
                        rest.Substring(star + 1),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out size))
                {
                    faults.Add(
                        where + ": beside prop '" + spec + "' — the size after '" + Size + "' is not "
                        + "a number, as in '" + Size + "0.5'.");
                }
                else if (size <= 0f)
                {
                    faults.Add(
                        where + ": beside prop '" + spec + "' is drawn at " + size + ", which is a "
                        + "prop that never appeared.");
                }
            }

            string path = ArtRoot + relative;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                faults.Add(where + ": beside prop '" + relative + "' — nothing imported at " + path);
            }

            if (faults.Count == before)
            {
                members.Add((model, size, offset, spec));
            }
        }

        /// <summary>
        /// The members under one throwaway root, or a fault when two of them
        /// stand in the same place.
        /// </summary>
        private static GameObject Composite(
            string where,
            List<(GameObject model, float scale, Vector3 offset, string spelled)> members,
            List<string> faults)
        {
            for (var i = 0; i < members.Count; i++)
            {
                for (int j = i + 1; j < members.Count; j++)
                {
                    if (members[i].offset != members[j].offset)
                    {
                        continue;
                    }

                    // TWO PROPS AT ONE OFFSET IS ONE PROP INSIDE ANOTHER, and
                    // the picture of that is a prop with something poking out
                    // of it -- which reads as a broken import rather than as a
                    // candidate, and would be looked at as one.
                    faults.Add(
                        where + ": beside props '" + members[i].spelled + "' and '"
                        + members[j].spelled + "' stand in the same place. Give each member of a group a "
                        + "'" + Shift + "dx,dz' of its own, or the tile holds one prop inside another.");

                    return null;
                }
            }

            var root = new GameObject(CompositeName);

            // Inactive while it is assembled, so nothing under it gets a frame
            // of being at the origin of the editor's own scene before the view
            // parents it where it belongs.
            root.SetActive(false);

            foreach ((GameObject model, float scale, Vector3 offset, string _) in members)
            {
                GameObject member = UnityEngine.Object.Instantiate(model, root.transform);

                // THE NAME IS LOAD-BEARING AND Instantiate TAKES IT AWAY. A
                // clone is named "turret_base(Clone)", and EffectAnchor finds
                // where a shot leaves from by looking for a transform named
                // exactly "turret_base" — including inside a beside prop. So a
                // group that kept the clone's name threw
                // "No transform named 'turret_base' on Tower 2 artificer, so
                // its shots have nowhere to leave from" the first time the
                // Artificer's own candidate was drawn. Loudly, which is the
                // only reason this is a fixed bug rather than a committed
                // picture of a rung that cannot fire.
                member.name = model.name;

                member.transform.localPosition = offset;
                member.transform.localScale = model.transform.localScale * scale;
                member.SetActive(true);
            }

            root.SetActive(true);

            return root;
        }

        /// <summary>Two comma-separated metres, sideways then forward.</summary>
        private static bool Offset(string spec, out Vector3 offset)
        {
            offset = Vector3.zero;
            string[] parts = spec.Split(',');

            if (parts.Length != 2)
            {
                return false;
            }

            if (!float.TryParse(
                    parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(
                    parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return false;
            }

            offset = new Vector3(x, 0f, z);

            return true;
        }
    }
}
