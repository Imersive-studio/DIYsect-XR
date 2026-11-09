namespace DIYsect.Presentation
{
    /// <summary>
    /// The one rule table for showing and hiding: (phase, event) -> next phase. Pure, so it is tested without a scene.
    /// Asking to show while leaving turns straight back (and the reverse), so nothing pops or waits.
    /// </summary>
    public static class RevealRule
    {
        public static RevealPhase Next(RevealPhase phase, RevealEvent change)
        {
            switch (change)
            {
                case RevealEvent.Show:
                    return phase == RevealPhase.Hidden || phase == RevealPhase.Exiting ? RevealPhase.Entering : phase;
                case RevealEvent.Hide:
                    return phase == RevealPhase.Shown || phase == RevealPhase.Entering ? RevealPhase.Exiting : phase;
                case RevealEvent.Finished:
                    if (phase == RevealPhase.Entering) return RevealPhase.Shown;
                    if (phase == RevealPhase.Exiting) return RevealPhase.Hidden;
                    return phase;
                default:
                    return phase;
            }
        }
    }
}
