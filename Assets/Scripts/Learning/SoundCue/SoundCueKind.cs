namespace DIYsect.Learning
{
    /// <summary>The three short sounds the learning layer can play.</summary>
    public enum SoundCueKind : byte
    {
        /// <summary>A soft tick while the scalpel cuts.</summary>
        Cut = 0,
        /// <summary>A soft two-tone sound when you pick up an organ.</summary>
        Grab = 1,
        /// <summary>A gentle rising chime for a correct quiz answer.</summary>
        Correct = 2,
    }
}
