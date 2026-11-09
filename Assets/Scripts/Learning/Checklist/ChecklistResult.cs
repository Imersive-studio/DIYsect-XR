namespace DIYsect.Learning
{
    /// <summary>What <see cref="ChecklistRule"/> decided about one event.</summary>
    public enum ChecklistResult : byte
    {
        /// <summary>The event was the expected one and the checklist moved on one step.</summary>
        Advanced = 0,
        /// <summary>A step was undone (for example the organ was dropped before it was inspected).</summary>
        Reverted = 1,
        /// <summary>The checklist went back to the start.</summary>
        Reset = 2,
        /// <summary>The event does not matter at this point. Nothing changed.</summary>
        Ignored = 3,
    }
}
