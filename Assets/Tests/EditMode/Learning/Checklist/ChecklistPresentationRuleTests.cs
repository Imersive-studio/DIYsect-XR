using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class ChecklistPresentationRuleTests
    {
        [Test]
        public void FirstPresentationSnapsAtEveryStep()
        {
            foreach (ChecklistState state in System.Enum.GetValues(typeof(ChecklistState)))
                Assert.That(ChecklistTransitionRule.Evaluate(null, state), Is.EqualTo(ChecklistTransition.Snap));
        }

        [Test]
        public void DuplicateStateNeverReplaysTransition()
        {
            foreach (ChecklistState state in System.Enum.GetValues(typeof(ChecklistState)))
                Assert.That(ChecklistTransitionRule.Evaluate(state, state), Is.EqualTo(ChecklistTransition.Snap));
        }

        [TestCase(ChecklistState.TraceIncision, ChecklistState.OpenSkin, ChecklistTransition.Advance)]
        [TestCase(ChecklistState.TraceIncision, ChecklistState.OpenRectus, ChecklistTransition.Advance)]
        [TestCase(ChecklistState.TraceIncision, ChecklistState.LiftOrgan, ChecklistTransition.Advance)]
        [TestCase(ChecklistState.InspectOrgan, ChecklistState.LiftOrgan, ChecklistTransition.Revert)]
        [TestCase(ChecklistState.Complete, ChecklistState.TraceIncision, ChecklistTransition.Reset)]
        [TestCase(ChecklistState.PutOrganBack, ChecklistState.Complete, ChecklistTransition.Complete)]
        public void ChangesChooseTheExpectedPresentation(ChecklistState previous, ChecklistState next, ChecklistTransition expected)
        {
            Assert.That(ChecklistTransitionRule.Evaluate(previous, next), Is.EqualTo(expected));
        }

        [Test]
        public void EveryResetOverridesDirection()
        {
            for (int step = 1; step <= 7; step++)
                Assert.That(ChecklistTransitionRule.Evaluate((ChecklistState)step, ChecklistState.TraceIncision), Is.EqualTo(ChecklistTransition.Reset));
        }

        [TestCase(0f, ChecklistCueStrength.Subtle)]
        [TestCase(11.999f, ChecklistCueStrength.Subtle)]
        [TestCase(12f, ChecklistCueStrength.Explicit)]
        [TestCase(60f, ChecklistCueStrength.Explicit)]
        public void IdleCueEscalatesAtTwelveSeconds(float elapsed, ChecklistCueStrength expected)
        {
            Assert.That(ChecklistEscalationRule.Evaluate(ChecklistState.OpenRectus, elapsed), Is.EqualTo(expected));
        }

        [Test]
        public void StepProgressResetsTheHintAndCompleteNeverEscalates()
        {
            Assert.That(ChecklistEscalationRule.Evaluate(ChecklistState.LiftOmentum, 0f), Is.EqualTo(ChecklistCueStrength.Subtle));
            Assert.That(ChecklistEscalationRule.Evaluate(ChecklistState.Complete, 60f), Is.EqualTo(ChecklistCueStrength.Subtle));
        }

        [Test]
        public void InstructionsMatchTheActualToolsAndAutomaticSkinStep()
        {
            Assert.That(ChecklistLabels.Instruction(ChecklistState.TraceIncision), Is.EqualTo("Hold grip on the scalpel. Draw the tip down the line"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.OpenSkin), Is.EqualTo("Skin opening"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.OpenRectus), Does.Contain("forceps tip"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.LiftOmentum), Does.Contain("forceps tip"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.LiftOrgan), Is.EqualTo("Hold grip on an organ to lift it out"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.InspectOrgan), Is.EqualTo("Keep holding to inspect it"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.PutOrganBack), Is.EqualTo("Bring it back to its place"));
            Assert.That(ChecklistLabels.Instruction(ChecklistState.Complete), Is.EqualTo(ChecklistLabels.AllDone));
        }
    }
}
