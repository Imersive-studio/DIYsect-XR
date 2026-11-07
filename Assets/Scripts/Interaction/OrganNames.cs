using System.Text;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Turns an organ's object name (the model file name, for example "kidney_l") into the name shown to people
    /// ("Left kidney"). The rule is: underscores become spaces, a trailing "l" or "r" side marker becomes a leading
    /// "Left" or "Right", and the first letter is a capital. Every other manifest name is already readable.
    /// </summary>
    public static class OrganNames
    {
        public static string Display(string objectName)
        {
            if (string.IsNullOrWhiteSpace(objectName))
                return string.Empty;

            string spaced = objectName.Trim().Replace('_', ' ').Replace('.', ' ');
            string[] words = spaced.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return string.Empty;

            string side = null;
            int count = words.Length;
            string last = words[count - 1].ToLowerInvariant();
            if (count > 1 && (last == "l" || last == "r"))
            {
                side = last == "l" ? "Left" : "Right";
                count--;
            }

            var text = new StringBuilder();
            if (side != null)
                text.Append(side).Append(' ');
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    text.Append(' ');
                text.Append(words[i]);
            }
            // The first letter is a capital, the rest is left as written.
            text[0] = char.ToUpperInvariant(text[0]);
            if (side != null && text.Length > side.Length + 1)
            {
                int first = side.Length + 1;
                text[first] = char.ToLowerInvariant(text[first]);
            }
            return text.ToString();
        }
    }
}
