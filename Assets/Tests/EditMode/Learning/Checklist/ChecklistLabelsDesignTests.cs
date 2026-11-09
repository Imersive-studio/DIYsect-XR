using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class ChecklistLabelsDesignTests
    {
        [Test]
        public void HeadlinesAreShortAndEndWithComplete()
        {
            string[] expected =
            {
                "Trace the midline", "Open the skin flaps", "Open the rectus", "Lift the omentum",
                "Lift out an organ", "Inspect it up close", "Put it back home", "Dissection complete",
            };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], ChecklistLabels.Headline((ChecklistState)i));
        }

        [Test]
        public void EveryStateHasAHint()
        {
            for (int i = 0; i <= (int)ChecklistState.Complete; i++)
                Assert.IsNotEmpty(ChecklistLabels.Hint((ChecklistState)i));
            Assert.AreEqual("Watch the skin flaps fold open.", ChecklistLabels.Hint(ChecklistState.OpenSkin));
            Assert.AreEqual("All seven steps done. Beautiful work, Doctor.", ChecklistLabels.Hint(ChecklistState.Complete));
        }

        [Test]
        public void ToolNamesFollowTheRealInteraction()
        {
            Assert.AreEqual("SCALPEL", ChecklistLabels.ToolName(ChecklistState.TraceIncision));
            Assert.AreEqual("AUTO", ChecklistLabels.ToolName(ChecklistState.OpenSkin));
            Assert.AreEqual("FORCEPS", ChecklistLabels.ToolName(ChecklistState.OpenRectus));
            Assert.AreEqual("FORCEPS", ChecklistLabels.ToolName(ChecklistState.LiftOmentum));
            Assert.AreEqual("HANDS", ChecklistLabels.ToolName(ChecklistState.LiftOrgan));
            Assert.AreEqual("HANDS", ChecklistLabels.ToolName(ChecklistState.InspectOrgan));
            Assert.AreEqual("HANDS", ChecklistLabels.ToolName(ChecklistState.PutOrganBack));
            Assert.AreEqual(string.Empty, ChecklistLabels.ToolName(ChecklistState.Complete));
        }

        [Test]
        public void CheersAreIndexedByTheFinishedStep()
        {
            Assert.AreEqual("Clean incision!", ChecklistLabels.Cheer(ChecklistState.TraceIncision));
            Assert.AreEqual("Nicely opened.", ChecklistLabels.Cheer(ChecklistState.OpenSkin));
            Assert.AreEqual("Muscle wall open.", ChecklistLabels.Cheer(ChecklistState.OpenRectus));
            Assert.AreEqual("Omentum lifted.", ChecklistLabels.Cheer(ChecklistState.LiftOmentum));
            Assert.AreEqual("Got it!", ChecklistLabels.Cheer(ChecklistState.LiftOrgan));
            Assert.AreEqual("Good look.", ChecklistLabels.Cheer(ChecklistState.InspectOrgan));
            Assert.AreEqual("Right back home.", ChecklistLabels.Cheer(ChecklistState.PutOrganBack));
            Assert.AreEqual(string.Empty, ChecklistLabels.Cheer(ChecklistState.Complete));
        }

        [Test]
        public void ChipCountAndBadgeTextTrackTheState()
        {
            Assert.AreEqual("STEP 1 OF 7", ChecklistLabels.ProgressChip(ChecklistState.TraceIncision));
            Assert.AreEqual("STEP 3 OF 7", ChecklistLabels.ProgressChip(ChecklistState.OpenRectus));
            Assert.AreEqual("STEP 7 OF 7", ChecklistLabels.ProgressChip(ChecklistState.PutOrganBack));
            Assert.AreEqual("ALL DONE", ChecklistLabels.ProgressChip(ChecklistState.Complete));
            Assert.AreEqual("0/7", ChecklistLabels.Count(ChecklistState.TraceIncision));
            Assert.AreEqual("2/7", ChecklistLabels.Count(ChecklistState.OpenRectus));
            Assert.AreEqual("7/7", ChecklistLabels.Count(ChecklistState.Complete));
            Assert.AreEqual("1", ChecklistLabels.Badge(ChecklistState.TraceIncision));
            Assert.AreEqual("7", ChecklistLabels.Badge(ChecklistState.PutOrganBack));
            Assert.AreEqual("7", ChecklistLabels.Badge(ChecklistState.Complete));
        }
    }
}
