// Pure rules for the student view window: which picture it shows and how often the live one renders.
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class StudentViewSourceRuleTests
    {
        [Test]
        public void HiddenShowsNothingWhoeverIsPresent()
        {
            Assert.AreEqual(StudentViewSource.None, StudentViewSourceRule.Choose(StudentViewMode.Hidden, false));
            Assert.AreEqual(StudentViewSource.None, StudentViewSourceRule.Choose(StudentViewMode.Hidden, true));
        }

        [Test]
        public void FollowingAndPinnedShowThePinWhetherOrNotTheStudentIsPresent()
        {
            foreach (StudentViewMode mode in new[] { StudentViewMode.FollowingNewest, StudentViewMode.Pinned })
            {
                Assert.AreEqual(StudentViewSource.Pin, StudentViewSourceRule.Choose(mode, false));
                Assert.AreEqual(StudentViewSource.Pin, StudentViewSourceRule.Choose(mode, true));
            }
        }

        [Test]
        public void LiveShowsTheStudentOnlyWhenPresent()
        {
            Assert.AreEqual(StudentViewSource.LiveStudent, StudentViewSourceRule.Choose(StudentViewMode.Live, true));
            Assert.AreEqual(StudentViewSource.LiveNoStudent, StudentViewSourceRule.Choose(StudentViewMode.Live, false));
        }

        [Test]
        public void EveryModeHasASource()
        {
            foreach (StudentViewMode mode in System.Enum.GetValues(typeof(StudentViewMode)))
                Assert.DoesNotThrow(() => StudentViewSourceRule.Choose(mode, true));
        }
    }

    public class StudentViewRenderRuleTests
    {
        [Test]
        public void TheFirstRenderIsAlwaysAllowed()
        {
            Assert.IsTrue(StudentViewRenderRule.ShouldRender(0f, -1f, 15f));
        }

        [Test]
        public void ARenderRightAfterAnotherIsRefused()
        {
            Assert.IsFalse(StudentViewRenderRule.ShouldRender(10.01f, 10f, 15f));
        }

        [Test]
        public void ARenderAfterTheIntervalIsAllowed()
        {
            Assert.IsTrue(StudentViewRenderRule.ShouldRender(10.07f, 10f, 15f));
        }

        [Test]
        public void TheRateIsNeverAboveFifteenOrBelowOne()
        {
            Assert.AreEqual(1f / 15f, StudentViewRenderRule.IntervalSeconds(60f), 1e-5f);
            Assert.AreEqual(1f, StudentViewRenderRule.IntervalSeconds(0f), 1e-5f);
            Assert.AreEqual(1f / 12f, StudentViewRenderRule.IntervalSeconds(12f), 1e-5f);
        }

        [Test]
        public void AtMostFifteenRendersFitInOneSecondOfFrames()
        {
            int renders = 0;
            float last = -1f;
            for (float t = 0f; t < 1f; t += 1f / 90f)
            {
                if (!StudentViewRenderRule.ShouldRender(t, last, 60f))
                    continue;
                renders++;
                last = t;
            }
            Assert.LessOrEqual(renders, 17, "About 15 renders a second at most, even at 90 frames a second.");
        }
    }
}
