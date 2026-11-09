using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Splits one reveal clock into per-line progress so lines of text appear one after another.
    /// Pure maths, tested without a scene.
    /// </summary>
    public static class StaggeredReveal
    {
        /// <summary>
        /// Progress (0 to 1) of a line that starts <paramref name="delaySeconds"/> after the first one.
        /// <paramref name="amount"/> is the reveal clock's 0 to 1 amount and <paramref name="totalSeconds"/> the time it takes
        /// to run in. Lines leave together, so the delay only applies while <paramref name="entering"/>.
        /// </summary>
        public static float Progress(float amount, float totalSeconds, float delaySeconds, bool entering)
        {
            if (!entering || delaySeconds <= 0f || totalSeconds <= 0f)
                return Mathf.Clamp01(amount);
            float delay = Mathf.Min(delaySeconds, totalSeconds * 0.9f);
            return Mathf.Clamp01((amount * totalSeconds - delay) / (totalSeconds - delay));
        }
    }
}
