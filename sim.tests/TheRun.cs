using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a run: a field to fight, orders to send,
/// and the two deliberately degenerate tables the arithmetic assertions need.
/// </summary>
/// <remarks>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheMatch"/> and <see cref="TheRuleset"/> do. Nothing here builds a
/// placeholder anchor schedule or a command format -- a build phase's product is
/// a defense and a wave, and that is what these hand over.
/// </remarks>
public static class TheRun
{
    /// <summary>The seed the runs in this suite are derived from.</summary>
    public const ulong Seed = 20260807UL;

    /// <summary>Where the role sits in the committed table's column layout.</summary>
    private const int RoleField = 3;

    /// <summary>Where max hp sits in the committed table's column layout.</summary>
    private const int MaxHpField = 4;

    /// <summary>Where the cost column sits in the committed table's column layout.</summary>
    private const int CostField = 15;

    /// <summary>
    /// Health no tower on this map can chew through in a match, so every unit a
    /// wave sends walks to the exit whatever the dice say.
    /// </summary>
    private const int UnkillableHp = 100000000;

    /// <summary>
    /// A price high enough that a whole wave of leakers costs more than a purse
    /// can hold, and low enough that no single order does. The refusal being
    /// aimed at is the one over the summed orders rather than the one inside the
    /// cost table.
    /// </summary>
    private const int RuinousCost = 100000000;

    /// <summary>
    /// What the ten-wave run on the committed content came to: its per-round
    /// pairs, in order, as a real run of it produced them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Written down rather than recomputed</b>, for the reason the golden
    /// trace beside <c>content/</c> is: an expected value computed by the code
    /// under test moves with it, so a lifecycle regression would move both sides
    /// of the comparison and nothing would go red. Nothing that checks these
    /// regenerates them.
    /// </para>
    /// <para>
    /// The run finishes its last wave on <see cref="HealthLeftInTheCommittedRun"/>
    /// of the ruleset's 1500, which is what makes the death flag inert across
    /// the scenario theory. If a content change ever takes that below zero, the
    /// theory's no-death row is the one that says so.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<RoundOutcome> TheCommittedRun => new[]
    {
        new RoundOutcome(324, 154),
        new RoundOutcome(336, 154),
        new RoundOutcome(384, 142),
        new RoundOutcome(293, 155),
        new RoundOutcome(300, 149),
        new RoundOutcome(311, 152),
        new RoundOutcome(395, 139),
        new RoundOutcome(379, 147),
        new RoundOutcome(386, 142),
        new RoundOutcome(411, 137),
    };

    /// <summary>What that run had left of the pool when its last wave resolved.</summary>
    public const int HealthLeftInTheCommittedRun = 29;

    /// <summary>The committed defense as one round's orders, sent at the committed wave.</summary>
    public static RoundOrders Orders(UnitTypeTable? types = null)
    {
        UnitTypeTable table = types ?? TheMatch.Types();

        return RoundOrders.Of(TheMatch.Layout(table), TheMatch.Wave(table));
    }

    /// <summary>The committed content thinned to this many towers and this many wave orders.</summary>
    public static RoundOrders Orders(UnitTypeTable types, int towers, int orders) =>
        RoundOrders.Of(Defense(types, towers), Wave(types, orders));

    /// <summary>
    /// One round at a field of ten, against a population written out here.
    /// </summary>
    /// <remarks>
    /// The seed of every pairing's match is derived from the round and the
    /// pairing rather than from who was drawn, so two calls to this that differ
    /// only in the population fight the same twenty seeds. That is what makes
    /// the difference between their answers a statement about the fold rather
    /// than about the dice.
    /// </remarks>
    public static RoundOutcome Against(UnitTypeTable types, RoundOrders orders, params RoundOrders[] pool) =>
        Against(types, orders, 10, pool);

    /// <summary>One round at a field of this many, against a population written out here.</summary>
    public static RoundOutcome Against(
        UnitTypeTable types,
        RoundOrders orders,
        int fieldSize,
        params RoundOrders[] pool) =>
        new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            FieldPool.Of(pool),
            Seed,
            waves: 1,
            fieldSize: fieldSize)
            .Advance(orders);

    /// <summary>
    /// A population of four, spread wide enough that averaging over it is not
    /// the same arithmetic as taking any one of them: the committed defense and
    /// three progressively thinner ones, against three lengths of wave.
    /// </summary>
    public static FieldPool Pool(UnitTypeTable? types = null)
    {
        UnitTypeTable table = types ?? TheMatch.Types();

        return FieldPool.Of(new[]
        {
            Orders(table, 6, 4),
            Orders(table, 4, 4),
            Orders(table, 2, 2),
            Orders(table, 1, 2),
        });
    }

    /// <summary>A fresh run on the committed content. Every scenario starts here.</summary>
    public static Run Fresh(
        int waves = Run.DefaultWaves,
        int fieldSize = Run.DefaultFieldSize,
        bool deathEndsTheRun = true,
        ulong seed = Seed)
    {
        UnitTypeTable types = TheMatch.Types();

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            Pool(types),
            seed,
            waves,
            fieldSize,
            deathEndsTheRun);
    }

    /// <summary>
    /// A run in which nothing can be killed, so every wave leaks in full and
    /// every number in the outcome is arithmetic rather than a dice roll.
    /// </summary>
    /// <remarks>
    /// The whole point is that the seed stops mattering. Each side of every
    /// pairing lets the same wave through, so the field's average is the wave's
    /// own cost and the health it costs can be written down in advance.
    /// </remarks>
    public static Run Unstoppable(
        int waves = Run.DefaultWaves,
        int fieldSize = Run.DefaultFieldSize,
        bool deathEndsTheRun = true)
    {
        UnitTypeTable types = UnkillableTypes();

        return new Run(
            TheMatch.Map(),
            TheRuleset.Committed(),
            types,
            FieldPool.Of(new[] { Orders(types) }),
            Seed,
            waves,
            fieldSize,
            deathEndsTheRun);
    }

    /// <summary>The committed table with every walking unit given health nothing can spend.</summary>
    public static UnitTypeTable UnkillableTypes() =>
        UnitTypeTable.Parse(Retable(
            (fields, role) => role == "moving"
                ? Set(fields, MaxHpField, UnkillableHp)
                : fields));

    /// <summary>
    /// The committed table with every walking unit priced beyond what a purse
    /// can hold once a wave of them leaks.
    /// </summary>
    public static UnitTypeTable RuinouslyPricedTypes() =>
        UnitTypeTable.Parse(Retable(
            (fields, role) => role == "moving"
                ? Set(Set(fields, MaxHpField, UnkillableHp), CostField, RuinousCost)
                : fields));

    /// <summary>What every unit of a wave costs to send, which is what a wave that fully leaks costs.</summary>
    public static int FullLeakCost(CostTable costs, WaveScript wave)
    {
        int total = 0;

        for (int index = 0; index < wave.Count; index++)
        {
            total += costs.PriceOf(Purchase.Unit(wave.Orders[index].TypeId), wave.Orders[index].Count);
        }

        return total;
    }

    /// <summary>The committed defense with only its first few towers left standing.</summary>
    private static TowerLayout Defense(UnitTypeTable types, int towers) =>
        TowerLayout.Parse(Head(TheMatch.DataRows(RepoLayout.DefenseFile), towers), types);

    /// <summary>The committed wave with only its first few orders sent.</summary>
    private static WaveScript Wave(UnitTypeTable types, int orders) =>
        WaveScript.Parse(Head(TheMatch.DataRows(RepoLayout.WaveFile), orders), types);

    private static string Head(string[] rows, int count)
    {
        Assert.InRange(count, 1, rows.Length);

        return string.Join("\n", rows.Take(count));
    }

    /// <summary>
    /// The committed unit table with every row rewritten by a rule.
    /// </summary>
    /// <remarks>
    /// The edit rewrites parsed fields rather than replacing a literal run of
    /// characters, for the reason <see cref="TheMatch.RetunedUnitsText"/> spells
    /// out: a literal is one spelling of one row at one moment, and a respacing
    /// turns the replacement into a no-op that leaves every caller comparing a
    /// table against itself.
    /// </remarks>
    /// <remarks>
    /// OBSERVED: make the rewrite a no-op -- assign <c>fields</c> straight to
    /// <c>rewritten</c> instead of calling the rule. The assertion below goes
    /// red saying "The rewrite changed no row of the committed unit table at
    /// all", and takes six tests in <c>RunTests</c> with it. Without the guard
    /// the same no-op leaves every one of them green, fought against the
    /// committed table under two names.
    /// </remarks>
    private static string Retable(Func<string[], string, string[]> rewrite)
    {
        string[] lines = File.ReadAllText(RepoLayout.UnitsFile).Split('\n');
        int edited = 0;

        for (int index = 0; index < lines.Length; index++)
        {
            string[] fields = lines[index].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 0 || fields[0] != "unit")
            {
                continue;
            }

            string[] rewritten = rewrite(fields, fields[RoleField]);

            if (string.Join(" ", rewritten) == string.Join(" ", fields))
            {
                continue;
            }

            lines[index] = string.Join("   ", rewritten);
            edited++;
        }

        Assert.True(edited > 0, "The rewrite changed no row of the committed unit table at all.");

        return string.Join("\n", lines);
    }

    private static string[] Set(string[] fields, int field, int value)
    {
        var copy = (string[])fields.Clone();
        copy[field] = value.ToString(CultureInfo.InvariantCulture);

        return copy;
    }
}
