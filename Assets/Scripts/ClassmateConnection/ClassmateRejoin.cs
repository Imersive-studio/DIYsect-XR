// Authority: none. Local client behavior; it only asks ClassmateAutoJoin to join the same room code again.
using System;
using System.Collections;
using UnityEngine;
using XRMultiplayer;

namespace DIYsect.ClassmateConnection
{
    /// <summary>
    /// Brings a classmate back to the room they were in. <see cref="ClassmateAutoJoin"/> already looks again once the connection
    /// drops, but it forgets the room code after one try, so a short outage (a phone changing from wifi to mobile data) would
    /// leave the classmate out. This remembers the code of the room the classmate was in and hands it back to
    /// <see cref="ClassmateAutoJoin.JoinWithCode"/> every few seconds until they are in again or <c>_giveUpSeconds</c> have passed.
    /// Netcode then sends the body, pins and pointers to the returning client as it does for any late joiner.
    /// </summary>
    public class ClassmateRejoin : MonoBehaviour
    {
        [SerializeField] private ClassmateAutoJoin _join;
        [SerializeField, Min(0.1f), Tooltip("Seconds between tries. The lobby service allows about one query a second.")]
        private float _retrySeconds = 5f;
        [SerializeField, Min(1f), Tooltip("Seconds of trying before the banner tells the classmate it could not reconnect.")]
        private float _giveUpSeconds = 120f;

        private string _roomCode = string.Empty;
        private Coroutine _retry;

        /// <summary>Raised when the time ran out and the classmate is still out of the room.</summary>
        public event Action GaveUp;

        /// <summary>The code that will be used to rejoin, or empty before the classmate has been in a room.</summary>
        public string RoomCode => _roomCode;

        private void Awake()
        {
            if (_join == null)
            {
                Debug.LogError($"{name}: ClassmateRejoin needs the scene's ClassmateAutoJoin. Run Tools > DIYsect > Add Reconnect Banner.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            XRINetworkGameManager.Connected.SubscribeAndUpdate(OnConnected);
        }

        private void OnDisable()
        {
            XRINetworkGameManager.Connected.Unsubscribe(OnConnected);
            StopRetry();
        }

        private void OnConnected(bool connected)
        {
            if (connected)
            {
                StopRetry();
                if (RoomInvite.TryNormalizeCode(XRINetworkGameManager.ConnectedRoomCode, out string code))
                    _roomCode = code;
                return;
            }
            if (_roomCode.Length == 0 || _retry != null)
                return;
            // Asked here, before ClassmateAutoJoin's own loop starts on the state change that follows, so its first look is for this room.
            _join.JoinWithCode(_roomCode);
            _retry = StartCoroutine(RetryLoop());
        }

        private void StopRetry()
        {
            if (_retry == null)
                return;
            StopCoroutine(_retry);
            _retry = null;
        }

        private IEnumerator RetryLoop()
        {
            float giveUpAt = Time.unscaledTime + _giveUpSeconds;
            var wait = new WaitForSecondsRealtime(_retrySeconds);
            while (!XRINetworkGameManager.Connected.Value)
            {
                yield return wait;
                if (XRINetworkGameManager.Connected.Value)
                    break;
                if (Time.unscaledTime >= giveUpAt)
                {
                    GaveUp?.Invoke();
                    break;
                }
                bool idle = _join.PendingCode.Length == 0
                    && XRINetworkGameManager.CurrentConnectionState.Value == XRINetworkGameManager.ConnectionState.Authenticated;
                if (!idle)
                    continue;
                // Leave the old seat in the lobby first, or the lobby may refuse a second join by the same player.
                XRINetworkGameManager manager = XRINetworkGameManager.Instance;
                if (manager != null)
                {
                    var left = manager.DisconnectAsync();
                    while (!left.IsCompleted)
                        yield return null;
                }
                _join.JoinWithCode(_roomCode);
            }
            _retry = null;
        }
    }
}
