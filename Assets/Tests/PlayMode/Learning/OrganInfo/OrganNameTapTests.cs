// Starts a real in-process Netcode host and a classmate over UnityTransport on localhost (no cloud services).
// Authority under test: none on the name card (local presentation). A quick tap names the organ and places no pin, a hold places the
// pin (the server decides) and shows no name, and a running quiz round claims the tap so no name is shown.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using TMPro;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DIYsect.Learning.Tests
{
    public class OrganNameTapTests : NetworkedBodyTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private const string LibraryPath = "Assets/Scripts/Learning/OrganInfo/OrganInfoLibrary.asset";
        private const int OrganIndex = 4;

        private QuizRound _server;
        private QuizRound _quizA;
        private QuestionBoard _boardServer;
        private SharedBody _bodyA;
        private GameObject _holder;
        private PointerGestureTracker _tracker;
        private Camera _camera;
        private OrganNameTapInput _names;
        private GameObject _card;

        [TearDown]
        public void DestroyHolder()
        {
            if (_holder != null)
                Object.Destroy(_holder);
            if (_card != null)
                Object.Destroy(_card);
        }

        private static OrganInfoLibrary LoadLibrary()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<OrganInfoLibrary>(LibraryPath);
#else
            throw new System.NotSupportedException("These tests load the organ library from the project and run in the editor only.");
#endif
        }

        private static void Set(object target, string field, object value)
        {
            target.GetType().GetField(field, Private).SetValue(target, value);
        }

        // One host and one classmate, a rig on the classmate's body with the name card, the pin input and the quiz answer input,
        // and a camera looking straight down at one organ.
        private IEnumerator StartClassmateRig(float holdSeconds = 0.3f, float fadeHold = 2.5f, float fade = 1f)
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuizRound>();
            _boardServer = _serverBody.GetComponent<QuestionBoard>();
            NetworkManager a = StartClient("ClientA");
            yield return WaitForBody(a, found => _bodyA = found);
            _quizA = _bodyA.GetComponent<QuizRound>();
            QuestionBoard boardA = _bodyA.GetComponent<QuestionBoard>();
            yield return WaitFor(() => _quizA.OrganCount == _serverBody.Organs.Count, "the client to receive one count per organ");

            // The host, the client and the scene share one world, so their bodies overlap. Only the classmate's body is hit.
            foreach (Collider other in _serverBody.GetComponentsInChildren<Collider>())
                other.enabled = false;
            Collider organ = _bodyA.Organs[OrganIndex].GetComponentInChildren<Collider>();
            Physics.SyncTransforms();

            _card = new GameObject("Card", typeof(CanvasGroup));
            var label = new GameObject("Name", typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            label.transform.SetParent(_card.transform, false);

            _holder = new GameObject("TapRig");
            _holder.SetActive(false);
            _holder.transform.SetPositionAndRotation(organ.bounds.center + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            _camera = _holder.AddComponent<Camera>();
            _tracker = _holder.AddComponent<PointerGestureTracker>();
            var answers = _holder.AddComponent<QuizAnswerInput>();
            Set(answers, "_quiz", _quizA);
            Set(answers, "_gestures", _tracker);
            Set(answers, "_camera", _camera);
            var pins = _holder.AddComponent<PinPlacementInput>();
            Set(pins, "_board", boardA);
            Set(pins, "_gestures", _tracker);
            Set(pins, "_camera", _camera);
            Set(pins, "_tapClaimers", new MonoBehaviour[] { answers });
            _names = _holder.AddComponent<OrganNameTapInput>();
            Set(_names, "_body", _bodyA);
            Set(_names, "_gestures", _tracker);
            Set(_names, "_camera", _camera);
            Set(_names, "_library", LoadLibrary());
            Set(_names, "_card", _card.GetComponent<CanvasGroup>());
            Set(_names, "_label", label);
            Set(_names, "_holdSeconds", fadeHold);
            Set(_names, "_fadeSeconds", fade);
            Set(_names, "_tapClaimers", new MonoBehaviour[] { answers });
            _holder.SetActive(true);
        }

        private Vector2 CentreOfScreen() => new Vector2(_camera.pixelWidth / 2f, _camera.pixelHeight / 2f);

        private void Raise(string eventName)
        {
            var handlers = (System.Action<Vector2>)typeof(PointerGestureTracker).GetField(eventName, Private).GetValue(_tracker);
            Assert.IsNotNull(handlers, $"Nothing listens for {eventName}.");
            handlers(CentreOfScreen());
        }

        private int AimedOrgan()
        {
            Physics.Raycast(_camera.ScreenPointToRay(CentreOfScreen()), out RaycastHit hit);
            Assert.IsNotNull(hit.collider, "The test ray should hit the classmate's organ.");
            int index = QuizOrganRaycast.IndexOf(_bodyA, hit.collider.transform);
            Assert.GreaterOrEqual(index, 0, $"The test ray hit {hit.collider.name}, which is not an organ.");
            return index;
        }

        [UnityTest]
        public IEnumerator AQuickTapShowsTheOrgansNameAndPlacesNoPin()
        {
            yield return StartClassmateRig();
            string organName = _bodyA.Organs[AimedOrgan()].name;
            Assert.IsTrue(LoadLibrary().TryGet(organName, out OrganInfoEntry entry), $"{organName} has no library entry.");

            Raise("Tapped");

            Assert.IsTrue(_names.IsShowing);
            StringAssert.Contains(entry.DisplayName, _names.CurrentText);
            StringAssert.Contains(entry.System, _names.CurrentText, "A short line with the organ's system, not a paragraph.");
            StringAssert.DoesNotContain(entry.Function, _names.CurrentText, "The long function sentence is left to the headset cards.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.AreEqual(0, _boardServer.Count, "A quick tap names the organ; it must not place a pin.");
        }

        [UnityTest]
        public IEnumerator AHoldPlacesAPinAndShowsNoName()
        {
            yield return StartClassmateRig();
            int aimed = AimedOrgan();

            Raise("Held");

            yield return WaitFor(() => _boardServer.Count == 1, "the pin request to reach the server");
            Assert.AreEqual(aimed, _boardServer[0].OrganIndex);
            Assert.IsFalse(_names.IsShowing, "A hold pins; it does not name the organ.");
        }

        [UnityTest]
        public IEnumerator TheCardFadesAwayAfterAFewSeconds()
        {
            yield return StartClassmateRig(fadeHold: 0.2f, fade: 0.2f);

            Raise("Tapped");
            Assert.IsTrue(_names.IsShowing);

            yield return WaitFor(() => !_names.IsShowing, "the card to fade away");
        }

        [UnityTest]
        public IEnumerator AClickOnNothingShowsNoName()
        {
            yield return StartClassmateRig();
            _holder.transform.position += Vector3.up * 50f;
            _holder.transform.rotation = Quaternion.LookRotation(Vector3.up);
            Physics.SyncTransforms();

            Raise("Tapped");

            Assert.IsFalse(_names.IsShowing);
        }

        [UnityTest]
        public IEnumerator ATapDuringAQuizRoundAnswersTheQuizAndShowsNoName()
        {
            yield return StartClassmateRig();
            _server.RequestStart(AimedOrgan());
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking, "the round to reach the classmate");

            Raise("Tapped");

            yield return WaitFor(() => _server.RightCount == 1, "the answer to be judged right");
            Assert.IsFalse(_names.IsShowing, "The name would give the answer away, so a round shows none.");
            Assert.AreEqual(0, _boardServer.Count);
        }

        [UnityTest]
        public IEnumerator AfterTheRoundEndsTappingNamesOrgansAgain()
        {
            yield return StartClassmateRig();
            _server.RequestStart(AimedOrgan());
            yield return WaitFor(() => _quizA.Phase == QuizPhase.Asking, "the round to reach the classmate");
            _server.RequestEnd();
            yield return WaitFor(() => _quizA.Phase != QuizPhase.Asking, "the round to end");

            Raise("Tapped");

            Assert.IsTrue(_names.IsShowing);
        }
    }
}
