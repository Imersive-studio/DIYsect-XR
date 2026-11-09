// Starts a real in-process Netcode host and a client over UnityTransport on localhost (no cloud services).
// Authority under test: none. The presenter only reads head poses the board already replicated and draws a local dot.
using System.Collections;
using DIYsect.Classroom;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class GazePresenterTests : NetworkedBodyTestBase
    {
        [UnityTest]
        public IEnumerator APeerShowsTheProfessorsGazeDotWhereTheProfessorLooks()
        {
            yield return StartHostAndSpawnBody();
            HeadPoseBoard server = _serverBody.GetComponent<HeadPoseBoard>();
            Assert.IsNotNull(server, "The body prefab has no HeadPoseBoard. Run Tools > DIYsect > Add Head Pose.");
            NetworkManager client = StartClient("Client");
            SharedBody clientBody = null;
            yield return WaitForBody(client, found => clientBody = found);

            // This peer is a phone: it publishes no head, so it hides no dot.
            var presenter = clientBody.gameObject.AddComponent<GazePresenter>();
            presenter.OverrideOwnSlot(null);

            Collider organ = clientBody.Organs[0].GetComponentInChildren<Collider>();
            Assert.IsNotNull(organ, "The first organ has no collider.");
            Vector3 target = organ.bounds.center;
            Vector3 headWorld = target + Vector3.up * 0.4f;
            Quaternion lookWorld = Quaternion.LookRotation(Vector3.down, Vector3.forward);
            Transform body = server.transform;
            server.Publish(body.InverseTransformPoint(headWorld), Quaternion.Inverse(body.rotation) * lookWorld);

            yield return WaitFor(() => presenter.IsShown(PointerRole.Professor), "the professor's gaze dot to appear");
            Assert.IsFalse(presenter.IsShown(PointerRole.Student), "Nobody is publishing a student head.");
            Vector3 dot = presenter.DotPosition(PointerRole.Professor);
            Vector2 sideways = new Vector2(dot.x - target.x, dot.z - target.z);
            Assert.Less(sideways.magnitude, 0.02f, "The dot should lie on the line the professor looks along.");
            Assert.Less(dot.y, headWorld.y, "The dot lies on the body below the head.");
        }
    }
}
