using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace View
{
    /// <summary>
    /// What one unit is carrying, drawn: the pool standing in front of its
    /// health, as a second segment of a bar above it.
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
    /// <b>Nothing here says a payload is in force, and that is the decision
    /// rather than an omission.</b> A wash of one colour per payload used to,
    /// and Sam took it off on 7 Sep 2026 along with every shape drawn on the
    /// bodies an aura found: what an aura is doing is read off the translucent
    /// circle it lays on the floor, and whether a particular body is inside
    /// that circle is read off where the body is standing. See
    /// <c>docs/decision-log.md</c>. The class keeps its name because that slot
    /// is expected back when the effects are done properly.
    /// </para>
    /// <para>
    /// <b>The bar that is left is still a placeholder.</b> Two segments above
    /// the body are the plainest thing that says "there is a pool in front of
    /// its health", what a shielded body should actually look like is a
    /// decision nobody has taken, and every distance it uses is in
    /// <see cref="MatchTuning"/>, in one section that says the same.
    /// </para>
    /// <para>
    /// <b>The bar appears only while there is a pool to draw.</b> A health bar
    /// over every creep in the match is a much larger decision than this, and it
    /// is not one this takes; the health segment is here so the shield has
    /// something to be a share of, and both segments are shares of the health
    /// pool the unit's row authored.
    /// </para>
    /// </remarks>
    public sealed class EffectMarks
    {
        /// <summary>
        /// Where the bar's size is read from.
        /// <see cref="EffectLook.Shipped"/> unless a capture handed one in; see
        /// <see cref="EffectLook"/> for why a capture would.
        /// </summary>
        private readonly EffectLook _look;

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

        /// <summary>The bar, hidden until something grants a pool. For tests.</summary>
        public Transform Bar => _bar;

        /// <summary>The health segment of that bar. For tests.</summary>
        public Transform HealthSegment => _health;

        /// <summary>The segment standing for the pool. For tests.</summary>
        public Transform ShieldSegment => _shield;

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
        /// Builds the marks: hangs a hidden two-segment bar off
        /// <paramref name="host"/>.
        /// </summary>
        /// <param name="host">The object the unit is drawn under.</param>
        /// <param name="health">
        /// The health segment's material, or null for a body that is a portrait
        /// rather than a creep in a match. Then no bar is built and
        /// <see cref="Show"/> refuses by name — a sheet makes and throws away a
        /// body per row, so a material made here would be one leaked per row.
        /// </param>
        /// <param name="shield">The pool segment's material, or null for the same.</param>
        public void Build(Transform host, Material health, Material shield)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));

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
        public void Show(int hp, int maxHp, int shield)
        {
            if (_bar == null)
            {
                throw new InvalidOperationException(
                    "These marks were built without a bar, which is what a body drawn for a contact sheet "
                    + "or an art preview gets. Nothing behind such a body is a snapshot, so there is "
                    + "nothing for it to be carrying — build it with the two segment materials if it is "
                    + "a creep in a match.");
            }

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
    }
}
