namespace DIYsect.Learning
{
    /// <summary>Why an organ's info card is shown (or not). The card is visible in every state except <see cref="Hidden"/>.</summary>
    public enum OrganInfoCardState : byte
    {
        /// <summary>Nobody holds the organ and nobody points at it. No card.</summary>
        Hidden = 0,
        /// <summary>A pointer rests on the organ. Card shown.</summary>
        Pointed = 1,
        /// <summary>Someone holds the organ. Card shown.</summary>
        Held = 2,
        /// <summary>Someone holds the organ and a pointer rests on it. Card shown.</summary>
        HeldAndPointed = 3,
    }
}
