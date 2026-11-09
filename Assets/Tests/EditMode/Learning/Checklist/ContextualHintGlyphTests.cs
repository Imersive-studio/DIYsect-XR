using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class ContextualHintGlyphTests
    {
        [Test]
        public void ArcAndArrowheadHaveTheExpectedPointCount()
        {
            Assert.That(ContextualHintGlyph.Points(0.01f, 12).Length, Is.EqualTo(ContextualHintGlyph.PointCount(12)));
        }

        [Test]
        public void ArcPointsSitOnTheCircleAndTheHeadStaysClose()
        {
            Vector3[] points = ContextualHintGlyph.Points(0.01f, 12);
            for (int i = 0; i <= 12; i++)
                Assert.That(points[i].magnitude, Is.EqualTo(0.01f).Within(1e-6f));
            foreach (Vector3 point in points)
                Assert.That(point.magnitude, Is.LessThan(0.016f));
        }

        [Test]
        public void TooFewPiecesFailLoudly()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ContextualHintGlyph.Points(0.01f, 2));
        }
    }
}
