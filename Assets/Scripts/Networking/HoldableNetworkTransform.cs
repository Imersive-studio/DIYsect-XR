// Authority: the server (same as NetworkTransform). Only the local application of incoming state can be paused.
using Unity.Netcode.Components;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// A server-authoritative NetworkTransform whose incoming state can be paused on one peer.
    /// The client that holds an organ moves it locally with XR Interaction Toolkit, so it must not also apply
    /// the (older) server state to the same transform. <see cref="OrganGrabAuthority"/> pauses and resumes it.
    /// </summary>
    public class HoldableNetworkTransform : NetworkTransform
    {
        /// <summary>True while the local client holds this organ and moves it itself.</summary>
        public bool IsPaused { get; private set; }

        internal void Pause()
        {
            IsPaused = true;
        }

        internal void Resume()
        {
            IsPaused = false;
        }

        protected override void Awake()
        {
            base.Awake();
            if (AuthorityMode != AuthorityModes.Server)
                Debug.LogError($"{name}: HoldableNetworkTransform must be server authoritative.", this);
        }

        public override void OnUpdate()
        {
            // NGO's docs say a non-authority instance should always call base. We deliberately skip it while paused.
            // That is safe here: incoming states are still buffered (TransformStateUpdate does not go through
            // OnUpdate), so when the pause ends, interpolation resumes and moves the organ to the current server pose.
            if (IsPaused)
                return;
            base.OnUpdate();
        }
    }
}
