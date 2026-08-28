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

        /// <summary>Which family.</summary>
        public SceneryGroup Group => group;

        /// <summary>Which model of it.</summary>
        public int Variant => variant;

        /// <summary>Stamps a freshly drawn piece.</summary>
        public void Wrote(SceneryGroup wasGroup, int wasVariant)
        {
            group = wasGroup;
            variant = wasVariant;
        }
    }
}
