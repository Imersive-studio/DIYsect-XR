using System.Collections.Generic;
using DIYsect.Classroom;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class SpeakRowTextTests
    {
        [TestCase(false, false, MicState.Muted)]
        [TestCase(false, true, MicState.Muted)]
        [TestCase(true, false, MicState.CanSpeak)]
        [TestCase(true, true, MicState.Live)]
        public void TheMicrophoneStateFollowsPermissionFirst(bool allowed, bool live, MicState expected)
        {
            Assert.AreEqual(expected, SpeakRowText.StateOf(allowed, live), "A live microphone without permission must still read as muted.");
        }

        [Test]
        public void TheButtonAllowsUntilPermissionIsGiven_ThenTakesItAway()
        {
            Assert.AreEqual(SpeakRowText.AllowLabel, SpeakRowText.ButtonLabel(false));
            Assert.AreEqual(SpeakRowText.MuteLabel, SpeakRowText.ButtonLabel(true));
        }

        [Test]
        public void AStudentWithAHandUpReadsAsAskingToSpeak()
        {
            Assert.AreEqual("asking to speak", SpeakRowText.StatusText(MicState.Muted, 1));
            Assert.AreEqual("muted", SpeakRowText.StatusText(MicState.Muted, 0));
            Assert.AreEqual("can speak, mic off", SpeakRowText.StatusText(MicState.CanSpeak, 0));
            Assert.AreEqual("speaking: mic on", SpeakRowText.StatusText(MicState.Live, 0));
        }

        [Test]
        public void TheHandNumberIsShownOnlyWhileTheHandIsUp()
        {
            Assert.AreEqual("#2", SpeakRowText.HandText(2));
            Assert.AreEqual(string.Empty, SpeakRowText.HandText(0));
        }

        [Test]
        public void RaisedHandsComeFirstInTheOrderTheyWentUp_ThenTheRestByName()
        {
            var rows = new List<SpeakRow>
            {
                new SpeakRow(5, "Zed", 0, false, false),
                new SpeakRow(6, "Ann", 0, false, false),
                new SpeakRow(7, "Mia", 2, false, false),
                new SpeakRow(8, "Bo", 1, false, false),
            };

            List<SpeakRow> sorted = SpeakRowText.Sorted(rows);

            Assert.AreEqual("Bo", sorted[0].Name, "The first hand up is first.");
            Assert.AreEqual("Mia", sorted[1].Name);
            Assert.AreEqual("Ann", sorted[2].Name);
            Assert.AreEqual("Zed", sorted[3].Name);
        }

        [Test]
        public void NoRowsSortsToNothing()
        {
            Assert.AreEqual(0, SpeakRowText.Sorted(null).Count);
        }
    }
}
