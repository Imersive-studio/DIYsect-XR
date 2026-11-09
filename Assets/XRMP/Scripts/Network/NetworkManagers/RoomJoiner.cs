// Authority: none. Local client behavior that asks the lobby service for an existing room and joins it.
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// One attempt to find and join an existing room, shared by <see cref="ClassmateAutoJoin"/> and
    /// <see cref="ProfessorAutoHost"/>. A room that was found but could not be joined (its host never reached the relay,
    /// which leaves a room that is listed but dead) is remembered for a minute and skipped, so it cannot hide a working room.
    /// </summary>
    public class RoomJoiner
    {
        private const float SkipSeconds = 60f;
        private const float JoinTimeoutSeconds = 15f;

        private readonly Dictionary<string, float> _skipUntil = new Dictionary<string, float>();
        private readonly string _tag;

        public RoomJoiner(string tag)
        {
            _tag = tag;
        }

        /// <summary>True once the client is in a room.</summary>
        public static bool Connected => XRINetworkGameManager.Connected.Value;

        /// <summary>Looks once for a room and joins it. Ends when the attempt is over, joined or not; check <see cref="Connected"/>.</summary>
        public IEnumerator TryJoinOnce()
        {
            XRINetworkGameManager manager = XRINetworkGameManager.Instance;
            if (manager == null || XRINetworkGameManager.CurrentConnectionState.Value != XRINetworkGameManager.ConnectionState.Authenticated)
                yield break;

            Task<Lobby> search = manager.lobbyManager.FindJoinableLobbyAsync(ActiveSkips());
            while (!search.IsCompleted)
                yield return null;

            if (search.IsFaulted)
            {
                Debug.LogWarning($"[{_tag}] Lobby search failed: {search.Exception?.GetBaseException().Message}");
                yield break;
            }
            if (search.Result == null)
            {
                Debug.Log($"[{_tag}] No room yet.");
                yield break;
            }

            Lobby lobby = search.Result;
            Debug.Log($"[{_tag}] Joining room {lobby.Name}.");
            manager.JoinLobbySpecific(lobby);

            // The lobby join is quick; the network connection to the host takes a few seconds more. The state reads
            // Connected as soon as the lobby is joined, so success is the network being connected, not the state.
            // A join that fails puts the state back at Authenticated, after the short moment the join needs to start.
            float started = Time.unscaledTime;
            float deadline = started + JoinTimeoutSeconds;
            yield return null;
            while (!Connected && Time.unscaledTime < deadline)
            {
                bool failedFast = Time.unscaledTime - started > 2f
                    && XRINetworkGameManager.CurrentConnectionState.Value == XRINetworkGameManager.ConnectionState.Authenticated;
                if (failedFast)
                    break;
                yield return null;
            }

            if (!Connected)
            {
                Debug.LogWarning($"[{_tag}] Could not join {lobby.Name} ({lobby.Id}); skipping it for {SkipSeconds:0} s.");
                _skipUntil[lobby.Id] = Time.unscaledTime + SkipSeconds;
                // Leave the lobby, so the next room can be tried: a half-joined client would not look again.
                if (XRINetworkGameManager.CurrentConnectionState.Value != XRINetworkGameManager.ConnectionState.Authenticated)
                {
                    Task<bool> left = manager.DisconnectAsync();
                    while (!left.IsCompleted)
                        yield return null;
                }
            }
        }

        private ICollection<string> ActiveSkips()
        {
            var active = new List<string>();
            var expired = new List<string>();
            foreach (KeyValuePair<string, float> entry in _skipUntil)
            {
                if (entry.Value > Time.unscaledTime)
                    active.Add(entry.Key);
                else
                    expired.Add(entry.Key);
            }
            foreach (string id in expired)
                _skipUntil.Remove(id);
            return active;
        }
    }
}
