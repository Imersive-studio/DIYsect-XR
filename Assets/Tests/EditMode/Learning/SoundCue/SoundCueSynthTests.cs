using System;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class SoundCueSynthTests
    {
        private const int Rate = 24000;

        private static readonly SoundCueKind[] Kinds = { SoundCueKind.Cut, SoundCueKind.Grab, SoundCueKind.Correct };

        [Test]
        public void LengthMatchesTheDuration()
        {
            foreach (SoundCueKind kind in Kinds)
            {
                float[] samples = SoundCueSynth.Render(kind, Rate);
                Assert.AreEqual(Math.Round(SoundCueSynth.DurationSeconds(kind) * Rate), samples.Length, 1, kind.ToString());
                Assert.Greater(samples.Length, 0, kind.ToString());
            }
        }

        [Test]
        public void CuesAreShortAndSoft()
        {
            Assert.Less(SoundCueSynth.DurationSeconds(SoundCueKind.Cut), 0.15f, "A tick must fit inside the cut interval.");
            foreach (SoundCueKind kind in Kinds)
                Assert.Less(SoundCueSynth.DurationSeconds(kind), 1f, kind.ToString());
        }

        [Test]
        public void PeakIsBelowOneAndNotSilent()
        {
            foreach (SoundCueKind kind in Kinds)
            {
                float peak = 0f;
                foreach (float sample in SoundCueSynth.Render(kind, Rate))
                    peak = Math.Max(peak, Math.Abs(sample));
                Assert.Less(peak, 1f, kind.ToString());
                Assert.AreEqual(SoundCueSynth.PeakLevel, peak, 1e-4f, kind.ToString());
            }
        }

        [Test]
        public void StartsAndEndsNearZeroSoNothingClicks()
        {
            foreach (SoundCueKind kind in Kinds)
            {
                float[] samples = SoundCueSynth.Render(kind, Rate);
                Assert.AreEqual(0f, samples[0], 0.01f, kind + " start");
                Assert.AreEqual(0f, samples[samples.Length - 1], 0.01f, kind + " end");
            }
        }

        [Test]
        public void NoSampleJumpsLikeAClick()
        {
            foreach (SoundCueKind kind in Kinds)
            {
                float[] samples = SoundCueSynth.Render(kind, Rate);
                for (int i = 1; i < samples.Length; i++)
                    Assert.Less(Math.Abs(samples[i] - samples[i - 1]), 0.3f, $"{kind} at {i}");
            }
        }

        [Test]
        public void SameInputGivesSameSamples()
        {
            foreach (SoundCueKind kind in Kinds)
                CollectionAssert.AreEqual(SoundCueSynth.Render(kind, Rate), SoundCueSynth.Render(kind, Rate), kind.ToString());
        }

        [Test]
        public void EverySampleIsFinite()
        {
            foreach (SoundCueKind kind in Kinds)
            {
                foreach (float sample in SoundCueSynth.Render(kind, 48000))
                    Assert.IsFalse(float.IsNaN(sample) || float.IsInfinity(sample));
            }
        }

        [Test]
        public void BadArgumentsAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundCueSynth.Render(SoundCueKind.Cut, 100));
            Assert.Throws<ArgumentOutOfRangeException>(() => SoundCueSynth.Render((SoundCueKind)99, Rate));
        }
    }
}
