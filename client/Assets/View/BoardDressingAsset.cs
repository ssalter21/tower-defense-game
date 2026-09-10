using UnityEngine;

namespace View
{
    /// <summary>
    /// The dressing settings as an asset, so they can be slid rather than
    /// recompiled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This is the one departure from "every tunable number is a const in
    /// one file".</b> <see cref="MatchTuning"/> and <see cref="SceneFraming"/>
    /// are constants because nobody sweeps them — they are set once by argument
    /// and then read. These are swept: finding the density a board reads well at
    /// means moving a slider and looking, dozens of times, and a recompile
    /// between each one turns a five-minute judgement into an afternoon.
    /// </para>
    /// <para>
    /// <b>What it costs is that the numbers are now a YAML blob rather than a
    /// diff a human reads.</b> That is accepted here and only here, and the
    /// asset is small enough that the blob is still legible. Everything the
    /// setting cannot express — where one particular tree goes — is in
    /// <c>content/dressing.txt</c>, which is plain text on purpose.
    /// </para>
    /// <para>
    /// <b>Ranges are on the fields rather than checked in code.</b> A slider
    /// that cannot reach a nonsense value needs no validation, and a chance
    /// above one would simply mean "always" without saying so.
    /// </para>
    /// </remarks>
    [CreateAssetMenu(menuName = "Board/Dressing", fileName = "BoardDressing")]
    public sealed class BoardDressingAsset : ScriptableObject
    {
        [Header("How much fills a whole hex")]
        [Range(0f, 1f)]
        [Tooltip("Chance a cell away from the corridor is filled by trees.")]
        [SerializeField]
        private float groveChance = 0.30f;

        [Range(0f, 1f)]
        [Tooltip("Chance a border cell is filled by a mountain. Tried first.")]
        [SerializeField]
        private float peakChance = 0.34f;

        [Range(0f, 1f)]
        [Tooltip("Chance a border cell that got no mountain is filled by trees.")]
        [SerializeField]
        private float borderGroveChance = 0.32f;

        [Header("What stands at the rim")]
        [Range(0f, 1f)]
        [Tooltip("Chance an undressed cell gets at least one small prop.")]
        [SerializeField]
        private float propChance = 0.42f;

        [Range(0f, 1f)]
        [Tooltip("Chance a cell that got one prop gets a second.")]
        [SerializeField]
        private float secondPropChance = 0.30f;

        [Range(0f, 1f)]
        [Tooltip("Chance a cell touching the corridor carries a camp instead.")]
        [SerializeField]
        private float campChance = 0.14f;

        [Range(0.5f, 4f)]
        [Tooltip("How much bigger than authored a small prop is drawn. The pack authors "
            + "them for a camera standing on the board; this one frames all of it.")]
        [SerializeField]
        private float propScale = 1.7f;

        [Range(0.4f, 0.95f)]
        [Tooltip("How far out a prop stands, as a fraction of the circumradius. The near end "
            + "must clear the middle of the hex, which is where a tower is drawn.")]
        [SerializeField]
        private float rimNear = 0.52f;

        [Range(0.4f, 0.95f)]
        [Tooltip("The outer end of that band.")]
        [SerializeField]
        private float rimFar = 0.70f;

        [Header("Sky")]
        [Range(0, 24)]
        [SerializeField]
        private int cloudCount = 5;

        [Range(2f, 20f)]
        [Tooltip("How high the lowest cloud floats, in metres.")]
        [SerializeField]
        private float cloudHeight = 6f;

        [Range(0f, 10f)]
        [Tooltip("How much higher than that a cloud may be, in metres.")]
        [SerializeField]
        private float cloudSpread = 2.5f;

        [Range(0f, 1f)]
        [Tooltip("The chance a cell standing over a lower one carries a mound on the lip of the drop.")]
        [SerializeField]
        private float ridgeChance = 0.5f;

        [Range(0f, 4f)]
        [Tooltip("How far a cell on the board's edge hangs below its own face, in metres.")]
        [SerializeField]
        private float rimDrop = 1f;

        [Range(-1, 8)]
        [Tooltip("The level at and below which ground is drawn as water. -1 is a board with none.")]
        [SerializeField]
        private int waterLevel = -1;

        /// <summary>
        /// The numbers, as the pure chooser wants them.
        /// </summary>
        /// <remarks>
        /// A copy rather than this object, so that <see cref="BoardScenery"/>
        /// never holds an engine reference and stays testable with no editor —
        /// and so a slider moved mid-draw cannot change the answer halfway
        /// through a board.
        /// </remarks>
        public DressingSettings Settings() =>
            new DressingSettings
            {
                GroveChance = groveChance,
                PeakChance = peakChance,
                BorderGroveChance = borderGroveChance,
                PropChance = propChance,
                SecondPropChance = secondPropChance,
                CampChance = campChance,
                PropScale = propScale,
                RimNear = Mathf.Min(rimNear, rimFar),
                RimFar = Mathf.Max(rimNear, rimFar),
                CloudCount = cloudCount,
                CloudHeight = cloudHeight,
                CloudSpread = cloudSpread,
                RidgeChance = ridgeChance,
                RimDrop = rimDrop,
                WaterLevel = waterLevel,
            };
    }
}
