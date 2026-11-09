// Authority: none. Pure local math for the classmate's sketch; no Unity objects.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>The small decisions behind drawing on the body with a finger, kept apart from input so they can be tested alone.</summary>
    public static class SketchRules
    {
        /// <summary>
        /// True when the current touches should draw: drawing is switched on and exactly one finger is down.
        /// A second finger is a pinch (zoom), so it ends the stroke.
        /// </summary>
        public static bool ShouldDraw(bool drawingOn, int fingersDown)
        {
            return drawingOn && fingersDown == 1;
        }

        /// <summary>The point on the line for a body hit: the hit moved a little off the surface along its normal, so the line is not buried in it.</summary>
        public static Vector3 Lift(Vector3 hitPoint, Vector3 hitNormal, float liftMeters)
        {
            if (!IsFinite(hitPoint))
                return hitPoint;
            Vector3 normal = IsFinite(hitNormal) && hitNormal.sqrMagnitude > 1e-8f ? hitNormal.normalized : Vector3.zero;
            return hitPoint + normal * Mathf.Max(0f, liftMeters);
        }

        /// <summary>True when the next point is far enough from the last one sent to be worth sending. The first point is always sent.</summary>
        public static bool IsFarEnough(Vector3? last, Vector3 next, float minSpacingMeters)
        {
            if (!IsFinite(next))
                return false;
            if (!last.HasValue)
                return true;
            return (next - last.Value).sqrMagnitude >= minSpacingMeters * minSpacingMeters;
        }

        private static bool IsFinite(Vector3 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z)
                || float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
        }
    }
}
