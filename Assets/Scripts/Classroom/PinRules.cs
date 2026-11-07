using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>Pure checks the server applies to a pin request. No networking, so they are easy to test.</summary>
    public static class PinRules
    {
        /// <summary>Longest accepted pin text in characters. Fits a FixedString64Bytes even in UTF-8.</summary>
        public const int MaxTextLength = 20;

        /// <summary>Furthest a viewpoint may be from the body root, in the body's local units (metres).</summary>
        public const float MaxViewDistance = 10f;

        /// <summary>Smallest quaternion length that still counts as a rotation. All zeros means "no view".</summary>
        private const float MinRotationLength = 1e-4f;

        /// <summary>Trims the text. Returns false when it is empty or longer than <see cref="MaxTextLength"/>.</summary>
        public static bool TryCleanText(string text, out string cleaned)
        {
            cleaned = text == null ? string.Empty : text.Trim();
            return cleaned.Length > 0 && cleaned.Length <= MaxTextLength;
        }

        /// <summary>True when every component is finite and the quaternion is not (near) zero.</summary>
        public static bool IsUsableRotation(Quaternion rotation)
        {
            if (!float.IsFinite(rotation.x) || !float.IsFinite(rotation.y) || !float.IsFinite(rotation.z) || !float.IsFinite(rotation.w))
                return false;
            return LengthOf(rotation) > MinRotationLength;
        }

        /// <summary>
        /// Checks a viewpoint given in the body's local space. The position must be finite and within
        /// <see cref="MaxViewDistance"/> of the body root; the rotation must be usable and is normalized.
        /// Returns false when either is invalid.
        /// </summary>
        public static bool TryCleanView(Vector3 localPosition, Quaternion localRotation, out Quaternion cleanedRotation)
        {
            cleanedRotation = default;
            if (!float.IsFinite(localPosition.x) || !float.IsFinite(localPosition.y) || !float.IsFinite(localPosition.z))
                return false;
            if (localPosition.sqrMagnitude > MaxViewDistance * MaxViewDistance)
                return false;
            if (!IsUsableRotation(localRotation))
                return false;
            float length = LengthOf(localRotation);
            cleanedRotation = new Quaternion(localRotation.x / length, localRotation.y / length, localRotation.z / length, localRotation.w / length);
            return true;
        }

        private static float LengthOf(Quaternion q)
        {
            return Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
        }

        /// <summary>True when the point is finite and inside the organ's local bounds grown by the margin.</summary>
        public static bool IsPointOnOrgan(Vector3 localPoint, Bounds organLocalBounds, float margin)
        {
            if (!float.IsFinite(localPoint.x) || !float.IsFinite(localPoint.y) || !float.IsFinite(localPoint.z))
                return false;
            organLocalBounds.Expand(2f * margin);
            return organLocalBounds.Contains(localPoint);
        }
    }
}
