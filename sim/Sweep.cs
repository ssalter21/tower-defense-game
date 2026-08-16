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
    /// taken.
    /// </para>
    /// <para>
    /// <b>The two sides of that comparison stand different walls.</b> A run
    /// opens on an empty board and stands whatever its own build phases put
    /// there; the pool's members stand the defense the pool was canned with. So
    /// leak cost taken is a measurement of the player and leak cost dealt is a
    /// measurement against the canned opponent, and the difference between them
    /// is not a wall both sides share.
    /// </para>
    /// <para>
    /// <b>The canned field is the economy's stand-in and not a tool pointed at
    /// it.</b> A wave is paid a share of the leak cost it dealt, and what a wave
    /// gets past is decided by the wall in front of it -- so the pool a sweep is
    /// handed is what a run's offense earns its money against.
    /// <see cref="SweepRow.BonusGold"/> is that money, on the row, beside the
    /// base it is a share of.
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
            var everyRun = new List<SweepRunRow>();

            for (int index = 0; index < plan.Creeps.Count; index++)
            {
                Score(plan, plan.Creeps[index], rows, everyRun);
            }

            var coverage = new[]
            {
                new CoverageBound(CreepAxis, plan.Creeps.Count, plan.Roster),
                new CoverageBound(SeedAxis, plan.RunsPerCreep, CoverageBound.Unbounded),
            };

            return new SweepReport(plan, rows.ToArray(), everyRun.ToArray(), coverage);
        }

        /// <summary>
        /// One creep's population of runs, folded into its whole-population row
        /// and then one row per ingredient count that occurred.
        /// </summary>
        /// <remarks>
        /// <b>One row per creep, and there used to be more.</b> A creep's runs
        /// were also binned by how many distinct creeps the run had ended able
        /// to field -- its "ingredients" -- which was a real spread only because
        /// the take gate made what a run could send vary from run to run. With
        /// the gate gone every run can send the whole roster from wave one, so
        /// the bin is the same number in every row and a column that never
        /// varies separates nothing. It came out with the gate that produced it.
        /// </remarks>
        private static void Score(
            SweepPlan plan,
            UnitType creep,
            List<SweepRow> rows,
            List<SweepRunRow> everyRun)
        {
            var whole = new Cell();

            for (int index = 0; index < plan.RunsPerCreep; index++)
            {
                ulong seed = plan.SeedOf(index);
                Played played = Play(plan, creep, seed);

                whole.Add(played);

                // A run is kept here or nowhere: the fold takes what it came to
                // and holds only sums, so recovering one afterwards means
                // playing the whole sweep again. What the plan is asked is
                // therefore whether to keep them rather than whether to compute
                // them, and the answer is off by default because the ceiling
                // this harness allows is millions of rows.
                if (plan.KeepsEveryRun)
                {
                    everyRun.Add(played.Row(creep, seed));
                }
            }

            rows.Add(whole.Row(creep));
        }

        /// <summary>Plays one run to its end and reads off what the row needs.</summary>
        private static Played Play(SweepPlan plan, UnitType creep, ulong seed)
        {
            var run = new Run(
                plan.Map,
                plan.Rules,
                plan.Types,
                plan.Ladder,
                plan.Field,
                seed,
                plan.Waves,
                plan.FieldSize,
                plan.DeathEndsTheRun);

            long spent = 0;
            long defense = 0;

            while (!run.IsOver)
            {
                // What the round cost is read off the round rather than priced
                // again out here: the build phase works it out to spend it, and
                // a second walk over the slots is a second copy of the pricing
                // rule free to disagree with the one the purse was charged by.
                //
                // The two halves of that one bill are reported separately,
                // because gold spent is what the cost-efficiency column is per
                // and towers do not walk.
                Build build = run.Advance(plan.Policy(run, creep.Id)).Build;

                defense += build.Defense;
                spent += build.Spent - build.Defense;
            }

            // The bonus is read off the finished vector rather than added up as
            // the rounds went by: what each round dealt is on the vector and the
            // rate is a multiplication, so what a run earned for its offense is
            // a fold and never a second play.
            //
            // The purse is read where the loop stopped, which is the end of the
            // run by either of the two ways one ends.
            return new Played(
                run.Outcome,
                spent,
                defense,
                run.Purse.Gold,
                plan.Waves,
                run.Round,
                (long)plan.Rules.IncomeBasePerWave * run.Round,
                Purse.BonusOver(plan.Rules, run.Outcome));
        }


        /// <summary>One played run, as the numbers a row is folded out of.</summary>
        private readonly struct Played
        {
            internal Played(
                RunOutcome outcome,
                long spent,
                long defense,
                long unspent,
                int waves,
                int rounds,
                long incomeBase,
                long bonus)
            {
                Spent = spent;
                Defense = defense;
                Unspent = unspent;
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

            internal long Spent { get; }

            internal long Defense { get; }

            internal long Unspent { get; }

            internal int Rounds { get; }

            internal long IncomeBase { get; }

            internal long Bonus { get; }

            internal int Dealt { get; }

            internal int Taken { get; }

            internal bool Won { get; }

            /// <summary>
            /// This one run as a row of the report, under the creep it favoured
            /// and the seed it was played on.
            /// </summary>
            /// <remarks>
            /// Every number on it is one the fold sums, and neither ratio is:
            /// a rate over a single run is a bit, and both of the integers one
            /// would be computed from are on the row already.
            /// </remarks>
            internal SweepRunRow Row(UnitType creep, ulong seed) =>
                new SweepRunRow(
                    creep.Id,
                    creep.Label,
                    seed,
                    Rounds,
                    Won,
                    Dealt,
                    Taken,
                    Spent,
                    Defense,
                    Unspent,
                    IncomeBase,
                    Bonus);
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

            private long _defense;

            private long _unspent;

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
                _defense += played.Defense;
                _unspent += played.Unspent;
                _incomeBase += played.IncomeBase;
                _bonus += played.Bonus;
            }

            /// <summary>
            /// The row. The two ratios are truncated integer division and both
            /// operands are on the row beside them, so nothing downstream has to
            /// take this type's rounding on trust.
            /// </summary>
            internal SweepRow Row(UnitType creep)
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
                    _runs,
                    _rounds,
                    _wins,
                    (int)((long)BasisPoints * _wins / _runs),
                    _dealt,
                    _taken,
                    _spent,
                    _defense,
                    _unspent,
                    _spent == 0 ? 0 : (int)(PerGold * _dealt / _spent),
                    _incomeBase,
                    _bonus);
            }
        }
    }
}
