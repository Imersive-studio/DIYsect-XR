using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// Decides what the classmate's banner says from four facts: connected or not, the connection state, the last join failure,
    /// and whether the automatic rejoin gave up. No Unity objects, so it is tested on its own.
    /// Before the classmate has ever been in the room the banner stays quiet unless a join failed, because the status line
    /// already says "Looking for the professor's room".
    /// </summary>
    public sealed class ReconnectBannerModel
    {
        public const string GaveUpText = "Could not reconnect. Check your internet, or ask your professor for the room code.";

        private bool _connected;
        private bool _wasConnected;
        private bool _gaveUp;
        private JoinFailure _failure;
        private XRINetworkGameManager.ConnectionState _state = XRINetworkGameManager.ConnectionState.None;

        /// <summary>True when the banner should be on screen.</summary>
        public bool Visible => Text.Length > 0;

        public string Text => Build();

        public void SetConnected(bool connected)
        {
            _connected = connected;
            if (!connected)
                return;
            _wasConnected = true;
            _gaveUp = false;
            _failure = JoinFailure.None;
        }

        public void SetState(XRINetworkGameManager.ConnectionState state)
        {
            _state = state;
            // A new attempt is running, so the last failure is old news.
            if (state == XRINetworkGameManager.ConnectionState.Connecting || state == XRINetworkGameManager.ConnectionState.Authenticating)
                _failure = JoinFailure.None;
        }

        public void SetFailure(JoinFailure failure)
        {
            if (failure == JoinFailure.Busy)
                return;
            _failure = failure;
        }

        public void SetGaveUp(bool gaveUp)
        {
            _gaveUp = gaveUp;
        }

        private string Build()
        {
            if (_connected)
                return string.Empty;

            JoinFailure failure = _failure;
            // A code that found no room, after being in the room, means the room is gone.
            if (_wasConnected && failure == JoinFailure.RoomNotFound)
                failure = JoinFailure.RoomClosed;
            string failureText = JoinFailureMessages.For(failure);

            if (!_wasConnected)
                return failureText;
            if (_gaveUp)
                return GaveUpText;

            string head;
            switch (_state)
            {
                case XRINetworkGameManager.ConnectionState.Connecting:
                    head = "Rejoining the class…";
                    break;
                case XRINetworkGameManager.ConnectionState.Authenticated:
                    head = "Connection lost. Reconnecting…";
                    break;
                default:
                    head = "No connection. Waiting for the internet…";
                    break;
            }
            return failureText.Length > 0 ? head + "\n" + failureText : head;
        }
    }
}
