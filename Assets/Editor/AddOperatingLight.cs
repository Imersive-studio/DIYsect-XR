// Run Tools > DIYsect > Add Operating Light after Create Scenes.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Hangs the surgical lamp (Tools/blender/make_surgical_lamp.py) over the dissection table in the VR and
    /// desktop scenes and puts a warm, soft-shadowed spot light on its lens face, aimed at the body.
    /// The professor's mixed reality scene is left out: there the real room lights the real table.
    /// Safe to run more than once: an existing lamp is moved back to the measured pose.
    /// </summary>
    public static class AddOperatingLight
    {
        private const string LampModel = "Assets/Models/Environment/Lamp/surgical_lamp.glb";
        private const string RootName = "DIYsect";
        private const string LampName = "OperatingLight";
        private const string LampLayerName = "OperatingLight";   // layer 9 in ProjectSettings/TagManager.asset
        private const string CloseUpCameraName = "CloseUpCamera";
        private const float HeightAboveTable = 1.0f;   // lens face above the table top, in metres
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        [MenuItem("Tools/DIYsect/Add Operating Light")]
        public static void Apply()
        {
            foreach (string path in Scenes)
            {
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                ApplyToScene(scene);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene))
                    throw new IOException($"Could not save {path}.");
            }
        }

        // Uses the existing table and camera in this scene; no body or network objects are copied.
        public static GameObject ApplyToScene(Scene scene)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(LampModel);
            if (model == null)
                throw new FileNotFoundException($"{LampModel} is missing or did not import. Run Tools/blender/make_surgical_lamp.py.");
            GameObject root = FindRoot(scene);
            Bounds top = FindTableTop(root);
            Transform lamp = root.transform.Find(LampName) ?? Place(model, root.transform);
            lamp.SetPositionAndRotation(new Vector3(top.center.x, top.max.y + HeightAboveTable, top.center.z), Quaternion.identity);
            ConfigureLight(lamp);
            HideFromCloseUpCamera(lamp);
            return lamp.gameObject;
        }

        // The lens face hangs at the same height as the close-up camera (one metre above the table), so the lamp
        // would fill the whole picture on the big screen. The lamp goes on its own layer and the close-up camera
        // does not render that layer; the lamp and its light are unchanged for every other camera.
        private static void HideFromCloseUpCamera(Transform lamp)
        {
            int layer = LayerMask.NameToLayer(LampLayerName);
            if (layer < 0)
                throw new InvalidOperationException($"The layer '{LampLayerName}' is missing from ProjectSettings/TagManager.asset.");
            foreach (Transform node in lamp.GetComponentsInChildren<Transform>(true))
                node.gameObject.layer = layer;
            Camera closeUp = null;
            foreach (GameObject root in lamp.gameObject.scene.GetRootGameObjects())
                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    if (camera.name == CloseUpCameraName)
                        closeUp = camera;
            if (closeUp == null)
                throw new InvalidOperationException($"{lamp.gameObject.scene.path} has no {CloseUpCameraName}. Run Tools > DIYsect > Create Scenes first.");
            closeUp.cullingMask &= ~(1 << layer);
        }

        private static Transform Place(GameObject model, Transform root)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, root);
            instance.name = LampName;
            foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // it must not shadow its own light
            return instance.transform;
        }

        private static void ConfigureLight(Transform lamp)
        {
            Transform holder = lamp.Find("Spot") ?? new GameObject("Spot").transform;
            holder.SetParent(lamp, false);
            holder.localPosition = new Vector3(0f, -0.02f, 0f);
            holder.localRotation = Quaternion.Euler(90f, 0f, 0f);   // straight down onto the table
            var light = AnatomyBodyDissection.GetOrAdd<Light>(holder.gameObject);
            light.type = LightType.Spot;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 2.2f;
            light.range = 2.5f;
            light.spotAngle = 60f;
            light.innerSpotAngle = 35f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.8f;
            light.renderMode = LightRenderMode.ForcePixel;
        }

        private static GameObject FindRoot(Scene scene)
        {
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    return candidate;
            throw new InvalidOperationException($"{scene.path} has no {RootName} root. Run Tools > DIYsect > Create Scenes.");
        }

        private static Bounds FindTableTop(GameObject root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{root.scene.path} has no DissectionTable/Top under {RootName}.");
        }
    }
}
