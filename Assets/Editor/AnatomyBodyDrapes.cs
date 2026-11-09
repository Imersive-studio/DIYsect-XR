// Run by Tools > DIYsect > Add Body Drapes And Materials (Create Anatomy Body also runs it).
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds the surgical drapes (Tools/blender/make_drapes.py) under the body's Supine pivot, next to the
    /// anatomy groups. The drapes were simulated around the exported anatomy in the same upright frame, so the
    /// Supine pivot lays them on the table with the body and no extra transform is needed. They have no
    /// colliders, so organ picking, pins and tools are unaffected. Safe to run more than once.
    /// </summary>
    public static class AnatomyBodyDrapes
    {
        // The drapes tent up over the half body for no reason and a loose piece floats in the air on the headset, so
        // they are kept in the body but switched off. Set this to true to show them again, then run the step.
        public const bool ShowDrapes = false;

        private const string DrapesModel = "Assets/Models/Environment/Drapes/surgical_drapes.glb";
        private const string GroupName = "Drapes";
        private const string SupineName = "Supine";

        public static void Apply(GameObject root)
        {
            Transform supine = root.transform.Find(SupineName);
            if (supine == null)
                throw new InvalidOperationException($"{root.name} has no {SupineName} pivot.");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(DrapesModel);
            if (model == null)
                throw new FileNotFoundException($"{DrapesModel} is missing or did not import. Run Tools/blender/make_drapes.py.");

            Transform group = supine.Find(GroupName);
            if (group == null)
            {
                group = new GameObject(GroupName).transform;
                group.SetParent(supine, false);
            }
            group.gameObject.SetActive(ShowDrapes);
            string instanceName = Path.GetFileNameWithoutExtension(DrapesModel);
            if (group.Find(instanceName) != null)
                return;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model, group);
            instance.name = instanceName;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(collider);
            Debug.Log($"Added {instanceName} under {SupineName}/{GroupName}.");
        }
    }
}
