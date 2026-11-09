using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    public class ChecklistIconTests
    {
        [Test]
        public void EveryIconHasStrokesInsideTheSquare()
        {
            foreach (ChecklistIcon icon in System.Enum.GetValues(typeof(ChecklistIcon)))
            {
                Vector2[][] strokes = ChecklistIconShapes.Strokes(icon);
                Assert.That(strokes.Length, Is.InRange(1, ChecklistIconShapes.MaxStrokes), icon.ToString());
                foreach (Vector2[] stroke in strokes)
                {
                    Assert.That(stroke.Length, Is.GreaterThanOrEqualTo(2), icon.ToString());
                    foreach (Vector2 point in stroke)
                        Assert.That(Mathf.Max(Mathf.Abs(point.x), Mathf.Abs(point.y)), Is.LessThanOrEqualTo(1f), icon.ToString());
                }
            }
        }

        [Test]
        public void IconFollowsTheToolOfEachStep()
        {
            foreach (ChecklistState state in System.Enum.GetValues(typeof(ChecklistState)))
            {
                ChecklistIcon expected;
                switch (ChecklistLabels.ToolName(state))
                {
                    case "SCALPEL": expected = ChecklistIcon.Scalpel; break;
                    case "FORCEPS": expected = ChecklistIcon.Forceps; break;
                    case "": expected = ChecklistIcon.Check; break;
                    default: expected = ChecklistIcon.Hand; break;
                }
                Assert.That(ChecklistIconRule.For(state), Is.EqualTo(expected), state.ToString());
            }
        }

        [Test]
        public void HeadlinesStayOneShortActionLine()
        {
            foreach (ChecklistState state in System.Enum.GetValues(typeof(ChecklistState)))
            {
                Assert.That(ChecklistLabels.Headline(state).Length, Is.LessThanOrEqualTo(24), state.ToString());
                Assert.That(ChecklistLabels.Headline(state), Does.Not.Contain("\n"));
                Assert.That(ChecklistLabels.Headline(state), Does.Not.Contain("."));
            }
        }
    }
}
