namespace DIYsect.Learning
{
    /// <summary>The words of the inspection title, taken from an <see cref="OrganInfoEntry"/>. Plain C#, so it is easy to test.</summary>
    public static class OrganInfoCardText
    {
        /// <summary>The small label above the title.</summary>
        public const string Eyebrow = "ANATOMICAL STRUCTURE";

        /// <summary>The organ's name in capitals. Empty for a missing entry.</summary>
        public static string Title(OrganInfoEntry entry)
        {
            return entry == null || entry.DisplayName == null ? string.Empty : entry.DisplayName.ToUpperInvariant();
        }

        /// <summary>The sentence about what the organ does. Empty for a missing entry.</summary>
        public static string Function(OrganInfoEntry entry)
        {
            return entry?.Function ?? string.Empty;
        }

        /// <summary>The short extra fact. Empty for a missing entry.</summary>
        public static string Fact(OrganInfoEntry entry)
        {
            return entry?.Fact ?? string.Empty;
        }
    }
}
