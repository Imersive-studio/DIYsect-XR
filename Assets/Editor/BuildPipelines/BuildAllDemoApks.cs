using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace DIYsect.Editor
{
    /// <summary>
    /// One command for the evening before a demo: builds the Professor, Student and Classmate APKs in a row from the
    /// same commit, using the existing build methods (each one restores the project settings it changed), and writes
    /// Builds/DemoBuilds.txt next to them.
    /// </summary>
    public static class BuildAllDemoApks
    {
        public const string SummaryPath = "Builds/DemoBuilds.txt";

        [MenuItem("Tools/DIYsect/Build All Demo APKs")]
        public static void BuildMenu() => Build();

        // Batch entry point: -executeMethod DIYsect.Editor.BuildAllDemoApks.BuildBatch (without -nographics)
        public static void BuildBatch() => EditorApplication.Exit(Build() ? 0 : 1);

        public static IReadOnlyList<DemoBuildStep> CreateSteps() => new[]
        {
            new DemoBuildStep("Professor", BuildAndroidRole.ProfessorOutput, BuildAndroidRole.BuildProfessor),
            new DemoBuildStep("Student", BuildAndroidRole.StudentOutput, BuildAndroidRole.BuildStudent),
            new DemoBuildStep("Classmate", BuildAndroidClassmate.OutputPath, BuildAndroidClassmate.Build)
        };

        public static bool Build()
        {
            DemoBuildResult result = new DemoBuildSequence(CreateSteps()).Run();
            if (!result.Succeeded)
            {
                Debug.LogError($"[BuildAllDemoApks] Stopped at {result.Failed.Name}. {result.Error}");
                return false;
            }
            WriteSummary(result.Completed);
            Debug.Log($"[BuildAllDemoApks] All {result.Completed.Count} builds succeeded. Summary: {SummaryPath}");
            return true;
        }

        private static void WriteSummary(IReadOnlyList<DemoBuildStep> steps)
        {
            var entries = steps.Select(s => new DemoBuildSummary.Entry(s.Name, s.OutputPath, File.Exists(s.OutputPath) ? new FileInfo(s.OutputPath).Length : -1)).ToList();
            Directory.CreateDirectory(Path.GetDirectoryName(SummaryPath));
            File.WriteAllText(SummaryPath, DemoBuildSummary.Format(ReadCommit(), DateTime.Now, entries));
        }

        // The commit the project is on, or "unknown" when git is not available.
        private static string ReadCommit()
        {
            try
            {
                var info = new ProcessStartInfo("git", "rev-parse HEAD")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using (Process git = Process.Start(info))
                {
                    string output = git.StandardOutput.ReadToEnd().Trim();
                    git.WaitForExit();
                    return git.ExitCode == 0 ? output : "unknown";
                }
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}
