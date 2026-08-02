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

    /// <summary>Where a defense's tower count sits, relative to the start of that record.</summary>
    public const int GhostTowerCountOffset = RecordFormat.HeaderBytes + 8;

    /// <summary>Where a defense's tower array starts, relative to the start of that record.</summary>
    public const int GhostTowersOffset = GhostTowerCountOffset + 2;

    /// <summary>Where a wave's order array starts, relative to the start of that record.</summary>
    public const int WaveOrdersOffset = RecordFormat.HeaderBytes + 2;

    /// <summary>Where a bundle's inlined map cells start.</summary>
    public const int BundleCellsOffset = RecordFormat.HeaderBytes + 8 + 2 + 2;

    /// <summary>Where the defense inside a bundle starts.</summary>
    public static int GhostIn(ReplayBundle bundle) =>
        BundleCellsOffset + (bundle.Map.Width * bundle.Map.Height);

    /// <summary>Where the wave inside a bundle starts.</summary>
    public static int WaveIn(ReplayBundle bundle) => GhostIn(bundle) + bundle.Ghost.ToBytes().Length;

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
