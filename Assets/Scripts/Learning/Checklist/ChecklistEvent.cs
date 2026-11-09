namespace DIYsect.Learning
{
    /// <summary>
    /// The small set of things that can tick the checklist. Each one is mapped from a public event every device
    /// already receives (the dissection stage, an organ changing holder, an organ snap verdict), see
    /// <see cref="ChecklistProgress"/>. Append new values at the end; never reorder or reuse.
    /// </summary>
    public enum ChecklistEvent : byte
    {
        /// <summary>The dissection stage reached Incised.</summary>
        IncisionTraced = 0,
        /// <summary>The dissection stage reached SkinOpen.</summary>
        SkinOpened = 1,
        /// <summary>The dissection stage reached RectusOpen.</summary>
        RectusOpened = 2,
        /// <summary>The dissection stage reached OmentumLifted.</summary>
        OmentumLifted = 3,
        /// <summary>Someone took hold of an organ.</summary>
        OrganLifted = 4,
        /// <summary>The organ in hand was held long enough to count as inspected.</summary>
        OrganInspected = 5,
        /// <summary>The organ in hand was let go.</summary>
        OrganDropped = 6,
        /// <summary>The organ was let go at its own socket and accepted.</summary>
        OrganPutBack = 7,
        /// <summary>The body went back to the intact stage (a fresh body).</summary>
        BodyReset = 8,
    }
}
