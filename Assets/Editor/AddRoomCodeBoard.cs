// Run Tools > DIYsect > Add Room Code Board after Build Shared Scene.
using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using DIYsect.Presentation;
using XRMultiplayer;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds a board to the shared classroom scene that shows the room code and how a phone joins with it. It stands to the
    /// left of the table, away from the organ screen and the checklist panels on the right, and is a child of the DIYsect
    /// root so table alignment carries it along. Every role sees it. Safe to run more than once: the old board is rebuilt. It is built with ProfessorPanelKit and stands on the floor.
    /// </summary>
    public static class AddRoomCodeBoard
    {
        public const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        public const string BoardName = "RoomCodeBoard";
        private const string RootName = "DIYsect";
        // From the table centre: to the left of where the student stands, a little further than the table, at eye height.
        public static readonly Vector3 OffsetFromTable = new Vector3(-1.35f, 0f, 0.55f);
        public const float CentreHeight = 1.55f;
        private static readonly Vector2 CardSize = new Vector2(0.9f, 0.6f);

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

            // The card is the board: its visible face is toward -Z, so forward points away from the viewer.
            Transform board = ProfessorPanelKit.BuildCard(root, BoardName, CardSize, RoomCodeBoardText.Title, "ROOM");
            Vector3 position = new Vector3(table.center.x, CentreHeight, table.center.z) + OffsetFromTable;
            Vector3 viewer = new Vector3(0f, CentreHeight, 0f);
            board.SetPositionAndRotation(position, Quaternion.LookRotation(position - viewer, Vector3.up));

            Transform content = board.Find("Content");
            Vector2 area = ProfessorPanelKit.ContentSize(CardSize);
            TMP_Text code = ProfessorPanelKit.AddText(content, "Code", RoomCodeBoardText.NoCode, 0.15f, GuidePalette.Mint, TextAlignmentOptions.Center,
                new Vector2(0.04f, -0.03f), new Vector2(area.x - 0.08f, 0.24f), FontStyles.Bold, 30f);
            code.textWrappingMode = TextWrappingModes.NoWrap;
            TMP_Text hint = ProfessorPanelKit.AddText(content, "Hint", RoomCodeBoardText.WaitingHint, 0.026f, GuidePalette.TextMuted, TextAlignmentOptions.Top,
                new Vector2(0.06f, -0.29f), new Vector2(area.x - 0.12f, area.y - 0.31f));

            var presenter = board.gameObject.AddComponent<RoomCodeBoard>();
            var serialized = new SerializedObject(presenter);
            serialized.FindProperty("_code").objectReferenceValue = code;
            serialized.FindProperty("_hint").objectReferenceValue = hint;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ProfessorPanelKit.AddStand(board, position.y - CardSize.y / 2f, root.position.y);

            foreach (Transform t in board.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = 0;

            ArrangeProfessorPanels.ArrangeIn(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {ScenePath}.");
            Debug.Log($"[AddRoomCodeBoard] {ScenePath}: room code board at {position}.");
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
