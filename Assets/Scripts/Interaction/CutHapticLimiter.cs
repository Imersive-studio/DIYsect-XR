namespace DIYsect.Interaction
{
    /// <summary>
    /// Decides when a cutting tick may be felt: only when the cut advanced since the last report, and at most once
    /// per <c>minIntervalSeconds</c>. Plain C# with no Unity types, so it is easy to test. The caller passes the clock.
    /// </summary>
    public class CutHapticLimiter
    {
        // Float clocks round (5.1 - 5.0 is 0.0999998), so a tick exactly one interval later is allowed.
        private const float IntervalTolerance = 1e-4f;

        private readonly float _minIntervalSeconds;
        private float _lastProgress;
        private float _lastPulseTime = float.NegativeInfinity;

        public CutHapticLimiter(float minIntervalSeconds)
        {
            _minIntervalSeconds = minIntervalSeconds;
        }

        /// <summary>Starts a new cut from <paramref name="progress"/>. Call it when the blade enters the incision zone.</summary>
        public void Begin(float progress)
        {
            _lastProgress = progress;
            _lastPulseTime = float.NegativeInfinity;
        }

        /// <summary>True if a tick should play now. <paramref name="progress"/> is the cut's progress and <paramref name="now"/> the time in seconds.</summary>
        public bool ShouldPulse(float progress, float now)
        {
            bool advanced = progress > _lastProgress;
            _lastProgress = progress;
            if (!advanced || now - _lastPulseTime < _minIntervalSeconds - IntervalTolerance)
                return false;
            _lastPulseTime = now;
            return true;
        }
    }
}
