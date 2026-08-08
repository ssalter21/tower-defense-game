using System;
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
    /// to field -- one take a round, so it runs from one to N and lands where
    /// the offering's churn put it. Win rate down that axis is the U-shape a
    /// meta goes wrong in: focused builds and greedy builds winning while
    /// everything between them loses.
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

    /// <summary>
    /// Everything one sweep is played under: the content it is pointed at, the
    /// shape of the runs, the economy's dials, and how far it is allowed to
    /// reach.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The map, the ruleset and the schedule are parameters, and so is
    /// everything else.</b> Pointing this at another map to score it, at another
    /// matrix, at a wider field or at a different offering ratio is an argument
    /// here -- which is the one thing that stops a sweep being a retrofit across
    /// every call site later.
    /// </para>
    /// <para>
    /// <b>The offering ratio and the scouting line arrive as numbers rather than
    /// as a second ruleset file.</b> They are folded into the rules through
    /// <see cref="Ruleset.With"/>, so the ruleset a sweep plays is a real
    /// ruleset with a real content hash and not a set of overrides carried
    /// alongside one.
    /// </para>
    /// <para>
    /// <b>Death ends a run here by default, exactly as it does everywhere
    /// else</b>, and a harness that wants otherwise asks. One knob with one
    /// default is the point: a sweep that quietly played a different game from
    /// the one the same content plays through <c>play-run</c> would be a report
    /// about a rule nobody chose. What no-death mode buys is a round of data for
    /// every wave rather than a short row wherever a build failed, which is why
    /// the flag exists -- and how many rounds a row actually got is on the row,
    /// so neither answer can be mistaken for the other.
    /// </para>
    /// </remarks>
    public sealed class SweepPlan
    {
        /// <summary>The argument that says "whatever the ruleset already says" for a retunable number.</summary>
        public const int AsAuthored = -1;

        /// <summary>
        /// How many seeds each creep is played on unless the caller says
        /// otherwise. A sample this size separates the roster and answers while
        /// somebody is still looking at the shell; it is the number the committed
        /// report was produced at.
        /// </summary>
        public const int DefaultRunsPerCreep = 8;

        /// <summary>The <see cref="MostCreeps"/> that scores the whole roster.</summary>
        public const int WholeRoster = 0;

        /// <summary>Names the derivation of one run's seed inside a sweep.</summary>
        private const string RunLabel = "sweep-run/1";

        private readonly UnitType[] _creeps;

        /// <summary>
        /// Builds a sweep's parameters, resolving the retunable numbers against
        /// the ruleset and refusing anything the harness cannot play.
        /// </summary>
        /// <param name="map">The board every match of every run is fought on.</param>
        /// <param name="rules">The matrix, the purse, the bands and the dials below.</param>
        /// <param name="types">The roster the rows are scored over and every cost is read out of.</param>
        /// <param name="schedule">The shape: the anchors, their tiers and the derived slot widths.</param>
        /// <param name="defense">What stands while each run's waves are sent.</param>
        /// <param name="field">
        /// The population a round's field of K is drawn from -- canned, until
        /// runs are stored and a real pool of them exists. See the remarks on
        /// <see cref="Sweep"/>.
        /// </param>
        /// <param name="firstSeed">What every run's seed in this sweep is derived from.</param>
        /// <param name="runsPerCreep">How many seeds each row of the roster is played on.</param>
        /// <param name="waves">N: how many waves a run lasts.</param>
        /// <param name="fieldSize">K: how many opponents a round is resolved against.</param>
        /// <param name="deathEndsTheRun">Whether health reaching zero stops a run.</param>
        /// <param name="ordinaryOptionsPerRound">The offering ratio's first half, or <see cref="AsAuthored"/>.</param>
        /// <param name="gameChangersPerAnchor">The offering ratio's second half, or <see cref="AsAuthored"/>.</param>
        /// <param name="freeSnapshotsPerRun">How many snapshots a run gets free, or <see cref="AsAuthored"/>.</param>
        /// <param name="snapshotPriceGold">What one costs after that, or <see cref="AsAuthored"/>.</param>
        /// <param name="mostCreeps">
        /// How many rows of the roster to score, or <see cref="WholeRoster"/>.
        /// Whatever it leaves out is reported rather than silently absent.
        /// </param>
        public SweepPlan(
            HexMap map,
            Ruleset rules,
            UnitTypeTable types,
            AnchorSchedule schedule,
            TowerLayout defense,
            FieldPool field,
            ulong firstSeed,
            int runsPerCreep = DefaultRunsPerCreep,
            int waves = Run.DefaultWaves,
            int fieldSize = Run.DefaultFieldSize,
            bool deathEndsTheRun = true,
            int ordinaryOptionsPerRound = AsAuthored,
            int gameChangersPerAnchor = AsAuthored,
            int freeSnapshotsPerRun = AsAuthored,
            int snapshotPriceGold = AsAuthored,
            int mostCreeps = WholeRoster)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            Types = types ?? throw new ArgumentNullException(nameof(types));
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            Defense = defense ?? throw new ArgumentNullException(nameof(defense));
            Field = field ?? throw new ArgumentNullException(nameof(field));

            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            Rules = rules.With(
                Or(ordinaryOptionsPerRound, rules.OrdinaryOptionsPerRound),
                Or(gameChangersPerAnchor, rules.GameChangersPerAnchor),
                Or(freeSnapshotsPerRun, rules.FreeSnapshotsPerRun),
                Or(snapshotPriceGold, rules.SnapshotPriceGold));

            RequireAtLeast(runsPerCreep, 1, "runs per creep", "A cell of no runs is a row about nothing.");
            RequireAtLeast(
                waves,
                1,
                "waves",
                "A sweep row is a bounded run: lifting the wave cap makes it a loop rather than a row.");
            RequireAtLeast(
                mostCreeps,
                WholeRoster,
                "the roster bound",
                "The whole roster is written as "
                + WholeRoster.ToString(CultureInfo.InvariantCulture)
                + " rather than as a negative count.");

            FirstSeed = firstSeed;
            RunsPerCreep = runsPerCreep;
            Waves = waves;
            FieldSize = fieldSize;
            DeathEndsTheRun = deathEndsTheRun;
            MostCreeps = mostCreeps;
            _creeps = Scored(types, mostCreeps);
            Roster = Walkers(types);
        }

        /// <summary>The board.</summary>
        public HexMap Map { get; }

        /// <summary>The rules as this sweep plays them, dials folded in and content hash moved with them.</summary>
        public Ruleset Rules { get; }

        /// <summary>The roster.</summary>
        public UnitTypeTable Types { get; }

        /// <summary>The shape.</summary>
        public AnchorSchedule Schedule { get; }

        /// <summary>What stands while a run's waves are sent.</summary>
        public TowerLayout Defense { get; }

        /// <summary>The population a round's field is drawn from.</summary>
        public FieldPool Field { get; }

        /// <summary>What every run's seed in this sweep is derived from.</summary>
        public ulong FirstSeed { get; }

        /// <summary>How many seeds each scored creep is played on.</summary>
        public int RunsPerCreep { get; }

        /// <summary>N.</summary>
        public int Waves { get; }

        /// <summary>K.</summary>
        public int FieldSize { get; }

        /// <summary>Whether health reaching zero stops a run.</summary>
        public bool DeathEndsTheRun { get; }

        /// <summary>How many rows of the roster this sweep scores, or <see cref="WholeRoster"/>.</summary>
        public int MostCreeps { get; }

        /// <summary>Every creep in the roster, scored or not.</summary>
        public int Roster { get; }

        /// <summary>The creeps this sweep actually scores, in table order.</summary>
        public IReadOnlyList<UnitType> Creeps => _creeps;

        /// <summary>
        /// The seed one run of this sweep is played on: derived from the sweep's
        /// own seed and the run's index rather than counted up from it, so two
        /// adjacent runs are not two adjacent streams.
        /// </summary>
        public ulong SeedOf(int run)
        {
            if (run < 0 || run >= RunsPerCreep)
            {
                throw new SimulationException(
                    "A sweep was asked for the seed of run "
                    + run.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + RunsPerCreep.ToString(CultureInfo.InvariantCulture)
                    + ". Every run of a cell is played on a seed derived from its index, so an index "
                    + "outside the cell is a run nothing in this sweep plays.");
            }

            return Hash64.Start(RunLabel).Add(unchecked((long)FirstSeed)).Add(run).Value;
        }

        /// <summary>An argument, or what the ruleset already says where the argument declined to say.</summary>
        private static int Or(int argument, int authored) => argument == AsAuthored ? authored : argument;

        /// <summary>How many rows of the roster walk. A tower is not a thing a wave sends.</summary>
        private static int Walkers(UnitTypeTable types)
        {
            int walkers = 0;

            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role == UnitRole.Moving)
                {
                    walkers++;
                }
            }

            return walkers;
        }

        /// <summary>
        /// The creeps a sweep scores: the roster's walkers in table order, cut
        /// to the bound if there is one.
        /// </summary>
        private static UnitType[] Scored(UnitTypeTable types, int mostCreeps)
        {
            var creeps = new List<UnitType>();

            for (int index = 0; index < types.Count; index++)
            {
                if (types.Types[index].Role != UnitRole.Moving)
                {
                    continue;
                }

                if (mostCreeps != WholeRoster && creeps.Count == mostCreeps)
                {
                    break;
                }

                creeps.Add(types.Types[index]);
            }

            if (creeps.Count == 0)
            {
                throw new SimulationException(
                    "A sweep was pointed at a roster with no creep in it to score. A row of this report is "
                    + "what one creep did over a population of runs, so a roster of towers alone is a sweep "
                    + "with nothing to be about rather than a sweep that comes back empty.");
            }

            return creeps.ToArray();
        }

        private static void RequireAtLeast(int value, int minimum, string what, string why)
        {
            if (value >= minimum)
            {
                return;
            }

            throw new SimulationException(
                "A sweep was planned with "
                + value.ToString(CultureInfo.InvariantCulture)
                + " for "
                + what
                + ", and the least it can be is "
                + minimum.ToString(CultureInfo.InvariantCulture)
                + ". "
                + why);
        }
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

    /// <summary>
    /// The balance harness: a population of runs per creep, folded into rows a
    /// spreadsheet reads.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This computes and it does not write.</b> It is handed parsed content
    /// and hands back rows; turning rows into a comma-separated file is the
    /// command line's job. That split is forced -- nothing in this assembly can
    /// open a file and the build gate scans the compiled image to keep it that
    /// way -- and it is the reason the whole harness needs one behavioural seam
    /// rather than a project.
    /// </para>
    /// <para>
    /// <b>A row is a creep, and the runs behind it favour that creep.</b> Each
    /// round the build phase takes that creep off the public offering when the
    /// menu carries it and the first option otherwise, then fills every slot the
    /// round has -- that creep first, then whatever else the run has unlocked,
    /// ascending by type id -- with an equal share of the purse each. What is
    /// left over banks and compounds, so an unaffordable slot is an investment
    /// rather than a waste.
    /// </para>
    /// <para>
    /// <b>A run wins when it survived every wave and out-dealt the field.</b>
    /// Both halves are folds over the outcome vector and neither needs a
    /// re-simulation. Surviving is the placing -- waves survived, which the
    /// health pool decides. Out-dealing is leak cost dealt against leak cost
    /// taken, and it is a fair comparison rather than a flattering one, because
    /// the defense that stands against this run's waves is the same defense the
    /// field stands: both sides are measured through the same wall.
    /// </para>
    /// <para>
    /// <b>The canned field is the economy's stand-in and not a tool pointed at
    /// it.</b> The percentile bands are measured against a distribution of other
    /// players' rounds, and no such pool exists until runs are stored, so the
    /// pool a sweep is handed <i>is</i> what the bands are computed against --
    /// what a run earns for its offense is decided by the harness's own canned
    /// opponent. <see cref="SweepRow.BonusGold"/> is that money, on the row,
    /// beside the base it is a share of.
    /// </para>
    /// <para>
    /// <b>What <see cref="SweepRow.DealtPerHundredGold"/> measures, and what it
    /// does not.</b> A leak charges health equal to what the creep cost to send,
    /// one for one, so leak cost dealt over gold spent is the <i>cost-weighted
    /// leak rate of what was sent</i> and the price level cancels out of it
    /// exactly -- halving a creep's price doubles how many of it a purse buys
    /// and halves what each leak charges. <b>This column therefore cannot tell
    /// anybody a creep is overpriced</b>, and reading it as though it could is
    /// the trap; what the cost column controls is granularity, which is how many
    /// bodies a purse turns into and therefore how a wave meets the slot width.
    /// The measured evidence is in
    /// <c>docs/research/cost-is-not-a-balance-lever-under-a-one-for-one-leak.md</c>.
    /// What it does say that the win rate does not: winning is one bit a run and
    /// it saturates, so every creep that clears the field's number reads the
    /// same, while this one stays graded and goes on separating them. Read it as
    /// how much of what was sent got through, weighted by price.
    /// </para>
    /// <para>
    /// <b>Every ratio is an integer and it arrives with its two operands.</b>
    /// There is no floating point in this assembly and the build gate scans for
    /// it, so a rate is basis points -- ten thousand is all of it -- truncated,
    /// beside the numerator and the denominator it came from. Basis points
    /// rather than per-mille because a sweep of a few hundred runs a cell
    /// distinguishes cells the coarser scale would round together, and rather
    /// than <see cref="Fix64"/> because that type is the tick loop's arithmetic
    /// -- its rounding is part of the simulation version, and a report's
    /// truncation has no business being pinned to that.
    /// </para>
    /// </remarks>
    public static class Sweep
    {
        /// <summary>What a basis point is out of. Not a lever: it is what the words mean.</summary>
        private const int BasisPoints = 10000;

        /// <summary>What <see cref="SweepRow.DealtPerHundredGold"/> is per.</summary>
        private const int PerGold = 100;

        /// <summary>The axis of the report that is the roster.</summary>
        private const string CreepAxis = "creeps";

        /// <summary>The axis of the report that is the seed space.</summary>
        private const string SeedAxis = "seeds";

        /// <summary>
        /// Plays the whole sweep and folds it into rows.
        /// </summary>
        /// <remarks>
        /// A pure function of its argument: the same plan produces the same rows
        /// on every machine, because every draw in every run of it is derived
        /// from the plan's own seed and nothing here reads a clock, a path or an
        /// environment.
        /// </remarks>
        public static SweepReport Of(SweepPlan plan)
        {
            if (plan is null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var rows = new List<SweepRow>();

            for (int index = 0; index < plan.Creeps.Count; index++)
            {
                Score(plan, plan.Creeps[index], rows);
            }

            var coverage = new[]
            {
                new CoverageBound(CreepAxis, plan.Creeps.Count, plan.Roster),
                new CoverageBound(SeedAxis, plan.RunsPerCreep, CoverageBound.Unbounded),
            };

            return new SweepReport(plan, rows.ToArray(), coverage);
        }

        /// <summary>
        /// One creep's population of runs, folded into its whole-population row
        /// and then one row per ingredient count that occurred.
        /// </summary>
        private static void Score(SweepPlan plan, UnitType creep, List<SweepRow> rows)
        {
            var whole = new Cell();

            // Bins are indexed by ingredient count, and a run cannot end holding
            // more ingredients than it had rounds to take them in. Walked in
            // ascending order below rather than kept in a keyed collection,
            // which is a banned type here.
            var bins = new Cell[plan.Waves + 1];

            for (int index = 0; index < plan.RunsPerCreep; index++)
            {
                Played played = Play(plan, creep, plan.SeedOf(index));

                // OBSERVED: return zero from Ingredients. Every sweep in the
                // suite refuses by this name; without it the runs vanish out of
                // every bin, stay in the population row, and the two "the bins
                // add up" tests go red naming a shortfall nothing caused.
                if (played.Ingredients < 1)
                {
                    throw new SimulationException(
                        "A run of "
                        + creep.Label
                        + " ended holding "
                        + played.Ingredients.ToString(CultureInfo.InvariantCulture)
                        + " ingredients. A run's build phases take one option each and a run has at least "
                        + "one of them, so a count below one is a bin sharing its index with the row over "
                        + "the whole population -- which would drop the run out of every bin while leaving "
                        + "it in the total, and the bins would stop adding up for a reason nothing reports.");
                }

                whole.Add(played);
                bins[played.Ingredients] ??= new Cell();
                bins[played.Ingredients]!.Add(played);
            }

            rows.Add(whole.Row(creep, SweepRow.AllIngredients));

            for (int ingredients = 1; ingredients < bins.Length; ingredients++)
            {
                if (bins[ingredients] is object)
                {
                    rows.Add(bins[ingredients]!.Row(creep, ingredients));
                }
            }
        }

        /// <summary>Plays one run to its end and reads off what the row needs.</summary>
        private static Played Play(SweepPlan plan, UnitType creep, ulong seed)
        {
            var run = new Run(
                plan.Map,
                plan.Rules,
                plan.Types,
                plan.Schedule,
                plan.Field,
                seed,
                plan.Waves,
                plan.FieldSize,
                plan.DeathEndsTheRun);

            long spent = 0;

            while (!run.IsOver)
            {
                BuildPhase phase = Decide(run, creep.Id);

                spent += GoldOf(run.Costs, phase);
                run.Advance(phase, plan.Defense);
            }

            // The bonus is read off the finished vector rather than added up as
            // the rounds went by: what each round dealt is on the vector, the
            // field is fixed for the run, and the bands are a lookup -- so what
            // a run earned for its offense is a fold and never a second play.
            return new Played(
                run.Outcome,
                Ingredients(run.Unlocks),
                spent,
                plan.Waves,
                run.Round,
                (long)plan.Rules.IncomeBasePerWave * run.Round,
                Purse.BonusOver(plan.Rules, run.Field, run.Outcome));
        }

        /// <summary>
        /// One build phase, decided from the round in front of it and from
        /// nothing else.
        /// </summary>
        /// <remarks>
        /// The take comes first because unlocking happens before buying, so a
        /// creep taken this round may be fielded in this round's wave. The purse
        /// is then divided evenly across the slots that will be filled, and a
        /// slot whose share does not reach one body is left empty rather than
        /// borrowed against.
        /// </remarks>
        private static BuildPhase Decide(Run run, int preferred)
        {
            Offering offering = run.Offering;
            Option take = Preferred(offering, preferred);
            int[] chosen = Chosen(run.Unlocks.With(take), preferred, offering.WaveSlots);
            var slots = new WaveSlot[chosen.Length];
            int share = chosen.Length == 0 ? 0 : run.Purse.Gold / chosen.Length;

            for (int index = 0; index < chosen.Length; index++)
            {
                int count = share / PriceOf(run.Costs, chosen[index]);

                // The record stores a slot's count as a u16, so a purse that
                // could buy more bodies than that fills the slot to its ceiling.
                slots[index] = count == 0
                    ? WaveSlot.Empty
                    : WaveSlot.Of(chosen[index], count > WaveSlot.Largest ? WaveSlot.Largest : count);
            }

            return BuildPhase.Of(take.Kind, take.Id, slots);
        }

        /// <summary>
        /// The option this row's runs take: the creep the row is about where the
        /// menu carries it, and the first thing on the menu otherwise.
        /// </summary>
        private static Option Preferred(Offering offering, int preferred)
        {
            for (int index = 0; index < offering.Options.Count; index++)
            {
                if (offering.Options[index].TypeId == preferred)
                {
                    return offering.Options[index];
                }
            }

            return offering.Options[0];
        }

        /// <summary>
        /// Which creeps this round's slots go to: the preferred one first, then
        /// the rest in the order they were taken, cut to the round's width and
        /// handed back ascending by type id -- which is the order a wave's lines
        /// are asserted in.
        /// </summary>
        /// <remarks>
        /// The selection is by preference and the result is by type id, and the
        /// two orders are separate on purpose: which creeps get a slot is the
        /// decision, and what order they are written in is the wave record's
        /// rule. The ordering is an insertion by hand because the framework's
        /// sorts are unstable and banned here.
        /// </remarks>
        private static int[] Chosen(Unlocks unlocks, int preferred, int waveSlots)
        {
            var candidates = new List<int>();

            if (unlocks.Has(preferred))
            {
                candidates.Add(preferred);
            }

            for (int index = 0; index < unlocks.Taken.Count && candidates.Count < waveSlots; index++)
            {
                int typeId = unlocks.Taken[index].TypeId;

                if (!candidates.Contains(typeId))
                {
                    candidates.Add(typeId);
                }
            }

            int taken = candidates.Count < waveSlots ? candidates.Count : waveSlots;
            var chosen = new int[taken];

            for (int index = 0; index < taken; index++)
            {
                int typeId = candidates[index];
                int place = index;

                while (place > 0 && chosen[place - 1] > typeId)
                {
                    chosen[place] = chosen[place - 1];
                    place--;
                }

                chosen[place] = typeId;
            }

            return chosen;
        }

        /// <summary>
        /// What one of that creep costs, refused where it costs nothing.
        /// </summary>
        /// <remarks>
        /// The harness budgets a slot by dividing a share of the purse by a
        /// price, and there is no dividing by nothing. A creep that costs zero
        /// is also a creep whose leak charges zero health, so it is outside the
        /// exchange rate the whole economy is denominated in rather than merely
        /// cheap.
        /// </remarks>
        private static int PriceOf(CostTable costs, int typeId)
        {
            int price = costs.PriceOf(Purchase.Unit(typeId));

            if (price > 0)
            {
                return price;
            }

            throw new SimulationException(
                "A sweep was pointed at a roster whose type id "
                + typeId.ToString(CultureInfo.InvariantCulture)
                + " costs nothing to send. Every purchasable thing carries a price, because a leak charges "
                + "health equal to what the creep cost one for one -- so a free creep is one a purse buys "
                + "without bound and a defense concedes for free, and there is no share of a purse to "
                + "divide by its price.");
        }

        /// <summary>What a build phase's slots cost, priced out of the run's own table.</summary>
        private static long GoldOf(CostTable costs, BuildPhase phase)
        {
            long spent = 0;

            for (int index = 0; index < phase.Slots.Count; index++)
            {
                WaveSlot slot = phase.Slots[index];

                if (!slot.IsEmpty)
                {
                    spent += costs.PriceOf(Purchase.Unit(slot.TypeId), slot.Count);
                }
            }

            return spent;
        }

        /// <summary>
        /// How many distinct creeps a run ended able to field. Takes rather than
        /// bodies: two game changers over one body are one ingredient, because
        /// what a wave may carry is a set of creeps.
        /// </summary>
        private static int Ingredients(Unlocks unlocks)
        {
            var seen = new List<int>();

            for (int index = 0; index < unlocks.Taken.Count; index++)
            {
                if (!seen.Contains(unlocks.Taken[index].TypeId))
                {
                    seen.Add(unlocks.Taken[index].TypeId);
                }
            }

            return seen.Count;
        }

        /// <summary>One played run, as the numbers a row is folded out of.</summary>
        private readonly struct Played
        {
            internal Played(
                RunOutcome outcome,
                int ingredients,
                long spent,
                int waves,
                int rounds,
                long incomeBase,
                long bonus)
            {
                Ingredients = ingredients;
                Spent = spent;
                Rounds = rounds;
                IncomeBase = incomeBase;
                Bonus = bonus;
                Dealt = outcome.LeakCostDealt;
                Taken = outcome.LeakCostTaken;

                // Survived every wave and got more past the field than the field
                // got past it. Both halves are folds over the vector, so nothing
                // here re-simulates a tick to find out.
                Won = outcome.WavesSurvived == waves && outcome.LeakCostDealt > outcome.LeakCostTaken;
            }

            internal int Ingredients { get; }

            internal long Spent { get; }

            internal int Rounds { get; }

            internal long IncomeBase { get; }

            internal long Bonus { get; }

            internal int Dealt { get; }

            internal int Taken { get; }

            internal bool Won { get; }
        }

        /// <summary>A row part-folded: the running sums, before they become ratios.</summary>
        private sealed class Cell
        {
            private int _runs;

            private int _rounds;

            private int _wins;

            private long _dealt;

            private long _taken;

            private long _spent;

            private long _incomeBase;

            private long _bonus;

            internal void Add(Played played)
            {
                _runs++;
                _rounds += played.Rounds;
                _wins += played.Won ? 1 : 0;
                _dealt += played.Dealt;
                _taken += played.Taken;
                _spent += played.Spent;
                _incomeBase += played.IncomeBase;
                _bonus += played.Bonus;
            }

            /// <summary>
            /// The row. The two ratios are truncated integer division and both
            /// operands are on the row beside them, so nothing downstream has to
            /// take this type's rounding on trust.
            /// </summary>
            internal SweepRow Row(UnitType creep, int ingredients)
            {
                if (_runs == 0)
                {
                    throw new SimulationException(
                        "A sweep folded a row for "
                        + creep.Label
                        + " out of no runs at all. A rate is a share of a population and there is no share "
                        + "of nothing, so an empty cell is a row that was emitted for a bin nothing landed "
                        + "in rather than a row reporting zero.");
                }

                return new SweepRow(
                    creep.Id,
                    creep.Label,
                    ingredients,
                    _runs,
                    _rounds,
                    _wins,
                    (int)((long)BasisPoints * _wins / _runs),
                    _dealt,
                    _taken,
                    _spent,
                    _spent == 0 ? 0 : (int)(PerGold * _dealt / _spent),
                    _incomeBase,
                    _bonus);
            }
        }
    }
}
