using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The corner points and triangles of the flat panel shapes. Pure maths with no Mesh, so it is tested
    /// without a scene. Shapes lie in the XY plane at z = 0 and face -Z, the side the viewer is on.
    /// </summary>
    public static class GuideShapeGeometry
    {
        private const int MinDiscSegments = 3;

        /// <summary>A rectangle with round corners, as a fan from the centre.</summary>
        public static void RoundedRect(Vector2 size, float radius, int cornerSegments, out Vector3[] vertices, out int[] triangles)
        {
            float halfWidth = Mathf.Max(0f, size.x) * 0.5f;
            float halfHeight = Mathf.Max(0f, size.y) * 0.5f;
            float corner = Mathf.Clamp(radius, 0f, Mathf.Min(halfWidth, halfHeight));
            int steps = Mathf.Max(1, cornerSegments);
            int edgePoints = 4 * (steps + 1);

            vertices = new Vector3[edgePoints + 1];
            vertices[0] = Vector3.zero;
            float insideX = halfWidth - corner;
            float insideY = halfHeight - corner;
            // Walk counter-clockwise: top right, top left, bottom left, bottom right.
            for (int c = 0; c < 4; c++)
            {
                float centreX = (c == 0 || c == 3) ? insideX : -insideX;
                float centreY = (c < 2) ? insideY : -insideY;
                for (int s = 0; s <= steps; s++)
                {
                    float angle = (c * 90f + 90f * s / steps) * Mathf.Deg2Rad;
                    vertices[1 + c * (steps + 1) + s] =
                        new Vector3(centreX + corner * Mathf.Cos(angle), centreY + corner * Mathf.Sin(angle), 0f);
                }
            }
            FanTriangles(edgePoints, out triangles);
        }

        /// <summary>A filled circle.</summary>
        public static void Disc(float diameter, int segments, out Vector3[] vertices, out int[] triangles)
        {
            int count = Mathf.Max(MinDiscSegments, segments);
            float radius = Mathf.Max(0f, diameter) * 0.5f;
            vertices = new Vector3[count + 1];
            vertices[0] = Vector3.zero;
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                vertices[1 + i] = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), 0f);
            }
            FanTriangles(count, out triangles);
        }

        /// <summary>A circle outline of the given outer size and band thickness.</summary>
        public static void Ring(float outerDiameter, float thickness, int segments, out Vector3[] vertices, out int[] triangles)
        {
            int count = Mathf.Max(MinDiscSegments, segments);
            float outer = Mathf.Max(0f, outerDiameter) * 0.5f;
            float inner = outer - Mathf.Clamp(thickness, 0f, outer);
            // Outer points are 0 to count - 1, inner points count to 2 * count - 1.
            vertices = new Vector3[count * 2];
            for (int i = 0; i < count; i++)
            {
                float angle = 2f * Mathf.PI * i / count;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);
                vertices[i] = new Vector3(outer * cos, outer * sin, 0f);
                vertices[count + i] = new Vector3(inner * cos, inner * sin, 0f);
            }
            triangles = new int[count * 6];
            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int at = i * 6;
                triangles[at] = i;
                triangles[at + 1] = count + i;
                triangles[at + 2] = next;
                triangles[at + 3] = next;
                triangles[at + 4] = count + i;
                triangles[at + 5] = count + next;
            }
        }

        // Vertex 0 is the centre; 1..edgePoints go counter-clockwise. The winding is reversed so the face looks at -Z.
        private static void FanTriangles(int edgePoints, out int[] triangles)
        {
            triangles = new int[edgePoints * 3];
            for (int i = 0; i < edgePoints; i++)
            {
                int current = 1 + i;
                int next = 1 + (i + 1) % edgePoints;
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = next;
                triangles[i * 3 + 2] = current;
            }
        }
    }
}
