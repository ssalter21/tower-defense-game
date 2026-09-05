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
    /// which model, what it holds in each hand, and what clip it is posed in.
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
        public const string ClipBankFolder = "Assets/Art/Animations";

        /// <summary>The banks an unqualified clip name is looked for in.</summary>
        private const string MediumBankPrefix = "Rig_Medium_";

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

            /// <summary>The clip as the set file spelled it, for the manifest.</summary>
            public string ClipName { get; internal set; }

            /// <summary>The model path as the set file spelled it, for the manifest.</summary>
            public string ModelPath { get; internal set; }

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

                if (fields.Length != 6)
                {
                    faults.Add(
                        where + ": " + fields.Length + " fields, not 6. A line is "
                        + "'side name model right-hand left-hand clip', with '-' for an empty hand.");
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
        /// One line's six fields, turned into loaded assets. Returns null and
        /// appends to <paramref name="faults"/> when anything is missing, so
        /// the caller can report the whole file at once.
        /// </summary>
        private static Candidate Resolve(string where, string[] fields, List<string> faults)
        {
            var before = faults.Count;
            var candidate = new Candidate
            {
                Name = fields[1],
                ModelPath = fields[2],
                RightHandPath = fields[3],
                LeftHandPath = fields[4],
                ClipName = fields[5],
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

            candidate.Model = LoadModel(where, "model", fields[2], faults, out _);
            candidate.RightHand = LoadModel(where, "right hand", fields[3], faults, out Vector3 right);
            candidate.LeftHand = LoadModel(where, "left hand", fields[4], faults, out Vector3 left);
            candidate.RightHandTilt = right;
            candidate.LeftHandTilt = left;
            candidate.Clip = FindClip(where, fields[5], faults);

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
            int slash = spec.IndexOf('/');
            string bank = slash < 0 ? null : spec.Substring(0, slash);
            string name = slash < 0 ? spec : spec.Substring(slash + 1);

            var searched = new List<string>();
            var found = new List<string>();

            foreach (string asset in AssetDatabase.FindAssets("t:Model", new[] { ClipBankFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(asset);
                string file = Path.GetFileNameWithoutExtension(path);

                bool wanted = bank == null
                    ? file.StartsWith(MediumBankPrefix, StringComparison.Ordinal)
                    : file == bank;

                if (!wanted)
                {
                    continue;
                }

                searched.Add(file);

                foreach (UnityEngine.Object entry in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    // __preview__ duplicates are editor thumbnail bookkeeping
                    // Unity hangs off any clip it has ever drawn an icon for.
                    // One of those resolves to nothing outside the editor.
                    if (!(entry is AnimationClip clip)
                        || clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (clip.name == name)
                    {
                        return clip;
                    }

                    found.Add(clip.name);
                }
            }

            faults.Add(
                where + ": no clip '" + name + "' in " + (searched.Count == 0
                    ? "any bank called '" + bank + "' under " + ClipBankFolder
                    : string.Join(", ", searched) + ". Those hold: " + string.Join(", ", found)));

            return null;
        }
    }
}
