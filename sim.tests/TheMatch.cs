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
    public static HexMap Map() => HexMap.Parse(File.ReadAllText(RepoLayout.MapFile));

    public static UnitTypeTable Types() => UnitTypeTable.Parse(File.ReadAllText(RepoLayout.UnitsFile));

    public static WaveScript Wave(UnitTypeTable types) =>
        WaveScript.Parse(File.ReadAllText(RepoLayout.WaveFile), types);

    public static TowerLayout Layout(UnitTypeTable types) =>
        TowerLayout.Parse(File.ReadAllText(RepoLayout.DefenseFile), types);
}
