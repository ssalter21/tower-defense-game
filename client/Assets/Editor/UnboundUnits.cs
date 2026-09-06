using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// The rows of <c>content/units.txt</c> that have no art chosen for them
    /// yet, and the stand-in every one of them is drawn as until one is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This list is a temporary allowance, and issue #271 — the roster
    /// expansion's integrate ticket — is where it is emptied.</b> The rows of
    /// the widened roster land in the simulation before their art does, so for
    /// the length of that effort a row can exist with nothing chosen to draw
    /// it. An id written here says so out loud; an id nobody wrote here and
    /// nobody gave art to still reaches <c>MatchArt</c> as a throw naming it,
    /// which is what stops a row being forgotten rather than allowed.
    /// </para>
    /// <para>
    /// <b>The stand-in was named on the ticket and is not chosen here.</b>
    /// Issue #251 asked for the Prototype pack's Dummy, and the Prototype pack
    /// ships exactly one. It is deliberately not a character: a mannequin reads
    /// as "not decided yet" from any camera angle, where a plausible-looking
    /// body reads as a choice somebody made. Art on this project is chosen by
    /// the developer and never by a builder reaching for the obvious model.
    /// </para>
    /// <para>
    /// <b>Read by the scene builder and by the test fixture, from here.</b>
    /// Those two keep their own copies of every art assignment on purpose —
    /// two tables that must agree, so that one choosing the wrong model is a
    /// failure rather than a tautology. A row with no art has no choice to
    /// disagree about, so there is one list of them and both read it.
    /// </para>
    /// </remarks>
    public static class UnboundUnits
    {
        /// <summary>The Prototype pack's Dummy, which every listed row draws as.</summary>
        public const string StandInModelPath = "Assets/Art/Kaykit/prototype/Dummy_Base.fbx";

        /// <summary>
        /// Every row with no art yet, and the size it stands in at:
        /// <see cref="MatchArt.CreepScale"/> for a moving row,
        /// <see cref="MatchArt.TowerScale"/> for a placed one.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The size is written per row rather than read off the unit table
        /// because the scene is built from tables in C# and reads no content at
        /// all. It is not free to be wrong: <c>ImportedArtTests</c> walks the
        /// shipped rows and holds every one of them against the size its role
        /// is drawn at, a stand-in included.
        /// </para>
        /// <para>
        /// <b>Editing this list means running two generators.</b> The match
        /// scene carries its own serialized copy of the art and the play-mode
        /// manifest carries another, so a row added here reaches a build only
        /// after <c>tools/build-match-scene.ps1</c> and
        /// <c>tools/build-test-assets.ps1</c>. Both have a test that goes red
        /// until the copy they generate is regenerated and committed.
        /// </para>
        /// </remarks>
        public static readonly (int UnitId, float Scale)[] Rows =
        {
            // The twelve creep rows the roster widened to, ids 38 to 49, in the
            // order content/units.txt carries them. Every one of them walks, so
            // every one of them is a creep scale — and a stand-in creep is
            // handed the shared walk and death clips, so these slide down the
            // corridor rather than standing still the way a tower would.
            (38, MatchArt.CreepScale), (39, MatchArt.CreepScale), (40, MatchArt.CreepScale),
            (41, MatchArt.CreepScale), (42, MatchArt.CreepScale), (43, MatchArt.CreepScale),
            (44, MatchArt.CreepScale), (45, MatchArt.CreepScale), (46, MatchArt.CreepScale),
            (47, MatchArt.CreepScale), (48, MatchArt.CreepScale), (49, MatchArt.CreepScale),
        };

        /// <summary>Whether this row is one of the ones with no art yet.</summary>
        public static bool Lists(int unitId)
        {
            foreach (var row in Rows)
            {
                if (row.UnitId == unitId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>One art entry per listed row, each drawing the stand-in.</summary>
        public static IEnumerable<UnitArt> StandIns()
        {
            foreach (var row in Rows)
            {
                yield return StandIn(row.UnitId, row.Scale);
            }
        }

        /// <summary>
        /// What one row with no art yet is drawn as: the stand-in at that size,
        /// nothing in either hand and no clips of its own.
        /// </summary>
        /// <remarks>
        /// A stand-in tower has no clips of its own, so it stands in its bind
        /// pose through all three of its states. A stand-in creep is handed the
        /// shared walk and death clips like every other creep, and those were
        /// authored against the character rig rather than this one, so their
        /// curves bind to nothing and it slides down the corridor without
        /// moving its legs. Both read as a row nobody has dressed yet, which is
        /// what they are.
        /// </remarks>
        public static UnitArt StandIn(int unitId, float scale) =>
            UnitArt.Of(unitId, StandInModel(), scale);

        /// <summary>The imported stand-in, or a throw naming where it should be.</summary>
        public static GameObject StandInModel()
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(StandInModelPath);

            if (model == null)
            {
                throw new IOException(
                    "Nothing imported at " + StandInModelPath + ". A row with no art yet is drawn as "
                    + "that model, so without it those rows cannot be drawn at all.");
            }

            return model;
        }
    }
}
