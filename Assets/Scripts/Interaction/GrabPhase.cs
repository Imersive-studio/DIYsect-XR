namespace DIYsect.Interaction
{
    /// <summary>Where a client is in asking the server for something it wants to hold (an organ, the scalpel, the depth handle, the guide line).</summary>
    public enum GrabPhase : byte
    {
        /// <summary>Not holding and not asking.</summary>
        Idle = 0,
        /// <summary>Asked the server and still holding on locally; waiting for the answer.</summary>
        Requested = 1,
        /// <summary>Asked the server, then let go (or lost it) before the answer came. A late yes is given straight back.</summary>
        Abandoned = 2,
        /// <summary>The server said yes and this client is holding it.</summary>
        Granted = 3,
    }
}
