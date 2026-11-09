using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    /// <summary>The microphone each role starts with. A phone classmate must be muted until the professor allows them to speak.</summary>
    public class VoiceDefaultsRuleTests
    {
        // The scene test below opens Professor_MR. Leaving it open makes a later test that opens the same scene as a preview fail,
        // because the scene's network guid is then registered twice.
        [TearDown]
        public void CloseScene()
        {
            UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Single);
        }

        [TestCase(ClassroomRoleKind.Classmate, true)]
        [TestCase(ClassroomRoleKind.Professor, false)]
        [TestCase(ClassroomRoleKind.Student, false)]
        public void OnlyAPhoneClassmateStartsMuted(ClassroomRoleKind role, bool expectedMuted)
        {
            Assert.AreEqual(expectedMuted, VoiceDefaultsRule.StartsMuted(role));
        }

        [Test]
        public void TheSharedSceneUsesAGroupChannelSoAPhoneAndAHeadsetHearEachOther()
        {
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/Professor_MR.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);
            var manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(UnityEngine.FindObjectsInactive.Include);
            Assert.IsNotNull(manager);
            Assert.IsFalse(manager.positionalVoiceChat,
                "A positional channel only connects people who report a 3D position. A phone has none, so it would never hear the professor.");
        }

        [Test]
        public void TheRuleRunsWhenTheVoiceChannelIsJoined()
        {
            // The voice manager reports exactly this text once the local player is in the channel.
            Assert.AreEqual("Joined Voice Channel", VoiceDefaultsRule.JoinedStatus);
        }
    }
}
