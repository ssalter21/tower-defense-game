using System;
using System.IO;
using System.Text;
using Sim;

namespace View
{
    /// <summary>
    /// The client's half of proving a session: putting the script a
    /// <see cref="ProvedSession"/> agreed with on a disk, and saying so on
    /// screen.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The claim is the simulation's and the file is this file's.</b>
    /// <c>System.IO</c> is a banned namespace in <c>sim</c> and the build gate
    /// scans the compiled image for it, so the claim — that the run somebody
    /// played and the record of it are one run — lives down there and the file
    /// lives up here. The shell once had a half of its own for the deleted
    /// <c>play</c> verb; this is now the only one, and a run is played here.
    /// </para>
    /// <para>
    /// <b>The decision to write is still the prover's.</b> A session that did
    /// not agree hands back no script at all, so there is nothing here to write
    /// and no way for this file to write one anyway. That guarantee is
    /// structural and is not restated as a condition over here.
    /// </para>
    /// <para>
    /// <b>What lands is a command script and not a save.</b> It is in
    /// <c>content/commands.txt</c>'s grammar, on that file's columns, so it
    /// pastes into it and <c>simcli record-run</c> compiles it into the command
    /// file <c>simcli play-run</c> plays. A run does not survive quitting; what
    /// survives is the record of what was played.
    /// </para>
    /// </remarks>
    public static class WrittenRun
    {
        /// <summary>
        /// What the script is called on disk. <c>content/commands.txt</c>'s own
        /// name, because it is the same grammar and the two are meant to be
        /// diffed against each other.
        /// </summary>
        public const string FileName = "commands.txt";

        /// <summary>The encoding every text file in this project is written in.</summary>
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Writes the script where the fresh run agreed with the session, and
        /// hands back where it landed.
        /// </summary>
        /// <param name="proved">The session, held against a fresh run of its own script.</param>
        /// <param name="directory">The folder the script is written into.</param>
        /// <returns>The path written, or null where there was nothing to write.</returns>
        public static string Written(ProvedSession proved, string directory)
        {
            if (proved is null) throw new ArgumentNullException(nameof(proved));
            if (directory is null) throw new ArgumentNullException(nameof(directory));

            if (proved.Script.Length == 0)
            {
                return null;
            }

            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, FileName);

            File.WriteAllText(path, proved.Script, Utf8);

            return path;
        }

        /// <summary>
        /// What a person is told about it: where it went, or why nothing went
        /// anywhere.
        /// </summary>
        /// <remarks>
        /// A disagreement's own sentence is shown rather than summarised.
        /// <see cref="ProvedSession"/> writes it for a person and it names the
        /// round the two runs parted on; a screen that replaced it with "could
        /// not save" would be throwing away the only description of the bug.
        /// </remarks>
        public static string Wording(ProvedSession proved, string path)
        {
            if (proved is null) throw new ArgumentNullException(nameof(proved));

            if (proved.Disagreement is object)
            {
                return proved.Disagreement;
            }

            return path is null
                ? "No round was played, so there is no script to write."
                : "Written to " + path;
        }
    }
}
