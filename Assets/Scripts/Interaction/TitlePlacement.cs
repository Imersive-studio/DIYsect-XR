using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Where the inspection title floats: above the organ and pushed back to its far side from the viewer, so from where
    /// the viewer stands it reads above the anatomy and never in front of it. Pure maths, tested without a scene.
    /// </summary>
    public static class TitlePlacement
    {
        /// <summary>Extra metres of push beyond the organ's own half size.</summary>
        public const float Push = 0.04f;

        /// <summary>
        /// The bottom centre of the title. <paramref name="gap"/> is the clearance above the organ's top;
        /// <paramref name="viewerPosition"/> is the camera.
        /// </summary>
        public static Vector3 Position(Bounds bounds, Vector3 viewerPosition, float gap)
        {
            Vector3 away = new Vector3(bounds.center.x - viewerPosition.x, 0f, bounds.center.z - viewerPosition.z);
            if (away.sqrMagnitude < 1e-6f)
                return new Vector3(bounds.center.x, bounds.max.y + gap, bounds.center.z);
            away.Normalize();
            float reach = Mathf.Abs(away.x) * bounds.extents.x + Mathf.Abs(away.z) * bounds.extents.z;
            Vector3 flat = bounds.center + away * (reach + Push);
            return new Vector3(flat.x, bounds.max.y + gap, flat.z);
        }
    }
}
