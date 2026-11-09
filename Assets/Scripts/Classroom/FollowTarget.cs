namespace DIYsect.Classroom
{
    /// <summary>Whose eyes the classmate's camera looks through. <see cref="Mine"/> is the classmate's own orbit view.</summary>
    public enum FollowTarget : byte
    {
        Mine = 0,
        Professor = 1,
        Student = 2,
    }
}
