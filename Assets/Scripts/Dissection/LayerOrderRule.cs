using System;
using System.Collections.Generic;

namespace DIYsect.Dissection
{
    /// <summary>
    /// The allowed order of the layer steps, as data: each row says "this action is accepted only at this stage
    /// and moves the dissection to that stage". A new step is a new row (and new enum values), not a new code path.
    /// Pure logic with no Unity or network state, so it is easy to test.
    /// </summary>
    public sealed class LayerOrderRule
    {
        public readonly struct Step
        {
            public Step(DissectionAction action, DissectionStage requires, DissectionStage result)
            {
                Action = action;
                Requires = requires;
                Result = result;
            }

            public DissectionAction Action { get; }
            public DissectionStage Requires { get; }
            public DissectionStage Result { get; }
        }

        /// <summary>The order the body is dissected in after the skin: rectus first, then the omentum.</summary>
        public static LayerOrderRule Default { get; } = new LayerOrderRule(new[]
        {
            new Step(DissectionAction.OpenRectus, DissectionStage.SkinOpen, DissectionStage.RectusOpen),
            new Step(DissectionAction.LiftOmentum, DissectionStage.RectusOpen, DissectionStage.OmentumLifted),
        });

        private readonly Dictionary<DissectionAction, Step> _steps = new Dictionary<DissectionAction, Step>();

        public LayerOrderRule(IEnumerable<Step> steps)
        {
            if (steps == null)
                throw new ArgumentNullException(nameof(steps));
            foreach (Step step in steps)
                _steps.Add(step.Action, step);
        }

        /// <summary>Judges one request at the current stage. <paramref name="next"/> is the new stage only when the result is Accepted.</summary>
        public StepResult Evaluate(DissectionStage current, DissectionAction action, out DissectionStage next)
        {
            next = current;
            if (!_steps.TryGetValue(action, out Step step))
                return StepResult.Unknown;
            if (current >= step.Result)
                return StepResult.AlreadyDone;
            if (current != step.Requires)
                return StepResult.WrongOrder;
            next = step.Result;
            return StepResult.Accepted;
        }
    }
}
