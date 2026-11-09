// Run Tools > DIYsect > Capture Submission Media, or in batch (needs graphics, not -nographics):
//   Unity -batchmode -projectPath . -executeMethod DIYsect.Editor.Media.CaptureSubmissionMedia.RunBatch
// Writes docs/media/*.png and the frames of the two animations to $DIYSECT_MEDIA_FRAMES (default Temp/media-frames).
// A Python script then turns the frames into GIFs. Nothing here saves a scene.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DIYsect.Classroom;
using DIYsect.Dissection;
using DIYsect.Interaction;
using DIYsect.Networking;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DIYsect.Editor.Media
{
    /// <summary>
    /// Plays the shared scene (Professor_MR) as a classmate in the editor with a local host, sets up each moment worth showing
    /// (pointer, question pins, guiding hand, incision and open skin flaps, the professor's table) and renders it with
    /// <see cref="MediaCamera"/>. These are editor renders, not device captures.
    /// </summary>
    [InitializeOnLoad]
    public static class CaptureSubmissionMedia
    {
        private const string ScenePath = "Assets/Scenes/Professor_MR.unity";
        private const string OutputFolder = "docs/media";
        private const string ResumeKey = "DIYsect.CaptureSubmissionMedia.Resume";
        private const string FramesEnv = "DIYSECT_MEDIA_FRAMES";

        private static readonly Stack<IEnumerator> _stack = new Stack<IEnumerator>();
        private static MediaCamera _media;
        private static string _frames;

        static CaptureSubmissionMedia()
        {
            if (SessionState.GetBool(ResumeKey, false) && EditorApplication.isPlayingOrWillChangePlaymode)
                EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/DIYsect/Capture Submission Media")]
        public static void Run()
        {
            SessionState.SetBool(ResumeKey, true);
            EditorApplication.EnterPlaymode();
        }

        public static void RunBatch() => Run();

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredPlayMode)
                return;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            SessionState.SetBool(ResumeKey, false);
            _stack.Clear();
            _stack.Push(Script());
            EditorApplication.update += Step;
        }

        // Advances the script one yield per editor update. A yielded IEnumerator runs to its end first.
        private static void Step()
        {
            try
            {
                while (_stack.Count > 0)
                {
                    IEnumerator top = _stack.Peek();
                    if (!top.MoveNext())
                    {
                        _stack.Pop();
                        continue;
                    }
                    switch (top.Current)
                    {
                        case IEnumerator nested: _stack.Push(nested); continue;
                        case AsyncOperation op when !op.isDone: return;
                        default: return;
                    }
                }
                Finish(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[CaptureSubmissionMedia] {e}");
                Finish(1);
            }
        }

        private static void Finish(int code)
        {
            EditorApplication.update -= Step;
            Debug.Log($"[CaptureSubmissionMedia] Finished with code {code}.");
            EditorApplication.Exit(code);
        }

        private static IEnumerator Wait(float seconds)
        {
            double end = EditorApplication.timeSinceStartup + seconds;
            while (EditorApplication.timeSinceStartup < end)
                yield return null;
        }

        // The scene objects the shots need.
        private sealed class Rig
        {
            public SharedBody Body;
            public DissectionProgress Progress;
            public QuestionBoard Questions;
            public PointerBoard Pointers;
            public PointerPresenter PointerView;
            public ScalpelSteering Steering;
            public GuidingHand Hand;
        }

        private static IEnumerator Script()
        {
            _frames = Environment.GetEnvironmentVariable(FramesEnv) ?? "Temp/media-frames";
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return Wait(2f);

            Rig rig = BuildRig();
            _media = new MediaCamera();
            yield return Wait(1f);

            // 1. The incision, then the skin flaps swinging open (animation), then the deeper layers.
            for (float t = 0.05f; t <= 1.001f; t += 0.05f)
                rig.Progress.EvaluateSample(Vector3.Lerp(rig.Progress.LineStart, rig.Progress.LineEnd, t));
            yield return Wait(0.5f);
            Bounds body = BoundsOf(rig.Body.transform);
            Vector3 incision = rig.Progress.transform.TransformPoint((rig.Progress.LineStart + rig.Progress.LineEnd) * 0.5f);
            Shot(incision, Views.Close);
            Save("incision_line.png", "Editor render | Classmate view | Incision along the midline");

            rig.Progress.TryOpen();
            yield return Animate("flaps", 30, 2.4f, null, "Editor render | Classmate view | Skin flaps opening");
            Save("skin_flaps_open.png", "Editor render | Classmate view | Skin flaps open");
            // The guiding hand's ring and label on the scalpel.
            yield return GuideScalpel(rig);
            rig.Progress.TryStep(DissectionAction.OpenRectus);
            yield return Wait(1.5f);
            rig.Progress.TryStep(DissectionAction.LiftOmentum);
            yield return Wait(3f);

            // 2. The translated pointer: the professor points at the liver.
            int liver = IndexOf(rig.Body, "liver");
            // The ribs and muscle flaps cover the liver from above, so it is lifted clear, as a student would lift it out.
            rig.Body.Organs[liver].transform.position += Vector3.up * 0.12f;
            yield return Wait(0.5f);
            Vector3 liverCentre = BoundsOf(rig.Body.Organs[liver].transform).center;
            rig.PointerView.SetViewCamera(_media.Camera);
            rig.Pointers.Point(liver);
            yield return Wait(1f);
            Shot(liverCentre, Views.Close);
            Save("classmate_pointer_organ.png", "Editor render | Classmate view | Professor's pointer on the liver", rig.PointerView);
            yield return Animate("pointer", 32, 1.6f, rig.PointerView, "Editor render | Classmate view | Pointer pulse");
            // Look away from the liver so only the edge arrow shows.
            Vector3 away = liverCentre + _media.Camera.transform.right * 0.7f;
            Shot(away, Views.Close);
            Save("classmate_pointer_edge_arrow.png", "Editor render | Classmate view | Pointer off screen, edge arrow", rig.PointerView);
            rig.Pointers.StopPointing();
            yield return Wait(0.5f);

            // 3. Question pins: five on the liver (they cluster and glow), one single with the asker's view, one answered.
            // The asker's view stored with a pin is this camera's, from a sensible classmate position.
            Shot(liverCentre, Views.Wide);
            yield return PlacePins(rig, liver);
            _media.Aim(liverCentre + new Vector3(0.1f, 0f, 0f), 10f, 58f, 0.8f);
            yield return Wait(0.3f);
            Save("classmate_question_pins.png", "Editor render | Classmate view | Question pins, glowing cluster and an answered pin");
            yield return OrbitFrames("pins", 30, liverCentre, rig.Body.transform);

            // 4. The professor's side: the body on the table with the headset-only objects switched on.
            ShowProfessorObjects();
            yield return Wait(2f);
            Shot(body.center + new Vector3(-0.45f, -0.35f, 0f), Views.Overview);
            Save("professor_table_overview.png", "Editor render | Professor side | Body on the table");
            StudentViewWindow window = UnityEngine.Object.FindFirstObjectByType<StudentViewWindow>(FindObjectsInactive.Include);
            if (window != null && window.Mode != StudentViewMode.Hidden)
            {
                Shot(window.transform.position, Views.Window);
                Save("professor_student_view.png", "Editor render | Professor side | Student view window");
            }
            else
                Debug.LogWarning("[CaptureSubmissionMedia] The student view window has no question with a view; skipped.");

            _media.Destroy();
            yield break;
        }

        private enum Views { Close, Wide, Tight, Overview, Window }

        private static void Shot(Vector3 target, Views view)
        {
            switch (view)
            {
                case Views.Close: _media.Aim(target, 10f, 50f, 0.7f); break;
                case Views.Wide: _media.Aim(target, 10f, 50f, 1.1f); break;
                case Views.Tight: _media.Aim(target, 10f, 55f, 0.5f); break;
                case Views.Overview: _media.Aim(target, 0f, 25f, 1.7f); break;
                case Views.Window: _media.Aim(target, 0f, 0f, 1.0f); break;
            }
        }

        private static void Save(string file, string tag, PointerPresenter pointers = null)
        {
            string path = $"{OutputFolder}/{file}";
            _media.Save(path, tag, pointers);
            Debug.Log($"[CaptureSubmissionMedia] Wrote {path}.");
        }

        // Writes frames of whatever is moving at the current camera pose, evenly spaced over the duration.
        private static IEnumerator Animate(string name, int count, float seconds, PointerPresenter pointers, string tag)
        {
            string folder = $"{_frames}/{name}";
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            float start = Time.unscaledTime;
            for (int i = 0; i < count; i++)
            {
                while (Time.unscaledTime < start + seconds * i / count)
                    yield return null;
                _media.Save($"{folder}/frame_{i:D3}.png", tag, pointers);
                yield return null;
            }
        }

        private static IEnumerator OrbitFrames(string name, int count, Vector3 target, Transform body)
        {
            string folder = $"{_frames}/{name}";
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
            for (int i = 0; i < count; i++)
            {
                _media.Aim(target, 10f + 360f * i / count, 55f, 0.55f);
                _media.Save($"{folder}/frame_{i:D3}.png", "Editor render | Classmate view | Orbiting the question pins");
                yield return null;
            }
        }

        private static IEnumerator PlacePins(Rig rig, int liver)
        {
            rig.Questions.RequestRejected += (client, reason) => Debug.LogWarning($"[CaptureSubmissionMedia] Pin rejected: {reason}.");
            Transform organ = rig.Body.Organs[liver].transform;
            Vector3 centre = TopPoint(organ);
            // Five open questions on one spot make the cluster glow fully.
            string[] texts = { "Why so big?", "What does it do?", "Is it red?", "Where is it?", "Can it heal?" };
            for (int i = 0; i < texts.Length; i++)
            {
                Vector3 offset = Quaternion.Euler(0f, 72f * i, 0f) * Vector3.right * 0.006f;
                rig.Questions.RequestPin(liver, centre + organ.InverseTransformVector(offset), texts[i]);
            }
            // The board allows five pins per ten seconds for one asker.
            yield return Wait(10.5f);

            // Two single questions: one on the descending colon with the asker's view, and one on the ascending colon that the professor answers.
            Camera view = _media.Camera;
            int colon = IndexOf(rig.Body, "descending_colon");
            rig.Questions.RequestPin(colon, TopPoint(rig.Body.Organs[colon].transform), "Where next?", view.transform.position, view.transform.rotation);
            int answered = IndexOf(rig.Body, "ascending_colon");
            rig.Questions.RequestPin(answered, TopPoint(rig.Body.Organs[answered].transform), "Is it safe?");
            yield return Wait(1.5f);
            if (rig.Questions.Count != 7)
                throw new InvalidOperationException($"Expected 7 pins, the board has {rig.Questions.Count}.");
            rig.Questions.RequestMarkAnswered(rig.Questions[6].Id);
            yield return Wait(1f);
        }

        // A point on the organ's upper side, in its local space, where a pin would be tapped from above.
        private static Vector3 TopPoint(Transform organ, Vector3 worldShift = default)
        {
            Bounds bounds = BoundsOf(organ);
            var ray = new Ray(bounds.center + worldShift + Vector3.up * 1f, Vector3.down);
            foreach (Collider collider in organ.GetComponentsInChildren<Collider>())
                if (collider.Raycast(ray, out RaycastHit hit, 3f))
                    return organ.InverseTransformPoint(hit.point);
            return organ.InverseTransformPoint(bounds.center + worldShift + Vector3.up * bounds.extents.y * 0.6f);
        }

        private static IEnumerator GuideScalpel(Rig rig)
        {
            Transform scalpel = rig.Steering.transform;
            // The scalpel stays on its tray; only who steers and who controls the depth is set.
            // Someone else steers and the professor controls the depth: the server's values are set directly, no real clients.
            SetNetworkValue(rig.Steering, "_steerer", 1UL);
            SetNetworkValue(rig.Hand, "_controller", 2UL);
            SetNetworkValue(rig.Hand, "_depth", 0.6f);
            yield return Wait(1f);
            // Close in on the ring, so it and the label read.
            Vector3 ring = scalpel.position;
            foreach (Transform part in scalpel.GetComponentsInChildren<Transform>(true))
                if (part.name == "DepthRing")
                    ring = part.position;
            _media.Aim(ring, 10f, 30f, 0.4f);
            // The label turns toward the camera on the next frame.
            yield return Wait(0.3f);
            Save("guiding_hand_depth.png", "Editor render | Classmate view | Guiding hand: Professor controls the depth");
            // Back to nobody steering, so the later shots show no indicator.
            SetNetworkValue(rig.Hand, "_controller", GuidingHand.NoController);
            SetNetworkValue(rig.Steering, "_steerer", ScalpelSteering.NoSteerer);
                        yield return Wait(0.5f);
        }

        private static void SetNetworkValue<T>(NetworkBehaviour owner, string field, T value) where T : unmanaged
        {
            FieldInfo info = owner.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)
                ?? throw new MissingFieldException(owner.GetType().Name, field);
            ((NetworkVariable<T>)info.GetValue(owner)).Value = value;
        }

        private static void ShowProfessorObjects()
        {
            foreach (string name in new[] { "DepthHandle", "StudentViewWindow", "ClassRoster", "GuidePen" })
            {
                Transform found = FindDeep(name);
                if (found != null)
                    found.gameObject.SetActive(true);
                else
                    Debug.LogWarning($"[CaptureSubmissionMedia] No {name} in the scene.");
            }
        }

        private static Rig BuildRig()
        {
            foreach (Camera other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                other.enabled = false;
            foreach (var orbit in UnityEngine.Object.FindObjectsByType<ClassmateOrbitCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                orbit.enabled = false;
            foreach (var hud in UnityEngine.Object.FindObjectsByType<PointerHud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                hud.enabled = false;

            NetworkManager manager = NetworkManager.Singleton ?? throw new InvalidOperationException("The scene has no NetworkManager.");
            if (!manager.StartHost())
                throw new InvalidOperationException("The local host did not start.");

            var body = UnityEngine.Object.FindFirstObjectByType<SharedBody>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("No SharedBody.");
            return new Rig
            {
                Body = body,
                Progress = body.GetComponent<DissectionProgress>(),
                Questions = body.GetComponent<QuestionBoard>(),
                Pointers = body.GetComponent<PointerBoard>(),
                PointerView = body.GetComponent<PointerPresenter>(),
                Steering = UnityEngine.Object.FindFirstObjectByType<ScalpelSteering>(FindObjectsInactive.Include) ?? throw new InvalidOperationException("No scalpel."),
                Hand = UnityEngine.Object.FindFirstObjectByType<GuidingHand>(FindObjectsInactive.Include),
            };
        }

        private static int IndexOf(SharedBody body, string organName)
        {
            for (int i = 0; i < body.Organs.Count; i++)
                if (body.Organs[i].name.ToLowerInvariant().Contains(organName))
                    return i;
            throw new InvalidOperationException($"The body has no organ named like '{organName}'.");
        }

        private static Bounds BoundsOf(Transform root)
        {
            Bounds bounds = default;
            bool first = true;
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>())
            {
                if (renderer is TrailRenderer || renderer is LineRenderer || renderer.GetComponent<PinMarker>() != null)
                    continue;
                if (first) { bounds = renderer.bounds; first = false; }
                else bounds.Encapsulate(renderer.bounds);
            }
            return bounds;
        }

        private static Transform FindDeep(string name)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
                foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                    if (t.name == name)
                        return t;
            return null;
        }
    }
}
