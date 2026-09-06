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
    /// One cell of the sweep: what a creep did over a population of runs.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A row is a creep, and nothing here is a rate the caller has to
    /// trust.</b> Every ratio arrives beside the two integers it was computed
    /// from, so a spreadsheet can recompute it exactly and nobody is stuck with
    /// this type's truncation.
    /// </para>
    /// <para>
    /// <b>There used to be a second axis, and it went with the take gate.</b>
    /// Rows were also binned by a run's "ingredients" -- how many distinct
    /// creeps it ended able to field -- and win rate down that axis was where a
    /// meta going wrong would show as a U-shape: focused builds and greedy
    /// builds winning while everything between them lost. That count varied only
    /// because the gate rationed what a run could send. With the gate deleted
    /// every run can send the whole roster from wave one, so the axis is one
    /// value wide and separates nothing. Whatever replaces it will be measuring
    /// a different thing and wants naming as such rather than inheriting this
    /// column.
    /// </para>
    /// </remarks>
    public sealed class SweepRow
    {
        internal SweepRow(
            int typeId,
            string label,
            string wall,
            int runs,
            int rounds,
            int wins,
            int winRateBasisPoints,
            long leakCostDealt,
            long leakCostTaken,
            long goldSpent,
            long defenseGold,
            long unspentGold,
            int dealtPerHundredGold,
            long incomeBaseGold,
            long bonusGold)
        {
            TypeId = typeId;
            Label = label;
            Wall = wall;
            Runs = runs;
            Rounds = rounds;
            Wins = wins;
            WinRateBasisPoints = winRateBasisPoints;
            LeakCostDealt = leakCostDealt;
            LeakCostTaken = leakCostTaken;
            GoldSpent = goldSpent;
            DefenseGold = defenseGold;
            UnspentGold = unspentGold;
            DealtPerHundredGold = dealtPerHundredGold;
            IncomeBaseGold = incomeBaseGold;
            BonusGold = bonusGold;
        }

        /// <summary>Which creep this row's runs favoured.</summary>
        public int TypeId { get; }

        /// <summary>That creep's label, for a person reading a spreadsheet.</summary>
        public string Label { get; }

        /// <summary>
        /// What the opponents' towers in this row's runs were made of.
        /// </summary>
        /// <remarks>
        /// <b>Two rows are comparable when this matches and not otherwise.</b>
        /// A creep meets every wall the sweep carries, so the same label appears
        /// once per wall with different numbers under it -- and reading one of
        /// them as "what this creep does" is the mistake the column exists to
        /// make impossible. See <see cref="SweepWall"/>.
        /// </remarks>
        public string Wall { get; }

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
        /// What these runs put on the board, in gold, summed.
        /// </summary>
        /// <remarks>
        /// Beside <see cref="GoldSpent"/> rather than inside it, because that
        /// one is what <see cref="DealtPerHundredGold"/> is per: folding the
        /// towers into it would move what the ratio means without moving what it
        /// is called, and every row already written down would stop being
        /// comparable with its own history.
        /// </remarks>
        public long DefenseGold { get; }

        /// <summary>
        /// What these runs were still holding when they ended, in gold, summed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Every run of the row contributes, the ones that died included: what
        /// was banked and never spent is a fact about the player, and counting
        /// it off the survivors alone would read the banking rule off the
        /// population that banked well enough to survive.
        /// </para>
        /// <para>
        /// It is the purse a run's last round closed on, so the money that
        /// round's wave was paid is in it -- the run stopped after the payment
        /// rather than before it, and a number that took the payment back out
        /// would be describing a moment no run was ever in.
        /// </para>
        /// </remarks>
        public long UnspentGold { get; }

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
        /// The performance bonus, beside the flat base, so the two integers say
        /// what attacking earned its sender and what it would have earned by
        /// turning up. A row where this is nothing is a creep whose runs never
        /// got anything past; a row where it is missing across the whole report
        /// is an economy paying the base alone.
        /// </remarks>
        public long BonusGold { get; }

        public override string ToString() =>
            Label
            + " against "
            + Wall
            + " over "
            + Runs.ToString(CultureInfo.InvariantCulture)
            + " runs: "
            + WinRateBasisPoints.ToString(CultureInfo.InvariantCulture)
            + "bp won, "
            + DealtPerHundredGold.ToString(CultureInfo.InvariantCulture)
            + " dealt per 100 gold";
    }

    /// <summary>
    /// One run of the sweep, unfolded: what a single play of a single seed came
    /// to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The columns are the folded row's, one population deep.</b> Every
    /// number here is what <see cref="SweepRow"/> sums, so a creep's rows add
    /// up to its fold exactly and a spreadsheet grouping them itself lands on
    /// the harness's own number. What is missing is the two ratios: a rate over
    /// one run is a bit rather than a rate, and both operands of it are here to
    /// be divided by whoever wants one.
    /// </para>
    /// <para>
    /// <b>The seed is what makes a row actionable.</b> A fold says a creep lost
    /// three of six; this says which three, and the seed beside each is the
    /// argument that plays that exact run again through any verb that takes
    /// one. Without it an outlier is a number nobody can get back to.
    /// </para>
    /// </remarks>
    public sealed class SweepRunRow
    {
        internal SweepRunRow(
            int typeId,
            string label,
            string wall,
            ulong seed,
            int rounds,
            bool won,
            long leakCostDealt,
            long leakCostTaken,
            long goldSpent,
            long defenseGold,
            long unspentGold,
            long incomeBaseGold,
            long bonusGold)
        {
            TypeId = typeId;
            Label = label;
            Wall = wall;
            Seed = seed;
            Rounds = rounds;
            Won = won;
            LeakCostDealt = leakCostDealt;
            LeakCostTaken = leakCostTaken;
            GoldSpent = goldSpent;
            DefenseGold = defenseGold;
            UnspentGold = unspentGold;
            IncomeBaseGold = incomeBaseGold;
            BonusGold = bonusGold;
        }

        /// <summary>Which creep this run favoured.</summary>
        public int TypeId { get; }

        /// <summary>That creep's label, for a person reading a spreadsheet.</summary>
        public string Label { get; }

        /// <summary>What the opponents' towers in this run were made of.</summary>
        public string Wall { get; }

        /// <summary>The seed this run was played on.</summary>
        public ulong Seed { get; }

        /// <summary>How many rounds it resolved before it ended.</summary>
        public int Rounds { get; }

        /// <summary>Whether it won. See <see cref="Sweep"/> for what winning is.</summary>
        public bool Won { get; }

        /// <summary>What its waves got past the field, in gold.</summary>
        public long LeakCostDealt { get; }

        /// <summary>What the field's waves got past it, in gold.</summary>
        public long LeakCostTaken { get; }

        /// <summary>What it bought creeps with, in gold.</summary>
        public long GoldSpent { get; }

        /// <summary>What it put on the board, in gold.</summary>
        public long DefenseGold { get; }

        /// <summary>What it was still holding when it ended, in gold.</summary>
        public long UnspentGold { get; }

        /// <summary>What its waves were paid for happening, in gold.</summary>
        public long IncomeBaseGold { get; }

        /// <summary>What its waves were paid for how they did, in gold.</summary>
        public long BonusGold { get; }

        public override string ToString() =>
            Label
            + " against "
            + Wall
            + " on seed "
            + Seed.ToString(CultureInfo.InvariantCulture)
            + ": "
            + (Won ? "won" : "lost")
            + " over "
            + Rounds.ToString(CultureInfo.InvariantCulture)
            + " rounds";
    }

    /// <summary>What one sweep came to: the rows, and how far it reached.</summary>
    public sealed class SweepReport
    {
        private readonly SweepRow[] _rows;

        private readonly SweepRunRow[] _everyRun;

        private readonly CoverageBound[] _coverage;

        internal SweepReport(
            SweepPlan plan,
            SweepRow[] rows,
            SweepRunRow[] everyRun,
            CoverageBound[] coverage)
        {
            Plan = plan;
            _everyRun = everyRun;
            _rows = rows;
            _coverage = coverage;
        }

        /// <summary>What this sweep was played under. Every number of it belongs in whatever writes the rows down.</summary>
        public SweepPlan Plan { get; }

        /// <summary>The rows, in roster order, each creep's whole population first and then its bins ascending.</summary>
        public IReadOnlyList<SweepRow> Rows => _rows;

        /// <summary>
        /// Every run behind those rows, in the order they were played, or
        /// nothing where the plan did not ask for them.
        /// </summary>
        /// <remarks>
        /// Empty is what a plan with <see cref="SweepPlan.KeepsEveryRun"/> off
        /// comes to, and it is not the same as a sweep that played nothing:
        /// <see cref="Rows"/> is what says how many runs there were either way.
        /// </remarks>
        public IReadOnlyList<SweepRunRow> EveryRun => _everyRun;

        /// <summary>Every axis this sweep could have bounded, and what it covered on each.</summary>
        public IReadOnlyList<CoverageBound> Coverage => _coverage;

        /// <summary>How many runs went into the whole report.</summary>
        /// <remarks>
        /// Every creep meets every wall over the whole sample, so the count is
        /// the product of all three axes rather than of the two it was before
        /// the wall became one.
        /// </remarks>
        public int Runs => Plan.Walls.Count * Plan.Creeps.Count * Plan.RunsPerCreep;

        public override string ToString() =>
            _rows.Length.ToString(CultureInfo.InvariantCulture)
            + " rows over "
            + Runs.ToString(CultureInfo.InvariantCulture)
            + " runs";
    }
}
