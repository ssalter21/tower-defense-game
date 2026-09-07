using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace View
{
    /// <summary>
    /// What one unit is carrying, drawn: a wash of colour while a payload is in
    /// force, and the pool standing in front of its health as a second segment
    /// of a bar above it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is not decoration and it does not live with the decoration.</b>
    /// Everything <see cref="MatchDecorations"/> draws is triggered by an event
    /// and forgotten; everything here is a pure function of the snapshot being
    /// drawn, so it survives a seek — which is exactly why the fields it reads
    /// are on <c>CreepSnapshot</c> rather than on the event stream. See
    /// <c>docs/adr/0007-snapshot-is-the-only-view-input.md</c>.
    /// </para>
    /// <para>
    /// <b>The whole look is a placeholder and this paragraph is what makes that
    /// a fact rather than an intention.</b> A wash of one colour per payload and
    /// a two-segment bar are the plainest things that say "something is on this
    /// unit" and "there is a pool in front of its health". What a slowed,
    /// hastened, cursed or shielded body should actually look like is a design
    /// decision nobody has taken, and every colour and distance it uses is in
    /// <see cref="MatchTuning"/>, in one section that says the same.
    /// </para>
    /// <para>
    /// <b>The bar appears only while there is a pool to draw.</b> A health bar
    /// over every creep in the match is a much larger decision than this, and it
    /// is not one this takes; the health segment is here so the shield has
    /// something to be a share of, and both segments are shares of the health
    /// pool the unit's row authored.
    /// </para>
    /// <para>
    /// <b>The wash is a property block and never a material.</b> A material per
    /// creep would be an asset instance per creep to destroy again, and a body
    /// keeps the atlas it was imported wearing either way: the block sets the
    /// base colour the shader multiplies that atlas by, so the model is still
    /// its own texture in another hue. Handing back no block takes it off.
    /// </para>
    /// </remarks>
    public sealed class EffectMarks
    {
        /// <summary>
        /// The colour property of both shaders <see cref="ViewMaterials"/>
        /// looks for, so the wash lands whichever one the art arrived wearing.
        /// Setting one a shader does not have costs nothing and does nothing.
        /// </summary>
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private readonly MaterialPropertyBlock _wash = new MaterialPropertyBlock();

        /// <summary>
        /// Where the bar's size and the wash's colour are read from.
        /// <see cref="EffectLook.Shipped"/> unless a capture handed one in; see
        /// <see cref="EffectLook"/> for why a capture would.
        /// </summary>
        private readonly EffectLook _look;

        private Renderer[] _body;

        private Transform _bar;

        private Transform _health;

        private Transform _shield;

        /// <summary>
        /// The second pair, at right angles, or null. Built only when
        /// <see cref="EffectLook.UnitBarCrossed"/> asks for one — which nothing
        /// the game ships does. See <see cref="Build"/>.
        /// </summary>
        private Transform _crossHealth;

        private Transform _crossShield;

        private bool _washed;

        /// <summary>The bar, hidden until something grants a pool. For tests.</summary>
        public Transform Bar => _bar;

        /// <summary>The health segment of that bar. For tests.</summary>
        public Transform HealthSegment => _health;

        /// <summary>The segment standing for the pool. For tests.</summary>
        public Transform ShieldSegment => _shield;

        /// <summary>What the last <see cref="Show"/> washed the body with, or null.</summary>
        public Color? Wash { get; private set; }

        /// <summary>
        /// Marks drawn at the look the game ships.
        /// </summary>
        public EffectMarks()
            : this(null)
        {
        }

        /// <summary>
        /// Marks drawn at <paramref name="look"/>, or at the shipped look when
        /// it is null. <b>Only a capture passes one</b> — the bar's size and
        /// both washes are placeholders <see cref="MatchTuning"/>'s own header
        /// declares, and a candidate for one is judged by being photographed
        /// through the real match.
        /// </summary>
        public EffectMarks(EffectLook look)
        {
            _look = look ?? EffectLook.Shipped;
        }

        /// <summary>
        /// Builds the marks: finds the renderers the wash lands on, and hangs a
        /// hidden two-segment bar off <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The object the unit is drawn under.</param>
        /// <param name="body">The instantiated model, whatever it is made of.</param>
        /// <param name="health">
        /// The health segment's material, or null for a body that is a portrait
        /// rather than a creep in a match. Then no bar is built and
        /// <see cref="Show"/> refuses by name — a sheet makes and throws away a
        /// body per row, so a material made here would be one leaked per row.
        /// </param>
        /// <param name="shield">The pool segment's material, or null for the same.</param>
        public void Build(Transform host, GameObject body, Material health, Material shield)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (body == null) throw new ArgumentNullException(nameof(body));

            _body = body.GetComponentsInChildren<Renderer>(true);

            if (health == null || shield == null)
            {
                return;
            }

            var group = new GameObject("Effects");
            group.transform.SetParent(host, worldPositionStays: false);

            _bar = group.transform;
            _bar.localPosition = Vector3.up * _look.UnitBarHeight;

            _health = Segment(_bar, "Health", health);
            _shield = Segment(_bar, "Shield", shield);

            // A CANDIDATE AND NOT THE GAME. The bar is a stretched box that
            // never turns, so it is read end-on from two of the four quadrants
            // of the orbit -- one of the five things #254 recorded as nobody's
            // decision. The candidate that answers it without billboarding is a
            // second bar across the first, and it is off unless a capture asks.
            if (_look.UnitBarCrossed)
            {
                var across = new GameObject("Across").transform;
                across.SetParent(_bar, worldPositionStays: false);
                across.localRotation = Quaternion.Euler(0f, 90f, 0f);

                _crossHealth = Segment(across, "Health", health);
                _crossShield = Segment(across, "Shield", shield);
            }

            _bar.gameObject.SetActive(false);
        }

        /// <summary>
        /// Draws what the snapshot says is on this unit.
        /// </summary>
        /// <param name="hp">Health remaining.</param>
        /// <param name="maxHp">The pool its row authored, which the bar is a share of.</param>
        /// <param name="shield">Everything standing in front of that health.</param>
        /// <param name="speedMagnitude">The percentage its speed is displaced by.</param>
        /// <param name="armourMagnitude">The percentage its armour is displaced by.</param>
        /// <remarks>
        /// <b>Both segments are shares of the same health pool, so the two of
        /// them together run past a whole bar.</b> A creep at full health
        /// carrying a pool worth two fifths of it draws one and two fifths of
        /// <see cref="MatchTuning.UnitBarLength"/>. The alternative — squeezing
        /// the pool into whatever health is missing — draws nothing at all on a
        /// full-health creep, which is the case a granted pool is most often in.
        /// A bar that grows is the honest one and it is one of the things the
        /// placeholder is being judged on.
        /// </remarks>
        public void Show(int hp, int maxHp, int shield, int speedMagnitude, int armourMagnitude)
        {
            if (_bar == null)
            {
                throw new InvalidOperationException(
                    "These marks were built without a bar, which is what a body drawn for a contact sheet "
                    + "or an art preview gets. Nothing behind such a body is a snapshot, so there is "
                    + "nothing for it to be carrying — build it with the two segment materials if it is "
                    + "a creep in a match.");
            }

            Paint(TintFor(speedMagnitude, armourMagnitude));

            if (shield <= 0 || maxHp <= 0)
            {
                _bar.gameObject.SetActive(false);

                return;
            }

            _bar.gameObject.SetActive(true);

            // Along the world axis rather than along the body, because the body
            // turns to follow the corridor and a bar that swung with it would
            // be reporting the route rather than the pool.
            _bar.rotation = Quaternion.identity;

            float left = Mathf.Clamp01(hp / (float)maxHp);
            float pool = Mathf.Clamp01(shield / (float)maxHp);

            // A CANDIDATE AND NOT THE GAME, on the same terms as the crossed
            // pair above: both segments being shares of the authored health is
            // the fifth of #254's undecided five, and the alternative it names
            // is one bar the two of them share. Off unless a capture asks, so
            // what the game draws is the bar that grows past its own length.
            if (_look.UnitBarClamped && left + pool > 0f)
            {
                float whole = left + pool;
                left /= whole;
                pool /= whole;
            }

            Stretch(_health, from: 0f, width: left);
            Stretch(_shield, from: left, width: pool);

            if (_crossHealth != null)
            {
                Stretch(_crossHealth, from: 0f, width: left);
                Stretch(_crossShield, from: left, width: pool);
            }
        }

        /// <summary>One segment of the bar, at rest.</summary>
        private static Transform Segment(Transform bar, string name, Material material)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = name;
            piece.transform.SetParent(bar, worldPositionStays: false);

            // The primitive arrives with a collider, and nothing in this project
            // uses physics -- the same reason the shell drops its own.
            Collider collider = piece.GetComponent<Collider>();

            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
            }

            MeshRenderer renderer = piece.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            // A bar throwing a hard shadow across the floor reads as a bug in
            // the lighting, exactly as a tracer's would.
            renderer.shadowCastingMode = ShadowCastingMode.Off;

            return piece.transform;
        }

        /// <summary>
        /// Sizes one segment to <paramref name="width"/> of a whole bar and
        /// puts it <paramref name="from"/> of the way along, measuring from the
        /// left-hand end.
        /// </summary>
        private void Stretch(Transform segment, float from, float width)
        {
            if (width <= 0f)
            {
                segment.gameObject.SetActive(false);

                return;
            }

            segment.gameObject.SetActive(true);

            segment.localScale = new Vector3(
                _look.UnitBarLength * width,
                _look.UnitBarThickness,
                _look.UnitBarThickness);

            // A cube is drawn about its own middle, so a segment starting at
            // `from` and `width` wide has its centre half a width past that --
            // and the whole bar is centred on the unit, which is the half.
            segment.localPosition = new Vector3(
                _look.UnitBarLength * (from + (width / 2f) - 0.5f),
                0f,
                0f);
        }

        /// <summary>
        /// The colour a unit carrying those two modifiers is washed with, or
        /// null for one carrying neither.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Speed first, because it is the modifier that changes where the body
        /// is rather than what a hit does to it, and one body can only wear one
        /// colour. A unit carrying both is drawn as the first of the two, which
        /// is a placeholder's answer to a question a real look would answer
        /// differently.
        /// </para>
        /// <para>
        /// <b>Both of those placeholder answers are written here as a look and
        /// not as a branch that is not taken.</b>
        /// <see cref="EffectLook.HasteEffectTint"/> and
        /// <see cref="EffectLook.BothModifiersTint"/> both answer
        /// <see cref="EffectLook.SpeedEffectTint"/> unless a capture says
        /// otherwise, so what the game draws is exactly what it drew before
        /// there were three names for it — one colour over both signs of a
        /// speed modifier, and the speed one over a body carrying an armour
        /// modifier as well. A candidate that separates them is a file rather
        /// than an edit.
        /// </para>
        /// </remarks>
        private Color? TintFor(int speedMagnitude, int armourMagnitude)
        {
            if (speedMagnitude != 0 && armourMagnitude != 0)
            {
                return _look.BothModifiersTint;
            }

            if (speedMagnitude < 0)
            {
                return _look.SpeedEffectTint;
            }

            if (speedMagnitude > 0)
            {
                return _look.HasteEffectTint;
            }

            if (armourMagnitude != 0)
            {
                return _look.ArmourEffectTint;
            }

            return null;
        }

        /// <summary>
        /// Washes the body for the modifiers on it and draws no bar, which is
        /// what a body with no bar to draw gets.
        /// </summary>
        /// <remarks>
        /// <b>A CANDIDATE AND NOT THE GAME.</b> Nothing the game ships calls
        /// this: a tower carrying a modifier is not drawn at all, which is the
        /// third of the five things <c>docs/frames/README.md</c> records as
        /// nobody's decision. It exists so the alternative can be photographed
        /// beside the shipped picture — see <see cref="EffectLook.TowerMarksShown"/>.
        /// </remarks>
        public void ShowModifiers(int speedMagnitude, int armourMagnitude) =>
            Paint(TintFor(speedMagnitude, armourMagnitude));

        /// <summary>
        /// Washes every renderer on the body with <paramref name="tint"/>, or
        /// hands them back their own colour when there is nothing on the unit.
        /// </summary>
        private void Paint(Color? tint)
        {
            if (tint == null)
            {
                if (_washed)
                {
                    foreach (Renderer renderer in _body)
                    {
                        renderer.SetPropertyBlock(null);
                    }

                    _washed = false;
                }

                Wash = null;

                return;
            }

            _wash.SetColor(BaseColorId, tint.Value);
            _wash.SetColor(ColorId, tint.Value);

            foreach (Renderer renderer in _body)
            {
                renderer.SetPropertyBlock(_wash);
            }

            _washed = true;
            Wash = tint;
        }
    }
}
