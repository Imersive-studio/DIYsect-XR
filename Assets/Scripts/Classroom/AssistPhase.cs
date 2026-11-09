namespace DIYsect.Classroom
{
    /// <summary>Where the student's "Help me" request stands. The server is the only writer; <see cref="AssistRule"/> decides every change.</summary>
    public enum AssistPhase
    {
        /// <summary>Nobody is asking for help.</summary>
        None,

        /// <summary>The student asked for help and nobody has answered yet.</summary>
        Requested,

        /// <summary>The professor took the depth handle, so help is on its way.</summary>
        Helping,
    }
}
