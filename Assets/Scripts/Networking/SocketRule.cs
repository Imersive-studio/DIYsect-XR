using System;
using System.Collections.Generic;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Decides what a released organ does. Plain C#, so it is easy to unit test.
    /// The organ is accepted when its centre is within the position tolerance of its own socket
    /// and its rotation is within the angle tolerance. With a larger capture radius it is also accepted within that
    /// radius when its own socket is the nearest socket (ties count as its own), so a forgiving drop never snaps an
    /// organ into a neighbour's place. It is rejected when it is not accepted but its centre is
    /// within the position tolerance of another organ's socket (the nearest such socket is reported).
    /// Anywhere else the answer is "none". Bad input (NaN, a missing organ) also gives "none".
    /// </summary>
    public sealed class SocketRule
    {
        private readonly float _positionTolerance;
        private readonly float _angleToleranceDegrees;
        private readonly float _captureRadius;

        public SocketRule(float positionTolerance, float angleToleranceDegrees)
            : this(positionTolerance, angleToleranceDegrees, positionTolerance)
        {
        }

        /// <param name="captureRadius">Up to this distance the organ is accepted if its own socket is the nearest one. Not less than the position tolerance.</param>
        public SocketRule(float positionTolerance, float angleToleranceDegrees, float captureRadius)
        {
            if (!(positionTolerance >= 0f) || float.IsInfinity(positionTolerance))
                throw new ArgumentOutOfRangeException(nameof(positionTolerance), "The position tolerance must be zero or more and finite.");
            if (!(angleToleranceDegrees >= 0f) || float.IsInfinity(angleToleranceDegrees))
                throw new ArgumentOutOfRangeException(nameof(angleToleranceDegrees), "The angle tolerance must be zero or more and finite.");
            if (!(captureRadius >= positionTolerance) || float.IsInfinity(captureRadius))
                throw new ArgumentOutOfRangeException(nameof(captureRadius), "The capture radius must be finite and at least the position tolerance.");
            _positionTolerance = positionTolerance;
            _angleToleranceDegrees = angleToleranceDegrees;
            _captureRadius = captureRadius;
        }

        /// <param name="organIndex">Index of the organ; its own socket is <c>sockets[organIndex]</c>.</param>
        /// <param name="organ">The organ's centre and rotation now.</param>
        /// <param name="sockets">Every socket's centre and rotation, one per organ.</param>
        public SnapDecision Evaluate(int organIndex, Pose organ, IReadOnlyList<Pose> sockets)
        {
            var none = new SnapDecision(SnapOutcome.None, -1);
            if (sockets == null || organIndex < 0 || organIndex >= sockets.Count || !IsFinite(organ))
                return none;

            Pose own = sockets[organIndex];
            bool turnedRight = IsFinite(own) && Quaternion.Angle(organ.rotation, own.rotation) <= _angleToleranceDegrees;
            float ownDistance = IsFinite(own) ? Vector3.Distance(organ.position, own.position) : float.MaxValue;
            if (turnedRight && ownDistance <= _positionTolerance)
                return new SnapDecision(SnapOutcome.Accepted, organIndex);
            if (turnedRight && ownDistance <= _captureRadius && OwnIsNearest(organIndex, organ.position, ownDistance, sockets))
                return new SnapDecision(SnapOutcome.Accepted, organIndex);

            // Close to its own socket but turned too far: not a wrong answer, just not done yet.
            if (IsFinite(own) && Vector3.Distance(organ.position, own.position) <= _positionTolerance)
                return none;

            int nearest = -1;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < sockets.Count; i++)
            {
                if (i == organIndex || !IsFinite(sockets[i]))
                    continue;
                float distance = Vector3.Distance(organ.position, sockets[i].position);
                if (distance <= _positionTolerance && distance < nearestDistance)
                {
                    nearest = i;
                    nearestDistance = distance;
                }
            }
            return nearest >= 0 ? new SnapDecision(SnapOutcome.Rejected, nearest) : none;
        }

        // True when no other socket is strictly closer than the organ's own socket.
        private static bool OwnIsNearest(int organIndex, Vector3 position, float ownDistance, IReadOnlyList<Pose> sockets)
        {
            for (int i = 0; i < sockets.Count; i++)
                if (i != organIndex && IsFinite(sockets[i]) && Vector3.Distance(position, sockets[i].position) < ownDistance)
                    return false;
            return true;
        }

        private static bool IsFinite(Pose pose)
        {
            Vector3 p = pose.position;
            Quaternion q = pose.rotation;
            return float.IsFinite(p.x) && float.IsFinite(p.y) && float.IsFinite(p.z)
                && float.IsFinite(q.x) && float.IsFinite(q.y) && float.IsFinite(q.z) && float.IsFinite(q.w)
                && q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w > 1e-6f;
        }
    }
}
