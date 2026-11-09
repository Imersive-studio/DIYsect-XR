namespace DIYsect.Learning
{
    /// <summary>What the view must do after an <see cref="OrganInfoCardEvent"/>.</summary>
    public enum OrganInfoCardEffect : byte
    {
        /// <summary>Leave the card as it is.</summary>
        None = 0,
        /// <summary>The card was hidden and must now be shown.</summary>
        Show = 1,
        /// <summary>The card was shown and must now be hidden.</summary>
        Hide = 2,
    }
}
