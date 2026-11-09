using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Presentation.Tests
{
    public class LabelAnchorTests
    {
        [Test]
        public void AboveSitsJustClearOfTheTopWhateverTheSize()
        {
            var small = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(0.04f, 0.04f, 0.04f));
            var large = new Bounds(new Vector3(0f, 1f, 0f), new Vector3(0.25f, 0.16f, 0.2f));

            Assert.AreEqual(1.02f + LabelAnchor.Gap, LabelAnchor.Above(small).y, 1e-5f);
            Assert.AreEqual(1.08f + LabelAnchor.Gap, LabelAnchor.Above(large).y, 1e-5f);
            Assert.AreEqual(0f, LabelAnchor.Above(large).x, 1e-5f);
        }

        [Test]
        public void BesideIsOutsideTheBoundsOnTheViewersRight()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(0.2f, 0.1f, 0.4f));
            Vector3 point = LabelAnchor.Beside(bounds, Vector3.right, 0.01f);
            Assert.AreEqual(0.11f, point.x, 1e-5f);
            Assert.IsFalse(bounds.Contains(point));

            Vector3 tilted = LabelAnchor.Beside(bounds, new Vector3(0f, 0.5f, 1f), 0.01f);
            Assert.AreEqual(0.21f, tilted.z, 1e-5f, "Height in the viewer's right is ignored.");
        }

        [Test]
        public void RendererBoundsCoversChildrenAndSkipsTheLabel()
        {
            var root = new GameObject("Organ");
            var a = GameObject.CreatePrimitive(PrimitiveType.Cube);
            a.transform.SetParent(root.transform, false);
            var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
            label.transform.SetParent(root.transform, false);
            label.transform.localPosition = Vector3.up * 5f;
            try
            {
                Bounds bounds = LabelAnchor.RendererBounds(root.transform, label.transform);
                Assert.AreEqual(0.5f, bounds.max.y, 1e-4f);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
