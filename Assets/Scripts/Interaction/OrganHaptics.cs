// Authority: none. Local presentation only. A pulse goes to the controller on this device that hovered or held the organ.
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Controller vibration for one organ: a light pulse when a controller starts pointing at it, a firmer one when it
    /// is grabbed, and one more when the server has judged the drop (accepted or rejected). The snap pulse goes only
    /// to the hand that let the organ go on this device; other clients never feel it. Event driven.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class OrganHaptics : MonoBehaviour
    {
        [SerializeField, Tooltip("The body's OrganSocketKeeper, which reports the drop result.")]
        private OrganSocketKeeper _keeper;
        [SerializeField] private HapticPulse _hover = new HapticPulse(0.25f, 0.05f);
        [SerializeField] private HapticPulse _grab = new HapticPulse(0.5f, 0.1f);
        [SerializeField] private HapticPulse _snap = new HapticPulse(0.5f, 0.1f);
        [SerializeField, Min(0.1f), Tooltip("How long after letting go a drop result still counts as ours.")]
        private float _snapWaitSeconds = 2f;

        private XRGrabInteractable _interactable;
        private NetworkedOrganGrab _networkedGrab;
        private int _organIndex = -1;
        private IXRSelectInteractor _releasedBy;
        private float _releasedUntil;

        private void Awake()
        {
            _interactable = GetComponent<XRGrabInteractable>();
            _networkedGrab = GetComponent<NetworkedOrganGrab>();
            if (!ResolveOrganIndex())
                enabled = false;
        }

        private void OnEnable()
        {
            _interactable.hoverEntered.AddListener(OnHoverEntered);
            _interactable.selectEntered.AddListener(OnSelectEntered);
            _interactable.selectExited.AddListener(OnSelectExited);
            _keeper.SnapResolved += OnSnapResolved;
        }

        private void OnDisable()
        {
            _interactable.hoverEntered.RemoveListener(OnHoverEntered);
            _interactable.selectEntered.RemoveListener(OnSelectEntered);
            _interactable.selectExited.RemoveListener(OnSelectExited);
            if (_keeper != null)
                _keeper.SnapResolved -= OnSnapResolved;
            _releasedBy = null;
        }

        private void OnHoverEntered(HoverEnterEventArgs args)
        {
            // No hover tick on an organ that is held (here or elsewhere): it cannot be picked up anyway.
            if (_interactable.isSelected || (_networkedGrab != null && _networkedGrab.isActiveAndEnabled && _networkedGrab.IsHeldByOtherClient))
                return;
            InteractorHaptics.Send(args.interactorObject, _hover);
        }

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            InteractorHaptics.Send(args.interactorObject, _grab);
        }

        // Remember which hand let go. The server's answer arrives a moment later.
        private void OnSelectExited(SelectExitEventArgs args)
        {
            _releasedBy = args.interactorObject;
            _releasedUntil = Time.unscaledTime + _snapWaitSeconds;
        }

        private void OnSnapResolved(int organIndex, SnapOutcome outcome)
        {
            if (organIndex != _organIndex || _releasedBy == null)
                return;
            // Every client hears the result, but only the one that just released the organ has a hand to pulse.
            if (Time.unscaledTime <= _releasedUntil)
                InteractorHaptics.Send(_releasedBy, _snap);
            _releasedBy = null;
        }

        private bool ResolveOrganIndex()
        {
            if (_keeper == null)
            {
                Debug.LogError($"{name}: OrganHaptics has no OrganSocketKeeper. Run Tools > DIYsect > Add Hover Labels And Haptics.", this);
                return false;
            }
            var organs = _keeper.GetComponent<SharedBody>().Organs;
            for (int i = 0; i < organs.Count; i++)
            {
                if (organs[i] != null && organs[i].transform == transform)
                {
                    _organIndex = i;
                    return true;
                }
            }
            Debug.LogError($"{name}: this organ is not in the SharedBody of its OrganSocketKeeper.", this);
            return false;
        }
    }
}
