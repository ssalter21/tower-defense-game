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
    /// <b>Everything it may read is on the run.</b> What the board holds, what
    /// the purse holds and what everything costs are all <see cref="Run"/>
    /// members, so a policy needs no second argument and cannot be handed a
    /// round the run is not on.
    /// </para>
    /// </remarks>
    /// <param name="run">The run as it stands before this round, board and purse included.</param>
    /// <param name="preferred">The type id of the creep the sweep row is about.</param>
    public delegate BuildPhase BuildPolicy(Run run, int preferred);

    /// <summary>
    /// The scripted player the sweep has always used: it builds with half the
    /// purse and spends the other half on the one creep its row is about.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Half the purse goes to the board.</b> What to build is
    /// <see cref="CoverThenUpgradeBot"/>'s decision and not this one: a round is
    /// one decision over one wallet, so both halves arrive as one build phase
    /// carrying both the actions and the slots. What the defensive half declines
    /// to spend banks rather than falling through to the wave.
    /// </para>
    /// <para>
    /// <b>The other half goes to one creep, and that is a narrowing.</b> While
    /// the menu existed this bot filled the round's other slots with whatever
    /// the forced pick had unlocked -- creeps drawn at random, which put noise
    /// from a second roster into a row about one. With the gate gone the whole
    /// roster is sendable, so "everything it has unlocked" is no longer a
    /// selection at all, and a row measuring its own creep is what its columns
    /// have always claimed to say.
    /// </para>
    /// <para>
    /// What the share does not reach banks and compounds, so a purse short of
    /// one body is an investment rather than a waste.
    /// </para>
    /// <para>
    /// <b>This is a decision and not the only one.</b> It is the default a
    /// <see cref="SweepPlan"/> carries, which is what makes scoring the same
    /// roster under a greedier build an argument to the plan rather than an edit
    /// here.
    /// </para>
    /// </remarks>
    public static class EvenShareBot
    {
        /// <summary>One build phase, decided from the round in front of it and from nothing else.</summary>
        /// <remarks>
        /// <b>One slot, which is why #191 did not touch this bot.</b> A slot's
        /// position became its release order, and a sweep row is about one
        /// creep -- so every wave this composes is one column and the order it
        /// is in is the only order there is. That is not a gap in the bot: it
        /// is what makes a row attributable to the creep it names. It <i>is</i>
        /// a gap in the report, because nothing in <c>content/sweep.csv</c>
        /// varies with how a wave is arranged, and the CSV carries a note
        /// saying so rather than leaving somebody to find it.
        /// </remarks>
        public static BuildPhase Decide(Run run, int preferred)
        {
            if (run is null)
            {
                throw new ArgumentNullException(nameof(run));
            }

            IReadOnlyList<BuildAction> built = CoverThenUpgradeBot.Decide(run);
            int wave = run.Purse.Gold - CoverThenUpgradeBot.BudgetOf(run.Purse);

            // What the round already fields, plus whatever this round's share
            // adds to it. A creep is bought once and attacks every round after,
            // so a bot that sent only what it could afford this round would be
            // asking to send fewer than it carries -- which is refused, and
            // rightly. A sweep row therefore measures a creep accumulating,
            // which is what a run of it now actually is.
            int held = run.Carrying.CountOf(preferred);
            int count = held + (wave / PriceOf(run.Costs, preferred));

            // The record stores a slot's count as a u16, so a purse that could
            // buy more bodies than that fills the slot to its ceiling.
            WaveSlot slot = count == 0
                ? WaveSlot.Empty
                : WaveSlot.Of(preferred, count > WaveSlot.Largest ? WaveSlot.Largest : count);

            BuildPhase phase = BuildPhase.Of(slot);

            for (int index = 0; index < built.Count; index++)
            {
                phase = phase.With(built[index]);
            }

            return phase;
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
