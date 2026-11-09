using System.Collections.Generic;

namespace DIYsect.Editor
{
    /// <summary>The outcome of a <see cref="DemoBuildSequence"/>: the steps that built, and the one that failed (null if none).</summary>
    public sealed class DemoBuildResult
    {
        public IReadOnlyList<DemoBuildStep> Completed { get; }
        public DemoBuildStep Failed { get; }
        public string Error { get; }
        public bool Succeeded => Failed == null;

        public DemoBuildResult(IReadOnlyList<DemoBuildStep> completed, DemoBuildStep failed, string error)
        {
            Completed = completed;
            Failed = failed;
            Error = error;
        }
    }
}
