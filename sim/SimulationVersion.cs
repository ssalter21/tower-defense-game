namespace Sim
{
    /// <summary>
    /// The version of the simulation's behaviour. One number, owned by nothing
    /// else, and the only one of the record's three identity fields a person is
    /// ever expected to change by hand.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Three identity fields own three non-overlapping things, and this one
    /// owns behaviour.</b> The point of separating them is that somebody about
    /// to change something can tell which one is theirs without having to
    /// understand the other two:
    /// </para>
    /// <list type="table">
    /// <item>
    /// <term><see cref="RecordFormat"/> version</term>
    /// <description>Owns <b>layout</b> -- where the bytes are. Bumped when a
    /// field is added, moved or widened. Counted per record kind.</description>
    /// </item>
    /// <item>
    /// <term><see cref="Current"/>, this number</term>
    /// <description>Owns <b>behaviour</b> -- tick order, targeting, the rounding
    /// rule, the dice algorithm. Bumped when any rule changes.</description>
    /// </item>
    /// <item>
    /// <term><see cref="UnitTypeTable.ContentHash"/></term>
    /// <description>Owns <b>the numbers</b>. Recomputed at load from the parsed
    /// tables, and never touched by hand at all.</description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>Changing rounding is a change to this number even though no number in
    /// any content file moved.</b> That is the sentence this constant exists to
    /// carry, because it is the case where reaching for the wrong field is most
    /// tempting: nothing in <c>units.txt</c> changed, so the content hash does
    /// not move, and nothing about the byte layout changed, so the format version
    /// does not move -- and every stored replay now produces a different result.
    /// Truncating where the sim used to round, dividing in a different order,
    /// swapping the tie-break in target selection, reordering the phases of a
    /// tick: all of them are this.
    /// </para>
    /// <para>
    /// The converse is worth saying too, because the mistake runs both ways:
    /// retuning a tower's damage is <i>not</i> this. The content hash already
    /// covers it, automatically, and bumping this as well would retire every
    /// record made under an unchanged ruleset for no reason.
    /// </para>
    /// </remarks>
    public static class SimulationVersion
    {
        /// <summary>
        /// The behaviour this build implements. Bump on any rule change; see the
        /// remarks on <see cref="SimulationVersion"/> for what counts as one.
        /// </summary>
        public const uint Current = 1;
    }
}
