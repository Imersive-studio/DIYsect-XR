// Authority: the server. It is the only writer of the help request. Clients ask by Rpc; every peer and late joiner reads the replicated phase.
using System;
using System.Collections;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Classroom
{
    /// <summary>
    /// The "Help me" request. The student (the person who steers the scalpel) asks for help or takes the request back;
    /// the professor answers by grabbing the depth handle, which the server sees through the <see cref="GuidingHand"/>.
    /// The phase moves only through <see cref="AssistRule"/>. The server watches the scalpel's steering and guiding hand,
    /// so the request always closes when the student puts the scalpel down or leaves, and when the professor lets go.
    /// The scalpel is its own network object that can spawn after the body, so the server looks for it until it finds it
    /// and keeps checking that it is still there.
    /// </summary>
    public class AssistBoard : NetworkBehaviour
    {
        [SerializeField, Min(1), Tooltip("Requests one client may make inside the window.")]
        private int _maxChangesPerWindow = 6;
        [SerializeField, Min(0.1f), Tooltip("Length of the rate limit window in seconds.")]
        private float _windowSeconds = 10f;
        [SerializeField, Min(0.1f), Tooltip("How often the server checks that it still has the scalpel, in seconds.")]
        private float _scalpelCheckSeconds = 0.5f;

        private readonly NetworkVariable<AssistPhase> _phase = new NetworkVariable<AssistPhase>(AssistPhase.None);
        private readonly NetworkVariable<ulong> _requester = new NetworkVariable<ulong>(ScalpelSteering.NoSteerer);
        private PinRateLimiter _limiter;
        private ScalpelSteering _steering;
        private GuidingHand _hand;
        private Coroutine _watching;

        /// <summary>Raised on every peer when the phase changes, and once on spawn with the current phase (so a late joiner draws it).</summary>
        public event Action<AssistPhase> PhaseChanged;

        public AssistPhase Phase => IsSpawned ? _phase.Value : AssistPhase.None;

        /// <summary>The client that asked for help, or <see cref="ScalpelSteering.NoSteerer"/> while nobody is asking.</summary>
        public ulong Requester => IsSpawned ? _requester.Value : ScalpelSteering.NoSteerer;

        /// <summary>True when this device's own request is open (requested or being helped).</summary>
        public bool IsLocalRequest => IsSpawned && NetworkManager != null && _phase.Value != AssistPhase.None && _requester.Value == NetworkManager.LocalClientId;

        private void Awake()
        {
            _limiter = new PinRateLimiter(_maxChangesPerWindow, _windowSeconds);
        }

        public override void OnNetworkSpawn()
        {
            _phase.OnValueChanged += HandlePhaseChanged;
            if (IsServer)
            {
                _requester.Value = ScalpelSteering.NoSteerer;
                _phase.Value = AssistPhase.None;
                NetworkManager.OnClientDisconnectCallback += HandleDisconnect;
                _watching = StartCoroutine(WatchScalpel());
            }
            PhaseChanged?.Invoke(_phase.Value);
        }

        public override void OnNetworkDespawn()
        {
            _phase.OnValueChanged -= HandlePhaseChanged;
            if (!IsServer)
                return;
            if (_watching != null)
                StopCoroutine(_watching);
            _watching = null;
            Unbind(closeRequest: false);
            if (NetworkManager != null)
                NetworkManager.OnClientDisconnectCallback -= HandleDisconnect;
        }

        /// <summary>Asks the server to open (true) or take back (false) this device's request. The server ignores anyone who is not steering the scalpel.</summary>
        public void RequestSet(bool wanted)
        {
            if (IsSpawned)
                SetRequestRpc(wanted);
        }

        /// <summary>The help button: opens the request, or takes it back when it is already open.</summary>
        public void Toggle()
        {
            RequestSet(!IsLocalRequest);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void SetRequestRpc(bool wanted, RpcParams rpcParams = default)
        {
            ulong sender = rpcParams.Receive.SenderClientId;
            if (!EnsureBound() || !_steering.HasSteerer || _steering.Steerer != sender)
                return;
            if (!_limiter.TryAccept(sender, Time.realtimeSinceStartup))
                return;
            if (!wanted)
            {
                // Only the person who asked can take the request back.
                if (_requester.Value == sender)
                    Apply(AssistEvent.StudentCancelled);
                return;
            }
            if (_phase.Value == AssistPhase.None)
                _requester.Value = sender;
            Apply(AssistEvent.StudentRequested);
            // The professor may already be holding the depth handle when the student asks.
            if (_phase.Value == AssistPhase.Requested && _hand.HasController && _hand.Controller != sender)
                Apply(AssistEvent.ProfessorTookControl);
        }

        private bool IsBound => _steering != null && _hand != null && _steering.IsSpawned && _hand.IsSpawned;

        // Server only. Every change goes through the rule table.
        private void Apply(AssistEvent change)
        {
            AssistPhase next = AssistRule.Next(_phase.Value, change);
            if (next == _phase.Value)
                return;
            _phase.Value = next;
            if (next == AssistPhase.None)
                _requester.Value = ScalpelSteering.NoSteerer;
        }

        private IEnumerator WatchScalpel()
        {
            var wait = new WaitForSeconds(_scalpelCheckSeconds);
            while (true)
            {
                EnsureBound();
                yield return wait;
            }
        }

        // Finds the scalpel when it has spawned, and lets go of it when it has gone (scene reload, despawn).
        private bool EnsureBound()
        {
            if (IsBound)
                return true;
            // The scalpel went away: nobody can be asking any more.
            if (_steering != null || _hand != null)
                Unbind(closeRequest: true);
            TryBind();
            return IsBound;
        }

        private void TryBind()
        {
            foreach (NetworkObject spawned in NetworkManager.SpawnManager.SpawnedObjectsList)
            {
                if (!spawned.IsSpawned || !spawned.TryGetComponent(out ScalpelSteering steering) || !spawned.TryGetComponent(out GuidingHand hand))
                    continue;
                _steering = steering;
                _hand = hand;
                _steering.SteererChanged += HandleSteererChanged;
                _hand.ControllerChanged += HandleControllerChanged;
                return;
            }
        }

        private void Unbind(bool closeRequest)
        {
            // A destroyed scalpel compares equal to null and has nothing to unsubscribe from.
            if (_steering != null)
                _steering.SteererChanged -= HandleSteererChanged;
            if (_hand != null)
                _hand.ControllerChanged -= HandleControllerChanged;
            _steering = null;
            _hand = null;
            if (closeRequest && _phase.Value != AssistPhase.None)
                Apply(AssistEvent.SteererLeft);
        }

        private void HandleSteererChanged(ulong steerer)
        {
            if (_phase.Value != AssistPhase.None && steerer != _requester.Value)
                Apply(AssistEvent.SteererLeft);
        }

        private void HandleControllerChanged(ulong controller)
        {
            if (_phase.Value == AssistPhase.None)
                return;
            if (controller == GuidingHand.NoController)
                Apply(AssistEvent.ProfessorReleased);
            else if (controller != _requester.Value)
                Apply(AssistEvent.ProfessorTookControl);
        }

        private void HandleDisconnect(ulong clientId)
        {
            _limiter.Forget(clientId);
            if (_phase.Value != AssistPhase.None && _requester.Value == clientId)
                Apply(AssistEvent.SteererLeft);
        }

        private void HandlePhaseChanged(AssistPhase previous, AssistPhase current)
        {
            PhaseChanged?.Invoke(current);
        }
    }
}
