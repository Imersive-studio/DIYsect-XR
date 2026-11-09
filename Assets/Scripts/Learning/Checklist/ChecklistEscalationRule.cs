namespace DIYsect.Learning
{
    public static class ChecklistEscalationRule
    {
        public const float DelaySeconds = 12f;

        public static ChecklistCueStrength Evaluate(ChecklistState state, float secondsWithoutProgress)
        {
            return state != ChecklistState.Complete && secondsWithoutProgress >= DelaySeconds
                ? ChecklistCueStrength.Explicit : ChecklistCueStrength.Subtle;
        }
    }
}
