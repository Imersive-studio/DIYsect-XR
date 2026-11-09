using System;
using System.Collections.Generic;
using DIYsect.Dissection;
using DIYsect.Interaction;
using DIYsect.Learning;
using DIYsect.Networking;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Editor
{
    // Builds only local presentation objects. Existing body and network objects are never replaced.
    internal static class ChecklistPresentationBuilder
    {
        private const string MaterialPath = "Assets/Models/Environment/ChecklistGuidance.mat";
        private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        private const int RingSegments = 24;

        internal static void Build(Transform parent, Transform root, ChecklistTracker tracker, Bounds table)
        {
            Material material = LoadMaterial();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException($"Missing checklist font: {FontPath}");
            DissectionProgress dissection = root.GetComponentInChildren<DissectionProgress>(true);
            var panel = Child(parent, "Panel");
            Vector3 head = new Vector3(root.position.x, 1.4f, root.position.z);
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
                foreach (Camera camera in sceneRoot.GetComponentsInChildren<Camera>(true))
                    if (camera.CompareTag("MainCamera")) head = new Vector3(camera.transform.position.x, 1.4f, camera.transform.position.z);
            Vector3 position = new Vector3(table.max.x + 0.65f, head.y, table.center.z);
            Vector3 horizontal = position - head;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.right;
            if (horizontal.magnitude < 1.1f) position = head + horizontal.normalized * 1.1f;
            panel.SetPositionAndRotation(position, Quaternion.LookRotation(position - head, Vector3.up));
            // Set the initial angular size from the actual rig position, then leave the panel anchored in the room.
            panel.localScale = Vector3.one * Mathf.Max(1f, Vector3.Distance(position, head) / 1.3f);
            var card = Child(panel, "Next step");
            card.localPosition = new Vector3(0f, 0.25f, 0f);
            Backing(card, material, new Vector2(0.68f, 0.21f));
            TMP_Text cardText = Text(card, "Instruction", font, new Vector2(0.61f, 0.17f), 0.036f);
            cardText.text = ChecklistLabels.Instruction(ChecklistState.TraceIncision);
            cardText.fontStyle = FontStyles.Bold;
            TMP_Text outgoing = Text(card, "Previous instruction", font, new Vector2(0.61f, 0.17f), 0.036f);
            outgoing.fontStyle = FontStyles.Bold;
            outgoing.alpha = 0f;
            var list = Child(panel, "Progress");
            list.localPosition = new Vector3(0f, -0.1f, 0f);
            Backing(list, material, new Vector2(0.68f, 0.43f));
            TMP_Text title = Text(list, "Title", font, new Vector2(0.6f, 0.045f), 0.034f);
            title.transform.localPosition = new Vector3(0.015f, 0.17f, -0.008f);
            title.text = ChecklistLabels.Title;
            var rows = new TMP_Text[ChecklistLabels.StepCount];
            var ticks = new LineRenderer[ChecklistLabels.StepCount];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = Text(list, $"Step {i + 1}", font, new Vector2(0.56f, 0.044f), 0.023f);
                rows[i].transform.localPosition = new Vector3(0.03f, 0.115f - i * 0.044f, -0.008f);
                rows[i].text = ChecklistLabels.StepText(i);
                ticks[i] = Line(list, $"Tick {i + 1}", material, 3, false);
                ticks[i].transform.localPosition = new Vector3(-0.29f, 0.115f - i * 0.044f, -0.009f);
                ticks[i].enabled = false;
            }
            ChecklistBodyCues cues = BuildCues(parent, root, tracker, dissection, material);
            var presenter = panel.gameObject.AddComponent<ChecklistPanel>();
            var serialized = new SerializedObject(presenter);
            Reference(serialized, "_tracker", tracker);
            Reference(serialized, "_card", cardText);
            Reference(serialized, "_outgoingCard", outgoing);
            Reference(serialized, "_cardRoot", card);
            Reference(serialized, "_cues", cues);
            References(serialized, "_rows", rows);
            References(serialized, "_ticks", ticks);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static ChecklistBodyCues BuildCues(Transform parent, Transform root, ChecklistTracker tracker,
            DissectionProgress dissection, Material material)
        {
            Transform container = Child(parent, "Body cues");
            var cues = new List<ChecklistBodyCues.Cue>();
            IncisionLinePresenter incision = dissection.GetComponentInChildren<IncisionLinePresenter>(true);
            if (incision == null) throw new InvalidOperationException("No incision path. Run Add Body Dissection first.");
            SerializedProperty sourcePath = new SerializedObject(incision).FindProperty("_path");
            var path = new Vector3[sourcePath.arraySize];
            if (path.Length < 2) throw new InvalidOperationException("The incision path needs at least two points.");
            for (int i = 0; i < path.Length; i++) path[i] = sourcePath.GetArrayElementAtIndex(i).vector3Value + Vector3.up * 0.003f;
            AddCue(cues, container, material, ChecklistState.TraceIncision, incision.transform, path);
            AddCue(cues, container, material, ChecklistState.TraceIncision, incision.transform,
                Ring(new Bounds(path[0], new Vector3(0.022f, 0f, 0.022f))));
            foreach (string name in new[] { "skin_flap_l", "skin_flap_r" })
            {
                Transform flap = RequireNamed(dissection.transform, name);
                var edge = new Vector3[path.Length];
                for (int i = 0; i < path.Length; i++) edge[i] = flap.InverseTransformPoint(incision.transform.TransformPoint(path[i]));
                AddCue(cues, container, material, ChecklistState.OpenSkin, flap, edge);
            }
            int touchZones = 0;
            foreach (LayerTouchTrigger trigger in dissection.GetComponentsInChildren<LayerTouchTrigger>(true))
            {
                if (trigger.Action != DissectionAction.OpenRectus && trigger.Action != DissectionAction.LiftOmentum) continue;
                var box = trigger.GetComponent<BoxCollider>();
                if (box == null) throw new InvalidOperationException($"{trigger.name} needs a box touch zone.");
                ChecklistState step = trigger.Action == DissectionAction.OpenRectus ? ChecklistState.OpenRectus : ChecklistState.LiftOmentum;
                AddCue(cues, container, material, step, box.transform, Ring(new Bounds(box.center, box.size)));
                touchZones++;
            }
            if (touchZones != 2) throw new InvalidOperationException("Expected one rectus and one omentum touch zone.");
            SharedBody body = dissection.GetComponent<SharedBody>();
            if (body == null || body.Organs.Count == 0 || body.GetComponent<OrganGrabPresenter>() == null)
                throw new InvalidOperationException("The body needs its organs and socket ghost presenter. Run Add Organ Sockets.");
            var organs = new Transform[body.Organs.Count];
            for (int i = 0; i < organs.Length; i++)
            {
                Transform organ = body.Organs[i].transform;
                organs[i] = organ;
                Bounds bounds = LocalBounds(organ);
                Vector3[] ring = Ring(bounds);
                AddCue(cues, container, material, ChecklistState.LiftOrgan, organ, ring, i);
                AddCue(cues, container, material, ChecklistState.InspectOrgan, organ, ring, i);
                // The existing OrganGrabPresenter supplies the full ghost while held. This light socket ring
                // remains useful after release without copying anatomy meshes or touching its verdict feedback.
                Transform home = Child(container, $"Home {i}");
                home.SetPositionAndRotation(organ.position, organ.rotation);
                Vector3 parentScale = container.lossyScale;
                home.localScale = Vector3.Scale(organ.lossyScale,
                    new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z));
                AddCue(cues, container, material, ChecklistState.PutOrganBack, home, ring, i);
            }
            ScalpelTool scalpel = root.GetComponentInChildren<ScalpelTool>(true);
            ForcepsTool forceps = root.GetComponentInChildren<ForcepsTool>(true);
            if (scalpel == null || forceps == null) throw new InvalidOperationException("The checklist needs a scalpel and forceps.");
            LineRenderer arrow = Line(container, "Tool to target", material, 5, true);
            var presenter = container.gameObject.AddComponent<ChecklistBodyCues>();
            var serialized = new SerializedObject(presenter);
            Reference(serialized, "_tracker", tracker);
            Reference(serialized, "_dissection", dissection);
            Reference(serialized, "_grabs", body.GetComponent<OrganGrabAuthority>());
            Reference(serialized, "_scalpelTip", scalpel.Tip);
            Reference(serialized, "_forcepsTip", forceps.Tip);
            Reference(serialized, "_arrow", arrow);
            References(serialized, "_organs", organs);
            SerializedProperty array = serialized.FindProperty("_cues");
            array.arraySize = cues.Count;
            int triangleEstimate = 8 + 28 + 4 + 900;
            for (int i = 0; i < cues.Count; i++)
            {
                ChecklistBodyCues.Cue cue = cues[i];
                SerializedProperty element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("Step").enumValueIndex = (int)cue.Step;
                element.FindPropertyRelative("Target").objectReferenceValue = cue.Target;
                element.FindPropertyRelative("Line").objectReferenceValue = cue.Line;
                element.FindPropertyRelative("OrganIndex").intValue = cue.OrganIndex;
                SerializedProperty points = element.FindPropertyRelative("Points");
                points.arraySize = cue.Points.Length;
                for (int j = 0; j < cue.Points.Length; j++) points.GetArrayElementAtIndex(j).vector3Value = cue.Points[j];
                triangleEstimate += (cue.Points.Length - 1) * 2;
            }
            if (triangleEstimate > 5000) throw new InvalidOperationException($"Checklist cues exceed their triangle allowance: {triangleEstimate}.");
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return presenter;
        }

        private static Bounds LocalBounds(Transform target)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) throw new InvalidOperationException($"{target.name} has no visible geometry.");
            Bounds result = default;
            bool first = true;
            foreach (Renderer renderer in renderers)
            {
                Bounds bounds = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                        new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 point = target.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (first) { result = new Bounds(point, Vector3.zero); first = false; }
                    else result.Encapsulate(point);
                }
            }
            return result;
        }

        private static Vector3[] Ring(Bounds bounds)
        {
            var points = new Vector3[RingSegments + 1];
            for (int i = 0; i <= RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                points[i] = new Vector3(bounds.center.x + (bounds.extents.x + 0.004f) * Mathf.Cos(angle),
                    bounds.max.y + 0.004f, bounds.center.z + (bounds.extents.z + 0.004f) * Mathf.Sin(angle));
            }
            return points;
        }

        private static void AddCue(List<ChecklistBodyCues.Cue> cues, Transform parent, Material material,
            ChecklistState step, Transform target, Vector3[] points, int organ = -1)
        {
            LineRenderer line = Line(parent, $"{step} {target.name}", material, points.Length, true);
            cues.Add(new ChecklistBodyCues.Cue { Step = step, Target = target, Points = points, Line = line, OrganIndex = organ });
        }

        private static Transform RequireNamed(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == name) return child;
            throw new InvalidOperationException($"Missing cue target {name}.");
        }

        private static Transform Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.layer = 0;
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static TMP_Text Text(Transform parent, string name, TMP_FontAsset font, Vector2 size, float height)
        {
            Transform child = Child(parent, name);
            child.localPosition = new Vector3(0f, 0f, -0.008f);
            var text = child.gameObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.rectTransform.sizeDelta = size;
            text.isOrthographic = false;
            text.fontSize = height * font.faceInfo.pointSize / (font.faceInfo.capLine * font.faceInfo.scale * 0.1f);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            text.GetComponent<Renderer>().receiveShadows = false;
            return text;
        }

        private static LineRenderer Line(Transform parent, string name, Material material, int count, bool world)
        {
            var line = Child(parent, name).gameObject.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = world;
            line.positionCount = count;
            line.widthMultiplier = 0.003f;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            var block = new MaterialPropertyBlock();
            block.SetColor("_BaseColor", new Color(0.55f, 1f, 0.85f));
            line.SetPropertyBlock(block);
            return line;
        }

        private static void Backing(Transform parent, Material material, Vector2 size)
        {
            GameObject backing = GameObject.CreatePrimitive(PrimitiveType.Quad);
            backing.name = "Backing";
            backing.transform.SetParent(parent, false);
            backing.transform.localScale = new Vector3(size.x, size.y, 1f);
            UnityEngine.Object.DestroyImmediate(backing.GetComponent<Collider>());
            Renderer renderer = backing.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            // Property blocks are not serialized, so a component restores this tint on load.
            var tint = backing.AddComponent<ChecklistBacking>();
            var serialized = new SerializedObject(tint);
            Reference(serialized, "_renderer", renderer);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material LoadMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("Missing URP Unlit shader.");
            material = new Material(shader) { name = "ChecklistGuidance" };
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void Reference(SerializedObject target, string name, UnityEngine.Object value)
        {
            target.FindProperty(name).objectReferenceValue = value;
        }

        private static void References<T>(SerializedObject target, string name, T[] values) where T : UnityEngine.Object
        {
            SerializedProperty array = target.FindProperty(name);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }
    }
}
