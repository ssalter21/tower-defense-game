using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One round's field, as it was drawn: which of the stage's stored rounds
    /// each of the K slots met, and which of them met the stand-in instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The members and the provenance travel together</b>, because a round is
    /// played against the first and reported by the second. What a slot drew is
    /// an index into the stage's stored population rather than a record id: the
    /// simulation is handed a population and never the bytes it came out of, so
    /// naming a member is whoever read the folder's job.
    /// </para>
    /// <para>
    /// <b>A canned slot is a slot the pool could not fill.</b> A field is K
    /// wide whatever the pool holds, so a stage with fewer than K stored rounds
    /// is topped up rather than narrowed -- and <see cref="Canned"/> is how many
    /// of the ten a run met were the stand-in rather than somebody.
    /// </para>
    /// </remarks>
    public sealed class FieldDraw
    {
        /// <summary>What <see cref="Drawn"/> holds for a slot the stand-in filled.</summary>
        public const int StoodIn = -1;

        private readonly RoundOrders[] _members;

        private readonly int[] _drawn;

        internal FieldDraw(RoundOrders[] members, int[] drawn)
        {
            _members = members;
            _drawn = drawn;
        }

        /// <summary>K: the opponents this round was resolved against, in slot order.</summary>
        public IReadOnlyList<RoundOrders> Members => _members;

        /// <summary>
        /// Which stored round each slot drew, indexed inside the stage's own
        /// population, or <see cref="StoodIn"/> where the stand-in filled it.
        /// </summary>
        public IReadOnlyList<int> Drawn => _drawn;

        /// <summary>How many of the slots the stand-in filled.</summary>
        public int Canned
        {
            get
            {
                int canned = 0;

                for (int index = 0; index < _drawn.Length; index++)
                {
                    if (_drawn[index] == StoodIn)
                    {
                        canned++;
                    }
                }

                return canned;
            }
        }
    }

    /// <summary>
    /// Everybody a round's field of K may be drawn from: the rounds stored at
    /// each stage, and the stand-in that fills a stage which has too few.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pool and the field are not the same thing.</b> The pool is the
    /// population -- the rounds somebody has played and stored, plus a canned
    /// stand-in -- and the field is the K of them one round is resolved
    /// against, drawn per stage from the run's seed at a derived position.
    /// </para>
    /// <para>
    /// <b>Stored rounds are drawn without replacement and the stand-in with
    /// it.</b> A field meets as much of its stage as the stage has, no member
    /// twice, so a replayed run meets the same ten and a wide stage is a wide
    /// field. What the stage cannot fill the stand-in does, drawn as every
    /// field was drawn before any round was stored -- so a pool with nothing
    /// stored at a stage resolves that stage exactly as it always did.
    /// </para>
    /// <para>
    /// <b>A stage is exact where the stand-in clamps.</b> The stand-in past the
    /// deepest round recorded is the deepest round's, because a run is as long
    /// as its wave count and a canned opponent is as deep as whoever composed
    /// it. Stored rounds do not clamp: a stage nobody has played is a stage of
    /// nobody, and standing round ten's opponents at round twenty would be
    /// inventing a population.
    /// </para>
    /// </remarks>
    public sealed class FieldPool
    {
        /// <summary>
        /// The leak cost a round of the canned stand-in is closed on. Nothing
        /// here plays a match, so there is no wave of its own to have got
        /// anything past and no share of anything to be paid.
        /// </summary>
        private const int NothingDealt = 0;

        /// <summary>
        /// The bounty a round of the canned stand-in is closed on. Nothing here
        /// plays a match, so nothing was killed in front of the wall being
        /// built and there is nothing to be paid for.
        /// </summary>
        private const int NothingEarned = 0;

        private static readonly RoundOrders[] Nobody = new RoundOrders[0];

        private readonly RoundOrders[][] _standIn;

        private readonly RoundOrders[][] _stored;

        private readonly RoundOrders[] _members;

        private FieldPool(RoundOrders[][] standIn, RoundOrders[][] stored, RoundOrders[] members)
        {
            _standIn = standIn;
            _stored = stored;
            _members = members;
        }

        /// <summary>How many rounds are in the population, stored and standing in alike.</summary>
        public int Size => _members.Length;

        /// <summary>How many rounds deep the stand-in goes.</summary>
        public int Rounds => _standIn.Length;

        /// <summary>Whether any round has been stored in this pool at all.</summary>
        public bool HasStored => _stored.Length > 0;

        /// <summary>The whole population, stage structure flattened away.</summary>
        public IReadOnlyList<RoundOrders> Members => _members;

        /// <summary>
        /// The pool, copied, so that what a run draws from cannot change under
        /// it. One round's stand-in, standing at every round.
        /// </summary>
        public static FieldPool Of(IReadOnlyList<RoundOrders> members) =>
            OfRounds(new[] { members });

        /// <summary>
        /// The pool as rounds: the stand-in recorded at round one, then the one
        /// recorded at round two, and so on.
        /// </summary>
        public static FieldPool OfRounds(IReadOnlyList<IReadOnlyList<RoundOrders>> rounds)
        {
            if (rounds is null)
            {
                throw new ArgumentNullException(nameof(rounds));
            }

            if (NobodyIn(rounds))
            {
                throw new SimulationException(
                    "A run was given a pool of nobody to draw its field from. A round is resolved against "
                    + "opponents, and there is no drawing one out of an empty population -- a run with "
                    + "nothing to fight is a harness that was pointed at no ghosts rather than a run whose "
                    + "field happens to be quiet.");
            }

            var copied = new RoundOrders[rounds.Count][];

            for (int round = 0; round < copied.Length; round++)
            {
                copied[round] = Copied(rounds[round], round);
            }

            return new FieldPool(copied, new RoundOrders[0][], Flattened(copied, new RoundOrders[0][]));
        }

        /// <summary>
        /// The same stand-in, with a population of stored rounds in front of it:
        /// the rounds stored at stage one, then those stored at stage two, and
        /// so on.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>A stage may be empty here, and that is the ordinary case.</b>
        /// Nobody has played a twentieth round of anything on a fresh folder,
        /// and a stage with nothing in it is filled entirely by the stand-in
        /// rather than refusing -- which is what makes a thin pool a thin field
        /// instead of a run that will not start.
        /// </para>
        /// <para>
        /// <b>It layers rather than replaces</b>, because the stand-in is what
        /// makes the field K wide whatever the folder holds. There is still
        /// exactly one pool argument to a run, which is what stops a run being
        /// handed a population and a top-up that describe different things.
        /// </para>
        /// </remarks>
        public FieldPool Storing(IReadOnlyList<IReadOnlyList<RoundOrders>> stored)
        {
            if (stored is null)
            {
                throw new ArgumentNullException(nameof(stored));
            }

            var copied = new RoundOrders[stored.Count][];

            for (int stage = 0; stage < copied.Length; stage++)
            {
                copied[stage] = CopiedStage(stored[stage], stage);
            }

            return new FieldPool(_standIn, copied, Flattened(_standIn, copied));
        }

        /// <summary>
        /// The canned pool: one player's run, standing in for a population of
        /// stored ones.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A round is resolved against K opponents drawn from a population of
        /// other players' rounds, and a folder of them can be empty or thin.
        /// This is what fills the slots it cannot: the one player written here,
        /// recorded once per round and drawn with replacement, so a field of ten
        /// with nothing stored is that opponent's round ten times.
        /// </para>
        /// <para>
        /// <b>The stand-in buys the wave it is handed once more every round.</b>
        /// Round one sends the authored counts, round seven sends seven of each,
        /// and the orders themselves never move -- so a member grows in depth
        /// rather than in width, which is the accumulation a build phase makes
        /// (a creep is bought once and is sent again for nothing) and the shape
        /// <c>content/field.txt</c> is calibrated at. A stand-in that sent the
        /// same wave every round is an opponent who banks a whole run's income
        /// and never spends it.
        /// </para>
        /// <para>
        /// <b>And it builds its wall out of a purse, round by round, by the rule
        /// a run builds by.</b> The stand-in opens holding
        /// <see cref="Ruleset.StartingPurseGold"/>, hands what stands and what it
        /// holds to <see cref="CoverThenUpgradeBot"/>, pays for what comes back
        /// through <see cref="BuildPhase.Resolve"/>, and closes the round on
        /// <see cref="Purse.CloseWave"/> -- the interest on what it banked and the
        /// flat base. That is the income line a run is paid and not the whole of
        /// what a run has: a run banks its unspent offensive share as well, and
        /// is paid a bonus, and the two paragraphs below say why neither is
        /// available here. So <paramref name="defense"/> is the wall it opens
        /// with and not the wall it stands at every round.
        /// </para>
        /// <para>
        /// <b>Every round builds before it is recorded, the first one included.</b>
        /// What a round's incoming waves meet is what that round built, which is
        /// the order a run resolves in -- so the layout recorded at round one is
        /// this seed plus whatever round one's own share bought, and round ten is
        /// the seed plus ten rounds of building rather than nine. On the committed
        /// content round one adds nothing, because the opening share is 50 gold
        /// against a cheapest upgrade of 92; that is a fact about this content and
        /// not a rule, and content whose route left a gap would place a tower in
        /// round one.
        /// </para>
        /// <para>
        /// <b>The bonus line is the one a stand-in cannot have.</b> A wave is
        /// paid a share of the leak cost it dealt and nothing here resolves a
        /// round of its own, so the closing pays the interest and the base and
        /// no bonus. Assuming a bonus would be assuming a number that only a
        /// played round produces, and the honest reading of an unplayed one is
        /// nothing.
        /// </para>
        /// <para>
        /// <b>The wave is not priced, and the share it would cost leaves the
        /// purse anyway.</b> What the stand-in sends is authored rather than
        /// composed -- <c>content/field.txt</c> is calibrated as roughly what a
        /// round's wave comes to once a purse has bought a wall as well -- so
        /// pricing it here would be pricing one wave twice, and a pool handed a
        /// script no purse could compose would be refused rather than recorded.
        /// What carries into the next round is therefore what the WALL declined
        /// to spend out of its own share; the rest is the wave's and is gone. A
        /// purse that banked the offensive share would compound at the ruleset's
        /// interest on gold a player spends, which is an opponent growing richer
        /// for sending the same wave.
        /// </para>
        /// <para>
        /// <b>The opening wall is handed over and not bought.</b> A member of
        /// this pool is a recorded round rather than a run played from nothing,
        /// and <paramref name="defense"/> is what was recorded at its first one
        /// -- so it stands beside the opening purse instead of coming out of it.
        /// Charging for it would refuse most layouts anybody could author: the
        /// committed six cost 344 gold against an opening purse of 100.
        /// </para>
        /// <para>
        /// <b>What a field of one collapses is a rank and not a payment.</b> A
        /// wave is paid a share of the leak cost it dealt, so nothing here
        /// prices anything; a population of one puts nearly every round above
        /// the field or below it, which is a property of the stand-in measured
        /// in <c>docs/research/a-canned-field-of-one-collapses-the-bands.md</c>
        /// and now bears only on <see cref="PerformanceField"/>, which nothing
        /// consumes.
        /// </para>
        /// </remarks>
        /// <param name="map">The board the wall stands on, and the route it is measured against.</param>
        /// <param name="rules">The purse it opens with, the interest it earns and the base it is paid.</param>
        /// <param name="types">The roster its wall is built out of and its wave is read against.</param>
        /// <param name="ladder">The edges a placement climbs, checked as a run's are.</param>
        /// <param name="defense">The wall this opponent opens with, and builds on from there.</param>
        /// <param name="wave">What it sends in its first round, and buys again in each one after.</param>
        /// <param name="rounds">
        /// How many rounds of it to record. A run longer than this fights the
        /// last of them, by the rule <see cref="OfRounds"/> carries.
        /// </param>
        /// <param name="only">
        /// The one attack type this opponent's wall may be built out of, or
        /// nothing for the whole roster.
        /// </param>
        /// <remarks>
        /// <para>
        /// <b>What <paramref name="only"/> is for is comparability, not
        /// flavour.</b> The damage matrix is authored so that no attack type is
        /// globally better, which means a wall of one type is a hard counter to
        /// one armour class and a soft touch to another -- so a roster swept
        /// against a wall of whatever a value-buying bot converged on reports a
        /// landslide and a zero, and which creep gets which is a fact about the
        /// bot. Restricting the wall makes the type the axis it always secretly
        /// was. Measured in
        /// <c>docs/research/a-sweep-row-measures-the-walls-attack-type.md</c>.
        /// </para>
        /// <para>
        /// <b>The opening layout is not filtered and is not meant to be.</b>
        /// <paramref name="defense"/> is a recorded wall handed over whole, so
        /// restricting what this opponent BUYS while it stands what it was given
        /// is the honest split: the seed is content somebody authored and the
        /// growth is the thing being held to one type.
        /// </para>
        /// </remarks>
        public static FieldPool Canned(
            HexMap map,
            Ruleset rules,
            UnitTypeTable types,
            UpgradeLadder ladder,
            TowerLayout defense,
            WaveScript wave,
            int rounds = Run.DefaultWaves,
            AttackType? only = null)
        {
            if (map is null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (rules is null)
            {
                throw new ArgumentNullException(nameof(rules));
            }

            if (types is null)
            {
                throw new ArgumentNullException(nameof(types));
            }

            if (ladder is null)
            {
                throw new ArgumentNullException(nameof(ladder));
            }

            if (defense is null)
            {
                throw new ArgumentNullException(nameof(defense));
            }

            if (wave is null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            if (rounds < 1)
            {
                throw new SimulationException(
                    "The canned pool was asked for "
                    + rounds.ToString(CultureInfo.InvariantCulture)
                    + " rounds of an opponent. A population is recorded round by round, and a pool that "
                    + "reaches no round at all is nobody to fight rather than a shallow stand-in.");
            }

            var recorded = new RoundOrders[rounds][];
            CostTable costs = CostTable.From(rules, types);
            Board board = Standing(defense);
            Purse purse = Purse.Holding(rules.StartingPurseGold);

            // The stand-in is handed the same grant schedule a run is, so the
            // wall a growing wave is measured against climbs to the top of a
            // line on the rounds a player's does. A second schedule here would
            // be an opponent written to be weak.
            int tokens = 0;

            for (int round = 0; round < recorded.Length; round++)
            {
                tokens += Run.CapstoneTokensGrantedAt(round + 1);
                // The share the wall may spend, taken out of the purse before
                // the wall is offered it: what is left of the round's gold is
                // the offensive share, and the authored wave is what that share
                // bought. So the purse walking into the next round is what the
                // WALL declined to spend and nothing else.
                Purse share = Purse.Holding(CoverThenUpgradeBot.BudgetOf(purse));

                // The wall is built before the round's wave is recorded, exactly
                // as a run's is: what this round's incoming waves meet is what
                // this round built.
                Build built = BuildPhase
                    .Of()
                    .With(CoverThenUpgradeBot.Decide(map, types, costs, ladder, board, purse, tokens, only))
                    .Resolve(
                        round + 1, WaveScript.Nothing, ladder, share, tokens, costs, types, map, board);

                board = built.Board;
                tokens -= built.CapstoneTokensSpent;
                recorded[round] = new[] { RoundOrders.Of(board.Layout(), Grown(wave, round + 1)) };
                purse = built.Purse.CloseWave(rules, NothingDealt, NothingEarned).Purse;
            }

            return OfRounds(recorded);
        }

        /// <summary>
        /// How many rounds are stored at this stage, counted from zero as a
        /// run's own rounds are. None, for a stage nobody has played.
        /// </summary>
        public int StoredAt(int round)
        {
            RequireRound(round);

            return round < _stored.Length ? _stored[round].Length : 0;
        }

        /// <summary>The stored round at this index of the population stored at this stage.</summary>
        public RoundOrders StoredAt(int round, int index)
        {
            int count = StoredAt(round);

            if (index < 0 || index >= count)
            {
                throw new SimulationException(
                    "The pool was asked for stored round "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " of the "
                    + count.ToString(CultureInfo.InvariantCulture)
                    + " at stage "
                    + (round + 1).ToString(CultureInfo.InvariantCulture)
                    + ". A field is drawn inside the bounds of its own stage's population, so an index "
                    + "outside them is a draw that was taken against the wrong size.");
            }

            return _stored[round][index];
        }

        /// <summary>
        /// How many stand-ins are recorded at this round, counted from zero as a
        /// run's own rounds are.
        /// </summary>
        public int StandInsAt(int round) => Recorded(round).Length;

        /// <summary>The stand-in at this index of the population recorded at this round.</summary>
        public RoundOrders StandingIn(int round, int index)
        {
            RoundOrders[] members = Recorded(round);

            if (index < 0 || index >= members.Length)
            {
                throw new SimulationException(
                    "The pool was asked for stand-in "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " of the "
                    + members.Length.ToString(CultureInfo.InvariantCulture)
                    + " recorded at round "
                    + (round + 1).ToString(CultureInfo.InvariantCulture)
                    + ". A field is drawn inside the bounds of its own round's population, so an index "
                    + "outside them is a draw that was taken against the wrong size.");
            }

            return members[index];
        }

        /// <summary>The member at this index of the whole population, stage structure flattened away.</summary>
        public RoundOrders At(int index)
        {
            if (index < 0 || index >= _members.Length)
            {
                throw new SimulationException(
                    "The pool was asked for member "
                    + index.ToString(CultureInfo.InvariantCulture)
                    + " of "
                    + _members.Length.ToString(CultureInfo.InvariantCulture)
                    + ". A field is drawn inside the pool's own bounds, so an index outside them is a draw "
                    + "that was taken against the wrong size.");
            }

            return _members[index];
        }

        /// <summary>
        /// One round of the stand-in, copied and checked. An empty round is
        /// refused here rather than at the draw that finds nobody in it.
        /// </summary>
        private static RoundOrders[] Copied(IReadOnlyList<RoundOrders> members, int round)
        {
            if (members is null || members.Count == 0)
            {
                throw new SimulationException(
                    "The pool has nobody recorded at round "
                    + (round + 1).ToString(CultureInfo.InvariantCulture)
                    + ", and somebody recorded at another one. A round of a run is fought against the "
                    + "members recorded at that round, so an empty round is a round nobody could be drawn "
                    + "for rather than a population that thins out.");
            }

            return Held(members, round, "recorded at round");
        }

        /// <summary>
        /// One stage of the stored population, copied and checked. Empty is
        /// allowed here and refused above: nobody has played every stage of
        /// anything, and the stand-in is what fills the ones nobody has.
        /// </summary>
        private static RoundOrders[] CopiedStage(IReadOnlyList<RoundOrders> members, int stage)
        {
            if (members is null || members.Count == 0)
            {
                return Nobody;
            }

            return Held(members, stage, "stored at stage");
        }

        /// <summary>The members, copied, with nothing at all refused by name.</summary>
        private static RoundOrders[] Held(IReadOnlyList<RoundOrders> members, int round, string where)
        {
            var copied = new RoundOrders[members.Count];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = members[index]
                    ?? throw new SimulationException(
                        "The pool's member at index "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " of those "
                        + where
                        + " "
                        + (round + 1).ToString(CultureInfo.InvariantCulture)
                        + " is nothing at all. Every member of a field is a defense and a wave, because a "
                        + "round measures both directions against each of them.");
            }

            return copied;
        }

        /// <summary>
        /// The whole population as one list: stage by stage, what is stored at
        /// that stage and then what stands in for it.
        /// </summary>
        private static RoundOrders[] Flattened(RoundOrders[][] standIn, RoundOrders[][] stored)
        {
            var all = new List<RoundOrders>();
            int stages = standIn.Length > stored.Length ? standIn.Length : stored.Length;

            for (int stage = 0; stage < stages; stage++)
            {
                if (stage < stored.Length)
                {
                    all.AddRange(stored[stage]);
                }

                if (stage < standIn.Length)
                {
                    all.AddRange(standIn[stage]);
                }
            }

            return all.ToArray();
        }

        /// <summary>
        /// The board an authored layout is, placement by placement in the order
        /// the layout lists them.
        /// </summary>
        /// <remarks>
        /// A layout is what a defense looks like from outside and a board is
        /// what building acts on, so the stand-in's opening wall is stood before
        /// its first round rather than parsed again each round. The ordinals
        /// follow the file's own order, which is the order an upgrade reaches
        /// them in.
        /// </remarks>
        private static Board Standing(TowerLayout defense)
        {
            Board board = Board.Empty;

            for (int index = 0; index < defense.Towers.Count; index++)
            {
                PlacedTower tower = defense.Towers[index];

                board = board.Place(tower.Type, tower.Column, tower.Row);
            }

            return board;
        }

        /// <summary>
        /// The same wave bought this many times: every order's count multiplied,
        /// at the ticks and in the columns it was authored with.
        /// </summary>
        private static WaveScript Grown(WaveScript wave, int times)
        {
            if (times == 1)
            {
                return wave;
            }

            var orders = new UnitOrder[wave.Count];

            for (int index = 0; index < orders.Length; index++)
            {
                UnitOrder order = wave.Orders[index];

                orders[index] = new UnitOrder(
                    order.TickOffset,
                    order.Type,
                    order.Count * times,
                    order.Corridor);
            }

            return WaveScript.FromSlots(orders);
        }

        /// <summary>Whether every round handed over is empty, which is a pool of nobody.</summary>
        private static bool NobodyIn(IReadOnlyList<IReadOnlyList<RoundOrders>> rounds)
        {
            for (int round = 0; round < rounds.Count; round++)
            {
                if (rounds[round] is object && rounds[round].Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The stand-in recorded at this round, or the deepest round's for a
        /// round the pool does not reach.
        /// </summary>
        private RoundOrders[] Recorded(int round)
        {
            RequireRound(round);

            return _standIn[round < _standIn.Length ? round : _standIn.Length - 1];
        }

        /// <summary>A round a run could be on, refused if it is not one.</summary>
        private static void RequireRound(int round)
        {
            if (round >= 0)
            {
                return;
            }

            throw new SimulationException(
                "The pool was asked who was recorded at round "
                + round.ToString(CultureInfo.InvariantCulture)
                + ". Rounds are counted from zero, so a negative one is an index that was derived "
                + "rather than a round anybody played.");
        }
    }
}
