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
        /// The performance bonus, as a share of the base. Zero against
        /// <see cref="PerformanceField.Absent"/>, which is what every run
        /// carries until a pool of other players' rounds exists -- a build-order
        /// fact and not a fault. See <see cref="PerformanceField.Absent"/>.
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
            + Purse.Sauce.ToString(CultureInfo.InvariantCulture)
            + " sauce";
    }

    /// <summary>
    /// The purse: one currency called sauce, buying defense and offense alike.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>There is one wallet and this is it.</b> A tower and a creep and a
    /// scouting snapshot are all priced by the same <see cref="CostTable"/> and
    /// all paid for out of the same sauce, so a build phase is one decision
    /// rather than two small independent ones.
    /// </para>
    /// <para>
    /// <b>A purse is a value.</b> Spending and being paid return a new one, so
    /// a run's economy is a fold over its rounds and a test can assert on any
    /// intermediate without replaying anything.
    /// </para>
    /// <para>
    /// <b>Unspent sauce compounds.</b> What the purse carried through a wave
    /// earns interest at the ruleset's rate, rounded up -- one sauce banked
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
        /// wave. See <see cref="RequireBoundedCompounding"/>.
        /// </summary>
        public const int RoundCapLifted = 0;

        /// <summary>What a percentage is out of. Not a lever: it is what the word means.</summary>
        private const int Percent = 100;

        private static readonly Purse Nothing = new Purse(0);

        private Purse(int sauce)
        {
            Sauce = sauce;
        }

        /// <summary>A purse holding nothing.</summary>
        public static Purse Empty => Nothing;

        /// <summary>A purse holding this much sauce.</summary>
        public static Purse Holding(int sauce)
        {
            if (sauce < 0)
            {
                throw new SimulationException(
                    "A purse was asked to hold "
                    + sauce.ToString(CultureInfo.InvariantCulture)
                    + " sauce. There is no credit in this economy: a purchase that cannot be afforded is "
                    + "refused rather than borrowed against.");
            }

            return sauce == 0 ? Nothing : new Purse(sauce);
        }

        /// <summary>What this purse holds.</summary>
        public int Sauce { get; }

        /// <summary>
        /// Refuses a run whose bank would compound with nothing to stop it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Interest is a share of the bank paid every wave, so the bank grows
        /// geometrically and the only thing bounding it is the number of waves.
        /// Lifting the round cap therefore forces a ceiling on the interest, and
        /// a run configured with neither is a run whose sauce goes to infinity.
        /// That consequence was recorded when the cap was made a parameter, and
        /// this is where it announces itself instead of turning up later as an
        /// exploding number in a sweep nobody was watching.
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

            if (rounds != RoundCapLifted || rules.InterestCapSauce != Ruleset.NoInterestCeiling)
            {
                return;
            }

            throw new SimulationException(
                "This run has no round cap and its ruleset has no interest cap, so the bank compounds at "
                + rules.InterestPercentPerWave.ToString(CultureInfo.InvariantCulture)
                + "% a wave with nothing to stop it. Compounding is bounded by the round cap and by "
                + "nothing else, so lifting the cap forces a ceiling on the interest -- the second field "
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

            if (price > Sauce)
            {
                throw new SimulationException(
                    "A purse holding "
                    + Sauce.ToString(CultureInfo.InvariantCulture)
                    + " sauce was spent "
                    + price.ToString(CultureInfo.InvariantCulture)
                    + " on "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + what.ToString()
                    + ". A purchase nobody can afford is refused where the command is read, so reaching "
                    + "here means an unaffordable command was let through.");
            }

            return Holding(Sauce - price);
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
        /// What everybody else's round was worth.
        /// <see cref="PerformanceField.Absent"/> pays no bonus -- see the
        /// remarks there, because that is the build order and not a fault.
        /// </param>
        /// <param name="leakCostDealt">What this wave got past its opponents, priced in sauce.</param>
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

            long interest = InterestOn(rules, Sauce);
            long bonus = BonusOn(rules, field, leakCostDealt);
            long closing = Sauce + interest + rules.IncomeBasePerWave + bonus;

            if (closing > int.MaxValue)
            {
                throw new SimulationException(
                    "A wave closed a purse at "
                    + closing.ToString(CultureInfo.InvariantCulture)
                    + " sauce, which does not fit in the 32-bit integer a purse is kept in. Interest "
                    + "compounds, so a bank left alone for long enough leaves that range on its own -- "
                    + "which is the consequence a lifted round cap forces an interest cap to answer.");
            }

            return new WavePayment(
                Sauce,
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
            // its share to be a fraction still earns a coin. One sauce at ten
            // percent earns one.
            long earned = (((long)bank * rules.InterestPercentPerWave) + Percent - 1) / Percent;

            if (rules.InterestCapSauce != Ruleset.NoInterestCeiling && earned > rules.InterestCapSauce)
            {
                return rules.InterestCapSauce;
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

            PerformanceBand band = rules.BandFor(field.PercentileOf(leakCostDealt));

            return (long)rules.IncomeBasePerWave * band.BonusPercentOfBase / Percent;
        }
    }
}
