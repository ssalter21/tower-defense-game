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

    [Fact]
    public void Asked_for_several_it_answers_with_the_head_of_the_same_order()
    {
        // The top-N rule is the single-target rule taken further along and not
        // a second rule: the first answer of the many is the answer of the one,
        // whatever else it takes.
        //
        // OBSERVED: order the head by ascending distance instead. The single
        // assertion in The_creep_closest_to_the_exit_is_the_one_that_gets_shot
        // goes red with it, which is what says the two are one rule.
        WalkingTarget[] reachable = { At(5, 1), At(6, 9), At(7, 4), At(8, 6) };

        Assert.Equal(new[] { 6, 8, 7 }, ChosenIds(reachable, 3, out int ties));
        Assert.Equal(0, ties);

        Assert.Equal(new[] { 6 }, ChosenIds(reachable, 1, out _));
        Assert.Equal(new[] { 6, 8, 7, 5 }, ChosenIds(reachable, 4, out _));
    }

    [Fact]
    public void Asked_for_more_than_there_are_it_answers_with_what_there_is()
    {
        // A three-shot row in front of two creeps fires two shots, not three at
        // two creeps and not three with one wasted. The count comes back so the
        // caller knows which half of its slice is worth reading.
        Assert.Equal(new[] { 6, 5 }, ChosenIds(new[] { At(5, 1), At(6, 9) }, 3, out _));
        Assert.Empty(ChosenIds(Array.Empty<WalkingTarget>(), 3, out int ties));
        Assert.Equal(0, ties);
    }

    [Fact]
    public void The_lower_id_settles_a_tie_at_every_place_in_the_order()
    {
        // Four abreast, and the whole order is by id because the distances
        // cannot separate them. A rule that only broke the tie at the front
        // would answer 5 and then whatever the span order gave it.
        //
        // OBSERVED: drop the id clause out of Beats. The first entry is still 5
        // -- the scan happens to see it first -- and the rest come back in span
        // order, so this goes red on the second place and not on the first.
        // That is the case a single-target test cannot reach at all.
        WalkingTarget[] column = { At(8, 3), At(6, 3), At(7, 3), At(5, 3) };

        Assert.Equal(new[] { 5, 6, 7, 8 }, ChosenIds(column, 4, out int ties));
        Assert.Equal(3, ties);
    }

    [Fact]
    public void The_tie_count_does_not_depend_on_how_many_shots_asked()
    {
        // It is counted against the best seen so far and against nothing else,
        // so it says how many level pairs the rule had to settle rather than
        // how wide the row firing was. It feeds the state hash, and a count
        // that moved with the shot count would mean a Marksman and an Archer
        // standing in the same crowd folded different numbers for the same
        // crowd.
        WalkingTarget[] column = { At(5, 3), At(6, 3), At(7, 3), At(8, 1) };

        for (int shots = 1; shots <= 4; shots++)
        {
            ChosenIds(column, shots, out int ties);
            Assert.Equal(2, ties);
        }
    }

    [Fact]
    public void Asking_for_several_allocates_nothing_either()
    {
        // The single-target overload's claim, for the shape that has a span to
        // fill. The caller owns the destination -- the match hands over a slice
        // of an array it sized once -- so the rule itself has nothing to
        // allocate.
        WalkingTarget[] reachable = { At(5, 3), At(6, 3), At(7, 1) };
        var chosen = new int[3];

        for (int warm = 0; warm < 100; warm++)
        {
            Targeting.Chosen(reachable, chosen, out _);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int run = 0; run < 1000; run++)
        {
            Targeting.Chosen(reachable, chosen, out _);
        }

        Assert.Equal(0, GC.GetAllocatedBytesForCurrentThread() - before);
    }

    [Fact]
    public void Asking_for_no_targets_at_all_is_a_caller_fault()
    {
        // A row fires at least one shot an attack -- the column is refused
        // below one where it is read -- so a caller with nothing to fill is a
        // caller that should not have asked. Answering "none" would let a tower
        // that never shoots look like a tower that found nothing.
        Assert.Throws<ArgumentException>(
            () => Targeting.Chosen(new[] { At(5, 3) }, Array.Empty<int>(), out _));
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

    /// <summary>
    /// The ids of the candidates a row firing that many shots takes, in the
    /// order the rule puts them in -- which is the projection the match makes
    /// off the indices it gets back.
    /// </summary>
    private static int[] ChosenIds(WalkingTarget[] reachable, int shots, out int tiebreaksBroken)
    {
        var chosen = new int[shots];
        int found = Targeting.Chosen(reachable, chosen, out tiebreaksBroken);
        var ids = new int[found];

        for (int index = 0; index < found; index++)
        {
            ids[index] = reachable[chosen[index]].Id;
        }

        return ids;
    }
}
