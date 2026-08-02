using System;

namespace Sim
{
    /// <summary>
    /// Thrown when a stored record cannot be read: wrong magic, a format version
    /// this reader does not know, a truncation, an array out of canonical order,
    /// or a bundle that contradicts itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the read gate, and it is the hard one.</b> There is no partial
    /// read, no best-effort read and no "skip the field I did not recognise".
    /// The record format has fixed-width fields with no length prefixes to skip
    /// by, so a reader that met something it did not understand would not be
    /// tolerating an unknown field -- it would be reading the next field at the
    /// wrong offset and returning a defense made of noise that still validates.
    /// </para>
    /// <para>
    /// <b>It is deliberately unrelated to <see cref="RetiredRecordException"/>.</b>
    /// Neither derives from the other and both derive straight from
    /// <see cref="Exception"/>, so no <c>catch</c> can accidentally treat "these
    /// bytes are not a record" and "this record is from an older ruleset" as one
    /// thing. They are the two gates, and the whole reason there are two is that
    /// the second one leaves the record perfectly readable.
    /// </para>
    /// </remarks>
    public sealed class RecordException : Exception
    {
        public RecordException(string record, string message)
            : base(record + ": " + message)
        {
            Record = record;
        }

        /// <summary>
        /// What the bytes were called, for the message. Never a file path: this
        /// assembly is handed bytes and does not know where they came from.
        /// </summary>
        public string Record { get; }
    }

    /// <summary>
    /// Thrown when a record that read perfectly well cannot be replayed, because
    /// the ruleset, the numbers or the geometry it was made under are not the
    /// ones in front of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The record is still readable, and that is the point of having two
    /// gates rather than one.</b> A defense whose simulation version has moved on
    /// can still be listed, drawn, and shown as historical; it simply may not be
    /// simulated. Collapsing the two gates would make every balance patch turn
    /// stored defenses into files nobody can open.
    /// </para>
    /// <para>
    /// Every one of these names <b>which gate failed and both values</b>. "This
    /// record is incompatible" is a message that sends somebody to a debugger;
    /// "the content hash gate failed -- the record says 3F2A... and this ruleset is
    /// 91C4..." is a message they can act on without one.
    /// </para>
    /// </remarks>
    public sealed class RetiredRecordException : Exception
    {
        public RetiredRecordException(string gate, string recorded, string live)
            : base(Describe(gate, recorded, live))
        {
            Gate = gate;
            Recorded = recorded;
            Live = live;
        }

        /// <summary>Which of the three replay gates refused: see the message.</summary>
        public string Gate { get; }

        /// <summary>What the record says, rendered for a person.</summary>
        public string Recorded { get; }

        /// <summary>What is actually in front of it, rendered the same way.</summary>
        public string Live { get; }

        private static string Describe(string gate, string recorded, string live) =>
            "This record will not replay: the "
            + gate
            + " gate failed. The record says "
            + recorded
            + " and this run has "
            + live
            + ". The record is still readable and can be shown as historical -- refusing to simulate it "
            + "is not the same as refusing to open it, and replaying it under today's numbers would be a "
            + "different operation returning a differently labelled result.";
    }
}
