using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    public class RoomInviteTests
    {
        [TestCase("ABC123", true)]
        [TestCase("invalid!", false)]
        public void ExplicitCodesPreventPublicRoomFallbackEvenAfterThePendingCodeIsCleared(string code, bool accepted)
        {
            var go = new UnityEngine.GameObject("Join policy");
            go.SetActive(false);
            try
            {
                var join = go.AddComponent<ClassmateAutoJoin>();
                Assert.IsTrue(join.ShouldAutoJoinAnyRoom);
                Assert.AreEqual(accepted, join.JoinWithCode(code));
                Assert.IsFalse(join.ShouldAutoJoinAnyRoom);
                // The failed-join path consumes the pending code. An explicit choice must still suppress discovery.
                typeof(ClassmateAutoJoin).GetField("_pendingCode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(join, string.Empty);
                Assert.IsFalse(join.ShouldAutoJoinAnyRoom);
                Assert.IsTrue(join.JoinWithCode("XYZ789"));
                Assert.AreEqual("XYZ789", join.PendingCode);
                Assert.IsFalse(join.CodeRejected);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [TestCase("KHC8QH", "KHC8QH")]
        [TestCase("khc8qh", "KHC8QH")]
        [TestCase(" khc 8-qh ", "KHC8QH")]
        [TestCase("a1b2c3", "A1B2C3")]
        public void TypedCodes_AreCleanedUp(string typed, string expected)
        {
            Assert.IsTrue(RoomInvite.TryNormalizeCode(typed, out string code));
            Assert.AreEqual(expected, code);
        }

        [TestCase("")]
        [TestCase("   ")]
        [TestCase(null)]
        [TestCase("KHC8Q")]
        [TestCase("KHC8QHX")]
        [TestCase("KHC8Q!")]
        [TestCase("KHC8Qé")]
        public void BadCodes_AreRefused(string typed)
        {
            Assert.IsFalse(RoomInvite.TryNormalizeCode(typed, out string code));
            Assert.AreEqual(string.Empty, code);
        }

        [Test]
        public void TheLink_CarriesTheCode_AndParsesBackToIt()
        {
            string link = RoomInvite.BuildLink("khc8qh");

            Assert.AreEqual("diysect://join?code=KHC8QH", link);
            Assert.IsTrue(RoomInvite.TryParseLink(link, out string code));
            Assert.AreEqual("KHC8QH", code);
        }

        [Test]
        public void ABadCode_BuildsNoLink()
        {
            Assert.AreEqual(string.Empty, RoomInvite.BuildLink("nope"));
            Assert.AreEqual(string.Empty, RoomInvite.BuildLink(null));
        }

        [TestCase("DIYSECT://JOIN?CODE=khc8qh", "KHC8QH")]
        [TestCase("diysect://join/?code=KHC8QH", "KHC8QH")]
        [TestCase("diysect://join?room=x&code=KHC8QH&utm=1", "KHC8QH")]
        [TestCase("diysect://join?code=khc%208qh", "KHC8QH")]
        public void LinksAreReadFlexibly(string link, string expected)
        {
            Assert.IsTrue(RoomInvite.TryParseLink(link, out string code));
            Assert.AreEqual(expected, code);
        }

        [TestCase("")]
        [TestCase(null)]
        [TestCase("https://example.com/join?code=KHC8QH")]
        [TestCase("diysect://leave?code=KHC8QH")]
        [TestCase("diysect://join")]
        [TestCase("diysect://join?code=")]
        [TestCase("diysect://join?code=TOOLONGCODE")]
        [TestCase("diysect://join?room=KHC8QH")]
        public void OtherLinks_AreRefused(string link)
        {
            Assert.IsFalse(RoomInvite.TryParseLink(link, out string code));
            Assert.AreEqual(string.Empty, code);
        }
    }
}
