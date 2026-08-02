namespace Sim.Tests.Scan;

/// <summary>One banned construct, found in one place in one assembly.</summary>
/// <param name="Row">Which of the eight rules was broken.</param>
/// <param name="Clause">
/// Which clause of the scan noticed. A row can have more than one clause --
/// floating point has three, because a float can enter through a signature, a
/// local slot or an instruction and each is a separate piece of code in the
/// scanner. Findings carry the clause so the poison suite can assert that
/// every clause fires, rather than only that every row does; otherwise a
/// broken clause hides behind a working one on the same row.
/// </param>
/// <param name="Site">Where it was found: a type, a member, or a method body.</param>
/// <param name="Detail">What exactly was found there.</param>
public sealed record BanFinding(BanRow Row, string Clause, string Site, string Detail)
{
    public override string ToString() => $"{Row} [{Clause}] at {Site}: {Detail}";
}
