// Authority: none. Pure local math, no Unity objects.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Tells a tap from a drag for one finger or mouse press. A press becomes a drag when it moves more than
    /// <see cref="DragPixels"/> away from where it started, or is held longer than <see cref="HoldSeconds"/>.
    /// Once it is a drag it stays a drag until release, so a long slow drag that wanders back never turns into a tap.
    /// </summary>
    public sealed class TapDragClassifier
    {
        private Vector2 _start;
        private Vector2 _last;
        private float _startTime;

        public TapDragClassifier(float dragPixels = 10f, float holdSeconds = 0.25f)
        {
            DragPixels = dragPixels;
            HoldSeconds = holdSeconds;
        }

        public float DragPixels { get; }
        public float HoldSeconds { get; }
        /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
        public bool Active { get; private set; }
        public bool IsDrag { get; private set; }

        public void Begin(Vector2 position, float time)
        {
            Active = true;
            IsDrag = false;
            _start = position;
            _last = position;
            _startTime = time;
        }

        /// <summary>Feeds the current pointer position. Returns how far to drag this frame in pixels (zero while it is still a possible tap).</summary>
        public Vector2 Move(Vector2 position, float time)
        {
            if (!Active)
                return Vector2.zero;
            Vector2 delta = Vector2.zero;
            if (!IsDrag)
            {
                bool movedFar = (position - _start).sqrMagnitude > DragPixels * DragPixels;
                bool heldLong = time - _startTime > HoldSeconds;
                if (movedFar || heldLong)
                {
                    IsDrag = true;
                    delta = position - _start; // include the movement spent finding out it was a drag
                }
            }
            else
            {
                delta = position - _last;
            }
            _last = position;
            return delta;
        }

        /// <summary>Marks the press as not a tap (a second finger arrived). No drag distance is reported for it.</summary>
        public void MarkNotATap()
        {
            if (Active)
                IsDrag = true;
        }

        /// <summary>Forgets the press without reporting a tap (the component was switched off mid-press).</summary>
        public void Cancel()
        {
            Active = false;
            IsDrag = false;
        }

        /// <summary>Ends the press. Returns true if it was a tap.</summary>
        public bool End(Vector2 position, float time)
        {
            if (!Active)
                return false;
            Move(position, time); // a very late release still counts as a drag
            Active = false;
            return !IsDrag;
        }
    }
}
