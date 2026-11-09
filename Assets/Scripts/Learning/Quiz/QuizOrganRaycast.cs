// Authority: none. A local physics query; it reads the scene and changes no shared state.
using UnityEngine;
using DIYsect.Networking;

namespace DIYsect.Learning
{
    /// <summary>
    /// Finds which organ of the shared body a ray hits first, so a quiz answer is the organ the classmate actually tapped.
    /// </summary>
    public static class QuizOrganRaycast
    {
        /// <summary>
        /// Casts the ray and returns true if the first thing it hits belongs to one of the body's organs.
        /// Triggers are ignored.
        /// </summary>
        public static bool TryHit(Ray ray, SharedBody body, float maxDistance, LayerMask layers, out int organIndex)
        {
            organIndex = -1;
            if (body == null || !Physics.Raycast(ray, out RaycastHit hit, maxDistance, layers, QueryTriggerInteraction.Ignore))
                return false;
            organIndex = IndexOf(body, hit.collider.transform);
            return organIndex >= 0;
        }

        /// <summary>The index of the organ that contains the transform, or -1 when it is not part of an organ.</summary>
        public static int IndexOf(SharedBody body, Transform hit)
        {
            var organs = body.Organs;
            for (int i = 0; i < organs.Count; i++)
                if (organs[i] != null && hit.IsChildOf(organs[i].transform))
                    return i;
            return -1;
        }
    }
}
