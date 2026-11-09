// Run Tools > DIYsect > Add Sound Cues after Add Body Networking (and after the quiz is added, so the chime is wired).
using System;
using System.IO;
using DIYsect.Classroom;
using DIYsect.Dissection;
using DIYsect.Learning;
using DIYsect.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor
{
    /// <summary>
    /// Adds a SoundCues child with a SoundCuePlayer under the body in each scene that needs it (LectureHall_VR,
    /// Professor_MR, Classmate_Desktop) and points it at the body's DissectionProgress, OrganGrabAuthority and
    /// QuizRound. Safe to run more than once: an existing player is reused and only its references are refreshed.
    /// </summary>
    public static class AddSoundCues
    {
        private static readonly string[] ScenePaths =
        {
            "Assets/Scenes/LectureHall_VR.unity",
            "Assets/Scenes/Professor_MR.unity",
            "Assets/Scenes/Classmate_Desktop.unity",
        };

        private const string ChildName = "SoundCues";

        [MenuItem("Tools/DIYsect/Add Sound Cues")]
        public static void Apply()
        {
            foreach (string path in ScenePaths)
                ApplyToScene(path);
        }

        // Batch entry point: -executeMethod DIYsect.Editor.AddSoundCues.ApplyBatch
        public static void ApplyBatch()
        {
            Apply();
        }

        private static void ApplyToScene(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} is missing. Run Tools > DIYsect > Create Scenes first.");
            Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

            SharedBody body = null;
            foreach (SharedBody candidate in UnityEngine.Object.FindObjectsByType<SharedBody>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.scene == scene)
                {
                    body = candidate;
                    break;
                }
            }
            if (body == null)
            {
                Debug.LogWarning($"{path} has no anatomy body, so no sound cues were added. Add the body, then run Tools > DIYsect > Add Sound Cues again.");
                return;
            }

            var progress = body.GetComponentInChildren<DissectionProgress>(true);
            var authority = body.GetComponentInChildren<OrganGrabAuthority>(true);
            if (progress == null || authority == null)
                throw new InvalidOperationException($"{path}: the body needs DissectionProgress and OrganGrabAuthority. Run Add Body Networking and Add Body Dissection first.");
            var quiz = body.GetComponentInChildren<QuizRound>(true);
            if (quiz == null)
                Debug.LogWarning($"{path}: the body has no QuizRound yet, so the correct-answer chime is not wired. Run Add Sound Cues again after the quiz is added.");

            var player = body.GetComponentInChildren<SoundCuePlayer>(true);
            if (player == null)
            {
                var child = new GameObject(ChildName);
                child.transform.SetParent(body.transform, false);
                player = child.AddComponent<SoundCuePlayer>();
            }

            var source = player.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            var serialized = new SerializedObject(player);
            serialized.FindProperty("_progress").objectReferenceValue = progress;
            serialized.FindProperty("_grabAuthority").objectReferenceValue = authority;
            serialized.FindProperty("_quiz").objectReferenceValue = quiz;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new IOException($"Could not save {path}.");
            Debug.Log($"{path}: sound cues added to {body.name}.");
        }
    }
}
