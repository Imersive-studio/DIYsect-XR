namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// What the start screen needs from whatever joins the room. The scene uses <see cref="ClassmateJoinAdapter"/> over
    /// ClassmateAutoJoin; a test uses a fake, so no lobby service is needed.
    /// </summary>
    public interface IClassmateJoiner
    {
        /// <summary>True while nothing will be joined until the classmate asks to.</summary>
        bool WaitingForPress { get; }

        /// <summary>True when the classmate is in the room.</summary>
        bool Connected { get; }

        /// <summary>True when the last room code found no room.</summary>
        bool CodeRejected { get; }

        /// <summary>Join the first public room.</summary>
        void PressJoin();

        /// <summary>Join the room with this code. Returns false when it is not a valid code.</summary>
        bool JoinWithCode(string typedCode);
    }
}
