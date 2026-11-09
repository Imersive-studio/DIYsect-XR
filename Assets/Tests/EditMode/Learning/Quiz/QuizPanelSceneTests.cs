using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
    }
}
