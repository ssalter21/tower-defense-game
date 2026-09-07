using System.Collections.Generic;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Every number and colour that decides what an <i>effect</i> looks like,
    /// read through one object so a capture can ask "what if it were this
    /// instead" without editing the file that says what it is.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is not a second set of values, and that is the whole design.</b>
    /// Every member below answers out of <see cref="MatchTuning"/> unless an
    /// override was handed in for that exact name, so the shipped look has one
    /// home and this cannot drift from it. <see cref="Shipped"/> overrides
    /// nothing and is what the game plays with; nothing in the built player
    /// ever constructs any other one.
    /// </para>
    /// <para>
    /// <b>Why it exists.</b> Every number and colour it covers is declared a
    /// placeholder in <see cref="MatchTuning"/>'s own header, and a placeholder
    /// is signed by looking at the alternatives beside it rather than by
    /// reading the constant. The alternatives have to be drawn through the real
    /// match at the size the game is played at — a sheet and the built player
    /// were shown to disagree — so what a capture needs is a way to play the
    /// recorded match with one candidate look on and photograph it. Promoting a
    /// candidate by hand-editing <see cref="MatchTuning"/>, rendering, and
    /// editing it back is the alternative, and it puts an unsigned value in the
    /// file the game ships from for as long as the render takes.
    /// </para>
    /// <para>
    /// <b>The names are <see cref="MatchTuning"/>'s own names.</b> A candidate
    /// file names the constant it is standing in for, which is what makes it
    /// readable next to the thing it is a candidate for, and an override for a
    /// name that does not exist is a fault rather than a silent no-op — see the
    /// parser in <c>View.Editor.EffectLookFile</c>. A look drawn from a misspelt
    /// key is a picture of the shipped value wearing a candidate's filename,
    /// which is exactly the species of picture this project keeps deleting.
    /// </para>
    /// </remarks>
    public sealed class EffectLook
    {
        /// <summary>
        /// The look the game ships: every member answers straight out of
        /// <see cref="MatchTuning"/>.
        /// </summary>
        public static readonly EffectLook Shipped = new EffectLook(null, null);

        private readonly IReadOnlyDictionary<string, float> _numbers;

        private readonly IReadOnlyDictionary<string, Color> _colours;

        /// <summary>
        /// A look that answers out of <see cref="MatchTuning"/> except where one
        /// of the two tables names a member.
        /// </summary>
        /// <param name="numbers">
        /// Overridden numbers, keyed by the <see cref="MatchTuning"/> member
        /// name. Null or empty means no number is overridden. A member that
        /// counts things reads its override rounded, so a candidate file carries
        /// one kind of literal.
        /// </param>
        /// <param name="colours">
        /// Overridden colours, keyed the same way. Null or empty means no colour
        /// is overridden.
        /// </param>
        public EffectLook(
            IReadOnlyDictionary<string, float> numbers,
            IReadOnlyDictionary<string, Color> colours)
        {
            _numbers = numbers;
            _colours = colours;
        }

        /// <summary>True when this look stands in front of nothing at all.</summary>
        public bool IsShipped =>
            (_numbers == null || _numbers.Count == 0) &&
            (_colours == null || _colours.Count == 0);

        /// <summary>How many members this look stands in front of.</summary>
        public int OverriddenCount =>
            (_numbers?.Count ?? 0) + (_colours?.Count ?? 0);

        /// <summary>The overridden number for <paramref name="name"/>, or the shipped one.</summary>
        private float N(string name, float shipped) =>
            _numbers != null && _numbers.TryGetValue(name, out float value) ? value : shipped;

        /// <summary>
        /// The same, rounded, for a member that counts things. Rounded rather
        /// than truncated so a candidate written <c>11.5</c> is twelve sides and
        /// not eleven.
        /// </summary>
        private int I(string name, int shipped) =>
            _numbers != null && _numbers.TryGetValue(name, out float value)
                ? Mathf.RoundToInt(value)
                : shipped;

        /// <summary>The overridden colour for <paramref name="name"/>, or the shipped one.</summary>
        private Color C(string name, Color shipped) =>
            _colours != null && _colours.TryGetValue(name, out Color value) ? value : shipped;

        /// <summary>
        /// <see cref="MatchTuning.AuraDiscAlpha"/>, or the candidate standing
        /// in front of it.
        /// </summary>
        public float AuraDiscAlpha =>
            N(nameof(MatchTuning.AuraDiscAlpha), MatchTuning.AuraDiscAlpha);

        /// <summary>
        /// <see cref="MatchTuning.AuraDiscThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float AuraDiscThickness =>
            N(nameof(MatchTuning.AuraDiscThickness), MatchTuning.AuraDiscThickness);

        /// <summary>
        /// <see cref="MatchTuning.ArmourStripBandFraction"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float ArmourStripBandFraction =>
            N(nameof(MatchTuning.ArmourStripBandFraction), MatchTuning.ArmourStripBandFraction);

        /// <summary>
        /// <see cref="MatchTuning.ArmourStripThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float ArmourStripThickness =>
            N(nameof(MatchTuning.ArmourStripThickness), MatchTuning.ArmourStripThickness);

        /// <summary>
        /// <see cref="MatchTuning.FloorClearance"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float FloorClearance =>
            N(nameof(MatchTuning.FloorClearance), MatchTuning.FloorClearance);

        /// <summary>
        /// <see cref="MatchTuning.HitSparkHeight"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float HitSparkHeight =>
            N(nameof(MatchTuning.HitSparkHeight), MatchTuning.HitSparkHeight);

        /// <summary>
        /// <see cref="MatchTuning.HitSparkRadius"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float HitSparkRadius =>
            N(nameof(MatchTuning.HitSparkRadius), MatchTuning.HitSparkRadius);

        /// <summary>
        /// <see cref="MatchTuning.KnifeBladeWidthFraction"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float KnifeBladeWidthFraction =>
            N(nameof(MatchTuning.KnifeBladeWidthFraction), MatchTuning.KnifeBladeWidthFraction);

        /// <summary>
        /// <see cref="MatchTuning.KnifeGuardFraction"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float KnifeGuardFraction =>
            N(nameof(MatchTuning.KnifeGuardFraction), MatchTuning.KnifeGuardFraction);

        /// <summary>
        /// <see cref="MatchTuning.KnifeLength"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float KnifeLength =>
            N(nameof(MatchTuning.KnifeLength), MatchTuning.KnifeLength);

        /// <summary>
        /// <see cref="MatchTuning.KnifeThicknessFraction"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float KnifeThicknessFraction =>
            N(nameof(MatchTuning.KnifeThicknessFraction), MatchTuning.KnifeThicknessFraction);

        /// <summary>
        /// <see cref="MatchTuning.LongShotThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float LongShotThickness =>
            N(nameof(MatchTuning.LongShotThickness), MatchTuning.LongShotThickness);

        /// <summary>
        /// <see cref="MatchTuning.MagicBoltLength"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float MagicBoltLength =>
            N(nameof(MatchTuning.MagicBoltLength), MatchTuning.MagicBoltLength);

        /// <summary>
        /// <see cref="MatchTuning.MagicBoltThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float MagicBoltThickness =>
            N(nameof(MatchTuning.MagicBoltThickness), MatchTuning.MagicBoltThickness);

        /// <summary>
        /// <see cref="MatchTuning.MortarBurstWidthFraction"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float MortarBurstWidthFraction =>
            N(nameof(MatchTuning.MortarBurstWidthFraction), MatchTuning.MortarBurstWidthFraction);

        /// <summary>
        /// <see cref="MatchTuning.MuzzleFlashRadius"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float MuzzleFlashRadius =>
            N(nameof(MatchTuning.MuzzleFlashRadius), MatchTuning.MuzzleFlashRadius);

        /// <summary>
        /// <see cref="MatchTuning.TracerThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float TracerThickness =>
            N(nameof(MatchTuning.TracerThickness), MatchTuning.TracerThickness);

        /// <summary>
        /// <see cref="MatchTuning.UnitBarHeight"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float UnitBarHeight =>
            N(nameof(MatchTuning.UnitBarHeight), MatchTuning.UnitBarHeight);

        /// <summary>
        /// <see cref="MatchTuning.UnitBarLength"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float UnitBarLength =>
            N(nameof(MatchTuning.UnitBarLength), MatchTuning.UnitBarLength);

        /// <summary>
        /// <see cref="MatchTuning.UnitBarThickness"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public float UnitBarThickness =>
            N(nameof(MatchTuning.UnitBarThickness), MatchTuning.UnitBarThickness);

        /// <summary>
        /// <see cref="MatchTuning.ArmourStripSides"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int ArmourStripSides =>
            I(nameof(MatchTuning.ArmourStripSides), MatchTuning.ArmourStripSides);

        /// <summary>
        /// <see cref="MatchTuning.ArmourStripTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int ArmourStripTicks =>
            I(nameof(MatchTuning.ArmourStripTicks), MatchTuning.ArmourStripTicks);

        /// <summary>
        /// <see cref="MatchTuning.BlessingGlowTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int BlessingGlowTicks =>
            I(nameof(MatchTuning.BlessingGlowTicks), MatchTuning.BlessingGlowTicks);

        /// <summary>
        /// <see cref="MatchTuning.BubbleRingTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int BubbleRingTicks =>
            I(nameof(MatchTuning.BubbleRingTicks), MatchTuning.BubbleRingTicks);

        /// <summary>
        /// <see cref="MatchTuning.ConsecrationLightTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int ConsecrationLightTicks =>
            I(nameof(MatchTuning.ConsecrationLightTicks), MatchTuning.ConsecrationLightTicks);

        /// <summary>
        /// <see cref="MatchTuning.FrostSpikeTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int FrostSpikeTicks =>
            I(nameof(MatchTuning.FrostSpikeTicks), MatchTuning.FrostSpikeTicks);

        /// <summary>
        /// <see cref="MatchTuning.GroundShockTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int GroundShockTicks =>
            I(nameof(MatchTuning.GroundShockTicks), MatchTuning.GroundShockTicks);

        /// <summary>
        /// <see cref="MatchTuning.HasteRingTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int HasteRingTicks =>
            I(nameof(MatchTuning.HasteRingTicks), MatchTuning.HasteRingTicks);

        /// <summary>
        /// <see cref="MatchTuning.HexPlateTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int HexPlateTicks =>
            I(nameof(MatchTuning.HexPlateTicks), MatchTuning.HexPlateTicks);

        /// <summary>
        /// <see cref="MatchTuning.HitSparkTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int HitSparkTicks =>
            I(nameof(MatchTuning.HitSparkTicks), MatchTuning.HitSparkTicks);

        /// <summary>
        /// <see cref="MatchTuning.KnifeFlightTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int KnifeFlightTicks =>
            I(nameof(MatchTuning.KnifeFlightTicks), MatchTuning.KnifeFlightTicks);

        /// <summary>
        /// <see cref="MatchTuning.LongShotTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int LongShotTicks =>
            I(nameof(MatchTuning.LongShotTicks), MatchTuning.LongShotTicks);

        /// <summary>
        /// <see cref="MatchTuning.MagicBoltFlightTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int MagicBoltFlightTicks =>
            I(nameof(MatchTuning.MagicBoltFlightTicks), MatchTuning.MagicBoltFlightTicks);

        /// <summary>
        /// <see cref="MatchTuning.MortarBurstShards"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int MortarBurstShards =>
            I(nameof(MatchTuning.MortarBurstShards), MatchTuning.MortarBurstShards);

        /// <summary>
        /// <see cref="MatchTuning.MortarBurstTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int MortarBurstTicks =>
            I(nameof(MatchTuning.MortarBurstTicks), MatchTuning.MortarBurstTicks);

        /// <summary>
        /// <see cref="MatchTuning.MuzzleFlashTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int MuzzleFlashTicks =>
            I(nameof(MatchTuning.MuzzleFlashTicks), MatchTuning.MuzzleFlashTicks);

        /// <summary>
        /// <see cref="MatchTuning.SlowRingTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int SlowRingTicks =>
            I(nameof(MatchTuning.SlowRingTicks), MatchTuning.SlowRingTicks);

        /// <summary>
        /// <see cref="MatchTuning.TracerTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int TracerTicks =>
            I(nameof(MatchTuning.TracerTicks), MatchTuning.TracerTicks);

        /// <summary>
        /// <see cref="MatchTuning.WardDomeTicks"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public int WardDomeTicks =>
            I(nameof(MatchTuning.WardDomeTicks), MatchTuning.WardDomeTicks);

        /// <summary>
        /// <see cref="MatchTuning.ArmourStripColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color ArmourStripColor =>
            C(nameof(MatchTuning.ArmourStripColor), MatchTuning.ArmourStripColor);

        /// <summary>
        /// <see cref="MatchTuning.BlessingGlowColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color BlessingGlowColor =>
            C(nameof(MatchTuning.BlessingGlowColor), MatchTuning.BlessingGlowColor);

        /// <summary>
        /// <see cref="MatchTuning.BubbleRingColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color BubbleRingColor =>
            C(nameof(MatchTuning.BubbleRingColor), MatchTuning.BubbleRingColor);

        /// <summary>
        /// <see cref="MatchTuning.ConsecrationLightColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color ConsecrationLightColor =>
            C(nameof(MatchTuning.ConsecrationLightColor), MatchTuning.ConsecrationLightColor);

        /// <summary>
        /// <see cref="MatchTuning.FrostSpikeColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color FrostSpikeColor =>
            C(nameof(MatchTuning.FrostSpikeColor), MatchTuning.FrostSpikeColor);

        /// <summary>
        /// <see cref="MatchTuning.GroundShockColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color GroundShockColor =>
            C(nameof(MatchTuning.GroundShockColor), MatchTuning.GroundShockColor);

        /// <summary>
        /// <see cref="MatchTuning.HasteRingColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color HasteRingColor =>
            C(nameof(MatchTuning.HasteRingColor), MatchTuning.HasteRingColor);

        /// <summary>
        /// <see cref="MatchTuning.HealthSegmentColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color HealthSegmentColor =>
            C(nameof(MatchTuning.HealthSegmentColor), MatchTuning.HealthSegmentColor);

        /// <summary>
        /// <see cref="MatchTuning.HexPlateColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color HexPlateColor =>
            C(nameof(MatchTuning.HexPlateColor), MatchTuning.HexPlateColor);

        /// <summary>
        /// <see cref="MatchTuning.HitSparkColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color HitSparkColor =>
            C(nameof(MatchTuning.HitSparkColor), MatchTuning.HitSparkColor);

        /// <summary>
        /// <see cref="MatchTuning.KnifeColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color KnifeColor =>
            C(nameof(MatchTuning.KnifeColor), MatchTuning.KnifeColor);

        /// <summary>
        /// <see cref="MatchTuning.LongShotColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color LongShotColor =>
            C(nameof(MatchTuning.LongShotColor), MatchTuning.LongShotColor);

        /// <summary>
        /// <see cref="MatchTuning.MagicBoltColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color MagicBoltColor =>
            C(nameof(MatchTuning.MagicBoltColor), MatchTuning.MagicBoltColor);

        /// <summary>
        /// <see cref="MatchTuning.MortarBurstColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color MortarBurstColor =>
            C(nameof(MatchTuning.MortarBurstColor), MatchTuning.MortarBurstColor);

        /// <summary>
        /// <see cref="MatchTuning.MuzzleFlashColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color MuzzleFlashColor =>
            C(nameof(MatchTuning.MuzzleFlashColor), MatchTuning.MuzzleFlashColor);

        /// <summary>
        /// <see cref="MatchTuning.ProjectileColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color ProjectileColor =>
            C(nameof(MatchTuning.ProjectileColor), MatchTuning.ProjectileColor);

        /// <summary>
        /// <see cref="MatchTuning.ShieldSegmentColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color ShieldSegmentColor =>
            C(nameof(MatchTuning.ShieldSegmentColor), MatchTuning.ShieldSegmentColor);

        /// <summary>
        /// <see cref="MatchTuning.SlowRingColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color SlowRingColor =>
            C(nameof(MatchTuning.SlowRingColor), MatchTuning.SlowRingColor);

        /// <summary>
        /// <see cref="MatchTuning.TracerColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color TracerColor =>
            C(nameof(MatchTuning.TracerColor), MatchTuning.TracerColor);

        /// <summary>
        /// <see cref="MatchTuning.WardDomeColor"/>, or the candidate
        /// standing in front of it.
        /// </summary>
        public Color WardDomeColor =>
            C(nameof(MatchTuning.WardDomeColor), MatchTuning.WardDomeColor);
        /// <summary>
        /// True when this look asks for something the game does not draw at
        /// all. Stored beside the numbers so a candidate file carries one kind
        /// of literal; anything at or above a half is on.
        /// </summary>
        private bool B(string name) =>
            _numbers != null && _numbers.TryGetValue(name, out float value) && value >= 0.5f;

        // ---------------------------------------------------------------
        // The candidate axes with no constant behind them
        // ---------------------------------------------------------------
        //
        // EVERY MEMBER BELOW SHIPS AS THE PICTURE THE GAME ALREADY DRAWS, and
        // that is what makes them look and not behaviour. Each one is a
        // placeholder decision docs/frames/README.md records as nobody's --
        // one colour over both signs of a speed modifier, the speed colour over
        // a body carrying an armour one as well, no marks on a tower at all, a
        // bar that never turns, and two segments that are both shares of the
        // authored health so together they run past a whole bar. There is no
        // constant in MatchTuning for any of them because the decision was to
        // draw one thing where two were possible, and a constant for the road
        // not taken would be a value nobody chose sitting in the file the game
        // ships from.
        //
        // So the default here is the road that was taken, expressed as itself:
        // the haste tint IS the speed tint until somebody says otherwise, and
        // the three flags are off. A capture turns one on to photograph the
        // alternative beside the shipped picture, which is the whole of what
        // signing one of these needs.

        /// <summary>
        /// Whether a second bar is drawn across the first, so the pool is
        /// readable from every quadrant of the orbit. Off, which is the shipped
        /// answer: one bar, read end-on from two of the four.
        /// </summary>
        public bool UnitBarCrossed => B(nameof(UnitBarCrossed));

        /// <summary>
        /// Whether health and pool share one bar's length instead of both being
        /// shares of the authored health. Off, which is the shipped answer: a
        /// creep at full health with a pool worth two fifths of it draws one and
        /// two fifths of a bar.
        /// </summary>
        public bool UnitBarClamped => B(nameof(UnitBarClamped));

    }
}
