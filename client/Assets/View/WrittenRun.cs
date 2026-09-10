using System;
using System.Collections.Generic;
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
        /// <remarks>
        /// The separators are made one character before the path leaves.
        /// <c>Application.persistentDataPath</c> is written with forward slashes
        /// and <see cref="Path.Combine"/> joins with the platform's own, so the
        /// plain call produces a path that changes separator half way along —
        /// and this path is read out to whoever played, on the end frame, by
        /// <see cref="Wording"/>.
        /// </remarks>
        public static string Written(ProvedSession proved, string directory)
        {
            if (proved is null) throw new ArgumentNullException(nameof(proved));
            if (directory is null) throw new ArgumentNullException(nameof(directory));

            if (proved.Script.Length == 0)
            {
                return null;
            }

            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, FileName).Replace('/', Path.DirectorySeparatorChar);

            File.WriteAllText(path, proved.Script, Utf8);

            return path;
        }

        /// <summary>
        /// Puts the run's own rounds into a pool folder, so that what somebody
        /// played becomes somebody else's opponents.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Only a session the prover agreed with is stored.</b> A run whose
        /// record does not play back to what the player was shown is a run
        /// nobody can reproduce, and a pool of those is a population that
        /// cannot be checked against anything. So this is called on exactly the
        /// sessions <see cref="Written"/> writes a script for.
        /// </para>
        /// <para>
        /// <b>What a round is made of, and the proof that it reads back, are
        /// the simulation's</b> -- <see cref="RecordedRun"/> -- so the client
        /// and <c>simcli play-run --store</c> store a run the same way. What is
        /// here is the file.
        /// </para>
        /// <para>
        /// <b>A round that stood no wall or sent nothing is not stored.</b> A
        /// stored round is a wall and a wave, so a first round that built
        /// nothing simply does not join the pool; the sentence saying so comes
        /// back for whoever wants to show it.
        /// </para>
        /// </remarks>
        /// <param name="run">The run, after every round of it resolved.</param>
        /// <param name="map">The board it was played on.</param>
        /// <param name="types">The roster its rounds are recorded against.</param>
        /// <param name="directory">The pool folder the records land in.</param>
        /// <returns>What was stored and what was not, one sentence each.</returns>
        public static IReadOnlyList<string> Stored(
            Run run,
            HexMap map,
            UnitTypeTable types,
            string directory)
        {
            if (directory is null) throw new ArgumentNullException(nameof(directory));

            IReadOnlyList<StorableRound> rounds = RecordedRun.Of(run, map, types);
            var said = new List<string>();

            Directory.CreateDirectory(directory);

            foreach (StorableRound round in rounds)
            {
                if (!round.IsStorable)
                {
                    said.Add(round.Sentence(string.Empty));

                    continue;
                }

                string name = round.Name + StreamingContent.PoolFileExtension;

                File.WriteAllBytes(Path.Combine(directory, name), round.Bytes);
                said.Add(round.Sentence(name));
            }

            return said;
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
