using UnityEditor;

namespace DIYsect.Editor
{
    /// <summary>
    /// Builds one role as its own Quest APK, so the headset opens straight into that role's scene. The all-scenes
    /// build opens <c>SampleScene</c>, the template's own demo, first. Each role gets its own application id, so the
    /// professor and the student can both be installed. XR on, same options as <see cref="BuildAndroid"/> except that these are
    /// not development builds: Netcode's own logging helper runs only in development builds, and in the first Professor
    /// build it made both StartHost and StartClient throw on the headset, so the app could never join or host a room.
    /// Logs still reach logcat.
    /// </summary>
    public static class BuildAndroidRole
    {
        public const string ProfessorScene = "Assets/Scenes/Professor_MR.unity";
        public const string StudentScene = "Assets/Scenes/LectureHall_VR.unity";
        public const string ProfessorOutput = "Builds/Android-Professor/DIYsect-Professor.apk";
        public const string StudentOutput = "Builds/Android-Student/DIYsect-Student.apk";

        [MenuItem("Tools/DIYsect/Build Professor APK")]
        public static void BuildProfessorMenu() => BuildProfessor();

        [MenuItem("Tools/DIYsect/Build Student APK")]
        public static void BuildStudentMenu() => BuildStudent();

        // Batch entry points: -executeMethod DIYsect.Editor.BuildAndroidRole.BuildProfessorBatch (or BuildStudentBatch).
        // Run them without -nographics: the build renders a camera and needs a graphics device.
        public static void BuildProfessorBatch() => EditorApplication.Exit(BuildProfessor() ? 0 : 1);

        public static void BuildStudentBatch() => EditorApplication.Exit(BuildStudent() ? 0 : 1);

        public static BuildPlayerOptions CreateProfessorOptions() => NotDevelopment(BuildAndroid.CreateOptions(new[] { ProfessorScene }, ProfessorOutput));

        public static BuildPlayerOptions CreateStudentOptions() => NotDevelopment(BuildAndroid.CreateOptions(new[] { StudentScene }, StudentOutput));

        private static BuildPlayerOptions NotDevelopment(BuildPlayerOptions options)
        {
            options.options &= ~BuildOptions.Development;
            return options;
        }

        public static bool BuildProfessor()
        {
            using (new RoleBuildSettingsScope(".professor", "DIYsect Professor"))
                return BuildAndroid.Run("BuildAndroidProfessor", CreateProfessorOptions());
        }

        public static bool BuildStudent()
        {
            using (new RoleBuildSettingsScope(".student", "DIYsect Student"))
                return BuildAndroid.Run("BuildAndroidStudent", CreateStudentOptions());
        }
    }
}
