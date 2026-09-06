using System.Linq;

namespace Sim.Tests;

/// <summary>
/// A kill paying the defender: what it pays, which row's number it pays, what a
/// leak pays instead, which routes a paid-for body can have arrived by, what the
/// table refuses, and where the money lands.
/// </summary>
/// <remarks>
/// <para>
/// <b>The Grave Robber is asserted against <c>content/units.txt</c> and every
/// other row here is a fixture.</b> Twelve gold on that row is content and is
/// signed in <c>docs/roster.md</c>; the ids, labels and numbers below mean
/// nothing outside this file and exist because the committed roster authors
/// exactly one bounty, on a body nothing transforms into and nothing raises.
/// </para>
/// <para>
/// The corridor and the turret are RaiseTests' shape and for its reason: five
/// hexes, every cell covered, and a turret that winds up and recovers in nothing
/// so one attack fits in a handful of ticks.
/// </para>
/// </remarks>
public class BountyTests
{
    private const string AShortCorridor = """
        S###E
        .....

        aaaaa
        aaaaa
        """;

    /// <summary>
    /// Four walking rows and two turrets. <c>goon</c> turns into <c>husk</c> and
    /// <c>raiser</c> puts <c>shade</c> down, so the three routes a body can
    /// arrive by are all here -- and the four bounties are all different, so a
    /// payment read off the wrong row is a different number rather than the same
    /// one.
    /// </summary>
    private const string TheFixtures = """
        layout 6
        unit  1 goon   moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 3    none 0  1
        unit  2 raiser moving 1500 10 0    0  0 0 0    0    none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 none 4    5  2
        unit  3 husk   moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 none none 0  3
        unit  4 shade  moving 400  10 0    0  0 0 0    0    none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 none none 0  4
        unit 11 cannon placed 0    0  4000 30 0 0 600  600  hitscan 0 0 40 pierce none     0 0 1 none none none 0 none 0 0 none none 0  0
        unit 12 quiet  placed 0    0  4000 30 0 0 1    1    hitscan 0 0 40 pierce none     0 0 1 none none none 0 none 0 0 none none 0  0
        """;

    /// <summary>The turret that kills whatever it hits, and the one that cannot kill anything.</summary>
    private const int Cannon = 11;

    private const int Quiet = 12;

    private const ulong Seed = 20260906UL;

    /// <summary>The Grave Robber, and what docs/roster.md signs it pays.</summary>
    private const int GraveRobber = 49;

    private const int TwelveGold = 12;

    [Fact]
    public void Killing_a_grave_robber_pays_the_defender_twelve_and_leaking_one_pays_nothing()
    {
        // The signed mechanic, on the committed roster and against the committed
        // defense. Four hundred gold of Grave Robbers walk into four archers and
        // two mages; the ones that die pay twelve each and the ones that reach
        // the exit pay nothing, which is the whole of the rule in one match.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(TwelveGold, types.ById(GraveRobber).Bounty);

        Match match = MetByTheCommittedDefense(types);
        MatchResult result = match.Resolve();

        // Both outcomes happen in this match, which is what makes it an
        // assertion about the difference between them rather than about one of
        // them.
        Assert.Equal(13, result.Leaked);
        Assert.Equal(3, match.Killed);
        Assert.Equal(3 * TwelveGold, match.Bounty);
        Assert.Equal(match.Bounty, result.Bounty);
    }

    [Fact]
    public void A_grave_robber_nothing_can_kill_pays_nothing_at_all()
    {
        // The other half of the same sentence, isolated. The committed defense,
        // the committed column, and health no tower on this map can chew
        // through -- so every body reaches the exit and the defense is paid
        // nought. A leak is charged against health at the row's cost and pays no
        // gold, and the two outcomes are exclusive.
        //
        // OBSERVED: pay the bounty in Match.MoveCreeps where a body leaks as
        // well as in Match.Damage. This goes red, 192 against 0 -- sixteen
        // bodies at twelve gold -- and a defense would be paid for being walked
        // past.
        UnitTypeTable types = TheRun.UnkillableTypes();

        Assert.Equal(TwelveGold, types.ById(GraveRobber).Bounty);

        MatchResult result = MetByTheCommittedDefense(types).Resolve();

        Assert.Equal(16, result.Leaked);
        Assert.Equal(0, result.Bounty);
    }

    [Fact]
    public void Nothing_else_on_the_committed_roster_pays_for_being_killed()
    {
        // One row, and the assertion is over the whole table rather than over
        // the row -- so a bounty authored on anything else is a red test rather
        // than a number nobody looked at.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            new[] { GraveRobber },
            types.Types.Where(row => row.Bounty > 0).Select(row => row.Id).ToArray());
    }

    [Fact]
    public void A_body_that_changed_row_pays_what_the_row_it_became_pays()
    {
        // The decision this ticket had to take, and the one the committed roster
        // cannot show: the payment is read off the body standing there when it
        // dies, not off the order that sent it. A goon is sent, turns into a
        // husk on the first damage that reaches its health, and is killed -- so
        // what it pays is the husk's three and never the goon's one.
        //
        // OBSERVED: pay `_wave.Orders[creep.OrderIndex].Type.Bounty` instead of
        // the body's own row in Match.Damage. This goes red, 2 against 6: the
        // rule reads as "what was sent" and a pair is paid for the half of it
        // that no longer exists.
        Match match = Built(TheFixtures, "order 0 1 2 0", Cannon);

        match.Resolve();

        Assert.Equal(2, match.Killed);
        Assert.Equal(6, match.Bounty);
    }

    [Fact]
    public void A_body_a_spawner_raised_pays_although_no_order_ever_sent_one()
    {
        // The second route, and the one that forces the rule above: a raised
        // body's order index points at the body that raised it, so the order
        // cannot say what the body is at all. Two raisers walk in, each putting
        // a shade down every five ticks; the cannon kills the raisers and then
        // works through what they left behind.
        //
        // OBSERVED: pay `_wave.Orders[creep.OrderIndex].Type.Bounty` instead of
        // the body's own row in Match.Damage. This goes red on the payments
        // themselves -- every shade is priced at its raiser's two, so no
        // payment of four is ever made and the total falls by half of what the
        // raised bodies were worth.
        Match match = Built(TheFixtures, "order 0 2 2 0", Cannon);
        var log = new TheMatch.EventLog();

        while (!match.IsFinished)
        {
            match.Advance(1, log);
        }

        int[] payments = log.IndicesOf("paid").Select(index => log.Amounts[index]).ToArray();

        Assert.True(log.CountOf("raised") > 0, "nothing was raised, so this measures the wrong route");

        // Both kinds of body were killed and each paid its own row's number:
        // two for a raiser somebody sent, four for a shade nobody did. Those two
        // numbers and no others, which is the assertion that the payment is read
        // off the body rather than off the order -- reading it off the order
        // would pay two for every one of them.
        Assert.True(payments.Count(gold => gold == 2) > 0, "no raiser was killed");
        Assert.True(payments.Count(gold => gold == 4) > 0, "no raised body was killed");
        Assert.Equal(new[] { 2, 4 }, payments.Distinct().OrderBy(gold => gold).ToArray());

        // And the total the match carries is those payments and nothing else.
        Assert.Equal(payments.Sum(), match.Bounty);
        Assert.Equal(payments.Length, match.Killed);
    }

    [Fact]
    public void A_raised_body_that_reaches_the_exit_pays_nothing_either()
    {
        // And the two halves crossed: a body nobody sent, walking past a turret
        // that cannot kill it. The route a body arrived by does not change what
        // reaching the exit is worth, which is nothing.
        Match match = Built(TheFixtures, "order 0 2 2 0", Quiet);
        var log = new TheMatch.EventLog();

        while (!match.IsFinished)
        {
            match.Advance(1, log);
        }

        Assert.True(log.CountOf("raised") > 0, "nothing was raised");
        Assert.Equal(0, match.Killed);
        Assert.Equal(0, match.Bounty);
    }

    [Fact]
    public void The_payment_reaches_the_event_stream_once_per_paying_kill()
    {
        // Decorative in the ADR-0008 sense: an entity id and a number read
        // straight off the emitter's row, no position, and a subscribed match
        // produces the same rolling hash as a silent one.
        Match watched = Built(TheFixtures, "order 0 1 2 0", Cannon);
        Match silent = Built(TheFixtures, "order 0 1 2 0", Cannon);
        var log = new TheMatch.EventLog();

        while (!watched.IsFinished)
        {
            watched.Advance(1, log);
        }

        silent.Resolve();

        Assert.Equal(silent.StateHash, watched.StateHash);

        // One event per paying kill, and each carries what that body's row pays
        // rather than a running total.
        int[] paid = log.IndicesOf("paid");

        Assert.Equal(watched.Killed, paid.Length);

        foreach (int index in paid)
        {
            Assert.Equal(3, log.Amounts[index]);
        }

        // And the bodies are the ones that died, by id.
        Assert.Equal(
            log.IndicesOf("died").Select(index => log.Subjects[index]).ToArray(),
            paid.Select(index => log.Subjects[index]).ToArray());
    }

    [Fact]
    public void A_row_nothing_can_ever_kill_may_not_pay_for_being_killed()
    {
        // The rule every unread column in content/units.txt is refused by: a
        // number read by nothing that would still move the content hash.
        // Nothing that stands is ever damaged here, and a row with no pool
        // cannot be damaged at all, so neither has a kill for the payment to be
        // made on.
        ContentException standing = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("bounty fixtures", PlantedText.Replace(OneRowThatPays, PaysTwo, StandsAndPaysTwo)));

        Assert.Contains("stands where it was put and pays a bounty", standing.Message, StringComparison.Ordinal);

        ContentException poolless = Assert.Throws<ContentException>(
            () => UnitTypeTable.Parse("bounty fixtures", PlantedText.Replace(OneRowThatPays, PaysTwo, NoPoolAndPaysTwo)));

        Assert.Contains("has no health pool and pays a bounty", poolless.Message, StringComparison.Ordinal);

        // And the row as written, which is the same line with the two things
        // wrong with it put right -- so what these refusals catch is the column
        // and not the rest of the row.
        Assert.Equal(2, UnitTypeTable.Parse("bounty fixtures", OneRowThatPays).ById(1).Bounty);
    }

    [Fact]
    public void What_the_kills_paid_is_the_fourth_line_of_what_a_wave_pays_a_purse()
    {
        // Where the money lands. A round meets K opponents twice over, and only
        // one of the two directions has this round's own towers standing in it
        // -- so what this round's defense was paid for killing is what reaches
        // this round's purse, averaged over the field exactly as leak cost is.
        //
        // OBSERVED: leave the bounty out of Purse.Closed's closing balance. The
        // last assertion goes red by the bounty: the payment still itemises it
        // and the purse no longer agrees with the itemisation.
        Ruleset rules = TheRuleset.Committed();
        WavePayment paid = Purse.Holding(333).CloseWave(rules, 45, 60);

        Assert.Equal(333, paid.Opening);
        Assert.Equal(34, paid.Interest);
        Assert.Equal(168, paid.IncomeBase);
        Assert.Equal(11, paid.Bonus);
        Assert.Equal(60, paid.Bounty);
        Assert.Equal(273, paid.Total);
        Assert.Equal(paid.Opening + paid.Total, paid.Purse.Gold);

        // Paid whole rather than at a rate. What a kill pays is authored on the
        // row that died, so there is no share of it for a ruleset to take and
        // nothing to truncate.
        Assert.Equal(120, Purse.Empty.CloseWave(rules, 0, 120).Bounty);
    }

    [Fact]
    public void A_round_is_credited_what_its_own_defense_was_paid_for_killing()
    {
        // End to end, through a run: an opponent who sends Grave Robbers, a
        // round that builds a wall to meet them, and a purse that closes on what
        // the wall killed. The first round stands nothing, so it is paid
        // nothing; the rounds after it stand a wall and are paid.
        //
        // OBSERVED: fold the Attacking side's bounty rather than the Defending
        // side's in Run.Play. The first round goes red -- a round with an empty
        // board is paid for kills its opponents' towers made.
        UnitTypeTable types = TheMatch.Types();
        Run run = AgainstGraveRobbers(types);

        RoundReport first = run.Advance(TheBuild.BuyingNothing());

        Assert.Equal(0, first.Outcome.BountyEarned);
        Assert.Equal(0, first.Payment.Bounty);

        RoundReport paid = first;

        for (int round = 0; round < 5; round++)
        {
            paid = run.Advance(TheBuild.Fortifying(run));
        }

        Assert.True(
            paid.Payment.Bounty > 0,
            "the wall killed nothing, so this measures no payment at all");

        // The vector carries it, which is what keeps a run's purse arithmetic
        // over what was stored rather than a second play.
        Assert.Equal(paid.Outcome.BountyEarned, paid.Payment.Bounty);
        Assert.Equal(0, paid.Payment.Bounty % TwelveGold);

        Assert.Equal(
            paid.Payment.Opening
                + paid.Payment.Interest
                + paid.Payment.IncomeBase
                + paid.Payment.Bonus
                + paid.Payment.Bounty,
            run.Purse.Gold);
    }

    /// <summary>
    /// One walking row that pays, and the two re-authorings of it the refusals
    /// need: the same row standing where it was put, and the same row with no
    /// health pool to be killed through.
    /// </summary>
    private const string OneRowThatPays = """
        layout 6
        unit  1 payer  moving 400 10 0    0  0 0 0   0   none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 none none 0  2
        unit 11 cannon placed 0   0  4000 30 0 0 600 600 hitscan 0 0 40 pierce none     0 0 1 none none none 0 none 0 0 none none 0  0
        """;

    private const string PaysTwo =
        "unit  1 payer  moving 400 10 0    0  0 0 0   0   none    0 4 12 none   armoured 0 0 1 none none none 0 none 0 0 none none 0  2";

    private const string StandsAndPaysTwo =
        "unit  1 payer  placed 0   0  4000 30 0 0 600 600 hitscan 0 0 40 pierce none     0 0 1 none none none 0 none 0 0 none none 0  2";

    private const string NoPoolAndPaysTwo =
        "unit  1 payer  moving 0   10 0    0  0 0 0   0   none    0 4 12 none   none     0 0 1 none none none 0 none 0 0 none none 0  2";

    /// <summary>
    /// Four hundred gold of Grave Robbers, which is the column the roster's own
    /// return band is measured over.
    /// </summary>
    private static WaveScript AColumnOfGraveRobbers(UnitTypeTable? types = null)
    {
        UnitTypeTable table = types ?? TheMatch.Types();

        return WaveScript.Parse(
            "grave robbers",
            "order 0 " + GraveRobber + " " + (400 / table.ById(GraveRobber).Cost) + " 0",
            table);
    }

    /// <summary>That column walking into the committed defense.</summary>
    private static Match MetByTheCommittedDefense(UnitTypeTable types) =>
        new(
            TheMatch.Map(),
            TheRuleset.Committed(),
            TheMatch.Layout(types),
            AColumnOfGraveRobbers(types),
            TheMatch.Seed);

    /// <summary>
    /// A run whose every opponent stands the committed defense and sends a
    /// column of Grave Robbers, so what the run's own wall kills is what pays
    /// it.
    /// </summary>
    private static Run AgainstGraveRobbers(UnitTypeTable types) =>
        new(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            FieldPool.Of(new[]
            {
                RoundOrders.Of(TheMatch.Layout(types), AColumnOfGraveRobbers(types)),
            }),
            TheRun.Seed,
            waves: 10,
            fieldSize: 2,
            deathEndsTheRun: false);

    /// <summary>
    /// The corridor, one turret on it, and a wave, out of one roster. Shaped
    /// exactly as <c>RaiseTests.Built</c> is, and for the same reason.
    /// </summary>
    private static Match Built(string units, string wave, int towerType)
    {
        UnitTypeTable types = UnitTypeTable.Parse("bounty fixtures", units);

        return new Match(
            HexMap.Parse("bounty map", AShortCorridor),
            TheRuleset.Committed(),
            TowerLayout.Parse("bounty defense", "tower " + towerType + " 2 1", types),
            WaveScript.Parse("bounty wave", wave, types),
            Seed);
    }
}
