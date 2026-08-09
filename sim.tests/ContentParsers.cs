using System.Reflection;

namespace Sim.Tests;

/// <summary>
/// One content parser: the type that does the parsing, the committed file it
/// reads, and a fold over everything that came out of it.
/// </summary>
/// <remarks>
/// <para>
/// The fold is the interesting member. Five of these parsers already end in a
/// digest of their own -- a content hash or a map hash -- and for those
/// <see cref="Digest"/> is that value. The other four produce a shape with no
/// hash on it, so the fold here walks what was parsed and absorbs every field.
/// Either way the comparison a test makes is the same one: parse twice, fold
/// twice, and a single number says whether the two parses agreed about
/// anything at all.
/// </para>
/// <para>
/// The folds written here are test scaffolding and nothing stores them, so
/// their labels are local names rather than versioned layouts. What matters is
/// only that a fold covers every field a culture could have moved.
/// </para>
/// </remarks>
public sealed class ContentParser
{
    internal ContentParser(Type parsedBy, string file, bool integersOnly, Func<string, Hash64> digest)
    {
        ParsedBy = parsedBy;
        File = file;
        IntegersOnly = integersOnly;
        Digest = digest;
    }

    /// <summary>The type whose <c>Parse</c> reads this file.</summary>
    public Type ParsedBy { get; }

    /// <summary>The committed file it is pointed at.</summary>
    public string File { get; }

    /// <summary>
    /// Whether this file is authored by hand in the integers-only dialect, and
    /// must therefore carry no decimal point on a data line.
    /// </summary>
    /// <remarks>
    /// Two of the files are not. <c>map.txt</c> is a character grid whose ground
    /// glyph <i>is</i> a full stop, and <c>golden-trace.txt</c> is written by a
    /// run rather than by a person and carries hexadecimal digests. Neither is a
    /// file a designer types a fraction into, which is the mistake the
    /// decimal-point check exists to catch.
    /// </remarks>
    public bool IntegersOnly { get; }

    /// <summary>Parses the text and folds everything the parse produced into one number.</summary>
    public Func<string, Hash64> Digest { get; }

    /// <summary>The parser's short name, which is what a failure message names.</summary>
    public string Name => ParsedBy.Name;

    public override string ToString() => Name + " (" + Path.GetFileName(File) + ")";
}

/// <summary>
/// Every content parser the simulation has, declared once.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the single declaration two gates are derived from.</b>
/// <see cref="RepoLayout.NumericContentFiles"/> is the integers-only subset of
/// the files below, and <c>HostileLocaleTests</c> runs the whole list under a
/// culture chosen to break it. Neither restates the parsers, so a parser can
/// only be covered by both or by neither.
/// </para>
/// <para>
/// A parser added to the simulation and not added here reddens
/// <c>HostileLocaleTests.Every_parser_in_the_simulation_is_declared_in_the_sweep</c>,
/// which reflects over the assembly rather than trusting this list to be
/// complete. That test is the reason this list can be relied on: without it,
/// the failure mode is a new parser that is silently in neither gate, which is
/// exactly how the tower layout, the golden trace and the upgrade ladder came
/// to be missing from the sweep for as long as they were.
/// </para>
/// </remarks>
public static class ContentParsers
{
    private static readonly Lazy<IReadOnlyList<ContentParser>> LazyAll = new(Declare);

    /// <summary>Every parser, in the order the content is layered.</summary>
    public static IReadOnlyList<ContentParser> All => LazyAll.Value;

    /// <summary>
    /// Every type in the simulation that declares a public static <c>Parse</c>.
    /// This is what "a content parser" means, discovered rather than listed.
    /// </summary>
    public static IReadOnlyList<Type> DiscoveredInTheAssembly()
    {
        var found = new List<Type>();

        foreach (Type type in typeof(UnitTypeTable).Assembly.GetExportedTypes())
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);

            for (int index = 0; index < methods.Length; index++)
            {
                if (string.Equals(methods[index].Name, "Parse", StringComparison.Ordinal))
                {
                    found.Add(type);
                    break;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// The unit type table, reparsed rather than cached, because the parsers
    /// that take one have to take one that was itself parsed under whatever
    /// culture is in effect. A table parsed once up front and reused would leave
    /// half of every dependent parse running under the invariant culture.
    /// </summary>
    private static UnitTypeTable Types() =>
        UnitTypeTable.Parse(System.IO.File.ReadAllText(RepoLayout.UnitsFile));

    private static IReadOnlyList<ContentParser> Declare() =>
    [
        new(typeof(UnitTypeTable), RepoLayout.UnitsFile, true, text => UnitTypeTable.Parse(text).ContentHash),
        new(
            typeof(UpgradeLadder),
            RepoLayout.UpgradesFile,
            true,
            text => UpgradeLadder.Parse(text, Types()).ContentHash),
        new(typeof(WaveScript), RepoLayout.WaveFile, true, text => Fold(WaveScript.Parse(text, Types()))),
        new(typeof(WaveScript), RepoLayout.FieldFile, true, text => Fold(WaveScript.Parse(text, Types()))),
        new(typeof(TowerLayout), RepoLayout.DefenseFile, true, text => Fold(TowerLayout.Parse(text, Types()))),
        new(typeof(Ruleset), RepoLayout.RulesetFile, true, text => Ruleset.Parse(text).ContentHash),
        new(
            typeof(AnchorSchedule),
            RepoLayout.ScheduleFile,
            true,
            text => AnchorSchedule.Parse(text, Types()).ContentHash),
        new(typeof(CommandScript), RepoLayout.CommandScriptFile, true, text => Fold(CommandScript.Parse(text))),
        new(typeof(HexMap), RepoLayout.MapFile, false, text => HexMap.Parse(text).MapHash),
        new(typeof(GoldenTrace), RepoLayout.GoldenTraceFile, false, text => Fold(GoldenTrace.Parse(text))),
    ];

    private static Hash64 Fold(WaveScript wave)
    {
        Hash64 hash = Hash64.Start("wave-sweep").Add(wave.Count).Add(wave.TotalUnits);

        for (int index = 0; index < wave.Count; index++)
        {
            UnitOrder order = wave.Orders[index];
            hash = hash.Add(order.TickOffset).Add(order.TypeId).Add(order.Count).Add(order.Corridor);
        }

        return hash;
    }

    private static Hash64 Fold(TowerLayout defense)
    {
        Hash64 hash = Hash64.Start("defense-sweep").Add(defense.Count);

        for (int index = 0; index < defense.Count; index++)
        {
            PlacedTower tower = defense.Towers[index];
            hash = hash.Add(tower.Type.Id).Add(tower.Column).Add(tower.Row).Add(tower.Hex.Q).Add(tower.Hex.R);
        }

        return hash;
    }

    private static Hash64 Fold(IReadOnlyList<RecordCommand> commands)
    {
        Hash64 hash = Hash64.Start("command-script-sweep").Add(commands.Count);

        for (int index = 0; index < commands.Count; index++)
        {
            RecordCommand command = commands[index];
            hash = hash.Add(command.Wave).Add((int)command.Take).Add(command.TakeId).Add(command.Slots.Count);

            for (int slot = 0; slot < command.Slots.Count; slot++)
            {
                hash = hash.Add(command.Slots[slot].TypeId).Add(command.Slots[slot].Count);
            }
        }

        return hash;
    }

    private static Hash64 Fold(GoldenTrace trace)
    {
        Hash64 hash = Hash64.Start("golden-trace-sweep").Add(trace.Count);

        for (int tick = 0; tick < trace.Count; tick++)
        {
            hash = hash.Add(unchecked((long)trace.At(tick).Value));
        }

        return hash;
    }
}
