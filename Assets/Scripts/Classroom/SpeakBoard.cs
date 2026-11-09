// Authority: the server. It validates every request and is the only writer of the list of people allowed to speak.
// Clients ask by Rpc and receive the list through a NetworkList. Late joiners get the whole list on spawn.
using System;
using Unity.Netcode;

namespace DIYsect.Classroom
{
    /// <summary>
    /// Who in the class may unmute their microphone. Everyone but the professor starts without permission. A classmate asks by
    /// raising a hand (<see cref="HandBoard"/>); the professor (the host) allows them, and can take it away again. A person who
    /// disconnects loses their permission. The classmate's mute button reads this list, so it cannot be used to speak without
    /// it. This stops the button, not the Vivox service itself: a modified build could still open its own microphone.
    /// </summary>
    public class SpeakBoard : NetworkBehaviour
    {
        private NetworkList<ulong> _allowed;

        /// <summary>Raised on every peer when the list changes.</summary>
        public event Action Changed;

        private void Awake()
        {
            _allowed = new NetworkList<ulong>();
        }

        public override void OnNetworkSpawn()
        {
            _allowed.OnListChanged += HandleListChanged;
            if (!IsServer)
            {
                // A joining client receives the existing list without a change event, so announce it once.
                Changed?.Invoke();
                return;
            }
            _allowed.Clear();
            NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            _allowed.OnListChanged -= HandleListChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>People currently allowed to speak, not counting the professor.</summary>
        public int Count => _allowed != null ? _allowed.Count : 0;

        /// <summary>True when this person may unmute. The professor (the host) always may.</summary>
        public bool IsAllowed(ulong clientId)
        {
            if (clientId == NetworkManager.ServerClientId)
                return true;
            if (_allowed == null)
                return false;
            for (int i = 0; i < _allowed.Count; i++)
                if (_allowed[i] == clientId)
                    return true;
            return false;
        }

        /// <summary>True when this device may unmute.</summary>
        public bool IsLocalAllowed => IsSpawned && IsAllowed(NetworkManager.LocalClientId);

        /// <summary>Asks the server to let a person speak. Only the host is obeyed.</summary>
        public void RequestAllow(ulong clientId)
        {
            if (IsSpawned)
                AllowRpc(clientId);
        }

        /// <summary>Asks the server to take a person's permission away. Only the host is obeyed.</summary>
        public void RequestRevoke(ulong clientId)
        {
            if (IsSpawned)
                RevokeRpc(clientId);
        }

        /// <summary>Asks the server to take everyone's permission away. Only the host is obeyed.</summary>
        public void RequestRevokeAll()
        {
            if (IsSpawned)
                RevokeAllRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AllowRpc(ulong clientId, RpcParams rpcParams = default)
        {
            if (!SpeakRules.CanChange(rpcParams.Receive.SenderClientId))
                return;
            if (clientId == NetworkManager.ServerClientId || ServerContains(clientId))
                return;
            // Only people who are in the room can be given permission.
            if (!NetworkManager.ConnectedClients.ContainsKey(clientId))
                return;
            _allowed.Add(clientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RevokeRpc(ulong clientId, RpcParams rpcParams = default)
        {
            if (!SpeakRules.CanChange(rpcParams.Receive.SenderClientId))
                return;
            ServerRemove(clientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RevokeAllRpc(RpcParams rpcParams = default)
        {
            if (SpeakRules.CanChange(rpcParams.Receive.SenderClientId))
                _allowed.Clear();
        }

        private bool ServerContains(ulong clientId)
        {
            for (int i = 0; i < _allowed.Count; i++)
                if (_allowed[i] == clientId)
                    return true;
            return false;
        }

        private void ServerRemove(ulong clientId)
        {
            for (int i = _allowed.Count - 1; i >= 0; i--)
                if (_allowed[i] == clientId)
                    _allowed.RemoveAt(i);
        }

        private void HandleDisconnect(ulong clientId)
        {
            ServerRemove(clientId);
        }

        private void HandleListChanged(NetworkListEvent<ulong> change)
        {
            Changed?.Invoke();
        }
    }
}
