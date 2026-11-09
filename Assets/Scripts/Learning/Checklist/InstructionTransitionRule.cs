namespace DIYsect.Learning
{
    /// <summary>
    /// The one rule table for the instruction card: (phase, event) -> next phase. Pure, so it is tested without a scene.
    /// A finished step always plays the check first. Anything else rolls straight to the new text, and a snap ends every
    /// animation. A new event during an animation turns it around at once, so the card never waits.
    /// </summary>
    public static class InstructionTransitionRule
    {
        public static InstructionTransitionPhase Next(InstructionTransitionPhase phase, InstructionTransitionEvent change)
        {
            switch (change)
            {
                case InstructionTransitionEvent.StepCompleted:
                    return InstructionTransitionPhase.Completing;
                case InstructionTransitionEvent.StepChanged:
                    return InstructionTransitionPhase.Rolling;
                case InstructionTransitionEvent.Finished:
                    if (phase == InstructionTransitionPhase.Completing) return InstructionTransitionPhase.Rolling;
                    return InstructionTransitionPhase.Idle;
                default:
                    return InstructionTransitionPhase.Idle;
            }
        }

        /// <summary>The event that a checklist transition turns into for the card.</summary>
        public static InstructionTransitionEvent EventFor(ChecklistTransition transition)
        {
            switch (transition)
            {
                case ChecklistTransition.Advance:
                case ChecklistTransition.Complete:
                    return InstructionTransitionEvent.StepCompleted;
                case ChecklistTransition.Revert:
                case ChecklistTransition.Reset:
                    return InstructionTransitionEvent.StepChanged;
                default:
                    return InstructionTransitionEvent.Snapped;
            }
        }
    }
}
