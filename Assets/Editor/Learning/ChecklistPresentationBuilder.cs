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
using DIYsect.Presentation;

namespace DIYsect.Editor
{
    // Builds only local presentation objects. Existing body and network objects are never replaced.
    internal static class ChecklistPresentationBuilder
    {
        private const string MaterialPath = "Assets/Models/Environment/ChecklistGuidance.mat";
        // The board uses the panel kit's Inter font so it reads as one family with the room code board.
        private const string FontPath = ProfessorPanelKit.FontPath;
        private const int RingSegments = 24;
        private const string BoardTitle = "Dissection steps";
        private static readonly Vector2 BoardSize = new Vector2(0.9f, 0.6f);
        // Card centre above the floor, the same as the room code board, and how far it stands past the table's edge.
        private const float BoardCentreHeight = 1.55f;
        private const float BoardOutFromTable = 0.65f;
        private const float MinViewDistance = 1.1f;

        internal static void Build(Transform parent, Transform root, ChecklistTracker tracker, Bounds table)
        {
            Material material = LoadMaterial();
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidOperationException($"Missing checklist font: {FontPath}");
            DissectionProgress dissection = root.GetComponentInChildren<DissectionProgress>(true);
            SharedBody body = dissection.GetComponent<SharedBody>();
            OrganGrabAuthority grabs = dissection.GetComponent<OrganGrabAuthority>();
            if (body == null || grabs == null)
                throw new InvalidOperationException("The checklist needs the body's SharedBody and OrganGrabAuthority. Run Add Body Networking.");
            ChecklistState first = ChecklistState.TraceIncision;

            // The board: the same card, header and stand as the room code board (Professor Panel Kit), standing on the
            // floor beside the table at real size. Like every kit panel its face is toward local -Z and it stays upright.
            Vector3 head = new Vector3(root.position.x, root.position.y + 1.4f, root.position.z);
            foreach (GameObject sceneRoot in root.gameObject.scene.GetRootGameObjects())
                foreach (Camera camera in sceneRoot.GetComponentsInChildren<Camera>(true))
                    if (camera.CompareTag("MainCamera")) head = new Vector3(camera.transform.position.x, head.y, camera.transform.position.z);
            Vector3 position = new Vector3(table.max.x + BoardOutFromTable, root.position.y + BoardCentreHeight, table.center.z);
            Vector3 horizontal = position - head;
            horizontal.y = 0f;
            if (horizontal.sqrMagnitude < 0.0001f) horizontal = Vector3.right;
            if (horizontal.magnitude < MinViewDistance)
            {
                Vector3 near = head + horizontal.normalized * MinViewDistance;
                position = new Vector3(near.x, position.y, near.z);
                horizontal = position - head;
                horizontal.y = 0f;
            }
            Transform panel = ProfessorPanelKit.BuildCard(parent, "Panel", BoardSize, BoardTitle, ChecklistLabels.ProgressChip(first));
            panel.SetPositionAndRotation(position, Quaternion.LookRotation(horizontal.normalized, Vector3.up));
            ProfessorPanelKit.AddStand(panel, position.y - BoardSize.y / 2f, root.position.y);
            Transform badge = panel.Find("BadgeText");
            TMP_Text stepChip = badge != null ? badge.GetComponent<TMP_Text>() : null;
            if (stepChip == null) throw new InvalidOperationException("The board card has no badge text.");

            // Depths in front of the kit card (its content sits at -0.004). Sorting: kit parts 0 to 4, shapes 5 and 6,
            // lines 7, text 10.
            const float shapeZ = -0.005f, lineZ = -0.006f, textZ = -0.007f;
            // A faint mint edge just outside the card; it brightens for a moment when the dissection is complete.
            GuideShape halo = Shape(panel, "Halo", GuideShapeKind.RoundedRect, BoardSize + new Vector2(0.02f, 0.02f),
                ProfessorPanelKit.CornerRadius + 0.01f, 0f, GuidePalette.MintGlow, -1, new Vector3(0f, 0f, 0.002f), material);

            // Left half: the step to do now. The check mark is drawn at x -0.12 in this space, just left of the headline.
            Transform card = Child(panel, "Now");
            card.localPosition = new Vector3(-0.29f, 0.115f, 0f);
            TMP_Text nowLabel = Label(card, "Now label", font, new Vector3(0.05f, 0.065f, textZ), new Vector2(0.3f, 0.02f), 0.011f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0f,
                GuidePalette.TextMuted, 12f);
            nowLabel.text = "NOW";
            LineRenderer checkMark = Line(card, "Check mark", material, 3, false);
            checkMark.sortingOrder = 7;
            TMP_Text headline = Label(card, "Headline", font, new Vector3(0.115f, 0f, textZ),
                new Vector2(0.43f, 0.06f), 0.032f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis, 0.022f, GuidePalette.TextPrimary, 0f);
            headline.text = ChecklistLabels.Headline(first);
            TMP_Text outgoing = Label(card, "Outgoing headline", font, new Vector3(0.115f, 0f, textZ),
                new Vector2(0.43f, 0.06f), 0.032f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis, 0.02f, GuidePalette.TextPrimary, 0f);
            outgoing.alpha = 0f;
            TMP_Text hint = Label(card, "Hint", font, new Vector3(0.115f, -0.075f, textZ),
                new Vector2(0.43f, 0.07f), 0.016f, FontStyles.Normal, TextAlignmentOptions.TopLeft,
                TextWrappingModes.Normal, TextOverflowModes.Overflow, 0.012f, GuidePalette.TextMuted, 0f);
            hint.text = ChecklistLabels.Hint(first);

            // Icon tile with a line drawing of the tool, and the tool's name beside it.
            const float iconX = -0.055f, iconY = -0.17f;
            GuideShape tileRim = Shape(card, "Icon tile rim", GuideShapeKind.RoundedRect, new Vector2(0.09f, 0.09f), 0.02f, 0f,
                GuidePalette.CoralSoft, 5, new Vector3(iconX, iconY, shapeZ), material);
            Shape(card, "Icon tile", GuideShapeKind.RoundedRect, new Vector2(0.084f, 0.084f), 0.018f, 0f,
                Color.Lerp(GuidePalette.Ink, GuidePalette.Slate, 0.35f), 6, new Vector3(iconX, iconY, shapeZ - 0.0005f), material);
            Transform iconRoot = Child(card, "Icon");
            iconRoot.localPosition = new Vector3(iconX, iconY, lineZ);
            var iconStrokes = new LineRenderer[ChecklistIconShapes.MaxStrokes];
            for (int i = 0; i < iconStrokes.Length; i++)
            {
                iconStrokes[i] = Line(iconRoot, $"Stroke {i + 1}", material, 2, false);
                iconStrokes[i].sortingOrder = 7;
            }
            // The tool chip is one group so the presenter can hide it with a single SetActive.
            Transform toolChip = Child(card, "Tool chip");
            toolChip.localPosition = new Vector3(0.005f, iconY, 0f);
            TMP_Text toolLabel = Label(toolChip, "Tool label", font, new Vector3(0.075f, 0.017f, textZ), new Vector2(0.15f, 0.02f),
                0.0095f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0f,
                GuidePalette.TextMuted, 12f);
            toolLabel.text = "TOOL";
            TMP_Text toolChipText = Label(toolChip, "Tool chip text", font, new Vector3(0.15f, -0.012f, textZ),
                new Vector2(0.3f, 0.035f), 0.018f, FontStyles.Bold | FontStyles.UpperCase, TextAlignmentOptions.MidlineLeft,
                TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0f, GuidePalette.Coral, 4f);
            toolChipText.text = ChecklistLabels.ToolName(first);
            toolChip.gameObject.SetActive(ChecklistLabels.ToolName(first).Length > 0);

            // Progress: seven short bars along the bottom of the left half, level with the last step of the list.
            var segments = new GuideShape[ChecklistLabels.StepCount];
            for (int i = 0; i < segments.Length; i++)
                segments[i] = Shape(card, $"Segment {i + 1}", GuideShapeKind.RoundedRect, new Vector2(0.05f, 0.008f), 0.004f, 0f,
                    i == 0 ? GuidePalette.Coral : GuidePalette.Slate, 5, new Vector3(-0.075f + i * 0.057f, -0.31f, shapeZ), material);

            Shape(panel, "Divider", GuideShapeKind.RoundedRect, new Vector2(0.0015f, 0.4f), 0.00075f, 0f,
                GuidePalette.MintLine, 5, new Vector3(0.048f, -0.07f, shapeZ), material);

            // Right half: every step. Rows sit at RowY, which the presenter's highlight slide also uses.
            Transform list = Child(panel, "Progress");
            list.localPosition = new Vector3(0.245f, -0.02f, 0f);
            TMP_Text title = Label(list, "Title", font, new Vector3(-0.067f, 0.2f, textZ), new Vector2(0.2f, 0.02f), 0.011f,
                FontStyles.Bold, TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0f,
                GuidePalette.TextMuted, 12f);
            title.text = "ALL STEPS";
            TMP_Text count = Label(list, "Count", font, new Vector3(0.13f, 0.2f, textZ),
                new Vector2(0.1f, 0.02f), 0.013f, FontStyles.Bold, TextAlignmentOptions.MidlineRight,
                TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0f, GuidePalette.TextMuted, 0f);
            count.text = ChecklistLabels.Count(first);
            GuideShape highlight = Shape(list, "Highlight", GuideShapeKind.RoundedRect, new Vector2(0.368f, 0.042f), 0.012f, 0f,
                GuidePalette.CoralSoft, 5, new Vector3(0.005f, RowY(0), shapeZ), material);

            var rows = new TMP_Text[ChecklistLabels.StepCount];
            var nodes = new GuideShape[ChecklistLabels.StepCount];
            var ticks = new LineRenderer[ChecklistLabels.StepCount];
            var connectors = new GuideShape[ChecklistLabels.StepCount - 1];
            const float nodeX = -0.149f;
            for (int i = 0; i < rows.Length; i++)
            {
                float y = RowY(i);
                bool current = i == 0;
                nodes[i] = Shape(list, $"Node {i + 1}", current ? GuideShapeKind.Disc : GuideShapeKind.Ring,
                    new Vector2(0.026f, 0.026f), 0f, 0.003f, current ? GuidePalette.Coral : GuidePalette.Slate, 6,
                    new Vector3(nodeX, y, shapeZ - 0.0005f), material);
                rows[i] = Label(list, $"Step {i + 1}", font, new Vector3(0.027f, y, textZ),
                    new Vector2(0.3f, 0.04f), 0.015f, current ? FontStyles.Bold : FontStyles.Normal,
                    TextAlignmentOptions.MidlineLeft, TextWrappingModes.NoWrap, TextOverflowModes.Ellipsis, 0.011f,
                    current ? GuidePalette.TextPrimary : GuidePalette.TextMuted, 0f);
                rows[i].text = ChecklistLabels.StepText(i);
                // Tick: three points sized to fit the 0.026 node, drawn in front of it in Ink.
                ticks[i] = Line(list, $"Tick {i + 1}", material, 3, false);
                ticks[i].transform.localPosition = new Vector3(nodeX, y, lineZ);
                ticks[i].SetPositions(new[]
                {
                    new Vector3(-0.0065f, 0f, 0f), new Vector3(-0.002f, -0.0055f, 0f), new Vector3(0.0065f, 0.0055f, 0f),
                });
                ticks[i].sortingOrder = 7;
                var inkBlock = new MaterialPropertyBlock();
                inkBlock.SetColor("_BaseColor", GuidePalette.Ink);
                ticks[i].SetPropertyBlock(inkBlock);
                ticks[i].enabled = false;
                if (i < connectors.Length)
                    connectors[i] = Shape(list, $"Connector {i + 1}", GuideShapeKind.RoundedRect, new Vector2(0.002f, 0.022f),
                        0.001f, 0f, GuidePalette.Slate, 5, new Vector3(nodeX, y - 0.025f, shapeZ), material);
            }
            // Estimate: the kit card and stand (about 300 triangles), 30 shapes (rounded rects near 30 triangles, discs and
            // rings near 24 to 48), 12 lines and 18 texts (about 380 glyphs) is roughly 2,000 triangles.
            ChecklistBodyCues cues = BuildCues(parent, root, tracker, dissection, material);
            var presenter = panel.gameObject.AddComponent<ChecklistPanel>();
            var serialized = new SerializedObject(presenter);
            Reference(serialized, "_tracker", tracker);
            Reference(serialized, "_cues", cues);
            Reference(serialized, "_cardRoot", card);
            Reference(serialized, "_headline", headline);
            Reference(serialized, "_outgoingHeadline", outgoing);
            Reference(serialized, "_hint", hint);
            Reference(serialized, "_stepChip", stepChip);
            Reference(serialized, "_toolChip", toolChip.gameObject);
            Reference(serialized, "_toolChipText", toolChipText);
            Reference(serialized, "_tileRim", tileRim);
            Reference(serialized, "_iconRoot", iconRoot);
            References(serialized, "_iconStrokes", iconStrokes);
            Reference(serialized, "_checkMark", checkMark);
            Reference(serialized, "_grabs", grabs);
            Reference(serialized, "_halo", halo);
            References(serialized, "_segments", segments);
            Reference(serialized, "_count", count);
            Reference(serialized, "_highlight", highlight);
            References(serialized, "_rows", rows);
            References(serialized, "_nodes", nodes);
            References(serialized, "_ticks", ticks);
            References(serialized, "_connectors", connectors);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            BuildHint(parent, body, grabs, material, font);
        }

        // The "Turn to examine" pill. Hidden until the first time this device holds an organ.
        private static void BuildHint(Transform parent, SharedBody body, OrganGrabAuthority grabs, Material material, TMP_FontAsset font)
        {
            Transform container = Child(parent, "Contextual hint");
            Transform root = Child(container, "Pill");
            GuideShape pill = Shape(root, "Fill", GuideShapeKind.RoundedRect, new Vector2(0.2f, 0.042f), 0.021f, 0f,
                GuidePalette.Ink, 2, Vector3.zero, material);
            LineRenderer glyph = Line(root, "Turn glyph", material, ContextualHintGlyph.PointCount(12), false);
            glyph.transform.localPosition = new Vector3(-0.076f, 0f, -0.002f);
            glyph.SetPositions(ContextualHintGlyph.Points(0.0085f, 12));
            glyph.widthMultiplier = 0.002f;
            glyph.sortingOrder = 5;
            glyph.enabled = true;
            TMP_Text text = Label(root, "Hint text", font, new Vector3(0.021f, 0f, -0.003f),
                new Vector2(0.14f, 0.034f), 0.011f, FontStyles.Bold, TextAlignmentOptions.MidlineLeft,
                TextWrappingModes.NoWrap, TextOverflowModes.Overflow, 0.008f, GuidePalette.TextPrimary, 0f);
            text.text = "Turn to examine";
            root.gameObject.SetActive(false);
            var hint = container.gameObject.AddComponent<ContextualHint>();
            var serialized = new SerializedObject(hint);
            Reference(serialized, "_grabs", grabs);
            Reference(serialized, "_body", body);
            Reference(serialized, "_root", root.gameObject);
            Reference(serialized, "_pill", pill);
            Reference(serialized, "_text", text);
            Reference(serialized, "_glyph", glyph);
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

        // Centre of progress row i, in the Progress group's local space.
        private static float RowY(int i) => 0.125f - i * 0.05f;

        private static GuideShape Shape(Transform parent, string name, GuideShapeKind kind, Vector2 size, float radius,
            float thickness, Color color, int order, Vector3 localPosition, Material material)
        {
            Transform child = Child(parent, name);
            child.localPosition = localPosition;
            var shape = child.gameObject.AddComponent<GuideShape>();
            var serialized = new SerializedObject(shape);
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            serialized.FindProperty("_size").vector2Value = size;
            serialized.FindProperty("_radius").floatValue = radius;
            serialized.FindProperty("_thickness").floatValue = thickness;
            serialized.FindProperty("_segments").intValue = kind == GuideShapeKind.RoundedRect ? 6 : 24;
            serialized.FindProperty("_color").colorValue = color;
            serialized.FindProperty("_sortingOrder").intValue = order;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var renderer = child.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return shape;
        }

        // Text sizes are given as cap heights in metres. autoSizeMin (a cap height) above zero turns on auto size
        // between that and the main cap height.
        private static TMP_Text Label(Transform parent, string name, TMP_FontAsset font, Vector3 localPosition,
            Vector2 rect, float capHeight, FontStyles style, TextAlignmentOptions alignment, TextWrappingModes wrapping,
            TextOverflowModes overflow, float autoSizeMin, Color color, float characterSpacing)
        {
            Transform child = Child(parent, name);
            child.localPosition = localPosition;
            var text = child.gameObject.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSharedMaterial = font.material;
            text.rectTransform.sizeDelta = rect;
            text.isOrthographic = false;
            text.fontSize = CapToFontSize(font, capHeight);
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.characterSpacing = characterSpacing;
            text.textWrappingMode = wrapping;
            text.overflowMode = overflow;
            if (autoSizeMin > 0f)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = CapToFontSize(font, autoSizeMin);
                text.fontSizeMax = CapToFontSize(font, capHeight);
            }
            var meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = 10;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return text;
        }

        private static float CapToFontSize(TMP_FontAsset font, float capHeight)
        {
            return capHeight * font.faceInfo.pointSize / (font.faceInfo.capLine * font.faceInfo.scale * 0.1f);
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
