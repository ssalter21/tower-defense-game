using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// A scripted player: one build phase, decided from the run in front of it
    /// and the creep the sweep row is about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the only producer of build phases that is not a command
    /// stream.</b> A run reaches a decision either from stored bytes somebody
    /// played or from one of these, and naming it is what stops "a row's runs
    /// favour that creep" meaning one specific unwritten strategy.
    /// </para>
    /// <para>
    /// <b>Everything it may read is on the run.</b> The offering standing in
    /// front of the round, what the run has unlocked, what the purse holds and
    /// what everything costs are all <see cref="Run"/> members, so a policy needs
    /// no second argument and cannot be handed a round the run is not on.
    /// </para>
    /// <para>
    /// A build phase takes exactly one option and it is not optional -- see
    /// <see cref="BuildPhase"/> -- so every policy unlocks something every
    /// round, whatever else it decides.
    /// </para>
    /// </remarks>
    /// <param name="run">The run as it stands before this round, offering and purse included.</param>
    /// <param name="preferred">The type id of the creep the sweep row is about.</param>
    public delegate BuildPhase BuildPolicy(Run run, int preferred);

    /// <summary>
    /// The scripted player the sweep has always used: it takes the creep the row
    /// is about and divides the purse evenly across the slots it fills.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The take comes first because unlocking happens before buying</b>, so a
    /// creep taken this round may be fielded in this round's wave. It is the
    /// creep the row is about where the round's menu carries it and the first
    /// option on the menu otherwise.
    /// </para>
    /// <para>
    /// <b>Then every slot the round has is filled</b> -- that creep first, then
    /// whatever else the run has unlocked, ascending by type id -- with an equal
    /// share of the purse each. What is left over banks and compounds, so a slot
    /// whose share does not reach one body is an investment rather than a waste.
    /// </para>
    /// <para>
    /// <b>An even share is a decision and not the only one.</b> It is the
    /// default a <see cref="SweepPlan"/> carries, which is what makes scoring
    /// the same roster under a greedier build an argument to the plan rather
    /// than an edit here.
    /// </para>
    /// </remarks>
    public static class EvenShareBot
    {
        /// <summary>One build phase, decided from the round in front of it and from nothing else.</summary>
        public static BuildPhase Decide(Run run, int preferred)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

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
        /// This bot budgets a slot by dividing a share of the purse by a price,
        /// and there is no dividing by nothing. A creep that costs zero is also a
        /// creep whose leak charges zero health, so it is outside the exchange
        /// rate the whole economy is denominated in rather than merely cheap.
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
    }
}
