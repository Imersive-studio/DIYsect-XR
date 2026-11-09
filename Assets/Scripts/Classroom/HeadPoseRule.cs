using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The server's rules for a published head pose. Pure logic with no network state, so it is easy to test.
    /// The host is the professor; any other client that publishes a head is the student (classmates have no head and never publish).
    /// </summary>
    public static class HeadPoseRule
    {
        public static PointerRole RoleFor(ulong sender, ulong serverClientId)
        {
            return sender == serverClientId ? PointerRole.Professor : PointerRole.Student;
        }

        /// <summary>True when every number is finite, the rotation is not degenerate and the head is within <paramref name="maxRadius"/> metres of the body.</summary>
        public static bool IsAcceptable(Vector3 localPosition, Quaternion localRotation, float maxRadius)
        {
            if (!IsFinite(localPosition.x) || !IsFinite(localPosition.y) || !IsFinite(localPosition.z))
                return false;
            if (!IsFinite(localRotation.x) || !IsFinite(localRotation.y) || !IsFinite(localRotation.z) || !IsFinite(localRotation.w))
                return false;
            float squaredLength = localRotation.x * localRotation.x + localRotation.y * localRotation.y + localRotation.z * localRotation.z + localRotation.w * localRotation.w;
            if (squaredLength < 1e-6f)
                return false;
            return localPosition.magnitude <= maxRadius;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
