using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Pure maths for fitting classmate UI to a phone or window: the safe area as anchors, the canvas scaler match for
    /// portrait or landscape, and the safe area as an immediate-mode GUI rectangle (origin top left).
    /// </summary>
    public static class ScreenLayoutMath
    {
        /// <summary>Landscape matches the canvas height, so a button keeps its height when the screen is wide.</summary>
        public const float LandscapeMatch = 1f;

        /// <summary>Portrait matches the canvas width, so a button never grows wider than the narrow screen.</summary>
        public const float PortraitMatch = 0f;

        /// <summary>The safe area as anchors (0..1 of the screen). A zero-size screen gives the full area.</summary>
        public static void AnchorsFor(Rect safeArea, Vector2 resolution, out Vector2 min, out Vector2 max)
        {
            if (resolution.x <= 0f || resolution.y <= 0f || safeArea.width <= 0f || safeArea.height <= 0f)
            {
                min = Vector2.zero;
                max = Vector2.one;
                return;
            }
            min = new Vector2(Mathf.Clamp01(safeArea.xMin / resolution.x), Mathf.Clamp01(safeArea.yMin / resolution.y));
            max = new Vector2(Mathf.Clamp01(safeArea.xMax / resolution.x), Mathf.Clamp01(safeArea.yMax / resolution.y));
        }

        public static bool IsPortrait(Vector2 resolution) => resolution.y > resolution.x;

        public static float MatchFor(Vector2 resolution) => IsPortrait(resolution) ? PortraitMatch : LandscapeMatch;

        /// <summary>The CanvasScaler scale (screen pixels per canvas unit) for ScaleWithScreenSize.</summary>
        public static float CanvasScale(Vector2 resolution, Vector2 referenceResolution, float match)
        {
            float logWidth = Mathf.Log(resolution.x / referenceResolution.x, 2f);
            float logHeight = Mathf.Log(resolution.y / referenceResolution.y, 2f);
            return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, match));
        }

        /// <summary>Screen.safeArea (origin bottom left) as a GUI rectangle (origin top left).</summary>
        public static Rect GuiSafeRect(Rect safeArea, float screenWidth, float screenHeight)
        {
            if (safeArea.width <= 0f || safeArea.height <= 0f)
                return new Rect(0f, 0f, screenWidth, screenHeight);
            return new Rect(safeArea.x, screenHeight - (safeArea.y + safeArea.height), safeArea.width, safeArea.height);
        }

        /// <summary>The GUI rectangle shrunk by <paramref name="padding"/> on every side, never smaller than a point at its centre.</summary>
        public static Rect Inset(Rect rect, float padding)
        {
            float x = Mathf.Min(padding, rect.width * 0.5f);
            float y = Mathf.Min(padding, rect.height * 0.5f);
            return new Rect(rect.x + x, rect.y + y, rect.width - 2f * x, rect.height - 2f * y);
        }

        /// <summary>Moves a rectangle inside <paramref name="bounds"/>; one that is larger than the bounds is lined up with the left and top.</summary>
        public static Rect ClampInside(Rect rect, Rect bounds)
        {
            rect.x = Mathf.Max(bounds.xMin, Mathf.Min(rect.x, bounds.xMax - rect.width));
            rect.y = Mathf.Max(bounds.yMin, Mathf.Min(rect.y, bounds.yMax - rect.height));
            return rect;
        }

        public static Vector2 ClampInside(Vector2 point, Rect bounds)
        {
            return new Vector2(Mathf.Clamp(point.x, bounds.xMin, bounds.xMax), Mathf.Clamp(point.y, bounds.yMin, bounds.yMax));
        }
    }
}
