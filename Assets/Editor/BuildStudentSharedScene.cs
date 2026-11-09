// Run Tools > DIYsect > Build Student Shared Scene. It makes LectureHall_VR the one scene for the headset student and the classmates.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Turns LectureHall_VR into a scene a headset student and the classmates on phones or a laptop can share. Netcode
    /// only keeps devices in step when they run the same scene, so the classmate's orbit camera and pin controls are
    /// added next to the student's headset rig, and <see cref="ClassroomRole"/> switches on the half that matches the
    /// device. The headset side is the one that hosts: <see cref="ProfessorAutoHost"/> joins a room if one exists and
    /// creates it if not, so the dissecting student can open the room and the classmates join it. In this scene the
    /// role called Professor means "the headset" (the student); the name comes from the shared scene it was built for.
    /// Safe to run more than once.
    /// </summary>
    public static class BuildStudentSharedScene
    {
        public const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        public const string ClassmateRigName = "ClassmateCamera";
        public const string HeadsetRigName = "XR Origin (XR Rig)";

        [MenuItem("Tools/DIYsect/Build Student Shared Scene")]
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
            var autoHost = GetOrAdd<ProfessorAutoHost>(manager.gameObject);
            AddClassmateAutoJoin.HideWelcomePanel(scene);

            GameObject classmateRig = FindByName(scene, ClassmateRigName) ?? throw new InvalidOperationException($"{ClassmateRigName} was not created.");
            GameObject headsetRig = FindByName(scene, HeadsetRigName) ?? throw new InvalidOperationException($"{ScenePath} has no {HeadsetRigName}.");

            // The headset's UI module reads only the mouse for a press, so a finger tap on a button would do nothing on a phone.
            // The scene's one EventSystem gets a second module for classmates; the role switch turns on the one that fits.
            var headsetUi = UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.UI.XRUIInputModule>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no XRUIInputModule.");
            var classmateUi = GetOrAdd<UnityEngine.InputSystem.UI.InputSystemUIInputModule>(headsetUi.gameObject);
            classmateUi.enabled = false;
            EditorUtility.SetDirty(classmateUi);

            var role = GetOrAdd<ClassroomRole>(manager.gameObject);
            var serialized = new SerializedObject(role);
            SetArray(serialized.FindProperty("_professorObjects"), new[] { headsetRig });
            SetArray(serialized.FindProperty("_classmateObjects"), new[] { classmateRig });
            SetArray(serialized.FindProperty("_professorBehaviours"), new Behaviour[] { autoHost, headsetUi });
            SetArray(serialized.FindProperty("_classmateBehaviours"), new Behaviour[] { autoJoin, statusLabel, classmateUi });
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"[BuildStudentSharedScene] {ScenePath} is the shared scene for the headset student and the classmates.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.BuildStudentSharedScene.ApplyBatch
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

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            return target.GetComponent<T>() ?? target.AddComponent<T>();
        }

        private static GameObject FindByName(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t.gameObject;
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
