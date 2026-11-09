using System;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class ChecklistLabelsTests
    {
        private static int Count(string text, string part)
        {
            return text.Split(new[] { part }, StringSplitOptions.None).Length - 1;
        }

        [Test]
        public void TheStepCountMatchesTheStatesBeforeComplete()
        {
            Assert.AreEqual((int)ChecklistState.Complete, ChecklistLabels.StepCount);
        }

        [Test]
        public void EveryStepHasWording()
        {
            for (int i = 0; i < ChecklistLabels.StepCount; i++)
                Assert.IsNotEmpty(ChecklistLabels.StepText(i), i.ToString());
        }

        [Test]
        public void AtTheStartTheFirstStepIsNextAndNothingIsTicked()
        {
            string text = ChecklistLabels.Format(ChecklistState.TraceIncision);

            StringAssert.StartsWith(ChecklistLabels.Title, text);
            StringAssert.Contains("[>] " + ChecklistLabels.StepText(0), text);
            Assert.AreEqual(0, Count(text, "[x]"));
            Assert.AreEqual(1, Count(text, "[>]"));
            Assert.AreEqual(ChecklistLabels.StepCount - 1, Count(text, "[ ]"));
            StringAssert.DoesNotContain(ChecklistLabels.AllDone, text);
        }

        [Test]
        public void InTheMiddleDoneStepsAreTickedAndOneIsNext()
        {
            string text = ChecklistLabels.Format(ChecklistState.LiftOmentum);

            Assert.AreEqual(3, Count(text, "[x]"));
            StringAssert.Contains("[>] " + ChecklistLabels.StepText(3), text);
            Assert.AreEqual(3, Count(text, "[ ]"));
        }

        [Test]
        public void WhenCompleteEveryStepIsTickedAndItSaysSo()
        {
            string text = ChecklistLabels.Format(ChecklistState.Complete);

            Assert.AreEqual(ChecklistLabels.StepCount, Count(text, "[x]"));
            Assert.AreEqual(0, Count(text, "[>]"));
            StringAssert.EndsWith(ChecklistLabels.AllDone, text);
        }

        [Test]
        public void TheDoneCountIsTheStateNumber()
        {
            Assert.AreEqual(0, ChecklistLabels.DoneCount(ChecklistState.TraceIncision));
            Assert.AreEqual(5, ChecklistLabels.DoneCount(ChecklistState.InspectOrgan));
            Assert.AreEqual(7, ChecklistLabels.DoneCount(ChecklistState.Complete));
        }

        [Test]
        public void TheOneLineNamesTheNextStep()
        {
            Assert.AreEqual("Checklist 2/7: Open the rectus", ChecklistLabels.OneLine(ChecklistState.OpenRectus));
        }

        [Test]
        public void TheOneLineSaysWhenEverythingIsDone()
        {
            Assert.AreEqual("Checklist 7/7: All steps done.", ChecklistLabels.OneLine(ChecklistState.Complete));
        }
    }
}
