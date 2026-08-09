using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A bound the sweep placed on its own coverage, and what it covered under
    /// it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Every axis reports, bounded or not.</b> A truncated sweep that said
    /// nothing would read exactly like a complete one -- same columns, same
    /// shape, fewer rows -- and nobody reading the second one would know to ask.
    /// So the answer is not "say so when truncated" but "always say what was
    /// covered", which makes completeness a value in the output rather than an
    /// absence of a warning.
    /// </para>
    /// <para>
    /// <see cref="Available"/> is <see cref="Unbounded"/> for an axis nothing
    /// enumerates -- the seed space is 2^64 wide, so a run count is a sample
    /// however large it is, and a sample is bounded by construction.
    /// </para>
    /// </remarks>
    public sealed class CoverageBound
    {
        /// <summary>The <see cref="Available"/> of an axis whose population nothing enumerates.</summary>
        public const int Unbounded = 0;

        internal CoverageBound(string axis, int covered, int available)
        {
            Axis = axis;
            Covered = covered;
            Available = available;
        }

        /// <summary>What was bounded: the roster the rows are scored over, or the seeds each was played on.</summary>
        public string Axis { get; }

        /// <summary>How much of it this sweep covered.</summary>
        public int Covered { get; }

        /// <summary>How much there was, or <see cref="Unbounded"/> where that is not a number.</summary>
        public int Available { get; }

        /// <summary>Whether anything was left out. A sample always leaves something out.</summary>
        public bool IsBounded => Available == Unbounded || Covered < Available;

        public override string ToString() =>
            Axis
            + ": "
            + Covered.ToString(CultureInfo.InvariantCulture)
            + (Available == Unbounded
                ? " sampled"
                : " of " + Available.ToString(CultureInfo.InvariantCulture))
            + (IsBounded ? ", bounded" : ", complete");
    }

    /// <summary>
    /// One cell of the sweep: what a creep did over a population of runs, either
    /// over all of them or over the ones that ended holding a given number of
    /// ingredients.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A row is a creep and a bin, and nothing here is a rate the caller has
    /// to trust.</b> Every ratio arrives beside the two integers it was computed
    /// from, so a spreadsheet can recompute it exactly and nobody is stuck with
    /// this type's truncation.
    /// </para>
    /// <para>
    /// <b><see cref="Ingredients"/> is the bin</b>, and
    /// <see cref="AllIngredients"/> is the row over every run of the creep. A
    /// run's ingredient count is how many distinct creeps it ended the run able
    /// to field -- a build phase takes one option, so it runs from one to the
    /// rounds the run resolved and lands where the offering's churn and the
    /// plan's <see cref="SweepPlan.Policy"/> put it between them. Win rate down
    /// that axis is the U-shape a meta goes wrong in: focused builds and greedy
    /// builds winning while everything between them loses.
    /// </para>
    /// </remarks>
    public sealed class SweepRow
    {
        /// <summary>The <see cref="Ingredients"/> of the row over every run of a creep.</summary>
        public const int AllIngredients = 0;

        internal SweepRow(
            int typeId,
            string label,
            int ingredients,
            int runs,
            int rounds,
            int wins,
            int winRateBasisPoints,
            long leakCostDealt,
            long leakCostTaken,
            long goldSpent,
            int dealtPerHundredGold,
            long incomeBaseGold,
            long bonusGold)
        {
            TypeId = typeId;
            Label = label;
            Ingredients = ingredients;
            Runs = runs;
            Rounds = rounds;
            Wins = wins;
            WinRateBasisPoints = winRateBasisPoints;
            LeakCostDealt = leakCostDealt;
            LeakCostTaken = leakCostTaken;
            GoldSpent = goldSpent;
            DealtPerHundredGold = dealtPerHundredGold;
            IncomeBaseGold = incomeBaseGold;
            BonusGold = bonusGold;
        }

        /// <summary>Which creep this row's runs favoured.</summary>
        public int TypeId { get; }

        /// <summary>That creep's label, for a person reading a spreadsheet.</summary>
        public string Label { get; }

        /// <summary>The bin, or <see cref="AllIngredients"/> for the creep's whole population.</summary>
        public int Ingredients { get; }

        /// <summary>How many runs fell in this row.</summary>
        public int Runs { get; }

        /// <summary>
        /// How many rounds those runs resolved between them.
        /// </summary>
        /// <remarks>
        /// <see cref="Runs"/> times N where death does not end a run, and less
        /// than that where it does. A row is comparable with the row beside it
        /// only if both were played over the same number of rounds, so this is
        /// what says whether a short row is a weak creep or a dead one.
        /// </remarks>
        public int Rounds { get; }

        /// <summary>How many of them won. See <see cref="Sweep"/> for what winning is.</summary>
        public int Wins { get; }

        /// <summary>
        /// Wins over runs in basis points -- ten thousand is every run -- so a
        /// percentage is this divided by a hundred. Truncated.
        /// </summary>
        public int WinRateBasisPoints { get; }

        /// <summary>What these runs' waves got past the field, in gold, summed.</summary>
        public long LeakCostDealt { get; }

        /// <summary>What the field's waves got past these runs, in gold, summed.</summary>
        public long LeakCostTaken { get; }

        /// <summary>What these runs bought creeps with, in gold, summed.</summary>
        public long GoldSpent { get; }

        /// <summary>
        /// <see cref="LeakCostDealt"/> per hundred gold spent, truncated. Read
        /// the remarks on <see cref="Sweep"/> before reading this as a price.
        /// </summary>
        public int DealtPerHundredGold { get; }

        /// <summary>What these runs' waves were paid for happening, in gold, summed.</summary>
        public long IncomeBaseGold { get; }

        /// <summary>
        /// What these runs' waves were paid for how they did, in gold, summed.
        /// </summary>
        /// <remarks>
        /// The performance bonus, beside the base it is a share of, so the two
        /// integers say what attacking earned its sender and what it would have
        /// earned by turning up. A row where this is nothing is a creep whose
        /// runs never cleared the bottom band; a row where it is missing across
        /// the whole report is an economy paying the base alone.
        /// </remarks>
        public long BonusGold { get; }

        public override string ToString() =>
            Label
            + (Ingredients == AllIngredients
                ? " over "
                : " at " + Ingredients.ToString(CultureInfo.InvariantCulture) + " ingredients over ")
            + Runs.ToString(CultureInfo.InvariantCulture)
            + " runs: "
            + WinRateBasisPoints.ToString(CultureInfo.InvariantCulture)
            + "bp won, "
            + DealtPerHundredGold.ToString(CultureInfo.InvariantCulture)
            + " dealt per 100 gold";
    }

    /// <summary>What one sweep came to: the rows, and how far it reached.</summary>
    public sealed class SweepReport
    {
        private readonly SweepRow[] _rows;

        private readonly CoverageBound[] _coverage;

        internal SweepReport(SweepPlan plan, SweepRow[] rows, CoverageBound[] coverage)
        {
            Plan = plan;
            _rows = rows;
            _coverage = coverage;
        }

        /// <summary>What this sweep was played under. Every number of it belongs in whatever writes the rows down.</summary>
        public SweepPlan Plan { get; }

        /// <summary>The rows, in roster order, each creep's whole population first and then its bins ascending.</summary>
        public IReadOnlyList<SweepRow> Rows => _rows;

        /// <summary>Every axis this sweep could have bounded, and what it covered on each.</summary>
        public IReadOnlyList<CoverageBound> Coverage => _coverage;

        /// <summary>How many runs went into the whole report.</summary>
        public int Runs => Plan.Creeps.Count * Plan.RunsPerCreep;

        public override string ToString() =>
            _rows.Length.ToString(CultureInfo.InvariantCulture)
            + " rows over "
            + Runs.ToString(CultureInfo.InvariantCulture)
            + " runs";
    }
}
