using System;
using System.Collections.Generic;
using DIYsect.Editor;
using NUnit.Framework;

namespace DIYsect.Build.Tests
{
    public class DemoBuildTests
    {
        private static DemoBuildStep Step(string name, List<string> log, bool ok = true) =>
            new DemoBuildStep(name, $"Builds/{name}.apk", () => { log.Add(name); return ok; });

        [Test]
        public void Run_RunsAllStepsInOrder()
        {
            var log = new List<string>();
            var result = new DemoBuildSequence(new[] { Step("A", log), Step("B", log), Step("C", log) }).Run();

            Assert.IsTrue(result.Succeeded);
            CollectionAssert.AreEqual(new[] { "A", "B", "C" }, log);
            Assert.AreEqual(3, result.Completed.Count);
        }

        [Test]
        public void Run_StopsAtFirstFailureAndNamesIt()
        {
            var log = new List<string>();
            var result = new DemoBuildSequence(new[] { Step("A", log), Step("B", log, false), Step("C", log) }).Run();

            Assert.IsFalse(result.Succeeded);
            CollectionAssert.AreEqual(new[] { "A", "B" }, log);
            Assert.AreEqual("B", result.Failed.Name);
            StringAssert.Contains("B", result.Error);
            Assert.AreEqual(1, result.Completed.Count);
        }

        [Test]
        public void Run_TreatsAThrowingStepAsAFailure()
        {
            var steps = new[] { new DemoBuildStep("Boom", "x.apk", () => throw new InvalidOperationException("no sdk")) };
            var result = new DemoBuildSequence(steps).Run();

            Assert.IsFalse(result.Succeeded);
            StringAssert.Contains("Boom", result.Error);
            StringAssert.Contains("no sdk", result.Error);
        }

        [Test]
        public void CreateSteps_ListsProfessorStudentClassmate()
        {
            var steps = BuildAllDemoApks.CreateSteps();

            CollectionAssert.AreEqual(new[] { "Professor", "Student", "Classmate" }, new[] { steps[0].Name, steps[1].Name, steps[2].Name });
            Assert.AreEqual(BuildAndroidClassmate.OutputPath, steps[2].OutputPath);
        }

        [Test]
        public void Format_ListsCommitDateAndEachApk()
        {
            var entries = new[]
            {
                new DemoBuildSummary.Entry("Professor", "Builds/p.apk", 2 * 1024 * 1024),
                new DemoBuildSummary.Entry("Student", "Builds/s.apk", -1)
            };
            string text = DemoBuildSummary.Format("abc123", new DateTime(2026, 11, 8, 21, 5, 0), entries);

            StringAssert.Contains("Commit: abc123", text);
            StringAssert.Contains("Date: 2026-11-08 21:05", text);
            StringAssert.Contains("Professor: Builds/p.apk (2.0 MB)", text);
            StringAssert.Contains("Student: Builds/s.apk (missing)", text);
        }

        [Test]
        public void Format_WritesUnknownWhenThereIsNoCommit()
        {
            StringAssert.Contains("Commit: unknown", DemoBuildSummary.Format("", DateTime.Now, new DemoBuildSummary.Entry[0]));
        }
    }
}
