using System.Text;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// What a classmate may call themselves. The name is shown to the whole class and on the professor's panels, which read
    /// angle brackets as formatting, so it is cleaned first. Pure text work, tested on its own.
    /// </summary>
    public static class NameRules
    {
        public const int MaxLength = 24;
        public const string EmptyText = "Please type your name.";

        /// <summary>
        /// Cleans a typed name: control characters and angle brackets go, runs of spaces become one, the ends are trimmed and the
        /// result is cut to <see cref="MaxLength"/>. Returns false when nothing is left.
        /// </summary>
        public static bool TryClean(string raw, out string name)
        {
            name = string.Empty;
            if (string.IsNullOrEmpty(raw))
                return false;
            var text = new StringBuilder(raw.Length);
            bool lastWasSpace = true;
            foreach (char c in raw)
            {
                if (c == '<' || c == '>' || (char.IsControl(c) && !char.IsWhiteSpace(c)))
                    continue;
                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace)
                        text.Append(' ');
                    lastWasSpace = true;
                    continue;
                }
                text.Append(c);
                lastWasSpace = false;
            }
            string cleaned = text.ToString().Trim();
            if (cleaned.Length > MaxLength)
                cleaned = cleaned.Substring(0, MaxLength).TrimEnd();
            name = cleaned;
            return name.Length > 0;
        }
    }
}
