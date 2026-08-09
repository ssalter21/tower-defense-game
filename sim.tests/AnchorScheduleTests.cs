using System.Text;

namespace Sim.Tests;

/// <summary>
/// The anchor schedule: the shape parsed from the committed file and from text
/// planted to break each rule, and the filling one run draws onto it.
/// </summary>
/// <remarks>
/// <para>
/// Every parse in here is handed <b>text or bytes</b>, exactly as
/// <see cref="ContentTests"/> and <see cref="RulesetTests"/> are: the test opens
/// the file and the simulation never learns it exists.
/// </para>
/// <para>
/// <b>Every refusal is asserted by name.</b> The loader is where the shape's
/// constraints live, because a constraint that is remembered is a constraint
/// that is not enforced -- and a suite that only asserted "it threw" would pass
/// just as well when the whole file is refused for the wrong reason.
/// </para>
/// <para>
/// <b>Each was watched failing under a deliberately wrong input</b>, and the
/// wrong input is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class AnchorScheduleTests
{
    [Fact]
    public void The_committed_shape_is_three_anchors_at_waves_three_six_and_nine()
    {
        // The shape, spelled out. Three-in-ten, in the interior, so that wave
        // ten is the payoff round where what was taken gets spent.
        //
        // OBSERVED: move the middle anchor from wave 6 to wave 5 in
        // content/schedule.txt. The wave assertion goes red, [3, 5, 9] against
        // [3, 6, 9], which is what a shape retuned without anybody re-reading
        // the numbers written down here looks like.
        AnchorSchedule schedule = TheSchedule.Committed();

        Assert.Equal(3, schedule.Anchors.Count);
        Assert.Equal(new[] { 3, 6, 9 }, schedule.Anchors.Select(anchor => anchor.Wave));
        Assert.Equal(new[] { 1, 2, 3 }, schedule.Anchors.Select(anchor => anchor.Tier));

        // Exactly one steep counter, and it is the last anchor's.
        Assert.Equal(new[] { false, false, true }, schedule.Anchors.Select(a => a.OpensTheSteepCounter));

        // Every anchor's answer is purchasable strictly before it, and wave
        // nine's is late enough that eight rounds of income have paid for it.
        Assert.Equal(new[] { 1, 1, 8 }, schedule.Anchors.Select(anchor => anchor.CounterFromWave));
        Assert.Equal(new[] { 3, 3, 4 }, schedule.Anchors.Select(anchor => anchor.CounterTypeId));

    }

    [Fact]
    public void The_steep_column_says_nothing_the_anchors_own_position_does_not()
    {
        // Which anchor opens the steep counter is the last one, always, so the
        // column carries no value the shape does not already have and it is not
        // folded into the content hash. That is only safe while both spellings
        // of a disagreement are unloadable, which is what these are.
        //
        // OBSERVED: delete the count check in RequireOneSteepAnchorAtTheEnd.
        // The second assertion goes red having caught nothing -- two anchors
        // say steep, the last one is among them, and the shape loads. Delete
        // the last-anchor check as well and the first goes red too, on a shape
        // whose steep column names no anchor at all.
        string text = TheSchedule.CommittedText();

        Assert.Throws<ContentException>(
            () => TheSchedule.Of(PlantedText.Replace(text, "9     3  steep", "9     3  plain")));

        Assert.Throws<ContentException>(
            () => TheSchedule.Of(PlantedText.Replace(text, "3     1  plain", "3     1  steep")));
    }

    [Fact]
    public void Every_tier_pool_is_deeper_than_the_menu_drawn_from_it_and_opens_offense()
    {
        // Four to a pool against a menu of three, so that drawing a menu is a
        // draw rather than a copy of the pool.
        //
        // OBSERVED: change "offering 3 3" to "offering 3 4" in
        // content/ruleset.txt. The depth assertion goes red on its own message
        // -- a pool no deeper than the menu makes the filling a copy -- which
        // is the two files being multiplied together rather than one of them
        // being retuned alone.
        AnchorSchedule schedule = TheSchedule.Committed();
        UnitTypeTable types = TheMatch.Types();
        int menu = TheRuleset.Committed().GameChangersPerAnchor;

        Assert.Equal(12, schedule.GameChangers.Count);

        foreach (Anchor anchor in schedule.Anchors)
        {
            IReadOnlyList<GameChanger> pool = schedule.PoolFor(anchor);

            Assert.Equal(4, pool.Count);
            Assert.True(pool.Count > menu, "A pool no deeper than the menu makes the filling a copy.");
            Assert.All(pool, changer => Assert.Equal(UnitRole.Moving, types.ById(changer.TypeId).Role));
        }

        // And the counters are the other half of the same claim: what answers
        // an anchor stands where it was put.
        Assert.All(
            schedule.Anchors,
            anchor => Assert.Equal(UnitRole.Placed, types.ById(anchor.CounterTypeId).Role));
    }

    [Fact]
    public void The_slot_widths_are_derived_from_the_shape_and_moving_an_anchor_moves_them()
    {
        // The series the design names -- 2 2 3 3 3 4 4 4 5 5 -- computed from
        // the anchors in one file and the widening step in another, rather than
        // read out of a second series that could drift from either.
        //
        // The second half is the one that matters. Move the middle anchor from
        // wave 6 to wave 5 and the widths have to move with it: a schedule and a
        // slot series maintained separately would leave this green.
        //
        // OBSERVED: have AnchorSchedule.WaveSlotsAt return
        // rules.WaveSlotsAt(Anchors.Count) -- every round as wide as the run
        // ever gets. The committed series goes red, [5, 5, 5, 5, 5, 5, 5, 5, 5,
        // 5] against [2, 2, 3, 3, 3, 4, 4, 4, 5, 5], and slot scarcity -- the
        // thing standing in for a second purse -- evaporates at wave one.
        Assert.Equal(
            new[] { 2, 2, 3, 3, 3, 4, 4, 4, 5, 5 },
            TheSchedule.Widths(TheSchedule.Committed(), 10));

        AnchorSchedule moved = TheSchedule.Of(
            PlantedText.Replace(TheSchedule.CommittedText(), "anchor        6", "anchor        5"));

        Assert.Equal(new[] { 3, 5, 9 }, moved.Anchors.Select(anchor => anchor.Wave));
        Assert.Equal(new[] { 2, 2, 3, 3, 4, 4, 4, 4, 5, 5 }, TheSchedule.Widths(moved, 10));

        // And the widening step is still the ruleset's, so the two files are
        // multiplied together rather than one of them being ignored.
        Assert.Equal(2, TheRuleset.Committed().StartingWaveSlots);
        Assert.Equal(1, TheRuleset.Committed().WaveSlotsPerAnchor);
        Assert.Equal(0, TheSchedule.Committed().AnchorsBy(2));
        Assert.Equal(3, TheSchedule.Committed().AnchorsBy(9));
    }

    [Fact]
    public void A_schedule_that_authors_a_second_slot_series_refuses_to_load()
    {
        // Slot width is derived, and a copy of a derivation is free to drift
        // from it the first time somebody edits one and not the other.
        //
        // OBSERVED: delete the 'slots' branch in AnchorSchedule.ReadRow. The
        // row falls through to the unknown-row refusal, this goes red on the
        // message, and a designer who authored a slot series here is told their
        // row is unrecognised rather than that the number is computed.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Minimal + "\nslots 2 1"));

        Assert.Contains("Wave slot width is DERIVED", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_simulation_takes_the_schedule_as_bytes_as_well_as_text_and_agrees_with_itself()
    {
        // OBSERVED: strip a byte-order mark unconditionally in the byte path --
        // a .Substring(1) on what DataText.FromUtf8 decoded, as though every
        // file handed to it carried one. This goes red on the throw: the first
        // line loses its '#', " The anchor schedule." reaches the field
        // splitter and the parse refuses on the '.' at column 21. The text path
        // is untouched, which is what a second entry point drifting from the
        // first looks like.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            AnchorSchedule.Parse(File.ReadAllText(RepoLayout.ScheduleFile), types).ContentHash,
            AnchorSchedule.ParseUtf8(File.ReadAllBytes(RepoLayout.ScheduleFile), types).ContentHash);
    }

    [Fact]
    public void A_byte_order_mark_is_not_a_content_change_to_the_schedule()
    {
        // OBSERVED: delete the byte-order-mark strip in DataText.SplitLines.
        // This goes red on the throw -- "carries a character outside printable
        // ASCII at column 1 (code point 65279)" -- so a schedule any Windows
        // text writer produced refuses to load rather than parsing to what it
        // says.
        UnitTypeTable types = TheMatch.Types();
        string text = File.ReadAllText(RepoLayout.ScheduleFile);
        byte[] withMark = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true).GetPreamble()
            .Concat(Encoding.UTF8.GetBytes(text))
            .ToArray();

        Assert.Equal(
            AnchorSchedule.Parse(text, types).ContentHash,
            AnchorSchedule.ParseUtf8(withMark, types).ContentHash);
    }

    [Fact]
    public void The_schedule_hash_is_not_the_ruleset_or_the_unit_table_hash()
    {
        // Every fold starts from a label naming the table and its layout, so
        // two tables cannot collide by holding coincidentally equal integers.
        //
        // OBSERVED: stop Hash64 distinguishing anything -- skip the label loop
        // in Start and return `this` from Add(long). All three assertions go
        // red, every table in the project coming back as the bare FNV offset
        // basis CBF29CE484222325.
        Hash64 hash = TheSchedule.Committed().ContentHash;

        Assert.NotEqual(hash, TheRuleset.Committed().ContentHash);
        Assert.NotEqual(hash, TheMatch.Types().ContentHash);
        Assert.NotEqual(hash, TheMatch.Map().MapHash);
    }

    [Fact]
    public void The_minimal_schedule_the_planted_texts_are_built_from_parses()
    {
        // Without this, every refusal below could be firing on a fault the
        // fixture always had rather than on the one the test planted.
        //
        // OBSERVED: delete the "changer 4 late-b 2 2 400" row from
        // TheSchedule.Minimal. The pool assertion goes red, 4 against 3 -- the
        // fixture read back, rather than the fixture assumed.
        Assert.Equal(2, TheSchedule.Small().Anchors.Count);
        Assert.Equal(4, TheSchedule.Small().GameChangers.Count);
    }

    [Fact]
    public void Anchors_out_of_wave_order_refuse_to_load()
    {
        // OBSERVED: drop the wave comparison in Draft.AddAnchor. This goes red
        // having caught nothing, and a shape whose anchors read 6 then 3 loads
        // -- at which point AnchorsBy counts them in a different order than the
        // file states them and the slot widths are whatever the sort happened
        // to be.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("anchor 3 1 plain 3 1", "anchor 7 1 plain 3 1")));

        Assert.Contains("Anchors ascend strictly", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_game_changer_on_two_anchors_menus_refuses_to_load()
    {
        // A game changer sits in exactly one tier pool and therefore on exactly
        // one anchor's menu, so that nobody doubles down on the same one twice.
        // A repeated id is that creep in two pools.
        //
        // OBSERVED: delete the duplicate-id loop in Draft.AddChanger. The first
        // assertion goes red on the message -- "has game changer id 1 after id
        // 2", the ascent rule catching the cross-tier case for the wrong reason
        // -- and the second is left with nothing at all, because ids 1, 1, 3, 4
        // ascend weakly and a pool offering one creep twice loads.
        ContentException across = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("changer 3 late-a 2 1 400", "changer 1 late-a 2 1 400")));

        Assert.Contains("exactly one anchor's menu", across.Message, StringComparison.Ordinal);

        ContentException within = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("changer 2 early-b 1 2 0", "changer 1 early-b 1 2 0")));

        Assert.Contains("second game changer with id 1", within.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Game_changer_ids_that_descend_refuse_to_load()
    {
        // Ids ascend strictly, which is what makes the pools canonical and a
        // duplicate a comparison against the row above rather than a scan.
        //
        // OBSERVED: drop the ascent check in Draft.AddChanger. This goes red
        // having caught nothing, and a file whose ids read 1, 2, 3, 6, 5 loads
        // -- at which point two files stating one shape in two orders are two
        // different content hashes.
        ContentException thrown = Assert.Throws<ContentException>(() => TheSchedule.Of(
            TheSchedule.Planted("changer 4 late-b 2 2 400", "changer 6 late-b 2 2 400")
            + "\nchanger 5 late-c 2 1 400"));

        Assert.Contains("Ids ascend strictly", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_shape_whose_tiers_do_not_escalate_refuses_to_load()
    {
        // Later anchors are stronger than earlier ones. A flat pool could hand
        // somebody a wave-nine-grade creep at wave three, where nothing yet
        // answers it.
        //
        // OBSERVED: drop the tier comparison in Draft.AddAnchor. This goes red
        // on the message -- "puts late-a (#3) in tier 2, which no anchor draws
        // from" -- because both anchors now draw from tier 1 and the refusal
        // that fires is about the pool nobody can reach rather than about the
        // wave-6 anchor offering wave-3's creeps.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("anchor 6 2 steep 3 5", "anchor 6 1 steep 3 5")));

        Assert.Contains("Tiers escalate with the waves", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anchor 6 2 steep 3 5", "anchor 6 2 plain 3 5", "0 anchors opening a steep counter")]
    [InlineData("anchor 3 1 plain 3 1", "anchor 3 1 steep 3 1", "2 anchors opening a steep counter")]
    public void A_shape_with_any_count_of_steep_counters_but_one_refuses_to_load(
        string authored,
        string planted,
        string named)
    {
        // None makes preparation optional; more than one turns a run on a
        // single missed buy, which is the outcome the whole ruleset avoids.
        //
        // OBSERVED: delete the count check in RequireOneSteepAnchorAtTheEnd.
        // The no-steep row goes red on the message, caught by the last-anchor
        // check saying the counter opens at wave 0 -- a wave no shape has. The
        // two-steep row goes red having caught nothing at all: both anchors say
        // steep, the last one is among them, and the shape loads.
        ContentException thrown =
            Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));

        Assert.Contains(named, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_steep_counter_that_is_not_the_last_anchors_refuses_to_load()
    {
        // Late enough that the rounds of income that pay for the answer have
        // happened before the question is asked.
        //
        // OBSERVED: delete the last-anchor check in
        // RequireOneSteepAnchorAtTheEnd. This goes red having caught nothing,
        // and a shape that demands its one specific answer at wave 3 -- before
        // anybody has the gold for it -- loads.
        ContentException thrown = Assert.Throws<ContentException>(() => TheSchedule.Of(
            PlantedText.Replace(
                TheSchedule.Planted("anchor 3 1 plain 3 1", "anchor 3 1 steep 3 1"),
                "anchor 6 2 steep 3 5",
                "anchor 6 2 plain 3 5")));

        Assert.Contains("rather than at the last anchor", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anchor 6 2 steep 3 5", "anchor 6 2 steep 3 6")]
    [InlineData("anchor 6 2 steep 3 5", "anchor 6 2 steep 3 9")]
    public void A_counter_not_purchasable_strictly_before_its_anchor_refuses_to_load(
        string authored,
        string planted)
    {
        // The constraint seam 3 inherits rather than chooses, enforced here
        // rather than remembered: an answer that first appears at the wave it
        // answers is a forced simultaneous buy, and it deletes the preparation
        // the whole schedule exists to restore.
        //
        // OBSERVED: relax the comparison in Draft.AddAnchor to
        // counterFromWave > wave. The first row goes red having caught nothing,
        // and a shape whose wave-6 answer is first purchasable at wave 6 loads
        // -- which is the exact case the constraint is written for, since the
        // second row was never the interesting one.
        ContentException thrown =
            Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));

        Assert.Contains("STRICTLY BEFORE the anchor that needs it", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_anchor_that_opens_defense_refuses_to_load()
    {
        // An anchor is a threat that can be seen coming, and the preparation
        // happens on the other side of the board. A better tower would be a
        // gift, and it would leave preparation with nothing to be about.
        //
        // OBSERVED: pass a null role to the RequireType call in
        // Draft.AddChanger. This goes red having caught nothing, and a shape
        // whose wave-3 menu offers the Archer tower loads -- at which point an
        // anchor hands out defense and the whole preparation axis has nothing
        // on it.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("changer 1 early-a 1 1 0", "changer 1 early-a 1 3 0")));

        Assert.Contains(
            "a game changer's body requiring a moving unit",
            thrown.Message,
            StringComparison.Ordinal);

        // The tail as well as the head: the head is what an unknown id would
        // say too, so pinning it alone would leave this green if the role check
        // went and the id lookup broke instead.
        Assert.Contains("which is a placed unit", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_anchor_answered_by_something_that_walks_the_corridor_refuses_to_load()
    {
        // The other side of the same rule. What answers an anchor stands where
        // it was put, because that is the side of the board preparation happens
        // on.
        //
        // OBSERVED: pass a null role to the RequireType call in
        // Draft.AddAnchor. This goes red having caught nothing, and a shape that
        // answers wave 3 with a Minion loads -- an anchor whose preparation is
        // another wave, which is the arms race the offense-only rule exists to
        // keep off the board.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("anchor 3 1 plain 3 1", "anchor 3 1 plain 1 1")));

        Assert.Contains(
            "an anchor's counter requiring a placed unit",
            thrown.Message,
            StringComparison.Ordinal);
        Assert.Contains("which is a moving unit", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("changer 1 early-a 1 1 0", "changer 1 early-a 1 1 400", "outside the steep anchor's tier")]
    [InlineData("changer 3 late-a 2 1 400", "changer 3 late-a 2 1 0", "with no bonus against its tag")]
    public void A_bonus_anywhere_but_the_steep_anchors_pool_refuses_to_load(
        string authored,
        string planted,
        string named)
    {
        // The steep counter is a property of an anchor and of the pool it draws
        // from, and the two have to agree. A bonus on a plain tier is a second
        // steep anchor that never said so; a plain creep on the steep tier is a
        // menu the steep anchor can draw that opens no counter at all.
        //
        // OBSERVED: delete RequireTheBonusOnlyOnTheSteepTier. Both rows go red
        // having caught nothing, and the shape that says "plain" at wave 3
        // hands out a four-hundred-point counter there.
        ContentException thrown =
            Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));

        Assert.Contains(named, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_tier_pool_no_anchor_draws_from_refuses_to_load()
    {
        // Content nobody can be offered, whose numbers would still move the
        // content hash.
        //
        // OBSERVED: delete the second loop of RequireEveryTierPaired. This goes
        // red having caught nothing, and a shape carrying a whole tier nobody
        // reaches loads -- retiring every run pinned to the shape before it for
        // rows no menu will ever draw.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Minimal + "\nchanger 5 orphan 7 1 0"));

        Assert.Contains("which no anchor draws from", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_anchor_whose_tier_has_no_pool_refuses_to_load()
    {
        // The other direction of the same pairing, and the one that would
        // otherwise surface as a run refusing to start rather than a file
        // refusing to load.
        //
        // OBSERVED: delete the first loop of RequireEveryTierPaired. This goes
        // red on the message: the second loop catches the same file from the
        // other end, saying "puts late-a (#3) in tier 2, which no anchor draws
        // from" and naming a pool the author never touched instead of the
        // anchor they moved.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("anchor 6 2 steep 3 5", "anchor 6 5 steep 3 5")));

        Assert.Contains("which no game changer is in", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_anchor_at_the_wave_a_run_starts_on_cannot_be_authored_at_all()
    {
        // Wave one is the starting state, and nothing separately forbids an
        // anchor there: the counter rule already does it, because a wave before
        // wave one is not a wave. That is the whole of why there is no second
        // bound on the column -- one rule, and the range that would have
        // duplicated it would be a guard no planted text could ever reach.
        //
        // OBSERVED: drop the counterFromWave comparison in Draft.AddAnchor.
        // This goes red having caught nothing, and an anchor at wave one loads
        // -- the wave a run starts on, standing in front of a build phase
        // nobody has had a round of income for.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Planted("anchor 3 1 plain 3 1", "anchor 1 1 plain 3 1")));

        Assert.Contains("STRICTLY BEFORE the anchor that needs it", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anchor 3 1 plain 3 1", "anchor 3 1 plain 99 1")]
    [InlineData("changer 1 early-a 1 1 0", "changer 1 early-a 1 99 0")]
    public void A_type_id_no_unit_table_has_refuses_to_load(string authored, string planted)
    {
        // The schedule names units and the unit table owns them, so a shape
        // read against the wrong table is refused rather than carrying an id
        // that resolves to nothing at the wave it matters.
        //
        // OBSERVED: replace the RequireType calls in Draft.AddAnchor and
        // Draft.AddChanger with ById, which throws its own ContentException.
        // Both rows go red on the message -- "unit types: has no type with id
        // 99" -- so the refusal names the roster and the line it came from is
        // the roster's line 0, pointing a designer at the wrong file entirely.
        ContentException thrown =
            Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));

        Assert.Contains(
            "names type id 99, which this unit type table does not define",
            thrown.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_the_schedule_does_not_have_refuses_to_load_rather_than_being_skipped()
    {
        // OBSERVED: return from AnchorSchedule.ReadRow's default branch instead
        // of throwing. This goes red having caught nothing, and a row somebody
        // misspelled is dropped -- a shape that is missing whatever that row
        // was going to say, loading as though it said nothing.
        ContentException thrown = Assert.Throws<ContentException>(
            () => TheSchedule.Of(TheSchedule.Minimal + "\nrotation 4"));

        Assert.Contains("'rotation'", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anchor 3 1 plain 3 1", "anchor 3 1 plain 3")]
    [InlineData("changer 1 early-a 1 1 0", "changer 1 early-a 1 1 0 0")]
    public void A_row_with_the_wrong_number_of_fields_refuses_to_load(string authored, string planted)
    {
        // OBSERVED: make DataText.RequireFieldCount a no-op. The short anchor
        // row goes red on the exception type -- an IndexOutOfRangeException,
        // because ReadRow walks six fields off a row that has five -- and the
        // long changer row goes red having caught nothing at all, its seventh
        // field silently dropped.
        ContentException thrown =
            Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));

        Assert.Contains("fields where the layout has 6", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("anchor 3 1 plain 3 1", "anchor 3.5 1 plain 3 1")]
    [InlineData("changer 3 late-a 2 1 400", "changer 3 late-a 2 1 4,00")]
    public void A_fraction_in_the_schedule_refuses_to_load(string authored, string planted)
    {
        // The two characters a designer types when they want a fraction, and
        // the simulation has no representation for one that arrived as text.
        //
        // OBSERVED: drop the '.' and ',' refusal in DataText.Fields and have
        // DataText.Integer stop at the first character that is not a digit
        // rather than refuse it. Both rows go red having caught nothing: an
        // anchor authored at wave 3.5 loads at wave 3, and a bonus of 4,00
        // loads as 4.
        Assert.Throws<ContentException>(() => TheSchedule.Of(TheSchedule.Planted(authored, planted)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("# nothing but a comment")]
    [InlineData("anchor 3 1 steep 3 1")]
    public void A_schedule_missing_a_whole_half_of_itself_refuses_to_load(string text)
    {
        // OBSERVED: delete the RequireEverything call in AnchorSchedule.Parse.
        // All three rows go red having caught nothing, and an empty file loads
        // to a shape with no anchors and no pools -- a run against which is ten
        // ordinary rounds on a slot width that never widens.
        //
        // The two count guards inside RequireEverything are not what to delete:
        // with those gone the steep rule and the tier pairing refuse all three
        // of these anyway, and this stays green on refusals that say nothing
        // about the half that is missing.
        Assert.Throws<ContentException>(() => TheSchedule.Of(text));
    }

    [Fact]
    public void The_filling_is_three_game_changers_an_anchor_drawn_from_that_anchors_tier_alone()
    {
        // What an anchor round puts in front of everybody: the ruleset's count
        // of game changers, all distinct, all from the tier the shape gave that
        // anchor. Tiers escalate, so drawing from the anchor's own pool is what
        // stops a late-grade creep reaching wave three.
        //
        // OBSERVED: draw from GameChangers rather than from PoolFor(anchor) in
        // AnchorSchedule.Fill. The tier assertion goes red on the wave-three
        // menu, 2 against 1 and then 3 against 1, which is that menu offering
        // the steep counter six waves before anything answers it.
        Run run = TheRun.Fresh();
        Ruleset rules = TheRuleset.Committed();

        Assert.Equal(3, run.Filling.Count);

        foreach (AnchorMenu menu in run.Filling.Menus)
        {
            Assert.Equal(rules.GameChangersPerAnchor, menu.GameChangers.Count);
            Assert.All(menu.GameChangers, changer => Assert.Equal(menu.Anchor.Tier, changer.Tier));

            Assert.Equal(
                menu.GameChangers.Count,
                menu.GameChangers.Select(changer => changer.Id).Distinct().Count());
        }

        // And a game changer reaches at most one menu across the whole run,
        // which is the file's rule surviving the draw.
        int[] drawn = run.Filling.Menus.SelectMany(menu => menu.GameChangers).Select(c => c.Id).ToArray();

        Assert.Equal(9, drawn.Length);
        Assert.Equal(9, drawn.Distinct().Count());

        Assert.True(run.Filling.IsAnchor(9));
        Assert.False(run.Filling.IsAnchor(10));
        Assert.Equal(9, run.Filling.At(9).Anchor.Wave);
        Assert.Throws<SimulationException>(() => run.Filling.At(10));
    }

    [Fact]
    public void The_filling_is_revealed_at_run_start_and_is_the_same_run_for_the_same_seed()
    {
        // Drawn once, before a round is played, so that the shape is what was
        // public all week and the filling is what this run got. A run that drew
        // its wave-nine menu at wave nine would have nothing to prepare against.
        //
        // OBSERVED: fold the round count into the filling's derived position --
        // Derived(FillingLabel, Round, 0, 0), computed by the property rather
        // than once in the constructor. This goes red on the first round that
        // resolves, [1, 3, 4, 6, 8, ...] against [3, 2, 1, 7, 5, ...]: the menu
        // a player prepared against is not the menu they are offered.
        Run run = TheRun.Fresh(waves: 4, fieldSize: 2);
        int[] before = Ids(run.Filling);

        while (!run.IsOver)
        {
            run.Advance(TheRun.Orders());
            Assert.Equal(before, Ids(run.Filling));
        }

        Assert.Equal(before, Ids(TheRun.Fresh(waves: 4, fieldSize: 2).Filling));

        // A different seed is a different run, which is the whole reason the
        // filling is drawn rather than authored.
        Assert.NotEqual(before, Ids(TheRun.Fresh(10, 10, true, TheRun.Seed + 1).Filling));
    }

    [Fact]
    public void The_filling_is_drawn_at_a_position_of_its_own_and_nothing_is_keyed_on_what_it_drew()
    {
        // Two claims in one arrangement. The filling has a purpose label of its
        // own, so it cannot be the field's draw under another name. And the
        // field is drawn from the run and the round alone, so a pool is never
        // sharded on which filling a run got -- variance is not paid for with a
        // thinner pool.
        //
        // OBSERVED: mix the filling into the field's derived position -- pass
        // the first drawn game changer's id to Derived in Run.FieldSeed. The
        // first round assertion goes red, 167 against 137: two runs on one seed
        // meet different fields because they were dealt different menus, which
        // is what sharding a pool on the filling costs.
        Run one = Rebuilt(TheSchedule.CommittedText());
        Run two = Rebuilt(TheSchedule.CommittedText() + "\nchanger 13 extra-early 1 1 0");

        // The two shapes agree about every anchor and disagree about how deep
        // the first tier's pool is, so the same seed deals two different
        // fillings against one identical set of anchors.
        Assert.Equal(
            one.Schedule.Anchors.Select(anchor => anchor.Wave),
            two.Schedule.Anchors.Select(anchor => anchor.Wave));

        Assert.NotEqual(one.Schedule.ContentHash, two.Schedule.ContentHash);
        Assert.NotEqual(Ids(one.Filling), Ids(two.Filling));

        RoundOrders orders = TheRun.Orders();

        Assert.Equal(one.Advance(orders).LeakCostTaken, two.Advance(orders).LeakCostTaken);
        Assert.Equal(one.Advance(orders).LeakCostTaken, two.Advance(orders).LeakCostTaken);
    }

    [Fact]
    public void A_tier_pool_thinner_than_the_menu_it_has_to_fill_is_refused()
    {
        // The pool depth the file cannot check on its own, because how many a
        // menu carries is a ruleset number. Refused where the two meet, before
        // a run has produced anything anybody would keep.
        //
        // OBSERVED: drop the pool-depth guard in AnchorSchedule.Fill. This goes
        // red on the exception type: the partial Fisher-Yates runs off the end
        // of its positions and an ArgumentOutOfRangeException surfaces out of
        // Pcg32.NextBelow, naming a bound of zero rather than a pool anybody
        // could re-author.
        SimulationException thrown = Assert.Throws<SimulationException>(
            () => TheSchedule.Small().Fill(TheRuleset.Committed().GameChangersPerAnchor, TheRun.Seed));

        Assert.Contains("from a tier pool of 2", thrown.Message, StringComparison.Ordinal);
        Assert.Throws<SimulationException>(() => TheSchedule.Committed().Fill(0, TheRun.Seed));
    }

    [Fact]
    public void A_steep_counter_is_about_four_times_as_hard_a_hit_and_is_paid_only_to_the_answer()
    {
        // Steep rather than binary. The bonus joins the rolled damage before the
        // type chart and armour, so it is mitigated along with the rest of the
        // hit rather than bypassing it -- which is what makes a high-armour
        // creep blunt its own counter.
        //
        // OBSERVED: return changer.BonusVsTag from AnchorSchedule.BonusVsTag
        // without checking the shooter against the anchor's counter. The
        // unprepared bonus goes red, 825 against 0: every tower in the game
        // gets the counter's bonus, and preparing for wave nine buys nothing at
        // all.
        AnchorSchedule schedule = TheSchedule.Committed();
        Ruleset rules = TheRuleset.Committed();
        UnitTypeTable types = TheMatch.Types();

        Anchor steep = schedule.Anchors[schedule.Anchors.Count - 1];
        GameChanger changer = schedule.PoolFor(steep).First(candidate => candidate.TypeId == 1);
        UnitType answer = types.ById(steep.CounterTypeId);
        UnitType body = types.ById(changer.TypeId);

        // The Mage's mid roll against the Minion-bodied game changer. The Mage
        // carries magic under the signed roster's one-type-per-line rule, and
        // the Minion is Armoured, so the matrix cell is 140 rather than the 100
        // an impact answer used to read -- every number below is 1.4x what it
        // was and the four-to-one ratio the anchor is designed around survives
        // that exactly, because a multiplier applies to both sides of it.
        int roll = (answer.DamageMin + answer.DamageMax) / 2;

        Assert.Equal(275, roll);
        Assert.Equal(825, changer.BonusVsTag);
        Assert.Equal(825, schedule.BonusVsTag(answer.Id, changer));
        Assert.Equal(0, schedule.BonusVsTag(3, changer));

        int prepared = DamageModel.Dealt(
            rules, roll, schedule.BonusVsTag(answer.Id, changer), answer.AttackType, body.ArmourType, 0);

        int unprepared = DamageModel.Dealt(
            rules, roll, schedule.BonusVsTag(3, changer), answer.AttackType, body.ArmourType, 0);

        Assert.Equal(1540, prepared);
        Assert.Equal(385, unprepared);
        Assert.Equal(4, prepared / unprepared);

        // Armour blunts the counter along with everything else, so the steep
        // anchor pays for its own toughness rather than being exempt from it.
        int armoured = DamageModel.Dealt(
            rules, roll, schedule.BonusVsTag(answer.Id, changer), answer.AttackType, body.ArmourType, 60);

        Assert.Equal(962, armoured);
        Assert.True(armoured < prepared, "Armour did nothing at all against a hit carrying a counter.");
        Assert.InRange(armoured * 100 / DamageModel.Dealt(
            rules, roll, 0, answer.AttackType, body.ArmourType, 60), 390, 410);
    }

    [Fact]
    public void Every_game_changer_outside_the_steep_anchors_menu_is_answered_without_a_bonus()
    {
        // Exactly one anchor per shape opens a steep counter, so preparing for
        // the other two is a matter of degree rather than of a number nobody
        // can see coming.
        //
        // OBSERVED: move skyborne's bonus from 825 to 826 in
        // content/schedule.txt. This goes red, 826 against 825, which is what a
        // counter retuned on one row of the steep pool and nowhere else looks
        // like from here.
        AnchorSchedule schedule = TheSchedule.Committed();

        foreach (Anchor anchor in schedule.Anchors)
        {
            foreach (GameChanger changer in schedule.PoolFor(anchor))
            {
                Assert.Equal(
                    anchor.OpensTheSteepCounter ? 825 : 0,
                    schedule.BonusVsTag(anchor.CounterTypeId, changer));
            }
        }
    }

    /// <summary>The ids a filling drew, anchor by anchor, as one flat list.</summary>
    private static int[] Ids(AnchorFilling filling) =>
        filling.Menus.SelectMany(menu => menu.GameChangers).Select(changer => changer.Id).ToArray();

    /// <summary>The committed run against a schedule planted from text.</summary>
    private static Run Rebuilt(string schedule)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            AnchorSchedule.Parse(schedule, types),
            TheRun.Pool(types),
            TheRun.Seed,
            waves: 2,
            fieldSize: 4);
    }
}
