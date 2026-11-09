using DIYsect.Interaction;

namespace DIYsect.Learning
{
    /// <summary>The words on the little name card a classmate gets from tapping an organ. Plain C#, so it is easy to test.</summary>
    public static class OrganNameText
    {
        /// <summary>
        /// The organ's name in bold, with its system and role in a small muted line when the library knows them. An organ the library does not
        /// know still gets its name, read from its object name ("kidney_l" becomes "Left kidney"). Empty when there is no name at all.
        /// </summary>
        public static string Build(OrganInfoEntry entry, string organObjectName)
        {
            if (entry != null && !string.IsNullOrWhiteSpace(entry.DisplayName))
            {
                // A glance, not a paragraph: the name, then its system and role in a smaller muted line.
                string detail = Detail(entry.System, entry.Role);
                return detail.Length == 0
                    ? $"<b>{entry.DisplayName}</b>"
                    : $"<b>{entry.DisplayName}</b>\n<size=78%><color=#9FB8C4>{detail}</color></size>";
            }
            string name = OrganNames.Display(organObjectName);
            return name.Length == 0 ? string.Empty : $"<b>{name}</b>";
        }

        private static string Detail(string system, string role)
        {
            bool hasSystem = !string.IsNullOrWhiteSpace(system);
            bool hasRole = !string.IsNullOrWhiteSpace(role);
            if (hasSystem && hasRole)
                return $"{system} · {role}";
            return hasSystem ? system : hasRole ? role : string.Empty;
        }
    }
}
