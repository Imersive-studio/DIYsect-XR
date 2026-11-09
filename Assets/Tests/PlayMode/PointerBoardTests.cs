// Starts a real in-process Netcode host and clients over UnityTransport on localhost (no cloud services).
// Authority under test: the server alone decides which organ each role points at. Clients only ask by Rpc;
// every peer then holds the same organ index and role, and the presenter words the label from the real dissection stage.
using System.Collections;
using DIYsect.Classroom;
using DIYsect.Dissection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class PointerBoardTests : NetworkedBodyTestBase
    {
        private PointerBoard _server;
        private PointerBoard _boardA;
        private PointerBoard _boardB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private GameObject _scalpel;
        private GameObject _mainCamera;

        [TearDown]
        public void DestroyScalpel()
        {
            if (_mainCamera != null)
                Object.Destroy(_mainCamera);
            if (_scalpel != null)
                Object.Destroy(_scalpel);
        }

        private IEnumerator StartTwoClients()
        {
            // The presenter places pointers for the scene's main camera and logs an error without one.
            _mainCamera = new GameObject("Test main camera") { tag = "MainCamera" };
            _mainCamera.AddComponent<Camera>();
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<PointerBoard>();
            Assert.IsNotNull(_server, "The body prefab has no PointerBoard. Run Tools > DIYsect > Add Organ Pointer.");
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            yield return WaitForBody(_managerA, found => _bodyA = found);
            yield return WaitForBody(_managerB, found => _bodyB = found);
            _boardA = _bodyA.GetComponent<PointerBoard>();
            _boardB = _bodyB.GetComponent<PointerBoard>();
        }

        private IEnumerator WaitForOrgan(PointerRole role, int expected)
        {
            foreach (PointerBoard board in new[] { _server, _boardA, _boardB })
            {
                PointerBoard captured = board;
                yield return WaitFor(() => captured.OrganOf(role) == expected, $"{role} pointer to be {expected} on {captured.name}",
                    () => $"Is {captured.OrganOf(role)}.");
            }
        }

        // Makes the given client the scalpel steerer, so the server calls that client the Student.
        private IEnumerator MakeSteerer(NetworkManager steererManager)
        {
            yield return SpawnScalpelWithSteerer(steererManager, scalpel => _scalpel = scalpel);
        }

        [UnityTest]
        public IEnumerator ClientPointingShowsTheSameOrganAndRoleOnEveryPeer()
        {
            yield return StartTwoClients();
            _boardA.Point(5);
            yield return WaitForOrgan(PointerRole.Professor, 5);
            Assert.AreEqual(PointerBoard.None, _server.OrganOf(PointerRole.Student));
            Assert.AreEqual(PointerBoard.None, _boardB.OrganOf(PointerRole.Student));

            _boardA.Point(9);
            yield return WaitForOrgan(PointerRole.Professor, 9);
        }

        [UnityTest]
        public IEnumerator StoppingClearsThePointerEverywhere()
        {
            yield return StartTwoClients();
            _boardA.Point(3);
            yield return WaitForOrgan(PointerRole.Professor, 3);
            _boardA.StopPointing();
            yield return WaitForOrgan(PointerRole.Professor, PointerBoard.None);
        }

        [UnityTest]
        public IEnumerator AnotherClientCannotStopMyPointer()
        {
            yield return StartTwoClients();
            _boardA.Point(3);
            yield return WaitForOrgan(PointerRole.Professor, 3);
            _boardB.StopPointing();
            // B's stop is sent first; A's later point proves the server handled both.
            _boardA.Point(4);
            yield return WaitForOrgan(PointerRole.Professor, 4);
        }

        [UnityTest]
        public IEnumerator InvalidOrganIndexIsRejectedAndLeavesThePointerAlone()
        {
            yield return StartTwoClients();
            int rejected = 0;
            PointerRejection? reason = null;
            _server.RequestRejected += (sender, why) => { rejected++; reason = why; };
            _boardA.Point(2);
            yield return WaitForOrgan(PointerRole.Professor, 2);

            _boardA.Point(-1);
            _boardA.Point(_serverBody.Organs.Count);
            _boardA.Point(int.MaxValue);
            yield return WaitFor(() => rejected == 3, "the server to reject three bad indexes", () => $"Rejected {rejected}.");
            Assert.AreEqual(PointerRejection.BadOrgan, reason);
            Assert.AreEqual(2, _server.OrganOf(PointerRole.Professor));
            Assert.AreEqual(2, _boardB.OrganOf(PointerRole.Professor));
        }

        [UnityTest]
        public IEnumerator TooManyRequestsInAShortTimeAreRefused()
        {
            yield return StartTwoClients();
            int tooFast = 0;
            _server.RequestRejected += (sender, why) => { if (why == PointerRejection.TooFast) tooFast++; };
            for (int i = 0; i < 40; i++)
                _boardA.Point(i % 3);
            yield return WaitFor(() => tooFast > 0, "the server to refuse the flood");
        }

        [UnityTest]
        public IEnumerator ProfessorAndStudentPointAtTheSameTimeWithoutReplacingEachOther()
        {
            yield return StartTwoClients();
            yield return MakeSteerer(_managerB);

            _boardA.Point(6);
            _boardB.Point(11);
            yield return WaitForOrgan(PointerRole.Professor, 6);
            yield return WaitForOrgan(PointerRole.Student, 11);

            _boardB.StopPointing();
            yield return WaitForOrgan(PointerRole.Student, PointerBoard.None);
            Assert.AreEqual(6, _boardA.OrganOf(PointerRole.Professor), "Stopping the student must not clear the professor.");
        }

        [UnityTest]
        public IEnumerator DisconnectingClearsThatClientsPointerOnly()
        {
            yield return StartTwoClients();
            yield return MakeSteerer(_managerB);
            _boardA.Point(6);
            _boardB.Point(11);
            yield return WaitForOrgan(PointerRole.Professor, 6);
            yield return WaitForOrgan(PointerRole.Student, 11);

            _managerA.Shutdown();
            yield return WaitFor(() => _server.OrganOf(PointerRole.Professor) == PointerBoard.None, "the professor pointer to clear after the disconnect");
            yield return WaitFor(() => _boardB.OrganOf(PointerRole.Professor) == PointerBoard.None, "client B to see it clear");
            Assert.AreEqual(11, _server.OrganOf(PointerRole.Student));
        }

        [UnityTest]
        public IEnumerator LateJoinerSeesTheCurrentPointer()
        {
            yield return StartTwoClients();
            _boardA.Point(7);
            yield return WaitForOrgan(PointerRole.Professor, 7);
            NetworkManager late = StartClient("Late");
            SharedBody lateBody = null;
            yield return WaitForBody(late, found => lateBody = found);
            var board = lateBody.GetComponent<PointerBoard>();
            yield return WaitFor(() => board.OrganOf(PointerRole.Professor) == 7, "the late client to see the pointer");
        }

        [UnityTest]
        public IEnumerator ClassmateTapShowsOnEveryPeerAndASecondTapClears()
        {
            yield return StartTwoClients();
            _boardA.PointAsClassmate(4);
            yield return WaitForOrgan(PointerRole.Classmate, 4);
            Assert.AreEqual(PointerBoard.None, _server.OrganOf(PointerRole.Professor), "The classmate slot is separate from the professor's.");

            _boardA.PointAsClassmate(4);
            yield return WaitForOrgan(PointerRole.Classmate, PointerBoard.None);

            _boardA.PointAsClassmate(8);
            yield return WaitForOrgan(PointerRole.Classmate, 8);
            _boardA.StopClassmate();
            yield return WaitForOrgan(PointerRole.Classmate, PointerBoard.None);
        }

        [UnityTest]
        public IEnumerator DisconnectingClearsTheClassmatePointer()
        {
            yield return StartTwoClients();
            _boardA.PointAsClassmate(6);
            yield return WaitForOrgan(PointerRole.Classmate, 6);
            _managerA.Shutdown();
            yield return WaitFor(() => _server.OrganOf(PointerRole.Classmate) == PointerBoard.None, "the classmate pointer to clear after the disconnect");
            yield return WaitFor(() => _boardB.OrganOf(PointerRole.Classmate) == PointerBoard.None, "client B to see it clear");
        }

        [UnityTest]
        public IEnumerator TheSteererCannotUseTheClassmatePointer()
        {
            yield return StartTwoClients();
            yield return MakeSteerer(_managerB);
            PointerRejection? reason = null;
            _server.RequestRejected += (sender, why) => reason = why;
            _boardB.PointAsClassmate(3);
            yield return WaitFor(() => reason == PointerRejection.NotAClassmate, "the server to refuse the steerer");
            Assert.AreEqual(PointerBoard.None, _server.OrganOf(PointerRole.Classmate));
        }

        [UnityTest]
        public IEnumerator ClassmateAndProfessorPointAtTheSameTimeWithoutReplacingEachOther()
        {
            yield return StartTwoClients();
            _boardA.Point(2);
            _boardB.PointAsClassmate(9);
            yield return WaitForOrgan(PointerRole.Professor, 2);
            yield return WaitForOrgan(PointerRole.Classmate, 9);
            var presenter = _server.GetComponent<PointerPresenter>();
            yield return WaitFor(() => presenter.Entries.Count == 2, "both pointers");
            StringAssert.StartsWith("Classmate:", presenter.Entries[1].Text);
            Assert.AreNotEqual(presenter.ColorOf(PointerRole.Classmate), presenter.ColorOf(PointerRole.Professor));
            Assert.AreNotEqual(presenter.ColorOf(PointerRole.Classmate), presenter.ColorOf(PointerRole.Student));
        }

        [UnityTest]
        public IEnumerator LabelSaysUnderTheLayerOnlyWhileThatLayerIsClosed()
        {
            yield return StartTwoClients();
            var progress = _serverBody.GetComponent<DissectionProgress>();
            var presenter = _bodyA.GetComponent<PointerPresenter>();
            int liver = IndexOf(_bodyA, "liver");
            int colon = IndexOf(_bodyA, "transverse_colon");

            _boardB.Point(liver);
            yield return WaitFor(() => presenter.Entries.Count == 1, "client A to show the pointer");
            Assert.AreEqual("Professor: Liver (under the skin)", presenter.Entries[0].Text);

            // The liver stays under the abdominal wall once the skin is open.
            OpenSkin(progress);
            yield return WaitFor(() => presenter.Entries.Count == 1 && presenter.Entries[0].HidingLayer == "the abdominal wall", "the label to name the abdominal wall");
            Assert.AreEqual("Professor: Liver (under the abdominal wall)", presenter.Entries[0].Text);

            Assert.AreEqual(StepResult.Accepted, progress.TryStep(DissectionAction.OpenRectus));
            yield return WaitFor(() => presenter.Entries.Count == 1 && presenter.Entries[0].HidingLayer == null, "the liver label to lose its 'under'");
            Assert.AreEqual("Professor: Liver", presenter.Entries[0].Text);

            _boardB.Point(colon);
            yield return WaitFor(() => presenter.Entries.Count == 1 && presenter.Entries[0].OrganIndex == colon, "the pointer to move to the colon");
            Assert.AreEqual("Professor: Transverse colon (under the omentum)", presenter.Entries[0].Text);
            Assert.AreEqual(StepResult.Accepted, progress.TryStep(DissectionAction.LiftOmentum));
            yield return WaitFor(() => presenter.Entries[0].HidingLayer == null, "the omentum label to go away once it is lifted");
            Assert.AreEqual("Professor: Transverse colon", presenter.Entries[0].Text);
        }

        [UnityTest]
        public IEnumerator StudentAndProfessorLabelsDifferInWordsAndSitOnTheirOwnOrgans()
        {
            yield return StartTwoClients();
            yield return MakeSteerer(_managerB);
            var presenter = _server.GetComponent<PointerPresenter>();
            _boardA.Point(1);
            _boardB.Point(2);
            yield return WaitFor(() => presenter.Entries.Count == 2, "both pointers");
            Assert.AreEqual(PointerRole.Professor, presenter.Entries[0].Role);
            Assert.AreEqual(PointerRole.Student, presenter.Entries[1].Role);
            StringAssert.StartsWith("Professor:", presenter.Entries[0].Text);
            StringAssert.StartsWith("Student:", presenter.Entries[1].Text);
            Assert.AreNotEqual(presenter.ColorOf(PointerRole.Professor), presenter.ColorOf(PointerRole.Student));
        }

        [UnityTest]
        public IEnumerator OffScreenOrganGetsAnEdgeArrowAndBothArrowsStayApart()
        {
            yield return StartTwoClients();
            yield return MakeSteerer(_managerB);
            var presenter = _server.GetComponent<PointerPresenter>();
            var cameraObject = new GameObject("Pointer test camera");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                // Looking straight away from the body, so every organ is behind the camera.
                camera.transform.position = _serverBody.transform.position;
                camera.transform.rotation = Quaternion.LookRotation(-(_serverBody.Organs[1].transform.position - camera.transform.position).normalized + Vector3.forward * 0.001f);
                presenter.SetViewCamera(camera);
                _boardA.Point(1);
                _boardB.Point(1);
                yield return WaitFor(() => presenter.Entries.Count == 2, "both pointers");
                presenter.UpdatePlacements();
                PointerPlacement professor = presenter.Entries[0].Placement;
                PointerPlacement student = presenter.Entries[1].Placement;
                Assert.IsFalse(professor.OnScreen);
                Assert.IsFalse(student.OnScreen);
                Assert.GreaterOrEqual(Vector2.Distance(professor.Position, student.Position), presenter.EdgeMargin);
            }
            finally
            {
                Object.Destroy(cameraObject);
            }
        }

        private static int IndexOf(SharedBody body, string organName)
        {
            for (int i = 0; i < body.Organs.Count; i++)
                if (body.Organs[i].name == organName)
                    return i;
            Assert.Fail($"No organ named {organName}.");
            return -1;
        }

        private static void OpenSkin(DissectionProgress server)
        {
            for (float t = 0.1f; t <= 1.0001f; t += 0.1f)
                server.EvaluateSample(Vector3.Lerp(server.LineStart, server.LineEnd, Mathf.Min(t, 1f)));
            Assert.IsTrue(server.TryOpen());
        }
    }
}
