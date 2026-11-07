namespace DIYsect.Classroom
{
    /// <summary>Why the server refused a pin request.</summary>
    public enum PinRejection
    {
        BadOrgan,
        BadPoint,
        BadText,
        RateLimited,
    }
}
