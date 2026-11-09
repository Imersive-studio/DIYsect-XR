// Authority: none. Local presentation for the professor. It reads the server-owned pins in QuestionBoard and the server-owned student head in HeadPoseBoard.
using System.Collections;
using DIYsect.Networking;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// A floating window that shows what a classmate saw when they asked a question. Each pin stores the asker's
    /// camera pose in the body's local space; this window puts its own camera at that pose (so it stays right after
    /// the body is moved onto a real table) and renders it into the window's texture. The headset camera never moves.
    /// By default it shows the newest question that has a view; <see cref="Next"/> and <see cref="Previous"/> step
    /// through the others. The professor can mark the shown question answered with <see cref="MarkSelectedAnswered"/>. The camera renders on demand a few times a second while the window is shown, not every
    /// frame, to keep the second view cheap on Quest.
    /// In <see cref="StudentViewMode.Live"/> the window instead shows what the student in VR sees now: the student's head pose
    /// comes from the <see cref="HeadPoseBoard"/> in the body's local space and is turned into world space with this device's
    /// own body, then rendered at most <see cref="StudentViewRenderRule.MaxRendersPerSecond"/> times a second.
    /// </summary>
    public class StudentViewWindow : MonoBehaviour
    {
        [SerializeField] private QuestionBoard _board;
        [SerializeField] private Camera _viewCamera;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMesh _caption;
        [SerializeField, Min(0.05f)] private float _refreshSeconds = 0.2f;
        [SerializeField, Tooltip("Optional. Needed for the Live mode. Lives on the body next to the question board.")]
        private HeadPoseBoard _headBoard;
        [SerializeField, Range(StudentViewRenderRule.MinRendersPerSecond, StudentViewRenderRule.MaxRendersPerSecond)]
        private float _liveRendersPerSecond = 12f;

        private int _selectedId = -1;
        private StudentViewMode _mode = StudentViewMode.Hidden;
        private Coroutine _refreshing;
        private float _lastRenderAt = -1f;
        private bool _studentWasPresent;

        /// <summary>The pin id shown now, or -1 when no question has a view.</summary>
        public int SelectedId => _selectedId;

        /// <summary>What the window is doing now. The panel and the refresh loop are derived from it.</summary>
        public StudentViewMode Mode => _mode;

        /// <summary>Raised after the mode changes, with the new mode.</summary>
        public event System.Action<StudentViewMode> ModeChanged;

        private void Awake()
        {
            bool valid = true;
            if (_board == null) { Debug.LogError($"{name}: StudentViewWindow has no QuestionBoard. Run Tools > DIYsect > Add Student View Window.", this); valid = false; }
            if (_viewCamera == null) { Debug.LogError($"{name}: StudentViewWindow has no view camera.", this); valid = false; }
            if (_panel == null) { Debug.LogError($"{name}: StudentViewWindow has no panel.", this); valid = false; }
            if (!valid)
            {
                enabled = false;
                return;
            }
            // Rendered on demand only.
            _viewCamera.enabled = false;
        }

        private void OnEnable()
        {
            _board.PinsChanged += HandlePinsChanged;
            if (_headBoard != null)
            {
                _studentWasPresent = StudentPresent();
                _headBoard.Changed += HandleHeadChanged;
            }
            HandlePinsChanged();
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.PinsChanged -= HandlePinsChanged;
            if (_headBoard != null)
                _headBoard.Changed -= HandleHeadChanged;
            // The mode is kept, so enabling the window again returns to the same question. Only the panel and loop stop.
            if (_panel != null)
                ApplyMode();
        }

        /// <summary>Shows the next question with a view (newer), wrapping around. Wired to the window's button.</summary>
        public void Next() => Step(+1);

        /// <summary>Shows the previous question with a view (older), wrapping around. Wired to the window's button.</summary>
        public void Previous() => Step(-1);

        /// <summary>Asks the server to mark the question shown now as answered. Only the professor is obeyed. Wired to the window's button.</summary>
        public void MarkSelectedAnswered()
        {
            if (_selectedId >= 0)
                _board.RequestMarkAnswered(_selectedId);
        }

        /// <summary>Switches between Live and following the questions. Does nothing when the window has no head board. Wired to the window's Live button.</summary>
        public void ToggleLive()
        {
            if (_mode == StudentViewMode.Live)
                ShowNewest();
            else if (_headBoard != null)
                SetMode(StudentViewMode.Live, -1);
        }

        /// <summary>Goes back to following the newest question, or hides the window when none has a view.</summary>
        public void ShowNewest()
        {
            int newest = NewestWithView();
            if (newest < 0)
                SetMode(StudentViewMode.Hidden, -1);
            else
                SetMode(StudentViewMode.FollowingNewest, newest);
        }

        // A pinned question stays while it still has a view. Anything else goes back to the newest.
        private void HandlePinsChanged()
        {
            // Live ignores the pins until the professor leaves it.
            if (_mode == StudentViewMode.Live)
                return;
            int index = IndexOf(_selectedId);
            if (_mode == StudentViewMode.Pinned && index >= 0 && _board[index].HasView)
                SetMode(StudentViewMode.Pinned, _selectedId);
            else
                ShowNewest();
        }

        // The head board fires on every pose update. Only a student arriving or leaving changes what the window shows.
        private void HandleHeadChanged()
        {
            bool present = StudentPresent();
            if (present == _studentWasPresent)
                return;
            _studentWasPresent = present;
            if (_mode == StudentViewMode.Live && isActiveAndEnabled)
                ApplyMode();
        }

        private bool StudentPresent() => _headBoard != null && _headBoard.IsPresent(PointerRole.Student);

        private void Step(int direction)
        {
            var withView = new System.Collections.Generic.List<int>();
            for (int i = 0; i < _board.Count; i++)
                if (_board[i].HasView)
                    withView.Add(i);
            if (withView.Count == 0)
            {
                ShowNewest();
                return;
            }
            int current = withView.IndexOf(IndexOf(_selectedId));
            int next = current < 0 ? withView.Count - 1 : (current + direction + withView.Count) % withView.Count;
            StudentViewMode mode = next == withView.Count - 1 ? StudentViewMode.FollowingNewest : StudentViewMode.Pinned;
            SetMode(mode, _board[withView[next]].Id);
        }

        // The only place the mode changes.
        private void SetMode(StudentViewMode mode, int pinId)
        {
            bool changed = mode != _mode;
            _mode = mode;
            _selectedId = mode == StudentViewMode.Hidden || mode == StudentViewMode.Live ? -1 : pinId;
            ApplyMode();
            if (changed)
                ModeChanged?.Invoke(_mode);
        }

        // The only place the panel, caption and refresh loop are set, all derived from the mode.
        private void ApplyMode()
        {
            bool visible = _mode != StudentViewMode.Hidden && isActiveAndEnabled;
            _panel.SetActive(visible);
            if (!visible)
            {
                StopRefreshing();
                return;
            }
            StudentViewSource source = StudentViewSourceRule.Choose(_mode, StudentPresent());
            if (_caption != null)
                _caption.text = CaptionFor(source);
            if (_refreshing == null)
                _refreshing = StartCoroutine(Refresh());
            Render(source, force: true);
        }

        private string CaptionFor(StudentViewSource source)
        {
            switch (source)
            {
                case StudentViewSource.LiveStudent: return "Live: through the student's eyes";
                case StudentViewSource.LiveNoStudent: return "Student not in the room";
                default:
                    QuestionPin pin = _board[IndexOf(_selectedId)];
                    return PinLabels.ForPin($"Classmate {pin.AuthorClientId}: {pin.Text}", pin.Answered);
            }
        }

        private IEnumerator Refresh()
        {
            var pinWait = new WaitForSeconds(_refreshSeconds);
            var liveWait = new WaitForSeconds(StudentViewRenderRule.IntervalSeconds(_liveRendersPerSecond));
            while (true)
            {
                yield return _mode == StudentViewMode.Live ? liveWait : pinWait;
                Render(StudentViewSourceRule.Choose(_mode, StudentPresent()), force: false);
            }
        }

        private void Render(StudentViewSource source, bool force)
        {
            switch (source)
            {
                case StudentViewSource.Pin:
                    int index = IndexOf(_selectedId);
                    if (index >= 0)
                        RenderPin(_board[index]);
                    break;
                case StudentViewSource.LiveStudent:
                    if (force || StudentViewRenderRule.ShouldRender(Time.unscaledTime, _lastRenderAt, _liveRendersPerSecond))
                        RenderLive();
                    break;
                case StudentViewSource.LiveNoStudent:
                    if (force)
                        ClearView();
                    break;
            }
        }

        private void RenderPin(QuestionPin pin)
        {
            Transform body = _board.transform;
            _viewCamera.transform.SetPositionAndRotation(body.TransformPoint(pin.ViewPosition), body.rotation * pin.ViewRotation);
            _viewCamera.Render();
        }

        // The head pose is in the body's local space; this device's own body turns it into world space.
        private void RenderLive()
        {
            if (!_headBoard.TryGetLocalPose(PointerRole.Student, out Vector3 localPosition, out Quaternion localRotation))
                return;
            Transform body = _headBoard.transform;
            _viewCamera.transform.SetPositionAndRotation(body.TransformPoint(localPosition), body.rotation * localRotation);
            _viewCamera.Render();
            _lastRenderAt = Time.unscaledTime;
        }

        // Shows black instead of the last picture when there is no student.
        private void ClearView()
        {
            RenderTexture target = _viewCamera.targetTexture;
            if (target == null)
                return;
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            GL.Clear(true, true, Color.black);
            RenderTexture.active = previous;
        }

        private void StopRefreshing()
        {
            if (_refreshing == null)
                return;
            StopCoroutine(_refreshing);
            _refreshing = null;
        }

        private int NewestWithView()
        {
            for (int i = _board.Count - 1; i >= 0; i--)
                if (_board[i].HasView)
                    return _board[i].Id;
            return -1;
        }

        private int IndexOf(int pinId)
        {
            if (pinId < 0)
                return -1;
            for (int i = 0; i < _board.Count; i++)
                if (_board[i].Id == pinId)
                    return i;
            return -1;
        }
    }
}
