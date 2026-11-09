// Authority: none. Presentation only; it reads who holds each organ and what the server judged, and never writes either.
using System.Collections.Generic;
using DIYsect.Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Shows the other half of grabbing on every client. While anyone holds an organ it shows the organ's name
    /// as a callout (a leader line with its role) and a see-through copy of the organ at its socket. On release both go away. When the server
    /// accepts or rejects a drop, the ghost flashes green or red for a moment.
    /// Labels and ghosts are created the first time an organ is held. Everything is driven by events, so there is
    /// no per-frame code here. A client that joins late shows the organs that are already held.
    /// </summary>
    [RequireComponent(typeof(SharedBody), typeof(OrganGrabAuthority), typeof(OrganSocketKeeper))]
    public class OrganGrabPresenter : NetworkBehaviour
    {
        [SerializeField, Tooltip("Transparent unlit material shared by every ghost.")]
        private Material _ghostMaterial;
        [SerializeField] private Color _acceptedColor = new Color(0.2f, 1f, 0.3f, 0.6f);
        [SerializeField] private Color _rejectedColor = new Color(1f, 0.2f, 0.2f, 0.6f);
        [SerializeField, Min(0.1f)] private float _flashSeconds = 0.8f;
        [SerializeField, Tooltip("Words for each organ. Run Tools > DIYsect > Apply Label Design to fill this in.")]
        private OrganCaptions _captions;
        [SerializeField, Tooltip("Callout font (Inter). Run Tools > DIYsect > Apply Label Design to fill this in.")]
        private TMP_FontAsset _font;
        [SerializeField, Tooltip("Unlit transparent material for the leader line. Run Tools > DIYsect > Apply Label Design to fill this in.")]
        private Material _leaderMaterial;

        private SharedBody _body;
        private OrganGrabAuthority _authority;
        private OrganSocketKeeper _keeper;
        private readonly Dictionary<int, OrganCallout> _callouts = new Dictionary<int, OrganCallout>();
        private readonly Dictionary<int, OrganGhost> _ghosts = new Dictionary<int, OrganGhost>();

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _authority = GetComponent<OrganGrabAuthority>();
            _keeper = GetComponent<OrganSocketKeeper>();
            if (_ghostMaterial == null)
            {
                Debug.LogError($"{name}: OrganGrabPresenter has no ghost material. Run Tools > DIYsect > Add Organ Sockets.", this);
                enabled = false;
            }
            if (_leaderMaterial == null)
            {
                Debug.LogError($"{name}: OrganGrabPresenter has no leader line material. Run Tools > DIYsect > Apply Label Design. Ghosts still work; there will be no callouts.", this);
            }
        }

        public override void OnNetworkSpawn()
        {
            if (!enabled)
                return;
            _authority.HolderChanged += OnHolderChanged;
            _keeper.SnapResolved += OnSnapResolved;
            // A client that joins late gets the holders with the spawn and no change events.
            for (int i = 0; i < _body.Organs.Count; i++)
                if (_authority.GetHolder(i) != OrganGrabAuthority.NoHolder)
                    OnHolderChanged(i, _authority.GetHolder(i));
        }

        public override void OnNetworkDespawn()
        {
            _authority.HolderChanged -= OnHolderChanged;
            _keeper.SnapResolved -= OnSnapResolved;
            Clear();
        }

        /// <summary>The callout of the organ, or null if it has never been held on this client.</summary>
        public OrganCallout GetCallout(int organIndex)
        {
            return _callouts.TryGetValue(organIndex, out OrganCallout callout) ? callout : null;
        }

        /// <summary>The ghost of the organ, or null if it has never been held on this client.</summary>
        public OrganGhost GetGhost(int organIndex)
        {
            return _ghosts.TryGetValue(organIndex, out OrganGhost ghost) ? ghost : null;
        }

        private void OnHolderChanged(int organIndex, ulong holder)
        {
            if (organIndex < 0 || organIndex >= _body.Organs.Count)
                return;
            bool held = holder != OrganGrabAuthority.NoHolder;
            if (held)
            {
                GetOrCreateCallout(organIndex)?.Show();
                GetOrCreateGhost(organIndex)?.SetHeld(true);
                return;
            }
            GetCallout(organIndex)?.Hide();
            GetGhost(organIndex)?.SetHeld(false);
        }

        private void OnSnapResolved(int organIndex, SnapOutcome outcome)
        {
            OrganGhost ghost = GetOrCreateGhost(organIndex);
            if (ghost == null)
                return;
            // Returned is not a verdict (the organ was put back over the body), so it gets no colour.
            if (outcome == SnapOutcome.Returned)
                return;
            ghost.Flash(outcome == SnapOutcome.Accepted ? _acceptedColor : _rejectedColor, _flashSeconds);
        }

        private OrganCallout GetOrCreateCallout(int organIndex)
        {
            if (_callouts.TryGetValue(organIndex, out OrganCallout existing))
                return existing;
            if (_leaderMaterial == null)
                return null;
            Transform organ = _body.Organs[organIndex].transform;
            OrganCaption caption = OrganCaptions.Lookup(_captions, organ.name);
            var callout = OrganCallout.Create(organ, caption, LabelText.Resolve(_font, this), _leaderMaterial);
            _callouts[organIndex] = callout;
            return callout;
        }

        private OrganGhost GetOrCreateGhost(int organIndex)
        {
            if (_ghosts.TryGetValue(organIndex, out OrganGhost existing))
                return existing;
            Transform organ = _body.Organs[organIndex].transform;
            if (!organ.TryGetComponent(out ISnappable snappable))
                return null;
            OrganGhost ghost = OrganGhost.Create(organ, snappable, _ghostMaterial);
            if (ghost != null)
                _ghosts[organIndex] = ghost;
            return ghost;
        }

        private void Clear()
        {
            foreach (OrganCallout callout in _callouts.Values)
                if (callout != null)
                    Destroy(callout.gameObject);
            foreach (OrganGhost ghost in _ghosts.Values)
                if (ghost != null)
                    Destroy(ghost.gameObject);
            _callouts.Clear();
            _ghosts.Clear();
        }
    }
}
