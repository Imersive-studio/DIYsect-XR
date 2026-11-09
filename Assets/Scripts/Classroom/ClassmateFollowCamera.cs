// Authority: none. This only moves the classmate's own camera on their own screen. The head poses it reads come from HeadPoseBoard (server-owned).
using System;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Lets a laptop or phone classmate look at the body from where the professor's or the student's head is, and come back.
    /// While following, it takes over the camera from the <see cref="ClassmateOrbitCamera"/> (which keeps my own orbit state
    /// untouched and ignores drag and zoom). Every change of target eases over a short time from the pose the camera has at
    /// that moment, so there is never a jump. Going back to my view eases to the pose my orbit view has, which is where I was.
    /// If the followed person leaves, the camera eases back to my view by itself.
    /// </summary>
    [DefaultExecutionOrder(100)] // after the orbit camera has placed (or skipped placing) the transform this frame
    public class ClassmateFollowCamera : MonoBehaviour
    {
        [SerializeField] private ClassmateOrbitCamera _orbit;
        [SerializeField] private HeadPoseBoard _board;
        [SerializeField, Min(0.05f), Tooltip("Seconds a move into follow, out of it, or between people takes.")]
        private float _easeSeconds = 0.8f;

        private FollowTransition _transition;

        // Made on first use: a button may ask before this component's own Awake has run.
        private FollowTransition Transition => _transition ??= new FollowTransition(_easeSeconds);
        private Vector3 _fromPosition;
        private Quaternion _fromRotation;

        /// <summary>Raised when the target changes or a person appears or leaves, so buttons can refresh.</summary>
        public event Action Changed;

        /// <summary>Raised with a short message when the followed person left and the camera is heading back to my view.</summary>
        public event Action<string> Message;

        public FollowTarget Target => Transition.Target;
        public bool IsFollowing => Transition.IsFollowing;
        public float EaseSeconds => _easeSeconds;

        private void Awake()
        {
            bool valid = true;
            if (_orbit == null) { Debug.LogError($"{name}: ClassmateFollowCamera has no ClassmateOrbitCamera. Run Tools > DIYsect > Add Classmate Desktop Camera.", this); valid = false; }
            if (_board == null) { Debug.LogError($"{name}: ClassmateFollowCamera has no HeadPoseBoard. Run Tools > DIYsect > Add Head Pose.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        private void OnEnable()
        {
            _board.Changed += HandleBoardChanged;
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.Changed -= HandleBoardChanged;
            // Hand the camera back so a disabled follow component never leaves it stuck.
            if (_orbit != null)
                _orbit.Driven = false;
        }

        /// <summary>True when that person's head is known, so the camera can follow them.</summary>
        public bool IsAvailable(FollowTarget target)
        {
            switch (target)
            {
                case FollowTarget.Professor: return _board.IsPresent(PointerRole.Professor);
                case FollowTarget.Student: return _board.IsPresent(PointerRole.Student);
                default: return true;
            }
        }

        /// <summary>Eases the camera to that person's eyes. False (and nothing changes) when they are not in the session.</summary>
        public bool Follow(FollowTarget target)
        {
            if (target == FollowTarget.Mine)
            {
                BackToMyView();
                return true;
            }
            if (target == Transition.Target)
                return true;
            if (!Transition.Request(target, IsAvailable(target)))
                return false;
            StartMove();
            return true;
        }

        /// <summary>Eases the camera back to my own orbit view. Does nothing when I am not following.</summary>
        public void BackToMyView()
        {
            if (!Transition.IsFollowing)
                return;
            Transition.Request(FollowTarget.Mine, true);
            StartMove();
        }

        private void StartMove()
        {
            // The move starts from wherever the camera is drawn now.
            _fromPosition = transform.position;
            _fromRotation = transform.rotation;
            Changed?.Invoke();
        }

        private void HandleBoardChanged()
        {
            Changed?.Invoke();
        }

        private void LateUpdate()
        {
            FollowTarget before = Transition.Target;
            bool lost = Transition.Tick(Time.unscaledDeltaTime, IsAvailable(Transition.Target));
            if (lost)
            {
                StartMove();
                Message?.Invoke(before == FollowTarget.Professor ? "Professor left" : "Student left");
            }
            if (!Transition.Driving)
            {
                _orbit.Driven = false;
                return;
            }
            _orbit.Driven = true;

            Vector3 toPosition;
            Quaternion toRotation;
            if (Transition.Target == FollowTarget.Mine)
            {
                toPosition = _orbit.MyPosition;
                toRotation = _orbit.MyRotation;
            }
            else
            {
                PointerRole role = Transition.Target == FollowTarget.Professor ? PointerRole.Professor : PointerRole.Student;
                if (!_board.TryGetLocalPose(role, out Vector3 localPosition, out Quaternion localRotation))
                    return; // the next Tick sees the person is gone
                Transform body = _board.transform;
                toPosition = body.TransformPoint(localPosition);
                toRotation = FollowTransition.Level(body.rotation * localRotation);
            }
            FollowTransition.Blend(_fromPosition, _fromRotation, toPosition, toRotation, Transition.Eased, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
