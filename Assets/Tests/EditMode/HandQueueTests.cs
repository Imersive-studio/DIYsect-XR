using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;

namespace DIYsect.Classroom.Tests
{
    public class HandQueueTests
    {
        [Test]
        public void HandsAreOrderedByWhoRaisedFirst()
        {
            var queue = new HandQueue(10);
            queue.Raise(7);
            queue.Raise(3);
            queue.Raise(9);

            Assert.AreEqual(3, queue.Count);
            Assert.AreEqual(1, queue.OrderOf(7));
            Assert.AreEqual(2, queue.OrderOf(3));
            Assert.AreEqual(3, queue.OrderOf(9));
            Assert.AreEqual(0, queue.OrderOf(42), "No hand up reads as 0.");
            Assert.AreEqual(7UL, queue[0]);
        }

        [Test]
        public void RaisingTwiceKeepsTheFirstPlaceAndAddsNothing()
        {
            var queue = new HandQueue(10);
            queue.Raise(1);
            queue.Raise(2);

            Assert.IsFalse(queue.Raise(1));
            Assert.AreEqual(2, queue.Count);
            Assert.AreEqual(1, queue.OrderOf(1));
        }

        [Test]
        public void LoweringAHandMovesEveryoneBehindItUp()
        {
            var queue = new HandQueue(10);
            queue.Raise(1);
            queue.Raise(2);
            queue.Raise(3);

            Assert.IsTrue(queue.Lower(1));
            Assert.AreEqual(1, queue.OrderOf(2));
            Assert.AreEqual(2, queue.OrderOf(3));
            Assert.IsFalse(queue.Lower(1), "Lowering a hand that is down changes nothing.");
        }

        [Test]
        public void ARaisedThenLoweredThenRaisedHandGoesToTheBack()
        {
            var queue = new HandQueue(10);
            queue.Raise(1);
            queue.Raise(2);
            queue.Lower(1);
            queue.Raise(1);

            Assert.AreEqual(2, queue.OrderOf(1));
            Assert.AreEqual(1, queue.OrderOf(2));
        }

        [Test]
        public void AFullQueueRefusesAnotherHand()
        {
            var queue = new HandQueue(2);
            Assert.IsTrue(queue.Raise(1));
            Assert.IsTrue(queue.Raise(2));
            Assert.IsFalse(queue.Raise(3));
            Assert.AreEqual(2, queue.Count);
        }

        [Test]
        public void ClearTakesEveryHandDown()
        {
            var queue = new HandQueue(10);
            queue.Raise(1);
            queue.Raise(2);
            queue.Clear();
            Assert.AreEqual(0, queue.Count);
            Assert.IsFalse(queue.Contains(1));
        }

        [Test]
        public void OnlyTheOwnerOrTheHostCanLowerAHand()
        {
            ulong host = NetworkManager.ServerClientId;
            Assert.IsTrue(HandRules.CanLower(5, 5), "A student lowers their own hand.");
            Assert.IsFalse(HandRules.CanLower(5, 6), "A student cannot lower another student's hand.");
            Assert.IsTrue(HandRules.CanLower(host, 6), "The professor lowers any hand.");
        }

        [Test]
        public void OnlyTheHostCanLowerEveryHand()
        {
            Assert.IsTrue(HandRules.CanLowerAll(NetworkManager.ServerClientId));
            Assert.IsFalse(HandRules.CanLowerAll(5));
        }
    }
}
