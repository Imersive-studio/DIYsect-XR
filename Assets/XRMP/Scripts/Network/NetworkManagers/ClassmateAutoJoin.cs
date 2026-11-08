// Authority: none. Local client behavior that only asks the lobby service for an existing lobby and joins it.
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
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
                _loop = StartCoroutine(JoinLoop());
        }

        private IEnumerator JoinLoop()
        {
            var wait = new WaitForSeconds(_retrySeconds);
            while (!XRINetworkGameManager.Connected.Value)
            {
                XRINetworkGameManager manager = XRINetworkGameManager.Instance;
                // Only look while signed in and idle: Connecting means a join is already under way.
                if (manager != null && XRINetworkGameManager.CurrentConnectionState.Value == XRINetworkGameManager.ConnectionState.Authenticated)
                {
                    Task<Lobby> search = manager.lobbyManager.FindJoinableLobbyAsync();
                    while (!search.IsCompleted)
                        yield return null;
                    if (search.IsCompletedSuccessfully && search.Result != null)
                    {
                        Debug.Log($"[ClassmateAutoJoin] Joining lobby {search.Result.Name}.");
                        manager.JoinLobbySpecific(search.Result);
                    }
                    else if (search.IsFaulted)
                    {
                        Debug.LogWarning($"[ClassmateAutoJoin] Lobby search failed: {search.Exception?.GetBaseException().Message}");
                    }
                    else
                    {
                        Debug.Log("[ClassmateAutoJoin] No lobby yet. Waiting for the professor.");
                    }
                }
                yield return wait;
            }
            _loop = null;
        }
    }
}
