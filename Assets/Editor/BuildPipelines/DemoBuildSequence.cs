using System;
using System.Collections.Generic;

namespace DIYsect.Editor
{
    /// <summary>
    /// Runs a list of build steps in order and stops at the first one that fails. It never builds anything itself, so
    /// tests give it fake steps. A step that throws counts as a failure, so the error always names the build.
    /// </summary>
    public sealed class DemoBuildSequence
    {
        private readonly IReadOnlyList<DemoBuildStep> _steps;

        public DemoBuildSequence(IReadOnlyList<DemoBuildStep> steps)
        {
            _steps = steps ?? throw new ArgumentNullException(nameof(steps));
        }

        public DemoBuildResult Run()
        {
            var completed = new List<DemoBuildStep>();
            foreach (DemoBuildStep step in _steps)
            {
                string error = RunStep(step);
                if (error != null)
                    return new DemoBuildResult(completed, step, error);
                completed.Add(step);
            }
            return new DemoBuildResult(completed, null, null);
        }

        private static string RunStep(DemoBuildStep step)
        {
            try
            {
                return step.Build() ? null : $"The {step.Name} build failed. See the build log above.";
            }
            catch (Exception e)
            {
                return $"The {step.Name} build threw {e.GetType().Name}: {e.Message}";
            }
        }
    }
}
