using NUnit.Framework;

namespace DIYsect.ClassmateControls.Tests
{
    /// <summary>A classmate cannot unmute until the professor allows it. They ask first; the professor's answer opens the button.</summary>
    public class SpeakPermissionTests
    {
        private FakeVoiceService _voice;
        private MicButtonModel _model;

        [SetUp]
        public void Create()
        {
            _voice = new FakeVoiceService { MayUnmute = false, HasMicPermission = true };
            _model = new MicButtonModel(_voice);
        }

        [Test]
        public void WithoutPermission_TheButtonOffersToAsk_NotToUnmute()
        {
            Assert.AreEqual(MicButtonModel.AskLabel, _model.Label);
            Assert.IsFalse(_model.IsLive);
        }

        [Test]
        public void PressingWithoutPermission_AsksTheProfessor_AndStaysMuted()
        {
            _model.Press();

            Assert.AreEqual(1, _voice.SpeakRequests, "The press raises the classmate's hand.");
            Assert.IsTrue(_voice.IsMuted, "Nobody is heard without permission.");
            Assert.IsFalse(_model.IsLive);
            Assert.AreEqual(MicButtonModel.AskedText, _model.Message);
            Assert.AreEqual(0, _voice.PermissionRequests, "The microphone is not even requested before the professor allows it.");
        }

        [Test]
        public void OncePermissionArrives_TheButtonReadsUnmute_AndUnmuteWorks()
        {
            _voice.MayUnmute = true;
            _model.Notify();
            Assert.AreEqual(MicButtonModel.UnmuteLabel, _model.Label);

            _model.Press();
            Assert.IsTrue(_model.IsLive);
            Assert.AreEqual(MicButtonModel.MuteLabel, _model.Label);
        }

        [Test]
        public void AClassmateWhoAsked_GoesLiveTheMomentTheProfessorAllows()
        {
            _model.Press();
            Assert.IsFalse(_model.IsLive);

            _voice.MayUnmute = true;
            _model.Notify();

            Assert.IsTrue(_model.IsLive, "Raise hand, professor says yes, speak: no second tap.");
            Assert.AreEqual(MicButtonModel.LiveText, _model.Message);
            _model.Notify();
            Assert.IsTrue(_model.IsLive, "A later notice must not toggle the microphone again.");
        }

        [Test]
        public void AClassmateWhoAsked_WithoutTheMicrophone_IsAskedForItWhenAllowed()
        {
            _voice.HasMicPermission = false;
            _model.Press();
            _voice.MayUnmute = true;
            _model.Notify();

            Assert.AreEqual(1, _voice.PermissionRequests, "Allowed to speak, so the phone asks for the microphone once.");
        }

        [Test]
        public void WhenTheProfessorTakesPermissionAway_AnOpenMicrophoneClosesAtOnce()
        {
            _voice.MayUnmute = true;
            _model.Press();
            Assert.IsTrue(_model.IsLive);

            _voice.MayUnmute = false;
            _model.Notify();

            Assert.IsTrue(_voice.IsMuted, "The microphone must close when permission is taken away.");
            Assert.IsFalse(_model.IsLive);
            Assert.AreEqual(MicButtonModel.AskLabel, _model.Label);
        }

        [Test]
        public void ThePermissionDialogResultCannotOpenTheMicrophoneWithoutPermission()
        {
            // The microphone permission was asked while allowed, then the professor took the permission away before the dialog answered.
            _voice.HasMicPermission = false;
            _voice.MayUnmute = true;
            _model.Press();
            _voice.MayUnmute = false;

            _voice.Answer(true);

            Assert.IsTrue(_voice.IsMuted, "An answered dialog must not unmute someone who may no longer speak.");
        }
    }
}
