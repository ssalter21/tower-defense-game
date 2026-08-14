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
        //
        // Both rows above were taken under the fold labelled
        // rule-fingerprint/1, which folded a match and nothing else. They are
        // history and cannot be recomputed by this build: see the label's
        // remarks on RuleFingerprint.
        (2u, 0x42346EF613910009UL),

        // Version 3 is #191 -- a wave slot's position became its release order,
        // so a round's slots leave one behind the other instead of all at once.
        // It is a release rule exactly as version 2's was, and every stored
        // record replays to a different outcome under it.
        //
        // IT IS ALSO THE ROW THAT CAUGHT A HOLE IN THIS FILE. Under
        // rule-fingerprint/1 this build's fingerprint came out
        // 42346EF613910009 -- byte for byte version 2's -- because the scenario
        // hands the match a wave written out above and a build phase never
        // composes one. The rule that moved lives in BuildPhase.Resolve, which
        // the fold could not see. A row whose evidence equals its predecessor's
        // is not evidence, so the fold gained a second half and the label went
        // to rule-fingerprint/2.
        (3u, 0x97AE0A007D5A9AB9UL),

        // Version 4 is #207 -- a creep is bought once and attacks every round
        // after, so a build phase names the whole of its round's wave and is
        // charged only for the increase over what it carries. Every stored run
        // replays to a different outcome under it, because every round after
        // the first now sends more than it did and pays less for it.
        //
        // IT CAUGHT THE SAME HOLE A SECOND TIME. Under rule-fingerprint/2 this
        // build's fingerprint came out 97AE0A007D5A9AB9 -- byte for byte
        // version 3's -- because both halves of that fold resolve a phase that
        // carries nothing, and a phase carrying nothing prices exactly as it did
        // before. The fold gained a third half that resolves against a carried
        // wave and folds what it cost, and the label went to
        // rule-fingerprint/3.
        (4u, 0x67E9F86CA94BE2D6UL),

        // Version 5 is #209 -- gold is paid for the health damage a wave does.
        // The bonus was one of four percentile bands worth at most a fifth of
        // the flat base; it is now a share of the leak cost the wave dealt, so
        // every stored run replays to a different purse from its first round
        // that gets anything past.
        //
        // IT CAUGHT THE SAME HOLE A THIRD TIME. Under rule-fingerprint/3 this
        // build's fingerprint came out 67E9F86CA94BE2D6 -- byte for byte
        // version 4's -- because all three halves of that fold resolve matches
        // and build phases and not one of them closes a wave. The rule that
        // moved lives in Purse, which the fold could not see. The fold gained a
        // fourth half that pays a purse off, and the label went to
        // rule-fingerprint/4.
        (5u, 0xB234D73EC659D3A7UL),
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

    /// <summary>
    /// The rules the payment half is folded through. Written out here rather
    /// than read off <c>content/ruleset.txt</c>, because the payment is
    /// arithmetic over authored numbers: folded against the committed file, a
    /// retune of the bonus rate or the income base would move this fingerprint
    /// and retire every record made under rules nobody changed.
    /// </summary>
    private const string FingerprintRules = """
        matrix pierce 140 70 100
        matrix impact 70 100 140
        matrix magic 100 140 70
        armour 1 100
        floor 1
        interest 10 0
        income 168
        purse 100
        bonus 25
        health 800
        snapshot 10 25
        """;

    private const string FingerprintWave = "order  0  1  3  0";

    /// <summary>
    /// The roster the composition half of the fingerprint composes out of: three
    /// moving rows, so that a wave can be arranged more than one way, and one
    /// placed row because a cost table prices both halves of a roster.
    /// </summary>
    private const string FingerprintComposedUnits = """
        unit  1  walker  moving  100  27  0     0  0  0  0  0  none     0  4
        unit  2  runner  moving  60   40  0     0  0  0  0  0  none     0  4
        unit  3  turret  placed  0    0   2000  5  2  1  4  9  hitscan  0  0
        unit  4  brute   moving  240  18  0     0  0  0  0  0  none     0  4
        """;

    /// <summary>
    /// A ladder with no edges in it, which is legal and is the point: what is
    /// being folded is the release schedule, and an edge would only add a
    /// refusal the composition half has no business asserting.
    /// </summary>
    private const string FingerprintComposedLadder = "layout 1";

    /// <summary>
    /// A purse the composed wave comfortably fits inside. It is deliberately not
    /// tight: what is being folded is the schedule the slots resolve to, and a
    /// purse that only just covered them would turn a price retune into a
    /// simulation-version bump.
    /// </summary>
    private const int FingerprintComposedGold = 100000;

    /// <summary>What the purse the payment half is folded through carries in.</summary>
    private const int FingerprintBank = 4321;

    /// <summary>
    /// The whole price of the wave the payment half's ceiling is taken against.
    /// A number rather than a wave, because what the ceiling is a ceiling on is
    /// a leak cost and this half is not composing one.
    /// </summary>
    private const int FingerprintWavePrice = 1300;

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

        // The one row-shaped rule moved. Every other number the file holds is
        // covered one at a time by the theory below, which is derived off the
        // file rather than listed; this one is here because it is not a column.
        //
        // The matrix, twice: once widened and once permuted. A single cell
        // cannot move on its own without the square stopping being a Latin
        // square, so the retune that tests the fold is a whole value class
        // moving, and the permutation is what proves a cell's position is
        // folded rather than the multiset of nine numbers.
        Assert.NotEqual(hash, Ruleset.Parse(WithMatrix(original, "150    70       100", "70   100       150", "100   150        70")).ContentHash);
        Assert.NotEqual(hash, Ruleset.Parse(WithMatrix(original, " 70   100       140", "100   140        70", "140    70       100")).ContentHash);

        // Nothing that is not a number moved. Each of these changes the file
        // and none of them changes a rule.
        Assert.Equal(hash, Ruleset.Parse(WithCommentsRewritten(original)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(WithColumnsRespaced(original)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal)).ContentHash);
        Assert.Equal(hash, Ruleset.Parse(original + "\n\n\n").ContentHash);
    }

    [Theory]
    [MemberData(nameof(TheRuleset.EveryNumber), MemberType = typeof(TheRuleset))]
    public void Moving_any_one_number_of_the_ruleset_moves_its_content_hash(string keyword, int column)
    {
        // One case per number the committed file holds outside the matrix,
        // taken off the file rather than written down here. The
        // simulation declares a ruleset field once, on the row that carries it,
        // and refuses a file with a row missing or a row carrying the wrong
        // number of columns -- so the columns of the committed file ARE the
        // declared fields, and a field somebody adds arrives here without
        // anybody adding a case for it.
        //
        // A field that is parsed and not folded is silent: retune it and the
        // content hash does not move, so every stored command stream stamped
        // against the old value passes the ruleset gate against the new one.
        // That is what this theory covers, in one case per number, without
        // depending on whoever added the number to have added an assertion.
        //
        // OBSERVED: stop Ruleset.Fold's walk one entry short of the end. The
        // two snapshot rows go red, the free count and the price each coming
        // out at DB21B47F2448B2BF whatever they are set to, and the other
        // twelve stay green.
        Assert.NotEqual(
            TheRuleset.Committed().ContentHash,
            Ruleset.Parse(TheRuleset.MovedNumber(keyword, column)).ContentHash);
    }

    [Fact]
    public void Editing_the_ladder_moves_its_content_hash_and_reformatting_it_does_not()
    {
        // The same pair again, for the file that holds the one prerequisite the
        // game has. It matters here for a reason it did not before #179: the
        // simulation reads this file now. An edge decides what `place` refuses,
        // so a ladder that was edited under a stored record is a stored record
        // whose refusals no longer hold -- which is what the stamp exists to
        // catch, and the stamp is only as good as this fold.
        //
        // OBSERVED: fold the characters of the text in UpgradeLadder.Parse
        // instead of the parsed fields. Every edit assertion still passes,
        // because a changed number is also changed bytes. Every formatting
        // assertion goes red, at which point re-wrapping a comment retires every
        // run recorded against the ladder.
        UnitTypeTable types = TheMatch.Types();
        string original = TheLadder.CommittedText();
        Hash64 hash = UpgradeLadder.Parse(original, types).ContentHash;

        // A number moved, once per column an edge is made of. Each retires every
        // run pinned to the old ladder, which is exactly right: the run that
        // could not place a Ranger is not the run that can.
        Assert.NotEqual(hash, Relinked(original, "upgrade    3  14", "upgrade    4  14"));
        Assert.NotEqual(hash, Relinked(original, "upgrade    3  14", "upgrade    3   4"));


        // Nothing that is not a number moved. Each of these changes the file
        // and none of them changes the ladder.
        Assert.Equal(hash, UpgradeLadder.Parse(WithCommentsRewritten(original), types).ContentHash);
        Assert.Equal(hash, UpgradeLadder.Parse(WithColumnsRespaced(original), types).ContentHash);
        Assert.Equal(
            hash,
            UpgradeLadder.Parse(original.Replace("\n", "\r\n", StringComparison.Ordinal), types).ContentHash);
        Assert.Equal(hash, UpgradeLadder.Parse(original + "\n\n\n", types).ContentHash);
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
    /// every tick of a fixed scenario, and then over the wave a fixed build
    /// phase composes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The scenario is deliberately small and deliberately local. Every rule
    /// worth calling one reaches it -- the tick order, the release cadence, the
    /// targeting tiebreak, the dice, and the rounding under both the movement
    /// step and the lateral offsets -- and nothing in <c>content/</c> does.
    /// </para>
    /// <para>
    /// <b>The second half is here because the first half missed one.</b> A match
    /// is handed a wave, so folding a match says nothing about how a wave is
    /// composed -- and #191 changed exactly that, giving a slot's position its
    /// release order. Under the fold as it stood, a change that alters what
    /// every stored record replays to produced a fingerprint identical to the
    /// version before it. So a build phase's resolved wave is folded too: the
    /// tick, the type and the count of every order it composes, which is the
    /// whole of what a slot arrangement decides.
    /// </para>
    /// <para>
    /// <b>The label carries the shape of the fold, and bumping it retires the
    /// rows taken under the old one.</b> <c>rule-fingerprint/1</c> folded a
    /// match alone; <c>rule-fingerprint/2</c> folded a match and a composition;
    /// <c>rule-fingerprint/3</c> folds a match, a composition, and a second
    /// composition against a wave the round carries -- which is the only half
    /// that can see what a wave costs; <c>rule-fingerprint/4</c> folds a wave's
    /// payment too, which is the only half that can see what a wave earns.
    /// Versions 1 to 4 are recorded under earlier labels and cannot be
    /// recomputed here, which is a loss stated out loud rather than a table that quietly
    /// compares fewer things -- the same rule <see cref="Match"/> applies to its
    /// own state-hash label.
    /// </para>
    /// </remarks>
    private static Hash64 RuleFingerprint()
    {
        UnitTypeTable types = UnitTypeTable.Parse("fingerprint units", FingerprintUnits);
        HexMap map = HexMap.Parse("fingerprint map", FingerprintMap);
        TowerLayout layout = TowerLayout.Parse("fingerprint defense", FingerprintDefense, types);
        WaveScript wave = WaveScript.Parse("fingerprint wave", FingerprintWave, types);

        var match = new Match(map, TheRuleset.Committed(), layout, wave, FingerprintSeed);
        Hash64 fingerprint = Hash64.Start("rule-fingerprint/4").Add(unchecked((long)match.StateHash.Value));

        for (int tick = 0; tick < FingerprintTicks && !match.IsFinished; tick++)
        {
            match.Advance(1);
            fingerprint = fingerprint.Add(unchecked((long)match.StateHash.Value));
        }

        MatchResult result = match.Result();

        fingerprint = fingerprint
            .Add(result.Leaked, result.Total)
            .Add(result.FinalTick)
            .Add(unchecked((long)result.RollingStateHash.Value));

        return PaidIntoFingerprint(ComposedIntoFingerprint(fingerprint));
    }

    /// <summary>
    /// The fourth half of the fold: what a wave pays a purse, itemised, and the
    /// ceiling a walk over a stored stream folds instead.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This half is here because the three above it missed one.</b> They
    /// resolve matches and build phases, and not one of them closes a wave -- so
    /// #209, which changed the bonus from a percentile band into a share of what
    /// a wave dealt and moves what every stored run replays to, produced a
    /// fingerprint identical to the version before it. The third time this file
    /// has had that hole.
    /// </para>
    /// <para>
    /// Three leak costs, an order of magnitude apart, because a bonus that
    /// ignores what a wave dealt folds all three the same -- which is exactly
    /// the shape the band lookup had.
    /// </para>
    /// </remarks>
    private static Hash64 PaidIntoFingerprint(Hash64 fingerprint)
    {
        Ruleset rules = Ruleset.Parse("fingerprint rules", FingerprintRules);
        Purse purse = Purse.Holding(FingerprintBank);
        int[] dealt = { 0, 37, 673 };

        // OBSERVED: pay a flat share of the income base -- the shape the four
        // bands had -- rather than a share of what the wave dealt. The
        // fingerprint goes B234D73EC659D3A7 to 80A3DB0779957EA1 and the
        // version-5 row goes red naming both numbers, which is what it could not
        // do before the fold had this half in it.
        for (int index = 0; index < dealt.Length; index++)
        {
            WavePayment paid = purse.CloseWave(rules, dealt[index]);

            fingerprint = fingerprint
                .Add(paid.Interest, paid.IncomeBase)
                .Add(paid.Bonus, paid.Purse.Gold);
        }

        // And the ceiling, which is the other thing a rule change to the payment
        // moves: a walk over a stored stream folds this instead of the payment,
        // and a ceiling that stops being one admits decisions no run could
        // afford.
        WavePayment ceiling = purse.CloseWaveAtBest(rules, FingerprintWavePrice);

        return fingerprint.Add(ceiling.Bonus, ceiling.Purse.Gold);
    }

    /// <summary>
    /// The second half of the fold: the wave a fixed build phase composes, order
    /// by order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Three filled slots and an empty one between two of them, because the
    /// empty slot is the case with a rule in it -- it takes no place in the
    /// column, so banking a slot closes the gap rather than leaving a hole. Two
    /// slots of unequal count, because equal counts would fold the same under a
    /// schedule that spaced slots by position instead of by the creeps ahead of
    /// them.
    /// </para>
    /// <para>
    /// OBSERVED: give every order the same release tick, which is what
    /// BuildPhase did before #191. The fingerprint goes 97AE0A007D5A9AB9 to
    /// D5B62912DBA14BFA and the version-3 row goes red naming both numbers,
    /// which is what it could not do before the fold had this half in it: the
    /// same edit under rule-fingerprint/1 was invisible.
    /// </para>
    /// </remarks>
    private static Hash64 ComposedIntoFingerprint(Hash64 fingerprint)
    {
        UnitTypeTable types = UnitTypeTable.Parse("fingerprint composed units", FingerprintComposedUnits);
        UpgradeLadder ladder = UpgradeLadder.Parse("fingerprint ladder", FingerprintComposedLadder, types);
        HexMap map = HexMap.Parse("fingerprint map", FingerprintMap);
        Ruleset rules = TheRuleset.Committed();

        Build composed = BuildPhase
            .Of(
                WaveSlot.Of(2, 3),
                WaveSlot.Empty,
                WaveSlot.Of(1, 1),
                WaveSlot.Of(4, 2))
            .Resolve(
                1,
                WaveScript.Nothing,
                ladder,
                Purse.Holding(FingerprintComposedGold),
                CostTable.From(rules, types),
                types,
                map,
                Board.Empty);

        fingerprint = fingerprint.Add(composed.Wave.Count, composed.Wave.TotalUnits);

        for (int index = 0; index < composed.Wave.Count; index++)
        {
            UnitOrder order = composed.Wave.Orders[index];
            fingerprint = fingerprint.Add(order.TickOffset, order.TypeId).Add(order.Count);
        }

        // The same phase again, against a round that already carries part of
        // what it sends. What this half sees that the one above cannot is what a
        // wave COSTS: a creep is bought once and attacks every round after, so
        // the price is the increase over what is carried and nothing else in
        // this file resolves a phase that carries anything.
        //
        // OBSERVED: price the slots at their full count -- charge slot.Count
        // rather than slot.Count minus what is held. The fingerprint goes back
        // to 97AE0A007D5A9AB9, byte for byte version 3's, and the version-4 row
        // goes red naming both numbers. Under rule-fingerprint/2 the same edit
        // was invisible, which is the hole this half closes and the second time
        // this file has had one.
        Build carried = BuildPhase
            .Of(
                WaveSlot.Of(2, 5),
                WaveSlot.Of(1, 1),
                WaveSlot.Of(4, 2))
            .Resolve(
                2,
                composed.Wave,
                ladder,
                Purse.Holding(FingerprintComposedGold),
                CostTable.From(rules, types),
                types,
                map,
                Board.Empty);

        return fingerprint.Add(carried.Spent, carried.Wave.TotalUnits);
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
    /// The committed ladder with one number moved, and the hash of what that
    /// parses to. The substitution is asserted to have happened, because a
    /// replacement that matched nothing would compare the file against itself
    /// and agree.
    /// </summary>
    private static Hash64 Relinked(string original, string authored, string planted)
    {
        Assert.Contains(authored, original, StringComparison.Ordinal);

        return UpgradeLadder.Parse(
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
