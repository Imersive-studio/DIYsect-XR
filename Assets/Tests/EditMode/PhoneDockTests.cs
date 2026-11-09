using System.Linq;
using DIYsect.Classroom;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Classroom.Tests
{
    public class PhoneDockTests
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";

        [Test]
        public void TheDockHasDrawAndClearWiredToTheSketch()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var draw = Object.FindFirstObjectByType<ClassmateDrawButton>(FindObjectsInactive.Include);
            Assert.IsNotNull(draw, "The phone has no Draw button. Run Tools > DIYsect > Rebuild Phone UI.");
            var serialized = new SerializedObject(draw);
            foreach (string field in new[] { "_sketch", "_button", "_label", "_background", "_clear" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, $"The Draw button has no {field}.");
            var button = (Button)serialized.FindProperty("_button").objectReferenceValue;
            var clear = (Button)serialized.FindProperty("_clear").objectReferenceValue;
            Assert.AreEqual("Dock", button.transform.parent.name, "Draw belongs in the dock.");
            Assert.AreEqual(button.transform.parent, clear.transform.parent, "Clear sits next to Draw.");
        }

        [Test]
        public void EveryPhoneMessageChipStartsHiddenWithItsText()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var follow in Object.FindObjectsByType<ShowWith>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var companion = (Behaviour)new SerializedObject(follow).FindProperty("_companion").objectReferenceValue;
                Assert.IsNotNull(companion, $"{follow.name} has no chip to follow it.");
                Assert.AreEqual(follow.gameObject.activeSelf, companion.enabled,
                    $"{follow.name}: the chip must start {(follow.gameObject.activeSelf ? "shown" : "hidden")} like its text, or an empty pill shows on the phone.");
            }
        }
    }
}
