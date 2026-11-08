namespace DIYsect.Networking
{
    /// <summary>What happened when an organ was let go.</summary>
    public enum SnapOutcome : byte
    {
        /// <summary>Released away from every socket and outside the body. The organ falls and rests where it lands.</summary>
        None = 0,
        /// <summary>Released at its own socket. The organ is snapped to its exact home pose.</summary>
        Accepted = 1,
        /// <summary>Released at another organ's socket. The organ is sent back to its own home pose.</summary>
        Rejected = 2,
        /// <summary>Released over the open body but not at a socket. The organ is put back home with no verdict.</summary>
        Returned = 3,
    }
}
