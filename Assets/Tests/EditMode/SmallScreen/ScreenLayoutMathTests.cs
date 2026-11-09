// Under test: the pure maths for safe-area anchors, the portrait or landscape canvas match and the pointer HUD inset.
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class ScreenLayoutMathTests
    {
        [Test]
        public void SafeAreaBecomesAnchors()
        {
            // 1080x2400 with a 90 px notch at the top and a 60 px gesture bar at the bottom.
            ScreenLayoutMath.AnchorsFor(new Rect(0f, 60f, 1080f, 2250f), new Vector2(1080f, 2400f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(0f, min.x, 1e-5f);
            Assert.AreEqual(60f / 2400f, min.y, 1e-5f);
            Assert.AreEqual(1f, max.x, 1e-5f);
            Assert.AreEqual(2310f / 2400f, max.y, 1e-5f);
        }

        [Test]
        public void ADegenerateScreenOrSafeAreaGivesTheFullScreen()
        {
            ScreenLayoutMath.AnchorsFor(new Rect(0f, 0f, 0f, 0f), new Vector2(1080f, 2400f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
            ScreenLayoutMath.AnchorsFor(new Rect(0f, 0f, 10f, 10f), Vector2.zero, out min, out max);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void PortraitMatchesWidthAndLandscapeMatchesHeight()
        {
            Assert.AreEqual(0f, ScreenLayoutMath.MatchFor(new Vector2(1080f, 2400f)));
            Assert.AreEqual(1f, ScreenLayoutMath.MatchFor(new Vector2(2400f, 1080f)));
            Assert.AreEqual(1f, ScreenLayoutMath.MatchFor(new Vector2(1280f, 720f)));
            Assert.AreEqual(1f, ScreenLayoutMath.MatchFor(new Vector2(1000f, 1000f)), "A square screen counts as landscape.");
        }

        [Test]
        public void CanvasScaleFollowsTheMatch()
        {
            var reference = new Vector2(1280f, 720f);
            Assert.AreEqual(1.5f, ScreenLayoutMath.CanvasScale(new Vector2(2400f, 1080f), reference, 1f), 1e-4f);
            Assert.AreEqual(1080f / 1280f, ScreenLayoutMath.CanvasScale(new Vector2(1080f, 2400f), reference, 0f), 1e-4f);
            Assert.AreEqual(1f, ScreenLayoutMath.CanvasScale(reference, reference, 0.5f), 1e-4f);
        }

        [Test]
        public void SafeAreaFlipsToGuiSpace()
        {
            // Safe area from the bottom 60 px up to 90 px below the top.
            Rect gui = ScreenLayoutMath.GuiSafeRect(new Rect(0f, 60f, 1080f, 2250f), 1080f, 2400f);
            Assert.AreEqual(new Rect(0f, 90f, 1080f, 2250f), gui);
        }

        [Test]
        public void ANoSafeAreaGivesTheWholeScreen()
        {
            Assert.AreEqual(new Rect(0f, 0f, 800f, 600f), ScreenLayoutMath.GuiSafeRect(Rect.zero, 800f, 600f));
        }

        [Test]
        public void InsetShrinksButNeverInvertsTheRect()
        {
            Assert.AreEqual(new Rect(14f, 14f, 72f, 22f), ScreenLayoutMath.Inset(new Rect(10f, 10f, 80f, 30f), 4f));
            Rect tiny = ScreenLayoutMath.Inset(new Rect(0f, 0f, 6f, 6f), 50f);
            Assert.GreaterOrEqual(tiny.width, 0f);
            Assert.GreaterOrEqual(tiny.height, 0f);
        }

        [Test]
        public void AHudRectPushedIntoTheNotchComesBackInsideTheSafeArea()
        {
            // Landscape 2400x1080 with a 120 px notch on the left and a 60 px bar at the bottom.
            Rect bounds = ScreenLayoutMath.Inset(ScreenLayoutMath.GuiSafeRect(new Rect(120f, 60f, 2280f, 1020f), 2400f, 1080f), 4f);
            Rect pushed = ScreenLayoutMath.ClampInside(new Rect(-30f, 1060f, 300f, 40f), bounds);
            Assert.GreaterOrEqual(pushed.xMin, bounds.xMin);
            Assert.GreaterOrEqual(pushed.yMin, bounds.yMin);
            Assert.LessOrEqual(pushed.xMax, bounds.xMax);
            Assert.LessOrEqual(pushed.yMax, bounds.yMax);
            Assert.AreEqual(300f, pushed.width, "Only the position moves.");
        }

        [Test]
        public void AnEdgeArrowAnchorIsClampedToTheInsetSafeArea()
        {
            Rect bounds = ScreenLayoutMath.Inset(ScreenLayoutMath.GuiSafeRect(new Rect(0f, 60f, 1080f, 2250f), 1080f, 2400f), 4f);
            Vector2 clamped = ScreenLayoutMath.ClampInside(new Vector2(1075f, 5f), ScreenLayoutMath.Inset(bounds, 50f));
            Assert.AreEqual(1080f - 4f - 50f, clamped.x, 1e-4f);
            Assert.AreEqual(90f + 4f + 50f, clamped.y, 1e-4f);
        }
    }
}
