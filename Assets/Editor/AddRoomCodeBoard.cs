// Run Tools > DIYsect > Add Room Code Board after Build Shared Scene.
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds a board to the shared classroom scene that shows the room code and how a phone joins with it. It stands to the
    /// left of the table, away from the organ screen and the checklist panels on the right, and is a child of the DIYsect
    /// root so table alignment carries it along. Every role sees it. Safe to run more than once: the old board is rebuilt.
    /// </summary>
    public static class AddRoomCodeBoard
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        public const string BoardName = "RoomCodeBoard";
        private const string RootName = "DIYsect";
        // From the table centre: to the left of where the student stands, a little further than the table, at eye height.
        public static readonly Vector3 OffsetFromTable = new Vector3(-1.35f, 0f, 0.55f);
        public const float CentreHeight = 1.55f;
        // A 1000 by 640 pixel canvas at 1 mm a pixel: a board 1.15 m wide and 0.7 m tall.
        private static readonly Vector2 Pixels = new Vector2(1150f, 700f);
        private const float MetresPerPixel = 0.001f;

        [MenuItem("Tools/DIYsect/Add Room Code Board")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Transform root = null;
            foreach (GameObject candidate in scene.GetRootGameObjects())
                if (candidate.name == RootName)
                    root = candidate.transform;
            if (root == null)
                throw new InvalidOperationException($"{ScenePath} has no {RootName} root.");

            Bounds table = FindTableTop(root);
            Transform old = root.Find(BoardName);
            if (old != null)
                UnityEngine.Object.DestroyImmediate(old.gameObject);

            var board = new GameObject(BoardName, typeof(RectTransform));
            board.transform.SetParent(root, false);
            Vector3 position = new Vector3(table.center.x, CentreHeight, table.center.z) + OffsetFromTable;
            Vector3 viewer = new Vector3(0f, CentreHeight, 0f);
            // A world space canvas is read from behind its forward axis, so forward points away from the viewer.
            board.transform.SetPositionAndRotation(position, Quaternion.LookRotation(position - viewer, Vector3.up));
            board.transform.localScale = Vector3.one * MetresPerPixel;

            var canvas = board.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            ((RectTransform)board.transform).sizeDelta = Pixels;
            board.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;

            Image backing = Child(board.transform, "Backing", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            backing.color = new Color(0.07f, 0.09f, 0.12f, 0.96f);

            Text(board.transform, "Title", RoomCodeBoardText.Title, 56, new Color(0.62f, 0.85f, 1f), new Vector2(0.05f, 0.83f), new Vector2(0.95f, 0.97f), FontStyles.Bold);
            TMP_Text code = Text(board.transform, "Code", RoomCodeBoardText.NoCode, 150, Color.white, new Vector2(0.03f, 0.42f), new Vector2(0.97f, 0.8f), FontStyles.Bold);
            code.characterSpacing = 8f;
            code.enableWordWrapping = false;
            TMP_Text hint = Text(board.transform, "Hint", RoomCodeBoardText.WaitingHint, 36, new Color(0.85f, 0.88f, 0.92f), new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.4f), FontStyles.Normal);

            var presenter = board.AddComponent<RoomCodeBoard>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_code").objectReferenceValue = code;
            serialized.FindProperty("_hint").objectReferenceValue = hint;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            foreach (Transform t in board.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = 0;

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"[AddRoomCodeBoard] {ScenePath}: room code board at {position}.");
        }

        private static RectTransform Child(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var rect = (RectTransform)child.transform;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return rect;
        }

        private static TMP_Text Text(Transform parent, string name, string text, float size, Color color, Vector2 min, Vector2 max, FontStyles style)
        {
            var label = Child(parent, name, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.fontStyle = style;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = true;
            return label;
        }

        private static Bounds FindTableTop(Transform root)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                if (renderer.name == "Top" && renderer.transform.parent != null && renderer.transform.parent.name.StartsWith("DissectionTable"))
                    return renderer.bounds;
            throw new InvalidOperationException($"{ScenePath} has no DissectionTable/Top under {RootName}.");
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddRoomCodeBoard.ApplyBatch
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
