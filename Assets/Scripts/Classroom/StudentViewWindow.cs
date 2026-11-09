// Authority: none. Local presentation for the professor. It reads the server-owned pins in QuestionBoard.
using System.Collections;
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
    /// </summary>
    public class StudentViewWindow : MonoBehaviour
    {
        [SerializeField] private QuestionBoard _board;
        [SerializeField] private Camera _viewCamera;
        [SerializeField] private GameObject _panel;
        [SerializeField] private TextMesh _caption;
        [SerializeField, Min(0.05f)] private float _refreshSeconds = 0.2f;

        private int _selectedId = -1;
        private StudentViewMode _mode = StudentViewMode.Hidden;
        private Coroutine _refreshing;

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
            HandlePinsChanged();
        }

        private void OnDisable()
        {
            if (_board != null)
                _board.PinsChanged -= HandlePinsChanged;
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
            int index = IndexOf(_selectedId);
            if (_mode == StudentViewMode.Pinned && index >= 0 && _board[index].HasView)
                SetMode(StudentViewMode.Pinned, _selectedId);
            else
                ShowNewest();
        }

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
            _selectedId = mode == StudentViewMode.Hidden ? -1 : pinId;
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
            QuestionPin pin = _board[IndexOf(_selectedId)];
            if (_caption != null)
                _caption.text = PinLabels.ForPin($"Classmate {pin.AuthorClientId}: {pin.Text}", pin.Answered);
            if (_refreshing == null)
                _refreshing = StartCoroutine(Refresh());
            RenderView(pin);
        }

        private IEnumerator Refresh()
        {
            var wait = new WaitForSeconds(_refreshSeconds);
            while (true)
            {
                yield return wait;
                int index = IndexOf(_selectedId);
                if (index >= 0)
                    RenderView(_board[index]);
            }
        }

        private void RenderView(QuestionPin pin)
        {
            Transform body = _board.transform;
            _viewCamera.transform.SetPositionAndRotation(body.TransformPoint(pin.ViewPosition), body.rotation * pin.ViewRotation);
            _viewCamera.Render();
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
