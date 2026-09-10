using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>How a run stopped, or that it has not.</summary>
    public enum RunEnding
    {
        /// <summary>Still going: there are waves left and there is health left.</summary>
        Unfinished = 0,

        /// <summary>The last wave of the run resolved.</summary>
        OutOfWaves = 1,

        /// <summary>Health reached zero, and death ends this run.</summary>
        OutOfHealth = 2,
    }

    /// <summary>
    /// One round, as the three numbers it is: what got past everybody else,
    /// what got past me, and what my defense was paid for the bodies it killed.
    /// The first two are leak costs -- gold, priced one for one off whatever
    /// walked to the exit -- and the third is gold too, off the bounty column of
    /// whatever died.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each is the <b>average</b> of the round's K resolutions rather than the
    /// best or the sum of them, and all three for the same reason: a field of
    /// ten is meant to average a bad draw away rather than to multiply it.
    /// </para>
    /// <para>
    /// <b>The bounty is on the vector because the purse is a fold over it.</b>
    /// What a run holds at the end has to be arithmetic over these numbers and
    /// never a second play -- see
    /// <see cref="Purse.BonusOver(Ruleset, RunOutcome)"/> and the run's own
    /// retrospective -- so an income line missing here would be a purse nobody
    /// could rebuild. It is gold and it appears nowhere in
    /// <see cref="RunOutcome.CompareTo"/>, exactly as
    /// <see cref="LeakCostDealt"/> does not. <b>There is one constructor and it
    /// takes all three</b>, for the same reason: an overload that filled the
    /// third in with a zero would be a way to leave an income line out by
    /// writing fewer arguments than the vector has numbers.
    /// </para>
    /// </remarks>
    public readonly struct RoundOutcome
    {
        public RoundOutcome(int leakCostDealt, int leakCostTaken, int bountyEarned)
        {
            LeakCostDealt = Amount(leakCostDealt, "dealt to");
            LeakCostTaken = Amount(leakCostTaken, "taken from");
            BountyEarned = Bounty(bountyEarned);
        }

        /// <summary>What this round's wave got past the field, priced in gold.</summary>
        public int LeakCostDealt { get; }

        /// <summary>What the field's waves got past this round's defense, priced in gold.</summary>
        public int LeakCostTaken { get; }

        /// <summary>What this round's defense was paid for the bodies it killed, in gold.</summary>
        public int BountyEarned { get; }

        public override string ToString() =>
            "dealt "
            + LeakCostDealt.ToString(CultureInfo.InvariantCulture)
            + ", took "
            + LeakCostTaken.ToString(CultureInfo.InvariantCulture)
            + ", earned "
            + BountyEarned.ToString(CultureInfo.InvariantCulture);

        /// <summary>A leak cost, refused if it is not one.</summary>
        private static int Amount(int leakCost, string direction)
        {
            if (leakCost < 0)
            {
                throw new SimulationException(
                    "A round is recorded as having "
                    + leakCost.ToString(CultureInfo.InvariantCulture)
                    + " in leak cost "
                    + direction
                    + " it. A leak costs its creep's price one for one and a price is never negative, so a "
                    + "round below zero is a subtraction somebody performed twice.");
            }

            return leakCost;
        }

        /// <summary>A bounty, refused if it is not one.</summary>
        private static int Bounty(int bountyEarned)
        {
            if (bountyEarned < 0)
            {
                throw new SimulationException(
                    "A round is recorded as having earned "
                    + bountyEarned.ToString(CultureInfo.InvariantCulture)
                    + " in bounties. A bounty is authored as a non-negative column and paid once per "
                    + "body killed, so a round below zero is a defense being charged for defending.");
            }

            return bountyEarned;
        }
    }

    /// <summary>
    /// What a run was: the per-round pairs in the order they happened, and how
    /// it stopped.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A vector, not a score.</b> Health, waves survived, how it ended and
    /// every score in the game are folds over those pairs, and they are computed
    /// here as folds rather than carried alongside as running totals. That is
    /// what lets what a wave was paid, a placing or a retrospective be
    /// arithmetic over a stored outcome instead of a re-simulation.
    /// </para>
    /// <para>
    /// <b>The placing is waves survived, then health remaining.</b> The graded
    /// pool is both the resource during the run and the order at the end of it.
    /// What the offense earned is gold: it is on this vector, it is folded into
    /// <see cref="LeakCostDealt"/>, and it appears nowhere in
    /// <see cref="CompareTo"/>.
    /// </para>
    /// <para>
    /// <b>Nothing here can raise health.</b> The pool is a clock: it is fixed at
    /// the start, every round subtracts from it, and there is no member on this
    /// type or on <see cref="Run"/> that adds to it.
    /// </para>
    /// </remarks>
    public sealed class RunOutcome : IComparable<RunOutcome>
    {
        private readonly RoundOutcome[] _rounds;

        private RunOutcome(
            int healthPoolGold,
            RoundOutcome[] rounds,
            int waves,
            bool deathEndsTheRun,
            int healthRemaining,
            int wavesSurvived,
            int leakCostDealt,
            int leakCostTaken)
        {
            HealthPoolGold = healthPoolGold;
            _rounds = rounds;
            Waves = waves;
            DeathEndsTheRun = deathEndsTheRun;
            HealthRemaining = healthRemaining;
            WavesSurvived = wavesSurvived;
            LeakCostDealt = leakCostDealt;
            LeakCostTaken = leakCostTaken;

            // The ending is a fold too. A run that has spent its health has
            // ended even if waves are left, and the health test comes first
            // because a run ends the moment health reaches zero.
            if (deathEndsTheRun && healthRemaining == 0)
            {
                Ending = RunEnding.OutOfHealth;
            }
            else if (waves != Purse.RoundCapLifted && rounds.Length >= waves)
            {
                Ending = RunEnding.OutOfWaves;
            }
            else
            {
                Ending = RunEnding.Unfinished;
            }
        }

        /// <summary>The health the run started with, in gold. What every fold below is measured off.</summary>
        public int HealthPoolGold { get; }

        /// <summary>How many waves the run was set to last, or <see cref="Purse.RoundCapLifted"/>.</summary>
        public int Waves { get; }

        /// <summary>Whether reaching zero health ends this run.</summary>
        public bool DeathEndsTheRun { get; }

        /// <summary>The vector. Everything else on this type is a fold over it.</summary>
        public IReadOnlyList<RoundOutcome> Rounds => _rounds;

        /// <summary>How the run stopped, or that it has not.</summary>
        public RunEnding Ending { get; }

        /// <summary>The pool less everything taken, floored at nothing. Gold cannot put any of it back.</summary>
        public int HealthRemaining { get; }

        /// <summary>
        /// How many rounds ended with health still above zero. The round that
        /// spends the last of the pool is not one the player survived, and no
        /// round after it is either.
        /// </summary>
        public int WavesSurvived { get; }

        /// <summary>Everything the run's waves got past the fields they were sent at.</summary>
        public int LeakCostDealt { get; }

        /// <summary>Everything the fields' waves got past the run's defenses.</summary>
        public int LeakCostTaken { get; }

        /// <summary>
        /// The outcome of a run of this shape whose rounds went like this.
        /// </summary>
        /// <remarks>
        /// Public because the folds are the point: a harness holding a stored
        /// vector rebuilds the outcome here and reads health, waves survived and
        /// the ending off it without simulating a tick.
        /// </remarks>
        /// <param name="healthPoolGold">What the run started with, from the ruleset.</param>
        /// <param name="rounds">The pairs, in the order they happened.</param>
        /// <param name="waves">
        /// How many waves the run lasts, or <see cref="Purse.RoundCapLifted"/>
        /// for a run with no last wave.
        /// </param>
        /// <param name="deathEndsTheRun">Whether health reaching zero stops it.</param>
        public static RunOutcome Of(
            int healthPoolGold,
            IReadOnlyList<RoundOutcome> rounds,
            int waves,
            bool deathEndsTheRun)
        {
            if (rounds is null)
            {
                throw new ArgumentNullException(nameof(rounds));
            }

            if (healthPoolGold < 1)
            {
                throw new SimulationException(
                    "A run started on "
                    + healthPoolGold.ToString(CultureInfo.InvariantCulture)
                    + " health. The pool is a graded clock rather than a wall, so a run that begins at zero "
                    + "or below is over before its first wave and every fold over it reads as a death "
                    + "nothing caused.");
            }

            if (waves < Purse.RoundCapLifted)
            {
                throw new SimulationException(
                    "A run was folded over "
                    + waves.ToString(CultureInfo.InvariantCulture)
                    + " waves. A run lasts a whole number of them, and the cap being lifted is written as "
                    + Purse.RoundCapLifted.ToString(CultureInfo.InvariantCulture)
                    + " rather than as a negative length.");
            }

            if (waves != Purse.RoundCapLifted && rounds.Count > waves)
            {
                throw new SimulationException(
                    "A run of "
                    + waves.ToString(CultureInfo.InvariantCulture)
                    + " waves has "
                    + rounds.Count.ToString(CultureInfo.InvariantCulture)
                    + " rounds on it. A round past the last wave is a round the run had already ended "
                    + "before, and folding it in moves health and waves survived by an amount nobody "
                    + "played for.");
            }

            var copied = new RoundOutcome[rounds.Count];
            long taken = 0;
            long dealt = 0;
            int survived = 0;
            bool alive = true;

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = rounds[index];
                taken += copied[index].LeakCostTaken;
                dealt += copied[index].LeakCostDealt;

                // The pool runs down and never back up, so the rounds survived
                // are a prefix: the first round the pool cannot pay for ends the
                // count, and a no-death run keeps recording rounds past it.
                if (alive && taken < healthPoolGold)
                {
                    survived++;
                }
                else
                {
                    alive = false;
                }
            }

            return new RunOutcome(
                healthPoolGold,
                copied,
                waves,
                deathEndsTheRun,
                taken >= healthPoolGold ? 0 : healthPoolGold - (int)taken,
                survived,
                Sum(dealt, "dealt"),
                Sum(taken, "taken"));
        }

        /// <summary>
        /// Which of two runs places higher: more waves survived first, more
        /// health remaining second, and nothing else ever.
        /// </summary>
        /// <remarks>
        /// Negative means this run places above the other, so a list sorted with
        /// this comparison reads best first. The offense is deliberately absent:
        /// what a wave earns its sender is gold, and the ranking has one
        /// meaning.
        /// </remarks>
        public int CompareTo(RunOutcome? other)
        {
            if (other is null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            if (WavesSurvived != other.WavesSurvived)
            {
                return WavesSurvived > other.WavesSurvived ? -1 : 1;
            }

            if (HealthRemaining != other.HealthRemaining)
            {
                return HealthRemaining > other.HealthRemaining ? -1 : 1;
            }

            return 0;
        }

        public override string ToString() =>
            WavesSurvived.ToString(CultureInfo.InvariantCulture)
            + " waves survived, "
            + HealthRemaining.ToString(CultureInfo.InvariantCulture)
            + " of "
            + HealthPoolGold.ToString(CultureInfo.InvariantCulture)
            + " health left, "
            + LeakCostDealt.ToString(CultureInfo.InvariantCulture)
            + " dealt over "
            + _rounds.Length.ToString(CultureInfo.InvariantCulture)
            + " rounds";

        /// <summary>A fold over the vector, refused if it has left the range a purse is kept in.</summary>
        private static int Sum(long total, string what)
        {
            if (total > int.MaxValue)
            {
                throw new SimulationException(
                    "A run's rounds add up to "
                    + total.ToString(CultureInfo.InvariantCulture)
                    + " in leak cost "
                    + what
                    + ", which does not fit in the 32-bit integer gold is counted in.");
            }

            return (int)total;
        }
    }
}
