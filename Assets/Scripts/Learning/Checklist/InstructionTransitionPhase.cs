namespace DIYsect.Learning
{
    /// <summary>Where the guide card is in changing from one instruction to the next. <see cref="InstructionTransitionRule"/> moves between them.</summary>
    public enum InstructionTransitionPhase : byte
    {
        /// <summary>Nothing is moving; the current instruction is shown.</summary>
        Idle,
        /// <summary>The finished instruction shrinks into a check mark that draws itself.</summary>
        Completing,
        /// <summary>The finished instruction (or check) rolls out while the next one rolls in.</summary>
        Rolling,
    }

    /// <summary>What can happen to the instruction card.</summary>
    public enum InstructionTransitionEvent : byte
    {
        /// <summary>A step was finished (the checklist moved forward).</summary>
        StepCompleted,
        /// <summary>The step changed without being finished (it was undone, or the checklist restarted).</summary>
        StepChanged,
        /// <summary>The current animation ran to its end.</summary>
        Finished,
        /// <summary>The card must show the current instruction at once (first show, late joiner, re-enable).</summary>
        Snapped,
    }
}
