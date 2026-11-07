namespace DIYsect.Classroom
{
    /// <summary>The plain sentence shown to a user for each reason the server can refuse a pin. Kept in one place.</summary>
    public static class PinRejectionMessages
    {
        public static string For(PinRejection reason)
        {
            switch (reason)
            {
                case PinRejection.BadOrgan:
                    return "Pin must be on an organ";
                case PinRejection.BadPoint:
                    return "Pin must be on the organ's surface";
                case PinRejection.BadText:
                    return "Question must be 1 to " + PinRules.MaxTextLength + " characters";
                case PinRejection.RateLimited:
                    return "Slow down: one question every few seconds";
                default:
                    return "That pin was refused";
            }
        }
    }
}
