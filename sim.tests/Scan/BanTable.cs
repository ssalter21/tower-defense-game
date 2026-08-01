using System.Collections.Immutable;

namespace Sim.Tests.Scan;

/// <summary>What each of the seven rows forbids, as data the scanner reads.</summary>
/// <remarks>
/// <para>
/// Everything here is a metadata reference the compiled assembly makes, which
/// is the level the rule has to be enforced at. A source-level check would be
/// enforcing the rule on the text someone last read rather than on the image
/// that ships, and the two differ the moment a rebuild is forgotten.
/// </para>
/// <para>
/// The banned-API analyzer that was once planned for this job is deliberately
/// not used anywhere in this project. It catches four of these seven rows and
/// reports nothing at all about the other three, so a build that passed it
/// would look like a build that had been checked. A check that silently is not
/// running is worse than no check, because it converts an unenforced rule into
/// a believed one.
/// </para>
/// </remarks>
public static class BanTable
{
    /// <summary>The scanner's clauses. Each is a separate piece of scanning code.</summary>
    public const string ClauseFloatSignature = "floats/signature";

    public const string ClauseFloatLocal = "floats/local";

    public const string ClauseFloatInstruction = "floats/instruction";

    public const string ClauseBannedType = "banned-type-reference";

    public const string ClauseBannedMember = "banned-member-reference";

    /// <summary>Every clause the scanner can report. The poison suite asserts all of them fire.</summary>
    public static readonly ImmutableArray<string> AllClauses = ImmutableArray.Create(
        ClauseFloatSignature,
        ClauseFloatLocal,
        ClauseFloatInstruction,
        ClauseBannedType,
        ClauseBannedMember);

    /// <summary>Types the simulation may not reference at all, by exact metadata name.</summary>
    public static readonly ImmutableDictionary<string, BanRow> BannedTypes =
        ImmutableDictionary.CreateRange(new[]
        {
            // Math: transcendentals are explicitly permitted to differ between
            // implementations, and the integer overloads are not worth keeping
            // the door open for the rest.
            Entry("System.Math", BanRow.Math),
            Entry("System.MathF", BanRow.Math),

            // Hashed collections: enumeration order is an implementation
            // detail, and it has changed between runtimes before.
            Entry("System.Collections.Generic.Dictionary`2", BanRow.HashedCollections),
            Entry("System.Collections.Generic.HashSet`1", BanRow.HashedCollections),
            Entry("System.Collections.Hashtable", BanRow.HashedCollections),

            // Ambient time and randomness. Everything the simulation is
            // allowed to not-know arrives through the record.
            Entry("System.Random", BanRow.AmbientTimeAndRandomness),
            Entry("System.DateTime", BanRow.AmbientTimeAndRandomness),
            Entry("System.DateTimeOffset", BanRow.AmbientTimeAndRandomness),
            Entry("System.TimeZoneInfo", BanRow.AmbientTimeAndRandomness),
            Entry("System.Diagnostics.Stopwatch", BanRow.AmbientTimeAndRandomness),

            // Conditional-compilation diagnostics. Note what is NOT here:
            // System.Diagnostics.DebuggableAttribute, which every Debug build
            // carries at assembly level and which is the reason this row is
            // spelled with exact type names rather than a namespace prefix.
            Entry("System.Diagnostics.Debug", BanRow.ConditionalDiagnostics),
            Entry("System.Diagnostics.Trace", BanRow.ConditionalDiagnostics),
            Entry("System.Diagnostics.ConditionalAttribute", BanRow.ConditionalDiagnostics),
        });

    /// <summary>
    /// Namespaces the simulation may not reach into. Prefix-matched, because
    /// the whole area is out of bounds rather than a listed set of types.
    /// </summary>
    public static readonly ImmutableDictionary<string, BanRow> BannedNamespacePrefixes =
        ImmutableDictionary.CreateRange(new[]
        {
            Entry("System.Threading.", BanRow.Threading),
            Entry("System.Security.Cryptography.", BanRow.AmbientTimeAndRandomness),
        });

    /// <summary>
    /// Members banned on types that are otherwise fine. Keyed
    /// <c>Namespace.Type::Member</c>; property accessors appear in metadata
    /// under their <c>get_</c> and <c>set_</c> names, so that is how they are
    /// written here.
    /// </summary>
    public static readonly ImmutableDictionary<string, BanRow> BannedMembers =
        ImmutableDictionary.CreateRange(new[]
        {
            // Unstable sorts. Both are documented as not preserving the order
            // of equal elements, which turns every tie in the simulation into
            // a coin flip the replay cannot reproduce. Sorting in this project
            // is done with an explicit total order and a stable algorithm.
            Entry("System.Array::Sort", BanRow.UnstableSorts),
            Entry("System.Collections.Generic.List`1::Sort", BanRow.UnstableSorts),

            // Ambient time that lives on types with legitimate other uses.
            Entry("System.Environment::get_TickCount", BanRow.AmbientTimeAndRandomness),
            Entry("System.Environment::get_TickCount64", BanRow.AmbientTimeAndRandomness),
            Entry("System.Guid::NewGuid", BanRow.AmbientTimeAndRandomness),
        });

    private static KeyValuePair<string, BanRow> Entry(string key, BanRow row) => new(key, row);
}
