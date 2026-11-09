namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// Decides what the start screen shows from what the joiner reports. No Unity objects, so it is tested on its own.
    /// The screen shows while the joiner waits for a press. Once the classmate presses Join (or a link or typed code
    /// starts the join) it reads "Joining" until they are in the room. After the first time in the room it never comes back:
    /// a dropped connection is the reconnect banner's job.
    /// </summary>
    public sealed class StartScreenModel
    {
        private bool _pressed;
        private bool _joined;

        public StartScreenPhase Phase { get; private set; } = StartScreenPhase.Hidden;

        /// <summary>True when the last room code found no room and the classmate can try again.</summary>
        public bool CodeRejected { get; private set; }

        /// <summary>The classmate pressed Join (the big button or the code button).</summary>
        public void Press()
        {
            _pressed = true;
        }

        /// <summary>Takes the joiner's current facts and returns the phase.</summary>
        public StartScreenPhase Update(bool waitingForPress, bool connected, bool codeRejected)
        {
            if (connected)
                _joined = true;
            CodeRejected = codeRejected && !connected;

            if (_joined || connected)
                Phase = StartScreenPhase.Hidden;
            else if (waitingForPress)
                Phase = StartScreenPhase.Ready;
            else
                Phase = _pressed ? StartScreenPhase.Joining : StartScreenPhase.Hidden;
            return Phase;
        }
    }
}
