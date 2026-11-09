// Pure math, no scene. A classmate draws on the body with one finger; a second finger is a pinch.
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class SketchRulesTests
    {
        [Test]
        public void OnlyOneFingerWhileDrawingIsOnDraws()
        {
            Assert.IsTrue(SketchRules.ShouldDraw(true, 1));
            Assert.IsFalse(SketchRules.ShouldDraw(true, 0));
            Assert.IsFalse(SketchRules.ShouldDraw(true, 2), "Two fingers are a pinch, not a stroke.");
            Assert.IsFalse(SketchRules.ShouldDraw(false, 1));
        }

        [Test]
        public void LiftMovesThePointAlongTheNormalByTheGivenAmount()
        {
            Vector3 lifted = SketchRules.Lift(new Vector3(1f, 2f, 3f), new Vector3(0f, 2f, 0f), 0.003f);
            Assert.AreEqual(new Vector3(1f, 2.003f, 3f).y, lifted.y, 1e-5f);
            Assert.AreEqual(1f, lifted.x, 1e-5f);
            Assert.AreEqual(3f, lifted.z, 1e-5f);
        }

        [Test]
        public void LiftWithAZeroOrBrokenNormalKeepsThePoint()
        {
            var point = new Vector3(1f, 2f, 3f);
            Assert.AreEqual(point, SketchRules.Lift(point, Vector3.zero, 0.003f));
            Assert.AreEqual(point, SketchRules.Lift(point, new Vector3(float.NaN, 0f, 0f), 0.003f));
        }

        [Test]
        public void TheFirstPointIsAlwaysFarEnough()
        {
            Assert.IsTrue(SketchRules.IsFarEnough(null, Vector3.zero, 0.006f));
        }

        [Test]
        public void ANearbyPointIsSkippedAndAFarOneIsKept()
        {
            Assert.IsFalse(SketchRules.IsFarEnough(Vector3.zero, new Vector3(0.002f, 0f, 0f), 0.006f));
            Assert.IsTrue(SketchRules.IsFarEnough(Vector3.zero, new Vector3(0.007f, 0f, 0f), 0.006f));
        }

        [Test]
        public void ABrokenPointIsNeverSent()
        {
            Assert.IsFalse(SketchRules.IsFarEnough(null, new Vector3(float.NaN, 0f, 0f), 0.006f));
            Assert.IsFalse(SketchRules.IsFarEnough(Vector3.zero, new Vector3(float.PositiveInfinity, 0f, 0f), 0.006f));
        }
    }
}
