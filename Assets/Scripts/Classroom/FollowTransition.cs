// Authority: none. Pure local state for the classmate's own camera.
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The mode and the ease of the follow camera, with no scene objects so it can be tested on its own.
    /// A request sets the target and restarts <see cref="Progress"/> at 0; the camera is drawn as a blend from the pose it had
    /// at that moment to the live pose of the target, by <see cref="Eased"/>. Ticking advances Progress to 1 over the duration,
    /// so every change of target (into follow, out of it, or from one person to the other) is a smooth ease, never a jump.
    /// </summary>
    public class FollowTransition
    {
        private readonly float _seconds;

        public FollowTransition(float seconds)
        {
            _seconds = Mathf.Max(0.01f, seconds);
            Progress = 1f;
        }

        public FollowTarget Target { get; private set; } = FollowTarget.Mine;

        /// <summary>0 when the move just started, 1 when it has arrived.</summary>
        public float Progress { get; private set; }

        /// <summary>Progress with the start and the end slowed down.</summary>
        public float Eased => Smooth(Progress);

        public bool Arrived => Progress >= 1f;

        /// <summary>True while the camera is following someone or still easing back to my view; the orbit camera must leave the transform alone.</summary>
        public bool Driving => Target != FollowTarget.Mine || !Arrived;

        /// <summary>True when the classmate asked to follow someone (even while still easing there).</summary>
        public bool IsFollowing => Target != FollowTarget.Mine;

        /// <summary>
        /// Asks for a new target. Asking for the current target changes nothing. A person who is not in the session cannot be
        /// followed (returns false); going back to my view is always allowed.
        /// </summary>
        public bool Request(FollowTarget target, bool available)
        {
            if (target == Target)
                return true;
            if (target != FollowTarget.Mine && !available)
                return false;
            Target = target;
            Progress = 0f;
            return true;
        }

        /// <summary>Moves the ease forward. If the followed person has left, the camera starts easing back to my view; true when that happened.</summary>
        public bool Tick(float deltaTime, bool targetAvailable)
        {
            bool lost = Target != FollowTarget.Mine && !targetAvailable;
            if (lost)
            {
                Target = FollowTarget.Mine;
                Progress = 0f;
            }
            Progress = Mathf.Min(1f, Progress + Mathf.Max(0f, deltaTime) / _seconds);
            return lost;
        }

        /// <summary>0 at t=0, 1 at t=1, flat at both ends.</summary>
        public static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        /// <summary>Blends two camera poses: position by a straight line, rotation by the shortest turn.</summary>
        public static void Blend(Vector3 fromPosition, Quaternion fromRotation, Vector3 toPosition, Quaternion toRotation, float t, out Vector3 position, out Quaternion rotation)
        {
            position = Vector3.Lerp(fromPosition, toPosition, t);
            rotation = Quaternion.Slerp(fromRotation, toRotation, t);
        }

        /// <summary>
        /// Keeps where the head looks but removes its sideways tilt, so the followed view stays upright when the head tilts.
        /// A head looking straight up or down keeps its rotation as it is.
        /// </summary>
        public static Quaternion Level(Quaternion rotation)
        {
            Vector3 forward = rotation * Vector3.forward;
            if (Mathf.Abs(forward.y) > 0.999f)
                return rotation;
            return Quaternion.LookRotation(forward, Vector3.up);
        }
    }
}
