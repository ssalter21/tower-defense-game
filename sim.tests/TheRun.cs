using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a run: a field to fight, orders to send,
/// and the two deliberately degenerate tables the arithmetic assertions need.
/// </summary>
/// <remarks>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheMatch"/>, <see cref="TheRuleset"/> and <see cref="TheSchedule"/>
/// do. Nothing here builds a command format -- a build phase's product is a
/// defense and a wave, and that is what these hand over.
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
    /// <b>This is a run that built and then shopped</b> -- see
    /// <see cref="TheBuild.Fortifying"/>. Every round of it added a tower to the
    /// wall while there was one left to add, and spent what remained of the
    /// purse on the roster's first creep. So the second column falls by more
    /// than half over the rounds the wall is going up -- the wall is working --
    /// while the first climbs every single round without exception.
    /// </para>
    /// <para>
    /// <b>That first column climbing is #207 showing up in the data.</b> A creep
    /// is bought once and attacks every round after, so this run's wave grows
    /// every round and what it gets past a thinning field grows with it.
    /// <b>The monotonicity is a property of this run and not a rule.</b> What
    /// the rules make monotone is the wave; leak cost dealt is what a match did
    /// with it, and the committed run in <c>content/</c> has a round where a
    /// bigger wave deals a point less. Nothing here may be read as saying that
    /// cannot happen.
    /// </para>
    /// <para>
    /// <b>This player runs out of health, and that is the point.</b> #179 moved
    /// the pool from 1500 to 800 and this run reaches zero in its fourth round,
    /// so the death flag is live here rather than inert. What it does is
    /// truncate: <see cref="TheCommittedRunWithoutDeath"/> is this vector with
    /// six more rounds after it and every shared round identical to the gold.
    /// That is a stronger statement of what the scenario theory is for than the
    /// old one -- the flag used to be provably not-a-second-lifecycle only
    /// because nothing ever reached it.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<RoundOutcome> TheCommittedRun => new[]
    {
        new RoundOutcome(26, 239),
        new RoundOutcome(108, 229),
        new RoundOutcome(281, 184),
        new RoundOutcome(376, 200),
    };

    /// <summary>
    /// The same player over the same content with the death flag off: the four
    /// rounds above, unchanged, and the six the pool did not survive.
    /// </summary>
    /// <remarks>
    /// Written down for the reason the vector above is, and held against it
    /// round for round. A flag that changed a number rather than only stopping
    /// the loop would show up as a disagreement in the first four.
    /// </remarks>
    public static IReadOnlyList<RoundOutcome> TheCommittedRunWithoutDeath => new[]
    {
        new RoundOutcome(26, 239),
        new RoundOutcome(108, 229),
        new RoundOutcome(281, 184),
        new RoundOutcome(376, 200),
        new RoundOutcome(577, 82),
        new RoundOutcome(788, 25),
        new RoundOutcome(1178, 22),
        new RoundOutcome(1653, 25),
        new RoundOutcome(2218, 23),
        new RoundOutcome(2927, 22),
    };

    /// <summary>What that run had left of the pool when it stopped: none of it.</summary>
    public const int HealthLeftInTheCommittedRun = 0;

    /// <summary>
    /// The wave the committed canned field sends: <c>content/field.txt</c>,
    /// which is what every run verb of the command line reads for
    /// <c>--field</c>.
    /// </summary>
    /// <remarks>
    /// <b>Not <c>content/wave.txt</c>, and the difference is a whole different
    /// game.</b> The wave file is one authored match released over fourteen
    /// hundred ticks and costing several times what any round's purse composes;
    /// this one is a build phase's output, everything on tick zero and a
    /// round's worth of gold. <c>content/field.txt</c>'s own header carries the
    /// measurements, and <c>docs/adr/0040</c> carries the decision.
    /// </remarks>
    public static WaveScript FieldWave(UnitTypeTable types) =>
        WaveScript.Parse("field", File.ReadAllText(RepoLayout.FieldFile), types);

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
    /// <para>
    /// The round is the run's first, shopped for out of the opening purse, and
    /// what comes back is the whole of it -- so an assertion about what the
    /// round dealt can be held against the wave the round actually bought.
    /// </para>
    /// <para>
    /// The seed of every pairing's match is derived from the round and the
    /// pairing rather than from who was drawn, so two calls to this that differ
    /// only in the population fight the same twenty seeds. That is what makes
    /// the difference between their answers a statement about the fold rather
    /// than about the dice.
    /// </para>
    /// </remarks>
    public static RoundReport Against(UnitTypeTable types, params RoundOrders[] pool) =>
        Against(types, 10, pool);

    /// <summary>One round at a field of this many, against a population written out here.</summary>
    /// <remarks>
    /// The purse is the one <see cref="AttackingPurse"/> names rather than the
    /// ruleset's opening hundred. A round has to get something past a whole
    /// defense for its offense score to be a number worth folding, and a
    /// hundred gold of the cheapest creep on the roster does not: it scores
    /// zero against every field size, which is an oracle that cannot tell an
    /// average from a maximum.
    /// </remarks>
    public static RoundReport Against(UnitTypeTable types, int fieldSize, params RoundOrders[] pool)
    {
        Ruleset rules = Ruleset.Parse(PlantedText.Replace(
            TheRuleset.CommittedText(),
            "purse         100",
            "purse       " + AttackingPurse.ToString(CultureInfo.InvariantCulture)));

        var run = new Run(
            TheMatch.Map(),
            rules,
            types,
            TheLadder.Committed(types),
            FieldPool.Of(pool),
            Seed,
            waves: 1,
            fieldSize: fieldSize);

        return run.Advance(TheBuild.Shopping(run));
    }

    /// <summary>
    /// What a round of <see cref="Against"/> opens holding: enough that the
    /// wave it buys reaches the field rather than dying on the way in.
    /// </summary>
    public const int AttackingPurse = 2000;

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

    /// <summary>
    /// A fresh run on the committed content. Every scenario starts here.
    /// </summary>
    /// <remarks>
    /// It opens on an empty board, as every run does. A scenario that wants
    /// something standing builds it, through the build phases it advances.
    /// </remarks>
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
            TheLadder.Committed(types),
            Pool(types),
            seed,
            waves,
            fieldSize,
            deathEndsTheRun);
    }

    /// <summary>
    /// <see cref="Fresh"/> with an opening purse deep enough to buy waves the
    /// field can be scored against.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The committed hundred buys five bodies, and five bodies get past nobody
    /// in a field drawn from <see cref="Pool"/>: every round of such a run deals
    /// nothing, so what it earned for its offense is a column of zeroes and an
    /// assertion over that column is an assertion about nothing. Opening on more
    /// than a wave's income is what buys a wave big enough to get something
    /// past, so that the three lines a purse moves on are three real numbers and
    /// a fold over them has something to be wrong about.
    /// </para>
    /// <para>
    /// Death does not end it, because it builds nothing and a run standing
    /// behind nothing runs out of health part-way through. Health never pays
    /// the purse anything, so the ten rounds this reaches are the ten a wealthy
    /// run's economy is made of either way -- and what would otherwise end it is
    /// the wall it never bought rather than the money it is about.
    /// </para>
    /// </remarks>
    public static Run Wealthy(int purse)
    {
        UnitTypeTable types = TheMatch.Types();

        Ruleset rules = Ruleset.Parse(PlantedText.Replace(
            TheRuleset.CommittedText(),
            "purse         100",
            "purse       " + purse.ToString(CultureInfo.InvariantCulture)));

        return new Run(
            TheMatch.Map(),
            rules,
            types,
            TheLadder.Committed(types),
            Pool(types),
            Seed,
            Run.DefaultWaves,
            Run.DefaultFieldSize,
            deathEndsTheRun: false);
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
            TheLadder.Committed(types),
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
