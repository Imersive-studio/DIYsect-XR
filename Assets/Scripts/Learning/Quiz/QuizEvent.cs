namespace DIYsect.Learning
{
    /// <summary>
    /// Something a client asks the quiz to do. <see cref="QuizRule"/> lists the phase each event is allowed in.
    /// Append new events at the end; never reorder or reuse values.
    /// </summary>
    public enum QuizEvent : byte
    {
        /// <summary>The professor starts a round with a target organ.</summary>
        Start = 0,
        /// <summary>A classmate taps an organ as the answer.</summary>
        Answer = 1,
        /// <summary>The professor ends the round and freezes the counts.</summary>
        End = 2,
        /// <summary>The professor clears the finished round from every screen.</summary>
        Close = 3,
    }
}
