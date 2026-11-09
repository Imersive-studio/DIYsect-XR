namespace DIYsect.MixedReality
{
    /// <summary>Where the table alignment is, as seen from the local client.</summary>
    public enum AlignmentPhase
    {
        /// <summary>Plane detection has not started yet, because the scene permission is not granted (or there is no plane manager).</summary>
        AwaitingPermission,

        /// <summary>Plane detection is on and nothing has been placed. A detected table will place the root.</summary>
        Detecting,

        /// <summary>The professor holds the alignment handle and the root follows it. Detected tables are ignored.</summary>
        Carrying,

        /// <summary>The root has been placed, by a detected table or by releasing the handle. Detected tables no longer move it.</summary>
        Placed,
    }
}
