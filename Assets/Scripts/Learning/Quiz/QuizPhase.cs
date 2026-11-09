namespace DIYsect.Learning
{
    /// <summary>
    /// Where a quiz round is. The server writes it in <see cref="QuizRound"/>; every allowed move between phases is a
    /// row in <see cref="QuizRule"/>. Append new phases at the end; never reorder or reuse values.
    /// </summary>
    public enum QuizPhase : byte
    {
        /// <summary>No quiz on screen.</summary>
        Idle = 0,
        /// <summary>The professor asked "tap the ..."; classmates may answer once each.</summary>
        Asking = 1,
        /// <summary>The round has ended; the final counts stay on screen until the next round or Close.</summary>
        Revealed = 2,
    }
}
