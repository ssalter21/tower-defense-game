namespace Sim
{
    /// <summary>
    /// A record's id: the hash of the record's own bytes. Computed from the
    /// bytes in hand, never stored inside the record it identifies.
    /// </summary>
    public static class RecordId
    {
        /// <summary>
        /// Names the fold. The digit is part of the hash input, so changing it
        /// gives the same bytes a different id.
        /// </summary>
        private const string HashLabel = "record-id/1";

        /// <summary>The id of these bytes.</summary>
        public static Hash64 Of(byte[] bytes) => Of(bytes, 0, bytes.Length);

        /// <summary>The id of a range of bytes -- an inner record inside a bundle.</summary>
        public static Hash64 Of(byte[] bytes, int start, int count) =>
            Hash64.Start(HashLabel).Add(bytes, start, count);
    }
}
