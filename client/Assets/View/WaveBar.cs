using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    /// <summary>
    /// What is being sent: an ordered row of boxes along the bottom of the
    /// screen, one per creep in the wave, dragged into the order they arrive
    /// in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The row grows as it is filled, and nothing bounds it.</b> It opens as
    /// one empty box; filling the last one appends another behind it, and
    /// emptying a box takes it out and closes the gap. The width that once came
    /// from the round -- two slots, then three, then four -- was deleted with
    /// the anchors by #179, and a fixed row here would put that gate back in
    /// through the interface after the rules had let it go. The purse is the
    /// only scarcity there is on this side, so the row is honest about being
    /// unbounded. <b>What that costs is layout</b>, and it is real: a late round
    /// with a deep purse can want more boxes than the screen is wide. Scrolling
    /// or wrapping is the answer when a played round actually overflows, and not
    /// before.
    /// </para>
    /// <para>
    /// <b>Position is arrival order, so dragging is a decision.</b>
    /// <see cref="BuildPhase"/> gives slot one's creeps the front of the column
    /// and each slot behind it the ticks the slots above it take -- see
    /// ADR-0051, which rejected sorting the bar at record time precisely because
    /// it would have left a drag that changed nothing. The box dragged to the
    /// front is the creep that walks out first.
    /// </para>
    /// <para>
    /// <b>Nothing here decides what is legal.</b> A box's list is
    /// <see cref="ComposedRound.Sendable"/>, which resolves a candidate wave and
    /// throws the <see cref="Sim.Build"/> away; so a creep another box already sends
    /// is absent because <c>Resolve</c> refuses a duplicate, and one the purse
    /// cannot cover after the towers have been paid for is absent because there
    /// is one wallet. Neither rule is written on this side. See ADR-0051.
    /// </para>
    /// <para>
    /// <b>Named and not pictured.</b> The ticket asks each box for a thumbnail
    /// profile of the creep in it and there is no such picture anywhere in this
    /// project -- see <see cref="RosterThumbnails"/>, which is the one seam both
    /// this and <see cref="TowerPalette"/> ask, and which answers with nothing
    /// until somebody supplies an answer. No glyph stands in for a creep in the
    /// meantime. No type ids on screen either, and none of the record's
    /// vocabulary.
    /// </para>
    /// <para>
    /// <b>Banking is not said here.</b> Unspent gold earns interest, so sending
    /// less is an investment -- but with no fixed row there is no empty slot
    /// left behind to read as a deliberate one, and a bar that editorialised
    /// about it would be forecasting. It is a purse fact and it belongs to the
    /// header's gold.
    /// </para>
    /// <para>
    /// <b>Built in code on a panel of its own</b>, like the playback bar and the
    /// palette, and for the same reason: chrome dragged into a scene is chrome
    /// in serialized YAML whose diffs cannot be read. Its sorting order is above
    /// the palette's so an open list is over the bar below it rather than
    /// behind it.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class WaveBar : MonoBehaviour
    {
        /// <summary>
        /// Drawn after the palette, so a box's open list is over the bars below
        /// it rather than behind them.
        /// </summary>
        private const int PanelSortingOrder = 2;

        /// <summary>How tall the row of boxes is.</summary>
        public const float BarHeight = 108f;

        private const float BoxWidth = 176f;

        private const float BoxHeight = 84f;

        /// <summary>
        /// The trailing empty box is narrower than a filled one. It carries a
        /// plus sign and never a name, so sizing it for a name would leave a
        /// gap at the end of every row.
        /// </summary>
        private const float EmptyBoxWidth = 84f;

        private const float ListWidth = 208f;

        private const float ListRowHeight = 36f;

        private const int NameSize = 22;

        private const int CountSize = 18;

        private const int PlusSize = 34;

        /// <summary>
        /// How far the pointer travels before a press on a box is a drag rather
        /// than a click.
        /// </summary>
        /// <remarks>
        /// One affordance, two verbs, told apart by distance: a box is both the
        /// thing that opens a list and the thing that is dragged, and a hand on
        /// a mouse moves a pixel or two while clicking. Without the slack a
        /// click would land as a one-pixel rearrangement, which is a decision
        /// nobody made.
        /// </remarks>
        public const float DragSlack = 6f;

        /// <summary>What the box being dragged is drawn in.</summary>
        private static readonly Color DraggedColor = new Color(0.45f, 0.68f, 0.85f, 1f);

        /// <summary>What the box a drop would land on is drawn in.</summary>
        private static readonly Color TargetColor = new Color(0.3f, 0.4f, 0.48f, 1f);

        /// <summary>What a count and a price read in.</summary>
        private static readonly Color QuietColor = new Color(0.68f, 0.72f, 0.78f, 1f);

        private readonly List<VisualElement> _boxes = new List<VisualElement>();

        private readonly List<Button> _choices = new List<Button>();

        /// <summary>The creeps the open list is offering.</summary>
        private readonly List<UnitType> _offered = new List<UnitType>();

        private ComposedRound _round;

        private PanelSettings _panel;

        private VisualElement _row;

        private VisualElement _list;

        private bool _listing;

        private int _listingAt;

        private int _from = -1;

        private int _to = -1;

        private float _grabbedAt;

        private float _offset;

        private bool _moved;

        /// <summary>Raised whenever the composed wave changed.</summary>
        /// <remarks>
        /// The bar writes the wave itself, as <see cref="BuildInput"/> writes
        /// the board: one writer each, and both of them go through
        /// <see cref="ComposedRound"/>. What this is for is everything else on
        /// screen that a wave costing gold makes stale -- the palette's prices
        /// above all, because one purse buys both halves and a creep bought is a
        /// tower no longer affordable.
        /// </remarks>
        public event Action Changed;

        /// <summary>The panel the row is drawn on.</summary>
        public UIDocument Document { get; private set; }

        /// <summary>
        /// Every box, in the order they are laid out: one per filled slot, and
        /// the trailing empty one last. Always at least one.
        /// </summary>
        public IReadOnlyList<VisualElement> Boxes => _boxes;

        /// <summary>The buttons of the open list, in the order it shows them.</summary>
        public IReadOnlyList<Button> Choices => _choices;

        /// <summary>Whether a box's list is on screen.</summary>
        public bool IsListing => _listing;

        /// <summary>Which box the open list belongs to.</summary>
        public int ListingAt => _listingAt;

        /// <summary>Whether a box is being dragged.</summary>
        public bool IsDragging => _from >= 0;

        /// <summary>The box being dragged, meaningless while nothing is.</summary>
        public int DraggingFrom => _from;

        /// <summary>Where a drop would put it, meaningless while nothing is dragged.</summary>
        public int DraggingTo => _to;

        /// <summary>
        /// Builds the bar under <paramref name="parent"/>, showing the wave
        /// <paramref name="round"/> has composed.
        /// </summary>
        public static WaveBar Build(Transform parent, ComposedRound round)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (round == null) throw new ArgumentNullException(nameof(round));

            var host = new GameObject("WaveBar");
            host.transform.SetParent(parent, worldPositionStays: false);

            var bar = host.AddComponent<WaveBar>();
            bar.Assemble(host.AddComponent<UIDocument>(), round);

            return bar;
        }

        /// <summary>
        /// Opens the list of what may go in a box. Closes again where nothing
        /// may -- a box with no legal creep left offers none, which is what
        /// prevention means here.
        /// </summary>
        public void Open(int index)
        {
            if (index < 0 || index >= _boxes.Count)
            {
                return;
            }

            IReadOnlyList<UnitType> creeps = _round.Sendable(index);
            bool filled = index < _round.Slots.Count;

            if (creeps.Count == 0 && !filled)
            {
                Close();

                return;
            }

            _listing = true;
            _listingAt = index;

            DrawList(index, creeps, filled);
        }

        /// <summary>Takes the list off the screen.</summary>
        public void Close()
        {
            _listing = false;
            _choices.Clear();
            _offered.Clear();
            _list.Clear();
            _list.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Puts one of a creep in the box the list is open on: what its button
        /// does, and the one way a creep reaches the wave.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A creep the open list is not offering is ignored rather than passed
        /// on, and the box is the open list's rather than the caller's. The list
        /// holds only what resolved, so that is the whole of what prevention
        /// means at this box; a method that would hand any creep at all to the
        /// composed round would be a second door into it, past the one thing
        /// that checked.
        /// </para>
        /// <para>
        /// Matched by id and never by reference, for the reason
        /// <see cref="TowerPalette.Take"/> is: a type id names one row of
        /// <c>content/units.txt</c> for ever, and two readings of that file
        /// produce two objects for one row.
        /// </para>
        /// </remarks>
        public void Choose(UnitType creep)
        {
            if (!_listing || creep is null)
            {
                return;
            }

            UnitType sent = null;

            foreach (UnitType offered in _offered)
            {
                if (offered.Id == creep.Id)
                {
                    sent = offered;
                }
            }

            if (sent is null)
            {
                return;
            }

            int box = _listingAt;

            Close();
            _round.Send(box, sent);
            Redraw();
        }

        /// <summary>Sends one more of what the open list's box holds.</summary>
        public void More()
        {
            if (!_listing || !_round.CanSendMore(_listingAt))
            {
                return;
            }

            int box = _listingAt;

            Close();
            _round.SendMore(box);
            Redraw();
        }

        /// <summary>
        /// Sends one fewer. At one this empties the box, which takes it out of
        /// the row.
        /// </summary>
        public void Fewer()
        {
            if (!_listing || _listingAt >= _round.Slots.Count)
            {
                return;
            }

            int box = _listingAt;

            Close();
            _round.SendFewer(box);
            Redraw();
        }

        /// <summary>
        /// A press landed on a box, at a point in the panel's own coordinates.
        /// </summary>
        /// <remarks>
        /// <b>The devices are read in the pointer callbacks and everything else
        /// takes arguments</b>, the same split <see cref="BuildInput"/> and
        /// <see cref="OrbitCameraRig"/> use: this, <see cref="Drag"/> and
        /// <see cref="Release"/> are what the class actually does, and a test
        /// drives a rearrangement through exactly the path a hand does with no
        /// device attached.
        /// </remarks>
        public void Grab(int index, float panelX)
        {
            if (index < 0 || index >= _round.Slots.Count)
            {
                return;
            }

            _from = index;
            _to = index;
            _grabbedAt = panelX;
            _offset = 0f;
            _moved = false;

            Restyle();
        }

        /// <summary>
        /// The pointer moved while a box was held. Marks where a drop would put
        /// it.
        /// </summary>
        public void Drag(float panelX)
        {
            if (_from < 0)
            {
                return;
            }

            _offset = panelX - _grabbedAt;

            if (Mathf.Abs(_offset) > DragSlack)
            {
                _moved = true;
            }

            _to = _moved ? Landing(panelX) : _from;

            Restyle();
        }

        /// <summary>
        /// The box was let go. A press that never travelled is a click, and a
        /// click opens the box's list.
        /// </summary>
        public void Release()
        {
            if (_from < 0)
            {
                return;
            }

            int from = _from;
            int to = _to;
            bool moved = _moved;

            _from = -1;
            _to = -1;
            _offset = 0f;
            _moved = false;

            if (!moved)
            {
                Restyle();
                Open(from);

                return;
            }

            if (from == to)
            {
                Restyle();

                return;
            }

            Close();
            _round.Rearrange(from, to);
            Redraw();
        }

        /// <summary>
        /// Whether the pointer is over this panel's chrome -- the row or an open
        /// list -- rather than over the board behind it.
        /// </summary>
        public bool Covers(Vector2 screenPoint) => RuntimePanel.Covers(Document, screenPoint);

        /// <summary>
        /// Where a drop at a point in the panel's own coordinates would put the
        /// held box: how many of the boxes that are staying put it has been
        /// carried past.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Counted, never indexed, and that is the whole of the arithmetic.</b>
        /// <see cref="ComposedRound.Rearrange"/> takes the box out of the row
        /// and then puts it back, so what it is handed is a position in the row
        /// <i>without</i> it — which is exactly how many of the others the
        /// pointer has passed. Taking the index of the last box whose middle was
        /// passed is the same number only while the held box is to the left of
        /// it, because removing it shifts everything after it down by one and
        /// nothing before it. So a rightward drag came out right and a leftward
        /// one landed a box too far left: grab the last of three, drop it
        /// between the first two, and it went to the front.
        /// </para>
        /// <para>
        /// <b>The held box is skipped rather than measured.</b> It is carried
        /// under the pointer, so its drawn middle travels with the thing being
        /// compared against it — and it is not one of the boxes a landing is
        /// counted among, because it is the one being placed. The trailing empty
        /// box is not counted either: there is nothing in it to arrive before.
        /// </para>
        /// </remarks>
        public int Landing(float panelX)
        {
            int landing = 0;

            for (int index = 0; index < _round.Slots.Count && index < _boxes.Count; index++)
            {
                if (index != _from && panelX > _boxes[index].worldBound.center.x)
                {
                    landing++;
                }
            }

            return landing;
        }

        /// <summary>
        /// The settings object is made here rather than loaded, so it is
        /// destroyed here too -- nothing else holds it and an orphaned one
        /// outlives the play session that made it.
        /// </summary>
        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        // ---------------------------------------------------------------
        // Building
        // ---------------------------------------------------------------

        private void Assemble(UIDocument document, ComposedRound round)
        {
            _round = round;
            _panel = RuntimePanel.Settings("Wave panel", PanelSortingOrder);

            Document = document;
            document.panelSettings = _panel;

            // The row and the list pick; the rest of the screen does not, so a
            // pointer anywhere else reaches the board behind the panel.
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _row = Row();
            document.rootVisualElement.Add(_row);

            _list = ListPanel();
            document.rootVisualElement.Add(_list);

            Redraw();
        }

        /// <summary>
        /// The row itself: a strip across the bottom of the screen, above the
        /// palette and the scrub bar, and opaque so a click on it stops at it
        /// rather than falling through onto the playfield behind.
        /// </summary>
        private static VisualElement Row()
        {
            var row = new VisualElement { name = "Wave" };

            row.style.position = Position.Absolute;
            row.style.left = 0f;
            row.style.right = 0f;
            row.style.bottom = PlaybackControls.BarHeight + TowerPalette.BarHeight;
            row.style.height = BarHeight;
            row.style.paddingLeft = RuntimePanel.Margin;
            row.style.paddingRight = RuntimePanel.Margin;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = RuntimePanel.BarColor;

            return row;
        }

        /// <summary>
        /// Rebuilds the row from the composed wave: one box per filled slot, and
        /// one empty box behind them.
        /// </summary>
        /// <remarks>
        /// Rebuilt rather than reconciled. A palette lists a fixed roster and is
        /// only ever restyled; a wave is inserted into, taken out of and
        /// rearranged, so the elements that would have to be matched up are
        /// exactly the thing that moved. Rebuilding happens on a click and never
        /// on a frame.
        /// </remarks>
        private void Redraw()
        {
            _row.Clear();
            _boxes.Clear();

            IReadOnlyList<WaveSlot> slots = _round.Slots;

            for (int index = 0; index < slots.Count; index++)
            {
                AddBox(index, _round.CreepIn(index), slots[index].Count);
            }

            AddEmptyBox(slots.Count);
            Restyle();

            Changed?.Invoke();
        }

        /// <summary>
        /// One filled box: what is in it, how many, and what that many come to.
        /// </summary>
        private void AddBox(int index, UnitType creep, int count)
        {
            VisualElement box = Box(BoxWidth, "Box " + RosterNames.Of(creep));

            VisualElement picture = RosterThumbnails.Of(creep);

            if (picture != null)
            {
                box.Add(picture);
            }

            var name = new Label
            {
                name = "Name",
                text = RosterNames.Of(creep),
                pickingMode = PickingMode.Ignore,
            };

            name.style.color = RuntimePanel.LabelColor;
            name.style.fontSize = NameSize;
            name.style.unityTextAlign = TextAnchor.MiddleCenter;

            // A name and a count, and nothing else in the box. What this many
            // come to is a purse fact, and it is said where the decision to buy
            // them is made — in the box's own list, which prices every creep it
            // offers and the raise it offers on top of them.
            var sending = new Label
            {
                name = "Sending",
                text = RosterNames.Count(count),
                pickingMode = PickingMode.Ignore,
            };

            sending.style.color = QuietColor;
            sending.style.fontSize = CountSize;
            sending.style.unityTextAlign = TextAnchor.MiddleCenter;

            box.Add(name);
            box.Add(sending);

            int grabbed = index;

            // Pointer events and not a Button, because this element is both the
            // thing that is clicked and the thing that is dragged, and a
            // Clickable would swallow the press before the drag could start.
            box.RegisterCallback<PointerDownEvent>(pointer =>
            {
                box.CapturePointer(pointer.pointerId);
                Grab(grabbed, pointer.position.x);
            });

            box.RegisterCallback<PointerMoveEvent>(pointer => Drag(pointer.position.x));

            box.RegisterCallback<PointerUpEvent>(pointer => box.ReleasePointer(pointer.pointerId));

            // The drag ends where the capture ends, and not on the pointer-up.
            // A capture can be lost without one ever arriving — the panel takes
            // it back, or the element leaves the hierarchy — and hanging the end
            // of the drag on the up event leaves the row tinted and IsDragging
            // stuck true until something else is grabbed. Releasing the capture
            // above raises this, so the ordinary path runs through here too and
            // there is one ending rather than two.
            box.RegisterCallback<PointerCaptureOutEvent>(_ => Release());

            _row.Add(box);
            _boxes.Add(box);
        }

        /// <summary>
        /// The trailing empty box: a plus sign, and the only empty box that ever
        /// exists.
        /// </summary>
        /// <remarks>
        /// It is not dragged. There is nothing in it to arrive at any position,
        /// and it is always last -- the row grows behind it.
        /// </remarks>
        private void AddEmptyBox(int index)
        {
            VisualElement box = Box(EmptyBoxWidth, "Empty box");

            var plus = new Label { name = "Plus", text = "+", pickingMode = PickingMode.Ignore };
            plus.style.color = RuntimePanel.LabelColor;
            plus.style.fontSize = PlusSize;
            plus.style.unityTextAlign = TextAnchor.MiddleCenter;

            box.Add(plus);
            box.RegisterCallback<PointerDownEvent>(_ => Open(index));

            _row.Add(box);
            _boxes.Add(box);
        }

        private static VisualElement Box(float width, string name)
        {
            var box = new VisualElement { name = name };

            box.style.width = width;
            box.style.height = BoxHeight;
            box.style.flexShrink = 0f;
            box.style.marginRight = RuntimePanel.ControlGap;
            box.style.flexDirection = FlexDirection.Column;
            box.style.justifyContent = Justify.Center;
            box.style.backgroundColor = RuntimePanel.ControlColor;

            return box;
        }

        /// <summary>
        /// Colours the row for the drag in progress, and puts the held box under
        /// the pointer.
        /// </summary>
        private void Restyle()
        {
            for (int index = 0; index < _boxes.Count; index++)
            {
                VisualElement box = _boxes[index];
                bool held = index == _from;

                box.style.backgroundColor = held
                    ? DraggedColor
                    : (_from >= 0 && index == _to ? TargetColor : RuntimePanel.ControlColor);

                box.style.translate = new Translate(held ? _offset : 0f, 0f);
            }
        }

        /// <summary>
        /// The container a box's list is drawn in. Absolutely positioned over
        /// the box it belongs to, and hidden when nothing is open.
        /// </summary>
        private static VisualElement ListPanel()
        {
            var list = new VisualElement { name = "List" };

            list.style.position = Position.Absolute;
            list.style.width = ListWidth;
            list.style.backgroundColor = RuntimePanel.BarColor;
            list.style.display = DisplayStyle.None;

            return list;
        }

        /// <summary>
        /// What a box offers: how many to send where something is in it, then
        /// every creep that may go in it.
        /// </summary>
        /// <remarks>
        /// The counts come first because they are what a box already holding a
        /// creep is usually clicked for -- a repeat is spelled by raising a
        /// count, not by a second box, so this is the affordance that stands in
        /// for the one the rules refuse.
        /// </remarks>
        private void DrawList(int index, IReadOnlyList<UnitType> creeps, bool filled)
        {
            _list.Clear();
            _choices.Clear();
            _offered.Clear();
            _list.style.display = DisplayStyle.Flex;

            if (filled)
            {
                if (_round.CanSendMore(index))
                {
                    UnitType creep = _round.CreepIn(index);

                    AddChoice("One more   " + RosterNames.Gold(_round.PriceOf(creep)), More);
                }

                AddChoice("One fewer", Fewer);
            }

            foreach (UnitType creep in creeps)
            {
                UnitType chosen = creep;

                AddChoice(
                    RosterNames.Of(creep) + "   " + RosterNames.Gold(_round.PriceOf(creep)),
                    () => Choose(chosen));

                _offered.Add(creep);
            }

            Place(index);
        }

        private void AddChoice(string wording, Action taken)
        {
            var button = new Button { name = wording, text = wording };

            button.style.height = ListRowHeight;
            button.style.marginLeft = 0f;
            button.style.marginRight = 0f;
            button.style.marginTop = 0f;
            button.style.marginBottom = 0f;
            button.style.backgroundColor = RuntimePanel.ControlColor;
            button.style.color = RuntimePanel.LabelColor;
            button.style.fontSize = CountSize;
            button.clicked += taken;

            _list.Add(button);
            _choices.Add(button);
        }

        /// <summary>
        /// Puts the open list directly over the box it belongs to.
        /// </summary>
        /// <remarks>
        /// Both are children of one panel, so the box's own laid-out position is
        /// the answer and no screen coordinate is crossed to get it. A box the
        /// panel has not laid out yet reports zero, which puts the list in the
        /// corner rather than anywhere wrong -- and a panel that has never been
        /// laid out has nothing on screen to be wrong about.
        /// </remarks>
        private void Place(int index)
        {
            Rect box = _boxes[index].worldBound;

            _list.style.left = box.xMin;
            _list.style.bottom = _list.parent.worldBound.height - box.yMin;
        }
    }
}
