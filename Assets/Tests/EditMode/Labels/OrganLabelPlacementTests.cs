using DIYsect.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Interaction.Tests
{
    /// <summary>The label sits on the organ's current top in world space, so turning the organ over moves it with the top.</summary>
    public class OrganLabelPlacementTests
    {
        private const string FontPath = "Assets/MRTabletopAssets/Fonts/Inter-Regular SDF.asset";

        private GameObject _organ;
        private OrganLabel _label;

        [SetUp]
        public void Build()
        {
            _organ = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _organ.name = "gallbladder";
            _organ.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            Assert.IsNotNull(font, $"{FontPath} is missing.");
            _label = OrganLabel.Create(_organ.transform, new OrganCaption("Gallbladder", "Digestive system", "Stores bile"), font, LabelAnchor.Gap);
        }

        [TearDown]
        public void Cleanup()
        {
            if (_label != null)
                Object.DestroyImmediate(_label.gameObject);
            Object.DestroyImmediate(_organ);
        }

        [Test]
        public void TheLabelHoldsTheNameAndTheSystemAndIsNotAChildOfTheOrgan()
        {
            Assert.AreEqual("Gallbladder", _label.Text);
            Assert.AreEqual("Digestive system", _label.Descriptor);
            Assert.IsNull(_label.transform.parent);
            Assert.IsFalse(_label.IsShown);
        }

        [Test]
        public void TheLabelSitsOnTheTopAndFollowsItAfterTheOrganIsTurned()
        {
            _label.Show();
            Assert.IsTrue(_label.IsShown);
            Assert.AreEqual(0.15f + LabelAnchor.Gap, _label.transform.position.y, 1e-4f);

            _organ.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            _label.Show();

            Assert.AreEqual(0.05f + LabelAnchor.Gap, _label.transform.position.y, 1e-4f, "Lying on its side, the top is lower.");
            Assert.AreEqual(0f, _label.transform.position.x, 1e-4f);
        }

        [Test]
        public void HidingStartsTheDissolveAndStopsBeingShown()
        {
            _label.Show();
            _label.Hide();

            Assert.IsFalse(_label.IsShown);
            Assert.IsTrue(_label.IsVisible, "It dissolves over a moment before it is gone.");
        }
    }
}
