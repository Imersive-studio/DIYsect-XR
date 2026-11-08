// State bug: the depth handle stays grabbed after the student put the scalpel down.
// Ordering used to make it deterministic: the host is the student. The test waits until the server has granted the
// professor control, then in that same frame the host puts the scalpel down. The host's call runs on the server at
// once, so the server clears control right after granting it, and the professor hears "granted" afterwards.
using System.Collections;
using DIYsect.Networking;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public class DepthHandleStuckTests : ScalpelSessionTestBase
    {
        [UnityTest]
        public IEnumerator HandleIsReleasedAndProfessorIsNotControllerWhenControlIsClearedBeforeTheGrantArrives()
        {
            // Bug 3: DepthControlInput starts streaming on a late "granted" and the handle stays selected,
            // although the server already cleared control.
            yield return StartScalpelSession();

            _serverSteering.RequestSteer();
            yield return WaitFor(() => _serverSteering.Steerer == _host.LocalClientId, "the host to steer the scalpel");

            // Built inactive so the serialized hand is set before Awake runs.
            GameObject handleObject = Track(GameObject.CreatePrimitive(PrimitiveType.Sphere));
            handleObject.SetActive(false);
            handleObject.transform.localScale = Vector3.one * 0.05f;
            handleObject.AddComponent<Rigidbody>().isKinematic = true;
            var handle = handleObject.AddComponent<XRGrabInteractable>();
            var input = handleObject.AddComponent<DepthControlInput>();
            SetSerialized(input, "_hand", _clientHand);
            handleObject.SetActive(true);

            XRDirectInteractor professorHand = CreateHand("Professor hand");
            yield return null;

            bool? answer = null;
            void OnResolved(bool granted) => answer = granted;
            _clientHand.ControlResolved += OnResolved;

            professorHand.StartManualInteraction((IXRSelectInteractable)handle);
            Assert.IsTrue(handle.isSelected, "The professor should hold the handle.");

            yield return WaitFor(() => _serverHand.Controller == _clientManager.LocalClientId, "the server to grant the professor control");
            _serverSteering.ReleaseSteer();

            yield return WaitFor(() => answer.HasValue, "the professor to hear the server's answer");
            _clientHand.ControlResolved -= OnResolved;
            Assert.IsTrue(answer.Value, "Setup: control was granted before the scalpel was put down.");

            yield return WaitFor(() => _serverHand.Controller == GuidingHand.NoController, "the server to clear control");
            yield return WaitFor(() => !handle.isSelected, "the professor's selection of the handle to be cancelled",
                () => $"Controller on the professor is {_clientHand.Controller}.");
            Assert.IsFalse(_clientHand.IsControlledByLocalClient, "The professor must not be the controller.");
            Assert.AreEqual(GuidingHand.NoController, _clientHand.Controller);
            Assert.IsFalse(professorHand.hasSelection);
        }
    }
}
