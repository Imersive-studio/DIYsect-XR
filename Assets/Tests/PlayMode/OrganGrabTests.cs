// Authority under test: the server grants a grab only if the organ is free, and frees it on release or disconnect.
using System.Collections;
using DIYsect.Interaction;
using NUnit.Framework;
using Unity.Netcode;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class OrganGrabTests : NetworkedBodyTestBase
    {
        private const int OrganIndex = 3;
        private const float PositionTolerance = 0.005f;

        private OrganGrabAuthority _server;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;
        private OrganGrabAuthority _authorityA;
        private OrganGrabAuthority _authorityB;

        [UnityTest]
        public IEnumerator OnlyOneClientCanGrabAnOrgan()
        {
            yield return StartTwoClients();

            yield return Grab(_authorityA, expectGranted: true);
            Assert.AreEqual(_managerA.LocalClientId, _server.GetHolder(OrganIndex));

            yield return Grab(_authorityB, expectGranted: false);
            Assert.AreEqual(_managerA.LocalClientId, _server.GetHolder(OrganIndex), "The rejected grab must not change the holder.");
            // The server's answer can reach client A before the replicated holder list does, so wait for it.
            yield return WaitFor(() => _authorityA.IsHeldByLocalClient(OrganIndex), "client A to see that it holds the organ");
            Assert.IsFalse(_authorityB.IsHeldByLocalClient(OrganIndex));
        }

        [UnityTest]
        public IEnumerator HolderMovesTheOrganAndEveryoneSeesIt()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);

            Vector3 position = _serverBody.Organs[OrganIndex].transform.localPosition + new Vector3(0.2f, 0.1f, -0.15f);
            Quaternion rotation = Quaternion.Euler(10f, 40f, 5f);
            _authorityA.SendPose(OrganIndex, position, rotation);

            yield return WaitFor(() => Near(_serverBody, position, rotation), "the server organ to move");
            yield return WaitFor(() => Near(_bodyB, position, rotation), "client B to see the organ move");

            // Client B holds nothing, so its pose is ignored.
            _authorityB.SendPose(OrganIndex, Vector3.zero, Quaternion.identity);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(Near(_serverBody, position, rotation), "A pose from a client that is not the holder must be ignored.");
        }

        [UnityTest]
        public IEnumerator ReleasedOrganCanBeGrabbedByAnotherClient()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);

            _authorityB.Release(OrganIndex);
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(_managerA.LocalClientId, _server.GetHolder(OrganIndex), "Only the holder may release.");

            _authorityA.Release(OrganIndex);
            yield return WaitFor(() => _server.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the server to free the organ");
            yield return WaitFor(() => _authorityB.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "client B to see the organ free");

            yield return Grab(_authorityB, expectGranted: true);
            Assert.AreEqual(_managerB.LocalClientId, _server.GetHolder(OrganIndex));
        }

        [UnityTest]
        public IEnumerator DisconnectingHolderFreesTheOrgan()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);

            _clients.Remove(_managerA);
            DestroyManager(_managerA);
            yield return WaitFor(() => _server.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the server to free the organ of the disconnected client");
            yield return WaitFor(() => _authorityB.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "client B to see the organ free");

            yield return Grab(_authorityB, expectGranted: true);
        }

        [UnityTest]
        public IEnumerator ClientThatRejoinsCanGrabAgain()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);

            _clients.Remove(_managerA);
            DestroyManager(_managerA);
            yield return WaitFor(() => _server.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the server to free the organ");

            NetworkManager rejoined = StartClient("ClientARejoined");
            SharedBody rejoinedBody = null;
            yield return WaitForBody(rejoined, body => rejoinedBody = body);
            OrganGrabAuthority rejoinedAuthority = rejoinedBody.GetComponent<OrganGrabAuthority>();
            yield return WaitFor(() => rejoinedAuthority.IsSpawned, "the rejoined client to spawn the authority");
            Assert.AreEqual(OrganGrabAuthority.NoHolder, rejoinedAuthority.GetHolder(OrganIndex));
            yield return Grab(rejoinedAuthority, expectGranted: true);
        }

        [UnityTest]
        public IEnumerator HolderKeepsItsLocalPoseWhileGrabbedAndConvergesAfterRelease()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);
            // The holder's own copy keeps its local pose only once it knows it is the holder, so wait for that.
            yield return WaitFor(() => _authorityA.IsHeldByLocalClient(OrganIndex), "client A to see that it holds the organ");

            // Stands in for XRI moving the organ on the holder's machine. The server has not heard of this pose yet.
            Transform organ = _bodyA.Organs[OrganIndex].transform;
            Vector3 localPosition = organ.localPosition + new Vector3(0.3f, 0f, 0.1f);
            Quaternion localRotation = Quaternion.Euler(0f, 60f, 0f);
            organ.SetLocalPositionAndRotation(localPosition, localRotation);

            yield return new WaitForSeconds(0.7f);
            Assert.IsTrue(Near(_bodyA, localPosition, localRotation),
                $"The holder's organ snapped back toward the stale server pose. Wanted {localPosition}, is {organ.localPosition}; paused: {organ.GetComponent<HoldableNetworkTransform>().IsPaused}.");

            // Release over the open body, a little above the organ's home. The server puts an organ released there
            // back home (Returned, or Accepted/Rejected near a socket: all three end at home), so the final pose is
            // deterministic. Releasing outside the body would leave the organ falling, so the pose would keep changing.
            Pose home = _serverBody.Organs[OrganIndex].GetComponent<ISnappable>().HomeRootPose;
            Vector3 serverPosition = OrganDropSpots.DropRootPosition(_serverBody, OrganIndex, new Vector3(0f, 0.1f, 0f));
            _authorityA.SendPose(OrganIndex, serverPosition, home.rotation);
            yield return WaitFor(() => Near(_serverBody, serverPosition, home.rotation), "the server organ to reach the drop pose");
            _authorityA.Release(OrganIndex);

            yield return WaitFor(() => Near(_serverBody, home.position, home.rotation, 0.0005f), "the server organ to ease home");
            yield return WaitFor(() => Near(_bodyA, home.position, home.rotation), "the holder to converge to the server pose after release");
        }

        [UnityTest]
        public IEnumerator DisablingTheHeldOrganReleasesIt()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);

            _bodyA.Organs[OrganIndex].GetComponent<NetworkedOrganGrab>().enabled = false;

            yield return WaitFor(() => _server.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the server to free the disabled organ");
            yield return Grab(_authorityB, expectGranted: true);
        }

        [UnityTest]
        public IEnumerator InvalidPosesAreIgnoredAndWarnedAboutOncePerSender()
        {
            yield return StartTwoClients();
            yield return Grab(_authorityA, expectGranted: true);
            Transform organ = _serverBody.Organs[OrganIndex].transform;
            Vector3 before = organ.localPosition;
            Quaternion beforeRotation = organ.localRotation;

            LogAssert.Expect(LogType.Warning, new Regex("ignored an invalid pose"));
            _authorityA.SendPose(OrganIndex, new Vector3(float.NaN, 0f, 0f), Quaternion.identity);
            _authorityA.SendPose(OrganIndex, new Vector3(50f, 0f, 0f), Quaternion.identity);
            _authorityA.SendPose(OrganIndex, before, new Quaternion(0f, 0f, 0f, 0f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(Near(_serverBody, before, beforeRotation), "An invalid pose must not move the organ.");

            Vector3 good = before + new Vector3(0.1f, 0f, 0f);
            _authorityA.SendPose(OrganIndex, good, beforeRotation);
            yield return WaitFor(() => Near(_serverBody, good, beforeRotation), "a valid pose to still be applied");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RespawningTheBodyDoesNotDuplicateTheRegistry()
        {
            yield return StartHostAndSpawnBody();
            var server = _serverBody.GetComponent<OrganGrabAuthority>();
            int organCount = _serverBody.Organs.Count;
            Assert.AreEqual(organCount, server.HolderCount);

            NetworkObject body = _serverBody.GetComponent<NetworkObject>();
            body.Despawn(false);
            yield return null;
            body.Spawn();
            Assert.AreEqual(organCount, server.HolderCount, "Respawn must not add the holder entries a second time.");
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<OrganGrabAuthority>();
            Assert.IsNotNull(_server, "The body prefab has no OrganGrabAuthority. Run Tools > DIYsect > Add Body Networking.");
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            yield return WaitForBody(_managerA, body => _bodyA = body);
            yield return WaitForBody(_managerB, body => _bodyB = body);
            _authorityA = _bodyA.GetComponent<OrganGrabAuthority>();
            _authorityB = _bodyB.GetComponent<OrganGrabAuthority>();
            yield return WaitFor(() => _authorityA.IsSpawned && _authorityB.IsSpawned, "the grab authority to spawn on both clients");
            yield return WaitFor(() => _authorityA.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder && _authorityB.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder,
                "both clients to receive the holder list");
        }

        // Requests the organ and waits for the server's answer.
        private IEnumerator Grab(OrganGrabAuthority authority, bool expectGranted)
        {
            bool answered = false;
            bool granted = false;
            void OnResolved(int index, bool result)
            {
                if (index != OrganIndex)
                    return;
                answered = true;
                granted = result;
            }
            authority.GrabResolved += OnResolved;
            authority.RequestGrab(OrganIndex);
            yield return WaitFor(() => answered, "the server to answer the grab request");
            authority.GrabResolved -= OnResolved;
            Assert.AreEqual(expectGranted, granted, expectGranted ? "The grab should have been granted." : "The grab should have been rejected.");
        }

        private static bool Near(SharedBody body, Vector3 position, Quaternion rotation, float tolerance = PositionTolerance)
        {
            Transform organ = body.Organs[OrganIndex].transform;
            return Vector3.Distance(organ.localPosition, position) < tolerance && Quaternion.Angle(organ.localRotation, rotation) < 0.5f;
        }
    }
}
