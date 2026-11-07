using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Plain rules for one guide stroke, kept apart from the network code so they can be tested alone.
    /// A point is accepted when it is finite, within <see cref="MaxRadius"/> of the body root, far enough from the
    /// previous point to add detail, and the stroke still has room.
    /// </summary>
    public sealed class GuideStrokeRules
    {
        public float MaxRadius { get; }
        public float MinSpacing { get; }
        public int MaxPoints { get; }

        public GuideStrokeRules(float maxRadius, float minSpacing, int maxPoints)
        {
            MaxRadius = maxRadius;
            MinSpacing = minSpacing;
            MaxPoints = maxPoints;
        }

        public enum Verdict { Accepted, NotFinite, OutOfReach, TooClose, Full }

        /// <summary>Judges a body-local point against the stroke so far (its last point and point count).</summary>
        public Verdict Judge(Vector3 point, Vector3? last, int count)
        {
            if (!float.IsFinite(point.x) || !float.IsFinite(point.y) || !float.IsFinite(point.z))
                return Verdict.NotFinite;
            if (point.magnitude > MaxRadius)
                return Verdict.OutOfReach;
            if (count >= MaxPoints)
                return Verdict.Full;
            if (last.HasValue && Vector3.Distance(point, last.Value) < MinSpacing)
                return Verdict.TooClose;
            return Verdict.Accepted;
        }
    }
}
