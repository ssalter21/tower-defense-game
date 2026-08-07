using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sim
{
    /// <summary>
    /// One anchor's menu: the anchor, and the game changers this run drew onto
    /// it.
    /// </summary>
    /// <remarks>
    /// The menu is merged into that round's ordinary offering and one thing is
    /// taken from the whole of it, so a game changer competes head to head with
    /// an ordinary unlock. What is public is the shape; who took what is not.
    /// </remarks>
    public sealed class AnchorMenu
    {
        private readonly GameChanger[] _changers;

        internal AnchorMenu(Anchor anchor, GameChanger[] changers)
        {
            Anchor = anchor;
            _changers = changers;
        }

        /// <summary>The anchor this menu belongs to.</summary>
        public Anchor Anchor { get; }

        /// <summary>The game changers drawn onto it, in the order they were drawn.</summary>
        public IReadOnlyList<GameChanger> GameChangers => _changers;

        public override string ToString() =>
            "wave "
            + Anchor.Wave.ToString(CultureInfo.InvariantCulture)
            + ": "
            + string.Join(", ", Array.ConvertAll(_changers, changer => changer.ToString()));
    }

    /// <summary>
    /// One run's filling: which game changers sit on each anchor's menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Drawn once, at run start, and revealed there.</b> The shape is fixed
    /// per rotation so that preparation is learnable; the filling moves per run
    /// so that a map fixed for a week does not go stale in it. One layer has to
    /// move or the week is solved by Tuesday, and both moving leaves nothing to
    /// prepare against.
    /// </para>
    /// <para>
    /// <b>Nothing else in a run is keyed on it.</b> A field, and later a ghost
    /// pool, are drawn on the run and the stage alone: sharding a pool by which
    /// filling a run got would pay for variance with a thinner pool, and
    /// rotation taxes that quite enough already.
    /// </para>
    /// </remarks>
    public sealed class AnchorFilling
    {
        private readonly AnchorMenu[] _menus;

        internal AnchorFilling(AnchorMenu[] menus)
        {
            _menus = menus;
        }

        /// <summary>The menus, in wave order.</summary>
        public IReadOnlyList<AnchorMenu> Menus => _menus;

        /// <summary>How many anchors this run has.</summary>
        public int Count => _menus.Length;

        /// <summary>Whether this wave is an anchor.</summary>
        public bool IsAnchor(int wave) => TryAt(wave, out AnchorMenu? _);

        /// <summary>The menu that stands at this wave.</summary>
        /// <exception cref="SimulationException">That wave is not an anchor.</exception>
        public AnchorMenu At(int wave)
        {
            if (TryAt(wave, out AnchorMenu? menu))
            {
                return menu!;
            }

            throw new SimulationException(
                "Wave "
                + wave.ToString(CultureInfo.InvariantCulture)
                + " is not an anchor of this shape, so it has no menu of game changers. An ordinary round "
                + "offers ordinary options and nothing else, and asking for a menu it does not have is a "
                + "refusal rather than an empty list nobody notices.");
        }

        /// <summary>The menu that stands at this wave, if there is one.</summary>
        public bool TryAt(int wave, out AnchorMenu? menu)
        {
            for (int index = 0; index < _menus.Length; index++)
            {
                if (_menus[index].Anchor.Wave == wave)
                {
                    menu = _menus[index];
                    return true;
                }
            }

            menu = null;
            return false;
        }
    }
}
