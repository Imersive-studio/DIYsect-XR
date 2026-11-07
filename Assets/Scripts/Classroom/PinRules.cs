using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>Pure checks the server applies to a pin request. No networking, so they are easy to test.</summary>
    public static class PinRules
    {
        /// <summary>Longest accepted pin text in characters. Fits a FixedString64Bytes even in UTF-8.</summary>
        public const int MaxTextLength = 20;

        /// <summary>Trims the text. Returns false when it is empty or longer than <see cref="MaxTextLength"/>.</summary>
        public static bool TryCleanText(string text, out string cleaned)
        {
            cleaned = text == null ? string.Empty : text.Trim();
            return cleaned.Length > 0 && cleaned.Length <= MaxTextLength;
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
