using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace View.Editor
{
    /// <summary>
    /// One candidate look, read from a file: what to draw an effect as instead
    /// of what the game draws it as, and the question it is a candidate for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why a file and not a table in code.</b> The same reason
    /// <see cref="CandidateSet"/> is one. What the game draws is what
    /// <see cref="MatchTuning"/> already says; a candidate is a question being
    /// put to somebody — "should a hastened body be told from a slowed one on
    /// the body" — and the answer is usually "not that one, try it wider". A
    /// file is edited and re-rendered by whoever is looking, without an agent, a
    /// recompile or a commit in between.
    /// </para>
    /// <para>
    /// <b>Every key is a <see cref="EffectLook"/> member name, and an unknown
    /// one is a fault.</b> That is the whole of the validation and it is the
    /// point of it: a look drawn from a misspelt key renders the shipped value
    /// under a candidate's filename, and two candidate frames that came out
    /// identical because both keys were wrong is a picture that answers a
    /// question it was never asked. The fault lists the nearest legal names.
    /// </para>
    /// <para>
    /// <b>A candidate may also move a row's shape.</b> What a bubble leaves is
    /// bound per unit rather than tuned, so "what if the Witch's hex were a cage
    /// rather than plates" cannot be asked with a number at all — it is a
    /// <c>signature</c> line, applied to a copy of the art the capture built.
    /// </para>
    /// </remarks>
    public sealed class EffectLookFile
    {
        /// <summary>The directive that names what this candidate is called.</summary>
        public const string LabelDirective = "label";

        /// <summary>The directive that names the question it is a candidate for.</summary>
        public const string QuestionDirective = "question";

        /// <summary>The directive that moves a row's bubble shape.</summary>
        public const string SignatureDirective = "signature";

        /// <summary>The directive that moves a row's shot shape.</summary>
        public const string ShotDirective = "shot";

        private EffectLookFile(
            string name,
            string label,
            string question,
            EffectLook look,
            IReadOnlyDictionary<int, BubbleSignature> bubbles,
            IReadOnlyDictionary<int, ShotSignature> shots)
        {
            Name = name;
            Label = label;
            Question = question;
            Look = look;
            Bubbles = bubbles;
            Shots = shots;
        }

        /// <summary>The file's own name without its extension — what frames are named after.</summary>
        public string Name { get; }

        /// <summary>What this candidate is called, for the sheet beside it.</summary>
        public string Label { get; }

        /// <summary>The question it is a candidate for.</summary>
        public string Question { get; }

        /// <summary>The look itself.</summary>
        public EffectLook Look { get; }

        /// <summary>Which rows draw a different bubble shape, by unit type id.</summary>
        public IReadOnlyDictionary<int, BubbleSignature> Bubbles { get; }

        /// <summary>Which rows draw a different shot shape, by unit type id.</summary>
        public IReadOnlyDictionary<int, ShotSignature> Shots { get; }

        /// <summary>
        /// Every member a candidate file may name, with the kind of value it
        /// takes. Reflected off <see cref="EffectLook"/> rather than listed, so
        /// the legal set is exactly what exists and cannot fall behind it.
        /// </summary>
        public static IReadOnlyDictionary<string, Type> Members { get; } =
            typeof(EffectLook)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.Name != nameof(EffectLook.IsShipped))
                .Where(property => property.Name != nameof(EffectLook.OverriddenCount))
                .ToDictionary(property => property.Name, property => property.PropertyType);

        /// <summary>Reads one candidate out of <paramref name="path"/>.</summary>
        public static EffectLookFile Read(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A candidate look needs a path.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("No candidate look at " + path, path);
            }

            return Parse(Path.GetFileNameWithoutExtension(path), File.ReadAllLines(path));
        }

        /// <summary>
        /// The same, from lines already in hand. What the tests drive, so a
        /// parse can be asserted without a file on disk.
        /// </summary>
        public static EffectLookFile Parse(string name, IReadOnlyList<string> lines)
        {
            if (lines == null) throw new ArgumentNullException(nameof(lines));

            var numbers = new Dictionary<string, float>();
            var colours = new Dictionary<string, Color>();
            var bubbles = new Dictionary<int, BubbleSignature>();
            var shots = new Dictionary<int, ShotSignature>();
            string label = null;
            string question = null;

            for (int index = 0; index < lines.Count; index++)
            {
                string line = (lines[index] ?? string.Empty).Trim();

                if (IsSkippable(line))
                {
                    continue;
                }

                string where = name + " line " + (index + 1);
                string[] parts = line.Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    throw new FormatException(
                        where + ": '" + line + "' is a key with no value. Every line is a name and what "
                        + "to draw it as.");
                }

                string key = parts[0];
                string value = parts[1].Trim();

                switch (key)
                {
                    case LabelDirective:
                        label = value;
                        continue;
                    case QuestionDirective:
                        question = value;
                        continue;
                    case SignatureDirective:
                        Bind(where, value, bubbles);
                        continue;
                    case ShotDirective:
                        Bind(where, value, shots);
                        continue;
                }

                if (!Members.TryGetValue(key, out Type kind))
                {
                    throw new FormatException(
                        where + ": nothing on the look is called '" + key + "'. " + Near(key));
                }

                if (kind == typeof(Color))
                {
                    colours[key] = ParseColour(where, key, value);
                }
                else
                {
                    numbers[key] = ParseNumber(where, key, value, kind);
                }
            }

            return new EffectLookFile(
                name,
                label ?? name,
                question ?? string.Empty,
                new EffectLook(numbers, colours),
                bubbles,
                shots);
        }

        /// <summary>
        /// The art the capture built, with this candidate's shapes on the rows
        /// it names. A copy: the bound art is left as the scene builder wired
        /// it, so two candidates rendered in one run cannot pick up each
        /// other's shapes.
        /// </summary>
        public MatchArt Applied(MatchArt art)
        {
            if (art == null) throw new ArgumentNullException(nameof(art));

            if (Bubbles.Count == 0 && Shots.Count == 0)
            {
                return art;
            }

            var moved = new List<UnitArt>(art.Units.Count);

            foreach (UnitArt unit in art.Units)
            {
                RowSignature had = unit.Signature;

                BubbleSignature bubble = Bubbles.TryGetValue(unit.UnitId, out BubbleSignature wanted)
                    ? wanted
                    : had.Bubble;

                ShotSignature shot = Shots.TryGetValue(unit.UnitId, out ShotSignature wantedShot)
                    ? wantedShot
                    : had.Shot;

                moved.Add(
                    bubble == had.Bubble && shot == had.Shot
                        ? unit
                        : unit.WithSignature(bubble, shot));
            }

            return MatchArt.Of(moved, art.CreepWalkClip, art.CreepDeathClip);
        }

        /// <summary>
        /// A whole-line comment, or an empty line. Everything else is a
        /// directive and keeps every character it was written with.
        /// </summary>
        /// <remarks>
        /// <b>A hash inside a line is not a comment, and treating it as one was
        /// wrong twice.</b> A colour may be written <c>#rrggbb</c>, so
        /// <c>SlowRingColor #FF0000</c> cut at the first hash is a key with no
        /// value; and a <c>question</c> naming the issue it came from —
        /// <c>#254</c>, <c>#266</c>, <c>#270</c> — would lose the rest of its
        /// sentence. Both are real lines in this folder. So a comment is a line
        /// that opens with a hash and nothing else is, which is a rule that can
        /// be stated in one sentence at the top of a candidate file.
        /// </remarks>
        private static bool IsSkippable(string line) =>
            line.Length == 0 || line[0] == '#';

        /// <summary>Reads a <c>&lt;unit id&gt; &lt;shape&gt;</c> pair onto a table.</summary>
        private static void Bind<TSignature>(
            string where, string value, IDictionary<int, TSignature> onto)
            where TSignature : struct, Enum
        {
            string[] parts = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                throw new FormatException(
                    where + ": '" + value + "' is not a unit id and a shape.");
            }

            if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int unitId))
            {
                throw new FormatException(where + ": '" + parts[0] + "' is not a unit type id.");
            }

            if (!Enum.TryParse(parts[1], out TSignature shape) || !Enum.IsDefined(typeof(TSignature), shape))
            {
                throw new FormatException(
                    where + ": there is no " + typeof(TSignature).Name + " called '" + parts[1] + "'. "
                    + "The shapes are " + string.Join(", ", Enum.GetNames(typeof(TSignature))) + ".");
            }

            onto[unitId] = shape;
        }

        /// <summary>Reads one number, and says which member refused it.</summary>
        private static float ParseNumber(string where, string key, string value, Type kind)
        {
            if (kind == typeof(bool))
            {
                if (bool.TryParse(value, out bool flag))
                {
                    return flag ? 1f : 0f;
                }
            }

            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float number))
            {
                return number;
            }

            throw new FormatException(
                where + ": " + key + " takes a number" + (kind == typeof(bool) ? " or true/false" : string.Empty)
                + ", and '" + value + "' is not one.");
        }

        /// <summary>Reads one colour, written <c>r,g,b</c> or <c>#rrggbb</c>.</summary>
        private static Color ParseColour(string where, string key, string value)
        {
            if (value.StartsWith("#", StringComparison.Ordinal))
            {
                if (ColorUtility.TryParseHtmlString(value, out Color parsed))
                {
                    return parsed;
                }

                throw new FormatException(where + ": " + key + " cannot read the colour '" + value + "'.");
            }

            string[] parts = value.Split(',');

            if (parts.Length < 3 || parts.Length > 4)
            {
                throw new FormatException(
                    where + ": " + key + " takes r,g,b — three numbers from nought to one, or four with "
                    + "an alpha — or a #rrggbb. '" + value + "' is neither.");
            }

            var channels = new float[4] { 0f, 0f, 0f, 1f };

            for (int index = 0; index < parts.Length; index++)
            {
                if (!float.TryParse(
                        parts[index].Trim(),
                        NumberStyles.Float,
                        CultureInfo.InvariantCulture,
                        out channels[index]))
                {
                    throw new FormatException(
                        where + ": " + key + "'s channel " + (index + 1) + " is '" + parts[index].Trim()
                        + "', which is not a number.");
                }
            }

            return new Color(channels[0], channels[1], channels[2], channels[3]);
        }

        /// <summary>
        /// The legal names closest to one that was refused. A misspelt key is
        /// the failure this file exists to make loud, and "no such member" plus
        /// eighty-nine alternatives is not much louder than silence.
        /// </summary>
        private static string Near(string key)
        {
            string[] close = Members.Keys
                .Where(name => name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0
                    || key.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                .OrderBy(name => name, StringComparer.Ordinal)
                .Take(8)
                .ToArray();

            return close.Length > 0
                ? "Did you mean " + string.Join(", ", close) + "?"
                : "There are " + Members.Count + " members, and every one is a MatchTuning name.";
        }
    }
}
