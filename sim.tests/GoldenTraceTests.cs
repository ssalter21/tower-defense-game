namespace Sim.Tests;

/// <summary>
/// The rolling per-tick state hash, checked against the trace a real run
/// produced and committed.
/// </summary>
public class GoldenTraceTests
{
    [Fact]
    public void The_match_agrees_with_the_committed_trace_at_every_tick()
    {
        // Per tick, not at the end. An end-of-match comparison would say the
        // run diverged; this says which tick it diverged on, which is the
        // difference between a bug that can be bisected and one that can only
        // be stared at.
        GoldenTrace trace = TheMatch.Trace();
        Match match = TheMatch.Fresh();

        trace.Check(0, match.StateHash);

        while (!match.IsFinished)
        {
            match.Advance(1);
            trace.Check(match.Tick, match.StateHash);
        }

        Assert.Equal(trace.FinalTick, match.Result().FinalTick);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun + 1, trace.Count);
    }

    [Fact]
    public void A_trace_that_disagrees_names_the_tick_it_disagreed_on()
    {
        // The negative control. Without it the check above is a comparison
        // whose failure path nobody has ever seen run.
        const int Doctored = 900;

        string[] lines = File.ReadAllLines(RepoLayout.GoldenTraceFile);

        for (int index = 0; index < lines.Length; index++)
        {
            if (lines[index].StartsWith($"tick {Doctored} ", StringComparison.Ordinal))
            {
                lines[index] = $"tick {Doctored} 0123456789ABCDEF";
            }
        }

        GoldenTrace doctored = GoldenTrace.Parse(string.Join("\n", lines));
        Match match = TheMatch.Fresh();
        doctored.Check(0, match.StateHash);

        DesyncException thrown = Assert.Throws<DesyncException>(() =>
        {
            while (!match.IsFinished)
            {
                match.Advance(1);
                doctored.Check(match.Tick, match.StateHash);
            }
        });

        Assert.Equal(Doctored, thrown.Tick);
        Assert.Equal(Hash64.FromValue(0x0123456789ABCDEFUL), thrown.Expected);
        Assert.NotEqual(thrown.Expected, thrown.Actual);
    }

    [Fact]
    public void The_hash_covers_where_the_dice_are_up_to()
    {
        // Two matches that differ only in the seed. Before the first shot is
        // fired nothing random has happened, so their snapshots are identical
        // field for field -- and their state hashes are not, because the
        // position of the dice stream is in the fold and in nothing a view can
        // see. The seed itself is deliberately not folded, so this fails the
        // moment the stream's position stops being.
        Match one = TheMatch.Fresh(TheMatch.Seed);
        Match other = TheMatch.Fresh(TheMatch.Seed + 1);

        for (int tick = 0; tick <= 3; tick++)
        {
            AssertSnapshotsAgree(one, other);
            Assert.NotEqual(other.StateHash, one.StateHash);

            one.Advance(1);
            other.Advance(1);
        }
    }

    [Fact]
    public void The_hash_covers_a_counter_the_snapshot_has_no_field_for()
    {
        // A tower between shots looks idle, so the snapshot says Idle and says
        // nothing about how long it has left. Here are two matches whose unit
        // tables differ by exactly one number -- that cooldown -- run to the
        // moment after the first shot: everything a view could draw is
        // identical, and the hashes are not. That is the whole claim, which is
        // "a desync in a field the view never sees is still caught", with a
        // field the view never sees.
        UnitTypeTable types = TheMatch.Types();
        UnitTypeTable slower = UnitTypeTable.Parse(
            File.ReadAllText(RepoLayout.UnitsFile).Replace(
                "unit   3   archer            placed  0      0      3200   18 ",
                "unit   3   archer            placed  0      0      3200   19 ",
                StringComparison.Ordinal));

        Assert.Equal(18, types.ById(3).CooldownTicks);
        Assert.Equal(19, slower.ById(3).CooldownTicks);

        Ruleset rules = TheRuleset.Committed();
        Match one = new(TheMatch.Map(), rules, TheMatch.Layout(types), TheMatch.Wave(types), TheMatch.Seed);
        Match other = new(TheMatch.Map(), rules, TheMatch.Layout(slower), TheMatch.Wave(slower), TheMatch.Seed);

        int agreed = 0;
        int caught = 0;

        for (int tick = 0; tick < 40; tick++)
        {
            one.Advance(1);
            other.Advance(1);

            if (!SnapshotsAgree(one, other))
            {
                break;
            }

            agreed++;

            if (one.StateHash != other.StateHash)
            {
                caught++;
            }
        }

        Assert.True(agreed > 10, "The two runs parted company on screen before the cooldowns could differ.");
        Assert.True(caught > 0, "The cooldown counter drifted and the rolling hash never noticed.");
    }

    [Fact]
    public void A_trace_with_a_gap_in_it_refuses_to_load()
    {
        // A missing tick is a tick nothing would be compared on, which is the
        // one tick a divergence would then be free to start at.
        ContentException thrown = Assert.Throws<ContentException>(() => GoldenTrace.Parse("""
            tick 0 0000000000000001
            tick 2 0000000000000002
            """));

        Assert.Contains("no gaps", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_hash_that_is_not_sixteen_hexadecimal_digits_refuses_to_load()
    {
        Assert.Throws<ContentException>(() => GoldenTrace.Parse("tick 0 00FF"));
        Assert.Throws<ContentException>(() => GoldenTrace.Parse("tick 0 0123456789abcdef"));
    }

    [Fact]
    public void The_committed_trace_covers_the_whole_match_and_nothing_more()
    {
        GoldenTrace trace = TheMatch.Trace();

        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, trace.FinalTick);
        Assert.Throws<ArgumentOutOfRangeException>(() => trace.At(trace.Count));
    }

    private static void AssertSnapshotsAgree(Match one, Match other)
    {
        Assert.True(SnapshotsAgree(one, other), $"The two runs already differ on screen at tick {one.Tick}.");
    }

    /// <summary>
    /// Whether two runs are drawing the same picture: every field of every
    /// snapshot entity, which is the whole of what a view is allowed to know.
    /// </summary>
    private static bool SnapshotsAgree(Match one, Match other)
    {
        Snapshot left = one.PullSnapshot();
        Snapshot right = other.PullSnapshot();

        if (left.Tick != right.Tick
            || left.Creeps.Count != right.Creeps.Count
            || left.Towers.Count != right.Towers.Count
            || left.Projectiles.Count != right.Projectiles.Count)
        {
            return false;
        }

        for (int index = 0; index < left.Creeps.Count; index++)
        {
            CreepSnapshot a = left.Creeps[index];
            CreepSnapshot b = right.Creeps[index];

            if (a.Id != b.Id
                || a.TypeId != b.TypeId
                || a.Hp != b.Hp
                || a.DistanceAlongPath != b.DistanceAlongPath
                || a.LateralOffset != b.LateralOffset
                || a.State != b.State
                || a.TicksInState != b.TicksInState)
            {
                return false;
            }
        }

        for (int index = 0; index < left.Towers.Count; index++)
        {
            TowerSnapshot a = left.Towers[index];
            TowerSnapshot b = right.Towers[index];

            if (a.Id != b.Id || a.State != b.State || a.TargetId != b.TargetId || a.TicksInState != b.TicksInState)
            {
                return false;
            }
        }

        for (int index = 0; index < left.Projectiles.Count; index++)
        {
            ProjectileSnapshot a = left.Projectiles[index];
            ProjectileSnapshot b = right.Projectiles[index];

            if (a.Id != b.Id
                || a.TypeId != b.TypeId
                || a.Target != b.Target
                || a.TicksInFlight != b.TicksInFlight
                || a.FlightDurationTicks != b.FlightDurationTicks)
            {
                return false;
            }
        }

        return true;
    }
}
