using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace DIYsect.Interaction
{
    /// <summary>
    /// Builds the TextMeshPro text objects that organ labels, callouts and the inspection title share, so sizes are always
    /// given as a cap height in metres and every label uses the font's own unlit shader (lights never tint it).
    /// </summary>
    public static class LabelText
    {
        /// <summary>TextMeshPro's own shader that draws text over everything. Apply Label Design adds it to the build.</summary>
        public const string OnTopShaderName = "TextMeshPro/Distance Field Overlay";

        // One draw-on-top copy of each font's material, shared by every label that uses it.
        private static readonly Dictionary<Material, Material> OnTopMaterials = new Dictionary<Material, Material>();

        /// <summary>
        /// The font's material on the overlay shader, so text shows through the body wall, with a soft dark halo.
        /// Shared, so every label batches together and nothing leaks a material per label.
        /// </summary>
        public static Material OnTopMaterial(TMP_FontAsset font)
        {
            Material source = font.material;
            if (OnTopMaterials.TryGetValue(source, out Material cached) && cached != null)
                return cached;
            Shader overlay = Shader.Find(OnTopShaderName);
            if (overlay == null)
            {
                Debug.LogWarning($"{OnTopShaderName} is not in the build, so labels can hide behind the body. Run Tools > DIYsect > Apply Label Design.");
                return source;
            }
            var onTop = new Material(source) { name = $"{source.name} (on top)", shader = overlay };
            // A soft ink halo keeps white text readable over pale skin as well as dark tissue.
            onTop.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            onTop.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.03f, 0.08f, 0.11f, 0.85f));
            onTop.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.55f);
            onTop.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.65f);
            OnTopMaterials[source] = onTop;
            return onTop;
        }

        /// <summary>The injected font, or TextMeshPro's default with a clear warning when the setup command has not been run.</summary>
        public static TMP_FontAsset Resolve(TMP_FontAsset injected, Object context)
        {
            if (injected != null)
                return injected;
            Debug.LogWarning("The Inter font is not assigned on this label. Run Tools > DIYsect > Apply Label Design. Using the default font.", context);
            return TMP_Settings.defaultFontAsset;
        }

        /// <summary>Converts a capital letter height in metres to a TextMeshPro font size.</summary>
        public static float CapToFontSize(TMP_FontAsset font, float capHeight)
        {
            float capLine = font.faceInfo.capLine > 0f ? font.faceInfo.capLine : font.faceInfo.pointSize * 0.7f;
            return capHeight * font.faceInfo.pointSize / (capLine * font.faceInfo.scale * 0.1f);
        }

        /// <summary>
        /// A text object under <paramref name="parent"/>. <paramref name="rect"/> is its box in metres. The object's pivot is
        /// <paramref name="pivot"/>, so its local position is that point of the box. With <paramref name="onTop"/> the text
        /// draws over everything in front of it, so a name stays readable while its organ is still under the body wall.
        /// </summary>
        public static TextMeshPro Create(Transform parent, string objectName, TMP_FontAsset font, float capHeight, Color color,
            FontStyles style, TextAlignmentOptions alignment, Vector2 rect, Vector2 pivot, bool wrap, float characterSpacing = 0f, bool onTop = false)
        {
            var child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(parent, false);
            var text = child.AddComponent<TextMeshPro>();
            text.font = font;
            text.fontSharedMaterial = onTop ? OnTopMaterial(font) : font.material;
            text.isOrthographic = false;
            text.rectTransform.sizeDelta = rect;
            text.rectTransform.pivot = pivot;
            text.fontSize = CapToFontSize(font, capHeight);
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.characterSpacing = characterSpacing;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            var meshRenderer = text.GetComponent<MeshRenderer>();
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            return text;
        }
    }
}
