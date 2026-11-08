// Authority: none here. It is a local input bridge; PointerBoard on the server decides what classmates see.
using System.Collections;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Turns "my controller ray rests on this organ" into a pointer for the classroom. When a ray (or near-far) interactor
    /// hovers an organ for a short moment it asks to point at it; when the hover ends it stops. The short wait stops a ray
    /// sweeping over the body from flickering the classmates' screens. Event driven: no per-frame code.
    /// </summary>
    [RequireComponent(typeof(SharedBody))]
    public class OrganPointerInput : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("Seconds a hover must last before it becomes a pointer.")]
        private float _dwellSeconds = 0.3f;

        private SharedBody _body;
        private IOrganPointer _pointer;
        private XRGrabInteractable[] _interactables;
        private UnityAction<HoverEnterEventArgs>[] _entered;
        private UnityAction<HoverExitEventArgs>[] _exited;
        private Coroutine _pending;
        private int _pendingIndex = -1;
        private int _pointing = -1;

        /// <summary>The organ this device is pointing at now, or -1.</summary>
        public int PointingIndex => _pointing;

        /// <summary>Replaces where requests go. The body's PointerBoard is the default; tests use a stand-in.</summary>
        public void SetPointer(IOrganPointer pointer)
        {
            _pointer = pointer;
        }

        private void Awake()
        {
            _body = GetComponent<SharedBody>();
            _pointer = GetComponent<IOrganPointer>();
            if (_pointer == null)
            {
                Debug.LogError($"{name}: OrganPointerInput needs a PointerBoard on the same object. Run Tools > DIYsect > Add Organ Pointer.", this);
                enabled = false;
                return;
            }
            int count = _body.Organs.Count;
            _interactables = new XRGrabInteractable[count];
            _entered = new UnityAction<HoverEnterEventArgs>[count];
            _exited = new UnityAction<HoverExitEventArgs>[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                _interactables[i] = _body.Organs[i].GetComponent<XRGrabInteractable>();
                if (_interactables[i] == null)
                {
                    Debug.LogError($"{_body.Organs[i].name}: no XRGrabInteractable. Run Tools > DIYsect > Add Body Networking.", this);
                    enabled = false;
                    return;
                }
                _entered[i] = args => HandleEntered(index, args.interactorObject);
                _exited[i] = args => HandleExited(index);
            }
        }

        private void OnEnable()
        {
            if (_interactables == null)
                return;
            for (int i = 0; i < _interactables.Length; i++)
            {
                _interactables[i].hoverEntered.AddListener(_entered[i]);
                _interactables[i].hoverExited.AddListener(_exited[i]);
            }
        }

        private void OnDisable()
        {
            if (_interactables == null)
                return;
            for (int i = 0; i < _interactables.Length; i++)
            {
                _interactables[i].hoverEntered.RemoveListener(_entered[i]);
                _interactables[i].hoverExited.RemoveListener(_exited[i]);
            }
            CancelPending();
            Stop();
        }

        private void HandleEntered(int index, IXRHoverInteractor interactor)
        {
            // Only a pointing device counts. A hand that touches or holds an organ is not pointing at it.
            if (!(interactor is XRRayInteractor) && !(interactor is NearFarInteractor))
                return;
            CancelPending();
            _pendingIndex = index;
            _pending = StartCoroutine(PointAfterDwell(index));
        }

        private void HandleExited(int index)
        {
            if (_pendingIndex == index)
                CancelPending();
            if (_pointing == index)
                Stop();
        }

        private IEnumerator PointAfterDwell(int index)
        {
            if (_dwellSeconds > 0f)
                yield return new WaitForSecondsRealtime(_dwellSeconds);
            _pending = null;
            _pendingIndex = -1;
            _pointing = index;
            _pointer.Point(index);
        }

        private void CancelPending()
        {
            if (_pending != null)
                StopCoroutine(_pending);
            _pending = null;
            _pendingIndex = -1;
        }

        private void Stop()
        {
            if (_pointing < 0)
                return;
            _pointing = -1;
            _pointer.StopPointing();
        }
    }
}
