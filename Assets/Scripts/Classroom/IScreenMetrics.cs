using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>What the screen layout components read. The game reads Screen; a test supplies its own numbers.</summary>
    public interface IScreenMetrics
    {
        Vector2Int Resolution { get; }
        Rect SafeArea { get; }
        ScreenOrientation Orientation { get; }
    }

    public sealed class UnityScreenMetrics : IScreenMetrics
    {
        public Vector2Int Resolution => new Vector2Int(Screen.width, Screen.height);
        public Rect SafeArea => Screen.safeArea;
        public ScreenOrientation Orientation => Screen.orientation;
    }
}
