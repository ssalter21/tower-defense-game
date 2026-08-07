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
/// <b>Each bundle is verified against the unit table committed beside it, not
/// against <c>content/units.txt</c>.</b> A bundle stamps the content hash of
/// the table it was recorded against and the replay gate refuses anything else,
/// so a golden checked against the live table is a golden that any retune
/// deletes -- and the writer emits only the current version, so an older one
/// deleted is gone. The <c>.units</c> copy beside each bundle is that bundle's
/// own ruleset, frozen; the gate is untouched and only what it is pointed at
/// has moved.
/// </para>
/// <para>
/// The end-to-end half of this lives in <c>tools/run-headless-match.ps1
/// -Verify</c>, which replays every golden through the actual command line
/// against that golden's pinned table and compares what it printed against the
/// committed <c>.result</c> beside it. The gate runs both.
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
    public void The_table_a_golden_was_recorded_against_is_committed_beside_it(int version)
    {
        // OBSERVED, both assertions, on this build.
        //
        // The file: rename content/golden/defense-0.units and the version-0 row
        // goes red naming the path it looked for. Nothing else in this class
        // notices, which is why the existence of the copy is asserted here
        // rather than left to whatever happens to open it first.
        //
        // The hash: move grunt max hp from 200 to 201 in
        // content/golden/defense-0.units and the second assertion goes red,
        // 6546B150CB4FEC4A against the 39B848CEFDDCC9CF in the bundle's header.
        // That is the whole content of "the table it was recorded against": a
        // copy of some other table would sit here looking exactly as
        // convincing.
        string path = RepoLayout.GoldenUnitsFile(version);

        Assert.True(
            File.Exists(path),
            "The golden bundle for defense format version "
            + version.ToString(CultureInfo.InvariantCulture)
            + " has no unit table beside it at "
            + path
            + ". Each bundle is replayed against the table it was recorded against, so a bundle "
            + "without one cannot be verified at all -- and it cannot be re-recorded either, because "
            + "the writer emits only the current version.");

        Assert.Equal(PinnedTypes(version).ContentHash, Golden(version).Header.ContentHash);
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void The_golden_at_every_version_replays_to_the_committed_result(int version)
    {
        // Through the replay gate, not around it. The map handle is not a
        // simulation input, so a version-0 record whose handle was defaulted is
        // still a record this build may simulate.
        //
        // The oracle is the committed file rather than a second run made here:
        // a result the checker computes is a result that agrees with itself
        // whatever it does.
        //
        // OBSERVED, all three assertions.
        //
        // Hand the refusal the pinned table instead of the retuned one and
        // Assert.Throws goes red having caught nothing -- which is what the line
        // would be worth if the gate had been softened to let a mismatched table
        // through.
        //
        // Doctor content/golden/defense-0.result for the other two: "12 of 40
        // leaked, tick 1852" to "11 of 40 leaked, tick 1852" reddens the first
        // Contains, and "state CA3F66473C4B975D" to "state 0123456789ABCDEF"
        // reddens the second. Without watching those, a Contains against a file
        // nobody checks is a test that passes because the substring is short.
        ReplayBundle bundle = Golden(version);

        // The live table, retuned. This is what a balance patch does to these
        // bytes, and what verifying against content/units.txt would make of
        // every one of these files.
        Assert.Equal(
            "content hash",
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(TheMatch.RetunedTypes())).Gate);

        MatchResult result = bundle.Replay(PinnedTypes(version)).Resolve();
        string committed = File.ReadAllText(RepoLayout.GoldenResultFile(version));

        Assert.Contains(
            result.Leaked.ToString(CultureInfo.InvariantCulture)
            + " of "
            + result.Total.ToString(CultureInfo.InvariantCulture)
            + " leaked, tick "
            + result.FinalTick.ToString(CultureInfo.InvariantCulture),
            committed,
            StringComparison.Ordinal);

        Assert.Contains("state " + result.RollingStateHash.ToString(), committed, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void A_golden_whose_pinned_table_was_tampered_with_is_refused(int version)
    {
        // Pinning redirects a check; it softens none. A copy with a row taken
        // out of it is not the table the bundle was recorded against, and the
        // gate says so by name and with both hashes -- the same refusal any
        // mismatched table earns, now coming from the file that decides.
        //
        // A row rather than a digit because the fold covers the row count and
        // every integer of every row, so dropping the last one moves the hash
        // whatever columns the table has.
        //
        // OBSERVED: drop the last comment line instead of the last unit row.
        // The hash is folded over the parsed integers, so it does not move, the
        // bundle replays, and Assert.Throws goes red having caught nothing at
        // all. That is what this assertion would look like if the pinned copy
        // were compared against nothing.
        string pinned = File.ReadAllText(RepoLayout.GoldenUnitsFile(version));
        UnitTypeTable tampered = UnitTypeTable.Parse("tampered pinned table", WithoutItsLastType(pinned));
        ReplayBundle bundle = Golden(version);

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => bundle.Replay(tampered));

        Assert.Equal("content hash", thrown.Gate);
        Assert.Contains(bundle.Header.ContentHash.ToString(), thrown.Recorded, StringComparison.Ordinal);
        Assert.Contains(tampered.ContentHash.ToString(), thrown.Live, StringComparison.Ordinal);
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
        //
        // Both records are run against ONE table, and that makes it a restaging
        // rather than a replay. Isolating one field means holding everything
        // else still, and the two bundles carry two pinned tables that are free
        // to differ; each run under its own would compare two rulesets as well,
        // and would report a retune as a map handle that changed the match. The
        // gate the restaging does keep is the map hash, which asks whether the
        // bytes agree with themselves rather than which ruleset made them.
        UnitTypeTable types = PinnedTypes(RecordFormat.GhostVersion);

        ReplayBundle old = Golden(0);
        ReplayBundle current = Golden(RecordFormat.GhostVersion);

        Assert.Equal(GhostRecord.NoMapHandle, old.Ghost.MapHandle);
        Assert.Equal(TheMatch.MapHandle, current.Ghost.MapHandle);
        Assert.NotEqual(old.GhostId, current.GhostId);

        Match one = old.RestageUnderCurrentRules(types).Match;
        Match other = current.RestageUnderCurrentRules(types).Match;

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

    /// <summary>The committed bundle whose defense is at this format version.</summary>
    private static ReplayBundle Golden(int version) =>
        ReplayBundle.FromBytes(File.ReadAllBytes(RepoLayout.GoldenBundleFile(version)));

    /// <summary>
    /// The ruleset that bundle was recorded against, read from the copy
    /// committed beside it. Named after the file so that a row that will not
    /// parse says which of the pinned tables it was.
    /// </summary>
    private static UnitTypeTable PinnedTypes(int version) =>
        UnitTypeTable.Parse(
            "golden/defense-" + version.ToString(CultureInfo.InvariantCulture) + ".units",
            File.ReadAllText(RepoLayout.GoldenUnitsFile(version)));

    /// <summary>
    /// The same table with its last unit row removed, whatever columns the rows
    /// have and however they are spaced. Ids ascend down the file, so the row
    /// that goes is the highest id -- one the committed defense builds towers
    /// from. That is harmless, and it is worth knowing why: the replay gate
    /// compares hashes before a single type is looked up, so what refuses the
    /// record is the gate rather than the missing row behind it.
    /// </summary>
    private static string WithoutItsLastType(string text)
    {
        string[] lines = text.Split('\n');

        for (int index = lines.Length - 1; index >= 0; index--)
        {
            string line = lines[index].Trim();

            if (line.Length > 0 && !line.StartsWith('#'))
            {
                return string.Join("\n", lines.Where((_, at) => at != index));
            }
        }

        throw new Xunit.Sdk.XunitException("The pinned table has no unit rows in it at all.");
    }
}
