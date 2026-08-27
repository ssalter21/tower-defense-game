namespace Sim.Tests;

/// <summary>
/// The replay bundle: self-contained, carrying the seed the defense refuses to
/// carry, and cross-checking its own contents.
/// </summary>
public class ReplayBundleTests
{
    [Fact]
    public void A_bundle_is_the_header_the_ruleset_the_seed_the_map_the_defense_and_the_wave_and_nothing_else()
    {
        // Header, ruleset hash, seed, width, height, the terrain grid, the level
        // grid, the defense and the wave -- nothing else fits. The second plane
        // of two hundred and forty-seven bytes is the whole of format version 2;
        // if this number grows again without the format version moving, that is
        // the mistake this assertion catches.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = TheMatch.Bundle();

        int cells = bundle.Map.Width * bundle.Map.Height;
        int ghost = TheMatch.Ghost(types).ToBytes().Length;
        int wave = TheMatch.WaveOf(types).ToBytes().Length;

        Assert.Equal(247, cells);
        Assert.Equal(18 + 8 + 8 + 2 + 2 + cells + cells + ghost + wave, bundle.ToBytes().Length);
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
        Assert.Equal(bundle.RulesetHash, read.RulesetHash);
        Assert.Equal(bundle.Seed, read.Seed);
        Assert.Equal(bundle.Map.MapHash, read.Map.MapHash);
        Assert.Equal(bundle.Ghost, read.Ghost);
        Assert.Equal(bundle.Wave, read.Wave);
    }

    [Fact]
    public void The_bundle_carries_everything_a_match_needs_and_consults_no_registry()
    {
        // Self-contained: the seed, the map inlined as the parsed grid, the
        // defense and the wave. The two things that have to be handed in are the
        // unit table and the ruleset, and the bundle carries the hash of each,
        // so neither can be substituted without the gate saying so.
        ReplayBundle bundle = ReplayBundle.FromBytes(TheMatch.Bundle().ToBytes());

        Assert.Equal(TheRuleset.Committed().ContentHash, bundle.RulesetHash);
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
        HexMap rebuilt =
            HexMap.FromCells("inlined", map.Width, map.Height, map.ToCellBytes(), map.ToLevelBytes());

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
        Assert.Equal(RecordFormat.ReplayVersion, read.Header.FormatVersion);
        Assert.Equal(SimulationVersion.Current, read.Header.SimVersion);
        Assert.Equal(TheMatch.Types().ContentHash, read.Header.ContentHash);
        Assert.Equal(TheRuleset.Committed().ContentHash, read.RulesetHash);
    }

    [Fact]
    public void The_committed_bundle_replays_to_the_committed_result()
    {
        // Through the replay gate, not around it: the file on disk is a record
        // this build is allowed to simulate, and simulating it is the match the
        // whole skeleton is built around.
        UnitTypeTable types = TheMatch.Types();
        MatchResult result = ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.BundleFile))
            .Replay(types, TheRuleset.Committed())
            .Resolve();

        Assert.Equal(TheMatch.LeakedInTheCommittedRun, result.Leaked);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
        Assert.Equal(TheMatch.Trace().At(TheMatch.FinalTickOfTheCommittedRun), result.RollingStateHash);
    }

    [Fact]
    public void A_grid_whose_shape_and_contents_disagree_refuses()
    {
        ContentException thrown = Assert.Throws<ContentException>(
            () => HexMap.FromCells("inlined", 5, 3, new byte[14], new byte[14]));

        Assert.Contains("no unambiguous reading", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_grid_with_a_level_for_every_hex_but_one_refuses()
    {
        // The second plane is checked against the first rather than against the
        // shape, because a plane that is short by one is a hex standing at a
        // height nothing in the record states.
        HexMap map = TheMatch.Map();

        ContentException thrown = Assert.Throws<ContentException>(() => HexMap.FromCells(
            "inlined",
            map.Width,
            map.Height,
            map.ToCellBytes(),
            new byte[(map.Width * map.Height) - 1]));

        Assert.Contains("levels", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_level_byte_outside_the_three_tiers_refuses()
    {
        // The terrain plane's own validation is untouched by the levels
        // arriving, which is the whole reason they arrive as a second plane
        // rather than as a widened cell encoding -- so this is a second, narrow
        // refusal and not a wider one.
        HexMap map = TheMatch.Map();
        byte[] levels = map.ToLevelBytes();
        levels[0] = (byte)HexMap.LevelCount;

        ContentException thrown = Assert.Throws<ContentException>(
            () => HexMap.FromCells("inlined", map.Width, map.Height, map.ToCellBytes(), levels));

        Assert.Contains("tiers", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bundle_written_before_the_level_plane_reads_back_on_the_flat()
    {
        // The version-0 golden is the one bundle nobody can make again, and it
        // was recorded on a board that had no second tier to stand on. So the
        // plane its branch supplies is not a defaulted field: it is the height
        // that record was actually played at, and the only one it could have
        // been.
        ReplayBundle old = ReplayBundle.FromBytes(
            File.ReadAllBytes(RepoLayout.GoldenBundleFile(0)));

        Assert.Equal(0, old.Header.FormatVersion);

        for (int row = 0; row < old.Map.Height; row++)
        {
            for (int column = 0; column < old.Map.Width; column++)
            {
                Assert.Equal(0, old.Map.LevelAt(column, row));
            }
        }

        // And its map stamp still checks out, exactly, under the layout it was
        // taken at -- which is the layout that covered the terrain alone. A
        // reader comparing it against today's fold would be reporting a layout
        // bump as a record that contradicts itself.
        Assert.Equal(old.Ghost.MapHash, old.Map.MapHashUnder(1));
        Assert.NotEqual(old.Ghost.MapHash, old.Map.MapHash);
    }

    [Fact]
    public void The_version_one_branch_reads_a_bundle_from_before_the_level_plane()
    {
        // NOTHING ELSE EXERCISES THIS BRANCH. content/golden/ is keyed on the
        // DEFENSE's format version and that one did not move, so re-recording
        // took the only version-1 bundle in the repository up to version 2 and
        // left its reader with no bytes at all. A branch nobody calls is a
        // branch that stops working quietly, which is the whole reason the
        // golden pool exists -- so the bytes are manufactured instead.
        ReplayBundle old = ReplayBundle.FromBytes(RecordBytes.AtVersionOne(TheMatch.Bundle()));

        Assert.Equal(1, old.Header.FormatVersion);
        Assert.Equal(TheRuleset.Committed().ContentHash, old.RulesetHash);
        Assert.Equal(TheMatch.Seed, old.Seed);
        Assert.Equal(6, old.Ghost.Count);
        Assert.Equal(6, old.Wave.Count);

        for (int row = 0; row < old.Map.Height; row++)
        {
            for (int column = 0; column < old.Map.Width; column++)
            {
                Assert.Equal(0, old.Map.LevelAt(column, row));
            }
        }

        // It reads, and it replays: this one names a ruleset, its stamps are
        // this build's, and its map hash checks out under the layout it was
        // taken at. Reading and replaying are separate gates and a retired
        // LAYOUT is not a retired record.
        Assert.Equal(old.Ghost.MapHash, old.Map.MapHashUnder(1));

        // FIFTEEN AND NOT TWELVE, and the difference is the whole point of the
        // plane this version predates. A version-1 bundle carries no levels, so
        // it replays the folded board flat -- and flat, the towers lose the
        // height their range was priced with and three more creeps get through.
        // While the map was flat this assertion could not tell the two apart.
        Assert.Equal(15, old.Replay(TheMatch.Types(), TheRuleset.Committed()).Resolve().Leaked);
    }

    [Fact]
    public void A_bundle_read_at_a_retired_version_cannot_be_written_back_out()
    {
        // OBSERVED, and it is the bug this assertion was written for. Delete
        // the version guard in ToBytes and these bytes come back with a header
        // stamped 1 over a body carrying the level plane -- which reads back
        // through the version-1 branch, which walks the levels as a defense.
        // The version-0 guard could not catch it: a version-1 bundle names a
        // ruleset, so the field it asks about is there.
        ReplayBundle old = ReplayBundle.FromBytes(RecordBytes.AtVersionOne(TheMatch.Bundle()));

        SimulationException thrown = Assert.Throws<SimulationException>(() => old.ToBytes());

        Assert.Contains("format version 1", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be rewritten", thrown.Message, StringComparison.Ordinal);
    }
}
