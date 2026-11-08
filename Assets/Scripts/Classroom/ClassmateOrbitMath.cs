using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The maths behind the classmate's orbit camera and tap detection. No input or scene access, so it is tested on
    /// its own. Yaw and pitch are in degrees; pitch looks down when positive.
    /// </summary>
    public static class ClassmateOrbitMath
    {
        /// <summary>Camera position and rotation for a camera orbiting <paramref name="pivot"/>.</summary>
        public static Pose PoseAround(Vector3 pivot, float yaw, float pitch, float distance)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            return new Pose(pivot + rotation * (Vector3.back * distance), rotation);
        }

        /// <summary>The yaw, pitch and distance that put a camera at <paramref name="cameraPosition"/> looking at the pivot.</summary>
        public static void FromPosition(Vector3 pivot, Vector3 cameraPosition, out float yaw, out float pitch, out float distance)
        {
            Vector3 toPivot = pivot - cameraPosition;
            distance = toPivot.magnitude;
            if (distance < 1e-5f)
            {
                yaw = 0f;
                pitch = 0f;
                distance = 0f;
                return;
            }
            Vector3 forward = toPivot / distance;
            yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            pitch = Mathf.Asin(Mathf.Clamp(-forward.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        /// <summary>
        /// Turns a drag in pixels into new yaw and pitch, so the body follows the finger: dragging right swings the camera
        /// left around it, and dragging down raises the camera. The pixel y axis points up, as the Input System reports it.
        /// </summary>
        public static Vector2 ApplyDrag(float yaw, float pitch, Vector2 dragPixels, float degreesPerPixel, float minPitch, float maxPitch)
        {
            float newYaw = yaw + dragPixels.x * degreesPerPixel;
            float newPitch = Mathf.Clamp(pitch - dragPixels.y * degreesPerPixel, minPitch, maxPitch);
            return new Vector2(newYaw, newPitch);
        }

        /// <summary>Moves closer when the fingers spread and further when they pinch.</summary>
        public static float ApplyPinch(float distance, float previousFingerGap, float currentFingerGap, float minDistance, float maxDistance)
        {
            if (previousFingerGap < 1f || currentFingerGap < 1f)
                return distance;
            return Mathf.Clamp(distance * (previousFingerGap / currentFingerGap), minDistance, maxDistance);
        }

        /// <summary>Moves by a scroll wheel step. Positive scroll moves closer.</summary>
        public static float ApplyScroll(float distance, float scroll, float stepFraction, float minDistance, float maxDistance)
        {
            return Mathf.Clamp(distance * (1f - Mathf.Sign(scroll) * stepFraction * Mathf.Min(1f, Mathf.Abs(scroll))), minDistance, maxDistance);
        }

        /// <summary>True when a press ended close enough to where it started to count as a tap, not a drag.</summary>
        public static bool IsTap(Vector2 down, Vector2 up, float thresholdPixels)
        {
            return (up - down).sqrMagnitude <= thresholdPixels * thresholdPixels;
        }
    }
}
