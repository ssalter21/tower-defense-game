using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// What one round came to: the pair it resolved to, the decision it was
    /// played from, and what its wave paid the purse.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three answers, handed back together, because a round settles all
    /// three at once.</b> The pair is what got past whom. The build is what the
    /// round built and what the wave cost to buy. The payment is what
    /// the wave earned, itemised into the interest, the base and the bonus. Each
    /// is worked out once, while the round is being played, so nothing holding
    /// this has to resolve a decision a second time or price a wave again to
    /// find out what a round did.
    /// </para>
    /// <para>
    /// <b>What a run's economics are is a walk over these.</b> Round by round:
    /// what was spent, what came back and which line it came back on.
    /// </para>
    /// </remarks>
    public sealed class RoundReport
    {
        internal RoundReport(RoundOutcome outcome, Build build, WavePayment payment, FieldDraw field)
        {
            Outcome = outcome;
            Build = build;
            Payment = payment;
            Field = field;
        }

        /// <summary>What this round's wave got past the field, and what the field got past it.</summary>
        public RoundOutcome Outcome { get; }

        /// <summary>What the build phase took, what its wave cost, and what it left in the purse.</summary>
        public Build Build { get; }

        /// <summary>What the wave paid the purse, line by line, and the purse afterwards.</summary>
        public WavePayment Payment { get; }

        /// <summary>
        /// The K opponents this round met: which of the stage's stored rounds
        /// were drawn, and how many slots the stand-in filled.
        /// </summary>
        public FieldDraw Field { get; }

        public override string ToString() =>
            Outcome.ToString()
            + ", "
            + Build.Board.Count.ToString(CultureInfo.InvariantCulture)
            + " towers standing, spent "
            + Build.Spent.ToString(CultureInfo.InvariantCulture)
            + ", paid "
            + Payment.ToString();
    }

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
    /// <b>Every run opens on an empty board.</b> There is no opening defense to
    /// hand in: what stands is what this run's own build phases put there, so
    /// the first build phase is a decision rather than a position somebody else
    /// composed. See <c>docs/adr/0048-a-board-is-not-a-layout.md</c>.
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
    /// Health, waves survived, how the run ended, any score and what every wave
    /// was paid come out of <see cref="RunOutcome"/> rather than being carried
    /// alongside as running totals -- which is what lets a placing or a
    /// retrospective be computed later without re-simulating a thing.
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
        /// of it is worth. Ten samples put the percentiles on the deciles; it is
        /// a number about the measurement and not about the run, so it moves
        /// with whatever reads the measurement rather than with N.
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
        /// <param name="rules">The health pool, the interest, the base and the bonus rate.</param>
        /// <param name="types">The unit table every cost in the run is priced out of.</param>
        /// <param name="ladder">
        /// The upgrade edges. A unit that is some edge's target cannot be
        /// placed and has to be reached by upgrading the rung below it, which is
        /// the one prerequisite a run enforces.
        /// </param>
        /// <param name="pool">
        /// The population a round's field of K is drawn from -- the rounds
        /// stored at each stage, and the stand-in that fills a stage holding
        /// fewer than K. See <see cref="Field"/>.
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
            UpgradeLadder ladder,
            FieldPool pool,
            ulong seed,
            int waves = DefaultWaves,
            int fieldSize = DefaultFieldSize,
            bool deathEndsTheRun = true)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
            Board = Board.Empty;
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Types = types ?? throw new ArgumentNullException(nameof(types));
            Ladder = ladder ?? throw new ArgumentNullException(nameof(ladder));

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

            _outcome = Folded(_rounds);
        }

        /// <summary>The seed every draw in this run is derived from.</summary>
        public ulong Seed { get; }

        /// <summary>
        /// The map every match in this run is fought on. Held rather than only
        /// consumed, for the reason <see cref="Rules"/> is: whatever checks a
        /// decision against this run has to ask the map this run is playing
        /// whether a cell is one a tower could stand on.
        /// </summary>
        public HexMap Map { get; }

        /// <summary>
        /// What this run has standing on the map. It opens empty, every round
        /// derives its layout from here rather than being handed one, and every
        /// build phase acts on it and hands back what it left.
        /// </summary>
        public Board Board { get; private set; }

        /// <summary>
        /// The health pool, the interest, the base, the bonus rate and the damage
        /// matrix every round of this run is resolved under. Held rather than
        /// only consumed, so that whatever checks a stored record against this
        /// run reads the tables the run is actually playing.
        /// </summary>
        public Ruleset Rules { get; }

        /// <summary>The unit table every creep and every cost in this run is read out of.</summary>
        public UnitTypeTable Types { get; }

        /// <summary>
        /// What a round of the pool is worth, in leak cost dealt: the spread of
        /// the opponents this run is scored against.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Nothing in this build prices anything off it.</b> A wave is paid a
        /// share of what it dealt, so the payment reads no distribution and no
        /// rank; this is a measurement of the pool that no consumer currently
        /// asks for. See
        /// <c>docs/adr/0042-the-field-is-measured-off-the-pool.md</c>, which the
        /// proportional bonus largely supersedes and which is open for a
        /// decision on whether the measurement is kept.
        /// </para>
        /// <para>
        /// <b>It comes out of the pool, so the stored rounds and the stand-in
        /// alike are in it.</b> Measuring
        /// it is <see cref="MeasureField"/> -- a run cannot be handed a pool and
        /// a distribution that disagree, because there is only the one of them.
        /// </para>
        /// <para>
        /// Measured on first use rather than in the constructor: measuring plays
        /// matches, and a caller that never asks what this run's field can do
        /// should not pay for the answer -- which, with nothing pricing off it,
        /// is every caller a played run has. What it measures depends on the
        /// seed, the pool and K alone, so when it happens cannot change what it
        /// says.
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
        /// The upgrade edges this run's build phases are checked against. A unit
        /// that is some edge's target is refused to <c>place</c> and reached by
        /// <c>upgrade</c> instead.
        /// </summary>
        public UpgradeLadder Ladder { get; }

        /// <summary>
        /// The one wallet. Every wave pays it interest on what was banked, the
        /// flat base, and a share of the leak cost the wave dealt on top of
        /// that.
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

        /// <summary>
        /// The creeps the next round already fields, and does not pay for again.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A creep is bought once and attacks every round after.</b> A build
        /// phase composes the whole of its round's wave -- what it carries and
        /// what it is adding, in whatever release order it wants -- and is
        /// charged only for the increase over this. So a wave that sends fewer
        /// of a type than this holds is refused rather than discounted: there is
        /// no selling a creep back, and a purchase is a lasting commitment.
        /// </para>
        /// <para>
        /// It is the last round's wave and not a running total kept beside it,
        /// because the last round's wave <i>is</i> the running total -- every
        /// round already sends everything the ones before it bought. A second
        /// tally would be free to disagree with the record.
        /// </para>
        /// </remarks>
        public WaveScript Carrying =>
            _sent.Count == 0 ? WaveScript.Nothing : _sent[_sent.Count - 1].Wave;

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
        /// <b>This is the only way into a round.</b> A round is one decision
        /// plus the wave that resolves it, and the decision reaches the
        /// simulation as a <see cref="BuildPhase"/> handed in here and by no
        /// other route -- a value a stored command carries every field of, so
        /// nothing can be played into a run that could not have been written
        /// down.
        /// </para>
        /// <para>
        /// The decision is checked against this run's upgrade ladder, this run's
        /// board and map, and this run's purse -- by
        /// <see cref="BuildPhase.Resolve(int, WaveScript, UpgradeLadder, Purse, CostTable, UnitTypeTable, HexMap, Board)"/>,
        /// which is the surface a stored command stream is validated against
        /// too, so there is one implementation of the rules and not two.
        /// </para>
        /// <para>
        /// <b>What stands is derived and never handed in.</b> The other half of
        /// a round's orders is the <see cref="Board"/> the phase left, sorted
        /// into a layout here. A defense a caller composed each round would be a
        /// decision reaching the simulation by a route no record carries --
        /// assembled by anybody, applied against no map and paid for out of no
        /// purse. It is the board <i>after</i> this round's building, because
        /// the purse walks the take, then the actions, then the slots: a tower
        /// bought this round is standing when this round's waves arrive.
        /// </para>
        /// <para>
        /// K opponents are drawn, and each is fought in both directions: this
        /// round's wave against their defense, and their wave against this
        /// round's defense. What comes back is the average of each side's K
        /// resolutions -- the average and not the best, symmetrically with the
        /// damage rule, so the ladder rewards robust play.
        /// </para>
        /// <para>
        /// <b>Everything that can refuse this round refuses before a coin
        /// moves</b> -- the decision against the ladder, the orders against
        /// the defense, the run against being over, and then everything the
        /// round itself can refuse at. The whole round is worked out into
        /// locals -- see <see cref="Play"/> -- and reaches the run through
        /// <see cref="Commit"/>, which writes everything a round moves,
        /// together, and is the only place any of it is written. What the
        /// decision built and what it left in the purse travel into
        /// <see cref="Play"/> as arguments rather than being written first, so
        /// a purse spent and a tower standing for a wave nobody was in the run
        /// to send is a state that cannot be reached rather than an ordering to
        /// get right.
        /// </para>
        /// <para>
        /// <b>What comes back is the whole round</b> -- see
        /// <see cref="RoundReport"/>. The pair, the decision as it resolved and
        /// what the wave paid are all settled here, so a caller that wants what
        /// a round cost reads it rather than resolving the decision again
        /// against a run the round has already moved.
        /// </para>
        /// </remarks>
        /// <param name="phase">What this round took, and how it filled its slots.</param>
        public RoundReport Advance(BuildPhase phase)
        {
            if (phase is null)
            {
                throw new ArgumentNullException(nameof(phase));
            }

            Build build = phase.Resolve(Round + 1, Carrying, Ladder, Purse, Costs, Types, Map, Board);
            RoundOrders orders = RoundOrders.Of(build.Board.Layout(), build.Wave);

            RequireUnfinished();

            (RoundOutcome outcome, WavePayment payment, FieldDraw field) = Play(
                orders, build.Purse, build.Board);

            return new RoundReport(outcome, build, payment, field);
        }

        /// <summary>
        /// The match one pairing of a resolved round came to, built again rather
        /// than kept, and advanced by nobody.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>This is the route by which a round is watched.</b>
        /// <see cref="Advance"/> resolves the wave against every member of the
        /// field in locals and lets them go, which is right: a run that kept its
        /// matches would hold N times K of them by the end, and a sweep that
        /// plays a hundred thousand runs overnight would carry a match for every
        /// pairing in every one of them and draw not one. A client needs exactly
        /// one, so it asks for the one it means to show. See
        /// <c>docs/adr/0051-a-round-is-composed-on-screen-and-arrives-as-a-stored-command.md</c>.
        /// </para>
        /// <para>
        /// <b>Asking twice is cheaper than remembering because the answer cannot
        /// differ.</b> A pairing's match is this map, this ruleset, the defense
        /// that stood, the wave that walked and a seed derived from the run's
        /// seed and the pairing -- every one of them settled before the round was
        /// played, and none of them moved by playing it. What comes back is
        /// therefore the match that was resolved, tick for tick and hash for
        /// hash, rather than a re-enactment of it. It is built by the same
        /// assembly the round's own matches were built by -- see
        /// <see cref="MatchFor"/> -- and not by a second one somebody would have
        /// to keep in step.
        /// </para>
        /// <para>
        /// <b>Nothing here moves the run.</b> The match comes back on tick zero,
        /// unresolved, for the caller to advance at whatever rate it draws at;
        /// advancing it is advancing a local of the caller's and reaches nothing
        /// in here. <see cref="Advance"/> remains the only member that moves
        /// anything.
        /// </para>
        /// <para>
        /// <b>Both directions of a pairing, and the caller names which.</b> A
        /// round is resolved twice against every opponent -- see
        /// <see cref="Play"/>, which sums what the wave dealt over one direction
        /// and what the defense took over the other -- so both matches are
        /// already scored by the time anybody asks for one. Naming the direction
        /// therefore chooses between two fights that happened rather than
        /// starting a third; <see cref="Advance"/> is still the only member that
        /// moves anything, and switching between them costs a rebuild and
        /// nothing else.
        /// </para>
        /// <para>
        /// <b>A bool and not the side enum</b>, which is ADR-0039's surface pin:
        /// every public member of this type other than <see cref="Advance"/>
        /// takes primitives, so that nothing a caller composes can reach a run
        /// except through a stored command. It buys a second thing here that an
        /// enum would have cost -- <see cref="Side"/> has a third member,
        /// <see cref="Side.Measured"/>, which is the stream the field is
        /// measured on and is not a fight anybody watched. Two watchable
        /// directions are two values, so the unwatchable one is unreachable by
        /// construction rather than by a guard somebody has to write.
        /// </para>
        /// </remarks>
        /// <param name="round">
        /// Which round, indexed as <see cref="Sent"/> is -- zero is the first
        /// round the run resolved. A round that has not been played has no match
        /// to hand back.
        /// </param>
        /// <param name="opponent">Which of the round's K pairings, counted from zero.</param>
        /// <param name="attacking">
        /// True for this round's wave against that opponent's defense -- the
        /// direction that scores. False for that opponent's wave against this
        /// round's defense, which is the direction health is spent on.
        /// </param>
        public Match MatchAt(int round, int opponent, bool attacking)
        {
            if (round < 0 || round >= _sent.Count)
            {
                throw new SimulationException(
                    "This run was asked for the match of round "
                    + round.ToString(CultureInfo.InvariantCulture)
                    + ", and "
                    + _sent.Count.ToString(CultureInfo.InvariantCulture)
                    + " rounds have resolved. A match is what a round came to, so there is one to rebuild "
                    + "only for a round that has already been played -- rounds are indexed as Sent is, from "
                    + "zero.");
            }

            if (opponent < 0 || opponent >= FieldSize)
            {
                throw new SimulationException(
                    "This run was asked for the match against opponent "
                    + opponent.ToString(CultureInfo.InvariantCulture)
                    + " of a field of "
                    + FieldSize.ToString(CultureInfo.InvariantCulture)
                    + ". A round is resolved against K opponents and against nobody else, so this is a "
                    + "pairing the round never fought.");
            }

            return MatchFor(
                _sent[round],
                FieldFor(round).Members[opponent],
                round,
                opponent,
                attacking ? Side.Attacking : Side.Defending);
        }

        /// <summary>
        /// Works one round out in full, commits it, and hands back the pair it
        /// resolved to beside what its wave paid.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The purse and the board arrive as arguments.</b> A build phase's
        /// decision governs the round it was made in -- the wave is bought out
        /// of this round's purse, and what it built stands against this round's
        /// opponents -- and passing them is what lets that happen without
        /// either having been written to the run first.
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
        /// <param name="purse">What the round carries into the wave, after whatever it bought.</param>
        /// <param name="board">What stands after whatever the round built.</param>
        private (RoundOutcome Outcome, WavePayment Payment, FieldDraw Field) Play(
            RoundOrders orders,
            Purse purse,
            Board board)
        {
            int round = _rounds.Count;
            FieldDraw drawn = FieldFor(round);
            IReadOnlyList<RoundOrders> against = drawn.Members;
            long dealt = 0;
            long taken = 0;

            for (int index = 0; index < against.Count; index++)
            {
                dealt += LeakCost(orders, against[index], round, index, Side.Attacking);
                taken += LeakCost(orders, against[index], round, index, Side.Defending);
            }

            // The average rather than the sum, on both sides. Summed, one round
            // against ten opponents would cost ten rounds' worth of health, and
            // a field would be a punishment for being in one.
            var outcome = new RoundOutcome((int)(dealt / against.Count), (int)(taken / against.Count));

            // Interest, the flat base, and a share of what this round's offense
            // got past on top. Nothing is taken off anybody to pay it: the wave
            // is paid for the damage it dealt and not against whichever opponent
            // it was drawn against.
            WavePayment payment = purse.CloseWave(Rules, outcome.LeakCostDealt);

            Commit(orders, outcome, payment.Purse, board, FoldedWith(outcome));

            return (outcome, payment, drawn);
        }

        /// <summary>
        /// The one place a round is written to the run.
        /// </summary>
        /// <remarks>
        /// Every field a round moves moves here, from arguments that are already
        /// settled, and nothing between the first write and the last can refuse.
        /// That is the whole of the guarantee <see cref="Advance"/> makes: it is
        /// a property of where the writes are rather than of what order the work
        /// above them happens in.
        /// </remarks>
        private void Commit(
            RoundOrders orders,
            RoundOutcome outcome,
            Purse purse,
            Board board,
            RunOutcome folded)
        {
            _rounds.Add(outcome);
            _sent.Add(orders);
            Purse = purse;
            Board = board;
            _outcome = folded;
        }

        /// <summary>
        /// Which K of the pool this round is fought against: the stage's stored
        /// rounds, no two of them the same, and the stand-in for whatever the
        /// stage could not fill.
        /// </summary>
        /// <remarks>
        /// <para>
        /// One stream, started at a position derived from the run's seed and the
        /// round rather than continued from wherever the last draw left it. Two
        /// things follow, and both are the point: round seven's field does not
        /// depend on what rounds one to six did, so a run is reproducible from
        /// its record and a server can re-validate one round of it; and no draw
        /// of any kind happens inside a match, so a match's stream position stays
        /// a running count of the shots fired in it.
        /// </para>
        /// <para>
        /// <b>Stored rounds are met without replacement and the stand-in with
        /// it.</b> A field of ten drawn out of a stage of twenty is ten
        /// different opponents rather than ten draws that may repeat, which is
        /// what makes a wide pool a wide field; the same draw off the same seed
        /// meets the same ten. What a stage cannot fill is the stand-in's, drawn
        /// one slot at a time with replacement -- so a stage nobody has stored a
        /// round at is the canned field, exactly as every stage was before there
        /// was a folder to read.
        /// </para>
        /// </remarks>
        private FieldDraw FieldFor(int round)
        {
            var dice = new Pcg32(FieldSeed(round));
            int stored = _pool.StoredAt(round);
            int met = stored < FieldSize ? stored : FieldSize;
            var members = new RoundOrders[FieldSize];
            var drawn = new int[FieldSize];
            var bag = new int[stored];

            for (int index = 0; index < bag.Length; index++)
            {
                bag[index] = index;
            }

            // A partial shuffle of the stage's stored rounds: one of the members
            // nobody has met yet is swapped into each slot in turn, so the first
            // `met` slots hold distinct members in an order the run's seed
            // decides. It stops at `met`, so the work is the field's width and
            // not the folder's.
            for (int index = 0; index < met; index++)
            {
                int pick = index + (int)dice.NextBelow((uint)(stored - index));
                int held = bag[pick];

                bag[pick] = bag[index];
                bag[index] = held;

                drawn[index] = held;
                members[index] = _pool.StoredAt(round, held);
            }

            // What the stage could not fill, filled by the stand-in and drawn
            // exactly as every field was drawn before any round was stored: one
            // draw per slot, with replacement, off the same stream. A stage with
            // nothing stored therefore consumes the stream it always consumed
            // and meets the field it always met.
            for (int index = met; index < FieldSize; index++)
            {
                drawn[index] = FieldDraw.StoodIn;
                members[index] = _pool.StandingIn(
                    round, (int)dice.NextBelow((uint)_pool.StandInsAt(round)));
            }

            return new FieldDraw(members, drawn);
        }

        /// <summary>
        /// One measurement draw: <see cref="FieldSize"/> members out of a
        /// population this many wide, with replacement, off the stream that
        /// sample's position starts.
        /// </summary>
        /// <remarks>
        /// Not the draw a round makes, because the two answer different
        /// questions. A round meets one stage and is topped up where that stage
        /// is thin; the measurement reads the whole population at once and has
        /// no stage to be thin at, so there is nothing for a stand-in to fill.
        /// </remarks>
        private int[] SampleOf(int round, int size)
        {
            var dice = new Pcg32(FieldSeed(round));
            var drawn = new int[FieldSize];

            for (int index = 0; index < drawn.Length; index++)
            {
                drawn[index] = (int)dice.NextBelow((uint)size);
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
        /// and pin every honest run to the middle of it.
        /// </para>
        /// <para>
        /// <b>Only the offense is resolved.</b> What is measured is leak cost
        /// dealt, so what the pool's rounds would have taken back is never
        /// played -- the measurement costs half of what a round costs rather
        /// than all of it. What it sends carries no game changer,
        /// for the reason the defending direction's does not: the pool is stored
        /// orders rather than stored runs, and nothing in it says which of its
        /// bodies was one.
        /// </para>
        /// <para>
        /// <b>Both the member being measured and the field it meets are drawn,
        /// and both are drawn over the whole population.</b> A walk down the
        /// pool would sample a population wider than <see cref="FieldSamples"/>
        /// by truncating it at its first members. The draw is round-blind where
        /// a round's own is not: a pool records a population per round and this
        /// reads all of them at once, so what it comes back with is one spread
        /// for the run rather than one per round -- which is what keeps the
        /// payment a fold. The price is that the population measured is not the
        /// population any single round fights, which is the resolution
        /// <c>docs/adr/0042-the-field-is-measured-off-the-pool.md</c> records
        /// under its amendment.
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
                int[] field = SampleOf(sample, _pool.Size);
                long dealt = 0;

                for (int index = 0; index < field.Length; index++)
                {
                    dealt += LeakCost(member, _pool.At(field[index]), sample, index, Side.Measured);
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
        private int LeakCost(RoundOrders sent, RoundOrders against, int round, int opponent, Side side)
        {
            Match match = MatchFor(sent, against, round, opponent, side);
            match.Resolve();

            WaveScript wave = Walking(sent, against, side);
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

        /// <summary>Refuses a round past the end of a run.</summary>
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
        /// The one place a pairing becomes a match.
        /// </summary>
        /// <remarks>
        /// Both routes to a match come through here -- the round resolving its
        /// field, and <see cref="MatchAt"/> handing one back to be watched -- so
        /// there is one statement of what a pairing's match is made of rather
        /// than two that have to agree. Nothing is resolved: what comes back is
        /// on tick zero, and what the caller does with it is the difference
        /// between scoring a round and drawing one.
        /// </remarks>
        private Match MatchFor(RoundOrders sent, RoundOrders against, int round, int opponent, Side side) =>
            new Match(
                Map,
                Rules,
                Standing(sent, against, side),
                Walking(sent, against, side),
                MatchSeed(round, opponent, side));

        /// <summary>
        /// Whose wave walks in one side of a pairing. <paramref name="sent"/> is
        /// always what this run's round composed -- or, while the field is being
        /// measured, what the pool member being measured sent -- and
        /// <paramref name="against"/> is always the opponent's, so which of the
        /// two walks is the side and nothing else.
        /// </summary>
        private static WaveScript Walking(RoundOrders sent, RoundOrders against, Side side) =>
            side == Side.Defending ? against.Wave : sent.Wave;

        /// <summary>Whose defense stands in one side of a pairing: the other one's.</summary>
        private static TowerLayout Standing(RoundOrders sent, RoundOrders against, Side side) =>
            side == Side.Defending ? sent.Defense : against.Defense;

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
            /// is the spread <see cref="Field"/> reports.
            /// </summary>
            Measured = 2,
        }
    }
}
