using System;
using System.Collections.Generic;

namespace Sim
{
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
    /// <b>A row is a creep, and the runs behind it favour that creep.</b> How
    /// much they favour it is <see cref="SweepPlan.Policy"/>: the scripted
    /// player the plan carries, handed the run and the creep the row is about,
    /// and asked for a build phase. Nothing below reads what it decided --
    /// a row is a fold over outcomes, so scoring a roster under another player
    /// is a plan and never an edit here.
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
        /// <remarks>
        /// The runs are played before any of them is binned, because how wide
        /// the bins have to be is a fact about what the runs did rather than
        /// about the plan they were played under. A fold that sized itself from
        /// the wave count would be carrying a claim about the player -- and the
        /// player is the plan's argument, so it is a claim this cannot check.
        /// </remarks>
        private static void Score(SweepPlan plan, UnitType creep, List<SweepRow> rows)
        {
            var population = new List<Played>();
            int widest = 0;

            for (int index = 0; index < plan.RunsPerCreep; index++)
            {
                Played played = Play(plan, creep, plan.SeedOf(index));

                population.Add(played);

                if (played.Ingredients > widest)
                {
                    widest = played.Ingredients;
                }
            }

            // Bins are indexed by ingredient count and there are as many as the
            // runs produced. Walked in ascending order below rather than kept in
            // a keyed collection, which is a banned type here.
            var whole = new Cell();
            var bins = new Cell[widest + 1];

            for (int index = 0; index < population.Count; index++)
            {
                Played played = population[index];

                whole.Add(played);
                bins[played.Ingredients] ??= new Cell();
                bins[played.Ingredients]!.Add(played);
            }

            rows.Add(whole.Row(creep, SweepRow.AllIngredients));

            for (int ingredients = 0; ingredients < bins.Length; ingredients++)
            {
                if (bins[ingredients] is null)
                {
                    continue;
                }

                if (ingredients == SweepRow.AllIngredients)
                {
                    throw new SimulationException(
                        "A run of "
                        + creep.Label
                        + " ended holding no ingredients at all, and that is the count a row over a whole "
                        + "population carries -- so a bin of them would be written down as that row rather "
                        + "than beside it. Nothing decides its way here: a policy hands back a build phase, "
                        + "a build phase takes exactly one option, and a run that resolved a round holds "
                        + "what it took.");
                }

                rows.Add(bins[ingredients]!.Row(creep, ingredients));
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
                // What the wave cost is read off the round rather than priced
                // again out here: the build phase works it out to spend it, and
                // a second walk over the slots is a second copy of the pricing
                // rule free to disagree with the one the purse was charged by.
                spent += run.Advance(plan.Policy(run, creep.Id), plan.Defense).Build.Spent;
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
