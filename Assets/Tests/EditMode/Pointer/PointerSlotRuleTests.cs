// Under test: the pure rule for the shared classmate pointer slot, and the wording of a classmate's label.
using DIYsect.Networking;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class PointerSlotRuleTests
    {
        private const ulong A = 1;
        private const ulong B = 2;

        [Test]
        public void TappingAnOrganOnAnEmptySlotPointsAtIt()
        {
            Assert.AreEqual(5, PointerSlotRule.Next(PointerSlotRule.None, PointerSlotRule.NoOwner, A, 5));
        }

        [Test]
        public void TappingTheSameOrganAgainStopsYourPointer()
        {
            Assert.AreEqual(PointerSlotRule.None, PointerSlotRule.Next(5, A, A, 5));
        }

        [Test]
        public void TappingAnotherOrganMovesThePointer()
        {
            Assert.AreEqual(7, PointerSlotRule.Next(5, A, A, 7));
        }

        [Test]
        public void ANewClassmateTakesOverTheSameOrganInsteadOfClearingIt()
        {
            Assert.AreEqual(5, PointerSlotRule.Next(5, A, B, 5));
        }

        [Test]
        public void TheSlotNoneMatchesThePointerBoardNone()
        {
            Assert.AreEqual(PointerBoard.None, PointerSlotRule.None);
        }

        [Test]
        public void ClassmateLabelNamesTheClassmateAndIsDistinctFromTheOtherRoles()
        {
            Assert.AreEqual("Classmate: Liver", PointerLabelText.Build(PointerRole.Classmate, "Liver", null));
            Assert.AreNotEqual(PointerLabelText.Build(PointerRole.Student, "Liver", null), PointerLabelText.Build(PointerRole.Classmate, "Liver", null));
        }

        [Test]
        public void OnlyTheSteererIsCalledTheStudent()
        {
            // The classmate Rpc refuses the scalpel steerer, which is the only client the role rule calls the Student.
            Assert.AreEqual(PointerRole.Student, PointerRoleRule.For(3, 3));
            Assert.AreEqual(PointerRole.Professor, PointerRoleRule.For(4, 3));
        }
    }
}
