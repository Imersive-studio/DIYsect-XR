using System.Text.RegularExpressions;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// Turns the failure text the lobby code reports (<c>LobbyManager.OnLobbyFailed</c>, <c>connectionFailedAction</c>) into a
    /// <see cref="JoinFailure"/> and a short plain sentence. The lobby code only passes a string on, so this reads the string.
    /// </summary>
    public static class JoinFailureMessages
    {
        private static readonly Regex Full = new Regex(@"\bfull\b", RegexOptions.IgnoreCase);
        private static readonly Regex Closed = new Regex(@"\b(closed|deleted|ended)\b", RegexOptions.IgnoreCase);

        /// <summary>Reads a failure text. Empty text is <see cref="JoinFailure.None"/>.</summary>
        public static JoinFailure Classify(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return JoinFailure.None;
            string text = raw.ToLowerInvariant();
            if (text.Contains("still in progress"))
                return JoinFailure.Busy;
            if (Full.IsMatch(text))
                return JoinFailure.RoomFull;
            if (text.Contains("rate limit"))
                return JoinFailure.TooManyTries;
            if (text.Contains("authenticate"))
                return JoinFailure.NoInternet;
            if (text.Contains("not found"))
                return JoinFailure.RoomNotFound;
            if (Closed.IsMatch(text))
                return JoinFailure.RoomClosed;
            if (text.Contains("connect to lobby"))
                return JoinFailure.HostUnreachable;
            return JoinFailure.CouldNotJoin;
        }

        /// <summary>The sentence for a failure. Empty when there is nothing worth saying.</summary>
        public static string For(JoinFailure failure)
        {
            switch (failure)
            {
                case JoinFailure.RoomNotFound:
                    return "No class found with that code. Check the code and try again.";
                case JoinFailure.RoomFull:
                    return "That class is full. Ask your professor to make room.";
                case JoinFailure.RoomClosed:
                    return "The class has ended or the room was closed.";
                case JoinFailure.TooManyTries:
                    return "Too many tries. Wait a few seconds and try again.";
                case JoinFailure.NoInternet:
                    return "Cannot reach the internet. Check your connection.";
                case JoinFailure.HostUnreachable:
                    return "Found the class but could not connect. Ask your professor to check their headset.";
                case JoinFailure.CouldNotJoin:
                    return "Could not join the class. Check your internet and try again.";
                default:
                    return string.Empty;
            }
        }
    }
}
