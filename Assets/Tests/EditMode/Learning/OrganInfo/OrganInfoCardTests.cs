using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Learning.Tests
{
    /// <summary>Builds a card on a stand-in organ. The per-frame placement needs a running scene, so only the build is tested here.</summary>
    public class OrganInfoCardTests
    {
        private const string FontPath = "Assets/MRTabletopAssets/Fonts/Inter-Regular SDF.asset";

        private GameObject _organ;
        private OrganInfoCard _card;

        [SetUp]
        public void BuildCard()
        {
            _organ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _organ.name = "liver";
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assert.IsNotNull(font, $"{FontPath} is missing.");
            var entry = new OrganInfoEntry("liver", "Liver", "Makes bile.", "It can regrow.", "Digestive system", "Produces bile");
            _card = OrganInfoCard.Create(_organ.transform, entry, font, null, 0.11f);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_card != null)
                Object.DestroyImmediate(_card.gameObject);
            Object.DestroyImmediate(_organ);
        }

        [Test]
        public void ACardStartsHiddenAndHoldsItsWords()
        {
            Assert.IsFalse(_card.IsShown);
            Assert.AreEqual("LIVER", _card.Title);
            Assert.AreEqual("Makes bile.", _card.Function);
            Assert.AreEqual("It can regrow.", _card.Fact);
        }

        [Test]
        public void ShowAndHideSwitchTheCard()
        {
            _card.Show();
            Assert.IsTrue(_card.IsShown);

            _card.Hide();
            Assert.IsFalse(_card.IsShown);
            Assert.IsTrue(_card.IsVisible, "It fades out over a moment before it is gone.");
        }

        [Test]
        public void TheCardIsNotAChildOfTheOrgan()
        {
            Assert.IsNull(_card.transform.parent);
        }
    }
}
