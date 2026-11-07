// Under test: the name label that appears while a controller points at an organ. No controller hardware is needed:
// a direct interactor is created in code and the interaction manager is asked to start and end the hover.
using System.Collections;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public class OrganHoverLabelTests : NetworkedBodyTestBase
    {
        private GameObject _hand;

        [UnityTearDown]
        public IEnumerator DestroyHand()
        {
            if (_hand != null)
                Object.Destroy(_hand);
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoveringShowsTheOrganNameAndLeavingHidesIt()
        {
            yield return StartHostAndSpawnBody();
            Transform organ = _serverBody.Organs[0].transform;
            var hover = organ.GetComponent<OrganHoverLabel>();
            Assert.IsNotNull(hover, "The organ has no OrganHoverLabel. Run Tools > DIYsect > Add Hover Labels And Haptics.");
            Assert.IsNotNull(organ.GetComponent<OrganHaptics>(), "The organ has no OrganHaptics. Run Tools > DIYsect > Add Hover Labels And Haptics.");
            Assert.IsNull(hover.Label, "Nothing is hovered yet, so no label exists.");

            var interactable = organ.GetComponent<XRGrabInteractable>();
            _hand = new GameObject("Test hand");
            _hand.AddComponent<SphereCollider>().isTrigger = true;
            _hand.AddComponent<Rigidbody>().isKinematic = true;
            var interactor = _hand.AddComponent<XRDirectInteractor>();
            yield return null;

            interactable.interactionManager.HoverEnterUnconditionally((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
            // Checked in the same frame: the manager's own update would end a hover that the interactor does not really have.
            Assert.IsTrue(interactable.isHovered);
            Assert.IsNotNull(hover.Label);
            Assert.IsTrue(hover.Label.IsShown);
            Assert.AreEqual(OrganNames.Display(organ.name), hover.Label.Text);

            interactable.interactionManager.HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
            Assert.IsFalse(interactable.isHovered);
            Assert.IsFalse(hover.Label.IsShown);
        }
    }
}
