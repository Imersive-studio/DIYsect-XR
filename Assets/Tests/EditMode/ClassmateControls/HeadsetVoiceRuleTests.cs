using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.ClassmateControls.Tests
{
    public class HeadsetVoiceRuleTests
    {
        private const string Joined = HeadsetVoiceRule.JoinedStatus;

        [Test]
        public void TheProfessorAndTheStudentSpeakFreely_AClassmateAsksFirst()
        {
            Assert.IsTrue(HeadsetVoiceRule.SpeaksFreely(ClassroomRoleKind.Professor));
            Assert.IsTrue(HeadsetVoiceRule.SpeaksFreely(ClassroomRoleKind.Student));
            Assert.IsFalse(HeadsetVoiceRule.SpeaksFreely(ClassroomRoleKind.Classmate));
        }

        [Test]
        public void AHeadsetInTheVoiceChannel_OpensItsMicrophone()
        {
            Assert.IsTrue(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Professor, true, true, Joined, true));
            Assert.IsTrue(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Student, true, true, Joined, true));
        }

        [Test]
        public void NothingOpensBeforeTheRoomTheMicrophoneAndTheChannelAreReady()
        {
            Assert.IsFalse(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Professor, false, true, Joined, true), "Not in a room yet.");
            Assert.IsFalse(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Professor, true, false, Joined, true), "No microphone permission.");
            Assert.IsFalse(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Professor, true, true, "Joining Voice Channel", true), "Still joining.");
            Assert.IsFalse(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Professor, true, true, Joined, false), "Already open.");
            Assert.IsFalse(HeadsetVoiceRule.ShouldOpen(ClassroomRoleKind.Classmate, true, true, Joined, true), "A phone asks the professor.");
        }
    }
}
