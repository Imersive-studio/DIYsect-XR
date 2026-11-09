using DIYsect.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

namespace DIYsect.InstrumentGrab.Tests
{
    /// <summary>
    /// Guards the wider grab zone on the instruments. EnsureGrabZone must add one trigger GrabZone child and list it
    /// once in the interactable's colliders, even when it runs twice.
    /// </summary>
    public class InstrumentGrabTests
    {
        private GameObject _tool;

        [SetUp]
        public void BuildTool()
        {
            _tool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _tool.AddComponent<Rigidbody>();
            _tool.AddComponent<XRGrabInteractable>();
        }

        [TearDown]
        public void DestroyTool()
        {
            Object.DestroyImmediate(_tool);
        }

        [Test]
        public void RunningTwiceAddsOneTriggerZoneListedOnce()
        {
            var grab = _tool.GetComponent<XRGrabInteractable>();

            WidenInstrumentGrab.EnsureGrabZone(_tool, grab, WidenInstrumentGrab.GrabZoneRadius);
            WidenInstrumentGrab.EnsureGrabZone(_tool, grab, WidenInstrumentGrab.GrabZoneRadius);

            int zones = 0;
            foreach (Transform child in _tool.transform)
                if (child.name == WidenInstrumentGrab.GrabZoneName) zones++;
            Assert.AreEqual(1, zones, "There must be exactly one GrabZone child.");

            var sphere = _tool.transform.Find(WidenInstrumentGrab.GrabZoneName).GetComponent<SphereCollider>();
            Assert.IsNotNull(sphere, "The GrabZone needs a sphere collider.");
            Assert.IsTrue(sphere.isTrigger, "The GrabZone must be a trigger.");
            Assert.AreEqual(WidenInstrumentGrab.GrabZoneRadius, sphere.radius * sphere.transform.lossyScale.x, 0.0001f);

            int listed = 0;
            foreach (Collider collider in grab.colliders)
                if (collider == sphere) listed++;
            Assert.AreEqual(1, listed, "The interactable must list the GrabZone once.");
        }

        [Test]
        public void ZoneKeepsItsWorldSizeOnAScaledTool()
        {
            _tool.transform.localScale = Vector3.one * 0.05f;
            var grab = _tool.GetComponent<XRGrabInteractable>();

            Collider zone = WidenInstrumentGrab.EnsureGrabZone(_tool, grab, WidenInstrumentGrab.GrabZoneRadius);

            var sphere = (SphereCollider)zone;
            Assert.AreEqual(WidenInstrumentGrab.GrabZoneRadius, sphere.radius * sphere.transform.lossyScale.x, 0.0001f,
                "A scaled tool must still get a zone of the full size in the world.");
        }
    }
}
