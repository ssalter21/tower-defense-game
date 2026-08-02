using UnityEngine;

namespace View
{
    /// <summary>
    /// Plain, lit, single-colour materials — the whole of this project's
    /// surfacing, for as long as it stays a blockout.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two materials distinguished by colour and nothing else, because
    /// anything more would be an art decision and art decisions in this project
    /// belong to the developer. There is no texture, no variation and no
    /// second shader.
    /// </para>
    /// <para>
    /// The shader is looked up by name from a short list rather than assumed,
    /// and the failure is loud. A material built on a null shader renders
    /// magenta, which looks like an import problem, a licence problem or a
    /// pipeline problem — every explanation except the true one.
    /// </para>
    /// </remarks>
    public static class ViewMaterials
    {
        /// <summary>
        /// The shaders tried, in order. The universal render pipeline is what
        /// this project is set up with; the built-in standard shader is the
        /// fallback for a project that has not been, so the failure is a
        /// different-looking floor rather than no floor.
        /// </summary>
        public static readonly string[] ShaderNames =
        {
            "Universal Render Pipeline/Lit",
            "Standard",
        };

        /// <summary>The colour property of both shaders above, in order.</summary>
        private static readonly string[] ColorProperties = { "_BaseColor", "_Color" };

        /// <summary>Builds one plain lit material.</summary>
        public static Material Create(string name, Color color)
        {
            var material = new Material(FindShader()) { name = name };

            foreach (string property in ColorProperties)
            {
                if (material.HasProperty(property))
                {
                    material.SetColor(property, color);
                }
            }

            return material;
        }

        /// <summary>
        /// The first shader in <see cref="ShaderNames"/> that exists, or a
        /// throw naming every name that was tried.
        /// </summary>
        public static Shader FindShader()
        {
            foreach (string name in ShaderNames)
            {
                Shader shader = Shader.Find(name);

                if (shader != null)
                {
                    return shader;
                }
            }

            throw new System.InvalidOperationException(
                "None of these shaders is in this project: " + string.Join(", ", ShaderNames)
                + ". A material built on a null shader draws magenta, which looks like every problem "
                + "except the one it is.");
        }
    }
}
