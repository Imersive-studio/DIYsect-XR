namespace DIYsect.Classroom
{
    /// <summary>
    /// The one rule table for the help request: (phase, event) -> next phase. Pure, so it is tested without a scene.
    /// A request that is already open stays open, an event that does not apply changes nothing, and anything that
    /// removes the student (<see cref="AssistEvent.SteererLeft"/>) always closes the request, so it can never get stuck.
    /// </summary>
    public static class AssistRule
    {
        public static AssistPhase Next(AssistPhase phase, AssistEvent change)
        {
            switch (change)
            {
                case AssistEvent.StudentRequested:
                    return phase == AssistPhase.None ? AssistPhase.Requested : phase;
                case AssistEvent.StudentCancelled:
                    return AssistPhase.None;
                case AssistEvent.ProfessorTookControl:
                    return phase == AssistPhase.Requested ? AssistPhase.Helping : phase;
                case AssistEvent.ProfessorReleased:
                    // Help is finished once the professor lets go. A request that nobody answered yet stays open.
                    return phase == AssistPhase.Helping ? AssistPhase.None : phase;
                case AssistEvent.SteererLeft:
                    return AssistPhase.None;
                default:
                    return phase;
            }
        }
    }
}
