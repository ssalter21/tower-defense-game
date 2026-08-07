namespace Sim.Tests;

/// <summary>
/// The committed content arranged as a sweep, small enough that a suite can
/// play several of them.
/// </summary>
/// <remarks>
/// <para>
/// The tests open the files and hand the simulation text, exactly as
/// <see cref="TheRun"/> and <see cref="TheMatch"/> do. The sweep never sees a
/// path.
/// </para>
/// <para>
/// <b>The shape here is deliberately small and it is not the committed
/// shape.</b> A gate that played the sweep at its real width would spend half a
/// minute proving arithmetic, so the runs are short and the field is thin --
/// and the committed report is checked once, as an artefact, by
/// <see cref="GoldenSweepTests"/>.
/// </para>
/// </remarks>
public static class TheSweep
{
    /// <summary>The seed every sweep in this suite derives its runs from.</summary>
    public const ulong Seed = 20260807UL;

    /// <summary>How many waves a run in this suite lasts.</summary>
    public const int Waves = 4;

    /// <summary>How many opponents a round in this suite is resolved against.</summary>
    public const int FieldSize = 2;

    /// <summary>How many seeds each creep in this suite is played on.</summary>
    public const int Runs = 6;

    /// <summary>How many creeps of the roster this suite scores, unless it is asking about the bound.</summary>
    public const int Creeps = 2;

    /// <summary>
    /// A roster that walks nowhere: the two towers the committed defense stands
    /// and nothing that could be sent at one.
    /// </summary>
    private const string TowersOnly = """
        layout 2
        unit 3 bolt placed 0 0 3200 6 3 2 90 150 hitscan 0 0 40 pierce none 0
        unit 4 mortar placed 0 0 4600 18 7 5 210 340 projectile 11 0 90 impact none 0
        """;


    /// <summary>Where the cost column sits in the committed table's column layout.</summary>
    private const int CostField = 15;

    /// <summary>
    /// A sweep over the committed content, against the canned field the harness
    /// ships with.
    /// </summary>
    /// <remarks>
    /// Every argument is defaulted to this suite's small shape and every one of
    /// them is overridable, because the tests below are almost entirely about
    /// one of them at a time being different.
    /// </remarks>
    public static SweepPlan Plan(
        UnitTypeTable? types = null,
        Ruleset? rules = null,
        AnchorSchedule? schedule = null,
        FieldPool? field = null,
        ulong seed = Seed,
        int runs = Runs,
        int waves = Waves,
        int fieldSize = FieldSize,
        bool deathEndsTheRun = false,
        int ordinaryOptionsPerRound = SweepPlan.AsAuthored,
        int gameChangersPerAnchor = SweepPlan.AsAuthored,
        int freeSnapshotsPerRun = SweepPlan.AsAuthored,
        int snapshotPriceSauce = SweepPlan.AsAuthored,
        int mostCreeps = Creeps)
    {
        UnitTypeTable table = types ?? TheMatch.Types();
        TowerLayout defense = TheMatch.Layout(table);

        return new SweepPlan(
            TheMatch.Map(),
            rules ?? TheRuleset.Committed(),
            table,
            schedule ?? TheSchedule.Committed(table),
            defense,
            field ?? Field(table),
            seed,
            runs,
            waves,
            fieldSize,
            deathEndsTheRun,
            ordinaryOptionsPerRound,
            gameChangersPerAnchor,
            freeSnapshotsPerRun,
            snapshotPriceSauce,
            mostCreeps);
    }

    /// <summary>
    /// The canned field the harness ships with: the committed defense standing
    /// behind <c>content/field.txt</c>, drawn with replacement.
    /// </summary>
    public static FieldPool Field(UnitTypeTable types) =>
        FieldPool.Of(new[] { RoundOrders.Of(TheMatch.Layout(types), Wave(types)) });

    /// <summary>
    /// A field that sends the skeleton's authored match instead: three hundred
    /// and eighty sauce a round, which is more than any run's purse can answer
    /// and is therefore what kills one.
    /// </summary>
    public static FieldPool LethalField(UnitTypeTable types) =>
        FieldPool.Of(new[] { RoundOrders.Of(TheMatch.Layout(types), TheMatch.Wave(types)) });

    /// <summary>
    /// The committed rules on a pool one round of that field spends. What death
    /// ending a run does is then visible inside the four waves this suite plays
    /// rather than only across ten.
    /// </summary>
    public static Ruleset ThinHealth() =>
        Ruleset.Parse(TheRuleset.Replace(TheRuleset.CommittedText(), "health       1500", "health        200"));

    /// <summary>The wave the canned field sends.</summary>
    public static WaveScript Wave(UnitTypeTable types) =>
        WaveScript.Parse("field", File.ReadAllText(RepoLayout.FieldFile), types);

    /// <summary>
    /// A plan whose roster is towers alone: a schedule loaded against the
    /// committed roster, and every other part of the plan built from one that
    /// walks nowhere.
    /// </summary>
    /// <remarks>
    /// The two tables were never checked against each other, which is the only
    /// way to reach a roster with nothing that walks -- an anchor opens offense
    /// and never defense, so a schedule whose changers field towers is refused
    /// at load and no plan can be built out of one honestly.
    /// </remarks>
    public static SweepPlan TowerRoster()
    {
        UnitTypeTable towers = UnitTypeTable.Parse(TowersOnly);
        TowerLayout defense = TheMatch.Layout(towers);

        return new SweepPlan(
            TheMatch.Map(),
            TheRuleset.Committed(),
            towers,
            TheSchedule.Committed(),
            defense,
            FieldPool.Of(new[] { RoundOrders.Of(defense, Wave(TheMatch.Types())) }),
            Seed,
            Runs,
            Waves,
            FieldSize);
    }

    /// <summary>The committed table with every walking unit priced at nothing.</summary>
    public static UnitTypeTable FreeTypes()
    {
        string[] lines = File.ReadAllText(RepoLayout.UnitsFile).Split('\n');
        int edited = 0;

        for (int index = 0; index < lines.Length; index++)
        {
            string[] fields = lines[index].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length <= CostField || fields[0] != "unit" || fields[3] != "moving")
            {
                continue;
            }

            fields[CostField] = "0";
            lines[index] = string.Join("   ", fields);
            edited++;
        }

        Assert.True(edited > 0, "The rewrite priced no walking row of the committed unit table at nothing.");

        return UnitTypeTable.Parse(string.Join("\n", lines));
    }

    /// <summary>The whole-population row for a creep, or a failure naming what the report did carry.</summary>
    public static SweepRow Whole(SweepReport report, string label) =>
        Row(report, label, SweepRow.AllIngredients);

    /// <summary>One row of the report, or a failure naming what the report did carry.</summary>
    public static SweepRow Row(SweepReport report, string label, int ingredients)
    {
        for (int index = 0; index < report.Rows.Count; index++)
        {
            if (report.Rows[index].Label == label && report.Rows[index].Ingredients == ingredients)
            {
                return report.Rows[index];
            }
        }

        throw new Xunit.Sdk.XunitException(
            "The report carries no row for "
            + label
            + " at "
            + ingredients.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + " ingredients. It carries: "
            + string.Join(", ", report.Rows));
    }

    /// <summary>The one bound on an axis, or a failure naming what the report did report.</summary>
    public static CoverageBound Bound(SweepReport report, string axis)
    {
        for (int index = 0; index < report.Coverage.Count; index++)
        {
            if (report.Coverage[index].Axis == axis)
            {
                return report.Coverage[index];
            }
        }

        throw new Xunit.Sdk.XunitException(
            "The report says nothing about its coverage of "
            + axis
            + ". It reports: "
            + string.Join(", ", report.Coverage));
    }
}
