using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Interaction.Tests
{
    public class LabelMathTests
    {
        [Test]
        public void StaggerStartsAfterItsDelayAndEndsTogether()
        {
            Assert.AreEqual(0f, StaggeredReveal.Progress(0.1f, 0.2f, 0.04f, true), 1e-5f, "0.02 s in, the delayed line has not started.");
            Assert.AreEqual(1f, StaggeredReveal.Progress(1f, 0.2f, 0.04f, true), 1e-5f);
            Assert.Greater(StaggeredReveal.Progress(0.6f, 0.2f, 0f, true), StaggeredReveal.Progress(0.6f, 0.2f, 0.04f, true));
        }

        [Test]
        public void LinesLeaveTogether()
        {
            Assert.AreEqual(0.5f, StaggeredReveal.Progress(0.5f, 0.2f, 0.04f, false), 1e-5f);
        }

        [Test]
        public void TrimKeepsTheStartAndCutsByLength()
        {
            var path = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(1f, 1f, 0f) };
            var output = new Vector3[3];

            Assert.AreEqual(0, LeaderPath.Trim(path, 0f, output));
            Assert.AreEqual(2, LeaderPath.Trim(path, 0.25f, output));
            Assert.AreEqual(new Vector3(0.5f, 0f, 0f), output[1]);
            Assert.AreEqual(3, LeaderPath.Trim(path, 0.75f, output));
            Assert.AreEqual(new Vector3(1f, 0.5f, 0f), output[2]);
            Assert.AreEqual(3, LeaderPath.Trim(path, 1f, output));
            Assert.AreEqual(new Vector3(1f, 1f, 0f), output[2]);
        }

        [Test]
        public void TheTitleSitsAboveTheOrganAndBeyondItFromTheViewer()
        {
            var bounds = new Bounds(new Vector3(0f, 1f, 2f), new Vector3(0.2f, 0.1f, 0.2f));
            Vector3 viewer = new Vector3(0f, 1.6f, 0f);

            Vector3 position = TitlePlacement.Position(bounds, viewer, 0.11f);

            Assert.AreEqual(bounds.max.y + 0.11f, position.y, 1e-5f);
            Assert.AreEqual(0f, position.x, 1e-5f);
            Assert.Greater(position.z, bounds.center.z, "Pushed to the far side, away from the viewer.");
        }

        [Test]
        public void CaptionsFallBackToTheDisplayNameWithNoSecondLine()
        {
            OrganCaption caption = OrganCaptions.Lookup(null, "kidney_l");

            Assert.AreEqual("Left kidney", caption.Name);
            Assert.AreEqual(string.Empty, caption.System);
            Assert.AreEqual(string.Empty, caption.Role);
        }
    }
}
