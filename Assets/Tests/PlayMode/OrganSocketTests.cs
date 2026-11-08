// Under test: the label and ghost while an organ is held, and the server's judgement of where it was dropped.
using System.Collections;
using System.Collections.Generic;
using DIYsect.Interaction;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace DIYsect.Networking.Tests
{
    public class OrganSocketTests : NetworkedBodyTestBase
    {
        // Peers receive poses over the network, which is accurate to a few millimetres (the other organ tests use the same bound).
        private const float PeerTolerance = 0.005f;
        private const float ExactTolerance = 0.0005f;

        private int _liver;
        private int _stomach;
        private OrganGrabAuthority _server;
        private NetworkManager _managerA;
        private NetworkManager _managerB;
        private SharedBody _bodyA;
        private SharedBody _bodyB;
        private OrganGrabAuthority _authorityA;
        private readonly Dictionary<string, SnapOutcome> _outcomes = new Dictionary<string, SnapOutcome>();
        private GameObject _ground;
        private Vector3 _gravity;

        // The drop test depends on gravity, so pin it and put it back afterwards.
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
        public IEnumerator GrabbingShowsTheOrganNameOnEveryClientAndReleaseHidesIt()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);

            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                OrganGrabPresenter presenter = body.GetComponent<OrganGrabPresenter>();
                yield return WaitFor(() => presenter.GetLabel(_liver) != null && presenter.GetLabel(_liver).IsShown, $"the label on {body.name}");
                Assert.AreEqual("Liver", presenter.GetLabel(_liver).Text);
                Assert.IsNull(presenter.GetLabel(_stomach), "Only the held organ has a label.");
            }

            _authorityA.Release(_liver);
            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                OrganGrabPresenter presenter = body.GetComponent<OrganGrabPresenter>();
                yield return WaitFor(() => !presenter.GetLabel(_liver).IsShown, $"the label to hide on {body.name}");
            }
        }

        [UnityTest]
        public IEnumerator SidedOrgansShowLeftAndRight()
        {
            yield return StartTwoClients();
            int kidney = IndexOf("kidney_l");
            yield return Grab(kidney);
            OrganGrabPresenter presenter = _bodyB.GetComponent<OrganGrabPresenter>();
            yield return WaitFor(() => presenter.GetLabel(kidney) != null && presenter.GetLabel(kidney).IsShown, "the label on client B");
            Assert.AreEqual("Left kidney", presenter.GetLabel(kidney).Text);
        }

        [UnityTest]
        public IEnumerator ReleaseNearItsOwnSocketSnapsToTheExactHomePoseAndPeersSeeAccepted()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Pose home = Home(_liver);
            // A little off and a little turned: inside the tolerance.
            _authorityA.SendPose(_liver, home.position + new Vector3(0.02f, 0.01f, 0f), home.rotation * Quaternion.Euler(0f, 10f, 0f));
            yield return new WaitForSeconds(0.3f);
            _outcomes.Clear();
            _authorityA.Release(_liver);

            foreach (string peer in new[] { "server", "A", "B" })
                yield return WaitFor(() => _outcomes.ContainsKey(peer), $"{peer} to hear the outcome");
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["server"]);
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["A"]);
            Assert.AreEqual(SnapOutcome.Accepted, _outcomes["B"]);

            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
                yield return WaitFor(() => AtHome(body, _liver), $"the liver to sit exactly at home on {body.name}");

            // Peers flash the ghost green.
            OrganGhost ghost = _bodyB.GetComponent<OrganGrabPresenter>().GetGhost(_liver);
            yield return WaitFor(() => ghost.FlashColor.HasValue || !ghost.IsShown, "the flash on client B");
        }

        [UnityTest]
        public IEnumerator ReleaseNearAnotherOrgansSocketIsRejectedAndTheOrganGoesHome()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            // Put the liver's centre on the stomach's socket.
            Pose liverHome = _serverBody.Organs[_liver].GetComponent<ISnappable>().HomeCentrePose;
            Pose stomach = _serverBody.Organs[_stomach].GetComponent<ISnappable>().HomeCentrePose;
            Pose root = Home(_liver);
            _authorityA.SendPose(_liver, root.position + (stomach.position - liverHome.position), root.rotation);
            yield return new WaitForSeconds(0.3f);
            Assert.IsFalse(AtHome(_serverBody, _liver), "The liver should be away from home before the release.");
            _outcomes.Clear();
            _authorityA.Release(_liver);

            foreach (string peer in new[] { "server", "A", "B" })
                yield return WaitFor(() => _outcomes.ContainsKey(peer), $"{peer} to hear the outcome");
            Assert.AreEqual(SnapOutcome.Rejected, _outcomes["server"]);
            Assert.AreEqual(SnapOutcome.Rejected, _outcomes["A"]);
            Assert.AreEqual(SnapOutcome.Rejected, _outcomes["B"]);
            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
                yield return WaitFor(() => AtHome(body, _liver), $"the liver to return home on {body.name}");
            Assert.IsTrue(AtHome(_serverBody, _stomach), "The stomach must not move.");
        }

        [UnityTest]
        public IEnumerator ReleaseAwayFromEverySocketAndTheBodyLetsTheOrganFallWithoutAVerdict()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Pose root = Home(_liver);

            // Far to the side and lower than the body, so it is outside the body zone; a floor catches the organ.
            Vector3 dropped = OrganDropSpots.DropRootPosition(_serverBody, _liver, new Vector3(1f, -0.1f, 0f));
            Transform serverOrgan = _serverBody.Organs[_liver].transform;
            float droppedWorldY = serverOrgan.parent.TransformPoint(dropped).y;
            _ground = OrganDropSpots.CreateGround(droppedWorldY - 0.4f, serverOrgan.parent.TransformPoint(dropped));

            _authorityA.SendPose(_liver, dropped, root.rotation);
            yield return WaitFor(() => Vector3.Distance(serverOrgan.localPosition, dropped) < ExactTolerance, "the server organ to reach the drop point");
            OrganSocketKeeper keeper = _serverBody.GetComponent<OrganSocketKeeper>();
            Assert.IsFalse(keeper.IsOverBody(_liver), "The drop point must be outside the body zone for this test.");

            OrganPhysics physics = _serverBody.GetComponent<OrganPhysics>();
            Rigidbody serverRigidbody = serverOrgan.GetComponent<Rigidbody>();
            _outcomes.Clear();
            _authorityA.Release(_liver);
            yield return WaitFor(() => _server.GetHolder(_liver) == OrganGrabAuthority.NoHolder, "the release");

            yield return WaitFor(() => physics.IsLoose(_liver), "the organ to become loose on the server");
            Assert.AreEqual(LayerMask.NameToLayer(OrganPhysics.LooseLayerName), serverOrgan.gameObject.layer, "A loose organ is on the LooseOrgan layer.");
            Assert.IsTrue(serverRigidbody.useGravity);
            float startY = serverOrgan.position.y;
            yield return WaitFor(() => serverOrgan.position.y < startY - 0.02f, "the organ to fall under gravity");

            // Clients never simulate it: their Rigidbody stays kinematic and they follow the server pose.
            foreach (SharedBody body in new[] { _bodyA, _bodyB })
            {
                Assert.IsTrue(body.Organs[_liver].GetComponent<Rigidbody>().isKinematic, $"The organ must stay kinematic on {body.name}.");
                SharedBody peer = body;
                yield return WaitFor(() => peer.Organs[_liver].transform.position.y < startY - 0.02f, $"the fall to show on {body.name}");
            }
            Assert.IsEmpty(_outcomes, "Nothing was judged, so nobody gets an outcome.");
        }

        [UnityTest]
        public IEnumerator GhostShowsAtTheSocketOnEveryClientWhileHeldAndHidesAfterRelease()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);
            Pose root = Home(_liver);
            _authorityA.SendPose(_liver, root.position + new Vector3(0.3f, 0f, 0f), root.rotation);

            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                OrganGrabPresenter presenter = body.GetComponent<OrganGrabPresenter>();
                yield return WaitFor(() => presenter.GetGhost(_liver) != null && presenter.GetGhost(_liver).IsShown, $"the ghost on {body.name}");
                Transform ghost = presenter.GetGhost(_liver).transform;
                Assert.Less(Vector3.Distance(ghost.localPosition, root.position), ExactTolerance, "The ghost sits at the home pose.");
                Assert.AreSame(body.Organs[_liver].transform.parent, ghost.parent);
                Assert.IsNull(presenter.GetGhost(_stomach), "One ghost per held organ, created lazily.");
            }

            _authorityA.Release(_liver);
            foreach (SharedBody body in new[] { _serverBody, _bodyA, _bodyB })
            {
                OrganGhost ghost = body.GetComponent<OrganGrabPresenter>().GetGhost(_liver);
                yield return WaitFor(() => !ghost.IsShown && !ghost.FlashColor.HasValue, $"the ghost to hide on {body.name}");
            }
        }

        [UnityTest]
        public IEnumerator LateJoinerSeesTheLabelAndGhostOfAnOrganThatIsAlreadyHeld()
        {
            yield return StartTwoClients();
            yield return Grab(_liver);

            NetworkManager late = StartClient("LateClient");
            SharedBody lateBody = null;
            yield return WaitForBody(late, body => lateBody = body);
            OrganGrabPresenter presenter = lateBody.GetComponent<OrganGrabPresenter>();
            yield return WaitFor(() => presenter.GetLabel(_liver) != null && presenter.GetLabel(_liver).IsShown, "the late client to show the label");
            Assert.AreEqual("Liver", presenter.GetLabel(_liver).Text);
            Assert.IsTrue(presenter.GetGhost(_liver).IsShown, "The ghost must show too.");
            Assert.IsNull(presenter.GetLabel(_stomach));

            _authorityA.Release(_liver);
            yield return WaitFor(() => !presenter.GetLabel(_liver).IsShown, "the late client to hide the label");
        }

        private IEnumerator StartTwoClients()
        {
            yield return StartHostAndSpawnBody();
            _server = _serverBody.GetComponent<OrganGrabAuthority>();
            _liver = IndexOf("liver");
            _stomach = IndexOf("stomach");
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
            float tolerance = body == _serverBody ? ExactTolerance : PeerTolerance;
            return Vector3.Distance(transform.localPosition, home.position) < tolerance && Quaternion.Angle(transform.localRotation, home.rotation) < 0.1f;
        }
    }
}
