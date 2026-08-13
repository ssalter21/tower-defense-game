using Sim.Cli;

namespace Sim.Tests;

/// <summary>
/// The frame a round stands above its prompt, against the committed run.
/// </summary>
/// <remarks>
/// <para>
/// <b>The whole frame is asserted, not a property of it.</b> What can be wrong
/// with a drawing is where its characters are -- a panel that drifted a column,
/// a heading over the wrong rows, a price under the wrong name -- and none of
/// that is visible to an assertion that counts lines or looks for a substring.
/// It is all visible in a diff of the block. This is <c>BoardMapTests</c>'s rule
/// one level up, and the map inside these blocks is that drawing, so a
/// regression in it reddens both.
/// </para>
/// <para>
/// <b>The run is the committed one, played round by round.</b> The frames below
/// are the ones somebody playing <c>content/commands.txt</c> at a prompt would
/// have been shown, so every number on them can be read against a line of
/// <c>content/run-outcome.txt</c>: wave four's health is 1500 less the 100, 90
/// and 65 the first three rounds took, and its gold is what wave three's round
/// line says the purse closed on.
/// </para>
/// <para>
/// <b>Each block was watched failing under a deliberately wrong drawing</b>, and
/// the wrong drawing is written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class RoundFrameTests
{
    /// <summary>
    /// How many rounds of the committed script are played. Four, because wave
    /// four is the frame <c>docs/playing-a-run-from-a-shell.md</c> §2 works
    /// through and wave three is the anchor before it.
    /// </summary>
    private const int Rounds = 4;

    /// <summary>
    /// The option wave one's menu carries the skeleton-warrior at, whose label
    /// is the longest on the committed roster.
    /// </summary>
    private const int WarriorOption = 13;

    /// <summary>
    /// A game changer's label, planted long enough to be the widest thing the
    /// name column can print: twenty characters against the sixteen of
    /// <c>skeleton-warrior</c>, so the four the column gains are countable.
    /// </summary>
    private const string LongChanger = "thermal-riser-mk-two";

    /// <summary>
    /// The frames of the committed run, played once for the whole class. A
    /// round resolves twenty matches and measuring the field costs a hundred
    /// more, and every test here reads a frame off the same play.
    /// </summary>
    private static readonly Lazy<Frames> Played = new(Play);

    [Fact]
    public void The_first_frame_of_a_run_offers_a_whole_roster_over_an_empty_board()
    {
        // A run opens on nothing standing and nothing unlocked, so the sendable
        // panel is a heading with no rows under it and the map's legend says so
        // in words. Both are the first thing anybody playing this ever sees.
        //
        // OBSERVED: have RoundFrame.Sendable return its rows without the
        // heading. Wave one loses a line nobody would miss -- and every later
        // frame silently promotes its first creep into the heading's row,
        // reading as a panel whose title is the name of a unit.
        Assert.Equal(
            """
            wave 1 of 10        health 800 of 800        gold 100

                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14
             0    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
             1      .  S  #  #  #  #  #  #  #  #  #  #  .  .  .        nothing standing
             2    .  .  .  .  .  .  .  .  .  .  .  .  #  .  .
             3      .  .  #  #  #  #  #  #  #  #  #  #  .  .  .      you may build
             4    .  .  #  .  .  .  .  .  .  .  .  .  .  .  .         11  soldier   30
             5      .  .  #  #  #  #  #  #  #  #  #  #  #  .  .        3  archer    40
             6    .  .  .  .  .  .  .  .  .  .  .  .  .  #  .          4  mage      92
             7      .  E  #  #  #  #  #  #  #  #  #  #  #  .  .
             8    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .

            you may build      what you may send
             11  soldier   30    1  minion            10 each
              3  archer    40    2  skeleton-scout     9 each
              4  mage      92    7  necromancer       19 each
                                12  skeleton          17 each
                                13  skeleton-warrior  31 each

            nothing built, no slot filled.
            """,
            Played.Value.Opening[0]);
    }

    [Fact]
    public void The_longest_name_on_the_roster_keeps_the_gap_before_its_price()
    {
        // The sendable panel alone, which is what the word `menu` reprints.
        // Its widest row is the widest label content/units.txt carries, and the
        // column width the whole panel is laid out on is settled by that row.
        //
        // It used to take a composed take to reach this row, because the panel
        // listed what a run had unlocked and the skeleton-warrior was rationed.
        // #179 deleted the ration: every creep is sendable from wave one, so
        // the widest row is on every frame this file draws and the panel is the
        // whole roster.
        //
        // OBSERVED: pad the label to a constant sixteen with the price straight
        // after it, which is what the panel did until #177. Every other block in
        // this file reads the same, and this one goes red on
        // "skeleton-warrior31 each" -- a name run into its price on the one row
        // wide enough for the padding to add nothing.
        UnitTypeTable types = TheMatch.Types();
        UpgradeLadder ladder = TheMatch.Ladder(types);

        Assert.Equal(
            """
            what you may send
              1  minion            10 each
              2  skeleton-scout     9 each
              7  necromancer       19 each
             12  skeleton          17 each
             13  skeleton-warrior  31 each
            """,
            RoundFrame.ToText(
                Fresh(types, TheRun.FieldWave(types)),
                ladder,
                BuildPhase.Of(),
                Panel.Menu));
    }

    [Fact]
    public void The_frame_in_front_of_wave_four_is_the_one_the_specification_works_through()
    {
        // docs/playing-a-run-from-a-shell.md §2's worked frame, drawn from the
        // real map, the real offering and the real purse. It is character for
        // character what that section draws, numbers included: the section was
        // restaged to gold 545 when the wave income moved from 100 to 168 in
        // #165, which is three rounds of a difference by the time this frame
        // stands.
        //
        // OBSERVED: take the price column off the buildable panel. The frame
        // still names four towers and their cells, and there is no longer
        // anything on it that says a mage is worth two archers and a soldier --
        // which is the one comparison the panel exists to let somebody make.
        Assert.Equal(
            """
            wave 4 of 10        health 545 of 800        gold 545

                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14
             0    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
             1      .  S  #  #  #  #  #  #  #  #  #  #  .  .  .        standing
             2    .  .  .  .  .  .  a  .  .  .  .  .  #  .  .          1  a  archer   6,2
             3      .  .  #  #  #  #  #  #  #  #  #  #  .  .  .        2  a  archer   7,4
             4    .  .  #  .  .  .  .  a  .  .  .  .  .  .  .          3  a  archer   7,6
             5      .  .  #  #  #  #  #  #  #  #  #  #  #  .  .
             6    .  .  .  .  .  .  .  a  .  .  .  .  .  #  .        you may build
             7      .  E  #  #  #  #  #  #  #  #  #  #  #  .  .       11  soldier   30
             8    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .          3  archer    40
                                                                       4  mage      92

            you may build      what you may send
             11  soldier   30    1  minion            10 each
              3  archer    40    2  skeleton-scout     9 each
              4  mage      92    7  necromancer       19 each
                                12  skeleton          17 each
                                13  skeleton-warrior  31 each

            nothing built, no slot filled.
            """,
            Played.Value.Opening[3]);
    }

    [Fact]
    public void Composing_the_round_moves_the_gold_the_board_the_unlocks_and_the_status_line()
    {
        // The same round with content/commands.txt's own wave-four decision
        // composed: take the skeleton, place a fourth archer at 4,4, and fill
        // the third slot with twenty scouts. Five things move and nothing else
        // does -- 545 gold becomes the 325 the round line's "spent 220" leaves,
        // an 'a' appears at column four of row four with a legend row under the
        // three already there, the skeleton joins the sendable panel at 17 gold
        // a body, and the status line says what was decided.
        //
        // Nothing on it is a forecast. Twenty scouts are composed and there is
        // no number here about what they will do, because the frame draws
        // mechanism and the wave has not been sent.
        //
        // OBSERVED: draw the map from run.Board rather than from the board the
        // phase resolved to. The gold falls to 325 and the status line says a
        // tower was built, and the map goes on showing three -- so the frame
        // charges for a decision it does not draw, which is the one thing the
        // panel beside the prompt is for.
        Assert.Equal(
            """
            wave 4 of 10        health 545 of 800        gold 325

                  0  1  2  3  4  5  6  7  8  9 10 11 12 13 14
             0    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .
             1      .  S  #  #  #  #  #  #  #  #  #  #  .  .  .        standing
             2    .  .  .  .  .  .  a  .  .  .  .  .  #  .  .          1  a  archer   6,2
             3      .  .  #  #  #  #  #  #  #  #  #  #  .  .  .        2  a  archer   7,4
             4    .  .  #  .  a  .  .  a  .  .  .  .  .  .  .          3  a  archer   7,6
             5      .  .  #  #  #  #  #  #  #  #  #  #  #  .  .        4  a  archer   4,4
             6    .  .  .  .  .  .  .  a  .  .  .  .  .  #  .
             7      .  E  #  #  #  #  #  #  #  #  #  #  #  .  .      you may build
             8    .  .  .  .  .  .  .  .  .  .  .  .  .  .  .         11  soldier   30
                                                                       3  archer    40
                                                                       4  mage      92

            you may build      what you may send
             11  soldier   30    1  minion            10 each
              3  archer    40    2  skeleton-scout     9 each
              4  mage      92    7  necromancer       19 each
                                12  skeleton          17 each
                                13  skeleton-warrior  31 each

            1 built, 1 slot filled.
            """,
            Played.Value.Composed[3]);
    }

    [Fact]
    public void Nothing_on_the_frame_depends_on_who_the_run_is_playing_against()
    {
        // The claim that nothing on the frame is forecast, asserted rather than
        // asserted about. Two runs on one seed and one content, differing only
        // in the pool their rounds are drawn from and scored against: any
        // predicted damage, any reading of whether a board will hold, any band
        // a wave might reach would have to resolve a match against that pool,
        // and would come back different. The frames are the same string.
        //
        // It is cheap for the same reason: neither run here measures its field,
        // because nothing the frame prints asks for it.
        //
        // OBSERVED: add "would deal" to the header, priced by walking the
        // sendable panel against run.Field. Every block above can be refreshed
        // by pasting the new output and goes green again; this one stays red,
        // naming two headers that differ.
        UnitTypeTable types = TheMatch.Types();
        UpgradeLadder ladder = TheMatch.Ladder(types);

        Assert.Equal(
            RoundFrame.ToText(Fresh(types, TheRun.FieldWave(types)), ladder, null),
            RoundFrame.ToText(Fresh(types, TheMatch.Wave(types)), ladder, null));
    }

    [Fact]
    public void A_part_of_the_frame_is_the_block_the_whole_frame_already_carries()
    {
        // The three words that reprint -- map, menu, costs -- draw parts of this
        // frame and not three smaller drawings beside it. Two of them are
        // carried by the whole frame character for character; the third is the
        // sendable panel alone, which the frame draws in the right-hand column
        // of a pair and so carries indented rather than verbatim.
        //
        // Which two changed with #179. The frame used to put the offering and
        // the sendable list side by side, so `menu` was carried whole and
        // `costs` was the odd one out; deleting the offering left the sendable
        // panel sharing its row with the prices, which is exactly what `costs`
        // now prints.
        //
        // OBSERVED: draw Panel.Map from BoardMap.ToText's three-argument
        // overload, which is the same grid and the same legend without the
        // panel of prices. Every block above still passes and `map` at the
        // prompt quietly stops saying what a tower costs.
        UnitTypeTable types = TheMatch.Types();
        UpgradeLadder ladder = TheMatch.Ladder(types);
        Run run = Fresh(types, TheRun.FieldWave(types));
        string whole = RoundFrame.ToText(run, ladder, null);

        Assert.Contains(
            RoundFrame.ToText(run, ladder, null, Panel.Map), whole, StringComparison.Ordinal);

        Assert.Contains(
            RoundFrame.ToText(run, ladder, null, Panel.Costs), whole, StringComparison.Ordinal);

        // And the odd one out is a column of that pair rather than a block of
        // its own: every row of it appears, and the block does not.
        //
        // OBSERVED: have Panel.Menu print the pair instead of the column. This
        // goes red on the DoesNotContain, the two spellings having become one,
        // and `menu` and `costs` stop being different words.
        Assert.DoesNotContain(
            RoundFrame.ToText(run, ladder, null, Panel.Menu), whole, StringComparison.Ordinal);

        Assert.All(
            RoundFrame.ToText(run, ladder, null, Panel.Menu).Split('\n'),
            line => Assert.Contains(line.Trim(), whole, StringComparison.Ordinal));

        Assert.Equal(
            """
            you may build      what you may send
             11  soldier   30    1  minion            10 each
              3  archer    40    2  skeleton-scout     9 each
              4  mage      92    7  necromancer       19 each
                                12  skeleton          17 each
                                13  skeleton-warrior  31 each
            """,
            RoundFrame.ToText(run, ladder, null, Panel.Costs));
    }

    /// <summary>
    /// The frames of the committed run: the one standing in front of each round
    /// with nothing composed, and the one that round's own decision composes.
    /// </summary>
    private sealed class Frames
    {
        public Frames(string[] opening, string[] composed)
        {
            Opening = opening;
            Composed = composed;
        }

        /// <summary>What the round opened on, before a word was typed.</summary>
        public string[] Opening { get; }

        /// <summary>The same round with the decision <c>content/commands.txt</c> made composed.</summary>
        public string[] Composed { get; }
    }

    /// <summary>
    /// Plays the opening rounds of the committed script, drawing the frame twice
    /// a round: once before the phase and once with it composed.
    /// </summary>
    /// <remarks>
    /// The decisions come out of the committed script rather than being written
    /// here, so the boards, the purses and the unlocks these frames draw are the
    /// ones a play of the committed run actually reaches.
    /// </remarks>
    private static Frames Play()
    {
        UnitTypeTable types = TheMatch.Types();
        UpgradeLadder ladder = TheMatch.Ladder(types);
        Run run = Fresh(types, TheRun.FieldWave(types));

        IReadOnlyList<RecordCommand> script = CommandScript.Parse(
            "commands.txt", File.ReadAllText(RepoLayout.CommandScriptFile));

        var opening = new string[Rounds];
        var composed = new string[Rounds];

        for (int index = 0; index < Rounds; index++)
        {
            BuildPhase phase = script[index].ToPhase();

            opening[index] = RoundFrame.ToText(run, ladder, null);
            composed[index] = RoundFrame.ToText(run, ladder, phase);

            run.Advance(phase);
        }

        return new Frames(opening, composed);
    }


    /// <summary>
    /// A fresh run on the committed content and the committed seed, against a
    /// canned pool sending the wave named here, and under the shape named here
    /// where the committed one is not the subject.
    /// </summary>
    private static Run Fresh(UnitTypeTable types, WaveScript field, UpgradeLadder? ladder = null) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            ladder ?? TheLadder.Committed(types),
            FieldPool.Canned(TheMatch.Layout(types), field),
            TheRun.Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize);
}
