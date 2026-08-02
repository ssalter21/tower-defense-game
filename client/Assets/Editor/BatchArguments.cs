using System;

namespace View.Editor
{
    /// <summary>
    /// The flags a batchmode entry point was launched with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every automation in this project is a static command-line entry point —
    /// <c>-batchmode -executeMethod</c>, no bridge, nobody at a keyboard — so
    /// every one of them has to fish its own arguments out of the process
    /// command line, because Unity hands them over in no other way. This is
    /// that fishing, written once.
    /// </para>
    /// <para>
    /// It was written once after being written three times. Two of the copies
    /// were character for character identical, which is the way this ends up
    /// wrong: not because the loop is hard, but because the fourth tool copies
    /// whichever of the three it happened to be looking at.
    /// </para>
    /// </remarks>
    public static class BatchArguments
    {
        /// <summary>
        /// The value after <paramref name="flag"/>, or null if it was not
        /// passed.
        /// </summary>
        /// <remarks>
        /// Null rather than empty, so a caller can tell "not passed" from
        /// "passed as nothing" and fall back to its own default for only the
        /// first of those.
        /// </remarks>
        public static string Value(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();

            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], flag, StringComparison.Ordinal))
                {
                    return args[index + 1];
                }
            }

            return null;
        }
    }
}
