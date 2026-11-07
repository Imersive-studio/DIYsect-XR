namespace DIYsect.Interaction
{
    /// <summary>Who owns the scalpel's cut depth, as seen from the local client.</summary>
    public enum GuidingState
    {
        /// <summary>Nobody holds the scalpel, so there is no depth to own.</summary>
        Idle,

        /// <summary>The student holds the scalpel and sets the depth with their own hand.</summary>
        StudentDepth,

        /// <summary>Another client (the professor) controls the depth.</summary>
        GuidedByOther,

        /// <summary>The local client controls the depth of somebody else's scalpel.</summary>
        GuidedByLocal,
    }
}
