namespace Sim.Tests.Scan;

/// <summary>
/// The seven banned rows. Each is a rule the simulation cannot break without
/// the compiled artefact showing it, and each has exactly one deliberate
/// violation in sim.poison so that the clause enforcing it is known to work.
/// </summary>
public enum BanRow
{
    /// <summary>
    /// Floating point, anywhere: signatures, local slots, instruction streams.
    /// C# does not promise that a float expression is evaluated at the
    /// precision of its declared type, so a float result is a property of the
    /// machine rather than of the program.
    /// </summary>
    Floats,

    /// <summary>
    /// <c>System.Math</c> and <c>System.MathF</c>. Their transcendental
    /// functions are explicitly permitted to differ between implementations,
    /// and the integer overloads are not worth the hole the type would open.
    /// </summary>
    Math,

    /// <summary>
    /// <c>Dictionary</c>, <c>HashSet</c> and friends. Their enumeration order
    /// is an implementation detail that has changed between runtimes, so a
    /// loop over one is a loop whose order the record does not pin.
    /// </summary>
    HashedCollections,

    /// <summary>
    /// Unstable sorts. <c>Array.Sort</c> and <c>List&lt;T&gt;.Sort</c> are
    /// documented as not preserving the order of equal elements, which makes
    /// every tie in the sim a coin flip the replay cannot reproduce.
    /// </summary>
    UnstableSorts,

    /// <summary>
    /// Ambient time and randomness: the clock, the tick count, the stopwatch,
    /// <c>System.Random</c>, <c>Guid.NewGuid</c>, hardware entropy. Everything
    /// the simulation is allowed to not-know comes in through the record.
    /// </summary>
    AmbientTimeAndRandomness,

    /// <summary>
    /// Threading. Interleaving is a source of nondeterminism no seed can pin,
    /// and the tick loop has no reason to want one.
    /// </summary>
    Threading,

    /// <summary>
    /// Conditional-compilation diagnostics: <c>Debug</c>, <c>Trace</c>,
    /// <c>[Conditional]</c>. An invariant that disappears from the shipping
    /// build is an unenforced rule that has been promoted to a believed one.
    /// This row is only catchable in IL because Debug is the committed
    /// configuration and the calls are therefore actually emitted.
    /// </summary>
    ConditionalDiagnostics,
}
