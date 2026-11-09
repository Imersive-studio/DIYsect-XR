using System;

namespace XRMultiplayer
{
    /// <summary>
    /// The invite a professor shares with the class: a short room code and a link that carries it, like a meeting link.
    /// A classmate who opens the link on a phone lands in the app, which joins that room. Pure text work with no network
    /// or device access, so it is tested on its own.
    /// </summary>
    public static class RoomInvite
    {
        /// <summary>The link scheme the classmate app registers with Android.</summary>
        public const string Scheme = "diysect";
        /// <summary>The Unity Lobby service gives every room a code this long, from capital letters and digits.</summary>
        public const int CodeLength = 6;

        /// <summary>The link for a room code, for example "diysect://join?code=KHC8QH". Empty when the code is not a valid code.</summary>
        public static string BuildLink(string code)
        {
            return TryNormalizeCode(code, out string normalized) ? $"{Scheme}://join?code={normalized}" : string.Empty;
        }

        /// <summary>
        /// Cleans up a code someone typed: capitals, with spaces and dashes removed ("khc 8-qh" becomes "KHC8QH"). Returns false
        /// when what is left is not a valid code.
        /// </summary>
        public static bool TryNormalizeCode(string input, out string code)
        {
            code = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;
            var letters = new System.Text.StringBuilder(input.Length);
            foreach (char c in input)
            {
                if (c == ' ' || c == '-' || c == '\t')
                    continue;
                char upper = char.ToUpperInvariant(c);
                bool valid = (upper >= 'A' && upper <= 'Z') || (upper >= '0' && upper <= '9');
                if (!valid)
                    return false;
                letters.Append(upper);
            }
            if (letters.Length != CodeLength)
                return false;
            code = letters.ToString();
            return true;
        }

        /// <summary>Reads the room code out of an invite link. Returns false for any other link, an empty one, or a bad code.</summary>
        public static bool TryParseLink(string link, out string code)
        {
            code = string.Empty;
            if (string.IsNullOrWhiteSpace(link))
                return false;
            string prefix = Scheme + "://";
            if (!link.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;
            string rest = link.Substring(prefix.Length);
            int query = rest.IndexOf('?');
            if (query < 0 || !rest.Substring(0, query).TrimEnd('/').Equals("join", StringComparison.OrdinalIgnoreCase))
                return false;
            foreach (string pair in rest.Substring(query + 1).Split('&'))
            {
                int equals = pair.IndexOf('=');
                if (equals > 0 && pair.Substring(0, equals).Equals("code", StringComparison.OrdinalIgnoreCase))
                    return TryNormalizeCode(Uri.UnescapeDataString(pair.Substring(equals + 1)), out code);
            }
            return false;
        }
    }
}
