using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>A gentle pulse: a smooth 0..1 wave over time. The caller passes unscaled time, so it keeps pulsing when time is paused.</summary>
    public static class PointerPulse
    {
        public static float Value(float seconds, float periodSeconds)
        {
            return 0.5f + 0.5f * Mathf.Sin(seconds * 2f * Mathf.PI / Mathf.Max(0.01f, periodSeconds));
        }
    }
}
