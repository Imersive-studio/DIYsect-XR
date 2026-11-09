using System;
using System.Collections.Generic;
using System.IO;
using DIYsect.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Editor
{
    /// <summary>
    /// One shared look for the professor's floating panels: a rounded dark card with a mint rim, a header strip, a
    /// content area, buttons and a stand that plants the panel on the floor. Every panel builder uses this, so the set
    /// reads as one family of physical displays. Colours come from <see cref="GuidePalette"/>. The visible face of a
    /// card is toward local -Z, like the other panels. Materials and meshes are shared assets, never per object.
    /// </summary>
    public static class ProfessorPanelKit
    {
        public const string FontPath = "Assets/MRTabletopAssets/Fonts/Inter-Regular SDF.asset";
        public const string MaterialFolder = "Assets/Materials/ProfessorPanels";

        public const float CornerRadius = 0.025f;
        public const float HeaderHeight = 0.075f;
        public const float LineThickness = 0.002f;
        // Content sits this far in front of the backing.
        public const float ContentDepth = -0.004f;

        private const string MeshFolder = MaterialFolder + "/Meshes";
        private const string TextureFolder = MaterialFolder + "/Textures";
        private const int ArcSegments = 8;
        private const int TextOrder = 10;
        private static readonly Vector3 NoScale = Vector3.one;

        /// <summary>The size of the content area of a card of this size: below the header and its accent line.</summary>
        public static Vector2 ContentSize(Vector2 cardSize)
        {
            return new Vector2(cardSize.x, cardSize.y - HeaderHeight - LineThickness);
        }

        /// <summary>
        /// Builds a card as a child of <paramref name="parent"/>, centred on its local origin. The child named
        /// "Content" has its origin at the top left of the area below the header (x right, y up).
        /// </summary>
        public static Transform BuildCard(Transform parent, string name, Vector2 size, string title, string badge = null)
        {
            var card = new GameObject(name).transform;
            card.SetParent(parent, false);
            float halfW = size.x / 2f;
            float halfH = size.y / 2f;

            Part(card, "Backing", RoundedRect(size, CornerRadius, 0f, true, true), Material("Panel_Ink", GuidePalette.Ink, false, true), Vector3.zero, 0);
            Part(card, "Header", RoundedRect(new Vector2(size.x, HeaderHeight), CornerRadius, 0f, true, false), Material("Panel_InkDeep", GuidePalette.InkDeep, false, true),
                new Vector3(0f, halfH - HeaderHeight / 2f, -0.001f), 1);
            Part(card, "Accent", RoundedRect(new Vector2(size.x, LineThickness), 0f, 0f, false, false), Material("Panel_Mint", GuidePalette.Mint, false, false),
                new Vector3(0f, halfH - HeaderHeight - LineThickness / 2f, -0.002f), 2);
            Part(card, "Rim", RoundedRect(size, CornerRadius, LineThickness, true, true), Material("Panel_MintLine", GuidePalette.MintLine, false, true),
                new Vector3(0f, 0f, -0.003f), 4);

            float titleRight = 0.028f;
            if (!string.IsNullOrEmpty(badge))
            {
                string chipText = badge.ToUpperInvariant();
                var chipSize = new Vector2(0.045f + 0.0115f * chipText.Length, 0.036f);
                float chipX = halfW - 0.025f - chipSize.x / 2f;
                float headerY = halfH - HeaderHeight / 2f;
                Part(card, "BadgeChip", RoundedRect(chipSize, chipSize.y / 2f, 0f, true, true), Material("Panel_MintGlow", GuidePalette.MintGlow, false, true),
                    new Vector3(chipX, headerY, -0.002f), 3);
                TextMeshPro chip = AddText(card, "BadgeText", chipText, 0.0105f, GuidePalette.Mint, TextAlignmentOptions.Center,
                    new Vector2(chipX - chipSize.x / 2f, headerY + chipSize.y / 2f), chipSize, FontStyles.Bold, 6f);
                chip.overflowMode = TextOverflowModes.Overflow;
                chip.transform.localPosition += new Vector3(0f, 0f, ContentDepth);
                titleRight = 0.025f + chipSize.x + 0.02f;
            }

            TextMeshPro heading = AddText(card, "Title", (title ?? string.Empty).ToUpperInvariant(), 0.019f, GuidePalette.TextPrimary, TextAlignmentOptions.MidlineLeft,
                new Vector2(-halfW + 0.028f, halfH), new Vector2(size.x - 0.028f - titleRight, HeaderHeight), FontStyles.Bold, 10f);
            heading.textWrappingMode = TextWrappingModes.NoWrap;
            heading.transform.localPosition += new Vector3(0f, 0f, ContentDepth);

            var content = new GameObject("Content").transform;
            content.SetParent(card, false);
            content.localPosition = new Vector3(-halfW, halfH - HeaderHeight - LineThickness, ContentDepth);
            AssetDatabase.SaveAssets();
            return card;
        }

        /// <summary>
        /// A 3D TextMeshPro with the Inter font. <paramref name="heightMetres"/> is the height of a capital letter.
        /// The rect hangs from its top left corner at <paramref name="localPosition"/> and the text faces -Z.
        /// <paramref name="characterSpacing"/> is optional letter spacing.
        /// </summary>
        public static TextMeshPro AddText(Transform parent, string name, string text, float heightMetres, Color color, TextAlignmentOptions align,
            Vector2 localPosition, Vector2 boxSize, FontStyles style = FontStyles.Normal, float characterSpacing = 0f)
        {
            TMP_FontAsset font = LoadFont();
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.layer = parent.gameObject.layer;
            // Adding TextMeshPro swaps the Transform for a RectTransform, so position through the label from here on.
            var label = child.AddComponent<TextMeshPro>();
            label.font = font;
            label.fontSharedMaterial = font.material;
            label.rectTransform.pivot = new Vector2(0f, 1f);
            label.rectTransform.sizeDelta = boxSize;
            label.isOrthographic = false;
            label.fontSize = CapToFontSize(font, heightMetres);
            label.fontStyle = style;
            label.alignment = align;
            label.color = color;
            label.characterSpacing = characterSpacing;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.text = text;
            var meshRenderer = label.GetComponent<MeshRenderer>();
            meshRenderer.sortingOrder = TextOrder;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            // Placed last: setting up the text resets the rect's position. Both are set so the saved scene keeps it.
            label.rectTransform.anchoredPosition = localPosition;
            label.rectTransform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            return label;
        }

        /// <summary>
        /// Restyles an existing 3D button: the kit button material, its old TextMesh label hidden (the component stays so
        /// code that sets its text still works) and a centred TextMeshPro label. Collider, interactable and events are untouched.
        /// Run it again and the same label is reused.
        /// </summary>
        public static void StyleButton(GameObject button, string label)
        {
            Renderer face = null;
            foreach (Renderer candidate in button.GetComponentsInChildren<Renderer>(true))
            {
                if (candidate is MeshRenderer && candidate.GetComponent<TextMesh>() == null && candidate.GetComponent<TMP_Text>() == null)
                {
                    face = candidate;
                    break;
                }
            }
            if (face == null)
                throw new InvalidOperationException($"{button.name} has no mesh to restyle.");
            face.sharedMaterial = Material("Panel_Button", GuidePalette.Slate, false, false);
            face.shadowCastingMode = ShadowCastingMode.Off;
            face.receiveShadows = false;

            foreach (TextMesh old in button.GetComponentsInChildren<TextMesh>(true))
            {
                var oldRenderer = old.GetComponent<MeshRenderer>();
                if (oldRenderer != null)
                    oldRenderer.enabled = false;
            }

            // The button's local box, so the label fits and sits just in front of the -Z face.
            Vector3 scale = face.transform.localScale;
            Bounds local = face.GetComponent<MeshFilter>() != null && face.GetComponent<MeshFilter>().sharedMesh != null
                ? face.GetComponent<MeshFilter>().sharedMesh.bounds
                : new Bounds(Vector3.zero, Vector3.one);
            var box = new Vector2(local.size.x * scale.x, local.size.y * scale.y);

            Transform previous = face.transform.Find("LabelTMP");
            if (previous != null)
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var parent = new GameObject("LabelTMP").transform;
            parent.SetParent(face.transform, false);
            parent.gameObject.layer = button.layer;
            // Undo the button's stretch so the text is not squashed. Position is in the button's local units.
            parent.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
            parent.localPosition = new Vector3(local.center.x, local.center.y, local.min.z - 0.002f / Mathf.Abs(scale.z));
            float cap = Mathf.Min(0.016f, box.y * 0.28f);
            TextMeshPro text = AddText(parent, "Text", label, cap, GuidePalette.TextPrimary, TextAlignmentOptions.Center,
                new Vector2(-box.x / 2f, box.y / 2f), box, FontStyles.Bold, 2f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Adds a slate pole from behind the bottom centre of the card down to the floor, a round foot and a soft dark
        /// contact disc, all children of the card. <paramref name="cardBottomY"/> and <paramref name="floorY"/> are
        /// world heights. Place the card first; the stand is measured from where it is now. Turning the card about the
        /// vertical afterwards (as Arrange Professor Panels does) keeps the stand under it.
        /// </summary>
        public static void AddStand(Transform card, float cardBottomY, float floorY)
        {
            const float poleDiameter = 0.025f;
            const float footDiameter = 0.28f;
            const float footHeight = 0.015f;
            const float discDiameter = 0.4f;
            const float behindCard = 0.03f;

            UnityEngine.Material slate = Material("Stand_Slate", GuidePalette.Slate, true, false);
            // Behind the card, centred under it.
            Vector3 foot = new Vector3(card.position.x, floorY, card.position.z) + card.forward * behindCard;
            foot.y = floorY;
            float topY = cardBottomY + 0.02f;
            float poleHeight = Mathf.Max(0.05f, topY - floorY);

            var stand = new GameObject("Stand").transform;
            stand.SetParent(card, false);
            stand.gameObject.layer = card.gameObject.layer;

            Transform pole = Primitive(stand, PrimitiveType.Cylinder, "Pole", slate);
            pole.position = new Vector3(foot.x, floorY + poleHeight / 2f, foot.z);
            pole.rotation = Quaternion.identity;
            pole.localScale = Vector3.one;
            SetWorldScale(pole, new Vector3(poleDiameter, poleHeight / 2f, poleDiameter));

            Transform plate = Primitive(stand, PrimitiveType.Cylinder, "Foot", slate);
            plate.position = new Vector3(foot.x, floorY + footHeight / 2f, foot.z);
            plate.rotation = Quaternion.identity;
            SetWorldScale(plate, new Vector3(footDiameter, footHeight / 2f, footDiameter));

            UnityEngine.Material shadow = Material("Stand_Shadow", new Color(0f, 0f, 0f, 0.55f), false, true);
            Texture2D soft = SoftDiscTexture();
            if (shadow.GetTexture("_BaseMap") != soft)
            {
                shadow.SetTexture("_BaseMap", soft);
                EditorUtility.SetDirty(shadow);
                AssetDatabase.SaveAssets();
            }
            Transform disc = Primitive(stand, PrimitiveType.Quad, "ContactShadow", shadow);
            disc.position = new Vector3(foot.x, floorY + 0.003f, foot.z);
            // A quad shows its face toward -Z, so this turns that face up.
            disc.rotation = Quaternion.Euler(90f, 0f, 0f);
            SetWorldScale(disc, new Vector3(discDiameter, discDiameter, 1f));
            disc.GetComponent<MeshRenderer>().sortingOrder = -1;
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// Loads or creates a material asset under <see cref="MaterialFolder"/>: URP Unlit (the same in passthrough and
        /// in the room) or URP Lit. The colour is written to the asset every time. A colour with alpha under 1 is
        /// treated as transparent. Several panels share one asset per name.
        /// </summary>
        public static UnityEngine.Material Material(string name, Color color, bool lit = false, bool transparent = false)
        {
            EnsureFolder(MaterialFolder);
            string path = $"{MaterialFolder}/{name}.mat";
            transparent |= color.a < 0.999f;
            var material = AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit")
                    ?? throw new InvalidOperationException("The URP shader is missing.");
                material = new UnityEngine.Material(shader) { name = name };
                AssetDatabase.CreateAsset(material, path);
            }
            material.SetColor("_BaseColor", color);
            if (lit)
            {
                material.SetFloat("_Smoothness", 0.1f);
                material.SetFloat("_Metallic", 0f);
            }
            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
            }
            else
            {
                material.SetFloat("_Surface", 0f);
                material.SetFloat("_ZWrite", 1f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = -1;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>
        /// A flat rounded rectangle in the XY plane facing -Z, saved as a mesh asset under the kit folder. With
        /// <paramref name="ring"/> above zero it is only the outline of that width. Rounded corners can be limited to the top or the bottom.
        /// </summary>
        public static Mesh RoundedRect(Vector2 size, float radius, float ring, bool roundTop, bool roundBottom)
        {
            EnsureFolder(MeshFolder);
            string key = $"RR_{Mm(size.x)}x{Mm(size.y)}_r{Mm(radius)}_w{Mm(ring)}_{(roundTop ? "T" : "t")}{(roundBottom ? "B" : "b")}";
            string path = $"{MeshFolder}/{key}.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null)
                return mesh;

            float rt = roundTop ? radius : 0f;
            float rb = roundBottom ? radius : 0f;
            // Corner order is counter clockwise from the top right.
            float[] radii = { rt, rt, rb, rb };
            List<Vector3> outer = Loop(size.x / 2f, size.y / 2f, radii);
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            if (ring <= 0f)
            {
                vertices.Add(Vector3.zero);
                vertices.AddRange(outer);
                for (int i = 0; i < outer.Count; i++)
                {
                    int next = (i + 1) % outer.Count;
                    // Clockwise seen from -Z.
                    triangles.Add(0);
                    triangles.Add(1 + next);
                    triangles.Add(1 + i);
                }
            }
            else
            {
                float[] innerRadii = { Mathf.Max(radii[0] - ring, 0f), Mathf.Max(radii[1] - ring, 0f), Mathf.Max(radii[2] - ring, 0f), Mathf.Max(radii[3] - ring, 0f) };
                List<Vector3> inner = Loop(size.x / 2f - ring, size.y / 2f - ring, innerRadii);
                vertices.AddRange(outer);
                vertices.AddRange(inner);
                int count = outer.Count;
                for (int i = 0; i < count; i++)
                {
                    int next = (i + 1) % count;
                    triangles.Add(i);
                    triangles.Add(count + i);
                    triangles.Add(next);
                    triangles.Add(next);
                    triangles.Add(count + i);
                    triangles.Add(count + next);
                }
            }
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            foreach (Vector3 v in vertices)
            {
                normals.Add(Vector3.back);
                uvs.Add(new Vector2(v.x / size.x + 0.5f, v.y / size.y + 0.5f));
            }
            mesh = new Mesh { name = key };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        // The outline, counter clockwise from the top right corner, with each corner an arc of its own radius.
        private static List<Vector3> Loop(float halfW, float halfH, float[] radii)
        {
            var points = new List<Vector3>();
            float[] startAngles = { 0f, 90f, 180f, 270f };
            Vector2[] signs = { new Vector2(1f, 1f), new Vector2(-1f, 1f), new Vector2(-1f, -1f), new Vector2(1f, -1f) };
            for (int corner = 0; corner < 4; corner++)
            {
                float r = radii[corner];
                if (r <= 0.00001f)
                {
                    points.Add(new Vector3(signs[corner].x * halfW, signs[corner].y * halfH, 0f));
                    continue;
                }
                var centre = new Vector2(signs[corner].x * (halfW - r), signs[corner].y * (halfH - r));
                for (int step = 0; step <= ArcSegments; step++)
                {
                    float angle = (startAngles[corner] + 90f * step / ArcSegments) * Mathf.Deg2Rad;
                    points.Add(new Vector3(centre.x + Mathf.Cos(angle) * r, centre.y + Mathf.Sin(angle) * r, 0f));
                }
            }
            return points;
        }

        private static void Part(Transform parent, string name, Mesh mesh, UnityEngine.Material material, Vector3 localPosition, int order)
        {
            var part = new GameObject(name);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.layer = parent.gameObject.layer;
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = part.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.sortingOrder = order;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
        }

        private static Transform Primitive(Transform parent, PrimitiveType type, string name, UnityEngine.Material material)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = name;
            UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
            part.transform.SetParent(parent, true);
            part.layer = parent.gameObject.layer;
            var meshRenderer = part.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return part.transform;
        }

        // Sets the size in world metres whatever the parents' scale is (parents here are only turned and moved).
        private static void SetWorldScale(Transform t, Vector3 world)
        {
            Vector3 parent = t.parent != null ? t.parent.lossyScale : NoScale;
            t.localScale = new Vector3(world.x / parent.x, world.y / parent.y, world.z / parent.z);
        }

        private static Texture2D SoftDiscTexture()
        {
            EnsureFolder(TextureFolder);
            string path = $"{TextureFolder}/SoftDisc.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null)
                return existing;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - d), 2f);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static TMP_FontAsset LoadFont()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
                ?? throw new InvalidOperationException($"The font is missing at {FontPath}.");
        }

        // Same conversion as the checklist builder: a capital letter height in metres to a font size.
        private static float CapToFontSize(TMP_FontAsset font, float capHeight)
        {
            return capHeight * font.faceInfo.pointSize / (font.faceInfo.capLine * font.faceInfo.scale * 0.1f);
        }

        private static int Mm(float metres)
        {
            return Mathf.RoundToInt(metres * 1000f);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
