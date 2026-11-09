using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class InstructionTransitionRuleTests
    {
        private const InstructionTransitionPhase Idle = InstructionTransitionPhase.Idle;
        private const InstructionTransitionPhase Completing = InstructionTransitionPhase.Completing;
        private const InstructionTransitionPhase Rolling = InstructionTransitionPhase.Rolling;

        [TestCase(Idle, InstructionTransitionEvent.StepCompleted, Completing)]
        [TestCase(Completing, InstructionTransitionEvent.StepCompleted, Completing)]
        [TestCase(Rolling, InstructionTransitionEvent.StepCompleted, Completing)]
        [TestCase(Idle, InstructionTransitionEvent.StepChanged, Rolling)]
        [TestCase(Completing, InstructionTransitionEvent.StepChanged, Rolling)]
        [TestCase(Rolling, InstructionTransitionEvent.StepChanged, Rolling)]
        [TestCase(Completing, InstructionTransitionEvent.Finished, Rolling)]
        [TestCase(Rolling, InstructionTransitionEvent.Finished, Idle)]
        [TestCase(Idle, InstructionTransitionEvent.Finished, Idle)]
        [TestCase(Idle, InstructionTransitionEvent.Snapped, Idle)]
        [TestCase(Completing, InstructionTransitionEvent.Snapped, Idle)]
        [TestCase(Rolling, InstructionTransitionEvent.Snapped, Idle)]
        public void TableGivesTheNextPhase(InstructionTransitionPhase phase, InstructionTransitionEvent change, InstructionTransitionPhase expected)
        {
            Assert.That(InstructionTransitionRule.Next(phase, change), Is.EqualTo(expected));
        }

        [TestCase(ChecklistTransition.Advance, InstructionTransitionEvent.StepCompleted)]
        [TestCase(ChecklistTransition.Complete, InstructionTransitionEvent.StepCompleted)]
        [TestCase(ChecklistTransition.Revert, InstructionTransitionEvent.StepChanged)]
        [TestCase(ChecklistTransition.Reset, InstructionTransitionEvent.StepChanged)]
        [TestCase(ChecklistTransition.Snap, InstructionTransitionEvent.Snapped)]
        public void ChecklistTransitionsMapToCardEvents(ChecklistTransition transition, InstructionTransitionEvent expected)
        {
            Assert.That(InstructionTransitionRule.EventFor(transition), Is.EqualTo(expected));
        }
    }
}
