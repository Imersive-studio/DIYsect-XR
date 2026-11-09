using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>
    /// The allowed moves of a quiz round, as data: each row says "this event is accepted in this phase and leads to
    /// that phase". A new move is a new row, not a new code path. Pure logic with no Unity or network state, so it is
    /// easy to test. Who may send an event (host or classmate) is checked by <see cref="QuizRound"/>, not here.
    /// </summary>
    public sealed class QuizRule
    {
        public readonly struct Transition
        {
            public Transition(QuizPhase from, QuizEvent quizEvent, QuizPhase to)
            {
                From = from;
                Event = quizEvent;
                To = to;
            }

            public QuizPhase From { get; }
            public QuizEvent Event { get; }
            public QuizPhase To { get; }
        }

        /// <summary>
        /// Start from nothing or from a finished round; answer while asking; end a running round; close a finished one.
        /// An answer keeps the phase at Asking: it only changes the counts.
        /// </summary>
        public static QuizRule Default { get; } = new QuizRule(new[]
        {
            new Transition(QuizPhase.Idle, QuizEvent.Start, QuizPhase.Asking),
            new Transition(QuizPhase.Revealed, QuizEvent.Start, QuizPhase.Asking),
            new Transition(QuizPhase.Asking, QuizEvent.Answer, QuizPhase.Asking),
            new Transition(QuizPhase.Asking, QuizEvent.End, QuizPhase.Revealed),
            new Transition(QuizPhase.Revealed, QuizEvent.Close, QuizPhase.Idle),
        });

        private readonly Dictionary<(QuizPhase, QuizEvent), QuizPhase> _moves = new Dictionary<(QuizPhase, QuizEvent), QuizPhase>();
        private readonly HashSet<QuizEvent> _known = new HashSet<QuizEvent>();

        public QuizRule(IEnumerable<Transition> transitions)
        {
            if (transitions == null)
                throw new ArgumentNullException(nameof(transitions));
            foreach (Transition transition in transitions)
            {
                _moves.Add((transition.From, transition.Event), transition.To);
                _known.Add(transition.Event);
            }
        }

        /// <summary>Judges one event in the current phase. <paramref name="next"/> is the new phase only when the result is Accepted.</summary>
        public QuizResult Evaluate(QuizPhase current, QuizEvent quizEvent, out QuizPhase next)
        {
            next = current;
            if (!_known.Contains(quizEvent))
                return QuizResult.Unknown;
            if (!_moves.TryGetValue((current, quizEvent), out QuizPhase to))
                return QuizResult.WrongPhase;
            next = to;
            return QuizResult.Accepted;
        }
    }
}
