namespace DIYsect.Dissection
{
    /// <summary>
    /// A request a client can make to move the dissection one layer deeper. Append new actions at the end;
    /// never reorder or reuse values. Each action's allowed stage is listed in <see cref="LayerOrderRule"/>.
    /// </summary>
    public enum DissectionAction : byte
    {
        OpenRectus = 0,
        LiftOmentum = 1,
    }
}
