using System.Linq;
using NUnit.Framework;

namespace DIYsect.Audience.Tests
{
    public sealed class AudienceRulesTests
    {
        [Test]
        public void SameSeedChoosesTheSameDistinctSeats()
        {
            int[] seats = AudienceRules.ChooseSeats(69, 25, 159);
            CollectionAssert.AreEqual(seats, AudienceRules.ChooseSeats(69, 25, 159));
            Assert.That(seats.Distinct().Count(), Is.EqualTo(25));
            Assert.That(seats, Is.All.InRange(0, 68));
            Assert.That(seats.SequenceEqual(AudienceRules.ChooseSeats(69, 25, 160)), Is.False);
        }

        [Test]
        public void ShiftsAreSlowAndVaryBetweenFigures()
        {
            float[] intervals = Enumerable.Range(0, 25).Select(AudienceRules.ShiftInterval).ToArray();
            Assert.That(intervals, Is.All.InRange(9f, 21f));
            Assert.That(intervals.Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void AMinorityFollowsTheStudentAndOnlyAwayFromTheTable()
        {
            Assert.That(Enumerable.Range(0, 25).Count(index => AudienceRules.FollowsStudent(index, 4f)), Is.EqualTo(5));
            Assert.That(Enumerable.Range(0, 25).Any(index => AudienceRules.FollowsStudent(index, 2.25f)), Is.False);
        }
    }
}
