// Run Tools > DIYsect > Create Classroom Props after opening the template project.
using System.IO;
using UnityEditor;
using UnityEngine;

namespace DIYsect.Editor
{
    public static class CreateClassroomProps
    {
        [MenuItem("Tools/DIYsect/Create Classroom Props")]
        public static void Create()
        {
            const string folder = "Assets/Models/Environment";
            Directory.CreateDirectory(folder);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (shader == null)
                throw new System.InvalidOperationException("No supported surface shader is installed.");
            var steel = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/Steel.mat");
            if (steel == null)
            {
                steel = new Material(shader) { color = new Color(0.55f, 0.58f, 0.6f) };
                steel.SetFloat("_Metallic", 0.8f);
                AssetDatabase.CreateAsset(steel, $"{folder}/Steel.mat");
            }
            var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>($"{folder}/CloseUp.renderTexture");
            if (texture == null)
            {
                texture = new RenderTexture(1024, 1024, 16) { name = "CloseUp" };
                AssetDatabase.CreateAsset(texture, $"{folder}/CloseUp.renderTexture");
            }
            var screenMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{folder}/Screen.mat");
            if (screenMaterial == null)
            {
                Shader screenShader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
                if (screenShader == null)
                    throw new System.InvalidOperationException("No supported screen shader is installed.");
                screenMaterial = new Material(screenShader) { mainTexture = texture };
                AssetDatabase.CreateAsset(screenMaterial, $"{folder}/Screen.mat");
            }
            CreateTable(folder, steel);
            SaveCube(folder, "InstrumentTray", new Vector3(0.45f, 0.025f, 0.3f), steel);
            SaveCube(folder, "CloseUpScreen", new Vector3(2f, 1.125f, 0.05f), screenMaterial);
            AssetDatabase.SaveAssets();
            Debug.Log("Classroom props ready. Place them in template scenes and assign CloseUp to an overhead camera's Target Texture. Exclude the screen from that camera's culling mask.");
        }

        private static void CreateTable(string folder, Material material)
        {
            string path = $"{folder}/DissectionTable.prefab";
            if (File.Exists(path)) return;
            var root = new GameObject("DissectionTable");
            AddCube(root.transform, "Top", new Vector3(0, 0.9f, 0), new Vector3(0.8f, 0.08f, 2f), material);
            foreach (float x in new[] { -0.32f, 0.32f })
                foreach (float z in new[] { -0.85f, 0.85f })
                    AddCube(root.transform, "Leg", new Vector3(x, 0.43f, z), new Vector3(0.06f, 0.86f, 0.06f), material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void SaveCube(string folder, string name, Vector3 scale, Material material)
        {
            string path = $"{folder}/{name}.prefab";
            if (File.Exists(path)) return;
            var root = new GameObject(name);
            AddCube(root.transform, "Surface", Vector3.zero, scale, material);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        private static void AddCube(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position;
            cube.transform.localScale = scale;
            cube.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
