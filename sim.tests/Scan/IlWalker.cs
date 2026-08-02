using System.Reflection;
using System.Reflection.Emit;

namespace Sim.Tests.Scan;

/// <summary>Walks a method body's IL, one instruction at a time.</summary>
/// <remarks>
/// <para>
/// The instruction stream has to be decoded rather than searched, because
/// operands are raw bytes: the four bytes of a metadata token, or the eight of
/// an <c>ldc.i8</c>, can hold any value at all, including one that reads as an
/// <c>ldc.r8</c> opcode. A substring search over the bytes would report floats
/// that are not there and would be untrustworthy in exactly the direction that
/// matters -- a scan that cries wolf gets switched off.
/// </para>
/// <para>
/// The operand-size table is not written out by hand. It is reflected out of
/// <see cref="OpCodes"/>, so it is the runtime's own table rather than a
/// transcription of it, and a transcription error is not one of the things
/// that can go wrong here.
/// </para>
/// </remarks>
internal static class IlWalker
{
    private static readonly Dictionary<int, OpCode> Table = BuildTable();

    /// <summary>Every opcode in the body, in order, with its offset.</summary>
    public static IEnumerable<(int Offset, OpCode OpCode)> Walk(byte[] il)
    {
        int position = 0;

        while (position < il.Length)
        {
            int offset = position;
            int key = il[position];

            if (key == 0xFE)
            {
                if (position + 1 >= il.Length)
                {
                    throw new BadImageFormatException($"Truncated two-byte opcode at IL offset {offset}.");
                }

                key = 0xFE00 | il[position + 1];
                position += 2;
            }
            else
            {
                position += 1;
            }

            if (!Table.TryGetValue(key, out OpCode opCode))
            {
                throw new BadImageFormatException($"Unknown opcode 0x{key:X} at IL offset {offset}.");
            }

            yield return (offset, opCode);

            position += OperandSize(opCode, il, position);
        }
    }

    private static int OperandSize(OpCode opCode, byte[] il, int operandStart) => opCode.OperandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget => 1,
        OperandType.ShortInlineI => 1,
        OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget => 4,
        OperandType.InlineField => 4,
        OperandType.InlineI => 4,
        OperandType.InlineMethod => 4,
        OperandType.InlineSig => 4,
        OperandType.InlineString => 4,
        OperandType.InlineTok => 4,
        OperandType.InlineType => 4,
        OperandType.ShortInlineR => 4,
        OperandType.InlineI8 => 8,
        OperandType.InlineR => 8,
        OperandType.InlineSwitch => SwitchSize(il, operandStart),
        _ => throw new BadImageFormatException($"Unhandled operand type {opCode.OperandType} for {opCode.Name}."),
    };

    private static int SwitchSize(byte[] il, int operandStart)
    {
        if (operandStart + 4 > il.Length)
        {
            throw new BadImageFormatException("Truncated switch operand.");
        }

        uint count = BitConverter.ToUInt32(il, operandStart);
        return 4 + ((int)count * 4);
    }

    private static Dictionary<int, OpCode> BuildTable()
    {
        var table = new Dictionary<int, OpCode>();

        foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                table[opCode.Value & 0xFFFF] = opCode;
            }
        }

        return table;
    }
}
