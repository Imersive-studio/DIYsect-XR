namespace DIYsect.Dissection
{
    /// <summary>What the server decided about one scalpel contact sample.</summary>
    public enum SampleResult
    {
        /// <summary>The sample advanced the incision.</summary>
        Accepted,
        /// <summary>The sample was on the line but did not move forward (backward or no movement). Nothing changed.</summary>
        Ignored,
        /// <summary>The sample was too far from the midline.</summary>
        OffLine,
        /// <summary>The sample skipped ahead further than one step allows.</summary>
        TooFar,
        /// <summary>The incision is already complete.</summary>
        WrongStage,
        /// <summary>The sample was not a finite point.</summary>
        Invalid,
    }
}
