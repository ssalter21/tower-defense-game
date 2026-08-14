using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One player's round: the defense that stands, and the wave that is sent.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One type for both sides of a pairing.</b> This is what a build phase
    /// decided, and it is also what enters somebody else's field a round later,
    /// so a run's own rounds go into a <see cref="FieldPool"/> with nothing
    /// converted between the two. Nothing here says which of the two roles a
    /// given pair is playing, because nothing has to.
    /// </para>
    /// <para>
    /// The defense and the wave arrive already checked against a unit type
    /// table, exactly as a match's do, so nothing downstream re-resolves an id
    /// it was already told is good.
    /// </para>
    /// </remarks>
    public sealed class RoundOrders
    {
        private RoundOrders(TowerLayout defense, WaveScript wave)
        {
            Defense = defense;
            Wave = wave;
        }

        /// <summary>What stands against every wave the field sends this round.</summary>
        public TowerLayout Defense { get; }

        /// <summary>What is sent at every defense the field stands this round.</summary>
        public WaveScript Wave { get; }

        /// <summary>A round's orders. Both halves, because a round measures both directions.</summary>
        public static RoundOrders Of(TowerLayout defense, WaveScript wave)
        {
            if (defense is null)
            {
                throw new ArgumentNullException(nameof(defense));
            }

            if (wave is null)
            {
                throw new ArgumentNullException(nameof(wave));
            }

            return new RoundOrders(defense, wave);
        }
    }

    /// <summary>
    /// Everybody a round's field of K may be drawn from.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The pool and the field are not the same thing.</b> The pool is the
    /// population -- a canned set today, recorded rounds of real players later
    /// -- and the field is the K of them one round is resolved against, drawn
    /// per round from the run's seed at a derived position.
    /// </para>
    /// <para>
    /// <b>A pool smaller than K is not an error.</b> The draw is with
    /// replacement, so a field of ten can be drawn from a population of three
    /// and the same round can face one opponent twice. The alternative is a
    /// field size bounded by how many ghosts happen to exist, which is variance
    /// paid for with a thinner pool.
    /// </para>
    /// <para>
    /// <b>A member is recorded at a round, and a round draws from its own.</b>
    /// The population is a list of rounds rather than one flat list: round seven
    /// is fought against the members recorded at round seven, so an opponent
    /// grows over a run exactly as the run does. A pool handed a flat list is
    /// one round's population standing at every round, which is what a caller
    /// with no rounds to record means.
    /// </para>
    /// <para>
    /// <b>Past the deepest round recorded, the deepest round stands.</b> A run
    /// is as long as its wave count and a pool is as deep as whoever filled it,
    /// so the two do not have to agree -- a run of twenty rounds against ten
    /// recorded ones fights the tenth from there on rather than refusing at the
    /// eleventh.
    /// </para>
    /// </remarks>
    public sealed class FieldPool
    {
        private readonly RoundOrders[][] _rounds;

        private readonly RoundOrders[] _members;

        private FieldPool(RoundOrders[][] rounds, RoundOrders[] members)
        {
            _rounds = rounds;
            _members = members;
        }

        /// <summary>How many rounds are in the population, over all of its rounds.</summary>
        public int Size => _members.Length;

        /// <summary>How many rounds deep the population goes.</summary>
        public int Rounds => _rounds.Length;

        /// <summary>The whole population, round structure flattened away.</summary>
        public IReadOnlyList<RoundOrders> Members => _members;

        /// <summary>
        /// The pool, copied, so that what a run draws from cannot change under
        /// it. One round's population, standing at every round.
        /// </summary>
        public static FieldPool Of(IReadOnlyList<RoundOrders> members) =>
            OfRounds(new[] { members });

        /// <summary>
        /// The pool as rounds: the members recorded at round one, then those
        /// recorded at round two, and so on.
        /// </summary>
        public static FieldPool OfRounds(IReadOnlyList<IReadOnlyList<RoundOrders>> rounds)
        {
            if (rounds is null)
            {
                throw new ArgumentNullException(nameof(rounds));
            }

            if (Nobody(rounds))
            {
                throw new SimulationException(
                    "A run was given a pool of nobody to draw its field from. A round is resolved against "
                    + "opponents, and there is no drawing one out of an empty population -- a run with "
                    + "nothing to fight is a harness that was pointed at no ghosts rather than a run whose "
                    + "field happens to be quiet.");
            }

            var copied = new RoundOrders[rounds.Count][];
            var flattened = new List<RoundOrders>();

            for (int round = 0; round < copied.Length; round++)
            {
                copied[round] = Copied(rounds[round], round);
                flattened.AddRange(copied[round]);
            }

            return new FieldPool(copied, flattened.ToArray());
        }

        /// <summary>
        /// The canned pool: one player's run, standing in for a population of
        /// stored ones.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A round is resolved against K opponents drawn from a population of
        /// other players' rounds, and there is no such population until runs are
        /// stored. Until then the population is the one player written here,
        /// recorded once per round and drawn with replacement, so a field of ten
        /// is that opponent's round ten times. That is a thin pool rather than a
        /// missing one, and widening it is a longer list at
        /// <see cref="OfRounds"/> and no change anywhere else.
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
        /// <b>What a field of one collapses is a rank and not a payment.</b> A
        /// wave is paid a share of the leak cost it dealt, so nothing here
        /// prices anything; a population of one puts nearly every round above
        /// the field or below it, which is a property of the stand-in measured
        /// in <c>docs/research/a-canned-field-of-one-collapses-the-bands.md</c>
        /// and now bears only on <see cref="PerformanceField"/>, which nothing
        /// consumes.
        /// </para>
        /// </remarks>
        /// <param name="defense">The wall this opponent stands behind in every round.</param>
        /// <param name="wave">What it sends in its first round, and buys again in each one after.</param>
        /// <param name="rounds">
        /// How many rounds of it to record. A run longer than this fights the
        /// last of them, by the rule <see cref="OfRounds"/> carries.
        /// </param>
        public static FieldPool Canned(TowerLayout defense, WaveScript wave, int rounds = Run.DefaultWaves)
        {
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

            for (int round = 0; round < recorded.Length; round++)
            {
                recorded[round] = new[] { RoundOrders.Of(defense, Grown(wave, round + 1)) };
            }

            return OfRounds(recorded);
        }

        /// <summary>
        /// How many members are recorded at this round, counted from zero as a
        /// run's own rounds are.
        /// </summary>
        public int SizeAt(int round) => Recorded(round).Length;

        /// <summary>The member at this index of the population recorded at this round.</summary>
        public RoundOrders At(int round, int index)
        {
            RoundOrders[] members = Recorded(round);

            if (index < 0 || index >= members.Length)
            {
                throw new SimulationException(
                    "The pool was asked for member "
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

        /// <summary>The member at this index of the whole population, round structure flattened away.</summary>
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
        /// One round of the population, copied and checked. An empty round is
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

            var copied = new RoundOrders[members.Count];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = members[index]
                    ?? throw new SimulationException(
                        "The pool's member at index "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " of round "
                        + (round + 1).ToString(CultureInfo.InvariantCulture)
                        + " is nothing at all. Every member of a field is a defense and a wave, because a "
                        + "round measures both directions against each of them.");
            }

            return copied;
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
        private static bool Nobody(IReadOnlyList<IReadOnlyList<RoundOrders>> rounds)
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
        /// The members recorded at this round, or the deepest round's members
        /// for a round the pool does not reach.
        /// </summary>
        private RoundOrders[] Recorded(int round)
        {
            if (round < 0)
            {
                throw new SimulationException(
                    "The pool was asked who was recorded at round "
                    + round.ToString(CultureInfo.InvariantCulture)
                    + ". Rounds are counted from zero, so a negative one is an index that was derived "
                    + "rather than a round anybody played.");
            }

            return _rounds[round < _rounds.Length ? round : _rounds.Length - 1];
        }
    }
}
