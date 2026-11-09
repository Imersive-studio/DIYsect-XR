// Authority: the server. It validates every request and is the only writer of the list of raised hands.
// Clients ask by Rpc and receive the list through a NetworkList, in the order the hands went up. Late joiners get the whole list on spawn.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The class's raised hands. A classmate raises or lowers their own hand; the professor (the host) can lower anyone's, or
    /// all of them. A person who disconnects loses their hand. Each person may change their hand a limited number of times in a
    /// short window, so a held-down button cannot flood the room.
    /// </summary>
    public class HandBoard : NetworkBehaviour
    {
        [SerializeField, Min(1), Tooltip("Hands the board holds at most. Further requests are ignored.")]
        private int _maxHands = 64;
        [SerializeField, Min(1), Tooltip("Hand changes one client may make inside the window.")]
        private int _maxChangesPerWindow = 6;
        [SerializeField, Min(0.1f), Tooltip("Length of the rate limit window in seconds.")]
        private float _windowSeconds = 10f;

        private NetworkList<ulong> _hands;
        private HandQueue _queue;
        private PinRateLimiter _limiter;

        /// <summary>Raised on every peer when the list of hands changes.</summary>
        public event Action Changed;

        // A script that reads the board in its OnEnable can run before this Awake has made the list, so the board reads as empty.
        public int Count => _hands != null ? _hands.Count : 0;

        /// <summary>The person at a place in the queue, first to raise first.</summary>
        public ulong this[int index] => _hands[index];

        private void Awake()
        {
            _hands = new NetworkList<ulong>();
            _queue = new HandQueue(_maxHands);
            _limiter = new PinRateLimiter(_maxChangesPerWindow, _windowSeconds);
        }

        public override void OnNetworkSpawn()
        {
            _hands.OnListChanged += HandleListChanged;
            if (!IsServer)
            {
                // A joining client receives the existing hands without a change event, so announce them once.
                Changed?.Invoke();
                return;
            }
            _hands.Clear();
            _queue.Clear();
            NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
        }

        public override void OnNetworkDespawn()
        {
            _hands.OnListChanged -= HandleListChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>1 for the first hand up, 2 for the next, and so on. 0 when the person has no hand up.</summary>
        public int OrderOf(ulong clientId)
        {
            for (int i = 0; i < Count; i++)
                if (_hands[i] == clientId)
                    return i + 1;
            return 0;
        }

        public bool IsRaised(ulong clientId) => OrderOf(clientId) > 0;

        /// <summary>True when this device's own hand is up.</summary>
        public bool IsLocalRaised => IsSpawned && IsRaised(NetworkManager.LocalClientId);

        /// <summary>Asks the server to put this device's hand up or take it down.</summary>
        public void RequestSet(bool raised)
        {
            if (IsSpawned)
                SetHandRpc(raised);
        }

        /// <summary>Asks the server to take a hand down. Only the host is obeyed for another person's hand.</summary>
        public void RequestLower(ulong clientId)
        {
            if (IsSpawned)
                LowerRpc(clientId);
        }

        /// <summary>Asks the server to take every hand down. Only the host is obeyed.</summary>
        public void RequestLowerAll()
        {
            if (IsSpawned)
                LowerAllRpc();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetHandRpc(bool raised, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!_limiter.TryAccept(sender, Time.realtimeSinceStartup))
                return;
            if (raised)
            {
                if (_queue.Raise(sender))
                    _hands.Add(sender);
            }
            else
            {
                ServerLower(sender);
            }
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void LowerRpc(ulong clientId, RpcParams rpcParams = default)
        {
            if (HandRules.CanLower(rpcParams.Receive.SenderClientId, clientId))
                ServerLower(clientId);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void LowerAllRpc(RpcParams rpcParams = default)
        {
            if (!HandRules.CanLowerAll(rpcParams.Receive.SenderClientId))
                return;
            _queue.Clear();
            _hands.Clear();
        }

        private void ServerLower(ulong clientId)
        {
            if (_queue.Lower(clientId))
                _hands.Remove(clientId);
        }

        private void HandleDisconnect(ulong clientId)
        {
            _limiter.Forget(clientId);
            ServerLower(clientId);
        }

        private void HandleListChanged(NetworkListEvent<ulong> change)
        {
            Changed?.Invoke();
        }
    }
}
