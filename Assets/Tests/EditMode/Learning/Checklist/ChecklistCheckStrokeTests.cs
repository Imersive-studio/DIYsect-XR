using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class ChecklistCheckStrokeTests
    {
        [Test]
        public void NothingIsDrawnAtZero()
        {
            ChecklistCheckStroke.Points(0f, 0.02f, out Vector3 a, out Vector3 b, out Vector3 c);
            Assert.That(b, Is.EqualTo(a));
            Assert.That(c, Is.EqualTo(a));
        }

        [Test]
        public void TheShortStrokeFinishesAtOneThird()
        {
            ChecklistCheckStroke.Points(1f / 3f, 0.02f, out Vector3 a, out Vector3 b, out Vector3 c);
            Assert.That((b - a).magnitude, Is.GreaterThan(0f));
            Assert.That(c, Is.EqualTo(b));
        }

        [Test]
        public void TheWholeMarkIsDrawnAtOneAndFitsItsSquare()
        {
            ChecklistCheckStroke.Points(1f, 0.02f, out Vector3 a, out Vector3 b, out Vector3 c);
            Assert.That(c.x, Is.GreaterThan(b.x));
            foreach (Vector3 point in new[] { a, b, c })
                Assert.That(Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)), Is.LessThanOrEqualTo(0.01f + 1e-6f));
        }

        [Test]
        public void TheMarkGrowsWithTime()
        {
            float last = -1f;
            for (int i = 0; i <= 10; i++)
            {
                ChecklistCheckStroke.Points(i / 10f, 0.02f, out Vector3 a, out Vector3 b, out Vector3 c);
                float length = (b - a).magnitude + (c - b).magnitude;
                Assert.That(length, Is.GreaterThanOrEqualTo(last - 1e-6f));
                last = length;
            }
        }
    }
}
