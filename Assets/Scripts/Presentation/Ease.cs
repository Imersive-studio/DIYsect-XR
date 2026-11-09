using UnityEngine;

namespace DIYsect.Presentation
{
    /// <summary>
    /// Easing curves. Each takes a time from 0 to 1 (clamped) and returns 0 at the start and 1 at the end.
    /// </summary>
    public static class Ease
    {
        /// <summary>Fast start, soft landing. The default for anything that appears.</summary>
        public static float OutCubic(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        /// <summary>Slow start, fast finish. For things that leave.</summary>
        public static float InCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * t;
        }

        /// <summary>Overshoots a little past 1 and settles back: the "tiny spring" at the end of a check mark.</summary>
        public static float OutBack(float t, float overshoot = 1.2f)
        {
            t = Mathf.Clamp01(t) - 1f;
            return 1f + t * t * ((overshoot + 1f) * t + overshoot);
        }
    }
}
