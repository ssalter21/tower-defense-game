using System;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One thing a record wrote down about the world it was made in, beside the
    /// same thing about the world in front of it now.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A stamp is a declaration and not a comparison.</b> A record kind names
    /// the pairs it carries and <see cref="ReplayGate"/> does the comparing, so
    /// what a kind checks is a list somebody can read against another kind's
    /// list.
    /// </para>
    /// <para>
    /// <b>One noun spells the gate and both values.</b> The noun is what a
    /// person calls the thing -- <c>content</c>, <c>ruleset</c>, <c>map</c> --
    /// and a value reads as <c>content B58DBED2315303D2</c>. A digest's gate is
    /// that noun and the word hash, because "the content hash gate failed" is
    /// what a refusal says.
    /// </para>
    /// <para>
    /// <b>A record carrying no value for a stamp agrees with nothing.</b> What
    /// is missing is the record's claim, and a missing claim matches no live
    /// value there is -- so the row reads "no ruleset stamp" and disagrees with
    /// every ruleset there is. See
    /// <c>docs/adr/0047-a-bundle-stamps-its-ruleset.md</c>.
    /// </para>
    /// </remarks>
    public readonly struct Stamp
    {
        private Stamp(string gate, string recorded, string live, bool agrees)
        {
            Gate = gate;
            Recorded = recorded;
            Live = live;
            Agrees = agrees;
        }

        /// <summary>Which replay gate this row is, named for a person.</summary>
        public string Gate { get; }

        /// <summary>What the record says, rendered for a person.</summary>
        public string Recorded { get; }

        /// <summary>What is actually in front of it, rendered the same way.</summary>
        public string Live { get; }

        /// <summary>Whether the record's value is the live one.</summary>
        public bool Agrees { get; }

        /// <summary>
        /// A counter the record carries and this build has its own value for:
        /// the simulation version, and nothing else so far.
        /// </summary>
        public static Stamp Of(string noun, uint recorded, uint live) =>
            new Stamp(
                noun,
                noun + " " + recorded.ToString(CultureInfo.InvariantCulture),
                noun + " " + live.ToString(CultureInfo.InvariantCulture),
                recorded == live);

        /// <summary>
        /// A digest the record carries, or none where the record does not say.
        /// </summary>
        public static Stamp Of(string noun, Hash64? recorded, Hash64 live) =>
            new Stamp(noun + " hash", Spell(noun, recorded), Spell(noun, live), recorded == live);

        /// <summary>
        /// A digest stamp as a person reads it, and what a record carrying none
        /// says instead. One spelling, used by the gate that refuses a record
        /// and by the restaging that runs one anyway, so the two cannot
        /// describe the same absence differently.
        /// </summary>
        public static string Spell(string noun, Hash64? value) =>
            value is null ? "no " + noun + " stamp" : noun + " " + value.Value.ToString();
    }

    /// <summary>
    /// The replay gate: the stamps a record stored, the live values to compare
    /// them against, and a refusal naming the first pair that disagrees.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One walk, declared per record kind.</b> A kind hands over the rows it
    /// carries, so what it compares is a list rather than a run of branches --
    /// and a stamp it does not compare is a gap in a list two kinds can be read
    /// side by side, rather than a branch that is not there. The reasoning is
    /// <c>docs/adr/0014-reading-and-replaying-are-separate-gates.md</c>.
    /// </para>
    /// <para>
    /// <b>Nothing here defaults, skips or substitutes.</b> A stamp a record
    /// does not carry refuses rather than passing, which is what retires a
    /// bundle that names no ruleset.
    /// </para>
    /// <para>
    /// <b>This is not the read gate.</b> Reading needs a known format version
    /// and refuses with a <see cref="RecordException"/>; this refuses a record
    /// that read perfectly well with a <see cref="RetiredRecordException"/>,
    /// and the two share no base, so no <c>catch</c> can treat "these bytes are
    /// not a record" and "this record is historical" as one thing.
    /// </para>
    /// </remarks>
    public static class ReplayGate
    {
        /// <summary>
        /// Walks the declared stamps and refuses on the first that disagrees.
        /// </summary>
        /// <remarks>
        /// The order is the record kind's, so a record failing several rows is
        /// named by the one its kind declared first rather than by whichever
        /// comparison happened to run.
        /// </remarks>
        public static void Require(params Stamp[] stamps)
        {
            if (Disagreeing(stamps, out Stamp refused))
            {
                throw new RetiredRecordException(refused.Gate, refused.Recorded, refused.Live);
            }
        }

        /// <summary>
        /// Whether every declared stamp agrees, for a caller labelling a result
        /// rather than refusing one. The same walk, so a label and a refusal
        /// cannot come to different answers about one pair.
        /// </summary>
        public static bool Agree(params Stamp[] stamps) => !Disagreeing(stamps, out _);

        private static bool Disagreeing(Stamp[] stamps, out Stamp refused)
        {
            if (stamps is null)
            {
                throw new ArgumentNullException(nameof(stamps));
            }

            for (int index = 0; index < stamps.Length; index++)
            {
                if (!stamps[index].Agrees)
                {
                    refused = stamps[index];
                    return true;
                }
            }

            refused = default;
            return false;
        }
    }
}
