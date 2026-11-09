// Authority: none. Local client behavior that only asks the lobby service for an existing lobby and joins it.
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Gets a classmate into the professor's room, so a phone or laptop never needs the headset menu. In order:
    /// 1. A room code from an invite link (a tap on <c>diysect://join?code=KHC8QH</c>) or one typed by the classmate wins.
    ///    It works for private rooms too.
    /// 2. Otherwise, when <see cref="AutoJoinAnyRoom"/> is on, it looks for an existing room every few seconds and joins the first one.
    /// It never creates a room, so the professor's headset stays the host. A room code that finds no room is reported
    /// through <see cref="CodeRejected"/> so the screen can say so.
    /// </summary>
    public class ClassmateAutoJoin : MonoBehaviour
    {
        [SerializeField, Min(2f), Tooltip("Seconds between looks for a lobby. The lobby service allows about one query a second.")]
        private float _retrySeconds = 5f;

        [SerializeField, Tooltip("Also join on its own in the Unity editor. Off by default, so the editor can host a test room instead of joining a leftover one.")]
        private bool _runInEditor;

        [SerializeField, Tooltip("Join the first public room found when no code was given. Turn off to make every classmate use the code or the link, like a meeting invite.")]
        private bool _autoJoinAnyRoom = true;

        [SerializeField, Tooltip("Wait for the classmate to press Join on the start screen before looking for a room. An invite link or a typed code counts as pressing it. Off keeps the old behaviour: join on its own.")]
        private bool _waitForJoinPress;

        private Coroutine _loop;
        private string _pendingCode = string.Empty;
        private bool _explicitCodeRequested;

        /// <summary>Join the first public room found when no code was given.</summary>
        public bool AutoJoinAnyRoom
        {
            get => _autoJoinAnyRoom;
            set => _autoJoinAnyRoom = value;
        }

        /// <summary>Public-room discovery is suppressed after any explicit code submission, including an invalid one.</summary>
        public bool ShouldAutoJoinAnyRoom => _autoJoinAnyRoom && !_explicitCodeRequested;

        /// <summary>When on, nothing is joined until <see cref="JoinPressed"/> (see <see cref="PressJoin"/>). Off by default, so other scenes join on their own.</summary>
        public bool WaitForJoinPress
        {
            get => _waitForJoinPress;
            set => _waitForJoinPress = value;
        }

        /// <summary>True once the classmate asked to join: the start screen button, a typed code, or an invite link.</summary>
        public bool JoinPressed { get; private set; }

        /// <summary>The classmate pressed Join on the start screen: join the first public room, or the code if one was given.</summary>
        public void PressJoin()
        {
            JoinPressed = true;
            CodeRejected = false;
        }

        /// <summary>The room code waiting to be used, or empty.</summary>
        public string PendingCode => _pendingCode;

        /// <summary>True when the last room code found no room. Cleared by the next code.</summary>
        public bool CodeRejected { get; private set; }

        /// <summary>Asks to join the room with this code, typed by the classmate or taken from a link. Returns false when it is not a valid code.</summary>
        public bool JoinWithCode(string typedCode)
        {
            _explicitCodeRequested = true;
            if (!RoomInvite.TryNormalizeCode(typedCode, out string code))
                return false;
            _pendingCode = code;
            CodeRejected = false;
            JoinPressed = true;
            return true;
        }

        private void OnEnable()
        {
            if (Application.isEditor && !_runInEditor)
            {
                enabled = false;
                return;
            }
            // The app was opened by tapping an invite link, or a link arrives while it is open.
            TakeCodeFromLink(Application.absoluteURL);
            Application.deepLinkActivated += TakeCodeFromLink;
            XRINetworkGameManager.CurrentConnectionState.Subscribe(OnConnectionState);
        }

        private void OnDisable()
        {
            Application.deepLinkActivated -= TakeCodeFromLink;
            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionState);
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private void TakeCodeFromLink(string link)
        {
            if (RoomInvite.TryParseLink(link, out string code))
            {
                Debug.Log($"[ClassmateAutoJoin] Invite link opened for room code {code}.");
                JoinWithCode(code);
            }
        }

        private void OnConnectionState(XRINetworkGameManager.ConnectionState state)
        {
            if (state == XRINetworkGameManager.ConnectionState.Authenticated && _loop == null)
                _loop = StartCoroutine(JoinLoop());
        }

        private IEnumerator JoinLoop()
        {
            var joiner = new RoomJoiner("ClassmateAutoJoin");
            var wait = new WaitForSeconds(_retrySeconds);
            while (!RoomJoiner.Connected)
            {
                if (_waitForJoinPress && !JoinPressed)
                {
                    yield return null;
                    continue;
                }
                if (_pendingCode.Length > 0)
                {
                    string code = _pendingCode;
                    yield return joiner.TryJoinByCode(code);
                    // A code that found no room is dropped, so the screen can say so and the classmate can try another.
                    if (!RoomJoiner.Connected && _pendingCode == code)
                    {
                        _pendingCode = string.Empty;
                        CodeRejected = joiner.LastCodeRejected;
                    }
                }
                else if (ShouldAutoJoinAnyRoom)
                {
                    yield return joiner.TryJoinOnce(() => ShouldAutoJoinAnyRoom);
                }
                yield return _pendingCode.Length > 0 ? null : wait;
            }
            _loop = null;
        }
    }
}
