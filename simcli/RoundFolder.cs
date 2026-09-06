using Sim;

namespace Sim.Cli;

/// <summary>
/// A directory of stored rounds: one file per round, named by its record id.
/// </summary>
/// <remarks>
/// <para>
/// <b>This opens files and does nothing else.</b> Which stored rounds a run may
/// meet, and what a stage's population is indexed like, are
/// <see cref="StoredRounds"/>'s -- so the shell and the client read one folder
/// by one rule rather than two that have to agree. Nothing in the simulation
/// assembly can open a path, and the build gate scans the compiled image to
/// keep it that way (ADR-0018).
/// </para>
/// <para>
/// <b>The order a directory lists in does not matter</b>, because
/// <see cref="StoredRounds"/> files each stage by id whatever order the records
/// arrive in. That is a rule about a draw, and it lives where the draw's
/// population is built rather than in each shell that walks a folder.
/// </para>
/// </remarks>
internal sealed class RoundFolder
{
    /// <summary>What a stored round is called on disk, after its id.</summary>
    public const string FileExtension = ".round";

    private readonly string _directory;

    public RoundFolder(string directory)
    {
        _directory = directory;
    }

    /// <summary>Every round in the folder, offered to the pool.</summary>
    public StoredRounds Read(HexMap map, UnitTypeTable types)
    {
        var pool = new StoredRounds(map, types);

        foreach (string path in Files())
        {
            pool.Add(Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path));
        }

        return pool;
    }

    /// <summary>
    /// Writes every round of a played run into the folder, having read each
    /// one's bytes back first.
    /// </summary>
    /// <remarks>
    /// Composing the records and proving each one reads back is
    /// <see cref="RecordedRun"/>'s, so the shell and the client store a run the
    /// same way. What is left here is the file: where it lands, what it is
    /// called, and the line saying it happened.
    /// </remarks>
    public IReadOnlyList<string> Written(Run run, HexMap map, UnitTypeTable types)
    {
        IReadOnlyList<StorableRound> rounds = RecordedRun.Of(run, map, types);
        var said = new List<string>();

        Directory.CreateDirectory(_directory);

        foreach (StorableRound round in rounds)
        {
            if (!round.IsStorable)
            {
                said.Add(round.Sentence(string.Empty));

                continue;
            }

            string name = round.Name + FileExtension;

            File.WriteAllBytes(Path.Combine(_directory, name), round.Bytes);
            said.Add(round.Sentence(name));
        }

        return said;
    }

    /// <summary>
    /// The folder's stored rounds. An absent folder is an empty pool rather
    /// than a refusal: a fresh clone has not seeded one, and a run against
    /// nobody stored is the canned field.
    /// </summary>
    private IEnumerable<string> Files() =>
        Directory.Exists(_directory)
            ? Directory.GetFiles(_directory, "*" + FileExtension)
            : new string[0];
}
