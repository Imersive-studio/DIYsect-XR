using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection.Tests
{
    public class ReconnectBannerModelTests
    {
        private const XRINetworkGameManager.ConnectionState Authenticated = XRINetworkGameManager.ConnectionState.Authenticated;

        private static ReconnectBannerModel InTheRoom()
        {
            var model = new ReconnectBannerModel();
            model.SetState(Authenticated);
            model.SetConnected(true);
            return model;
        }

        [Test]
        public void StartsHidden_AndStaysHiddenWhileLookingForTheFirstRoom()
        {
            var model = new ReconnectBannerModel();
            model.SetState(XRINetworkGameManager.ConnectionState.Authenticating);
            model.SetState(Authenticated);
            model.SetState(XRINetworkGameManager.ConnectionState.Connecting);
            Assert.IsFalse(model.Visible);
        }

        [Test]
        public void HiddenWhileConnected()
        {
            Assert.IsFalse(InTheRoom().Visible);
        }

        [Test]
        public void ADropShowsReconnecting_AndComingBackHidesIt()
        {
            ReconnectBannerModel model = InTheRoom();
            model.SetConnected(false);
            model.SetState(Authenticated);
            Assert.IsTrue(model.Visible);
            StringAssert.Contains("Reconnecting", model.Text);

            model.SetConnected(true);
            Assert.IsFalse(model.Visible);
        }

        [Test]
        public void TheTextFollowsTheConnectionState_AfterADrop()
        {
            ReconnectBannerModel model = InTheRoom();
            model.SetConnected(false);

            model.SetState(XRINetworkGameManager.ConnectionState.Connecting);
            StringAssert.Contains("Rejoining", model.Text);
            model.SetState(XRINetworkGameManager.ConnectionState.None);
            StringAssert.Contains("internet", model.Text);
            model.SetState(Authenticated);
            StringAssert.Contains("Reconnecting", model.Text);
        }

        [Test]
        public void GivingUpTellsTheClassmateWhatToDo_AndReturningClearsIt()
        {
            ReconnectBannerModel model = InTheRoom();
            model.SetConnected(false);
            model.SetState(Authenticated);
            model.SetGaveUp(true);
            Assert.AreEqual(ReconnectBannerModel.GaveUpText, model.Text);

            model.SetConnected(true);
            model.SetConnected(false);
            StringAssert.Contains("Reconnecting", model.Text);
        }

        [Test]
        public void AWrongCodeBeforeEverJoiningShowsTheFriendlyMessage_AndANewAttemptClearsIt()
        {
            var model = new ReconnectBannerModel();
            model.SetState(Authenticated);
            model.SetFailure(JoinFailure.RoomNotFound);
            Assert.AreEqual(JoinFailureMessages.For(JoinFailure.RoomNotFound), model.Text);

            model.SetState(XRINetworkGameManager.ConnectionState.Connecting);
            Assert.IsFalse(model.Visible);
        }

        [Test]
        public void AFullRoomShowsTheFullMessage()
        {
            var model = new ReconnectBannerModel();
            model.SetFailure(JoinFailure.RoomFull);
            Assert.AreEqual(JoinFailureMessages.For(JoinFailure.RoomFull), model.Text);
        }

        [Test]
        public void ABusyReportIsNotShown()
        {
            var model = new ReconnectBannerModel();
            model.SetFailure(JoinFailure.Busy);
            Assert.IsFalse(model.Visible);
        }

        [Test]
        public void NoRoomFoundAfterBeingInTheRoomMeansTheRoomIsGone()
        {
            ReconnectBannerModel model = InTheRoom();
            model.SetConnected(false);
            model.SetState(Authenticated);
            model.SetFailure(JoinFailure.RoomNotFound);
            StringAssert.Contains(JoinFailureMessages.For(JoinFailure.RoomClosed), model.Text);
            StringAssert.Contains("Reconnecting", model.Text);
        }

        [Test]
        public void ConnectingClearsAnOldFailureWhileReconnecting()
        {
            ReconnectBannerModel model = InTheRoom();
            model.SetConnected(false);
            model.SetFailure(JoinFailure.TooManyTries);
            model.SetState(XRINetworkGameManager.ConnectionState.Connecting);
            Assert.AreEqual("Rejoining the class…", model.Text);
        }
    }
}
