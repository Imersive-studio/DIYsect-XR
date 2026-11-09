// Authority: none. Local UI only; it shows or hides the classmate's bottom buttons.
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.ClassmateControls
{
    /// <summary>
    /// The row of buttons along the bottom of the classmate's screen (Mute, Raise hand). It shows only while the classmate is
    /// in the room, because neither button means anything outside it. The classmate camera (and with it this bar) is switched
    /// off on the professor's headset by ClassroomRole.
    /// </summary>
    public class ClassmateBottomBar : MonoBehaviour
    {
        [SerializeField, Tooltip("The row that is shown and hidden.")]
        private GameObject _bar;
        [SerializeField, Tooltip("The scene's network manager. Leave empty to use NetworkManager.Singleton.")]
        private NetworkManager _network;
        [SerializeField, Min(0.05f), Tooltip("Seconds between looks at the connection.")]
        private float _checkSeconds = 0.25f;

        private float _nextCheck;

        /// <summary>True while the bar is on screen.</summary>
        public bool IsShowing => _bar != null && _bar.activeSelf;

        private void Awake()
        {
            if (_bar == null)
            {
                Debug.LogError($"{name}: ClassmateBottomBar has no bar. Run Tools > DIYsect > Build Shared Scene.", this);
                enabled = false;
                return;
            }
            _bar.SetActive(false);
        }

        private void OnEnable()
        {
            _nextCheck = 0f;
        }

        // The connection has no change event that exists before the network manager does, so this looks a few times a second.
        private void Update()
        {
            if (Time.unscaledTime < _nextCheck)
                return;
            _nextCheck = Time.unscaledTime + _checkSeconds;
            NetworkManager network = _network != null ? _network : NetworkManager.Singleton;
            // A host (the editor's test room) is in its own room; a client is in once the server has accepted it.
            bool inRoom = network != null && network.IsListening && (network.IsConnectedClient || network.IsHost);
            if (_bar.activeSelf != inRoom)
                _bar.SetActive(inRoom);
        }
    }
}
