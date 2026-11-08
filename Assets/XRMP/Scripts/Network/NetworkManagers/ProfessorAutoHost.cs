// Authority: none. Local client behavior that asks the lobby service for a new lobby and hosts it.
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Creates the class's room on its own as soon as the professor's headset has signed in, so nobody has to find the
    /// lobby button in the VR menu. The classmates' <see cref="ClassmateAutoJoin"/> then finds this room. It always
    /// creates a new room instead of joining an old one, so a leftover test room cannot take the professor's place.
    /// If creating fails it tries again a few times, spaced out for the lobby service's rate limit.
    /// </summary>
    public class ProfessorAutoHost : MonoBehaviour
    {
        [SerializeField, Tooltip("Name of the room classmates join.")]
        private string _roomName = "DIYsect class";
        [SerializeField, Min(0)] private int _maxAttempts = 3;
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
            for (int attempt = 1; attempt <= _maxAttempts && !XRINetworkGameManager.Connected.Value; attempt++)
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
