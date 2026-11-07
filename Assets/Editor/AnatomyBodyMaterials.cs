// Run by Tools > DIYsect > Add Body Drapes And Materials (Create Anatomy Body also runs it).
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Replaces the flat exported colors with wet-tissue URP Lit materials. Each renderer slot is matched by
    /// the model it belongs to (the GLB instance name under Supine) and the slot's original glTF material
    /// name, read from the imported model so the result is the same on every run. The first matching rule wins.
    /// To support a new tissue, add a row to <see cref="Tissues"/> and a rule to <see cref="Rules"/>.
    /// Materials are created once in Assets/Materials/Tissue and then left alone, so hand tuning is kept.
    /// </summary>
    public static class AnatomyBodyMaterials
    {
        private const string Folder = "Assets/Materials/Tissue";
        private const string SupineName = "Supine";

        private readonly struct Tissue
        {
            public readonly string Name;
            public readonly Color Color;        // sRGB
            public readonly float Smoothness;   // wet tissue is glossy; cloth and dry bone are not
            public readonly bool TwoSided;      // thin sheets seen from both sides

            public Tissue(string name, Color color, float smoothness, bool twoSided = false)
            {
                Name = name;
                Color = color;
                Smoothness = smoothness;
                TwoSided = twoSided;
            }
        }

        private readonly struct Rule
        {
            public readonly string Model;      // part of the GLB instance name, or "" for any
            public readonly string Slot;       // part of the original material name, or "" for any
            public readonly string Tissue;

            public Rule(string model, string slot, string tissue)
            {
                Model = model;
                Slot = slot;
                Tissue = tissue;
            }
        }

        private static readonly Tissue[] Tissues =
        {
            new Tissue("Skin", new Color(0.80f, 0.60f, 0.49f), 0.35f),
            new Tissue("SubcutaneousFat", new Color(0.95f, 0.80f, 0.42f), 0.7f),
            new Tissue("SkeletalMuscle", new Color(0.55f, 0.10f, 0.08f), 0.6f),
            new Tissue("Tendon", new Color(0.90f, 0.87f, 0.80f), 0.65f),
            new Tissue("Ligament", new Color(0.86f, 0.82f, 0.74f), 0.55f),
            new Tissue("Cartilage", new Color(0.80f, 0.85f, 0.86f), 0.7f),
            new Tissue("Bone", new Color(0.89f, 0.84f, 0.72f), 0.35f),
            new Tissue("Omentum", new Color(0.96f, 0.80f, 0.36f), 0.75f, twoSided: true),
            new Tissue("Lung", new Color(0.86f, 0.55f, 0.55f), 0.55f),
            new Tissue("Myocardium", new Color(0.55f, 0.13f, 0.11f), 0.7f),
            new Tissue("Liver", new Color(0.45f, 0.13f, 0.09f), 0.8f),
            new Tissue("Kidney", new Color(0.50f, 0.17f, 0.12f), 0.75f),
            new Tissue("Spleen", new Color(0.40f, 0.10f, 0.16f), 0.75f),
            new Tissue("Stomach", new Color(0.85f, 0.58f, 0.52f), 0.75f),
            new Tissue("Mucosa", new Color(0.72f, 0.30f, 0.28f), 0.8f),
            new Tissue("SmallBowel", new Color(0.88f, 0.62f, 0.55f), 0.8f),
            new Tissue("Colon", new Color(0.82f, 0.62f, 0.50f), 0.75f),
            new Tissue("Gallbladder", new Color(0.30f, 0.42f, 0.22f), 0.85f),
            new Tissue("Pancreas", new Color(0.92f, 0.74f, 0.52f), 0.6f),
            new Tissue("SurgicalDrape", new Color(0.16f, 0.42f, 0.52f), 0.12f, twoSided: true),
        };

        // Specific rules come first: the first match wins.
        private static readonly Rule[] Rules =
        {
            new Rule("skin_flap", "Subcutaneous", "SubcutaneousFat"),
            new Rule("skin_flap", "", "Skin"),
            new Rule("surgical_drapes", "", "SurgicalDrape"),
            new Rule("", "Tendon", "Tendon"),
            new Rule("", "Ligament", "Ligament"),
            new Rule("", "Cartilage", "Cartilage"),
            new Rule("", "Bone", "Bone"),
            new Rule("", "Peritoneum", "Omentum"),
            new Rule("", "Lung", "Lung"),
            new Rule("atrium", "", "Myocardium"),
            new Rule("ventricle", "", "Myocardium"),
            new Rule("mucosa_of_stomach", "", "Mucosa"),
            new Rule("stomach", "", "Stomach"),
            new Rule("duodenum", "", "SmallBowel"),
            new Rule("colon", "", "Colon"),
            new Rule("gallbladder", "", "Gallbladder"),
            new Rule("pancreas", "", "Pancreas"),
            new Rule("liver", "", "Liver"),
            new Rule("kidney", "", "Kidney"),
            new Rule("spleen", "", "Spleen"),
            new Rule("muscle", "", "SkeletalMuscle"),
            new Rule("diaphragm", "", "SkeletalMuscle"),
        };

        /// <summary>Assigns tissue materials to every renderer under the body's Supine pivot.</summary>
        public static void Apply(GameObject root)
        {
            Transform supine = root.transform.Find(SupineName);
            if (supine == null)
                throw new InvalidOperationException($"{root.name} has no {SupineName} pivot.");
            Dictionary<string, Material> materials = LoadOrCreateMaterials();

            var unmatched = new List<string>();
            int slots = 0;
            foreach (Renderer renderer in supine.GetComponentsInChildren<Renderer>(true))
            {
                string model = ModelName(renderer.transform, supine);
                Material[] original = OriginalMaterials(renderer);
                var assigned = new Material[original.Length];
                for (int i = 0; i < original.Length; i++)
                {
                    string slot = original[i] != null ? original[i].name : "";
                    string tissue = Match(model, slot);
                    if (tissue == null)
                    {
                        unmatched.Add($"{model} / {slot}");
                        assigned[i] = original[i];
                        continue;
                    }
                    assigned[i] = materials[tissue];
                    slots++;
                }
                renderer.sharedMaterials = assigned;
            }
            if (unmatched.Count > 0)
                Debug.LogError($"No tissue rule for {unmatched.Count} material slots: {string.Join(", ", unmatched)}. Add rules to AnatomyBodyMaterials.");
            Debug.Log($"Assigned tissue materials to {slots} material slots.");
        }

        // The GLB instance is the child of a layer group (Supine / group / instance).
        private static string ModelName(Transform renderer, Transform supine)
        {
            Transform current = renderer;
            while (current.parent != null && current.parent.parent != supine)
                current = current.parent;
            return current.name.ToLowerInvariant();
        }

        // The materials the importer created, so re-running sees the glTF names, not the tissue names.
        private static Material[] OriginalMaterials(Renderer renderer)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer);
            return source != null ? source.sharedMaterials : renderer.sharedMaterials;
        }

        private static string Match(string model, string slot)
        {
            foreach (Rule rule in Rules)
                if (model.Contains(rule.Model) && slot.IndexOf(rule.Slot, StringComparison.OrdinalIgnoreCase) >= 0)
                    return rule.Tissue;
            return null;
        }

        private static Dictionary<string, Material> LoadOrCreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null)
                throw new InvalidOperationException("URP Lit shader not found.");
            Directory.CreateDirectory(Folder);
            var materials = new Dictionary<string, Material>();
            foreach (Tissue tissue in Tissues)
            {
                string path = $"{Folder}/{tissue.Name}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(lit) { name = tissue.Name };
                    material.SetColor("_BaseColor", tissue.Color);
                    material.SetFloat("_Smoothness", tissue.Smoothness);
                    material.SetFloat("_Metallic", 0f);
                    if (tissue.TwoSided)
                    {
                        material.SetFloat("_Cull", 0f);   // render both faces
                        material.doubleSidedGI = true;
                    }
                    AssetDatabase.CreateAsset(material, path);
                }
                materials[tissue.Name] = material;
            }
            foreach (Rule rule in Rules)
                if (!materials.ContainsKey(rule.Tissue))
                    throw new InvalidOperationException($"Rule for '{rule.Model}/{rule.Slot}' names unknown tissue {rule.Tissue}.");
            return materials;
        }
    }
}
