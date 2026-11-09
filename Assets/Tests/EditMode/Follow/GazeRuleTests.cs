// Pure rules of the shared gaze dots; no scene, no network.
using DIYsect.Networking;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class GazeRuleTests
    {
        [Test]
        public void TheFirstCastAlwaysRuns()
        {
            Assert.IsTrue(GazeRule.ShouldCast(0f, float.NegativeInfinity));
        }

        [Test]
        public void CastsAreLimitedToAboutFifteenAPerSecond()
        {
            Assert.IsFalse(GazeRule.ShouldCast(10.03f, 10f));
            Assert.IsFalse(GazeRule.ShouldCast(10.06f, 10f));
            Assert.IsTrue(GazeRule.ShouldCast(10.07f, 10f));
            int casts = 0;
            float last = float.NegativeInfinity;
            for (int frame = 0; frame < 120; frame++)
            {
                float now = frame / 60f;
                if (GazeRule.ShouldCast(now, last)) { last = now; casts++; }
            }
            Assert.LessOrEqual(casts, 32); // two seconds at 60 fps
        }

        [Test]
        public void ADotIsFullyShownWhileTheHitIsFresh()
        {
            Assert.AreEqual(1f, GazeRule.Alpha(true, false, 0f));
            Assert.AreEqual(1f, GazeRule.Alpha(true, false, 1.5f));
            Assert.IsTrue(GazeRule.IsVisible(true, false, 1.2f));
        }

        [Test]
        public void ADotFadesOverAboutAThirdOfASecondAfterTheHold()
        {
            Assert.AreEqual(0.5f, GazeRule.Alpha(true, false, 1.65f), 1e-4f);
            Assert.AreEqual(0f, GazeRule.Alpha(true, false, 1.8f), 1e-4f);
            Assert.IsFalse(GazeRule.IsVisible(true, false, 5f));
        }

        [Test]
        public void NoHeadOrMyOwnHeadOrNoHitMeansNoDot()
        {
            Assert.AreEqual(0f, GazeRule.Alpha(false, false, 0f));
            Assert.AreEqual(0f, GazeRule.Alpha(true, true, 0f));
            Assert.AreEqual(0f, GazeRule.Alpha(true, false, float.NaN));
        }

        [Test]
        public void AHeadsetHidesItsOwnSlotAndAPhoneHidesNone()
        {
            Assert.AreEqual(PointerRole.Professor, GazeRule.OwnSlot(true, true));
            Assert.AreEqual(PointerRole.Student, GazeRule.OwnSlot(true, false));
            Assert.IsNull(GazeRule.OwnSlot(false, false));
            Assert.IsNull(GazeRule.OwnSlot(false, true));
        }

        [Test]
        public void SmoothingDoesNotDependOnTheFrameRate()
        {
            float oneStep = GazeRule.SmoothFactor(1f / 30f, 14f);
            float twoSteps = 1f - (1f - GazeRule.SmoothFactor(1f / 60f, 14f)) * (1f - GazeRule.SmoothFactor(1f / 60f, 14f));
            Assert.AreEqual(oneStep, twoSteps, 1e-5f);
            Assert.AreEqual(0f, GazeRule.SmoothFactor(0f, 14f));
            Assert.Less(GazeRule.SmoothFactor(10f, 14f), 1.0001f);
        }
    }
}
