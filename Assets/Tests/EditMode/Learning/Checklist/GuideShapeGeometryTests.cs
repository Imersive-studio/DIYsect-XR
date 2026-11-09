using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class GuideShapeGeometryTests
    {
        private static void AssertFinite(Vector3[] vertices)
        {
            foreach (Vector3 v in vertices)
            {
                Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y) || float.IsNaN(v.z));
                Assert.IsFalse(float.IsInfinity(v.x) || float.IsInfinity(v.y) || float.IsInfinity(v.z));
                Assert.AreEqual(0f, v.z);
            }
        }

        private static void AssertIndicesValid(Vector3[] vertices, int[] triangles)
        {
            Assert.AreEqual(0, triangles.Length % 3);
            foreach (int index in triangles)
                Assert.That(index, Is.InRange(0, vertices.Length - 1));
        }

        // Every non-empty triangle must face -Z (the viewer), which is a negative signed area here.
        private static void AssertFacesViewer(Vector3[] vertices, int[] triangles)
        {
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                Assert.LessOrEqual(area, 1e-9f);
            }
        }

        [Test]
        public void RoundedRectStaysInsideItsSizeAndFacesTheViewer()
        {
            GuideShapeGeometry.RoundedRect(new Vector2(0.72f, 0.25f), 0.03f, 6, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            AssertFacesViewer(vertices, triangles);
            foreach (Vector3 v in vertices)
            {
                Assert.LessOrEqual(Mathf.Abs(v.x), 0.36f + 1e-5f);
                Assert.LessOrEqual(Mathf.Abs(v.y), 0.125f + 1e-5f);
            }
        }

        [Test]
        public void RoundedRectReachesTheEdgesOfItsBounds()
        {
            GuideShapeGeometry.RoundedRect(new Vector2(0.4f, 0.2f), 0.05f, 4, out Vector3[] vertices, out _);
            float maxX = 0f, maxY = 0f;
            foreach (Vector3 v in vertices)
            {
                maxX = Mathf.Max(maxX, v.x);
                maxY = Mathf.Max(maxY, v.y);
            }
            Assert.AreEqual(0.2f, maxX, 1e-5f);
            Assert.AreEqual(0.1f, maxY, 1e-5f);
        }

        [Test]
        public void RoundedRectClampsALargeRadiusAndTooFewSegments()
        {
            GuideShapeGeometry.RoundedRect(new Vector2(0.1f, 0.02f), 5f, 0, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            foreach (Vector3 v in vertices)
            {
                Assert.LessOrEqual(Mathf.Abs(v.x), 0.05f + 1e-5f);
                Assert.LessOrEqual(Mathf.Abs(v.y), 0.01f + 1e-5f);
            }
            Assert.Greater(triangles.Length, 0);
        }

        [Test]
        public void RoundedRectWithNoRadiusIsStillValid()
        {
            GuideShapeGeometry.RoundedRect(new Vector2(0.2f, 0.1f), 0f, 3, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            AssertFacesViewer(vertices, triangles);
        }

        [Test]
        public void DiscHasTheRequestedDiameterAndFacesTheViewer()
        {
            GuideShapeGeometry.Disc(0.085f, 24, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            AssertFacesViewer(vertices, triangles);
            Assert.AreEqual(25, vertices.Length);
            for (int i = 1; i < vertices.Length; i++)
                Assert.AreEqual(0.0425f, vertices[i].magnitude, 1e-5f);
        }

        [Test]
        public void DiscClampsToAtLeastThreeSegments()
        {
            GuideShapeGeometry.Disc(0.1f, 0, out Vector3[] vertices, out int[] triangles);
            Assert.AreEqual(4, vertices.Length);
            Assert.AreEqual(9, triangles.Length);
        }

        [Test]
        public void RingKeepsTheBandBetweenInnerAndOuterRadius()
        {
            GuideShapeGeometry.Ring(0.026f, 0.004f, 24, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            AssertFacesViewer(vertices, triangles);
            for (int i = 0; i < 24; i++)
            {
                Assert.AreEqual(0.013f, vertices[i].magnitude, 1e-5f);
                Assert.AreEqual(0.009f, vertices[24 + i].magnitude, 1e-5f);
            }
        }

        [Test]
        public void RingThicknessIsClampedToTheRadius()
        {
            GuideShapeGeometry.Ring(0.02f, 1f, 12, out Vector3[] vertices, out int[] triangles);
            AssertFinite(vertices);
            AssertIndicesValid(vertices, triangles);
            for (int i = 12; i < 24; i++)
                Assert.AreEqual(0f, vertices[i].magnitude, 1e-6f);
        }
    }
}
