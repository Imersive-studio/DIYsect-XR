using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Small anti-aliased white shapes drawn once in code, so the pointer markers need no art files. The professor gets a
    /// ring and a solid triangle arrow; the student gets a diamond and a chevron arrow, so the two differ in shape as well as colour.
    /// Arrows point to the right (+x) and are rotated when drawn.
    /// </summary>
    public static class PointerShapes
    {
        private const int Size = 64;

        public static Texture2D Ring() => Build(p => Mathf.Abs(p.magnitude - 0.6f) - 0.13f);

        public static Texture2D Diamond() => Build(p => Mathf.Abs(Mathf.Abs(p.x) + Mathf.Abs(p.y) - 0.75f) - 0.13f);

        public static Texture2D Triangle()
        {
            Vector2[] corners = { new Vector2(0.9f, 0f), new Vector2(-0.7f, 0.75f), new Vector2(-0.7f, -0.75f) };
            return Build(p => ConvexDistance(p, corners));
        }

        public static Texture2D Chevron()
        {
            return Build(p => Mathf.Min(SegmentDistance(p, new Vector2(-0.5f, 0.75f), new Vector2(0.65f, 0f)),
                SegmentDistance(p, new Vector2(-0.5f, -0.75f), new Vector2(0.65f, 0f))) - 0.16f);
        }

        // Signed distance in the -1..1 square: negative inside the shape.
        private static Texture2D Build(System.Func<Vector2, float> distance)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size * 2f - 1f, (y + 0.5f) / Size * 2f - 1f);
                    float alpha = Mathf.Clamp01(0.5f - distance(p) * (Size * 0.5f));
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static float ConvexDistance(Vector2 p, Vector2[] corners)
        {
            float worst = float.MinValue;
            for (int i = 0; i < corners.Length; i++)
            {
                Vector2 a = corners[i];
                Vector2 b = corners[(i + 1) % corners.Length];
                Vector2 edge = (b - a).normalized;
                var outward = new Vector2(edge.y, -edge.x);
                worst = Mathf.Max(worst, Vector2.Dot(p - a, outward));
            }
            return worst;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }
    }
}
