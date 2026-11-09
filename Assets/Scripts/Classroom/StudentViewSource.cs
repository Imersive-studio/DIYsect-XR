namespace DIYsect.Classroom
{
    /// <summary>Where the student view window gets its picture from right now.</summary>
    public enum StudentViewSource
    {
        /// <summary>Nothing to show; the window is hidden.</summary>
        None,

        /// <summary>The stored pose of a question pin.</summary>
        Pin,

        /// <summary>The student's head, live.</summary>
        LiveStudent,

        /// <summary>Live was chosen but the student is not in the room.</summary>
        LiveNoStudent,
    }
}
