using UnityEngine;

namespace View
{
    /// <summary>
    /// What one piece of scenery in the scene is: which family it came from and
    /// which model of it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>This exists so the bake can read the board back.</b> The editor's
    /// preview draws real objects a human then drags around; turning what they
    /// left into <c>content/dressing.txt</c> means knowing, of each object, what
    /// it is. A transform says where a thing is and nothing about what it is.
    /// </para>
    /// <para>
    /// <b>The mesh could have answered instead, and it was the wrong answer.</b>
    /// Searching the model lists for whichever group holds this mesh works right
    /// up until the same model appears in two groups, or somebody drops in a
    /// mesh from outside them — and then the bake writes a plausible line that
    /// says the wrong thing. Recording it at draw time cannot be ambiguous.
    /// </para>
    /// <para>
    /// It rides along at runtime too, costing one component per rock. That is
    /// cheaper than a second code path, and it makes the hierarchy legible to
    /// anybody clicking around it.
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class ScenerySignature : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Which family this model came from.")]
        private SceneryGroup group;

        [SerializeField]
        [Tooltip("Which model of that family, counted from the scene builder's list and wrapped.")]
        private int variant;

        [SerializeField]
        [Tooltip("Set instead of the family, where a person named one model out of the imported art.")]
        private string model;

        /// <summary>Which family. Meaningless where <see cref="Model"/> is set.</summary>
        public SceneryGroup Group => group;

        /// <summary>Which model of it.</summary>
        public int Variant => variant;

        /// <summary>
        /// The catalogue name this piece was drawn from, or empty where it came
        /// out of a family.
        /// </summary>
        public string Model => model;

        /// <summary>True where this piece names its model rather than a family.</summary>
        public bool IsNamed => !string.IsNullOrEmpty(model);

        /// <summary>Stamps a freshly drawn piece.</summary>
        public void Wrote(SceneryGroup wasGroup, int wasVariant)
        {
            group = wasGroup;
            variant = wasVariant;
            model = null;
        }

        /// <summary>
        /// Stamps a piece that names one model out of the imported art.
        /// </summary>
        /// <remarks>
        /// The family is left at whatever it was and is not read again while
        /// <see cref="Model"/> is set. Recording both would invite a bake to
        /// pick the one that happened to be checked first, which is the
        /// ambiguity this component exists to remove.
        /// </remarks>
        public void WroteNamed(string wasModel)
        {
            model = wasModel;
        }
    }
}
