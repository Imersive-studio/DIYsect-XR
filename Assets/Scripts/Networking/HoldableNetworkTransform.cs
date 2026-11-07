// Authority: the server (same as NetworkTransform). Only the local application of incoming state can be paused.
using Unity.Netcode.Components;

namespace DIYsect.Networking
{
    /// <summary>
    /// A server-authoritative NetworkTransform whose incoming state can be paused on one peer.
    /// The client that holds an organ moves it locally with XR Interaction Toolkit, so it must not also apply
    /// the (older) server state to the same transform. While paused, incoming states are still buffered,
    /// so when it resumes the organ interpolates to the current server pose.
    /// </summary>
    public class HoldableNetworkTransform : NetworkTransform
    {
        /// <summary>True while the local client holds this organ and moves it itself.</summary>
        public bool IsPaused { get; set; }

        public override void OnUpdate()
        {
            if (IsPaused)
                return;
            base.OnUpdate();
        }
    }
}
