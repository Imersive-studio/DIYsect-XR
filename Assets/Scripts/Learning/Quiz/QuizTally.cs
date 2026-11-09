// Authority: server only. QuizRound keeps one tally on the server; clients only see the counts it publishes.
using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>
    /// The answers of one quiz round: who answered which organ, how many were right and wrong, and how many taps
    /// each organ got. Each client counts once per round. Pure logic with no Unity or network state, so it is easy to test.
    /// </summary>
    public sealed class QuizTally
    {
        private readonly Dictionary<ulong, int> _answerByClient = new Dictionary<ulong, int>();
        private readonly int[] _perOrgan;

        public QuizTally(int organCount)
        {
            if (organCount <= 0)
                throw new ArgumentOutOfRangeException(nameof(organCount), "The body must have at least one organ.");
            _perOrgan = new int[organCount];
            Target = -1;
        }

        /// <summary>The organ the round asks for, or -1 before the first round.</summary>
        public int Target { get; private set; }
        public int Right { get; private set; }
        public int Wrong { get; private set; }
        public int OrganCount => _perOrgan.Length;

        /// <summary>True when the index is one of the body's organs.</summary>
        public bool IsOrgan(int organIndex) => organIndex >= 0 && organIndex < _perOrgan.Length;

        /// <summary>Taps the organ got in this round.</summary>
        public int CountFor(int organIndex) => IsOrgan(organIndex) ? _perOrgan[organIndex] : 0;

        /// <summary>Starts a new round for the target organ and forgets every earlier answer.</summary>
        public QuizResult Begin(int targetOrgan)
        {
            if (!IsOrgan(targetOrgan))
                return QuizResult.BadOrgan;
            _answerByClient.Clear();
            Array.Clear(_perOrgan, 0, _perOrgan.Length);
            Right = 0;
            Wrong = 0;
            Target = targetOrgan;
            return QuizResult.Accepted;
        }

        /// <summary>Records one answer. <paramref name="correct"/> is only meaningful when the result is Accepted.</summary>
        public QuizResult Record(ulong client, int organIndex, out bool correct)
        {
            correct = false;
            if (!IsOrgan(organIndex))
                return QuizResult.BadOrgan;
            if (_answerByClient.ContainsKey(client))
                return QuizResult.AlreadyAnswered;
            _answerByClient.Add(client, organIndex);
            _perOrgan[organIndex]++;
            correct = organIndex == Target;
            if (correct)
                Right++;
            else
                Wrong++;
            return QuizResult.Accepted;
        }
    }
}
