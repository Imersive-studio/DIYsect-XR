using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>Where a pointer marker goes on a classmate's screen. Positions are viewport coordinates (0..1, y up).</summary>
    public readonly struct PointerPlacement
    {
        public PointerPlacement(bool onScreen, Vector2 position, float angleDegrees)
        {
            OnScreen = onScreen;
            Position = position;
            AngleDegrees = angleDegrees;
        }

        /// <summary>True when the organ is in front of the camera and inside the screen: a ring goes on it. False: an edge arrow.</summary>
        public bool OnScreen { get; }

        /// <summary>The organ's screen position, or the point on the screen edge the arrow sits at.</summary>
        public Vector2 Position { get; }

        /// <summary>Direction of the arrow, counter-clockwise from screen right. Only meaningful off screen.</summary>
        public float AngleDegrees { get; }
    }
}
