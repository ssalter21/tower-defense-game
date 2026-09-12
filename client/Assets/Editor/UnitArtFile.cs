using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// One candidate look for a row's art, read from a file: what a unit holds,
    /// what stands beside it, the atlas it wears and where its effects leave
    /// from — drawn instead of what the scene builder binds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this exists.</b> <see cref="EffectLookFile"/> answers "what should
    /// this effect look like" and cannot answer "what should this tower be
    /// holding", because a row's model, props, atlas and anchor are bound in
    /// <c>MatchSceneBuilder</c>'s own table rather than tuned. Four rungs on
    /// <c>docs/roster.md</c> are signed with a look the shipped bindings do not
    /// deliver — issue #281 — and the only way any of them gets settled is
    /// somebody seeing the alternatives at 1600x900, which is the framing a
    /// person plays at. Until this existed there was no way to draw one: the
    /// candidate sheet frames a character on its own, and the match capture
    /// drew the bindings and nothing else. <c>mage-hat-pitch.txt</c> named the
    /// gap on 7 September 2026 and said it wanted exactly this.
    /// </para>
    /// <para>
    /// <b>Nothing the game ships from moves.</b> The candidate is applied to a
    /// copy of the <see cref="MatchArt"/> the capture built, the way a
    /// signature line already is — <c>MatchSceneBuilder</c>, <c>MatchArt.asset</c>,
    /// the generated scene, <c>content/units.txt</c> and <c>docs/roster.md</c>
    /// are all untouched, and a run with no candidate draws precisely what it
    /// drew before. AGENTS.md rule 6 puts anything a player sees on the human
    /// side of the line; this renders alternatives and stops.
    /// </para>
    /// <para>
    /// <b>Every row named must exist, and an unknown directive is a fault.</b>
    /// The same rule <see cref="EffectLookFile"/> works to and for the same
    /// reason: a candidate that quietly did nothing would render the shipped
    /// look under a candidate's filename, and two frames that came out
    /// identical because both files were misspelt is a picture answering a
    /// question nobody asked. This project has been bitten by that twice on
    /// this map alone.
    /// </para>
    /// <para>
    /// <b>The lines.</b> Blank lines and <c>#</c> comments are skipped.
    /// </para>
    /// <code>
    /// label     a heavier turret at half again
    /// question  what "a heavier turret_base" can mean when the pack ships one turret
    /// beside    37  Kaykit/adventurers/turret_base.fbx*1.5
    /// beside    36  Kaykit/adventurers/turret_base.fbx*1~0,0+Kaykit/adventurers/ammo_crate.fbx*1~0.7,0.4
    /// hand      24  left   Kaykit/mystery-monthly-series-6/cleric/Cleric_Tome.fbx
    /// hand      24  right  -
    /// atlas     29  Kaykit/adventurers/druid_texture_alt_A.png
    /// anchor    24  Cleric_Tome
    /// anchor    24  tip-of Cleric_Tome 0,1,0
    /// </code>
    /// </remarks>
    public sealed class UnitArtFile
    {
        /// <summary>The directive that names what this candidate is called.</summary>
        public const string LabelDirective = "label";

        /// <summary>The directive that names the question it is a candidate for.</summary>
        public const string QuestionDirective = "question";

        /// <summary>The directive that moves what stands beside a row.</summary>
        public const string BesideDirective = "beside";

        /// <summary>The directive that moves what a row holds.</summary>
        public const string HandDirective = "hand";

        public const string StandDirective = "stand";

        /// <summary>The directive that moves the atlas a row wears.</summary>
        public const string AtlasDirective = "atlas";

        /// <summary>The directive that moves where a row's effects leave from.</summary>
        public const string AnchorDirective = "anchor";

        /// <summary>What an <see cref="AnchorDirective"/> writes for a tip anchor.</summary>
        public const string TipOf = "tip-of";

        private UnitArtFile(string name, string label, string question, IReadOnlyList<Change> changes)
        {
            Name = name;
            Label = label;
            Question = question;
            Changes = changes;
        }

        /// <summary>The file's own name without its extension — what frames are named after.</summary>
        public string Name { get; }

        /// <summary>What this candidate is called, for the sheet beside it.</summary>
        public string Label { get; }

        /// <summary>The question it is a candidate for.</summary>
        public string Question { get; }

        /// <summary>One entry per row the file moves, in the order the file wrote them.</summary>
        public IReadOnlyList<Change> Changes { get; }

        /// <summary>
        /// Reads a candidate art file and resolves every reference in it.
        /// </summary>
        /// <remarks>
        /// <b>Every fault in the file is reported at once</b>, which is
        /// <see cref="CandidateSet"/>'s rule: throwing on the first bad line
        /// means a file is fixed one typo per three-minute batchmode run.
        /// </remarks>
        /// <exception cref="IOException">
        /// The file is missing, names no change, or any line is malformed or
        /// names something that is not there.
        /// </exception>
        public static UnitArtFile Read(string path)
        {
            if (!File.Exists(path))
            {
                throw new IOException("No candidate art file at " + path + ".");
            }

            var faults = new List<string>();
            var changes = new List<Change>();
            string label = null;
            string question = null;
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

                switch (fields[0])
                {
                    case LabelDirective:
                        label = Rest(line, fields[0]);
                        break;
                    case QuestionDirective:
                        question = Rest(line, fields[0]);
                        break;
                    case BesideDirective:
                    case StandDirective:
                    case HandDirective:
                    case AtlasDirective:
                    case AnchorDirective:
                        Directive(where, fields, faults, changes);
                        break;
                    default:
                        faults.Add(
                            where + ": '" + fields[0] + "' is not a directive. The lines are '"
                            + LabelDirective + "', '" + QuestionDirective + "', '" + BesideDirective
                            + "', '" + StandDirective + "', '" + HandDirective + "', '" + AtlasDirective
                            + "' and '" + AnchorDirective + "'.");
                        break;
                }
            }

            if (faults.Count > 0)
            {
                throw new IOException(
                    "The candidate art at " + path + " has " + faults.Count + " fault(s):"
                    + Environment.NewLine + "  "
                    + string.Join(Environment.NewLine + "  ", faults));
            }

            if (changes.Count == 0)
            {
                throw new IOException(
                    "The candidate art at " + path + " moves nothing. A file that moved nothing would "
                    + "draw the shipped look under a candidate's filename, which is a picture answering "
                    + "a question nobody asked.");
            }

            return new UnitArtFile(
                Path.GetFileNameWithoutExtension(path), label, question, Merge(changes));
        }

        /// <summary>
        /// The wired art with this candidate's looks on the rows it names.
        /// </summary>
        /// <remarks>
        /// A copy, always: the bound art is what every other frame of the same
        /// run is drawn from.
        /// </remarks>
        /// <exception cref="IOException">
        /// The candidate names a row the art does not carry — a typo in a unit
        /// id, which would otherwise be a candidate that drew nothing.
        /// </exception>
        public MatchArt Applied(MatchArt art)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));

            var missing = new List<int>();

            foreach (Change change in Changes)
            {
                var found = false;

                foreach (UnitArt unit in art.Units)
                {
                    found |= unit.UnitId == change.UnitId;
                }

                if (!found)
                {
                    missing.Add(change.UnitId);
                }
            }

            if (missing.Count > 0)
            {
                throw new IOException(
                    "The candidate art " + Name + " moves row(s) " + string.Join(", ", missing)
                    + ", which the wired art does not carry. A row that is not there is a mistyped unit "
                    + "id, and a candidate that moved nothing draws the shipped look under its own name.");
            }

            var moved = new List<UnitArt>(art.Units.Count);

            foreach (UnitArt unit in art.Units)
            {
                UnitArt drawn = unit;

                foreach (Change change in Changes)
                {
                    if (change.UnitId == unit.UnitId)
                    {
                        drawn = change.AppliedTo(drawn);
                    }
                }

                moved.Add(drawn);
            }

            return MatchArt.Of(moved, art.CreepWalkClip, art.CreepDeathClip);
        }

        /// <summary>One row's moved look, with every reference already resolved.</summary>
        public sealed class Change
        {
            /// <summary>The row in <c>content/units.txt</c> this moves.</summary>
            public int UnitId { get; internal set; }

            /// <summary>What goes in the right hand, or null to leave it alone.</summary>
            public GameObject RightHand { get; internal set; }

            /// <summary>What goes in the left hand, or null to leave it alone.</summary>
            public GameObject LeftHand { get; internal set; }

            /// <summary>True when the file emptied the right hand.</summary>
            public bool RightHandCleared { get; internal set; }

            /// <summary>True when the file emptied the left hand.</summary>
            public bool LeftHandCleared { get; internal set; }

            /// <summary>What stands beside it, or null to leave that alone.</summary>
            public BesideProp? Beside { get; internal set; }

            public Vector3? Stand { get; internal set; }

            /// <summary>The atlas it wears, or null to leave that alone.</summary>
            public Texture2D Texture { get; internal set; }

            /// <summary>Where its effects leave from, or null to leave that alone.</summary>
            public EffectAnchor? Anchor { get; internal set; }

            /// <summary>What the file spelled, one entry per directive, for the record beside the frame.</summary>
            public List<string> Spelled { get; } = new List<string>();

            /// <summary>This row's art with the candidate's looks on it.</summary>
            internal UnitArt AppliedTo(UnitArt unit)
            {
                UnitArt drawn = unit.WithLook(RightHand, LeftHand, Beside, Texture, Anchor);

                if (Stand.HasValue)
                {
                    drawn = drawn.WithLook(null, null, Stood(drawn.Beside, Stand.Value), null, null);
                }

                return RightHandCleared || LeftHandCleared
                    ? drawn.WithEmptyHands(RightHandCleared, LeftHandCleared)
                    : drawn;
            }

            private BesideProp Stood(BesideProp beside, Vector3 offset)
            {
                if (!beside.IsSet)
                {
                    throw new IOException(
                        "'" + StandDirective + " " + UnitId + "' moves where the row's beside prop stands, and "
                        + "nothing stands beside row " + UnitId + ". Name a prop with '" + BesideDirective
                        + "' on the same row, or pick a row that has one.");
                }

                return BesideProp.Standing(beside.Model, beside.Scale, offset);
            }
        }

        /// <summary>
        /// One row's directives folded into one <see cref="Change"/> each, so a
        /// file may spell a beside prop and an anchor for the same row on two
        /// lines and get one moved row.
        /// </summary>
        private static IReadOnlyList<Change> Merge(List<Change> changes)
        {
            var byRow = new Dictionary<int, Change>();
            var order = new List<Change>();

            foreach (Change change in changes)
            {
                if (!byRow.TryGetValue(change.UnitId, out Change already))
                {
                    byRow[change.UnitId] = change;
                    order.Add(change);
                    continue;
                }

                already.RightHand = change.RightHand ?? already.RightHand;
                already.LeftHand = change.LeftHand ?? already.LeftHand;
                already.RightHandCleared |= change.RightHandCleared;
                already.LeftHandCleared |= change.LeftHandCleared;
                already.Beside = change.Beside ?? already.Beside;
                already.Stand = change.Stand ?? already.Stand;
                already.Texture = change.Texture ?? already.Texture;
                already.Anchor = change.Anchor ?? already.Anchor;
                already.Spelled.AddRange(change.Spelled);
            }

            return order;
        }

        /// <summary>
        /// One directive line, resolved and appended, or a fault naming what was
        /// wrong with it.
        /// </summary>
        private static void Directive(
            string where, string[] fields, List<string> faults, List<Change> changes)
        {
            if (fields.Length < 3)
            {
                faults.Add(
                    where + ": '" + fields[0] + "' names " + (fields.Length - 1) + " thing(s). Every "
                    + "directive but '" + LabelDirective + "' and '" + QuestionDirective
                    + "' is '<directive> <unit id> <what>'.");
                return;
            }

            if (!int.TryParse(
                    fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int unitId))
            {
                faults.Add(
                    where + ": '" + fields[1] + "' is not a unit id. It is the row's id in "
                    + "content/units.txt, as in '37' for the Mortar.");
                return;
            }

            var change = new Change { UnitId = unitId };
            change.Spelled.Add(string.Join(" ", fields));
            var before = faults.Count;

            switch (fields[0])
            {
                case BesideDirective:
                    Beside(where, fields, faults, change);
                    break;
                case StandDirective:
                    Stand(where, fields, faults, change);
                    break;
                case HandDirective:
                    Hand(where, fields, faults, change);
                    break;
                case AtlasDirective:
                    change.Texture = Atlas(where, fields[2], faults);
                    break;
                case AnchorDirective:
                    change.Anchor = Anchor(where, fields, faults);
                    break;
            }

            if (faults.Count == before)
            {
                changes.Add(change);
            }
        }

        /// <summary>What stands beside the row, which may be several props on the one tile.</summary>
        private static void Beside(
            string where, string[] fields, List<string> faults, Change change)
        {
            if (fields[2] == BesideGroup.Empty)
            {
                // AN EMPTY TILE IS A CANDIDATE. "What if the Consecration's font
                // were not there" is a question a sheet can be asked, and it is
                // spelled the way a set file spells a tower standing beside
                // nothing.
                change.Beside = default(BesideProp);
                return;
            }

            GameObject model = BesideGroup.Parse(where, fields[2], faults, out float scale);

            if (model != null)
            {
                change.Beside = BesideProp.OnTheNextTile(model, scale);
            }
        }

        private static void Stand(string where, string[] fields, List<string> faults, Change change)
        {
            string[] parts = fields[2].Split(',');

            if (parts.Length != 2
                || !float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                faults.Add(
                    where + ": '" + StandDirective + "' is '" + StandDirective + " <unit id> <dx>,<dz>' -- "
                    + "metres from the tower's root, sideways then forward, in the frame it rests in. "
                    + "One tile sideways is " + HexGeometry.ColumnPitch.ToString(CultureInfo.InvariantCulture)
                    + ",0, which is where the shipped socket puts a prop.");
                return;
            }

            change.Stand = new Vector3(x, 0f, z);
        }

        /// <summary>What the row holds, in the hand the line names.</summary>
        private static void Hand(string where, string[] fields, List<string> faults, Change change)
        {
            if (fields.Length < 4)
            {
                faults.Add(
                    where + ": '" + HandDirective + "' is '" + HandDirective
                    + " <unit id> left|right <prop>', with '" + BesideGroup.Empty
                    + "' for an empty hand.");
                return;
            }

            var right = false;

            switch (fields[2])
            {
                case "right": right = true; break;
                case "left": break;
                default:
                    faults.Add(
                        where + ": hand '" + fields[2] + "' is not 'left' or 'right'. A body has the "
                        + "two sockets handslot.l and handslot.r and no others.");
                    return;
            }

            if (fields[3] == BesideGroup.Empty)
            {
                if (right) change.RightHandCleared = true;
                else change.LeftHandCleared = true;

                return;
            }

            // A '@x,y,z' turn is not read here on purpose. A held prop's turn is
            // bound beside the prop in MatchSceneBuilder's own table and is a
            // fact about how that asset was authored, not a candidate -- a file
            // that moved one would be asking two questions in one line, and the
            // frame could not say which of them it answered.
            if (fields[3].IndexOf('@') >= 0)
            {
                faults.Add(
                    where + ": held prop '" + fields[3] + "' carries a turn. Which way an asset sits in "
                    + "a fist is bound beside the prop and is not what a candidate moves.");
                return;
            }

            string path = BesideGroup.ArtRoot + fields[3];
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (model == null)
            {
                faults.Add(where + ": held prop '" + fields[3] + "' — nothing imported at " + path);
                return;
            }

            if (right) change.RightHand = model;
            else change.LeftHand = model;
        }

        /// <summary>The atlas the row wears.</summary>
        private static Texture2D Atlas(string where, string spec, List<string> faults)
        {
            string path = BesideGroup.ArtRoot + spec;
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

            if (texture == null)
            {
                faults.Add(where + ": atlas '" + spec + "' — nothing imported at " + path);
            }

            return texture;
        }

        /// <summary>
        /// Where the row's effects leave from: a node by name, or the tip of one
        /// along a direction.
        /// </summary>
        /// <remarks>
        /// The node is not checked against the model here, unlike every other
        /// reference in this file, because which nodes a body carries depends on
        /// the model bound to the row and this file names a row rather than a
        /// model. <c>EffectAnchor</c> already throws by name at bind time when a
        /// node is not there, which is a loud failure in the run rather than a
        /// silent one in the picture.
        /// </remarks>
        private static EffectAnchor? Anchor(string where, string[] fields, List<string> faults)
        {
            if (fields[2] != TipOf)
            {
                return EffectAnchor.At(fields[2]);
            }

            if (fields.Length < 5)
            {
                faults.Add(
                    where + ": '" + TipOf + "' is '" + AnchorDirective + " <unit id> " + TipOf
                    + " <node> <x,y,z>', where the three are the direction the prop runs in, as in "
                    + "'0,1,0' for a shaft standing up.");
                return null;
            }

            string[] parts = fields[4].Split(',');

            if (parts.Length != 3
                || !float.TryParse(
                    parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(
                    parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                || !float.TryParse(
                    parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                faults.Add(
                    where + ": '" + fields[4] + "' is not three comma-separated numbers, as in '0,1,0'.");
                return null;
            }

            return EffectAnchor.AtTipOf(fields[3], new Vector3(x, y, z));
        }

        /// <summary>Everything after the directive, with its own spacing kept.</summary>
        private static string Rest(string line, string directive) =>
            line.Substring(directive.Length).Trim();
    }
}
