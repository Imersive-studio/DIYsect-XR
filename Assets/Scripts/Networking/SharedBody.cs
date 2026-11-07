// Authority: the server owns the body and every organ pose. Clients only receive and interpolate them.
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Root of the shared anatomy body. The body is one NetworkObject. Each organ carries a
    /// server-authoritative NetworkTransform that syncs its pose in local space (relative to its parent
    /// group), so the body can be placed differently per scene while the organs stay identical.
    /// Joining and reconnecting clients receive the current organ poses when the body spawns for them.
    /// Grabbing and ownership are a later task; they should build on <see cref="Organs"/>.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class SharedBody : NetworkBehaviour
    {
        [SerializeField] private NetworkTransform[] _organs;

        /// <summary>The organ transforms that are synced, in a fixed order.</summary>
        public IReadOnlyList<NetworkTransform> Organs => _organs;

        private void Awake()
        {
            Validate();
        }

        private void OnValidate()
        {
            Validate();
        }

        private void Validate()
        {
            if (_organs == null || _organs.Length == 0)
            {
                Debug.LogError($"{name}: SharedBody has no organs assigned. Run Tools > DIYsect > Add Body Networking.", this);
                return;
            }

            for (int i = 0; i < _organs.Length; i++)
            {
                NetworkTransform organ = _organs[i];
                if (organ == null)
                {
                    Debug.LogError($"{name}: SharedBody organ {i} is missing.", this);
                    continue;
                }
                if (organ.AuthorityMode != NetworkTransform.AuthorityModes.Server || !organ.InLocalSpace || !organ.Interpolate)
                    Debug.LogError($"{organ.name}: organ NetworkTransform must be server authoritative, in local space, with interpolation.", organ);
            }
        }
    }
}
