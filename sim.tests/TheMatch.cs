using System.Globalization;

namespace Sim.Tests;

/// <summary>
/// The one match the skeleton is built around, loaded from the committed files
/// the way every real caller loads it.
/// </summary>
/// <remarks>
/// The tests open the files and hand the simulation text. The simulation never
/// learns a path exists, which is the arrangement the whole no-ambient-IO
/// position rests on and which the IL scan enforces on the compiled assembly
/// rather than trusting these tests to have been honest about it.
/// </remarks>
public static class TheMatch
{
    /// <summary>
    /// The seed the committed golden trace was produced with. It lives in the
    /// match record rather than in the defense, so changing the dice does not
    /// change what a defense is.
    /// </summary>
    public const ulong Seed = 20260801UL;

    /// <summary>How many creeps get through, in the committed run.</summary>
    public const int LeakedInTheCommittedRun = 12;

    /// <summary>The tick the committed run ends on.</summary>
    public const int FinalTickOfTheCommittedRun = 5283;

    /// <summary>
    /// The handle the committed map is filed under, and the one
    /// tools/run-headless-match.ps1 records the bundle with. Zero would mean
    /// "the record does not say"; this map is the one the skeleton ships, so it
    /// is one.
    /// </summary>
    public const int MapHandle = 1;

    public static HexMap Map() => HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

    /// <summary>
    /// The committed roster: the table, with the committed upgrade ladder folded
    /// into its content hash.
    /// </summary>
    /// <remarks>
    /// The ladder is folded because this is what "the committed roster" means to
    /// every writer -- the bundle and the command stream both stamp this hash, and
    /// the replay gate compares it. A fixture that parsed the table alone would
    /// build records the committed ones do not match, and would report that as a
    /// record that had gone stale.
    /// </remarks>
    public static UnitTypeTable Types()
    {
        UnitTypeTable types = UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

        return types.WithLadder(Ladder(types));
    }

    /// <summary>The committed upgrade ladder, read against a roster.</summary>
    public static UpgradeLadder Ladder(UnitTypeTable types) =>
        UpgradeLadder.Parse(File.ReadAllText(RepoLayout.UpgradesFile), types);

    public static WaveScript Wave(UnitTypeTable types) =>
        WaveScript.Parse(File.ReadAllText(RepoLayout.WaveFile), types);

    public static TowerLayout Layout(UnitTypeTable types) =>
        TowerLayout.Parse(File.ReadAllText(RepoLayout.DefenseFile), types);

    public static GoldenTrace Trace() => GoldenTrace.Parse(File.ReadAllText(RepoLayout.GoldenTraceFile));

    /// <summary>A fresh match on the committed content. Every scenario starts here.</summary>
    public static Match Fresh(ulong seed = Seed)
    {
        UnitTypeTable types = Types();
        return new Match(Map(), TheRuleset.Committed(), Layout(types), Wave(types), seed);
    }

    /// <summary>The committed defense, recorded.</summary>
    public static GhostRecord Ghost(UnitTypeTable types) =>
        GhostRecord.Of(Map(), Layout(types), types, MapHandle);

    /// <summary>The committed wave, recorded.</summary>
    public static WaveRecord WaveOf(UnitTypeTable types) => WaveRecord.Of(Wave(types), types);

    /// <summary>The committed match as one self-contained replay bundle.</summary>
    /// <remarks>
    /// Stamped with the committed ruleset, because that is what the committed
    /// bundle on disk is stamped with and every gate test compares against it.
    /// </remarks>
    public static ReplayBundle Bundle(ulong seed = Seed) => Bundle(TheRuleset.Committed(), seed);

    /// <summary>The same match, stamped with a ruleset the caller names.</summary>
    public static ReplayBundle Bundle(Ruleset rules, ulong seed = Seed)
    {
        UnitTypeTable types = Types();
        return ReplayBundle.Of(Map(), Layout(types), Wave(types), types, rules, seed, MapHandle);
    }

    /// <summary>
    /// The committed match run the way the command line runs it: a tick at a
    /// time, nothing pulling a snapshot, and a listener collecting the moments
    /// the landmark table is made of.
    /// </summary>
    public static Landmarks LandmarksOfTheCommittedRun()
    {
        Match match = Fresh();
        var landmarks = new Landmarks();

        while (!match.IsFinished)
        {
            landmarks.EnteringTick(match.Tick + 1);
            match.Advance(1, landmarks);
        }

        return landmarks;
    }

    /// <summary>The data rows of a committed file, with its prose header dropped.</summary>
    public static string[] DataRows(string path) =>
        File.ReadAllLines(path)
            .Where(line => line.Length > 0 && !line.TrimStart().StartsWith('#'))
            .ToArray();

    /// <summary>
    /// The committed unit types with exactly one number moved -- a ruleset that
    /// has been retuned. Every id and every role is unchanged, so records made
    /// against it parse perfectly and only the content hash tells them apart,
    /// which is the whole situation the replay gate exists for.
    /// </summary>
    public static UnitTypeTable RetunedTypes() => UnitTypeTable.Parse(RetunedUnitsText());

    /// <summary>
    /// The committed unit table as text, with the first row's max hp one
    /// higher and nothing else touched.
    /// </summary>
    /// <remarks>
    /// The edit is made by rewriting a field of a parsed row rather than by
    /// replacing a literal run of characters. A literal here is a spelling of
    /// one row of one file at one moment: a rescale, a respacing or a fifth
    /// column turns the replacement into a no-op, at which point every caller
    /// is comparing a table against itself and every one of them is green. The
    /// assertions below are what make that impossible -- the retuned table has
    /// to differ from the committed one in exactly this field and in no other.
    /// </remarks>
    /// <remarks>
    /// OBSERVED: make the field rewrite a no-op -- assign the field back to
    /// itself instead of adding one. The assertion below goes red, 2001 against
    /// 2000, and so do the three gate tests that consume this. Before the guard
    /// was here the same no-op left every one of them green, comparing a table
    /// against itself.
    /// </remarks>
    public static string RetunedUnitsText()
    {
        string[] lines = File.ReadAllText(RepoLayout.UnitsFile).Split('\n');
        int edited = -1;

        for (int index = 0; index < lines.Length && edited < 0; index++)
        {
            string[] fields = lines[index]
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

            if (fields.Length == 0 || fields[0] != "unit")
            {
                continue;
            }

            int maxHp = int.Parse(fields[MaxHpField], CultureInfo.InvariantCulture);
            fields[MaxHpField] = (maxHp + 1).ToString(CultureInfo.InvariantCulture);
            lines[index] = string.Join("   ", fields);
            edited = index;
        }

        Assert.True(edited >= 0, "The committed unit table has no unit rows in it at all.");

        string retuned = string.Join("\n", lines);
        UnitTypeTable before = Types();
        UnitTypeTable after = UnitTypeTable.Parse(retuned);

        // Exactly one number moved, and it is a number the tick loop reads.
        Assert.Equal(before.Count, after.Count);
        Assert.Equal(before.Types[0].MaxHp + 1, after.Types[0].MaxHp);
        Assert.NotEqual(before.ContentHash, after.ContentHash);

        for (int index = 1; index < before.Count; index++)
        {
            Assert.Equal(before.Types[index].MaxHp, after.Types[index].MaxHp);
        }

        return retuned;
    }

    /// <summary>Where max hp sits in the layout the committed table declares.</summary>
    private const int MaxHpField = 4;

    /// <summary>Everything a match said happened, in the order it said it.</summary>
    public sealed class EventLog : IMatchEvents
    {
        public List<string> Kinds { get; } = new();

        public List<int> Subjects { get; } = new();

        public List<int> Amounts { get; } = new();

        public int Count => Kinds.Count;

        public void TowerFired(int towerId, int targetId) => Record("fired", towerId, targetId);

        public void CreepDamaged(int creepId, int amount) => Record("damaged", creepId, amount);

        public void CreepDied(int creepId) => Record("died", creepId, 0);

        public void CreepLeaked(int creepId) => Record("leaked", creepId, 0);

        public void ProjectileOrphaned(int projectileId) => Record("orphaned", projectileId, 0);

        public void CreepOvertook(int creepId, int overtakenCreepId) =>
            Record("overtook", creepId, overtakenCreepId);

        public int CountOf(string kind) => Kinds.Count(name => name == kind);

        private void Record(string kind, int subject, int amount)
        {
            Kinds.Add(kind);
            Subjects.Add(subject);
            Amounts.Add(amount);
        }
    }
}
