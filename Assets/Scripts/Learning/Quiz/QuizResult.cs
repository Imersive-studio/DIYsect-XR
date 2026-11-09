namespace DIYsect.Learning
{
    /// <summary>What the server decided about one quiz request. Only <see cref="Accepted"/> changes anything.</summary>
    public enum QuizResult : byte
    {
        /// <summary>The request was valid and was applied.</summary>
        Accepted = 0,
        /// <summary>The quiz is not in a phase that allows this request (for example answering when no round is running).</summary>
        WrongPhase = 1,
        /// <summary>This client may not make this request (only the host starts and ends rounds; the host does not answer).</summary>
        NotAllowed = 2,
        /// <summary>This client already answered the current round.</summary>
        AlreadyAnswered = 3,
        /// <summary>The organ index is not one of the body's organs.</summary>
        BadOrgan = 4,
        /// <summary>The answer was for an earlier round.</summary>
        StaleRound = 5,
        /// <summary>The server does not know this request.</summary>
        Unknown = 6,
    }
}
