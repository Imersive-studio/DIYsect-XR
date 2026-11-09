// Run Tools > DIYsect > Add Classmate Sketch, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.AddClassmateSketch.ApplyBatch
using System;
using System.IO;
using System.Linq;
using DIYsect.Classroom;
using DIYsect.Interaction;
using DIYsect.Networking;
using DIYsect.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Lets a phone classmate sketch on the body. In the body prefab: a ClassmateSketch child (its own GuideLine, a violet core
    /// line with a soft glow line behind it, and a SketchPresenter that smooths, fades and labels it). The professor's GuideLine stays on the body root,
    /// so anything that asks the body for its GuideLine still gets the professor's. In Professor_MR: a ClassmateSketchInput on the
    /// classmate camera (with the phone's fingertip cursor), listed as a classmate-only script in the ClassroomRole and saved switched off. It has no buttons of its own:
    /// a Draw button calls SetDrawing and a Clear button calls Clear. Safe to run more than once.
    /// </summary>
    public static class AddClassmateSketch
    {
        public const string ChildName = "ClassmateSketch";
        private const string PrefabPath = "Assets/Prefabs/AnatomyBody.prefab";
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string LineMaterialPath = "Assets/Prefabs/ClassmateSketch.mat";
        // Thicker than the professor's 4 mm line, so it reads on a phone screen.
        private const float LineWidth = 0.008f;
        private const float GlowWidth = 0.026f;
        private const string GlowMaterialPath = "Assets/Prefabs/ClassmateSketchGlow.mat";
        private const string CursorMaterialPath = "Assets/Prefabs/ClassmateSketchCursor.mat";
        private const string SoftDiscPath = "Assets/Materials/ProfessorPanels/Textures/SoftDisc.png";
        private const string CursorName = "SketchCursor";
        private const float CursorSize = 0.03f;

        [MenuItem("Tools/DIYsect/Add Classmate Sketch")]
        public static void Apply()
        {
            ApplyToPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ApplyToScene();
        }

        public static void ApplyToPrefab()
        {
            if (!File.Exists(PrefabPath))
                throw new FileNotFoundException($"{PrefabPath} is missing. Run Tools > DIYsect > Create Anatomy Body first.");
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                if (root.GetComponent<GuideLine>() == null)
                    throw new InvalidOperationException("The body has no guide line yet. Run Tools > DIYsect > Add Guide Line first.");
                Transform child = root.transform.Find(ChildName);
                if (child == null)
                {
                    child = new GameObject(ChildName).transform;
                    child.SetParent(root.transform, false);
                }
                child.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                child.localScale = Vector3.one;
                // A NetworkBehaviour on a child rides the body's NetworkObject; there is no second NetworkObject.
                var line = AnatomyBodyDissection.GetOrAdd<GuideLine>(child.gameObject);
                // The bright core and, behind it, a wider soft glow; both take their colour (and the fade) from the line's vertex colour.
                var renderer = AnatomyBodyDissection.GetOrAdd<LineRenderer>(child.gameObject);
                StyleLine(renderer, LineWidth, InkMaterial(LineMaterialPath), new Color(GuidePalette.Violet.r, GuidePalette.Violet.g, GuidePalette.Violet.b, 1f), 1);
                Transform glowObject = child.Find("Glow");
                if (glowObject == null)
                {
                    glowObject = new GameObject("Glow").transform;
                    glowObject.SetParent(child, false);
                }
                glowObject.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                var glow = AnatomyBodyDissection.GetOrAdd<LineRenderer>(glowObject.gameObject);
                StyleLine(glow, GlowWidth, InkMaterial(GlowMaterialPath), new Color(GuidePalette.Violet.r, GuidePalette.Violet.g, GuidePalette.Violet.b, 0.28f), 0);

                var old = child.GetComponent<GuideLinePresenter>();
                if (old != null)
                    UnityEngine.Object.DestroyImmediate(old, true);
                var presenter = AnatomyBodyDissection.GetOrAdd<SketchPresenter>(child.gameObject);
                var serialized = new SerializedObject(presenter);
                serialized.FindProperty("_line").objectReferenceValue = line;
                serialized.FindProperty("_glow").objectReferenceValue = glow;
                serialized.FindProperty("_font").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(ApplyLabelDesign.FontPath);
                serialized.FindProperty("_chipText").stringValue = "CLASSMATE";
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log($"Added the classmate sketch line to {PrefabPath}.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        /// <summary>Puts the sketch input on the classmate camera and wires it to the scene body's sketch line. Does not save the scene.</summary>
        public static ClassmateSketchInput AddToRig(GameObject rig, SharedBody body)
        {
            Transform child = body.transform.Find(ChildName)
                ?? throw new InvalidOperationException($"The body has no {ChildName} child. Run Tools > DIYsect > Add Classmate Sketch.");
            var line = child.GetComponent<GuideLine>()
                ?? throw new InvalidOperationException($"{ChildName} has no GuideLine. Run Tools > DIYsect > Add Classmate Sketch.");
            var tracker = rig.GetComponent<PointerGestureTracker>()
                ?? throw new InvalidOperationException("The classmate camera has no PointerGestureTracker. Run Tools > DIYsect > Add Classmate Desktop Camera.");

            var input = rig.GetComponent<ClassmateSketchInput>() ?? rig.AddComponent<ClassmateSketchInput>();
            var serialized = new SerializedObject(input);
            serialized.FindProperty("_body").objectReferenceValue = body;
            serialized.FindProperty("_line").objectReferenceValue = line;
            serialized.FindProperty("_gestures").objectReferenceValue = tracker;
            serialized.FindProperty("_camera").objectReferenceValue = rig.GetComponent<Camera>();
            serialized.FindProperty("_presenter").objectReferenceValue = child.GetComponent<SketchPresenter>();
            serialized.FindProperty("_cursor").objectReferenceValue = EnsureCursor(rig);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Only a classmate sketches. The role switch turns it on; drawing itself stays off until a button asks.
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return input;
        }

        private static void StyleLine(LineRenderer renderer, float width, Material material, Color colour, int order)
        {
            renderer.useWorldSpace = false;
            renderer.widthMultiplier = width;
            renderer.numCapVertices = 6;
            renderer.numCornerVertices = 4;
            renderer.alignment = LineAlignment.View;
            renderer.textureMode = LineTextureMode.Stretch;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sharedMaterial = material;
            renderer.startColor = renderer.endColor = colour;
            renderer.sortingOrder = order;
            renderer.positionCount = 0;
            renderer.enabled = false;
        }

        // A transparent unlit material that uses the line's vertex colour, so the presenter can fade it without new materials.
        private static Material InkMaterial(string path, Texture2D texture = null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? throw new InvalidOperationException("The URP Particles/Unlit shader is missing.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = material == null || material.shader != shader;
            if (created)
            {
                if (material != null)
                    AssetDatabase.DeleteAsset(path);
                material = new Material(shader) { name = Path.GetFileNameWithoutExtension(path) };
            }
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_ZWrite", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetColor("_BaseColor", Color.white);
            if (texture != null)
                material.SetTexture("_BaseMap", texture);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (created)
                AssetDatabase.CreateAsset(material, path);
            EditorUtility.SetDirty(material);
            return material;
        }

        // The phone's fingertip disc: a small soft violet quad the sketch input places on the body under the finger.
        private static Transform EnsureCursor(GameObject rig)
        {
            Transform cursor = rig.transform.Find(CursorName);
            if (cursor == null)
            {
                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = CursorName;
                UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
                quad.transform.SetParent(rig.transform, false);
                cursor = quad.transform;
            }
            cursor.localScale = Vector3.one * CursorSize;
            var renderer = cursor.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = InkMaterial(CursorMaterialPath, AssetDatabase.LoadAssetAtPath<Texture2D>(SoftDiscPath));
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Quads take vertex colour from the mesh (white); the material carries the violet.
            renderer.sharedMaterial.SetColor("_BaseColor", new Color(GuidePalette.Violet.r, GuidePalette.Violet.g, GuidePalette.Violet.b, 0.85f));
            cursor.gameObject.SetActive(false);
            return cursor;
        }

        /// <summary>
        /// Called by Build Shared Scene when it rewrites the classmate script list, so the sketch input is not lost.
        /// Returns <paramref name="behaviours"/> plus the input on <paramref name="rig"/> when it exists, switched off until the role enables it.
        /// </summary>
        public static Behaviour[] WithClassmateInput(Behaviour[] behaviours, GameObject rig)
        {
            if (rig == null || !rig.TryGetComponent(out ClassmateSketchInput input))
                return behaviours;
            input.enabled = false;
            EditorUtility.SetDirty(input);
            return behaviours.Concat(new Behaviour[] { input }).Distinct().ToArray();
        }

        private static void ApplyToScene()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var orbit = UnityEngine.Object.FindFirstObjectByType<ClassmateOrbitCamera>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no classmate camera. Run Tools > DIYsect > Build Shared Scene first.");
            var board = UnityEngine.Object.FindFirstObjectByType<QuestionBoard>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no body with a QuestionBoard.");
            var body = board.GetComponent<SharedBody>()
                ?? throw new InvalidOperationException("The scene body has no SharedBody.");
            ClassmateSketchInput input = AddToRig(orbit.gameObject, body);

            var role = UnityEngine.Object.FindFirstObjectByType<ClassroomRole>(FindObjectsInactive.Include)
                ?? throw new InvalidOperationException($"{ScenePath} has no ClassroomRole. Run Tools > DIYsect > Build Shared Scene first.");
            var roleSerialized = new SerializedObject(role);
            SerializedProperty list = roleSerialized.FindProperty("_classmateBehaviours");
            bool listed = false;
            for (int i = 0; i < list.arraySize; i++)
                listed |= list.GetArrayElementAtIndex(i).objectReferenceValue == input;
            if (!listed)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = input;
            }
            roleSerialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"{ScenePath}: classmate sketch input on {orbit.name}, a classmate-only script.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddClassmateSketch.ApplyBatch
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
