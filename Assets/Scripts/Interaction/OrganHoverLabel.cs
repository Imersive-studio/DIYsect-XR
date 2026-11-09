// Authority: none. Local presentation only; each headset shows the label for its own controllers' hover.
using DIYsect.Networking;
using DIYsect.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Shows the organ's name, with its body system under it, just above the organ while a controller points at it
    /// (hovers it). The words come from the <see cref="OrganCaptions"/> asset; when it is missing or does not know the organ
    /// the name comes from <see cref="OrganNames"/> and there is no second line. It is a label that only this device sees.
    /// It stays hidden while the organ is held, by this device or by another one, because the callout from
    /// <see cref="OrganGrabPresenter"/> is shown then. Event driven; the label does its own per-frame placement only while shown.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class OrganHoverLabel : MonoBehaviour
    {
        [SerializeField, Tooltip("Words for each organ. Run Tools > DIYsect > Apply Label Design to fill this in.")]
        private OrganCaptions _captions;
        [SerializeField, Tooltip("Label font (Inter). Run Tools > DIYsect > Apply Label Design to fill this in.")]
        private TMP_FontAsset _font;
        [SerializeField, Min(0f), Tooltip("Metres between the top of the organ and the label.")]
        private float _labelGap = LabelAnchor.Gap;

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
            OrganCaption caption = OrganCaptions.Lookup(_captions, name);
            _label = OrganLabel.Create(transform, caption, LabelText.Resolve(_font, this), _labelGap);
            _label.gameObject.name = $"Hover Label {_label.Text}";
            return _label;
        }

        private void OnDestroy()
        {
            if (_label != null)
                Destroy(_label.gameObject);
        }
    }
}
