using UnityEngine;

namespace DIYsect.Dissection
{
    /// <summary>
    /// The pure rule that decides whether a blade point counts as "on the midline". No scene and no network, so it is
    /// easy to test.
    ///
    /// The midline is a straight segment from start (xiphoid) to end (pubis), but the skin above it is curved: the
    /// belly arches up to a few centimetres over the straight segment. The glowing guide follows the skin, so a blade
    /// traced on the guide sits above the segment. A point counts when it is close to the midline sideways, and its
    /// height lies between just below the straight segment and just above the skin there. That accepts a blade on the
    /// skin and a blade pressed into the body, and still rejects one off to the side or high in the air.
    /// </summary>
    public static class IncisionRule
    {
        /// <summary>0 at the start, 1 at the end: how far along the straight segment the point projects (clamped).</summary>
        public static float Along(Vector3 point, Vector3 start, Vector3 end)
        {
            Vector3 line = end - start;
            float lengthSquared = line.sqrMagnitude;
            if (lengthSquared <= 0f)
                return 0f;
            return Mathf.Clamp01(Vector3.Dot(point - start, line) / lengthSquared);
        }

        /// <summary>
        /// Height of the skin at <paramref name="along"/>, read from points evenly spaced from start to end. With fewer
        /// than two points there is no measured skin, so it returns <paramref name="fallback"/>.
        /// </summary>
        public static float SurfaceHeight(Vector3[] surface, float along, float fallback)
        {
            if (surface == null || surface.Length < 2)
                return fallback;
            float position = Mathf.Clamp01(along) * (surface.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(position), surface.Length - 2);
            return Mathf.Lerp(surface[index].y, surface[index + 1].y, position - index);
        }

        /// <summary>True when the point is inside the cutting band around the midline. Also returns where along it lies.</summary>
        public static bool IsOnLine(Vector3 point, Vector3 start, Vector3 end, Vector3[] surface, float tolerance, out float along)
        {
            along = Along(point, start, end);
            Vector3 closest = Vector3.Lerp(start, end, along);
            Vector3 sideways = point - closest;
            sideways.y = 0f;
            if (sideways.magnitude > tolerance)
                return false;
            float bottom = closest.y - tolerance;
            float top = Mathf.Max(closest.y, SurfaceHeight(surface, along, closest.y)) + tolerance;
            return point.y >= bottom && point.y <= top;
        }

        /// <summary>The most the skin rises above the straight segment, never below zero. Sizes the trigger zone.</summary>
        public static float HighestRise(Vector3[] surface, Vector3 start, Vector3 end)
        {
            float rise = 0f;
            if (surface == null)
                return rise;
            foreach (Vector3 point in surface)
            {
                float straight = Vector3.Lerp(start, end, Along(point, start, end)).y;
                rise = Mathf.Max(rise, point.y - straight);
            }
            return rise;
        }
    }
}
