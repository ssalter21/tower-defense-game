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

    /// <summary>Where a command stream's anchor schedule hash sits.</summary>
    public const int CommandScheduleHashOffset = CommandRulesetHashOffset + 8;

    /// <summary>Where a command stream's run seed sits.</summary>
    public const int CommandSeedOffset = CommandScheduleHashOffset + 8;

    /// <summary>Where a command stream's build phase count sits.</summary>
    public const int CommandCountOffset = CommandSeedOffset + 8;

    /// <summary>Where a command stream's first build phase starts.</summary>
    public const int CommandsOffset = CommandCountOffset + 2;

    /// <summary>Where the wave sits inside one build phase.</summary>
    public const int CommandWaveOffset = 0;

    /// <summary>Where the take's kind sits inside one build phase.</summary>
    public const int CommandTakeKindOffset = 2;

    /// <summary>Where the take's id sits inside one build phase.</summary>
    public const int CommandTakeIdOffset = 3;

    /// <summary>Where the slot count sits inside one build phase.</summary>
    public const int CommandSlotCountOffset = 5;

    /// <summary>Where a build phase's slots start inside it.</summary>
    public const int CommandSlotsOffset = RecordFormat.CommandBytes;

    /// <summary>
    /// Where a build phase starts in a stream out of <see cref="TheCommands"/>.
    /// Every command in one fills a single slot, which is what makes the stride
    /// a constant and lets a test name a byte inside a command by index.
    /// </summary>
    public static int CommandAt(int index) =>
        CommandsOffset
        + (index * (RecordFormat.CommandBytes + (TheCommands.SlotsPerCommand * RecordFormat.SlotBytes)));

    /// <summary>Where the defense inside a bundle starts.</summary>
    public static int GhostIn(ReplayBundle bundle) =>
        BundleCellsOffset + (bundle.Map.Width * bundle.Map.Height);

    /// <summary>Where the wave inside a bundle starts.</summary>
    public static int WaveIn(ReplayBundle bundle) => GhostIn(bundle) + bundle.Ghost.ToBytes().Length;

    /// <summary>
    /// A bundle as the version-0 bytes it would have been: the format version
    /// turned back to zero and the ruleset stamp cut out.
    /// </summary>
    /// <remarks>
    /// The writer emits the current version and only that, so a fresh version-0
    /// bundle is not something this repository can make. The one that exists,
    /// <c>content/golden/defense-0.replay</c>, is stamped at a retired
    /// simulation version and is refused at that gate long before the ruleset
    /// one is reached -- so manufacturing the bytes here is what lets a test
    /// watch a missing stamp refuse on its own.
    /// </remarks>
    public static byte[] WithoutTheRulesetStamp(byte[] bundle)
    {
        byte[] older = WithU16(bundle, FormatVersionOffset, 0);

        return older[..BundleRulesetHashOffset].Concat(older[BundleSeedOffset..]).ToArray();
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
