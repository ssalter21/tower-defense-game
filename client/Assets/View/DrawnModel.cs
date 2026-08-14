using System;
using UnityEngine;

namespace View
{
    /// <summary>
    /// Puts a unit type's model under the object that draws it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One place, because a creep and a tower do exactly the same three things
    /// to a model and got them wrong in exactly the same way once already.
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
    }
}
