// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: none on the input side. The student's headset only sends its answer; the server judges it.
using System.Collections;
using System.Reflection;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.TestTools;

namespace DIYsect.Learning.Tests
{
    public class QuizHeadsetAnswerTests : NetworkedBodyTestBase
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private QuizRound _server;
        private QuizRound _student;
        private GameObject _rig;
        private GameObject _card;
        private Transform _aim;
        private QuizHeadsetAnswer _answer;
        private int _aimedOrgan;

        [TearDown]
        public void DestroyRig()
        {
            if (_rig != null)
                Object.Destroy(_rig);
            if (_card != null)
                Object.Destroy(_card);
        }

        // One host (the professor) and one client (the student), with a controller held above one organ, pointing down.
        private IEnumerator StartStudentRig()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<QuizRound>();
            NetworkManager student = StartClient("Student");
            SharedBody body = null;
            yield return WaitForBody(student, found => body = found);
            _student = body.GetComponent<QuizRound>();
            yield return WaitFor(() => _student.OrganCount == _serverBody.Organs.Count, "the student to receive one count per organ");
            // The host, the client and the scene share one world, so their bodies overlap. Only the student's body is hit.
            foreach (Collider other in _serverBody.GetComponentsInChildren<Collider>())
                other.enabled = false;
            Collider organ = body.Organs[4].GetComponentInChildren<Collider>();
            Physics.SyncTransforms();
            Vector3 centre = organ.bounds.center;

            _card = new GameObject("StudentQuizCard");
            TMP_Text question = new GameObject("Question").AddComponent<TextMeshPro>();
            question.transform.SetParent(_card.transform, false);
            TMP_Text status = new GameObject("Status").AddComponent<TextMeshPro>();
            status.transform.SetParent(_card.transform, false);
            _card.SetActive(false);

            _rig = new GameObject("StudentRig");
            _rig.SetActive(false);
            _aim = new GameObject("Right Controller").transform;
            _aim.SetParent(_rig.transform, false);
            _aim.SetPositionAndRotation(centre + Vector3.up * 0.5f, Quaternion.LookRotation(Vector3.down, Vector3.forward));
            _answer = _rig.AddComponent<QuizHeadsetAnswer>();
            typeof(QuizHeadsetAnswer).GetField("_quiz", Private).SetValue(_answer, _student);
            typeof(QuizHeadsetAnswer).GetField("_rightAim", Private).SetValue(_answer, _aim);
            typeof(QuizHeadsetAnswer).GetField("_card", Private).SetValue(_answer, _card);
            typeof(QuizHeadsetAnswer).GetField("_question", Private).SetValue(_answer, question);
            typeof(QuizHeadsetAnswer).GetField("_status", Private).SetValue(_answer, status);
            _rig.SetActive(true);

            Physics.Raycast(new Ray(_aim.position, _aim.forward), out RaycastHit hit);
            Assert.IsNotNull(hit.collider, "The test ray should hit the student's organ.");
            _aimedOrgan = QuizOrganRaycast.IndexOf(body, hit.collider.transform);
            Assert.GreaterOrEqual(_aimedOrgan, 0, $"The test ray hit {hit.collider.name}, which is not an organ.");
        }

        private string Status => _card.transform.Find("Status").GetComponent<TMP_Text>().text;

        [UnityTest]
        public IEnumerator TheCardShowsOnlyWhileARoundIsOn()
        {
            yield return StartStudentRig();
            Assert.IsFalse(_card.activeSelf, "No round, no card.");
            _server.RequestStart(_aimedOrgan, "Find this organ");
            yield return WaitFor(() => _card.activeSelf, "the card to show when the professor starts a round");
            Assert.AreEqual(QuizMessages.HeadsetHint, Status);
            _server.RequestEnd();
            _server.RequestClose();
            yield return WaitFor(() => !_card.activeSelf, "the card to hide once the round is cleared");
        }

        [UnityTest]
        public IEnumerator ThePulledTriggerAnswersTheRoundOnce()
        {
            yield return StartStudentRig();
            Assert.IsFalse(_answer.TryAnswerFrom(_aim), "With no round running the trigger is not an answer.");
            _server.RequestStart(_aimedOrgan);
            yield return WaitFor(() => _answer.IsAsking, "the round to reach the student");

            Assert.IsTrue(_answer.TryAnswerFrom(_aim), "Pointing at an organ during a round sends an answer.");
            yield return WaitFor(() => _server.RightCount == 1, "the student's answer to be judged right",
                () => $"Right {_server.RightCount}, wrong {_server.WrongCount}.");
            yield return WaitFor(() => Status.StartsWith("Right!"), "the card to show the verdict", () => $"Status: {Status}");

            Assert.IsFalse(_answer.TryAnswerFrom(_aim), "A second pull in the same round sends nothing.");
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.AreEqual(1, _server.RightCount + _server.WrongCount);
        }

        [UnityTest]
        public IEnumerator EachControllerTriggerAnswersUsingItsOwnAim()
        {
            var input = new InputTestFixture();
            input.Setup();
            try
            {
                InputSystem.RegisterLayout<XRSimulatedController>();
                var left = InputSystem.AddDevice<XRSimulatedController>();
                var right = InputSystem.AddDevice<XRSimulatedController>();
                InputSystem.SetDeviceUsage(left, CommonUsages.LeftHand);
                InputSystem.SetDeviceUsage(right, CommonUsages.RightHand);
                yield return StartStudentRig();
                var leftAim = new GameObject("Left Controller").transform;
                leftAim.SetParent(_rig.transform, false);
                leftAim.SetPositionAndRotation(_aim.position, _aim.rotation);
                typeof(QuizHeadsetAnswer).GetField("_leftAim", Private).SetValue(_answer, leftAim);
                Quaternion towardOrgan = _aim.rotation;
                _aim.rotation = Quaternion.LookRotation(Vector3.up);

                _server.RequestStart(_aimedOrgan);
                yield return WaitFor(() => _answer.IsAsking, "the first round");
                input.Press(left.triggerButton);
                yield return WaitFor(() => _server.RightCount == 1, "the left trigger to answer with the left aim");
                input.Release(left.triggerButton);
                input.Press(left.triggerButton);
                input.Release(left.triggerButton);
                yield return null;
                Assert.AreEqual(1, _server.RightCount + _server.WrongCount);

                _server.RequestEnd();
                _server.RequestClose();
                yield return WaitFor(() => !_answer.IsAsking, "the first round to end");
                _aim.rotation = towardOrgan;
                leftAim.rotation = Quaternion.LookRotation(Vector3.up);
                _server.RequestStart(_aimedOrgan);
                yield return WaitFor(() => _answer.IsAsking && !_answer.HasAnsweredThisRound, "the next round");
                input.Press(right.triggerButton);
                yield return WaitFor(() => _server.RightCount == 1, "the right trigger to answer with the right aim");
                input.Release(right.triggerButton);
            }
            finally
            {
                if (_answer != null)
                    _answer.enabled = false;
                input.TearDown();
            }
        }

        [UnityTest]
        public IEnumerator PointingAtNothingSendsNoAnswer()
        {
            yield return StartStudentRig();
            _server.RequestStart(_aimedOrgan);
            yield return WaitFor(() => _answer.IsAsking, "the round to reach the student");
            _aim.rotation = Quaternion.LookRotation(Vector3.up);
            Assert.IsFalse(_answer.TryAnswerFrom(_aim));
            Assert.AreEqual(QuizMessages.HeadsetMissed, Status);
            Assert.IsFalse(_answer.HasAnsweredThisRound, "A miss does not use up the student's answer.");
        }
    }
}
