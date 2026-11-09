using System.IO;
using DIYsect.Classroom;
using DIYsect.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Editor
{
    /// <summary>
    /// The phone's look, shared by every classmate screen step: Inter text, dark glass pills and cards with rounded corners,
    /// and one mint accent, the same palette as the labels in the headsets. Keeping it here means every screen changes together.
    /// </summary>
    public static class ClassmateUiBuilder
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

        // Sizes, in reference units (the short side of the screen is 720).
        public const float ButtonHeight = 64f;
        public const float SmallButtonHeight = 60f;
        public const float BodySize = 24f;
        public const float SmallSize = 19f;
        public const float Gap = 12f;
        public const float Margin = 20f;

        public static readonly Color Glass = new Color(0.035f, 0.065f, 0.085f, 0.74f);
        public static readonly Color GlassStrong = new Color(0.035f, 0.065f, 0.085f, 0.9f);
        public static readonly Color Field = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color Warning = new Color(1f, 0.78f, 0.45f, 1f);
        public static readonly Color Page = new Color(0.027f, 0.047f, 0.063f, 1f);

        private const string SpritePath = "Assets/Materials/PhoneUi/Rounded.png";
        private const int SpriteSize = 64;
        private const int SpriteRadius = 31;

        public enum Kind { Primary, Glass, Quiet }

        /// <summary>A screen overlay canvas under the parent with the usual scaler and, when asked, a raycaster for buttons.</summary>
        public static Canvas NewCanvas(Transform parent, string name, bool clickable, int sortingOrder = 0)
        {
            var canvasObject = new GameObject(name);
            canvasObject.transform.SetParent(parent, false);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            if (clickable)
                canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Inter text that shrinks to fit its box and never spills out of it. Not clickable.</summary>
        public static TextMeshProUGUI AddText(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 anchorMax, FontStyles style = FontStyles.Normal)
        {
            var textObject = new GameObject(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            var label = textObject.AddComponent<TextMeshProUGUI>();
            label.font = Font();
            label.text = text;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.fontSize = size;
            label.enableAutoSizing = true;
            label.fontSizeMax = size;
            label.fontSizeMin = Mathf.Max(12f, size * 0.6f);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            Stretch(label.rectTransform, anchorMin, anchorMax);
            return label;
        }

        /// <summary>A rounded panel. <paramref name="radius"/> is the corner radius in reference units.</summary>
        public static Image AddPanel(Transform parent, string name, Color color, float radius)
        {
            var panelObject = new GameObject(name, typeof(RectTransform));
            panelObject.transform.SetParent(parent, false);
            var image = panelObject.AddComponent<Image>();
            Round(image, color, radius);
            image.raycastTarget = false;
            return image;
        }

        /// <summary>Makes an existing image a rounded panel of the given colour.</summary>
        public static void Round(Image image, Color color, float radius)
        {
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = SpriteRadius / Mathf.Max(1f, radius);
            image.color = color;
        }

        /// <summary>
        /// A pill button with a centred label: mint for the main action, dark glass for the others, or quiet (text only).
        /// Pressing dims it a little; a switched-off button fades. The caller places the rect.
        /// </summary>
        public static Button AddButton(Transform parent, string name, string text, float fontSize, Kind kind, out TextMeshProUGUI label, float height = ButtonHeight)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform));
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            Color fill = kind == Kind.Primary ? GuidePalette.Mint : kind == Kind.Glass ? Glass : new Color(1f, 1f, 1f, 0.07f);
            Round(image, fill, height * 0.5f);
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            ColorBlock colours = button.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = Color.white;
            colours.selectedColor = Color.white;
            colours.pressedColor = new Color(0.78f, 0.78f, 0.78f, 1f);
            colours.disabledColor = new Color(1f, 1f, 1f, 0.45f);
            colours.fadeDuration = 0.08f;
            button.colors = colours;
            ((RectTransform)buttonObject.transform).sizeDelta = new Vector2(0f, height);

            Color ink = kind == Kind.Primary ? GuidePalette.InkDeep : GuidePalette.TextPrimary;
            label = AddText(buttonObject.transform, "Label", text, fontSize, ink, TextAlignmentOptions.Center, Vector2.zero, Vector2.one,
                kind == Kind.Primary ? FontStyles.Bold : FontStyles.Normal);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.rectTransform.offsetMin = new Vector2(height * 0.4f, 0f);
            label.rectTransform.offsetMax = new Vector2(-height * 0.4f, 0f);
            return button;
        }

        /// <summary>A rounded text box with a placeholder. The caller places the rect.</summary>
        public static TMP_InputField AddInputField(Transform parent, string name, string placeholder, float fontSize, int characterLimit, TMP_InputField.ContentType contentType)
        {
            var fieldObject = new GameObject(name, typeof(RectTransform));
            fieldObject.transform.SetParent(parent, false);
            var background = fieldObject.AddComponent<Image>();
            Round(background, Field, ButtonHeight * 0.5f);

            var area = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            area.transform.SetParent(fieldObject.transform, false);
            var areaRect = (RectTransform)area.transform;
            Stretch(areaRect, Vector2.zero, Vector2.one);
            areaRect.offsetMin = new Vector2(24f, 0f);
            areaRect.offsetMax = new Vector2(-24f, 0f);

            TextMeshProUGUI text = AddText(area.transform, "Text", string.Empty, fontSize, GuidePalette.TextPrimary, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            text.enableAutoSizing = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.richText = false;
            TextMeshProUGUI hint = AddText(area.transform, "Placeholder", placeholder, fontSize, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            hint.enableAutoSizing = false;
            hint.textWrappingMode = TextWrappingModes.NoWrap;

            var field = fieldObject.AddComponent<TMP_InputField>();
            field.targetGraphic = background;
            field.textViewport = areaRect;
            field.textComponent = text;
            field.placeholder = hint;
            field.fontAsset = Font();
            field.pointSize = fontSize;
            field.characterLimit = characterLimit;
            field.contentType = contentType;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.caretColor = GuidePalette.Mint;
            field.customCaretColor = true;
            field.selectionColor = GuidePalette.MintLine;
            return field;
        }

        /// <summary>
        /// A glass chip that wraps one line of text and grows with it, anchored at <paramref name="anchor"/>. The text is what the
        /// screen script shows and hides; the chip's background follows it (<see cref="ShowWith"/>), so a hidden message leaves nothing behind.
        /// </summary>
        public static TextMeshProUGUI AddChip(Transform parent, string name, Vector2 anchor, Vector2 position, float fontSize, Color textColour, bool hidden = false)
        {
            Image chip = AddPanel(parent, name + "Chip", Glass, fontSize);
            var rect = chip.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            var layout = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(Mathf.RoundToInt(fontSize * 1.1f), Mathf.RoundToInt(fontSize * 1.1f), Mathf.RoundToInt(fontSize * 0.55f), Mathf.RoundToInt(fontSize * 0.55f));
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            var fitter = chip.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI label = AddText(chip.transform, name, string.Empty, fontSize, textColour, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
            label.enableAutoSizing = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            var follow = label.gameObject.AddComponent<ShowWith>();
            var serialized = new SerializedObject(follow);
            serialized.FindProperty("_companion").objectReferenceValue = chip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            // Hidden from the start: no switch happens at runtime to tell the chip, so it starts off as well.
            label.gameObject.SetActive(!hidden);
            chip.enabled = !hidden;
            return label;
        }

        /// <summary>
        /// A full-screen page with a centred column for the start and name screens. The page is bigger than the safe area, so the
        /// dark background still covers a notch; the column has a fixed width, so it looks the same upright and sideways.
        /// </summary>
        public static RectTransform AddPage(Transform canvas, out GameObject page)
        {
            page = new GameObject("Page", typeof(RectTransform), typeof(Image));
            page.transform.SetParent(canvas, false);
            var backdrop = page.GetComponent<Image>();
            backdrop.color = Page;
            var pageRect = (RectTransform)page.transform;
            Stretch(pageRect, Vector2.zero, Vector2.one);
            pageRect.offsetMin = new Vector2(-600f, -600f);
            pageRect.offsetMax = new Vector2(600f, 600f);

            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(page.transform, false);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(560f, 600f);
            return contentRect;
        }

        /// <summary>Places a rect in a column: <paramref name="top"/> units down from the column's top, full width unless inset.</summary>
        public static void InColumn(RectTransform rect, float top, float height, float left = 0f, float right = 0f)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>The small mint capitals above a title, like the eyebrow on the headset labels.</summary>
        public static TextMeshProUGUI AddEyebrow(Transform parent, string text, float top)
        {
            TextMeshProUGUI eyebrow = AddText(parent, "Eyebrow", text, 17f, GuidePalette.Mint, TextAlignmentOptions.Center, Vector2.zero, Vector2.one, FontStyles.Bold);
            eyebrow.characterSpacing = 18f;
            InColumn(eyebrow.rectTransform, top, 26f);
            return eyebrow;
        }

        public static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>True when the component is missing or its object reference <paramref name="property"/> is empty, so its screen must be built.</summary>
        public static bool NeedsUi(Object component, string property)
        {
            return component == null || new SerializedObject(component).FindProperty(property).objectReferenceValue == null;
        }

        public static TMP_FontAsset Font()
        {
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ProfessorPanelKit.FontPath)
                ?? throw new FileNotFoundException($"{ProfessorPanelKit.FontPath} is missing.");
        }

        // A white rounded square, 9-sliced so any size keeps the same corners. Written once and reused.
        private static Sprite RoundedSprite()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite != null)
                return sprite;
            Directory.CreateDirectory(Path.GetDirectoryName(SpritePath));
            var texture = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
            float centre = (SpriteSize - 1) * 0.5f;
            float inner = centre - SpriteRadius;
            for (int y = 0; y < SpriteSize; y++)
            {
                for (int x = 0; x < SpriteSize; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x - centre) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y - centre) - inner, 0f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(SpriteRadius + 0.5f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(SpriteRadius, SpriteRadius, SpriteRadius, SpriteRadius);
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        }
    }
}
