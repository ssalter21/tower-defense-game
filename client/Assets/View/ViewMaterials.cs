using UnityEngine;

namespace View
{
    /// <summary>
    /// Plain lit materials: a single colour, or a single texture. The whole of
    /// this project's surfacing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The colour pair is the blockout and the textured one is the art.</b>
    /// Road and grass are distinguished by colour and nothing else, which is
    /// what a floor looks like before anybody has chosen a tile;
    /// <see cref="Textured"/> is what it looks like afterwards, and it takes an
    /// atlas somebody picked rather than finding one. Both are still one shader
    /// and no variation — that much of the blockout rule survives the art
    /// arriving.
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

        /// <summary>The albedo map property of both shaders above, in order.</summary>
        private static readonly string[] MapProperties = { "_BaseMap", "_MainTex" };

        /// <summary>The smoothness property of both shaders above, in order.</summary>
        private static readonly string[] SmoothnessProperties = { "_Smoothness", "_Glossiness" };

        /// <summary>
        /// One plain lit material with every trace of shine taken off it.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>For anything very large and very flat, where the default is a
        /// bug.</b> Both shaders default to half smoothness, which on a hex tile
        /// is a faint sheen nobody notices and on a plane two hundred metres
        /// across is a catastrophe: the further part of it is seen at a grazing
        /// angle, Fresnel drives the specular term up, and the ground goes to a
        /// blown white band along the horizon.
        /// </para>
        /// <para>
        /// <b>It was diagnosed off the pixels and not off the shader.</b> The
        /// white band looked exactly like fog piled up at the far clip, and two
        /// rounds of moving the fog did nothing to it. Sampling a column of the
        /// frame is what showed the value at 255 where the fog colour would have
        /// been 222 -- brighter than the thing the ground was supposedly fading
        /// into, which no amount of fog can do.
        /// </para>
        /// </remarks>
        public static Material Matte(string name, Color color)
        {
            Material material = Create(name, color);

            foreach (string property in SmoothnessProperties)
            {
                if (material.HasProperty(property))
                {
                    material.SetFloat(property, 0f);
                }
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            // The specular highlight has its own toggle on the URP shader, and
            // switching it off is what actually removes the term rather than
            // making it small.
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0f);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }

            if (material.HasProperty("_EnvironmentReflections"))
            {
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }

            return material;
        }

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
        /// Builds one lit material wearing <paramref name="atlas"/>.
        /// </summary>
        /// <remarks>
        /// <b>The base colour is set to white, and that is not a detail.</b>
        /// Both shaders multiply the map by it, so a material left on the
        /// project's grass colour draws a correctly-mapped tile in the wrong
        /// hue — which reads as the atlas being wrong rather than as the tint
        /// being wrong, and sends anybody looking at it into the importer.
        /// </remarks>
        public static Material Textured(string name, Texture atlas)
        {
            var material = new Material(FindShader()) { name = name };

            foreach (string property in ColorProperties)
            {
                if (material.HasProperty(property))
                {
                    material.SetColor(property, Color.white);
                }
            }

            foreach (string property in MapProperties)
            {
                if (material.HasProperty(property))
                {
                    material.SetTexture(property, atlas);

                    break;
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
