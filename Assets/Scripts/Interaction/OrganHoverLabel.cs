// Authority: none. Local presentation only; each headset shows the label for its own controllers' hover.
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Shows the organ's name above it while a controller points at it (hovers it). It uses the same
    /// <see cref="OrganLabel"/> and <see cref="OrganNames"/> as the grab label, but it is a separate label that only
    /// this device sees. It stays hidden while the organ is held, by this device or by another one, because the grab
    /// label from <see cref="OrganGrabPresenter"/> is shown then. Event driven; no per-frame code except the label's Billboard while shown.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class OrganHoverLabel : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Metres between the organ's centre and its name tag. Same as the grab label.")]
        private float _labelHeight = 0.15f;
        [SerializeField, Min(0.001f)] private float _labelCharacterSize = 0.01f;

        private XRGrabInteractable _interactable;
        private NetworkedOrganGrab _grab;
        private OrganLabel _label;

        /// <summary>The hover label, or null if this organ has never been hovered on this device.</summary>
        public OrganLabel Label => _label;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _grab = GetComponent<NetworkedOrganGrab>();
        }

        private void OnEnable()
        {
            _interactable.hoverEntered.AddListener(OnHoverChanged);
            _interactable.hoverExited.AddListener(OnHoverChanged);
            _interactable.selectEntered.AddListener(OnSelectChanged);
            _interactable.selectExited.AddListener(OnSelectChanged);
            if (_grab != null)
                _grab.HolderChanged += OnHolderChanged;
        }

        private void OnDisable()
        {
            _interactable.hoverEntered.RemoveListener(OnHoverChanged);
            _interactable.hoverExited.RemoveListener(OnHoverChanged);
            _interactable.selectEntered.RemoveListener(OnSelectChanged);
            _interactable.selectExited.RemoveListener(OnSelectChanged);
            if (_grab != null)
                _grab.HolderChanged -= OnHolderChanged;
            if (_label != null)
                _label.Hide();
        }

        private void OnHoverChanged(UnityEngine.XR.Interaction.Toolkit.HoverEnterEventArgs args) => Refresh();
        private void OnHoverChanged(UnityEngine.XR.Interaction.Toolkit.HoverExitEventArgs args) => Refresh();
        private void OnSelectChanged(UnityEngine.XR.Interaction.Toolkit.SelectEnterEventArgs args) => Refresh();
        private void OnSelectChanged(UnityEngine.XR.Interaction.Toolkit.SelectExitEventArgs args) => Refresh();
        private void OnHolderChanged(ulong holder) => Refresh();

        // The interactable updates isHovered and isSelected before it raises these events, so they are current here.
        private void Refresh()
        {
            bool heldByOther = _grab != null && _grab.isActiveAndEnabled && _grab.IsHeldByOtherClient;
            bool show = _interactable.isHovered && !_interactable.isSelected && !heldByOther;
            if (show)
                GetOrCreateLabel().Show();
            else if (_label != null)
                _label.Hide();
        }

        private OrganLabel GetOrCreateLabel()
        {
            if (_label != null)
                return _label;
            // Same placement as OrganGrabPresenter: above the middle of the organ's mesh.
            Vector3 centre = TryGetComponent(out ISnappable snappable)
                ? Quaternion.Inverse(snappable.CurrentCentrePose.rotation) * (snappable.CurrentCentrePose.position - transform.localPosition)
                : Vector3.zero;
            _label = OrganLabel.Create(transform, centre, OrganNames.Display(name), _labelHeight, _labelCharacterSize);
            _label.gameObject.name = $"Hover Label {_label.Text}";
            return _label;
        }
    }
}
