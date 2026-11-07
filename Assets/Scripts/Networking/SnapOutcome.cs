namespace DIYsect.Networking
{
    /// <summary>What happened when an organ was let go.</summary>
    public enum SnapOutcome : byte
    {
        /// <summary>Released away from every socket. The organ stays where it was dropped.</summary>
        None = 0,
        /// <summary>Released at its own socket. The organ is snapped to its exact home pose.</summary>
        Accepted = 1,
        /// <summary>Released at another organ's socket. The organ is sent back to its own home pose.</summary>
        Rejected = 2,
    }
}
