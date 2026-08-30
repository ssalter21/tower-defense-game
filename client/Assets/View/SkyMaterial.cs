using UnityEngine;

namespace View
{
    /// <summary>
    /// The one skybox material, and the six properties worth setting on it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Unity's procedural sky rather than a painted one.</b> A cubemap is six
    /// textures somebody has to author, licence and commit, and what is wanted
    /// here is a graded blue with a sun in it. The procedural shader is that in
    /// four numbers, and it takes the sun's direction from the scene's own
    /// directional light — so the bright part of the sky is on the same side as
    /// the shadows without anybody keeping the two in step.
    /// </para>
    /// <para>
    /// <b>Missing is a real answer, unlike a missing floor shader.</b>
    /// <see cref="ViewMaterials.FindShader"/> throws when it comes up empty,
    /// because a floor drawn magenta looks like every problem except the one it
    /// is. A sky is different: a board with no sky is the board this project
    /// drew until now, clearing to a flat colour, and that is a worse picture
    /// rather than a broken one. So this returns null and
    /// <see cref="Horizon"/> carries on.
    /// </para>
    /// </remarks>
    public static class SkyMaterial
    {
        /// <summary>The shader looked for, and the only one that will do.</summary>
        public const string ShaderName = "Skybox/Procedural";

        /// <summary>A skybox material, or null where the shader is not in the project.</summary>
        public static Material Create(string name)
        {
            Shader shader = Shader.Find(ShaderName);

            if (shader == null)
            {
                return null;
            }

            var material = new Material(shader) { name = name };

            Tint(material, SkySettings.Default);

            return material;
        }

        /// <summary>
        /// Sets the look on a skybox material, whatever built it.
        /// </summary>
        /// <remarks>
        /// Each property is set only where the shader has it, so this is safe
        /// against a material somebody swapped for a different sky shader — it
        /// tints what it can and leaves the rest, rather than logging six
        /// warnings a frame.
        /// </remarks>
        public static void Tint(Material material, SkySettings look)
        {
            if (material == null)
            {
                return;
            }

            SkySettings sky = look.OrDefault();

            Set(material, "_SkyTint", sky.Zenith);
            Set(material, "_GroundColor", sky.Haze);
            Set(material, "_Exposure", sky.Exposure);
            Set(material, "_AtmosphereThickness", sky.Atmosphere);

            // A sun in the sky, and a small one. The disk is drawn where the
            // scene's directional light points, so this is the same sun that
            // casts the board's shadows and not a second one painted on -- the
            // rule the rest of this project keeps about shadows applies to the
            // thing casting them too.
            Set(material, "_SunDisk", SceneFraming.SkySunQuality);
            Set(material, "_SunSize", SceneFraming.SkySunSize);
            Set(material, "_SunSizeConvergence", SceneFraming.SkySunConvergence);
        }

        private static void Set(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void Set(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }
    }
}
