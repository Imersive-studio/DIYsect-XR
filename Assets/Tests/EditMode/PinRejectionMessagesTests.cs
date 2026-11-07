using System;
using DIYsect.Classroom;
using NUnit.Framework;

namespace DIYsect.Classroom.Tests
{
    public class PinRejectionMessagesTests
    {
        [Test]
        public void EveryReasonHasItsOwnNonEmptyMessage()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (PinRejection reason in Enum.GetValues(typeof(PinRejection)))
            {
                string message = PinRejectionMessages.For(reason);
                Assert.IsFalse(string.IsNullOrWhiteSpace(message), reason + " has no message.");
                Assert.IsTrue(seen.Add(message), reason + " shares its message with another reason.");
            }
        }
    }
}
