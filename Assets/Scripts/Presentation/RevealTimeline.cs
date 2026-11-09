using System;
using UnityEngine;

namespace DIYsect.Presentation
{
    /// <summary>
    /// The clock behind one show and hide animation. <see cref="Phase"/> is the only state; it changes only through
    /// <see cref="RevealRule"/>. <see cref="Amount"/> runs from 0 (hidden) to 1 (fully shown) and stays continuous when
    /// the direction flips half way, so a label caught mid-fade turns around smoothly.
    /// Plain C#: a presenter calls <see cref="Tick"/> from its Update only while <see cref="IsAnimating"/> is true.
    /// </summary>
    public class RevealTimeline
    {
        private readonly float _inSeconds;
        private readonly float _outSeconds;

        public RevealTimeline(float inSeconds, float outSeconds)
        {
            if (inSeconds <= 0f || outSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(inSeconds), "Reveal durations must be above zero.");
            _inSeconds = inSeconds;
            _outSeconds = outSeconds;
        }

        public RevealPhase Phase { get; private set; } = RevealPhase.Hidden;

        /// <summary>Linear progress from 0 (hidden) to 1 (shown). Presenters apply an <see cref="Ease"/> curve to it.</summary>
        public float Amount { get; private set; }

        public bool IsVisible => Phase != RevealPhase.Hidden;
        public bool IsAnimating => Phase == RevealPhase.Entering || Phase == RevealPhase.Exiting;

        /// <summary>Raised after every phase change, with the new phase.</summary>
        public event Action<RevealPhase> PhaseChanged;

        public void Show() => Apply(RevealEvent.Show);
        public void Hide() => Apply(RevealEvent.Hide);

        /// <summary>Jumps straight to fully shown or hidden, for a late joiner or a scene that loads mid-step.</summary>
        public void Snap(bool shown)
        {
            Amount = shown ? 1f : 0f;
            Set(shown ? RevealPhase.Shown : RevealPhase.Hidden);
        }

        /// <summary>Moves the animation on by <paramref name="deltaSeconds"/>. Does nothing unless it is entering or exiting.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
                return;
            if (Phase == RevealPhase.Entering)
            {
                Amount = Mathf.Min(1f, Amount + deltaSeconds / _inSeconds);
                if (Amount >= 1f)
                    Apply(RevealEvent.Finished);
            }
            else if (Phase == RevealPhase.Exiting)
            {
                Amount = Mathf.Max(0f, Amount - deltaSeconds / _outSeconds);
                if (Amount <= 0f)
                    Apply(RevealEvent.Finished);
            }
        }

        private void Apply(RevealEvent change) => Set(RevealRule.Next(Phase, change));

        private void Set(RevealPhase next)
        {
            if (next == Phase)
                return;
            Phase = next;
            PhaseChanged?.Invoke(next);
        }
    }
}
