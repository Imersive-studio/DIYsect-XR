using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds an ARM64 IL2CPP development APK for Quest 3 from the scenes that
    /// are enabled in the Build Profiles scene list.
    /// </summary>
    public static class BuildAndroid
    {
        const string OutputPath = "Builds/Android/DIYsect.apk";

        [MenuItem("Tools/DIYsect/Build Quest APK")]
        public static void BuildMenu() => Build();

        // Batch entry point: -executeMethod DIYsect.Editor.BuildAndroid.BuildBatch
        public static void BuildBatch()
        {
            bool ok = Build();
            EditorApplication.Exit(ok ? 0 : 1);
        }

        static bool Build()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            return Run("BuildAndroid", CreateOptions(scenes, OutputPath));
        }

        /// <summary>The ARM64 IL2CPP development build options shared by every Android build.</summary>
        public static BuildPlayerOptions CreateOptions(string[] scenes, string outputPath)
        {
            return new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.StrictMode
            };
        }

        /// <summary>Applies the shared Android player settings, builds, and logs the result. Returns true on success.</summary>
        public static bool Run(string label, BuildPlayerOptions options)
        {
            if (options.scenes == null || options.scenes.Length == 0)
            {
                Debug.LogError($"[{label}] No enabled scenes in the build settings.");
                return false;
            }

            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory(Path.GetDirectoryName(options.locationPathName));
            Debug.Log($"[{label}] Building {options.scenes.Length} scene(s): {string.Join(", ", options.scenes)}");
            var started = DateTime.UtcNow;
            BuildSummary summary = BuildPipeline.BuildPlayer(options).summary;
            var elapsed = DateTime.UtcNow - started;

            if (summary.result != BuildResult.Succeeded)
            {
                Debug.LogError($"[{label}] Build {summary.result}: {summary.totalErrors} error(s), {summary.totalWarnings} warning(s) after {elapsed.TotalSeconds:F0}s");
                return false;
            }

            Debug.Log($"[{label}] Build succeeded: {summary.outputPath}, {summary.totalSize / (1024 * 1024)} MB, {elapsed.TotalSeconds:F0}s, {summary.totalWarnings} warning(s)");
            return true;
        }
    }
}
