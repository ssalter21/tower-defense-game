using Sim;

namespace Sim.Cli;

/// <summary>
/// One authored file a run is built out of: what a command line calls it, what
/// it is called on disk, and the type that parses it.
/// </summary>
/// <remarks>
/// The three belong together because they are three names for one thing. Spelled
/// apart, an option list can carry a file the reader never opens, a usage block
/// can offer an option no verb takes, and a gate can check a file no run reads --
/// and each of those reads as working software from every side but one.
/// </remarks>
internal sealed class ContentFile
{
    internal ContentFile(string option, string fileName, Type parsedBy)
    {
        Option = option;
        FileName = fileName;
        ParsedBy = parsedBy;
    }

    /// <summary>What a command line names it with, without the two dashes.</summary>
    public string Option { get; }

    /// <summary>What it is called inside a directory of content.</summary>
    public string FileName { get; }

    /// <summary>The type whose <c>Parse</c> reads it.</summary>
    /// <remarks>
    /// Held as a type rather than a name so that renaming a parser moves this
    /// row with it. Nothing here invokes it -- the parsers take different
    /// arguments and are called in an order the roster fixes, which
    /// <c>RunContent.Of</c> spells out -- and what reads it is the gate in
    /// <c>sim.tests</c> that holds every content file to a parse under a hostile
    /// culture.
    /// </remarks>
    public Type ParsedBy { get; }

    public override string ToString() => "--" + Option + " (" + FileName + ")";
}

/// <summary>
/// The seven files a run is built from, declared once.
/// </summary>
/// <remarks>
/// <para>
/// <b>Everything that needs the list reads it off here.</b> The run verbs'
/// option list, the usage block, the reader that opens the files and the
/// repository layout the gate tests find content by are all derived from this
/// declaration, so adding a content file is a row here and a parser, rather than
/// seven edits of which any six compile.
/// </para>
/// <para>
/// <b>The gate tests compile this very file.</b> <c>sim.tests</c> links it as a
/// source rather than restating it -- see the <c>Compile</c> item in
/// <c>Sim.Tests.csproj</c> -- because a second list that has to agree with this
/// one is the failure this declaration exists to remove, and the test project
/// cannot reference the runner: it exercises the command line as a process.
/// </para>
/// <para>
/// <b>The simulation is not where this lives, deliberately.</b> Nothing in the
/// simulation assembly knows a file name or where its input came from
/// (ADR-0018); it is handed text. Naming the files is the shell's job, which is
/// why the declaration sits beside the code that opens them.
/// </para>
/// <para>
/// <b><c>--upgrades</c> is in the list rather than defaulted</b>, and so is
/// every other row: a content file that is optional is a default, and a default
/// is a number nobody authored folded into a content hash as though somebody
/// had. What shortens an invocation is <c>--content</c>, which names a directory
/// and finds all seven by the names below -- an explicit directory rather than
/// an assumed file.
/// </para>
/// </remarks>
internal static class RunContentFiles
{
    /// <summary>The board: the corridor, the anchors and the buildable ground.</summary>
    public static ContentFile Map { get; } = new ContentFile("map", "map.txt", typeof(HexMap));

    /// <summary>The roster every creep, cost and offering is read out of.</summary>
    public static ContentFile Units { get; } = new ContentFile("units", "units.txt", typeof(UnitTypeTable));

    /// <summary>Which unit follows which, folded into the roster's content hash.</summary>
    public static ContentFile Upgrades { get; } =
        new ContentFile("upgrades", "upgrades.txt", typeof(UpgradeLadder));

    /// <summary>Every number a shot and a purse resolve through.</summary>
    public static ContentFile Rules { get; } = new ContentFile("rules", "ruleset.txt", typeof(Ruleset));

    /// <summary>The shape of a run: which waves are anchors, and what each draws from.</summary>
    public static ContentFile Schedule { get; } =
        new ContentFile("schedule", "schedule.txt", typeof(AnchorSchedule));

    /// <summary>
    /// What stands while this run's waves are sent, and what the canned opponent
    /// stands behind. Read twice on purpose, so both directions of a round are
    /// measured through the same wall.
    /// </summary>
    public static ContentFile Defense { get; } = new ContentFile("defense", "defense.txt", typeof(TowerLayout));

    /// <summary>
    /// The canned opponent, and not a wave the run sends.
    /// </summary>
    /// <remarks>
    /// A run's own waves are composed by the build phases coming off its command
    /// stream and are read from no file at all. <c>record</c> is the only verb
    /// that takes <c>--wave</c>, and what it means there is a whole authored
    /// match. See
    /// <c>docs/adr/0040-a-run-is-authored-as-text-and-compiled-to-a-record.md</c>.
    /// </remarks>
    public static ContentFile Field { get; } = new ContentFile("field", "field.txt", typeof(WaveScript));

    /// <summary>The seven, in the order the content is layered.</summary>
    public static IReadOnlyList<ContentFile> All { get; } =
        new[] { Map, Units, Upgrades, Rules, Schedule, Defense, Field };
}
