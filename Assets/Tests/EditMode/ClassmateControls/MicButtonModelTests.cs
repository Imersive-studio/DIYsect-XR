using NUnit.Framework;

namespace DIYsect.ClassmateControls.Tests
{
    public class MicButtonModelTests
    {
        private FakeVoiceService _voice;
        private MicButtonModel _model;

        [SetUp]
        public void Create()
        {
            _voice = new FakeVoiceService();
            _model = new MicButtonModel(_voice);
        }

        [Test]
        public void AClassmateStartsMuted_AndTheButtonOffersUnmute()
        {
            Assert.IsFalse(_model.IsLive);
            Assert.AreEqual(MicButtonModel.UnmuteLabel, _model.Label);
        }

        [Test]
        public void WithPermission_PressingTogglesBetweenMutedAndLive()
        {
            _voice.HasMicPermission = true;

            _model.Press();
            Assert.IsTrue(_model.IsLive);
            Assert.AreEqual(MicButtonModel.MuteLabel, _model.Label);
            Assert.AreEqual(0, _voice.PermissionRequests, "No dialog when the app may already use the microphone.");

            _model.Press();
            Assert.IsFalse(_model.IsLive);
            Assert.AreEqual(MicButtonModel.UnmuteLabel, _model.Label);
        }

        [Test]
        public void TheFirstUnmute_AsksForTheMicrophoneAndGoesLiveWhenAllowed()
        {
            _model.Press();
            Assert.AreEqual(1, _voice.PermissionRequests);
            Assert.IsTrue(_model.Asking);
            Assert.AreEqual(MicButtonModel.AskingLabel, _model.Label);
            Assert.IsFalse(_model.IsLive, "Still muted while the dialog is open.");

            _voice.Answer(true);

            Assert.IsFalse(_model.Asking);
            Assert.IsTrue(_model.IsLive);
            Assert.AreEqual(string.Empty, _model.Message);
        }

        [Test]
        public void ARefusedMicrophone_StaysMutedAndSaysSo()
        {
            _model.Press();
            _voice.Answer(false);

            Assert.IsFalse(_model.IsLive);
            Assert.IsTrue(_voice.IsMuted);
            Assert.AreEqual(MicButtonModel.DeniedText, _model.Message);
            Assert.AreEqual(MicButtonModel.UnmuteLabel, _model.Label);
        }

        [Test]
        public void PressingAgainAfterARefusal_AsksAgainAndTheNoteClears()
        {
            _model.Press();
            _voice.Answer(false);
            _model.Press();

            Assert.AreEqual(2, _voice.PermissionRequests);
            Assert.AreEqual(string.Empty, _model.Message);
        }

        [Test]
        public void PressingWhileTheDialogIsOpen_DoesNotAskTwice()
        {
            _model.Press();
            _model.Press();
            Assert.AreEqual(1, _voice.PermissionRequests);
        }

        [Test]
        public void LosingThePermissionLater_ReadsAsMutedEvenIfTheManagerSaysUnmuted()
        {
            _voice.HasMicPermission = true;
            _model.Press();
            Assert.IsTrue(_model.IsLive);
            _voice.HasMicPermission = false;
            Assert.IsFalse(_model.IsLive);
        }

        [Test]
        public void TheModelTellsItsViewWhenSomethingChanged()
        {
            int changes = 0;
            _model.Changed += () => changes++;
            _voice.HasMicPermission = true;
            _model.Press();
            Assert.Greater(changes, 0);
        }
    }
}
