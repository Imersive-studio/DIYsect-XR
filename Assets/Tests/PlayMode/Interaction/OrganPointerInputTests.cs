// Under test: a controller ray that rests on an organ asks for a pointer, and leaving it stops the pointer.
// No hardware: a ray interactor is created in code and the interaction manager is asked to start and end the hover.
using System.Collections;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.Networking;
using DIYsect.Networking.Tests;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace DIYsect.Interaction.Tests
{
    public class OrganPointerInputTests : NetworkedBodyTestBase
    {
        private GameObject _hand;

        private sealed class Recorder : IOrganPointer
        {
            public readonly System.Collections.Generic.List<int> Points = new System.Collections.Generic.List<int>();
            public int Stops;
            public void Point(int organIndex) => Points.Add(organIndex);
            public void StopPointing() => Stops++;
        }

        [UnityTearDown]
        public IEnumerator DestroyHand()
        {
            if (_hand != null)
                Object.Destroy(_hand);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RayHoverAsksForAPointerAndLeavingStopsIt()
        {
            yield return StartHostAndSpawnBody();
            var input = _serverBody.GetComponent<OrganPointerInput>();
            Assert.IsNotNull(input, "The body has no OrganPointerInput. Run Tools > DIYsect > Add Organ Pointer.");
            var recorder = new Recorder();
            input.SetPointer(recorder);

            // No dwell here: the interaction manager ends a hover the ray does not really have on its next update,
            // which would cancel a pending dwell. The dwell itself is a plain timer.
            typeof(OrganPointerInput).GetField("_dwellSeconds", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(input, 0f);
            Transform organ = _serverBody.Organs[4].transform;
            var interactable = organ.GetComponent<XRGrabInteractable>();
            _hand = new GameObject("Test ray");
            var interactor = _hand.AddComponent<XRRayInteractor>();
            yield return null;

            interactable.interactionManager.HoverEnterUnconditionally((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
            Assert.AreEqual(4, input.PointingIndex);
            CollectionAssert.AreEqual(new[] { 4 }, recorder.Points);

            interactable.interactionManager.HoverExit((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
            Assert.AreEqual(-1, input.PointingIndex);
            Assert.AreEqual(1, recorder.Stops);
        }

        [UnityTest]
        public IEnumerator ADirectHandTouchIsNotAPointer()
        {
            yield return StartHostAndSpawnBody();
            var recorder = new Recorder();
            _serverBody.GetComponent<OrganPointerInput>().SetPointer(recorder);
            var interactable = _serverBody.Organs[4].GetComponent<XRGrabInteractable>();
            _hand = new GameObject("Test hand");
            _hand.AddComponent<SphereCollider>().isTrigger = true;
            _hand.AddComponent<Rigidbody>().isKinematic = true;
            var interactor = _hand.AddComponent<XRDirectInteractor>();
            yield return null;

            interactable.interactionManager.HoverEnterUnconditionally((IXRHoverInteractor)interactor, (IXRHoverInteractable)interactable);
            yield return new WaitForSecondsRealtime(0.6f);
            CollectionAssert.IsEmpty(recorder.Points);
        }
    }
}
