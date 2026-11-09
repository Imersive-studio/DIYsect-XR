using System;

namespace DIYsect.Learning
{
    /// <summary>
    /// The volume setting: one master volume plus a level for each cue, all between 0 and 1. The sound that plays
    /// is master times the cue level. Plain C# with no Unity types. Values outside 0..1 are clamped and a NaN counts as 0.
    /// </summary>
    public sealed class SoundCueVolumes
    {
        public const float DefaultMaster = 0.5f;
        public const float DefaultCut = 0.5f;
        public const float DefaultGrab = 0.7f;
        public const float DefaultCorrect = 0.8f;

        private const string KeyPrefix = "DIYsect.SoundCue.";
        private const string MasterKey = KeyPrefix + "Master";

        private readonly float[] _cueLevels = { DefaultCut, DefaultGrab, DefaultCorrect };

        public float Master { get; private set; } = DefaultMaster;

        /// <summary>Clamps to 0..1. NaN becomes 0.</summary>
        public static float Clamp01(float value)
        {
            if (float.IsNaN(value) || value < 0f)
                return 0f;
            return value > 1f ? 1f : value;
        }

        public void SetMaster(float value)
        {
            Master = Clamp01(value);
        }

        public float GetCue(SoundCueKind kind)
        {
            return _cueLevels[IndexOf(kind)];
        }

        public void SetCue(SoundCueKind kind, float value)
        {
            _cueLevels[IndexOf(kind)] = Clamp01(value);
        }

        /// <summary>The volume to play a cue at: master times the cue level.</summary>
        public float Effective(SoundCueKind kind)
        {
            return Master * GetCue(kind);
        }

        /// <summary>Reads saved values. Anything missing or not a number keeps its default.</summary>
        public static SoundCueVolumes Load(ISoundCueStore store)
        {
            var volumes = new SoundCueVolumes();
            if (store == null)
                return volumes;
            if (TryRead(store, MasterKey, out float master))
                volumes.SetMaster(master);
            foreach (SoundCueKind kind in Enum.GetValues(typeof(SoundCueKind)))
            {
                if (TryRead(store, KeyFor(kind), out float level))
                    volumes.SetCue(kind, level);
            }
            return volumes;
        }

        /// <summary>Remembers every value and writes it out.</summary>
        public void Save(ISoundCueStore store)
        {
            if (store == null)
                return;
            store.SetFloat(MasterKey, Master);
            foreach (SoundCueKind kind in Enum.GetValues(typeof(SoundCueKind)))
                store.SetFloat(KeyFor(kind), GetCue(kind));
            store.Flush();
        }

        public static string KeyFor(SoundCueKind kind)
        {
            return KeyPrefix + kind;
        }

        private static bool TryRead(ISoundCueStore store, string key, out float value)
        {
            return store.TryGetFloat(key, out value) && !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private static int IndexOf(SoundCueKind kind)
        {
            int index = (int)kind;
            if (index < 0 || index >= 3)
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown sound cue.");
            return index;
        }
    }
}
