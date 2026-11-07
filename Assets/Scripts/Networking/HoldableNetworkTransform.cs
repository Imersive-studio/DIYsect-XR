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
        // How long no new server state may arrive before the organ is placed exactly on the last one. It is longer than
        // the interpolation window (a few network ticks), so a normal glide is never cut short.
        private const float SettleDelaySeconds = 0.25f;

        private Vector3 _targetPosition;
        private Vector3 _targetEuler;
        private bool _hasTarget;
        private float _lastStateTime;

        /// <summary>True while the local client holds this organ and moves it itself.</summary>
        public bool IsPaused { get; private set; }

        internal void Pause()
        {
            IsPaused = true;
        }

        internal void Resume()
        {
            IsPaused = false;
            // Let interpolation glide to the server pose first. Settling only applies once things have been quiet for a while.
            _lastStateTime = Time.unscaledTime;
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
            SettleOnServerPose();
        }

        // Remembers the newest pose the server sent. A state only carries the axes that changed, so we merge it into
        // the pose we already know.
        protected override void OnNetworkTransformStateUpdated(ref NetworkTransformState oldState, ref NetworkTransformState newState)
        {
            base.OnNetworkTransformStateUpdated(ref oldState, ref newState);
            // The rotation merge below reads per-axis Euler flags, which NGO only sets when quaternion sync is off.
            if (UseQuaternionSynchronization)
            {
                Debug.LogError($"{name}: HoldableNetworkTransform expects Euler rotation sync; turn off Use Quaternion Synchronization.", this);
                return;
            }
            // The first call comes from the spawn synchronization, before NGO applies it to the transform. That is
            // fine: the full synchronization state carries every axis, so each seeded value below is overwritten.
            if (!_hasTarget)
            {
                _targetPosition = InLocalSpace ? transform.localPosition : transform.position;
                _targetEuler = (InLocalSpace ? transform.localRotation : transform.rotation).eulerAngles;
                _hasTarget = true;
            }

            Vector3 position = newState.GetPosition();
            if (newState.HasPositionX) _targetPosition.x = position.x;
            if (newState.HasPositionY) _targetPosition.y = position.y;
            if (newState.HasPositionZ) _targetPosition.z = position.z;

            Vector3 euler = newState.GetRotation().eulerAngles;
            if (newState.HasRotAngleX) _targetEuler.x = euler.x;
            if (newState.HasRotAngleY) _targetEuler.y = euler.y;
            if (newState.HasRotAngleZ) _targetEuler.z = euler.z;

            _lastStateTime = Time.unscaledTime;
        }

        // NGO's interpolator decides it has arrived once every axis is within 5 mm of the target (it rounds the gap to
        // two decimals) and then stops moving. When a frame lands inside that band before the last step, the organ is
        // left up to 5 mm short for good. Once the server has been quiet for a moment, put the organ exactly on its pose.
        private void SettleOnServerPose()
        {
            if (!_hasTarget || !IsSpawned || CanCommitToTransform)
                return;
            if (Time.unscaledTime - _lastStateTime < SettleDelaySeconds)
                return;

            // NGO rewrites the transform every frame from its own value (up to 5 mm short), so this write also happens
            // every frame for an organ that got stuck; it is a cheap local assignment. The check only skips organs that
            // NGO already placed exactly.
            Quaternion rotation = Quaternion.Euler(_targetEuler);
            Vector3 position = InLocalSpace ? transform.localPosition : transform.position;
            Quaternion current = InLocalSpace ? transform.localRotation : transform.rotation;
            if (position == _targetPosition && current == rotation)
                return;
            if (InLocalSpace)
                transform.SetLocalPositionAndRotation(_targetPosition, rotation);
            else
                transform.SetPositionAndRotation(_targetPosition, rotation);
        }
    }
}
