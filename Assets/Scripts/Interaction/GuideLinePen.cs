// Authority: none here. It is a local input bridge; GuideLine on the server decides what is drawn.
using System.Collections;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.Interaction
{
    /// <summary>
    /// The professor's marker pen. Grab it, hold the trigger and move the tip over the body to draw the guide line;
    /// release the trigger to finish. Every new stroke replaces the previous line, so a quick tap clears it.
    /// While the trigger is held the tip is sampled on a short timer and sent only when it has moved, so there is
    /// no per-frame code and no flood of points. Putting the pen down also ends the stroke.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GuideLinePen : MonoBehaviour
    {
        [SerializeField] private GuideLine _line;
        [SerializeField] private Transform _tip;
        [SerializeField, Min(0.01f)] private float _sampleSeconds = 0.03f;
        [SerializeField, Min(0.001f)] private float _minMove = 0.005f;

        private XRGrabInteractable _grab;
        private Coroutine _drawing;

        private void Awake()
        {
            _grab = GetComponent<XRGrabInteractable>();
            if (_line == null)
                Debug.LogError($"{name}: GuideLinePen has no GuideLine. Run Tools > DIYsect > Add Guide Line.", this);
            if (_tip == null)
                Debug.LogError($"{name}: GuideLinePen has no tip transform.", this);
            if (_line == null || _tip == null)
                enabled = false;
        }

        private void OnEnable()
        {
            _grab.activated.AddListener(HandleActivated);
            _grab.deactivated.AddListener(HandleDeactivated);
            _grab.selectExited.AddListener(HandleReleased);
        }

        private void OnDisable()
        {
            _grab.activated.RemoveListener(HandleActivated);
            _grab.deactivated.RemoveListener(HandleDeactivated);
            _grab.selectExited.RemoveListener(HandleReleased);
            StopDrawing();
        }

        /// <summary>Starts a stroke at the current tip, as the trigger does. Public so tests can drive it.</summary>
        public void BeginStroke()
        {
            StopDrawing();
            _line.StartStroke();
            _drawing = StartCoroutine(Sample());
        }

        /// <summary>Ends the stroke, as releasing the trigger does.</summary>
        public void FinishStroke()
        {
            if (_drawing == null)
                return;
            StopDrawing();
            _line.AddPoint(_tip.position);
            _line.EndStroke();
        }

        private void HandleActivated(ActivateEventArgs args) => BeginStroke();

        private void HandleDeactivated(DeactivateEventArgs args) => FinishStroke();

        private void HandleReleased(SelectExitEventArgs args) => FinishStroke();

        private IEnumerator Sample()
        {
            var wait = new WaitForSeconds(_sampleSeconds);
            Vector3 last = _tip.position;
            _line.AddPoint(last);
            while (true)
            {
                yield return wait;
                if (Vector3.Distance(_tip.position, last) < _minMove)
                    continue;
                last = _tip.position;
                _line.AddPoint(last);
            }
        }

        private void StopDrawing()
        {
            if (_drawing == null)
                return;
            StopCoroutine(_drawing);
            _drawing = null;
        }
    }
}
