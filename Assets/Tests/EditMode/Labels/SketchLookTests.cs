using System.Collections.Generic;
using DIYsect.Interaction;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Interaction.Tests
{
    public class SketchLookTests
    {
        private readonly List<Vector3> _output = new List<Vector3>();

        [Test]
        public void TheSmoothCurvePassesThroughEveryFingerPoint()
        {
            var points = new List<Vector3> { Vector3.zero, Vector3.right, new Vector3(1f, 1f, 0f), Vector3.up };
            SketchLook.Smooth(points, 4, _output);

            Assert.AreEqual((points.Count - 1) * 4 + 1, _output.Count);
            for (int i = 0; i < points.Count; i++)
                Assert.AreEqual(0f, Vector3.Distance(points[i], _output[i * 4]), 0.0001f, $"The curve must pass through point {i}.");
        }

        [Test]
        public void TwoPointsOrFewerAreKeptAsTheyAre()
        {
            SketchLook.Smooth(new List<Vector3> { Vector3.zero, Vector3.one }, 4, _output);
            Assert.AreEqual(2, _output.Count);
            SketchLook.Smooth(null, 4, _output);
            Assert.AreEqual(0, _output.Count);
        }

        [Test]
        public void AClearedSketchFadesOutOverAShortTime()
        {
            Assert.AreEqual(1f, SketchLook.FadeAlpha(0f));
            Assert.AreEqual(0.5f, SketchLook.FadeAlpha(SketchLook.FadeSeconds * 0.5f), 0.0001f);
            Assert.AreEqual(0f, SketchLook.FadeAlpha(SketchLook.FadeSeconds * 2f));
            Assert.AreEqual(1f, SketchLook.FadeAlpha(float.NaN), "A broken clock counts as not started.");
        }

        [Test]
        public void ThePhonesOwnInkGivesWayOnceTheSharedLineCatchesUp()
        {
            Assert.IsFalse(SketchLook.CaughtUp(3, 10));
            Assert.IsTrue(SketchLook.CaughtUp(9, 10));
            Assert.IsFalse(SketchLook.CaughtUp(1, 1), "A single point is not a line yet.");
        }
    }
}
