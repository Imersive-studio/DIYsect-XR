// Under test: server-authoritative organ physics. Seated organs are kinematic; an organ let go away from the body
// falls and rests on the server while clients only follow its pose; an organ let go over the body eases home.
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class OrganPhysicsTests : NetworkedBodyTestBase
    {
        private const float ExactTolerance = 0.0005f;
        private const float PeerTolerance = 0.01f;

        private int _liver;
        private OrganGrabAuthority _server;
        private OrganPhysics _physics;
        private OrganSocketKeeper _keeper;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;
        private OrganGrabAuthority _authorityA;
        private GameObject _ground;
        private Vector3 _gravity;
        private readonly Dictionary<string, SnapOutcome> _outcomes = new Dictionary<string, SnapOutcome>();

        [SetUp]
        public void PinGravity()
        {
            _gravity = Physics.gravity;
            Physics.gravity = new Vector3(0f, -9.81f, 0f);
        }

        [UnityTearDown]
        public IEnumerator RestoreGravity()
        {
            Physics.gravity = _gravity;
            if (_ground != null)
                Object.Destroy(_ground);
            yield break;
        }

        [UnityTest]
        public IEnumerator OrgansStartSeatedOnTheServerAndOnClients()
        {
            yield return StartTwoClients();
            int seated = LayerMask.NameToLayer(OrganPhysics.SeatedLayerName);
            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                SharedBody peer = body;
                yield return WaitFor(() => peer.GetComponent<OrganPhysics>().IsSpawned, $"organ physics to spawn on {body.name}");
                for (int i = 0; i < body.Organs.Count; i++)
                {
                    Rigidbody rigidbody = body.Organs[i].GetComponent<Rigidbody>();
                    Assert.IsTrue(rigidbody.isKinematic, $"Organ {i} must be kinematic on {body.name}.");
                    Assert.IsFalse(rigidbody.useGravity, $"Organ {i} must not use gravity on {body.name}.");
                    Assert.AreEqual(seated, body.Organs[i].gameObject.layer, $"Organ {i} must be on the SeatedOrgan layer on {body.name}.");
                }
            }
            Assert.IsFalse(_physics.IsLoose(_liver));
        }

        [UnityTest]
        public IEnumerator AGrabbedOrganIsSeatedEvenIfItWasLoose()
        {
            yield return StartTwoClients();
            _physics.Loosen(_liver);
            Assert.IsTrue(_physics.IsLoose(_liver), "The organ should be loose before the grab.");
            Assert.AreEqual(LayerMask.NameToLayer(OrganPhysics.LooseLayerName), _serverBody.Organs[_liver].gameObject.layer);

            yield return Grab(_liver);

            Assert.IsFalse(_physics.IsLoose(_liver), "A granted grab seats the organ.");
            Rigidbody rigidbody = _serverBody.Organs[_liver].GetComponent<Rigidbody>();
            Assert.IsTrue(rigidbody.isKinematic);
            Assert.IsFalse(rigidbody.useGravity);
            Assert.AreEqual(LayerMask.NameToLayer(OrganPhysics.SeatedLayerName), _serverBody.Organs[_liver].gameObject.layer);
        }

        [UnityTest]
        public IEnumerator AnOrganDroppedAwayFromTheBodyFallsAndRestsOnTheGroundWhileClientsFollow()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Transform serverOrgan = _serverBody.Organs[_liver].transform;
            Pose root = Home(_liver);

            Vector3 dropped = OrganDropSpots.DropRootPosition(_serverBody, _liver, new Vector3(1f, 0f, 0f));
            Vector3 droppedWorld = serverOrgan.parent.TransformPoint(dropped);
            float groundTop = droppedWorld.y - 0.4f;
            _ground = OrganDropSpots.CreateGround(groundTop, droppedWorld);

            _authorityA.SendPose(_liver, dropped, root.rotation);
            yield return WaitFor(() => Vector3.Distance(serverOrgan.localPosition, dropped) < ExactTolerance, "the server organ to reach the drop point");
            Assert.IsFalse(_keeper.IsOverBody(_liver), "The drop point must be outside the body zone for this test.");

            _authorityA.Release(_liver);
            yield return WaitFor(() => _physics.IsLoose(_liver), "the organ to become loose on the server");
            Assert.AreEqual(LayerMask.NameToLayer(OrganPhysics.LooseLayerName), serverOrgan.gameObject.layer);

            Rigidbody serverRigidbody = serverOrgan.GetComponent<Rigidbody>();
            yield return WaitFor(() => serverOrgan.position.y < droppedWorld.y - 0.05f && serverRigidbody.linearVelocity.magnitude < 0.02f && serverRigidbody.angularVelocity.magnitude < 0.05f,
                "the organ to come to rest", () => $"y {serverOrgan.position.y}, speed {serverRigidbody.linearVelocity.magnitude}");
            Assert.GreaterOrEqual(serverOrgan.position.y, groundTop - 0.01f, "The organ rests on the ground, not below it.");
            Assert.Less(serverOrgan.position.y, droppedWorld.y, "The organ fell.");
            Assert.AreEqual(OrganGrabAuthority.NoHolder, _server.GetHolder(_liver), "Nobody holds it any more.");

            foreach (SharedBody body in new[] { _bodyA, _bodyB })
            {
                SharedBody peer = body;
                yield return WaitFor(() => Vector3.Distance(peer.Organs[_liver].transform.localPosition, serverOrgan.localPosition) < PeerTolerance,
                    $"the pose to converge on {body.name}");
                Assert.IsTrue(body.Organs[_liver].GetComponent<Rigidbody>().isKinematic, $"The organ must stay kinematic on {body.name}.");
            }
        }

        [UnityTest]
        public IEnumerator AnOrganDroppedOverTheBodyButNotAtASocketReturnsHomeAndStaysSeated()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Pose root = Home(_liver);
            Assert.IsTrue(OrganDropSpots.TryFindClearOffset(_serverBody, _liver, new[] { Vector3.up * 0.12f, Vector3.up * 0.16f, Vector3.up * 0.2f }, 0.05f, out Vector3 offset),
                "No drop point above the liver is clear of the other sockets.");
            Vector3 dropped = root.position + offset;

            _authorityA.SendPose(_liver, dropped, root.rotation);
            Transform serverOrgan = _serverBody.Organs[_liver].transform;
            yield return WaitFor(() => Vector3.Distance(serverOrgan.localPosition, dropped) < ExactTolerance, "the server organ to reach the drop point");
            Assert.IsTrue(_keeper.IsOverBody(_liver), "The drop point must be inside the body zone for this test.");

            _outcomes.Clear();
            _authorityA.Release(_liver);
            foreach (string peer in new[] { "server", "A", "B" })
                yield return WaitFor(() => _outcomes.ContainsKey(peer), $"{peer} to hear the outcome");
            Assert.AreEqual(SnapOutcome.Returned, _outcomes["server"]);
            Assert.AreEqual(SnapOutcome.Returned, _outcomes["A"]);
            Assert.AreEqual(SnapOutcome.Returned, _outcomes["B"]);

            yield return WaitFor(() => AtHome(_serverBody, _liver), "the liver to ease home on the server");
            yield return WaitFor(() => AtHome(_bodyA, _liver), "the liver to be home on client A");
            yield return WaitFor(() => AtHome(_bodyB, _liver), "the liver to be home on client B");
            Assert.IsFalse(_physics.IsLoose(_liver), "An organ returned home stays kinematic.");
            Assert.IsTrue(serverOrgan.GetComponent<Rigidbody>().isKinematic);
            Assert.AreEqual(LayerMask.NameToLayer(OrganPhysics.SeatedLayerName), serverOrgan.gameObject.layer);
        }

        [UnityTest]
        public IEnumerator AnOrganDroppedFiveCentimetresFromItsOwnSocketIsCapturedAndEasesHome()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Pose root = Home(_liver);
            // 5 cm is beyond the 3 cm position tolerance but inside the 6 cm capture radius. Pick a direction in which
            // no other socket is closer than the liver's own, so the own-socket-nearest condition holds.
            Assert.IsTrue(OrganDropSpots.TryFindClearOffset(_serverBody, _liver, OrganDropSpots.Directions(0.05f), 0.0505f, out Vector3 offset),
                "No 5 cm direction from the liver's socket keeps its own socket the nearest.");
            Vector3 dropped = root.position + offset;

            _authorityA.SendPose(_liver, dropped, root.rotation);
            Transform serverOrgan = _serverBody.Organs[_liver].transform;
            yield return WaitFor(() => Vector3.Distance(serverOrgan.localPosition, dropped) < ExactTolerance, "the server organ to reach the drop point");
            Assert.IsFalse(AtHome(_serverBody, _liver), "The liver should be away from home before the release.");

            _outcomes.Clear();
            _authorityA.Release(_liver);
            foreach (string peer in new[] { "server", "A", "B" })
                yield return WaitFor(() => _outcomes.ContainsKey(peer), $"{peer} to hear the outcome");
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["server"]);
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["A"]);
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["B"]);

            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                SharedBody peer = body;
                yield return WaitFor(() => AtHome(peer, _liver), $"the liver to ease home on {body.name}");
            }
            Assert.IsFalse(_physics.IsLoose(_liver));
        }

        [UnityTest]
        public IEnumerator LooseAndSeatedOrgansDoNotCollide()
        {
            yield return StartTwoClients();
            int seated = LayerMask.NameToLayer(OrganPhysics.SeatedLayerName);
            int loose = LayerMask.NameToLayer(OrganPhysics.LooseLayerName);
            Assert.IsTrue(Physics.GetIgnoreLayerCollision(seated, loose), "Loose organs must ignore seated ones.");
        }

        [UnityTest]
        public IEnumerator OrgansAndTheScalpelKeepTheirParentWhileGrabbed()
        {
            yield return StartTwoClients();
            for (int i = 0; i < _serverBody.Organs.Count; i++)
                Assert.IsFalse(UnparentOnGrab(_serverBody.Organs[i].gameObject), $"Organ {i} ({_serverBody.Organs[i].name}) must keep its parent while grabbed.");
            Assert.IsFalse(UnparentOnGrab(LoadScalpelPrefabAsset()), "The scalpel must keep its parent while grabbed.");
        }

        // XRGrabInteractable.unparentTransformOnGrab, read by reflection because the test assembly does not reference XRI.
        private static bool UnparentOnGrab(GameObject target)
        {
            Component grab = target.GetComponent("XRGrabInteractable");
            Assert.IsNotNull(grab, $"{target.name} has no XRGrabInteractable.");
            PropertyInfo property = grab.GetType().GetProperty("unparentTransformOnGrab", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(property, "XRGrabInteractable.unparentTransformOnGrab was not found.");
            return (bool)property.GetValue(grab);
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<OrganGrabAuthority>();
            _physics = _serverBody.GetComponent<OrganPhysics>();
            _keeper = _serverBody.GetComponent<OrganSocketKeeper>();
            _liver = IndexOf("liver");
            _managerA = StartClient("ClientA");
            _managerB = StartClient("ClientB");
            yield return WaitForBody(_managerA, body => _bodyA = body);
            yield return WaitForBody(_managerB, body => _bodyB = body);
            _authorityA = _bodyA.GetComponent<OrganGrabAuthority>();
            OrganGrabAuthority authorityB = _bodyB.GetComponent<OrganGrabAuthority>();
            yield return WaitFor(() => _authorityA.IsSpawned && authorityB.IsSpawned, "the grab authority to spawn on both clients");
            yield return WaitFor(() => _authorityA.HolderCount > 0 && authorityB.HolderCount > 0, "both clients to receive the holder list");
            Listen("server", _serverBody);
            Listen("A", _bodyA);
            Listen("B", _bodyB);
        }

        private void Listen(string peer, SharedBody body)
        {
            body.GetComponent<OrganSocketKeeper>().SnapResolved += (index, outcome) =>
            {
                if (index == _liver)
                    _outcomes[peer] = outcome;
            };
        }

        private IEnumerator Grab(int organ)
        {
            bool answered = false;
            bool granted = false;
            void OnResolved(int index, bool result)
            {
                if (index != organ)
                    return;
                answered = true;
                granted = result;
            }
            _authorityA.GrabResolved += OnResolved;
            _authorityA.RequestGrab(organ);
            yield return WaitFor(() => answered, "the server to answer the grab request");
            _authorityA.GrabResolved -= OnResolved;
            Assert.IsTrue(granted, "The grab should have been granted.");
        }

        private int IndexOf(string organName)
        {
            for (int i = 0; i < _serverBody.Organs.Count; i++)
                if (_serverBody.Organs[i].name == organName)
                    return i;
            Assert.Fail($"The body has no organ named {organName}.");
            return -1;
        }

        private Pose Home(int organ)
        {
            return _serverBody.Organs[organ].GetComponent<ISnappable>().HomeRootPose;
        }

        private bool AtHome(SharedBody body, int organ)
        {
            Pose home = body.Organs[organ].GetComponent<ISnappable>().HomeRootPose;
            Transform transform = body.Organs[organ].transform;
            float tolerance = body == _serverBody ? ExactTolerance : 0.005f;
            return Vector3.Distance(transform.localPosition, home.position) < tolerance && Quaternion.Angle(transform.localRotation, home.rotation) < 0.1f;
        }
    }
}
