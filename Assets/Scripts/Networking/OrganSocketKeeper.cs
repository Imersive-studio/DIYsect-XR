// Authority: the server. It judges every release and moves the organ. Clients only hear the result.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Every organ's home pose is a socket. When the server frees an organ, this checks where it was dropped
    /// with <see cref="SocketRule"/>:
    /// accepted organs are snapped to the exact home pose, organs dropped at another organ's socket are sent back
    /// to their own home pose (rejected), and organs dropped anywhere else stay where they are.
    /// Every peer is told the outcome by <see cref="SnapResolved"/> so it can show feedback.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority))]
    public class OrganSocketKeeper : NetworkBehaviour
    {
        [SerializeField, Min(0f), Tooltip("How close in metres the organ's centre must be to a socket's centre.")]
        private float _positionTolerance = 0.03f;
        [SerializeField, Range(0f, 180f), Tooltip("How far in degrees the organ may be turned from its home rotation to be accepted.")]
        private float _angleToleranceDegrees = 45f;

        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private ISnappable[] _snappables;
        private Pose[] _sockets;
        private SocketRule _rule;

        /// <summary>Raised on every peer when an organ was judged (organ index, outcome). Not raised for <see cref="SnapOutcome.None"/>.</summary>
        public event Action<int, SnapOutcome> SnapResolved;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _rule = new SocketRule(_positionTolerance, _angleToleranceDegrees);

            _snappables = new ISnappable[_body.Organs.Count];
            _sockets = new Pose[_snappables.Length];
            for (int i = 0; i < _snappables.Length; i++)
            {
                _snappables[i] = _body.Organs[i] != null ? _body.Organs[i].GetComponent<ISnappable>() : null;
                if (_snappables[i] == null)
                {
                    Debug.LogError($"{name}: organ {i} has no SnappableOrgan. Run Tools > DIYsect > Add Organ Sockets.", this);
                    enabled = false;
                    return;
                }
                _sockets[i] = _snappables[i].HomeCentrePose;
            }
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && enabled)
                _authority.ServerReleased += OnServerReleased;
        }

        public override void OnNetworkDespawn()
        {
            if (_authority != null)
                _authority.ServerReleased -= OnServerReleased;
        }

        /// <summary>Checks the organ against the sockets, moves it if needed and tells every peer. Server only.</summary>
        private void OnServerReleased(int organIndex)
        {
            if (organIndex < 0 || organIndex >= _snappables.Length)
                return;
            SnapDecision decision = _rule.Evaluate(organIndex, _snappables[organIndex].CurrentCentrePose, _sockets);
            if (decision.Outcome == SnapOutcome.None)
                return;
            _snappables[organIndex].SnapHome();
            SnapResolvedRpc(organIndex, decision.Outcome);
        }

        [Rpc(SendTo.Everyone)]
        private void SnapResolvedRpc(int organIndex, SnapOutcome outcome)
        {
            SnapResolved?.Invoke(organIndex, outcome);
        }
    }
}
