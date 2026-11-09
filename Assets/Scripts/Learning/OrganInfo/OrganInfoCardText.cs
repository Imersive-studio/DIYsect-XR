using System.Text;

namespace DIYsect.Learning
{
    /// <summary>Turns an <see cref="OrganInfoEntry"/> into the words on the card. Plain C#, so it is easy to test.</summary>
    public static class OrganInfoCardText
    {
        /// <summary>
        /// The name in bold, the function, then the fact after "Good to know:". The card's text field understands the
        /// bold tag. Returns an empty string for a missing entry.
        /// </summary>
        public static string Build(OrganInfoEntry entry)
        {
            if (entry == null)
                return string.Empty;
            var text = new StringBuilder();
            text.Append("<b>").Append(entry.DisplayName).Append("</b>\n");
            text.Append(entry.Function).Append("\n\n");
            text.Append("<b>Good to know:</b> ").Append(entry.Fact);
            return text.ToString();
        }
    }
}
