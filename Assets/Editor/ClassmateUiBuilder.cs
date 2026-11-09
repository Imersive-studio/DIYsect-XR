using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Editor
{
    /// <summary>Small helpers the classmate screen steps share for building uGUI: a canvas, text and thumb-sized buttons.</summary>
    public static class ClassmateUiBuilder
    {
        public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

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

        /// <summary>A text that shrinks to fit its box. Not clickable.</summary>
        public static Text AddText(Transform parent, string name, string text, int fontSize, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var label = textObject.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.alignment = alignment;
            label.color = color;
            label.text = text;
            label.raycastTarget = false;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = fontSize;
            var rect = label.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return label;
        }

        /// <summary>A dark button with a centred label. The caller places the rect.</summary>
        public static Button AddButton(Transform parent, string name, string text, int fontSize, Color background, out Text label)
        {
            var buttonObject = new GameObject(name);
            buttonObject.transform.SetParent(parent, false);
            var image = buttonObject.AddComponent<Image>();
            image.color = background;
            var button = buttonObject.AddComponent<Button>();
            button.targetGraphic = image;
            label = AddText(buttonObject.transform, "Label", text, fontSize, Color.white, TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);
            label.rectTransform.offsetMin = new Vector2(8f, 0f);
            label.rectTransform.offsetMax = new Vector2(-8f, 0f);
            return button;
        }
    }
}
