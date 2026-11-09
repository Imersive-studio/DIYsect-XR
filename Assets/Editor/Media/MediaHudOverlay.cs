using DIYsect.Classroom;
using DIYsect.Networking;
using UnityEngine;
using UnityEngine.UI;

namespace DIYsect.Editor.Media
{
    /// <summary>
    /// Draws the classmate screen's pointer markers and labels, plus a small tag, on a canvas that the capture camera renders.
    /// The pointer HUD itself is immediate-mode GUI, which cannot be drawn into an offscreen image, so this repeats its
    /// layout using the same shapes, colours and label text that <see cref="PointerPresenter"/> produces.
    /// Editor only. It is built for one render and destroyed after it.
    /// </summary>
    public sealed class MediaHudOverlay
    {
        private const float BaseMarker = 64f;
        private const float LabelHeight = 34f;

        private readonly Camera _camera;
        private readonly Canvas _canvas;
        private readonly Font _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        private readonly int _width, _height;
        private readonly float _scale;

        public MediaHudOverlay(Camera camera, int width, int height)
        {
            _camera = camera;
            _width = width;
            _height = height;
            _scale = Mathf.Clamp(Mathf.Min(width, height) / 540f, 1f, 3f);
            var holder = new GameObject("MediaHudCanvas");
            _canvas = holder.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _canvas.worldCamera = camera;
            // Just past the near plane, so nothing in the scene can hide the labels.
            _canvas.planeDistance = camera.nearClipPlane + 0.001f;
        }

        public void Destroy()
        {
            Object.Destroy(_canvas.gameObject);
        }

        /// <summary>Marks each active pointer: ring or diamond on an organ in view, triangle or chevron at the edge, then its label.</summary>
        public void DrawPointers(PointerPresenter presenter)
        {
            float pulse = 1f + 0.12f * (presenter.Pulse - 0.5f) * 2f;
            foreach (PointerEntry entry in presenter.Entries)
                DrawPointer(presenter, entry, pulse);
        }

        /// <summary>A caption tag in the bottom left, the same on every image.</summary>
        public void DrawTag(string text)
        {
            AddLabel(text, new Color(0.75f, 0.75f, 0.75f), new Vector2(12f * _scale, 12f * _scale), new Vector2(0f, 0f), 0.85f);
        }

        private void DrawPointer(PointerPresenter presenter, PointerEntry entry, float pulse)
        {
            Color color = presenter.ColorOf(entry.Role);
            bool professor = entry.Role == PointerRole.Professor;
            PointerPlacement place = entry.Placement;
            var anchor = new Vector2(place.Position.x * _width, place.Position.y * _height);
            float size = BaseMarker * _scale * pulse;

            Texture2D shape = place.OnScreen
                ? (professor ? PointerShapes.Ring() : PointerShapes.Diamond())
                : (professor ? PointerShapes.Triangle() : PointerShapes.Chevron());
            RawImage marker = NewImage("Marker", shape, color);
            Place(marker.rectTransform, anchor, new Vector2(size, size), new Vector2(0.5f, 0.5f));
            if (!place.OnScreen)
                marker.rectTransform.localRotation = Quaternion.Euler(0f, 0f, place.AngleDegrees);

            float height = LabelHeight * _scale;
            Vector2 centre = anchor;
            if (!place.OnScreen)
            {
                Vector2 inward = new Vector2(_width * 0.5f, _height * 0.5f) - anchor;
                centre += inward.normalized * (size * 0.5f + height * 0.5f);
            }
            // The professor's label sits above its marker and the student's below (y is up here).
            centre.y += (professor ? 1f : -1f) * (size * 0.5f + height * 0.5f + 4f * _scale);
            AddLabel(entry.Text, color, centre, new Vector2(0.5f, 0.5f), 1f);
        }

        private void AddLabel(string text, Color accent, Vector2 position, Vector2 pivot, float opacity)
        {
            var textObject = new GameObject("LabelText", typeof(RectTransform));
            Text label = textObject.AddComponent<Text>();
            label.font = _font;
            label.fontSize = Mathf.RoundToInt(18f * _scale);
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.color = Color.white;
            label.text = text;

            float width = label.preferredWidth + 24f * _scale;
            float height = LabelHeight * _scale;
            // Keep the label inside the image, as the HUD does.
            Vector2 origin = position - new Vector2(width * pivot.x, height * pivot.y);
            origin.x = Mathf.Clamp(origin.x, 4f, _width - width - 4f);
            origin.y = Mathf.Clamp(origin.y, 4f, _height - height - 4f);

            RawImage panel = NewImage("LabelPanel", Texture2D.whiteTexture, new Color(0f, 0f, 0f, 0.75f * opacity));
            Place(panel.rectTransform, origin, new Vector2(width, height), Vector2.zero);
            RawImage stripe = NewImage("LabelStripe", Texture2D.whiteTexture, accent);
            Place(stripe.rectTransform, origin, new Vector2(6f * _scale, height), Vector2.zero);
            textObject.transform.SetParent(_canvas.transform, false);
            Place((RectTransform)textObject.transform, origin + new Vector2(6f * _scale, 0f), new Vector2(width - 6f * _scale, height), Vector2.zero);
        }

        private RawImage NewImage(string name, Texture texture, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            image.transform.SetParent(_canvas.transform, false);
            return image;
        }

        // Positions are in pixels from the bottom left of the image.
        private static void Place(RectTransform rect, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }
    }
}
