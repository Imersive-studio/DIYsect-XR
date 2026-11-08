using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Screen placement of a pointer, as pure maths on a viewport point (what Camera.WorldToViewportPoint returns:
    /// x and y in 0..1 over the screen, z the distance in front of the camera, negative when behind it).
    /// </summary>
    public static class PointerScreenMath
    {
        public static PointerPlacement Place(Vector3 viewport, float margin)
        {
            bool inFront = viewport.z > 0f;
            bool inside = viewport.x >= 0f && viewport.x <= 1f && viewport.y >= 0f && viewport.y <= 1f;
            if (inFront && inside)
                return new PointerPlacement(true, new Vector2(viewport.x, viewport.y), 0f);

            var direction = new Vector2(viewport.x - 0.5f, viewport.y - 0.5f);
            // Behind the camera the projection is mirrored through the centre, so undo that.
            if (!inFront)
                direction = -direction;
            // Straight ahead or straight behind has no side: point down, as "turn around".
            if (direction.sqrMagnitude < 1e-8f)
                direction = Vector2.down;

            float half = 0.5f - margin;
            float scale = half / Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.y));
            Vector2 edge = new Vector2(0.5f, 0.5f) + direction * scale;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return new PointerPlacement(false, edge, angle);
        }

        /// <summary>
        /// Moves <paramref name="moving"/> away from <paramref name="anchored"/> until they are at least
        /// <paramref name="minDistance"/> apart, staying inside the margin. Keeps two edge arrows from overlapping.
        /// </summary>
        public static Vector2 Separate(Vector2 anchored, Vector2 moving, float minDistance, float margin)
        {
            if (Vector2.Distance(anchored, moving) >= minDistance)
                return moving;
            Vector2 away = moving - anchored;
            away = away.sqrMagnitude < 1e-8f ? Vector2.right : away.normalized;
            Vector2 first = Clamp(anchored + away * minDistance, margin);
            if (Vector2.Distance(anchored, first) >= minDistance * 0.99f)
                return first;
            Vector2 second = Clamp(anchored - away * minDistance, margin);
            return Vector2.Distance(anchored, second) > Vector2.Distance(anchored, first) ? second : first;
        }

        private static Vector2 Clamp(Vector2 point, float margin)
        {
            return new Vector2(Mathf.Clamp(point.x, margin, 1f - margin), Mathf.Clamp(point.y, margin, 1f - margin));
        }
    }
}
