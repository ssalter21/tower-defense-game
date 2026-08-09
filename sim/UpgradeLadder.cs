using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One row of the ladder: the id of the unit a swap consumes, and the id of
    /// the unit it becomes.
    /// </summary>
    /// <remarks>
    /// Two ids and nothing else. No tier number, no cost and no direction
    /// marker: the target's price is its own row's <see cref="UnitType.Cost"/>,
    /// and the direction is which of the two ids is written first.
    /// </remarks>
    public readonly struct UpgradeEdge
    {
        internal UpgradeEdge(int from, int to, int line)
        {
            From = from;
            To = to;
            Line = line;
        }

        /// <summary>The unit a swap consumes.</summary>
        public int From { get; }

        /// <summary>The unit it becomes. Always the larger of the two ids.</summary>
        public int To { get; }

        /// <summary>The line of the authored file this edge came from, for messages.</summary>
        internal int Line { get; }

        public override string ToString() =>
            From.ToString(CultureInfo.InvariantCulture)
            + " -> "
            + To.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Which unit follows which: the upgrade edges, in canonical order,
    /// validated against the unit type table that has to already know every id
    /// they name.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The simulation never reads this.</b> An edge is an annotation on the
    /// roster and nothing in a tick loop can observe one -- a parsed ladder is
    /// held by the command line and is never handed to <see cref="Run"/>, which
    /// is what makes that a property of the code rather than a promise about it.
    /// It lives in this assembly because every parser does: the assembly cannot
    /// open a file, so a caller reads the bytes and hands them over.
    /// </para>
    /// <para>
    /// <b>An edge joins two unit ids, not two towers.</b> Neither end is
    /// required to be a <see cref="UnitRole.Placed"/> unit, so a ladder of
    /// creeps stays structurally possible; whether the two ends agree on a role
    /// at all is one of the questions <see cref="Completeness"/> answers, and it
    /// answers it by returning rather than by refusing.
    /// </para>
    /// <para>
    /// <b>The target id must exceed the source id, so a cycle is unstateable
    /// rather than detected.</b> Ids ascend strictly down
    /// <c>content/units.txt</c> and are never reused, so this file is a
    /// topological order by construction and every walk over it is one sweep in
    /// id order with no visited set. What it costs is that a rung can never be
    /// inserted below an existing unit.
    /// </para>
    /// <para>
    /// <b>The order is asserted, not sorted.</b> Rows ascend strictly by
    /// <c>(From, To)</c>, which makes a duplicate edge a comparison against the
    /// row above, and keeps the file canonical -- the fold that carries these
    /// edges into a unit table's content hash walks them in file order.
    /// </para>
    /// <para>
    /// <b>A ladder with no edges at all is legal</b>, and it is what
    /// <c>content/upgrades.txt</c> was born as. A roster mid-edit has to stay
    /// loadable, and an empty ladder folds nothing.
    /// </para>
    /// </remarks>
    public sealed class UpgradeLadder
    {
        /// <summary>The one row layout this file has ever been written in.</summary>
        public const int CurrentLayout = 1;

        private const string Keyword = "upgrade";

        private const string LayoutKeyword = "layout";

        /// <summary>Fields per row, keyword included: the keyword and two ids.</summary>
        private const int FieldCount = 3;

        /// <summary>Ids are <c>u16</c> in the record format, and zero means "no unit".</summary>
        private const int MinimumId = 1;

        private const int MaximumId = 65535;

        private readonly UpgradeEdge[] _edges;

        private UpgradeLadder(UpgradeEdge[] edges, int layout)
        {
            _edges = edges;
            Layout = layout;
        }

        /// <summary>The edges, in canonical order -- ascending by source and then by target.</summary>
        public IReadOnlyList<UpgradeEdge> Edges => _edges;

        /// <summary>How many edges there are. Zero is a legal answer.</summary>
        public int Count => _edges.Length;

        /// <summary>Which row layout this ladder was written in and read through.</summary>
        public int Layout { get; }

        /// <summary>Parses a ladder from text, against the types its ids may name.</summary>
        public static UpgradeLadder Parse(string text, UnitTypeTable types) =>
            Parse("upgrade ladder", text, types);

        /// <summary>Parses a ladder from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static UpgradeLadder ParseUtf8(byte[] utf8, UnitTypeTable types) =>
            ParseUtf8("upgrade ladder", utf8, types);

        /// <summary>Parses a ladder, naming the content in any error message.</summary>
        public static UpgradeLadder ParseUtf8(string source, byte[] utf8, UnitTypeTable types) =>
            Parse(source, DataText.FromUtf8(source, utf8), types);

        /// <summary>Parses a ladder, naming the content in any error message.</summary>
        public static UpgradeLadder Parse(string source, string text, UnitTypeTable types)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            string[] lines = DataText.SplitLines(text);
            var edges = new List<UpgradeEdge>();
            int layout = 0;
            bool declared = false;
            int previousFrom = 0;
            int previousTo = 0;

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                int number = index + 1;

                if (DataText.IsBlankOrComment(line))
                {
                    continue;
                }

                string[] fields = DataText.Fields(source, number, line);

                if (string.Equals(fields[0], LayoutKeyword, StringComparison.Ordinal))
                {
                    layout = ReadLayout(source, number, fields, declared);
                    declared = true;
                    continue;
                }

                if (!string.Equals(fields[0], Keyword, StringComparison.Ordinal))
                {
                    throw new ContentException(
                        source,
                        number,
                        "starts with '"
                        + fields[0]
                        + "', but the rows this file has are '"
                        + Keyword
                        + "' and '"
                        + LayoutKeyword
                        + "'.");
                }

                if (!declared)
                {
                    throw new ContentException(
                        source,
                        number,
                        "is an edge above any '"
                        + LayoutKeyword
                        + "' row. The layout says how to read a row, so it is stated before the first of "
                        + "them -- and it is stated rather than assumed, because there is no ladder written "
                        + "before the row existed for this reader to be lenient toward.");
                }

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, number, Keyword, FieldCount, fields.Length);
                }

                int from = DataText.IntegerInRange(source, number, "the source id", fields[1], MinimumId, MaximumId);
                int to = DataText.IntegerInRange(source, number, "the target id", fields[2], MinimumId, MaximumId);

                RequireKnown(source, number, types, "upgrades", from);
                RequireKnown(source, number, types, "upgrades into", to);

                if (to <= from)
                {
                    throw new ContentException(
                        source,
                        number,
                        "upgrades id "
                        + from.ToString(CultureInfo.InvariantCulture)
                        + " into id "
                        + to.ToString(CultureInfo.InvariantCulture)
                        + ", and a target id has to exceed its source. Ids ascend down the unit table and "
                        + "are never reused, so that rule makes a cycle unstateable rather than something a "
                        + "walk has to detect. A tier that belongs below a row already authored is a new row "
                        + "at a new id.");
                }

                if (from == previousFrom && to == previousTo)
                {
                    throw new ContentException(
                        source,
                        number,
                        "states the edge "
                        + from.ToString(CultureInfo.InvariantCulture)
                        + " -> "
                        + to.ToString(CultureInfo.InvariantCulture)
                        + " a second time. One row is one edge, and a repeat would fold into the unit "
                        + "table's content hash twice while meaning what one row already meant.");
                }

                if (from < previousFrom || (from == previousFrom && to < previousTo))
                {
                    throw new ContentException(
                        source,
                        number,
                        "is out of canonical order: rows ascend strictly by source id and then by target "
                        + "id. The order is asserted rather than sorted on load, because it is what makes a "
                        + "duplicate a comparison against the row above -- and because these edges fold "
                        + "into a content hash in file order, so sorting would leave one ladder with two "
                        + "hashes.");
                }

                previousFrom = from;
                previousTo = to;
                edges.Add(new UpgradeEdge(from, to, number));
            }

            if (!declared)
            {
                throw new ContentException(
                    source,
                    0,
                    "has no '"
                    + LayoutKeyword
                    + "' row. A ladder with no edges in it is legal; a ladder that does not say how its "
                    + "rows are written is not.");
            }

            return new UpgradeLadder(edges.ToArray(), layout);
        }

        /// <summary>
        /// Whether this reader has a branch for that row layout.
        /// </summary>
        /// <remarks>
        /// Spelled out one layout at a time rather than as
        /// <c>layout &lt;= current</c>, for the reason
        /// <see cref="UnitTypeTable.IsKnownLayout"/> is: these are the branches
        /// that exist rather than the branches that ought to.
        /// </remarks>
        public static bool IsKnownLayout(int layout) => layout == 1;

        /// <summary>
        /// Reads the layout a file declares. It comes before every row it
        /// governs, because it is what says how to read one.
        /// </summary>
        /// <remarks>
        /// There is no separate refusal for a layout row that arrives after an
        /// edge, and there is no hole where one would go: an edge above the
        /// first layout row is already refused, so a layout row with edges above
        /// it is always the second one and is refused as that.
        /// </remarks>
        private static int ReadLayout(string source, int line, string[] fields, bool declared)
        {
            if (fields.Length != 2)
            {
                throw DataText.WrongFieldCount(source, line, LayoutKeyword, 2, fields.Length);
            }

            if (declared)
            {
                throw new ContentException(
                    source,
                    line,
                    "is a second '"
                    + LayoutKeyword
                    + "' row. A file is written in one row layout, and two rows claiming two of them means "
                    + "the rows above and below this line would be read against different field orders.");
            }

            int layout = DataText.Integer(source, line, "the row layout", fields[1]);

            if (!IsKnownLayout(layout))
            {
                throw new ContentException(
                    source,
                    line,
                    "declares row layout "
                    + layout.ToString(CultureInfo.InvariantCulture)
                    + ", and this reader has a branch for "
                    + CurrentLayout.ToString(CultureInfo.InvariantCulture)
                    + " alone. A layout that was skipped, or a branch somebody deleted, is refused here "
                    + "rather than read against whichever field order happened to be nearest.");
            }

            return layout;
        }

        /// <summary>
        /// An id required to name a row of the table. An unknown id refuses to
        /// load rather than being skipped, exactly as it does everywhere else a
        /// content file names one.
        /// </summary>
        private static void RequireKnown(
            string source,
            int line,
            UnitTypeTable types,
            string verb,
            int id)
        {
            if (types.TryById(id, out UnitType? _))
            {
                return;
            }

            throw new ContentException(
                source,
                line,
                verb
                + " type id "
                + id.ToString(CultureInfo.InvariantCulture)
                + ", which the unit type table does not define. An edge naming a row nobody authored is "
                + "an edge nothing can price or draw.");
        }
    }
}
