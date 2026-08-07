using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One run: N waves, a build phase before each, every round resolved against
    /// a field of K opponents, against a health pool denominated in sauce.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One surface, every scenario</b> -- the same claim <see cref="Match"/>
    /// makes, one level up. Construct from the map, the rules, the unit table,
    /// the pool a field is drawn from, a seed, N, K and whether death ends it;
    /// hand <see cref="Advance"/> what the build phase decided; read the
    /// <see cref="Outcome"/>. Normal play, a sweep row, a no-death harness run
    /// and a server re-validating a submitted run are those calls with different
    /// arguments. <b>None of them is a mode, a flag or a branch.</b>
    /// </para>
    /// <para>
    /// <b>N, K and death are parameters and not constants.</b> Ten waves and ten
    /// opponents are this map's answers and both are expected to move; death is
    /// an argument so that a sweep can run without it and always get N rounds of
    /// data out of every row rather than a short row wherever a build failed.
    /// </para>
    /// <para>
    /// <b>Health is denominated in sauce and cannot be repaired.</b> A leaked
    /// creep costs its price one for one, so underbuilding a defense to fund an
    /// offense <i>is</i> spending health, and the exchange rate is legible
    /// without a table. What a round costs is the field's <b>average</b> rather
    /// than its sum, so ten opponents' leaks do not kill everybody at once. The
    /// only member on this type that moves anything is <see cref="Advance"/>,
    /// and it only ever subtracts.
    /// </para>
    /// <para>
    /// <b>The outcome is a vector and everything else is a fold over it.</b>
    /// Health, waves survived, how the run ended and any score come out of
    /// <see cref="RunOutcome"/> rather than being carried alongside as running
    /// totals -- which is what lets a percentile band be computed later without
    /// re-simulating a thing.
    /// </para>
    /// <para>
    /// <b>Every run-level draw is at a derived position.</b> The damage roll
    /// stays the only randomness inside a match, so a match's stream position
    /// stays a running count of shots fired; a round's field is drawn from a
    /// stream started at a position derived from the run's seed and the round.
    /// Round seven's field is therefore the same whatever rounds one to six did,
    /// which is what makes a run reproducible from its record.
    /// </para>
    /// </remarks>
    public sealed class Run
    {
        /// <summary>How many waves a run lasts unless the caller says otherwise.</summary>
        public const int DefaultWaves = 10;

        /// <summary>How many opponents a round is resolved against unless the caller says otherwise.</summary>
        public const int DefaultFieldSize = 10;

        /// <summary>
        /// Names the derivation of a round's field draw. The digit bumps when
        /// what goes into it changes, which is what stops two schemes producing
        /// two different runs under one seed and one record.
        /// </summary>
        private const string FieldLabel = "run-field/1";

        /// <summary>Names the derivation of one pairing's match seed.</summary>
        private const string MatchLabel = "run-match/1";

        /// <summary>My wave against their defense: the direction that scores.</summary>
        private const int Attacking = 0;

        /// <summary>Their wave against my defense: the direction that costs health.</summary>
        private const int Defending = 1;

        private readonly HexMap _map;

        private readonly Ruleset _rules;

        private readonly FieldPool _pool;

        /// <summary>The vector. Every number this run reports is a fold over it.</summary>
        private readonly List<RoundOutcome> _rounds = new List<RoundOutcome>();

        private readonly List<RoundOrders> _sent = new List<RoundOrders>();

        private RunOutcome _outcome;

        /// <summary>
        /// Builds a run. Everything it will ever know arrives here: nothing in
        /// this assembly can open a file, read a clock or ask the machine
        /// anything.
        /// </summary>
        /// <param name="map">The board every match in the run is fought on.</param>
        /// <param name="rules">The health pool, the interest, the base and the bands.</param>
        /// <param name="types">The unit table every cost in the run is priced out of.</param>
        /// <param name="pool">The population a round's field of K is drawn from.</param>
        /// <param name="seed">The one seed every draw in the run is derived from.</param>
        /// <param name="waves">
        /// N. <see cref="Purse.RoundCapLifted"/> for a run with no last wave,
        /// which a ruleset with no interest ceiling refuses.
        /// </param>
        /// <param name="fieldSize">K, the number of opponents a round is resolved against.</param>
        /// <param name="deathEndsTheRun">Whether health reaching zero stops it.</param>
        public Run(
            HexMap map,
            Ruleset rules,
            UnitTypeTable types,
            FieldPool pool,
            ulong seed,
            int waves = DefaultWaves,
            int fieldSize = DefaultFieldSize,
            bool deathEndsTheRun = true)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            // Before a wave resolves rather than at the first overflow: a run
            // that has already produced numbers is a run whose numbers somebody
            // is going to keep.
            Purse.RequireBoundedCompounding(rules, waves);

            if (fieldSize < 1)
            {
                throw new SimulationException(
                    "A run was given a field of "
                    + fieldSize.ToString(CultureInfo.InvariantCulture)
                    + " opponents. What a round costs is the average of what the field did, and there is no "
                    + "average of nothing -- a field of one is a run against a single opponent, which is a "
                    + "smaller number and not a missing one.");
            }

            if (waves == Purse.RoundCapLifted && !deathEndsTheRun)
            {
                throw new SimulationException(
                    "This run has no last wave and death does not end it, so no round in it can ever be the "
                    + "last. A run is bounded by its wave count or by its health pool, and a sweep that "
                    + "lifts both is a loop rather than a row.");
            }

            Seed = seed;
            Waves = waves;
            FieldSize = fieldSize;
            DeathEndsTheRun = deathEndsTheRun;
            Costs = CostTable.From(rules, types);
            Purse = Purse.Empty;

            _outcome = Folded();
        }

        /// <summary>The seed every draw in this run is derived from.</summary>
        public ulong Seed { get; }

        /// <summary>N, or <see cref="Purse.RoundCapLifted"/> for a run with no last wave.</summary>
        public int Waves { get; }

        /// <summary>K: how many opponents each round is resolved against.</summary>
        public int FieldSize { get; }

        /// <summary>Whether health reaching zero stops this run.</summary>
        public bool DeathEndsTheRun { get; }

        /// <summary>What everything in this run is priced out of, units and snapshots alike.</summary>
        public CostTable Costs { get; }

        /// <summary>
        /// The one wallet. Every wave pays it interest on what was banked plus
        /// the flat base; the bonus waits on a field to be measured against, and
        /// pays nothing until there is one.
        /// </summary>
        public Purse Purse { get; private set; }

        /// <summary>How many rounds have resolved.</summary>
        public int Round => _rounds.Count;

        /// <summary>
        /// What this run stood and sent, round by round. Stored unconditionally,
        /// in every configuration, because this is what enters somebody else's
        /// field later: symmetry across a field of ten is restored across time
        /// rather than within a round.
        /// </summary>
        public IReadOnlyList<RoundOrders> Sent => _sent;

        /// <summary>The vector, and the folds over it. Rebuilt after every round.</summary>
        public RunOutcome Outcome => _outcome;

        /// <summary>What is left of the pool. A fold, and never anything else.</summary>
        public int Health => _outcome.HealthRemaining;

        /// <summary>How the run stopped, or that it has not.</summary>
        public RunEnding Ending => _outcome.Ending;

        /// <summary>Whether the run is over, by either of the two ways it can be.</summary>
        public bool IsOver => _outcome.Ending != RunEnding.Unfinished;

        /// <summary>
        /// Resolves one round: the build phase's decision, then the wave.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The argument is the build phase slot.</b> A round is one decision
        /// plus the wave that resolves it, and the decision reaches the
        /// simulation as a value handed in here and by no other route.
        /// </para>
        /// <para>
        /// K opponents are drawn, and each is fought in both directions: this
        /// round's wave against their defense, and their wave against this
        /// round's defense. What comes back is the average of each side's K
        /// resolutions -- the average and not the best, symmetrically with the
        /// damage rule, so the ladder rewards robust play.
        /// </para>
        /// </remarks>
        /// <param name="orders">The defense that stands and the wave that is sent.</param>
        public RoundOutcome Advance(RoundOrders orders)
        {
            if (orders is null)
            {
                throw new ArgumentNullException(nameof(orders));
            }

            if (IsOver)
            {
                throw new SimulationException(
                    "This run is over: "
                    + Round.ToString(CultureInfo.InvariantCulture)
                    + " rounds resolved and "
                    + Health.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + _rules.HealthPoolSauce.ToString(CultureInfo.InvariantCulture)
                    + " health left. A round resolved past the end of a run is a round nobody was still in "
                    + "the run to play, and folding it in moves an outcome that had already been settled.");
            }

            int round = _rounds.Count;
            int[] field = FieldFor(round);
            long dealt = 0;
            long taken = 0;

            for (int index = 0; index < field.Length; index++)
            {
                RoundOrders against = _pool.At(field[index]);

                dealt += LeakCost(orders.Wave, against.Defense, round, index, Attacking);
                taken += LeakCost(against.Wave, orders.Defense, round, index, Defending);
            }

            // The average rather than the sum, on both sides. Summed, one round
            // against ten opponents would cost ten rounds' worth of health, and
            // a field would be a punishment for being in one.
            var outcome = new RoundOutcome((int)(dealt / field.Length), (int)(taken / field.Length));

            _rounds.Add(outcome);
            _sent.Add(orders);

            // The bonus is a percentile of a field of other players' rounds, and
            // no such pool exists yet, so every wave is paid the base alone. See
            // PerformanceField.Absent, which is where that is written down.
            Purse = Purse.CloseWave(_rules, PerformanceField.Absent, outcome.LeakCostDealt).Purse;

            _outcome = Folded();

            return outcome;
        }

        /// <summary>
        /// Which K of the pool this round is fought against.
        /// </summary>
        /// <remarks>
        /// One stream, started at a position derived from the run's seed and the
        /// round rather than continued from wherever the last draw left it. Two
        /// things follow, and both are the point: round seven's field does not
        /// depend on what rounds one to six did, so a run is reproducible from
        /// its record and a server can re-validate one round of it; and no draw
        /// of any kind happens inside a match, so a match's stream position stays
        /// a running count of the shots fired in it.
        /// </remarks>
        private int[] FieldFor(int round)
        {
            var dice = new Pcg32(Derived(FieldLabel, round, 0, 0));
            var drawn = new int[FieldSize];

            for (int index = 0; index < drawn.Length; index++)
            {
                drawn[index] = (int)dice.NextBelow((uint)_pool.Size);
            }

            return drawn;
        }

        /// <summary>
        /// What one match let through, priced. A leaked creep costs what it cost
        /// to send, one for one, so what got past is the wave's own orders read
        /// off the cost table.
        /// </summary>
        private int LeakCost(WaveScript wave, TowerLayout defense, int round, int opponent, int side)
        {
            var match = new Match(_map, defense, wave, Derived(MatchLabel, round, opponent, side));
            match.Resolve();

            IReadOnlyList<int> leaked = match.LeakedByOrder;
            long cost = 0;

            for (int index = 0; index < leaked.Count; index++)
            {
                cost += Costs.PriceOf(Purchase.Unit(wave.Orders[index].TypeId), leaked[index]);
            }

            if (cost > int.MaxValue)
            {
                throw new SimulationException(
                    "One match let "
                    + cost.ToString(CultureInfo.InvariantCulture)
                    + " sauce past, which does not fit in the 32-bit integer health and sauce are both "
                    + "counted in. A wave that costs more than a purse can hold is a cost column that was "
                    + "authored in the wrong units.");
            }

            return (int)cost;
        }

        /// <summary>
        /// A stream position derived from the run's seed and from where in the
        /// run it is wanted, rather than taken from wherever the previous draw
        /// happened to leave a stream.
        /// </summary>
        private ulong Derived(string purpose, int round, int opponent, int side) =>
            Hash64.Start(purpose)
                .Add(unchecked((long)Seed))
                .Add(round, opponent)
                .Add(side)
                .Value;

        /// <summary>The vector, folded. The only place health and the ending come from.</summary>
        private RunOutcome Folded() =>
            RunOutcome.Of(_rules.HealthPoolSauce, _rounds, Waves, DeathEndsTheRun);
    }
}
