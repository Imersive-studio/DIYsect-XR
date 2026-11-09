using NUnit.Framework;
using XRMultiplayer;

namespace DIYsect.Invite.Tests
{
    /// <summary>What the professor's two views turn on, and how the kept choice is read back.</summary>
    public class ProfessorViewRuleTests
    {
        [Test]
        public void TheClassroomShowsTheHallAtAFixedEyeHeight_WithNoPassthroughAndNoTablePlacing()
        {
            ProfessorViewMode mode = ProfessorViewMode.Classroom;
            Assert.IsTrue(ProfessorViewRule.HallVisible(mode));
            Assert.IsTrue(ProfessorViewRule.FixedEyeHeight(mode));
            Assert.IsFalse(ProfessorViewRule.PassthroughOn(mode));
            Assert.IsFalse(ProfessorViewRule.TableAlignmentOn(mode));
        }

        [Test]
        public void MixedRealityShowsTheRealRoomAndPlacesTheTableOnARealTable()
        {
            ProfessorViewMode mode = ProfessorViewMode.MixedReality;
            Assert.IsFalse(ProfessorViewRule.HallVisible(mode));
            Assert.IsFalse(ProfessorViewRule.FixedEyeHeight(mode));
            Assert.IsTrue(ProfessorViewRule.PassthroughOn(mode));
            Assert.IsTrue(ProfessorViewRule.TableAlignmentOn(mode));
        }

        [Test]
        public void TheProfessorStartsInTheClassroomSoTheySeeWhatTheClassSees()
        {
            Assert.AreEqual(ProfessorViewMode.Classroom, ProfessorViewRule.Default);
        }

        [Test]
        public void TheButtonFlipsToTheOtherViewAndSaysWhichOne()
        {
            Assert.AreEqual(ProfessorViewMode.MixedReality, ProfessorViewRule.Other(ProfessorViewMode.Classroom));
            Assert.AreEqual(ProfessorViewMode.Classroom, ProfessorViewRule.Other(ProfessorViewMode.MixedReality));
            StringAssert.Contains("Classroom", ProfessorViewRule.ButtonText(ProfessorViewMode.Classroom));
            StringAssert.Contains("Mixed reality", ProfessorViewRule.ButtonText(ProfessorViewMode.Classroom), "It says what pressing does.");
            StringAssert.Contains("Mixed reality", ProfessorViewRule.ButtonText(ProfessorViewMode.MixedReality));
        }

        [TestCase("Classroom", ProfessorViewMode.Classroom)]
        [TestCase("MixedReality", ProfessorViewMode.MixedReality)]
        [TestCase("", ProfessorViewMode.Classroom)]
        [TestCase("nonsense", ProfessorViewMode.Classroom)]
        [TestCase("7", ProfessorViewMode.Classroom)]
        public void AKeptChoiceIsReadBackAndAnythingElseFallsBackToTheClassroom(string kept, ProfessorViewMode expected)
        {
            Assert.AreEqual(expected, ProfessorViewRule.Parse(kept));
        }
    }
}
