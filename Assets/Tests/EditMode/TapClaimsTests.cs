using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Classroom.Tests
{
    public class TapClaimsTests
    {
        private class Claimer : MonoBehaviour, ITapClaim
        {
            public bool Claims;
            public bool ClaimsTap => Claims;
        }

        private class NotAClaimer : MonoBehaviour { }

        private GameObject _holder;

        [SetUp]
        public void CreateHolder() => _holder = new GameObject("Holder");

        [TearDown]
        public void DestroyHolder() => Object.DestroyImmediate(_holder);

        [Test]
        public void NothingClaimsWhenThereAreNoClaimers()
        {
            Assert.IsFalse(TapClaims.AnyClaims(null));
            Assert.IsFalse(TapClaims.AnyClaims(new List<ITapClaim>()));
        }

        [Test]
        public void AnyOneClaimerClaimsTheTap()
        {
            var idle = _holder.AddComponent<Claimer>();
            var busy = _holder.AddComponent<Claimer>();
            var list = new List<ITapClaim> { idle, busy };
            Assert.IsFalse(TapClaims.AnyClaims(list));
            busy.Claims = true;
            Assert.IsTrue(TapClaims.AnyClaims(list));
        }

        [Test]
        public void ResolveKeepsClaimersAndIgnoresTheRestWithAnError()
        {
            var good = _holder.AddComponent<Claimer>();
            var wrong = _holder.AddComponent<NotAClaimer>();
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("NotAClaimer.*does not implement ITapClaim"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("empty"));

            List<ITapClaim> claimers = TapClaims.Resolve(new MonoBehaviour[] { good, wrong, null }, _holder);

            Assert.AreEqual(1, claimers.Count);
            Assert.AreSame(good, claimers[0]);
        }

        [Test]
        public void ResolveOfNothingIsEmpty()
        {
            Assert.AreEqual(0, TapClaims.Resolve(null, _holder).Count);
        }
    }
}
