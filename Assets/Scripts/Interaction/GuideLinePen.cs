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
    /// The stroke follows <see cref="GrabRequest"/>: pressing the trigger asks the server for the stroke, and the tip
    /// is sampled only once the server says yes (if someone else is drawing, nothing happens). Sampling runs on a
    /// short timer and sends only when the tip moved, so there is no per-frame code. Moving the table under the pen ends the
    /// stroke. Releasing the trigger, putting the pen down or disabling it ends the stroke; the host clearing the line stops it.
    /// </summary>
    [RequireComponent(typeof(XRGrabInteractable))]
    public class GuideLinePen : MonoBehaviour
    {
        [SerializeField] private GuideLine _line;
        [SerializeField] private Transform _tip;
        [SerializeField, Min(0.01f)] private float _sampleSeconds = 0.03f;
        [SerializeField, Min(0.001f)] private float _minMove = 0.005f;

        private const float BodyMovedMeters = 0.001f;
        private const float BodyMovedDegrees = 0.1f;

        private XRGrabInteractable _grab;
        private Coroutine _drawing;
        private bool _endedByTableMove;
        private readonly GrabRequest _request = new GrabRequest();

        /// <summary>This pen's stroke phase: Granted while it is drawing.</summary>
        public GrabPhase Phase => _request.Phase;

        public bool IsDrawing => _request.Phase == GrabPhase.Granted;

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
            _line.StrokeResolved += HandleStrokeResolved;
            _line.StrokeRevoked += HandleStrokeRevoked;
        }

        private void OnDisable()
        {
            Apply(_request.Handle(GrabRequest.Event.Disable));
            _grab.activated.RemoveListener(HandleActivated);
            _grab.deactivated.RemoveListener(HandleDeactivated);
            _grab.selectExited.RemoveListener(HandleReleased);
            if (_line != null)
            {
                _line.StrokeResolved -= HandleStrokeResolved;
                _line.StrokeRevoked -= HandleStrokeRevoked;
            }
            StopDrawing();
        }

        /// <summary>Asks for a stroke at the current tip, as the trigger does. Public so tests can drive it.</summary>
        public void BeginStroke() => Apply(_request.Handle(GrabRequest.Event.Select));

        /// <summary>Ends the stroke, as releasing the trigger does.</summary>
        public void FinishStroke() => Apply(_request.Handle(GrabRequest.Event.Exit));

        private void HandleActivated(ActivateEventArgs args) => BeginStroke();

        private void HandleDeactivated(DeactivateEventArgs args) => FinishStroke();

        private void HandleReleased(SelectExitEventArgs args) => FinishStroke();

        private void HandleStrokeResolved(bool granted) => Apply(_request.HandleAnswer(granted));

        private void HandleStrokeRevoked() => Apply(_request.Handle(GrabRequest.Event.Revoked));

        // Carries out what the stroke state machine asked for. A pen has no selection to cancel: it stays in hand.
        private void Apply(GrabRequest.Actions actions)
        {
            if ((actions & GrabRequest.Actions.SendRequest) != 0)
                _line.StartStroke();
            if ((actions & GrabRequest.Actions.StopStreaming) != 0)
                StopDrawing();
            if ((actions & GrabRequest.Actions.SendRelease) != 0)
            {
                // A table move leaves the tip far from the line, so the closing point is left out.
                if (!_endedByTableMove)
                    _line.AddPoint(_tip.position);
                _line.EndStroke();
            }
            if ((actions & GrabRequest.Actions.StartStreaming) != 0)
                _drawing ??= StartCoroutine(Sample());
        }

        private IEnumerator Sample()
        {
            var wait = new WaitForSeconds(_sampleSeconds);
            Vector3 last = _tip.position;
            Pose body = new Pose(_line.transform.position, _line.transform.rotation);
            _line.AddPoint(last);
            while (true)
            {
                yield return wait;
                // The table was moved (A or X, or its handle): the pen did not move with it, so the next point would
                // jump across the body. End the stroke instead.
                if (BodyMoved(body))
                {
                    _endedByTableMove = true;
                    FinishStroke();
                    _endedByTableMove = false;
                    yield break;
                }
                if (Vector3.Distance(_tip.position, last) < _minMove)
                    continue;
                last = _tip.position;
                _line.AddPoint(last);
            }
        }

        private bool BodyMoved(Pose since)
        {
            return Vector3.Distance(_line.transform.position, since.position) > BodyMovedMeters
                || Quaternion.Angle(_line.transform.rotation, since.rotation) > BodyMovedDegrees;
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
