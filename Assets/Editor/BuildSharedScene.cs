// Run Tools > DIYsect > Build Shared Scene. It makes Professor_MR the one scene for the professor and the classmates.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Turns Professor_MR into the scene every device runs. It adds the classmate camera (orbit, tap to pin) next to the
    /// professor's headset rig, adds the classmate's auto join and status line to the network manager, and adds
    /// <see cref="ClassroomRole"/>, which switches on the half that matches the device and switches off the other.
    /// Safe to run more than once.
    /// </summary>
    public static class BuildSharedScene
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string ClassmateRigName = "ClassmateCamera";

        // Roots (or children of MRInteractionSetup) that belong to the professor's headset only.
        private static readonly string[] ProfessorObjectNames =
        {
            "XR Origin (XR Rig)", "DepthHandle", "GuidePen", "StudentViewWindow", "ClassRoster", "TableAlignmentHandle", "TableAlignment",
        };

        [MenuItem("Tools/DIYsect/Build Shared Scene")]
        public static void Apply()
        {
            // The camera tool opens, edits and saves the scene itself. The headset rig stays: the role switch decides.
            ClassmateDesktopRig.Apply(ScenePath, switchOffXr: false);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            XRINetworkGameManager manager = UnityEngine.Object.FindFirstObjectByType<XRINetworkGameManager>(FindObjectsInactive.Include);
            if (manager == null)
                throw new InvalidOperationException($"{ScenePath} has no XRINetworkGameManager.");

            var autoJoin = GetOrAdd<ClassmateAutoJoin>(manager.gameObject);
            var statusLabel = GetOrAdd<ClassmateStatusLabel>(manager.gameObject);
            var autoHost = manager.GetComponent<ProfessorAutoHost>() ?? throw new InvalidOperationException("Run Tools > DIYsect > Add Role Auto Connect first.");
            AddClassmateAutoJoin.HideWelcomePanel(scene);

            GameObject classmateRig = FindByName(scene, ClassmateRigName) ?? throw new InvalidOperationException($"{ClassmateRigName} was not created.");
            var professorObjects = new List<GameObject>();
            foreach (string name in ProfessorObjectNames)
            {
                GameObject found = FindByName(scene, name);
                if (found == null)
                    throw new InvalidOperationException($"{ScenePath} has no {name}.");
                professorObjects.Add(found);
            }

            // The professor can walk with the left thumbstick and snap-turn with the right, and press A or X to bring the table back.
            EnableProfessorLocomotion(scene);
            var alignment = UnityEngine.Object.FindFirstObjectByType<DIYsect.MixedReality.TableAlignment>(FindObjectsInactive.Include);
            if (alignment != null && alignment.GetComponent<DIYsect.MixedReality.TableRecenterInput>() == null)
                alignment.gameObject.AddComponent<DIYsect.MixedReality.TableRecenterInput>();

            var role = GetOrAdd<ClassroomRole>(manager.gameObject);
            var serialized = new SerializedObject(role);
            SetArray(serialized.FindProperty("_professorObjects"), professorObjects.ToArray());
            SetArray(serialized.FindProperty("_classmateObjects"), new[] { classmateRig });
            SetArray(serialized.FindProperty("_professorBehaviours"), new Behaviour[] { autoHost });
            SetArray(serialized.FindProperty("_classmateBehaviours"), new Behaviour[] { autoJoin, statusLabel });
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new System.IO.IOException($"Could not save {ScenePath}.");
            Debug.Log($"[BuildSharedScene] {ScenePath} is the shared scene: {professorObjects.Count} professor objects, the {ClassmateRigName}, and the role switch.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.BuildSharedScene.ApplyBatch
        public static void ApplyBatch()
        {
            try
            {
                Apply();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                EditorApplication.Exit(1);
            }
        }

        // The template is a tabletop game, so its move and turn providers ship switched off. They are the standard XR Interaction
        // Toolkit ones: this turns on moving, and snap turning only (continuous turning makes many people sick).
        private static void EnableProfessorLocomotion(Scene scene)
        {
            var origin = UnityEngine.Object.FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>(FindObjectsInactive.Include);
            if (origin == null)
                throw new InvalidOperationException($"{ScenePath} has no XR Origin.");
            Transform locomotion = origin.transform.Find("Locomotion") ?? throw new InvalidOperationException("The rig has no Locomotion object.");
            Switch(locomotion.Find("Move"), name => true);
            Switch(locomotion.Find("Turn"), name => name == "SnapTurnProvider");
        }

        private static void Switch(Transform target, Func<string, bool> enabledFor)
        {
            if (target == null)
                throw new InvalidOperationException("A locomotion object is missing from the rig.");
            target.gameObject.SetActive(true);
            PrefabUtility.RecordPrefabInstancePropertyModifications(target.gameObject);
            foreach (Behaviour provider in target.GetComponents<Behaviour>())
            {
                provider.enabled = enabledFor(provider.GetType().Name);
                EditorUtility.SetDirty(provider);
                PrefabUtility.RecordPrefabInstancePropertyModifications(provider);
            }
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t.gameObject;
            }
            return null;
        }

        private static void SetArray(SerializedProperty array, UnityEngine.Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
