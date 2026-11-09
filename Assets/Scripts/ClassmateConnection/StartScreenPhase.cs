namespace DIYsect.ClassmateConnection
{
    /// <summary>What the classmate's start screen is doing.</summary>
    public enum StartScreenPhase
    {
        /// <summary>Not on screen: the classmate is in the room, or joined without it (an invite link).</summary>
        Hidden,
        /// <summary>On screen, waiting for the classmate to press Join.</summary>
        Ready,
        /// <summary>On screen, the classmate pressed Join and the room is being found.</summary>
        Joining,
    }
}
