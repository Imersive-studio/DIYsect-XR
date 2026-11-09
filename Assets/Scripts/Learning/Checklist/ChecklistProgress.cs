using System;
using DIYsect.Dissection;

namespace DIYsect.Learning
{
    /// <summary>
    /// The one writer of the checklist state. It turns what every device sees (the dissection stage, an organ
    /// changing holder, an organ snap verdict) into <see cref="ChecklistEvent"/> values, asks
    /// <see cref="ChecklistRule"/> what they mean, and raises <see cref="Changed"/> when the state moves.
    /// It also remembers which organ the student lifted, so only that organ can be inspected and put back.
    /// Pure logic with no Unity or network types: <see cref="ChecklistTracker"/> feeds it the real events.
    /// </summary>
    public sealed class ChecklistProgress
    {
        /// <summary>Value of <see cref="ActiveOrgan"/> when no organ is being tracked.</summary>
        public const int NoOrgan = -1;

        private readonly ChecklistRule _rule;

        public ChecklistProgress() : this(ChecklistRule.Default)
        {
        }

        public ChecklistProgress(ChecklistRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
        }

        /// <summary>The step the student has to do next. Every earlier step is ticked.</summary>
        public ChecklistState State { get; private set; } = ChecklistState.TraceIncision;

        /// <summary>The organ index lifted for the checklist, or <see cref="NoOrgan"/>. Set while inspecting and putting back.</summary>
        public int ActiveOrgan { get; private set; } = NoOrgan;

        /// <summary>Raised with the new state each time it changes (once per call, even when several steps tick at once).</summary>
        public event Action<ChecklistState> Changed;

        /// <summary>
        /// Brings the checklist up to the shared dissection stage. Used for every stage change and, at spawn, for
        /// late joiners: all the layer steps up to this stage are ticked in order. Stages the checklist has
        /// already passed change nothing. The intact stage means a fresh body, so the checklist starts again.
        /// </summary>
        public void ApplyStage(DissectionStage stage)
        {
            ChecklistState before = State;
            if (stage == DissectionStage.Intact)
            {
                Apply(ChecklistEvent.BodyReset);
            }
            else
            {
                if (stage >= DissectionStage.Incised)
                    Apply(ChecklistEvent.IncisionTraced);
                if (stage >= DissectionStage.SkinOpen)
                    Apply(ChecklistEvent.SkinOpened);
                if (stage >= DissectionStage.RectusOpen)
                    Apply(ChecklistEvent.RectusOpened);
                if (stage >= DissectionStage.OmentumLifted)
                    Apply(ChecklistEvent.OmentumLifted);
            }
            Raise(before);
        }

        /// <summary>An organ was taken (<paramref name="held"/> true) or let go (false).</summary>
        public void OnHolderChanged(int organ, bool held)
        {
            ChecklistState before = State;
            if (held)
            {
                // Only the first organ lifted after the omentum counts; grabs at any other time are not a step.
                if (State == ChecklistState.LiftOrgan)
                {
                    ActiveOrgan = organ;
                    Apply(ChecklistEvent.OrganLifted);
                }
            }
            else if (organ == ActiveOrgan)
            {
                Apply(ChecklistEvent.OrganDropped);
            }
            Raise(before);
        }

        /// <summary>The organ was held long enough to inspect it. Ignored for any organ other than the active one.</summary>
        public void OnHeldLongEnough(int organ)
        {
            if (organ != ActiveOrgan)
                return;
            ChecklistState before = State;
            Apply(ChecklistEvent.OrganInspected);
            Raise(before);
        }

        /// <summary>The server judged a release. Only an accepted snap of the active organ puts it back.</summary>
        public void OnSnapResolved(int organ, bool accepted)
        {
            if (!accepted || organ != ActiveOrgan)
                return;
            ChecklistState before = State;
            Apply(ChecklistEvent.OrganPutBack);
            Raise(before);
        }

        private void Apply(ChecklistEvent trigger)
        {
            ChecklistResult result = _rule.Evaluate(State, trigger, out ChecklistState next);
            if (result == ChecklistResult.Ignored)
                return;
            State = next;
            // The tracked organ only matters while it is being inspected or put back.
            if (State != ChecklistState.InspectOrgan && State != ChecklistState.PutOrganBack)
                ActiveOrgan = NoOrgan;
        }

        private void Raise(ChecklistState before)
        {
            if (State != before)
                Changed?.Invoke(State);
        }
    }
}
