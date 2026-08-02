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
    public const int FinalTickOfTheCommittedRun = 1852;

    public static HexMap Map() => HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

    public static UnitTypeTable Types() => UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

    public static WaveScript Wave(UnitTypeTable types) =>
        WaveScript.Parse(File.ReadAllText(RepoLayout.WaveFile), types);

    public static TowerLayout Layout(UnitTypeTable types) =>
        TowerLayout.Parse(File.ReadAllText(RepoLayout.DefenseFile), types);

    public static GoldenTrace Trace() => GoldenTrace.Parse(File.ReadAllText(RepoLayout.GoldenTraceFile));

    /// <summary>A fresh match on the committed content. Every scenario starts here.</summary>
    public static Match Fresh(ulong seed = Seed)
    {
        UnitTypeTable types = Types();
        return new Match(Map(), Layout(types), Wave(types), seed);
    }

    /// <summary>The committed defense, recorded.</summary>
    public static GhostRecord Ghost(UnitTypeTable types) => GhostRecord.Of(Map(), Layout(types), types);

    /// <summary>The committed wave, recorded.</summary>
    public static WaveRecord WaveOf(UnitTypeTable types) => WaveRecord.Of(Wave(types), types);

    /// <summary>
    /// The committed unit types with exactly one number moved -- a ruleset that
    /// has been retuned. Every id and every role is unchanged, so records made
    /// against it parse perfectly and only the content hash tells them apart,
    /// which is the whole situation the replay gate exists for.
    /// </summary>
    public static UnitTypeTable RetunedTypes()
    {
        string text = File.ReadAllText(RepoLayout.UnitsFile);
        string retuned = text.Replace(
            "unit   1   grunt   moving  200",
            "unit   1   grunt   moving  201",
            StringComparison.Ordinal);

        Assert.NotEqual(text, retuned);

        return UnitTypeTable.Parse(retuned);
    }

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

        public int CountOf(string kind) => Kinds.Count(name => name == kind);

        private void Record(string kind, int subject, int amount)
        {
            Kinds.Add(kind);
            Subjects.Add(subject);
            Amounts.Add(amount);
        }
    }
}
