namespace Sim
{
    /// <summary>
    /// A record's id: the hash of the record's own bytes. Derived, never stored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>An id is not a field, and this class is not a property on a record for
    /// that reason.</b> Storing an id inside the thing it identifies creates two
    /// values that must agree and can therefore disagree, and it makes "this wave
    /// goes with this defense" a claim in a field rather than a fact about the
    /// bytes. Computed instead, the claim cannot be faked -- not by a filename,
    /// not by an envelope, not by an editor.
    /// </para>
    /// <para>
    /// <b>It is a function of bytes, not of a record, and the difference bites
    /// exactly once.</b> There is one writer, which emits only the current
    /// format, so re-writing a record that was read from an older format version
    /// legitimately produces different bytes and therefore a different id. That
    /// is correct -- they are different bytes -- and it is why the id is taken
    /// over the bytes you have rather than by asking a parsed record what it
    /// thinks it is called.
    /// </para>
    /// <para>
    /// Canonical array order is what makes the whole arrangement mean anything:
    /// two identical defenses have identical bytes, so they have identical ids,
    /// so content-addressing works. Sorting on load instead of asserting order
    /// would have left identical defenses with different bytes and quietly turned
    /// every id into a hash of somebody's typing order.
    /// </para>
    /// </remarks>
    public static class RecordId
    {
        /// <summary>
        /// Names the fold. The digit bumps if what an id is computed over ever
        /// changes, which would retire every stored reference to one.
        /// </summary>
        private const string HashLabel = "record-id/1";

        /// <summary>The id of these bytes.</summary>
        public static Hash64 Of(byte[] bytes) => Of(bytes, 0, bytes.Length);

        /// <summary>The id of a range of bytes -- an inner record inside a bundle.</summary>
        public static Hash64 Of(byte[] bytes, int start, int count) =>
            Hash64.Start(HashLabel).Add(bytes, start, count);
    }
}
