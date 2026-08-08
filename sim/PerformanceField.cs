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
        /// <summary>What a percentage is out of. Not a lever: it is what the word means.</summary>
        private const int Percent = 100;

        private static readonly PerformanceField NoField = new PerformanceField(new int[0]);

        private readonly int[] _dealt;

        private PerformanceField(int[] dealt)
        {
            _dealt = dealt;
        }

        /// <summary>
        /// No field at all: nobody to be measured against, so nothing to
        /// measure. A wave paid against this earns its base and a bonus of zero.
        /// </summary>
        /// <remarks>
        /// <b>No run carries this.</b> A run measures its pool and is paid out
        /// of what it measured, so the bonus a wave earns is a real number off
        /// the bands. This is what the measurement itself is played against --
        /// the pool's own rounds are being priced, not paid -- and it is the
        /// honest answer for anybody holding a population of nobody.
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
                dealt[index] = RequireAmount(leakCostsDealt[index], "A field");
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
            RequireAmount(leakCostDealt, "A wave");

            if (!IsPresent)
            {
                throw new SimulationException(
                    "A wave was ranked against a field of nobody. A percentile is a share of the field, "
                    + "and there is no share of nothing -- see PerformanceField.Absent, which is the field "
                    + "a population of nobody makes and which pays no bonus rather than reporting a rank.");
            }

            int beaten = 0;

            for (int index = 0; index < _dealt.Length; index++)
            {
                if (_dealt[index] < leakCostDealt)
                {
                    beaten++;
                }
            }

            return (int)((long)Percent * beaten / _dealt.Length);
        }

        /// <summary>A leak cost, refused if it is not one.</summary>
        private static int RequireAmount(int leakCostDealt, string who)
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
