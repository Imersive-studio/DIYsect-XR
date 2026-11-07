// Authority: the server. It decides who may draw, validates every point and holds the line.
// Clients ask by Rpc to start, extend and end a stroke; every peer receives the points.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// The guide line: the professor draws a line in the air over the body to show the student where to cut, and
    /// every peer sees it on the body. Points are stored in the body root's local space, so the line stays on the
    /// body wherever each client has placed it (the professor's real table, the lecture hall table, a desktop).
    /// One client draws at a time. Starting a stroke replaces the previous line. The drawer or the host can clear it.
    /// Drawing ends when the drawer ends the stroke or disconnects; the line itself stays until it is cleared.
    /// Late joiners receive the current line when the body spawns for them.
    /// Role assumption, as for the guiding hand: roles are not modelled; the professor scene has the pen.
    /// </summary>
    public class GuideLine : NetworkBehaviour
    {
        public const ulong NoDrawer = ulong.MaxValue;

        [Tooltip("Points farther than this from the body root (metres) are rejected.")]
        [SerializeField, Min(0.1f)] private float _maxRadius = 1.5f;
        [Tooltip("A new point must be at least this far from the previous one (metres).")]
        [SerializeField, Min(0.001f)] private float _minSpacing = 0.005f;
        [SerializeField, Min(2)] private int _maxPoints = 256;

        private readonly NetworkList<Vector3> _points = new NetworkList<Vector3>();
        private readonly NetworkVariable<ulong> _drawer = new NetworkVariable<ulong>(NoDrawer);
        private GuideStrokeRules _rules;

        /// <summary>Raised on every peer whenever the line changes, and once on spawn.</summary>
        public event Action LineChanged;

        /// <summary>Raised on the requesting client when the server answers a start request.</summary>
        public event Action<bool> StrokeResolved;

        public int Count => IsSpawned ? _points.Count : 0;
        public Vector3 this[int index] => _points[index];
        public ulong Drawer => IsSpawned ? _drawer.Value : NoDrawer;
        public bool IsDrawnByLocalClient => IsSpawned && NetworkManager != null && _drawer.Value == NetworkManager.LocalClientId;

        private void Awake()
        {
            _rules = new GuideStrokeRules(_maxRadius, _minSpacing, _maxPoints);
        }

        public override void OnNetworkSpawn()
        {
            _points.OnListChanged += HandleListChanged;
            if (IsServer)
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
            LineChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            _points.OnListChanged -= HandleListChanged;
            if (IsServer && NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }

        /// <summary>Asks to start a new stroke, replacing the current line. The answer arrives through <see cref="StrokeResolved"/>.</summary>
        public void StartStroke()
        {
            if (IsSpawned)
                StartStrokeRpc();
        }

        /// <summary>Adds a point (world space) to the stroke. The server ignores it unless the caller is drawing.</summary>
        public void AddPoint(Vector3 worldPoint)
        {
            if (IsSpawned)
                AddPointRpc(transform.InverseTransformPoint(worldPoint));
        }

        /// <summary>Ends the caller's stroke. The line stays.</summary>
        public void EndStroke()
        {
            if (IsSpawned)
                EndStrokeRpc();
        }

        /// <summary>Removes the line. Only the drawer of the line or the host may do this.</summary>
        public void Clear()
        {
            if (IsSpawned)
                ClearRpc();
        }

        /// <summary>
        /// Server only. Judges and stores one body-local point from <paramref name="sender"/>. Public so the rules
        /// can be tested without the network; the Rpc is the only way a client reaches it.
        /// </summary>
        public GuideStrokeRules.Verdict ServerAddPoint(ulong sender, Vector3 localPoint)
        {
            if (!IsServer || _drawer.Value != sender)
                return GuideStrokeRules.Verdict.Full;
            Vector3? last = _points.Count > 0 ? _points[_points.Count - 1] : (Vector3?)null;
            GuideStrokeRules.Verdict verdict = _rules.Judge(localPoint, last, _points.Count);
            if (verdict == GuideStrokeRules.Verdict.Accepted)
                _points.Add(localPoint);
            return verdict;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void StartStrokeRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool granted = _drawer.Value == NoDrawer || _drawer.Value == sender;
            if (granted)
            {
                _drawer.Value = sender;
                _points.Clear();
            }
            StrokeResultRpc(granted, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void StrokeResultRpc(bool granted, RpcParams rpcParams = default)
        {
            StrokeResolved?.Invoke(granted);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void AddPointRpc(Vector3 localPoint, RpcParams rpcParams = default)
        {
            ServerAddPoint(rpcParams.Receive.SenderClientId, localPoint);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void EndStrokeRpc(RpcParams rpcParams = default)
        {
            if (_drawer.Value == rpcParams.Receive.SenderClientId)
                _drawer.Value = NoDrawer;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ClearRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool mayClear = sender == NetworkManager.ServerClientId || sender == _drawer.Value || _drawer.Value == NoDrawer;
            if (!mayClear)
                return;
            _points.Clear();
            _drawer.Value = NoDrawer;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_drawer.Value == clientId)
                _drawer.Value = NoDrawer;
        }

        private void HandleListChanged(NetworkListEvent<Vector3> change)
        {
            LineChanged?.Invoke();
        }
    }
}
