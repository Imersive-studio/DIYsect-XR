using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.OpenXR;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

namespace DIYsect.Editor
{
    /// <summary>
    /// Logs the OpenXR Project Validation issues (and the XR Core Utils build
    /// rules, when reachable) for the Android and Standalone build targets.
    /// </summary>
    public static class XrValidationReport
    {
        static readonly BuildTargetGroup[] Targets = { BuildTargetGroup.Android, BuildTargetGroup.Standalone };

        [MenuItem("Tools/DIYsect/Report XR Validation")]
        public static void Report() => Run(false);

        [MenuItem("Tools/DIYsect/Fix XR Validation Issues")]
        public static void Fix() => Run(true);

        // Batch entry points: -executeMethod DIYsect.Editor.XrValidationReport.ReportBatch
        public static void ReportBatch() => Finish(Run(false));
        public static void FixBatch() => Finish(Run(true));

        static void Finish(int errors) => EditorApplication.Exit(errors > 0 ? 1 : 0);

        static int Run(bool applyFixes)
        {
            int totalErrors = 0;
            var previousGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            foreach (var group in Targets)
            {
                // Several OpenXR rules read the selected build target group.
                EditorUserBuildSettings.selectedBuildTargetGroup = group;
                Debug.Log($"[XrValidation] ===== {group} =====");
                LogLoaders(group);
                LogFeatures(group);

                var issues = new List<OpenXRFeature.ValidationRule>();
                OpenXRProjectValidation.GetCurrentValidationIssues(issues, group);
                Debug.Log($"[XrValidation] {group}: {issues.Count} OpenXR issue(s)");
                foreach (var issue in issues)
                {
                    Debug.Log(Describe(group, "OpenXR", issue.message, issue.error, issue.fixIt != null, issue.fixItAutomatic, issue.fixItMessage));
                    if (issue.error) totalErrors++;
                }

                if (applyFixes)
                {
                    foreach (var issue in issues.Where(i => i.fixIt != null && i.fixItAutomatic))
                    {
                        Debug.Log($"[XrValidation] fixing ({group}): {issue.message}");
                        issue.fixIt();
                    }
                    AssetDatabase.SaveAssets();
                    issues.Clear();
                    OpenXRProjectValidation.GetCurrentValidationIssues(issues, group);
                    Debug.Log($"[XrValidation] {group}: {issues.Count} OpenXR issue(s) after fixes");
                    foreach (var issue in issues)
                        Debug.Log(Describe(group, "OpenXR-after", issue.message, issue.error, issue.fixIt != null, issue.fixItAutomatic, issue.fixItMessage));
                }

                totalErrors += ReportCoreUtilsRules(group, applyFixes);
            }
            EditorUserBuildSettings.selectedBuildTargetGroup = previousGroup;
            Debug.Log($"[XrValidation] total errors: {totalErrors}");
            return totalErrors;
        }

        static string Describe(BuildTargetGroup group, string source, string message, bool error, bool hasFix, bool auto, string fixMessage)
        {
            return $"[XrValidation] {group} {source} {(error ? "ERROR" : "WARNING")} fixable={hasFix} auto={auto} :: {message}" +
                   (hasFix ? $" (fix: {fixMessage})" : string.Empty);
        }

        static void LogLoaders(BuildTargetGroup group)
        {
            var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(group);
            if (settings == null || settings.Manager == null)
            {
                Debug.Log($"[XrValidation] {group}: no XR General Settings");
                return;
            }
            var loaders = settings.Manager.activeLoaders.Select(l => l.GetType().Name);
            Debug.Log($"[XrValidation] {group}: loaders = [{string.Join(", ", loaders)}], initOnStart={settings.InitManagerOnStart}");
        }

        static void LogFeatures(BuildTargetGroup group)
        {
            var features = OpenXRSettings.GetSettingsForBuildTargetGroup(group)?.GetFeatures<OpenXRFeature>() ?? new OpenXRFeature[0];
            var enabled = features.Where(f => f.enabled).Select(f => f.name).OrderBy(n => n);
            Debug.Log($"[XrValidation] {group}: enabled OpenXR features = [{string.Join("; ", enabled)}]");
        }

        // BuildValidator keeps its rule table internal, so read it by reflection.
        static int ReportCoreUtilsRules(BuildTargetGroup group, bool applyFixes)
        {
            int errors = 0;
            try
            {
                var type = Type.GetType("Unity.XR.CoreUtils.Editor.BuildValidator, Unity.XR.CoreUtils.Editor");
                var prop = type?.GetProperty("PlatformRules", BindingFlags.NonPublic | BindingFlags.Static);
                if (prop?.GetValue(null) is not IDictionary table || !table.Contains(group))
                {
                    Debug.Log($"[XrValidation] {group}: no XR Core Utils rules registered");
                    return 0;
                }

                int failing = 0;
                foreach (var rule in (IEnumerable)table[group])
                {
                    var t = rule.GetType();
                    var enabled = (Func<bool>)t.GetProperty("IsRuleEnabled").GetValue(rule);
                    if (enabled != null && !enabled()) continue;
                    var check = (Func<bool>)t.GetProperty("CheckPredicate").GetValue(rule);
                    if (check == null || check()) continue;
                    failing++;
                    bool isError = (bool)t.GetProperty("Error").GetValue(rule);
                    var message = (string)t.GetProperty("Message").GetValue(rule);
                    var category = (string)t.GetProperty("Category").GetValue(rule);
                    bool hasFix = t.GetProperty("FixIt").GetValue(rule) != null;
                    bool auto = (bool)t.GetProperty("FixItAutomatic").GetValue(rule);
                    var fixMessage = (string)t.GetProperty("FixItMessage").GetValue(rule);
                    Debug.Log(Describe(group, "CoreUtils/" + category, message, isError, hasFix, auto, fixMessage));
                    if (applyFixes && hasFix && auto)
                    {
                        Debug.Log($"[XrValidation] fixing ({group}): {message}");
                        ((Action)t.GetProperty("FixIt").GetValue(rule))();
                        isError = !check();
                    }
                    if (isError) errors++;
                }
                Debug.Log($"[XrValidation] {group}: {failing} XR Core Utils issue(s)");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[XrValidation] {group}: XR Core Utils rules not readable: {e.Message}");
            }
            return errors;
        }
    }
}
