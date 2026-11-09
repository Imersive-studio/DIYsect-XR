// Authority: none. Local client behavior that joins an existing room or asks the lobby service for a new one.
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Creates the professor's room after authentication. The professor must be the host: question answering and quiz
    /// controls are server-only. Never joins a public room owned by another device. Failed creates are retried at the
    /// configured interval (a zero attempt limit means keep trying).
    /// </summary>
    public class ProfessorAutoHost : MonoBehaviour
    {
        [SerializeField, Tooltip("Name of the room created when none exists.")]
        private string _roomName = "DIYsect class";
        [SerializeField, Min(0)] private int _maxAttempts = 0;
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

            for (int attempt = 1; (_maxAttempts == 0 || attempt <= _maxAttempts) && !RoomJoiner.Connected; attempt++)
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
