namespace DIYsect.Learning
{
    /// <summary>Something that happened to one organ that can change whether its info card is shown.</summary>
    public enum OrganInfoCardEvent : byte
    {
        /// <summary>A pointer (the professor's or the student's) came to rest on the organ.</summary>
        PointerEntered = 0,
        /// <summary>The pointer moved off the organ or was switched off.</summary>
        PointerLeft = 1,
        /// <summary>Someone picked the organ up (the server named a holder).</summary>
        Grabbed = 2,
        /// <summary>The holder let go. The organ is put back, eased home or dropped, so the hand is done with it.</summary>
        Released = 3,
    }
}
