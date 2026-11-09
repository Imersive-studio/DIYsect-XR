namespace DIYsect.Networking
{
    /// <summary>Who a pointer belongs to. Everyone sees the roles in different colours and wording.</summary>
    public enum PointerRole : byte
    {
        Professor = 0,
        Student = 1,
        /// <summary>A classmate on a phone who tapped an organ to say "look here". It has its own slot, colour and wording.</summary>
        Classmate = 2,
    }
}
