// Pure state and maths for the classmate follow camera; no scene.
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Classroom.Tests
{
    public class FollowTransitionTests
    {
        private const float Seconds = 1f;

        [Test]
        public void StartsOnMyViewAndDoesNotDriveTheCamera()
        {
            var transition = new FollowTransition(Seconds);
            Assert.AreEqual(FollowTarget.Mine, transition.Target);
            Assert.IsFalse(transition.IsFollowing);
            Assert.IsFalse(transition.Driving);
        }

        [Test]
        public void FollowingSomeoneWhoIsNotHereIsRefusedAndChangesNothing()
        {
            var transition = new FollowTransition(Seconds);
            Assert.IsFalse(transition.Request(FollowTarget.Student, available: false));
            Assert.AreEqual(FollowTarget.Mine, transition.Target);
            Assert.IsFalse(transition.Driving);
        }

        [Test]
        public void ARequestStartsAtZeroSoTheFirstFrameDoesNotJump()
        {
            var transition = new FollowTransition(Seconds);
            Assert.IsTrue(transition.Request(FollowTarget.Professor, available: true));
            Assert.IsTrue(transition.Driving);
            Assert.AreEqual(0f, transition.Progress, 1e-6f);
            Assert.AreEqual(0f, transition.Eased, 1e-6f);
        }

        [Test]
        public void ProgressReachesOneAfterTheDurationWhateverTheFrameRate()
        {
            foreach (float step in new[] { 1f / 30f, 1f / 60f, 1f / 144f })
            {
                var transition = new FollowTransition(Seconds);
                transition.Request(FollowTarget.Professor, true);
                float time = 0f;
                while (!transition.Arrived && time < 5f)
                {
                    transition.Tick(step, true);
                    time += step;
                }
                Assert.AreEqual(Seconds, time, step + 1e-4f, $"step {step}");
            }
        }

        [Test]
        public void TheEaseIsFlatAtBothEndsAndNeverGoesBackwards()
        {
            Assert.AreEqual(0f, FollowTransition.Smooth(0f), 1e-6f);
            Assert.AreEqual(1f, FollowTransition.Smooth(1f), 1e-6f);
            Assert.AreEqual(0.5f, FollowTransition.Smooth(0.5f), 1e-6f);
            Assert.Less(FollowTransition.Smooth(0.05f), 0.05f, "slow at the start");
            Assert.Greater(FollowTransition.Smooth(0.95f), 0.95f, "slow at the end");
            float last = 0f;
            for (int i = 0; i <= 100; i++)
            {
                float value = FollowTransition.Smooth(i / 100f);
                Assert.GreaterOrEqual(value, last);
                last = value;
            }
        }

        [Test]
        public void BackToMyViewKeepsDrivingUntilItHasArrived()
        {
            var transition = new FollowTransition(Seconds);
            transition.Request(FollowTarget.Professor, true);
            transition.Tick(Seconds, true);
            Assert.IsTrue(transition.Request(FollowTarget.Mine, true));
            Assert.IsFalse(transition.IsFollowing);
            Assert.IsTrue(transition.Driving, "the camera is still easing home");
            transition.Tick(0.5f, true);
            Assert.IsTrue(transition.Driving);
            transition.Tick(0.6f, true);
            Assert.IsFalse(transition.Driving, "arrived: the orbit camera takes over again");
        }

        [Test]
        public void SwitchingPersonRestartsTheEaseInsteadOfJumping()
        {
            var transition = new FollowTransition(Seconds);
            transition.Request(FollowTarget.Professor, true);
            transition.Tick(Seconds, true);
            transition.Request(FollowTarget.Student, true);
            Assert.AreEqual(FollowTarget.Student, transition.Target);
            Assert.AreEqual(0f, transition.Progress, 1e-6f);
        }

        [Test]
        public void AskingForTheCurrentTargetAgainKeepsTheProgress()
        {
            var transition = new FollowTransition(Seconds);
            transition.Request(FollowTarget.Professor, true);
            transition.Tick(0.4f, true);
            Assert.IsTrue(transition.Request(FollowTarget.Professor, true));
            Assert.AreEqual(0.4f, transition.Progress, 1e-5f);
        }

        [Test]
        public void WhenTheFollowedPersonLeavesTheCameraHeadsBackToMyView()
        {
            var transition = new FollowTransition(Seconds);
            transition.Request(FollowTarget.Student, true);
            transition.Tick(Seconds, true);
            bool lost = transition.Tick(0.1f, targetAvailable: false);
            Assert.IsTrue(lost);
            Assert.AreEqual(FollowTarget.Mine, transition.Target);
            Assert.IsTrue(transition.Driving);
            Assert.Less(transition.Progress, 0.2f);
        }

        [Test]
        public void BlendReturnsTheEndsAtZeroAndOne()
        {
            Quaternion fromRotation = Quaternion.Euler(10f, 20f, 0f);
            Quaternion toRotation = Quaternion.Euler(40f, 200f, 0f);
            FollowTransition.Blend(Vector3.zero, fromRotation, Vector3.one, toRotation, 0f, out Vector3 p0, out Quaternion r0);
            FollowTransition.Blend(Vector3.zero, fromRotation, Vector3.one, toRotation, 1f, out Vector3 p1, out Quaternion r1);
            Assert.AreEqual(Vector3.zero, p0);
            Assert.AreEqual(Vector3.one, p1);
            Assert.Less(Quaternion.Angle(fromRotation, r0), 0.01f);
            Assert.Less(Quaternion.Angle(toRotation, r1), 0.01f);
        }

        [Test]
        public void LevelKeepsTheLookDirectionAndRemovesTheTilt()
        {
            Quaternion tilted = Quaternion.Euler(20f, 70f, 25f);
            Quaternion level = FollowTransition.Level(tilted);
            Assert.Less(Vector3.Angle(tilted * Vector3.forward, level * Vector3.forward), 0.01f);
            Assert.AreEqual(0f, (level * Vector3.right).y, 1e-5f, "the camera's right stays horizontal");
        }

        [Test]
        public void LevelLeavesAHeadLookingStraightDownAlone()
        {
            Quaternion down = Quaternion.Euler(90f, 30f, 0f);
            Assert.Less(Quaternion.Angle(down, FollowTransition.Level(down)), 0.01f);
        }
    }
}
