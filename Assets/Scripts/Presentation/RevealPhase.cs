namespace DIYsect.Presentation
{
    /// <summary>Where a label or instruction is in its show and hide animation. <see cref="RevealRule"/> moves between them.</summary>
    public enum RevealPhase
    {
        Hidden,
        Entering,
        Shown,
        Exiting,
    }

    /// <summary>What can happen to a reveal: someone asks to show or hide it, or its current animation finishes.</summary>
    public enum RevealEvent
    {
        Show,
        Hide,
        Finished,
    }
}
