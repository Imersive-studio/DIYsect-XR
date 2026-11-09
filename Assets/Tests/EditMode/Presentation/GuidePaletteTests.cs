using NUnit.Framework;
using UnityEngine;

namespace DIYsect.Presentation.Tests
{
    public class GuidePaletteTests
    {
        private static void AssertColor(Color expected, Color actual)
        {
            Assert.AreEqual(expected.r, actual.r, 1e-5f);
            Assert.AreEqual(expected.g, actual.g, 1e-5f);
            Assert.AreEqual(expected.b, actual.b, 1e-5f);
            Assert.AreEqual(expected.a, actual.a, 1e-5f);
        }

        [Test]
        public void KeyColoursMatchTheDesign()
        {
            AssertColor(new Color(0x5E / 255f, 0xF2 / 255f, 0xC2 / 255f, 1f), GuidePalette.Mint);
            AssertColor(new Color(0xFF / 255f, 0x7A / 255f, 0x59 / 255f, 1f), GuidePalette.Coral);
            AssertColor(new Color(0x0B / 255f, 0x1F / 255f, 0x2A / 255f, 0.94f), GuidePalette.Ink);
            Assert.AreEqual(0.10f, GuidePalette.MintGlow.a, 1e-5f);
            Assert.AreEqual(0.35f, GuidePalette.MintLine.a, 1e-5f);
            Assert.AreEqual(0.16f, GuidePalette.CoralSoft.a, 1e-5f);
            Assert.AreEqual(0.75f, GuidePalette.TextDone.a, 1e-5f);
        }

        [Test]
        public void PulseStartsAtAReachesBAtHalfPeriodAndReturns()
        {
            AssertColor(GuidePalette.Coral, GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, 0f, 1.6f));
            AssertColor(GuidePalette.CoralSoft, GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, 0.8f, 1.6f));
            AssertColor(GuidePalette.Coral, GuidePalette.Pulse(GuidePalette.Coral, GuidePalette.CoralSoft, 1.6f, 1.6f));
        }

        [Test]
        public void PulseIsSmoothAtTheQuarterPoint()
        {
            Color middle = GuidePalette.Pulse(Color.black, Color.white, 0.4f, 1.6f);
            Assert.AreEqual(0.5f, middle.r, 1e-4f);
        }

        [Test]
        public void PulseWithNoPeriodStaysOnTheFirstColour()
        {
            AssertColor(GuidePalette.Mint, GuidePalette.Pulse(GuidePalette.Mint, GuidePalette.Coral, 3f, 0f));
        }
    }
}
