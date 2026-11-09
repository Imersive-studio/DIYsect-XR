namespace DIYsect.Classroom
{
    /// <summary>What the student view window is doing, as seen from the local client.</summary>
    public enum StudentViewMode
    {
        /// <summary>No question has a view, so the window is not shown. The next question with a view is followed.</summary>
        Hidden,

        /// <summary>The window shows the newest question with a view and switches to each new one.</summary>
        FollowingNewest,

        /// <summary>The professor stepped to an older question, so the window stays on it until it is removed or they return to the newest.</summary>
        Pinned,
    }
}
