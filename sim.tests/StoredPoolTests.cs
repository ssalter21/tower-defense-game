using System.Globalization;
using System.Linq;

namespace Sim.Tests;

/// <summary>
/// A pool with rounds stored in it: who a stage's field is drawn from, what
/// happens where the stage is thinner than the field, and what happens where it
/// is empty.
/// </summary>
/// <remarks>
/// <para>
/// <b>The last of these is the one that matters most.</b> Every run this
/// repository has ever recorded was played against a pool with nothing stored
/// in it, so a draw that moved for such a pool would retire the golden run, the
/// committed sweep and every stored command stream at once. What pins that is
/// not a claim about the arithmetic but two runs held against each other: one
/// pool, one seed, and a stage storing nobody.
/// </para>
/// <para>
/// <b>Each of these was watched failing under a deliberately wrong input</b>,
/// written above it so the observation can be repeated.
/// </para>
/// </remarks>
public class StoredPoolTests
{
    /// <summary>How many rounds these runs play. Enough to reach a stage nobody stored.</summary>
    private const int Waves = 3;

    [Fact]
    public void A_stage_wider_than_the_field_is_met_without_meeting_anybody_twice()
    {
        // A field of ten out of a stage of twelve is ten different opponents. A
        // draw with replacement would meet somebody twice about a third of the
        // time, which is a run scored against nine opponents and one of them
        // counted double.
        //
        // OBSERVED: draw every slot with replacement -- index the bag at
        // NextBelow(stored) without the swap. The distinctness assertion goes
        // red on this seed at nine distinct out of ten.
        UnitTypeTable types = TheMatch.Types();
        FieldDraw field = FirstRound(types, Stored(types, 12));

        Assert.Equal(Run.DefaultFieldSize, field.Drawn.Count);
        Assert.Equal(0, field.Canned);
        Assert.Equal(Run.DefaultFieldSize, field.Drawn.Distinct().Count());
        Assert.All(field.Drawn, drawn => Assert.InRange(drawn, 0, 11));
    }

    [Fact]
    public void The_same_seed_meets_the_same_ten_and_another_seed_does_not()
    {
        // What makes a replayed run the run it was: the field is derived from
        // the run's seed and the stage, so playing the same decisions again
        // meets the same opponents in the same slots.
        //
        // OBSERVED: seed the draw from the pool's size instead of from
        // FieldSeed(round). The first assertion still passes and the second
        // goes red, because two seeds then draw one field.
        UnitTypeTable types = TheMatch.Types();

        Assert.Equal(
            FirstRound(types, Stored(types, 12)).Drawn,
            FirstRound(types, Stored(types, 12)).Drawn);

        Assert.NotEqual(
            FirstRound(types, Stored(types, 12)).Drawn,
            FirstRound(types, Stored(types, 12), TheRun.Seed + 1).Drawn);
    }

    [Fact]
    public void A_stage_thinner_than_the_field_is_topped_up_and_the_round_says_how_many()
    {
        // A pool has to resolve a run whatever it holds, so a stage of three
        // against a field of ten is three opponents and seven of the canned
        // field rather than a run that refuses. What it is not is a field of
        // three: the width is K and the outcome is an average over K.
        //
        // OBSERVED: fill the remaining slots by drawing from the stored members
        // again. The canned count goes red at 0 against 7, and a thin pool
        // becomes three opponents counted three or four times each.
        UnitTypeTable types = TheMatch.Types();
        FieldDraw field = FirstRound(types, Stored(types, 3));

        Assert.Equal(Run.DefaultFieldSize, field.Drawn.Count);
        Assert.Equal(Run.DefaultFieldSize - 3, field.Canned);
        Assert.Equal(new[] { 0, 1, 2 }, field.Drawn.Take(3).OrderBy(drawn => drawn));
        Assert.All(field.Drawn.Skip(3), drawn => Assert.Equal(FieldDraw.StoodIn, drawn));
    }

    [Fact]
    public void A_stage_nobody_stored_a_round_at_is_the_field_it_always_was()
    {
        // The claim the whole change rests on. A pool with nothing stored at a
        // stage draws that stage exactly as it did before there were folders --
        // same stream, same members, same numbers -- so content/run-outcome.txt
        // and the committed sweep are runs against a pool that happens to store
        // nobody rather than runs under an older rule.
        //
        // OBSERVED: fill the top-up slots from the stored members rather than
        // from the stand-in. This goes red on the first assertion, where a
        // stage storing nobody has nobody to be topped up from -- and the
        // stand-in that used to fill those slots is never reached.
        UnitTypeTable types = TheMatch.Types();
        FieldPool canned = TheRun.Pool(types);

        IReadOnlyList<RoundOutcome> without = Played(types, canned);

        Assert.Equal(without, Played(types, canned.Storing(new RoundOrders[0][])));
        Assert.Equal(without, Played(types, canned.Storing(new[] { new RoundOrders[0] })));

        // And a pool storing rounds at the third stage alone leaves the first
        // two exactly where they were: a stage is drawn from its own population
        // and from nobody else's.
        IReadOnlyList<RoundOutcome> late = Played(
            types,
            canned.Storing(new[] { new RoundOrders[0], new RoundOrders[0], Members(types, 4) }));

        Assert.Equal(without.Take(2), late.Take(2));
        Assert.NotEqual(without[2], late[2]);
    }

    /// <summary>The field the first round of a run against this pool was drawn.</summary>
    private static FieldDraw FirstRound(UnitTypeTable types, FieldPool pool, ulong seed = TheRun.Seed) =>
        Against(types, pool, seed).Advance(TheBuild.BuyingNothing()).Field;

    /// <summary>What every round of a run against this pool came to.</summary>
    private static IReadOnlyList<RoundOutcome> Played(UnitTypeTable types, FieldPool pool)
    {
        Run run = Against(types, pool);

        for (int round = 0; round < Waves; round++)
        {
            run.Advance(TheBuild.BuyingNothing());
        }

        return run.Outcome.Rounds;
    }

    /// <summary>
    /// A run that builds nothing and sends nothing, so that what moves between
    /// two of these is the field and only the field.
    /// </summary>
    private static Run Against(UnitTypeTable types, FieldPool pool, ulong seed = TheRun.Seed) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            TheLadder.Committed(types),
            pool,
            seed,
            Waves,
            Run.DefaultFieldSize,
            deathEndsTheRun: false);

    /// <summary>The canned pool with this many rounds stored at every stage.</summary>
    private static FieldPool Stored(UnitTypeTable types, int members)
    {
        var stages = new IReadOnlyList<RoundOrders>[Waves];

        for (int stage = 0; stage < stages.Length; stage++)
        {
            stages[stage] = Members(types, members);
        }

        return TheRun.Pool(types).Storing(stages);
    }

    /// <summary>
    /// This many members, each a different wall behind a different wave, so
    /// that meeting one twice is a number a round can be wrong about.
    /// </summary>
    private static RoundOrders[] Members(UnitTypeTable types, int members)
    {
        var stored = new RoundOrders[members];

        for (int index = 0; index < stored.Length; index++)
        {
            stored[index] = TheRun.Orders(types, (index % 6) + 1, (index % 4) + 1);
        }

        return stored;
    }
}
