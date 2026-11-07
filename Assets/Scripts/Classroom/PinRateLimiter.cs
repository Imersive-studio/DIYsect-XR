// Authority: server only. The server asks it before accepting a pin; clients never use it.
using System.Collections.Generic;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Sliding window limit per sender: at most <c>maxPerWindow</c> accepted requests in any
    /// <c>windowSeconds</c>. Time is passed in so it can be tested without a clock.
    /// </summary>
    public class PinRateLimiter
    {
        private readonly int _maxPerWindow;
        private readonly float _windowSeconds;
        private readonly Dictionary<ulong, Queue<float>> _accepted = new Dictionary<ulong, Queue<float>>();

        public PinRateLimiter(int maxPerWindow, float windowSeconds)
        {
            _maxPerWindow = maxPerWindow;
            _windowSeconds = windowSeconds;
        }

        /// <summary>Records the request and returns true if the sender is under the limit.</summary>
        public bool TryAccept(ulong sender, float now)
        {
            if (!_accepted.TryGetValue(sender, out Queue<float> times))
            {
                times = new Queue<float>();
                _accepted[sender] = times;
            }
            while (times.Count > 0 && now - times.Peek() >= _windowSeconds)
                times.Dequeue();
            if (times.Count >= _maxPerWindow)
                return false;
            times.Enqueue(now);
            return true;
        }

        public void Forget(ulong sender)
        {
            _accepted.Remove(sender);
        }
    }
}
