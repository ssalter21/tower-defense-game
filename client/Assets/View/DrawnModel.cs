using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Puts a unit type's model under the object that draws it, and puts the
    /// row's own atlas on it where the row names one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One place, because a creep and a tower do exactly the same three things
    /// to a model and got them wrong in exactly the same way once already. The
    /// atlas is here for the same reason: a tier is told apart by colour on
    /// both sides of the board.
    /// </para>
    /// <para>
    /// <b>The model's own local ROTATION is left as the importer produced
    /// it.</b> Forcing it to identity looks tidy and tips over any model whose
    /// FBX root carries an axis-conversion rotation — which is how the hitscan
    /// tower came to be lying on its side on the road, while the characters,
    /// whose roots happen to be identity, stood up perfectly and hid the bug.
    /// <b>The scale is multiplied into whatever the importer produced</b> for
    /// the same reason: an FBX root can carry a unit-conversion factor, and
    /// assigning over it resizes the model by whatever that factor was.
    /// </para>
    /// </remarks>
    public static class DrawnModel
    {
        /// <summary>
        /// Instantiates <paramref name="model"/> under <paramref name="host"/>,
        /// at the origin and at <paramref name="scale"/>.
        /// </summary>
        public static GameObject Under(Transform host, GameObject model, float scale)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (model == null) throw new ArgumentNullException(nameof(model));

            if (scale <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(scale), "a unit drawn at no size at all is a unit that never appeared");
            }

            GameObject instance = UnityEngine.Object.Instantiate(model, host, worldPositionStays: false);

            instance.name = model.name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale *= scale;

            return instance;
        }

        /// <summary>
        /// Draws <paramref name="body"/> in <paramref name="skin"/> instead of
        /// the atlas it imported wearing, and hands back the material that does
        /// it. Null skin, null material, nothing touched.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Call it on the bare body, before anything is hung off a bone.</b>
        /// It reaches every renderer under what it is given, and a prop is its
        /// own import off its own pack's atlas, so a character atlas laid over
        /// one draws it in swatches meant for a torso.
        /// </para>
        /// <para>
        /// <b>One material across every slot.</b> A KayKit character is one
        /// mesh against one atlas, which is what makes swapping the atlas the
        /// same operation as swapping the material.
        /// </para>
        /// <para>
        /// <b>How the atlas is filtered is the importer's business.</b> Every
        /// character atlas in the project imports bilinear, so a row that
        /// reached in and set its own filtering would sample differently from
        /// the rung it stands beside — and would set it on the shared asset,
        /// for everything else drawing with it.
        /// </para>
        /// <para>
        /// <b>Whoever calls this destroys what comes back.</b> A material is an
        /// asset instance and destroying the object drawing with it leaves it
        /// behind, so a body per creep would be an orphan per creep. Same rule
        /// as <c>MatchDecorations.DestroyMaterials</c>.
        /// </para>
        /// </remarks>
        public static Material Wear(GameObject body, Texture skin)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            if (skin == null)
            {
                return null;
            }

            Material material = ViewMaterials.Textured(skin.name, skin);

            foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>(true))
            {
                var slots = new Material[Math.Max(renderer.sharedMaterials.Length, 1)];

                for (var i = 0; i < slots.Length; i++)
                {
                    slots[i] = material;
                }

                renderer.sharedMaterials = slots;
            }

            return material;
        }

        /// <summary>
        /// Destroys a material <see cref="Wear"/> made, by whichever of the two
        /// destroys Unity has that works where this is called from.
        /// </summary>
        /// <remarks>
        /// A play-mode <c>Destroy</c> is deferred to the end of the frame and
        /// outside play mode there is no end of frame to defer to — it refuses
        /// by name instead. The roster capture builds and tears down a body per
        /// row from an editor batch, so both paths are live. Same split as
        /// <c>MatchRoot.Retire</c>.
        /// </remarks>
        public static void Discard(Material material)
        {
            if (material == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(material);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(material);
            }
        }
    }
}
