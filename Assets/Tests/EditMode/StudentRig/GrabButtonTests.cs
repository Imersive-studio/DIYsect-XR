using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;

namespace DIYsect.StudentRig.Tests
{
    /// <summary>
    /// The headset rig reads one input action asset. Its Select action, which grabs, is bound to the grip by the toolkit and also
    /// to the A button (right hand) and the X button (left hand), so a tool can be grabbed with either.
    /// </summary>
    public class GrabButtonTests
    {
        private const string ActionsPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions";

        [TestCase("XRI Right Interaction/Select", "<XRController>{RightHand}/primaryButton")]
        [TestCase("XRI Left Interaction/Select", "<XRController>{LeftHand}/primaryButton")]
        [TestCase("XRI Right Interaction/Select Value", "<XRController>{RightHand}/primaryButton")]
        [TestCase("XRI Left Interaction/Select Value", "<XRController>{LeftHand}/primaryButton")]
        public void TheFaceButtonAlsoGrabs(string action, string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ActionsPath);
            Assert.IsNotNull(asset, $"{ActionsPath} is missing.");
            InputAction select = asset.FindAction(action, throwIfNotFound: true);
            CollectionAssert.Contains(select.bindings.Select(binding => binding.path).ToArray(), path, $"{action} is not bound to {path}.");
            CollectionAssert.Contains(select.bindings.Select(binding => binding.path).ToArray(), action.EndsWith("Value")
                ? action.Contains("Right") ? "<XRController>{RightHand}/{Grip}" : "<XRController>{LeftHand}/{Grip}"
                : action.Contains("Right") ? "<XRController>{RightHand}/{GripButton}" : "<XRController>{LeftHand}/{GripButton}",
                "The grip must still grab.");
        }
    }
}
