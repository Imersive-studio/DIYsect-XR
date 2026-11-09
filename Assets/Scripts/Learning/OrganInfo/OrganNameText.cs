using DIYsect.Interaction;

namespace DIYsect.Learning
{
    /// <summary>The words on the little name card a classmate gets from tapping an organ. Plain C#, so it is easy to test.</summary>
    public static class OrganNameText
    {
        /// <summary>
        /// The organ's name in bold, with what it does under it when the library knows the organ. An organ the library does not
        /// know still gets its name, read from its object name ("kidney_l" becomes "Left kidney"). Empty when there is no name at all.
        /// </summary>
        public static string Build(OrganInfoEntry entry, string organObjectName)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                return string.IsNullOrWhiteSpace(entry.Function)
                    ? $"<b>{entry.DisplayName}</b>"
                    : $"<b>{entry.DisplayName}</b>\n{entry.Function}";
            }
            string name = OrganNames.Display(organObjectName);
            return name.Length == 0 ? string.Empty : $"<b>{name}</b>";
        }
    }
}
