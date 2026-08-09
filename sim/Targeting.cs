using System;

namespace Sim
{
    /// <summary>
    /// A creep a tower can reach, as target selection sees it: an id and how
    /// far along the route it is, and nothing else.
    /// </summary>
    /// <remarks>
    /// Health, type and armour are absent because the rule does not read them.
    /// A selector handed them could grow a preference for the weakest or the
    /// most dangerous target without anything having to be added to it, and the
    /// first anybody would know is a replay that no longer reproduces.
    /// </remarks>
    public readonly struct WalkingTarget
    {
        public WalkingTarget(int id, Fix64 distanceAlongRoute)
        {
            Id = id;
            DistanceAlongRoute = distanceAlongRoute;
        }

        /// <summary>The creep's entity id, which is also its spawn order.</summary>
        public int Id { get; }

        /// <summary>How far along the route it has walked, in hexes from the entrance.</summary>
        public Fix64 DistanceAlongRoute { get; }
    }

    /// <summary>
    /// Which creep a tower shoots at. The whole of the acquisition rule, as a
    /// question that can be asked rather than one that has to be run.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The rule is a total order, which is the only kind of rule that can be
    /// replayed.</b> "The closest to the exit" alone is not one, because two
    /// creeps at the same distance would leave the answer to whichever happened
    /// to be looked at first, and that is exactly the sort of thing that differs
    /// between runs for reasons nobody can see. The lower id settles it.
    /// </para>
    /// <para>
    /// <b>Ties are counted rather than merely broken.</b> The count goes into
    /// the match's rolling state hash and into nothing a view can see, which
    /// makes it a detector: two runs that disagree about unit ordering will
    /// usually agree on everything visible for a while and disagree here
    /// immediately. A tie is counted between two candidates neither of which
    /// wins, because the point is that the level pair happened.
    /// </para>
    /// <para>
    /// <b>The static takes a span and holds nothing.</b> The match projects the
    /// creeps a tower can reach onto one and asks; it keeps no copy of the rule
    /// and there is no second spelling of it to drift. Nothing here allocates,
    /// because seeking re-simulates and the tick loop's cost is the seek's.
    /// </para>
    /// </remarks>
    public static class Targeting
    {
        /// <summary>
        /// Which of the creeps a tower can reach it shoots at: whichever is
        /// furthest along the corridor, and the lowest id of those if two are
        /// level.
        /// </summary>
        /// <param name="reachable">
        /// The walking creeps in range, in ascending id order. The chosen
        /// candidate does not depend on that order but
        /// <paramref name="tiebreaksBroken"/> does, because a tie is counted
        /// against the best seen so far.
        /// </param>
        /// <param name="tiebreaksBroken">
        /// How many candidates were level with the best at the moment they were
        /// looked at.
        /// </param>
        /// <returns>The index into <paramref name="reachable"/>, or -1 when it is empty.</returns>
        public static int Chosen(ReadOnlySpan<WalkingTarget> reachable, out int tiebreaksBroken)
        {
            tiebreaksBroken = 0;

            if (reachable.Length == 0)
            {
                return -1;
            }

            int best = 0;

            for (int index = 1; index < reachable.Length; index++)
            {
                if (reachable[index].DistanceAlongRoute > reachable[best].DistanceAlongRoute)
                {
                    best = index;
                }
                else if (reachable[index].DistanceAlongRoute == reachable[best].DistanceAlongRoute)
                {
                    tiebreaksBroken++;

                    if (reachable[index].Id < reachable[best].Id)
                    {
                        best = index;
                    }
                }
            }

            return best;
        }
    }
}
