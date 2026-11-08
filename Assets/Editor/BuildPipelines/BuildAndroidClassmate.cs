using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds the classmate scene as a phone APK that never starts XR. It shares the build steps with
    /// <see cref="BuildAndroid"/>; only the scene list, output path and temporary player settings differ.
    /// </summary>
    public static class BuildAndroidClassmate
    {
        public const string ScenePath = "Assets/Scenes/Classmate_Desktop.unity";
        public const string OutputPath = "Builds/Android-Classmate/DIYsect-Classmate.apk";

        [MenuItem("Tools/DIYsect/Build Classmate Phone APK")]
        public static void BuildMenu() => Build();

        // Batch entry point: -executeMethod DIYsect.Editor.BuildAndroidClassmate.BuildBatch
        public static void BuildBatch()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        /// <summary>Options of the phone build: only the classmate scene, written to <see cref="OutputPath"/>.</summary>
        /// <remarks>
        /// Not a development build: that mode puts Unity's red on-screen error console over the classmate's view.
        /// Logs still reach logcat.
        /// </remarks>
        public static BuildPlayerOptions CreateOptions()
        {
            BuildPlayerOptions options = BuildAndroid.CreateOptions(new[] { ScenePath }, OutputPath);
            options.options &= ~BuildOptions.Development;
            return options;
        }

        /// <summary>Builds the phone APK. The XR, identifier and name changes are undone even if the build fails.</summary>
        public static bool Build()
        {
            using (new PhoneBuildSettingsScope())
            {
                return BuildAndroid.Run("BuildAndroidClassmate", CreateOptions());
            }
        }
    }
}
