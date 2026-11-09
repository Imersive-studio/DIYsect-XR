using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The line drawings of the guide icons, as strokes of points in a square from -1 to 1 (up is +y). The panel scales
    /// them to the icon tile. Plain data, so the drawings are checked without a scene.
    /// </summary>
    public static class ChecklistIconShapes
    {
        /// <summary>The most strokes any icon uses, so the panel knows how many line renderers to keep.</summary>
        public const int MaxStrokes = 6;

        private static readonly Vector2[][] HandStrokes =
        {
            new[] { new Vector2(-0.45f, -0.05f), new Vector2(-0.45f, -0.6f), new Vector2(-0.2f, -0.9f), new Vector2(0.3f, -0.9f), new Vector2(0.45f, -0.6f), new Vector2(0.45f, -0.05f) },
            new[] { new Vector2(-0.45f, -0.05f), new Vector2(-0.45f, 0.6f) },
            new[] { new Vector2(-0.15f, -0.05f), new Vector2(-0.15f, 0.85f) },
            new[] { new Vector2(0.15f, -0.05f), new Vector2(0.15f, 0.8f) },
            new[] { new Vector2(0.45f, -0.05f), new Vector2(0.45f, 0.55f) },
            new[] { new Vector2(-0.45f, -0.45f), new Vector2(-0.8f, -0.1f) },
        };

        private static readonly Vector2[][] ScalpelStrokes =
        {
            new[] { new Vector2(0.8f, -0.8f), new Vector2(0.15f, -0.15f) },
            new[] { new Vector2(0.15f, -0.15f), new Vector2(-0.2f, 0.35f), new Vector2(-0.8f, 0.8f), new Vector2(-0.45f, 0.1f), new Vector2(0.15f, -0.15f) },
        };

        private static readonly Vector2[][] ForcepsStrokes =
        {
            new[] { new Vector2(0f, -0.9f), new Vector2(-0.3f, -0.2f), new Vector2(-0.12f, 0.9f) },
            new[] { new Vector2(0f, -0.9f), new Vector2(0.3f, -0.2f), new Vector2(0.12f, 0.9f) },
        };

        private static readonly Vector2[][] CheckStrokes =
        {
            new[] { new Vector2(-0.55f, 0f), new Vector2(-0.15f, -0.45f), new Vector2(0.6f, 0.5f) },
        };

        /// <summary>The strokes of <paramref name="icon"/>. Each has at least two points. Do not change the arrays.</summary>
        public static Vector2[][] Strokes(ChecklistIcon icon)
        {
            switch (icon)
            {
                case ChecklistIcon.Scalpel: return ScalpelStrokes;
                case ChecklistIcon.Forceps: return ForcepsStrokes;
                case ChecklistIcon.Check: return CheckStrokes;
                default: return HandStrokes;
            }
        }
    }
}
