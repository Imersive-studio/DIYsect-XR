namespace DIYsect.Dissection
{
    /// <summary>
    /// How far the dissection has gone. The order is the order of the steps. Later tasks append stages
    /// after <see cref="OmentumLifted"/> and add a row to <see cref="LayerOrderRule"/>; never reorder or reuse values.
    /// </summary>
    public enum DissectionStage : byte
    {
        Intact = 0,
        Incised = 1,
        SkinOpen = 2,
        RectusOpen = 3,
        OmentumLifted = 4,
    }
}
