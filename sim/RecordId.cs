namespace Sim
{
    /// <summary>
    /// A record's id: the hash of the record's own bytes, computed from the
    /// bytes in hand rather than stored inside the record.
    /// See <c>docs/adr/0030-record-ids-are-content-addressed.md</c>.
    /// </summary>
    public static class RecordId
    {
        // Part of the hash input, so changing it gives the same bytes a different id.
        private const string HashLabel = "record-id/1";

        public static Hash64 Of(byte[] bytes) => Of(bytes, 0, bytes.Length);

        /// <summary>The id of a range of bytes -- an inner record inside a bundle.</summary>
        public static Hash64 Of(byte[] bytes, int start, int count) =>
            Hash64.Start(HashLabel).Add(bytes, start, count);
    }
}
