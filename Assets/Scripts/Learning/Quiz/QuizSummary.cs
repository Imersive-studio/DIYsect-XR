using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>Pure helpers that turn the published per-organ counts into what the professor needs to see.</summary>
    public static class QuizSummary
    {
        /// <summary>
        /// The organs that got taps but are not the target, most taps first (ties keep organ order), at most
        /// <paramref name="max"/> of them. <paramref name="tapsFor"/> returns the taps for an organ index.
        /// </summary>
        public static List<int> TopWrong(int organCount, Func<int, int> tapsFor, int target, int max)
        {
            if (tapsFor == null)
                throw new ArgumentNullException(nameof(tapsFor));
            var picks = new List<int>();
            for (int organ = 0; organ < organCount; organ++)
                if (organ != target && tapsFor(organ) > 0)
                    picks.Add(organ);
            // A stable order: more taps first, then the lower organ index.
            picks.Sort((a, b) =>
            {
                int byTaps = tapsFor(b).CompareTo(tapsFor(a));
                return byTaps != 0 ? byTaps : a.CompareTo(b);
            });
            if (max >= 0 && picks.Count > max)
                picks.RemoveRange(max, picks.Count - max);
            return picks;
        }
    }
}
