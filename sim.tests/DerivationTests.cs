using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The trigger, as opposed to the gate: whether each of the three identity
/// fields is computed over the thing it claims to be about.
/// </summary>
/// <remarks>
/// <para>
/// <b>The record format's negative suite tests the gate.</b> It takes a good
/// record, damages one byte, and watches the specific refusal fire. Every one of
/// those tests passes just as well when a hash is folded over the wrong input,
/// because a hash compared against itself always agrees with itself. A content
/// hash computed over file bytes, or a map hash computed over the width alone,
/// would sail through the whole suite and then never move when it mattered.
/// </para>
/// <para>
/// <b>These are the other half.</b> Each one names an edit that must move a
/// hash, and -- where there is one -- an edit that must not. A derivation test
/// without the second half is barely a test: any hash at all moves when
/// everything moves. What separates a hash over the parsed numbers from a hash
/// over the file is precisely the edit that changes the file and not the
/// numbers.
/// </para>
/// <para>
/// <b>Each was watched failing under a deliberately wrong input</b>, which is
/// the only way to know an assertion is load bearing. The wrong input for each
/// is written above it, so the observation can be repeated.
/// </para>
/// </remarks>
public class DerivationTests
{
    /// <summary>
    /// The behaviour this build implements, folded into one number, per
    /// simulation version that has declared one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b><see cref="SimulationVersion.Current"/> is a hand-edited constant and
    /// nothing derives it.</b> It cannot notice a rule change on its own and it
    /// is not going to: a version number that recomputed itself from the rules
    /// would move without anybody deciding to retire anything, which is the
    /// opposite of what it is for. So the mechanism cannot be "the simulation
    /// version notices"; it has to be "something notices and names the
    /// simulation version", and this table is that something.
    /// </para>
    /// <para>
    /// <b>The fingerprint is over a scenario written here, not over the
    /// committed content.</b> That is what makes it a simulation-version trigger
    /// rather than a second content hash: retuning a number in
    /// <c>content/units.txt</c> moves the golden trace and must not move this,
    /// because the content hash already covers a retune and bumping the
    /// simulation version as well would retire every record made under rules
    /// that did not change. Change a rule -- the rounding, the tick order, the
    /// tiebreak, the release cadence -- and this moves while the content hash
    /// does not.
    /// </para>
    /// <para>
    /// A bump is therefore two edits, and this table is the second one. Adding a
    /// row is the moment somebody has decided that every stored record made
    /// under the old rules is retired; the value in it is read off a run,
    /// because a number anybody could have written down by hand is a number
    /// nobody derived.
    /// </para>
    /// </remarks>
    private static readonly (uint Version, ulong Fingerprint)[] BehaviourByVersion =
    {
        (1u, 0xAB1569545287E5B0UL),

        // Version 2 is version 1 with the within-column release cadence dilated
        // from fifteen ticks to forty-five, finishing the 8 August 2026 clock
        // change that content could not reach. Nothing in any content file moved
        // to earn this row, which is the case this table exists to tell apart
        // from a retune.
        (2u, 0x42346EF613910009UL),
    };

    /// <summary>
    /// The scenario the fingerprint is taken over: one corridor, one tower, one
    /// order of three walkers. Written out here on purpose -- a fingerprint over
    /// the committed files would move every time somebody retuned a number, and
    /// would then be a content hash wearing a simulation version's name.
    /// </summary>
    private const string FingerprintMap = """
        S####E
        ......
        """;

    private const string FingerprintUnits = """
        unit  1  walker  moving  100  27  0     0  0  0  0  0  none     0  4
        unit  3  turret  placed  0    0   2000  5  2  1  4  9  hitscan  0  0
        """;

    private const string FingerprintDefense = "tower  3  2  1";

    private const string FingerprintWave = "order  0  1  3  0";

    private const ulong FingerprintSeed = 20260802UL;

    /// <summary>How many ticks of it are folded in. Enough for three spawns, every shot and every death.</summary>
    private const int FingerprintTicks = 400;

    [Fact]
    public void Editing_a_type_table_moves_the_content_hash_and_editing_a_comment_does_not()
    {
        // OBSERVED: point the fold at the file instead of at the parsed
        // integers -- in UnitTypeTable.Parse, absorb the characters of the text
        // in place of the loop over the rows. The retune assertion still passes,
        // because a changed number is also changed bytes. The comment assertion
        // goes red, 1BEF1F9F2EEF6616 against 0CDA1F685DA57EC3, and so does every
        // formatting assertion after it. That asymmetry is the whole reason the
        // hash is over what was parsed: a hash over the file would say "somebody
        // touched units.txt", which is a signal nobody can act on.
        string original = File.ReadAllText(RepoLayout.UnitsFile);
        Hash64 hash = UnitTypeTable.Parse(original).ContentHash;

        // A number moved. One digit, and every record pinned to the old
        // ruleset is retired -- which is exactly what should happen. The edit
        // is made by rewriting a parsed field rather than by replacing a
        // literal run of characters; see TheMatch.RetunedUnitsText.
        Assert.NotEqual(hash, UnitTypeTable.Parse(TheMatch.RetunedUnitsText()).ContentHash);

        // Nothing that is not a number moved. Every one of these changes the
        // file, and a hash over the file would retire every stored record for
        // each of them -- at which point the signal means "somebody touched
        // units.txt", which is a signal nobody can act on and everybody learns
        // to override.
        Assert.Equal(hash, UnitTypeTable.Parse(WithCommentsRewritten(original)).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(WithColumnsRespaced(original)).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal)).ContentHash);
        Assert.Equal(hash, UnitTypeTable.Parse(original + "\n\n\n").ContentHash);

        // And a label, which is for people. The simulation branches on nothing
        // in it, so renaming one is not a ruleset change either.
        Assert.Equal(
            hash,
            UnitTypeTable.Parse(original.Replace("grunt ", "goblin", StringComparison.Ordinal)).ContentHash);
    }

    [Fact]
    public void Editing_the_ruleset_moves_its_content_hash_and_reformatting_it_does_not()
    {
        // The same pair as the unit table's, for the file that holds every
        // number the rules are made of. Both halves matter and the second one
        // is the one that separates a hash over the parsed integers from a hash
        // over the file.
        //
        // OBSERVED: fold the characters of the text in Ruleset.Parse instead of
        // the parsed fields. Every retune assertion still passes, because a
        // changed number is also changed bytes. Every formatting assertion goes
        // red -- the first of them 0292B3908133DF72 against F849109AC5C46BD7 --
        // at which point the hash means "somebody touched ruleset.txt", which
        // is a signal nobody can act on.
        string original = TheRuleset.CommittedText();
        Hash64 hash = Ruleset.Parse(original).ContentHash;

        // A number moved, once per rule. Each of these retires every record
        // pinned to the old ruleset, which is exactly what should happen.
        // The matrix, twice: once widened and once permuted. A single cell
        // cannot move on its own without the square stopping being a Latin
        // square, so the retune that tests the fold is a whole value class
        // moving, and the permutation is what proves a cell's position is
        // folded rather than the multiset of nine numbers.
        Assert.NotEqual(hash, Ruleset.Parse(WithMatrix(original, "150    70       100", "70   100       150", "100   150        70")).ContentHash);
        Assert.NotEqual(hash, Ruleset.Parse(WithMatrix(original, " 70   100       140", "100   140        70", "140    70       100")).ContentHash);
        Assert.NotEqual(hash, Retuned(original, "armour          1          100", "armour          2          100"));
        Assert.NotEqual(hash, Retuned(original, "floor           1", "floor           2"));
        Assert.NotEqual(hash, Retuned(original, "interest       10         0", "interest       11         0"));

        // The interest cap, which is parsed and could be parsed and dropped.
        // OBSERVED: delete .Add(draft.InterestCapGold) from the fold. This line
        // goes red with the capped and uncapped rulesets both hashing
        // 1E384929C5F43BFB, and every record pinned to one would replay happily
        // against the other.
        Assert.NotEqual(hash, Retuned(original, "interest       10         0", "interest       10       500"));
        Assert.NotEqual(hash, Retuned(original, "income        100", "income        101"));

        // What a run opens holding, which decides whether the first build phase
        // can buy anything at all.
        // OBSERVED: delete .Add(draft.StartingPurseGold) from the fold. This
        // line goes red with a run that opens on 100 gold and one that opens
        // on 101 both hashing 6EBEF9AA88D5E2AA, so a stored run could be
        // replayed against an opening balance it never had.
        Assert.NotEqual(hash, Retuned(original, "purse         100", "purse         101"));
        Assert.NotEqual(hash, Retuned(original, "band           90       20", "band           90       21"));
        Assert.NotEqual(hash, Retuned(original, "health       1500", "health       1501"));
        Assert.NotEqual(hash, Retuned(original, "slots           2         1", "slots           3         1"));
        Assert.NotEqual(hash, Retuned(original, "offering        3         3", "offering        4         3"));
        Assert.NotEqual(hash, Retuned(original, "snapshot       10        25", "snapshot       10        26"));

        // Nothing that is not a number moved. Each of these changes the file
        // and none of them changes a rule.
        Assert.Equal(hash, Ruleset.Parse(WithCommentsRewritten(original)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(WithColumnsRespaced(original)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(original + "\n\n\n").ContentHash);
    }

    [Fact]
    public void Editing_the_schedule_moves_its_content_hash_and_reformatting_it_does_not()
    {
        // The same pair again, for the file that holds the shape. The second
        // half is what separates a hash over the parsed integers from a hash
        // over the file, and it matters more here than anywhere: the shape is
        // the thing a rotation publishes, so "somebody touched schedule.txt" is
        // a signal every player would learn to ignore.
        //
        // OBSERVED: fold the characters of the text in AnchorSchedule.Parse
        // instead of the parsed fields. Every retune assertion still passes,
        // because a changed number is also changed bytes. Every formatting
        // assertion goes red -- the first of them E29E570DEDD45072 against
        // 6546745EC46DCEE5 -- at which point renaming a game changer retires
        // every run recorded against the shape.
        UnitTypeTable types = TheMatch.Types();
        string original = TheSchedule.CommittedText();
        Hash64 hash = AnchorSchedule.Parse(original, types).ContentHash;

        // A number moved, once per column the shape is made of. Each retires
        // every run pinned to the old shape, which is exactly right.
        Assert.NotEqual(hash, Reshaped(original, "anchor        3     1", "anchor        2     1"));
        Assert.NotEqual(hash, Reshaped(original, "plain        3     1\nanchor        6", "plain        4     1\nanchor        6"));
        Assert.NotEqual(hash, Reshaped(original, "steep        4     8", "steep        4     7"));
        Assert.NotEqual(hash, Reshaped(original, "changer   12  thermal-riser", "changer   13  thermal-riser"));
        Assert.NotEqual(hash, Reshaped(original, "swift-column     1     2", "swift-column     1     1"));

        // The bonus, which is parsed and could be parsed and dropped.
        // OBSERVED: delete .Add(BonusVsTag) from GameChanger.Fold. This line
        // goes red with the 825 and the 830 shapes both hashing
        // 9738D9F811E8A4B1, and a run pinned to one would replay against the
        // other with the steep counter retuned underneath it.
        Assert.NotEqual(hash, Reshaped(original, "thermal-riser    3     1    825", "thermal-riser    3     1    830"));

        // Nothing that is not a number moved. Each of these changes the file
        // and none of them changes the shape.
        Assert.Equal(hash, AnchorSchedule.Parse(WithCommentsRewritten(original), types).ContentHash);
        Assert.Equal(hash, AnchorSchedule.Parse(WithColumnsRespaced(original), types).ContentHash);
        Assert.Equal(
            hash,
            AnchorSchedule.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal), types).ContentHash);
        Assert.Equal(hash, AnchorSchedule.Parse(original + "\n\n\n", types).ContentHash);

        // And a label, which is for people. The simulation branches on nothing
        // in it, so renaming a game changer is not a shape change either.
        Assert.Equal(
            hash,
            AnchorSchedule.Parse(
                original.Replace("thermal-riser", "updraft", StringComparison.Ordinal),
                types).ContentHash);
    }

    [Fact]
    public void The_rules_this_build_implements_are_the_ones_its_simulation_version_names()
    {
        // OBSERVED, both ways round, on this build.
        //
        // RIGHT INPUT: change Fix64's restoring division from truncation toward
        // zero to rounding the magnitude to nearest -- four lines, no number in
        // any content file moved, no byte of any record layout moved -- and this
        // goes red, AB1569545287E5B0 to 90B255DEE5C77BB4. The change reaches the
        // match through the lateral offsets, which are three tenths of a hex and
        // are folded into the state hash the moment a creep spawns.
        //
        // WRONG INPUT: replace the body of RuleFingerprint with
        // Hash64.Start(label).Add(SimulationVersion.Current) -- the declared
        // version, compared against itself -- and record the value it produces.
        // The identical rounding change is then green, and so is reverting it:
        // the same number comes out of a build that rounds and a build that
        // truncates, because nothing in the fold ever ran the rules. That is the
        // failure this whole file exists to catch, and it is invisible from
        // inside the assertion below.
        (uint Version, ulong Fingerprint) declared = Row(SimulationVersion.Current);

        Assert.Equal(
            Hash64.FromValue(declared.Fingerprint),
            RuleFingerprint());
    }

    [Fact]
    public void Retuning_a_number_is_not_a_rule_change_and_does_not_touch_the_fingerprint()
    {
        // The converse, and the reason the fingerprint is taken over a scenario
        // written in this file rather than over the committed content. The
        // mistake runs both ways: bumping the simulation version for a retune
        // retires every record made under rules that did not change, and the
        // content hash already covers a retune automatically.
        UnitTypeTable types = TheMatch.Types();
        UnitTypeTable retuned = TheMatch.RetunedTypes();

        Assert.NotEqual(types.ContentHash, retuned.ContentHash);
        Assert.Equal(Hash64.FromValue(Row(SimulationVersion.Current).Fingerprint), RuleFingerprint());
    }

    [Fact]
    public void The_rounding_rule_itself_is_pinned_to_a_value_a_different_rule_would_change()
    {
        // Rounding is truncation toward zero, for multiplication and division
        // alike, and these are the numbers that say so rather than a comment
        // saying so. Three tenths in Q32.32 is 1288490188.8 exactly: truncation
        // keeps 1288490188 and round-to-nearest would keep 1288490189, so a
        // change of rule with no number moved is visible right here.
        //
        // The negative sign is the other half. Toward zero and toward negative
        // infinity agree on every positive value and disagree on every negative
        // one that is not exact, so a rule quietly changed to flooring passes
        // the line above and fails the line below.
        Assert.Equal(1288490188L, Fix64.FromRatio(3, 10).Raw);
        Assert.Equal(-1288490188L, Fix64.FromRatio(-3, 10).Raw);

        // The same claim for multiplication, whose truncation is a shift on the
        // magnitude with the sign reattached afterwards. Half of the smallest
        // representable value is exactly half a raw unit, so all three candidate
        // rules disagree about it: truncation toward zero keeps nothing, flooring
        // would take the negative one down to -1, and rounding half away from
        // zero would take both out to a whole unit.
        Assert.Equal(0L, (Fix64.FromRatio(1, 2) * Fix64.Epsilon).Raw);
        Assert.Equal(0L, (Fix64.FromRatio(-1, 2) * Fix64.Epsilon).Raw);
    }

    [Fact]
    public void Editing_one_hex_of_the_map_moves_the_map_hash_and_rewrapping_the_legend_does_not()
    {
        // OBSERVED: fold only the width and the height in HexMap.FromGrid --
        // delete the loop over the cells. The reformatting assertions below
        // still pass, and the first one goes red with the two five-by-three
        // grids both hashing 370199AFA18A6E97. That is the shape of a map hash
        // that pins nothing: a stored defense would replay happily on geometry
        // somebody had nudged, and every gate in the negative suite would still
        // be green.
        //
        // One hex longer rather than one character different, because a
        // single-character edit of a valid map is never another valid map: the
        // corridor assertion refuses an isolated cell, a branch, a second
        // entrance and a dead end, which between them cover every one-character
        // change there is. The corridor gaining a hex is the smallest edit to
        // the playfield that exists.
        HexMap shorter = HexMap.Parse("""
            .....
            .S#E.
            .....
            """);

        HexMap longer = HexMap.Parse("""
            .....
            .S##E
            .....
            """);

        Assert.NotEqual(shorter.MapHash, longer.MapHash);

        // The same grid somewhere else on the same board. Nothing about the
        // corridor changed except which hexes it is made of.
        Assert.NotEqual(
            shorter.MapHash,
            HexMap.Parse("""
                .....
                .....
                .S#E.
                """).MapHash);

        // And the file around it, rewritten. The map's comment marker, its line
        // endings and its trailing blank lines are not the playfield, and a hash
        // over the file would retire every stored defense for each of them.
        string original = File.ReadAllText(RepoLayout.MapFile);
        Hash64 hash = HexMap.Parse(original).MapHash;

        Assert.Equal(hash, HexMap.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal)).MapHash);
        Assert.Equal(hash, HexMap.Parse(original + "\n\n").MapHash);
        Assert.Equal(hash, HexMap.Parse("// a completely rewritten legend\n\n" + WithoutMapComments(original)).MapHash);
    }

    [Fact]
    public void No_single_hex_of_the_committed_map_can_be_changed_without_something_noticing()
    {
        // The exhaustive form. Every cell of the committed grid, changed to each
        // of the other three kinds, and every one of them either refused by the
        // corridor assertion or landing on a different map hash. Nothing is
        // allowed to load quietly with the hash it had.
        byte[] cells = HexMap.Parse(File.ReadAllText(RepoLayout.MapFile)).ToCellBytes();
        HexMap original = HexMap.FromCells("map", 15, 9, cells);
        int refused = 0;
        int rehashed = 0;

        for (int index = 0; index < cells.Length; index++)
        {
            for (byte kind = 0; kind <= (byte)MapCell.Exit; kind++)
            {
                if (cells[index] == kind)
                {
                    continue;
                }

                byte[] edited = (byte[])cells.Clone();
                edited[index] = kind;

                try
                {
                    Assert.NotEqual(original.MapHash, HexMap.FromCells("edited", 15, 9, edited).MapHash);
                    rehashed++;
                }
                catch (ContentException)
                {
                    refused++;
                }
            }
        }

        Assert.Equal(cells.Length * 3, refused + rehashed);
        Assert.True(refused > 0, "The corridor assertion caught none of them, which cannot be right.");
    }

    /// <summary>
    /// One number for what this build's rules do: a fold over the state hash of
    /// every tick of a fixed scenario.
    /// </summary>
    /// <remarks>
    /// The scenario is deliberately small and deliberately local. Every rule
    /// worth calling one reaches it -- the tick order, the release cadence, the
    /// targeting tiebreak, the dice, and the rounding under both the movement
    /// step and the lateral offsets -- and nothing in <c>content/</c> does.
    /// </remarks>
    private static Hash64 RuleFingerprint()
    {
        UnitTypeTable types = UnitTypeTable.Parse("fingerprint units", FingerprintUnits);
        HexMap map = HexMap.Parse("fingerprint map", FingerprintMap);
        TowerLayout layout = TowerLayout.Parse("fingerprint defense", FingerprintDefense, types);
        WaveScript wave = WaveScript.Parse("fingerprint wave", FingerprintWave, types);

        var match = new Match(map, TheRuleset.Committed(), layout, wave, FingerprintSeed);
        Hash64 fingerprint = Hash64.Start("rule-fingerprint/1").Add(unchecked((long)match.StateHash.Value));

        for (int tick = 0; tick < FingerprintTicks && !match.IsFinished; tick++)
        {
            match.Advance(1);
            fingerprint = fingerprint.Add(unchecked((long)match.StateHash.Value));
        }

        MatchResult result = match.Result();

        return fingerprint
            .Add(result.Leaked, result.Total)
            .Add(result.FinalTick)
            .Add(unchecked((long)result.RollingStateHash.Value));
    }

    private static (uint Version, ulong Fingerprint) Row(uint version)
    {
        foreach ((uint Version, ulong Fingerprint) row in BehaviourByVersion)
        {
            if (row.Version == version)
            {
                return row;
            }
        }

        // The value is in the refusal because the sentence after it tells
        // somebody to take it, and a refusal that throws before computing the
        // thing it names sends them off to write a scratch test that computes it
        // again. It is not a suggestion of what to write down: it is what this
        // build's rules actually do, taken the only way the row may be taken.
        throw new Xunit.Sdk.XunitException(
            "Simulation version "
            + version.ToString(CultureInfo.InvariantCulture)
            + " has no behaviour fingerprint recorded for it. A bump is two edits: the constant, and a "
            + "row here carrying what this build's rules actually do. This build's is "
            + RuleFingerprint().ToString()
            + ", so the row is ("
            + version.ToString(CultureInfo.InvariantCulture)
            + "u, 0x"
            + RuleFingerprint().ToString()
            + "UL) -- and adding it is the moment every record made under the old rules is retired.");
    }

    /// <summary>
    /// The committed ruleset with its three matrix rows replaced, keeping the
    /// attack-type order the file authors them in.
    /// </summary>
    private static string WithMatrix(string original, string pierce, string impact, string magic)
    {
        string[] lines = original.Split('\n');
        string[] cells = { pierce, impact, magic };
        string[] attacks = { "pierce", "impact", "magic" };
        int written = 0;

        for (int index = 0; index < lines.Length; index++)
        {
            if (!lines[index].StartsWith("matrix ", StringComparison.Ordinal))
            {
                continue;
            }

            lines[index] = "matrix " + attacks[written] + " " + cells[written];
            written++;
        }

        Assert.Equal(3, written);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// The committed ruleset with one number moved, and the hash of what that
    /// parses to. The substitution is asserted to have happened, because a
    /// replacement that matched nothing would compare the file against itself
    /// and agree.
    /// </summary>
    private static Hash64 Retuned(string original, string authored, string planted)
    {
        Assert.Contains(authored, original, StringComparison.Ordinal);

        return Ruleset.Parse(original.Replace(authored, planted, StringComparison.Ordinal)).ContentHash;
    }

    /// <summary>
    /// The committed schedule with one number moved, and the hash of what that
    /// parses to. The substitution is asserted to have happened, because a
    /// replacement that matched nothing would compare the file against itself
    /// and agree.
    /// </summary>
    private static Hash64 Reshaped(string original, string authored, string planted)
    {
        Assert.Contains(authored, original, StringComparison.Ordinal);

        return AnchorSchedule.Parse(
            original.Replace(authored, planted, StringComparison.Ordinal),
            TheMatch.Types()).ContentHash;
    }

    /// <summary>The same table with every comment line replaced by a different one.</summary>
    private static string WithCommentsRewritten(string original) =>
        string.Join(
            "\n",
            original
                .Split('\n')
                .Select(line => line.TrimStart().StartsWith('#') ? "# something else entirely" : line));

    /// <summary>The same table with the columns lined up by somebody else's habits.</summary>
    private static string WithColumnsRespaced(string original) =>
        string.Join(
            "\n",
            original
                .Split('\n')
                .Select(line => line.TrimStart().StartsWith('#') || line.Trim().Length == 0
                    ? line
                    : "\t" + string.Join(
                        "  \t ",
                        line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)) + "  "));

    /// <summary>The map's grid, with its legend stripped off.</summary>
    private static string WithoutMapComments(string original) =>
        string.Join(
            "\n",
            original
                .Split('\n')
                .Where(line => !line.TrimStart().StartsWith("//", StringComparison.Ordinal))
                .Where(line => line.Trim().Length > 0));
}
