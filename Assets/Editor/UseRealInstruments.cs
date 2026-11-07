// Run Tools > DIYsect > Use Real Instruments after Tools/blender/extract_instruments.py (Apply Realistic Look runs it).
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Gives the placeholder scalpel and forceps their real look (A3, "SURGICAL INSTRUMENT" by ferofluid,
    /// CC BY 4.0, with a project-authored blade). Only what is seen changes: the primitive parts stop rendering
    /// but keep their colliders, and the real model is placed with its origin on the tool's Tip, which is where
    /// extract_instruments.py puts the blade point and the forceps jaws. Grabbing, the tip trigger, networking and
    /// the guiding-hand depth (which moves the scalpel's Model group) all work as before. Safe to run more than once.
    /// </summary>
    public static class UseRealInstruments
    {
        private const string SteelPath = "Assets/Materials/Instruments/SurgicalSteel.mat";

        private readonly struct Tool
        {
            public readonly string Prefab;
            public readonly string Model;
            public readonly string[] PlaceholderParts;

            public Tool(string prefab, string model, params string[] placeholderParts)
            {
                Prefab = prefab;
                Model = model;
                PlaceholderParts = placeholderParts;
            }
        }

        private static readonly Tool[] Tools =
        {
            new Tool("Assets/Prefabs/ScalpelPlaceholder.prefab", "Assets/Models/Instruments/scalpel.glb", "Handle", "Blade"),
            new Tool("Assets/Prefabs/ForcepsPlaceholder.prefab", "Assets/Models/Instruments/forceps.glb", "ArmLeft", "ArmRight"),
        };

        [MenuItem("Tools/DIYsect/Use Real Instruments")]
        public static void Apply()
        {
            Material steel = LoadOrCreateSteel();
            foreach (Tool tool in Tools)
                ApplyTo(tool, steel);
        }

        private static void ApplyTo(Tool tool, Material steel)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(tool.Model);
            if (model == null)
                throw new FileNotFoundException($"{tool.Model} is missing. Run Tools/blender/extract_instruments.py.");
            GameObject root = PrefabUtility.LoadPrefabContents(tool.Prefab);
            try
            {
                Transform tip = FindDeep(root.transform, "Tip");
                foreach (string part in tool.PlaceholderParts)
                    FindDeep(root.transform, part).GetComponent<Renderer>().enabled = false;

                string visualName = Path.GetFileNameWithoutExtension(tool.Model);
                Transform visual = tip.parent.Find(visualName);
                if (visual == null)
                {
                    visual = ((GameObject)PrefabUtility.InstantiatePrefab(model, tip.parent)).transform;
                    visual.name = visualName;
                }
                visual.SetLocalPositionAndRotation(tip.localPosition, Quaternion.identity);
                visual.localScale = Vector3.one;
                foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                foreach (Renderer renderer in visual.GetComponentsInChildren<Renderer>(true))
                {
                    var materials = new Material[renderer.sharedMaterials.Length];
                    for (int i = 0; i < materials.Length; i++)
                        materials[i] = steel;
                    renderer.sharedMaterials = materials;
                }
                PrefabUtility.SaveAsPrefabAsset(root, tool.Prefab);
                Debug.Log($"{tool.Prefab}: real {visualName} placed on its tip at {tip.localPosition}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static Material LoadOrCreateSteel()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SteelPath);
            if (material != null)
                return material;
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
                throw new InvalidOperationException("URP Lit shader not found.");
            Directory.CreateDirectory(Path.GetDirectoryName(SteelPath));
            material = new Material(lit) { name = "SurgicalSteel" };
            material.SetColor("_BaseColor", new Color(0.80f, 0.81f, 0.83f));
            material.SetFloat("_Metallic", 1f);
            material.SetFloat("_Smoothness", 0.85f);
            AssetDatabase.CreateAsset(material, SteelPath);
            return material;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
                if (candidate.name == name)
                    return candidate;
            throw new InvalidOperationException($"{root.name} has no child named {name}.");
        }
    }
}
