using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Networking.Tests
{
    public class SocketRuleTests
    {
        private const float PositionTolerance = 0.05f;
        private const float AngleTolerance = 30f;

        private SocketRule _rule;
        private List<Pose> _sockets;

        [SetUp]
        public void SetUp()
        {
            _rule = new SocketRule(PositionTolerance, AngleTolerance);
            _sockets = new List<Pose>
            {
                new Pose(new Vector3(0f, 0f, 0f), Quaternion.identity),
                new Pose(new Vector3(0.1f, 0f, 0f), Quaternion.identity),
                new Pose(new Vector3(0f, 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f)),
            };
        }

        [Test]
        public void ExactlyAtItsOwnSocketIsAccepted()
        {
            SnapDecision decision = _rule.Evaluate(0, _sockets[0], _sockets);
            Assert.AreEqual(SnapOutcome.Accepted, decision.Outcome);
            Assert.AreEqual(0, decision.SocketIndex);
        }

        [Test]
        public void WithinBothTolerancesIsAccepted()
        {
            var organ = new Pose(new Vector3(0.03f, 0f, 0f), Quaternion.Euler(0f, 20f, 0f));
            Assert.AreEqual(SnapOutcome.Accepted, _rule.Evaluate(0, organ, _sockets).Outcome);
        }

        [Test]
        public void ToleranceEdgeIsInclusive()
        {
            var organ = new Pose(new Vector3(PositionTolerance, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(SnapOutcome.Accepted, _rule.Evaluate(0, organ, _sockets).Outcome);
        }

        [Test]
        public void AtAnotherOrgansSocketIsRejectedWithThatSocket()
        {
            SnapDecision decision = _rule.Evaluate(0, _sockets[1], _sockets);
            Assert.AreEqual(SnapOutcome.Rejected, decision.Outcome);
            Assert.AreEqual(1, decision.SocketIndex);
        }

        [Test]
        public void AtAnotherSocketIsRejectedWhateverTheRotation()
        {
            var organ = new Pose(_sockets[2].position, Quaternion.Euler(0f, 180f, 0f));
            SnapDecision decision = _rule.Evaluate(0, organ, _sockets);
            Assert.AreEqual(SnapOutcome.Rejected, decision.Outcome);
            Assert.AreEqual(2, decision.SocketIndex);
        }

        [Test]
        public void FarFromEverySocketDoesNothing()
        {
            var organ = new Pose(new Vector3(5f, 5f, 5f), Quaternion.identity);
            SnapDecision decision = _rule.Evaluate(0, organ, _sockets);
            Assert.AreEqual(SnapOutcome.None, decision.Outcome);
            Assert.AreEqual(-1, decision.SocketIndex);
        }

        [Test]
        public void NearItsOwnSocketButTurnedTooFarDoesNothing()
        {
            var organ = new Pose(_sockets[0].position, Quaternion.Euler(0f, 90f, 0f));
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, organ, _sockets).Outcome);
        }

        [Test]
        public void OwnSocketWinsWhereTwoSocketsOverlap()
        {
            // Socket 1 is 0.1 m away. With a wide tolerance both sockets contain the point.
            var wide = new SocketRule(0.2f, AngleTolerance);
            var organ = new Pose(new Vector3(0.04f, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(SnapOutcome.Accepted, wide.Evaluate(0, organ, _sockets).Outcome);
            Assert.AreEqual(SnapOutcome.Accepted, wide.Evaluate(1, organ, _sockets).Outcome);
        }

        [Test]
        public void NearestOtherSocketIsReported()
        {
            var wide = new SocketRule(0.6f, AngleTolerance);
            var organ = new Pose(new Vector3(0.09f, 0f, 0f), Quaternion.Euler(0f, 180f, 0f));
            // Organ 2 is turned away from its own socket and sits nearest to socket 1.
            SnapDecision decision = wide.Evaluate(2, organ, _sockets);
            Assert.AreEqual(SnapOutcome.Rejected, decision.Outcome);
            Assert.AreEqual(1, decision.SocketIndex);
        }

        [Test]
        public void BadInputDoesNothing()
        {
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(-1, _sockets[0], _sockets).Outcome);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(3, _sockets[0], _sockets).Outcome);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, _sockets[0], null).Outcome);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, _sockets[0], new List<Pose>()).Outcome);
            var nan = new Pose(new Vector3(float.NaN, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, nan, _sockets).Outcome);
            var infinite = new Pose(new Vector3(float.PositiveInfinity, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, infinite, _sockets).Outcome);
            var zeroRotation = new Pose(Vector3.zero, new Quaternion(0f, 0f, 0f, 0f));
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, zeroRotation, _sockets).Outcome);
        }

        [Test]
        public void ABrokenSocketIsIgnored()
        {
            _sockets[1] = new Pose(new Vector3(float.NaN, 0f, 0f), Quaternion.identity);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(0, new Pose(new Vector3(0.1f, 0f, 0f), Quaternion.identity), _sockets).Outcome);
            Assert.AreEqual(SnapOutcome.None, _rule.Evaluate(1, _sockets[0], _sockets).Outcome, "The organ's own socket is broken.");
        }

        [Test]
        public void ZeroToleranceNeedsAnExactMatch()
        {
            var exact = new SocketRule(0f, 0f);
            Assert.AreEqual(SnapOutcome.Accepted, exact.Evaluate(0, _sockets[0], _sockets).Outcome);
            Assert.AreEqual(SnapOutcome.None, exact.Evaluate(0, new Pose(new Vector3(0.001f, 0f, 0f), Quaternion.identity), _sockets).Outcome);
        }

        [Test]
        public void InvalidTolerancesAreRefused()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SocketRule(-0.1f, 10f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SocketRule(float.NaN, 10f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SocketRule(0.1f, -1f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SocketRule(0.1f, float.PositiveInfinity));
        }
    }
}
