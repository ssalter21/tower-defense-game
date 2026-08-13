using Sim;

namespace Sim.Cli;

/// <summary>
/// The shell's half of proving a session: putting the script a
/// <see cref="ProvedSession"/> agreed with on a disk, and saying so.
/// </summary>
/// <remarks>
/// <para>
/// <b>The decision to write is still the prover's.</b> A script nobody played
/// back is a record of nothing in particular, so the only path from a session
/// to a file runs through <see cref="ProvedSession.Agreed"/>: a disagreement
/// writes nothing and comes back as a verb's non-zero exit code. What moved
/// down into the simulation is the claim; what stays here is the file, because
/// <c>System.IO</c> is a banned namespace there and a client's storage is
/// Unity's rather than a path.
/// </para>
/// <para>
/// <b>An extension rather than a wrapper.</b> The write is one more thing a
/// proved session can have done to it, and spelling it that way keeps the
/// sentence at the verb -- prove, then write -- instead of introducing a second
/// object that holds the first and has to be asked for what it holds.
/// </para>
/// </remarks>
internal static class WrittenSession
{
    /// <summary>What a session that committed no round is told.</summary>
    private const string NothingPlayed =
        "No round was played, so there is no script to write and nothing for a second run to disagree "
        + "with. Nothing was written to ";

    /// <summary>
    /// Writes the script where the fresh run agreed with the session, and says
    /// what did not where it did not.
    /// </summary>
    /// <param name="proved">The session, held against a fresh run of its own script.</param>
    /// <param name="path">Where an agreeing session's script goes, and the only file this writes.</param>
    /// <param name="writer">Where the line about it goes.</param>
    /// <returns>False where the session was refused, which is a verb's exit code.</returns>
    public static bool Written(this ProvedSession proved, string path, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(proved);
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(writer);

        if (proved.Disagreement is not null)
        {
            PlainText.Say(writer, proved.Disagreement + "\n\nNothing was written to " + path + ".");

            return false;
        }

        if (proved.Script.Length == 0)
        {
            PlainText.Say(writer, NothingPlayed + path + ".");

            return true;
        }

        PlainText.Written(path, proved.Script);
        PlainText.Say(
            writer,
            "wrote      "
            + path
            + " ("
            + PlainText.Number(proved.RoundsProved)
            + " rounds, played into a fresh run and matched round for round before writing)");

        return true;
    }
}
