using System.Collections.Generic;
using NUnit.Framework;

namespace DIYsect.Learning.Tests
{
    public class SoundCueVolumesTests
    {
        private class MemoryStore : ISoundCueStore
        {
            public readonly Dictionary<string, float> Values = new Dictionary<string, float>();
            public int Flushes;

            public bool TryGetFloat(string key, out float value) => Values.TryGetValue(key, out value);
            public void SetFloat(string key, float value) => Values[key] = value;
            public void Flush() => Flushes++;
        }

        [Test]
        public void DefaultsAreLow()
        {
            var volumes = new SoundCueVolumes();
            Assert.AreEqual(SoundCueVolumes.DefaultMaster, volumes.Master);
            Assert.AreEqual(SoundCueVolumes.DefaultCut, volumes.GetCue(SoundCueKind.Cut));
            Assert.AreEqual(SoundCueVolumes.DefaultGrab, volumes.GetCue(SoundCueKind.Grab));
            Assert.AreEqual(SoundCueVolumes.DefaultCorrect, volumes.GetCue(SoundCueKind.Correct));
            foreach (SoundCueKind kind in new[] { SoundCueKind.Cut, SoundCueKind.Grab, SoundCueKind.Correct })
                Assert.LessOrEqual(volumes.Effective(kind), 0.5f, kind.ToString());
        }

        [Test]
        public void ValuesAreClampedToZeroAndOne()
        {
            var volumes = new SoundCueVolumes();
            volumes.SetMaster(2f);
            Assert.AreEqual(1f, volumes.Master);
            volumes.SetMaster(-1f);
            Assert.AreEqual(0f, volumes.Master);
            volumes.SetMaster(float.NaN);
            Assert.AreEqual(0f, volumes.Master);
            volumes.SetCue(SoundCueKind.Grab, 5f);
            Assert.AreEqual(1f, volumes.GetCue(SoundCueKind.Grab));
            volumes.SetCue(SoundCueKind.Grab, -5f);
            Assert.AreEqual(0f, volumes.GetCue(SoundCueKind.Grab));
        }

        [Test]
        public void EffectiveIsMasterTimesCue()
        {
            var volumes = new SoundCueVolumes();
            volumes.SetMaster(0.5f);
            volumes.SetCue(SoundCueKind.Correct, 0.4f);
            Assert.AreEqual(0.2f, volumes.Effective(SoundCueKind.Correct), 1e-6f);
            volumes.SetMaster(0f);
            Assert.AreEqual(0f, volumes.Effective(SoundCueKind.Correct));
        }

        [Test]
        public void SaveThenLoadKeepsEveryValue()
        {
            var store = new MemoryStore();
            var volumes = new SoundCueVolumes();
            volumes.SetMaster(0.3f);
            volumes.SetCue(SoundCueKind.Cut, 0.1f);
            volumes.SetCue(SoundCueKind.Grab, 0.2f);
            volumes.SetCue(SoundCueKind.Correct, 0.9f);
            volumes.Save(store);
            Assert.AreEqual(1, store.Flushes);

            SoundCueVolumes loaded = SoundCueVolumes.Load(store);
            Assert.AreEqual(0.3f, loaded.Master, 1e-6f);
            Assert.AreEqual(0.1f, loaded.GetCue(SoundCueKind.Cut), 1e-6f);
            Assert.AreEqual(0.2f, loaded.GetCue(SoundCueKind.Grab), 1e-6f);
            Assert.AreEqual(0.9f, loaded.GetCue(SoundCueKind.Correct), 1e-6f);
        }

        [Test]
        public void LoadKeepsDefaultsForMissingOrBrokenValues()
        {
            var store = new MemoryStore();
            store.Values[SoundCueVolumes.KeyFor(SoundCueKind.Cut)] = float.NaN;
            store.Values[SoundCueVolumes.KeyFor(SoundCueKind.Grab)] = float.PositiveInfinity;
            SoundCueVolumes loaded = SoundCueVolumes.Load(store);
            Assert.AreEqual(SoundCueVolumes.DefaultMaster, loaded.Master);
            Assert.AreEqual(SoundCueVolumes.DefaultCut, loaded.GetCue(SoundCueKind.Cut));
            Assert.AreEqual(SoundCueVolumes.DefaultGrab, loaded.GetCue(SoundCueKind.Grab));
            Assert.AreEqual(SoundCueVolumes.DefaultCorrect, loaded.GetCue(SoundCueKind.Correct));
        }

        [Test]
        public void LoadClampsOutOfRangeSavedValues()
        {
            var store = new MemoryStore();
            store.Values[SoundCueVolumes.KeyFor(SoundCueKind.Correct)] = 7f;
            Assert.AreEqual(1f, SoundCueVolumes.Load(store).GetCue(SoundCueKind.Correct));
        }

        [Test]
        public void NullStoreIsSafe()
        {
            Assert.AreEqual(SoundCueVolumes.DefaultMaster, SoundCueVolumes.Load(null).Master);
            Assert.DoesNotThrow(() => new SoundCueVolumes().Save(null));
        }

        [Test]
        public void KeysAreDistinct()
        {
            var keys = new HashSet<string>
            {
                SoundCueVolumes.KeyFor(SoundCueKind.Cut),
                SoundCueVolumes.KeyFor(SoundCueKind.Grab),
                SoundCueVolumes.KeyFor(SoundCueKind.Correct),
            };
            Assert.AreEqual(3, keys.Count);
        }
    }
}
