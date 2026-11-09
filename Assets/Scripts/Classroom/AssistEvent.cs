namespace DIYsect.Classroom
{
    /// <summary>Everything that can change an <see cref="AssistPhase"/>.</summary>
    public enum AssistEvent
    {
        /// <summary>The student pressed the help button.</summary>
        StudentRequested,

        /// <summary>The student pressed the help button again to take the request back.</summary>
        StudentCancelled,

        /// <summary>The professor grabbed the depth handle (the guiding hand has a controller who is not the student).</summary>
        ProfessorTookControl,

        /// <summary>The professor let the depth handle go (the guiding hand has no controller).</summary>
        ProfessorReleased,

        /// <summary>The student put the scalpel down, or left the room: the person who asked is gone.</summary>
        SteererLeft,
    }
}
