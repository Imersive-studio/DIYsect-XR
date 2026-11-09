// Run Tools > DIYsect > Arrange Professor Panels. Build Shared Scene and Add Room Code Board also run it at the end.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Places the professor's panels on one even arc around where the professor stands, so none of them overlaps another.
    /// Each editor step that builds a panel puts it somewhere sensible on its own, but together they piled up on the
    /// professor's right. From left to right: the room code board, the student view window, the class roster (with the
    /// speaking panel under it) and the quiz panel. Only the bearing changes: each panel keeps its height and tilt, and is
    /// turned about the vertical by the same angle it moved, so the side that faces the professor still faces them.
    /// Safe to run more than once.
    /// </summary>
    public static class ArrangeProfessorPanels
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        public const float EyeHeight = 1.6f;

        /// <summary>One panel: its object name under the DIYsect root, its bearing from the professor (degrees, right is positive) and its distance.</summary>
        public readonly struct Slot
        {
            public Slot(string name, float bearing, float distance)
            {
                Name = name;
                Bearing = bearing;
                Distance = distance;
            }

            public string Name { get; }
            public float Bearing { get; }
            public float Distance { get; }
        }

        public static readonly Slot[] Slots =
        {
            new Slot("RoomCodeBoard", -72f, 2.0f),
            new Slot("StudentViewWindow", -28f, 1.75f),
            new Slot("ClassRoster", 26f, 1.75f),
            new Slot("SpeakPanel", 26f, 1.75f),
            new Slot("QuizPanel", 56f, 1.85f),
        };

        [MenuItem("Tools/DIYsect/Arrange Professor Panels")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ArrangeIn(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        /// <summary>Arranges the panels in an open scene without saving it, for steps that save the scene themselves.</summary>
        public static void ArrangeIn(Scene scene)
        {
            Vector3 eye = ProfessorEye(scene);
            var moved = new List<string>();
            foreach (Slot slot in Slots)
            {
                Transform panel = Find(scene, slot.Name);
                if (panel == null)
                    continue;
                float oldBearing = Bearing(eye, panel.position);
                float radians = slot.Bearing * Mathf.Deg2Rad;
                panel.position = new Vector3(eye.x + Mathf.Sin(radians) * slot.Distance, panel.position.y, eye.z + Mathf.Cos(radians) * slot.Distance);
                panel.rotation = Quaternion.AngleAxis(slot.Bearing - oldBearing, Vector3.up) * panel.rotation;
                EditorUtility.SetDirty(panel);
                moved.Add(slot.Name);
            }
            Debug.Log($"[ArrangeProfessorPanels] {scene.path}: arranged {string.Join(", ", moved)}.");
        }

        /// <summary>Where the professor's eyes are: above the headset rig, at standing height.</summary>
        public static Vector3 ProfessorEye(Scene scene)
        {
            Transform rig = Find(scene, "XR Origin (XR Rig)") ?? throw new InvalidOperationException($"{scene.path} has no XR Origin (XR Rig).");
            return new Vector3(rig.position.x, EyeHeight, rig.position.z);
        }

        /// <summary>The bearing in degrees from the eye to a point, right of straight ahead (+Z) is positive.</summary>
        public static float Bearing(Vector3 eye, Vector3 point)
        {
            return Mathf.Atan2(point.x - eye.x, point.z - eye.z) * Mathf.Rad2Deg;
        }

        public static Transform Find(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "DIYsect" && root.name != "MRInteractionSetup")
                    continue;
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name && (t.parent == null || t.parent.name == "DIYsect" || t.parent.name == "MRInteractionSetup"))
                        return t;
            }
            return null;
        }

        // Batch entry point: -executeMethod DIYsect.Editor.ArrangeProfessorPanels.ApplyBatch
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
    }
}
