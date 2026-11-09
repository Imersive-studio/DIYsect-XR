using System.Collections.Generic;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class ClassRosterTextTests
    {
        private static RosterEntry Student(ulong id, string name, RosterKind kind = RosterKind.Screen, int questions = 0) =>
            new RosterEntry(id, name, kind, questions);

        [Test]
        public void NoStudents_SaysSo()
        {
            Assert.AreEqual("Class: no students yet", ClassRosterText.Format(new List<RosterEntry>()));
            Assert.AreEqual("Class: no students yet", ClassRosterText.Format(null));
        }

        [Test]
        public void OneStudent_IsSingularAndShowsTheDevice()
        {
            string text = ClassRosterText.Format(new[] { Student(1, "Maya", RosterKind.Headset) });

            StringAssert.StartsWith("Class: 1 student\n", text);
            StringAssert.Contains("Maya  headset", text);
        }

        [Test]
        public void SeveralStudents_AreCountedSortedAndShowTheirQuestions()
        {
            string text = ClassRosterText.Format(new[]
            {
                Student(3, "Zed", RosterKind.Screen, 2),
                Student(2, "Ana", RosterKind.Headset, 1),
                Student(4, "Bo"),
            });

            string[] lines = text.Split('\n');
            Assert.AreEqual("Class: 3 students", lines[0]);
            Assert.AreEqual("Ana  headset  1 question", lines[1]);
            Assert.AreEqual("Bo  phone/laptop", lines[2]);
            Assert.AreEqual("Zed  phone/laptop  2 questions", lines[3]);
        }

        [Test]
        public void StudentsWithTheSameName_GetTheirClientIdSoTheyCanBeTold_Apart()
        {
            string text = ClassRosterText.Format(new[] { Student(5, "Player"), Student(9, "Player") });

            StringAssert.Contains("Player #5", text);
            StringAssert.Contains("Player #9", text);
        }

        [Test]
        public void LongNames_AreShortened_AndMarkupAndLineBreaksAreRemoved()
        {
            string text = ClassRosterText.Format(new[] { Student(1, "<b>A very long student name indeed</b>\nsecond line") });

            string line = text.Split('\n')[1];
            StringAssert.DoesNotContain("<", line);
            StringAssert.Contains("…", line);
            Assert.AreEqual(2, text.Split('\n').Length, "A line break in a name must not add a line to the roster.");
        }

        [Test]
        public void ABlankName_ShowsAsStudent()
        {
            StringAssert.Contains("Student  phone/laptop", ClassRosterText.Format(new[] { Student(1, "  ") }));
        }

        [Test]
        public void MoreThanTheLimit_AreSummarised()
        {
            var entries = new List<RosterEntry>();
            for (int i = 0; i < ClassRosterText.MaxLines + 3; i++)
                entries.Add(Student((ulong)i, $"S{i:00}"));

            string[] lines = ClassRosterText.Format(entries).Split('\n');

            Assert.AreEqual(1 + ClassRosterText.MaxLines + 1, lines.Length);
            Assert.AreEqual("+3 more", lines[lines.Length - 1]);
            Assert.AreEqual($"Class: {ClassRosterText.MaxLines + 3} students", lines[0]);
        }

        [Test]
        public void TheInvite_IsShownAboveTheRoster_WhenThereIsACode()
        {
            string text = ClassRosterText.Format(new[] { Student(1, "Maya") }, "KHC8QH", "diysect://join?code=KHC8QH");

            string[] lines = text.Split('\n');
            Assert.AreEqual("Join code: KHC8QH", lines[0]);
            Assert.AreEqual("diysect://join?code=KHC8QH", lines[1]);
            Assert.AreEqual("Class: 1 student", lines[2]);
        }

        [Test]
        public void NoCode_LeavesTheInviteOut()
        {
            Assert.AreEqual("Class: no students yet", ClassRosterText.Format(new List<RosterEntry>(), null, null));
            Assert.AreEqual("Class: no students yet", ClassRosterText.Format(new List<RosterEntry>(), "", ""));
        }

        [Test]
        public void AMarkupCode_CannotInjectTags()
        {
            string text = ClassRosterText.Format(new List<RosterEntry>(), "<b>X</b>", null);

            StringAssert.DoesNotContain("<", text);
        }
    }
}
