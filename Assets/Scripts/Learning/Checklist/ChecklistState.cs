namespace DIYsect.Learning
{
    /// <summary>
    /// Where the student is in the dissection checklist: the step they have to do next. The value is also the
    /// number of steps already ticked, so every step below it is done. A new step is a new value before
    /// <see cref="Complete"/> (Complete moves up one) plus a row in <see cref="ChecklistRule"/>.
    /// </summary>
    public enum ChecklistState : byte
    {
        /// <summary>Waiting for the midline incision to be traced.</summary>
        TraceIncision = 0,
        /// <summary>Incision done; waiting for the skin flaps to be opened.</summary>
        OpenSkin = 1,
        /// <summary>Skin open; waiting for the rectus to be opened.</summary>
        OpenRectus = 2,
        /// <summary>Rectus open; waiting for the omentum to be lifted.</summary>
        LiftOmentum = 3,
        /// <summary>Omentum lifted; waiting for an organ to be lifted out.</summary>
        LiftOrgan = 4,
        /// <summary>An organ is in hand; waiting for it to be held long enough to inspect.</summary>
        InspectOrgan = 5,
        /// <summary>The organ was inspected; waiting for it to be put back in its own socket.</summary>
        PutOrganBack = 6,
        /// <summary>Every step is ticked.</summary>
        Complete = 7,
    }
}
