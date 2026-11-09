using DIYsect.Networking;
using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    /// <summary>Builds a card on a stand-in organ. The per-frame placement needs a running scene, so it is not tested here.</summary>
    public class OrganInfoCardTests
    {
        private class FakeSnappable : ISnappable
        {
            public bool IsValid => true;
            public Pose HomeRootPose => new Pose(Vector3.zero, Quaternion.identity);
            public Pose HomeCentrePose => new Pose(Vector3.zero, Quaternion.identity);
            public Pose CurrentCentrePose => new Pose(Vector3.zero, Quaternion.identity);
            public void SnapHome() { }
        }

        private GameObject _organ;
        private OrganInfoCard _card;

        [SetUp]
        public void BuildCard()
        {
            _organ = new GameObject("liver");
            var snappable = new FakeSnappable();
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _card = OrganInfoCard.Create(_organ.transform, snappable, "<b>Liver</b>\nMakes bile.", 0.25f, font);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_card != null)
                Object.DestroyImmediate(_card.gameObject);
            Object.DestroyImmediate(_organ);
        }

        [Test]
        public void ACardStartsHiddenAndHoldsItsText()
        {
            Assert.IsFalse(_card.IsShown);
            Assert.AreEqual("<b>Liver</b>\nMakes bile.", _card.Content);
        }

        [Test]
        public void ShowAndHideSwitchTheCard()
        {
            _card.Show();
            Assert.IsTrue(_card.IsShown);

            _card.Hide();
            Assert.IsFalse(_card.IsShown);
        }

        [Test]
        public void TheCardIsNotAChildOfTheOrgan()
        {
            Assert.IsNull(_card.transform.parent);
        }

        [Test]
        public void TheCardIsAWorldSpaceCanvas()
        {
            Assert.AreEqual(RenderMode.WorldSpace, _card.GetComponent<Canvas>().renderMode);
        }
    }
}
