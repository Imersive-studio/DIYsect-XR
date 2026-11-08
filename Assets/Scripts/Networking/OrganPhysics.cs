// Authority: the server. Only the server simulates organ physics; on every client the organ Rigidbodies stay
// kinematic and follow the server-written NetworkTransform (Netcode's server-authoritative physics model).
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Networking
{
    /// <summary>
    /// Switches each organ between two physics states on the server:
    /// seated (in the body, being held, or easing home): kinematic, on the SeatedOrgan layer;
    /// loose (let go away from the body): dynamic with gravity, on the LooseOrgan layer, so it falls and rests on
    /// the table, the tray or the floor. Loose organs ignore seated ones (the organs inside the body overlap, so
    /// colliding with them would throw the loose organ around), which is set once with
    /// <see cref="Physics.IgnoreLayerCollision(int,int,bool)"/>.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority))]
    public class OrganPhysics : NetworkBehaviour
    {
        public const string SeatedLayerName = "SeatedOrgan";
        public const string LooseLayerName = "LooseOrgan";

        [SerializeField, Min(0.01f)] private float _mass = 0.5f;
        [SerializeField, Min(0f)] private float _linearDamping = 0.5f;
        [SerializeField, Min(0f)] private float _angularDamping = 2f;

        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private Rigidbody[] _bodies;
        private int _seatedLayer;
        private int _looseLayer;

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _seatedLayer = LayerMask.NameToLayer(SeatedLayerName);
            _looseLayer = LayerMask.NameToLayer(LooseLayerName);
            if (_seatedLayer < 0 || _looseLayer < 0)
            {
                Debug.LogError($"{name}: the layers {SeatedLayerName} and {LooseLayerName} are missing from Project Settings > Tags and Layers.", this);
                enabled = false;
                return;
            }
            Physics.IgnoreLayerCollision(_seatedLayer, _looseLayer, true);
            _bodies = new Rigidbody[_body.Organs.Count];
            for (int i = 0; i < _bodies.Length; i++)
            {
                _bodies[i] = _body.Organs[i] != null ? _body.Organs[i].GetComponent<Rigidbody>() : null;
                if (_bodies[i] == null)
                    Debug.LogError($"{name}: organ {i} has no Rigidbody. Run Tools > DIYsect > Add Body Networking.", this);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!enabled)
                return;
            for (int i = 0; i < _bodies.Length; i++)
                Seat(i);
            if (IsServer)
                _authority.ServerGranted += HandleGrabGranted;
        }

        public override void OnNetworkDespawn()
        {
            if (_authority != null)
                _authority.ServerGranted -= HandleGrabGranted;
        }

        /// <summary>True when the organ is falling or resting freely (server view).</summary>
        public bool IsLoose(int organIndex)
        {
            Rigidbody body = BodyAt(organIndex);
            return body != null && !body.isKinematic;
        }

        /// <summary>Server only. Kinematic, still, on the seated layer: in the body, held, or easing home.</summary>
        public void Seat(int organIndex)
        {
            Rigidbody body = BodyAt(organIndex);
            if (body == null)
                return;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.isKinematic = true;
            body.useGravity = false;
            SetLayer(body.gameObject, _seatedLayer);
        }

        /// <summary>Server only. Dynamic with gravity on the loose layer, so the organ falls and comes to rest.</summary>
        public void Loosen(int organIndex)
        {
            Rigidbody body = BodyAt(organIndex);
            if (body == null || !IsServer)
                return;
            SetLayer(body.gameObject, _looseLayer);
            body.mass = _mass;
            body.linearDamping = _linearDamping;
            body.angularDamping = _angularDamping;
            body.interpolation = RigidbodyInterpolation.None;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.useGravity = true;
            body.isKinematic = false;
            body.WakeUp();
        }

        private void HandleGrabGranted(int organIndex)
        {
            Seat(organIndex);
        }

        private Rigidbody BodyAt(int organIndex)
        {
            return _bodies != null && organIndex >= 0 && organIndex < _bodies.Length ? _bodies[organIndex] : null;
        }

        private static void SetLayer(GameObject organ, int layer)
        {
            foreach (Transform part in organ.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;
        }
    }
}
