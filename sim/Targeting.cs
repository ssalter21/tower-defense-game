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
            Span<int> one = stackalloc int[1];

            return Chosen(reachable, one, out tiebreaksBroken) == 0 ? -1 : one[0];
        }

        /// <summary>
        /// The same rule, asked for as many creeps as a shot has shots: the
        /// first <paramref name="chosen"/>.Length of them in the order the rule
        /// puts them in, nearest the exit first.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>It is the same total order taken further along, not a second
        /// rule.</b> One shot is this with room for one answer, which is what
        /// the overload above is -- so a Marksman firing three and an Archer
        /// firing one acquire by one comparison, and the day the rule moves,
        /// both move.
        /// </para>
        /// <para>
        /// <b><paramref name="tiebreaksBroken"/> is counted against the best
        /// seen so far and against nothing else.</b> It is span-order-dependent
        /// by design and it feeds the state hash, which is the whole of its
        /// job: it is the field that moves when two runs disagree about unit
        /// ordering and agree about everything a view can see. Counting a tie
        /// per place in the order instead would have made the count depend on
        /// how many shots the row fires, so a Marksman and an Archer standing in
        /// the same crowd would fold different numbers for the same crowd. What
        /// is counted is how many level pairs the rule had to settle, which is a
        /// fact about the creeps rather than about who is looking at them.
        /// </para>
        /// <para>
        /// <b>The insertion walks a handful of places and allocates nothing.</b>
        /// A shot count is a small integer -- three, for the row that named this
        /// column -- so ordering the whole span to take the head of it would be
        /// the expensive way round, and this is the tick loop.
        /// </para>
        /// </remarks>
        /// <param name="reachable">The walking creeps in range, in ascending id order.</param>
        /// <param name="chosen">
        /// Where the indices go, best first. Its length is how many are wanted;
        /// only the returned count of entries is written.
        /// </param>
        /// <param name="tiebreaksBroken">
        /// How many candidates were level with the best at the moment they were
        /// looked at.
        /// </param>
        /// <returns>How many of <paramref name="chosen"/> were filled.</returns>
        public static int Chosen(
            ReadOnlySpan<WalkingTarget> reachable,
            Span<int> chosen,
            out int tiebreaksBroken)
        {
            tiebreaksBroken = 0;

            if (chosen.Length == 0)
            {
                throw new ArgumentException(
                    "A shot asked target selection for no targets at all. A row fires at least one shot "
                    + "an attack, and a caller with nothing to fill is a caller that should not have "
                    + "asked.",
                    nameof(chosen));
            }

            int found = 0;

            for (int index = 0; index < reachable.Length; index++)
            {
                // The tie is counted against the best of everything looked at so
                // far, which is chosen[0] before this candidate joins the order.
                if (found > 0 && reachable[index].DistanceAlongRoute == reachable[chosen[0]].DistanceAlongRoute)
                {
                    tiebreaksBroken++;
                }

                found = Insert(reachable, chosen, found, index);
            }

            return found;
        }

        /// <summary>
        /// Puts a candidate in its place in the ordered head, dropping whatever
        /// falls off the end.
        /// </summary>
        private static int Insert(
            ReadOnlySpan<WalkingTarget> reachable,
            Span<int> chosen,
            int found,
            int candidate)
        {
            int place = found;

            while (place > 0 && Beats(reachable[candidate], reachable[chosen[place - 1]]))
            {
                place--;
            }

            if (place >= chosen.Length)
            {
                return found;
            }

            int last = found < chosen.Length ? found : chosen.Length - 1;

            for (int index = last; index > place; index--)
            {
                chosen[index] = chosen[index - 1];
            }

            chosen[place] = candidate;

            return found < chosen.Length ? found + 1 : found;
        }

        /// <summary>
        /// The order itself: furthest along the corridor, and the lower id where
        /// two are level. Written once, so the single-target answer and the
        /// n-target one cannot come apart.
        /// </summary>
        private static bool Beats(WalkingTarget candidate, WalkingTarget standing) =>
            candidate.DistanceAlongRoute > standing.DistanceAlongRoute
            || (candidate.DistanceAlongRoute == standing.DistanceAlongRoute && candidate.Id < standing.Id);
    }
}
