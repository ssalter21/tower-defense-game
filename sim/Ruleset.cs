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

        private readonly PerformanceBand[] _bands;

        private Ruleset(Draft draft)
        {
            Matrix = draft.Matrix!;
            ArmourPercentPerPoint = draft.ArmourPercentPerPoint;
            ArmourDenominator = draft.ArmourDenominator;
            DamageFloor = draft.DamageFloor;
            InterestPercentPerWave = draft.InterestPercentPerWave;
            InterestCapGold = draft.InterestCapGold;
            IncomeBasePerWave = draft.IncomeBasePerWave;
            StartingPurseGold = draft.StartingPurseGold;
            _bands = draft.Bands.ToArray();
            HealthPoolGold = draft.HealthPoolGold;
            StartingWaveSlots = draft.StartingWaveSlots;
            WaveSlotsPerAnchor = draft.WaveSlotsPerAnchor;
            OrdinaryOptionsPerRound = draft.OrdinaryOptionsPerRound;
            GameChangersPerAnchor = draft.GameChangersPerAnchor;
            FreeSnapshotsPerRun = draft.FreeSnapshotsPerRun;
            SnapshotPriceGold = draft.SnapshotPriceGold;
            ContentHash = Fold();
        }

        /// <summary>
        /// The same rules with four of their numbers replaced. Every other
        /// field is carried across by reference to the same values, so the two
        /// rulesets differ in exactly what was asked for.
        /// </summary>
        private Ruleset(
            Ruleset original,
            int ordinaryOptionsPerRound,
            int gameChangersPerAnchor,
            int freeSnapshotsPerRun,
            int snapshotPriceGold)
        {
            Matrix = original.Matrix;
            ArmourPercentPerPoint = original.ArmourPercentPerPoint;
            ArmourDenominator = original.ArmourDenominator;
            DamageFloor = original.DamageFloor;
            InterestPercentPerWave = original.InterestPercentPerWave;
            InterestCapGold = original.InterestCapGold;
            IncomeBasePerWave = original.IncomeBasePerWave;
            StartingPurseGold = original.StartingPurseGold;
            _bands = original._bands;
            HealthPoolGold = original.HealthPoolGold;
            StartingWaveSlots = original.StartingWaveSlots;
            WaveSlotsPerAnchor = original.WaveSlotsPerAnchor;
            OrdinaryOptionsPerRound = ordinaryOptionsPerRound;
            GameChangersPerAnchor = gameChangersPerAnchor;
            FreeSnapshotsPerRun = freeSnapshotsPerRun;
            SnapshotPriceGold = snapshotPriceGold;
            ContentHash = Fold();
        }

        /// <summary>Three attack types against three armour types, as nine percentages.</summary>
        public DamageMatrix Matrix { get; }

        /// <summary>
        /// How much of a target's base effective health one point of armour
        /// adds, in percent. The coefficient in the armour expression.
        /// </summary>
        public int ArmourPercentPerPoint { get; }

        /// <summary>
        /// What the armour expression divides by at zero armour. A cell is a
        /// percentage of this, so a hundred here makes a cell of 100 the
        /// identity.
        /// </summary>
        public int ArmourDenominator { get; }

        /// <summary>The least a hit may deal. No combination of type and armour deletes one.</summary>
        public int DamageFloor { get; }

        /// <summary>What the bank pays a wave, in percent, rounded up.</summary>
        public int InterestPercentPerWave { get; }

        /// <summary>
        /// The most interest one wave may pay, in gold.
        /// <see cref="NoInterestCeiling"/> means there is none, and compounding
        /// is then bounded by the run's round cap alone -- which is why a run
        /// with no round cap and no ceiling here is refused. See
        /// <see cref="Purse.RequireBoundedCompounding"/>.
        /// </summary>
        public int InterestCapGold { get; }

        /// <summary>The flat income a wave pays, in gold, before any bonus.</summary>
        public int IncomeBasePerWave { get; }

        /// <summary>
        /// What a run's purse opens holding, in gold. Nothing has been earned
        /// yet when the first build phase stands, so without this the opening
        /// round's only affordable wave is the empty one.
        /// </summary>
        public int StartingPurseGold { get; }

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
        public int HealthPoolGold { get; }

        /// <summary>How many wave slots the first round has.</summary>
        public int StartingWaveSlots { get; }

        /// <summary>
        /// How many slots an anchor adds. Slot width is derived rather than
        /// authored: a round's width is this many per anchor at or before it,
        /// on top of <see cref="StartingWaveSlots"/>.
        /// </summary>
        public int WaveSlotsPerAnchor { get; }

        /// <summary>How many ordinary options the offering carries each round.</summary>
        public int OrdinaryOptionsPerRound { get; }

        /// <summary>How many game changers join the offering on an anchor round.</summary>
        public int GameChangersPerAnchor { get; }

        /// <summary>How many scouting snapshots a run gets before it starts paying.</summary>
        public int FreeSnapshotsPerRun { get; }

        /// <summary>What a snapshot costs in gold once the free ones are spent.</summary>
        public int SnapshotPriceGold { get; }

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

            string[] lines = DataText.SplitLines(text);
            var draft = new Draft();

            for (int index = 0; index < lines.Length; index++)
            {
                string line = lines[index];
                int number = index + 1;

                if (DataText.IsBlankOrComment(line))
                {
                    continue;
                }

                ReadRow(source, number, DataText.Fields(source, number, line), draft);
            }

            draft.RequireEverything(source);

            return new Ruleset(draft);
        }

        /// <summary>
        /// These rules with the offering ratio and the scouting line retuned:
        /// how many ordinary options a round carries, how many game changers an
        /// anchor merges in, how many snapshots a run gets free and what one
        /// costs after that.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>These four are the sweep's economy dials, and this is the seam
        /// they turn on.</b> They decide whether a merged anchor menu is a real
        /// trade and what scouting is worth, and both are numbers the harness is
        /// meant to move rather than arguments somebody settled -- so a sweep
        /// retunes them here instead of every caller reaching for a second
        /// ruleset file.
        /// </para>
        /// <para>
        /// <b>The content hash moves with them.</b> It is a fold over the parsed
        /// integers in field order and these are four of those integers, so a
        /// retuned ruleset is loudly a different ruleset and a record stamped
        /// against the authored one will not replay against it.
        /// </para>
        /// <para>
        /// Every bound here is the one the parser applies to the same column,
        /// because a number that reaches the rules through this door has had no
        /// file to be refused at.
        /// </para>
        /// </remarks>
        public Ruleset With(
            int ordinaryOptionsPerRound,
            int gameChangersPerAnchor,
            int freeSnapshotsPerRun,
            int snapshotPriceGold)
        {
            RequireInRange(ordinaryOptionsPerRound, "the ordinary options", 1, MostOptions);
            RequireInRange(gameChangersPerAnchor, "the game changers an anchor adds", 1, MostOptions);
            RequireInRange(freeSnapshotsPerRun, "the free snapshot count", 0, int.MaxValue);
            RequireInRange(snapshotPriceGold, "the snapshot price", 0, int.MaxValue);

            return new Ruleset(
                this,
                ordinaryOptionsPerRound,
                gameChangersPerAnchor,
                freeSnapshotsPerRun,
                snapshotPriceGold);
        }

        /// <summary>
        /// How many wave slots a round offers, given how many anchors fall at or
        /// before it. Derived from the starting width and the widening step
        /// rather than authored as a second series, so moving an anchor cannot
        /// leave the two out of step.
        /// </summary>
        public int WaveSlotsAt(int anchorsSoFar)
        {
            if (anchorsSoFar < 0)
            {
                throw new SimulationException(
                    "A round cannot have passed "
                    + anchorsSoFar.ToString(CultureInfo.InvariantCulture)
                    + " anchors. Slot width is the starting width plus the widening step once per anchor, "
                    + "and a negative count would narrow it below where a run starts.");
            }

            return StartingWaveSlots + (WaveSlotsPerAnchor * anchorsSoFar);
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
        /// The content hash: every parsed integer, in field order, under the
        /// layout label. Computed from the fields rather than from whatever
        /// built them, so a parsed ruleset and a retuned one are hashed by one
        /// walk and cannot drift apart.
        /// </summary>
        private Hash64 Fold()
        {
            Hash64 hash = Matrix.Fold(Hash64.Start(HashLabel))
                .Add(ArmourPercentPerPoint)
                .Add(ArmourDenominator)
                .Add(DamageFloor)
                .Add(InterestPercentPerWave)
                .Add(InterestCapGold)
                .Add(IncomeBasePerWave)
                .Add(StartingPurseGold)
                .Add(_bands.Length);

            for (int index = 0; index < _bands.Length; index++)
            {
                hash = _bands[index].Fold(hash);
            }

            return hash
                .Add(HealthPoolGold)
                .Add(StartingWaveSlots)
                .Add(WaveSlotsPerAnchor)
                .Add(OrdinaryOptionsPerRound)
                .Add(GameChangersPerAnchor)
                .Add(FreeSnapshotsPerRun)
                .Add(SnapshotPriceGold);
        }

        /// <summary>A retuned number, refused where the authored column would have refused it.</summary>
        private static void RequireInRange(int value, string what, int minimum, int maximum)
        {
            if (value >= minimum && value <= maximum)
            {
                return;
            }

            throw new SimulationException(
                "A ruleset was retuned with "
                + value.ToString(CultureInfo.InvariantCulture)
                + " for "
                + what
                + ", which runs from "
                + minimum.ToString(CultureInfo.InvariantCulture)
                + " to "
                + maximum.ToString(CultureInfo.InvariantCulture)
                + ". A number handed in here has had no file to be refused at, so it is held to the range "
                + "the authored column is held to -- otherwise a sweep is the one caller that can build a "
                + "ruleset no text file could express.");
        }

        private static void ReadRow(string source, int line, string[] fields, Draft draft)
        {
            switch (fields[0])
            {
                case "matrix":
                    Expect(source, line, fields, "matrix", 5);
                    draft.AddMatrixRow(
                        source,
                        line,
                        DataText.Keyword(source, line, "the attack type", fields[1], DamageMatrix.AttackWords),
                        Cell(source, line, "the swift cell", fields[2]),
                        Cell(source, line, "the armoured cell", fields[3]),
                        Cell(source, line, "the arcane cell", fields[4]));
                    return;

                case "armour":
                    Expect(source, line, fields, "armour", 3);
                    draft.Once(source, line, "armour");
                    draft.ArmourPercentPerPoint =
                        DataText.IntegerInRange(source, line, "the armour coefficient", fields[1], 0, 1000);
                    draft.ArmourDenominator =
                        DataText.IntegerInRange(source, line, "the armour denominator", fields[2], 1, MaximumFactor);
                    return;

                case "floor":
                    Expect(source, line, fields, "floor", 2);
                    draft.Once(source, line, "floor");
                    draft.DamageFloor =
                        DataText.IntegerInRange(source, line, "the damage floor", fields[1], 1, MaximumFactor);
                    return;

                case "interest":
                    Expect(source, line, fields, "interest", 3);
                    draft.Once(source, line, "interest");
                    draft.InterestPercentPerWave =
                        DataText.IntegerInRange(source, line, "the interest rate", fields[1], 0, 1000);
                    draft.InterestCapGold =
                        DataText.IntegerInRange(source, line, "the interest cap", fields[2], 0, int.MaxValue);
                    return;

                case "income":
                    Expect(source, line, fields, "income", 2);
                    draft.Once(source, line, "income");
                    draft.IncomeBasePerWave =
                        DataText.IntegerInRange(source, line, "the income base", fields[1], 0, int.MaxValue);
                    return;

                case "purse":
                    Expect(source, line, fields, "purse", 2);
                    draft.Once(source, line, "purse");
                    draft.StartingPurseGold =
                        DataText.IntegerInRange(source, line, "the starting purse", fields[1], 0, int.MaxValue);
                    return;

                case "band":
                    Expect(source, line, fields, "band", 3);
                    draft.AddBand(
                        source,
                        line,
                        DataText.IntegerInRange(source, line, "the band's percentile", fields[1], 0, 99),
                        DataText.IntegerInRange(source, line, "the band's bonus", fields[2], 0, int.MaxValue));
                    return;

                case "health":
                    Expect(source, line, fields, "health", 2);
                    draft.Once(source, line, "health");
                    draft.HealthPoolGold =
                        DataText.IntegerInRange(source, line, "the health pool", fields[1], 1, int.MaxValue);
                    return;

                case "slots":
                    Expect(source, line, fields, "slots", 3);
                    draft.Once(source, line, "slots");
                    draft.StartingWaveSlots =
                        DataText.IntegerInRange(source, line, "the starting slot width", fields[1], 1, 64);
                    draft.WaveSlotsPerAnchor =
                        DataText.IntegerInRange(source, line, "the slots an anchor adds", fields[2], 0, 64);
                    return;

                case "offering":
                    Expect(source, line, fields, "offering", 3);
                    draft.Once(source, line, "offering");
                    draft.OrdinaryOptionsPerRound = DataText.IntegerInRange(
                        source, line, "the ordinary options", fields[1], 1, MostOptions);
                    draft.GameChangersPerAnchor = DataText.IntegerInRange(
                        source, line, "the game changers an anchor adds", fields[2], 1, MostOptions);
                    return;

                case "snapshot":
                    Expect(source, line, fields, "snapshot", 3);
                    draft.Once(source, line, "snapshot");
                    draft.FreeSnapshotsPerRun =
                        DataText.IntegerInRange(source, line, "the free snapshot count", fields[1], 0, int.MaxValue);
                    draft.SnapshotPriceGold =
                        DataText.IntegerInRange(source, line, "the snapshot price", fields[2], 0, int.MaxValue);
                    return;

                default:
                    throw new ContentException(
                        source,
                        line,
                        "starts with '"
                        + fields[0]
                        + "', which is not one of the rows this ruleset has: "
                        + string.Join(", ", Draft.EveryKeyword)
                        + ". An unrecognised row is refused rather than skipped, because a rule nobody "
                        + "read is a rule the defaults quietly supplied.");
            }
        }

        private static int Cell(string source, int line, string name, string field) =>
            DataText.IntegerInRange(source, line, name, field, 1, MaximumFactor);

        private static void Expect(string source, int line, string[] fields, string keyword, int count) =>
            DataText.RequireFieldCount(source, line, keyword, count, fields);

        /// <summary>
        /// The ruleset part-read: every field, plus which rows have been seen.
        /// A row is written here once and read out once, so a rule that was
        /// never authored is a missing entry rather than a zero.
        /// </summary>
        private sealed class Draft
        {
            internal static readonly string[] EveryKeyword =
            {
                "matrix", "armour", "floor", "interest", "income", "purse", "band", "health", "slots",
                "offering", "snapshot",
            };

            private readonly List<string> _seen = new List<string>();

            private readonly int[] _cells = new int[DamageMatrix.CellCount];

            private int _matrixRows;

            internal DamageMatrix? Matrix { get; private set; }

            internal List<PerformanceBand> Bands { get; } = new List<PerformanceBand>();

            internal int ArmourPercentPerPoint { get; set; }

            internal int ArmourDenominator { get; set; }

            internal int DamageFloor { get; set; }

            internal int InterestPercentPerWave { get; set; }

            internal int InterestCapGold { get; set; }

            internal int IncomeBasePerWave { get; set; }

            internal int StartingPurseGold { get; set; }

            internal int HealthPoolGold { get; set; }

            internal int StartingWaveSlots { get; set; }

            internal int WaveSlotsPerAnchor { get; set; }

            internal int OrdinaryOptionsPerRound { get; set; }

            internal int GameChangersPerAnchor { get; set; }

            internal int FreeSnapshotsPerRun { get; set; }

            internal int SnapshotPriceGold { get; set; }

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

                foreach (string keyword in EveryKeyword)
                {
                    if (string.Equals(keyword, "matrix", StringComparison.Ordinal)
                        || string.Equals(keyword, "band", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (!Was(keyword))
                    {
                        throw new ContentException(
                            source,
                            0,
                            "has no '"
                            + keyword
                            + "' row. Every rule is authored here: a missing one would be supplied by this "
                            + "reader and folded into the content hash as though somebody had chosen it.");
                    }
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
