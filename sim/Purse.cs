using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What one wave paid a purse, itemised, and the purse it left behind.
    /// </summary>
    /// <remarks>
    /// Itemised rather than summed, because the three lines answer different
    /// questions: the interest is what banking was worth, the base is what the
    /// wave was worth for happening, and the bonus is what the wave was worth
    /// against the field. A run's retrospective is a fold over these.
    /// </remarks>
    public sealed class WavePayment
    {
        internal WavePayment(int opening, int interest, int incomeBase, int bonus, Purse purse)
        {
            Opening = opening;
            Interest = interest;
            IncomeBase = incomeBase;
            Bonus = bonus;
            Purse = purse;
        }

        /// <summary>What the purse carried into the wave, and what earned the interest.</summary>
        public int Opening { get; }

        /// <summary>What the bank paid, rounded up and capped where the ruleset caps it.</summary>
        public int Interest { get; }

        /// <summary>The flat base, paid whether or not there was a field.</summary>
        public int IncomeBase { get; }

        /// <summary>
        /// The performance bonus: the share of the base the band this wave's
        /// result reached in the field pays. Never negative, and zero only for a
        /// wave in the bottom band or one measured against
        /// <see cref="PerformanceField.Absent"/>.
        /// </summary>
        public int Bonus { get; }

        /// <summary>Everything the wave paid.</summary>
        public int Total => Interest + IncomeBase + Bonus;

        /// <summary>The purse afterwards, holding what it opened with plus what it was paid.</summary>
        public Purse Purse { get; }

        public override string ToString() =>
            Opening.ToString(CultureInfo.InvariantCulture)
            + " + "
            + Interest.ToString(CultureInfo.InvariantCulture)
            + " interest + "
            + IncomeBase.ToString(CultureInfo.InvariantCulture)
            + " base + "
            + Bonus.ToString(CultureInfo.InvariantCulture)
            + " bonus = "
            + Purse.Gold.ToString(CultureInfo.InvariantCulture)
            + " gold";
    }

    /// <summary>
    /// The purse: one currency called gold, buying defense and offense alike.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There is one wallet and this is it.</b> A tower and a creep and a
    /// scouting snapshot are all priced by the same <see cref="CostTable"/> and
    /// all paid for out of the same gold, so a build phase is one decision
    /// rather than two small independent ones.
    /// </para>
    /// <para>
    /// <b>A purse is a value.</b> Spending and being paid return a new one, so
    /// a run's economy is a fold over its rounds and a test can assert on any
    /// intermediate without replaying anything.
    /// </para>
    /// <para>
    /// <b>Unspent gold compounds.</b> What the purse carried through a wave
    /// earns interest at the ruleset's rate, rounded up -- one gold banked
    /// earns one, never nothing -- so an empty wave slot is an investment and
    /// every purchase is measured against what not making it would have grown
    /// to. Compounding is bounded by the run's round cap and by nothing else,
    /// which is what <see cref="RequireBoundedCompounding"/> exists to announce.
    /// </para>
    /// </remarks>
    public sealed class Purse
    {
        /// <summary>
        /// The round count that says the cap has been lifted: a run with no last
        /// wave. A run of no rounds at all is not a run and has no spelling
        /// here. See <see cref="RequireBoundedCompounding"/>.
        /// </summary>
        public const int RoundCapLifted = 0;

        /// <summary>What a percentage is out of. Not a lever: it is what the word means.</summary>
        private const int Percent = 100;

        private static readonly Purse Nothing = new Purse(0);

        private Purse(int gold)
        {
            Gold = gold;
        }

        /// <summary>A purse holding nothing.</summary>
        public static Purse Empty => Nothing;

        /// <summary>A purse holding this much gold.</summary>
        public static Purse Holding(int gold)
        {
            if (gold < 0)
            {
                throw new SimulationException(
                    "A purse was asked to hold "
                    + gold.ToString(CultureInfo.InvariantCulture)
                    + " gold. There is no credit in this economy: a purchase that cannot be afforded is "
                    + "refused rather than borrowed against.");
            }

            return gold == 0 ? Nothing : new Purse(gold);
        }

        /// <summary>What this purse holds.</summary>
        public int Gold { get; }

        /// <summary>
        /// Refuses a run whose bank would compound with nothing to stop it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Interest is a share of the bank paid every wave, so the bank grows
        /// geometrically and the only thing bounding it is the number of waves.
        /// Lifting the round cap therefore forces a ceiling on the interest, and
        /// a run configured with neither is a run whose gold goes to infinity.
        /// This is where that announces itself, rather than turning up later as
        /// an exploding number in a sweep nobody was watching.
        /// </para>
        /// <para>
        /// <b>Call this where a run is constructed</b>, before a wave is
        /// resolved. Refusing at the first overflow would be refusing after the
        /// run had already produced numbers.
        /// </para>
        /// </remarks>
        /// <param name="rules">Where the interest rate and its ceiling are authored.</param>
        /// <param name="rounds">
        /// How many rounds the run lasts, or <see cref="RoundCapLifted"/> for a
        /// run with no last wave.
        /// </param>
        public static void RequireBoundedCompounding(Ruleset rules, int rounds)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (rounds < 0)
            {
                throw new SimulationException(
                    "A run was constructed to last "
                    + rounds.ToString(CultureInfo.InvariantCulture)
                    + " rounds. A run lasts a whole number of them, and the cap being lifted is written "
                    + "as "
                    + RoundCapLifted.ToString(CultureInfo.InvariantCulture)
                    + " rather than as a negative length.");
            }

            if (rounds != RoundCapLifted || rules.InterestCapGold != Ruleset.NoInterestCeiling)
            {
                return;
            }

            throw new SimulationException(
                "This run has no round cap and its ruleset has no interest cap, so the bank compounds at "
                + rules.InterestPercentPerWave.ToString(CultureInfo.InvariantCulture)
                + "% a wave with nothing to stop it. Compounding is bounded by the round cap and by "
                + "nothing else, so lifting the cap forces a ceiling on the interest -- the cap column "
                + "of the ruleset's 'interest' row. Refused before the run starts rather than discovered "
                + "later as an exploding number.");
        }

        /// <summary>The purse after buying this many of one thing.</summary>
        public Purse Spend(CostTable costs, Purchase what, int count)
        {
            if (costs is null)
            {
                throw new ArgumentNullException(nameof(costs));
            }

            int price = costs.PriceOf(what, count);

            if (price > Gold)
            {
                throw new SimulationException(
                    "A purse holding "
                    + Gold.ToString(CultureInfo.InvariantCulture)
                    + " gold was spent "
                    + price.ToString(CultureInfo.InvariantCulture)
                    + " on "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what.ToString()
                    + ". A purchase nobody can afford is refused where the command is read, so reaching "
                    + "here means an unaffordable command was let through.");
            }

            return Holding(Gold - price);
        }

        /// <summary>
        /// What a wave pays this purse, and the purse afterwards.
        /// </summary>
        /// <remarks>
        /// Three lines, in this order. The interest is taken on what the purse
        /// carried <b>through</b> the wave, before the wave's own money lands,
        /// which is what makes not spending an investment rather than a
        /// rebate. The base is flat. The bonus is a share of the base decided by
        /// the band the wave's result reached in the field.
        /// </remarks>
        /// <param name="rules">The rate, the ceiling, the base and the bands.</param>
        /// <param name="field">
        /// What everybody else's round was worth. A population of nobody --
        /// <see cref="PerformanceField.Absent"/> -- has no percentile to report
        /// and so pays no bonus.
        /// </param>
        /// <param name="leakCostDealt">What this wave got past its opponents, priced in gold.</param>
        public WavePayment CloseWave(Ruleset rules, PerformanceField field, int leakCostDealt)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            return Closed(rules, BonusOn(rules, field, leakCostDealt));
        }

        /// <summary>
        /// The most a wave can pay this purse, for whoever cannot know what the
        /// wave did.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The interest and the base, and then the top band rather than the band
        /// a result reached -- so this is an upper bound on the payment and never
        /// the payment. What a wave earns depends on what it got past the field,
        /// which is a number only a resolved round has.
        /// </para>
        /// <para>
        /// <b>The bound is above rather than below on purpose.</b> A walk over a
        /// stored stream spends this purse checking whether each decision was
        /// affordable; bounded above, everything it refuses was unaffordable
        /// whatever the run performed, and a decision it lets through is checked
        /// again against the purse the round really holds. Bounded below it would
        /// refuse decisions the run affords perfectly well.
        /// </para>
        /// </remarks>
        /// <param name="rules">The rate, the ceiling, the base and the bands.</param>
        public WavePayment CloseWaveAtBest(Ruleset rules)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            return Closed(rules, ShareOfTheBase(rules, rules.BestBand));
        }

        /// <summary>
        /// What the bonus came to over a whole run: what each round dealt, placed
        /// against the field, priced out of the bands and added up.
        /// </summary>
        /// <remarks>
        /// <b>A fold over the outcome vector and nothing else.</b> The vector
        /// carries what every round of the run got past the field it faced, so
        /// what a run earned for its offense is arithmetic over a stored run --
        /// no tick is replayed and no match is resolved to find out what a round
        /// paid.
        /// </remarks>
        /// <param name="rules">Where the base and the bands are authored.</param>
        /// <param name="field">The distribution every round of the run was paid against.</param>
        /// <param name="outcome">The vector, played or rebuilt from a stored one.</param>
        public static int BonusOver(Ruleset rules, PerformanceField field, RunOutcome outcome)
        {
            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            if (outcome is null)
            {
                throw new ArgumentNullException(nameof(outcome));
            }

            long earned = 0;

            for (int round = 0; round < outcome.Rounds.Count; round++)
            {
                earned += BonusOn(rules, field, outcome.Rounds[round].LeakCostDealt);
            }

            if (earned > int.MaxValue)
            {
                throw new SimulationException(
                    "A run's waves earned "
                    + earned.ToString(CultureInfo.InvariantCulture)
                    + " gold in performance bonuses, which does not fit in the 32-bit integer gold is "
                    + "counted in. A bonus is a share of the flat base, so a total past that range is a "
                    + "base or a band authored in the wrong units.");
            }

            return (int)earned;
        }

        /// <summary>The purse after the interest, the base and a bonus somebody worked out.</summary>
        private WavePayment Closed(Ruleset rules, long bonus)
        {
            long interest = InterestOn(rules, Gold);
            long closing = Gold + interest + rules.IncomeBasePerWave + bonus;

            if (closing > int.MaxValue)
            {
                throw new SimulationException(
                    "A wave closed a purse at "
                    + closing.ToString(CultureInfo.InvariantCulture)
                    + " gold, which does not fit in the 32-bit integer a purse is kept in. Interest "
                    + "compounds, so a bank left alone for long enough leaves that range on its own -- "
                    + "which is the consequence a lifted round cap forces an interest cap to answer.");
            }

            return new WavePayment(
                Gold,
                (int)interest,
                rules.IncomeBasePerWave,
                (int)bonus,
                Holding((int)closing));
        }

        /// <summary>
        /// What a bank of this size earns in one wave: the rate, rounded up, and
        /// then the ruleset's ceiling if it has one.
        /// </summary>
        private static long InterestOn(Ruleset rules, int bank)
        {
            // Rounded up rather than truncated, so that a bank small enough for
            // its share to be a fraction still earns a coin. One gold at ten
            // percent earns one.
            long earned = (((long)bank * rules.InterestPercentPerWave) + Percent - 1) / Percent;

            if (rules.InterestCapGold != Ruleset.NoInterestCeiling && earned > rules.InterestCapGold)
            {
                return rules.InterestCapGold;
            }

            return earned;
        }

        /// <summary>
        /// The band's share of the base. No field means no percentile to be at,
        /// so it pays nothing.
        /// </summary>
        private static long BonusOn(Ruleset rules, PerformanceField field, int leakCostDealt)
        {
            if (!field.IsPresent)
            {
                return 0;
            }

            return ShareOfTheBase(rules, rules.BandFor(field.PercentileOf(leakCostDealt)));
        }

        /// <summary>What one band pays, in gold. Truncated, and never negative.</summary>
        private static long ShareOfTheBase(Ruleset rules, PerformanceBand band) =>
            (long)rules.IncomeBasePerWave * band.BonusPercentOfBase / Percent;
    }
}
