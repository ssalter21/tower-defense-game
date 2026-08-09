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
/// <b>Each bundle is run against the table committed beside it, not against
/// <c>content/units.txt</c>.</b> See <see cref="RepoLayout.GoldenUnitsFile"/>
/// for what that buys.
/// </para>
/// <para>
/// <b>And it is restaged rather than replayed, which is what lets an
/// irreplaceable record survive a simulation version bump.</b> A bump retires
/// every record made under the previous value -- that is what it is for -- and
/// these are records nobody can make again, so replaying them would mean every
/// bump quietly took a version out of this pool and left its reader branch
/// unproven from then on. Nothing a golden claims is weakened by asking the
/// question this way: what these files are evidence for is a reader, and
/// restaging parses them exactly as replaying does before running the result to
/// a pinned outcome. What it sets aside -- "were these the same rules?" -- is a
/// question about a competitive record, it is set aside by name rather than by
/// a gate not running, and it is asserted below on the one bundle that can
/// always answer it.
/// </para>
/// <para>
/// The end-to-end half of this lives in <c>tools/run-headless-match.ps1
/// -Verify</c>, which restages every golden through the actual command line
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

        foreach (RecordKind kind in new[]
        {
            RecordKind.Ghost, RecordKind.Wave, RecordKind.Replay, RecordKind.Command,
        })
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
            + ". A bundle is replayed against the table it was recorded against, and a bundle "
            + "without one cannot be replayed at all -- nor re-recorded, because the writer emits "
            + "only the current version.");

        Assert.Equal(PinnedTypes(version).ContentHash, Golden(version).Header.ContentHash);
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void The_golden_at_every_version_restages_to_the_committed_result(int version)
    {
        // Restaged, and the class remarks say why: these bundles cannot be made
        // again, so the operation that runs them has to be one that survives a
        // simulation version bump. The map handle is not a simulation input, so
        // a version-0 record whose handle was defaulted is still a record this
        // build can run.
        //
        // The oracle is the committed file rather than a second run made here:
        // a result the checker computes is a result that agrees with itself
        // whatever it does. The rolling hash it carries is a fold over the state
        // hash of every tick, so comparing that one number compares the whole
        // run -- what a per-tick walk buys over it is the tick number a
        // divergence started on, and content/golden-trace.txt is where that is
        // bought, for the live match it is the trace of.
        //
        // Both substrings are anchored at both ends, and that is not
        // fussiness: "1 of 40 leaked, tick 185" sits inside "11 of 40 leaked,
        // tick 1852", so a leak count or a tick that lost a digit would be found
        // in the committed line and pass.
        //
        // OBSERVED: doctor content/golden/defense-0.result. "12 of 40 leaked,
        // tick 1852" to "11 of 40 leaked, tick 1852" reddens the first Contains,
        // and "state CA3F66473C4B975D" to "state 0123456789ABCDEF" reddens the
        // second. Without watching those, a Contains against a file nobody
        // checks is a test that passes because the substring is short.
        MatchResult result = Golden(version)
            .RestageUnderCurrentRules(PinnedTypes(version), TheRuleset.Committed())
            .Match
            .Resolve();
        string committed = File.ReadAllText(RepoLayout.GoldenResultFile(version));

        Assert.Contains(
            "result     "
            + result.Leaked.ToString(CultureInfo.InvariantCulture)
            + " of "
            + result.Total.ToString(CultureInfo.InvariantCulture)
            + " leaked, tick "
            + result.FinalTick.ToString(CultureInfo.InvariantCulture)
            + " (",
            committed,
            StringComparison.Ordinal);

        Assert.Contains("state " + result.RollingStateHash.ToString(), committed, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(EveryDefenseVersion))]
    public void A_golden_whose_pinned_table_was_tampered_with_is_refused(int version)
    {
        // Pinning redirects a check; it softens none. A copy with a row taken
        // out of it is not the table the bundle was recorded against, and it is
        // refused by the same gate, by name. That the refusal also carries both
        // hashes is ReplayGateTests' claim rather than this one's.
        //
        // A row rather than a digit because the fold covers the row count and
        // every integer of every row, so dropping the last one moves the hash
        // whatever columns the table has.
        //
        // OBSERVED: drop the last comment line instead of the last unit row.
        // The hash is folded over the parsed integers, so it does not move, the
        // bundle replays, and Assert.Throws goes red having caught nothing at
        // all. That is what this assertion would look like if the pinned copy
        // were compared against nothing. Watched on the current version, which
        // is the row where the content hash is the gate that fires.
        string pinned = File.ReadAllText(RepoLayout.GoldenUnitsFile(version));
        UnitTypeTable tampered = UnitTypeTable.Parse("tampered pinned table", WithoutItsLastType(pinned));

        RetiredRecordException thrown =
            Assert.Throws<RetiredRecordException>(() => Golden(version).Replay(tampered, TheRuleset.Committed()));

        // WHICH gate depends on the version, and naming it rather than accepting
        // any refusal is the point. The three gates are ordered, so a record
        // made under retired rules is refused before its table is looked at all
        // -- a stronger refusal than the content hash, not a weaker one, and one
        // that would be indistinguishable from it if this only asserted that
        // something threw. The content hash is the gate for whichever version is
        // current, which is the row that can always be re-recorded and is
        // therefore where that claim belongs.
        Assert.Equal(
            Golden(version).Header.SimVersion == SimulationVersion.Current
                ? "content hash"
                : "simulation version",
            thrown.Gate);
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
        // The pair being compared is BUILT here rather than read from the two
        // goldens, and that is a correction rather than a convenience. The
        // goldens were used for this once, and it worked only for as long as
        // content/wave.txt had not moved since the version-0 record was made:
        // a bundle carries its own wave, so the moment that file is retuned the
        // two goldens stop being the same match and this assertion starts
        // reporting a wave change as a map handle that changed the match. The
        // clock dilation of 8 August 2026 moved exactly that file. Two bundles
        // recorded here from one live match, differing in the handle and in
        // nothing else, isolate the field for good.
        //
        // The goldens still carry the format claim -- that a version-0 record
        // has no handle and a current one does -- and that half stays below.
        UnitTypeTable types = PinnedTypes(RecordFormat.GhostVersion);

        ReplayBundle old = Golden(0);
        ReplayBundle current = Golden(RecordFormat.GhostVersion);

        Assert.Equal(GhostRecord.NoMapHandle, old.Ghost.MapHandle);
        Assert.Equal(TheMatch.MapHandle, current.Ghost.MapHandle);
        Assert.NotEqual(old.GhostId, current.GhostId);

        // OBSERVED: fold the map handle into Match's state hash -- add
        // ghost.MapHandle to the opening fold. The tick-for-tick assertion goes
        // red on tick zero, which is what a field the tick loop can see looks
        // like and is exactly the thing that would make defaulting it a lie.
        ReplayBundle handled = ReplayBundle.Of(
            TheMatch.Map(),
            TheMatch.Layout(types),
            TheMatch.Wave(types),
            types,
            TheMatch.Seed,
            TheMatch.MapHandle);

        ReplayBundle unhandled = ReplayBundle.Of(
            TheMatch.Map(),
            TheMatch.Layout(types),
            TheMatch.Wave(types),
            types,
            TheMatch.Seed,
            GhostRecord.NoMapHandle);

        Assert.NotEqual(handled.GhostId, unhandled.GhostId);

        Match one = unhandled.RestageUnderCurrentRules(types, TheRuleset.Committed()).Match;
        Match other = handled.RestageUnderCurrentRules(types, TheRuleset.Committed()).Match;

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
        Assert.Equal(RecordFormat.CommandVersion, TheCommands.Stream().Header.FormatVersion);

        // And the golden for the current version is exactly those bytes, so a
        // format change that forgot to re-record it is caught here rather than
        // discovered years later when the branch it documents is needed.
        Assert.Equal(
            TheMatch.Bundle().ToBytes(),
            File.ReadAllBytes(RepoLayout.GoldenBundleFile(RecordFormat.GhostVersion)));
    }

    [Fact]
    public void The_current_versions_pinned_table_is_content_units_txt_byte_for_byte()
    {
        // tools/run-headless-match.ps1 -Regenerate pins the current version's
        // table by copying content/units.txt beside the bundle, so the two are
        // the same bytes or the pin is a copy of some earlier table. The hash
        // assertion above cannot see this: it folds the parsed integers, so
        // comment text is free to drift between the two forever.
        //
        // OBSERVED: add a comment line to content/golden/defense-1.units. This
        // goes red on the byte arrays while every other assertion in this class
        // stays green -- which is exactly the state the pin was found in.
        //
        // Only the current version. The older ones are copies of tables this
        // repository can no longer produce, and a retune is meant to leave them
        // where they are.
        Assert.Equal(
            File.ReadAllBytes(RepoLayout.UnitsFile),
            File.ReadAllBytes(RepoLayout.GoldenUnitsFile(RecordFormat.GhostVersion)));
    }

    [Fact]
    public void The_current_versions_pinned_ladder_is_content_upgrades_txt_byte_for_byte()
    {
        // The sibling of the assertion above, and it exists because that one was
        // once FOUND out of sync: a second pinned file is a second thing that can
        // drift, and the hash assertion cannot see it -- the fold covers the
        // parsed edges, so comment text is free to drift between the two forever.
        //
        // Only the current version. Older versions were recorded before this file
        // existed and have no ladder pinned beside them at all, which is what
        // keeps the hashes frozen in their headers standing.
        Assert.Equal(
            File.ReadAllBytes(RepoLayout.UpgradesFile),
            File.ReadAllBytes(RepoLayout.GoldenUpgradesFile(RecordFormat.GhostVersion)));
    }

    [Fact]
    public void The_oldest_golden_has_no_ladder_pinned_beside_it_and_never_will()
    {
        // Stated as an assertion rather than left to the absence of a file.
        // content/golden/defense-0.replay cannot be recorded again, and its header
        // carries the hash of a table with no ladder folded into it; a ladder
        // appearing beside it -- empty or not -- would fold something and retire
        // the only version-0 defense record that will ever exist.
        Assert.False(File.Exists(RepoLayout.GoldenUpgradesFile(0)));
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
    /// The roster that bundle was recorded against, read from the copies
    /// committed beside it: the table, and the upgrade ladder folded into its
    /// content hash. Named after the files so that a row that will not parse says
    /// which of the pinned copies it was.
    /// </summary>
    /// <remarks>
    /// <b>The ladder is folded when the file is there and nothing is folded when
    /// it is not, and that second half is what keeps version 0 legal forever.</b>
    /// <c>content/golden/defense-0.replay</c> was recorded before
    /// <c>content/upgrades.txt</c> existed, so no ladder is pinned beside it, so
    /// the hash frozen in its header is the hash of the table alone -- and that
    /// bundle is the only evidence the version-0 reader branch will ever have.
    /// Folding an empty ladder here in place of folding none would be the same
    /// mistake as folding a live one.
    /// </remarks>
    private static UnitTypeTable PinnedTypes(int version)
    {
        string number = version.ToString(CultureInfo.InvariantCulture);

        UnitTypeTable types = UnitTypeTable.Parse(
            "golden/defense-" + number + ".units",
            File.ReadAllText(RepoLayout.GoldenUnitsFile(version)));

        string ladder = RepoLayout.GoldenUpgradesFile(version);

        if (!File.Exists(ladder))
        {
            return types;
        }

        return types.WithLadder(
            UpgradeLadder.Parse("golden/defense-" + number + ".upgrades", File.ReadAllText(ladder), types));
    }

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
