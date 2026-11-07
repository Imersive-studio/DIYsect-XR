namespace DIYsect.Networking
{
    /// <summary>The answer of <see cref="SocketRule"/>: the outcome and the socket the organ was nearest to.</summary>
    public readonly struct SnapDecision
    {
        public SnapDecision(SnapOutcome outcome, int socketIndex)
        {
            Outcome = outcome;
            SocketIndex = socketIndex;
        }

        public SnapOutcome Outcome { get; }

        /// <summary>The socket that decided the outcome, or -1 for <see cref="SnapOutcome.None"/>.</summary>
        public int SocketIndex { get; }
    }
}
