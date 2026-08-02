namespace Sim.Tests;

/// <summary>
/// The replay bundle: self-contained, carrying the seed the defense refuses to
/// carry, and cross-checking its own contents.
/// </summary>
public class ReplayBundleTests
{
    [Fact]
    public void A_bundle_is_the_header_the_seed_the_map_the_defense_and_the_wave_and_nothing_else()
    {
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = TheMatch.Bundle();

        int cells = bundle.Map.Width * bundle.Map.Height;
        int ghost = TheMatch.Ghost(types).ToBytes().Length;
        int wave = TheMatch.WaveOf(types).ToBytes().Length;

        Assert.Equal(135, cells);
        Assert.Equal(18 + 8 + 2 + 2 + cells + ghost + wave, bundle.ToBytes().Length);
    }

    [Fact]
    public void A_bundle_read_back_and_written_out_again_is_the_same_bytes()
    {
        byte[] bytes = TheMatch.Bundle().ToBytes();

        Assert.Equal(bytes, ReplayBundle.FromBytes(bytes).ToBytes());
    }

    [Fact]
    public void A_bundle_read_back_is_the_bundle_it_was_written_from()
    {
        ReplayBundle bundle = TheMatch.Bundle();
        ReplayBundle read = ReplayBundle.FromBytes(bundle.ToBytes());

        Assert.Equal(bundle.Header, read.Header);
        Assert.Equal(bundle.Seed, read.Seed);
        Assert.Equal(bundle.Map.MapHash, read.Map.MapHash);
        Assert.Equal(bundle.Ghost, read.Ghost);
        Assert.Equal(bundle.Wave, read.Wave);
    }

    [Fact]
    public void The_bundle_carries_everything_a_match_needs_and_consults_no_registry()
    {
        // Self-contained: the seed, the map inlined as the parsed grid, the
        // defense and the wave. The only thing that has to be handed in is the
        // ruleset, which the content hash pins.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        Assert.Equal(TheMatch.Seed, bundle.Seed);
        Assert.Equal(TheMatch.Map().MapHash, bundle.Map.MapHash);
        Assert.Equal(TheMatch.Map().Route.Count, bundle.Map.Route.Count);
        Assert.Equal(6, bundle.Ghost.Count);
        Assert.Equal(6, bundle.Wave.Count);
    }

    [Fact]
    public void The_inlined_map_is_exactly_what_the_map_hash_covers()
    {
        // Width, height and the cell kinds row-major, which is the same thing
        // the map hash is folded over -- so the grid a replay carries and the
        // grid its hash pins cannot drift apart.
        ReplayBundle bundle = TheMatch.Bundle();
        byte[] bytes = bundle.ToBytes();
        byte[] cells = TheMatch.Map().ToCellBytes();

        for (int index = 0; index < cells.Length; index++)
        {
            Assert.Equal(cells[index], bytes[RecordBytes.BundleCellsOffset + index]);
        }

        Assert.Equal(TheMatch.Map().MapHash, bundle.Ghost.MapHash);
    }

    [Fact]
    public void The_seed_is_in_the_match_record_and_changing_it_does_not_change_the_defense()
    {
        // The seed lives here and nowhere else. Putting it in the defense would
        // make rolling different dice a different defense -- a different id, and
        // every replay pointing at the old one orphaned.
        ReplayBundle first = TheMatch.Bundle(TheMatch.Seed);
        ReplayBundle second = TheMatch.Bundle(TheMatch.Seed + 1);

        Assert.NotEqual(first.Seed, second.Seed);
        Assert.NotEqual(first.ToBytes(), second.ToBytes());
        Assert.Equal(first.GhostId, second.GhostId);
        Assert.Equal(first.WaveId, second.WaveId);
        Assert.Equal(first.Ghost.ToBytes(), second.Ghost.ToBytes());
    }

    [Fact]
    public void The_ids_of_the_records_inside_a_bundle_are_the_ids_they_have_on_their_own()
    {
        // Derived from the inner byte ranges, so "this wave goes with this
        // defense" is true by construction rather than by a field.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        Assert.Equal(RecordId.Of(TheMatch.Ghost(types).ToBytes()), bundle.GhostId);
        Assert.Equal(RecordId.Of(TheMatch.WaveOf(types).ToBytes()), bundle.WaveId);
        Assert.NotEqual(bundle.GhostId, bundle.WaveId);
    }

    [Fact]
    public void The_inlined_map_gets_the_same_corridor_assertion_a_map_file_gets()
    {
        // Not a second implementation: HexMap.FromCells traces the corridor with
        // the code HexMap.Parse traces it with, so the maps the two would
        // disagree about do not exist.
        HexMap map = TheMatch.Map();
        HexMap rebuilt = HexMap.FromCells("inlined", map.Width, map.Height, map.ToCellBytes());

        Assert.Equal(map.MapHash, rebuilt.MapHash);
        Assert.Equal(map.Route.Count, rebuilt.Route.Count);
        Assert.Equal(map.Spawn, rebuilt.Spawn);
        Assert.Equal(map.Exit, rebuilt.Exit);
    }

    [Fact]
    public void The_committed_bundle_on_disk_is_the_committed_content_recorded()
    {
        // The fixture the command line loads, and the one the version bump will
        // be rehearsed against. It came out of the record verb rather than out
        // of a hex editor, and this is what says so: the same writer, over the
        // same content, at the same seed, produces the same bytes.
        byte[] onDisk = File.ReadAllBytes(RepoLayout.BundleFile);

        Assert.Equal(TheMatch.Bundle().ToBytes(), onDisk);

        ReplayBundle read = ReplayBundle.FromBytes(onDisk);

        Assert.Equal(TheMatch.Seed, read.Seed);
        Assert.Equal(0, read.Header.FormatVersion);
        Assert.Equal(SimulationVersion.Current, read.Header.SimVersion);
        Assert.Equal(TheMatch.Types().ContentHash, read.Header.ContentHash);
    }

    [Fact]
    public void The_committed_bundle_replays_to_the_committed_result()
    {
        // Through the replay gate, not around it: the file on disk is a record
        // this build is allowed to simulate, and simulating it is the match the
        // whole skeleton is built around.
        UnitTypeTable types = TheMatch.Types();
        MatchResult result = ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.BundleFile))
            .Replay(types)
            .Resolve();

        Assert.Equal(TheMatch.LeakedInTheCommittedRun, result.Leaked);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
        Assert.Equal(TheMatch.Trace().At(TheMatch.FinalTickOfTheCommittedRun), result.RollingStateHash);
    }

    [Fact]
    public void A_grid_whose_shape_and_contents_disagree_refuses()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => HexMap.FromCells("inlined", 5, 3, new byte[14]));

        Assert.Contains("no unambiguous reading", thrown.Message, StringComparison.Ordinal);
    }
}
