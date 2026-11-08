using System;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>One short controller vibration: how strong (0 to 1) and how long (seconds). Shown as two fields in the Inspector.</summary>
    [Serializable]
    public struct HapticPulse
    {
        [SerializeField, Range(0f, 1f)] private float _amplitude;
        [SerializeField, Min(0f)] private float _durationSeconds;

        public HapticPulse(float amplitude, float durationSeconds)
        {
            _amplitude = amplitude;
            _durationSeconds = durationSeconds;
        }

        public float Amplitude => _amplitude;
        public float DurationSeconds => _durationSeconds;
    }
}
