using System.Text;

namespace DIYsect.Learning
{
    /// <summary>
    /// Keeps a question's text small enough to send over the network. The text travels in a FixedString128Bytes, which
    /// holds 126 bytes of UTF-8, so the limit here leaves a little room. Pure logic, easy to test.
    /// </summary>
    public static class QuizPromptText
    {
        /// <summary>Most UTF-8 bytes a question text may use.</summary>
        public const int MaxBytes = 120;

        /// <summary>Trims the text and cuts it, never in the middle of a character, so it fits in <see cref="MaxBytes"/>.</summary>
        public static string Clamp(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            string trimmed = text.Trim();
            if (Encoding.UTF8.GetByteCount(trimmed) <= MaxBytes)
                return trimmed;

            var kept = new StringBuilder();
            int bytes = 0;
            int i = 0;
            while (i < trimmed.Length)
            {
                // A character outside the basic range is two UTF-16 units; keep them together.
                int length = char.IsHighSurrogate(trimmed[i]) && i + 1 < trimmed.Length ? 2 : 1;
                string piece = trimmed.Substring(i, length);
                int size = Encoding.UTF8.GetByteCount(piece);
                if (bytes + size > MaxBytes)
                    break;
                kept.Append(piece);
                bytes += size;
                i += length;
            }
            return kept.ToString().TrimEnd();
        }
    }
}
