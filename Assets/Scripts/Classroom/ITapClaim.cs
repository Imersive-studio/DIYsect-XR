namespace DIYsect.Classroom
{
    /// <summary>
    /// Something that can claim the current tap for itself (for example a quiz answer), so the question-pin input stands aside.
    /// Classroom defines it; features that depend on Classroom implement it.
    /// </summary>
    public interface ITapClaim
    {
        /// <summary>True while a tap should be treated as this feature's input and not as a question pin.</summary>
        bool ClaimsTap { get; }
    }
}
