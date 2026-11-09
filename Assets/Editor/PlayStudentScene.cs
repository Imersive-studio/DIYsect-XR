// Run Tools > DIYsect > Play Student Scene, or start the editor with:
// Unity -projectPath . -executeMethod DIYsect.Editor.PlayStudentScene.Run
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.XR.Templates.MRTTabletopAssets;

namespace DIYsect.Editor
{
    /// <summary>
    /// Opens the student's lecture hall scene and enters Play mode, where the XR Interaction Simulator starts by
    /// itself. The template's welcome panel is skipped for you, the way pressing its Skip button does, because
    /// pointing a simulated controller at it is fiddly. The flag lives in SessionState, which survives the editor
    /// reloading its scripts when Play starts, and only this command sets it, so normal Play is untouched.
    /// </summary>
    [InitializeOnLoad]
    public static class PlayStudentScene
    {
        private const string SkipFlag = "DIYsect.PlayStudentScene.SkipCoaching";
        private const int FramesToWait = 20;   // let the scene's Start methods run first

        private static int _framesLeft;

        static PlayStudentScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/DIYsect/Play Student Scene")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/LectureHall_VR.unity", OpenSceneMode.Single);
            SessionState.SetBool(SkipFlag, true);
            EditorApplication.isPlaying = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(SkipFlag, false))
            {
                SessionState.SetBool(SkipFlag, false);
                _framesLeft = FramesToWait;
                EditorApplication.update += SkipWelcomePanel;
            }
            else if (change == PlayModeStateChange.ExitingPlayMode)
            {
                EditorApplication.update -= SkipWelcomePanel;
            }
        }

        private static void SkipWelcomePanel()
        {
            if (--_framesLeft > 0)
                return;
            EditorApplication.update -= SkipWelcomePanel;
            var goals = Object.FindFirstObjectByType<GoalManager>();
            if (goals == null)
            {
                Debug.LogWarning("Play Student Scene: no welcome panel found to skip.");
                return;
            }
            goals.OnSkipButtonPressed();
        }
    }
}
