// Authority: none. Pure local math for the phone's "magic window" look-around; no Unity objects.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Turns how far the phone has been tilted since the view started into a small, gentle turn of the camera.
    /// Everything is plain maths so it can be tested without a phone: the tilt since a reference attitude, a dead zone so a hand that
    /// is not quite still does nothing, a limit on how far the view may turn, and smoothing so the view glides.
    /// A vector holds (yaw, pitch) in degrees. Yaw turns the view right when positive, pitch tips it down when positive.
    /// </summary>
    public static class GyroLookRule
    {
        public const float DefaultMaxYaw = 20f;
        public const float DefaultMaxPitch = 12f;
        public const float DefaultDeadZone = 1.5f;

        /// <summary>
        /// The (yaw, pitch) the phone has turned since <paramref name="reference"/>, as seen from the phone itself.
        /// A missing or broken attitude (not a number, or all zero) counts as no tilt.
        /// </summary>
        public static Vector2 TiltSince(Quaternion reference, Quaternion current)
        {
            if (!IsUsable(reference) || !IsUsable(current))
                return Vector2.zero;
            Vector3 euler = (Quaternion.Inverse(reference) * current).eulerAngles;
            var tilt = new Vector2(Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.x));
            return IsFinite(tilt) ? tilt : Vector2.zero;
        }

        /// <summary>Ignores the first <paramref name="deadZone"/> degrees on each axis (the rest is kept, so there is no jump), then limits the turn.</summary>
        public static Vector2 Limit(Vector2 tilt, float maxYaw, float maxPitch, float deadZone)
        {
            if (!IsFinite(tilt))
                return Vector2.zero;
            return new Vector2(
                Mathf.Clamp(PastDeadZone(tilt.x, deadZone), -Mathf.Abs(maxYaw), Mathf.Abs(maxYaw)),
                Mathf.Clamp(PastDeadZone(tilt.y, deadZone), -Mathf.Abs(maxPitch), Mathf.Abs(maxPitch)));
        }

        /// <summary>Eases <paramref name="current"/> toward <paramref name="target"/>; the same result at any frame rate. A bad value falls back to the target.</summary>
        public static Vector2 Smooth(Vector2 current, Vector2 target, float speed, float deltaTime)
        {
            if (!IsFinite(target))
                target = Vector2.zero;
            if (!IsFinite(current))
                return target;
            return Vector2.Lerp(current, target, OrbitMath.DampFactor(speed, deltaTime));
        }

        /// <summary>The camera's rotation with the look-around added on top: yaw about the world's up, then pitch about the camera's own right.</summary>
        public static Quaternion Apply(Quaternion orbitRotation, Vector2 offset)
        {
            if (!IsFinite(offset))
                return orbitRotation;
            return Quaternion.Euler(0f, offset.x, 0f) * orbitRotation * Quaternion.Euler(offset.y, 0f, 0f);
        }

        private static float PastDeadZone(float degrees, float deadZone)
        {
            float magnitude = Mathf.Max(0f, Mathf.Abs(degrees) - Mathf.Max(0f, deadZone));
            return Mathf.Sign(degrees) * magnitude;
        }

        /// <summary>True for a real attitude: every part is a number and the quaternion is not all zeros (what a sensor reports before its first reading).</summary>
        public static bool IsUsable(Quaternion q)
        {
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w)
                || float.IsInfinity(q.x) || float.IsInfinity(q.y) || float.IsInfinity(q.z) || float.IsInfinity(q.w))
                return false;
            return q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 1e-6f;
        }

        private static bool IsFinite(Vector2 v)
        {
            return !(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.x) || float.IsInfinity(v.y));
        }
    }
}
