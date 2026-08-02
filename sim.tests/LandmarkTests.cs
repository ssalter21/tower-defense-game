namespace Sim.Tests;

/// <summary>
/// The landmark table: the handful of ticks the sit-down checklist is written
/// against, derived from the event stream and checked against what a view would
/// have seen.
/// </summary>
/// <remarks>
/// <para>
/// The tests that matter here are the two that hold the derivation against an
/// oracle. The command line pulls no snapshots, so it can only know what the
/// simulation told it -- and the way that goes wrong is not with a loud failure
/// but with a plausible tick number that is two out. So each of the two
/// landmarks nothing else in the event stream implies is asserted equal to the
/// tick a snapshot-watching observer would have called it, which is exactly the
/// observer the Tier 3 checklist puts in front of the screen.
/// </para>
/// </remarks>
public class LandmarkTests
{
    [Fact]
    public void The_committed_table_is_what_a_run_of_the_committed_match_reports()
    {
        // The golden check, in process. The build gate also runs the command
        // line itself and compares the file byte for byte; this is the half
        // that is true on a laptop with nothing but `dotnet test`.
        Assert.Equal(
            TheMatch.DataRows(RepoLayout.LandmarksFile),
            TheMatch.LandmarksOfTheCommittedRun().ToText().Split('\n'));
    }

    [Fact]
    public void The_first_overtake_is_the_tick_a_watcher_would_have_called_it()
    {
        // Ids ascend with spawn order, so a creep with a higher id further along
        // the corridor than one with a lower id is an overtake. That is what a
        // person at the screen sees, and it is the oracle the event-derived
        // number has to agree with -- because a landmark that is a few ticks
        // out does not look wrong, it just sends somebody to the wrong second
        // of the match.
        Match match = TheMatch.Fresh();
        int watched = 0;

        while (!match.IsFinished && watched == 0)
        {
            match.Advance(1);
            Snapshot snapshot = match.PullSnapshot();

            for (int index = 1; index < snapshot.Creeps.Count; index++)
            {
                if (snapshot.Creeps[index].DistanceAlongPath > snapshot.Creeps[index - 1].DistanceAlongPath)
                {
                    watched = snapshot.Tick;
                    break;
                }
            }
        }

        Assert.True(watched > 0, "Nothing ever overtook anything, so there was no oracle to compare against.");
        Assert.Equal(watched, TickOf(Landmarks.FirstOvertake));
    }

    [Fact]
    public void The_orphaned_projectile_is_the_tick_one_left_the_snapshot_without_landing()
    {
        // The same shape of oracle for the other landmark the event stream
        // alone could not imply: a projectile that was in the picture last tick
        // and is not in this one, with too little flight time behind it to have
        // arrived.
        Match match = TheMatch.Fresh();
        var previous = new List<ProjectileSnapshot>();
        int watched = 0;

        while (!match.IsFinished && watched == 0)
        {
            match.Advance(1);
            Snapshot snapshot = match.PullSnapshot();

            foreach (ProjectileSnapshot gone in previous)
            {
                bool stillFlying = snapshot.Projectiles.Any(projectile => projectile.Id == gone.Id);

                if (!stillFlying && gone.TicksInFlight + 1 < gone.FlightDurationTicks)
                {
                    watched = snapshot.Tick;
                    break;
                }
            }

            previous = snapshot.Projectiles.ToList();
        }

        Assert.True(watched > 0, "No projectile was ever orphaned, so there was no oracle to compare against.");
        Assert.Equal(watched, TickOf(Landmarks.Orphaned));
    }

    [Fact]
    public void The_committed_run_reports_the_two_moments_only_this_stream_carries()
    {
        // Not just once, either. A landmark table whose two hardest rows each
        // came from the only time that thing ever happened would be a table
        // resting on a coincidence.
        var events = new TheMatch.EventLog();
        TheMatch.Fresh().Resolve(events);

        Assert.True(events.CountOf("orphaned") > 1, "Projectiles are hardly ever orphaned in this match.");
        Assert.True(events.CountOf("overtook") > 1, "Creeps hardly ever pass each other in this match.");
    }

    [Fact]
    public void A_pass_is_reported_on_the_tick_it_happens_and_not_on_every_tick_after_it()
    {
        // The failure this rules out is an event that restates a state: two
        // creeps stay in the order a pass left them for the rest of the match,
        // and an event firing every tick of that would be a fact about the
        // world rather than a thing that happened in it.
        var events = new TheMatch.EventLog();
        TheMatch.Fresh().Resolve(events);

        var pairs = new List<string>();

        for (int index = 0; index < events.Count; index++)
        {
            if (events.Kinds[index] == "overtook")
            {
                pairs.Add($"{events.Subjects[index]} over {events.Amounts[index]}");
            }
        }

        Assert.NotEmpty(pairs);
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Fact]
    public void Listening_to_a_match_does_not_change_it()
    {
        // The two new events are told, not decided. If either of them had
        // reached into the tick loop, this is where it would show.
        MatchResult heard = TheMatch.Fresh().Resolve(new TheMatch.EventLog());
        MatchResult silent = TheMatch.Fresh().Resolve();

        Assert.Equal(silent.RollingStateHash, heard.RollingStateHash);
        Assert.Equal(silent.FinalTick, heard.FinalTick);
        Assert.Equal(silent.Leaked, heard.Leaked);
    }

    [Fact]
    public void A_table_with_a_hole_in_it_refuses_to_render()
    {
        // A defense of nothing but hitscan towers launches no projectiles, so
        // nothing can ever be orphaned. That is a match the checklist cannot be
        // written against, and the moment to find that out is here -- by name --
        // rather than at the sit-down, in front of a row pointing at nothing.
        UnitTypeTable types = TheMatch.Types();
        TowerLayout hitscanOnly = TowerLayout.Parse(
            string.Join("\n", File.ReadAllLines(RepoLayout.DefenseFile).Where(line => !line.StartsWith("tower   4"))),
            types);

        Assert.Equal(4, hitscanOnly.Count);

        var match = new Match(TheMatch.Map(), hitscanOnly, TheMatch.Wave(types), TheMatch.Seed);
        var landmarks = new Landmarks();

        while (!match.IsFinished)
        {
            landmarks.EnteringTick(match.Tick + 1);
            match.Advance(1, landmarks);
        }

        Assert.Equal(Landmarks.Orphaned, landmarks.Missing);

        SimulationException thrown = Assert.Throws<SimulationException>(() => landmarks.ToText());
        Assert.Contains(Landmarks.Orphaned, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Ticks_that_arrive_in_a_lump_refuse_rather_than_being_filed_under_one_number()
    {
        // Every event of a hundred ticks recorded as having happened on one of
        // them is the failure that looks like nothing at all.
        var landmarks = new Landmarks();
        landmarks.EnteringTick(1);

        SimulationException thrown = Assert.Throws<SimulationException>(() => landmarks.EnteringTick(101));

        Assert.Contains("one at a time", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_landmark_that_nothing_dated_refuses_rather_than_landing_on_tick_zero()
    {
        // What a caller who resolves the whole match in one call would get: a
        // listener that was never told which tick anything was on.
        var landmarks = new Landmarks();

        SimulationException thrown = Assert.Throws<SimulationException>(
            () => TheMatch.Fresh().Resolve(landmarks));

        Assert.Contains("which tick", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_landmark_is_a_tick_the_committed_match_actually_had()
    {
        Landmarks landmarks = TheMatch.LandmarksOfTheCommittedRun();

        Assert.Equal(4, landmarks.Rows.Count);
        Assert.Null(landmarks.Missing);

        foreach (Landmark landmark in landmarks.Rows)
        {
            Assert.InRange(landmark.Tick, 1, TheMatch.FinalTickOfTheCommittedRun);
            Assert.True(landmark.Who > 0, $"{landmark.Name} names no entity.");
        }

        // The last creep to die is the last thing any of these can be about, so
        // every other row sits at or before it.
        int last = TickOf(Landmarks.LastCreepDies);

        foreach (Landmark landmark in landmarks.Rows)
        {
            Assert.True(
                landmark.Tick <= last,
                $"{landmark.Name} is on tick {landmark.Tick}, after the last creep died on {last}.");
        }
    }

    private static int TickOf(string name)
    {
        Landmarks landmarks = TheMatch.LandmarksOfTheCommittedRun();

        foreach (Landmark landmark in landmarks.Rows)
        {
            if (landmark.Name == name)
            {
                return landmark.Tick;
            }
        }

        throw new InvalidOperationException($"The committed run reported no '{name}'.");
    }
}
