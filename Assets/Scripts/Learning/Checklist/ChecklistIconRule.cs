namespace DIYsect.Learning
{
    /// <summary>Which icon goes with each step. Pure, and in step with <see cref="ChecklistLabels.ToolName"/>.</summary>
    public static class ChecklistIconRule
    {
        public static ChecklistIcon For(ChecklistState state)
        {
            switch (state)
            {
                case ChecklistState.TraceIncision:
                    return ChecklistIcon.Scalpel;
                case ChecklistState.OpenRectus:
                case ChecklistState.LiftOmentum:
                    return ChecklistIcon.Forceps;
                case ChecklistState.Complete:
                    return ChecklistIcon.Check;
                default:
                    return ChecklistIcon.Hand;
            }
        }
    }
}
