using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// One golden record per historical format version, kept forever, and the
/// assertion that every one of them still reads.
/// </summary>
/// <remarks>
/// <para>
/// <b>These bundles cannot be produced again.</b> The writer emits the current
/// format version and only that, so the version-0 bundle in
/// <c>content/golden/</c> is the last version-0 bundle that will ever exist:
/// it came out of a real <c>record</c> run before the map handle was added, was
/// read back and replayed before it was written, and was then kept. Every
/// version after it arrives the same way -- recorded while it is current, and
/// kept once it is not.
/// </para>
/// <para>
/// <b>What they are evidence for is the branch, not the bytes.</b> The read
/// gate's negative suite proves a record at an unknown version is refused; that
/// is the gate. This is the other side: a record at a <i>known</i> version is
/// still read, by the branch that claims to know it, and produces what it
/// always produced. Delete a reader branch and the golden for that version goes
/// red naming the version -- which is the only way a branch nobody calls any
/// more can be noticed at all.
/// </para>
/// <para>
/// The end-to-end half of this lives in <c>tools/run-headless-match.ps1
/// -Verify</c>, which replays every golden through the actual command line and
/// compares what it printed against the committed <c>.result</c> beside it. The
/// gate runs both.
/// </para>
/// </remarks>
public class GoldenRecordTests
{
    /// <summary>
    /// Every format version of every kind, from zero up to the one the writer
    /// emits. These are the versions that must have reader branches, and the
    /// only place in the tests that spells out how the list is derived.
    /// </summary>
    public static TheoryData<RecordKind, int> EveryHistoricalVersion()
    {
        var data = new TheoryData<RecordKind, int>();

        foreach (RecordKind kind in new[] { RecordKind.Ghost, RecordKind.Wave, RecordKind.Replay })
        {
            for (int version = 0; version <= RecordFormat.CurrentVersionOf(kind); version++)
            {
                data.Add(kind, version);
            }
        }

        return data;
    }

    /// <summary>Every defense format version that has ever shipped.</summary>
    public static TheoryData<int> EveryDefenseVersion()
    {
        var data = new TheoryData<int>();

        for (int version = 0; version <= RecordFormat.GhostVersion; version++)
        {
            data.Add(version);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(EveryHistoricalVersion))]
    public void Every_version_up_to_the_current_one_has_a_reader_branch(RecordKind kind, int version)
    {
        // The version list and the branch list are two things, and this is what
        // holds them together. Deleting a branch -- either the row in IsKnown or
        // the case in the record's own switch -- fails here, and the message
        // names the version that lost it rather than saying a test broke.
        Assert.True(
            RecordFormat.IsKnown(kind, version),
            RecordFormat.NameOf(kind)
            + " format version "
            + version.ToString(CultureInfo.InvariantCulture)
            + " has no reader branch, and versions 0 to "
            + RecordFormat.CurrentVersionOf(kind).ToString(CultureInfo.InvariantCulture)
            + " all shipped. An older version reads fine forever through its own branch, so a missing "
            + "one is a branch that was skipped or deleted. Stored records at that version are now "
            + "unreadable by this build.");
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void A_golden_bundle_is_committed_for_every_defense_format_version(int version)
    {
        string path = RepoLayout.GoldenBundleFile(version);

        Assert.True(
            File.Exists(path),
            "There is no golden bundle for defense format version "
            + version.ToString(CultureInfo.InvariantCulture)
            + " at "
            + path
            + ". One is committed per historical version, forever: the writer emits only the current "
            + "version, so an older bundle cannot be made again and a deleted one leaves that reader "
            + "branch with nothing testing it.");

        ReplayBundle bundle = ReplayBundle.FromBytes(File.ReadAllBytes(path));

        Assert.Equal(version, bundle.Ghost.Header.FormatVersion);
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void The_golden_at_every_version_replays_to_the_committed_result(int version)
    {
        // Through the replay gate, not around it. The map handle is not a
        // simulation input, so a version-0 record whose handle was defaulted is
        // still a record this build may simulate -- and it produces, tick for
        // tick, the trace a real run committed.
        //
        // The oracle is the committed file rather than a second run made here:
        // a result the checker computes is a result that agrees with itself
        // whatever it does.
        UnitTypeTable types = TheMatch.Types();
        ReplayBundle bundle = ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.GoldenBundleFile(version)));
        GoldenTrace trace = TheMatch.Trace();

        Match match = bundle.Replay(types);
        trace.Check(0, match.StateHash);

        while (!match.IsFinished)
        {
            match.Advance(1);
            trace.Check(match.Tick, match.StateHash);
        }

        MatchResult result = match.Result();

        Assert.Equal(TheMatch.LeakedInTheCommittedRun, result.Leaked);
        Assert.Equal(TheMatch.FinalTickOfTheCommittedRun, result.FinalTick);
        Assert.Equal(trace.At(trace.FinalTick), result.RollingStateHash);
    }

    [Fact]
    public void The_defaulted_map_handle_changes_nothing_about_the_match()
    {
        // The claim the whole choice of field rests on, stated as an assertion
        // rather than as a paragraph. Two bundles, two format versions, one of
        // them missing a field the other carries -- and the same result, tick
        // for tick, because nothing in the tick loop can see a map handle.
        //
        // A field the tick loop COULD see would fail this, and that is the test
        // for whether defaulting is legitimate: not "is the field small", not
        // "is a sensible value obvious", but "can a replay's result depend on
        // it". See RecordFormat.GhostVersion.
        UnitTypeTable types = TheMatch.Types();

        ReplayBundle old = ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.GoldenBundleFile(0)));
        ReplayBundle current = ReplayBundle.FromBytes(
            File.ReadAllBytes(RepoLayout.GoldenBundleFile(RecordFormat.GhostVersion)));

        Assert.Equal(GhostRecord.NoMapHandle, old.Ghost.MapHandle);
        Assert.Equal(TheMatch.MapHandle, current.Ghost.MapHandle);
        Assert.NotEqual(old.GhostId, current.GhostId);

        Match one = old.Replay(types);
        Match other = current.Replay(types);

        while (!one.IsFinished || !other.IsFinished)
        {
            Assert.Equal(one.StateHash, other.StateHash);
            one.Advance(1);
            other.Advance(1);
        }

        Assert.Equal(one.Result().RollingStateHash, other.Result().RollingStateHash);
        Assert.Equal(one.Result().Leaked, other.Result().Leaked);
        Assert.Equal(one.Result().FinalTick, other.Result().FinalTick);
    }

    [Fact]
    public void The_writer_emits_the_current_version_and_no_other()
    {
        // History lives in the reader. A writer that could be asked for an older
        // format would double the pairs anybody has to reason about, and the
        // golden files above would stop being irreplaceable -- which is the
        // property that makes keeping them mean something.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(RecordFormat.GhostVersion, TheMatch.Ghost(types).Header.FormatVersion);
        Assert.Equal(RecordFormat.WaveVersion, TheMatch.WaveOf(types).Header.FormatVersion);
        Assert.Equal(RecordFormat.ReplayVersion, TheMatch.Bundle().Header.FormatVersion);

        // And the golden for the current version is exactly those bytes, so a
        // format change that forgot to re-record it is caught here rather than
        // discovered years later when the branch it documents is needed.
        Assert.Equal(
            TheMatch.Bundle().ToBytes(),
            File.ReadAllBytes(RepoLayout.GoldenBundleFile(RecordFormat.GhostVersion)));
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void The_committed_result_says_which_branch_read_it(int version)
    {
        // The .result files are compared byte for byte by the runner, which
        // proves they are current. This proves they are about the right thing:
        // a result regenerated against the wrong bundle would still match a run
        // of that wrong bundle forever.
        string text = File.ReadAllText(RepoLayout.GoldenResultFile(version));

        Assert.Contains(
            "read at defense record format " + version.ToString(CultureInfo.InvariantCulture),
            text,
            StringComparison.Ordinal);
    }
}
