// Authority: none. Local UI only; it asks the server's HandBoard to raise or lower this classmate's hand, and the server decides.
using DIYsect.Classroom;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// The classmate's Raise hand / Lower hand button. The label follows the server's list, so it reads right after a
    /// reconnect and when the professor lowers the hand from the headset. It is a UI element, so the gesture tracker ignores a press on it.
    /// </summary>
    public class ClassmateHandButton : MonoBehaviour
    {
        [SerializeField] private HandBoard _hands;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;

        /// <summary>The text on the button.</summary>
        public string Label => _label.text;

        private void Awake()
        {
            bool valid = true;
            if (_hands == null) { Debug.LogError($"{name}: ClassmateHandButton has no HandBoard. Run Tools > DIYsect > Build Shared Scene.", this); valid = false; }
            if (_button == null || _label == null) { Debug.LogError($"{name}: ClassmateHandButton has no button or label.", this); valid = false; }
            if (!valid)
                enabled = false;
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(Press);
            _hands.Changed += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            // The button and the board can be destroyed first when the scene unloads.
            if (_button != null)
                _button.onClick.RemoveListener(Press);
            if (_hands != null)
                _hands.Changed -= Refresh;
        }

        /// <summary>Raises the hand if it is down, lowers it if it is up.</summary>
        public void Press()
        {
            if (_hands.IsSpawned)
                _hands.RequestSet(!_hands.IsLocalRaised);
        }

        private void Refresh()
        {
            _label.text = HandButtonLabels.For(_hands.IsSpawned ? _hands.OrderOf(_hands.NetworkManager.LocalClientId) : 0);
        }
    }
}
