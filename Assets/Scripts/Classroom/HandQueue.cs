using System.Collections.Generic;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Who has a hand up, in the order they raised it. One hand per person, a limited number in all. No network or Unity
    /// objects, so it is tested on its own; <see cref="HandBoard"/> keeps one on the server and copies every change to the clients.
    /// </summary>
    public sealed class HandQueue
    {
        private readonly List<ulong> _order = new List<ulong>();
        private readonly int _max;

        public HandQueue(int max)
        {
            _max = max;
        }

        public int Count => _order.Count;

        /// <summary>The person at a place in the queue, first to raise first.</summary>
        public ulong this[int index] => _order[index];

        public bool Contains(ulong clientId) => _order.Contains(clientId);

        /// <summary>1 for the first hand up, 2 for the next, and so on. 0 when the person has no hand up.</summary>
        public int OrderOf(ulong clientId) => _order.IndexOf(clientId) + 1;

        /// <summary>Puts the hand up at the back of the queue. False when it was already up or the queue is full.</summary>
        public bool Raise(ulong clientId)
        {
            if (_order.Count >= _max || _order.Contains(clientId))
                return false;
            _order.Add(clientId);
            return true;
        }

        /// <summary>Takes the hand down. False when it was not up.</summary>
        public bool Lower(ulong clientId) => _order.Remove(clientId);

        public void Clear() => _order.Clear();

        /// <summary>Everyone with a hand up, first to raise first.</summary>
        public IReadOnlyList<ulong> Snapshot() => _order.ToArray();
    }
}
