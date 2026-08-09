using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One run: N waves, a build phase before each, every round resolved against
    /// a field of K opponents, against a health pool denominated in gold.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One surface, every scenario</b> -- the same claim <see cref="Match"/>
    /// makes, one level up. Construct from the map, the rules, the unit table,
    /// the shape, the pool a field is drawn from, a seed, N, K and whether death
    /// ends it; hand <see cref="Advance"/> what the build phase decided; read the
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
    /// <b>Health is denominated in gold and cannot be repaired.</b> A leaked
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
    /// <para>
    /// <b>The filling is one such draw, and nothing is keyed on what it drew.</b>
    /// Which game changers reach each anchor's menu is drawn once, at run start,
    /// from a position of its own; the field is still drawn from the run and the
    /// round alone.
    /// </para>
    /// </remarks>
    public sealed class Run
    {
        /// <summary>How many waves a run lasts unless the caller says otherwise.</summary>
        public const int DefaultWaves = 10;

        /// <summary>How many opponents a round is resolved against unless the caller says otherwise.</summary>
        public const int DefaultFieldSize = 10;

        /// <summary>
        /// How many of the pool's own rounds are played to measure what a round
        /// of it is worth. Ten samples put the percentiles on the deciles, which
        /// is the granularity four bands at 0, 50, 75 and 90 can use; it is a
        /// number about the measurement and not about the run, so it moves with
        /// the bands rather than with N.
        /// </summary>
        public const int FieldSamples = 10;

        /// <summary>
        /// Names the derivation of a round's field draw. The digit bumps when
        /// what goes into it changes, which is what stops two schemes producing
        /// two different runs under one seed and one record.
        /// </summary>
        private const string FieldLabel = "run-field/1";

        /// <summary>Names the derivation of one pairing's match seed.</summary>
        private const string MatchLabel = "run-match/1";

        /// <summary>
        /// Names the derivation of which member of the pool one sample of the
        /// field's own worth is taken from. Its own position rather than a walk
        /// down the pool, so that a population wider than the sample count is
        /// sampled rather than truncated at its first members.
        /// </summary>
        private const string MeasureLabel = "run-measure/1";

        /// <summary>
        /// Names the derivation of this run's filling: which game changers sit
        /// on each anchor's menu. Drawn once, at run start.
        /// </summary>
        private const string FillingLabel = "run-filling/1";

        /// <summary>
        /// Names the derivation of one round's public offering. Drawn fresh
        /// every round, from the run's seed and the wave alone -- which is what
        /// lets everybody in a match be handed the same one.
        /// </summary>
        private const string OfferingLabel = "run-offering/1";

        private readonly HexMap _map;

        private readonly FieldPool _pool;

        /// <summary>The vector. Every number this run reports is a fold over it.</summary>
        private readonly List<RoundOutcome> _rounds = new List<RoundOutcome>();

        private readonly List<RoundOrders> _sent = new List<RoundOrders>();

        private RunOutcome _outcome;

        private PerformanceField? _field;

        /// <summary>
        /// Builds a run. Everything it will ever know arrives here: nothing in
        /// this assembly can open a file, read a clock or ask the machine
        /// anything.
        /// </summary>
        /// <param name="map">The board every match in the run is fought on.</param>
        /// <param name="rules">The health pool, the interest, the base and the bands.</param>
        /// <param name="types">The unit table every cost in the run is priced out of.</param>
        /// <param name="schedule">
        /// The shape: which waves are anchors, how wide each round's slots are,
        /// and which tier pool each anchor's menu is filled from.
        /// </param>
        /// <param name="pool">
        /// The population a round's field of K is drawn from, and the one the
        /// performance bonus is measured against. See <see cref="Field"/>.
        /// </param>
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
            AnchorSchedule schedule,
            FieldPool pool,
            ulong seed,
            int waves = DefaultWaves,
            int fieldSize = DefaultFieldSize,
            bool deathEndsTheRun = true)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Types = types ?? throw new ArgumentNullException(nameof(types));
            Schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));

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
            Purse = Purse.Holding(rules.StartingPurseGold);
            Unlocks = Unlocks.None;

            // Revealed at run start: the shape was public all week and the
            // filling is what this run drew onto it.
            Filling = schedule.Fill(rules.GameChangersPerAnchor, Derived(FillingLabel, 0, 0, 0));

            _outcome = Folded(_rounds);
        }

        /// <summary>The seed every draw in this run is derived from.</summary>
        public ulong Seed { get; }

        /// <summary>
        /// The health pool, the interest, the base, the bands and the damage
        /// matrix every round of this run is resolved under. Held rather than
        /// only consumed, so that whatever checks a stored record against this
        /// run reads the tables the run is actually playing.
        /// </summary>
        public Ruleset Rules { get; }

        /// <summary>The unit table every creep and every cost in this run is read out of.</summary>
        public UnitTypeTable Types { get; }

        /// <summary>
        /// The distribution every wave of this run is paid against: what a round
        /// of the pool is worth, in leak cost dealt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It comes out of the pool, so swapping the canned stand-in for a
        /// real ghost pool is the pool argument and nothing else.</b> The pool is
        /// the population the bands are measured against, and measuring it is
        /// <see cref="MeasureField"/> -- a run cannot be handed a pool and a
        /// distribution that disagree, because there is only the one of them.
        /// </para>
        /// <para>
        /// <b>Fixed for the whole run.</b> Every round of the run is placed
        /// against the same spread, which is what makes what a round paid
        /// arithmetic over the outcome vector -- see
        /// <see cref="Purse.BonusOver"/>.
        /// </para>
        /// <para>
        /// Measured on first use rather than in the constructor: measuring plays
        /// matches, and a caller that only wants to read this run's offerings
        /// should not pay for them. What it measures depends on the seed, the
        /// pool and K alone, so when it happens cannot change what it says.
        /// </para>
        /// </remarks>
        public PerformanceField Field => _field ??= MeasureField();

        /// <summary>N, or <see cref="Purse.RoundCapLifted"/> for a run with no last wave.</summary>
        public int Waves { get; }

        /// <summary>K: how many opponents each round is resolved against.</summary>
        public int FieldSize { get; }

        /// <summary>Whether health reaching zero stops this run.</summary>
        public bool DeathEndsTheRun { get; }

        /// <summary>What everything in this run is priced out of, units and snapshots alike.</summary>
        public CostTable Costs { get; }

        /// <summary>
        /// The shape this run is played against: where the anchors are, what
        /// answers each, and how wide a round's slots are.
        /// </summary>
        public AnchorSchedule Schedule { get; }

        /// <summary>
        /// What this run drew onto each anchor's menu, at a position derived from
        /// the seed and revealed here at run start.
        /// </summary>
        public AnchorFilling Filling { get; }

        /// <summary>
        /// The one wallet. Every wave pays it interest on what was banked, the
        /// flat base, and the band its result reached in the <see cref="Field"/>
        /// on top of that.
        /// </summary>
        public Purse Purse { get; private set; }

        /// <summary>
        /// What this run may field. Every build phase takes one thing off the
        /// offering and it is held for the rest of the run, free to unlock and
        /// paid to buy.
        /// </summary>
        public Unlocks Unlocks { get; private set; }

        /// <summary>How many rounds have resolved.</summary>
        public int Round => _rounds.Count;

        /// <summary>
        /// The offering standing in front of the round about to be played.
        /// Waves are counted from one, so it is the round after the ones that
        /// have resolved.
        /// </summary>
        public Offering Offering => OfferingAt(Round + 1);

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
        /// <para>
        /// <b>Nothing moves until everything that can refuse has refused.</b>
        /// The whole round is worked out into locals -- see <see cref="Play"/> --
        /// and reaches the run through <see cref="Commit"/>, which writes
        /// everything a round moves, together, and is the only place any of it
        /// is written. A throw anywhere in a round therefore leaves the run
        /// exactly where it was, structurally rather than by the order the
        /// statements happen to be in.
        /// </para>
        /// </remarks>
        /// <param name="orders">The defense that stands and the wave that is sent.</param>
        public RoundOutcome Advance(RoundOrders orders)
        {
            if (orders is null)
            {
                throw new ArgumentNullException(nameof(orders));
            }

            RequireUnfinished();

            return Play(orders, Unlocks, Purse);
        }

        /// <summary>
        /// Resolves one round from the decision a build phase made rather than
        /// from orders somebody composed by hand.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The same round, entered a step earlier. The decision is checked
        /// against this round's offering, this run's unlocks, this round's slot
        /// width and this run's purse -- by
        /// <see cref="BuildPhase.Resolve(Offering, Unlocks, Purse, CostTable)"/>,
        /// which is the surface a stored command stream is validated against
        /// too, so there is one implementation of the rules and not two.
        /// </para>
        /// <para>
        /// The defense arrives beside it because a build phase composes what is
        /// sent; what stands is the other half of a round's orders.
        /// </para>
        /// <para>
        /// <b>Everything that can refuse this round refuses before a coin
        /// moves</b> -- the decision against the offering, the orders against
        /// the defense, the run against being over, and then everything the
        /// round itself can refuse at. What the decision unlocked and what it
        /// left in the purse travel into <see cref="Play"/> as arguments and
        /// reach the run through the same <see cref="Commit"/> the other route
        /// uses, so a purse spent and an unlock taken for a wave nobody was in
        /// the run to send is a state that cannot be reached rather than an
        /// ordering to get right.
        /// </para>
        /// </remarks>
        /// <param name="phase">What this round took, and how it filled its slots.</param>
        /// <param name="defense">What stands against every wave the field sends this round.</param>
        public RoundOutcome Advance(BuildPhase phase, TowerLayout defense)
        {
            if (phase is null)
            {
                throw new ArgumentNullException(nameof(phase));
            }

            Build build = phase.Resolve(Offering, Unlocks, Purse, Costs);
            RoundOrders orders = RoundOrders.Of(defense, build.Wave);

            RequireUnfinished();

            return Play(orders, build.Unlocks, build.Purse);
        }

        /// <summary>
        /// The public offering that stood in front of a wave of this run.
        /// </summary>
        /// <remarks>
        /// Derived rather than remembered, so any wave's offering can be drawn
        /// at any time -- which is what a stored command stream needs to be
        /// validated against without the run in front of it having been played.
        /// </remarks>
        public Offering OfferingAt(int wave) =>
            Sim.Offering.Draw(Rules, Types, Schedule, Filling, wave, Derived(OfferingLabel, wave, 0, 0));

        /// <summary>
        /// Works one round out in full, and commits it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The unlocks and the purse arrive as arguments.</b> A build phase's
        /// decision governs the round it was made in -- the creep it just
        /// unlocked is fielded in this round's wave, and the wave is bought out
        /// of this round's purse -- and passing them is what lets that happen
        /// without either having been written to the run first.
        /// </para>
        /// <para>
        /// <b>Nothing above the commit moves the run.</b> Measuring the field,
        /// playing the matches, composing the round's pair, closing the purse
        /// and folding the outcome can each refuse, and a run left holding any
        /// part of a round that refused is a run nobody could tell from one
        /// somebody played. The measured field is the one thing written above
        /// the commit, and it is a memo of a number no round can move -- see
        /// <see cref="Field"/>.
        /// </para>
        /// </remarks>
        /// <param name="orders">The defense that stands and the wave that is sent.</param>
        /// <param name="unlocks">What the run may field this round, this round's take included.</param>
        /// <param name="purse">What the round carries into the wave, after whatever it bought.</param>
        private RoundOutcome Play(RoundOrders orders, Unlocks unlocks, Purse purse)
        {
            // Measured ahead of the round's own matches: what a round of the
            // pool is worth depends on the seed, the pool and K, so the round
            // being played cannot move it, and a measurement that refuses
            // refuses before a single match of this round is resolved.
            PerformanceField field = Field;

            int round = _rounds.Count;
            int[] drawn = FieldFor(round);
            long dealt = 0;
            long taken = 0;

            for (int index = 0; index < drawn.Length; index++)
            {
                RoundOrders against = _pool.At(drawn[index]);

                dealt += LeakCost(orders.Wave, against.Defense, unlocks, round, index, Side.Attacking);
                taken += LeakCost(against.Wave, orders.Defense, unlocks, round, index, Side.Defending);
            }

            // The average rather than the sum, on both sides. Summed, one round
            // against ten opponents would cost ten rounds' worth of health, and
            // a field would be a punishment for being in one.
            var outcome = new RoundOutcome((int)(dealt / drawn.Length), (int)(taken / drawn.Length));

            // Interest, the flat base, and the band this round's offense reached
            // in the field on top. Nothing is taken off anybody to pay it: the
            // wave is placed against the spread of what a round of the pool is
            // worth, not against whichever opponent it was drawn against.
            Purse closed = purse.CloseWave(Rules, field, outcome.LeakCostDealt).Purse;

            return Commit(orders, outcome, unlocks, closed, FoldedWith(outcome));
        }

        /// <summary>
        /// The one place a round is written to the run.
        /// </summary>
        /// <remarks>
        /// Every field a round moves moves here, from arguments that are already
        /// settled, and nothing between the first write and the last can refuse.
        /// That is the whole of the guarantee both ways into a round make: it is
        /// a property of where the writes are rather than of what order the work
        /// above them happens in.
        /// </remarks>
        private RoundOutcome Commit(
            RoundOrders orders,
            RoundOutcome outcome,
            Unlocks unlocks,
            Purse purse,
            RunOutcome folded)
        {
            _rounds.Add(outcome);
            _sent.Add(orders);
            Unlocks = unlocks;
            Purse = purse;
            _outcome = folded;

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
            var dice = new Pcg32(FieldSeed(round));
            var drawn = new int[FieldSize];

            for (int index = 0; index < drawn.Length; index++)
            {
                drawn[index] = (int)dice.NextBelow((uint)_pool.Size);
            }

            return drawn;
        }

        /// <summary>
        /// What a round of the pool is worth, measured by playing the pool's own
        /// rounds: <see cref="FieldSamples"/> of them, each sent at the field
        /// that sample's draw put in front of it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Each sample is the average of its K resolutions, as a round's own
        /// score is.</b> A percentile compares one number against a spread of
        /// numbers, so the two sides have to be the same measurement: scoring an
        /// averaged round against single matches would widen the field's tails
        /// and pin every honest run to the middle band.
        /// </para>
        /// <para>
        /// <b>Only the offense is resolved.</b> What the bands are measured
        /// against is leak cost dealt, so what the pool's rounds would have taken
        /// back is never played -- the measurement costs half of what a round
        /// costs rather than all of it. What it sends carries no game changer,
        /// for the reason the defending direction's does not: the pool is stored
        /// orders rather than stored runs, and nothing in it says which of its
        /// bodies was one.
        /// </para>
        /// <para>
        /// <b>Both the member being measured and the field it meets are drawn.</b>
        /// A walk down the pool would sample a population wider than
        /// <see cref="FieldSamples"/> by truncating it at its first members. The
        /// field a sample meets is the field the round of the same index meets,
        /// which is what makes the spread this comes back with the spread of the
        /// opponents this run will actually be scored against.
        /// </para>
        /// <para>
        /// A pool thinner than K is not a thin measurement. The draw is with
        /// replacement, so one canned opponent sampled ten times is ten matches
        /// on ten derived seeds, and the spread between them is the spread of
        /// what that round is worth.
        /// </para>
        /// </remarks>
        private PerformanceField MeasureField()
        {
            var worth = new int[FieldSamples];
            var dice = new Pcg32(Derived(MeasureLabel, 0, 0, 0));

            for (int sample = 0; sample < worth.Length; sample++)
            {
                RoundOrders member = _pool.At((int)dice.NextBelow((uint)_pool.Size));
                int[] field = FieldFor(sample);
                long dealt = 0;

                for (int index = 0; index < field.Length; index++)
                {
                    dealt += LeakCost(
                        member.Wave,
                        _pool.At(field[index]).Defense,
                        Unlocks.None,
                        sample,
                        index,
                        Side.Measured);
                }

                worth[sample] = (int)(dealt / field.Length);
            }

            return PerformanceField.Of(worth);
        }

        /// <summary>
        /// What one match let through, priced. A leaked creep costs what it cost
        /// to send, one for one, so what got past is the wave's own orders read
        /// off the cost table.
        /// </summary>
        private int LeakCost(
            WaveScript wave,
            TowerLayout defense,
            Unlocks unlocks,
            int round,
            int opponent,
            Side side)
        {
            // Only this run's own wave can carry a game changer anybody here
            // knows about: what is fielded is a fact about the sender's
            // unlocks, and the pool is stored orders rather than stored runs,
            // so nothing coming the other way says which of its bodies was one.
            ShotBonus bonuses = side == Side.Attacking
                ? ShotBonus.Fielded(wave, defense, unlocks, Schedule)
                : ShotBonus.None;

            var match = new Match(_map, Rules, defense, wave, MatchSeed(round, opponent, side), bonuses);
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
                    + " gold past, which does not fit in the 32-bit integer health and gold are both "
                    + "counted in. A wave that costs more than a purse can hold is a cost column that was "
                    + "authored in the wrong units.");
            }

            return (int)cost;
        }

        /// <summary>
        /// Refuses a round past the end of a run. One implementation, called
        /// from both ways in, so that entering a round from a build phase
        /// cannot reach a run the other way in would have turned away.
        /// </summary>
        private void RequireUnfinished()
        {
            if (!IsOver)
            {
                return;
            }

            throw new SimulationException(
                "This run is over: "
                + Round.ToString(CultureInfo.InvariantCulture)
                + " rounds resolved and "
                + Health.ToString(CultureInfo.InvariantCulture)
                + " of "
                + Rules.HealthPoolGold.ToString(CultureInfo.InvariantCulture)
                + " health left. A round resolved past the end of a run is a round nobody was still in the "
                + "run to play, and folding it in moves an outcome that had already been settled.");
        }

        /// <summary>Where a round's field is drawn from.</summary>
        private ulong FieldSeed(int round) => Derived(FieldLabel, round, 0, 0);

        /// <summary>
        /// What one pairing's match is seeded with. Derived from the pairing
        /// rather than from who was drawn into it, so widening a field adds
        /// matches without moving the ones already there.
        /// </summary>
        private ulong MatchSeed(int round, int opponent, Side side) =>
            Derived(MatchLabel, round, opponent, (int)side);

        /// <summary>
        /// A stream position derived from the run's seed and from where in the
        /// run it is wanted, rather than taken from wherever the previous draw
        /// happened to leave a stream. The label names the purpose, so two draws
        /// at the same coordinates cannot collide; its digit is the layout of
        /// what follows.
        /// </summary>
        private ulong Derived(string purpose, int round, int opponent, int side) =>
            Hash64.Start(purpose)
                .Add(unchecked((long)Seed))
                .Add(round, opponent)
                .Add(side)
                .Value;

        /// <summary>A vector, folded. The only place health and the ending come from.</summary>
        private RunOutcome Folded(IReadOnlyList<RoundOutcome> rounds) =>
            RunOutcome.Of(Rules.HealthPoolGold, rounds, Waves, DeathEndsTheRun);

        /// <summary>
        /// The fold a round would leave behind, taken over a vector of its own.
        /// </summary>
        /// <remarks>
        /// The run's vector is not appended to, because the fold refuses a total
        /// that has left the range gold is counted in -- so a round that cannot
        /// be folded has to be a round that was never added.
        /// </remarks>
        private RunOutcome FoldedWith(RoundOutcome round) =>
            Folded(new List<RoundOutcome>(_rounds) { round });

        /// <summary>
        /// Which pairing a match is: a round measures both directions against
        /// every opponent and the field is measured against itself, and no two of
        /// the three may share a seed.
        /// </summary>
        private enum Side
        {
            /// <summary>This round's wave against their defense. The direction that scores.</summary>
            Attacking = 0,

            /// <summary>Their wave against this round's defense. The direction that costs health.</summary>
            Defending = 1,

            /// <summary>
            /// One of the pool's own waves against the pool. Neither direction of
            /// this run's round: it is what a round of the field is worth, which
            /// is the spread the bands are read off.
            /// </summary>
            Measured = 2,
        }
    }
}
