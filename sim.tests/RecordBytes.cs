namespace Sim.Tests;

/// <summary>
/// Where the fields of a record are, and how to damage one on purpose.
/// </summary>
/// <remarks>
/// <para>
/// The negative suite is one test per loud failure: take a good fixture, flip
/// one byte, assert the specific error. That only works if a test can say
/// exactly which byte, so the offsets are computed here from the format's own
/// constants rather than being written out as magic numbers -- a hard-coded 183
/// would go on passing after the layout moved underneath it, asserting the wrong
/// failure fired for the wrong reason.
/// </para>
/// </remarks>
public static class RecordBytes
{
    /// <summary>Where the format version sits inside any of the three headers.</summary>
    public const int FormatVersionOffset = 4;

    /// <summary>Where the simulation version sits inside any of the three headers.</summary>
    public const int SimVersionOffset = 6;

    /// <summary>Where the content hash sits inside any of the three headers.</summary>
    public const int ContentHashOffset = 10;

    /// <summary>Where a defense's map hash sits, relative to the start of that record.</summary>
    public const int GhostMapHashOffset = RecordFormat.HeaderBytes;

    /// <summary>
    /// Where a version-1 defense's map handle sits. A version-0 record has
    /// nothing here and its tower count starts two bytes earlier.
    /// </summary>
    public const int GhostMapHandleOffset = GhostMapHashOffset + 8;

    /// <summary>Where a defense's tower count sits, relative to the start of that record.</summary>
    public const int GhostTowerCountOffset = GhostMapHandleOffset + 2;

    /// <summary>Where a defense's tower array starts, relative to the start of that record.</summary>
    public const int GhostTowersOffset = GhostTowerCountOffset + 2;

    /// <summary>Where a wave's order array starts, relative to the start of that record.</summary>
    public const int WaveOrdersOffset = RecordFormat.HeaderBytes + 2;

    /// <summary>Where a bundle's ruleset hash sits. A version-0 bundle has nothing here.</summary>
    public const int BundleRulesetHashOffset = RecordFormat.HeaderBytes;

    /// <summary>Where a bundle's seed sits.</summary>
    public const int BundleSeedOffset = BundleRulesetHashOffset + 8;

    /// <summary>Where a bundle's map width sits, with the height two bytes after it.</summary>
    public const int BundleMapWidthOffset = BundleSeedOffset + 8;

    /// <summary>Where a bundle's inlined map cells start.</summary>
    public const int BundleCellsOffset = BundleMapWidthOffset + 2 + 2;

    /// <summary>Where a command stream's ruleset hash sits.</summary>
    public const int CommandRulesetHashOffset = RecordFormat.HeaderBytes;

    /// <summary>
    /// Where a command stream's upgrade ladder hash sits.
    /// </summary>
    /// <remarks>
    /// The same offset and the same width the anchor schedule's hash had. #179
    /// deleted the schedule and gave the simulation the ladder to read, so the
    /// third stamp went on the content that took its place -- which is what
    /// format version 2 is the bump for.
    /// </remarks>
    public const int CommandLadderHashOffset = CommandRulesetHashOffset + 8;

    /// <summary>Where a command stream's run seed sits.</summary>
    public const int CommandSeedOffset = CommandLadderHashOffset + 8;

    /// <summary>Where a command stream's build phase count sits.</summary>
    public const int CommandCountOffset = CommandSeedOffset + 8;

    /// <summary>Where a command stream's first build phase starts.</summary>
    public const int CommandsOffset = CommandCountOffset + 2;

    /// <summary>Where the wave sits inside one build phase.</summary>
    public const int CommandWaveOffset = 0;

    /// <summary>Where the action count sits inside one build phase.</summary>
    public const int CommandActionCountOffset = 2;

    /// <summary>Where a build phase's actions start inside it.</summary>
    public const int CommandActionsOffset = CommandActionCountOffset + 2;

    /// <summary>Where the slots of a build phase with no actions start inside it.</summary>
    public const int CommandSlotsOffset = RecordFormat.CommandBytes;

    /// <summary>
    /// Where a build phase starts in a stream out of <see cref="TheCommands"/>.
    /// Every command in one fills a single slot and builds nothing, which is
    /// what makes the stride a constant and lets a test name a byte inside a
    /// command by index.
    /// </summary>
    public static int CommandAt(int index) =>
        CommandsOffset
        + (index * (RecordFormat.CommandBytes + (TheCommands.SlotsPerCommand * RecordFormat.SlotBytes)));

    /// <summary>Where a bundle's inlined map levels start: the plane after the cells.</summary>
    public static int LevelsIn(ReplayBundle bundle) =>
        BundleCellsOffset + (bundle.Map.Width * bundle.Map.Height);

    /// <summary>Where the defense inside a bundle starts: after both planes of the grid.</summary>
    public static int GhostIn(ReplayBundle bundle) =>
        LevelsIn(bundle) + (bundle.Map.Width * bundle.Map.Height);

    /// <summary>Where the wave inside a bundle starts.</summary>
    public static int WaveIn(ReplayBundle bundle) => GhostIn(bundle) + bundle.Ghost.ToBytes().Length;

    /// <summary>
    /// A bundle as the version-0 bytes it would have been: the format version
    /// turned back to zero and the ruleset stamp cut out.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The writer emits the current version and only that, so a fresh version-0
    /// bundle is not something this repository can make. The one that exists,
    /// <c>content/golden/defense-0.replay</c>, is stamped at a retired
    /// simulation version and is refused at that gate long before the ruleset
    /// one is reached -- so manufacturing the bytes here is what lets a test
    /// watch a missing stamp refuse on its own.
    /// </para>
    /// <para>
    /// <b>The level plane comes out with the stamp, and the defense's map hash
    /// goes back to the layout of that era.</b> A version-0 bundle has neither
    /// a ruleset stamp nor a second plane: a reader at that version takes one
    /// plane of map bytes and then the defense, so bytes left in would be read
    /// as a defense that is a row of levels. And its defense pinned the terrain
    /// alone, under <c>hex-map/1</c> -- a record carrying today's fold under
    /// yesterday's version is a contradiction no real version-0 bundle has, and
    /// one that fails the map gate rather than the gate a test built on this
    /// was watching.
    /// </para>
    /// </remarks>
    public static byte[] WithoutTheRulesetStamp(ReplayBundle good, byte[] bundle)
    {
        byte[] older = WithU16(bundle, FormatVersionOffset, 0);
        int levels = LevelsIn(good);
        int cells = good.Map.Width * good.Map.Height;

        byte[] cut = older[..BundleRulesetHashOffset]
            .Concat(older[BundleSeedOffset..levels])
            .Concat(older[(levels + cells)..])
            .ToArray();

        return WithU64(
            cut,
            GhostIn(good) - 8 - cells + GhostMapHashOffset,
            good.Map.MapHashUnder(1).Value);
    }

    /// <summary>The same bytes with a little-endian u64 replaced.</summary>
    public static byte[] WithU64(byte[] bytes, int offset, ulong value)
    {
        byte[] copy = (byte[])bytes.Clone();

        for (int shift = 0; shift < 64; shift += 8)
        {
            copy[offset + (shift / 8)] = (byte)(value >> shift);
        }

        return copy;
    }

    /// <summary>The same bytes with one of them replaced.</summary>
    public static byte[] With(byte[] bytes, int offset, byte value)
    {
        byte[] copy = (byte[])bytes.Clone();
        copy[offset] = value;
        return copy;
    }

    /// <summary>The same bytes with a little-endian u16 replaced.</summary>
    public static byte[] WithU16(byte[] bytes, int offset, int value)
    {
        byte[] copy = (byte[])bytes.Clone();
        copy[offset] = (byte)value;
        copy[offset + 1] = (byte)(value >> 8);
        return copy;
    }

    /// <summary>The same bytes with a little-endian u32 replaced.</summary>
    public static byte[] WithU32(byte[] bytes, int offset, uint value)
    {
        byte[] copy = (byte[])bytes.Clone();

        for (int shift = 0; shift < 32; shift += 8)
        {
            copy[offset + (shift / 8)] = (byte)(value >> shift);
        }

        return copy;
    }

    /// <summary>The same bytes with one bit of one byte turned over.</summary>
    public static byte[] Flip(byte[] bytes, int offset) => With(bytes, offset, (byte)(bytes[offset] ^ 1));

    /// <summary>The same bytes, cut short.</summary>
    public static byte[] Truncated(byte[] bytes, int dropped) => bytes[..^dropped];

    /// <summary>The same bytes with something else written over a range of them.</summary>
    public static byte[] Splice(byte[] bytes, int offset, byte[] replacement)
    {
        byte[] copy = (byte[])bytes.Clone();
        replacement.CopyTo(copy, offset);
        return copy;
    }

    /// <summary>The same bytes with two equal-length runs exchanged.</summary>
    public static byte[] Swap(byte[] bytes, int first, int second, int length)
    {
        byte[] copy = (byte[])bytes.Clone();

        for (int index = 0; index < length; index++)
        {
            copy[first + index] = bytes[second + index];
            copy[second + index] = bytes[first + index];
        }

        return copy;
    }
}
