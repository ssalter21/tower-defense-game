namespace Sim.Tests;

/// <summary>
/// The acquisition rule on its own: which creep a tower shoots at, asked
/// directly rather than by building a map, a roster, a defense and a wave,
/// running a match and reading the answer back out of a snapshot.
/// </summary>
/// <remarks>
/// <para>
/// Everything here is a span somebody wrote down. That is the point of the
/// seam: the interesting cases are the ones a real match reaches by luck --
/// two creeps exactly level, a creep leaving the candidate set on the tick it
/// dies -- and a case reached by luck is a case that stops being tested the day
/// the luck runs out. The tie assertion in <see cref="MatchTests"/> did exactly
/// that on 8 August 2026.
/// </para>
/// <para>
/// The match-driven oracle in <see cref="MatchTests"/> is still worth having
/// and asserts something these cannot: that the match really does acquire by
/// this rule. These say what the rule is.
/// </para>
/// </remarks>
public class TargetingTests
{
    [Fact]
    public void A_tower_that_reaches_nothing_walking_shoots_at_nothing()
    {
        Assert.Equal(-1, Targeting.Chosen(ReadOnlySpan<WalkingTarget>.Empty, out int ties));
        Assert.Equal(0, ties);
    }

    [Fact]
    public void The_creep_closest_to_the_exit_is_the_one_that_gets_shot()
    {
        // The answer is an index into the span rather than an id, so the
        // caller keeps whatever else it knows about that candidate.
        WalkingTarget[] reachable = { At(5, 1), At(6, 9), At(7, 4) };

        Assert.Equal(1, Targeting.Chosen(reachable, out int ties));
        Assert.Equal(0, ties);
    }

    [Fact]
    public void Two_creeps_level_with_each_other_are_settled_on_the_lower_id_and_counted()
    {
        // The tie, constructed. "Closest to the exit" on its own is not a rule
        // that can be replayed, because it leaves the answer to whichever of
        // the two happened to be looked at first.
        //
        // OBSERVED: change the distance comparison in Targeting.Chosen from
        // greater-than to greater-or-equal, which is what dropping the lower-id
        // clause and keeping "furthest along" comes to. This goes red picking
        // 6, and the golden trace goes red with it.
        WalkingTarget[] level = { At(5, 3), At(6, 3) };

        Assert.Equal(5, ChosenId(level, out int ties));
        Assert.Equal(1, ties);
    }

    [Fact]
    public void The_lower_id_wins_whichever_end_of_the_span_it_is_at()
    {
        // A total order, not a scan order. The match holds its creeps in
        // ascending id and so never hands over the second of these, which is
        // exactly why the rule is written out rather than left to the ordering
        // to imply: the day something hands over a differently ordered span,
        // the answer must not move.
        Assert.Equal(5, ChosenId(new[] { At(5, 3), At(6, 3) }, out _));
        Assert.Equal(5, ChosenId(new[] { At(6, 3), At(5, 3) }, out _));
    }

    [Fact]
    public void Every_candidate_level_with_the_best_is_counted()
    {
        // Four abreast is three ties. The count is not "how many shots were
        // ambiguous" -- it is how many level pairs the rule had to settle.
        WalkingTarget[] column = { At(5, 3), At(6, 3), At(7, 3), At(8, 3) };

        Assert.Equal(5, ChosenId(column, out int ties));
        Assert.Equal(3, ties);
    }

    [Fact]
    public void A_tie_neither_of_them_wins_is_still_counted()
    {
        // A level pair behind the leader is still a level pair, so it is
        // counted even though the shot goes elsewhere.
        WalkingTarget[] reachable = { At(5, 2), At(6, 2), At(7, 9) };

        Assert.Equal(7, ChosenId(reachable, out int ties));
        Assert.Equal(1, ties);
    }

    [Fact]
    public void A_creep_released_this_tick_joins_the_span_at_the_entrance()
    {
        // One of the two ticks the match-driven oracle cannot speak for. The
        // wave releases at the end of a tick, after the towers have chosen, so
        // a creep that spawned this tick is in the picture pulled afterwards
        // and was not in the candidate set --
        // MatchTests.A_creep_first_appears_at_the_entrance_having_walked_nowhere
        // is the half of that a picture can see. Asked here, the tick is two
        // spans.
        //
        // A spawn arrives with the highest id yet and at distance zero, which
        // is as far from the exit as a candidate can be, so it changes neither
        // the choice nor the tie count while anything at all is ahead of it.
        WalkingTarget[] before = { At(5, 3), At(6, 3) };
        WalkingTarget[] after = { At(5, 3), At(6, 3), At(7, 0) };

        Assert.Equal(5, ChosenId(before, out int tiesBefore));
        Assert.Equal(5, ChosenId(after, out int tiesAfter));
        Assert.Equal(1, tiesBefore);
        Assert.Equal(1, tiesAfter);

        // And it is the shot the moment it is the only thing in reach, which
        // is what a tower covering the entrance sees.
        Assert.Equal(7, ChosenId(new[] { At(7, 0) }, out _));
    }

    [Fact]
    public void A_creep_that_died_this_tick_is_not_in_the_span_and_takes_its_tie_with_it()
    {
        // The other tick the oracle cannot speak for, and the worse one: the
        // creep the towers looked at is no longer walking by the time the
        // picture is pulled, so the snapshot cannot say what the candidates
        // were. The array is being mutated underneath the answer, which is
        // where targeting is most likely to be wrong.
        WalkingTarget[] alive = { At(5, 3), At(6, 3), At(7, 1) };
        WalkingTarget[] dead = { At(6, 3), At(7, 1) };

        Assert.Equal(5, ChosenId(alive, out int tiesAlive));
        Assert.Equal(1, tiesAlive);

        // The creep that was taking the shot dies; the one it was level with
        // inherits it, and the tie goes with it because there is nothing left
        // to be level with.
        Assert.Equal(6, ChosenId(dead, out int tiesDead));
        Assert.Equal(0, tiesDead);
    }

    [Fact]
    public void Asking_the_question_allocates_nothing()
    {
        // Seeking re-simulates rather than reading a cache, so anything the
        // tick path allocates is a cost every scrub of the slider pays. The
        // span is a stack value, a candidate is a struct, and nothing here
        // closes over anything or boxes -- measured rather than argued.
        //
        // OBSERVED: give Chosen a `new List<int>()` it never reads. This goes
        // red at 32,000 bytes against a permitted zero.
        WalkingTarget[] reachable = { At(5, 3), At(6, 3), At(7, 1) };

        for (int warm = 0; warm < 100; warm++)
        {
            Targeting.Chosen(reachable, out _);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int run = 0; run < 1000; run++)
        {
            Targeting.Chosen(reachable, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    /// <summary>A candidate at a whole number of hexes along the route.</summary>
    private static WalkingTarget At(int id, int distanceAlongRoute) =>
        new WalkingTarget(id, Fix64.FromInt(distanceAlongRoute));

    /// <summary>
    /// The id of the chosen candidate, or zero for nothing -- which is the
    /// projection the match makes off the index it gets back.
    /// </summary>
    private static int ChosenId(WalkingTarget[] reachable, out int tiebreaksBroken)
    {
        int chosen = Targeting.Chosen(reachable, out tiebreaksBroken);
        return chosen < 0 ? 0 : reachable[chosen].Id;
    }
}
