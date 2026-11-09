using System;

namespace DIYsect.Learning
{
    /// <summary>
    /// How opaque the name card is over time: fully visible for a few seconds after it is shown, then fading out. Showing it
    /// again restarts the clock. Time is passed in, so it is tested without a clock.
    /// </summary>
    public sealed class NameCardFade
    {
        private readonly float _holdSeconds;
        private readonly float _fadeSeconds;
        private float _shownAt = float.NegativeInfinity;

        public NameCardFade(float holdSeconds = 2.5f, float fadeSeconds = 1f)
        {
            if (holdSeconds < 0f) throw new ArgumentOutOfRangeException(nameof(holdSeconds));
            if (fadeSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(fadeSeconds));
            _holdSeconds = holdSeconds;
            _fadeSeconds = fadeSeconds;
        }

        /// <summary>Starts (or restarts) the card at this time.</summary>
        public void Show(float now)
        {
            _shownAt = now;
        }

        /// <summary>1 while held, falling to 0 over the fade, 0 after, and 0 before it was ever shown.</summary>
        public float Alpha(float now)
        {
            float age = now - _shownAt;
            if (age < 0f)
                return 0f;
            if (age <= _holdSeconds)
                return 1f;
            float faded = (age - _holdSeconds) / _fadeSeconds;
            return faded >= 1f ? 0f : 1f - faded;
        }

        public bool Visible(float now) => Alpha(now) > 0f;
    }
}
