using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The pure rules behind how a sketch looks: the finger's points become a smooth curve, and a cleared sketch fades out.
    /// No scene, so it is easy to test.
    /// </summary>
    public static class SketchLook
    {
        /// <summary>Seconds a cleared or replaced sketch takes to fade away.</summary>
        public const float FadeSeconds = 0.35f;

        /// <summary>Curve points added between two finger points. Enough for a round circle, few enough to stay cheap.</summary>
        public const int PointsPerSegment = 4;

        /// <summary>
        /// Fills <paramref name="output"/> with a smooth curve through every point (a Catmull-Rom spline, so the curve passes
        /// through the finger's points and does not overshoot much). Fewer than three points are copied as they are.
        /// </summary>
        public static void Smooth(IReadOnlyList<Vector3> points, int perSegment, List<Vector3> output)
        {
            output.Clear();
            int count = points?.Count ?? 0;
            if (count < 3 || perSegment < 1)
            {
                for (int i = 0; i < count; i++)
                    output.Add(points[i]);
                return;
            }
            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = points[Mathf.Max(i - 1, 0)];
                Vector3 p1 = points[i];
                Vector3 p2 = points[i + 1];
                Vector3 p3 = points[Mathf.Min(i + 2, count - 1)];
                for (int step = 0; step < perSegment; step++)
                    output.Add(CatmullRom(p0, p1, p2, p3, step / (float)perSegment));
            }
            output.Add(points[count - 1]);
        }

        /// <summary>1 when the fade starts, 0 once it is over. Negative or broken times count as not started.</summary>
        public static float FadeAlpha(float secondsSinceStart, float duration = FadeSeconds)
        {
            if (!float.IsFinite(secondsSinceStart) || secondsSinceStart <= 0f)
                return 1f;
            if (duration <= 0f)
                return 0f;
            return Mathf.Clamp01(1f - secondsSinceStart / duration);
        }

        /// <summary>True when the shared line has caught up with what this phone drew, so its own preview can go.</summary>
        public static bool CaughtUp(int sharedCount, int previewCount) => sharedCount >= Mathf.Max(2, previewCount - 1);

        private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;
            return 0.5f * (2f * p1 + (p2 - p0) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (3f * p1 - p0 - 3f * p2 + p3) * t3);
        }
    }
}
