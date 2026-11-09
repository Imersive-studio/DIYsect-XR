using System;
using System.Collections.Generic;

namespace DIYsect.Learning
{
    /// <summary>
    /// Keeps the card state of every organ. It is the one writer: all changes go through <see cref="Handle"/>, which
    /// uses <see cref="OrganInfoCardRules"/>. It holds no network state of its own; the presenter feeds it the grab and
    /// pointer events every device already receives. Plain C#, no Unity.
    /// </summary>
    public sealed class OrganInfoCardTracker
    {
        private readonly OrganInfoCardState[] _states;

        public OrganInfoCardTracker(int organCount)
        {
            _states = new OrganInfoCardState[Math.Max(0, organCount)];
        }

        /// <summary>Raised when an organ's card state changes (organ index, previous state, new state).</summary>
        public event Action<int, OrganInfoCardState, OrganInfoCardState> Changed;

        public int Count => _states.Length;

        /// <summary>The state of the organ's card. An index out of range is Hidden.</summary>
        public OrganInfoCardState StateOf(int organIndex)
        {
            return IsValid(organIndex) ? _states[organIndex] : OrganInfoCardState.Hidden;
        }

        /// <summary>Applies one event to one organ and raises <see cref="Changed"/> if its state moved. An index out of range is ignored.</summary>
        public OrganInfoCardStep Handle(int organIndex, OrganInfoCardEvent cardEvent)
        {
            if (!IsValid(organIndex))
                return new OrganInfoCardStep(OrganInfoCardState.Hidden, OrganInfoCardEffect.None);

            OrganInfoCardState previous = _states[organIndex];
            OrganInfoCardStep step = OrganInfoCardRules.Apply(previous, cardEvent);
            if (step.Next != previous)
            {
                _states[organIndex] = step.Next;
                Changed?.Invoke(organIndex, previous, step.Next);
            }
            return step;
        }

        /// <summary>
        /// Tells the tracker which organs have a pointer on them now (any number, in any order; indexes out of range are
        /// skipped). Organs that gained a pointer get PointerEntered; organs that lost theirs get PointerLeft.
        /// </summary>
        public void SyncPointers(IReadOnlyList<int> pointedOrgans)
        {
            for (int i = 0; i < _states.Length; i++)
            {
                bool pointedNow = Contains(pointedOrgans, i);
                if (pointedNow == OrganInfoCardRules.IsPointed(_states[i]))
                    continue;
                Handle(i, pointedNow ? OrganInfoCardEvent.PointerEntered : OrganInfoCardEvent.PointerLeft);
            }
        }

        private bool IsValid(int organIndex)
        {
            return organIndex >= 0 && organIndex < _states.Length;
        }

        private static bool Contains(IReadOnlyList<int> list, int value)
        {
            if (list == null)
                return false;
            for (int i = 0; i < list.Count; i++)
                if (list[i] == value)
                    return true;
            return false;
        }
    }
}
