namespace DIYsect.Dissection
{
    /// <summary>What the server decided about one layer request.</summary>
    public enum StepResult : byte
    {
        /// <summary>The request was in order and the stage advanced.</summary>
        Accepted,
        /// <summary>The layer before this one is not open yet. Nothing changed.</summary>
        WrongOrder,
        /// <summary>The dissection is already at or past this step. Nothing changed.</summary>
        AlreadyDone,
        /// <summary>The server does not know this action. Nothing changed.</summary>
        Unknown,
    }
}
