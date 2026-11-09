using NUnit.Framework;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.MixedReality.Tests
{
    /// <summary>
    /// A classmate who typed a bad code and then presses Join must be able to join any room again. A submitted code switches
    /// public-room discovery off, and pressing Join used to leave it off, so the screen sat on "Joining the room" for ever.
    /// </summary>
    public class ClassmateJoinPressTests
    {
        [Test]
        public void PressingJoinAfterABadCodeAllowsAnyRoomAgain()
        {
            var host = new GameObject("JoinPressTest");
            try
            {
                var join = host.AddComponent<ClassmateAutoJoin>();
                join.AutoJoinAnyRoom = true;
                Assert.IsTrue(join.ShouldAutoJoinAnyRoom, "Any room may be joined before a code is typed.");

                Assert.IsFalse(join.JoinWithCode("no"), "Too short to be a room code.");
                Assert.IsFalse(join.ShouldAutoJoinAnyRoom, "A submitted code switches public-room discovery off.");

                join.PressJoin();
                Assert.IsTrue(join.ShouldAutoJoinAnyRoom, "Pressing Join must ask for any room again.");
                Assert.IsTrue(join.JoinPressed);
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
        }
    }
}
