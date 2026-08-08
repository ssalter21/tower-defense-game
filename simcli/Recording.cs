using Sim;

namespace Sim.Cli;

/// <summary>
/// Turning the authored content files into the one self-contained replay bundle
/// the headless run and everything after it load.
/// </summary>
/// <remarks>
/// <para>
/// <b>The committed bundle comes from here, and never from a hex editor.</b> A
/// fixture assembled by hand is a fixture that agrees with whatever the person
/// assembling it believed the format was; this one is written by the only
/// writer the format has, which is the same code every real record will be
/// written by.
/// </para>
/// <para>
/// <b>Nothing is written until the bytes have been read back and played.</b>
/// The bundle is parsed from its own output, taken through the replay gate and
/// run to the end before it is handed over -- so a bundle that cannot be
/// replayed never reaches the disk, and the committed fixture is one that
/// provably came from a real run rather than from a real writer.
/// </para>
/// </remarks>
internal static class Recording
{
    /// <summary>
    /// Records the content as a bundle and proves it by replaying it. The run
    /// comes back with it, so whatever asked for the recording can say what the
    /// match it recorded actually did.
    /// </summary>
    public static (byte[] Bytes, HeadlessRun Proof) Of(
        string mapText,
        string unitsText,
        string rulesText,
        string defenseText,
        string waveText,
        ulong seed,
        int mapHandle)
    {
        UnitTypeTable types = UnitTypeTable.Parse(unitsText);
        HexMap map = HexMap.Parse(mapText);
        TowerLayout layout = TowerLayout.Parse(defenseText, types);
        WaveScript wave = WaveScript.Parse(waveText, types);

        byte[] bytes = ReplayBundle.Of(map, layout, wave, types, seed, mapHandle).ToBytes();

        return (bytes, HeadlessRun.Of(bytes, unitsText, rulesText));
    }
}
