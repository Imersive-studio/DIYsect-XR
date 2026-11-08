// Authority: the server. It decides who controls the scalpel's cut depth and holds the depth value.
// Clients ask by Rpc; the server answers. The controller streams depth values, the server validates and stores them.
using System;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// The guiding hand: lets a second person (the professor) take one degree of freedom of the student's
    /// scalpel, the cut depth, while the student keeps steering the rest.
    /// Depth is a value from 0 (blade at the skin) to 1 (deepest the tool allows). Role assumption: roles are not
    /// modelled, so any client that is not the steerer may guide. The professor is the one who does in practice,
    /// and every peer labels the guide "Professor". The server grants control only to one client at a time, only
    /// while somebody steers the scalpel, and never to the steerer. It ends control when the guide releases it,
    /// disconnects, or when the student puts the scalpel down.
    /// </summary>
    [RequireComponent(typeof(ScalpelSteering))]
    public class GuidingHand : NetworkBehaviour
    {
        /// <summary>Controller value while the student has all degrees of freedom.</summary>
        public const ulong NoController = ulong.MaxValue;

        private readonly NetworkVariable<ulong> _controller = new NetworkVariable<ulong>(NoController);
        private readonly NetworkVariable<float> _depth = new NetworkVariable<float>(0f);
        private ScalpelSteering _steering;

        /// <summary>Raised on every peer when the controller changes (new controller id or <see cref="NoController"/>). Also raised once on spawn with the current value.</summary>
        public event Action<ulong> ControllerChanged;

        /// <summary>Raised on every peer when the depth value changes.</summary>
        public event Action<float> DepthChanged;

        /// <summary>Raised on the requesting client when the server answers a control request.</summary>
        public event Action<bool> ControlResolved;

        /// <summary>Raised on the guide's client when the server ended its control (the student put the scalpel down).</summary>
        public event Action ControlRevoked;

        public ulong Controller => IsSpawned ? _controller.Value : NoController;
        public bool HasController => Controller != NoController;
        public bool IsControlledByLocalClient => IsSpawned && NetworkManager != null && _controller.Value == NetworkManager.LocalClientId;

        /// <summary>Depth in 0..1, meaningful while <see cref="HasController"/>.</summary>
        public float Depth => IsSpawned ? _depth.Value : 0f;

        private void Awake()
        {
            _steering = GetComponent<ScalpelSteering>();
            if (_steering == null)
            {
                Debug.LogError($"{name}: GuidingHand needs a ScalpelSteering on the same object. Run Tools > DIYsect > Add Placeholder Scalpel.", this);
                enabled = false;
            }
        }

        public override void OnNetworkSpawn()
        {
            _controller.OnValueChanged += HandleControllerChanged;
            _depth.OnValueChanged += HandleDepthChanged;
            if (IsServer)
            {
                _controller.Value = NoController;
                _depth.Value = 0f;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
                _steering.SteererChanged += HandleSteererChanged;
            }
            ControllerChanged?.Invoke(_controller.Value);
        }

        public override void OnNetworkDespawn()
        {
            _controller.OnValueChanged -= HandleControllerChanged;
            _depth.OnValueChanged -= HandleDepthChanged;
            if (IsServer)
            {
                _steering.SteererChanged -= HandleSteererChanged;
                if (NetworkManager != null)
                    NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        /// <summary>Asks the server for control of the depth. The answer arrives through <see cref="ControlResolved"/>.</summary>
        public void RequestControl()
        {
            RequestControlRpc();
        }

        /// <summary>Gives the depth back to the student. The server ignores this unless the caller is the controller.</summary>
        public void ReleaseControl()
        {
            ReleaseControlRpc();
        }

        /// <summary>Sends a depth value. The server ignores it unless the caller is the controller; it rejects values that are not finite and clamps the rest to 0..1.</summary>
        public void SendDepth(float depth)
        {
            SendDepthRpc(depth);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestControlRpc(RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            bool steerable = _steering.HasSteerer && _steering.Steerer != sender;
            // The current controller asking again is an idempotent success: answered yes, nothing is written.
            bool alreadyControls = _controller.Value == sender && steerable;
            bool granted = alreadyControls || (_controller.Value == NoController && steerable);
            if (granted && !alreadyControls)
                _controller.Value = sender;
            ControlResultRpc(granted, RpcTarget.Single(sender, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ControlResultRpc(bool granted, RpcParams rpcParams = default)
        {
            ControlResolved?.Invoke(granted);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void ReleaseControlRpc(RpcParams rpcParams = default)
        {
            if (_controller.Value == rpcParams.Receive.SenderClientId)
                Clear();
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SendDepthRpc(float depth, RpcParams rpcParams = default)
        {
            if (_controller.Value != rpcParams.Receive.SenderClientId || !float.IsFinite(depth))
                return;
            _depth.Value = Mathf.Clamp01(depth);
        }

        // Server only. Returns the depth to the student and resets the value, so the next guide starts from zero.
        private void Clear()
        {
            _controller.Value = NoController;
            _depth.Value = 0f;
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (_controller.Value == clientId)
                Clear();
        }

        // Guiding only makes sense while the student steers, and a client cannot guide its own steering.
        // The guide is told explicitly: a NetworkVariable set and cleared in the same tick may never reach it as a
        // change, but this message always arrives after the grant answer (messages to one client stay in order).
        private void HandleSteererChanged(ulong steerer)
        {
            ulong controller = _controller.Value;
            if (controller == NoController || (steerer != ScalpelSteering.NoSteerer && steerer != controller))
                return;
            Clear();
            ControlRevokedRpc(RpcTarget.Single(controller, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void ControlRevokedRpc(RpcParams rpcParams = default)
        {
            ControlRevoked?.Invoke();
        }

        private void HandleControllerChanged(ulong previous, ulong current)
        {
            ControllerChanged?.Invoke(current);
        }

        private void HandleDepthChanged(float previous, float current)
        {
            DepthChanged?.Invoke(current);
        }
    }
}
