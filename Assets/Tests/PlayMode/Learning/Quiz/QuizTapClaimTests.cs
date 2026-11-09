// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: none on the input side. A classmate's tap either answers the quiz (server judges) or asks for a pin (server decides),
// never both.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Learning.Tests
{
    public class QuizTapClaimTests : NetworkedBodyTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private QuizRound _server;
        private QuizRound _quizA;
        private QuestionBoard _boardA;
        private QuestionBoard _boardServer;
        private GameObject _holder;
        private PointerGestureTracker _tracker;
        private Camera _camera;
        private int _aimedOrgan;

        [TearDown]
        public void DestroyHolder()
        {
            if (_holder != null)
                Object.Destroy(_holder);
        }

        // One host and one classmate, a tap rig on the classmate's body and a camera looking straight down at one organ.
        private IEnumerator StartClassmateRig(bool claimQuiz)
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuizRound>();
            _boardServer = _serverBody.GetComponent<QuestionBoard>();
            NetworkManager a = StartClient("ClientA");
            SharedBody bodyA = null;
            yield return WaitForBody(a, found => bodyA = found);
            _quizA = bodyA.GetComponent<QuizRound>();
            _boardA = bodyA.GetComponent<QuestionBoard>();
            yield return WaitFor(() => _quizA.OrganCount == _serverBody.Organs.Count, "the client to receive one count per organ");

            // The host, the client and the scene share one world, so their bodies overlap. Only the classmate's body is hit.
            foreach (Collider other in _serverBody.GetComponentsInChildren<Collider>())
                other.enabled = false;

            Collider organ = bodyA.Organs[4].GetComponentInChildren<Collider>();
            Physics.SyncTransforms();
            Vector3 centre = organ.bounds.center;

            _holder = new GameObject("TapRig");
            _holder.SetActive(false);
            _holder.transform.SetPositionAndRotation(centre + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            _camera = _holder.AddComponent<Camera>();
            _tracker = _holder.AddComponent<PointerGestureTracker>();
            var answers = _holder.AddComponent<QuizAnswerInput>();
            typeof(QuizAnswerInput).GetField("_quiz", Private).SetValue(answers, _quizA);
            typeof(QuizAnswerInput).GetField("_gestures", Private).SetValue(answers, _tracker);
            typeof(QuizAnswerInput).GetField("_camera", Private).SetValue(answers, _camera);
            var pins = _holder.AddComponent<PinPlacementInput>();
            typeof(PinPlacementInput).GetField("_board", Private).SetValue(pins, _boardA);
            typeof(PinPlacementInput).GetField("_gestures", Private).SetValue(pins, _tracker);
            typeof(PinPlacementInput).GetField("_camera", Private).SetValue(pins, _camera);
            if (claimQuiz)
                typeof(PinPlacementInput).GetField("_tapClaimers", Private).SetValue(pins, new MonoBehaviour[] { answers });
            _holder.SetActive(true);

            Physics.Raycast(_camera.ScreenPointToRay(CentreOfScreen()), out RaycastHit hit);
            Assert.IsNotNull(hit.collider, "The test ray should hit the classmate's organ.");
            _aimedOrgan = QuizOrganRaycast.IndexOf(bodyA, hit.collider.transform);
            Assert.GreaterOrEqual(_aimedOrgan, 0, $"The test ray hit {hit.collider.name}, which is not an organ.");
        }

        private Vector2 CentreOfScreen() => new Vector2(_camera.pixelWidth / 2f, _camera.pixelHeight / 2f);

        // The tracker's Tapped event is what a real click or short touch raises; Held is a press held still for half a second.
        private void Raise(string eventName)
        {
            var handlers = (System.Action<Vector2>)typeof(PointerGestureTracker).GetField(eventName, Private).GetValue(_tracker);
            Assert.IsNotNull(handlers, $"Nothing listens for {eventName}.");
            handlers(CentreOfScreen());
        }

        private void Tap() => Raise("Tapped");

        private void Hold() => Raise("Held");

        [UnityTest]
        public IEnumerator ATapDuringARoundAnswersTheQuizAndPlacesNoPin()
        {
            yield return StartClassmateRig(claimQuiz: true);
            _server.RequestStart(_aimedOrgan);
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking, "the round to reach the classmate");

            Tap();

            yield return WaitFor(() => _server.RightCount == 1, "the answer to be judged right", () => $"Right {_server.RightCount}, wrong {_server.WrongCount}.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.AreEqual(0, _boardServer.Count, "The tap that answered the quiz must not also place a question pin.");
            Assert.AreEqual(0, _boardA.Count);
        }

        [UnityTest]
        public IEnumerator AHoldWithNoRoundPlacesAPin()
        {
            yield return StartClassmateRig(claimQuiz: true);

            Hold();

            yield return WaitFor(() => _boardServer.Count == 1, "the pin request to reach the server");
            Assert.AreEqual(_aimedOrgan, _boardServer[0].OrganIndex);
            Assert.AreEqual(0, _server.RightCount + _server.WrongCount, "With no round running the tap is not an answer.");
        }

        [UnityTest]
        public IEnumerator AHoldDuringARoundPlacesNoPinWhileTheQuizClaimsTheTap()
        {
            yield return StartClassmateRig(claimQuiz: true);
            _server.RequestStart(_aimedOrgan);
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking, "the round to reach the classmate");

            Hold();

            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.AreEqual(0, _boardServer.Count, "A running round claims the press, so no pin is placed.");
        }

        [UnityTest]
        public IEnumerator WithoutTheClaimTheOldBehaviourRemains()
        {
            yield return StartClassmateRig(claimQuiz: false);
            _server.RequestStart(_aimedOrgan);
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking, "the round to reach the classmate");

            Tap();
            Hold();

            yield return WaitFor(() => _server.RightCount == 1 && _boardServer.Count == 1, "both the answer (tap) and the pin (hold) when nothing claims the press");
        }
    }
}
