using System;
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The small "turn" symbol of the contextual hint: most of a circle with an arrowhead at its end, as points in the
    /// plane of the hint (z is 0). Pure maths, tested without a scene.
    /// </summary>
    public static class ContextualHintGlyph
    {
        private const float StartDegrees = 40f;
        private const float EndDegrees = 320f;
        private const float HeadLength = 0.55f;

        /// <summary>Number of points for an arc of <paramref name="arcSegments"/> pieces plus the arrowhead.</summary>
        public static int PointCount(int arcSegments) => arcSegments + 1 + 3;

        /// <summary>The arc, then the arrowhead (back to one barb, tip again, other barb). <paramref name="radius"/> is in metres.</summary>
        public static Vector3[] Points(float radius, int arcSegments)
        {
            if (arcSegments < 3) throw new ArgumentOutOfRangeException(nameof(arcSegments), "A glyph arc needs at least three pieces.");
            var points = new Vector3[PointCount(arcSegments)];
            for (int i = 0; i <= arcSegments; i++)
            {
                float angle = Mathf.Lerp(StartDegrees, EndDegrees, i / (float)arcSegments) * Mathf.Deg2Rad;
                points[i] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
            }
            Vector3 tip = points[arcSegments];
            float endAngle = EndDegrees * Mathf.Deg2Rad;
            // Direction of travel at the end of a counter-clockwise arc.
            Vector3 along = new Vector3(-Mathf.Sin(endAngle), Mathf.Cos(endAngle), 0f);
            Vector3 outward = new Vector3(Mathf.Cos(endAngle), Mathf.Sin(endAngle), 0f);
            float head = radius * HeadLength;
            points[arcSegments + 1] = tip - along * head + outward * head * 0.6f;
            points[arcSegments + 2] = tip;
            points[arcSegments + 3] = tip - along * head - outward * head * 0.6f;
            return points;
        }
    }
}
