using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using XRMultiplayer;

namespace DIYsect.Learning.Tests
{
    /// <summary>
    /// The quiz panel disables itself on the headset when it has no round, status text or question set. The play mode tests
    /// hand it a question set in code, so a scene with an empty reference passed them and only a headset showed the error.
    /// </summary>
    public class QuizPanelSceneTests
    {
        private const string ProfessorScene = "Assets/Scenes/Professor_MR.unity";

        [TearDown]
        public void CloseScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [Test]
        public void TheProfessorsQuizPanelHasEverythingItNeedsInTheSavedScene()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            QuizPanel panel = Object.FindFirstObjectByType<QuizPanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(panel, "Professor_MR has no QuizPanel. Run Tools > DIYsect > Add Quiz.");

            var serialized = new SerializedObject(panel);
            Assert.IsNotNull(serialized.FindProperty("_quiz").objectReferenceValue, "The panel has no QuizRound. Run Tools > DIYsect > Add Quiz.");
            Assert.IsNotNull(serialized.FindProperty("_status").objectReferenceValue, "The panel has no status text. Run Tools > DIYsect > Add Quiz.");
            var questions = serialized.FindProperty("_questions").objectReferenceValue as QuizQuestionSet;
            Assert.IsNotNull(questions, "The panel has no question set saved in the scene, so it turns itself off on the headset. Run Tools > DIYsect > Add Quiz.");
            Assert.Greater(questions.Count, 0, "The question set is empty.");
        }

        [Test]
        public void EveryQuizButtonHasAReachablePressAreaAndKeepsItsVisualsOffTheCloseUpCamera()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var panel = Object.FindFirstObjectByType<QuizPanel>(FindObjectsInactive.Include);
            foreach (string name in new[] { "PreviousQuestion", "NextQuestion", "Start", "End", "Clear" })
            {
                Transform button = panel.transform.Find("Content/" + name);
                Assert.IsNotNull(button, name);
                Assert.IsNotNull(button.GetComponent<XRSimpleInteractable>(), name);
                Transform press = button.Find("PressArea");
                Assert.IsNotNull(press, name + " needs a press area on Default.");
                Assert.AreEqual(0, press.gameObject.layer, name);
                Assert.IsNotNull(press.GetComponent<BoxCollider>(), name);
                Assert.IsNull(button.GetComponent<Collider>(), "The visual must not intercept the ray.");
                foreach (Renderer renderer in button.GetComponentsInChildren<Renderer>(true))
                    Assert.AreEqual(LayerMask.NameToLayer("CloseUpScreen"), renderer.gameObject.layer, renderer.name);
            }
        }

        [Test]
        public void TheStudentQuizIsWiredAndSavedAsStudentOnly()
        {
            EditorSceneManager.OpenScene(ProfessorScene, OpenSceneMode.Single);
            var answer = Object.FindFirstObjectByType<QuizHeadsetAnswer>(FindObjectsInactive.Include);
            Assert.IsNotNull(answer, "Run Add Quiz before Build Shared Scene.");
            Assert.IsFalse(answer.enabled);
            var serialized = new SerializedObject(answer);
            foreach (string field in new[] { "_quiz", "_leftAim", "_rightAim", "_card", "_question", "_status" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, field);
            var card = (GameObject)serialized.FindProperty("_card").objectReferenceValue;
            Assert.IsFalse(card.activeSelf);
            var role = Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include);
            SerializedProperty students = new SerializedObject(role).FindProperty("_studentBehaviours");
            int matches = 0;
            for (int i = 0; i < students.arraySize; i++)
                if (students.GetArrayElementAtIndex(i).objectReferenceValue == answer)
                    matches++;
            Assert.AreEqual(1, matches, "The role switch must enable the quiz exactly once for students.");
        }
    }
}
