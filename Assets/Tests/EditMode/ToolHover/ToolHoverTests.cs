using DIYsect.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.ToolHover.Tests
{
    /// <summary>Guards the tool hover tint: the on/off rule, and that the tint is set and cleared without copying materials.</summary>
    public class ToolHoverTests
    {
        private GameObject _tool;

        [TearDown]
        public void TearDown()
        {
            if (_tool != null)
                Object.DestroyImmediate(_tool);
        }

        [Test]
        public void NoHoversIsNotHighlighted()
        {
            Assert.IsFalse(ToolHoverRule.IsHighlighted(0, false));
        }

        [Test]
        public void OneHoverIsHighlighted()
        {
            Assert.IsTrue(ToolHoverRule.IsHighlighted(1, false));
        }

        [Test]
        public void TwoHoversStayHighlightedWhenOneLeaves()
        {
            Assert.IsTrue(ToolHoverRule.IsHighlighted(2, false));
            Assert.IsTrue(ToolHoverRule.IsHighlighted(1, false));
            Assert.IsFalse(ToolHoverRule.IsHighlighted(0, false));
        }

        [Test]
        public void SelectedIsNeverHighlighted()
        {
            Assert.IsFalse(ToolHoverRule.IsHighlighted(1, true));
            Assert.IsFalse(ToolHoverRule.IsHighlighted(2, true));
        }

        [Test]
        public void HighlightSetsTheBlockColourAndRestoreClearsIt()
        {
            _tool = new GameObject("Tool");
            _tool.AddComponent<Rigidbody>();
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.transform.SetParent(_tool.transform, false);
            _tool.AddComponent<XRGrabInteractable>();
            var feedback = _tool.AddComponent<ToolHoverFeedback>();
            var renderer = visual.GetComponent<Renderer>();
            var block = new MaterialPropertyBlock();

            feedback.Highlight();
            renderer.GetPropertyBlock(block);
            Assert.IsFalse(block.isEmpty);
            Color tint = block.GetColor("_BaseColor");
            Assert.Greater(tint.r, 0.9f);
            Assert.AreEqual(tint, block.GetColor("_Color"));

            feedback.Restore();
            renderer.GetPropertyBlock(block);
            Assert.IsTrue(block.isEmpty);
        }
    }
}
