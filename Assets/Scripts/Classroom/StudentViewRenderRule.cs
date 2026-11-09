using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// How often the live student view may render. Pure logic, so it is easy to test. The second view is costly on Quest,
    /// so it is capped at <see cref="MaxRendersPerSecond"/> and never runs every frame.
    /// </summary>
    public static class StudentViewRenderRule
    {
        public const float MaxRendersPerSecond = 15f;
        public const float MinRendersPerSecond = 1f;

        // A render a little early is fine, so timer jitter never skips a whole turn.
        private const float EarlyFraction = 0.9f;

        /// <summary>Seconds between two renders for that rate. The rate is kept between the minimum and the maximum.</summary>
        public static float IntervalSeconds(float rendersPerSecond)
        {
            return 1f / Mathf.Clamp(rendersPerSecond, MinRendersPerSecond, MaxRendersPerSecond);
        }

        /// <summary>True when enough time has passed since the last render. A negative <paramref name="lastRenderAt"/> means never rendered.</summary>
        public static bool ShouldRender(float now, float lastRenderAt, float rendersPerSecond)
        {
            if (lastRenderAt < 0f)
                return true;
            return now - lastRenderAt >= IntervalSeconds(rendersPerSecond) * EarlyFraction;
        }
    }
}
