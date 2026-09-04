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
}
