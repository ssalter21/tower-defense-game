using System;
using System.Collections.Generic;
using System.Globalization;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// What may be bought: a row of towers along the bottom of the screen, and
    /// — when a placed tower is clicked — the rungs above it, drawn at the hex
    /// it stands on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two surfaces because there are two verbs, and they name their cell
    /// differently.</b> A <c>place</c> names a tower first and a hex second, so
    /// it is a palette you choose from and then point with. An <c>upgrade</c>
    /// names the cell — it is the placement standing there that decides which
    /// rungs exist at all — so it is offered where that placement is and nowhere
    /// else. Putting the ladder in the bottom bar too would have meant a list
    /// that changes meaning depending on what was last clicked.
    /// </para>
    /// <para>
    /// <b>Nothing here decides what is legal.</b> Every entry is priced and
    /// every rung is offered by asking <see cref="ComposedRound"/>, which asks
    /// <see cref="BuildPhase.Resolve"/>. This file lays out an answer it did not
    /// compute — see ADR-0051, and <see cref="ComposedRound.Allows"/>.
    /// </para>
    /// <para>
    /// <b>An unaffordable tower stays on the bar and turns its price red.</b>
    /// Removing it would make the roster look like it shrank, and greying the
    /// entry would be prevention without explanation — the thing ADR-0051
    /// rejected the grey commit button for. The price is the reason, so the
    /// price is what changes. Selecting one is still allowed; what happens then
    /// is that no hex lights, which is the same rule everywhere else on screen.
    /// </para>
    /// <para>
    /// <b>Built in code on a panel of its own, like the playback bar.</b> Same
    /// reasoning as <see cref="PlaybackControls"/>: chrome dragged into a scene
    /// is chrome in serialized YAML that no diff can be read. It carries a
    /// second <see cref="PanelSettings"/> rather than sharing that bar's,
    /// because the two are built independently and #198 is where the whole HUD
    /// becomes one header; the sorting order below is what keeps the offer over
    /// the bar until then.
    /// </para>
    /// <para>
    /// <b>No type ids and no record vocabulary.</b> A tower is its name and its
    /// price — see <see cref="RosterNames"/>. The digit on an entry is the key
    /// that selects it and nothing else.
    /// </para>
    /// <para>
    /// <b>Named and not pictured.</b> An entry asks
    /// <see cref="RosterThumbnails"/> for a picture of the tower and lays out
    /// without one while there is none, which there is: no per-unit image is
    /// committed anywhere and choosing or baking one is an art decision. That
    /// seam is shared with <see cref="WaveBar"/>, so whichever answer arrives
    /// lands once rather than twice.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class TowerPalette : MonoBehaviour
    {
        /// <summary>
        /// How many entries the number row can reach. The keys are <c>1</c> to
        /// <c>9</c>; a tenth tower would be clickable and have no shortcut.
        /// </summary>
        public const int ShortcutCount = 9;

        /// <summary>
        /// Drawn after the playback bar's panel, so the offer at a hex is over
        /// the scrub bar rather than behind it when a tower stands low on the
        /// screen.
        /// </summary>
        private const int PanelSortingOrder = 1;

        /// <summary>
        /// How tall the bar is. Public because the bars are stacked: the wave
        /// row above it is anchored to the top of this one, and a copy of the
        /// number over there would come apart the first time this one changed.
        /// </summary>
        public const float BarHeight = 104f;

        private const float EntryWidth = 208f;

        private const float EntryHeight = 76f;

        private const float OfferWidth = 208f;

        private const float RungHeight = 40f;

        private const float OfferAnchorHeight = 2.2f;

        private const int NameSize = 22;

        private const int PriceSize = 18;

        /// <summary>What the chosen entry is drawn in.</summary>
        private static readonly Color ChosenColor = new Color(0.45f, 0.68f, 0.85f, 1f);

        /// <summary>What a price the purse can cover reads in.</summary>
        private static readonly Color QuietColor = new Color(0.68f, 0.72f, 0.78f, 1f);

        /// <summary>What a price reads as when the purse cannot cover it.</summary>
        private static readonly Color UnaffordableColor = new Color(0.93f, 0.42f, 0.38f, 1f);

        private readonly List<Entry> _entries = new List<Entry>();

        private readonly List<Button> _buttons = new List<Button>();

        private readonly List<Button> _rungs = new List<Button>();

        /// <summary>The rungs the open offer is showing, as unit types.</summary>
        private readonly List<UnitType> _offered = new List<UnitType>();

        private ComposedRound _round;

        private Camera _camera;

        private PanelSettings _panel;

        private VisualElement _offer;

        private bool _offering;

        private int _offerColumn;

        private int _offerRow;

        /// <summary>The tower a click on a hex would place, or null.</summary>
        public UnitType Selected { get; private set; }

        /// <summary>The panel the bar is drawn on.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>Every entry's button, in the order they are laid out.</summary>
        public IReadOnlyList<Button> Entries => _buttons;

        /// <summary>The rungs the open offer is showing, in the order it shows them.</summary>
        public IReadOnlyList<Button> Rungs => _rungs;

        /// <summary>Whether a placed tower's ladder is on screen.</summary>
        public bool IsOffering => _offering;

        /// <summary>The cell the open offer belongs to.</summary>
        public int OfferColumn => _offerColumn;

        /// <summary>The cell the open offer belongs to.</summary>
        public int OfferRow => _offerRow;

        /// <summary>
        /// Raised when a rung of an open offer is clicked: the cell, then what
        /// to upgrade it into.
        /// </summary>
        /// <remarks>
        /// The palette does not apply it. One place writes to the composed
        /// round — <see cref="BuildInput"/> — so that what redraws the board
        /// after a change cannot be somewhere the change did not go through.
        /// </remarks>
        public event Action<int, int, UnitType> UpgradeChosen;

        /// <summary>
        /// Builds the palette under <paramref name="parent"/>, listing what
        /// <paramref name="round"/> may build and drawing its offers where
        /// <paramref name="camera"/> sees them.
        /// </summary>
        public static TowerPalette Build(Transform parent, ComposedRound round, Camera camera)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (round == null) throw new ArgumentNullException(nameof(round));

            var host = new GameObject("TowerPalette");
            host.transform.SetParent(parent, worldPositionStays: false);

            var palette = host.AddComponent<TowerPalette>();
            palette.Assemble(host.AddComponent<UIDocument>(), round, camera);

            return palette;
        }

        /// <summary>
        /// Chooses what a click on a hex would place. Null clears the choice.
        /// Choosing the tower already chosen clears it too, so the same key or
        /// the same button puts the pointer back to doing nothing.
        /// </summary>
        public void Select(UnitType type)
        {
            Selected = ReferenceEquals(type, Selected) ? null : type;

            Follow();
        }

        /// <summary>
        /// Chooses the entry at a place in the row, counted from zero — what a
        /// number key does. Out of range clears nothing and chooses nothing.
        /// </summary>
        public void SelectAt(int index)
        {
            if (index < 0 || index >= _entries.Count)
            {
                return;
            }

            Select(_entries[index].Type);
        }

        /// <summary>
        /// Opens the ladder of the tower standing on a cell. Closes again where
        /// nothing stands there, or where nothing it could become is affordable
        /// — a tower with no upgrade to offer offers none.
        /// </summary>
        public void Offer(int column, int row)
        {
            IReadOnlyList<UnitType> rungs = _round.UpgradesOn(column, row);

            if (rungs.Count == 0)
            {
                CloseOffer();

                return;
            }

            _offering = true;
            _offerColumn = column;
            _offerRow = row;

            DrawOffer(rungs);
            Follow();
        }

        /// <summary>
        /// Takes one rung of the open offer: what its button does, and the one
        /// way the ladder leaves this class.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The cell is the open offer's rather than the caller's, because an
        /// upgrade names its target by hex and the hex is what the offer is
        /// pinned to. Nothing is applied here — see <see cref="UpgradeChosen"/>.
        /// </para>
        /// <para>
        /// A rung this offer is not showing is ignored rather than passed on.
        /// The offer holds only what resolved, so that is the whole of what
        /// prevention means at this hex; a public method that would hand any
        /// type at all to the composed round would be a second door into it,
        /// past the one thing that checked.
        /// </para>
        /// <para>
        /// The rungs are matched by id and not by reference. A type id names one
        /// row of <c>content/units.txt</c> for ever, and two readings of that
        /// file produce two objects for one row — so comparing the objects would
        /// make this method work or not depending on which parse the caller
        /// happened to hold, which is the sort of failure that only shows up
        /// once a second table exists somewhere.
        /// </para>
        /// </remarks>
        public void Take(UnitType rung)
        {
            if (!_offering || rung is null)
            {
                return;
            }

            foreach (UnitType offered in _offered)
            {
                if (offered.Id == rung.Id)
                {
                    UpgradeChosen?.Invoke(_offerColumn, _offerRow, offered);

                    return;
                }
            }
        }

        /// <summary>Takes the ladder off the screen.</summary>
        public void CloseOffer()
        {
            _offering = false;
            _rungs.Clear();
            _offered.Clear();
            _offer.Clear();
            _offer.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Puts the bar back in step with the round: which entry is chosen,
        /// which prices the purse can no longer cover, and where the open offer
        /// is on screen.
        /// </summary>
        /// <remarks>
        /// Called after every change and every frame, for the reason
        /// <see cref="PlaybackControls.Follow"/> is: the camera moves without
        /// anything being clicked, so an offer pinned to a hex has to be
        /// repositioned by the frame rather than by the change.
        /// </remarks>
        public void Follow()
        {
            foreach (Entry entry in _entries)
            {
                bool chosen = ReferenceEquals(entry.Type, Selected);

                entry.Button.style.backgroundColor = chosen ? ChosenColor : RuntimePanel.ControlColor;
                entry.Price.style.color = _round.CanAfford(entry.Type) ? QuietColor : UnaffordableColor;
            }

            if (_offering)
            {
                PositionOffer();
            }
        }

        /// <summary>
        /// Whether the pointer is over this panel's chrome — the bar or an open
        /// offer — rather than over the board behind it.
        /// </summary>
        public bool Covers(Vector2 screenPoint) => RuntimePanel.Covers(Document, screenPoint);

        private void Update() => Follow();

        /// <summary>
        /// The settings object is made here rather than loaded, so it is
        /// destroyed here too — nothing else holds it and an orphaned one
        /// outlives the play session that made it.
        /// </summary>
        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        // ---------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------

        private void Assemble(UIDocument document, ComposedRound round, Camera camera)
        {
            _round = round;
            _camera = camera;
            _panel = RuntimePanel.Settings("Palette panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;

            // The bar and the offer pick; the rest of the screen does not, so a
            // pointer anywhere else reaches the board behind the panel.
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            VisualElement bar = Bar();
            document.rootVisualElement.Add(bar);

            for (int index = 0; index < round.Palette.Count; index++)
            {
                AddEntry(bar, round.Palette[index], index);
            }

            _offer = OfferPanel();
            document.rootVisualElement.Add(_offer);

            Follow();
        }

        /// <summary>
        /// The bar itself: a row across the bottom of the screen, sitting on top
        /// of the playback bar and opaque, so a click on it stops at it rather
        /// than falling through onto the playfield behind.
        /// </summary>
        private static VisualElement Bar()
        {
            var bar = new VisualElement { name = "Palette" };

            bar.style.position = Position.Absolute;
            bar.style.left = 0f;
            bar.style.right = 0f;
            bar.style.bottom = PlaybackControls.BarHeight;
            bar.style.height = BarHeight;
            bar.style.paddingLeft = RuntimePanel.Margin;
            bar.style.paddingRight = RuntimePanel.Margin;
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.backgroundColor = RuntimePanel.BarColor;

            return bar;
        }

        /// <summary>
        /// One tower on the bar: what it is called, what it costs, and which key
        /// picks it up.
        /// </summary>
        private void AddEntry(VisualElement bar, UnitType type, int index)
        {
            var button = new Button { name = "Entry " + RosterNames.Of(type), text = string.Empty };

            button.style.width = EntryWidth;
            button.style.height = EntryHeight;
            button.style.flexShrink = 0f;
            button.style.marginLeft = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.marginRight = RuntimePanel.ControlGap;
            button.style.flexDirection = FlexDirection.Column;
            button.style.justifyContent = Justify.Center;
            button.style.backgroundColor = RuntimePanel.ControlColor;

            VisualElement picture = RosterThumbnails.Of(type);

            if (picture != null)
            {
                button.Add(picture);
            }

            var name = new Label { name = "Name", text = RosterNames.Of(type), pickingMode = PickingMode.Ignore };
            name.style.color = RuntimePanel.LabelColor;
            name.style.fontSize = NameSize;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;

            var price = new Label
            {
                name = "Price",
                text = Wording(index, _round.PriceOf(type)),
                pickingMode = PickingMode.Ignore,
            };

            price.style.fontSize = PriceSize;
            price.style.unityTextAlign = TextAnchor.MiddleCenter;

            button.Add(name);
            button.Add(price);
            button.clicked += () => Select(type);

            bar.Add(button);
            _entries.Add(new Entry(type, button, price));
            _buttons.Add(button);
        }

        /// <summary>
        /// What an entry says under its name: the price, and the key that picks
        /// it where there is one.
        /// </summary>
        /// <remarks>
        /// The digit is a keyboard shortcut and never an identifier — nothing on
        /// this screen shows a type id. Entries past the number row simply have
        /// no digit rather than a wrong one.
        /// </remarks>
        private static string Wording(int index, int price) =>
            index < ShortcutCount
                ? RosterNames.Gold(price) + "   [" + (index + 1).ToString(CultureInfo.InvariantCulture) + "]"
                : RosterNames.Gold(price);

        /// <summary>
        /// The container the ladder is drawn in. Absolutely positioned, moved to
        /// the hex it belongs to every frame, and hidden when nothing is
        /// offered.
        /// </summary>
        private static VisualElement OfferPanel()
        {
            var offer = new VisualElement { name = "Offer" };

            offer.style.position = Position.Absolute;
            offer.style.width = OfferWidth;
            offer.style.backgroundColor = RuntimePanel.BarColor;
            offer.style.display = DisplayStyle.None;

            return offer;
        }

        private void DrawOffer(IReadOnlyList<UnitType> rungs)
        {
            _offer.Clear();
            _rungs.Clear();
            _offered.Clear();
            _offer.style.display = DisplayStyle.Flex;

            foreach (UnitType rung in rungs)
            {
                var button = new Button
                {
                    name = "Rung " + RosterNames.Of(rung),
                    text = RosterNames.Of(rung) + "   " + RosterNames.Gold(_round.PriceOf(rung)),
                };

                button.style.height = RungHeight;
                button.style.marginLeft = 0f;
                button.style.marginRight = 0f;
                button.style.marginTop = 0f;
                button.style.marginBottom = 0f;
                button.style.backgroundColor = RuntimePanel.ControlColor;
                button.style.color = RuntimePanel.LabelColor;
                button.style.fontSize = PriceSize;

                UnitType chosen = rung;
                button.clicked += () => Take(chosen);

                _offer.Add(button);
                _rungs.Add(button);
                _offered.Add(rung);
            }
        }

        /// <summary>
        /// Puts the open offer over the hex it belongs to, projected through the
        /// camera every frame so it stays there while the board is orbited and
        /// flown.
        /// </summary>
        /// <remarks>
        /// Hidden rather than clamped when the hex is behind the camera. An
        /// offer pinned to the screen edge would be a menu pointing at nothing,
        /// and the board can be looked at from underneath.
        /// </remarks>
        private void PositionOffer()
        {
            if (_camera == null)
            {
                return;
            }

            Vector3 world = HexGeometry.ToWorld(_offerColumn, _offerRow)
                + (Vector3.up * OfferAnchorHeight);

            if (_camera.WorldToViewportPoint(world).z <= 0f)
            {
                _offer.style.display = DisplayStyle.None;

                return;
            }

            _offer.style.display = DisplayStyle.Flex;

            Vector2 point = RuntimePanelUtils.CameraTransformWorldToPanel(
                Document.rootVisualElement.panel, world, _camera);

            _offer.style.left = point.x - (OfferWidth * 0.5f);
            _offer.style.top = point.y;
        }

        /// <summary>One tower on the bar and the parts of it that change.</summary>
        private readonly struct Entry
        {
            public Entry(UnitType type, Button button, Label price)
            {
                Type = type;
                Button = button;
                Price = price;
            }

            public UnitType Type { get; }

            public Button Button { get; }

            public Label Price { get; }
        }
    }
}
