// Run Tools > DIYsect > Add Body Drapes And Materials after Create Anatomy Body (which also runs it).
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    /// <summary>
    /// Opens Assets/Prefabs/AnatomyBody.prefab once and gives it its realistic look: the surgical drapes,
    /// then wet-tissue materials on every part (the drapes included). Each step has its own class.
    /// </summary>
    public static class AnatomyBodyLook
    {
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";

        [MenuItem("Tools/DIYsect/Add Body Drapes And Materials")]
        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                AnatomyBodyDrapes.Apply(root);
                AnatomyBodyMaterials.Apply(root);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Saved drapes and tissue materials to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
