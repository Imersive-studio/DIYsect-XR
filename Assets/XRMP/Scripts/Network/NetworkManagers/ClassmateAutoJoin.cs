// Authority: none. Local client behavior that only asks the lobby service for an existing lobby and joins it.
using System.Collections;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Joins the professor's lobby on its own, so a classmate on a phone or laptop never needs the headset menu.
    /// Once the client has signed in it looks for an existing lobby every few seconds and joins the first one.
    /// It never creates a lobby, so the professor's headset stays the host.
    /// </summary>
    public class ClassmateAutoJoin : MonoBehaviour
    {
        [SerializeField, Min(2f), Tooltip("Seconds between looks for a lobby. The lobby service allows about one query a second.")]
        private float _retrySeconds = 5f;

        [SerializeField, Tooltip("Also join on its own in the Unity editor. Off by default, so the editor can host a test room instead of joining a leftover one.")]
        private bool _runInEditor;

        private Coroutine _loop;

        private void OnEnable()
        {
            if (Application.isEditor && !_runInEditor)
            {
                enabled = false;
                return;
            }
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
                _loop = StartCoroutine(JoinLoop());
        }

        private IEnumerator JoinLoop()
        {
            var joiner = new RoomJoiner("ClassmateAutoJoin");
            var wait = new WaitForSeconds(_retrySeconds);
            while (!RoomJoiner.Connected)
            {
                yield return joiner.TryJoinOnce();
                yield return wait;
            }
            _loop = null;
        }
    }
}
