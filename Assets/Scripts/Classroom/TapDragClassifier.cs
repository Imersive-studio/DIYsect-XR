// Authority: none. Pure local math, no Unity objects.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Tells a tap, a hold and a drag apart for one finger or mouse press. A press becomes a drag when it moves more than
    /// <see cref="DragPixels"/> away from where it started. A press that stays put for <see cref="HoldSeconds"/> is a hold
    /// (reported once through <see cref="TakeHold"/>, while the finger is still down). A press released before that, without
    /// moving, is a tap. Once it is a drag it stays a drag until release, so a long slow drag that wanders back never turns into a tap.
    /// A hold that then moves also becomes a drag (the camera orbits); the hold has already been reported.
    /// </summary>
    public sealed class TapDragClassifier
    {
        private Vector2 _start;
        private Vector2 _last;
        private float _startTime;
        private bool _holdReached;
        private bool _holdTaken;

        public TapDragClassifier(float dragPixels = 10f, float holdSeconds = 0.5f)
        {
            DragPixels = dragPixels;
            HoldSeconds = holdSeconds;
        }

        public float DragPixels { get; }
        public float HoldSeconds { get; }
        /// <summary>True between <see cref="Begin"/> and <see cref="End"/>.</summary>
        public bool Active { get; private set; }
        public bool IsDrag { get; private set; }
        /// <summary>True once the press has stayed put for <see cref="HoldSeconds"/>. It stays true until the next press.</summary>
        public bool HoldReached => _holdReached;

        public void Begin(Vector2 position, float time)
        {
            Active = true;
            IsDrag = false;
            _holdReached = false;
            _holdTaken = false;
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
                if (movedFar)
                {
                    IsDrag = true;
                    delta = position - _start; // include the movement spent finding out it was a drag
                }
                else if (time - _startTime >= HoldSeconds)
                {
                    _holdReached = true;
                }
            }
            else
            {
                delta = position - _last;
            }
            _last = position;
            return delta;
        }

        /// <summary>Returns true once per press, when the press has become a hold. Call it after <see cref="Move"/> or <see cref="End"/>.</summary>
        public bool TakeHold()
        {
            if (!_holdReached || _holdTaken)
                return false;
            _holdTaken = true;
            return true;
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
            _holdReached = false;
        }

        /// <summary>Ends the press. Returns true if it was a tap (not a drag, and not a hold).</summary>
        public bool End(Vector2 position, float time)
        {
            if (!Active)
                return false;
            Move(position, time); // a very late release still counts as a drag
            Active = false;
            return !IsDrag && !_holdReached;
        }
    }
}
