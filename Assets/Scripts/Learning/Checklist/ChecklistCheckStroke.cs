using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The three points of a check mark that draws itself. <c>fraction</c> 0 is nothing, 1 is the whole mark: the short
    /// stroke down comes first (first third of the time), then the long stroke up. Pure maths, tested without a scene.
    /// </summary>
    public static class ChecklistCheckStroke
    {
        private static readonly Vector2 Start = new Vector2(-0.46f, 0.04f);
        private static readonly Vector2 Middle = new Vector2(-0.115f, -0.346f);
        private static readonly Vector2 End = new Vector2(0.5f, 0.385f);

        /// <summary>Fills the three points for a mark that fits a square of side <paramref name="size"/> (metres), drawn to <paramref name="fraction"/>.</summary>
        public static void Points(float fraction, float size, out Vector3 first, out Vector3 second, out Vector3 third)
        {
            fraction = Mathf.Clamp01(fraction);
            Vector2 a = Start * size;
            Vector2 b = Middle * size;
            Vector2 c = End * size;
            Vector2 down = Vector2.Lerp(a, b, Mathf.Clamp01(fraction * 3f));
            Vector2 tip = fraction < 1f / 3f ? down : Vector2.Lerp(b, c, (fraction - 1f / 3f) * 1.5f);
            first = a;
            second = down;
            third = tip;
        }
    }
}
