using DIYsect.Classroom;

namespace DIYsect.Learning
{
    /// <summary>The small decisions behind the cues, kept as plain functions so they can be tested.</summary>
    public static class SoundCueRules
    {
        /// <summary>A continuous cut gives a tick at most this often (seconds).</summary>
        public const float CutMinIntervalSeconds = 0.15f;

        /// <summary>The chime plays only when the server accepted the answer and it was the right organ.</summary>
        public static bool ShouldChime(QuizResult result, bool correct)
        {
            return result == QuizResult.Accepted && correct;
        }
    }
}
