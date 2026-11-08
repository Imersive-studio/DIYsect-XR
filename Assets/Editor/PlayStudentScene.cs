// Run Tools > DIYsect > Play Student Scene, or start the editor with:
// Unity -projectPath . -executeMethod DIYsect.Editor.PlayStudentScene.Run
using UnityEditor;
using UnityEditor.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>Opens the student's lecture hall scene and enters Play mode, where the XR Interaction Simulator starts by itself.</summary>
    public static class PlayStudentScene
    {
        [MenuItem("Tools/DIYsect/Play Student Scene")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LectureHall_VR.unity", OpenSceneMode.Single);
            EditorApplication.isPlaying = true;
        }
    }
}
