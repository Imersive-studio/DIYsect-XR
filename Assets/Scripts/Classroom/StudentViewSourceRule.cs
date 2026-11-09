namespace DIYsect.Classroom
{
    /// <summary>
    /// Which picture the student view window shows for a mode. Pure logic with no scene state, so it is easy to test.
    /// Hidden: None. FollowingNewest and Pinned: Pin. Live: LiveStudent when the student's head is known, else LiveNoStudent.
    /// </summary>
    public static class StudentViewSourceRule
    {
        public static StudentViewSource Choose(StudentViewMode mode, bool studentPresent)
        {
            switch (mode)
            {
                case StudentViewMode.FollowingNewest:
                case StudentViewMode.Pinned:
                    return StudentViewSource.Pin;
                case StudentViewMode.Live:
                    return studentPresent ? StudentViewSource.LiveStudent : StudentViewSource.LiveNoStudent;
                default:
                    return StudentViewSource.None;
            }
        }
    }
}
