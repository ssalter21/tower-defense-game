using System;
using System.Collections.Generic;
using Sim;
using UnityEngine;
using UnityEngine.UIElements;

namespace View
{
    [DisallowMultipleComponent]
    public sealed class HexWheel : MonoBehaviour
    {
        private const float PanelSortingOrder = 3.5f;

        public const float HubRadius = 44f;

        public const float LabelWidth = 124f;

        public const float MinimumLabelRadius = 104f;

        public const float RootRingBeyondLabel = 52f;

        public const float LadderRingDepth = 96f;

        private const float GapDegrees = 1.2f;

        private const float GapRadius = 3f;

        private const float TopDegrees = -90f;

        private const int NameSize = 18;

        private const int PriceSize = 15;

        private const float OpaqueFill = 0.92f;

        private const float SeeThroughFill = 0.4f;

        private static readonly Color QuietColor = new Color(0.68f, 0.72f, 0.78f, 1f);

        private static readonly Color UnaffordableColor = new Color(0.93f, 0.42f, 0.38f, 1f);

        private static readonly Color SentColor = new Color(0.45f, 0.68f, 0.85f, 1f);

        private static readonly Color EdgeColor = new Color(0.06f, 0.07f, 0.09f, 1f);

        private static readonly Color CoveredColor = new Color(1f, 0.86f, 0.35f, 1f);

        private readonly List<HexWheelWedge> _wedges = new List<HexWheelWedge>();

        private readonly List<VisualElement> _labels = new List<VisualElement>();

        private readonly List<Vector2> _labelAt = new List<Vector2>();

        private readonly List<Vector2[]> _covered = new List<Vector2[]>();

        private Camera _camera;

        private HexMap _map;

        private PanelSettings _panel;

        private VisualElement _face;

        private bool _showsCovered;

        public UIDocument Document { get; private set; }

        public bool IsOpen { get; private set; }

        public int Column { get; private set; }

        public int Row { get; private set; }

        public Vector2 Centre { get; private set; }

        public IReadOnlyList<HexWheelWedge> Wedges => _wedges;

        public IReadOnlyList<VisualElement> Labels => _labels;

        public int CoveredCount => _covered.Count;

        public float OuterRadius => OuterRadiusOf(_wedges);

        public static HexWheel Build(Transform parent, Camera camera, HexMap map)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (map is null) throw new ArgumentNullException(nameof(map));

            var host = new GameObject("HexWheel");
            host.transform.SetParent(parent, worldPositionStays: false);

            var wheel = host.AddComponent<HexWheel>();
            wheel.Assemble(host.AddComponent<UIDocument>(), camera, map);

            return wheel;
        }

        public static float RootLabelRadius(int wedgeCount) =>
            Mathf.Max(MinimumLabelRadius, wedgeCount * LabelWidth / (2f * Mathf.PI));

        public static float RootOuterRadius(int wedgeCount) => RootLabelRadius(wedgeCount) + RootRingBeyondLabel;

        public static int RingsOf(IReadOnlyList<HexWheelWedge> wedges)
        {
            int rings = 0;

            foreach (HexWheelWedge wedge in wedges)
            {
                rings = Mathf.Max(rings, wedge.Outward.Count);
            }

            return rings;
        }

        public static float OuterRadiusOf(IReadOnlyList<HexWheelWedge> wedges) =>
            wedges == null || wedges.Count == 0
                ? 0f
                : RootOuterRadius(wedges.Count) + (RingsOf(wedges) * LadderRingDepth);

        public void Open(int column, int row, IReadOnlyList<HexWheelWedge> wedges, bool showsCovered = false)
        {
            if (wedges is null) throw new ArgumentNullException(nameof(wedges));

            Column = column;
            Row = row;
            IsOpen = true;
            _showsCovered = showsCovered;

            _wedges.Clear();
            _wedges.AddRange(wedges);

            DrawLabels();
            Follow();
        }

        public void Close()
        {
            IsOpen = false;
            _wedges.Clear();
            _labels.Clear();
            _labelAt.Clear();
            _covered.Clear();
            _face.Clear();
            _face.style.display = DisplayStyle.None;
        }

        public void Follow()
        {
            if (!IsOpen || _camera == null || Document.rootVisualElement.panel == null)
            {
                return;
            }

            Vector3 world = HexGeometry.ToWorld(Column, Row, _map.LevelAt(Column, Row));

            if (_camera.WorldToViewportPoint(world).z <= 0f)
            {
                _face.style.display = DisplayStyle.None;

                return;
            }

            _face.style.display = DisplayStyle.Flex;
            Centre = ToPanel(world);

            for (int index = 0; index < _labels.Count; index++)
            {
                _labels[index].style.left = Centre.x + _labelAt[index].x;
                _labels[index].style.top = Centre.y + _labelAt[index].y;
            }

            FindCovered();
            _face.MarkDirtyRepaint();
        }

        private void Update() => Follow();

        private void OnDestroy()
        {
            if (_panel != null) Destroy(_panel);
        }

        private void Assemble(UIDocument document, Camera camera, HexMap map)
        {
            _camera = camera;
            _map = map;
            _panel = RuntimePanel.Settings("Hex wheel panel");
            _panel.sortingOrder = PanelSortingOrder;

            Document = document;
            document.panelSettings = _panel;
            document.rootVisualElement.pickingMode = PickingMode.Ignore;

            _face = new VisualElement { name = "Wheel", pickingMode = PickingMode.Ignore };
            _face.style.position = Position.Absolute;
            _face.style.left = 0f;
            _face.style.top = 0f;
            _face.style.right = 0f;
            _face.style.bottom = 0f;
            _face.style.display = DisplayStyle.None;
            _face.generateVisualContent += Draw;

            document.rootVisualElement.Add(_face);
        }

        private Vector2 ToPanel(Vector3 world) =>
            RuntimePanelUtils.CameraTransformWorldToPanel(Document.rootVisualElement.panel, world, _camera);

        private void DrawLabels()
        {
            _face.Clear();
            _labels.Clear();
            _labelAt.Clear();

            int count = _wedges.Count;

            if (count == 0)
            {
                return;
            }

            float span = 360f / count;
            float rootLabel = RootLabelRadius(count);
            float rootOuter = RootOuterRadius(count);

            for (int index = 0; index < count; index++)
            {
                float degrees = TopDegrees + (index * span);
                HexWheelWedge wedge = _wedges[index];

                AddLabel(wedge, Polar(degrees, rootLabel));

                for (int ring = 0; ring < wedge.Outward.Count; ring++)
                {
                    float radius = rootOuter + ((ring + 0.5f) * LadderRingDepth);
                    AddLabel(wedge.Outward[ring], Polar(degrees, radius));
                }
            }
        }

        private void AddLabel(HexWheelWedge wedge, Vector2 offset)
        {
            if (string.IsNullOrEmpty(wedge.Word))
            {
                return;
            }

            var label = new VisualElement { name = "Wedge " + wedge.Word, pickingMode = PickingMode.Ignore };

            label.style.position = Position.Absolute;
            label.style.width = LabelWidth;
            label.style.translate = new Translate(Length.Percent(-50f), Length.Percent(-50f));
            label.style.flexDirection = FlexDirection.Column;
            label.style.alignItems = Align.Center;

            Label name = Line("Name", wedge.Word, NameSize);
            name.style.color = wedge.Undesigned ? QuietColor : RuntimePanel.LabelColor;
            label.Add(name);

            if (!string.IsNullOrEmpty(wedge.Price))
            {
                Label price = Line("Price", wedge.Price, PriceSize);
                price.style.color = wedge.Refused ? UnaffordableColor : QuietColor;
                label.Add(price);
            }

            _face.Add(label);
            _labels.Add(label);
            _labelAt.Add(offset);
        }

        private static Label Line(string name, string text, int size)
        {
            var line = new Label { name = name, text = text, pickingMode = PickingMode.Ignore };

            line.style.fontSize = size;
            line.style.unityTextAlign = TextAnchor.MiddleCenter;
            line.style.whiteSpace = WhiteSpace.Normal;

            return line;
        }

        private static Vector2 Polar(float degrees, float radius)
        {
            float radians = degrees * Mathf.Deg2Rad;

            return new Vector2(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius);
        }

        private void FindCovered()
        {
            _covered.Clear();

            if (!_showsCovered)
            {
                return;
            }

            float reach = OuterRadius;

            for (int row = 0; row < _map.Height; row++)
            {
                for (int column = 0; column < _map.Width; column++)
                {
                    if (column == Column && row == Row)
                    {
                        continue;
                    }

                    Vector3 middle = HexGeometry.ToWorld(column, row, _map.LevelAt(column, row));

                    if (Vector2.Distance(ToPanel(middle), Centre) > reach)
                    {
                        continue;
                    }

                    var corners = new Vector2[6];

                    for (int corner = 0; corner < corners.Length; corner++)
                    {
                        corners[corner] = ToPanel(middle + HexGeometry.Corner(corner));
                    }

                    _covered.Add(corners);
                }
            }
        }

        private void Draw(MeshGenerationContext context)
        {
            int count = _wedges.Count;

            if (!IsOpen || count == 0)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            float fill = _showsCovered ? SeeThroughFill : OpaqueFill;
            float span = 360f / count;
            float rootOuter = RootOuterRadius(count);

            for (int index = 0; index < count; index++)
            {
                float from = TopDegrees + (index * span) - (span * 0.5f) + GapDegrees;
                float to = from + span - (2f * GapDegrees);
                HexWheelWedge wedge = _wedges[index];

                Wedge(painter, HubRadius, rootOuter, from, to, wedge, fill);

                for (int ring = 0; ring < wedge.Outward.Count; ring++)
                {
                    float inner = rootOuter + (ring * LadderRingDepth) + GapRadius;
                    Wedge(painter, inner, inner + LadderRingDepth - GapRadius, from, to, wedge.Outward[ring], fill);
                }
            }

            painter.strokeColor = CoveredColor;
            painter.lineWidth = 2.5f;

            foreach (Vector2[] corners in _covered)
            {
                painter.BeginPath();
                painter.MoveTo(corners[0]);

                for (int corner = 1; corner < corners.Length; corner++)
                {
                    painter.LineTo(corners[corner]);
                }

                painter.ClosePath();
                painter.Stroke();
            }
        }

        private void Wedge(Painter2D painter, float inner, float outer, float from, float to, HexWheelWedge wedge, float fill)
        {
            painter.BeginPath();
            painter.Arc(Centre, outer, Angle.Degrees(from), Angle.Degrees(to), ArcDirection.Clockwise);
            painter.Arc(Centre, inner, Angle.Degrees(to), Angle.Degrees(from), ArcDirection.CounterClockwise);
            painter.ClosePath();

            if (!wedge.Undesigned)
            {
                Color colour = wedge.Sent ? SentColor : RuntimePanel.ControlColor;
                colour.a = fill;
                painter.fillColor = colour;
                painter.Fill();
            }

            painter.strokeColor = wedge.Undesigned ? RuntimePanel.LabelColor : EdgeColor;
            painter.lineWidth = wedge.Undesigned ? 3f : 1.5f;
            painter.Stroke();
        }
    }
}
