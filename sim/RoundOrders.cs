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

        public override string ToString() =>
            Defense.Count.ToString(CultureInfo.InvariantCulture)
            + " towers standing, "
            + Wave.TotalUnits.ToString(CultureInfo.InvariantCulture)
            + " units sent";
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
    /// </remarks>
    public sealed class FieldPool
    {
        private readonly RoundOrders[] _members;

        private FieldPool(RoundOrders[] members)
        {
            _members = members;
        }

        /// <summary>How many rounds are in the population.</summary>
        public int Size => _members.Length;

        /// <summary>The population, in the order it was given.</summary>
        public IReadOnlyList<RoundOrders> Members => _members;

        /// <summary>The pool, copied, so that what a run draws from cannot change under it.</summary>
        public static FieldPool Of(IReadOnlyList<RoundOrders> members)
        {
            if (members is null)
            {
                throw new ArgumentNullException(nameof(members));
            }

            if (members.Count == 0)
            {
                throw new SimulationException(
                    "A run was given a pool of nobody to draw its field from. A round is resolved against "
                    + "opponents, and there is no drawing one out of an empty population -- a run with "
                    + "nothing to fight is a harness that was pointed at no ghosts rather than a run whose "
                    + "field happens to be quiet.");
            }

            var copied = new RoundOrders[members.Count];

            for (int index = 0; index < copied.Length; index++)
            {
                copied[index] = members[index]
                    ?? throw new SimulationException(
                        "The pool's member at index "
                        + index.ToString(CultureInfo.InvariantCulture)
                        + " is nothing at all. Every member of a field is a defense and a wave, because a "
                        + "round measures both directions against each of them.");
            }

            return new FieldPool(copied);
        }

        /// <summary>The member at this index.</summary>
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
    }
}
