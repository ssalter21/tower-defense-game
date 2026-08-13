using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One band of the performance bonus: the percentile a wave has to reach,
    /// and what reaching it pays on top of the income base.
    /// </summary>
    public sealed class PerformanceBand
    {
        internal PerformanceBand(int percentileThreshold, int bonusPercentOfBase)
        {
            PercentileThreshold = percentileThreshold;
            BonusPercentOfBase = bonusPercentOfBase;
        }

        /// <summary>The percentile of the field this band starts at, inclusive.</summary>
        public int PercentileThreshold { get; }

        /// <summary>What this band pays, as a percentage of the income base. Never negative.</summary>
        public int BonusPercentOfBase { get; }

        public override string ToString() =>
            "p"
            + PercentileThreshold.ToString(CultureInfo.InvariantCulture)
            + " pays +"
            + BonusPercentOfBase.ToString(CultureInfo.InvariantCulture)
            + "%";

        internal Hash64 Fold(Hash64 hash) => hash.Add(PercentileThreshold).Add(BonusPercentOfBase);
    }

    /// <summary>
    /// Every number the rules are made of, parsed from authored text, and the
    /// content hash over what was parsed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the same arrangement as <see cref="UnitTypeTable"/> and it is
    /// here for the same reason: the numbers live in a committed data file, the
    /// file is handed to <see cref="Parse(string)"/> as text, and nothing in
    /// this assembly knows where it came from.
    /// </para>
    /// <para>
    /// <b><see cref="ContentHash"/> is folded over the parsed integers in field
    /// order, not over the file.</b> Reindent a column, rewrap a comment or
    /// convert the line endings and the hash does not move. Change one number
    /// and it does.
    /// </para>
    /// <para>
    /// <b>Every row is required and none may appear twice.</b> A defaulted rule
    /// is a rule the file does not state and the reader invented, and it would
    /// be folded into the hash as though somebody had authored it.
    /// </para>
    /// <para>
    /// <b>A field is spelled once, in <see cref="Rules"/>.</b> Everything that
    /// reads or writes one goes through that declaration.
    /// </para>
    /// </remarks>
    public sealed class Ruleset
    {
        /// <summary>
        /// Names this ruleset and its field layout inside the hash. The digit is
        /// the layout version: moving, adding or removing a field bumps it.
        /// </summary>
        private const string HashLabel = "ruleset/3";

        /// <summary>The <see cref="InterestCapGold"/> that means no ceiling at all.</summary>
        public const int NoInterestCeiling = 0;

        /// <summary>
        /// The most options either half of a round's menu may carry. A menu is
        /// walked rather than looked up, and a take names a position on it.
        /// </summary>
        public const int MostOptions = 64;

        /// <summary>What a percentage is out of. Not a lever: it is what the word means.</summary>
        private const int Percent = 100;

        /// <summary>
        /// The largest any factor of the damage expression may be -- a matrix
        /// cell, the armour denominator, the floor. It bounds the product of a
        /// hit and a cell, which is what keeps that expression's intermediate
        /// inside a 64-bit integer.
        /// </summary>
        private const int MaximumFactor = 1000000;

        /// <summary>
        /// Every rule the file states, in the order the content hash folds them.
        /// One entry per row keyword, carrying the columns that follow it: the
        /// field each fills, what a refusal calls it, and the range it is held
        /// to.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the only place a ruleset number is spelled.</b>
        /// <see cref="RuleFor"/> looks a row's opening word up here,
        /// <see cref="RowWords"/> is the words it may be,
        /// <see cref="Rule.Read"/> writes what it parsed into
        /// <see cref="_values"/> at the column's field, <see cref="Fold"/> walks
        /// this array in order, and <see cref="With"/> takes a retuned number's
        /// range off the same column the file is held to. A number that can be
        /// parsed is therefore a number that is folded, and the fold has no
        /// order of its own to disagree with the file's.
        /// </para>
        /// <para>
        /// The damage matrix and the performance bands are row-shaped rather
        /// than column-shaped -- three rows of a square read in attack-type
        /// order, and however many ascending bands the file states -- so they
        /// carry no columns and bring their own reader and their own fold. Where
        /// they sit in this array is where they sit in the hash.
        /// </para>
        /// </remarks>
        private static readonly Rule[] Rules =
        {
            Rule.RowShaped("matrix", 5, ReadMatrixRow, FoldMatrix),
            Rule.Numbers(
                "armour",
                new Column(Field.ArmourPercentPerPoint, "the armour coefficient", 0, 1000),
                new Column(Field.ArmourDenominator, "the armour denominator", 1, MaximumFactor)),
            Rule.Numbers("floor", new Column(Field.DamageFloor, "the damage floor", 1, MaximumFactor)),
            Rule.Numbers(
                "interest",
                new Column(Field.InterestPercentPerWave, "the interest rate", 0, 1000),
                new Column(Field.InterestCapGold, "the interest cap", 0, int.MaxValue)),
            Rule.Numbers("income", new Column(Field.IncomeBasePerWave, "the income base", 0, int.MaxValue)),
            Rule.Numbers("purse", new Column(Field.StartingPurseGold, "the starting purse", 0, int.MaxValue)),
            Rule.RowShaped("band", 3, ReadBandRow, FoldBands),
            Rule.Numbers("health", new Column(Field.HealthPoolGold, "the health pool", 1, int.MaxValue)),
            Rule.Numbers(
                "snapshot",
                new Column(Field.FreeSnapshotsPerRun, "the free snapshot count", 0, int.MaxValue),
                new Column(Field.SnapshotPriceGold, "the snapshot price", 0, int.MaxValue)),
        };

        /// <summary>
        /// The column that fills each <see cref="Field"/>, indexed by it. Built
        /// from <see cref="Rules"/> and refusing a field that no column fills or
        /// that two do, so the enum cannot carry a member the declaration does
        /// not -- which would be a number nothing parses, nothing folds and
        /// every reader sees as zero.
        /// </summary>
        private static readonly Column[] ColumnByField = IndexColumns();

        private readonly PerformanceBand[] _bands;

        private readonly int[] _values;

        private Ruleset(Draft draft)
        {
            Matrix = draft.Matrix!;
            _bands = draft.Bands.ToArray();
            _values = draft.Values;
            ContentHash = Fold();
        }

        /// <summary>
        /// The same rules with some of their numbers replaced. The matrix and
        /// the bands are carried across by reference to the same values, so the
        /// two rulesets differ in exactly what was asked for.
        /// </summary>
        private Ruleset(Ruleset original, int[] values)
        {
            Matrix = original.Matrix;
            _bands = original._bands;
            _values = values;
            ContentHash = Fold();
        }

        /// <summary>
        /// One number the ruleset holds. Every member is filled by exactly one
        /// column of <see cref="Rules"/>, which is what indexes
        /// <see cref="_values"/>.
        /// </summary>
        private enum Field
        {
            ArmourPercentPerPoint,
            ArmourDenominator,
            DamageFloor,
            InterestPercentPerWave,
            InterestCapGold,
            IncomeBasePerWave,
            StartingPurseGold,
            HealthPoolGold,
            FreeSnapshotsPerRun,
            SnapshotPriceGold,

            /// <summary>How many of them there are. Not one of them.</summary>
            Count,
        }

        /// <summary>Three attack types against three armour types, as nine percentages.</summary>
        public DamageMatrix Matrix { get; }

        /// <summary>
        /// How much of a target's base effective health one point of armour
        /// adds, in percent. The coefficient in the armour expression.
        /// </summary>
        public int ArmourPercentPerPoint => _values[(int)Field.ArmourPercentPerPoint];

        /// <summary>
        /// What the armour expression divides by at zero armour. A cell is a
        /// percentage of this, so a hundred here makes a cell of 100 the
        /// identity.
        /// </summary>
        public int ArmourDenominator => _values[(int)Field.ArmourDenominator];

        /// <summary>The least a hit may deal. No combination of type and armour deletes one.</summary>
        public int DamageFloor => _values[(int)Field.DamageFloor];

        /// <summary>What the bank pays a wave, in percent, rounded up.</summary>
        public int InterestPercentPerWave => _values[(int)Field.InterestPercentPerWave];

        /// <summary>
        /// The most interest one wave may pay, in gold.
        /// <see cref="NoInterestCeiling"/> means there is none, and compounding
        /// is then bounded by the run's round cap alone -- which is why a run
        /// with no round cap and no ceiling here is refused. See
        /// <see cref="Purse.RequireBoundedCompounding"/>.
        /// </summary>
        public int InterestCapGold => _values[(int)Field.InterestCapGold];

        /// <summary>The flat income a wave pays, in gold, before any bonus.</summary>
        public int IncomeBasePerWave => _values[(int)Field.IncomeBasePerWave];

        /// <summary>
        /// What a run's purse opens holding, in gold. Nothing has been earned
        /// yet when the first build phase stands, so without this the opening
        /// round's only affordable wave is the empty one.
        /// </summary>
        public int StartingPurseGold => _values[(int)Field.StartingPurseGold];

        /// <summary>
        /// The performance bonus, as bands against the field's distribution.
        /// Ascending by threshold, starting at the zeroth percentile, and never
        /// negative.
        /// </summary>
        public IReadOnlyList<PerformanceBand> Bands => _bands;

        /// <summary>
        /// The band that pays the most: the last one, because the bands ascend
        /// by threshold and none of them pays less than the one below it. What
        /// a wave earns is at most this, whatever it did.
        /// </summary>
        public PerformanceBand BestBand => _bands[_bands.Length - 1];

        /// <summary>The health pool a run starts with, denominated in gold.</summary>
        public int HealthPoolGold => _values[(int)Field.HealthPoolGold];

        /// <summary>How many scouting snapshots a run gets before it starts paying.</summary>
        public int FreeSnapshotsPerRun => _values[(int)Field.FreeSnapshotsPerRun];

        /// <summary>What a snapshot costs in gold once the free ones are spent.</summary>
        public int SnapshotPriceGold => _values[(int)Field.SnapshotPriceGold];

        /// <summary>
        /// The content hash: a fold over every parsed integer, in field order.
        /// See the remarks on <see cref="Ruleset"/>.
        /// </summary>
        public Hash64 ContentHash { get; }

        /// <summary>Parses the ruleset from text. Not from a path -- see <see cref="DataText"/>.</summary>
        public static Ruleset Parse(string text) => Parse("ruleset", text);

        /// <summary>Parses the ruleset from UTF-8 bytes, which is what a caller that read a file holds.</summary>
        public static Ruleset ParseUtf8(byte[] utf8) => ParseUtf8("ruleset", utf8);

        /// <summary>Parses the ruleset, naming the content in any error message.</summary>
        public static Ruleset ParseUtf8(string source, byte[] utf8) =>
            Parse(source, DataText.FromUtf8(source, utf8));

        /// <summary>Parses the ruleset, naming the content in any error message.</summary>
        public static Ruleset Parse(string source, string text)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var draft = new Draft();

            foreach (DataText.Row row in DataText.Rows(source, text))
            {
                RuleFor(source, row).Read(source, row, draft);
            }

            draft.RequireEverything(source);

            return new Ruleset(draft);
        }

        /// <summary>
        /// These rules with the scouting line retuned: how many snapshots a run
        /// gets free and what one costs after that.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>These two are the sweep's economy dials, and this is the seam they
        /// turn on.</b> They decide what scouting is worth, which is a number
        /// the harness is meant to move rather than an argument somebody settled
        /// -- so a sweep retunes them here instead of every caller reaching for
        /// a second ruleset file.
        /// </para>
        /// <para>
        /// <b>The content hash moves with them.</b> It is a fold over the parsed
        /// integers in field order and these are two of those integers, so a
        /// retuned ruleset is loudly a different ruleset and a record stamped
        /// against the authored one will not replay against it.
        /// </para>
        /// <para>
        /// Every bound here is the one the parser applies to the same column,
        /// because a number that reaches the rules through this door has had no
        /// file to be refused at -- and it is the same bound rather than a
        /// matching one, read off the column in <see cref="Rules"/>.
        /// </para>
        /// </remarks>
        public Ruleset With(
            int freeSnapshotsPerRun,
            int snapshotPriceGold)
        {
            var values = (int[])_values.Clone();

            Retune(values, Field.FreeSnapshotsPerRun, freeSnapshotsPerRun);
            Retune(values, Field.SnapshotPriceGold, snapshotPriceGold);

            return new Ruleset(this, values);
        }

        /// <summary>
        /// The band a wave that reached this percentile of the field falls in:
        /// the last one whose threshold it reaches. Every band pays at least
        /// what the one below it pays and none of them is negative, so falling
        /// short of the field is a smaller bonus and never a penalty.
        /// </summary>
        /// <param name="percentile">
        /// How much of the field the wave beat, 0 to 100. A hundred is a wave
        /// nothing in the field matched, which is inside the top band rather
        /// than past it.
        /// </param>
        public PerformanceBand BandFor(int percentile)
        {
            if (percentile < 0 || percentile > Percent)
            {
                throw new SimulationException(
                    "A wave came in at the "
                    + percentile.ToString(CultureInfo.InvariantCulture)
                    + "th percentile of its field. A percentile is how much of the field the wave beat, "
                    + "so it runs from 0 to "
                    + Percent.ToString(CultureInfo.InvariantCulture)
                    + " and a value outside that is a count that was never divided by the field's size.");
            }

            PerformanceBand reached = _bands[0];

            for (int index = 1; index < _bands.Length; index++)
            {
                if (_bands[index].PercentileThreshold > percentile)
                {
                    break;
                }

                reached = _bands[index];
            }

            return reached;
        }

        /// <summary>
        /// The content hash: every parsed integer under the layout label, in the
        /// order <see cref="Rules"/> declares the rules that hold them. Computed
        /// from the fields rather than from whatever built them, so a parsed
        /// ruleset and a retuned one are hashed by one walk and cannot drift
        /// apart.
        /// </summary>
        private Hash64 Fold()
        {
            Hash64 hash = Hash64.Start(HashLabel);

            for (int index = 0; index < Rules.Length; index++)
            {
                hash = Rules[index].Fold(this, hash);
            }

            return hash;
        }

        /// <summary>The matrix, at the position in the hash its row is declared at.</summary>
        private static Hash64 FoldMatrix(Ruleset rules, Hash64 hash) => rules.Matrix.Fold(hash);

        /// <summary>
        /// The bands, counted and then folded in order. The count goes in
        /// because a ruleset with a band removed must not hash as the prefix of
        /// one that still has it.
        /// </summary>
        private static Hash64 FoldBands(Ruleset rules, Hash64 hash)
        {
            hash = hash.Add(rules._bands.Length);

            for (int index = 0; index < rules._bands.Length; index++)
            {
                hash = rules._bands[index].Fold(hash);
            }

            return hash;
        }

        /// <summary>
        /// A retuned number written into a copy of the values, refused where the
        /// authored column would have refused it.
        /// </summary>
        private static void Retune(int[] values, Field field, int value)
        {
            Column column = ColumnByField[(int)field];

            if (value < column.Minimum || value > column.Maximum)
            {
                throw new SimulationException(
                    "A ruleset was retuned with "
                    + value.ToString(CultureInfo.InvariantCulture)
                    + " for "
                    + column.Name
                    + ", which runs from "
                    + column.Minimum.ToString(CultureInfo.InvariantCulture)
                    + " to "
                    + column.Maximum.ToString(CultureInfo.InvariantCulture)
                    + ". A number handed in here has had no file to be refused at, so it is held to the "
                    + "range the authored column is held to -- otherwise a sweep is the one caller that "
                    + "can build a ruleset no text file could express.");
            }

            values[(int)field] = value;
        }

        /// <summary>
        /// The column that fills each field, indexed by it, gathered off
        /// <see cref="Rules"/> the first time a ruleset is built.
        /// </summary>
        private static Column[] IndexColumns()
        {
            var byField = new Column[(int)Field.Count];
            var filled = new bool[(int)Field.Count];

            for (int index = 0; index < Rules.Length; index++)
            {
                Column[] columns = Rules[index].Columns;

                for (int column = 0; column < columns.Length; column++)
                {
                    int field = (int)columns[column].Field;

                    if (filled[field])
                    {
                        throw new SimulationException(
                            Name(columns[column].Field)
                            + " is filled by two columns of the ruleset. One would overwrite the other and "
                            + "the fold would take it twice, leaving whichever field lost its column read "
                            + "as a zero nobody authored.");
                    }

                    byField[field] = columns[column];
                    filled[field] = true;
                }
            }

            for (int field = 0; field < filled.Length; field++)
            {
                if (!filled[field])
                {
                    throw new SimulationException(
                        Name((Field)field)
                        + " is filled by no column of any ruleset rule. Nothing would parse it, nothing "
                        + "would fold it into the content hash, and its accessor would answer zero -- so "
                        + "a field exists on the row that carries it or it does not exist.");
                }
            }

            return byField;
        }

        private static string Name(Field field) => "The ruleset field " + field.ToString();

        /// <summary>The rule a row's opening word names, or a refusal listing the words this file has.</summary>
        private static Rule RuleFor(string source, DataText.Row row)
        {
            for (int index = 0; index < Rules.Length; index++)
            {
                if (string.Equals(row.Keyword, Rules[index].Keyword, StringComparison.Ordinal))
                {
                    return Rules[index];
                }
            }

            throw DataText.NoSuchRow(source, row.Line, row.Keyword, RowWords());
        }

        /// <summary>The words a ruleset row may open with: the keywords of <see cref="Rules"/>.</summary>
        private static string[] RowWords()
        {
            var words = new string[Rules.Length];

            for (int index = 0; index < Rules.Length; index++)
            {
                words[index] = Rules[index].Keyword;
            }

            return words;
        }

        private static void ReadMatrixRow(string source, DataText.Row row, Draft draft) =>
            draft.AddMatrixRow(
                source,
                row.Line,
                DataText.Keyword(source, row.Line, "the attack type", row.Fields[1], DamageMatrix.AttackWords),
                Cell(source, row.Line, "the swift cell", row.Fields[2]),
                Cell(source, row.Line, "the armoured cell", row.Fields[3]),
                Cell(source, row.Line, "the arcane cell", row.Fields[4]));

        private static void ReadBandRow(string source, DataText.Row row, Draft draft) =>
            draft.AddBand(
                source,
                row.Line,
                DataText.IntegerInRange(source, row.Line, "the band's percentile", row.Fields[1], 0, 99),
                DataText.IntegerInRange(source, row.Line, "the band's bonus", row.Fields[2], 0, int.MaxValue));

        private static int Cell(string source, int line, string name, string field) =>
            DataText.IntegerInRange(source, line, name, field, 1, MaximumFactor);

        /// <summary>
        /// One number on a row: the field it fills, what a refusal calls it, and
        /// the range the authored column is held to.
        /// </summary>
        private readonly struct Column
        {
            internal Column(Field field, string name, int minimum, int maximum)
            {
                Field = field;
                Name = name;
                Minimum = minimum;
                Maximum = maximum;
            }

            internal Field Field { get; }

            /// <summary>What a refusal calls this number, as a noun phrase.</summary>
            internal string Name { get; }

            internal int Minimum { get; }

            internal int Maximum { get; }
        }

        /// <summary>
        /// One kind of row the ruleset file has: the word it opens with, how
        /// many fields it carries, and what reading one leaves behind.
        /// </summary>
        /// <remarks>
        /// A rule is either a run of numbered columns -- read into the draft and
        /// folded straight off the values, stated exactly once -- or row-shaped,
        /// carrying its own reader and its own fold and appearing as many times
        /// as the shape it describes has rows.
        /// </remarks>
        private sealed class Rule
        {
            private readonly Action<string, DataText.Row, Draft>? _read;

            private readonly Func<Ruleset, Hash64, Hash64>? _fold;

            private Rule(
                string keyword,
                int fields,
                Column[] columns,
                Action<string, DataText.Row, Draft>? read,
                Func<Ruleset, Hash64, Hash64>? fold)
            {
                Keyword = keyword;
                FieldsPerRow = fields;
                Columns = columns;
                _read = read;
                _fold = fold;
            }

            /// <summary>The word a row of this rule opens with.</summary>
            internal string Keyword { get; }

            /// <summary>How many whitespace-separated fields a row of it carries, keyword included.</summary>
            internal int FieldsPerRow { get; }

            /// <summary>The numbers that follow the keyword, in the order they are written.</summary>
            internal Column[] Columns { get; }

            /// <summary>
            /// True for the matrix and the bands: rules whose rows describe a
            /// shape rather than filling a column, so that a completeness check
            /// over them is about how many rows arrived.
            /// </summary>
            internal bool IsRowShaped => _read is not null;

            /// <summary>A rule whose row is a keyword followed by one number per column.</summary>
            internal static Rule Numbers(string keyword, params Column[] columns) =>
                new Rule(keyword, columns.Length + 1, columns, null, null);

            /// <summary>A rule whose rows describe a shape, read and folded by name.</summary>
            internal static Rule RowShaped(
                string keyword,
                int fields,
                Action<string, DataText.Row, Draft> read,
                Func<Ruleset, Hash64, Hash64> fold) =>
                new Rule(keyword, fields, Array.Empty<Column>(), read, fold);

            /// <summary>Reads one row of this rule into the draft.</summary>
            internal void Read(string source, DataText.Row row, Draft draft)
            {
                DataText.RequireFieldCount(source, row.Line, Keyword, FieldsPerRow, row.Fields);

                if (_read is not null)
                {
                    _read(source, row, draft);
                    return;
                }

                draft.Once(source, row.Line, Keyword);

                for (int index = 0; index < Columns.Length; index++)
                {
                    Column column = Columns[index];

                    draft.Values[(int)column.Field] = DataText.IntegerInRange(
                        source,
                        row.Line,
                        column.Name,
                        row.Fields[index + 1],
                        column.Minimum,
                        column.Maximum);
                }
            }

            /// <summary>Folds what this rule holds into the content hash.</summary>
            internal Hash64 Fold(Ruleset rules, Hash64 hash)
            {
                if (_fold is not null)
                {
                    return _fold(rules, hash);
                }

                for (int index = 0; index < Columns.Length; index++)
                {
                    hash = hash.Add(rules._values[(int)Columns[index].Field]);
                }

                return hash;
            }
        }

        /// <summary>
        /// The ruleset part-read: every field, plus which rows have been seen.
        /// A row is written here once and read out once, so a rule that was
        /// never authored is a missing entry rather than a zero.
        /// </summary>
        private sealed class Draft
        {
            private readonly List<string> _seen = new List<string>();

            private readonly int[] _cells = new int[DamageMatrix.CellCount];

            private int _matrixRows;

            internal DamageMatrix? Matrix { get; private set; }

            internal List<PerformanceBand> Bands { get; } = new List<PerformanceBand>();

            /// <summary>Every number a column filled, indexed by its field.</summary>
            internal int[] Values { get; } = new int[(int)Field.Count];

            /// <summary>Records a row that may appear exactly once.</summary>
            internal void Once(string source, int line, string keyword)
            {
                for (int index = 0; index < _seen.Count; index++)
                {
                    if (string.Equals(_seen[index], keyword, StringComparison.Ordinal))
                    {
                        throw new ContentException(
                            source,
                            line,
                            "is a second '"
                            + keyword
                            + "' row. Each rule is stated once: two rows claiming one rule means the "
                            + "ruleset in force is whichever of them was read last.");
                    }
                }

                _seen.Add(keyword);
            }

            /// <summary>
            /// Fills the next row of the matrix. Rows arrive in attack-type
            /// order, so a missing, repeated or reordered one is a comparison
            /// against the row above rather than a lookup nobody performed.
            /// </summary>
            internal void AddMatrixRow(string source, int line, int attack, int swift, int armoured, int arcane)
            {
                if (_matrixRows == DamageMatrix.AttackTypes)
                {
                    throw new ContentException(
                        source,
                        line,
                        "is a fourth 'matrix' row. The matrix has exactly "
                        + DamageMatrix.AttackTypes.ToString(CultureInfo.InvariantCulture)
                        + " rows, one per attack type.");
                }

                if (attack != _matrixRows)
                {
                    throw new ContentException(
                        source,
                        line,
                        "gives the matrix row for "
                        + DamageMatrix.AttackWords[attack]
                        + " where "
                        + DamageMatrix.AttackWords[_matrixRows]
                        + " was expected. The rows are authored in attack-type order, which is what makes "
                        + "a repeated or a missing one impossible to read past.");
                }

                _cells[(attack * DamageMatrix.ArmourTypes) + 0] = swift;
                _cells[(attack * DamageMatrix.ArmourTypes) + 1] = armoured;
                _cells[(attack * DamageMatrix.ArmourTypes) + 2] = arcane;
                _matrixRows++;

                if (_matrixRows == DamageMatrix.AttackTypes)
                {
                    RequireLatinSquare(source, line);
                    Matrix = new DamageMatrix(_cells);
                }
            }

            /// <summary>Adds a band, in ascending order and never paying less than the one below it.</summary>
            internal void AddBand(string source, int line, int threshold, int bonus)
            {
                if (Bands.Count == 0 && threshold != 0)
                {
                    throw new ContentException(
                        source,
                        line,
                        "opens the bands at the "
                        + threshold.ToString(CultureInfo.InvariantCulture)
                        + "th percentile. The first band starts at zero, so that every wave falls in one "
                        + "and the bonus is never undefined for a wave that did badly.");
                }

                if (Bands.Count > 0)
                {
                    PerformanceBand below = Bands[Bands.Count - 1];

                    if (threshold <= below.PercentileThreshold)
                    {
                        throw new ContentException(
                            source,
                            line,
                            "starts at the "
                            + threshold.ToString(CultureInfo.InvariantCulture)
                            + "th percentile, at or below the "
                            + below.PercentileThreshold.ToString(CultureInfo.InvariantCulture)
                            + "th above it. Bands ascend strictly down this file, which is what makes the "
                            + "band a wave falls in the last one it reaches.");
                    }

                    if (bonus < below.BonusPercentOfBase)
                    {
                        throw new ContentException(
                            source,
                            line,
                            "pays "
                            + bonus.ToString(CultureInfo.InvariantCulture)
                            + " where the band below it pays "
                            + below.BonusPercentOfBase.ToString(CultureInfo.InvariantCulture)
                            + ". The bands are progressive: doing better never pays less.");
                    }
                }

                Bands.Add(new PerformanceBand(threshold, bonus));
            }

            /// <summary>Every rule stated, or a refusal naming the first one that was not.</summary>
            internal void RequireEverything(string source)
            {
                if (_matrixRows != DamageMatrix.AttackTypes)
                {
                    throw new ContentException(
                        source,
                        0,
                        "has "
                        + _matrixRows.ToString(CultureInfo.InvariantCulture)
                        + " of the "
                        + DamageMatrix.AttackTypes.ToString(CultureInfo.InvariantCulture)
                        + " 'matrix' rows the damage matrix is made of.");
                }

                if (Bands.Count == 0)
                {
                    throw new ContentException(
                        source,
                        0,
                        "has no 'band' rows, so the performance bonus has no distribution to be measured "
                        + "against and every wave would be paid an amount nobody authored.");
                }

                for (int index = 0; index < Rules.Length; index++)
                {
                    Rule rule = Rules[index];

                    if (rule.IsRowShaped || Was(rule.Keyword))
                    {
                        continue;
                    }

                    throw new ContentException(
                        source,
                        0,
                        "has no '"
                        + rule.Keyword
                        + "' row. Every rule is authored here: a missing one would be supplied by this "
                        + "reader and folded into the content hash as though somebody had chosen it.");
                }
            }

            /// <summary>
            /// Every row and every column a permutation of the same three
            /// values, which is what makes no attack type globally better and no
            /// armour type globally tougher.
            /// </summary>
            private void RequireLatinSquare(string source, int line)
            {
                int[] expected = { _cells[0], _cells[1], _cells[2] };

                if (expected[0] == expected[1] || expected[1] == expected[2] || expected[0] == expected[2])
                {
                    throw new ContentException(
                        source,
                        line,
                        "has a first matrix row of "
                        + Describe(expected)
                        + ", which repeats a value. The three cells of a row are the three distinct "
                        + "outcomes type can produce, and a repeat means two armour types the attacker "
                        + "cannot tell apart.");
                }

                for (int attack = 0; attack < DamageMatrix.AttackTypes; attack++)
                {
                    int[] row =
                    {
                        _cells[(attack * DamageMatrix.ArmourTypes) + 0],
                        _cells[(attack * DamageMatrix.ArmourTypes) + 1],
                        _cells[(attack * DamageMatrix.ArmourTypes) + 2],
                    };

                    if (!IsPermutation(expected, row))
                    {
                        throw new ContentException(
                            source,
                            line,
                            "has a "
                            + DamageMatrix.AttackWords[attack]
                            + " row of "
                            + Describe(row)
                            + " where the matrix is built from "
                            + Describe(expected)
                            + ". Every row is a permutation of the same three cells, so that no attack "
                            + "type is globally better than another.");
                    }
                }

                for (int armour = 0; armour < DamageMatrix.ArmourTypes; armour++)
                {
                    int[] column =
                    {
                        _cells[(0 * DamageMatrix.ArmourTypes) + armour],
                        _cells[(1 * DamageMatrix.ArmourTypes) + armour],
                        _cells[(2 * DamageMatrix.ArmourTypes) + armour],
                    };

                    if (!IsPermutation(expected, column))
                    {
                        throw new ContentException(
                            source,
                            line,
                            "has a column of "
                            + Describe(column)
                            + " where the matrix is built from "
                            + Describe(expected)
                            + ". Every column is a permutation of the same three cells, so that no armour "
                            + "type is globally tougher than another.");
                    }
                }
            }

            private static bool IsPermutation(int[] expected, int[] actual)
            {
                var taken = new bool[expected.Length];

                for (int index = 0; index < actual.Length; index++)
                {
                    bool found = false;

                    for (int candidate = 0; candidate < expected.Length; candidate++)
                    {
                        if (!taken[candidate] && expected[candidate] == actual[index])
                        {
                            taken[candidate] = true;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return false;
                    }
                }

                return true;
            }

            private static string Describe(int[] values)
            {
                var text = new string[values.Length];

                for (int index = 0; index < values.Length; index++)
                {
                    text[index] = values[index].ToString(CultureInfo.InvariantCulture);
                }

                return "(" + string.Join(", ", text) + ")";
            }

            private bool Was(string keyword)
            {
                for (int index = 0; index < _seen.Count; index++)
                {
                    if (string.Equals(_seen[index], keyword, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
