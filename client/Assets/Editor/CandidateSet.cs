using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// A named set of characters to render: one line per character, saying
    /// which model, what it holds in each hand, what clip it is posed in and,
    /// where the look being asked about is a colour or a thing on the ground
    /// beside it, which atlas it wears and what stands there.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a file and not a table in code.</b> The live roster is what
    /// <c>content/units.txt</c> already says; a candidate set is a question
    /// being put to somebody — "does this model read as a Necromancer holding
    /// that scythe" — and the answer is usually "not that one, try the other".
    /// A file is edited and re-rendered by whoever is looking, without an
    /// agent, a recompile or a commit in between.
    /// </para>
    /// <para>
    /// <b>Every path is relative to <c>Assets/Art/</c>.</b> Absolute project
    /// paths would put <c>Assets/Art/</c> on the front of ninety-three fields
    /// that all share it; a bare filename would need a search, and a search is
    /// how the wrong <c>Idle_A</c> gets picked. One root, named here and in the
    /// file's own header.
    /// </para>
    /// <para>
    /// <b>A held prop may carry a turn, written <c>path@x,y,z</c>.</b> This
    /// pack authors every weapon for the right hand, so a bow in the off hand
    /// comes out mirrored and needs <c>@0,180,0</c>, and a staff hangs from the
    /// fist by its head until <c>@0,0,-90</c> stands it up — the same two
    /// numbers <c>MatchSceneBuilder</c> carries per unit for the live rows. It
    /// rides on the prop rather than in a column of its own because it is a
    /// fact about the prop: the same scythe wants the same turn in whosever
    /// hand it is. Only exactly right in one pose, since a weapon parented to a
    /// hand turns with the arm.
    /// </para>
    /// <para>
    /// <b>The atlas is a seventh field and it is optional.</b> A tier is told
    /// apart by colour, a prop or a second model, and the first of those cannot
    /// be photographed by naming a model and two props — the character would
    /// come out in the atlas it imported wearing, which is the rung below it.
    /// So a line may carry a texture path after its clip. Six fields is a line
    /// asking about a silhouette and seven is a line asking about a colour;
    /// both are legal, and a set written before this column existed still
    /// reads.
    /// </para>
    /// <para>
    /// <b>The beside prop is an eighth field and it carries its own size,
    /// written <c>path*scale</c>.</b> A turret, a statue, a font and a tree
    /// stand on the ground rather than in a fist, so no bone positions them and
    /// no hand scales them — a Forest Nature tree comes in authored for a
    /// forest and would stand over the character it is meant to stand beside.
    /// The size rides on the prop rather than in a column of its own for the
    /// same reason a turn does: it is a fact about the prop and not about the
    /// character. A turn is refused here, because a thing on the floor takes
    /// the rotation its importer gave it.
    /// </para>
    /// <para>
    /// <b>A model may name mesh parts to leave out, written
    /// <c>path!Node_A,Node_B</c>.</b> Some of these characters carry their
    /// kit in the body mesh rather than in a hand — the Grave Robber's sword
    /// sits in a child called <c>Hoarder_FrontPouch_Sword</c>, not on
    /// <c>handslot.r</c> — so "with and without it" is not two models and
    /// cannot be asked by naming one. The named children are hidden for the
    /// render and nothing on disk changes; a name that matches nothing is a
    /// fault rather than a silent no-op, because a candidate that came out
    /// still wearing the thing under discussion is exactly the picture this
    /// file exists to prevent.
    /// </para>
    /// <para>
    /// <b>The clip may name its bank, and for a Large rig it must.</b>
    /// <c>Idle_A</c>, <c>Walking_A</c> and <c>Death_A</c> exist in the Medium
    /// banks and again in the Large ones, and a Large-rig character posed by a
    /// Medium clip does not fail — it draws, wrongly, in a way that reads as
    /// the model being bad rather than the clip being for another skeleton. So
    /// an unqualified name searches the <c>Rig_Medium_*</c> banks only, and
    /// <c>Rig_Large_General/Idle_A</c> says exactly which bank is meant.
    /// </para>
    /// </remarks>
    public static class CandidateSet
    {
        /// <summary>The root every path in a set file hangs off.</summary>
        public const string ArtRoot = "Assets/Art/";

        /// <summary>Where the clip banks are, all rigs together.</summary>
        public const string ClipBankFolder = ClipBanks.Folder;

        /// <summary>What a field holds when the character carries nothing there.</summary>
        private const string Empty = "-";

        /// <summary>Which view a character is drawn through.</summary>
        public enum Side
        {
            /// <summary>Through <see cref="TowerView"/>.</summary>
            Tower,

            /// <summary>Through <see cref="CreepView"/>.</summary>
            Creep,
        }

        /// <summary>One character, with every reference already resolved.</summary>
        public sealed class Candidate
        {
            /// <summary>Tower or creep — which view draws it.</summary>
            public Side Side { get; internal set; }

            /// <summary>What the PNG is called and what the manifest lists.</summary>
            public string Name { get; internal set; }

            /// <summary>The character model.</summary>
            public GameObject Model { get; internal set; }

            /// <summary>What goes on <c>handslot.r</c>, or null.</summary>
            public GameObject RightHand { get; internal set; }

            /// <summary>How that item is turned relative to the bone.</summary>
            public Vector3 RightHandTilt { get; internal set; }

            /// <summary>What goes on <c>handslot.l</c>, or null.</summary>
            public GameObject LeftHand { get; internal set; }

            /// <summary>How that item is turned relative to the bone.</summary>
            public Vector3 LeftHandTilt { get; internal set; }

            /// <summary>The clip it is posed in.</summary>
            public AnimationClip Clip { get; internal set; }

            /// <summary>The atlas it wears, or null for the model's own.</summary>
            public Texture2D Texture { get; internal set; }

            /// <summary>The atlas as the set file spelled it, or <c>-</c>.</summary>
            public string TexturePath { get; internal set; }

            /// <summary>What stands on the ground beside it, or null.</summary>
            public GameObject Beside { get; internal set; }

            /// <summary>How big that prop is drawn. One for a prop drawn as imported.</summary>
            public float BesideScale { get; internal set; }

            /// <summary>The beside prop as the set file spelled it, size and all, or <c>-</c>.</summary>
            public string BesidePath { get; internal set; }

            /// <summary>The clip as the set file spelled it, for the manifest.</summary>
            public string ClipName { get; internal set; }

            /// <summary>The model path as the set file spelled it, for the manifest.</summary>
            public string ModelPath { get; internal set; }

            /// <summary>
            /// Mesh children to hide for this render, from the model field's
            /// <c>!</c> suffix. Empty when the whole body is drawn.
            /// </summary>
            public IReadOnlyList<string> Hidden { get; internal set; }

            /// <summary>
            /// The right-hand prop as the set file spelled it, turn and all,
            /// or <c>-</c>.
            /// </summary>
            /// <remarks>
            /// Kept as text beside the loaded object because the manifest is
            /// what tells a reader which prop a tile is holding, and the thing
            /// being signed off is the pairing of character and prop. A
            /// <see cref="GameObject"/>'s name is the asset's, not the path
            /// the set file chose it by, and two packs ship an <c>axe</c>.
            /// </remarks>
            public string RightHandPath { get; internal set; }

            /// <summary>The left-hand prop as the set file spelled it, or <c>-</c>.</summary>
            public string LeftHandPath { get; internal set; }
        }

        /// <summary>
        /// Reads a set file and resolves every reference in it.
        /// </summary>
        /// <remarks>
        /// <b>Every fault in the file is reported at once.</b> Throwing on the
        /// first bad line means a set of thirty-one is fixed one typo per
        /// three-minute batchmode run. The whole file is checked before
        /// anything renders, so a set either draws completely or draws nothing
        /// and says why.
        /// </remarks>
        /// <exception cref="IOException">
        /// The file is missing, empty, or any line is malformed or names
        /// something that is not on disk.
        /// </exception>
        public static IReadOnlyList<Candidate> Read(string path)
        {
            if (!File.Exists(path))
            {
                throw new IOException("No candidate set file at " + path + ".");
            }

            var candidates = new List<Candidate>();
            var faults = new List<string>();
            string[] lines = File.ReadAllLines(path);

            for (var index = 0; index < lines.Length; index++)
            {
                string line = lines[index].Trim();

                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string where = Path.GetFileName(path) + ":" + (index + 1);
                string[] fields = line.Split(
                    new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (fields.Length < 6 || fields.Length > 8)
                {
                    faults.Add(
                        where + ": " + fields.Length + " fields, not 6, 7 or 8. A line is "
                        + "'side name model right-hand left-hand clip [texture [beside]]', with '-' for "
                        + "an empty hand, an empty slot or the model's own atlas.");
                    continue;
                }

                Candidate candidate = Resolve(where, fields, faults);

                if (candidate != null)
                {
                    candidates.Add(candidate);
                }
            }

            if (faults.Count > 0)
            {
                throw new IOException(
                    "The candidate set at " + path + " names " + faults.Count
                    + " thing(s) that are not there:" + Environment.NewLine + "  "
                    + string.Join(Environment.NewLine + "  ", faults));
            }

            if (candidates.Count == 0)
            {
                throw new IOException("The candidate set at " + path + " has no entries.");
            }

            return candidates;
        }

        /// <summary>
        /// One line's fields, turned into loaded assets. Returns null and
        /// appends to <paramref name="faults"/> when anything is missing, so
        /// the caller can report the whole file at once.
        /// </summary>
        private static Candidate Resolve(string where, string[] fields, List<string> faults)
        {
            var before = faults.Count;

            // The '!' comes off before anything else looks at the model field,
            // so the path that is loaded and the path the manifest prints are
            // both the model and neither carries the question being asked of
            // it.
            int bang = fields[2].IndexOf('!');
            string modelPath = bang < 0 ? fields[2] : fields[2].Substring(0, bang);
            string[] hidden = bang < 0
                ? System.Array.Empty<string>()
                : fields[2].Substring(bang + 1)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries);

            var candidate = new Candidate
            {
                Name = fields[1],
                ModelPath = modelPath,
                Hidden = hidden,
                RightHandPath = fields[3],
                LeftHandPath = fields[4],
                ClipName = fields[5],
                TexturePath = fields.Length > 6 ? fields[6] : Empty,
                BesidePath = fields.Length > 7 ? fields[7] : Empty,
            };

            switch (fields[0])
            {
                case "tower": candidate.Side = Side.Tower; break;
                case "creep": candidate.Side = Side.Creep; break;
                default:
                    faults.Add(where + ": side is '" + fields[0] + "', not 'tower' or 'creep'.");
                    break;
            }

            // A turn is a fact about a held prop and means nothing on the
            // character itself, so a '@' here is refused rather than ignored.
            // Ignoring it would turn a misplaced correction into a candidate
            // that draws untilted and says nothing about why -- which is the
            // whole failure this file's reader is built to avoid.
            if (fields[2].IndexOf('@') >= 0)
            {
                faults.Add(
                    where + ": model '" + fields[2] + "' carries a turn. A '@x,y,z' belongs on a "
                    + "held prop, not on the character.");
            }

            // A '!' on anything but the model is refused for the same reason
            // the '@' is: a hand holds a whole prop, so there is no part of one
            // to leave out, and ignoring the suffix would draw the prop entire
            // and say nothing about why.
            foreach ((string field, string spec) in
                new[] { ("right hand", fields[3]), ("left hand", fields[4]) })
            {
                if (spec.IndexOf('!') >= 0)
                {
                    faults.Add(
                        where + ": " + field + " '" + spec + "' leaves a part out. A '!Node' belongs "
                        + "on the character, whose body carries its own kit, not on a held prop.");
                }
            }

            candidate.Model = LoadModel(where, "model", modelPath, faults, out _);
            candidate.RightHand = LoadModel(where, "right hand", fields[3], faults, out Vector3 right);
            candidate.LeftHand = LoadModel(where, "left hand", fields[4], faults, out Vector3 left);
            candidate.RightHandTilt = right;
            candidate.LeftHandTilt = left;
            candidate.Clip = FindClip(where, fields[5], faults);
            VerifyHidden(where, candidate.Model, hidden, faults);
            candidate.Texture = LoadTexture(where, candidate.TexturePath, faults);
            candidate.Beside = LoadBeside(where, candidate.BesidePath, faults, out float besideScale);
            candidate.BesideScale = besideScale;

            // Only a tower has the socket. CreepView draws no beside prop --
            // nothing that walks stands beside anything -- so a creep line
            // naming one would render a picture with the prop missing and say
            // nothing about why.
            if (candidate.Side == Side.Creep && candidate.BesidePath != Empty)
            {
                faults.Add(
                    where + ": '" + candidate.BesidePath + "' stands beside a creep. The beside socket is "
                    + "a tower's; a creep would carry its prop down the corridor.");
            }

            return faults.Count == before ? candidate : null;
        }

        /// <summary>
        /// The model at an art-relative path, null for <c>-</c>, or a fault
        /// naming the path that found nothing. A <c>@x,y,z</c> suffix comes
        /// back in <paramref name="tilt"/>.
        /// </summary>
        private static GameObject LoadModel(
            string where, string field, string spec, List<string> faults, out Vector3 tilt)
        {
            tilt = Vector3.zero;

            if (spec == Empty)
            {
                return null;
            }

            int at = spec.IndexOf('@');
            string relative = at < 0 ? spec : spec.Substring(0, at);

            if (at >= 0 && !Tilt(spec.Substring(at + 1), out tilt))
            {
                faults.Add(
                    where + ": " + field + " '" + spec + "' — the turn after '@' is not three "
                    + "comma-separated degrees, as in '@0,180,0'.");
            }

            string path = ArtRoot + relative;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                faults.Add(where + ": " + field + " '" + relative + "' — nothing imported at " + path);
            }

            return model;
        }

        /// <summary>
        /// The prop that stands beside the character, null for <c>-</c>, or a
        /// fault naming what was wrong with it. A <c>*scale</c> suffix comes
        /// back in <paramref name="scale"/>, and a prop that names none is
        /// drawn at the size it imported at.
        /// </summary>
        private static GameObject LoadBeside(
            string where, string spec, List<string> faults, out float scale)
        {
            scale = 1f;

            if (spec == Empty)
            {
                return null;
            }

            // A turn belongs on a held prop, whose bone decides where it points.
            // Something on the floor takes the rotation its importer gave it, so
            // a '@' here is refused rather than quietly dropped.
            if (spec.IndexOf('@') >= 0)
            {
                faults.Add(
                    where + ": beside prop '" + spec + "' carries a turn. A '@x,y,z' belongs on a held "
                    + "prop; a thing standing on the ground keeps the rotation it was imported with.");

                return null;
            }

            int star = spec.IndexOf('*');
            string relative = star < 0 ? spec : spec.Substring(0, star);

            // One mistake, one fault. A size that will not parse leaves nothing
            // to say anything about, so the range check is the else and not the
            // next statement -- two lines about one typo is how a file of these
            // stops being readable.
            if (star < 0)
            {
                // Nothing said, so the prop is drawn as imported.
            }
            else if (!float.TryParse(
                spec.Substring(star + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out scale))
            {
                faults.Add(
                    where + ": beside prop '" + spec + "' — the size after '*' is not a number, as in "
                    + "'*0.5'.");
            }
            else if (scale <= 0f)
            {
                faults.Add(
                    where + ": beside prop '" + spec + "' is drawn at " + scale + ", which is a prop that "
                    + "never appeared.");
            }

            string path = ArtRoot + relative;
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                faults.Add(where + ": beside prop '" + relative + "' — nothing imported at " + path);
            }

            return model;
        }

        /// <summary>
        /// The atlas at an art-relative path, null for <c>-</c>, or a fault
        /// naming the path that found nothing.
        /// </summary>
        private static Texture2D LoadTexture(string where, string spec, List<string> faults)
        {
            if (spec == Empty)
            {
                return null;
            }

            string path = ArtRoot + spec;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                faults.Add(where + ": texture '" + spec + "' — nothing imported at " + path);
            }

            return texture;
        }

        /// <summary>
        /// Checks that every name a <c>!</c> suffix leaves out is actually a
        /// child of the model, and lists what the body does carry when one is
        /// not.
        /// </summary>
        /// <remarks>
        /// Checked here, off the imported asset, rather than at render time
        /// against the built view: a misspelled node found during the render
        /// is a set of candidates drawn wrong and looked at anyway, and this
        /// file's whole contract is that a set either draws completely or
        /// draws nothing and says why. The listing is worth its width — these
        /// node names are the pack's, not this project's, and nothing else
        /// prints them.
        /// </remarks>
        private static void VerifyHidden(
            string where, GameObject model, IReadOnlyList<string> hidden, List<string> faults)
        {
            if (model == null || hidden.Count == 0)
            {
                return;
            }

            var parts = new List<string>();

            foreach (Transform part in model.GetComponentsInChildren<Transform>(true))
            {
                parts.Add(part.name);
            }

            foreach (string name in hidden)
            {
                if (!parts.Contains(name))
                {
                    faults.Add(
                        where + ": model has no child '" + name + "' to leave out. It carries: "
                        + string.Join(", ", parts));
                }
            }
        }

        /// <summary>Three comma-separated Euler degrees, or false.</summary>
        private static bool Tilt(string spec, out Vector3 tilt)
        {
            tilt = Vector3.zero;

            string[] parts = spec.Split(',');

            if (parts.Length != 3
                || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                || !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return false;
            }

            tilt = new Vector3(x, y, z);

            return true;
        }

        /// <summary>
        /// The named clip out of the banks, or a fault naming it and listing
        /// what the banks searched do hold.
        /// </summary>
        private static AnimationClip FindClip(string where, string spec, List<string> faults)
        {
            AnimationClip clip = ClipBanks.Find(spec, out string whereItLooked);

            if (clip == null)
            {
                faults.Add(where + ": no clip '" + ClipBanks.NameIn(spec) + "' in " + whereItLooked);
            }

            return clip;
        }
    }
}
