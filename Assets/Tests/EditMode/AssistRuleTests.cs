using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class AssistRuleTests
    {
        [TestCase(AssistPhase.None, AssistEvent.StudentRequested, AssistPhase.Requested)]
        [TestCase(AssistPhase.None, AssistEvent.StudentCancelled, AssistPhase.None)]
        [TestCase(AssistPhase.None, AssistEvent.ProfessorTookControl, AssistPhase.None)]
        [TestCase(AssistPhase.None, AssistEvent.ProfessorReleased, AssistPhase.None)]
        [TestCase(AssistPhase.None, AssistEvent.SteererLeft, AssistPhase.None)]
        [TestCase(AssistPhase.Requested, AssistEvent.StudentRequested, AssistPhase.Requested)]
        [TestCase(AssistPhase.Requested, AssistEvent.StudentCancelled, AssistPhase.None)]
        [TestCase(AssistPhase.Requested, AssistEvent.ProfessorTookControl, AssistPhase.Helping)]
        [TestCase(AssistPhase.Requested, AssistEvent.ProfessorReleased, AssistPhase.Requested)]
        [TestCase(AssistPhase.Requested, AssistEvent.SteererLeft, AssistPhase.None)]
        [TestCase(AssistPhase.Helping, AssistEvent.StudentRequested, AssistPhase.Helping)]
        [TestCase(AssistPhase.Helping, AssistEvent.StudentCancelled, AssistPhase.None)]
        [TestCase(AssistPhase.Helping, AssistEvent.ProfessorTookControl, AssistPhase.Helping)]
        [TestCase(AssistPhase.Helping, AssistEvent.ProfessorReleased, AssistPhase.None)]
        [TestCase(AssistPhase.Helping, AssistEvent.SteererLeft, AssistPhase.None)]
        public void RuleTableCoversEveryPhaseAndEvent(AssistPhase phase, AssistEvent change, AssistPhase expected)
        {
            Assert.AreEqual(expected, AssistRule.Next(phase, change));
        }
    }
}
