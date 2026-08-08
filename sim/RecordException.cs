using System;

namespace Sim
{
    /// <summary>
    /// Thrown when a stored record cannot be read: wrong magic, an unknown
    /// format version, a truncation, an array out of canonical order, or a
    /// bundle that contradicts itself. Shares no base with
    /// <see cref="RetiredRecordException"/>, so neither can be caught as the
    /// other. See <c>docs/adr/0013-record-reading-is-an-all-or-nothing-gate.md</c>.
    /// </summary>
    public sealed class RecordException : Exception
    {
        public RecordException(string record, string message)
            : base(record + ": " + message)
        {
            Record = record;
        }

        /// <summary>What the bytes were called. Never a file path.</summary>
        public string Record { get; }
    }

    /// <summary>
    /// Thrown when a record that read perfectly well cannot be replayed, because
    /// the ruleset, the numbers or the geometry it was made under are not the
    /// ones in front of it. The record remains readable and can still be listed,
    /// drawn and shown as historical.
    /// See <c>docs/adr/0014-reading-and-replaying-are-separate-gates.md</c>.
    /// </summary>
    public sealed class RetiredRecordException : Exception
    {
        public RetiredRecordException(string gate, string recorded, string live)
            : base(Describe(gate, recorded, live))
        {
            Gate = gate;
            Recorded = recorded;
            Live = live;
        }

        /// <summary>Which replay gate refused, named for a person.</summary>
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
