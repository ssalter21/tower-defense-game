using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// The distribution a wave's result is paid against: what everybody else's
    /// round was worth.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>No sauce moves between players.</b> A wave is measured against the
    /// spread of the field and never against a named opponent, so the payment
    /// reads the same whether the field is one lobby or a whole population, and
    /// beating somebody takes nothing off them.
    /// </para>
    /// <para>
    /// The measure is leak cost dealt -- what a wave got past its opponents,
    /// priced in sauce -- which is one half of the pair a round already records.
    /// </para>
    /// </remarks>
    public sealed class PerformanceField
    {
        private static readonly PerformanceField NoField = new PerformanceField(new int[0]);

        private readonly int[] _dealt;

        private PerformanceField(int[] dealt)
        {
            _dealt = dealt;
        }

        /// <summary>
        /// No field at all: nobody to be measured against, so nothing to
        /// measure.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A run measured against this is paid its base and a bonus of zero,
        /// and that is the build order rather than a fault.</b> A band is a
        /// percentile of a field, a field is a pool of other players' rounds,
        /// and no such pool exists yet -- the sweep's canned one is the first,
        /// and real opponents' rounds come later still. Until then this is what
        /// every wave is measured against and every bonus is zero.
        /// </para>
        /// <para>
        /// So: a zero bonus is not a missing multiplication, an unread ruleset
        /// row or a band that failed to match. It is this value, named, and the
        /// bands are already authored, already progressive and already never
        /// negative for the run that gets a field.
        /// </para>
        /// </remarks>
        public static PerformanceField Absent => NoField;

        /// <summary>The field, as what each of its rounds dealt in leak cost.</summary>
        public static PerformanceField Of(IReadOnlyList<int> leakCostsDealt)
        {
            if (leakCostsDealt is null)
            {
                throw new ArgumentNullException(nameof(leakCostsDealt));
            }

            var dealt = new int[leakCostsDealt.Count];

            for (int index = 0; index < dealt.Length; index++)
            {
                dealt[index] = Amount(leakCostsDealt[index], "A field");
            }

            return dealt.Length == 0 ? NoField : new PerformanceField(dealt);
        }

        /// <summary>How many rounds are in the field.</summary>
        public int Size => _dealt.Length;

        /// <summary>Whether there is a field here to be measured against at all.</summary>
        public bool IsPresent => _dealt.Length > 0;

        /// <summary>
        /// What percentile of the field a wave that dealt this much sits at:
        /// how much of the field it beat outright, in percent, truncated. A wave
        /// nothing in the field matched sits at 100.
        /// </summary>
        public int PercentileOf(int leakCostDealt)
        {
            Amount(leakCostDealt, "A wave");

            if (!IsPresent)
            {
                throw new SimulationException(
                    "A wave was ranked against a field of nobody. A percentile is a share of the field, "
                    + "and there is no share of nothing -- see PerformanceField.Absent, which is what a "
                    + "run with no field to measure against carries and why its bonus is zero.");
            }

            int beaten = 0;

            for (int index = 0; index < _dealt.Length; index++)
            {
                if (_dealt[index] < leakCostDealt)
                {
                    beaten++;
                }
            }

            return (int)(100L * beaten / _dealt.Length);
        }

        private static int Amount(int leakCostDealt, string who)
        {
            if (leakCostDealt < 0)
            {
                throw new SimulationException(
                    who
                    + " is recorded as having dealt "
                    + leakCostDealt.ToString(CultureInfo.InvariantCulture)
                    + " in leak cost. A leak costs its creep's price one for one, and a price is never "
                    + "negative, so a round below zero is a subtraction somebody performed twice.");
            }

            return leakCostDealt;
        }
    }
}
