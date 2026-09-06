using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// Finds an animation clip by name among the imported banks. A bare name
    /// searches the <c>Rig_Medium</c> banks; <c>Rig_Large_General/Idle_A</c>
    /// names one bank and searches only that.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The qualifier exists because there are two rigs.</b> The collection
    /// ships <c>Rig_Medium</c> and <c>Rig_Large</c>, and <c>Idle_A</c>,
    /// <c>Walking_A</c> and <c>Death_A</c> are in both. A clip from the wrong
    /// one does not throw: it drives bones the skeleton has not got and leaves
    /// the ones it has where they were, which reads as the model being bad
    /// rather than the clip being for another skeleton. So an unqualified name
    /// resolves in the medium banks only, and reaching a Large clip means
    /// saying which bank it is in.
    /// </para>
    /// <para>
    /// <b>One resolver for the set files and the binding table.</b>
    /// <see cref="CandidateSet"/> reads a name out of a text file and
    /// <c>MatchSceneBuilder</c> reads one out of a table in C#, and they must
    /// agree about what a name means or a candidate sheet is a photograph of a
    /// pose the match will not draw. The art assignments those two carry are
    /// deliberately separate transcriptions; which bank a name resolves in is
    /// mechanism, and is here once.
    /// </para>
    /// </remarks>
    public static class ClipBanks
    {
        /// <summary>Where the banks are, both rigs together.</summary>
        public const string Folder = "Assets/Art/Animations";

        /// <summary>The prefix on the banks an unqualified name searches.</summary>
        private const string MediumBankPrefix = "Rig_Medium_";

        /// <summary>The clip a spec names, without the bank it may carry.</summary>
        public static string NameIn(string spec)
        {
            int slash = spec.IndexOf('/');

            return slash < 0 ? spec : spec.Substring(slash + 1);
        }

        /// <summary>
        /// The clip a spec names, or null.
        /// </summary>
        /// <param name="spec">A clip name, optionally <c>bank/clip</c>.</param>
        /// <param name="whereItLooked">
        /// The banks searched and what they hold, for a caller writing the
        /// failure — or, where the spec named a bank that is not there, that.
        /// </param>
        public static AnimationClip Find(string spec, out string whereItLooked)
        {
            int slash = spec.IndexOf('/');
            string bankWanted = slash < 0 ? null : spec.Substring(0, slash);
            string wanted = NameIn(spec);

            var searched = new List<string>();
            var found = new List<string>();

            foreach (string asset in AssetDatabase.FindAssets("t:Model", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(asset);
                string file = Path.GetFileNameWithoutExtension(path);

                bool wantThisBank = bankWanted == null
                    ? file.StartsWith(MediumBankPrefix, StringComparison.Ordinal)
                    : file == bankWanted;

                if (!wantThisBank)
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

                    if (clip.name == wanted)
                    {
                        whereItLooked = null;

                        return clip;
                    }

                    found.Add(clip.name);
                }
            }

            whereItLooked = searched.Count == 0
                ? "any bank called '" + bankWanted + "' under " + Folder
                : string.Join(", ", searched) + ". Those hold: " + string.Join(", ", found);

            return null;
        }
    }
}
