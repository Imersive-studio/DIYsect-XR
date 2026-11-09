using DIYsect.ClassmateConnection;
using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection.Tests
{
    public class StartScreenModelTests
    {
        private StartScreenModel _model;

        [SetUp]
        public void Create()
        {
            _model = new StartScreenModel();
        }

        [Test]
        public void WhileTheJoinerWaitsForAPress_TheScreenIsReady()
        {
            Assert.AreEqual(StartScreenPhase.Ready, _model.Update(waitingForPress: true, connected: false, codeRejected: false));
        }

        [Test]
        public void WhenNothingWaitsAndNothingWasPressed_TheScreenStaysHidden()
        {
            // The editor, a scene that joins on its own, or a classmate who opened an invite link.
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(false, false, false));
        }

        [Test]
        public void PressingJoin_ShowsJoiningUntilTheClassmateIsInTheRoom()
        {
            _model.Update(true, false, false);
            _model.Press();
            Assert.AreEqual(StartScreenPhase.Joining, _model.Update(false, false, false));
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(false, true, false));
        }

        [Test]
        public void AnInviteLinkOpenedWhileTheScreenIsUp_HidesItAtOnce()
        {
            _model.Update(true, false, false);
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(false, false, false));
        }

        [Test]
        public void AfterJoining_ALostConnectionNeverBringsTheScreenBack()
        {
            _model.Press();
            _model.Update(false, true, false);
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(false, false, false));
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(true, false, false));
        }

        [Test]
        public void AConnectedClassmateNeverSeesTheScreenEvenIfTheJoinerStillWaits()
        {
            Assert.AreEqual(StartScreenPhase.Hidden, _model.Update(true, true, false));
        }

        [Test]
        public void ARejectedCode_IsReportedWhileTheScreenIsUp()
        {
            _model.Press();
            Assert.AreEqual(StartScreenPhase.Joining, _model.Update(false, false, true));
            Assert.IsTrue(_model.CodeRejected);
            _model.Update(false, true, true);
            Assert.IsFalse(_model.CodeRejected, "Once in the room an old rejection means nothing.");
        }
    }

    public class ClassmateAutoJoinHookTests
    {
        [Test]
        public void ByDefaultItDoesNotWait_SoOtherScenesKeepTheirBehaviour()
        {
            var go = new UnityEngine.GameObject("join");
            var join = go.AddComponent<ClassmateAutoJoin>();
            Assert.IsFalse(join.WaitForJoinPress);
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void PressJoinOrAValidCode_CountsAsPressed_AnInvalidCodeDoesNot()
        {
            var go = new UnityEngine.GameObject("join");
            var join = go.AddComponent<ClassmateAutoJoin>();
            join.WaitForJoinPress = true;
            Assert.IsFalse(join.JoinPressed);
            Assert.IsFalse(join.JoinWithCode("abc"));
            Assert.IsFalse(join.JoinPressed);
            Assert.IsTrue(join.JoinWithCode("khc 8qh"));
            Assert.IsTrue(join.JoinPressed);
            Assert.AreEqual("KHC8QH", join.PendingCode);

            var second = go.AddComponent<ClassmateAutoJoin>();
            second.PressJoin();
            Assert.IsTrue(second.JoinPressed);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
