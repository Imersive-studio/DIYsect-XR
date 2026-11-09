// Run Tools > DIYsect > Add Controller Hands, or: Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.AddControllerHands.ApplyBatch
using System;
using System.IO;
using System.Linq;
using DIYsect.Interaction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

namespace DIYsect.Editor
{
    /// <summary>
    /// Shows a hand on each controller instead of the white controller model, in Professor_MR. For each controller of the
    /// XR rig it switches off the template's controller model and adds a copy of the hand that other people already see on
    /// the avatars (Hand_Left_Networked / Hand_Right_Networked), without its network scripts, holding the controller's
    /// handle. A <see cref="ControllerHand"/> on it bends the fingers from the trigger and grip. Tracked hands (controllers
    /// put down) keep the template's own hand visuals. Safe to run more than once.
    /// </summary>
    public static class AddControllerHands
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string HandPrefabPath = "Assets/XRMP/Prefabs/PlayerPrefabs/Hand_{0}_Networked.prefab";
        // Wrist pose on the right controller, found by rendering the hand around the controller model: the palm sits on the
        // handle with the thumb on top, and a full grip closes the fingers around the handle.
        private static readonly Vector3 GripPosition = new Vector3(0.015f, 0.015f, -0.105f);
        private const float GripPitch = 45f;
        private const string ActionsPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/XRI Default Input Actions.inputactions";
        private const string HandName = "Controller Hand";
        private const string QuestModel = "Quest";
        private const string AndroidXRModel = "AXR";

        private static readonly string[][] FingerJoints =
        {
            new[] { "ThumbProximal", "ThumbDistal" },
            new[] { "IndexProximal", "IndexIntermediate", "IndexDistal" },
            new[] { "MiddleProximal", "MiddleIntermediate", "MiddleDistal" },
            new[] { "RingProximal", "RingIntermediate", "RingDistal" },
            new[] { "LittleProximal", "LittleIntermediate", "LittleDistal" },
        };

        [MenuItem("Tools/DIYsect/Add Controller Hands")]
        public static void Apply()
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                ApplyAndSave();
        }

        public static void ApplyBatch()
        {
            try
            {
                ApplyAndSave();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ApplyAndSave()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyToScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
        }

        public static void ApplyToScene(Scene scene)
        {
            XRInputModalityManager modality = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<XRInputModalityManager>(true))
                .SingleOrDefault() ?? throw new InvalidOperationException($"{scene.path} has no XR rig with an XRInputModalityManager.");
            AddHand(modality.leftController, "Left", 'L');
            AddHand(modality.rightController, "Right", 'R');
        }

        private static void AddHand(GameObject controller, string side, char prefix)
        {
            if (controller == null)
                throw new InvalidOperationException($"The XR rig has no {side.ToLowerInvariant()} controller.");

            Transform visual = controller.transform.Find($"{side} Controller Visual")
                ?? throw new InvalidOperationException($"{controller.name} has no '{side} Controller Visual'.");
            visual.gameObject.SetActive(false);

            Transform existing = controller.transform.Find(HandName);
            if (existing != null)
                UnityEngine.Object.DestroyImmediate(existing.gameObject);

            string sourcePath = string.Format(HandPrefabPath, side);
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath)
                ?? throw new FileNotFoundException($"{sourcePath} is missing.");
            var hand = (GameObject)PrefabUtility.InstantiatePrefab(source, controller.scene);
            PrefabUtility.UnpackPrefabInstance(hand, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            hand.name = HandName;
            hand.transform.SetParent(controller.transform, false);
            // The left hand is the right hand mirrored across the controller's centre.
            float mirror = side == "Left" ? -1f : 1f;
            hand.transform.SetLocalPositionAndRotation(new Vector3(mirror * GripPosition.x, GripPosition.y, GripPosition.z),
                Quaternion.Euler(GripPitch, 0f, -mirror * 90f));
            hand.transform.localScale = Vector3.one;

            // Only the look is wanted: the network scripts (finger sync for the avatar) would look for a network player.
            foreach (MonoBehaviour script in hand.GetComponentsInChildren<MonoBehaviour>(true))
                UnityEngine.Object.DestroyImmediate(script);

            Transform quest = FindChild(hand.transform, QuestModel);
            quest.gameObject.SetActive(true);
            // The model's wrist is the hand's pivot, so the grip pose above places the wrist.
            // The left model names it "L_Wrist - Quest", the right one "R_Wrist".
            Transform wrist = quest.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.StartsWith($"{prefix}_Wrist", StringComparison.Ordinal))
                ?? throw new InvalidOperationException($"{quest.name} has no {prefix}_Wrist.");
            quest.localPosition = -(quest.InverseTransformPoint(wrist.position));
            Transform androidXR = hand.transform.Find(AndroidXRModel);
            if (androidXR != null)
                androidXR.gameObject.SetActive(false);
            foreach (Renderer renderer in hand.GetComponentsInChildren<Renderer>(true))
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }

            var component = hand.AddComponent<ControllerHand>();
            var serialized = new SerializedObject(component);
            serialized.FindProperty("_trigger").objectReferenceValue = FindAction($"XRI {side} Interaction/Activate Value");
            serialized.FindProperty("_grip").objectReferenceValue = FindAction($"XRI {side} Interaction/Select Value");
            SerializedProperty fingers = serialized.FindProperty("_fingers");
            fingers.arraySize = FingerJoints.Length;
            for (int f = 0; f < FingerJoints.Length; f++)
            {
                SerializedProperty joints = fingers.GetArrayElementAtIndex(f).FindPropertyRelative("Joints");
                joints.arraySize = FingerJoints[f].Length;
                for (int j = 0; j < FingerJoints[f].Length; j++)
                    joints.GetArrayElementAtIndex(j).objectReferenceValue = FindChild(quest, $"{prefix}_{FingerJoints[f][j]}");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"{controller.name}: controller model hidden, hand added at {hand.transform.localPosition}.");
        }

        private static InputActionReference FindAction(string path)
        {
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ActionsPath))
                if (asset is InputActionReference reference && reference.action != null &&
                    $"{reference.action.actionMap.name}/{reference.action.name}" == path)
                    return reference;
            throw new InvalidOperationException($"{ActionsPath} has no action {path}.");
        }

        private static Transform FindChild(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name)
                    return child;
            throw new InvalidOperationException($"{root.name} has no child named {name}.");
        }
    }
}
