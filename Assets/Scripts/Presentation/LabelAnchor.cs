using UnityEngine;

namespace DIYsect.Presentation
{
    /// <summary>
    /// Where text goes around an object, worked out from the object's world bounds instead of a fixed height, so a small
    /// gallbladder and a large liver both get their label just clear of their own surface. World up is always up, even
    /// while the object is turned over in a hand. Pure maths, tested without a scene.
    /// </summary>
    public static class LabelAnchor
    {
        /// <summary>Default clearance between an organ's top and its label, in metres.</summary>
        public const float Gap = 0.025f;

        /// <summary>The point <paramref name="gap"/> metres straight above the top centre of <paramref name="bounds"/>.</summary>
        public static Vector3 Above(Bounds bounds, float gap = Gap)
        {
            return new Vector3(bounds.center.x, bounds.max.y + gap, bounds.center.z);
        }

        /// <summary>
        /// The point just clear of <paramref name="bounds"/> on the viewer's right, at the object's middle height: used for
        /// a hint that must never sit on top of the anatomy. <paramref name="viewerRight"/> is the viewer's right direction;
        /// only its horizontal part is used.
        /// </summary>
        public static Vector3 Beside(Bounds bounds, Vector3 viewerRight, float gap = Gap)
        {
            Vector3 flatRight = new Vector3(viewerRight.x, 0f, viewerRight.z);
            flatRight = flatRight.sqrMagnitude > 1e-6f ? flatRight.normalized : Vector3.right;
            float reach = Mathf.Abs(Vector3.Dot(bounds.extents, new Vector3(Mathf.Abs(flatRight.x), 0f, Mathf.Abs(flatRight.z))));
            return bounds.center + flatRight * (reach + gap);
        }

        /// <summary>
        /// The world bounds of every enabled renderer under <paramref name="root"/>, ignoring any renderer under
        /// <paramref name="ignore"/> (the label itself). Falls back to a zero size box at the root when nothing renders.
        /// </summary>
        public static Bounds RendererBounds(Transform root, Transform ignore = null)
        {
            bool found = false;
            var bounds = new Bounds(root.position, Vector3.zero);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || (ignore != null && renderer.transform.IsChildOf(ignore)))
                    continue;
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
            return bounds;
        }
    }
}
