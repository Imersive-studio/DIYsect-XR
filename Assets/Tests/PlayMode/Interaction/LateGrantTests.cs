// State bug: a grant that arrives after the user already let go.
// Ordering used to make it deterministic: the client selects and deselects in the same frame, so its request and its
// release leave together. Netcode delivers messages from one sender in order, so the server grants first and then
// releases, and the client hears "granted" only after it has already let go. No fixed frame counts are needed.
using System.Collections;
using DIYsect.Networking;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public class LateGrantTests : ScalpelSessionTestBase
    {
        private const int OrganIndex = 3;

        [UnityTest]
        public IEnumerator OrganBridgeEndsIdleAndServerHasNoHolderWhenTheGrantArrivesAfterLettingGo()
        {
            // Bug 1 (fixed by GrabRequest): the organ bridge stayed in "may hold" when "granted" arrived after the user let go.
            yield return StartHostAndSpawnBody();
            NetworkManager manager = StartClient("Client");
            SharedBody clientBody = null;
            yield return WaitForBody(manager, body => clientBody = body);
            OrganGrabAuthority serverAuthority = _serverBody.GetComponent<OrganGrabAuthority>();
            OrganGrabAuthority clientAuthority = clientBody.GetComponent<OrganGrabAuthority>();
            yield return WaitFor(() => clientAuthority.IsSpawned && clientAuthority.HolderCount == _serverBody.Organs.Count, "the client to receive the holder list");

            Transform organ = clientBody.Organs[OrganIndex].transform;
            var bridge = organ.GetComponent<NetworkedOrganGrab>();
            var interactable = organ.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(bridge);
            Assert.IsTrue(bridge.enabled, "The organ bridge is disabled, probably a validation error.");
            XRDirectInteractor hand = CreateHand("Late grant hand");
            yield return null;

            bool? answer = null;
            bool serverReleased = false;
            void OnResolved(int index, bool granted) { if (index == OrganIndex) answer = granted; }
            void OnServerReleased(int index) { if (index == OrganIndex) serverReleased = true; }
            clientAuthority.GrabResolved += OnResolved;
            serverAuthority.ServerReleased += OnServerReleased;

            hand.StartManualInteraction((IXRSelectInteractable)interactable);
            Assert.IsTrue(interactable.isSelected, "The organ should be selected.");
            hand.EndManualInteraction();
            Assert.IsFalse(interactable.isSelected, "The organ should be let go.");

            yield return WaitFor(() => answer.HasValue, "the server's answer to arrive after the release");
            yield return WaitFor(() => serverReleased, "the server to process the release");
            clientAuthority.GrabResolved -= OnResolved;
            serverAuthority.ServerReleased -= OnServerReleased;
            Assert.IsTrue(answer.Value, "Setup: the request is granted because it reached the server before the release.");

            Assert.AreEqual(OrganGrabAuthority.NoHolder, serverAuthority.GetHolder(OrganIndex), "The server must end with no holder.");
            yield return WaitFor(() => clientAuthority.GetHolder(OrganIndex) == OrganGrabAuthority.NoHolder, "the client to see no holder");
            Assert.IsFalse(bridge.IsHeldByLocalClient);
            Assert.AreEqual(GrabPhase.Idle, bridge.Phase, "A grant that arrives after the user let go must leave the bridge idle.");
        }

        [UnityTest]
        public IEnumerator ScalpelBridgeEndsIdleAndServerHasNoSteererWhenTheGrantArrivesAfterLettingGo()
        {
            // Bug 2 (fixed by GrabRequest): the scalpel bridge stayed in "may steer" when "granted" arrived after the user let go.
            yield return StartScalpelSession();
            var bridge = _clientScalpel.GetComponent<NetworkedScalpelGrab>();
            var interactable = _clientScalpel.GetComponent<XRGrabInteractable>();
            Assert.IsNotNull(bridge);
            Assert.IsTrue(bridge.enabled);
            XRDirectInteractor hand = CreateHand("Late grant hand");
            yield return null;

            bool? answer = null;
            bool serverSawSteerer = false;
            void OnResolved(bool granted) => answer = granted;
            void OnServerSteerer(ulong steerer) { if (steerer == _clientManager.LocalClientId) serverSawSteerer = true; }
            _clientSteering.SteerResolved += OnResolved;
            _serverSteering.SteererChanged += OnServerSteerer;

            hand.StartManualInteraction((IXRSelectInteractable)interactable);
            Assert.IsTrue(interactable.isSelected, "The scalpel should be selected.");
            hand.EndManualInteraction();
            Assert.IsFalse(interactable.isSelected);

            yield return WaitFor(() => answer.HasValue, "the server's answer to arrive after the release");
            yield return WaitFor(() => serverSawSteerer && _serverSteering.Steerer == ScalpelSteering.NoSteerer, "the server to grant and then release");
            _clientSteering.SteerResolved -= OnResolved;
            _serverSteering.SteererChanged -= OnServerSteerer;
            Assert.IsTrue(answer.Value, "Setup: the request is granted because it reached the server before the release.");

            yield return WaitFor(() => _clientSteering.Steerer == ScalpelSteering.NoSteerer, "the client to see no steerer");
            Assert.IsFalse(_clientSteering.IsSteeredByLocalClient);
            Assert.AreEqual(GrabPhase.Idle, bridge.Phase, "A grant that arrives after the user let go must leave the bridge idle.");
        }
    }
}
