// Authority: none. Presentation only; it draws what ChecklistTracker reports.
using UnityEngine;

namespace DIYsect.Learning
{
    /// <summary>
    /// The world-space checklist in a headset: a TextMesh beside the table that lists every step with [x], [>] or [ ].
    /// It redraws when <see cref="ChecklistTracker"/> reports a change and never polls.
    /// </summary>
    public class ChecklistPanel : MonoBehaviour
    {
        [SerializeField] private ChecklistTracker _tracker;
        [SerializeField] private TextMesh _text;

        /// <summary>The panel text now.</summary>
        public string Text => _text != null ? _text.text : string.Empty;

        private void Awake()
        {
            if (_tracker == null || _text == null)
            {
                Debug.LogError($"{name}: ChecklistPanel needs a ChecklistTracker and a TextMesh. Run Tools > DIYsect > Add Dissection Checklist.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            _tracker.StateChanged += Refresh;
            Refresh(_tracker.State);
        }

        private void OnDisable()
        {
            if (_tracker != null)
                _tracker.StateChanged -= Refresh;
        }

        private void Refresh(ChecklistState state)
        {
            _text.text = ChecklistLabels.Format(state);
        }
    }
}
