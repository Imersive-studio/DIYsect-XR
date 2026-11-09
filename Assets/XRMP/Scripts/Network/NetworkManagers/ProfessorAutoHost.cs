// Authority: none. Local client behavior that joins an existing room or asks the lobby service for a new one.
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Gets the professor's headset into the class's room on its own, so nobody has to find the lobby button in the VR
    /// menu. Once signed in it first looks for a room for a few seconds and joins one if there is one (a laptop or
    /// editor hosting the class); otherwise it creates the room itself and the classmates' <see cref="ClassmateAutoJoin"/>
    /// finds it. If creating fails it tries again, spaced out for the lobby service's rate limit.
    /// </summary>
    public class ProfessorAutoHost : MonoBehaviour
    {
        [SerializeField, Tooltip("Name of the room created when none exists.")]
        private string _roomName = "DIYsect class";
        [SerializeField, Min(0f), Tooltip("Seconds to look for an existing room before creating one.")]
        private float _lookForRoomSeconds = 10f;
        [SerializeField, Min(0)] private int _maxAttempts = 1;
        [SerializeField, Min(6f), Tooltip("Seconds between attempts. The lobby service allows two creates per six seconds.")]
        private float _retrySeconds = 8f;

        private Coroutine _loop;

        private void OnEnable()
        {
            XRINetworkGameManager.CurrentConnectionState.Subscribe(OnConnectionState);
        }

        private void OnDisable()
        {
            XRINetworkGameManager.CurrentConnectionState.Unsubscribe(OnConnectionState);
            if (_loop != null)
            {
                StopCoroutine(_loop);
                _loop = null;
            }
        }

        private void OnConnectionState(XRINetworkGameManager.ConnectionState state)
        {
            if (state == XRINetworkGameManager.ConnectionState.Authenticated && _loop == null)
                _loop = StartCoroutine(HostLoop());
        }

        private IEnumerator HostLoop()
        {
            yield return new WaitForSeconds(1f);

            var joiner = new RoomJoiner("ProfessorAutoHost");
            float lookUntil = Time.unscaledTime + _lookForRoomSeconds;
            while (!RoomJoiner.Connected && Time.unscaledTime < lookUntil)
            {
                yield return joiner.TryJoinOnce();
                if (!RoomJoiner.Connected)
                    yield return new WaitForSeconds(2f);
            }

            for (int attempt = 1; attempt <= _maxAttempts && !RoomJoiner.Connected; attempt++)
            {
                XRINetworkGameManager manager = XRINetworkGameManager.Instance;
                if (manager != null && XRINetworkGameManager.CurrentConnectionState.Value == XRINetworkGameManager.ConnectionState.Authenticated)
                {
                    Debug.Log($"[ProfessorAutoHost] Creating room \"{_roomName}\" (attempt {attempt}).");
                    manager.CreateNewLobby(_roomName);
                }
                yield return new WaitForSeconds(_retrySeconds);
            }
            _loop = null;
        }
    }
}
