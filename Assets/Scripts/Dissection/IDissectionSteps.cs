using System;

namespace DIYsect.Dissection
{
    /// <summary>What a tool needs to ask for a layer step. Lets tools stay ignorant of networking.</summary>
    public interface IDissectionSteps
    {
        DissectionStage Stage { get; }

        /// <summary>Raised on the requesting peer when the server has answered one of its requests.</summary>
        event Action<DissectionAction, StepResult> StepResolved;

        /// <summary>Asks the server to do this step. The server accepts it only in the right stage.</summary>
        void RequestStep(DissectionAction action);
    }
}
