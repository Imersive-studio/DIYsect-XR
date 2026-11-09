// Authority: none. Presentation only; it reads the connection state and shows text, and changes no shared state.
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// A banner on the classmate's screen that says when the connection to the room is lost and while it is being restored, and
    /// gives a friendly sentence when a join fails (wrong code, full room). It listens to the connection state and the failure
    /// callbacks, so nothing runs every frame. Hidden while connected.
    /// </summary>
    public class ReconnectBanner : MonoBehaviour
    {
        [SerializeField, Tooltip("The panel that is shown and hidden.")]
        private GameObject _panel;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private ClassmateRejoin _rejoin;

        private readonly ReconnectBannerModel _model = new ReconnectBannerModel();
        private bool _started;
        private LobbyManager _lobby;
        private XRINetworkGameManager _manager;

        /// <summary>True while the banner is on screen.</summary>
        public bool IsShowing => _panel != null && _panel.activeSelf;

        /// <summary>The text on screen, or empty.</summary>
        public string CurrentMessage => IsShowing ? _label.text : string.Empty;

        private void Awake()
        {
            if (_panel == null || _label == null || _rejoin == null)
            {
                Debug.LogError($"{name}: ReconnectBanner needs a panel, a Text and a ClassmateRejoin. Run Tools > DIYsect > Add Reconnect Banner.", this);
                enabled = false;
                return;
            }
            _panel.SetActive(false);
        }

        private void OnEnable()
        {
            XRINetworkGameManager.CurrentConnectionState.SubscribeAndUpdate(OnState);
            XRINetworkGameManager.Connected.SubscribeAndUpdate(OnConnected);
            _rejoin.GaveUp += OnGaveUp;
            if (_started)
                HookFailures();
        }

        private void Start()
        {
            // The network manager sets itself up in its own Awake, so it may not exist yet in OnEnable.
            _started = true;
            HookFailures();
        }

        private void OnDisable()
        {
            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnState);
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            if (_rejoin != null)
                _rejoin.GaveUp -= OnGaveUp;
            UnhookFailures();
        }

        private void HookFailures()
        {
            UnhookFailures();
            _manager = XRINetworkGameManager.Instance;
            if (_manager == null)
                return;
            _manager.connectionFailedAction += OnFailureText;
            _lobby = _manager.lobbyManager;
            if (_lobby != null)
                _lobby.OnLobbyFailed += OnFailureText;
        }

        private void UnhookFailures()
        {
            if (_manager != null)
                _manager.connectionFailedAction -= OnFailureText;
            if (_lobby != null)
                _lobby.OnLobbyFailed -= OnFailureText;
            _manager = null;
            _lobby = null;
        }

        private void OnConnected(bool connected)
        {
            _model.SetConnected(connected);
            Refresh();
        }

        private void OnState(XRINetworkGameManager.ConnectionState state)
        {
            _model.SetState(state);
            Refresh();
        }

        private void OnGaveUp()
        {
            _model.SetGaveUp(true);
            Refresh();
        }

        /// <summary>Takes the text of a failed join, as the lobby code reports it.</summary>
        public void OnFailureText(string raw)
        {
            _model.SetFailure(JoinFailureMessages.Classify(raw));
            Refresh();
        }

        private void Refresh()
        {
            _label.text = _model.Text;
            _panel.SetActive(_model.Visible);
        }
    }
}
