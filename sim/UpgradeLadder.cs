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
        internal UpgradeEdge(int from, int to)
        {
            From = from;
            To = to;
        }

        /// <summary>The unit a swap consumes.</summary>
        public int From { get; }

        /// <summary>The unit it becomes. Always the larger of the two ids.</summary>
        public int To { get; }

        public override string ToString() =>
            From.ToString(CultureInfo.InvariantCulture)
            + " -> "
            + To.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The kinds of thing a walk over a whole ladder can have to say about it.
    /// </summary>
    /// <remarks>
    /// The first two are faults and the last three are notes, and which is which
    /// is <see cref="LadderReport"/>'s to say rather than this enum's: a caller
    /// reads the two lists it is given and never sorts one kind out of the
    /// other.
    /// </remarks>
    public enum LadderRemark
    {
        /// <summary>The two ends of one edge are units of different roles.</summary>
        MixedRoles,

        /// <summary>Two units are joined by paths that are not all the same length.</summary>
        UnequalRoads,

        /// <summary>A unit the ladder names that nothing upgrades into.</summary>
        Root,

        /// <summary>A unit the ladder names that upgrades into nothing.</summary>
        Leaf,

        /// <summary>An edge whose target costs no more than its source.</summary>
        FlatOrFallingPrice,
    }

    /// <summary>
    /// One thing a completeness walk found, the units it is about, and the
    /// sentence a person reads.
    /// </summary>
    public readonly struct LadderFinding
    {
        internal LadderFinding(LadderRemark remark, int subject, int other, string sentence)
        {
            Remark = remark;
            Subject = subject;
            Other = other;
            Sentence = sentence;
        }

        /// <summary>Which of the five this is.</summary>
        public LadderRemark Remark { get; }

        /// <summary>The unit id this is about: the source of an edge, or the unit itself.</summary>
        public int Subject { get; }

        /// <summary>
        /// The second unit id, or zero where the remark is about one unit. Zero
        /// is already "no unit" in this id space.
        /// </summary>
        public int Other { get; }

        /// <summary>What it says, in one line, with the labels and the numbers in it.</summary>
        public string Sentence { get; }

        public override string ToString() => Sentence;
    }

    /// <summary>
    /// What a walk over a whole ladder came to: the faults, which are never a
    /// legitimate roster, and the notes, which are design statements.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Both lists are returned and neither is thrown.</b> A second pass
    /// inside the loader would apply this roster's design rules to every ladder
    /// -- a test fixture, a hand-typed file, a branch mid-edit -- and make a
    /// design statement an unloadable file, so a half-authored roster could not
    /// be run at all.
    /// </para>
    /// <para>
    /// <b>Two postures and no third.</b> A fault is never a legitimate roster
    /// and the build gate goes red on one. A note is a design statement, printed
    /// and never judged. There is no suppression mechanism, because a warning
    /// nobody has to clear is the stale-content failure this whole arrangement
    /// exists to prevent, wearing a different hat.
    /// </para>
    /// </remarks>
    public sealed class LadderReport
    {
        private readonly LadderFinding[] _faults;

        private readonly LadderFinding[] _notes;

        internal LadderReport(LadderFinding[] faults, LadderFinding[] notes)
        {
            _faults = faults;
            _notes = notes;
        }

        /// <summary>Mixed roles and unequal roads. A build gate fails on any of these.</summary>
        public IReadOnlyList<LadderFinding> Faults => _faults;

        /// <summary>Roots, leaves and flat or falling prices. Printed, never judged.</summary>
        public IReadOnlyList<LadderFinding> Notes => _notes;

        /// <summary>Whether there is nothing here a build gate should fail on.</summary>
        public bool HasNoFaults => _faults.Length == 0;
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
    /// <b>A ladder with no edges at all is legal.</b> A roster half-way through
    /// being authored has to stay loadable, and a ladder with no edges folds
    /// nothing into a content hash.
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

        /// <summary>The words a row here may open with.</summary>
        private static readonly string[] RowWords = { Keyword, LayoutKeyword };

        /// <summary>Ids are <c>u16</c> in the record format, and zero means "no unit".</summary>
        private const int MinimumId = 1;

        private const int MaximumId = 65535;

        /// <summary>
        /// What a completeness walk calls itself when it resolves an id. A walk
        /// runs against the table the ladder was parsed against, where every id
        /// in it resolved already, so a refusal naming this is a caller that
        /// handed over two tables that were never checked together -- a fault in
        /// the program rather than in anybody's authored content, which is why
        /// nothing here rewraps it as a <see cref="ContentException"/>.
        /// </summary>
        private const string LadderWalk = "a ladder walked against a table it was not parsed against";

        private readonly UpgradeEdge[] _edges;

        private UpgradeLadder(UpgradeEdge[] edges, int layout, Hash64 contentHash)
        {
            _edges = edges;
            Layout = layout;
            ContentHash = contentHash;
        }

        /// <summary>The edges, in canonical order -- ascending by source and then by target.</summary>
        public IReadOnlyList<UpgradeEdge> Edges => _edges;

        /// <summary>How many edges there are. Zero is a legal answer.</summary>
        public int Count => _edges.Length;

        /// <summary>
        /// Whether this id is some edge's target, and therefore a rung that has
        /// to be upgraded into rather than placed.
        /// </summary>
        /// <remarks>
        /// A linear scan, on the arrangement the rest of this file is built on:
        /// a ladder is a handful of rows and the alternative is a hashed
        /// collection whose enumeration order is an implementation detail. This
        /// is the one question the build phase asks a ladder, and asking it is
        /// the reversal of the standing claim that the simulation never reads
        /// this file -- see <see cref="BuildPhase.Resolve"/>.
        /// </remarks>
        public bool IsTargetOfAnEdge(int typeId)
        {
            for (int index = 0; index < _edges.Length; index++)
            {
                if (_edges[index].To == typeId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Which row layout this ladder was written in and read through.</summary>
        public int Layout { get; }

        /// <summary>
        /// A fold over the edges in file order, under a label naming this file
        /// and its layout.
        /// </summary>
        /// <remarks>
        /// This is not a hash anything is stamped with. It exists to be folded
        /// into a unit table's own content hash by
        /// <see cref="UnitTypeTable.WithLadder"/>, which is where the rule that
        /// an empty ladder changes nothing lives -- so a caller that folds this
        /// value by hand has bypassed that rule rather than reimplemented it.
        /// </remarks>
        public Hash64 ContentHash { get; }

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

            var edges = new List<UpgradeEdge>();
            int layout = 0;
            bool declared = false;
            int previousFrom = 0;
            int previousTo = 0;

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                string[] fields = row.Fields;

                if (string.Equals(row.Keyword, LayoutKeyword, StringComparison.Ordinal))
                {
                    layout = ReadLayout(source, row.Line, fields, declared);
                    declared = true;
                    continue;
                }

                DataText.RequireRow(source, row, RowWords);

                if (!declared)
                {
                    throw new ContentException(
                        source,
                        row.Line,
                        "is an edge above any '"
                        + LayoutKeyword
                        + "' row. The layout says how to read a row, so it is stated before the first of "
                        + "them -- and it is stated rather than assumed, because there is no ladder written "
                        + "before the row existed for this reader to be lenient toward.");
                }

                if (fields.Length != FieldCount)
                {
                    throw DataText.WrongFieldCount(source, row.Line, Keyword, FieldCount, fields.Length);
                }

                int from = DataText.IntegerInRange(
                    source, row.Line, "the source id", fields[1], MinimumId, MaximumId);
                int to = DataText.IntegerInRange(
                    source, row.Line, "the target id", fields[2], MinimumId, MaximumId);

                DataText.RequireType(source, row.Line, types, from, null, "an upgrade's source");
                DataText.RequireType(source, row.Line, types, to, null, "an upgrade's target");

                if (to <= from)
                {
                    throw new ContentException(
                        source,
                        row.Line,
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
                        row.Line,
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
                        row.Line,
                        "is out of canonical order: rows ascend strictly by source id and then by target "
                        + "id. The order is asserted rather than sorted on load, because it is what makes a "
                        + "duplicate a comparison against the row above -- and because these edges fold "
                        + "into a content hash in file order, so sorting would leave one ladder with two "
                        + "hashes.");
                }

                previousFrom = from;
                previousTo = to;
                edges.Add(new UpgradeEdge(from, to));
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

            Hash64 hash = Hash64.Start(HashLabelOf(layout)).Add(edges.Count);

            foreach (UpgradeEdge edge in edges)
            {
                hash = hash.Add(edge.From, edge.To);
            }

            return new UpgradeLadder(edges.ToArray(), layout, hash);
        }

        /// <summary>
        /// The label this layout's edges fold under. It names both the file and
        /// its row layout, so a ladder read through one branch cannot hash equal
        /// to a ladder read through another.
        /// </summary>
        private static string HashLabelOf(int layout)
        {
            switch (layout)
            {
                case 1:
                    return "upgrade-ladder/1";

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(layout),
                        "Row layout "
                        + layout.ToString(CultureInfo.InvariantCulture)
                        + " has no reader branch in this ladder.");
            }
        }

        /// <summary>
        /// Walks the whole ladder against the roster and says what is wrong with
        /// it and what is merely worth knowing. Returns both; throws neither.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>One sweep, in id order, and no path enumeration.</b> A target id
        /// always exceeds its source, so the edge list is a topological order by
        /// construction: when an edge leaving a unit is reached, every path into
        /// that unit is already complete. The shortest and longest path between
        /// every reachable pair therefore fall out of a single pass over the
        /// edges, and unequal roads is the pairs where those two differ. There is
        /// no visited set and no cycle handling, because a cycle cannot be
        /// written down.
        /// </para>
        /// <para>
        /// <b>Roots and leaves are units the ladder names.</b> A unit in no edge
        /// at all is neither, so an empty ladder has nothing to say about a
        /// roster rather than calling every row both.
        /// </para>
        /// </remarks>
        public LadderReport Completeness(UnitTypeTable types)
        {
            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            var faults = new List<LadderFinding>();
            var notes = new List<LadderFinding>();

            int count = types.Count;
            var named = new bool[count];
            var hasIncoming = new bool[count];
            var hasOutgoing = new bool[count];

            // Path length between every pair, as two n-by-n grids of "no path
            // yet is zero". A ladder is a handful of rows over a handful of
            // units, so a grid costs less than anything cleverer would.
            var shortest = new int[count * count];
            var longest = new int[count * count];

            for (int index = 0; index < _edges.Length; index++)
            {
                UpgradeEdge edge = _edges[index];
                UnitType from = types.Require(edge.From, null, LadderWalk);
                UnitType to = types.Require(edge.To, null, LadderWalk);

                if (from.Role != to.Role)
                {
                    faults.Add(new LadderFinding(
                        LadderRemark.MixedRoles,
                        from.Id,
                        to.Id,
                        Describe(from) + " and " + Describe(to)
                        + ", so this edge joins two different kinds of thing rather than two rungs of "
                        + "one line."));
                }

                if (to.Cost <= from.Cost)
                {
                    notes.Add(new LadderFinding(
                        LadderRemark.FlatOrFallingPrice,
                        from.Id,
                        to.Id,
                        from.ToString() + " costs " + Gold(from.Cost) + " and " + to.ToString() + " costs "
                        + Gold(to.Cost) + ", so the upgrade is not dearer than what it replaces."));
                }

                int source = IndexOf(types, from);
                int target = IndexOf(types, to);

                named[source] = true;
                named[target] = true;
                hasOutgoing[source] = true;
                hasIncoming[target] = true;

                Reach(shortest, longest, count, source, target, 1, 1);

                // Every unit that already reaches this edge's source now reaches
                // its target one step further along. The loop stops at the
                // source's own index because an edge only ever points at a
                // larger id, so nothing above it can reach it.
                for (int start = 0; start < source; start++)
                {
                    int at = (start * count) + source;

                    if (shortest[at] > 0)
                    {
                        Reach(shortest, longest, count, start, target, shortest[at] + 1, longest[at] + 1);
                    }
                }
            }

            for (int start = 0; start < count; start++)
            {
                for (int end = 0; end < count; end++)
                {
                    int at = (start * count) + end;

                    if (shortest[at] == 0 || shortest[at] == longest[at])
                    {
                        continue;
                    }

                    faults.Add(new LadderFinding(
                        LadderRemark.UnequalRoads,
                        types.Types[start].Id,
                        types.Types[end].Id,
                        types.Types[start].ToString() + " reaches " + types.Types[end].ToString()
                        + " by paths of " + Upgrades(shortest[at]) + " and " + Upgrades(longest[at])
                        + ", so one route to the same unit costs fewer upgrades than another."));
                }
            }

            for (int index = 0; index < count; index++)
            {
                if (named[index] && !hasIncoming[index])
                {
                    notes.Add(new LadderFinding(
                        LadderRemark.Root,
                        types.Types[index].Id,
                        0,
                        types.Types[index].ToString() + " has no incoming edge, so nothing upgrades into it."));
                }
            }

            for (int index = 0; index < count; index++)
            {
                if (named[index] && !hasOutgoing[index])
                {
                    notes.Add(new LadderFinding(
                        LadderRemark.Leaf,
                        types.Types[index].Id,
                        0,
                        types.Types[index].ToString()
                        + " has no outgoing edge, so it is the top of its line."));
                }
            }

            return new LadderReport(faults.ToArray(), notes.ToArray());
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
        /// Records a path from one unit to another, keeping the shortest and the
        /// longest seen so far. A pair whose two answers differ is unequal roads.
        /// </summary>
        private static void Reach(
            int[] shortest,
            int[] longest,
            int count,
            int start,
            int end,
            int shorter,
            int longer)
        {
            int at = (start * count) + end;

            if (shortest[at] == 0 || shorter < shortest[at])
            {
                shortest[at] = shorter;
            }

            if (longer > longest[at])
            {
                longest[at] = longer;
            }
        }

        /// <summary>
        /// Where in the table a row sits, which is what the walk's grids are
        /// indexed by. A linear scan for the reason
        /// <see cref="UnitTypeTable.TryById"/> is one: the table has a handful of
        /// rows and the obvious dictionary is a banned type.
        /// </summary>
        /// <remarks>
        /// An id the table does not define is refused where it is resolved,
        /// above, so what arrives here is a row this table handed out and the
        /// scan finds it. Falling off the end is the two halves of the table
        /// disagreeing with each other rather than anything an author wrote,
        /// and it says so instead of returning a -1 that would index a grid.
        /// </remarks>
        private static int IndexOf(UnitTypeTable types, UnitType type)
        {
            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Id == type.Id)
                {
                    return index;
                }
            }

            throw new SimulationException(
                "This unit type table resolved "
                + type.ToString()
                + " and does not list it, so the rows it hands out and the rows it exposes are not the "
                + "same rows.");
        }

        /// <summary>A unit and what its role does, for a sentence naming both ends of an edge.</summary>
        private static string Describe(UnitType type) =>
            type.ToString() + " " + (type.Role == UnitRole.Placed ? "stands" : "walks");

        private static string Gold(int cost) => cost.ToString(CultureInfo.InvariantCulture) + " gold";

        private static string Upgrades(int length) =>
            length.ToString(CultureInfo.InvariantCulture) + (length == 1 ? " upgrade" : " upgrades");

    }
}
