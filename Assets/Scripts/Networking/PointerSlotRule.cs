namespace DIYsect.Networking
{
    /// <summary>
    /// The rule for the classmate pointer slot, as pure logic with no network state so it is easy to test.
    /// One slot is shared by every classmate and the newest tap wins. Tapping the organ you already point at stops your pointer;
    /// tapping the organ someone else points at takes it over, because that is a new "look here".
    /// </summary>
    public static class PointerSlotRule
    {
        /// <summary>Slot value when nobody is pointing. The same value as the pointer board's own "none".</summary>
        public const int None = -1;

        /// <summary>Owner value of a slot nobody holds.</summary>
        public const ulong NoOwner = ulong.MaxValue;

        /// <summary>The organ the slot holds after <paramref name="sender"/> taps <paramref name="requestedOrgan"/>.</summary>
        public static int Next(int currentOrgan, ulong currentOwner, ulong sender, int requestedOrgan)
        {
            bool sameOrganByOwner = requestedOrgan == currentOrgan && currentOwner == sender;
            return sameOrganByOwner ? None : requestedOrgan;
        }
    }
}
