// Authority: none. Local UI only; it asks the ClassmateFollowCamera, which moves nothing but the classmate's own camera.
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The three on-screen buttons of the follow camera: "Follow professor", "Follow student" and "Back to my view".
    /// A follow button is switched off (and says so) while that person is not in the session or is already being followed.
    /// "Back to my view" shows only while following. They are UI elements, so the PointerGestureTracker ignores a press on
    /// them and they can never place a pin.
    /// </summary>
    public class ClassmateFollowButtons : MonoBehaviour
    {
        [SerializeField] private ClassmateFollowCamera _follow;
        [SerializeField] private Button _professorButton;
        [SerializeField] private Text _professorLabel;
        [SerializeField] private Button _studentButton;
        [SerializeField] private Text _studentLabel;
        [SerializeField] private Button _backButton;
        [SerializeField, Min(0.1f), Tooltip("Seconds the 'person left' message stays on the button.")]
        private float _messageSeconds = 3f;

        private string _message = string.Empty;
        private float _messageUntil;

        private void Awake()
        {
            bool valid = true;
            if (_follow == null) { Debug.LogError($"{name}: ClassmateFollowButtons has no ClassmateFollowCamera. Run Tools > DIYsect > Add Classmate Desktop Camera.", this); valid = false; }
            if (_professorButton == null || _professorLabel == null) { Debug.LogError($"{name}: ClassmateFollowButtons has no professor button or label.", this); valid = false; }
            if (_studentButton == null || _studentLabel == null) { Debug.LogError($"{name}: ClassmateFollowButtons has no student button or label.", this); valid = false; }
            if (_backButton == null) { Debug.LogError($"{name}: ClassmateFollowButtons has no back button.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        private void OnEnable()
        {
            _professorButton.onClick.AddListener(FollowProfessor);
            _studentButton.onClick.AddListener(FollowStudent);
            _backButton.onClick.AddListener(Back);
            _follow.Changed += Refresh;
            _follow.Message += ShowMessage;
            Refresh();
        }

        private void OnDisable()
        {
            // The buttons can be destroyed first when the scene unloads.
            if (_professorButton != null) _professorButton.onClick.RemoveListener(FollowProfessor);
            if (_studentButton != null) _studentButton.onClick.RemoveListener(FollowStudent);
            if (_backButton != null) _backButton.onClick.RemoveListener(Back);
            if (_follow != null)
            {
                _follow.Changed -= Refresh;
                _follow.Message -= ShowMessage;
            }
        }

        private void Update()
        {
            if (_message.Length > 0 && Time.unscaledTime >= _messageUntil)
            {
                _message = string.Empty;
                Refresh();
            }
        }

        public void FollowProfessor() => _follow.Follow(FollowTarget.Professor);

        public void FollowStudent() => _follow.Follow(FollowTarget.Student);

        public void Back() => _follow.BackToMyView();

        private void ShowMessage(string message)
        {
            _message = message;
            _messageUntil = Time.unscaledTime + _messageSeconds;
            Refresh();
        }

        private void Refresh()
        {
            Set(_professorButton, _professorLabel, FollowTarget.Professor, "professor");
            Set(_studentButton, _studentLabel, FollowTarget.Student, "student");
            _backButton.gameObject.SetActive(_follow.IsFollowing);
        }

        private void Set(Button button, Text label, FollowTarget target, string word)
        {
            bool following = _follow.Target == target;
            bool available = _follow.IsAvailable(target);
            string left = target == FollowTarget.Professor ? "Professor left" : "Student left";
            button.interactable = available && !following;
            if (following)
                label.text = $"Following {word}";
            else if (!available)
                label.text = _message == left ? _message : $"No {word} yet";
            else
                label.text = $"Follow {word}";
        }
    }
}
