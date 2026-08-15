using Sim;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// The one place a row of <c>content/units.txt</c> becomes a picture on the
    /// screen. There are no pictures yet, so it is a seam and an empty one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Nothing in this repository knows what a unit looks like at thumbnail
    /// size.</b> Both surfaces that list units ask for one — the tower palette
    /// (#196) and the wave bar (#197) — and neither can be given one: there is
    /// no per-unit icon committed anywhere, <see cref="UnitArt"/> carries a
    /// model and its clips and no portrait field, and no facility exists that
    /// renders a model into a texture. Both of the ways to close that are art
    /// decisions, which belong to the developer and not to whoever is writing
    /// the screen.
    /// </para>
    /// <para>
    /// <b>So a unit is its name until somebody says otherwise, and no glyph
    /// stands in for one.</b> A letter, an emoji or a coloured square would be
    /// art chosen by the person who happened to be laying out a bar, it would
    /// read as a decision, and it would have to be found and removed in two
    /// places once a real answer arrived. An empty seam has to be filled before
    /// it shows anything, which is the behaviour that keeps the question open
    /// rather than quietly answering it.
    /// </para>
    /// <para>
    /// <b>One seam and not two, so the answer lands once.</b> Whatever a
    /// thumbnail turns out to be — a sprite atlas keyed by unit id, a render
    /// texture per model baked by a tool in <c>tools/</c> — it is built here,
    /// and every list that shows units gets it by having called this all along.
    /// The two callers already do; they draw what comes back where the picture
    /// goes and lay out without one while nothing does.
    /// </para>
    /// <para>
    /// <b>What closing it needs.</b> Either a committed image per live row of
    /// <c>content/units.txt</c> — nine of them today, addressed by type id so a
    /// retired row cannot collide with a live one — or the decision that a
    /// thumbnail is a rendered model, which needs a camera, a pose and a
    /// framing chosen per unit and a static entry point under <c>tools/</c> to
    /// bake them, because nothing here may depend on an editor session. Either
    /// way it is a look, and looks are signed off. See <c>docs/roster.md</c>,
    /// whose <c>Looks</c> line is where each unit's art direction is written
    /// down.
    /// </para>
    /// </remarks>
    public static class RosterThumbnails
    {
        /// <summary>
        /// The picture of a unit, ready to be put in a box or on an entry — or
        /// null while this project has none.
        /// </summary>
        /// <remarks>
        /// Null rather than an empty element, so a caller lays out as though
        /// the picture were not there at all rather than reserving a hole for
        /// it. A bar with a column of blank squares down it is a bar that looks
        /// broken; a bar of names looks like a bar of names.
        /// </remarks>
        public static VisualElement Of(UnitType type)
        {
            // Deliberately unconditional. There is nothing to look up, because
            // there is nothing anywhere to look up — see the remarks above, and
            // do not fill this in with a placeholder.
            return null;
        }
    }
}
