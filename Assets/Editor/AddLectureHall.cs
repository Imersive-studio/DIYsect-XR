// Run Tools > DIYsect > Add Lecture Hall after simplifying the hall (Tools/blender/simplify_room.py).
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Puts the simplified lecture hall (A2, CC BY 4.0, Heat Of Fusion) around the student's dissection table in
    /// LectureHall_VR and removes the primitive room placeholder. The placement is measured from the hall itself:
    /// it is turned so its projection screen is ahead of the student and the seats behind, then moved so the screen
    /// is centred <see cref="ScreenDistance"/> beyond the table, which puts the table in the clear front area of the
    /// hall, and its floor level stays at the scene floor. The hall casts no shadows, so the scene's light reaches
    /// the room under its closed ceiling. Safe to run more than once.
    /// </summary>
    public static class AddLectureHall
    {
        private const string HallModel = "Assets/Models/Environment/lecture_hall.glb";
        private const string ScenePath = "Assets/Scenes/LectureHall_VR.unity";
        private const string RootName = "DIYsect";
        private const string HallName = "LectureHall";
        private const string PlaceholderPrefix = "RoomPlaceholder";
        private const string ScreenPart = "screen_screen";
        private const string SeatPart = "chair";
        // Metres from the table centre to the hall's projection screen. The hall's clear front area is about 3.5 m
        // deep (screen wall to first desk row); 2.1 m keeps the table and the standing student inside it and puts
        // the close-up screen (3.2 m from the student, 2 m from the table) just in front of the projection wall.
        private const float ScreenDistance = 2.1f;

        [MenuItem("Tools/DIYsect/Add Lecture Hall")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            EnsureInScene(scene, removePlaceholder: true);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        /// <summary>
        /// Puts the hall under the DIYsect root of a scene that is already open and returns it, placed the same way as in
        /// LectureHall_VR. Does not save. The shared scene uses this for the student role.
        /// </summary>
        public static GameObject EnsureInScene(Scene scene, bool removePlaceholder)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(HallModel);
            if (model == null)
                throw new FileNotFoundException($"{HallModel} is missing. Run Tools/blender/simplify_room.py on the downloaded hall.");
            Transform root = FindRoot(scene);
            if (removePlaceholder)
                RemovePlaceholder(root);

            Transform hall = root.Find(HallName);
            if (hall == null)
            {
                hall = ((GameObject)PrefabUtility.InstantiatePrefab(model, root)).transform;
                hall.name = HallName;
            }
            Bounds tableTop = FindTableTop(root, scene.path);

            // Face the screen toward the student: try the imported heading, turn it around if the seats end up in front.
            hall.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            if (CentreOf(hall, ScreenPart).z < CentreOf(hall, SeatPart).z)
                hall.rotation = Quaternion.Euler(0f, 180f, 0f);
            Vector3 screen = CentreOf(hall, ScreenPart);
            Vector3 target = new Vector3(tableTop.center.x, 0f, tableTop.center.z + ScreenDistance);
            hall.position = new Vector3(target.x - screen.x, root.position.y, target.z - screen.z);

            int renderers = 0;
            foreach (Renderer renderer in hall.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderers++;
            }

            Debug.Log($"{scene.path}: lecture hall at {hall.position}, heading {hall.eulerAngles.y:F0} degrees, {renderers} renderers without shadows.");
            return hall.gameObject;
        }

        private static void RemovePlaceholder(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (!child.name.StartsWith(PlaceholderPrefix))
                    continue;
                string removed = child.name;
                UnityEngine.Object.DestroyImmediate(child.gameObject);
                Debug.Log($"Removed {removed}.");
            }
        }

        // Centre of the combined bounds of every renderer whose name contains the part name.
        private static Vector3 CentreOf(Transform hall, string part)
        {
            Bounds? bounds = null;
            foreach (Renderer renderer in hall.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.name.IndexOf(part, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (bounds == null)
                    bounds = renderer.bounds;
                else
                {
                    Bounds grown = bounds.Value;
                    grown.Encapsulate(renderer.bounds);
                    bounds = grown;
                }
            }
            if (bounds == null)
                throw new InvalidOperationException($"The lecture hall has no part named like '{part}'.");
            return bounds.Value.center;
        }

        private static Transform FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    return candidate.transform;
            throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
        }

        private static Bounds FindTableTop(Transform root, string scenePath)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{scenePath} has no DissectionTable/Top under {RootName}.");
        }
    }
}
